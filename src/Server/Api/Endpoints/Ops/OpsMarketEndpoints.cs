using System.Security.Claims;
using Application.Catalog;
using Application.Auth;
using Application.Jobs;
using Application.Market;
using Domain.Jobs;
using Domain.Catalog;
using Domain.Market;
using Hangfire;
using Infrastructure.Jobs.Fundamentals;
using Infrastructure.Jobs.Market;
using Infrastructure.Jobs.News;
using Microsoft.Extensions.Logging;
using SharedApiContracts.Market;

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

        group.MapPost("/daily-snapshot-historical/run", async (
            IBackgroundJobClient jobClient,
            IPipelineRunLogRepository runLogRepo,
            ILoggerFactory loggerFactory,
            IEmailEncryptor emailEncryptor,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            jobClient.Enqueue<DailySnapshotHistoricalJob>(j => j.ExecuteAsync(CancellationToken.None));
            await TryLogQueuedRunAsync("DailySnapshot", ctx, runLogRepo, emailEncryptor, loggerFactory.CreateLogger("OpsMarketEndpoints"), ct);
            return Results.Accepted();
        })
        .Produces(StatusCodes.Status202Accepted)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPost("/daily-snapshot-reset/run", async (
            IMarketRepository marketRepo,
            IBackgroundJobClient jobClient,
            IPipelineRunLogRepository runLogRepo,
            ILoggerFactory loggerFactory,
            IEmailEncryptor emailEncryptor,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            await marketRepo.DeleteAllDailySnapshotsAsync(ct);
            jobClient.Enqueue<DailySnapshotHistoricalJob>(j => j.ExecuteAsync(CancellationToken.None));
            await TryLogQueuedRunAsync("DailySnapshotReset", ctx, runLogRepo, emailEncryptor, loggerFactory.CreateLogger("OpsMarketEndpoints"), ct);
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

        var distributionsGroup = app.MapGroup("/api/v1/ops/distributions")
            .RequireAuthorization("AdminOps")
            .WithTags("Ops");

        distributionsGroup.MapGet("", async (
            IFibraRepository fibraRepo,
            IMarketRepository marketRepo,
            CancellationToken ct) =>
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var from = today.AddMonths(-3);
            var fibras = await fibraRepo.GetAllActiveAsync(ct);
            var fibraById = fibras.ToDictionary(f => f.Id);
            var distributions = await marketRepo.GetDistributionsByRangeAsync(from, today, ct);

            var dtos = distributions
                .Where(d => fibraById.ContainsKey(d.FibraId))
                .Select(d =>
                {
                    var fibra = fibraById[d.FibraId];
                    return new DistributionAdminDto(
                        d.Id,
                        d.FibraId,
                        d.Ticker,
                        fibra.FullName,
                        d.PaymentDate,
                        d.ExDividendDate,
                        d.AmountPerUnit,
                        d.TaxableAmount,
                        d.CapitalReturnAmount,
                        d.AvisoUrl,
                        d.Source,
                        d.CapturedAt);
                })
                .OrderByDescending(d => d.PaymentDate)
                .ThenByDescending(d => d.CapturedAt)
                .ToList();

            return Results.Ok(dtos);
        })
        .Produces<IReadOnlyList<DistributionAdminDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        distributionsGroup.MapPost("/sync", async (
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

        distributionsGroup.MapPost("", async (
            DistributionUpsertRequest request,
            IFibraRepository fibraRepo,
            IMarketRepository marketRepo,
            ILoggerFactory loggerFactory,
            IEmailEncryptor emailEncryptor,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger("OpsMarketEndpoints");
            var errors = ValidateDistributionRequest(request);
            if (errors.Count > 0)
                return Results.ValidationProblem(errors);

            var normalizedTicker = request.Ticker.Trim();
            var fibra = (await fibraRepo.GetAllActiveAsync(ct))
                .FirstOrDefault(f => string.Equals(f.Ticker, normalizedTicker, StringComparison.OrdinalIgnoreCase));
            if (fibra is null)
                return Results.NotFound();

            var distribution = new Distribution
            {
                Id = Guid.NewGuid(),
                FibraId = fibra.Id,
                Ticker = fibra.Ticker,
                PaymentDate = request.PaymentDate,
                ExDividendDate = request.ExDividendDate,
                AmountPerUnit = request.AmountPerUnit,
                TaxableAmount = request.TaxableAmount,
                CapitalReturnAmount = request.CapitalReturnAmount,
                AvisoUrl = NormalizeAvisoUrl(request.AvisoUrl),
                Currency = fibra.Currency,
                Source = "manual",
                CapturedAt = DateTimeOffset.UtcNow,
            };

            await marketRepo.AddDistributionAsync(distribution, ct);

            logger.LogInformation(
                "Ops {Action} distribution {Ticker} by {Actor} at {Timestamp}",
                "CREATE",
                distribution.Ticker,
                GetActor(ctx, emailEncryptor),
                DateTimeOffset.UtcNow);

            return Results.Created($"/api/v1/ops/distributions/{distribution.Id}", ToDto(distribution, fibra.FullName));
        })
        .Produces<DistributionAdminDto>(StatusCodes.Status201Created)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        distributionsGroup.MapPut("/{id:guid}", async (
            Guid id,
            DistributionUpsertRequest request,
            IFibraRepository fibraRepo,
            IMarketRepository marketRepo,
            ILoggerFactory loggerFactory,
            IEmailEncryptor emailEncryptor,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger("OpsMarketEndpoints");
            var errors = ValidateDistributionRequest(request);
            if (errors.Count > 0)
                return Results.ValidationProblem(errors);

            var existing = await marketRepo.GetDistributionByIdAsync(id, ct);
            if (existing is null)
                return Results.NotFound();

            var normalizedTicker = request.Ticker.Trim();
            var fibra = (await fibraRepo.GetAllActiveAsync(ct))
                .FirstOrDefault(f => string.Equals(f.Ticker, normalizedTicker, StringComparison.OrdinalIgnoreCase));
            if (fibra is null)
                return Results.NotFound();

            existing.FibraId = fibra.Id;
            existing.Ticker = fibra.Ticker;
            existing.PaymentDate = request.PaymentDate;
            existing.ExDividendDate = request.ExDividendDate;
            existing.AmountPerUnit = request.AmountPerUnit;
            existing.TaxableAmount = request.TaxableAmount;
            existing.CapitalReturnAmount = request.CapitalReturnAmount;
            existing.AvisoUrl = NormalizeAvisoUrl(request.AvisoUrl);
            existing.Currency = fibra.Currency;
            existing.CapturedAt = DateTimeOffset.UtcNow;

            try
            {
                await marketRepo.UpdateDistributionAsync(existing, ct);
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException)
            {
                return Results.Conflict();
            }

            logger.LogInformation(
                "Ops {Action} distribution {DistributionId} by {Actor} at {Timestamp}",
                "UPDATE",
                existing.Id,
                GetActor(ctx, emailEncryptor),
                DateTimeOffset.UtcNow);

            return Results.Ok(ToDto(existing, fibra.FullName));
        })
        .Produces<DistributionAdminDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status409Conflict)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        distributionsGroup.MapDelete("/{id:guid}", async (
            Guid id,
            IMarketRepository marketRepo,
            ILoggerFactory loggerFactory,
            IEmailEncryptor emailEncryptor,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger("OpsMarketEndpoints");
            var deleted = await marketRepo.DeleteDistributionAsync(id, ct);
            if (!deleted)
                return Results.NotFound();

            logger.LogInformation(
                "Ops {Action} distribution {DistributionId} by {Actor} at {Timestamp}",
                "DELETE",
                id,
                GetActor(ctx, emailEncryptor),
                DateTimeOffset.UtcNow);

            return Results.NoContent();
        })
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound)
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

    private static Dictionary<string, string[]> ValidateDistributionRequest(DistributionUpsertRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Ticker))
            errors["ticker"] = ["El ticker es requerido."];

        if (request.PaymentDate == default)
            errors["paymentDate"] = ["La fecha de pago es requerida."];

        if (request.AmountPerUnit <= 0)
            errors["amountPerUnit"] = ["El monto por CBFI debe ser mayor a cero."];

        if (request.AvisoUrl is not null && NormalizeAvisoUrl(request.AvisoUrl) is null)
            errors["avisoUrl"] = ["AvisoUrl debe ser una URL absoluta de bmv.com.mx."];

        return errors;
    }

    private static string? NormalizeAvisoUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri))
            return null;

        if (!uri.Host.EndsWith("bmv.com.mx", StringComparison.OrdinalIgnoreCase))
            return null;

        return uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            || uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            ? uri.ToString()
            : null;
    }

    private static DistributionAdminDto ToDto(Distribution distribution, string empresa)
        => new(
            distribution.Id,
            distribution.FibraId,
            distribution.Ticker,
            empresa,
            distribution.PaymentDate,
            distribution.ExDividendDate,
            distribution.AmountPerUnit,
            distribution.TaxableAmount,
            distribution.CapitalReturnAmount,
            distribution.AvisoUrl,
            distribution.Source,
            distribution.CapturedAt);

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
