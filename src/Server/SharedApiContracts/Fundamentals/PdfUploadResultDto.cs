namespace SharedApiContracts.Fundamentals;

public sealed record PdfUploadResultDto(
    Guid Id,
    string FibraTicker,
    string Period,
    bool MarkdownExtracted,
    bool IsPossibleUpdate,
    string? WarningMessage);
