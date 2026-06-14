namespace SharedApiContracts.Seo;

public record UrlRedirectDto(
    Guid Id,
    string FromPath,
    string ToPath,
    int StatusCode,
    bool IsActive,
    string? Notes,
    DateTimeOffset CreatedAt,
    string CreatedBy,
    DateTimeOffset UpdatedAt,
    string UpdatedBy);
