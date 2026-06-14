namespace SharedApiContracts.Seo;

public record FaqItemDto(
    Guid Id,
    string PageType,
    string EntityKey,
    string Question,
    string Answer,
    int Order,
    bool IsActive,
    DateTimeOffset UpdatedAt,
    string UpdatedBy);
