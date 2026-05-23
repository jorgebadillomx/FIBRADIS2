namespace SharedApiContracts.News;

public sealed record AiProviderConfigDto(
    string Provider,
    string ModelId,
    DateTimeOffset UpdatedAt,
    string? UpdatedBy,
    IReadOnlyList<AiProviderOptionDto> AvailableProviders
);
