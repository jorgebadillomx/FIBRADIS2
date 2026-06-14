namespace SharedApiContracts.Seo;

public record UpsertUrlRedirectRequest(
    string FromPath,
    string ToPath,
    int StatusCode,
    bool IsActive,
    string? Notes);
