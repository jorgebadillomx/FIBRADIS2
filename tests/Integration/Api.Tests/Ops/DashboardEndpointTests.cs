using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Infrastructure.Persistence.SqlServer;
using Microsoft.Extensions.DependencyInjection;
using SharedApiContracts.Auth;
using SharedApiContracts.Jobs;

namespace Api.Tests.Ops;

public class DashboardEndpointTests(ApiWebFactory factory) : IClassFixture<ApiWebFactory>, IAsyncLifetime
{
    private readonly ApiWebFactory _factory = factory;
    private HttpClient _adminClient = null!;

    public async Task InitializeAsync()
    {
        await _factory.SeedUsersAsync();
        _adminClient = _factory.CreateClient();
        _adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LoginAndGetTokenAsync("adminops@test.com", "ops123"));
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetDashboard_WithAdminOpsToken_ReturnsPipelineDashboardDto()
    {
        var response = await _adminClient.GetAsync("/api/v1/ops/dashboard");
        var body = await response.Content.ReadFromJsonAsync<PipelineDashboardDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(4, body!.Pipelines.Count);
        Assert.Contains(body.Pipelines, pipeline => pipeline.Pipeline == "Market");
        Assert.Contains(body.Pipelines, pipeline => pipeline.Pipeline == "News");
        Assert.Contains(body.Pipelines, pipeline => pipeline.Pipeline == "Distribution");
        Assert.Contains(body.Pipelines, pipeline => pipeline.Pipeline == "Fundamentals");
        Assert.NotNull(body.RecentErrors);
    }

    [Fact]
    public async Task PostMarketRun_ReturnsAcceptedAndCreatesQueuedPipelineRunLog()
    {
        var response = await _adminClient.PostAsync("/api/v1/ops/market/run", content: null);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var entry = db.PipelineRunLogs
            .OrderByDescending(x => x.StartedAt)
            .FirstOrDefault(x => x.Pipeline == "Market" && x.Status == "Queued");

        Assert.NotNull(entry);
        Assert.Equal("adminops@test.com", entry!.TriggeredBy);
    }

    [Fact]
    public async Task PostNewsRun_ReturnsAcceptedAndCreatesQueuedPipelineRunLog()
    {
        var response = await _adminClient.PostAsync("/api/v1/ops/news-pipeline/run", content: null);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var entry = db.PipelineRunLogs
            .OrderByDescending(x => x.StartedAt)
            .FirstOrDefault(x => x.Pipeline == "News" && x.Status == "Queued");

        Assert.NotNull(entry);
        Assert.Equal("adminops@test.com", entry!.TriggeredBy);
    }

    [Fact]
    public async Task PostDistributionRun_ReturnsAcceptedAndCreatesQueuedPipelineRunLog()
    {
        var response = await _adminClient.PostAsync("/api/v1/ops/market/distribution/run", content: null);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var entry = db.PipelineRunLogs
            .OrderByDescending(x => x.StartedAt)
            .FirstOrDefault(x => x.Pipeline == "Distribution" && x.Status == "Queued");

        Assert.NotNull(entry);
        Assert.Equal("adminops@test.com", entry!.TriggeredBy);
    }

    [Fact]
    public async Task PostFundamentalsRun_ReturnsAcceptedAndCreatesQueuedPipelineRunLog()
    {
        var response = await _adminClient.PostAsync("/api/v1/ops/market/fundamentals/run", content: null);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var entry = db.PipelineRunLogs
            .OrderByDescending(x => x.StartedAt)
            .FirstOrDefault(x => x.Pipeline == "Fundamentals" && x.Status == "Queued");

        Assert.NotNull(entry);
        Assert.Equal("adminops@test.com", entry!.TriggeredBy);
    }

    [Fact]
    public async Task GetDashboard_WithUserRole_ReturnsForbidden()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LoginAndGetTokenAsync("user@test.com", "password123"));

        var response = await client.GetAsync("/api/v1/ops/dashboard");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/v1/ops/market/run")]
    [InlineData("/api/v1/ops/news-pipeline/run")]
    [InlineData("/api/v1/ops/market/distribution/run")]
    [InlineData("/api/v1/ops/market/fundamentals/run")]
    public async Task RunEndpoints_WithoutToken_ReturnUnauthorized(string path)
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsync(path, content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/v1/ops/market/run")]
    [InlineData("/api/v1/ops/news-pipeline/run")]
    [InlineData("/api/v1/ops/market/distribution/run")]
    [InlineData("/api/v1/ops/market/fundamentals/run")]
    public async Task RunEndpoints_WithUserRole_ReturnForbidden(string path)
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await LoginAndGetTokenAsync("user@test.com", "password123"));

        var response = await client.PostAsync(path, content: null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetDashboard_WithoutToken_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/ops/dashboard");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<string> LoginAndGetTokenAsync(string email, string password)
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, password));
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return body!.AccessToken;
    }
}
