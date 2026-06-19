using System.Security.Claims;
using System.Text.Json;
using Application.Auth;
using Application.Integrations;
using Application.Jobs;
using Application.Ops;
using Domain.Jobs;
using Domain.Ops;
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

        group.MapPost("/sync-inpc/backfill", async (
            IBanxicoClient banxico,
            IInpcRepository inpcRepo,
            IPipelineRunLogRepository runLogRepo,
            ILoggerFactory loggerFactory,
            IEmailEncryptor emailEncryptor,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger("OpsBanxicoEndpoints");
            var startedAt = DateTimeOffset.UtcNow;
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var from = today.AddMonths(-72);
            var processed = 0;
            var errors = 0;
            var status = "Failed";
            IResult result;

            try
            {
                var history = await banxico.GetInpcHistoryAsync(from, today, ct);
                var entries = history
                    .Select(h => new InpcMonthlyEntry
                    {
                        Periodo = new DateOnly(h.Periodo.Year, h.Periodo.Month, 1),
                        InpcIndex = h.InpcIndex,
                        CapturedAt = startedAt,
                    })
                    .ToList();

                await inpcRepo.UpsertManyAsync(entries, ct);
                processed = entries.Count;
                status = "Completed";
                result = Results.Ok(new
                {
                    from = from.ToString("yyyy-MM", System.Globalization.CultureInfo.InvariantCulture),
                    to = today.ToString("yyyy-MM", System.Globalization.CultureInfo.InvariantCulture),
                    processed,
                });
                logger.LogInformation(
                    "BanxicoInpcBackfill: {Count} registros INPC upsertados desde {From} hasta {To}",
                    processed,
                    from,
                    today);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                errors = 1;
                logger.LogError(ex, "BanxicoInpcBackfill: error inesperado");
                result = Results.Problem(
                    "No se pudo ejecutar el backfill de INPC.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
            finally
            {
                await TryLogRunAsync(
                    "BanxicoInpcBackfill",
                    startedAt,
                    status,
                    processed,
                    errors,
                    ctx,
                    runLogRepo,
                    emailEncryptor,
                    logger,
                    ct);
            }

            return result;
        })
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status500InternalServerError)
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

    private static async Task TryLogRunAsync(
        string pipeline,
        DateTimeOffset startedAt,
        string status,
        int processed,
        int errors,
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
                StartedAt = startedAt,
                CompletedAt = DateTimeOffset.UtcNow,
                Status = status,
                ItemsProcessed = processed,
                ErrorCount = errors,
                Details = JsonSerializer.Serialize(new { processed, errors }),
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
