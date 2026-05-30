using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using SharedApiContracts.Auth;

namespace Api.Tests.Ops;

public class AiCallLogEndpointTests(ApiWebFactory factory) : IClassFixture<ApiWebFactory>, IAsyncLifetime
{
    private readonly ApiWebFactory _factory = factory;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await _factory.SeedUsersAsync();
        _client = _factory.CreateClient();

        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest("adminops@test.com", "ops123"));
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.AccessToken);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetAiCallLogs_Returns200_WithPagedResult()
    {
        var response = await _client.GetAsync("/api/v1/ops/ai-call-logs");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = await System.Text.Json.JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync());

        Assert.True(doc.RootElement.TryGetProperty("total", out var total));
        Assert.True(total.GetInt32() >= 0);
        Assert.True(doc.RootElement.TryGetProperty("items", out _));
    }

    [Fact]
    public async Task GetAiCallLogs_WithProviderFilter_Returns200()
    {
        var response = await _client.GetAsync("/api/v1/ops/ai-call-logs?provider=Gemini");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAiCallLogs_WithSuccessFilter_Returns200()
    {
        var response = await _client.GetAsync("/api/v1/ops/ai-call-logs?success=true");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAiCallLogs_WithInvalidProvider_Returns400()
    {
        var response = await _client.GetAsync("/api/v1/ops/ai-call-logs?provider=InvalidValue");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAiCallLogs_WithInvalidOperation_Returns400()
    {
        var response = await _client.GetAsync("/api/v1/ops/ai-call-logs?operation=InvalidOp");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAiCallLogs_Unauthorized_Returns401()
    {
        var anonClient = _factory.CreateClient();
        var response = await anonClient.GetAsync("/api/v1/ops/ai-call-logs");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
