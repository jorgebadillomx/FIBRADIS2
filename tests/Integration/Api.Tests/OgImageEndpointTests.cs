using System.Net;
using System.Buffers.Binary;
using Domain.Market;
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Tests;

public class OgImageEndpointTests : IClassFixture<ApiWebFactory>
{
    [Fact]
    public async Task GetFibraOgImage_ReturnsPngWithCacheHeaders()
    {
        using var factory = new ApiWebFactory();
        var client = factory.CreateClient();

        await SeedOgImageDataAsync(factory);

        var response = await client.GetAsync("/og/fibras/funo11.png");
        var bytes = await response.Content.ReadAsByteArrayAsync();
        var fallback = await File.ReadAllBytesAsync(GetFallbackPath());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/png", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("max-age=21600", response.Headers.CacheControl?.ToString() ?? string.Empty);
        Assert.Equal((1200, 630), GetPngDimensions(bytes));
        Assert.False(bytes.SequenceEqual(fallback));
    }

    [Fact]
    public async Task GetFibraOgImage_UnknownTicker_ReturnsFallbackAsset()
    {
        using var factory = new ApiWebFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/og/fibras/inexistente-xxxx99.png");
        var bytes = await response.Content.ReadAsByteArrayAsync();
        var fallback = await File.ReadAllBytesAsync(GetFallbackPath());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/png", response.Content.Headers.ContentType?.MediaType);
        Assert.True(bytes.SequenceEqual(fallback));
    }

    private async Task SeedOgImageDataAsync(ApiWebFactory factory)
    {
        await factory.SeedCatalogAsync();
        await factory.SeedMarketAsync();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        var funo = await db.Fibras.FirstAsync(f => f.Ticker == "FUNO11");
        if (!await db.Distributions.AnyAsync(d => d.FibraId == funo.Id))
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            db.Distributions.AddRange(
                new Distribution
                {
                    FibraId = funo.Id,
                    Ticker = funo.Ticker,
                    PaymentDate = today.AddDays(-20),
                    AmountPerUnit = 0.52m,
                    Currency = "MXN",
                    Source = "og-image-test",
                    CapturedAt = DateTimeOffset.UtcNow,
                },
                new Distribution
                {
                    FibraId = funo.Id,
                    Ticker = funo.Ticker,
                    PaymentDate = today.AddDays(-110),
                    AmountPerUnit = 0.47m,
                    Currency = "MXN",
                    Source = "og-image-test",
                    CapturedAt = DateTimeOffset.UtcNow,
                });

            await db.SaveChangesAsync();
        }
    }

    private static string GetFallbackPath()
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "src", "Server", "Api", "wwwroot", "og-image.png"));

    private static (int Width, int Height) GetPngDimensions(byte[] bytes)
        => (BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(16, 4)), BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(20, 4)));
}
