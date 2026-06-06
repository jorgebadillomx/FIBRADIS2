using Application.Fundamentals;
using Domain.Fundamentals;
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories.Fundamentals;

public class FundamentalSourceManifestRepository(AppDbContext db) : IFundamentalSourceManifestRepository
{
    public async Task<FundamentalSourceManifest?> GetBySourceAndPackageUrlAsync(string sourceName, string? packageUrl, CancellationToken ct)
        => await db.FundamentalSourceManifests
            .FirstOrDefaultAsync(x => x.SourceName == sourceName && x.PackageUrl == packageUrl, ct);

    public async Task<FundamentalSourceManifest?> GetLatestByFibraAndPeriodAsync(Guid fibraId, string period, CancellationToken ct)
        => await db.FundamentalSourceManifests
            .Where(x => x.FibraId == fibraId && x.Period == period && x.ReportType == "quarterly")
            .OrderByDescending(x => x.UpdatedAt)
            .FirstOrDefaultAsync(ct);

    public async Task AddAsync(FundamentalSourceManifest manifest, CancellationToken ct)
    {
        db.FundamentalSourceManifests.Add(manifest);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(FundamentalSourceManifest manifest, CancellationToken ct)
    {
        db.FundamentalSourceManifests.Update(manifest);
        await db.SaveChangesAsync(ct);
    }
}
