using System.Security.Claims;
using System.Text.RegularExpressions;
using Application.Catalog;
using Application.Fundamentals;
using Domain.Fundamentals;
using SharedApiContracts.Fundamentals;

namespace Api.Endpoints.Ops;

public static partial class OpsFundamentalsEndpoints
{
    [GeneratedRegex(@"^Q[1-4]-20\d{2}$")]
    private static partial Regex PeriodRegex();

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

            if (presentFields.Count == 0)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["fields"] = ["Se requiere al menos un campo numérico con valor."],
                });
            }

            var status = presentFields.Count < 6 ? "partial" : "pending";

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
                CapturedAt: record.CapturedAt));
        })
        .Produces<FundamentalPreviewDto>(StatusCodes.Status200OK)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
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
                ConfirmedAt: confirmedAt));
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

            return Results.Ok(new { path = relativePath });
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
                ConfirmedAt: r.ConfirmedAt)).ToList();

            return Results.Ok(dtos);
        })
        .Produces<IReadOnlyList<FundamentalRecordDto>>(StatusCodes.Status200OK)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        return app;
    }
}
