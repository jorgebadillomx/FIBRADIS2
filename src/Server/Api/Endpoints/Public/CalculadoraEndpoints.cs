using Application.Catalog;
using Application.Market;
using Domain.Market;
using SharedApiContracts.Market;

namespace Api.Endpoints.Public;

public static class CalculadoraEndpoints
{
    private const int DistributionHistoryDays = 400;

    public static IEndpointRouteBuilder MapCalculadora(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/market").WithTags("Market");

        group.MapGet("/calculadora", async (
            IFibraRepository fibraRepo,
            IMarketRepository marketRepo,
            IBmvSchedule bmvSchedule,
            CancellationToken ct) =>
        {
            var utcNow = DateTimeOffset.UtcNow;
            var isMarketOpen = bmvSchedule.IsTradingHours(utcNow);

            var fibras = await fibraRepo.GetAllActiveAsync(ct);
            var snapshots = await marketRepo.GetLatestSnapshotPerFibraAsync(ct);
            var snapshotByFibra = snapshots.ToDictionary(s => s.FibraId);

            var distributions = await marketRepo.GetDistributionsByFibrasAsync(
                fibras.Select(f => f.Id).ToArray(),
                DistributionHistoryDays,
                ct);

            var distributionsByFibra = distributions
                .GroupBy(d => d.FibraId)
                .ToDictionary(g => g.Key, g => (IReadOnlyList<Distribution>)g.ToList());

            var results = fibras.Select(fibra =>
            {
                snapshotByFibra.TryGetValue(fibra.Id, out var snapshot);
                distributionsByFibra.TryGetValue(fibra.Id, out var fibraDistributions);

                var summary = CalculadoraDistributionCalculator.Calculate(
                    fibraDistributions ?? Array.Empty<Distribution>());

                return new CalculadoraFibraDto(
                    fibra.Ticker,
                    fibra.FullName,
                    snapshot?.LastPrice,
                    summary.UltimoPeriodo,
                    summary.DistCbfi,
                    summary.DistCbfiAnual,
                    FreshnessClassifier.Classify(snapshot, isMarketOpen, utcNow));
            }).ToList();

            return Results.Ok(results);
        })
        .AllowAnonymous()
        .Produces<IReadOnlyList<CalculadoraFibraDto>>(StatusCodes.Status200OK);

        return app;
    }
}
