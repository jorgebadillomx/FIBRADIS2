using Domain.News;
using Application.News;
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories.News;

public class BlocklistRepository(AppDbContext db) : IBlocklistRepository
{
    public async Task<IReadOnlyList<BlocklistTerm>> GetAllAsync(CancellationToken ct = default)
        => await db.BlocklistTerms
            .OrderBy(t => t.Term)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<string>> GetAllTermsAsync(CancellationToken ct = default)
        => await db.BlocklistTerms
            .Select(t => t.Term)
            .ToListAsync(ct);

    public async Task<bool> ExistsAsync(string term, CancellationToken ct = default)
    {
        var normalizedTerm = NormalizeTerm(term);
        return await db.BlocklistTerms.AnyAsync(t => t.Term == normalizedTerm, ct);
    }

    public async Task<BlocklistTerm> AddAsync(string term, CancellationToken ct = default)
    {
        var entity = new BlocklistTerm
        {
            Term = NormalizeTerm(term),
            CreatedAt = DateTimeOffset.UtcNow,
        };

        db.BlocklistTerms.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await db.BlocklistTerms.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (entity is null)
            return false;

        db.BlocklistTerms.Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static string NormalizeTerm(string term)
        => string.Join(
            ' ',
            term
                .Trim()
                .ToLowerInvariant()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries));
}
