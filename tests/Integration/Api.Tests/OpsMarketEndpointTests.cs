using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using SharedApiContracts.Auth;

namespace Api.Tests;

public class OpsMarketEndpointTests : IAsyncLifetime
{
    private readonly ApiWebFactory _factory = new();
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await _factory.SeedUsersAsync();
        _client = _factory.CreateClient();

        var adminLogin = await _client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest("adminops@test.com", "ops123"));
        var adminBody = await adminLogin.Content.ReadFromJsonAsync<LoginResponse>();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", adminBody!.AccessToken);
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task PostDailySnapshotHistoricalRun_WithAdminOpsToken_ReturnsAccepted()
    {
        var response = await _client.PostAsync("/api/v1/ops/market/daily-snapshot-historical/run", content: null);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    [Fact]
    public async Task PostDistributionRun_WithAdminOpsToken_ReturnsAccepted()
    {
        var response = await _client.PostAsync("/api/v1/ops/market/distribution/run", content: null);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }
}
