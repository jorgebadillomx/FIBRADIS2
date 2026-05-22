using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using SharedApiContracts.Auth;
using SharedApiContracts.News;

namespace Api.Tests;

public class NewsBlocklistOpsEndpointTests : IAsyncLifetime
{
    private readonly ApiWebFactory _factory = new();
    private HttpClient _client = null!;
    private string _adminOpsToken = string.Empty;

    public async Task InitializeAsync()
    {
        await _factory.SeedUsersAsync();
        _client = _factory.CreateClient();

        var adminLogin = await _client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest("adminops@test.com", "ops123"));
        var adminBody = await adminLogin.Content.ReadFromJsonAsync<LoginResponse>();
        _adminOpsToken = adminBody!.AccessToken;
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminOpsToken);
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GetBlocklistTerms_WithAdminOpsToken_ReturnsSeededTerms()
    {
        var response = await _client.GetAsync("/api/v1/news/blocklist-terms");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<List<BlocklistTermDto>>();
        Assert.NotNull(payload);
        Assert.Contains(payload, item => item.Term == "fibra óptica");
    }

    [Fact]
    public async Task PostAndDeleteBlocklistTerm_WithAdminOpsToken_PersistsChanges()
    {
        var createResponse = await _client.PostAsJsonAsync(
            "/api/v1/news/blocklist-terms",
            new CreateBlocklistTermRequest("nuevo termino"));

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<BlocklistTermDto>();
        Assert.NotNull(created);
        Assert.Equal("nuevo termino", created!.Term);

        var getResponse = await _client.GetAsync("/api/v1/news/blocklist-terms");
        var items = await getResponse.Content.ReadFromJsonAsync<List<BlocklistTermDto>>();
        Assert.Contains(items!, item => item.Id == created.Id);

        var deleteResponse = await _client.DeleteAsync($"/api/v1/news/blocklist-terms/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var afterDeleteResponse = await _client.GetAsync("/api/v1/news/blocklist-terms");
        var afterDeleteItems = await afterDeleteResponse.Content.ReadFromJsonAsync<List<BlocklistTermDto>>();
        Assert.DoesNotContain(afterDeleteItems!, item => item.Id == created.Id);
    }

    [Fact]
    public async Task PostBlocklistTerm_WithMoreThan256Characters_ReturnsBadRequest()
    {
        var tooLongTerm = new string('x', 257);

        var response = await _client.PostAsJsonAsync(
            "/api/v1/news/blocklist-terms",
            new CreateBlocklistTermRequest(tooLongTerm));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains("term", problem!.Errors.Keys);
    }
}
