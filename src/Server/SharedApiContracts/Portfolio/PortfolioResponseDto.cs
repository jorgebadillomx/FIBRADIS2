namespace SharedApiContracts.Portfolio;

public sealed record PortfolioKpisDto(
    decimal InversionTotal,
    decimal? ValorTotal,
    decimal? PlusvaliaTotal_Pct,
    decimal? PlusvaliaTotal_Mxn,
    decimal RentasAnualesBrutas,
    decimal RentasRealesBrutas,
    decimal PctRentasPortafolio,
    bool IsPartial
);

public sealed record PortfolioDistributionDto(
    string PaymentDate,
    decimal AmountPerUnit
);

public sealed record PortfolioPositionDto(
    Guid FibraId,
    string Ticker,
    string Nombre,
    int Titulos,
    decimal CostoPromedio,
    decimal CostoTotalCompra,
    decimal PctPortafolio,
    decimal? PrecioActual,
    decimal? ValorMercado,
    decimal? PlusvaliaFilaPct,
    decimal? PlusvaliaFilaMxn,
    decimal? RentaAnual,
    string? FreshnessStatus,
    decimal? CapRate,
    decimal? NavPerCbfi,
    decimal? Ltv,
    decimal? NoiMargin,
    decimal? FfoMargin,
    decimal? DailyChangePct,
    decimal? Week52High,
    long? Volume,
    decimal? Week52Low,
    decimal? Week52Avg,
    string? FundamentalsPeriod,
    IReadOnlyList<PortfolioDistributionDto> RecentDistributions
);

public sealed record PortfolioResponseDto(
    PortfolioKpisDto? Kpis,
    IReadOnlyList<PortfolioPositionDto> Positions
);

public sealed record PortfolioColumnConfigDto(
    IReadOnlyList<string> Columns
);

public sealed record PortfolioPositionPatchDto(
    int Titulos,
    decimal CostoPromedio
);
