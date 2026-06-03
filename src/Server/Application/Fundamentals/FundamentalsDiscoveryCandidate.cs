namespace Application.Fundamentals;

public sealed record FundamentalsDiscoveryCandidate(
    string SourceName,
    string SourceTitle,
    string PackageUrl,
    string? DownloadUrl,
    string? Period,
    string ReportType,
    DateTimeOffset? PublishedAt);
