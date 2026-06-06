using Domain.Fundamentals;

namespace Application.Fundamentals;

public interface IFundamentalSourceManifestRepository
{
    Task<FundamentalSourceManifest?> GetBySourceAndPackageUrlAsync(string sourceName, string? packageUrl, CancellationToken ct);
    Task<FundamentalSourceManifest?> GetLatestByFibraAndPeriodAsync(Guid fibraId, string period, CancellationToken ct);
    Task AddAsync(FundamentalSourceManifest manifest, CancellationToken ct);
    Task UpdateAsync(FundamentalSourceManifest manifest, CancellationToken ct);
}
