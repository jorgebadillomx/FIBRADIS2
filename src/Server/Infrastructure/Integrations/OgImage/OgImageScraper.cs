using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using Application.News;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Integrations.OgImage;

public partial class OgImageScraper(HttpClient http, ILogger<OgImageScraper> logger) : IOgImageScraper
{
    public async Task<string?> TryGetOgImageAsync(string url, CancellationToken ct = default)
    {
        try
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var parsedUrl) || !await IsAllowedHostAsync(parsedUrl, ct))
            {
                logger.LogDebug("og:image scraping skipped for '{Url}': private or loopback host", url);
                return null;
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(0, 16383);
            using var response = await http.SendAsync(request, HttpCompletionOption.ResponseContentRead, ct);
            if (!response.IsSuccessStatusCode)
                return null;

            var html = await response.Content.ReadAsStringAsync(ct);
            var match = OgImagePropertyRegex().Match(html);
            if (!match.Success)
                match = OgImageContentRegex().Match(html);

            if (!match.Success)
                return null;

            var imageUrl = WebUtility.HtmlDecode(match.Groups[1].Value.Trim());
            var normalizedImageUrl = imageUrl.StartsWith("//") ? $"https:{imageUrl}" : imageUrl;
            return Uri.TryCreate(normalizedImageUrl, UriKind.Absolute, out var imageUri)
                && (imageUri.Scheme == Uri.UriSchemeHttp || imageUri.Scheme == Uri.UriSchemeHttps)
                && normalizedImageUrl.Length <= 2048
                ? normalizedImageUrl
                : null;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "og:image extraction failed for '{Url}'", url);
            return null;
        }
    }

    private async Task<bool> IsAllowedHostAsync(Uri uri, CancellationToken ct)
    {
        try
        {
            IPAddress[] addresses;
            if (IPAddress.TryParse(uri.Host, out var literalIp))
                addresses = [literalIp];
            else
                addresses = await Dns.GetHostAddressesAsync(uri.Host, ct);

            return addresses.Length > 0 && addresses.All(IsAllowedIp);
        }
        catch (SocketException ex)
        {
            logger.LogDebug(ex, "og:image scraping skipped for '{Host}': DNS resolution failed", uri.Host);
            return false;
        }
    }

    private static bool IsAllowedIp(IPAddress ip)
    {
        if (ip.IsIPv4MappedToIPv6)
            ip = ip.MapToIPv4();

        if (IPAddress.IsLoopback(ip)) return false;

        if (ip.AddressFamily == AddressFamily.InterNetwork)
        {
            var b = ip.GetAddressBytes();
            return !(b[0] == 10
                || (b[0] == 172 && b[1] >= 16 && b[1] <= 31)
                || (b[0] == 192 && b[1] == 168)
                || (b[0] == 169 && b[1] == 254));
        }

        if (ip.AddressFamily == AddressFamily.InterNetworkV6)
            return !(ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal || IPAddress.IsLoopback(ip));

        return false;
    }

    [GeneratedRegex("""<meta\s[^>]*(?:property|name)=["']og:image["'][^>]*content=["']([^"']+)["']""", RegexOptions.IgnoreCase)]
    private static partial Regex OgImagePropertyRegex();

    [GeneratedRegex("""<meta\s[^>]*content=["']([^"']+)["'][^>]*(?:property|name)=["']og:image["']""", RegexOptions.IgnoreCase)]
    private static partial Regex OgImageContentRegex();
}
