using Application.Catalog;
using Domain.Catalog;
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories.Catalog;

public class FibraRepository(AppDbContext db) : IFibraRepository
{
    public async Task AddAsync(Fibra fibra, CancellationToken ct = default)
    {
        fibra.Ticker = fibra.Ticker.Trim().ToUpperInvariant();

        if (await db.Fibras.AnyAsync(f => f.Ticker == fibra.Ticker, ct))
        {
            throw new DbUpdateException($"Ya existe una FIBRA con ticker '{fibra.Ticker}'.");
        }

        db.Fibras.Add(fibra);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Fibra fibra, CancellationToken ct = default)
    {
        db.Fibras.Update(fibra);
        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> ExistsByTickerAsync(string ticker, CancellationToken ct = default)
        => await db.Fibras.AnyAsync(f => f.Ticker == ticker.ToUpper(), ct);

    public async Task<(IReadOnlyList<Fibra> Items, int Total)> GetActivePagedAsync(
        FibraFilter filter, CancellationToken ct = default)
    {
        var query = db.Fibras
            .Where(f => f.State == FibraState.Active)
            .OrderBy(f => f.Ticker);

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<Fibra?> GetByTickerAsync(string ticker, CancellationToken ct = default)
        => await db.Fibras
            .FirstOrDefaultAsync(f => f.Ticker == ticker.ToUpper(), ct);

    public async Task<Fibra?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.Fibras.FirstOrDefaultAsync(f => f.Id == id, ct);

    public async Task<IReadOnlyList<Fibra>> GetAllAsync(CancellationToken ct = default)
        => await db.Fibras
            .OrderBy(f => f.Ticker)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Fibra>> GetAllActiveAsync(CancellationToken ct = default)
        => await db.Fibras
            .Where(f => f.State == FibraState.Active)
            .OrderBy(f => f.Ticker)
            .ToListAsync(ct);
}
