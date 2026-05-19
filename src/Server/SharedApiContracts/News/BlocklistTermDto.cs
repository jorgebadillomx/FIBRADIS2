namespace SharedApiContracts.News;

public sealed record BlocklistTermDto(
    Guid Id,
    string Term,
    DateTimeOffset CreatedAt
);
