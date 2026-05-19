using Domain.Market;

namespace Application.Market;

public interface IMarketRepository
{
    Task AddPriceSnapshotAsync(PriceSnapshot snapshot, CancellationToken ct = default);
    Task<IReadOnlyList<PriceSnapshot>> GetLastSnapshotsAsync(Guid fibraId, int count, CancellationToken ct = default);
    Task UpsertDailySnapshotAsync(DailySnapshot snapshot, CancellationToken ct = default);

    Task<IReadOnlyList<PriceSnapshot>> GetLatestSnapshotPerFibraAsync(CancellationToken ct = default);
}
