namespace SharedApiContracts.Ai;

public sealed record AiCallLogDto(
    Guid Id,
    DateTimeOffset Timestamp,
    string Operation,
    string Provider,
    string ModelId,
    int PromptLength,
    long DurationMs,
    bool Success,
    string? InputPreview,
    string? ResponseRaw,
    string? ErrorMessage,
    string? Context,
    DateTimeOffset CreatedAt);
