namespace Domain.Ops;

public class ConfigAuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Actor { get; set; } = string.Empty;
    public DateTimeOffset ChangedAt { get; set; } = DateTimeOffset.UtcNow;
    public string FieldName { get; set; } = string.Empty;
    public string? PreviousValue { get; set; }
    public string? NewValue { get; set; }
}
