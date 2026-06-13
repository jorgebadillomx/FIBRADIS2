using System.Net;
using System.Text;
using Application.Integrations;
using Infrastructure.Integrations.Banxico;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Tests.Integrations.Banxico;

public class BanxicoClientTests
{
    [Fact]
    public async Task GetCetes28dAsync_WhenTokenMissing_ReturnsNullAndSkipsHttp()
    {
        var logger = new ListLogger<BanxicoClient>();
        var handler = new ThrowingHandler();
        var client = CreateClient(handler, logger, token: "");

        var result = await client.GetCetes28dAsync();

        Assert.Null(result);
        Assert.Contains(logger.Messages, message => message.Contains("Token", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetCetes28dAsync_WhenTokenIsNull_ReturnsNullAndSkipsHttp()
    {
        var logger = new ListLogger<BanxicoClient>();
        var handler = new ThrowingHandler();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Banxico:Series"] = "SF43936" })
            .Build();
        var client = new BanxicoClient(new HttpClient(handler), configuration, logger);

        var result = await client.GetCetes28dAsync();

        Assert.Null(result);
        Assert.Contains(logger.Messages, message => message.Contains("Token", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetCetes28dAsync_WhenResponseIsValid_ReturnsParsedRateAndSendsTokenHeader()
    {
        var logger = new ListLogger<BanxicoClient>();
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                {"bmx":{"series":[{"datos":[{"dato":"9.50"}]}]}}
                """,
                Encoding.UTF8,
                "application/json"),
        });
        var client = CreateClient(handler, logger);

        var result = await client.GetCetes28dAsync();

        Assert.Equal(9.50m, result);
        Assert.NotNull(handler.LastRequest);
        Assert.Equal("application/json", handler.LastRequest!.Headers.Accept.Single().MediaType);
        Assert.Equal("test-token", handler.LastRequest.Headers.GetValues("Bmx-Token").Single());
    }

    [Fact]
    public async Task GetCetes28dAsync_WhenDatoIsNE_ReturnsNull()
    {
        var logger = new ListLogger<BanxicoClient>();
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                {"bmx":{"series":[{"datos":[{"dato":"N/E"}]}]}}
                """,
                Encoding.UTF8,
                "application/json"),
        });
        var client = CreateClient(handler, logger);

        var result = await client.GetCetes28dAsync();

        Assert.Null(result);
        Assert.Contains(logger.Messages, message => message.Contains("N/E", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetCetes28dAsync_WhenResponseFails_ReturnsNull()
    {
        var logger = new ListLogger<BanxicoClient>();
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest));
        var client = CreateClient(handler, logger);

        var result = await client.GetCetes28dAsync();

        Assert.Null(result);
        Assert.Contains(logger.Messages, message => message.Contains("no exitosa", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetTiie28dAsync_WhenTokenMissing_ReturnsNullAndSkipsHttp()
    {
        var logger = new ListLogger<BanxicoClient>();
        var handler = new ThrowingHandler();
        var client = CreateClient(handler, logger, token: "");

        var result = await client.GetTiie28dAsync();

        Assert.Null(result);
        Assert.Contains(logger.Messages, message => message.Contains("Token", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetTiie28dAsync_WhenValidResponse_ParsesCorrectly()
    {
        var logger = new ListLogger<BanxicoClient>();
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                {"bmx":{"series":[{"datos":[{"dato":"9.25"}]}]}}
                """,
                Encoding.UTF8,
                "application/json"),
        });
        var client = CreateClient(handler, logger);

        var result = await client.GetTiie28dAsync();

        Assert.Equal(9.25m, result);
        Assert.NotNull(handler.LastRequest);
        Assert.Contains("SF43783", handler.LastRequest!.RequestUri!.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal("test-token", handler.LastRequest.Headers.GetValues("Bmx-Token").Single());
    }

    [Fact]
    public async Task GetInpcHistoryAsync_WhenTokenMissing_ReturnsEmptyListAndSkipsHttp()
    {
        var logger = new ListLogger<BanxicoClient>();
        var handler = new ThrowingHandler();
        var client = CreateClient(handler, logger, token: "");

        var result = await client.GetInpcHistoryAsync(new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31));

        Assert.Empty(result);
        Assert.Contains(logger.Messages, message => message.Contains("Token", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetInpcHistoryAsync_WhenValidRange_ReturnsParsedEntries()
    {
        var logger = new ListLogger<BanxicoClient>();
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                {"bmx":{"series":[{"datos":[
                  {"fecha":"30/04/2024","dato":"134.1258"},
                  {"fecha":"31/05/2024","dato":"135.5190"}
                ]}]}}
                """,
                Encoding.UTF8,
                "application/json"),
        });
        var client = CreateClient(handler, logger);

        var result = await client.GetInpcHistoryAsync(new DateOnly(2024, 4, 1), new DateOnly(2024, 5, 31));

        Assert.Equal(2, result.Count);
        Assert.Equal((new DateOnly(2024, 4, 1), 134.1258m), result[0]);
        Assert.Equal((new DateOnly(2024, 5, 1), 135.5190m), result[1]);
        Assert.NotNull(handler.LastRequest);
        Assert.Contains("/series/SP1/datos/2024-04-01/2024-05-31", handler.LastRequest!.RequestUri!.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal("test-token", handler.LastRequest.Headers.GetValues("Bmx-Token").Single());
    }

    [Fact]
    public async Task GetInpcHistoryAsync_WhenDatoIsNE_ExcludesEntry()
    {
        var logger = new ListLogger<BanxicoClient>();
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """
                {"bmx":{"series":[{"datos":[
                  {"fecha":"30/04/2024","dato":"N/E"},
                  {"fecha":"31/05/2024","dato":"135.5190"}
                ]}]}}
                """,
                Encoding.UTF8,
                "application/json"),
        });
        var client = CreateClient(handler, logger);

        var result = await client.GetInpcHistoryAsync(new DateOnly(2024, 4, 1), new DateOnly(2024, 5, 31));

        Assert.Single(result);
        Assert.Equal((new DateOnly(2024, 5, 1), 135.5190m), result[0]);
    }

    [Fact]
    public async Task GetInpcHistoryAsync_WhenHttpFails_ReturnsEmptyList()
    {
        var logger = new ListLogger<BanxicoClient>();
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest));
        var client = CreateClient(handler, logger);

        var result = await client.GetInpcHistoryAsync(new DateOnly(2024, 4, 1), new DateOnly(2024, 5, 31));

        Assert.Empty(result);
        Assert.Contains(logger.Messages, message => message.Contains("no exitosa", StringComparison.OrdinalIgnoreCase));
    }

    private static BanxicoClient CreateClient(
        HttpMessageHandler handler,
        ILogger<BanxicoClient> logger,
        string token = "test-token",
        string series = "SF43936")
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Banxico:Token"] = token,
                ["Banxico:Series"] = series,
            })
            .Build();

        return new BanxicoClient(new HttpClient(handler), configuration, logger);
    }

    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(responseFactory(request));
        }
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => throw new InvalidOperationException("HTTP should not be called when the token is missing.");
    }
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
        public void Dispose() { }
    }
}
