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
    public void SupportedTickers_Contains19FibrasAndExcludesNext25()
    {
        var source = new EconomaticaDiscoverySource(new HttpClient());

        Assert.Equal(19, source.SupportedTickers.Count);
        Assert.DoesNotContain("NEXT25", source.SupportedTickers);
        Assert.Contains("FHIPO14", source.SupportedTickers);
        Assert.Contains("FUNO11", source.SupportedTickers);
        Assert.Contains("HCITY17", source.SupportedTickers);
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

    private static Fibra BuildFibra(string ticker) => new()
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
        NameVariants = [ticker],
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
}
