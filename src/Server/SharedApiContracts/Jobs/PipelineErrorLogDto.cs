namespace SharedApiContracts.Jobs;

public sealed record PipelineErrorLogDto(
    Guid Id,
    string Pipeline,
    DateTimeOffset Timestamp,
    string ErrorType,
    string Message,
    string? Context,
    string AiContext,
    DateTimeOffset CreatedAt
);
