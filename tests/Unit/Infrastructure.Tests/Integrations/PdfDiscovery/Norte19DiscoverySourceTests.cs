using Domain.Catalog;
using Infrastructure.Integrations.PdfDiscovery;
using System.Net;

namespace Infrastructure.Tests.Integrations.PdfDiscovery;

public class Norte19DiscoverySourceTests
{
    private static readonly string FixturesPath =
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Fixtures");

    [Fact]
    public async Task DiscoverCandidatesAsync_ReturnsCandidatesForQuartersWithPdf()
    {
        var json = await File.ReadAllTextAsync(Path.Combine(FixturesPath, "norte19-api-sample.json"));
        var fibra = BuildFibra("HCITY17");
        var source = BuildSource(json);

        var candidates = await source.DiscoverCandidatesAsync(fibra, CancellationToken.None);

        // Q1-2026 and Q4-2025 have pdf; Q3-2025 has null pdf → 2 candidates
        Assert.Equal(2, candidates.Count);
    }

    [Fact]
    public async Task DiscoverCandidatesAsync_PackageUrlUsesStableHash()
    {
        var json = await File.ReadAllTextAsync(Path.Combine(FixturesPath, "norte19-api-sample.json"));
        var fibra = BuildFibra("HCITY17");
        var source = BuildSource(json);

        var candidates = await source.DiscoverCandidatesAsync(fibra, CancellationToken.None);

        Assert.Contains(candidates, c => c.PackageUrl == "norte19:N19_Resultados_1_T26_vf_9a3511cd96");
        Assert.Contains(candidates, c => c.PackageUrl == "norte19:N19_Resultados_4_T25_ESP_vf_abcd1234ef");
    }

    [Fact]
    public async Task DiscoverCandidatesAsync_DownloadUrlIsPresignedUrl()
    {
        var json = await File.ReadAllTextAsync(Path.Combine(FixturesPath, "norte19-api-sample.json"));
        var fibra = BuildFibra("HCITY17");
        var source = BuildSource(json);

        var candidates = await source.DiscoverCandidatesAsync(fibra, CancellationToken.None);

        Assert.All(candidates, c =>
        {
            Assert.NotNull(c.DownloadUrl);
            Assert.Contains("X-Amz-Expires", c.DownloadUrl, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task DiscoverCandidatesAsync_PeriodParsedCorrectly()
    {
        var json = await File.ReadAllTextAsync(Path.Combine(FixturesPath, "norte19-api-sample.json"));
        var fibra = BuildFibra("HCITY17");
        var source = BuildSource(json);

        var candidates = await source.DiscoverCandidatesAsync(fibra, CancellationToken.None);

        Assert.Contains(candidates, c => c.Period == "Q1-2026");
        Assert.Contains(candidates, c => c.Period == "Q4-2025");
    }

    [Fact]
    public async Task DiscoverCandidatesAsync_QuarterWithNullPdf_IsSkipped()
    {
        var json = await File.ReadAllTextAsync(Path.Combine(FixturesPath, "norte19-api-sample.json"));
        var fibra = BuildFibra("HCITY17");
        var source = BuildSource(json);

        var candidates = await source.DiscoverCandidatesAsync(fibra, CancellationToken.None);

        // Q3-2025 has null pdf; its hash must not appear
        Assert.DoesNotContain(candidates, c => c.Period == "Q3-2025");
    }

    [Fact]
    public async Task DiscoverCandidatesAsync_SourceNameIncludesTicker()
    {
        var json = await File.ReadAllTextAsync(Path.Combine(FixturesPath, "norte19-api-sample.json"));
        var fibra = BuildFibra("HCITY17");
        var source = BuildSource(json);

        var candidates = await source.DiscoverCandidatesAsync(fibra, CancellationToken.None);

        Assert.All(candidates, c => Assert.Equal("official:HCITY17", c.SourceName));
    }

    [Fact]
    public void SupportedTickers_ContainsOnlyHcity17()
    {
        var source = new Norte19DiscoverySource(new HttpClient());
        Assert.Equal(["HCITY17"], source.SupportedTickers);
    }

    [Theory]
    [InlineData("1T26", "Q1-2026")]
    [InlineData("4T25", "Q4-2025")]
    [InlineData("2T24", "Q2-2024")]
    [InlineData("3T18", "Q3-2018")]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("5T26", null)]
    [InlineData("1X26", null)]
    public void ParseNorte19Quarter_ReturnsExpectedPeriod(string? input, string? expected)
    {
        Assert.Equal(expected, Norte19DiscoverySource.ParseNorte19Quarter(input));
    }

    private static Norte19DiscoverySource BuildSource(string json)
    {
        var handler = new FakeJsonHandler(json);
        return new Norte19DiscoverySource(new HttpClient(handler));
    }

    private static Fibra BuildFibra(string ticker) => new()
    {
        Id = Guid.NewGuid(),
        Ticker = ticker,
        YahooTicker = $"{ticker}.MX",
        FullName = ticker,
        ShortName = ticker,
        Currency = "MXN",
        Market = "BMV",
        Sector = "Hotelero",
        State = FibraState.Active,
        NameVariants = [ticker],
        ReportsUrl = "https://www.norte19.com/investors",
        CreatedAt = DateTimeOffset.UtcNow,
    };

    private sealed class FakeJsonHandler(string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken ct)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"),
                RequestMessage = request,
            };
            return Task.FromResult(response);
        }
    }
}
