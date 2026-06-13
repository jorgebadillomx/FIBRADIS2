using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SharedApiContracts.Market;

namespace Api.Tests;

public class IndicadoresEndpointTests : IAsyncLifetime
{
    private readonly ApiWebFactory _factory = new();
    private HttpClient _anonClient = null!;
    private HttpClient _userClient = null!;

    public async Task InitializeAsync()
    {
        await _factory.SeedUsersAsync();
        _anonClient = _factory.CreateClient();

        var login = await _anonClient.PostAsJsonAsync("/api/v1/auth/login", new SharedApiContracts.Auth.LoginRequest("user@test.com", "password123"));
        var loginBody = await login.Content.ReadFromJsonAsync<SharedApiContracts.Auth.LoginResponse>();

        _userClient = _factory.CreateClient();
        _userClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginBody!.AccessToken);
    }

    public Task DisposeAsync()
    {
        _anonClient.Dispose();
        _userClient.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GetIndicadores_Anonymous_Returns401()
    {
        var response = await _anonClient.GetAsync("/api/v1/market/indicadores");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetIndicadores_WithSeedConfig_ReturnsNullsByDefault()
    {
        var response = await _userClient.GetAsync("/api/v1/market/indicadores");
        var body = await response.Content.ReadFromJsonAsync<IndicadoresDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Null(body!.Cetes28d);
        Assert.Null(body.Tiie28d);
        Assert.Null(body.LastUpdated);
        Assert.Empty(body.InpcHistory ?? []);
    }

    [Fact]
    public async Task GetIndicadores_WithStoredRate_ReturnsCurrentValue()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var config = await db.OperationalConfigs.FirstOrDefaultAsync();
        if (config is null)
        {
            config = new Domain.Ops.OperationalConfig { Id = 1 };
            db.OperationalConfigs.Add(config);
        }
        var updatedAt = new DateTimeOffset(2026, 6, 12, 18, 30, 0, TimeSpan.Zero);
        config.Cetes28dRate = 9.50m;
        config.Cetes28dRateUpdatedAt = updatedAt;
        await db.SaveChangesAsync();

        try
        {
            var response = await _userClient.GetAsync("/api/v1/market/indicadores");
            var body = await response.Content.ReadFromJsonAsync<IndicadoresDto>();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(body);
            Assert.Equal(9.50m, body!.Cetes28d);
            Assert.Equal(updatedAt, body.LastUpdated);
        }
        finally
        {
            config.Cetes28dRate = null;
            config.Cetes28dRateUpdatedAt = null;
            await db.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task GetIndicadores_WithTiieAndInpcHistory_ReturnsExtendedPayload()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var config = await db.OperationalConfigs.FirstOrDefaultAsync();
        if (config is null)
        {
            config = new Domain.Ops.OperationalConfig { Id = 1 };
            db.OperationalConfigs.Add(config);
        }
        var cetesUpdatedAt = new DateTimeOffset(2026, 6, 12, 18, 30, 0, TimeSpan.Zero);
        var tiieUpdatedAt = new DateTimeOffset(2026, 6, 12, 19, 0, 0, TimeSpan.Zero);

        config.Cetes28dRate = 9.50m;
        config.Cetes28dRateUpdatedAt = cetesUpdatedAt;
        config.Tiie28dRate = 10.25m;
        config.Tiie28dRateUpdatedAt = tiieUpdatedAt;

        var start = new DateOnly(2024, 1, 1);
        for (var i = 0; i < 24; i++)
        {
            var period = start.AddMonths(i);
            var baseValue = 100m + (i % 12);
            db.InpcMonthlyEntries.Add(new Domain.Ops.InpcMonthlyEntry
            {
                Periodo = period,
                InpcIndex = i < 12 ? baseValue : baseValue * 1.1m,
                CapturedAt = tiieUpdatedAt,
            });
        }

        await db.SaveChangesAsync();

        try
        {
            var response = await _userClient.GetAsync("/api/v1/market/indicadores");
            var body = await response.Content.ReadFromJsonAsync<IndicadoresDto>();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(body);
            Assert.Equal(9.50m, body!.Cetes28d);
            Assert.Equal(10.25m, body.Tiie28d);
            Assert.Equal(tiieUpdatedAt, body.LastUpdated);
            Assert.NotNull(body.InpcHistory);
            Assert.Equal(12, body.InpcHistory!.Count);
            Assert.Equal("2025-01", body.InpcHistory[0].Periodo);
            Assert.Equal(10.00m, body.InpcHistory[0].AnualPct);
            Assert.Equal("2025-12", body.InpcHistory[^1].Periodo);
            Assert.Equal(10.00m, body.InpcHistory[^1].AnualPct);
        }
        finally
        {
            config.Cetes28dRate = null;
            config.Cetes28dRateUpdatedAt = null;
            config.Tiie28dRate = null;
            config.Tiie28dRateUpdatedAt = null;
            db.InpcMonthlyEntries.RemoveRange(db.InpcMonthlyEntries.ToList());
            await db.SaveChangesAsync();
        }
    }
}
