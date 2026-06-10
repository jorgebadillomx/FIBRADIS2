namespace Application.Fundamentals;

public static class FundamentalsDiscoveryPeriodHelper
{
    private const int DefaultLookbackQuarters = 20;
    public static string CurrentClosedPeriod(DateTimeOffset now)
    {
        var month = now.Month;
        return month switch
        {
            >= 1 and <= 3 => $"Q4-{now.Year - 1}",
            >= 4 and <= 6 => $"Q1-{now.Year}",
            >= 7 and <= 9 => $"Q2-{now.Year}",
            _ => $"Q3-{now.Year}",
        };
    }

    public static string AdvancePeriod(string period, int quarters)
    {
        var parsed = ParsePeriodOrThrow(period);
        var totalQuarters = ((parsed.Year * 4) + (parsed.Quarter - 1)) + quarters;
        if (totalQuarters < 0)
            throw new ArgumentOutOfRangeException(nameof(quarters), "El desplazamiento de periodos produce un año negativo.");

        var year = totalQuarters / 4;
        var quarter = (totalQuarters % 4) + 1;
        return $"Q{quarter}-{year}";
    }

    public static bool IsPeriodInRange(string period, string fromPeriod)
        => ComparePeriods(period, fromPeriod) >= 0;

    public static string ComputeFromPeriod(string? lastProcessedPeriod, DateTimeOffset now)
        => string.IsNullOrWhiteSpace(lastProcessedPeriod)
            ? AdvancePeriod(CurrentClosedPeriod(now), -DefaultLookbackQuarters)
            : AdvancePeriod(lastProcessedPeriod, 1);

    public static int ComparePeriods(string a, string b)
    {
        var left = ParsePeriodOrThrow(a);
        var right = ParsePeriodOrThrow(b);

        var yearComparison = left.Year.CompareTo(right.Year);
        if (yearComparison != 0)
            return yearComparison;

        return left.Quarter.CompareTo(right.Quarter);
    }

    private static (int Quarter, int Year) ParsePeriodOrThrow(string period)
    {
        if (!TryParsePeriod(period, out var parsed))
            throw new ArgumentException($"Periodo inválido: '{period}'.", nameof(period));

        return parsed;
    }

    private static bool TryParsePeriod(string? period, out (int Quarter, int Year) parsed)
    {
        parsed = default;
        if (string.IsNullOrWhiteSpace(period))
            return false;

        var value = period.Trim().ToUpperInvariant();
        if (value.Length != 7 || value[0] != 'Q' || value[2] != '-' || !char.IsDigit(value[1]))
            return false;

        if (!int.TryParse(value[1].ToString(), out var quarter) || quarter is < 1 or > 4)
            return false;

        if (!int.TryParse(value[3..], out var year) || year < 0)
            return false;

        parsed = (quarter, year);
        return true;
    }
}
