namespace SharedApiContracts.Market;

public record FibraHistoryDto(
    string Ticker,
    IReadOnlyList<DailyPricePointDto> PriceHistory,
    IReadOnlyList<DistributionPointDto> Distributions,
    decimal? AnnualizedYield
);

public record DailyPricePointDto(
    string Date,
    decimal? Open,
    decimal? Close
);

public record DistributionPointDto(
    string Date,
    decimal AmountPerUnit,
    decimal? TaxableAmountPerUnit,
    decimal? CapitalReturnAmountPerUnit
);

public record CalendarEventDto(
    string EventType,
    string Ticker,
    string Empresa,
    string Date,
    decimal AmountPerUnit,
    decimal? TaxableAmount,
    decimal? CapitalReturnAmount,
    string? AvisoUrl,
    bool IsEstimated
);
