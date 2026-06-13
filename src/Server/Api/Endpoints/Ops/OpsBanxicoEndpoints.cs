using System.Security.Claims;
using Application.Auth;
using Application.Jobs;
using Domain.Jobs;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace Api.Endpoints.Ops;

public static class OpsBanxicoEndpoints
{
    public static IEndpointRouteBuilder MapOpsBanxico(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ops/banxico")
            .RequireAuthorization("AdminOps")
            .WithTags("Ops");

        group.MapPost("/sync-tiie/run", async (
            IBackgroundJobClient jobs,
            IPipelineRunLogRepository runLogRepo,
            ILoggerFactory loggerFactory,
            IEmailEncryptor emailEncryptor,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            jobs.Enqueue<Application.Jobs.BanxicoSyncJob>(j => j.ExecuteAsync(CancellationToken.None));
            await TryLogQueuedRunAsync("BanxicoSync", ctx, runLogRepo, emailEncryptor, loggerFactory.CreateLogger("OpsBanxicoEndpoints"), ct);
            return Results.Accepted();
        })
        .Produces(StatusCodes.Status202Accepted)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPost("/sync-inpc/run", async (
            IBackgroundJobClient jobs,
            IPipelineRunLogRepository runLogRepo,
            ILoggerFactory loggerFactory,
            IEmailEncryptor emailEncryptor,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            jobs.Enqueue<Application.Jobs.BanxicoMonthlySyncJob>(j => j.ExecuteAsync(CancellationToken.None));
            await TryLogQueuedRunAsync("BanxicoInpc", ctx, runLogRepo, emailEncryptor, loggerFactory.CreateLogger("OpsBanxicoEndpoints"), ct);
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
            }, CancellationToken.None);
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
