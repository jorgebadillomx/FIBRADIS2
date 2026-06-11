using System.Net;
using System.Text.Json;
using Domain.Market;
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Tests;

public class MarketEventsEndpointTests(ApiWebFactory factory)
    : IClassFixture<ApiWebFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<ApiWebFactory, Lazy<Task>> _seedByFactory = new();

    private Task EnsureSeededAsync()
        => _seedByFactory.GetOrAdd(factory, f => new Lazy<Task>(() => SeedOnceAsync(f))).Value;

    private static async Task SeedOnceAsync(ApiWebFactory f)
    {
        await f.SeedMarketAsync();

        using var scope = f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var funo = await db.Fibras.FirstAsync(f2 => f2.Ticker == "FUNO11");
        var fmty = await db.Fibras.FirstAsync(f2 => f2.Ticker == "FMTY14");

        if (!await db.Distributions.AnyAsync(d => d.FibraId == funo.Id && d.PaymentDate == new DateOnly(2026, 6, 10)))
        {
            db.Distributions.Add(new Distribution
            {
                Id = Guid.NewGuid(),
                FibraId = funo.Id,
                Ticker = funo.Ticker,
                PaymentDate = new DateOnly(2026, 6, 10),
                ExDividendDate = new DateOnly(2026, 6, 8),
                AmountPerUnit = 0.66m,
                TaxableAmount = 0.44m,
                CapitalReturnAmount = 0.22m,
                Currency = funo.Currency,
                Source = "seed-test",
                CapturedAt = DateTimeOffset.UtcNow,
            });
        }

        if (!await db.Distributions.AnyAsync(d => d.FibraId == fmty.Id && d.PaymentDate == new DateOnly(2026, 6, 9)))
        {
            db.Distributions.Add(new Distribution
            {
                Id = Guid.NewGuid(),
                FibraId = fmty.Id,
                Ticker = fmty.Ticker,
                PaymentDate = new DateOnly(2026, 6, 9),
                AmountPerUnit = 0.55m,
                Currency = fmty.Currency,
                Source = "seed-test",
                CapturedAt = DateTimeOffset.UtcNow,
            });
        }

        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task GetEvents_ReturnsPaymentAndExDividendEventsOrderedByDate()
    {
        await EnsureSeededAsync();

        var response = await _client.GetAsync("/api/v1/market/events?from=2026-06-01&to=2026-06-30");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(body);
        var items = doc.RootElement.EnumerateArray().ToList();

        Assert.Equal(3, items.Count);
        Assert.Equal("ExDerecho", items[0].GetProperty("eventType").GetString());
        Assert.Equal("2026-06-08", items[0].GetProperty("date").GetString());
        Assert.Equal("FUNO11", items[0].GetProperty("ticker").GetString());
        Assert.Equal("Fibra Uno", items[0].GetProperty("empresa").GetString());

        Assert.Equal("Pago", items[1].GetProperty("eventType").GetString());
        Assert.Equal("2026-06-09", items[1].GetProperty("date").GetString());

        Assert.Equal("Pago", items[2].GetProperty("eventType").GetString());
        Assert.Equal("2026-06-10", items[2].GetProperty("date").GetString());
    }
}
