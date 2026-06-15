namespace SharedApiContracts.Seo;

public record SeoMetadataDto(
    Guid Id,
    string PageType,
    string EntityKey,
    string Title,
    string MetaDescription,
    string CanonicalPath,
    string RobotsDirectives,
    bool RobotsDirectivesIsOverridden,
    bool IsActive,
    DateTimeOffset UpdatedAt,
    string UpdatedBy);
