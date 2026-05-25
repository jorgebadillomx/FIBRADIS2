namespace SharedApiContracts.Ops;

public sealed record ConfigAuditLogDto(
    Guid Id,
    string Actor,
    DateTimeOffset ChangedAt,
    string FieldName,
    string? PreviousValue,
    string? NewValue);
