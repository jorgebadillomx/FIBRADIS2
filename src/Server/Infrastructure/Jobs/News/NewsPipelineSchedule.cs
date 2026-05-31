namespace Infrastructure.Jobs.News;

public static class NewsPipelineSchedule
{
    public const string HourlyJobId = "news-pipeline-hourly";
    public const string CronExpression = "0 0 * * *";

    public static string GetCronExpression(int cadenceMinutes)
        => cadenceMinutes switch
        {
            1440 => "0 0 * * *",
            _ => "0 0 * * *",
        };
}
