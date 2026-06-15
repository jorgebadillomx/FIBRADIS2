using System.Buffers.Binary;
using Application.Seo;
using Domain.Catalog;
using Domain.Market;
using Infrastructure.Seo;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

namespace Infrastructure.Tests.Seo;

public sealed class OgImageRendererTests
{
    [Fact]
    public async Task RenderFibraCardAsync_WithLiveData_ReturnsPng1200x630()
    {
        var renderer = CreateRenderer();
        var fibra = CreateFibra();
        var marketData = CreateMarketData(fibra);

        var bytes = await renderer.RenderFibraCardAsync(fibra, marketData);
        var fallback = await File.ReadAllBytesAsync(GetFallbackPath());

        Assert.True(IsPng(bytes));
        Assert.Equal((1200, 630), GetPngDimensions(bytes));
        Assert.False(bytes.SequenceEqual(fallback));
        Assert.NotEmpty(bytes);
    }

    [Fact]
    public async Task RenderFibraCardAsync_WithoutFibra_ReturnsFallbackAsset()
    {
        var renderer = CreateRenderer();
        var bytes = await renderer.RenderFibraCardAsync(null, null);
        var fallback = await File.ReadAllBytesAsync(GetFallbackPath());

        Assert.True(IsPng(bytes));
        Assert.True(bytes.SequenceEqual(fallback));
    }

    [Fact]
    public async Task RenderFibraCardAsync_WithoutPrice_ReturnsFallbackAsset()
    {
        var renderer = CreateRenderer();
        var fibra = CreateFibra();
        var marketData = new FibraSeoMarketData(
            new PriceSnapshot
            {
                FibraId = fibra.Id,
                Ticker = fibra.Ticker,
                LastPrice = null,
                CapturedAt = DateTimeOffset.UtcNow,
                Status = MarketDataStatus.Processed,
            },
            [],
            null,
            DateOnly.FromDateTime(DateTime.UtcNow));

        var bytes = await renderer.RenderFibraCardAsync(fibra, marketData);
        var fallback = await File.ReadAllBytesAsync(GetFallbackPath());

        Assert.True(IsPng(bytes));
        Assert.True(bytes.SequenceEqual(fallback));
    }

    private static OgImageRenderer CreateRenderer()
        => new(new FakeWebHostEnvironment
        {
            WebRootPath = GetApiWebRootPath(),
            WebRootFileProvider = new PhysicalFileProvider(GetApiWebRootPath()),
        });

    private static Fibra CreateFibra()
        => new()
        {
            Id = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000001"),
            Ticker = "FUNO11",
            FullName = "Fibra Uno",
            ShortName = "Fibra Uno",
            Sector = "Industrial",
            Market = "BMV",
            Currency = "MXN",
            State = FibraState.Active,
            CreatedAt = DateTimeOffset.UtcNow,
        };

    private static FibraSeoMarketData CreateMarketData(Fibra fibra)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return new FibraSeoMarketData(
            new PriceSnapshot
            {
                FibraId = fibra.Id,
                Ticker = fibra.Ticker,
                LastPrice = 21.50m,
                Week52High = 28.10m,
                Week52Low = 20.80m,
                CapturedAt = new DateTimeOffset(2026, 6, 13, 11, 30, 0, TimeSpan.Zero),
                Status = MarketDataStatus.Processed,
            },
            [
                new Distribution
                {
                    FibraId = fibra.Id,
                    Ticker = fibra.Ticker,
                    PaymentDate = today.AddDays(-20),
                    AmountPerUnit = 0.52m,
                    Currency = "MXN",
                },
                new Distribution
                {
                    FibraId = fibra.Id,
                    Ticker = fibra.Ticker,
                    PaymentDate = today.AddDays(-110),
                    AmountPerUnit = 0.47m,
                    Currency = "MXN",
                },
            ],
            0.67m,
            today);
    }

    private static string GetApiWebRootPath()
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "src", "Server", "Api", "wwwroot"));

    private static string GetFallbackPath() => Path.Combine(GetApiWebRootPath(), "og-image.png");

    private static bool IsPng(byte[] bytes)
        => bytes.Length >= 24
            && bytes[0] == 0x89
            && bytes[1] == 0x50
            && bytes[2] == 0x4E
            && bytes[3] == 0x47
            && bytes[4] == 0x0D
            && bytes[5] == 0x0A
            && bytes[6] == 0x1A
            && bytes[7] == 0x0A;

    private static (int Width, int Height) GetPngDimensions(byte[] bytes)
    {
        var width = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(16, 4));
        var height = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(20, 4));
        return (width, height);
    }

    private sealed class FakeWebHostEnvironment : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = string.Empty;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ApplicationName { get; set; } = "Infrastructure.Tests";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = "Production";
    }
}
