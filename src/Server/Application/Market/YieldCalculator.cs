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
        // TTM = trailing twelve months: solo pagos YA realizados. Acotar el extremo superior a
        // `today` evita que una distribución con fecha futura (error de captura o aviso anticipado)
        // infle el yield.
        var inYear = distributions
            .Where(d => d.PaymentDate >= cutoff && d.PaymentDate <= today)
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
