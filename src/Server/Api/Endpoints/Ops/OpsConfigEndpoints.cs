using System.Security.Claims;
using Application.Ops;
using Hangfire;
using Infrastructure.Jobs.Market;
using Infrastructure.Jobs.News;
using Microsoft.Extensions.Logging;
using SharedApiContracts.Ops;

namespace Api.Endpoints.Ops;

public static class OpsConfigEndpoints
{
    public static IEndpointRouteBuilder MapOpsConfig(this IEndpointRouteBuilder app)
    {
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
                actor,
                ct);

            var useInMemoryHangfire = ctx.RequestServices
                .GetRequiredService<IConfiguration>()
                .GetValue<bool>("Hangfire:UseInMemoryStorage");

            if (!useInMemoryHangfire && cadenceChanged)
            {
                var jobManager = ctx.RequestServices.GetRequiredService<IRecurringJobManager>();
                var cronExpr = $"*/{request.NewsCadenceMinutes!.Value} * * * *";
                jobManager.AddOrUpdate<NewsPipelineJob>(
                    NewsPipelineSchedule.HourlyJobId,
                    j => j.ExecuteAsync(CancellationToken.None),
                    cronExpr,
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

        if (request.CommissionFactor is null && request.AvgPeriods is null && request.NewsCadenceMinutes is null && request.FibraNewsMonths is null)
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

        if (request.NewsCadenceMinutes is not null && !IsValidCadence(request.NewsCadenceMinutes.Value))
        {
            errors["newsCadenceMinutes"] = ["newsCadenceMinutes debe ser un divisor de 60 entre 15 y 60 (15, 20, 30, 60)."];
        }

        if (request.FibraNewsMonths is not null && (request.FibraNewsMonths < 1 || request.FibraNewsMonths > 36))
        {
            errors["fibraNewsMonths"] = ["fibraNewsMonths debe estar entre 1 y 36 meses."];
        }

        return errors;
    }

    private static bool IsValidCadence(int minutes)
        => minutes is >= 15 and <= 60 && 60 % minutes == 0;

    private static OperationalConfigDto ToDto(Domain.Ops.OperationalConfig config)
        => new(
            config.CommissionFactor,
            config.AvgPeriods,
            config.NewsCadenceMinutes,
            config.FibraNewsMonths,
            config.UpdatedAt,
            config.UpdatedBy);

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
