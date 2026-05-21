using System.Net;
using System.Net.Http;
using System.Text;
using Infrastructure.Integrations.OgImage;
using Microsoft.Extensions.Logging.Abstractions;

namespace Infrastructure.Tests.Integrations.OgImage;

public class OgImageScraperTests
{
    [Fact]
    public async Task TryGetOgImageAsync_WhenMetaTagExists_ReturnsAbsoluteUrl()
    {
        var handler = new StubOgImageHandler("""
            <html><head>
            <meta property="og:image" content="https://cdn.example.com/article.jpg" />
            </head><body></body></html>
            """);
        var client = new HttpClient(handler);
        var scraper = new OgImageScraper(client, NullLogger<OgImageScraper>.Instance);

        var imageUrl = await scraper.TryGetOgImageAsync("https://example.com/article");

        Assert.Equal("https://cdn.example.com/article.jpg", imageUrl);
        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Equal(0, handler.LastRequest.Headers.Range?.Ranges.Single().From);
        Assert.Equal(16383, handler.LastRequest.Headers.Range?.Ranges.Single().To);
    }

    [Fact]
    public async Task TryGetOgImageAsync_WhenMetaTagIsRelative_ReturnsNull()
    {
        var client = new HttpClient(new StubOgImageHandler("""
            <html><head>
            <meta property="og:image" content="/images/article.jpg" />
            </head><body></body></html>
            """));
        var scraper = new OgImageScraper(client, NullLogger<OgImageScraper>.Instance);

        var imageUrl = await scraper.TryGetOgImageAsync("https://example.com/article");

        Assert.Null(imageUrl);
    }

    [Fact]
    public async Task TryGetOgImageAsync_WhenResponseIsNotSuccessful_ReturnsNull()
    {
        var client = new HttpClient(new StubOgImageHandler(string.Empty, HttpStatusCode.BadGateway));
        var scraper = new OgImageScraper(client, NullLogger<OgImageScraper>.Instance);

        var imageUrl = await scraper.TryGetOgImageAsync("https://example.com/article");

        Assert.Null(imageUrl);
    }

    [Fact]
    public async Task TryGetOgImageAsync_WhenAttributesAreContentFirst_ReturnsUrl()
    {
        // content="..." precede a property="og:image" — orden invertido de atributos
        var client = new HttpClient(new StubOgImageHandler("""
            <html><head>
            <meta content="https://cdn.example.com/alt.jpg" property="og:image" />
            </head><body></body></html>
            """));
        var scraper = new OgImageScraper(client, NullLogger<OgImageScraper>.Instance);

        var imageUrl = await scraper.TryGetOgImageAsync("https://example.com/article");

        Assert.Equal("https://cdn.example.com/alt.jpg", imageUrl);
    }

    [Fact]
    public async Task TryGetOgImageAsync_WhenContentHasHtmlEntities_ReturnsDecodedUrl()
    {
        var client = new HttpClient(new StubOgImageHandler("""
            <html><head>
            <meta property="og:image" content="https://cdn.example.com/img?a=1&amp;b=2" />
            </head><body></body></html>
            """));
        var scraper = new OgImageScraper(client, NullLogger<OgImageScraper>.Instance);

        var imageUrl = await scraper.TryGetOgImageAsync("https://example.com/article");

        Assert.Equal("https://cdn.example.com/img?a=1&b=2", imageUrl);
    }

    [Fact]
    public async Task TryGetOgImageAsync_WhenUrlExceedsMaxLength_ReturnsNull()
    {
        var longPath = new string('a', 2100);
        var client = new HttpClient(new StubOgImageHandler($"""
            <html><head>
            <meta property="og:image" content="https://cdn.example.com/{longPath}" />
            </head><body></body></html>
            """));
        var scraper = new OgImageScraper(client, NullLogger<OgImageScraper>.Instance);

        var imageUrl = await scraper.TryGetOgImageAsync("https://example.com/article");

        Assert.Null(imageUrl);
    }

    [Fact]
    public async Task TryGetOgImageAsync_WhenHttpThrows_ReturnsNull()
    {
        var client = new HttpClient(new ThrowingHandler());
        var scraper = new OgImageScraper(client, NullLogger<OgImageScraper>.Instance);

        var imageUrl = await scraper.TryGetOgImageAsync("https://example.com/article");

        Assert.Null(imageUrl);
    }

    [Fact]
    public async Task TryGetOgImageAsync_WhenCancellationIsRequested_PropagatesOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var client = new HttpClient(new BlockingHandler());
        var scraper = new OgImageScraper(client, NullLogger<OgImageScraper>.Instance);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => scraper.TryGetOgImageAsync("https://example.com/article", cts.Token));
    }

    [Theory]
    [InlineData("http://127.0.0.1/secret")]
    [InlineData("http://10.0.0.1/internal")]
    [InlineData("http://192.168.1.1/router")]
    [InlineData("http://172.16.0.1/private")]
    [InlineData("http://169.254.0.1/link-local")]
    [InlineData("http://[::ffff:127.0.0.1]/secret")]
    public async Task TryGetOgImageAsync_WhenUrlIsPrivateIp_ReturnsNull(string privateUrl)
    {
        var client = new HttpClient(new StubOgImageHandler("""
            <html><head>
            <meta property="og:image" content="https://cdn.example.com/article.jpg" />
            </head></html>
            """));
        var scraper = new OgImageScraper(client, NullLogger<OgImageScraper>.Instance);

        var imageUrl = await scraper.TryGetOgImageAsync(privateUrl);

        Assert.Null(imageUrl);
    }

    [Fact]
    public async Task TryGetOgImageAsync_WhenHostnameResolvesToLoopback_ReturnsNull()
    {
        // localhost always resolves to 127.0.0.1 / ::1 (loopback) — hostname-based SSRF must be blocked
        var client = new HttpClient(new StubOgImageHandler("""
            <html><head>
            <meta property="og:image" content="https://cdn.example.com/article.jpg" />
            </head></html>
            """));
        var scraper = new OgImageScraper(client, NullLogger<OgImageScraper>.Instance);

        var imageUrl = await scraper.TryGetOgImageAsync("http://localhost/secret");

        Assert.Null(imageUrl);
    }

    [Fact]
    public async Task TryGetOgImageAsync_WhenProtocolRelativeUrl_ReturnsHttpsNormalizedUrl()
    {
        var client = new HttpClient(new StubOgImageHandler("""
            <html><head>
            <meta property="og:image" content="//cdn.example.com/article.jpg" />
            </head><body></body></html>
            """));
        var scraper = new OgImageScraper(client, NullLogger<OgImageScraper>.Instance);

        var imageUrl = await scraper.TryGetOgImageAsync("https://example.com/article");

        Assert.Equal("https://cdn.example.com/article.jpg", imageUrl);
    }
}

internal sealed class StubOgImageHandler(string content, HttpStatusCode statusCode = HttpStatusCode.OK) : HttpMessageHandler
{
    public HttpRequestMessage? LastRequest { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        return Task.FromResult(new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(content, Encoding.UTF8, "text/html"),
        });
    }
}

internal sealed class ThrowingHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => throw new HttpRequestException("simulated network failure");
}

internal sealed class BlockingHandler : HttpMessageHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        throw new InvalidOperationException("unreachable");
    }
}
