namespace SharedApiContracts.Ops;

public sealed record OperationalConfigDto(
    decimal CommissionFactor,
    int AvgPeriods,
    int NewsCadenceMinutes,
    int FibraNewsMonths,
    int FundamentalsCadenceMinutes,
    int DistributionCadenceMinutes,
    DateTimeOffset UpdatedAt,
    string? UpdatedBy,
    bool TermsEnabled,
    string? TermsText,
    string? ContactEmail,
    int UniverseDegradationThresholdPct);
