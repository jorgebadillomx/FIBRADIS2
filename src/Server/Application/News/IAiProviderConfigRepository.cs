using Domain.News;

namespace Application.News;

public interface IAiProviderConfigRepository
{
    Task<AiProviderConfig> GetConfigAsync(CancellationToken ct = default);
    Task SetProviderAsync(AiProvider provider, string modelId, string actor, CancellationToken ct = default);
}
