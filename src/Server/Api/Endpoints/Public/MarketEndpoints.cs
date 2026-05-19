using Application.Catalog;
using Application.Market;
using SharedApiContracts.Market;

namespace Api.Endpoints.Public;

public static class MarketEndpoints
{
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

        return app;
    }
}
