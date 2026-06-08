namespace Infrastructure.Jobs.Fundamentals;

public static class FundamentalsPipelineSchedule
{
    public const string JobId = "fundamentals-pipeline";

    // 2:00 AM hora México, cada 2 días
    public const string CronExpression = "0 2 */2 * *";
}
