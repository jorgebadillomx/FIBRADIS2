using System.Security.Claims;
using Application.Auth;
using Application.Jobs;
using Domain.Jobs;
using Hangfire;
using Infrastructure.Jobs.Fundamentals;
using Infrastructure.Jobs.Market;
using Infrastructure.Jobs.News;
using Microsoft.Extensions.Logging;

namespace Api.Endpoints.Ops;

public static class OpsMarketEndpoints
{
    public static IEndpointRouteBuilder MapOpsMarket(this IEndpointRouteBuilder app)
    {
        var newsGroup = app.MapGroup("/api/v1/ops/news-pipeline")
            .RequireAuthorization("AdminOps")
            .WithTags("Ops");

        newsGroup.MapPost("/run", async (
            IBackgroundJobClient jobClient,
            IPipelineRunLogRepository runLogRepo,
            ILoggerFactory loggerFactory,
            IEmailEncryptor emailEncryptor,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            jobClient.Enqueue<NewsPipelineJob>(j => j.ExecuteAsync(CancellationToken.None));
            await TryLogQueuedRunAsync("News", ctx, runLogRepo, emailEncryptor, loggerFactory.CreateLogger("OpsMarketEndpoints"), ct);
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

        group.MapPost("/run", async (
            IBackgroundJobClient jobClient,
            IPipelineRunLogRepository runLogRepo,
            ILoggerFactory loggerFactory,
            IEmailEncryptor emailEncryptor,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            jobClient.Enqueue<MarketPipelineJob>(j => j.ExecuteAsync(true, CancellationToken.None));
            await TryLogQueuedRunAsync("Market", ctx, runLogRepo, emailEncryptor, loggerFactory.CreateLogger("OpsMarketEndpoints"), ct);
            return Results.Accepted();
        })
        .Produces(StatusCodes.Status202Accepted)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPost("/daily-snapshot-historical/run", (IBackgroundJobClient jobClient) =>
        {
            jobClient.Enqueue<DailySnapshotHistoricalJob>(j => j.ExecuteAsync(CancellationToken.None));
            return Results.Accepted();
        })
        .Produces(StatusCodes.Status202Accepted)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPost("/distribution/run", async (
            IBackgroundJobClient jobClient,
            IPipelineRunLogRepository runLogRepo,
            ILoggerFactory loggerFactory,
            IEmailEncryptor emailEncryptor,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            jobClient.Enqueue<DistributionPipelineJob>(j => j.ExecuteAsync(CancellationToken.None));
            await TryLogQueuedRunAsync("Distribution", ctx, runLogRepo, emailEncryptor, loggerFactory.CreateLogger("OpsMarketEndpoints"), ct);
            return Results.Accepted();
        })
        .Produces(StatusCodes.Status202Accepted)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPost("/fundamentals/run", async (
            IBackgroundJobClient jobClient,
            IPipelineRunLogRepository runLogRepo,
            ILoggerFactory loggerFactory,
            IEmailEncryptor emailEncryptor,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            jobClient.Enqueue<FundamentalsPipelineJob>(j => j.ExecuteAsync(true, CancellationToken.None));
            await TryLogQueuedRunAsync("Fundamentals", ctx, runLogRepo, emailEncryptor, loggerFactory.CreateLogger("OpsMarketEndpoints"), ct);
            return Results.Accepted();
        })
        .Produces(StatusCodes.Status202Accepted)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        return app;
    }

    private static async Task TryLogQueuedRunAsync(
        string pipeline,
        HttpContext ctx,
        IPipelineRunLogRepository runLogRepo,
        IEmailEncryptor emailEncryptor,
        ILogger logger,
        CancellationToken ct)
    {
        try
        {
            await runLogRepo.AddAsync(new PipelineRunLog
            {
                Pipeline = pipeline,
                StartedAt = DateTimeOffset.UtcNow,
                Status = "Queued",
                TriggeredBy = GetActor(ctx, emailEncryptor),
            }, CancellationToken.None); // no depende del ciclo de vida del request
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to write PipelineRunLog for {Pipeline} manual trigger", pipeline);
        }
    }

    private static string GetActor(HttpContext ctx, IEmailEncryptor emailEncryptor)
    {
        var actor = ctx.User.Identity?.Name
            ?? ctx.User.FindFirstValue(ClaimTypes.Email)
            ?? ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (actor is null)
            return "unknown";

        return emailEncryptor.Decrypt(actor);
    }
}
