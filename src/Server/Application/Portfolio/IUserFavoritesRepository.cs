namespace Application.Portfolio;

public interface IUserFavoritesRepository
{
    Task<IReadOnlyList<Guid>> GetFavoriteIdsAsync(Guid userId, CancellationToken ct);
    Task AddAsync(Guid userId, Guid fibraId, CancellationToken ct);
    Task RemoveAsync(Guid userId, Guid fibraId, CancellationToken ct);
}
