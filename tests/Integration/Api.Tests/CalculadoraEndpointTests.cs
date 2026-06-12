using System.Net;
using System.Text.Json;
using Domain.Catalog;
using Domain.Market;
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Tests;

public class CalculadoraEndpointTests(ApiWebFactory factory)
    : IClassFixture<ApiWebFactory>
{
    private readonly HttpClient _client = factory.CreateClient();
    private static bool _seeded;
    private static readonly SemaphoreSlim _lock = new(1, 1);

    private async Task EnsureSeededAsync()
    {
        await _lock.WaitAsync();
        try
        {
            if (_seeded)
                return;

            await factory.SeedCatalogAsync();
            await factory.SeedMarketAsync();
            await SeedCalculadoraDataAsync();
            _seeded = true;
        }
        finally
        {
            _lock.Release();
        }
    }

    [Fact]
    public async Task GetCalculadora_ReturnsOk_WithExpectedDistributionTotals()
    {
        await EnsureSeededAsync();

        var response = await _client.GetAsync("/api/v1/market/calculadora");
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var rows = doc.RootElement.EnumerateArray().ToList();
        var fmty = rows.Single(row => row.GetProperty("ticker").GetString() == "FMTY14");

        Assert.Equal("Fibra Monterrey", fmty.GetProperty("empresa").GetString());
        Assert.Equal(16.20m, fmty.GetProperty("precioActual").GetDecimal());
        Assert.Equal("Q2-2026", fmty.GetProperty("ultimoPeriodo").GetString());
        Assert.Equal(0.40m, fmty.GetProperty("distCbfi").GetDecimal());
        Assert.Equal(0.45m, fmty.GetProperty("distCbfiAnual").GetDecimal());
        Assert.True(fmty.TryGetProperty("freshnessStatus", out _));
    }

    [Fact]
    public async Task GetCalculadora_ActiveFibraWithoutDistributions_ReturnsNullDistributionFields()
    {
        await EnsureSeededAsync();

        var response = await _client.GetAsync("/api/v1/market/calculadora");
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        var rows = doc.RootElement.EnumerateArray().ToList();
        var testRow = rows.Single(row => row.GetProperty("ticker").GetString() == "CALC01");

        Assert.Equal(JsonValueKind.Null, testRow.GetProperty("ultimoPeriodo").ValueKind);
        Assert.Equal(JsonValueKind.Null, testRow.GetProperty("distCbfi").ValueKind);
        Assert.Equal(JsonValueKind.Null, testRow.GetProperty("distCbfiAnual").ValueKind);
        Assert.Equal(JsonValueKind.Null, testRow.GetProperty("freshnessStatus").ValueKind);
    }

    private async Task SeedCalculadoraDataAsync()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        var fmty = await db.Fibras.FirstAsync(f => f.Ticker == "FMTY14");
        if (!await db.PriceSnapshots.AnyAsync(p => p.FibraId == fmty.Id))
        {
            db.PriceSnapshots.Add(new PriceSnapshot
            {
                Id = Guid.NewGuid(),
                FibraId = fmty.Id,
                Ticker = "FMTY14",
                LastPrice = 16.20m,
                DailyChange = -0.08m,
                DailyChangePct = -0.49m,
                Volume = 2_345_678L,
                Week52High = 17.80m,
                Week52Low = 15.10m,
                CapturedAt = DateTimeOffset.UtcNow - TimeSpan.FromMinutes(5),
                Status = MarketDataStatus.Processed,
            });
        }

        if (!await db.Distributions.AnyAsync(d => d.FibraId == fmty.Id && d.PaymentDate == new DateOnly(2026, 5, 10)))
        {
            db.Distributions.AddRange(
                new Distribution
                {
                    Id = Guid.NewGuid(),
                    FibraId = fmty.Id,
                    Ticker = "FMTY14",
                    PaymentDate = new DateOnly(2026, 5, 10),
                    AmountPerUnit = 0.30m,
                    Currency = "MXN",
                    Source = "test",
                    CapturedAt = DateTimeOffset.UtcNow,
                },
                new Distribution
                {
                    Id = Guid.NewGuid(),
                    FibraId = fmty.Id,
                    Ticker = "FMTY14",
                    PaymentDate = new DateOnly(2026, 5, 20),
                    AmountPerUnit = 0.10m,
                    Currency = "MXN",
                    Source = "test",
                    CapturedAt = DateTimeOffset.UtcNow,
                },
                new Distribution
                {
                    Id = Guid.NewGuid(),
                    FibraId = fmty.Id,
                    Ticker = "FMTY14",
                    PaymentDate = new DateOnly(2026, 2, 15),
                    AmountPerUnit = 0.05m,
                    Currency = "MXN",
                    Source = "test",
                    CapturedAt = DateTimeOffset.UtcNow,
                },
                new Distribution
                {
                    Id = Guid.NewGuid(),
                    FibraId = fmty.Id,
                    Ticker = "FMTY14",
                    PaymentDate = new DateOnly(2025, 11, 15),
                    AmountPerUnit = 0.20m,
                    Currency = "MXN",
                    Source = "test",
                    CapturedAt = DateTimeOffset.UtcNow,
                });
        }

        if (!await db.Fibras.AnyAsync(f => f.Ticker == "CALC01"))
        {
            db.Fibras.Add(new Fibra
            {
                Id = Guid.NewGuid(),
                Ticker = "CALC01",
                YahooTicker = "CALC01.MX",
                FullName = "Calculadora Test",
                ShortName = "Calc Test",
                Sector = "Diversificado",
                Market = "BMV",
                Currency = "MXN",
                State = FibraState.Active,
                NameVariants = [],
                CreatedAt = DateTimeOffset.UtcNow,
            });
        }

        await db.SaveChangesAsync();
    }
}
