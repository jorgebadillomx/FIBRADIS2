namespace SharedApiContracts.Fundamentals;

public sealed record FundamentalRecordDto(
    Guid Id,
    string FibraTicker,
    string Period,
    string Status,
    bool IsPossibleUpdate,
    decimal? CapRate,
    decimal? NavPerCbfi,
    decimal? Ltv,
    decimal? NoiMargin,
    decimal? FfoMargin,
    decimal? QuarterlyDistribution,
    string? Summary,
    string? PdfReference,
    DateTimeOffset? PdfUploadedAt,
    string? ImportedBy,
    string? ConfirmedBy,
    DateTimeOffset CapturedAt,
    DateTimeOffset? ConfirmedAt,
    bool HasMarkdownContent = false,
    IReadOnlyDictionary<string, string>? FieldNotes = null,
    DateTimeOffset? DeletedAt = null);
