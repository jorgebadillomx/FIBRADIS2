using System.Net.Http.Json;
using Application.Seo;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Seo;

public class IndexNowService(
    HttpClient http,
    IConfiguration config,
    ILogger<IndexNowService> logger) : IIndexNowService
{
    public async Task PingAsync(IEnumerable<string> urls, CancellationToken ct = default)
    {
        try
        {
            var key = config["Seo:IndexNowKey"];
            if (string.IsNullOrWhiteSpace(key))
            {
                logger.LogDebug("IndexNow key not configured; skipping ping.");
                return;
            }

            var baseUrl = config["App:BaseUrl"]?.TrimEnd('/') ?? string.Empty;
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                logger.LogWarning("IndexNow: App:BaseUrl not configured; skipping ping.");
                return;
            }

            var host = new Uri(baseUrl).Host;
            var urlList = urls.ToList();
            if (urlList.Count == 0) return;

            var payload = new
            {
                host,
                key,
                keyLocation = $"{baseUrl}/indexnow.txt",
                urlList,
            };

            var response = await http.PostAsJsonAsync("https://api.indexnow.org/indexnow", payload, ct);
            if (!response.IsSuccessStatusCode)
                logger.LogWarning("IndexNow ping returned {Status} for {Count} URL(s).", response.StatusCode, urlList.Count);
            else
                logger.LogDebug("IndexNow ping accepted {Count} URL(s).", urlList.Count);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "IndexNow ping failed unexpectedly.");
        }
    }
}
