namespace SharedApiContracts.Portfolio;

public sealed record PortfolioKpisDto(
    decimal InversionTotal,
    decimal? ValorTotal,
    decimal? PlusvaliaTotal_Pct,
    decimal? PlusvaliaTotal_Mxn,
    decimal? YieldPortafolio,
    decimal? IngresoMensual,
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
    decimal? Yoc,
    decimal? OpportunityScore,
    string? LogoUrl,
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

public sealed record PortfolioConfigDto(
    decimal CommissionFactor
);

public sealed record PortfolioPerformancePointDto(
    string Date,
    decimal ValuePct
);

public sealed record PortfolioPerformanceResponseDto(
    IReadOnlyList<PortfolioPerformancePointDto> PortfolioSeries,
    IReadOnlyList<PortfolioPerformancePointDto> IpcSeries,
    IReadOnlyList<PortfolioPerformancePointDto> Sp500Series,
    IReadOnlyList<PortfolioPerformancePointDto>? InpcSeries
);

public sealed record PortfolioPositionPatchDto(
    int Titulos,
    decimal CostoPromedio
);

public sealed record PortfolioCalendarEventDto(
    string Ticker,
    string Nombre,
    string? LogoUrl,
    string PaymentDate,
    decimal AmountPerUnit,
    decimal? TaxableAmount,
    decimal? CapitalReturnAmount,
    int Titulos,
    decimal TotalAmount,
    decimal? TotalTaxable,
    decimal? TotalCapital
);
