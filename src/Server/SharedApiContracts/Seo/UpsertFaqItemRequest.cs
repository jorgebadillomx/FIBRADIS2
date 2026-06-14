namespace SharedApiContracts.Seo;

public record UpsertFaqItemRequest(
    string PageType,
    string EntityKey,
    string Question,
    string Answer,
    int Order,
    bool IsActive);
