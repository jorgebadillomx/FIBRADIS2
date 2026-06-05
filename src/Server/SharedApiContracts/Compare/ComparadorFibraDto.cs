namespace SharedApiContracts.Compare;

public sealed record ComparadorMercadoDto(
    decimal? PrecioActual,
    decimal? CambiaDiaPct,
    decimal? Avg52S,
    long? Volumen,
    string? FreshnessStatus);

public sealed record ComparadorFundamentalesDto(
    string? Periodo,
    decimal? CapRate,
    decimal? NavPerCbfi,
    decimal? Ltv,
    decimal? NoiMargin,
    decimal? FfoMargin);

public sealed record ComparadorDistribucionesDto(
    decimal? DistribucionTrimestral,
    decimal? YieldCalculadoPct,
    decimal? YieldDecretadoPct);

public sealed record ComparadorScoreDto(
    decimal? Score,
    bool IsLimitedData,
    bool IsExcluded,
    decimal? NavDescuentoScore,
    decimal? DividendYieldScore,
    decimal? LtvScore,
    decimal? NoiMarginScore,
    decimal? PriceVs52wScore);

public sealed record ComparadorFibraDto(
    Guid FibraId,
    string Ticker,
    string Nombre,
    ComparadorMercadoDto Mercado,
    ComparadorFundamentalesDto Fundamentales,
    ComparadorDistribucionesDto Distribuciones,
    ComparadorScoreDto Score);
