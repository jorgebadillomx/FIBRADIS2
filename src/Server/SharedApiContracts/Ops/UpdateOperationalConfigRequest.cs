namespace SharedApiContracts.Ops;

public sealed record UpdateOperationalConfigRequest(
    decimal? CommissionFactor,
    int? AvgPeriods,
    int? NewsCadenceMinutes,
    int? FibraNewsMonths,
    int? FundamentalsCadenceMinutes = null,
    int? DistributionCadenceMinutes = null,
    bool? TermsEnabled = null,
    string? TermsText = null,
    string? ContactEmail = null,
    int? UniverseDegradationThresholdPct = null,
    decimal? IsrRetentionRate = null,
    decimal? IvaRate = null);
