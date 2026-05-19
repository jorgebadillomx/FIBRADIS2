using Domain.Market;

namespace Application.Market;

public interface IMarketRepository
{
    Task AddPriceSnapshotAsync(PriceSnapshot snapshot, CancellationToken ct = default);
    Task<IReadOnlyList<PriceSnapshot>> GetLastSnapshotsAsync(Guid fibraId, int count, CancellationToken ct = default);
    Task UpsertDailySnapshotAsync(DailySnapshot snapshot, CancellationToken ct = default);
    Task<IReadOnlyList<PriceSnapshot>> GetLatestSnapshotPerFibraAsync(CancellationToken ct = default);

    Task<IReadOnlyList<DailySnapshot>> GetDailySnapshotsAsync(Guid fibraId, int days, CancellationToken ct = default);
    Task<IReadOnlyList<Distribution>> GetDistributionsAsync(Guid fibraId, int? maxDays = null, CancellationToken ct = default);
    Task AddDistributionAsync(Distribution dist, CancellationToken ct = default);
}
