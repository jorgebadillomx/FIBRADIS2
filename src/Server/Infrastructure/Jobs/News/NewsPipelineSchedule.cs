namespace Infrastructure.Jobs.News;

public static class NewsPipelineSchedule
{
    public const string HourlyJobId = "news-pipeline-hourly";
    public const string CronExpression = "0 * * * *";
}
