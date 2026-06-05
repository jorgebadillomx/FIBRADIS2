using Application.Catalog;
using Application.Fundamentals;
using Application.Market;
using Application.Opportunities;
using Microsoft.AspNetCore.Mvc;
using SharedApiContracts.Compare;

namespace Api.Endpoints.Public;

public static class CompareEndpoints
{
    private const int MinFibras = 2;
    private const int MaxFibras = 4;

    public static IEndpointRouteBuilder MapCompare(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/compare").WithTags("Compare");

        group.MapGet("/", async (
            [FromQuery] string tickers,
            IFibraRepository fibraRepo,
            IMarketRepository marketRepo,
            IFundamentalRepository fundamentalRepo,
            IBmvSchedule bmvSchedule,
            CancellationToken ct) =>
        {
            var tickerList = tickers
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(t => t.ToUpperInvariant())
                .Distinct()
                .ToList();

            if (tickerList.Count < MinFibras)
            {
                return Results.Problem(
                    title: "Selección inválida",
                    detail: $"Se requieren al menos {MinFibras} FIBRAs.",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            if (tickerList.Count > MaxFibras)
            {
                return Results.Problem(
                    title: "Selección inválida",
                    detail: $"Se permiten como máximo {MaxFibras} FIBRAs.",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            var fibras = new List<Domain.Catalog.Fibra>(tickerList.Count);
            foreach (var ticker in tickerList)
            {
                var fibra = await fibraRepo.GetByTickerAsync(ticker, ct);
                if (fibra is null)
                {
                    return Results.Problem(
                        title: "Ticker no encontrado",
                        detail: $"Ticker no encontrado: '{ticker}'.",
                        statusCode: StatusCodes.Status400BadRequest,
                        extensions: new Dictionary<string, object?>
                        {
                            ["domainCode"] = "FIBRA_NOT_FOUND",
                            ["ticker"] = ticker,
                        });
                }

                fibras.Add(fibra);
            }

            var fibraIds = fibras.Select(f => f.Id).ToList();
            var utcNow = DateTimeOffset.UtcNow;
            var isMarketOpen = bmvSchedule.IsTradingHours(utcNow);

            var allActiveFibras = await fibraRepo.GetAllActiveAsync(ct);
            var allActiveFibraIds = allActiveFibras.Select(f => f.Id).ToList();

            var allSnapshotForScore = await marketRepo.GetLatestSnapshotPerFibraAsync(ct);
            var allFundamentalsForScore = await fundamentalRepo.GetSummaryLatestAsync(ct);

            var snapshotByFibra = allSnapshotForScore
                .Where(s => fibraIds.Contains(s.FibraId))
                .ToDictionary(s => s.FibraId);
            var fundamentalByFibra = allFundamentalsForScore
                .Where(r => fibraIds.Contains(r.Record.FibraId))
                .ToDictionary(r => r.Record.FibraId, r => r.Record);

            var distributions = await marketRepo.GetDistributionsByFibrasAsync(fibraIds, 365, ct);
            var distsByFibra = distributions
                .GroupBy(d => d.FibraId)
                .ToDictionary(g => g.Key, g => g.ToList());
            var avg52wByFibra = await marketRepo.GetWeek52AvgByFibrasAsync(fibraIds, 365, ct);

            var allDistsForScore = await marketRepo.GetDistributionsByFibrasAsync(allActiveFibraIds, 365, ct);
            var allAvg52wForScore = await marketRepo.GetWeek52AvgByFibrasAsync(allActiveFibraIds, 365, ct);

            var cutoff = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-365);
            var annualDistForScore = allDistsForScore
                .GroupBy(d => d.FibraId)
                .ToDictionary(g => g.Key, g => g.Where(d => d.PaymentDate >= cutoff).Sum(d => d.AmountPerUnit));

            var scores = OpportunityScoreCalculator.Calculate(
                allActiveFibras,
                allSnapshotForScore.ToDictionary(s => s.FibraId),
                allFundamentalsForScore.ToDictionary(r => r.Record.FibraId, r => r.Record),
                annualDistForScore,
                allAvg52wForScore,
                OpportunityWeights.Balanced);

            var scoreByFibra = scores.ToDictionary(s => s.FibraId);

            var result = fibras.Select(fibra =>
            {
                snapshotByFibra.TryGetValue(fibra.Id, out var snap);
                fundamentalByFibra.TryGetValue(fibra.Id, out var fund);
                distsByFibra.TryGetValue(fibra.Id, out var dists);
                avg52wByFibra.TryGetValue(fibra.Id, out var avg52w);
                scoreByFibra.TryGetValue(fibra.Id, out var score);

                var freshnessStatus = FreshnessClassifier.Classify(snap, isMarketOpen, utcNow);

                var annualDist = dists?
                    .Where(d => d.PaymentDate >= cutoff)
                    .Sum(d => d.AmountPerUnit);

                decimal? yieldCalculado = snap?.LastPrice is > 0m && annualDist is > 0m
                    ? Math.Round(annualDist.Value / snap.LastPrice.Value * 100m, 2)
                    : null;

                var trimestreDist = fund?.QuarterlyDistribution;
                decimal? yieldDecretado = snap?.LastPrice is > 0m && trimestreDist is > 0m
                    ? Math.Round(trimestreDist.Value * 4m / snap.LastPrice.Value * 100m, 2)
                    : null;

                return new ComparadorFibraDto(
                    fibra.Id,
                    fibra.Ticker,
                    fibra.ShortName,
                    new ComparadorMercadoDto(snap?.LastPrice, snap?.DailyChangePct, avg52w > 0 ? avg52w : null, snap?.Volume, freshnessStatus),
                    new ComparadorFundamentalesDto(fund?.Period, fund?.CapRate, fund?.NavPerCbfi, fund?.Ltv, fund?.NoiMargin, fund?.FfoMargin),
                    new ComparadorDistribucionesDto(trimestreDist, yieldCalculado, yieldDecretado),
                    score is null
                        ? new ComparadorScoreDto(null, false, true, null, null, null, null, null)
                        : new ComparadorScoreDto(score.Score, score.IsLimitedData, score.IsExcluded,
                            score.NavDiscountScore, score.DividendYieldScore, score.LtvInvertedScore,
                            score.NoiMarginScore, score.Pricevs52wScore));
            }).ToList();

            return Results.Ok(result);
        })
        .AllowAnonymous()
        .Produces<IReadOnlyList<ComparadorFibraDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest);

        return app;
    }
}
