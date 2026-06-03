using Application.Fundamentals;
using Domain.Catalog;
using Infrastructure.Integrations.PdfDiscovery;

namespace Infrastructure.Tests.Integrations.PdfDiscovery;

public class AmefibraDiscoverySourceTests
{
    [Fact]
    public async Task DiscoverCandidatesAsync_CachesListings_ClientCalledOnceForMultipleFibras()
    {
        var trackingClient = new CountingAmefibraClient([
            new AmefibraListingItem("2022 Reporte T4 FUNO", "https://amefibra.com/funo-q4-2022/", null),
            new AmefibraListingItem("2022 Reporte T4 Fibra Inn", "https://amefibra.com/finn-q4-2022/", null),
        ]);
        var source = new AmefibraDiscoverySource(trackingClient);

        var funo = BuildFibra("FUNO11", ["Fibra Uno", "FUNO"]);
        var finn = BuildFibra("FINN13", ["Fibra Inn", "FINN"]);

        await source.DiscoverCandidatesAsync(funo, CancellationToken.None);
        await source.DiscoverCandidatesAsync(finn, CancellationToken.None);

        Assert.Equal(1, trackingClient.GetListingsCallCount);
    }

    [Fact]
    public async Task DiscoverCandidatesAsync_FibraMatchedByNameVariant_ReturnsCandidates()
    {
        var client = new CountingAmefibraClient([
            new AmefibraListingItem("2022 Reporte T4 FUNO", "https://amefibra.com/funo-q4-2022/", null),
        ]);
        var source = new AmefibraDiscoverySource(client);
        var fibra = BuildFibra("FUNO11", ["Fibra Uno", "FUNO"]);

        var candidates = await source.DiscoverCandidatesAsync(fibra, CancellationToken.None);

        Assert.NotEmpty(candidates);
        Assert.Equal("Q4-2022", candidates[0].Period);
        Assert.Equal("AMEFIBRA", candidates[0].SourceName);
    }

    [Fact]
    public async Task DiscoverCandidatesAsync_FibraNotInAmefibra_ReturnsEmpty()
    {
        var client = new CountingAmefibraClient([
            new AmefibraListingItem("2022 Reporte T4 FUNO", "https://amefibra.com/funo-q4-2022/", null),
        ]);
        var source = new AmefibraDiscoverySource(client);

        // SOMA21 is not in Amefibra listings — its title won't match any SOMA candidate
        var soma = BuildFibra("SOMA21", ["Fibra SOMA", "SOMA"]);

        var candidates = await source.DiscoverCandidatesAsync(soma, CancellationToken.None);

        Assert.Empty(candidates);
    }

    [Fact]
    public async Task SupportedTickers_IsEmpty_MeaningAllFibrasAreSupported()
    {
        var source = new AmefibraDiscoverySource(new CountingAmefibraClient([]));
        Assert.Empty(source.SupportedTickers);
    }

    private static Fibra BuildFibra(string ticker, List<string> nameVariants) => new()
    {
        Id = Guid.NewGuid(),
        Ticker = ticker,
        YahooTicker = $"{ticker}.MX",
        FullName = nameVariants.First(),
        ShortName = ticker,
        Currency = "MXN",
        Market = "BMV",
        Sector = "Diversificado",
        State = FibraState.Active,
        NameVariants = nameVariants,
        CreatedAt = DateTimeOffset.UtcNow,
    };
}

internal sealed class CountingAmefibraClient(IReadOnlyList<AmefibraListingItem> listings) : IAmefibraDiscoveryClient
{
    public int GetListingsCallCount { get; private set; }

    public Task<IReadOnlyList<AmefibraListingItem>> GetListingItemsAsync(CancellationToken ct)
    {
        GetListingsCallCount++;
        return Task.FromResult(listings);
    }

    public Task<AmefibraPackageDetails> GetPackageDetailsAsync(string packageUrl, CancellationToken ct)
        => Task.FromResult(new AmefibraPackageDetails(packageUrl, null, null));

    public Task<(byte[] Content, string? PdfUrl, string? FileName)> DownloadPdfAsync(string packageUrl, string downloadUrl, CancellationToken ct)
        => Task.FromResult<(byte[], string?, string?)>(([], null, null));
}
