using Application.Opportunities;

namespace Application.Tests.Opportunities;

public class UniverseCoverageCalculatorTests
{
    [Fact]
    public void Calculate_NormalState_WhenMissingBelowThreshold()
    {
        var result = UniverseCoverageCalculator.Calculate(
            universeSize: 25,
            fibrasWithPrice: 20,        // 5 sin precio = 20%
            degradationThresholdPct: 30,
            lastValidPriceAt: null);

        Assert.Equal("Normal", result.Status);
        Assert.Equal(20.0m, result.MissingPct);
    }

    [Fact]
    public void Calculate_DegradedState_WhenMissingAboveThreshold()
    {
        var result = UniverseCoverageCalculator.Calculate(
            universeSize: 25,
            fibrasWithPrice: 17,        // 8 sin precio = 32%
            degradationThresholdPct: 30,
            lastValidPriceAt: null);

        Assert.Equal("Degraded", result.Status);
        Assert.Equal(32.0m, result.MissingPct);
    }

    [Fact]
    public void Calculate_SuspendedState_WhenMissingAbove50Pct()
    {
        var result = UniverseCoverageCalculator.Calculate(
            universeSize: 25,
            fibrasWithPrice: 12,        // 13 sin precio = 52%
            degradationThresholdPct: 30,
            lastValidPriceAt: null);

        Assert.Equal("Suspended", result.Status);
        Assert.Equal(52.0m, result.MissingPct);
    }

    [Fact]
    public void Calculate_DegradedState_WhenMissingExactlyAtDegradationThreshold()
    {
        // 3/10 = 30% exacto
        var result = UniverseCoverageCalculator.Calculate(
            universeSize: 10,
            fibrasWithPrice: 7,         // 3 sin precio = 30%
            degradationThresholdPct: 30,
            lastValidPriceAt: null);

        Assert.Equal("Degraded", result.Status);
        Assert.Equal(30.0m, result.MissingPct);
    }

    [Fact]
    public void Calculate_SuspendedState_WhenMissingExactlyAt50Pct()
    {
        var result = UniverseCoverageCalculator.Calculate(
            universeSize: 10,
            fibrasWithPrice: 5,         // 5 sin precio = 50%
            degradationThresholdPct: 30,
            lastValidPriceAt: null);

        Assert.Equal("Suspended", result.Status);
        Assert.Equal(50.0m, result.MissingPct);
    }

    [Fact]
    public void Calculate_DegradedState_WithCustomThreshold20()
    {
        // umbral=20, 22 sin precio de 100 = 22%
        var result = UniverseCoverageCalculator.Calculate(
            universeSize: 100,
            fibrasWithPrice: 78,        // 22 sin precio = 22%
            degradationThresholdPct: 20,
            lastValidPriceAt: null);

        Assert.Equal("Degraded", result.Status);
        Assert.Equal(22.0m, result.MissingPct);
    }

    [Fact]
    public void Calculate_NormalState_WhenUniverseIsEmpty()
    {
        var result = UniverseCoverageCalculator.Calculate(
            universeSize: 0,
            fibrasWithPrice: 0,
            degradationThresholdPct: 30,
            lastValidPriceAt: null);

        Assert.Equal("Normal", result.Status);
        Assert.Equal(0m, result.MissingPct);
    }

    [Fact]
    public void Calculate_ThresholdZero_ClampedToOne_ZeroMissingIsNormal()
    {
        // threshold=0 sin clamp haría 0% missing >= 0% → "Degraded" (bug);
        // con clamp a 1: 0% missing < 1% → "Normal" (correcto)
        var result = UniverseCoverageCalculator.Calculate(
            universeSize: 20,
            fibrasWithPrice: 20, // 0% missing
            degradationThresholdPct: 0,
            lastValidPriceAt: null);

        Assert.Equal("Normal", result.Status);
        Assert.Equal(0m, result.MissingPct);
        Assert.Equal(1, result.DegradationThresholdPct); // threshold clampeado a 1
    }
}
