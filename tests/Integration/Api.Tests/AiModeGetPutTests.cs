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
            Password = "ops123",
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

    // ── GET /api/v1/ops/news ──────────────────────────────────────────────────

    [Fact]
    public async Task GetOpsNewsList_WithAdminOpsToken_ReturnsPagedResult()
    {
        await _factory.SeedNewsAsync();

        var response = await _client.GetAsync("/api/v1/ops/news?page=1&pageSize=20");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(doc.RootElement.TryGetProperty("items", out var items), "missing: items");
        Assert.True(doc.RootElement.TryGetProperty("total", out _), "missing: total");
        Assert.True(doc.RootElement.TryGetProperty("page", out var page), "missing: page");
        Assert.Equal(1, page.GetInt32());
        Assert.True(items.GetArrayLength() >= 1);
    }

    [Fact]
    public async Task GetOpsNewsList_WithoutToken_Returns401()
    {
        var response = await _factory.CreateClient().GetAsync("/api/v1/ops/news");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── GET /api/v1/ops/news/{id} ─────────────────────────────────────────────

    [Fact]
    public async Task GetOpsNewsBody_WithAdminOpsToken_ReturnsBodyTextProperty()
    {
        await _factory.SeedNewsAsync();
        var id = ApiWebFactory.TestNewsArticleId;

        var response = await _client.GetAsync($"/api/v1/ops/news/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(doc.RootElement.TryGetProperty("id", out _), "missing: id");
        Assert.True(doc.RootElement.TryGetProperty("bodyText", out _), "missing: bodyText");
    }

    [Fact]
    public async Task GetOpsNewsBody_WhenArticleNotFound_Returns404()
    {
        var response = await _client.GetAsync($"/api/v1/ops/news/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── PUT /api/v1/ops/news/{id}/body-text ──────────────────────────────────

    [Fact]
    public async Task PutOpsNewsBodyText_WithNewText_Returns204AndPersists()
    {
        await _factory.SeedNewsAsync();
        var id = ApiWebFactory.TestNewsArticleId;

        var response = await _client.PutAsJsonAsync(
            $"/api/v1/ops/news/{id}/body-text",
            new { bodyText = "Texto corregido manualmente por el operador." });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var getResponse = await _client.GetAsync($"/api/v1/ops/news/{id}");
        using var doc = JsonDocument.Parse(await getResponse.Content.ReadAsStringAsync());
        Assert.Equal("Texto corregido manualmente por el operador.", doc.RootElement.GetProperty("bodyText").GetString());
    }

    [Fact]
    public async Task PutOpsNewsBodyText_WithEmptyString_SetsBodyTextNull()
    {
        await _factory.SeedNewsAsync();
        var id = ApiWebFactory.TestNewsArticleId;

        var response = await _client.PutAsJsonAsync(
            $"/api/v1/ops/news/{id}/body-text",
            new { bodyText = (string?)null });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var getResponse = await _client.GetAsync($"/api/v1/ops/news/{id}");
        using var doc = JsonDocument.Parse(await getResponse.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty("bodyText").ValueKind);
    }

    [Fact]
    public async Task PutOpsNewsBodyText_WhenArticleNotFound_Returns404()
    {
        var response = await _client.PutAsJsonAsync(
            $"/api/v1/ops/news/{Guid.NewGuid()}/body-text",
            new { bodyText = "Texto nuevo" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PutOpsNewsBodyText_WithoutToken_Returns401()
    {
        await _factory.SeedNewsAsync();
        var id = ApiWebFactory.TestNewsArticleId;

        var response = await _factory.CreateClient().PutAsJsonAsync(
            $"/api/v1/ops/news/{id}/body-text",
            new { bodyText = "Intento no autorizado" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── newsModel — AC 1, 2, 3 ───────────────────────────────────────────────

    [Fact]
    public async Task GetAiMode_ResponseContainsNewsModel()
    {
        var response = await _client.GetAsync("/api/v1/ops/ai-mode");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(doc.RootElement.TryGetProperty("newsModel", out var newsModelProp), "missing: newsModel");
        var value = newsModelProp.GetString();
        Assert.False(string.IsNullOrEmpty(value), "newsModel should not be empty");
    }

    [Fact]
    public async Task PutAiMode_WithNewsModelOnly_UpdatesModelAndPreservesMode()
    {
        await _client.PutAsJsonAsync("/api/v1/ops/ai-mode", new { mode = "Off" });

        var putResponse = await _client.PutAsJsonAsync("/api/v1/ops/ai-mode", new { newsModel = "gemini-2.5-flash" });
        Assert.Equal(HttpStatusCode.NoContent, putResponse.StatusCode);

        var getResponse = await _client.GetAsync("/api/v1/ops/ai-mode");
        using var doc = JsonDocument.Parse(await getResponse.Content.ReadAsStringAsync());
        Assert.Equal("gemini-2.5-flash", doc.RootElement.GetProperty("newsModel").GetString());
        Assert.Equal("Off", doc.RootElement.GetProperty("mode").GetString());
    }

    [Fact]
    public async Task PutAiMode_WithModeOnly_PreservesNewsModel()
    {
        await _client.PutAsJsonAsync("/api/v1/ops/ai-mode", new { newsModel = "gemini-2.5-flash" });

        var putResponse = await _client.PutAsJsonAsync("/api/v1/ops/ai-mode", new { mode = "On" });
        Assert.Equal(HttpStatusCode.NoContent, putResponse.StatusCode);

        var getResponse = await _client.GetAsync("/api/v1/ops/ai-mode");
        using var doc = JsonDocument.Parse(await getResponse.Content.ReadAsStringAsync());
        Assert.Equal("gemini-2.5-flash", doc.RootElement.GetProperty("newsModel").GetString());
        Assert.Equal("On", doc.RootElement.GetProperty("mode").GetString());
    }

    [Fact]
    public async Task PutAiMode_WithInvalidNewsModel_Returns400()
    {
        var response = await _client.PutAsJsonAsync("/api/v1/ops/ai-mode", new { newsModel = "gemini-invalid" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PutAiMode_WithEmptyBody_Returns400()
    {
        var response = await _client.PutAsJsonAsync("/api/v1/ops/ai-mode", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
