using Domain.Seo;

namespace Application.Seo;

public interface IRedirectRepository
{
    Task<IReadOnlyList<UrlRedirect>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<UrlRedirect>> GetActiveAsync(CancellationToken ct = default);
    Task<UrlRedirect?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<UrlRedirect?> GetByFromPathAsync(string fromPath, CancellationToken ct = default);
    Task AddAsync(UrlRedirect redirect, CancellationToken ct = default);
    Task UpdateAsync(UrlRedirect redirect, CancellationToken ct = default);
}
