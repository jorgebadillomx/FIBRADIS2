using System.Text.Json;

namespace Api.Tests;

public class ProblemDetailsTests(ApiWebFactory factory) : IClassFixture<ApiWebFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetNonExistentRoute_Returns404WithProblemDetailsAndCorrelationId()
    {
        var response = await _client.GetAsync("/api/v1/ruta-inexistente");

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);

        // Header X-Correlation-Id must be present
        Assert.True(response.Headers.Contains("X-Correlation-Id"),
            "Response should contain X-Correlation-Id header");

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        // ProblemDetails standard fields
        Assert.True(doc.RootElement.TryGetProperty("status", out _),
            "ProblemDetails must contain 'status' field");

        // Extended fields: correlationId and domainCode
        Assert.True(doc.RootElement.TryGetProperty("correlationId", out var correlationIdProp),
            "ProblemDetails must contain 'correlationId' extension");
        Assert.False(string.IsNullOrEmpty(correlationIdProp.GetString()),
            "correlationId must not be empty");

        Assert.True(doc.RootElement.TryGetProperty("domainCode", out _),
            "ProblemDetails must contain 'domainCode' extension");
    }

    [Fact]
    public async Task GetNonExistentRoute_CorrelationIdHeaderMatchesBodyCorrelationId()
    {
        var response = await _client.GetAsync("/api/v1/ruta-inexistente");

        var headerCorrelationId = response.Headers.GetValues("X-Correlation-Id").FirstOrDefault();
        Assert.NotNull(headerCorrelationId);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var bodyCorrelationId = doc.RootElement.GetProperty("correlationId").GetString();

        Assert.Equal(headerCorrelationId, bodyCorrelationId);
    }
}
