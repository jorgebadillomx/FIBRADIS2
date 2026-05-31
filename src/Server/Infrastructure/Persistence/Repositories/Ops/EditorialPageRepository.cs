using Application.Ops;
using Domain.Ops;
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories.Ops;

public class EditorialPageRepository(AppDbContext db) : IEditorialPageRepository
{
    public async Task<IReadOnlyList<EditorialPage>> GetAllAsync(CancellationToken ct = default)
        => await db.EditorialPages
            .OrderBy(page => page.Order)
            .ToListAsync(ct);

    public Task<EditorialPage?> GetBySlugAsync(string slug, CancellationToken ct = default)
        => db.EditorialPages.FirstOrDefaultAsync(page => page.Slug == slug, ct);

    public Task<int> UpdateContentAsync(string slug, string content, CancellationToken ct = default)
        => db.EditorialPages
            .Where(page => page.Slug == slug)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(page => page.Content, content)
                .SetProperty(page => page.UpdatedAt, DateTimeOffset.UtcNow), ct);
}
