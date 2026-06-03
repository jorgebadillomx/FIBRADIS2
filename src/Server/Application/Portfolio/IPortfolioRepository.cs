using Domain.Portfolio;

namespace Application.Portfolio;

public interface IPortfolioRepository
{
    Task<IReadOnlyList<PortfolioPosition>> GetByUserIdAsync(Guid userId, CancellationToken ct);
    Task UpsertPortfolioAsync(Guid userId, IReadOnlyList<PortfolioPosition> positions, CancellationToken ct);
    Task<int> GetPositionCountByUserIdAsync(Guid userId, CancellationToken ct);
    Task<UserPortfolioSettings?> GetSettingsAsync(Guid userId, CancellationToken ct);
    Task UpsertSettingsAsync(Guid userId, string? columnConfigJson, CancellationToken ct);
}
