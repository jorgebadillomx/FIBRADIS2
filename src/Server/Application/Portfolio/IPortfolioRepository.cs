using Domain.Portfolio;

namespace Application.Portfolio;

public interface IPortfolioRepository
{
    Task<IReadOnlyList<PortfolioPosition>> GetByUserIdAsync(Guid userId, CancellationToken ct);
    Task UpsertPortfolioAsync(Guid userId, IReadOnlyList<PortfolioPosition> positions, CancellationToken ct);
    Task ArchivePortfolioAsync(Guid userId, CancellationToken ct);
    Task<bool> RestoreSnapshotAsync(Guid userId, CancellationToken ct);
    Task<PortfolioSnapshot?> GetSnapshotAsync(Guid userId, CancellationToken ct);
    Task MergePositionsAsync(Guid userId, IReadOnlyList<PortfolioPosition> positions, CancellationToken ct);
    Task<int> GetPositionCountByUserIdAsync(Guid userId, CancellationToken ct);
    Task<UserPortfolioSettings?> GetSettingsAsync(Guid userId, CancellationToken ct);
    Task UpsertSettingsAsync(Guid userId, string? columnConfigJson, CancellationToken ct);
    Task<PortfolioPosition?> GetPositionAsync(Guid userId, Guid fibraId, CancellationToken ct = default);
    Task UpdatePositionAsync(PortfolioPosition position, CancellationToken ct = default);
    Task<bool> DeletePositionAsync(Guid userId, Guid fibraId, CancellationToken ct = default);
}
