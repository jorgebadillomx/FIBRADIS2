namespace SharedApiContracts.Ops;

public sealed record UpdateOperationalConfigRequest(
    decimal? CommissionFactor,
    int? AvgPeriods,
    int? NewsCadenceMinutes,
    int? FibraNewsMonths,
    int? FundamentalsCadenceMinutes = null);
