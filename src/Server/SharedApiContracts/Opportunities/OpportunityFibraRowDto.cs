namespace SharedApiContracts.Opportunities;

public sealed record OpportunityFibraRowDto(
    Guid FibraId,
    string Ticker,
    string Nombre,
    decimal? Score,
    int ComponentCount,
    bool IsLimitedData,
    // Percentile scores (0-100) — null if component not available
    decimal? NavDiscountScore,
    decimal? DividendYieldScore,
    decimal? LtvInvertedScore,
    decimal? NoiMarginScore,
    decimal? Pricevs52wScore,
    decimal? YieldRealScore,
    // Raw values for display/tooltip
    decimal? NavDiscountPct,
    decimal? DividendYieldPct,
    decimal? LtvPct,
    decimal? NoiMarginPct,
    decimal? PriceVsAvg52wPct,
    decimal? YieldRealPct,
    // Price info
    decimal? PrecioActual,
    decimal? NavPerCbfi,
    decimal? Avg52w
);
