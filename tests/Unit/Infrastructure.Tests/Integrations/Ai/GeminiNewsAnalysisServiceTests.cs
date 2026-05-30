using System.Net;
using System.Text.Json;
using Application.News;
using Domain.News;
using Infrastructure.Integrations.Ai;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Infrastructure.Tests.Integrations.Ai;

public class GeminiNewsAnalysisServiceTests
{
    private const string ValidAnalysisJson = """
        {
          "candidates": [
            {
              "content": {
                "parts": [
                  {
                    "text": "{\"isRelevant\":true,\"relevanceReason\":\"Relevante para FIBRAs\",\"headline\":null,\"impact\":\"medio\",\"sectorTags\":[\"industrial\"],\"subsector\":null,\"affectedFibers\":[\"FUNO\"],\"keyFacts\":[\"Hecho 1\"],\"keyFigures\":[{\"label\":\"Dist.\",\"valueText\":\"$0.47\",\"importance\":\"alta\"}],\"summaryMarkdown\":\"Resumen analítico.\",\"investorTakeaway\":\"Conclusión.\",\"confidence\":0.85,\"extractionNotes\":null}"
                  }
                ]
              }
            }
          ]
        }
        """;

    [Fact]
    public async Task GenerateAnalysisAsync_ReturnsNull_WhenApiKeyNotConfigured()
    {
        var handler = new StubHttpMessageHandler(_ => throw new InvalidOperationException("Should not call API"));
        var service = CreateService(handler, new Dictionary<string, string?> { ["Gemini:ApiKey"] = "" });

        var result = await service.GenerateAnalysisAsync("Título", "Snippet", null);

        Assert.Null(result);
    }

    [Fact]
    public async Task GenerateAnalysisAsync_ReturnsAnalysis_WhenResponseIsValid()
    {
        var handler = new StubHttpMessageHandler(_ => Task.FromResult(CreateJsonResponse(ValidAnalysisJson)));
        var service = CreateService(handler);

        var result = await service.GenerateAnalysisAsync("FUNO anuncia resultados", "Snippet", null);

        Assert.NotNull(result);
        Assert.True(result.IsRelevant);
        Assert.Equal("medio", result.Impact);
        Assert.Equal("Resumen analítico.", result.SummaryMarkdown);
        Assert.Contains("FUNO", result.AffectedFibers);
    }

    [Fact]
    public async Task GenerateAnalysisAsync_ThrowsConfigurationException_WhenCredentialRejected()
    {
        var handler = new StubHttpMessageHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent("{\"error\":{\"code\":401,\"message\":\"API key not valid.\"}}"),
        }));
        var service = CreateService(handler);

        await Assert.ThrowsAsync<AiProviderConfigurationException>(
            () => service.GenerateAnalysisAsync("Título", null, null));
    }

    [Fact]
    public async Task GenerateAnalysisAsync_ReturnsNull_WhenJsonIsMalformed()
    {
        var badJson = """
            {
              "candidates": [
                {
                  "content": {
                    "parts": [{ "text": "este no es json valido {{{{" }]
                  }
                }
              ]
            }
            """;
        var handler = new StubHttpMessageHandler(_ => Task.FromResult(CreateJsonResponse(badJson)));
        var service = CreateService(handler);

        var result = await service.GenerateAnalysisAsync("Título", null, null);

        Assert.Null(result);
    }

    private static IAiNewsAnalysisService CreateService(
        HttpMessageHandler handler,
        IReadOnlyDictionary<string, string?>? settings = null,
        string modelId = "gemini-2.5-flash")
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings ?? new Dictionary<string, string?> { ["Gemini:ApiKey"] = "test-key" })
            .Build();

        return new GeminiNewsAnalysisService(
            new HttpClient(handler),
            configuration,
            new FakeAiProviderConfigRepo(modelId),
            new FakeAiPromptRepo(),
            NullLogger<GeminiNewsAnalysisService>.Instance);
    }

    private static HttpResponseMessage CreateJsonResponse(string json)
        => new(HttpStatusCode.OK) { Content = new StringContent(json) };

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> factory)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => factory(request);
    }

    internal sealed class FakeAiProviderConfigRepo(string modelId) : IAiProviderConfigRepository
    {
        public Task<AiProviderConfig> GetConfigAsync(CancellationToken ct = default)
            => Task.FromResult(new AiProviderConfig { Id = 1, Provider = AiProvider.Gemini, ModelId = modelId });
        public Task SetProviderAsync(AiProvider provider, string modelId, string actor, CancellationToken ct = default)
            => Task.CompletedTask;
    }

    internal sealed class FakeAiPromptRepo : IAiPromptRepository
    {
        public Task<AiPrompt?> GetPromptAsync(string contentType, CancellationToken ct = default)
            => Task.FromResult<AiPrompt?>(null);
        public Task SetPromptAsync(string contentType, string template, string actor, CancellationToken ct = default)
            => Task.CompletedTask;
    }
}
