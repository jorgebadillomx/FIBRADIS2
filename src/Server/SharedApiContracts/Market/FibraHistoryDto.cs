namespace SharedApiContracts.Market;

public record FibraHistoryDto(
    string Ticker,
    IReadOnlyList<DailyPricePointDto> PriceHistory,
    IReadOnlyList<DistributionPointDto> Distributions,
    decimal? AnnualizedYield
);

public record DailyPricePointDto(
    string Date,
    decimal? Close
);

public record DistributionPointDto(
    string Date,
    decimal AmountPerUnit
);
