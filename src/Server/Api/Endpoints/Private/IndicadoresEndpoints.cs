using Application.Ops;
using SharedApiContracts.Market;

namespace Api.Endpoints.Private;

public static class IndicadoresEndpoints
{
    public static IEndpointRouteBuilder MapIndicators(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/market/indicadores", async (
            IOperationalConfigRepository repo,
            CancellationToken ct) =>
        {
            var config = await repo.GetAsync(ct);
            return Results.Ok(new IndicadoresDto(config.Cetes28dRate, config.Cetes28dRateUpdatedAt));
        })
        .RequireAuthorization("User")
        .WithTags("Market")
        .Produces<IndicadoresDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        return app;
    }
}
