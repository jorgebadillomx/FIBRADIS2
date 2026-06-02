namespace Infrastructure.Jobs.Market;

public static class DistributionPipelineSchedule
{
    public const string JobId = "distribution-pipeline";

    public static string GetCronExpression(int cadenceMinutes)
        => cadenceMinutes switch
        {
            720  => "0 */12 * * *",
            1440 => "0 0 * * *",
            _    => "0 0 * * *",
        };
}
