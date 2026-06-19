using System.Globalization;
using Application.Ops;
using SharedApiContracts.Market;

namespace Api.Endpoints.Private;

public static class IndicadoresEndpoints
{
    public static IEndpointRouteBuilder MapIndicators(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/market/indicadores", async (
            IOperationalConfigRepository repo,
            IInpcRepository inpcRepo,
            CancellationToken ct) =>
        {
            var config = await repo.GetAsync(ct);
            var inpcEntries = (await inpcRepo.GetLastAsync(25, ct))
                .OrderBy(x => x.Periodo)
                .ToList();
            var inpcHistory = BuildInpcHistory(inpcEntries);
            var lastUpdated = new[] { config.Cetes28dRateUpdatedAt, config.Tiie28dRateUpdatedAt }
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .DefaultIfEmpty()
                .Max();

            return Results.Ok(new IndicadoresDto(
                config.Cetes28dRate,
                config.Tiie28dRate,
                lastUpdated == default ? null : lastUpdated,
                inpcHistory));
        })
        .RequireAuthorization("User")
        .WithTags("Market")
        .Produces<IndicadoresDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        return app;
    }

    private static IReadOnlyList<InpcMonthlyDto> BuildInpcHistory(IReadOnlyList<Domain.Ops.InpcMonthlyEntry> entries)
    {
        var result = new List<InpcMonthlyDto>();
        foreach (var current in entries)
        {
            var yearAgo = entries.FirstOrDefault(entry =>
                entry.Periodo.Year == current.Periodo.Year - 1 &&
                entry.Periodo.Month == current.Periodo.Month);

            if (yearAgo is null || yearAgo.InpcIndex == 0)
                continue;

            var anualPct = Math.Round((current.InpcIndex / yearAgo.InpcIndex - 1m) * 100m, 2);
            result.Add(new InpcMonthlyDto(current.Periodo.ToString("yyyy-MM", CultureInfo.InvariantCulture), anualPct));
        }

        return result.TakeLast(13).ToList();
    }
}
