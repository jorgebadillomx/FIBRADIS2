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
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var fibras = await fibraRepo.GetAllActiveAsync(ct);
            var latestSnapshots = await marketRepo.GetLatestSnapshotPerFibraAsync(ct);
            var snapshotByFibra = latestSnapshots.ToDictionary(s => s.FibraId);
            var distributions = await marketRepo.GetDistributionsByFibrasAsync(fibras.Select(f => f.Id).ToArray(), DistributionHistoryDays, ct);
            var distributionsByFibra = distributions
                .GroupBy(d => d.FibraId)
                .ToDictionary(g => g.Key, g => (IReadOnlyList<Domain.Market.Distribution>)g.OrderByDescending(d => d.PaymentDate).ToList());

            var results = fibras.Select(fibra =>
            {
                snapshotByFibra.TryGetValue(fibra.Id, out var snap);
                var freshnessStatus = FreshnessClassifier.Classify(snap, isMarketOpen, utcNow);
                distributionsByFibra.TryGetValue(fibra.Id, out var fibraDistributions);
                var annualizedYield = YieldCalculator.Calculate(
                    fibraDistributions ?? Array.Empty<Domain.Market.Distribution>(),
                    snap?.LastPrice,
                    today);

                return new MarketSnapshotDto(
                    fibra.Id,
                    fibra.Ticker,
                    snap?.LastPrice,
                    snap?.DailyChange,
                    snap?.DailyChangePct,
                    snap?.Volume,
                    snap?.Week52High,
                    snap?.Week52Low,
                    annualizedYield,
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
                snapshots.Select(s => new DailyPricePointDto(s.Date.ToString("yyyy-MM-dd"), s.Open, s.Close)).ToList(),
                distributions.Take(MaxDistributionsInResponse).Select(d => new DistributionPointDto(
                    d.PaymentDate.ToString("yyyy-MM-dd"),
                    d.AmountPerUnit,
                    d.TaxableAmount,
                    d.CapitalReturnAmount)).ToList(),
                annualizedYield
            );

            return Results.Ok(dto);
        })
        .AllowAnonymous()
        .Produces<FibraHistoryDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/events", async (
            [FromQuery] DateOnly? from,
            [FromQuery] DateOnly? to,
            IFibraRepository fibraRepo,
            IMarketRepository marketRepo,
            CancellationToken ct) =>
        {
            var (rangeFrom, rangeTo) = ResolveRange(from, to);
            var fibras = await fibraRepo.GetAllActiveAsync(ct);
            var fibraById = fibras.ToDictionary(f => f.Id);
            var distributions = await marketRepo.GetDistributionsByRangeAsync(rangeFrom, rangeTo, ct);
            var events = new List<CalendarEventDto>(distributions.Count * 2);

            foreach (var dist in distributions)
            {
                if (!fibraById.TryGetValue(dist.FibraId, out var fibra))
                    continue;

                if (dist.PaymentDate >= rangeFrom && dist.PaymentDate <= rangeTo)
                {
                    events.Add(new CalendarEventDto(
                        "Pago",
                        fibra.Ticker,
                        fibra.FullName,
                        dist.PaymentDate.ToString("yyyy-MM-dd"),
                        dist.AmountPerUnit,
                        dist.TaxableAmount,
                        dist.CapitalReturnAmount,
                        dist.AvisoUrl));
                }

                if (dist.ExDividendDate is DateOnly exDate && exDate >= rangeFrom && exDate <= rangeTo)
                {
                    events.Add(new CalendarEventDto(
                        "ExDerecho",
                        fibra.Ticker,
                        fibra.FullName,
                        exDate.ToString("yyyy-MM-dd"),
                        dist.AmountPerUnit,
                        dist.TaxableAmount,
                        dist.CapitalReturnAmount,
                        dist.AvisoUrl));
                }
            }

            var ordered = events
                .OrderBy(e => e.Date)
                .ThenBy(e => e.EventType)
                .ThenBy(e => e.Ticker)
                .ToList();

            return Results.Ok(ordered);
        })
        .AllowAnonymous()
        .Produces<IReadOnlyList<CalendarEventDto>>(StatusCodes.Status200OK);

        return app;
    }

    private const int MaxRangeDays = 366;

    private static (DateOnly From, DateOnly To) ResolveRange(DateOnly? from, DateOnly? to)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var start = from ?? new DateOnly(today.Year, today.Month, 1);
        var end = to ?? new DateOnly(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month));
        if (start > end)
            (start, end) = (end, start);
        if ((end.DayNumber - start.DayNumber) > MaxRangeDays)
            end = start.AddDays(MaxRangeDays);
        return (start, end);
    }
}
