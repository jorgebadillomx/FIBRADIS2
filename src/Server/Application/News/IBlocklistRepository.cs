using Domain.News;

namespace Application.News;

public interface IBlocklistRepository
{
    Task<IReadOnlyList<BlocklistTerm>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetAllTermsAsync(CancellationToken ct = default);
    Task<bool> ExistsAsync(string term, CancellationToken ct = default);
    Task<BlocklistTerm> AddAsync(string term, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
