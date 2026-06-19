using Domain.Ops;

namespace Application.Ops;

public interface IOperationalConfigRepository
{
    Task<OperationalConfig> GetAsync(CancellationToken ct = default);
    Task UpdateCetesRateAsync(decimal rate, DateTimeOffset updatedAt, CancellationToken ct = default);
    Task UpdateTiieRateAsync(decimal rate, DateTimeOffset updatedAt, CancellationToken ct = default);
    Task UpdateOrganizationSameAsAsync(string? organizationSameAsJson, string actor, CancellationToken ct = default);
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
        decimal? isrRetentionRate = null,
        decimal? ivaRate = null,
        CancellationToken ct = default);
}
