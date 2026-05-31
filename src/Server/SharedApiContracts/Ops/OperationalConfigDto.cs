namespace SharedApiContracts.Ops;

public sealed record OperationalConfigDto(
    decimal CommissionFactor,
    int AvgPeriods,
    int NewsCadenceMinutes,
    int FibraNewsMonths,
    int FundamentalsCadenceMinutes,
    DateTimeOffset UpdatedAt,
    string? UpdatedBy);
