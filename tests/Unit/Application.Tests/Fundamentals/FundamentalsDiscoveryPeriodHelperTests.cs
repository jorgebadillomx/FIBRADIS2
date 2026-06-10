using Application.Fundamentals;

namespace Application.Tests.Fundamentals;

public class FundamentalsDiscoveryPeriodHelperTests
{
    [Theory]
    [InlineData(2026, 1, 15, "Q4-2025")]
    [InlineData(2026, 4, 1, "Q1-2026")]
    [InlineData(2026, 7, 31, "Q2-2026")]
    [InlineData(2026, 10, 5, "Q3-2026")]
    public void CurrentClosedPeriod_ReturnsExpectedClosedQuarter(
        int year,
        int month,
        int day,
        string expected)
    {
        var now = new DateTimeOffset(year, month, day, 12, 0, 0, TimeSpan.Zero);

        Assert.Equal(expected, FundamentalsDiscoveryPeriodHelper.CurrentClosedPeriod(now));
    }

    [Theory]
    [InlineData("Q1-2025", 1, "Q2-2025")]
    [InlineData("Q3-2025", 2, "Q1-2026")]
    [InlineData("Q4-2025", 1, "Q1-2026")]
    [InlineData("Q2-2024", 8, "Q2-2026")]
    [InlineData("Q1-2025", -1, "Q4-2024")]
    [InlineData("Q1-2025", -4, "Q1-2024")]
    [InlineData("Q4-2025", -1, "Q3-2025")]
    [InlineData("Q2-2026", -5, "Q1-2025")]
    public void AdvancePeriod_ShiftsQuarterChronologically(
        string period,
        int quarters,
        string expected)
    {
        Assert.Equal(expected, FundamentalsDiscoveryPeriodHelper.AdvancePeriod(period, quarters));
    }

    [Theory]
    [InlineData("Q4-2024", "Q4-2024", true)]
    [InlineData("Q1-2025", "Q4-2024", true)]
    [InlineData("Q4-2024", "Q1-2025", false)]
    [InlineData("Q2-2025", "Q1-2025", true)]
    [InlineData("Q1-2026", "Q3-2025", true)]
    public void IsPeriodInRange_UsesChronologicalComparison(
        string period,
        string fromPeriod,
        bool expected)
    {
        Assert.Equal(expected, FundamentalsDiscoveryPeriodHelper.IsPeriodInRange(period, fromPeriod));
    }

    [Theory]
    [InlineData("Q3-2025", 2026, 1, "Q4-2025")]
    [InlineData("Q4-2025", 2026, 4, "Q1-2026")]
    [InlineData("Q1-2026", 2026, 7, "Q2-2026")]
    [InlineData("Q2-2026", 2026, 10, "Q3-2026")]
    public void ComputeFromPeriod_WithExistingPeriod_AdvancesOneQuarter(
        string lastProcessedPeriod,
        int year,
        int month,
        string expected)
    {
        var now = new DateTimeOffset(year, month, 10, 12, 0, 0, TimeSpan.Zero);

        Assert.Equal(expected, FundamentalsDiscoveryPeriodHelper.ComputeFromPeriod(lastProcessedPeriod, now));
    }

    [Theory]
    [InlineData(null, 2026, 1, "Q4-2020")]
    [InlineData(null, 2026, 4, "Q1-2021")]
    [InlineData(null, 2026, 7, "Q2-2021")]
    [InlineData(null, 2026, 10, "Q3-2021")]
    [InlineData("", 2026, 4, "Q1-2021")]
    [InlineData("   ", 2026, 4, "Q1-2021")]
    public void ComputeFromPeriod_WithNullOrEmptyPeriod_StartsFiveYearsBack(
        string? lastProcessedPeriod,
        int year,
        int month,
        string expected)
    {
        var now = new DateTimeOffset(year, month, 10, 12, 0, 0, TimeSpan.Zero);

        Assert.Equal(expected, FundamentalsDiscoveryPeriodHelper.ComputeFromPeriod(lastProcessedPeriod, now));
    }

    [Theory]
    [InlineData("Q4-2024", "Q1-2025", -1)]
    [InlineData("Q1-2025", "Q4-2024", 1)]
    [InlineData("Q2-2025", "Q2-2025", 0)]
    [InlineData("Q4-2025", "Q1-2026", -1)]
    [InlineData("Q1-2026", "Q4-2025", 1)]
    [InlineData("q1-2025", "Q1-2025", 0)]
    public void ComparePeriods_OrdersChronologically(
        string left,
        string right,
        int expected)
    {
        Assert.Equal(expected, FundamentalsDiscoveryPeriodHelper.ComparePeriods(left, right));
    }
}
