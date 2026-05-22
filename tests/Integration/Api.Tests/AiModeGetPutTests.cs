using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Api.Tests;

public class AiModeGetPutTests(ApiWebFactory factory) : IClassFixture<ApiWebFactory>, IAsyncLifetime
{
    private readonly ApiWebFactory _factory = factory;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await _factory.SeedUsersAsync();

        _client = _factory.CreateClient();

        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            Email = "adminops@test.com",
            Password = "admin456",
        });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        using var doc = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync());
        var token = doc.RootElement.GetProperty("accessToken").GetString();
        Assert.NotNull(token);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetAiMode_WithAdminOpsToken_ReturnsOkWithModeDto()
    {
        var response = await _client.GetAsync("/api/v1/ops/ai-mode");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("mode", out var modeProp));
        var mode = modeProp.GetString();
        Assert.True(mode == "Off" || mode == "On", $"Unexpected mode: {mode}");
    }

    [Fact]
    public async Task GetAiMode_WithAdminOpsToken_ResponseContainsRequiredFields()
    {
        var response = await _client.GetAsync("/api/v1/ops/ai-mode");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        Assert.True(doc.RootElement.TryGetProperty("mode", out _), "missing: mode");
        Assert.True(doc.RootElement.TryGetProperty("updatedAt", out _), "missing: updatedAt");
    }

    [Fact]
    public async Task PutAiMode_ChangeToOn_Returns204()
    {
        var response = await _client.PutAsJsonAsync("/api/v1/ops/ai-mode", new { Mode = "On" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task PutAiMode_ChangeToOn_ThenGetReflectsNewMode()
    {
        await _client.PutAsJsonAsync("/api/v1/ops/ai-mode", new { Mode = "On" });

        var getResponse = await _client.GetAsync("/api/v1/ops/ai-mode");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        using var doc = JsonDocument.Parse(await getResponse.Content.ReadAsStringAsync());
        Assert.Equal("On", doc.RootElement.GetProperty("mode").GetString());
    }

    [Fact]
    public async Task PutAiMode_ChangeToOff_Returns204()
    {
        var response = await _client.PutAsJsonAsync("/api/v1/ops/ai-mode", new { Mode = "Off" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task PutAiMode_InvalidMode_Returns400()
    {
        var response = await _client.PutAsJsonAsync("/api/v1/ops/ai-mode", new { Mode = "InvalidMode" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAiMode_WithoutToken_Returns401()
    {
        var unauthClient = _factory.CreateClient();

        var response = await unauthClient.GetAsync("/api/v1/ops/ai-mode");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PutAiMode_WithoutToken_Returns401()
    {
        var unauthClient = _factory.CreateClient();

        var response = await unauthClient.PutAsJsonAsync("/api/v1/ops/ai-mode", new { Mode = "On" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PutAiMode_WithUserToken_Returns403()
    {
        var loginResponse = await _factory.CreateClient().PostAsJsonAsync("/api/v1/auth/login", new
        {
            Email = "user@test.com",
            Password = "password123",
        });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        using var doc = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync());
        var token = doc.RootElement.GetProperty("accessToken").GetString();

        var userClient = _factory.CreateClient();
        userClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await userClient.PutAsJsonAsync("/api/v1/ops/ai-mode", new { Mode = "On" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
