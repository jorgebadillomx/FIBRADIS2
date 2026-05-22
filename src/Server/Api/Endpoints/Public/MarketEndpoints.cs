using Application.Catalog;
using Application.Market;
using Microsoft.AspNetCore.Mvc;
using SharedApiContracts.Market;

namespace Api.Endpoints.Public;

public static class MarketEndpoints
{
    private const int DistributionHistoryDays = 1825; // ~5 years
    private const int MaxDistributionsInResponse = 60; // covers 5 years of quarterly payments

    public static IEndpointRouteBuilder MapMarket(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/market").WithTags("Market");

        group.MapGet("/snapshots", async (
            IFibraRepository fibraRepo,
            IMarketRepository marketRepo,
            IBmvSchedule bmvSchedule,
            CancellationToken ct) =>
        {
            var utcNow = DateTimeOffset.UtcNow;
            var isMarketOpen = bmvSchedule.IsTradingHours(utcNow);

            var fibras = await fibraRepo.GetAllActiveAsync(ct);
            var latestSnapshots = await marketRepo.GetLatestSnapshotPerFibraAsync(ct);
            var snapshotByFibra = latestSnapshots.ToDictionary(s => s.FibraId);

            var results = fibras.Select(fibra =>
            {
                snapshotByFibra.TryGetValue(fibra.Id, out var snap);
                var freshnessStatus = FreshnessClassifier.Classify(snap, isMarketOpen, utcNow);

                return new MarketSnapshotDto(
                    fibra.Id,
                    fibra.Ticker,
                    snap?.LastPrice,
                    snap?.DailyChange,
                    snap?.DailyChangePct,
                    snap?.Volume,
                    snap?.Week52High,
                    snap?.Week52Low,
                    snap?.CapturedAt.ToString("O"),
                    freshnessStatus);
            }).ToList();

            return Results.Ok(results);
        })
        .AllowAnonymous()
        .Produces<IReadOnlyList<MarketSnapshotDto>>(StatusCodes.Status200OK);

        group.MapGet("/fibras/{ticker}/history", async (
            string ticker,
            [FromQuery] string? period,
            IFibraRepository fibraRepo,
            IMarketRepository marketRepo,
            CancellationToken ct) =>
        {
            var fibra = await fibraRepo.GetByTickerAsync(ticker.ToUpperInvariant(), ct);
            if (fibra is null)
                return Results.NotFound();

            int days = period?.ToLowerInvariant() switch
            {
                "1m" => 30,
                "3m" => 90,
                "6m" => 180,
                "1y" => 365,
                _ => 30,
            };

            var snapshots = await marketRepo.GetDailySnapshotsAsync(fibra.Id, days, ct);
            var distributions = await marketRepo.GetDistributionsAsync(fibra.Id, maxDays: DistributionHistoryDays, ct);
            var latest = await marketRepo.GetLatestSnapshotPerFibraAsync(ct);
            var lastPrice = latest.FirstOrDefault(s => s.FibraId == fibra.Id)?.LastPrice;

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var annualizedYield = YieldCalculator.Calculate(distributions, lastPrice, today);

            var dto = new FibraHistoryDto(
                fibra.Ticker,
                snapshots.Select(s => new DailyPricePointDto(s.Date.ToString("yyyy-MM-dd"), s.Close)).ToList(),
                distributions.Take(MaxDistributionsInResponse).Select(d => new DistributionPointDto(d.PaymentDate.ToString("yyyy-MM-dd"), d.AmountPerUnit)).ToList(),
                annualizedYield
            );

            return Results.Ok(dto);
        })
        .AllowAnonymous()
        .Produces<FibraHistoryDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        return app;
    }
}
