using Application.News;
using Domain.News;
using Infrastructure.Integrations.Ai;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Infrastructure.Tests.Integrations.Ai;

public class RoutingAiSummaryServiceTests
{
    [Fact]
    public async Task GenerateSummaryAsync_DelegatesToGemini_WhenProviderIsGemini()
    {
        var geminiCalled = false;
        var geminiHandler = new StubHandler(() =>
        {
            geminiCalled = true;
            return BuildGeminiResponse("Resumen Gemini.");
        });

        var fakeRepo = new FakeProviderRepo(AiProvider.Gemini, "gemini-2.5-flash");
        var gemini = BuildGemini(geminiHandler, fakeRepo);
        var deepSeek = BuildDeepSeek(new StubHandler(_ => throw new Exception("DeepSeek no debería ser llamado")), fakeRepo);

        var sut = new RoutingAiSummaryService(gemini, deepSeek, fakeRepo);

        var result = await sut.GenerateSummaryAsync("Título", "Snippet", "Cuerpo largo suficiente para pasar el quality gate y tener al menos cinco oraciones. Más texto aquí para pasar. Y más texto. Y aún más. Final.");

        Assert.True(geminiCalled);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GenerateSummaryAsync_DelegatesToDeepSeek_WhenProviderIsDeepSeek()
    {
        var deepSeekCalled = false;
        var deepSeekHandler = new StubHandler(() =>
        {
            deepSeekCalled = true;
            return BuildDeepSeekResponse("Resumen DeepSeek completo con suficiente texto para pasar el quality gate mínimo.");
        });

        var fakeRepo = new FakeProviderRepo(AiProvider.DeepSeek, "deepseek-chat");
        var gemini = BuildGemini(new StubHandler(_ => throw new Exception("Gemini no debería ser llamado")), fakeRepo);
        var deepSeek = BuildDeepSeek(deepSeekHandler, fakeRepo);

        var sut = new RoutingAiSummaryService(gemini, deepSeek, fakeRepo);

        var result = await sut.GenerateSummaryAsync("Título", "Snippet");

        Assert.True(deepSeekCalled);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GenerateSummaryAsync_ThrowsAiProviderConfigurationException_WhenProviderIsUnknown()
    {
        var fakeRepo = new FakeProviderRepo((AiProvider)99, "unknown-model");
        var gemini = BuildGemini(new StubHandler(_ => Task.FromResult(new System.Net.Http.HttpResponseMessage())), fakeRepo);
        var deepSeek = BuildDeepSeek(new StubHandler(_ => Task.FromResult(new System.Net.Http.HttpResponseMessage())), fakeRepo);

        var sut = new RoutingAiSummaryService(gemini, deepSeek, fakeRepo);

        await Assert.ThrowsAsync<AiProviderConfigurationException>(() =>
            sut.GenerateSummaryAsync("Título", null));
    }

    private static GeminiAiSummaryService BuildGemini(HttpMessageHandler handler, IAiProviderConfigRepository repo)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Gemini:ApiKey"] = "test-key" })
            .Build();
        return new GeminiAiSummaryService(
            new System.Net.Http.HttpClient(handler),
            config,
            repo,
            new FakePromptRepo(),
            NullLogger<GeminiAiSummaryService>.Instance);
    }

    private static DeepSeekAiSummaryService BuildDeepSeek(HttpMessageHandler handler, IAiProviderConfigRepository repo)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["DeepSeek:ApiKey"] = "test-key" })
            .Build();
        return new DeepSeekAiSummaryService(
            new System.Net.Http.HttpClient(handler),
            config,
            repo,
            new FakePromptRepo(),
            NullLogger<DeepSeekAiSummaryService>.Instance);
    }

    private static System.Net.Http.HttpResponseMessage BuildGeminiResponse(string text)
    {
        var json = $$$"""
        {
            "candidates": [{
                "content": {
                    "parts": [{ "text": "{{{text}}}" }]
                }
            }]
        }
        """;
        return new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new System.Net.Http.StringContent(json),
        };
    }

    private static System.Net.Http.HttpResponseMessage BuildDeepSeekResponse(string text)
    {
        var json = $$$"""
        {
            "choices": [{
                "message": { "content": "{{{text}}}" }
            }]
        }
        """;
        return new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new System.Net.Http.StringContent(json),
        };
    }

    private sealed class FakeProviderRepo(AiProvider provider, string modelId) : IAiProviderConfigRepository
    {
        public Task<AiProviderConfig> GetConfigAsync(CancellationToken ct = default)
            => Task.FromResult(new AiProviderConfig { Id = 1, Provider = provider, ModelId = modelId });

        public Task SetProviderAsync(AiProvider p, string m, string actor, CancellationToken ct = default)
            => Task.CompletedTask;
    }

    private sealed class FakePromptRepo : IAiPromptRepository
    {
        public Task<AiPrompt?> GetPromptAsync(string contentType, CancellationToken ct = default)
            => Task.FromResult<AiPrompt?>(null);

        public Task SetPromptAsync(string contentType, string template, string actor, CancellationToken ct = default)
            => Task.CompletedTask;
    }

    private sealed class StubHandler(Func<System.Net.Http.HttpRequestMessage, Task<System.Net.Http.HttpResponseMessage>> factory)
        : System.Net.Http.HttpMessageHandler
    {
        public StubHandler(Func<System.Net.Http.HttpResponseMessage> factory)
            : this(_ => Task.FromResult(factory())) { }

        protected override Task<System.Net.Http.HttpResponseMessage> SendAsync(
            System.Net.Http.HttpRequestMessage request, CancellationToken cancellationToken)
            => factory(request);
    }
}
