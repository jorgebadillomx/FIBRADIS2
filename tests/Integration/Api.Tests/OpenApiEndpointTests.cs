namespace Api.Tests;

public class OpenApiEndpointTests(ApiWebFactory factory) : IClassFixture<ApiWebFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetOpenApiSpec_Returns200WithOpenApiDocument()
    {
        var response = await _client.GetAsync("/openapi/v1.json");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"openapi\"", body);
        Assert.Contains("/api/v1/health", body);
    }
}
