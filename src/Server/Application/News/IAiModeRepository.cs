using Domain.News;

namespace Application.News;

public interface IAiModeRepository
{
    Task<AiMode> GetCurrentModeAsync(CancellationToken ct = default);
    Task<AiModeConfig> GetConfigAsync(CancellationToken ct = default);
    Task SetModeAsync(AiMode mode, string actor, CancellationToken ct = default);
}
