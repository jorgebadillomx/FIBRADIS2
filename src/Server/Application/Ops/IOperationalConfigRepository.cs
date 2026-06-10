using Domain.Ops;

namespace Application.Ops;

public interface IOperationalConfigRepository
{
    Task<OperationalConfig> GetAsync(CancellationToken ct = default);
    Task UpdateAsync(
        decimal? commissionFactor,
        int? avgPeriods,
        int? newsCadenceMinutes,
        int? fibraNewsMonths,
        int? distributionCadenceMinutes,
        bool? termsEnabled,
        string? termsText,
        string? contactEmail,
        string actor,
        int? fundamentalsCadenceMinutes = null,
        int? universeDegradationThresholdPct = null,
        CancellationToken ct = default);
}
