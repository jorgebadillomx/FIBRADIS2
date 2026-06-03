using Domain.Catalog;
using Infrastructure.Integrations.PdfDiscovery;
using System.Net;

namespace Infrastructure.Tests.Integrations.PdfDiscovery;

public class BmvDiscoverySourceTests
{
    private static readonly string FixturesPath =
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Fixtures");

    [Fact]
    public async Task DiscoverCandidatesAsync_Hcity_ExtractsBmvPdfLinks()
    {
        var html = await File.ReadAllTextAsync(Path.Combine(FixturesPath, "bmv-hcity-sample.html"));
        var fibra = BuildFibra("HCITY17", "https://www.bmv.com.mx/es/emisoras/informacionfinanciera/HCITY-31249-CGEN_CAPIT");
        var source = BuildSource(html);

        var candidates = await source.DiscoverCandidatesAsync(fibra, CancellationToken.None);

        Assert.Equal(2, candidates.Count);
        Assert.All(candidates, c => Assert.Contains("indrpfn_", c.PackageUrl, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task DiscoverCandidatesAsync_ConvertsBmvPeriodFormat()
    {
        var html = await File.ReadAllTextAsync(Path.Combine(FixturesPath, "bmv-hcity-sample.html"));
        var fibra = BuildFibra("HCITY17", "https://www.bmv.com.mx/es/emisoras/informacionfinanciera/HCITY-31249-CGEN_CAPIT");
        var source = BuildSource(html);

        var candidates = await source.DiscoverCandidatesAsync(fibra, CancellationToken.None);

        Assert.Contains(candidates, c => c.Period == "Q1-2026"); // 2026-01
        Assert.Contains(candidates, c => c.Period == "Q4-2025"); // 2025-04
    }

    [Fact]
    public async Task DiscoverCandidatesAsync_NonIndrpfnLinks_AreExcluded()
    {
        var html = await File.ReadAllTextAsync(Path.Combine(FixturesPath, "bmv-hcity-sample.html"));
        var fibra = BuildFibra("HCITY17", "https://www.bmv.com.mx/es/emisoras/informacionfinanciera/HCITY-31249-CGEN_CAPIT");
        var source = BuildSource(html);

        var candidates = await source.DiscoverCandidatesAsync(fibra, CancellationToken.None);

        // "otro-documento.pdf" in fixture (non-indrpfn) should not be included
        Assert.DoesNotContain(candidates, c => c.PackageUrl.Contains("otro-documento"));
    }

    [Fact]
    public async Task SupportedTickers_ContainsOnlyHcity17()
    {
        var source = new BmvDiscoverySource(new HttpClient());
        Assert.Equal(["HCITY17"], source.SupportedTickers);
    }

    [Fact]
    public async Task DiscoverCandidatesAsync_WhenReportsUrlIsNull_ReturnsEmpty()
    {
        var fibra = BuildFibra("HCITY17", null);
        var source = BuildSource("<html></html>");

        var candidates = await source.DiscoverCandidatesAsync(fibra, CancellationToken.None);

        Assert.Empty(candidates);
    }

    private static BmvDiscoverySource BuildSource(string html)
    {
        var handler = new FakeHtmlHandler(html);
        return new BmvDiscoverySource(new HttpClient(handler));
    }

    private static Fibra BuildFibra(string ticker, string? reportsUrl) => new()
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
