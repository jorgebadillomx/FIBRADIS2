using Domain.Auth;

namespace Domain.Tests.Auth;

public class UserTests
{
    [Fact]
    public void ComputedIsActive_ReturnsTrueForLifetimeSubscriptionWithStartDate()
    {
        var user = new User
        {
            SubscriptionType = SubscriptionType.Lifetime,
            SubscriptionStartedAt = DateTime.UtcNow.AddDays(-1),
        };

        Assert.True(user.ComputedIsActive);
    }

    [Fact]
    public void ComputedIsActive_ReturnsTrueForActiveMonthlySubscription()
    {
        var user = new User
        {
            SubscriptionType = SubscriptionType.Monthly,
            SubscriptionStartedAt = DateTime.UtcNow.AddDays(-30),
            SubscriptionEndsAt = DateTime.UtcNow.AddDays(30),
        };

        Assert.True(user.ComputedIsActive);
    }

    [Fact]
    public void ComputedIsActive_ReturnsTrueForActiveAnnualSubscription()
    {
        var user = new User
        {
            SubscriptionType = SubscriptionType.Annual,
            SubscriptionStartedAt = DateTime.UtcNow.AddDays(-30),
            SubscriptionEndsAt = DateTime.UtcNow.AddMonths(10),
        };

        Assert.True(user.ComputedIsActive);
    }

    [Fact]
    public void ComputedIsActive_ReturnsTrueForActiveTrial()
    {
        var user = new User
        {
            TrialEndsAt = DateTime.UtcNow.AddDays(7),
        };

        Assert.True(user.ComputedIsActive);
    }

    [Fact]
    public void ComputedIsActive_ReturnsFalseWithoutSubscriptionOrTrial()
    {
        var user = new User();

        Assert.False(user.ComputedIsActive);
    }

    [Fact]
    public void ComputedIsActive_ReturnsFalseForExpiredTrial()
    {
        var user = new User
        {
            TrialEndsAt = DateTime.UtcNow.AddDays(-1),
        };

        Assert.False(user.ComputedIsActive);
    }

    [Fact]
    public void ComputedIsActive_ReturnsFalseForExpiredSubscription()
    {
        var user = new User
        {
            SubscriptionType = SubscriptionType.Monthly,
            SubscriptionStartedAt = DateTime.UtcNow.AddMonths(-2),
            SubscriptionEndsAt = DateTime.UtcNow.AddDays(-1),
        };

        Assert.False(user.ComputedIsActive);
    }
}
