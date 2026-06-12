namespace Application.Integrations;

public interface IBanxicoClient
{
    Task<decimal?> GetCetes28dAsync(CancellationToken ct = default);
}
