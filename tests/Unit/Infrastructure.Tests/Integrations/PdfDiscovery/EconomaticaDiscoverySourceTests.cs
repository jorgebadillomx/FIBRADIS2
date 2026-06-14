using Domain.Catalog;
using Infrastructure.Integrations.PdfDiscovery;
using System.Net;

namespace Infrastructure.Tests.Integrations.PdfDiscovery;

public class EconomaticaDiscoverySourceTests
{
    private static readonly string FixturesPath =
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Fixtures");

    [Fact]
    public async Task DiscoverCandidatesAsync_ReturnsOnlyCandidatesWithIcoPdfClass()
    {
        var html = await File.ReadAllTextAsync(Path.Combine(FixturesPath, "economatica-fhipo-sample.html"));
        var source = BuildSource(html);
        var fibra = BuildFibra("FHIPO14");

        var candidates = await source.DiscoverCandidatesAsync(fibra, CancellationToken.None);

        // 5 ico_pdf links; 1 link without ico_pdf class is excluded
        Assert.Equal(5, candidates.Count);
    }

    [Fact]
    public async Task DiscoverCandidatesAsync_PackageUrlIsFullAbsoluteUrl()
    {
        var html = await File.ReadAllTextAsync(Path.Combine(FixturesPath, "economatica-fhipo-sample.html"));
        var source = BuildSource(html);
        var fibra = BuildFibra("FHIPO14");

        var candidates = await source.DiscoverCandidatesAsync(fibra, CancellationToken.None);

        Assert.All(candidates, c =>
            Assert.StartsWith("http://www.economatica.mx/FHIPO/REPORTES%20TRIMESTRALES/", c.PackageUrl));
    }

    [Fact]
    public async Task DiscoverCandidatesAsync_PeriodParsedCorrectly()
    {
        var html = await File.ReadAllTextAsync(Path.Combine(FixturesPath, "economatica-fhipo-sample.html"));
        var source = BuildSource(html);
        var fibra = BuildFibra("FHIPO14");

        var candidates = await source.DiscoverCandidatesAsync(fibra, CancellationToken.None);

        Assert.Contains(candidates, c => c.Period == "Q4-2025");
        Assert.Contains(candidates, c => c.Period == "Q3-2025");
        Assert.Contains(candidates, c => c.Period == "Q1-2025");
        Assert.Contains(candidates, c => c.Period == "Q1-2014"); // historical data
    }

    [Fact]
    public async Task DiscoverCandidatesAsync_SourceNameIncludesTicker()
    {
        var html = await File.ReadAllTextAsync(Path.Combine(FixturesPath, "economatica-fhipo-sample.html"));
        var source = BuildSource(html);
        var fibra = BuildFibra("FHIPO14");

        var candidates = await source.DiscoverCandidatesAsync(fibra, CancellationToken.None);

        Assert.All(candidates, c => Assert.Equal("economatica:FHIPO14", c.SourceName));
    }

    [Fact]
    public async Task DiscoverCandidatesAsync_UsesBaseTicker_InUrl()
    {
        var html = await File.ReadAllTextAsync(Path.Combine(FixturesPath, "economatica-fhipo-sample.html"));
        var source = BuildSource(html);
        var fibra = BuildFibra("FHIPO14"); // "14" stripped → "FHIPO"

        var candidates = await source.DiscoverCandidatesAsync(fibra, CancellationToken.None);

        // URL must use "FHIPO" (base ticker), not "FHIPO14"
        Assert.All(candidates, c => Assert.Contains("/FHIPO/", c.PackageUrl));
        Assert.All(candidates, c => Assert.DoesNotContain("FHIPO14", c.PackageUrl));
    }

    [Fact]
    public void SupportedTickers_IsEmpty_SourceIsUniversal()
    {
        var source = new EconomaticaDiscoverySource(new HttpClient());

        // Universal source: no whitelist gating — applies to every active fibra.
        Assert.Empty(source.SupportedTickers);
    }

    [Fact]
    public async Task DiscoverCandidatesAsync_BaseTicker_FoundFirst_DoesNotTryAlternatives()
    {
        var html = await File.ReadAllTextAsync(Path.Combine(FixturesPath, "economatica-fhipo-sample.html"));
        // Only the base-ticker URL ("/FVIA/") serves reports.
        var handler = new RoutingHandler(url =>
            url.Contains("/FVIA/", StringComparison.OrdinalIgnoreCase)
                ? (HttpStatusCode.OK, html)
                : (HttpStatusCode.NotFound, ""));
        var source = new EconomaticaDiscoverySource(new HttpClient(handler));
        var fibra = BuildFibra("FVIA16"); // "16" stripped → "FVIA"

        var candidates = await source.DiscoverCandidatesAsync(fibra, CancellationToken.None);

        Assert.NotEmpty(candidates);
        Assert.All(candidates, c => Assert.Contains("/FVIA/", c.PackageUrl));
        // Base form ("FVIA") resolved on the first probe; only that one URL was requested.
        Assert.Single(handler.RequestedUrls);
        Assert.Contains("/FVIA/", handler.RequestedUrls[0]);
    }

    [Fact]
    public async Task DiscoverCandidatesAsync_BaseTicker404_FallsBackToAlternativeForm()
    {
        var html = await File.ReadAllTextAsync(Path.Combine(FixturesPath, "economatica-fhipo-sample.html"));
        // Base form 404s; a later form (the full ticker via name variant) serves reports.
        var handler = new RoutingHandler(url =>
            url.Contains("/FIBRAVIA/", StringComparison.OrdinalIgnoreCase)
                ? (HttpStatusCode.OK, html)
                : (HttpStatusCode.NotFound, ""));
        var source = new EconomaticaDiscoverySource(new HttpClient(handler));
        var fibra = BuildFibra("FVIA16", nameVariants: ["Fibra Vía"]); // variant → "FIBRAVIA"

        var candidates = await source.DiscoverCandidatesAsync(fibra, CancellationToken.None);

        Assert.NotEmpty(candidates);
        Assert.All(candidates, c => Assert.Contains("/FIBRAVIA/", c.PackageUrl));
        // The base form ("/FVIA/") was probed strictly before the alternative ("/FIBRAVIA/").
        var fviaIndex = handler.RequestedUrls.FindIndex(u => u.Contains("/FVIA/"));
        var fibraviaIndex = handler.RequestedUrls.FindIndex(u => u.Contains("/FIBRAVIA/"));
        Assert.True(fviaIndex >= 0, "Base form /FVIA/ should have been probed");
        Assert.True(fibraviaIndex > fviaIndex, "/FIBRAVIA/ must be probed after /FVIA/");
    }

    [Fact]
    public async Task DiscoverCandidatesAsync_HttpTimeout_DoesNotAbort_ReturnsEmpty()
    {
        // HttpClient timeout surfaces as TaskCanceledException (an OperationCanceledException)
        // WITHOUT the caller's token being cancelled. It must be treated as "form failed",
        // not propagated as a cancellation that aborts the whole pipeline run.
        var handler = new ThrowingHandler(() => throw new TaskCanceledException("timeout"));
        var source = new EconomaticaDiscoverySource(new HttpClient(handler));
        var fibra = BuildFibra("FVIA16");

        var candidates = await source.DiscoverCandidatesAsync(fibra, CancellationToken.None);

        Assert.Empty(candidates);
    }

    [Fact]
    public async Task DiscoverCandidatesAsync_CallerCancellation_Propagates()
    {
        var html = await File.ReadAllTextAsync(Path.Combine(FixturesPath, "economatica-fhipo-sample.html"));
        var source = BuildSource(html);
        var fibra = BuildFibra("FHIPO14");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // A genuinely cancelled caller token must surface as OperationCanceledException.
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => source.DiscoverCandidatesAsync(fibra, cts.Token));
    }

    [Fact]
    public async Task DiscoverCandidatesAsync_NoFormResolves_ReturnsEmptyWithoutThrowing()
    {
        // Every URL 404s → fibra not listed on Economatica.
        var handler = new RoutingHandler(_ => (HttpStatusCode.NotFound, ""));
        var source = new EconomaticaDiscoverySource(new HttpClient(handler));
        var fibra = BuildFibra("XXXX99");

        var candidates = await source.DiscoverCandidatesAsync(fibra, CancellationToken.None);

        Assert.Empty(candidates);
    }

    [Theory]
    [InlineData("FHIPO_RT_2025_4T--DIS", "Q4-2025")]
    [InlineData("FUNO_RT_2014_1T--BYH", "Q1-2014")]
    [InlineData("VESTA_RT_2024_3T--ABC", "Q3-2024")]
    [InlineData("DANHOS_RT_2020_2T--XYZ", "Q2-2020")]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("just-a-random-file", null)]
    public void ParseEconomaticaPeriod_ReturnsExpectedPeriod(string? input, string? expected)
    {
        Assert.Equal(expected, EconomaticaDiscoverySource.ParseEconomaticaPeriod(input));
    }

    private static EconomaticaDiscoverySource BuildSource(string html)
    {
        var handler = new FakeHtmlHandler(html);
        return new EconomaticaDiscoverySource(new HttpClient(handler));
    }

    private static Fibra BuildFibra(string ticker, List<string>? nameVariants = null) => new()
    {
        Id = Guid.NewGuid(),
        Ticker = ticker,
        YahooTicker = $"{ticker}.MX",
        FullName = ticker,
        ShortName = ticker,
        Currency = "MXN",
        Market = "BIVA",
        Sector = "Hipotecario",
        State = FibraState.Active,
        NameVariants = nameVariants ?? [ticker],
        ReportsUrl = null,
        CreatedAt = DateTimeOffset.UtcNow,
    };

    private sealed class FakeHtmlHandler(string html) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken ct)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(html, System.Text.Encoding.UTF8, "text/html"),
                RequestMessage = request,
            };
            return Task.FromResult(response);
        }
    }

    // Maps each requested URL to a (status, body) pair and records the URLs requested,
    // so tests can assert which ticker forms were probed and in what order.
    private sealed class RoutingHandler(Func<string, (HttpStatusCode Status, string Body)> route)
        : HttpMessageHandler
    {
        public List<string> RequestedUrls { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken ct)
        {
            var url = request.RequestUri!.ToString();
            RequestedUrls.Add(url);
            var (status, body) = route(url);
            var response = new HttpResponseMessage(status)
            {
                Content = new StringContent(body, System.Text.Encoding.UTF8, "text/html"),
                RequestMessage = request,
            };
            return Task.FromResult(response);
        }
    }

    // Always throws on send — simulates network failure / HttpClient timeout.
    private sealed class ThrowingHandler(Func<Exception> exceptionFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken ct)
            => throw exceptionFactory();
    }
}
