using Domain.Market;

namespace Application.Market;

public interface IMarketRepository
{
    Task AddPriceSnapshotAsync(PriceSnapshot snapshot, CancellationToken ct = default);
    Task<IReadOnlyList<PriceSnapshot>> GetLastSnapshotsAsync(Guid fibraId, int count, CancellationToken ct = default);
    Task<bool> UpsertDailySnapshotAsync(DailySnapshot snapshot, CancellationToken ct = default);
    Task<DateOnly?> GetLatestDailySnapshotDateAsync(Guid fibraId, CancellationToken ct = default);
    Task DeleteAllDailySnapshotsAsync(CancellationToken ct = default);
    Task DeleteOldPriceSnapshotsAsync(DateOnly cutoff, CancellationToken ct = default);
    Task<IReadOnlyList<PriceSnapshot>> GetLatestSnapshotPerFibraAsync(CancellationToken ct = default);

    /// <summary>Último snapshot <c>Processed</c> de una FIBRA específica (consulta dirigida, no carga el universo).</summary>
    Task<PriceSnapshot?> GetLatestProcessedSnapshotAsync(Guid fibraId, CancellationToken ct = default);

    Task<IReadOnlyList<DailySnapshot>> GetDailySnapshotsAsync(Guid fibraId, int days, CancellationToken ct = default);
    Task<IReadOnlyDictionary<Guid, IReadOnlyList<DailySnapshot>>> GetDailySnapshotsByFibrasAsync(
        IReadOnlyList<Guid> fibraIds, int days, CancellationToken ct = default);
    Task<IReadOnlyList<Distribution>> GetDistributionsAsync(Guid fibraId, int? maxDays = null, CancellationToken ct = default);
    Task<IReadOnlyList<Distribution>> GetDistributionsByFibrasAsync(IReadOnlyList<Guid> fibraIds, int days, CancellationToken ct = default);
    Task<IReadOnlyDictionary<Guid, decimal>> GetWeek52AvgByFibrasAsync(
        IReadOnlyList<Guid> fibraIds, int days = 365, CancellationToken ct = default);
    Task AddDistributionAsync(Distribution dist, CancellationToken ct = default);
    Task<bool> UpsertDistributionAsync(Distribution dist, CancellationToken ct = default);
    Task<int> GetDistributionCountAsync(CancellationToken ct = default);
    Task<IReadOnlyCollection<Guid>> GetFibraIdsWithDistributionsAsync(CancellationToken ct = default);
    Task<Distribution?> GetDistributionByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Distribution>> GetDistributionsByRangeAsync(DateOnly from, DateOnly to, CancellationToken ct = default);
    Task<bool> UpdateDistributionAmountAsync(Guid fibraId, DateOnly paymentDate, decimal amount, CancellationToken ct = default);
    Task<bool> UpdateDistributionBreakdownAsync(
        Guid fibraId,
        DateOnly paymentDate,
        DateOnly? exDate,
        decimal? taxable,
        decimal? capital,
        string? avisoUrl,
        CancellationToken ct = default);
    Task UpdateDistributionAsync(Distribution distribution, CancellationToken ct = default);
    Task<bool> DeleteDistributionAsync(Guid id, CancellationToken ct = default);
}
