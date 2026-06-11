using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using SharedApiContracts.Auth;
using SharedApiContracts.Market;

namespace Api.Tests;

public class DistributionEndpointTests : IAsyncLifetime
{
    private readonly ApiWebFactory _factory = new();
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await _factory.SeedUsersAsync();
        await _factory.SeedMarketAsync();
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
    public async Task PostSync_WithAdminOpsToken_ReturnsAccepted()
    {
        var response = await _client.PostAsync("/api/v1/ops/distributions/sync", content: null);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    [Fact]
    public async Task CrudFlow_CreateUpdateDeleteDistribution_Works()
    {
        var createResponse = await _client.PostAsJsonAsync(
            "/api/v1/ops/distributions",
            new DistributionUpsertRequest(
                "FUNO11",
                new DateOnly(2026, 6, 20),
                new DateOnly(2026, 6, 18),
                0.61m,
                0.41m,
                0.20m,
                "https://www.bmv.com.mx/docs-pub/aviso.pdf"));

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var createdBody = await createResponse.Content.ReadAsStringAsync();
        using var createdDoc = JsonDocument.Parse(createdBody);
        var distributionId = createdDoc.RootElement.GetProperty("id").GetGuid();

        var updateResponse = await _client.PutAsJsonAsync(
            $"/api/v1/ops/distributions/{distributionId}",
            new DistributionUpsertRequest(
                "FUNO11",
                new DateOnly(2026, 6, 20),
                new DateOnly(2026, 6, 18),
                0.66m,
                0.46m,
                0.22m,
                "https://www.bmv.com.mx/docs-pub/aviso.pdf"));

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updatedBody = await updateResponse.Content.ReadAsStringAsync();
        using var updatedDoc = JsonDocument.Parse(updatedBody);
        Assert.Equal(0.66m, updatedDoc.RootElement.GetProperty("amountPerUnit").GetDecimal());
        Assert.Equal(0.46m, updatedDoc.RootElement.GetProperty("taxableAmount").GetDecimal());
        Assert.Equal(0.22m, updatedDoc.RootElement.GetProperty("capitalReturnAmount").GetDecimal());

        var deleteResponse = await _client.DeleteAsync($"/api/v1/ops/distributions/{distributionId}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }
}
