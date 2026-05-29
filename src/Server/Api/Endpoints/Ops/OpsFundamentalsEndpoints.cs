using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using Application.Catalog;
using Application.Fundamentals;
using Application.Jobs;
using Application.News;
using Domain.Fundamentals;
using Domain.Jobs;
using Infrastructure.Integrations.Ai;
using Infrastructure.Integrations.Pdf;
using Microsoft.Extensions.Logging;
using SharedApiContracts.Fundamentals;

namespace Api.Endpoints.Ops;

public static partial class OpsFundamentalsEndpoints
{
    [GeneratedRegex(@"^Q[1-4]-20\d{2}$")]
    private static partial Regex PeriodRegex();

    private sealed record DiagnoseExtractionRequest(string Markdown);

    private static IReadOnlyDictionary<string, string>? DeserializeFieldNotes(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, string?>>(json);
            if (dict is null) return null;
            var filtered = dict
                .Where(kv => kv.Value is not null)
                .ToDictionary(kv => kv.Key, kv => kv.Value!);
            return filtered.Count > 0 ? filtered : null;
        }
        catch { return null; }
    }

    public static IEndpointRouteBuilder MapOpsFundamentals(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ops/fundamentals")
            .RequireAuthorization("AdminOps")
            .WithTags("Fundamentals");

        group.MapPost("/import", async (
            ImportFundamentalsRequest request,
            IFundamentalRepository repo,
            IFibraRepository fibraRepo,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var period = request.Period ?? "";
            if (!PeriodRegex().IsMatch(period))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["period"] = ["Formato inválido. Use 'Q1-2024', 'Q2-2024', etc."],
                });
            }

            var fibra = await fibraRepo.GetByIdAsync(request.FibraId, ct);
            if (fibra is null)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["fibraId"] = [$"No existe una FIBRA con id '{request.FibraId}'."],
                });
            }

            var presentFields = new List<string>();
            var missingFields = new List<string>();
            void Check(string name, object? value)
            {
                if (value is not null) presentFields.Add(name);
                else missingFields.Add(name);
            }
            Check("capRate", request.CapRate);
            Check("navPerCbfi", request.NavPerCbfi);
            Check("ltv", request.Ltv);
            Check("noiMargin", request.NoiMargin);
            Check("ffoMargin", request.FfoMargin);
            Check("quarterlyDistribution", request.QuarterlyDistribution);

            var status = presentFields.Count >= 6 ? "pending" : "partial";

            var existing = await repo.GetProcessedByFibraAndPeriodAsync(request.FibraId, period, ct);
            var isPossibleUpdate = existing is not null;
            string? warningMessage = isPossibleUpdate
                ? $"Ya existe un registro procesado para {fibra.Ticker} / {period}. Se requiere Reprocess explícito para sobreescribir."
                : null;

            var actor = ctx.User.Identity?.Name
                ?? ctx.User.FindFirstValue(ClaimTypes.Email)
                ?? ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? "unknown";

            var record = new FundamentalRecord
            {
                Id = Guid.NewGuid(),
                FibraId = request.FibraId,
                Period = period,
                Status = status,
                ProcessingMode = "manual",
                CapRate = request.CapRate,
                NavPerCbfi = request.NavPerCbfi,
                Ltv = request.Ltv,
                NoiMargin = request.NoiMargin,
                FfoMargin = request.FfoMargin,
                QuarterlyDistribution = request.QuarterlyDistribution,
                Summary = request.Summary,
                PdfReference = request.PdfReference,
                IsPossibleUpdate = isPossibleUpdate,
                ImportedBy = actor,
                CapturedAt = DateTimeOffset.UtcNow,
            };

            if (request.FieldNotes is { Count: > 0 })
                record.SetFieldNotes(request.FieldNotes.ToDictionary(kv => kv.Key, kv => (string?)kv.Value));

            await repo.AddAsync(record, ct);

            return Results.Ok(new FundamentalPreviewDto(
                Id: record.Id,
                FibraTicker: fibra.Ticker,
                Period: record.Period,
                Status: record.Status,
                IsPossibleUpdate: record.IsPossibleUpdate,
                WarningMessage: warningMessage,
                PresentFields: presentFields.AsReadOnly(),
                MissingFields: missingFields.AsReadOnly(),
                PdfReference: record.PdfReference,
                CapturedAt: record.CapturedAt,
                HasMarkdownContent: !string.IsNullOrWhiteSpace(record.MarkdownContent)));
        })
        .Produces<FundamentalPreviewDto>(StatusCodes.Status200OK)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPost("/upload-pdf", async (
            IFormFile file,
            Guid fibraId,
            string period,
            IFundamentalRepository repo,
            IFibraRepository fibraRepo,
            IConfiguration config,
            IPipelineErrorLogRepository errorLogRepo,
            ILoggerFactory loggerFactory,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger("OpsFundamentals");

            if (!PeriodRegex().IsMatch(period))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["period"] = ["Formato inválido. Use 'Q1-2024', 'Q2-2024', etc."],
                });
            }

            var fibra = await fibraRepo.GetByIdAsync(fibraId, ct);
            if (fibra is null)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["fibraId"] = [$"No existe una FIBRA con id '{fibraId}'."],
                });
            }

            if (!string.Equals(file.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["file"] = ["Solo se aceptan archivos PDF."],
                });
            }

            const long maxSizeBytes = 20L * 1024 * 1024;
            if (file.Length > maxSizeBytes)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["file"] = ["El archivo excede el tamaño máximo de 20 MB."],
                });
            }

            var existing = await repo.GetProcessedByFibraAndPeriodAsync(fibraId, period, ct);
            var isPossibleUpdate = existing is not null;

            var actor = ctx.User.Identity?.Name
                ?? ctx.User.FindFirstValue(ClaimTypes.Email)
                ?? ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? "unknown";

            var id = Guid.NewGuid();
            var basePath = config["Uploads:BasePath"] ?? "wwwroot/uploads/fundamentals";
            Directory.CreateDirectory(basePath);
            var fileName = $"{id}.pdf";
            var fullPath = Path.Combine(basePath, fileName);

            try
            {
                await using var stream = File.Create(fullPath);
                await file.CopyToAsync(stream, ct);
            }
            catch (IOException ex)
            {
                logger.LogError(ex, "Error al guardar PDF en disco para record {RecordId}", id);
                try
                {
                    await errorLogRepo.LogErrorAsync(new PipelineErrorLog
                    {
                        Pipeline = "Fundamentals",
                        Timestamp = DateTimeOffset.UtcNow,
                        ErrorType = ex.GetType().Name.Length > 100 ? ex.GetType().Name[..100] : ex.GetType().Name,
                        Message = ex.Message,
                        Context = JsonSerializer.Serialize(new { recordId = id, fibraId, period, fileName }),
                        AiContext = $"Error al guardar el archivo PDF en disco para la importación de fundamentales de {fibra.Ticker} / {period}.",
                    }, CancellationToken.None);
                }
                catch (Exception logEx) { logger.LogWarning(logEx, "No se pudo guardar en PipelineErrorLog"); }

                return Results.Problem(
                    title: "Error al guardar el archivo",
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError);
            }

            var relativePath = $"uploads/fundamentals/{fileName}";
            var markdownExtracted = false;
            string? markdownContent = null;

            try
            {
                await using var mdStream = File.OpenRead(fullPath);
                markdownContent = MarkdownCompactor.Compact(PdfMarkdownExtractor.Extract(mdStream));
                markdownExtracted = !string.IsNullOrWhiteSpace(markdownContent);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "No se pudo extraer markdown del PDF para record {RecordId}", id);
                try
                {
                    await errorLogRepo.LogErrorAsync(new PipelineErrorLog
                    {
                        Pipeline = "Fundamentals",
                        Timestamp = DateTimeOffset.UtcNow,
                        ErrorType = ex.GetType().Name.Length > 100 ? ex.GetType().Name[..100] : ex.GetType().Name,
                        Message = ex.Message,
                        Context = JsonSerializer.Serialize(new { recordId = id, fibraId, period }),
                        AiContext = $"Error al extraer y compactar el texto markdown del PDF de {fibra.Ticker} / {period}. El PDF se guardó en disco pero el MarkdownContent quedará vacío.",
                    }, CancellationToken.None);
                }
                catch (Exception logEx) { logger.LogWarning(logEx, "No se pudo guardar en PipelineErrorLog"); }
            }

            var record = new FundamentalRecord
            {
                Id = id,
                FibraId = fibraId,
                Period = period,
                Status = "pending",
                ProcessingMode = "ai",
                MarkdownContent = markdownContent,
                PdfReference = relativePath,
                PdfUploadedAt = DateTimeOffset.UtcNow,
                IsPossibleUpdate = isPossibleUpdate,
                ImportedBy = actor,
                CapturedAt = DateTimeOffset.UtcNow,
            };

            await repo.AddAsync(record, ct);

            return Results.Ok(new PdfUploadResultDto(
                Id: id,
                FibraTicker: fibra.Ticker,
                Period: period,
                MarkdownExtracted: markdownExtracted,
                IsPossibleUpdate: isPossibleUpdate,
                WarningMessage: isPossibleUpdate
                    ? $"Ya existe un registro procesado para {fibra.Ticker} / {period}."
                    : null));
        })
        .DisableAntiforgery()
        .Produces<PdfUploadResultDto>(StatusCodes.Status200OK)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status500InternalServerError)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPost("/{id:guid}/confirm", async (
            Guid id,
            IFundamentalRepository repo,
            IFibraRepository fibraRepo,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var record = await repo.GetByIdAsync(id, ct);
            if (record is null)
                return Results.NotFound();

            if (record.Status is not ("pending" or "partial"))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["status"] = [$"El registro tiene estado '{record.Status}'. Solo se puede confirmar un registro en estado 'pending' o 'partial'."],
                });
            }

            var actor = ctx.User.Identity?.Name
                ?? ctx.User.FindFirstValue(ClaimTypes.Email)
                ?? ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? "unknown";

            var confirmedAt = DateTimeOffset.UtcNow;
            await repo.UpdateStatusAsync(id, "processed", actor, confirmedAt, ct);

            var fibra = await fibraRepo.GetByIdAsync(record.FibraId, ct);

            return Results.Ok(new FundamentalRecordDto(
                Id: record.Id,
                FibraTicker: fibra?.Ticker ?? record.FibraId.ToString(),
                Period: record.Period,
                Status: "processed",
                IsPossibleUpdate: record.IsPossibleUpdate,
                CapRate: record.CapRate,
                NavPerCbfi: record.NavPerCbfi,
                Ltv: record.Ltv,
                NoiMargin: record.NoiMargin,
                FfoMargin: record.FfoMargin,
                QuarterlyDistribution: record.QuarterlyDistribution,
                Summary: record.Summary,
                PdfReference: record.PdfReference,
                PdfUploadedAt: record.PdfUploadedAt,
                ImportedBy: record.ImportedBy,
                ConfirmedBy: actor,
                CapturedAt: record.CapturedAt,
                ConfirmedAt: confirmedAt,
                HasMarkdownContent: !string.IsNullOrWhiteSpace(record.MarkdownContent),
                FieldNotes: DeserializeFieldNotes(record.FieldNotesJson)));
        })
        .Produces<FundamentalRecordDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPost("/{id:guid}/pdf", async (
            Guid id,
            IFormFile file,
            IFundamentalRepository repo,
            IConfiguration config,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var record = await repo.GetByIdAsync(id, ct);
            if (record is null)
                return Results.NotFound();

            if (!string.Equals(file.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["file"] = ["Solo se aceptan archivos PDF."],
                });
            }

            const long maxSizeBytes = 20L * 1024 * 1024;
            if (file.Length > maxSizeBytes)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["file"] = ["El archivo excede el tamaño máximo de 20 MB."],
                });
            }

            var basePath = config["Uploads:BasePath"] ?? "wwwroot/uploads/fundamentals";
            Directory.CreateDirectory(basePath);
            var fileName = $"{id}.pdf";
            var fullPath = Path.Combine(basePath, fileName);
            try
            {
                await using var stream = File.Create(fullPath);
                await file.CopyToAsync(stream, ct);
            }
            catch (IOException ex)
            {
                return Results.Problem(
                    title: "Error al guardar el archivo",
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError);
            }

            var relativePath = $"uploads/fundamentals/{fileName}";
            await repo.UpdatePdfReferenceAsync(id, relativePath, ct);

            var markdownExtracted = false;
            try
            {
                await using var mdStream = File.OpenRead(fullPath);
                var markdown = MarkdownCompactor.Compact(PdfMarkdownExtractor.Extract(mdStream));
                if (!string.IsNullOrWhiteSpace(markdown))
                {
                    await repo.UpdateMarkdownContentAsync(id, markdown, ct);
                    markdownExtracted = true;
                }
            }
            catch (Exception ex)
            {
                var logger = loggerFactory.CreateLogger("OpsFundamentals");
                logger.LogWarning(ex, "No se pudo extraer markdown del PDF {RecordId}", id);
            }

            return Results.Ok(new { path = relativePath, markdownExtracted });
        })
        .DisableAntiforgery()
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapGet("/{id:guid}/pdf", async (
            Guid id,
            IFundamentalRepository repo,
            IConfiguration config,
            CancellationToken ct) =>
        {
            var record = await repo.GetByIdAsync(id, ct);
            if (record is null || record.PdfReference is null)
                return Results.NotFound();

            var basePath = config["Uploads:BasePath"] ?? "wwwroot/uploads/fundamentals";
            var fullPath = Path.Combine(basePath, $"{id}.pdf");

            if (!File.Exists(fullPath))
                return Results.NotFound();

            return Results.File(fullPath, "application/pdf", $"{id}.pdf");
        })
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPost("/{id:guid}/extract-kpis", async (
            Guid id,
            IFundamentalRepository repo,
            IFibraRepository fibraRepo,
            IKpiExtractorService kpiExtractor,
            IPipelineErrorLogRepository errorLogRepo,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger("OpsFundamentals");
            var record = await repo.GetByIdAsync(id, ct);
            if (record is null)
                return Results.NotFound();

            if (string.IsNullOrWhiteSpace(record.MarkdownContent))
            {
                try
                {
                    await errorLogRepo.LogErrorAsync(new PipelineErrorLog
                    {
                        Pipeline = "Fundamentals",
                        Timestamp = DateTimeOffset.UtcNow,
                        ErrorType = "EmptyMarkdown",
                        Message = "El registro no tiene MarkdownContent al intentar extraer KPIs.",
                        Context = JsonSerializer.Serialize(new { recordId = id, fibraId = record.FibraId, period = record.Period }),
                        AiContext = $"No se pudo extraer KPIs del registro {id} ({record.Period}) porque el MarkdownContent está vacío. El PDF pudo no tener texto extraíble.",
                    }, CancellationToken.None);
                }
                catch (Exception logEx) { logger.LogWarning(logEx, "No se pudo guardar en PipelineErrorLog"); }

                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["markdownContent"] = ["El registro no tiene contenido markdown. Sube el PDF primero."],
                });
            }

            KpiExtractionResult result;
            try
            {
                result = await kpiExtractor.ExtractAsync(record.MarkdownContent, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Error al extraer KPIs para registro {RecordId}", id);
                try
                {
                    await errorLogRepo.LogErrorAsync(new PipelineErrorLog
                    {
                        Pipeline = "Fundamentals",
                        Timestamp = DateTimeOffset.UtcNow,
                        ErrorType = ex.GetType().Name.Length > 100 ? ex.GetType().Name[..100] : ex.GetType().Name,
                        Message = ex.Message,
                        Context = JsonSerializer.Serialize(new { recordId = id, fibraId = record.FibraId, period = record.Period, markdownLength = record.MarkdownContent.Length }),
                        AiContext = $"El extractor de KPIs por IA falló para el registro {id} de {record.Period}. El proveedor de IA devolvió un error. El markdown tenía {record.MarkdownContent.Length} caracteres.",
                    }, CancellationToken.None);
                }
                catch (Exception logEx) { logger.LogWarning(logEx, "No se pudo guardar en PipelineErrorLog"); }

                return Results.Problem(
                    statusCode: StatusCodes.Status502BadGateway,
                    detail: "El proveedor de IA no está disponible.");
            }

            await repo.UpdateKpiExtractionAsync(id, result, ct);

            var updated = await repo.GetByIdAsync(id, ct);
            var fibra = await fibraRepo.GetByIdAsync(record.FibraId, ct);

            return Results.Ok(new FundamentalRecordDto(
                Id: updated!.Id,
                FibraTicker: fibra?.Ticker ?? record.FibraId.ToString(),
                Period: updated.Period,
                Status: updated.Status,
                IsPossibleUpdate: updated.IsPossibleUpdate,
                CapRate: updated.CapRate,
                NavPerCbfi: updated.NavPerCbfi,
                Ltv: updated.Ltv,
                NoiMargin: updated.NoiMargin,
                FfoMargin: updated.FfoMargin,
                QuarterlyDistribution: updated.QuarterlyDistribution,
                Summary: updated.Summary,
                PdfReference: updated.PdfReference,
                PdfUploadedAt: updated.PdfUploadedAt,
                ImportedBy: updated.ImportedBy,
                ConfirmedBy: updated.ConfirmedBy,
                CapturedAt: updated.CapturedAt,
                ConfirmedAt: updated.ConfirmedAt,
                HasMarkdownContent: !string.IsNullOrWhiteSpace(updated.MarkdownContent),
                FieldNotes: DeserializeFieldNotes(updated.FieldNotesJson)));
        })
        .Produces<FundamentalRecordDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status502BadGateway)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPatch("/{id:guid}/kpis", async (
            Guid id,
            PatchKpisRequest request,
            IFundamentalRepository repo,
            IFibraRepository fibraRepo,
            CancellationToken ct) =>
        {
            var record = await repo.GetByIdAsync(id, ct);
            if (record is null)
                return Results.NotFound();

            await repo.UpdateKpisManualAsync(
                id,
                request.CapRate, request.NavPerCbfi, request.Ltv,
                request.NoiMargin, request.FfoMargin, request.QuarterlyDistribution,
                request.Summary,
                ct);

            var updated = await repo.GetByIdAsync(id, ct);
            var fibra = await fibraRepo.GetByIdAsync(record.FibraId, ct);

            return Results.Ok(new FundamentalRecordDto(
                Id: updated!.Id,
                FibraTicker: fibra?.Ticker ?? record.FibraId.ToString(),
                Period: updated.Period,
                Status: updated.Status,
                IsPossibleUpdate: updated.IsPossibleUpdate,
                CapRate: updated.CapRate,
                NavPerCbfi: updated.NavPerCbfi,
                Ltv: updated.Ltv,
                NoiMargin: updated.NoiMargin,
                FfoMargin: updated.FfoMargin,
                QuarterlyDistribution: updated.QuarterlyDistribution,
                Summary: updated.Summary,
                PdfReference: updated.PdfReference,
                PdfUploadedAt: updated.PdfUploadedAt,
                ImportedBy: updated.ImportedBy,
                ConfirmedBy: updated.ConfirmedBy,
                CapturedAt: updated.CapturedAt,
                ConfirmedAt: updated.ConfirmedAt,
                HasMarkdownContent: !string.IsNullOrWhiteSpace(updated.MarkdownContent),
                FieldNotes: DeserializeFieldNotes(updated.FieldNotesJson)));
        })
        .Produces<FundamentalRecordDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapGet("/", async (
            Guid fibraId,
            IFundamentalRepository repo,
            IFibraRepository fibraRepo,
            CancellationToken ct) =>
        {
            var fibra = await fibraRepo.GetByIdAsync(fibraId, ct);
            if (fibra is null)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["fibraId"] = [$"No existe una FIBRA con id '{fibraId}'."],
                });
            }

            var records = await repo.GetByFibraAsync(fibraId, ct);
            var dtos = records.Select(r => new FundamentalRecordDto(
                Id: r.Id,
                FibraTicker: fibra.Ticker,
                Period: r.Period,
                Status: r.Status,
                IsPossibleUpdate: r.IsPossibleUpdate,
                CapRate: r.CapRate,
                NavPerCbfi: r.NavPerCbfi,
                Ltv: r.Ltv,
                NoiMargin: r.NoiMargin,
                FfoMargin: r.FfoMargin,
                QuarterlyDistribution: r.QuarterlyDistribution,
                Summary: r.Summary,
                PdfReference: r.PdfReference,
                PdfUploadedAt: r.PdfUploadedAt,
                ImportedBy: r.ImportedBy,
                ConfirmedBy: r.ConfirmedBy,
                CapturedAt: r.CapturedAt,
                ConfirmedAt: r.ConfirmedAt,
                HasMarkdownContent: !string.IsNullOrWhiteSpace(r.MarkdownContent),
                FieldNotes: DeserializeFieldNotes(r.FieldNotesJson))).ToList();

            return Results.Ok(dtos);
        })
        .Produces<IReadOnlyList<FundamentalRecordDto>>(StatusCodes.Status200OK)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        // TEMPORAL: diagnóstico — eliminar cuando se resuelva la regresión de la IA
        group.MapPost("/diagnose-extraction", async (
            DiagnoseExtractionRequest request,
            IConfiguration config,
            IAiProviderConfigRepository providerRepo,
            IAiPromptRepository promptRepo,
            IHttpClientFactory httpClientFactory,
            CancellationToken ct) =>
        {
            const int maxChars = 80_000;
            const string geminiBase = "https://generativelanguage.googleapis.com/v1beta/models";

            var apiKey = config["Gemini:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
                return Results.Problem("Gemini:ApiKey no está configurado.", statusCode: StatusCodes.Status500InternalServerError);

            var providerConfig = await providerRepo.GetConfigAsync(ct);
            var storedPrompt = await promptRepo.GetPromptAsync(AiPromptTemplateDefaults.KpiExtractionContentType, ct);
            var promptTemplate = storedPrompt?.PromptTemplate ?? AiPromptTemplateDefaults.KpiExtraction;
            var promptSource = storedPrompt is not null ? "db" : "default";

            var markdown = request.Markdown;
            var wasTruncated = false;
            if (markdown.Length > maxChars)
            {
                markdown = markdown[..maxChars];
                wasTruncated = true;
            }

            var prompt = promptTemplate.Replace("{markdown_content}", markdown, StringComparison.Ordinal);
            var url = $"{geminiBase}/{providerConfig.ModelId}:generateContent?key={apiKey}";

            var body = new
            {
                contents = new[] { new { parts = new[] { new { text = prompt } } } },
                generationConfig = new { maxOutputTokens = 2048, responseMimeType = "application/json" },
            };

            using var http = httpClientFactory.CreateClient();
            http.Timeout = TimeSpan.FromSeconds(120);
            using var httpResponse = await http.PostAsJsonAsync(url, body, ct);
            var rawApiBody = await httpResponse.Content.ReadAsStringAsync(ct);

            string? extractedText = null;
            try
            {
                using var doc = JsonDocument.Parse(rawApiBody);
                var root = doc.RootElement;
                if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
                {
                    var candidate = candidates[0];
                    if (candidate.TryGetProperty("content", out var content) &&
                        content.TryGetProperty("parts", out var parts) &&
                        parts.GetArrayLength() > 0 &&
                        parts[0].TryGetProperty("text", out var textProp))
                    {
                        extractedText = textProp.GetString();
                    }
                }
            }
            catch { /* rawApiBody still returned below */ }

            JsonElement? parsedKpis = null;
            if (!string.IsNullOrWhiteSpace(extractedText))
            {
                var jsonStr = extractedText;
                var start = jsonStr.IndexOf('{');
                var end = jsonStr.LastIndexOf('}');
                if (start >= 0 && end > start)
                    jsonStr = jsonStr[start..(end + 1)];
                try
                {
                    var kpisDoc = JsonDocument.Parse(jsonStr);
                    parsedKpis = kpisDoc.RootElement.Clone();
                }
                catch { /* JSON inválido — se retorna null */ }
            }

            return Results.Ok(new
            {
                Provider = providerConfig.Provider.ToString(),
                Model = providerConfig.ModelId,
                PromptSource = promptSource,
                OriginalMarkdownLength = request.Markdown.Length,
                SentMarkdownLength = markdown.Length,
                WasTruncated = wasTruncated,
                HttpStatus = (int)httpResponse.StatusCode,
                PromptUsed = prompt,
                RawGeminiApiResponse = rawApiBody,
                ExtractedText = extractedText,
                ParsedKpis = parsedKpis,
            });
        })
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        return app;
    }
}
