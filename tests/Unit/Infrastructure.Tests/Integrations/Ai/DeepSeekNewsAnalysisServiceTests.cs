using System.Net;
using Application.News;
using Domain.News;
using Infrastructure.Integrations.Ai;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Infrastructure.Tests.Integrations.Ai;

public class DeepSeekNewsAnalysisServiceTests
{
    private const string ValidAnalysisJson = """
        {
          "choices": [
            {
              "message": {
                "content": "{\"isRelevant\":true,\"relevanceReason\":\"Relevante\",\"headline\":null,\"impact\":\"alto\",\"sectorTags\":[\"retail\"],\"subsector\":null,\"affectedFibers\":[\"FIBRAMQ\"],\"keyFacts\":[\"Hecho 1\"],\"keyFigures\":[],\"summaryMarkdown\":\"Resumen de análisis.\",\"investorTakeaway\":\"Conclusión directa.\",\"confidence\":0.9,\"extractionNotes\":null}"
              }
            }
          ]
        }
        """;

    [Fact]
    public async Task GenerateAnalysisAsync_ReturnsNull_WhenApiKeyNotConfigured()
    {
        var handler = new StubHttpMessageHandler(_ => throw new InvalidOperationException("Should not call API"));
        var service = CreateService(handler, new Dictionary<string, string?> { ["DeepSeek:ApiKey"] = "" });

        var result = await service.GenerateAnalysisAsync("Título", "Snippet", null);

        Assert.Null(result);
    }

    [Fact]
    public async Task GenerateAnalysisAsync_ReturnsAnalysis_WhenResponseIsValid()
    {
        var handler = new StubHttpMessageHandler(_ => Task.FromResult(CreateJsonResponse(ValidAnalysisJson)));
        var service = CreateService(handler);

        var result = await service.GenerateAnalysisAsync("FIBRAMQ noticias", null, null);

        Assert.NotNull(result);
        Assert.True(result.IsRelevant);
        Assert.Equal("alto", result.Impact);
        Assert.Equal("Resumen de análisis.", result.SummaryMarkdown);
        Assert.Contains("FIBRAMQ", result.AffectedFibers);
    }

    [Fact]
    public async Task GenerateAnalysisAsync_ThrowsConfigurationException_WhenCredentialRejected()
    {
        var handler = new StubHttpMessageHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent("{\"error\":{\"message\":\"Invalid API key\"}}"),
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
              "choices": [
                {
                  "message": { "content": "esto no es json {{{" }
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
        string modelId = "deepseek-v4-flash")
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings ?? new Dictionary<string, string?> { ["DeepSeek:ApiKey"] = "test-key" })
            .Build();

        return new DeepSeekNewsAnalysisService(
            new HttpClient(handler),
            configuration,
            new FakeAiProviderConfigRepo(modelId),
            new FakeAiPromptRepo(),
            NullLogger<DeepSeekNewsAnalysisService>.Instance);
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
            => Task.FromResult(new AiProviderConfig { Id = 1, Provider = AiProvider.DeepSeek, ModelId = modelId });
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
