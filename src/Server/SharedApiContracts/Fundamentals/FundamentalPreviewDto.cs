namespace SharedApiContracts.Fundamentals;

public sealed record FundamentalPreviewDto(
    Guid Id,
    string FibraTicker,
    string Period,
    string Status,
    bool IsPossibleUpdate,
    string? WarningMessage,
    IReadOnlyList<string> PresentFields,
    IReadOnlyList<string> MissingFields,
    string? PdfReference,
    DateTimeOffset CapturedAt);
