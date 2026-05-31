namespace Application.Fundamentals;

public sealed record AmefibraListingItem(
    string Title,
    string PackageUrl,
    string? DownloadUrl);

public sealed record AmefibraPackageDetails(
    string PackageUrl,
    string? DownloadUrl,
    DateTimeOffset? SourcePublishedAt);

public sealed record AmefibraTitleParseResult(
    string? FibraHint,
    string? Period,
    string ReportType,
    string DiscoveryStatus,
    string? ErrorReason);
