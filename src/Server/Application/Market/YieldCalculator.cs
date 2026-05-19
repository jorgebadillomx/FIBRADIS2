using Domain.Market;

namespace Application.Market;

public static class YieldCalculator
{
    public static decimal? Calculate(
        IReadOnlyList<Distribution> distributions,
        decimal? lastPrice,
        DateOnly today)
    {
        if (!lastPrice.HasValue || lastPrice.Value <= 0)
            return null;

        var cutoff = today.AddDays(-365);
        var inYear = distributions
            .Where(d => d.PaymentDate >= cutoff)
            .OrderBy(d => d.PaymentDate)
            .ToList();

        if (inYear.Count == 0)
            return null;

        // TTM: suma directa de lo pagado en el último año.
        // Evita sobreestimar cuando hay 3 pagos trimestrales (365/90 ≈ 4, no 3).
        decimal annualizedTotal = inYear.Sum(d => d.AmountPerUnit);

        return Math.Round(annualizedTotal / lastPrice.Value, 4);
    }
}
