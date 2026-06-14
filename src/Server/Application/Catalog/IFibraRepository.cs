namespace Application.Catalog;

public interface IFibraRepository
{
    Task AddAsync(Domain.Catalog.Fibra fibra, CancellationToken ct = default);

    Task UpdateAsync(Domain.Catalog.Fibra fibra, CancellationToken ct = default);

    Task<bool> ExistsByTickerAsync(string ticker, CancellationToken ct = default);

    Task<(IReadOnlyList<Domain.Catalog.Fibra> Items, int Total)> GetActivePagedAsync(
        FibraFilter filter, CancellationToken ct = default);

    Task<Domain.Catalog.Fibra?> GetByTickerAsync(string ticker, CancellationToken ct = default);

    Task<Domain.Catalog.Fibra?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<Domain.Catalog.Fibra>> GetAllAsync(CancellationToken ct = default);

    Task<IReadOnlyList<Domain.Catalog.Fibra>> GetAllActiveAsync(CancellationToken ct = default);

    /// <summary>
    /// FIBRAs activas del mismo sector, excluyendo la propia. Para enlazado interno
    /// "FIBRAs relacionadas" en la ficha (story 12-8). Ordenadas por ticker, máximo <paramref name="count"/>.
    /// </summary>
    Task<IReadOnlyList<Domain.Catalog.Fibra>> GetActiveBySectorAsync(
        string sector, Guid excludeId, int count, CancellationToken ct = default);

    Task<IReadOnlyList<(string FullName, string Ticker)>> GetAllActiveForSitemapAsync(CancellationToken ct = default);
}
