namespace Domain.Fundamentals;

public class FundamentalSourceManifest
{
    public Guid Id { get; init; }
    public string SourceName { get; set; } = "AMEFIBRA";
    public Guid? FibraId { get; set; }
    public string SourceTitle { get; set; } = string.Empty;
    public string? Period { get; set; }
    public string ReportType { get; set; } = "quarterly";
    public string DiscoveryStatus { get; set; } = "eligible";
    public string PackageUrl { get; set; } = string.Empty;
    public string? DownloadUrl { get; set; }
    public string? DownloadSignature { get; set; }
    public string? PdfUrl { get; set; }
    public string? FileName { get; set; }
    public DateTimeOffset? SourcePublishedAt { get; set; }
    public DateTimeOffset FirstSeenAt { get; set; }
    public DateTimeOffset LastSeenAt { get; set; }
    public string LastDecision { get; set; } = "new";
    public string? LastDecisionReason { get; set; }
    public Guid? LastProcessedRecordId { get; set; }
    public string? LastError { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
