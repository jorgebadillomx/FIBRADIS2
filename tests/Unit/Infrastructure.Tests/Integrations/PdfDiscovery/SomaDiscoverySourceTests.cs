using Domain.Catalog;
using Infrastructure.Integrations.PdfDiscovery;
using System.Net;

namespace Infrastructure.Tests.Integrations.PdfDiscovery;

public class SomaDiscoverySourceTests
{
    private static readonly string FixturesPath =
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Fixtures");

    [Fact]
    public async Task DiscoverCandidatesAsync_ReturnsOnlyQuarterlyReports()
    {
        var json = await File.ReadAllTextAsync(Path.Combine(FixturesPath, "soma-documents-api-sample.json"));
        var source = BuildSource(json);
        var fibra = BuildSoma21Fibra();

        var candidates = await source.DiscoverCandidatesAsync(fibra, CancellationToken.None);

        Assert.All(candidates, c => Assert.Equal("quarterly", c.ReportType));
    }

    [Fact]
    public async Task DiscoverCandidatesAsync_ExcludesAnnualAndSustainability()
    {
        var json = await File.ReadAllTextAsync(Path.Combine(FixturesPath, "soma-documents-api-sample.json"));
        var source = BuildSource(json);
        var fibra = BuildSoma21Fibra();

        var candidates = await source.DiscoverCandidatesAsync(fibra, CancellationToken.None);

        Assert.DoesNotContain(candidates, c => c.SourceTitle.Contains("Annual", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(candidates, c => c.SourceTitle.Contains("Sustainability", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task DiscoverCandidatesAsync_ParsesPeriodFromFilename()
    {
        var json = await File.ReadAllTextAsync(Path.Combine(FixturesPath, "soma-documents-api-sample.json"));
        var source = BuildSource(json);
        var fibra = BuildSoma21Fibra();

        var candidates = await source.DiscoverCandidatesAsync(fibra, CancellationToken.None);

        Assert.Contains(candidates, c => c.Period == "Q1-2026");
        Assert.Contains(candidates, c => c.Period == "Q4-2025");
        Assert.Contains(candidates, c => c.Period == "Q3-2025");
    }

    [Fact]
    public async Task DiscoverCandidatesAsync_PdfUrlIsDirectDownload()
    {
        var json = await File.ReadAllTextAsync(Path.Combine(FixturesPath, "soma-documents-api-sample.json"));
        var source = BuildSource(json);
        var fibra = BuildSoma21Fibra();

        var candidates = await source.DiscoverCandidatesAsync(fibra, CancellationToken.None);

        Assert.All(candidates, c =>
        {
            Assert.Equal(c.PackageUrl, c.DownloadUrl);
            Assert.EndsWith(".pdf", c.PackageUrl, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task DiscoverCandidatesAsync_PublishedAtParsedFromDate()
    {
        var json = await File.ReadAllTextAsync(Path.Combine(FixturesPath, "soma-documents-api-sample.json"));
        var source = BuildSource(json);
        var fibra = BuildSoma21Fibra();

        var candidates = await source.DiscoverCandidatesAsync(fibra, CancellationToken.None);

        Assert.All(candidates, c => Assert.NotNull(c.PublishedAt));
    }

    private static SomaDiscoverySource BuildSource(string json)
    {
        var handler = new FakeJsonHandler(json);
        return new SomaDiscoverySource(new HttpClient(handler));
    }

    private static Fibra BuildSoma21Fibra() => new()
    {
        Id = Guid.NewGuid(),
        Ticker = "SOMA21",
        YahooTicker = "SOMA21.MX",
        FullName = "Fibra SOMA",
        ShortName = "Fibra SOMA",
        Currency = "MXN",
        Market = "BIVA",
        Sector = "Comercial",
        State = FibraState.Active,
        NameVariants = ["SOMA21", "Fibra SOMA"],
        CreatedAt = DateTimeOffset.UtcNow,
    };

    private sealed class FakeJsonHandler(string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
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
