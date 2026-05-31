namespace Infrastructure.Jobs.Fundamentals;

public static class FundamentalsPipelineSchedule
{
    public const string JobId = "fundamentals-pipeline";

    public static string GetCronExpression(int cadenceMinutes)
        => cadenceMinutes switch
        {
            60 => "0 * * * *",
            120 => "0 */2 * * *",
            180 => "0 */3 * * *",
            240 => "0 */4 * * *",
            360 => "0 */6 * * *",
            720 => "0 */12 * * *",
            1440 => "0 0 * * *",
            _ when cadenceMinutes is > 0 and < 60 => $"*/{cadenceMinutes} * * * *",
            _ => "0 */6 * * *",
        };
}
