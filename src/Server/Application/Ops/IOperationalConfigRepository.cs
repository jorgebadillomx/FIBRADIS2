using Domain.Ops;

namespace Application.Ops;

public interface IOperationalConfigRepository
{
    Task<OperationalConfig> GetAsync(CancellationToken ct = default);
    Task UpdateAsync(
        decimal? commissionFactor,
        int? avgPeriods,
        int? newsCadenceMinutes,
        string actor,
        CancellationToken ct = default);
}
