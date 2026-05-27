using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using Application.Catalog;
using Application.Fundamentals;
using Domain.Fundamentals;
using Infrastructure.Integrations.Pdf;
using Microsoft.Extensions.Logging;
using SharedApiContracts.Fundamentals;

namespace Api.Endpoints.Ops;

public static partial class OpsFundamentalsEndpoints
{
    [GeneratedRegex(@"^Q[1-4]-20\d{2}$")]
    private static partial Regex PeriodRegex();

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

        group.MapPost("/extract-kpis", async (
            IFormFile file,
            IKpiExtractorService kpiExtractor,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger("OpsFundamentals");

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

            string markdown;
            try
            {
                await using var stream = file.OpenReadStream();
                markdown = PdfMarkdownExtractor.Extract(stream);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Error al extraer texto del PDF");
                markdown = string.Empty;
            }

            if (string.IsNullOrWhiteSpace(markdown))
            {
                return Results.Ok(new KpiExtractionDto(
                    CapRate: null, CapRateNote: null,
                    NavPerCbfi: null, NavPerCbfiNote: null,
                    Ltv: null, LtvNote: null,
                    NoiMargin: null, NoiMarginNote: null,
                    FfoMargin: null, FfoMarginNote: null,
                    QuarterlyDistribution: null, QuarterlyDistributionNote: null,
                    Summary: null,
                    ExtractionNotes: "PDF sin texto extraíble. Puede ser un PDF escaneado que requiere OCR.",
                    MarkdownLength: 0));
            }

            KpiExtractionResult result;
            try
            {
                result = await kpiExtractor.ExtractAsync(markdown, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Error al extraer KPIs desde PDF");
                return Results.Problem(
                    statusCode: StatusCodes.Status502BadGateway,
                    detail: "El proveedor de IA no está disponible.");
            }

            return Results.Ok(new KpiExtractionDto(
                CapRate: result.CapRate,
                CapRateNote: result.CapRateNote,
                NavPerCbfi: result.NavPerCbfi,
                NavPerCbfiNote: result.NavPerCbfiNote,
                Ltv: result.Ltv,
                LtvNote: result.LtvNote,
                NoiMargin: result.NoiMargin,
                NoiMarginNote: result.NoiMarginNote,
                FfoMargin: result.FfoMargin,
                FfoMarginNote: result.FfoMarginNote,
                QuarterlyDistribution: result.QuarterlyDistribution,
                QuarterlyDistributionNote: result.QuarterlyDistributionNote,
                Summary: result.Summary,
                ExtractionNotes: result.ExtractionNotes ?? string.Empty,
                MarkdownLength: markdown.Length));
        })
        .DisableAntiforgery()
        .Produces<KpiExtractionDto>(StatusCodes.Status200OK)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status502BadGateway)
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
                var markdown = PdfMarkdownExtractor.Extract(mdStream);
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
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger("OpsFundamentals");
            var record = await repo.GetByIdAsync(id, ct);
            if (record is null)
                return Results.NotFound();

            if (string.IsNullOrWhiteSpace(record.MarkdownContent))
            {
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

        return app;
    }
}
