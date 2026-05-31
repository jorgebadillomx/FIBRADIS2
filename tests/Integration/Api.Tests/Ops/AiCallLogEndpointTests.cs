using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Domain.Ai;
using Infrastructure.Persistence.SqlServer;
using Microsoft.Extensions.DependencyInjection;
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
    public async Task GetAiCallLogs_WithProviderFilter_ReturnOnlyMatchingProvider()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        db.AiCallLogs.AddRange(
            new AiCallLog
            {
                Timestamp = DateTimeOffset.UtcNow,
                Operation = "KpiExtraction",
                Provider = "Gemini",
                ModelId = "gemini-2.5-flash",
                PromptLength = 100,
                DurationMs = 500,
                Success = true,
            },
            new AiCallLog
            {
                Timestamp = DateTimeOffset.UtcNow,
                Operation = "NewsAnalysis",
                Provider = "DeepSeek",
                ModelId = "deepseek-chat",
                PromptLength = 200,
                DurationMs = 600,
                Success = true,
            });
        await db.SaveChangesAsync();

        var response = await _client.GetAsync("/api/v1/ops/ai-call-logs?provider=Gemini");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"provider\":\"Gemini\"", body);
        Assert.DoesNotContain("\"provider\":\"DeepSeek\"", body);
    }

    [Fact]
    public async Task GetAiCallLogs_WithOperationFilter_ReturnsOnlyMatchingOperation()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        db.AiCallLogs.AddRange(
            new AiCallLog
            {
                Timestamp = DateTimeOffset.UtcNow,
                Operation = "KpiExtraction",
                Provider = "Gemini",
                ModelId = "gemini-2.5-flash",
                PromptLength = 100,
                DurationMs = 500,
                Success = true,
            },
            new AiCallLog
            {
                Timestamp = DateTimeOffset.UtcNow,
                Operation = "NewsAnalysis",
                Provider = "Gemini",
                ModelId = "gemini-2.5-flash",
                PromptLength = 150,
                DurationMs = 400,
                Success = true,
            });
        await db.SaveChangesAsync();

        var response = await _client.GetAsync("/api/v1/ops/ai-call-logs?operation=KpiExtraction");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"operation\":\"KpiExtraction\"", body);
        Assert.DoesNotContain("\"operation\":\"NewsAnalysis\"", body);
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
