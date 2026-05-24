using Application.Catalog;
using Application.Fundamentals;
using SharedApiContracts.Fundamentals;

namespace Api.Endpoints.Public;

public static class FundamentalsEndpoints
{
    public static IEndpointRouteBuilder MapFundamentalsPublic(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/fundamentals").WithTags("Catalog");

        group.MapGet("/{ticker}/latest", async (
            string ticker,
            IFibraRepository fibraRepo,
            IFundamentalRepository fundamentalRepo,
            CancellationToken ct) =>
        {
            var fibra = await fibraRepo.GetByTickerAsync(ticker, ct);
            if (fibra is null)
                return Results.Problem(
                    title: "FIBRA no encontrada",
                    detail: $"No existe una FIBRA con ticker '{ticker}'.",
                    statusCode: StatusCodes.Status404NotFound,
                    extensions: new Dictionary<string, object?> { ["domainCode"] = "FIBRA_NOT_FOUND" });

            var record = await fundamentalRepo.GetLatestProcessedByFibraAsync(fibra.Id, ct);
            if (record is null)
                return Results.NotFound();

            return Results.Ok(new FundamentalesPublicDto(
                Period: record.Period,
                PeriodsAgo: null,
                CapRate: record.CapRate,
                NavPerCbfi: record.NavPerCbfi,
                Ltv: record.Ltv,
                NoiMargin: record.NoiMargin,
                FfoMargin: record.FfoMargin,
                QuarterlyDistribution: record.QuarterlyDistribution,
                Summary: record.Summary,
                CapturedAt: record.CapturedAt));
        })
        .AllowAnonymous()
        .Produces<FundamentalesPublicDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        return app;
    }
}
