namespace SharedApiContracts.Ops;

public sealed record OperationalConfigDto(
    decimal CommissionFactor,
    int AvgPeriods,
    int NewsCadenceMinutes,
    DateTimeOffset UpdatedAt,
    string? UpdatedBy);
