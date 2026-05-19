using Application.Catalog;
using Domain.Catalog;
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories.Catalog;

public class FibraRepository(AppDbContext db) : IFibraRepository
{
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

    public async Task<IReadOnlyList<Fibra>> GetAllActiveAsync(CancellationToken ct = default)
        => await db.Fibras
            .Where(f => f.State == FibraState.Active)
            .OrderBy(f => f.Ticker)
            .ToListAsync(ct);
}
