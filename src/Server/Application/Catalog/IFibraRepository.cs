namespace Application.Catalog;

public interface IFibraRepository
{
    Task<(IReadOnlyList<Domain.Catalog.Fibra> Items, int Total)> GetActivePagedAsync(
        FibraFilter filter, CancellationToken ct = default);

    Task<Domain.Catalog.Fibra?> GetByTickerAsync(string ticker, CancellationToken ct = default);

    Task<Domain.Catalog.Fibra?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<Domain.Catalog.Fibra>> GetAllActiveAsync(CancellationToken ct = default);
}
