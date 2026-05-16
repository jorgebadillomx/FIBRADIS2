using System.Text.Json;

namespace Api.Tests;

public class HealthEndpointTests(ApiWebFactory factory) : IClassFixture<ApiWebFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetHealth_Returns200WithHealthyStatus()
    {
        var response = await _client.GetAsync("/api/v1/health");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var status = doc.RootElement.GetProperty("status").GetString();
        Assert.Equal("healthy", status);
    }
}
