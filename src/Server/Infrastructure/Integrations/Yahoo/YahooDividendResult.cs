namespace Infrastructure.Integrations.Yahoo;

public record YahooDividendResult(DateOnly PaymentDate, decimal AmountPerUnit);
