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
        Assert.Null(body.LastUpdated);
    }

    [Fact]
    public async Task GetIndicadores_WithStoredRate_ReturnsCurrentValue()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var config = await db.OperationalConfigs.SingleAsync();
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
}
