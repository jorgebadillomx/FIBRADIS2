using Infrastructure.Jobs.Market;

namespace Infrastructure.Tests.Jobs.Market;

public class MarketPipelineScheduleTests
{
    [Fact]
    public void GetRecurringJobs_OnlySchedulesWithinBmvTradingWindow()
    {
        var jobs = MarketPipelineSchedule.GetRecurringJobs();

        Assert.Collection(
            jobs,
            opening =>
            {
                Assert.Equal(MarketPipelineSchedule.OpeningWindowJobId, opening.JobId);
                Assert.Equal("15,30,45 8 * * 1-5", opening.CronExpression);
            },
            session =>
            {
                Assert.Equal(MarketPipelineSchedule.MainSessionJobId, session.JobId);
                Assert.Equal("0,15,30,45 9-15 * * 1-5", session.CronExpression);
            });
    }

    [Fact]
    public void DistributionSchedule_UsesDailyUtcEightAm()
    {
        Assert.Equal("distribution-pipeline", MarketPipelineSchedule.DistributionJobId);
        Assert.Equal("0 8 * * *", MarketPipelineSchedule.DistributionCronExpression);
    }
}
