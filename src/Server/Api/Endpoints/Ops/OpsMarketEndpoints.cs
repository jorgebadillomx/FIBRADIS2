using Hangfire;
using Infrastructure.Jobs.Market;

namespace Api.Endpoints.Ops;

public static class OpsMarketEndpoints
{
    public static IEndpointRouteBuilder MapOpsMarket(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ops/market")
            .RequireAuthorization("AdminOps")
            .WithTags("Ops");

        group.MapPost("/daily-snapshot-historical/run", (IBackgroundJobClient jobClient) =>
        {
            jobClient.Enqueue<DailySnapshotHistoricalJob>(j => j.ExecuteAsync(CancellationToken.None));
            return Results.Accepted();
        })
        .Produces(StatusCodes.Status202Accepted)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPost("/distribution/run", (IBackgroundJobClient jobClient) =>
        {
            jobClient.Enqueue<DistributionPipelineJob>(j => j.ExecuteAsync(CancellationToken.None));
            return Results.Accepted();
        })
        .Produces(StatusCodes.Status202Accepted)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        return app;
    }
}
