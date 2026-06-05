using System.Security.Claims;
using Application.Ops;
using Hangfire;
using Infrastructure.Jobs.Fundamentals;
using Infrastructure.Jobs.Market;
using Infrastructure.Jobs.News;
using Microsoft.Extensions.Logging;
using SharedApiContracts.Ops;

namespace Api.Endpoints.Ops;

public static class OpsConfigEndpoints
{
    public static IEndpointRouteBuilder MapOpsConfig(this IEndpointRouteBuilder app)
    {
        // Public endpoint — no auth required
        app.MapGet("/api/v1/site-content", async (
            IOperationalConfigRepository repo,
            CancellationToken ct) =>
        {
            var config = await repo.GetAsync(ct);
            return Results.Ok(new SharedApiContracts.Ops.SiteContentDto(
                config.TermsEnabled,
                config.TermsEnabled ? config.TermsText : null,
                config.ContactEmail));
        })
        .AllowAnonymous()
        .WithTags("Public")
        .Produces<SharedApiContracts.Ops.SiteContentDto>(StatusCodes.Status200OK);

        var group = app.MapGroup("/api/v1/ops")
            .RequireAuthorization("AdminOps")
            .WithTags("Ops");

        group.MapGet("/config", async (
            IOperationalConfigRepository repo,
            CancellationToken ct) =>
        {
            var config = await repo.GetAsync(ct);
            return Results.Ok(ToDto(config));
        })
        .Produces<OperationalConfigDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPut("/config", async (
            UpdateOperationalConfigRequest request,
            IOperationalConfigRepository repo,
            ILoggerFactory loggerFactory,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var errors = ValidateRequest(request);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            var logger = loggerFactory.CreateLogger("OpsConfigEndpoints");
            var actor = GetActor(ctx, logger);
            var currentConfig = await repo.GetAsync(ct);
            var cadenceChanged = request.NewsCadenceMinutes.HasValue
                && currentConfig.NewsCadenceMinutes != request.NewsCadenceMinutes.Value;

            await repo.UpdateAsync(
                request.CommissionFactor,
                request.AvgPeriods,
                request.NewsCadenceMinutes,
                request.FibraNewsMonths,
                request.FundamentalsCadenceMinutes,
                request.DistributionCadenceMinutes,
                request.TermsEnabled,
                request.TermsText,
                request.ContactEmail,
                actor,
                request.UniverseDegradationThresholdPct,
                ct);

            var useInMemoryHangfire = ctx.RequestServices
                .GetRequiredService<IConfiguration>()
                .GetValue<bool>("Hangfire:UseInMemoryStorage");

            if (!useInMemoryHangfire && cadenceChanged)
            {
                var jobManager = ctx.RequestServices.GetRequiredService<IRecurringJobManager>();
                jobManager.AddOrUpdate<NewsPipelineJob>(
                    NewsPipelineSchedule.HourlyJobId,
                    j => j.ExecuteAsync(CancellationToken.None),
                    NewsPipelineSchedule.GetCronExpression(request.NewsCadenceMinutes!.Value),
                    new RecurringJobOptions { TimeZone = MarketPipelineSchedule.GetMexicoTimeZone() });
            }

            var fundamentalsCadenceChanged = request.FundamentalsCadenceMinutes.HasValue
                && currentConfig.FundamentalsCadenceMinutes != request.FundamentalsCadenceMinutes.Value;
            if (!useInMemoryHangfire && fundamentalsCadenceChanged)
            {
                var jobManager = ctx.RequestServices.GetRequiredService<IRecurringJobManager>();
                jobManager.AddOrUpdate<FundamentalsPipelineJob>(
                    FundamentalsPipelineSchedule.JobId,
                    j => j.ExecuteAsync(CancellationToken.None),
                    FundamentalsPipelineSchedule.GetCronExpression(request.FundamentalsCadenceMinutes!.Value),
                    new RecurringJobOptions { TimeZone = MarketPipelineSchedule.GetMexicoTimeZone() });
            }

            var distributionCadenceChanged = request.DistributionCadenceMinutes.HasValue
                && currentConfig.DistributionCadenceMinutes != request.DistributionCadenceMinutes.Value;
            if (!useInMemoryHangfire && distributionCadenceChanged)
            {
                var jobManager = ctx.RequestServices.GetRequiredService<IRecurringJobManager>();
                jobManager.AddOrUpdate<DistributionPipelineJob>(
                    DistributionPipelineSchedule.JobId,
                    j => j.ExecuteAsync(CancellationToken.None),
                    DistributionPipelineSchedule.GetCronExpression(request.DistributionCadenceMinutes!.Value),
                    new RecurringJobOptions { TimeZone = MarketPipelineSchedule.GetMexicoTimeZone() });
            }

            return Results.NoContent();
        })
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapGet("/audit-log", async (
            IConfigAuditLogRepository repo,
            CancellationToken ct) =>
        {
            var entries = await repo.GetRecentAsync(50, ct);
            return Results.Ok(entries.Select(ToDto).ToList());
        })
        .Produces<IReadOnlyList<ConfigAuditLogDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        return app;
    }

    private static Dictionary<string, string[]> ValidateRequest(UpdateOperationalConfigRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.CommissionFactor is null
            && request.AvgPeriods is null
            && request.NewsCadenceMinutes is null
            && request.FibraNewsMonths is null
            && request.FundamentalsCadenceMinutes is null
            && request.DistributionCadenceMinutes is null
            && request.TermsEnabled is null
            && request.TermsText is null
            && request.ContactEmail is null
            && request.UniverseDegradationThresholdPct is null)
        {
            errors["body"] = ["Se debe proporcionar al menos un campo para actualizar."];
            return errors;
        }

        if (request.CommissionFactor is not null &&
            (request.CommissionFactor <= 0m || request.CommissionFactor > 0.1m))
        {
            errors["commissionFactor"] = ["commissionFactor debe ser mayor a 0 y menor o igual a 0.1."];
        }

        if (request.AvgPeriods is not null && (request.AvgPeriods < 1 || request.AvgPeriods > 20))
        {
            errors["avgPeriods"] = ["avgPeriods debe estar entre 1 y 20."];
        }

        if (request.NewsCadenceMinutes is not null && request.NewsCadenceMinutes.Value != 1440)
        {
            errors["newsCadenceMinutes"] = ["newsCadenceMinutes debe ser 1440 (24 horas)."];
        }

        if (request.FibraNewsMonths is not null && (request.FibraNewsMonths < 1 || request.FibraNewsMonths > 36))
        {
            errors["fibraNewsMonths"] = ["fibraNewsMonths debe estar entre 1 y 36 meses."];
        }

        if (request.FundamentalsCadenceMinutes is not null && request.FundamentalsCadenceMinutes is not (60 or 120 or 180 or 240 or 360 or 720 or 1440))
        {
            errors["fundamentalsCadenceMinutes"] = ["fundamentalsCadenceMinutes debe ser uno de: 60, 120, 180, 240, 360, 720 o 1440."];
        }

        if (request.DistributionCadenceMinutes is not null && request.DistributionCadenceMinutes is not (720 or 1440))
        {
            errors["distributionCadenceMinutes"] = ["distributionCadenceMinutes debe ser 720 (12h) o 1440 (24h)."];
        }

        if (request.UniverseDegradationThresholdPct is not null &&
            (request.UniverseDegradationThresholdPct < 1 || request.UniverseDegradationThresholdPct > 49))
        {
            errors["universeDegradationThresholdPct"] =
                ["universeDegradationThresholdPct debe estar entre 1 y 49."];
        }

        return errors;
    }

    private static OperationalConfigDto ToDto(Domain.Ops.OperationalConfig config)
        => new(
            config.CommissionFactor,
            config.AvgPeriods,
            config.NewsCadenceMinutes,
            config.FibraNewsMonths,
            config.FundamentalsCadenceMinutes,
            config.DistributionCadenceMinutes,
            config.UpdatedAt,
            config.UpdatedBy,
            config.TermsEnabled,
            config.TermsText,
            config.ContactEmail,
            config.UniverseDegradationThresholdPct);

    private static ConfigAuditLogDto ToDto(Domain.Ops.ConfigAuditLog entry)
        => new(
            entry.Id,
            entry.Actor,
            entry.ChangedAt,
            entry.FieldName,
            entry.PreviousValue,
            entry.NewValue);

    private static string GetActor(HttpContext ctx, ILogger logger)
    {
        var actor = ctx.User.Identity?.Name
            ?? ctx.User.FindFirstValue(ClaimTypes.Email)
            ?? ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (actor is null)
        {
            logger.LogWarning("GetActor: no identity claim found in JWT; using 'unknown'");
            return "unknown";
        }

        return actor;
    }
}
