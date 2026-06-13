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
        var paymentQuarter = QuarterOf(lastDate);
        var paymentYear = lastDate.Year;

        // El periodo reportado es el trimestre anterior al pago (FIBRAs pagan Q+1)
        var reportQuarter = paymentQuarter - 1;
        var reportYear = paymentYear;
        if (reportQuarter == 0) { reportQuarter = 4; reportYear--; }

        var distCbfi = ordered
            .Where(d => d.PaymentDate.Year == paymentYear && QuarterOf(d.PaymentDate) == paymentQuarter)
            .Sum(d => d.AmountPerUnit);

        // Suma trailing 12 meses desde la última distribución (incluye la actual)
        var cutoff = lastDate.AddYears(-1);
        var distCbfiAnual = ordered
            .Where(d => d.PaymentDate >= cutoff)
            .Sum(d => d.AmountPerUnit);

        return new CalculadoraDistributionSummary(
            $"Q{reportQuarter}-{reportYear}",
            distCbfi,
            distCbfiAnual);
    }

    private static int QuarterOf(DateOnly date) => (date.Month - 1) / 3 + 1;
}
