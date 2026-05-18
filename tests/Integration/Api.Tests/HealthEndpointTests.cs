using System.Text.Json;

namespace Api.Tests;

public class HealthCheckTests(ApiWebFactory factory) : IClassFixture<ApiWebFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetHealth_ReturnsOk_WithJsonBody()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("status", out _), "Response debe tener 'status'");
        Assert.True(root.TryGetProperty("checks", out var checks), "Response debe tener 'checks'");
        Assert.Equal(JsonValueKind.Array, checks.ValueKind);
    }

    [Fact]
    public async Task GetHealth_ContainsDatabaseAndPipelineChecks()
    {
        var response = await _client.GetAsync("/health");
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var checks = doc.RootElement.GetProperty("checks").EnumerateArray().ToList();

        var checkNames = checks.Select(c => c.GetProperty("name").GetString()).ToHashSet();
        Assert.Contains("database", checkNames);
        Assert.Contains("pipeline-freshness", checkNames);
    }

    [Fact]
    public async Task AnyRequest_ReturnsCorrelationIdHeader()
    {
        var response = await _client.GetAsync("/health");
        Assert.True(response.Headers.Contains("X-Correlation-Id"),
            "Response debe incluir header X-Correlation-Id");
    }

    [Fact]
    public async Task AnyRequest_WithCorrelationIdHeader_ReturnsTheSameId()
    {
        var expectedId = "test-correlation-123";
        var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("X-Correlation-Id", expectedId);

        var response = await _client.SendAsync(request);
        var returnedId = response.Headers.GetValues("X-Correlation-Id").FirstOrDefault();

        Assert.Equal(expectedId, returnedId);
    }
}
