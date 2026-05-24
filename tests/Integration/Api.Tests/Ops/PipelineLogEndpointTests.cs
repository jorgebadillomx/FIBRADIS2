using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Infrastructure.Persistence.SqlServer;
using Microsoft.Extensions.DependencyInjection;
using SharedApiContracts.Auth;

namespace Api.Tests.Ops;

public class PipelineLogEndpointTests(ApiWebFactory factory) : IClassFixture<ApiWebFactory>, IAsyncLifetime
{
    private readonly ApiWebFactory _factory = factory;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await _factory.SeedUsersAsync();
        _client = _factory.CreateClient();

        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("adminops@test.com", "ops123"));
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.AccessToken);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetPipelineLogs_Returns200WhenEmpty()
    {
        var response = await _client.GetAsync("/api/v1/ops/pipeline-logs");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetPipelineLogs_WithSeededEntry_ReturnsMatchingPipeline()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();
        db.PipelineErrorLogs.Add(new Domain.Jobs.PipelineErrorLog
        {
            Pipeline = "News",
            Timestamp = DateTimeOffset.UtcNow,
            ErrorType = "InvalidOperationException",
            Message = "Seed failure",
            Context = "{\"url\":\"https://example.com\"}",
            AiContext = "El pipeline de noticias falló al guardar un artículo de prueba con contexto suficiente para revisión operativa.",
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();

        var response = await _client.GetAsync("/api/v1/ops/pipeline-logs?pipeline=News&page=1&pageSize=50");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"pipeline\":\"News\"", body);
        Assert.DoesNotContain("\"pipeline\":\"Market\"", body);
    }
}
