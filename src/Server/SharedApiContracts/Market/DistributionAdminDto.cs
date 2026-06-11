namespace SharedApiContracts.Market;

public record DistributionAdminDto(
    Guid Id,
    Guid FibraId,
    string Ticker,
    string Empresa,
    DateOnly PaymentDate,
    DateOnly? ExDividendDate,
    decimal AmountPerUnit,
    decimal? TaxableAmount,
    decimal? CapitalReturnAmount,
    string? AvisoUrl,
    string Source,
    DateTimeOffset CapturedAt
);
