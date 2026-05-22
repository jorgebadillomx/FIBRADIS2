using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Diagnostics;
using Application.News;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Integrations.GoogleNews;

public partial class GoogleNewsUrlDecoder(HttpClient http, ILogger<GoogleNewsUrlDecoder> logger) : IGoogleNewsUrlDecoder
{
    public async Task<string?> TryDecodeAsync(string googleNewsUrl, CancellationToken ct = default)
    {
        try
        {
            if (!TryGetArticleToken(googleNewsUrl, out var articleToken))
                return null;

            var articlePageUrl = BuildMetadataPageUrl(articleToken);
            var html = await FetchArticlePageAsync(articlePageUrl, ct);
            if (string.IsNullOrWhiteSpace(html))
                return null;

            var signature = SignatureRegex().Match(html).Groups[1].Value;
            var timestamp = TimestampRegex().Match(html).Groups[1].Value;
            if (string.IsNullOrWhiteSpace(signature) || string.IsNullOrWhiteSpace(timestamp))
            {
                logger.LogDebug(
                    "Google News decoder could not find signature/timestamp for '{Url}'. HtmlLength: {HtmlLength}",
                    googleNewsUrl,
                    html.Length);
                return null;
            }

            var payload = BuildBatchExecutePayload(articleToken, timestamp, signature);
            using var content = new StringContent(payload, Encoding.UTF8, "application/x-www-form-urlencoded");
            using var response = await http.PostAsync("https://news.google.com/_/DotsSplashUi/data/batchexecute", content, ct);
            if (!response.IsSuccessStatusCode)
                return null;

            var body = await response.Content.ReadAsStringAsync(ct);
            var match = BatchResultRegex().Match(body);
            if (!match.Success)
            {
                logger.LogDebug(
                    "Google News decoder could not parse batchexecute payload for '{Url}'. BodyLength: {BodyLength}",
                    googleNewsUrl,
                    body.Length);
                return null;
            }

            var decodedUrl = match.Groups[1].Value;

            return Uri.TryCreate(decodedUrl, UriKind.Absolute, out var resolved)
                && (resolved.Scheme == Uri.UriSchemeHttp || resolved.Scheme == Uri.UriSchemeHttps)
                ? decodedUrl
                : null;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Google News URL decode failed for '{Url}'", googleNewsUrl);
            return null;
        }
    }

    private static bool TryGetArticleToken(string url, out string token)
    {
        token = string.Empty;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var parsed))
            return false;

        if (!string.Equals(parsed.Host, "news.google.com", StringComparison.OrdinalIgnoreCase))
            return false;

        var segments = parsed.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 2)
            return false;

        var bucket = segments[^2];
        if (!string.Equals(bucket, "articles", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(bucket, "rss", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        token = segments[^1];
        return !string.IsNullOrWhiteSpace(token);
    }

    private static string BuildMetadataPageUrl(string token)
        => $"https://news.google.com/rss/articles/{token}?oc=5";

    private async Task<string?> FetchArticlePageAsync(string articlePageUrl, CancellationToken ct)
    {
        try
        {
            return await http.GetStringAsync(articlePageUrl, ct);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            logger.LogWarning(ex, "Google News article page returned 429 for '{Url}'. Trying curl fallback.", articlePageUrl);
            return await TryFetchWithCurlAsync(articlePageUrl, ct);
        }
    }

    private async Task<string?> TryFetchWithCurlAsync(string articlePageUrl, CancellationToken ct)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "curl.exe",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            psi.ArgumentList.Add("-sS");
            psi.ArgumentList.Add("-L");
            psi.ArgumentList.Add("-H");
            psi.ArgumentList.Add("User-Agent: Mozilla/5.0");
            psi.ArgumentList.Add("-H");
            psi.ArgumentList.Add("Accept-Language: es-MX,es;q=0.9,en;q=0.8");
            psi.ArgumentList.Add("-H");
            psi.ArgumentList.Add("Accept: text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            psi.ArgumentList.Add(articlePageUrl);

            using var process = new Process { StartInfo = psi };
            process.Start();

            var stdoutTask = process.StandardOutput.ReadToEndAsync(ct);
            var stderrTask = process.StandardError.ReadToEndAsync(ct);
            await process.WaitForExitAsync(ct);

            var stdout = await stdoutTask;
            var stderr = await stderrTask;

            if (process.ExitCode != 0)
            {
                logger.LogWarning("curl fallback failed for '{Url}'. ExitCode: {ExitCode}. Stderr: {Stderr}", articlePageUrl, process.ExitCode, stderr);
                return null;
            }

            if (string.IsNullOrWhiteSpace(stdout))
            {
                logger.LogDebug("curl fallback returned empty output for '{Url}'. Stderr: {Stderr}", articlePageUrl, stderr);
                return null;
            }

            return string.IsNullOrWhiteSpace(stdout) ? null : stdout;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "curl fallback threw for '{Url}'", articlePageUrl);
            return null;
        }
    }

    private static string BuildBatchExecutePayload(string token, string timestamp, string signature)
    {
        var inner = $"[\"garturlreq\",[[\"X\",\"X\",[\"X\",\"X\"],null,null,1,1,\"MX:es-419\",null,1,null,null,null,null,null,0,1],\"X\",\"X\",1,[1,1,1],1,1,null,0,0,null,0],\"{token}\",{timestamp},\"{signature}\"]";
        var wrapper = JsonSerializer.Serialize(new object[][]
        {
            [
                "Fbv4je",
                inner,
            ],
        });

        return $"f.req={Uri.EscapeDataString($"[{wrapper}]")}";
    }

    [GeneratedRegex("data-n-a-sg=\"([^\"]+)\"", RegexOptions.IgnoreCase)]
    private static partial Regex SignatureRegex();

    [GeneratedRegex("data-n-a-ts=\"([^\"]+)\"", RegexOptions.IgnoreCase)]
    private static partial Regex TimestampRegex();

    [GeneratedRegex("\\\\\"garturlres\\\\\",\\\\\"([^\\\\\"]+)\\\\\"", RegexOptions.Singleline)]
    private static partial Regex BatchResultRegex();
}
