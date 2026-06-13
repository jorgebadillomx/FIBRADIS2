namespace Application.Integrations;

public interface IBanxicoClient
{
    Task<decimal?> GetCetes28dAsync(CancellationToken ct = default);
    Task<decimal?> GetTiie28dAsync(CancellationToken ct = default);
    Task<IReadOnlyList<(DateOnly Periodo, decimal InpcIndex)>> GetInpcHistoryAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken ct = default);
}
