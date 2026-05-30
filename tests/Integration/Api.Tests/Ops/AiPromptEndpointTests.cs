using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using SharedApiContracts.Auth;
using SharedApiContracts.News;

namespace Api.Tests.Ops;

public class AiPromptEndpointTests(ApiWebFactory factory) : IClassFixture<ApiWebFactory>, IAsyncLifetime
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
    public async Task GetAiPrompt_WithAdminOpsToken_Returns200WithTemplate()
    {
        var response = await _client.GetAsync("/api/v1/ops/ai-prompts/news");
        var dto = await response.Content.ReadFromJsonAsync<AiPromptDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(dto);
        Assert.Equal("news", dto.ContentType);
        Assert.Contains("{title}", dto.PromptTemplate);
    }

    [Fact]
    public async Task PutAiPrompt_WithValidTemplate_Returns204()
    {
        var template = "Custom\nTítulo: {title}\n{snippet_section}\n{body_section}";

        var response = await _client.PutAsJsonAsync("/api/v1/ops/ai-prompts/news", new UpdateAiPromptRequest(template));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task PutAiPrompt_WithoutTitlePlaceholder_Returns400()
    {
        var template = "Custom\n{snippet_section}\n{body_section}";

        var response = await _client.PutAsJsonAsync("/api/v1/ops/ai-prompts/news", new UpdateAiPromptRequest(template));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PutAiPrompt_WithoutSnippetSectionPlaceholder_Returns400()
    {
        var template = "Custom\nTítulo: {title}\n{body_section}";

        var response = await _client.PutAsJsonAsync("/api/v1/ops/ai-prompts/news", new UpdateAiPromptRequest(template));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PutAiPrompt_WithoutBodySectionPlaceholder_Returns400()
    {
        var template = "Custom\nTítulo: {title}\n{snippet_section}";

        var response = await _client.PutAsJsonAsync("/api/v1/ops/ai-prompts/news", new UpdateAiPromptRequest(template));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PutAiPrompt_ExceedingMaxLength_Returns400()
    {
        var template = $"Título: {{title}}\n{{snippet_section}}\n{{body_section}}\n{new string('x', 4000)}";

        var response = await _client.PutAsJsonAsync("/api/v1/ops/ai-prompts/news", new UpdateAiPromptRequest(template));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
