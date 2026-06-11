namespace SharedApiContracts.Market;

public record DistributionUpsertRequest(
    string Ticker,
    DateOnly PaymentDate,
    DateOnly? ExDividendDate,
    decimal AmountPerUnit,
    decimal? TaxableAmount,
    decimal? CapitalReturnAmount,
    string? AvisoUrl
);
