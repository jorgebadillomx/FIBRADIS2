using System.Text.Json;
using System.Security.Claims;
using Application.Fundamentals;
using Application.Catalog;
using Application.Market;
using Application.Ops;
using Application.Portfolio;
using Domain.Market;
using Microsoft.AspNetCore.Mvc;
using SharedApiContracts.Portfolio;

namespace Api.Endpoints.Private;

public static class PortfolioEndpoints
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static IEndpointRouteBuilder MapPortfolio(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/portfolio")
            .RequireAuthorization("User")
            .WithTags("Portfolio");

        group.MapGet("/status", async (
            IPortfolioRepository portfolioRepo,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var userId = GetUserId(ctx);
            var count = await portfolioRepo.GetPositionCountByUserIdAsync(userId, ct);
            return Results.Ok(new { hasPortfolio = count > 0, positionCount = count });
        })
        .Produces<object>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("/snapshot", async (
            IPortfolioRepository portfolioRepo,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var userId = GetUserId(ctx);
            var snapshot = await portfolioRepo.GetSnapshotAsync(userId, ct);
            return Results.Ok(new PortfolioSnapshotStatusDto(snapshot is not null, snapshot?.ArchivedAt));
        })
        .Produces<PortfolioSnapshotStatusDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/archive", async (
            IPortfolioRepository portfolioRepo,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var userId = GetUserId(ctx);
            await portfolioRepo.ArchivePortfolioAsync(userId, ct);
            return Results.NoContent();
        })
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/restore", async (
            IPortfolioRepository portfolioRepo,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var userId = GetUserId(ctx);
            var restored = await portfolioRepo.RestoreSnapshotAsync(userId, ct);
            return restored ? Results.NoContent() : Results.NotFound();
        })
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("/", async (
            IPortfolioRepository portfolioRepo,
            IMarketRepository marketRepo,
            IFibraRepository fibraRepo,
            IFundamentalRepository fundamentalRepo,
            IBmvSchedule bmvSchedule,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var userId = GetUserId(ctx);
            var positions = await portfolioRepo.GetByUserIdAsync(userId, ct);
            if (positions.Count == 0)
                return Results.Ok(new PortfolioResponseDto(null, []));

            var fibraIds = positions.Select(p => p.FibraId).ToArray();

            var latestSnapshots = await marketRepo.GetLatestSnapshotPerFibraAsync(ct);
            var snapshotByFibra = latestSnapshots
                .Where(s => fibraIds.Contains(s.FibraId))
                .ToDictionary(s => s.FibraId);

            var distributions = await marketRepo.GetDistributionsByFibrasAsync(fibraIds, 365, ct);
            var distsByFibra = distributions
                .GroupBy(d => d.FibraId)
                .ToDictionary(g => g.Key, g => (IReadOnlyList<Distribution>)g.OrderByDescending(d => d.PaymentDate).ToList());

            var allFibras = await fibraRepo.GetAllActiveAsync(ct);
            var fibraById = allFibras
                .Where(f => fibraIds.Contains(f.Id))
                .ToDictionary(f => f.Id);

            var latestFundamentals = await fundamentalRepo.GetSummaryLatestAsync(ct);
            var fundamentalByFibra = latestFundamentals
                .Where(row => fibraIds.Contains(row.Record.FibraId))
                .ToDictionary(row => row.Record.FibraId, row => row.Record);

            var week52Avgs = await marketRepo.GetWeek52AvgByFibrasAsync(fibraIds, 365, ct);
            var result = PortfolioKpiCalculator.Calculate(positions, snapshotByFibra, distsByFibra, fibraById);
            var utcNow = DateTimeOffset.UtcNow;
            var isMarketOpen = bmvSchedule.IsTradingHours(utcNow);

            var positionDtos = result.Positions.Select(row =>
            {
                snapshotByFibra.TryGetValue(row.FibraId, out var snapshot);
                fundamentalByFibra.TryGetValue(row.FibraId, out var fundamental);
                var week52AvgFound = week52Avgs.TryGetValue(row.FibraId, out var week52Avg);
                var recentDists = distsByFibra.TryGetValue(row.FibraId, out var dists)
                    ? dists.Take(4)
                        .Select(d => new PortfolioDistributionDto(
                            d.PaymentDate.ToString("yyyy-MM-dd"),
                            d.AmountPerUnit))
                        .ToArray()
                    : Array.Empty<PortfolioDistributionDto>();

                return new PortfolioPositionDto(
                    FibraId: row.FibraId,
                    Ticker: row.Ticker,
                    Nombre: row.Nombre,
                    Titulos: row.Titulos,
                    CostoPromedio: row.CostoPromedio,
                    CostoTotalCompra: row.CostoTotalCompra,
                    PctPortafolio: row.PctPortafolio,
                    PrecioActual: snapshot?.LastPrice,
                    ValorMercado: row.ValorMercado,
                    PlusvaliaFilaPct: row.PlusvaliaFilaPct,
                    PlusvaliaFilaMxn: row.PlusvaliaFilaMxn,
                    RentaAnual: row.RentaAnual,
                    FreshnessStatus: FreshnessClassifier.Classify(snapshot, isMarketOpen, utcNow),
                    CapRate: fundamental?.CapRate,
                    NavPerCbfi: fundamental?.NavPerCbfi,
                    Ltv: fundamental?.Ltv,
                    NoiMargin: fundamental?.NoiMargin,
                    FfoMargin: fundamental?.FfoMargin,
                    DailyChangePct: snapshot?.DailyChangePct,
                    Week52High: snapshot?.Week52High,
                    Volume: snapshot?.Volume,
                    Week52Low: snapshot?.Week52Low,
                    Week52Avg: week52AvgFound ? week52Avg : null,
                    FundamentalsPeriod: fundamental?.Period,
                    RecentDistributions: recentDists);
            }).ToList();

            var kpisDto = new PortfolioKpisDto(
                InversionTotal: result.InversionTotal,
                ValorTotal: result.ValorTotal,
                PlusvaliaTotal_Pct: result.PlusvaliaTotal_Pct,
                PlusvaliaTotal_Mxn: result.PlusvaliaTotal_Mxn,
                RentasAnualesBrutas: result.RentasAnualesBrutas,
                RentasRealesBrutas: result.RentasRealesBrutas,
                PctRentasPortafolio: result.PctRentasPortafolio,
                IsPartial: result.IsPartial);

            return Results.Ok(new PortfolioResponseDto(kpisDto, positionDtos));
        })
        .Produces<PortfolioResponseDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("/column-config", async (
            IPortfolioRepository portfolioRepo,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var userId = GetUserId(ctx);
            var settings = await portfolioRepo.GetSettingsAsync(userId, ct);
            return Results.Ok(new PortfolioColumnConfigDto(ParseColumns(settings?.ColumnConfigJson)));
        })
        .Produces<PortfolioColumnConfigDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPut("/column-config", async (
            PortfolioColumnConfigDto request,
            IPortfolioRepository portfolioRepo,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var userId = GetUserId(ctx);
            var columns = NormalizeColumns(request.Columns);
            var json = JsonSerializer.Serialize(new PortfolioColumnConfigDto(columns), JsonOptions);
            await portfolioRepo.UpsertSettingsAsync(userId, json, ct);
            return Results.NoContent();
        })
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/upload", async (
            IFormFile file,
            IPortfolioUploadService uploadSvc,
            IPortfolioRepository portfolioRepo,
            IFibraRepository fibraRepo,
            IOperationalConfigRepository configRepo,
            HttpContext ctx,
            CancellationToken ct,
            [FromQuery] string mode = "replace",
            [FromQuery] bool force = false) =>
        {
            var userId = GetUserId(ctx);
            var config = await configRepo.GetAsync(ct);
            var activeFibras = await fibraRepo.GetAllActiveAsync(ct);

            await using var stream = file.OpenReadStream();
            var result = await uploadSvc.ParseAndValidateAsync(
                stream, file.FileName, activeFibras, config.CommissionFactor, ct);

            if (!result.Success)
            {
                return Results.Problem(
                    title: "Errores de validación en el archivo",
                    detail: "Corrija los errores y vuelva a subir el archivo.",
                    statusCode: 400,
                    extensions: new Dictionary<string, object?> { ["errors"] = result.Errors });
            }

            var positions = result.Positions.Select(p => new Domain.Portfolio.PortfolioPosition
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                FibraId = p.FibraId,
                Titulos = p.Titulos,
                CostoPromedio = p.CostoPromedio,
                CostoTotalCompra = p.CostoTotalCompra,
                UploadedAt = DateTimeOffset.UtcNow,
            }).ToList();

            if (string.Equals(mode, "merge", StringComparison.OrdinalIgnoreCase))
            {
                var existingPositions = await portfolioRepo.GetByUserIdAsync(userId, ct);
                if (!force && AreDuplicatePositions(positions, existingPositions))
                    return Results.Ok(new PortfolioUploadResponseDto(0) { DuplicateDetected = true });

                await portfolioRepo.MergePositionsAsync(userId, positions, ct);
                return Results.Ok(new PortfolioUploadResponseDto(positions.Count));
            }

            await portfolioRepo.UpsertPortfolioAsync(userId, positions, ct);
            return Results.Ok(new PortfolioUploadResponseDto(positions.Count));
        })
        .DisableAntiforgery()
        .Produces<PortfolioUploadResponseDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPatch("/positions/{fibraId:guid}", async (
            Guid fibraId,
            PortfolioPositionPatchDto request,
            IPortfolioRepository portfolioRepo,
            IOperationalConfigRepository configRepo,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            if (request.Titulos <= 0)
                return Results.Problem("La cantidad debe ser un entero positivo.", statusCode: 400);
            if (request.CostoPromedio <= 0)
                return Results.Problem("El costo promedio debe ser mayor a cero.", statusCode: 400);

            var userId = GetUserId(ctx);
            var position = await portfolioRepo.GetPositionAsync(userId, fibraId, ct);
            if (position is null)
                return Results.NotFound();

            var config = await configRepo.GetAsync(ct);
            if (config is null)
                return Results.Problem("Configuración operacional no inicializada.", statusCode: 500);

            position.Titulos = request.Titulos;
            position.CostoPromedio = request.CostoPromedio;
            position.CostoTotalCompra = request.Titulos * request.CostoPromedio * (1 + config.CommissionFactor);

            await portfolioRepo.UpdatePositionAsync(position, ct);
            return Results.NoContent();
        })
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapDelete("/positions/{fibraId:guid}", async (
            Guid fibraId,
            IPortfolioRepository portfolioRepo,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var userId = GetUserId(ctx);
            var deleted = await portfolioRepo.DeletePositionAsync(userId, fibraId, ct);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        return app;
    }

    private static Guid GetUserId(HttpContext ctx)
        => Guid.Parse(ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private static IReadOnlyList<string> ParseColumns(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        try
        {
            var dto = JsonSerializer.Deserialize<PortfolioColumnConfigDto>(json, JsonOptions);
            return NormalizeColumns(dto?.Columns);
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static IReadOnlyList<string> NormalizeColumns(IReadOnlyList<string>? columns)
    {
        if (columns is null || columns.Count == 0)
            return [];

        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "capRate",
            "navPerCbfi",
            "ltv",
            "noiMargin",
            "ffoMargin",
            "dailyChangePct",
            "week52High",
        };

        return columns
            .Where(column => !string.IsNullOrWhiteSpace(column) && allowed.Contains(column))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool AreDuplicatePositions(
        IReadOnlyList<Domain.Portfolio.PortfolioPosition> parsed,
        IReadOnlyList<Domain.Portfolio.PortfolioPosition> existing)
    {
        if (parsed.Count != existing.Count)
            return false;

        var existingByFibra = existing.ToDictionary(p => p.FibraId);
        return parsed.All(position =>
            existingByFibra.TryGetValue(position.FibraId, out var current)
            && current.Titulos == position.Titulos
            && Math.Abs(current.CostoPromedio - position.CostoPromedio) < 0.001m);
    }
}
