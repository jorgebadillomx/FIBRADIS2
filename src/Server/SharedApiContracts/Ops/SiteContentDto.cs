namespace SharedApiContracts.Ops;

public sealed record SiteContentDto(
    bool TermsEnabled,
    string? TermsText,
    string? ContactEmail);
