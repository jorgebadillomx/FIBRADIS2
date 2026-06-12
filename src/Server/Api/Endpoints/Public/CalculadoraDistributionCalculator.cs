using Domain.Market;

namespace Api.Endpoints.Public;

public sealed record CalculadoraDistributionSummary(
    string? UltimoPeriodo,
    decimal? DistCbfi,
    decimal? DistCbfiAnual
);

public static class CalculadoraDistributionCalculator
{
    public static CalculadoraDistributionSummary Calculate(IReadOnlyList<Distribution> distributions)
    {
        if (distributions.Count == 0)
            return new CalculadoraDistributionSummary(null, null, null);

        var ordered = distributions
            .OrderByDescending(d => d.PaymentDate)
            .ToList();

        var lastDate = ordered[0].PaymentDate;
        var lastYear = lastDate.Year;
        var lastQuarter = (lastDate.Month - 1) / 3 + 1;

        var distCbfi = ordered
            .Where(d => d.PaymentDate.Year == lastYear && QuarterOf(d.PaymentDate) == lastQuarter)
            .Sum(d => d.AmountPerUnit);

        var distCbfiAnual = ordered
            .Where(d => d.PaymentDate.Year == lastYear)
            .Sum(d => d.AmountPerUnit);

        return new CalculadoraDistributionSummary(
            $"Q{lastQuarter}-{lastYear}",
            distCbfi,
            distCbfiAnual);
    }

    private static int QuarterOf(DateOnly date) => (date.Month - 1) / 3 + 1;
}
