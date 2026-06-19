namespace SharedApiContracts.Ops;

public sealed record FiscalRatesDto(
    decimal IsrRetentionRate,
    decimal IvaRate);
