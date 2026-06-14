using Domain.Seo;

namespace Application.Seo;

public interface ISeoMetadataRepository
{
    Task<SeoMetadata?> GetAsync(SeoPageType pageType, string entityKey, CancellationToken ct = default);
    Task<IReadOnlyList<SeoMetadata>> GetAllAsync(SeoMetadataQuery? query = null, CancellationToken ct = default);
    Task<bool> ExistsAsync(SeoPageType pageType, string entityKey, CancellationToken ct = default);
    Task<SeoMetadata> UpsertAsync(SeoMetadata metadata, bool overrideMode = false, CancellationToken ct = default);
    Task<IReadOnlyList<(SeoPageType PageType, string EntityKey)>> GetExistingKeysAsync(
        IEnumerable<(SeoPageType PageType, string EntityKey)> keys,
        CancellationToken ct = default);
}
