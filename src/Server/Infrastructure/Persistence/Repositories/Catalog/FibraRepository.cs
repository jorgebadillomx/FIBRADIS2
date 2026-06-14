using System.Data;
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

    public async Task<IReadOnlyList<Fibra>> GetActiveBySectorAsync(
        string sector, Guid excludeId, int count, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(sector) || count <= 0)
            return [];

        return await db.Fibras
            .AsNoTracking()
            .Where(f => f.State == FibraState.Active && f.Sector == sector && f.Id != excludeId)
            .OrderBy(f => f.Ticker)
            .Take(count)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<(string FullName, string Ticker)>> GetAllActiveForSitemapAsync(CancellationToken ct = default)
    {
        var query = db.Fibras
            .AsNoTracking()
            .Where(f => f.State == FibraState.Active)
            .OrderBy(f => f.Ticker)
            .Select(f => new { f.FullName, f.Ticker });

        // READ UNCOMMITTED evita contención de locks al generar el sitemap; el provider
        // InMemory (tests) no soporta transacciones con isolation level, así que se omite
        if (!db.Database.IsRelational())
        {
            var rowsNoTx = await query.ToListAsync(ct);
            return rowsNoTx.Select(r => (r.FullName, r.Ticker)).ToList();
        }

        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadUncommitted, ct);
        var rows = await query.ToListAsync(ct);
        await tx.CommitAsync(ct);
        return rows.Select(r => (r.FullName, r.Ticker)).ToList();
    }
}
