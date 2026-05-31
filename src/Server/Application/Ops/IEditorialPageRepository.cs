using Domain.Ops;

namespace Application.Ops;

public interface IEditorialPageRepository
{
    Task<IReadOnlyList<EditorialPage>> GetAllAsync(CancellationToken ct = default);
    Task<EditorialPage?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<int> UpdateContentAsync(string slug, string content, CancellationToken ct = default);
}
