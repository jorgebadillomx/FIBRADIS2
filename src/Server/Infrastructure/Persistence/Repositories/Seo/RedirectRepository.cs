using Application.Seo;
using Domain.Seo;
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories.Seo;

public class RedirectRepository(AppDbContext db) : IRedirectRepository
{
    public async Task<IReadOnlyList<UrlRedirect>> GetAllAsync(CancellationToken ct = default)
        => await db.UrlRedirects
            .AsNoTracking()
            .OrderBy(redirect => redirect.FromPath)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<UrlRedirect>> GetActiveAsync(CancellationToken ct = default)
        => await db.UrlRedirects
            .AsNoTracking()
            .Where(redirect => redirect.IsActive)
            .OrderBy(redirect => redirect.FromPath)
            .ToListAsync(ct);

    public Task<UrlRedirect?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.UrlRedirects.FirstOrDefaultAsync(redirect => redirect.Id == id, ct);

    public Task<UrlRedirect?> GetByFromPathAsync(string fromPath, CancellationToken ct = default)
        => db.UrlRedirects.FirstOrDefaultAsync(
            redirect => redirect.FromPath == UrlRedirectPath.Normalize(fromPath),
            ct);

    public async Task AddAsync(UrlRedirect redirect, CancellationToken ct = default)
    {
        Normalize(redirect);
        db.UrlRedirects.Add(redirect);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(UrlRedirect redirect, CancellationToken ct = default)
    {
        Normalize(redirect);
        db.UrlRedirects.Update(redirect);
        await db.SaveChangesAsync(ct);
    }

    private static void Normalize(UrlRedirect redirect)
    {
        redirect.FromPath = UrlRedirectPath.Normalize(redirect.FromPath);
        redirect.ToPath = UrlRedirectPath.Normalize(redirect.ToPath);
        redirect.Notes = string.IsNullOrWhiteSpace(redirect.Notes) ? null : redirect.Notes.Trim();
        redirect.CreatedBy = redirect.CreatedBy.Trim();
        redirect.UpdatedBy = redirect.UpdatedBy.Trim();
    }
}
