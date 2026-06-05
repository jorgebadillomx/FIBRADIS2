using Application.Portfolio;
using Domain.Portfolio;
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories.Portfolio;

public class UserFavoritesRepository(AppDbContext db) : IUserFavoritesRepository
{
    public async Task<IReadOnlyList<Guid>> GetFavoriteIdsAsync(Guid userId, CancellationToken ct)
        => await db.UserFavorites
            .Where(f => f.UserId == userId)
            .OrderBy(f => f.AddedAt)
            .Select(f => f.FibraId)
            .ToListAsync(ct);

    public async Task AddAsync(Guid userId, Guid fibraId, CancellationToken ct)
    {
        var exists = await db.UserFavorites
            .AnyAsync(f => f.UserId == userId && f.FibraId == fibraId, ct);
        if (exists)
            return;

        db.UserFavorites.Add(new UserFavorite
        {
            UserId = userId,
            FibraId = fibraId,
            AddedAt = DateTimeOffset.UtcNow,
        });

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Concurrent PUT for the same (UserId, FibraId) — composite PK violation.
            // AnyAsync passed but another request inserted between the check and save.
            // The favorite is already persisted; treat as no-op.
        }
    }

    public async Task RemoveAsync(Guid userId, Guid fibraId, CancellationToken ct)
    {
        var entity = await db.UserFavorites
            .FirstOrDefaultAsync(f => f.UserId == userId && f.FibraId == fibraId, ct);
        if (entity is null)
            return;

        db.UserFavorites.Remove(entity);
        await db.SaveChangesAsync(ct);
    }
}
