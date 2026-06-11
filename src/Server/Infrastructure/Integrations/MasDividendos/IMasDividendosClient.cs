namespace Infrastructure.Integrations.MasDividendos;

public interface IMasDividendosClient
{
    Task<IReadOnlyList<MasDividendosRecord>> GetAllAsync(CancellationToken ct = default);
}
