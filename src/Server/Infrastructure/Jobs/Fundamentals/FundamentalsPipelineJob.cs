using System.Text.Json;
using Application.Fundamentals;
using Application.Jobs;
using Domain.Jobs;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Jobs.Fundamentals;

public class FundamentalsPipelineJob(
    IFundamentalsAutomationService automationService,
    IPipelineRunLogRepository pipelineRunLogRepo,
    ILogger<FundamentalsPipelineJob> logger)
{
    public async Task ExecuteAsync(CancellationToken ct = default) =>
        await ExecuteAsync(forceRun: false, ct);

    [DisableConcurrentExecution(timeoutInSeconds: 0)]
    public async Task ExecuteAsync(bool forceRun, CancellationToken ct = default)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var status = "Failed";
        FundamentalsAutomationRunResult? result = null;

        try
        {
            if (!forceRun)
            {
                var lastRun = await pipelineRunLogRepo.GetLastCompletedAsync("Fundamentals", ct);
                if (lastRun is not null && (startedAt - lastRun.CompletedAt!.Value).TotalHours < 36)
                {
                    logger.LogDebug(
                        "Fundamentals pipeline skipped — last run was {Hours:F1}h ago",
                        (startedAt - lastRun.CompletedAt!.Value).TotalHours);
                    return;
                }
            }

            result = await automationService.ExecuteAsync(ct);
            status = "Completed";
        }
        finally
        {
            try
            {
                await pipelineRunLogRepo.AddAsync(new PipelineRunLog
                {
                    Pipeline = "Fundamentals",
                    StartedAt = startedAt,
                    CompletedAt = DateTimeOffset.UtcNow,
                    Status = status,
                    ItemsProcessed = result?.RecordsProcessed ?? 0,
                    ErrorCount = result?.Errors ?? 0,
                    Details = result is null
                        ? null
                        : JsonSerializer.Serialize(new
                        {
                            fibrasScanned = result.FibrasScanned,
                            reportsDetected = result.ReportsDetected,
                            newReports = result.NewReports,
                            skippedReports = result.SkippedReports,
                            possibleUpdates = result.PossibleUpdates,
                            annualReports = result.AnnualReports,
                            ambiguousReports = result.AmbiguousReports,
                            errors = result.Errors,
                            recordsProcessed = result.RecordsProcessed,
                        }),
                }, CancellationToken.None);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to write PipelineRunLog for Fundamentals pipeline");
            }
        }
    }
}
