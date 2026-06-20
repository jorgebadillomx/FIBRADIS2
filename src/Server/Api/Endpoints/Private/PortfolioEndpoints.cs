using System.Globalization;
using System.Text.Json;
using System.Security.Claims;
using Application.Fundamentals;
using Application.Catalog;
using Application.Market;
using Application.Opportunities;
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
            if (TryGetUserId(ctx) is not { } userId)
                return Results.Problem(statusCode: StatusCodes.Status401Unauthorized);

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
            if (TryGetUserId(ctx) is not { } userId)
                return Results.Problem(statusCode: StatusCodes.Status401Unauthorized);

            var snapshot = await portfolioRepo.GetSnapshotAsync(userId, ct);
            return Results.Ok(new PortfolioSnapshotStatusDto(snapshot is not null, snapshot?.ArchivedAt));
        })
        .Produces<PortfolioSnapshotStatusDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("/config", async (
            IOperationalConfigRepository configRepo,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            if (TryGetUserId(ctx) is not { } userId)
                return Results.Problem(statusCode: StatusCodes.Status401Unauthorized);

            var config = await configRepo.GetAsync(ct);
            return Results.Ok(new PortfolioConfigDto(config?.CommissionFactor ?? 0m));
        })
        .Produces<PortfolioConfigDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("/performance", async (
            string? range,
            IPortfolioRepository portfolioRepo,
            IFibraRepository fibraRepo,
            IMarketRepository marketRepo,
            IInpcRepository inpcRepo,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            if (TryGetUserId(ctx) is not { } userId)
                return Results.Problem(statusCode: StatusCodes.Status401Unauthorized);

            var positions = await portfolioRepo.GetByUserIdAsync(userId, ct);
            if (positions.Count == 0)
                return Results.Ok(new PortfolioPerformanceResponseDto([], [], [], null));

            var days = ResolvePerformanceDays(range);
            if (days is null)
                return Results.Problem("Valor de 'range' inválido. Válidos: 30d, 90d, 1y, all.", statusCode: 400);

            var portfolioSeries = await BuildPerformanceSeriesAsync(positions, days.Value, marketRepo, ct);
            var inpcSeries = await BuildInpcSeriesAsync(portfolioSeries, inpcRepo, ct);
            var benchmarkTickers = new[] { "^MXX", "^GSPC" };
            var benchmarkSeries = new Dictionary<string, IReadOnlyList<PortfolioPerformancePointDto>>(StringComparer.OrdinalIgnoreCase);

            foreach (var benchmarkTicker in benchmarkTickers)
            {
                var benchmark = await fibraRepo.GetByTickerAsync(benchmarkTicker, ct);
                if (benchmark is null)
                {
                    benchmarkSeries[benchmarkTicker] = [];
                    continue;
                }

                var snapshots = await marketRepo.GetDailySnapshotsAsync(benchmark.Id, days.Value, ct);
                benchmarkSeries[benchmarkTicker] = BuildNormalizedSeries(snapshots);
            }

            benchmarkSeries.TryGetValue("^MXX", out var ipcSeries);
            benchmarkSeries.TryGetValue("^GSPC", out var sp500Series);

            return Results.Ok(new PortfolioPerformanceResponseDto(
                portfolioSeries,
                ipcSeries ?? [],
                sp500Series ?? [],
                inpcSeries));
        })
        .Produces<PortfolioPerformanceResponseDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/archive", async (
            IPortfolioRepository portfolioRepo,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            if (TryGetUserId(ctx) is not { } userId)
                return Results.Problem(statusCode: StatusCodes.Status401Unauthorized);

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
            if (TryGetUserId(ctx) is not { } userId)
                return Results.Problem(statusCode: StatusCodes.Status401Unauthorized);

            var restored = await portfolioRepo.RestoreSnapshotAsync(userId, ct);
            return restored ? Results.NoContent() : Results.NotFound();
        })
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("/", async (
            IPortfolioRepository portfolioRepo,
            IOpportunityWeightsRepository weightsRepo,
            IMarketRepository marketRepo,
            IFibraRepository fibraRepo,
            IFundamentalRepository fundamentalRepo,
            IBmvSchedule bmvSchedule,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            if (TryGetUserId(ctx) is not { } userId)
                return Results.Problem(statusCode: StatusCodes.Status401Unauthorized);

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
            var utcNow = DateTimeOffset.UtcNow;
            var annualDistCutoff = DateOnly.FromDateTime(utcNow.UtcDateTime).AddDays(-365);
            var annualDistByFibra = distributions
                .GroupBy(d => d.FibraId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Where(d => d.PaymentDate >= annualDistCutoff).Sum(d => d.AmountPerUnit));
            var opportunityWeights = await OpportunityEndpoints.ResolveWeightsAsync(weightsRepo, userId, ct);
            var opportunityScores = OpportunityScoreCalculator.Calculate(
                fibraById.Values.ToList(),
                snapshotByFibra,
                fundamentalByFibra,
                annualDistByFibra,
                week52Avgs,
                opportunityWeights);
            var scoreByFibraId = opportunityScores.ToDictionary(score => score.FibraId);
            var result = PortfolioKpiCalculator.Calculate(positions, snapshotByFibra, distsByFibra, fibraById);
            var isMarketOpen = bmvSchedule.IsTradingHours(utcNow);

            var positionDtos = result.Positions.Select(row =>
            {
                snapshotByFibra.TryGetValue(row.FibraId, out var snapshot);
                fundamentalByFibra.TryGetValue(row.FibraId, out var fundamental);
                var week52AvgFound = week52Avgs.TryGetValue(row.FibraId, out var week52Avg);
                var opportunityScore = scoreByFibraId.TryGetValue(row.FibraId, out var opportunity)
                    ? opportunity.Score
                    : null;
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
                    Yoc: row.Yoc,
                    OpportunityScore: opportunityScore,
                    LogoUrl: $"/logos/{row.Ticker.ToLowerInvariant()}.png",
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
                YieldPortafolio: result.YieldPortafolio,
                IngresoMensual: result.IngresoMensual,
                RentasAnualesBrutas: result.RentasAnualesBrutas,
                RentasRealesBrutas: result.RentasRealesBrutas,
                PctRentasPortafolio: result.PctRentasPortafolio,
                IsPartial: result.IsPartial);

            return Results.Ok(new PortfolioResponseDto(kpisDto, positionDtos));
        })
        .Produces<PortfolioResponseDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("/calendar", async (
            DateOnly? from,
            DateOnly? to,
            IPortfolioRepository portfolioRepo,
            IMarketRepository marketRepo,
            IFibraRepository fibraRepo,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            if (TryGetUserId(ctx) is not { } userId)
                return Results.Problem(statusCode: StatusCodes.Status401Unauthorized);

            var positions = await portfolioRepo.GetByUserIdAsync(userId, ct);
            if (positions.Count == 0)
                return Results.Ok(Array.Empty<PortfolioCalendarEventDto>());

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var rangeFrom = from ?? today.AddMonths(-2);
            var rangeTo = to ?? today.AddMonths(1);

            var fibraIds = positions.Select(p => p.FibraId).ToHashSet();
            var distributions = await marketRepo.GetDistributionsByRangeAsync(rangeFrom, rangeTo, ct);
            var relevantDists = distributions
                .Where(d => fibraIds.Contains(d.FibraId) && d.PaymentDate >= rangeFrom && d.PaymentDate <= rangeTo)
                .ToList();

            if (relevantDists.Count == 0)
                return Results.Ok(Array.Empty<PortfolioCalendarEventDto>());

            var allFibras = await fibraRepo.GetAllActiveAsync(ct);
            var fibraById = allFibras.ToDictionary(f => f.Id);
            var posByFibra = positions.ToDictionary(p => p.FibraId);

            var events = relevantDists
                .Select(d =>
                {
                    var pos = posByFibra[d.FibraId];
                    fibraById.TryGetValue(d.FibraId, out var fibra);
                    return new PortfolioCalendarEventDto(
                        Ticker: d.Ticker,
                        Nombre: fibra?.ShortName ?? fibra?.FullName ?? d.Ticker,
                        LogoUrl: $"/logos/{d.Ticker.ToLowerInvariant()}.png",
                        PaymentDate: d.PaymentDate.ToString("yyyy-MM-dd"),
                        AmountPerUnit: d.AmountPerUnit,
                        TaxableAmount: d.TaxableAmount,
                        CapitalReturnAmount: d.CapitalReturnAmount,
                        Titulos: pos.Titulos,
                        TotalAmount: d.AmountPerUnit * pos.Titulos,
                        TotalTaxable: d.TaxableAmount.HasValue ? d.TaxableAmount.Value * pos.Titulos : null,
                        TotalCapital: d.CapitalReturnAmount.HasValue ? d.CapitalReturnAmount.Value * pos.Titulos : null
                    );
                })
                .OrderBy(e => e.PaymentDate)
                .ThenBy(e => e.Ticker)
                .ToList();

            return Results.Ok(events);
        })
        .Produces<IReadOnlyList<PortfolioCalendarEventDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("/column-config", async (
            IPortfolioRepository portfolioRepo,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            if (TryGetUserId(ctx) is not { } userId)
                return Results.Problem(statusCode: StatusCodes.Status401Unauthorized);

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
            if (TryGetUserId(ctx) is not { } userId)
                return Results.Problem(statusCode: StatusCodes.Status401Unauthorized);

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
            if (TryGetUserId(ctx) is not { } userId)
                return Results.Problem(statusCode: StatusCodes.Status401Unauthorized);

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

            if (TryGetUserId(ctx) is not { } userId)
                return Results.Problem(statusCode: StatusCodes.Status401Unauthorized);

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
            if (TryGetUserId(ctx) is not { } userId)
                return Results.Problem(statusCode: StatusCodes.Status401Unauthorized);

            var deleted = await portfolioRepo.DeletePositionAsync(userId, fibraId, ct);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        return app;
    }

    private static Guid? TryGetUserId(HttpContext ctx) =>
        Guid.TryParse(ctx.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    private static int? ResolvePerformanceDays(string? range) =>
        range?.Trim().ToLowerInvariant() switch
        {
            "30d" => 30,
            "90d" => 90,
            "1y" => 365,
            "all" => 3650,
            null => 30,
            _ => null,
        };

    private static async Task<IReadOnlyList<PortfolioPerformancePointDto>> BuildPerformanceSeriesAsync(
        IReadOnlyList<Domain.Portfolio.PortfolioPosition> positions,
        int days,
        IMarketRepository marketRepo,
        CancellationToken ct)
    {
        var fibraIds = positions.Select(p => p.FibraId).ToList();
        var allSnapshots = await marketRepo.GetDailySnapshotsByFibrasAsync(fibraIds, days, ct);
        var valuesByDate = new SortedDictionary<DateOnly, decimal>();

        foreach (var position in positions)
        {
            if (!allSnapshots.TryGetValue(position.FibraId, out var snapshots))
                continue;

            foreach (var snapshot in snapshots.Where(s => s.Close.HasValue))
            {
                if (!valuesByDate.TryGetValue(snapshot.Date, out var current))
                    current = 0m;

                valuesByDate[snapshot.Date] = current + (snapshot.Close!.Value * position.Titulos);
            }
        }

        return BuildNormalizedPoints(valuesByDate);
    }

    private static IReadOnlyList<PortfolioPerformancePointDto> BuildNormalizedSeries(
        IReadOnlyList<Domain.Market.DailySnapshot> snapshots)
    {
        var valuesByDate = new SortedDictionary<DateOnly, decimal>();
        foreach (var snapshot in snapshots.Where(snapshot => snapshot.Close.HasValue))
        {
            valuesByDate[snapshot.Date] = snapshot.Close!.Value;
        }

        return BuildNormalizedPoints(valuesByDate);
    }

    private static async Task<IReadOnlyList<PortfolioPerformancePointDto>?> BuildInpcSeriesAsync(
        IReadOnlyList<PortfolioPerformancePointDto> portfolioSeries,
        IInpcRepository inpcRepo,
        CancellationToken ct)
    {
        if (portfolioSeries.Count == 0)
            return null;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var firstPortfolioDate = DateOnly.ParseExact(portfolioSeries[0].Date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var rangeMonthStart = new DateOnly(firstPortfolioDate.Year, firstPortfolioDate.Month, 1);
        var from = rangeMonthStart.AddMonths(-1);
        var entries = await inpcRepo.GetRangeAsync(from, today, ct);
        if (entries.Count == 0)
            return null;

        var normalizedEntries = entries
            .Where(entry => entry.InpcIndex > 0m)
            .Select(entry => new InpcStepEntry(entry.Periodo, entry.InpcIndex))
            .ToList();

        if (normalizedEntries.Count == 0)
            return null;

        var baseEntry = normalizedEntries.LastOrDefault(entry => entry.Periodo <= rangeMonthStart)
            ?? normalizedEntries[0];
        var result = new List<PortfolioPerformancePointDto>(portfolioSeries.Count);

        foreach (var point in portfolioSeries)
        {
            var date = DateOnly.ParseExact(point.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            var month = new DateOnly(date.Year, date.Month, 1);
            var current = normalizedEntries.LastOrDefault(entry => entry.Periodo <= month) ?? baseEntry;
            var valuePct = Math.Round((current.InpcIndex / baseEntry.InpcIndex - 1m) * 100m, 4);
            result.Add(new PortfolioPerformancePointDto(point.Date, valuePct));
        }

        return result;
    }

    private sealed record InpcStepEntry(DateOnly Periodo, decimal InpcIndex);

    private static IReadOnlyList<PortfolioPerformancePointDto> BuildNormalizedPoints(
        SortedDictionary<DateOnly, decimal> valuesByDate)
    {
        if (valuesByDate.Count == 0)
            return [];

        var entries = valuesByDate.SkipWhile(entry => entry.Value <= 0m).ToList();
        if (entries.Count == 0)
            return [];

        var first = entries[0].Value;
        return entries
            .Select(entry => new PortfolioPerformancePointDto(
                entry.Key.ToString("yyyy-MM-dd"),
                Math.Round((entry.Value / first - 1m) * 100m, 4)))
            .ToList();
    }

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
            "yoc",
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
