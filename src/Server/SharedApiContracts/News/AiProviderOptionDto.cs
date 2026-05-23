namespace SharedApiContracts.News;

public sealed record AiProviderOptionDto(
    string Provider,
    IReadOnlyList<string> Models
);
