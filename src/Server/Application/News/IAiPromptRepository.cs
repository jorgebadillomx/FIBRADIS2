using Domain.News;

namespace Application.News;

public interface IAiPromptRepository
{
    Task<AiPrompt?> GetPromptAsync(string contentType, CancellationToken ct = default);
    Task SetPromptAsync(string contentType, string template, string actor, CancellationToken ct = default);
}
