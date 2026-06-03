using Domain.Catalog;
using Infrastructure.Integrations.PdfDiscovery;
using System.Net;

namespace Infrastructure.Tests.Integrations.PdfDiscovery;

public class OfficialSiteDiscoverySourceTests
{
    private static readonly string FixturesPath =
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Fixtures");

    [Fact]
    public async Task DiscoverCandidatesAsync_Fibramq_ReturnsOnlySpanishPdfs()
    {
        var html = await File.ReadAllTextAsync(Path.Combine(FixturesPath, "fibramq-investors-sample.html"));
        var fibra = BuildFibra("FIBRAMQ12", "https://www.fibramacquarie.com/es/inversionistas.html");
        var source = BuildSource(html, fibra.ReportsUrl!);

        var candidates = await source.DiscoverCandidatesAsync(fibra, CancellationToken.None);

        // Only Spanish PDFs (-spa.pdf suffix) should be returned
        Assert.All(candidates, c => Assert.Contains("-spa.pdf", c.PackageUrl, StringComparison.OrdinalIgnoreCase));
        Assert.Equal(3, candidates.Count); // 1Q26-spa, 4Q25-spa, 3Q25-spa
    }

    [Fact]
    public async Task DiscoverCandidatesAsync_Fibramq_ParsesPeriodFromFilename()
    {
        var html = await File.ReadAllTextAsync(Path.Combine(FixturesPath, "fibramq-investors-sample.html"));
        var fibra = BuildFibra("FIBRAMQ12", "https://www.fibramacquarie.com/es/inversionistas.html");
        var source = BuildSource(html, fibra.ReportsUrl!);

        var candidates = await source.DiscoverCandidatesAsync(fibra, CancellationToken.None);

        Assert.Contains(candidates, c => c.Period == "Q1-2026");
        Assert.Contains(candidates, c => c.Period == "Q4-2025");
        Assert.Contains(candidates, c => c.Period == "Q3-2025");
    }

    [Fact]
    public async Task DiscoverCandidatesAsync_WhenPageHasNoPdfs_ReturnsEmpty()
    {
        const string emptyHtml = "<html><body><p>Sin documentos</p></body></html>";
        var fibra = BuildFibra("FIBRAMQ12", "https://www.fibramacquarie.com/es/inversionistas.html");
        var source = BuildSource(emptyHtml, fibra.ReportsUrl!);

        var candidates = await source.DiscoverCandidatesAsync(fibra, CancellationToken.None);

        Assert.Empty(candidates);
    }

    [Fact]
    public async Task DiscoverCandidatesAsync_Fhipo_ExtractsPdfsFromWordPress()
    {
        var html = await File.ReadAllTextAsync(Path.Combine(FixturesPath, "fhipo-reportes-sample.html"));
        var fibra = BuildFibra("FHIPO14", "https://fhipo.com/es/reportes-trimestrales/");
        var source = BuildSource(html, fibra.ReportsUrl!);

        var candidates = await source.DiscoverCandidatesAsync(fibra, CancellationToken.None);

        Assert.Equal(3, candidates.Count);
        Assert.All(candidates, c => Assert.Equal("quarterly", c.ReportType));
    }

    [Fact]
    public async Task DiscoverCandidatesAsync_UnsupportedTicker_ReturnsEmpty()
    {
        var html = "<html><body></body></html>";
        var fibra = BuildFibra("FUNO11_NOT_REGISTERED", "https://fibra.uno/investors");
        var source = BuildSource(html, fibra.ReportsUrl!);

        var candidates = await source.DiscoverCandidatesAsync(fibra, CancellationToken.None);

        Assert.Empty(candidates);
    }

    private static OfficialSiteDiscoverySource BuildSource(string html, string pageUrl = "")
    {
        var handler = new FakeHtmlHandler(html);
        var client = new HttpClient(handler);
        return new OfficialSiteDiscoverySource(client);
    }

    private static Fibra BuildFibra(string ticker, string reportsUrl) => new()
    {
        Id = Guid.NewGuid(),
        Ticker = ticker,
        YahooTicker = $"{ticker}.MX",
        FullName = ticker,
        ShortName = ticker,
        Currency = "MXN",
        Market = "BMV",
        Sector = "Industrial",
        State = FibraState.Active,
        NameVariants = [ticker],
        ReportsUrl = reportsUrl,
        CreatedAt = DateTimeOffset.UtcNow,
    };

    private sealed class FakeHtmlHandler(string html) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
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
