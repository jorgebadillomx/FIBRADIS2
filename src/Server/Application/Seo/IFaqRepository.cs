using Domain.Seo;

namespace Application.Seo;

public interface IFaqRepository
{
    Task<IReadOnlyList<FaqItem>> GetByPageAsync(
        SeoPageType pageType,
        string entityKey,
        bool includeInactive = false,
        CancellationToken ct = default);

    Task<FaqItem?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<FaqItem?> GetByNaturalKeyAsync(SeoPageType pageType, string entityKey, string question, CancellationToken ct = default);

    Task<bool> ExistsAsync(SeoPageType pageType, string entityKey, string question, CancellationToken ct = default);

    Task AddAsync(FaqItem item, CancellationToken ct = default);

    Task<bool> AddIfMissingAsync(FaqItem item, CancellationToken ct = default);

    Task UpdateAsync(FaqItem item, CancellationToken ct = default);

    Task<bool> DeactivateAsync(Guid id, string updatedBy, CancellationToken ct = default);
}
