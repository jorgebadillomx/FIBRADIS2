using System.Net;
using System.Net.Http;
using System.Text;
using Application.News;
using Infrastructure.Integrations.GoogleNews;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Tests.Integrations.GoogleNews;

public class GoogleNewsRssClientTests
{
    [Fact]
    public async Task FetchAsync_WhenPubDateIsInvalid_LogsWarningAndFallsBackToUtcNow()
    {
        var logger = new ListLogger<GoogleNewsRssClient>();
        var client = new GoogleNewsRssClient(
            new HttpClient(new StubHttpMessageHandler("""
                <rss><channel><item>
                  <title>FUNO11 anuncia resultados</title>
                  <link>https://example.com/news</link>
                  <description>Resumen</description>
                  <source>Google News</source>
                  <pubDate>fecha-invalida</pubDate>
                </item></channel></rss>
                """)),
            logger);

        var before = DateTimeOffset.UtcNow.AddSeconds(-5);
        var items = await client.FetchAsync("FUNO11 FIBRA");
        var after = DateTimeOffset.UtcNow.AddSeconds(5);

        Assert.Single(items);
        Assert.InRange(items[0].PublishedAt, before, after);
        Assert.Contains(logger.Messages, message => message.Contains("invalid pubDate", StringComparison.Ordinal));
    }

    [Fact]
    public async Task FetchAsync_WhenPubDateIsMissing_LogsWarningAndFallsBackToUtcNow()
    {
        var logger = new ListLogger<GoogleNewsRssClient>();
        var client = new GoogleNewsRssClient(
            new HttpClient(new StubHttpMessageHandler("""
                <rss><channel><item>
                  <title>FUNO11 anuncia resultados</title>
                  <link>https://example.com/news</link>
                  <description>Resumen</description>
                  <source>Google News</source>
                </item></channel></rss>
                """)),
            logger);

        var before = DateTimeOffset.UtcNow.AddSeconds(-5);
        var items = await client.FetchAsync("FUNO11 FIBRA");
        var after = DateTimeOffset.UtcNow.AddSeconds(5);

        Assert.Single(items);
        Assert.InRange(items[0].PublishedAt, before, after);
        Assert.Contains(logger.Messages, message => message.Contains("missing pubDate", StringComparison.Ordinal));
    }
}

internal sealed class StubHttpMessageHandler(string responseContent) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseContent, Encoding.UTF8, "application/xml"),
        });
}

internal sealed class ListLogger<T> : ILogger<T>
{
    public List<string> Messages { get; } = [];

    public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
        => Messages.Add(formatter(state, exception));

    private sealed class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();

        public void Dispose()
        {
        }
    }
}
