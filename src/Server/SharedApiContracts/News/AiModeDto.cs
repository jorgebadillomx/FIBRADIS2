namespace SharedApiContracts.News;

public sealed record AiModeDto(
    string Mode,
    DateTimeOffset UpdatedAt,
    string? UpdatedBy,
    string? PreviousMode
);
