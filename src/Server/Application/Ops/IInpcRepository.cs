using Domain.Ops;

namespace Application.Ops;

public interface IInpcRepository
{
    Task<DateOnly?> GetLatestPeriodoAsync(CancellationToken ct = default);
    Task UpsertManyAsync(IEnumerable<InpcMonthlyEntry> entries, CancellationToken ct = default);
    Task<IReadOnlyList<InpcMonthlyEntry>> GetLastAsync(int count, CancellationToken ct = default);
    Task<IReadOnlyList<InpcMonthlyEntry>> GetRangeAsync(DateOnly from, DateOnly to, CancellationToken ct = default);
}
