using Hangfire;
using Infrastructure.Jobs.Market;
using Infrastructure.Jobs.News;

namespace Api.Endpoints.Ops;

public static class OpsMarketEndpoints
{
    public static IEndpointRouteBuilder MapOpsMarket(this IEndpointRouteBuilder app)
    {
        var newsGroup = app.MapGroup("/api/v1/ops/news-pipeline")
            .RequireAuthorization("AdminOps")
            .WithTags("Ops");

        newsGroup.MapPost("/run", (IBackgroundJobClient jobClient) =>
        {
            jobClient.Enqueue<NewsPipelineJob>(j => j.ExecuteAsync(CancellationToken.None));
            return Results.Accepted();
        })
        .Produces(StatusCodes.Status202Accepted)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        newsGroup.MapPost("/retry-body-text", (IBackgroundJobClient jobClient) =>
        {
            jobClient.Enqueue<NewsBodyTextRetryJob>(j => j.ExecuteAsync(CancellationToken.None));
            return Results.Accepted();
        })
        .Produces(StatusCodes.Status202Accepted)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

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
