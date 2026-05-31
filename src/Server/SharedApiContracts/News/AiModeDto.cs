namespace SharedApiContracts.News;

public sealed record AiModeDto(
    string Mode,
    string NewsModel,
    DateTimeOffset UpdatedAt,
    string? UpdatedBy,
    string? PreviousMode,
    int MinBodyTextLengthForAi
);
