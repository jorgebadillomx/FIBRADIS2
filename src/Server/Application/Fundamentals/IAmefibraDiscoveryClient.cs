namespace Application.Fundamentals;

public interface IAmefibraDiscoveryClient
{
    Task<IReadOnlyList<AmefibraListingItem>> GetListingItemsAsync(CancellationToken ct);
    Task<AmefibraPackageDetails> GetPackageDetailsAsync(string packageUrl, CancellationToken ct);
    Task<(byte[] Content, string? PdfUrl, string? FileName)> DownloadPdfAsync(string packageUrl, string downloadUrl, CancellationToken ct);
}
