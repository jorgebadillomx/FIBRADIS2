namespace SharedApiContracts.Editorial;

public sealed record EditorialPageDto(
    string Slug,
    string Title,
    string Content,
    DateTimeOffset UpdatedAt
);
