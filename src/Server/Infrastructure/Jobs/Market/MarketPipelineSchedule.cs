using System.Runtime.InteropServices;

namespace Infrastructure.Jobs.Market;

public static class MarketPipelineSchedule
{
    public const string OpeningWindowJobId = "market-pipeline-opening-window";
    public const string MainSessionJobId = "market-pipeline-main-session";
    public const string DistributionJobId = "distribution-pipeline";
    public const string DistributionCronExpression = "0 8 * * *";

    public static TimeZoneInfo GetMexicoTimeZone() => TimeZoneInfo.FindSystemTimeZoneById(
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "Central Standard Time"
            : "America/Mexico_City");

    public static IReadOnlyList<(string JobId, string CronExpression)> GetRecurringJobs() =>
    [
        (OpeningWindowJobId, "15,30,45 8 * * 1-5"),
        (MainSessionJobId, "0,15,30,45 9-15 * * 1-5"),
    ];
}
