namespace SharedApiContracts.News;

public sealed record AiPromptDto(
    string ContentType,
    string PromptTemplate,
    DateTimeOffset UpdatedAt,
    string UpdatedBy
);
