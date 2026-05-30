using Application.Ai;
using Application.News;
using Domain.Ai;
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

        var sut = new RoutingAiSummaryService(gemini, deepSeek, fakeRepo, new NullCallLogRepo());

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

        var sut = new RoutingAiSummaryService(gemini, deepSeek, fakeRepo, new NullCallLogRepo());

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

        var sut = new RoutingAiSummaryService(gemini, deepSeek, fakeRepo, new NullCallLogRepo());

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

    [Fact]
    public async Task GenerateSummaryAsync_LogsSuccess_WhenGeminiReturnsText()
    {
        var spy = new SpyCallLogRepo();
        var fakeRepo = new FakeProviderRepo(AiProvider.Gemini, "gemini-2.5-flash");
        // 5+ sentences to pass the quality gate in GeminiAiSummaryService
        var text = "La FIBRA reportó resultados positivos este trimestre. El CAP rate se ubicó en niveles competitivos. Las distribuciones se mantuvieron estables. El portafolio creció de manera orgánica. La ocupación promedio superó el 95 por ciento.";
        var handler = new StubHandler(() => BuildGeminiResponse(text));
        var gemini = BuildGemini(handler, fakeRepo);
        var deepSeek = BuildDeepSeek(new StubHandler(_ => throw new Exception("no debe llamarse")), fakeRepo);

        var sut = new RoutingAiSummaryService(gemini, deepSeek, fakeRepo, spy);
        await sut.GenerateSummaryAsync("Título", "Snippet");

        Assert.Single(spy.Logged);
        Assert.True(spy.Logged[0].Success);
        Assert.Equal("Gemini", spy.Logged[0].Provider);
        Assert.Equal("NewsSummary", spy.Logged[0].Operation);
    }

    [Fact]
    public async Task GenerateSummaryAsync_LogsFailure_AndRethrows_WhenGeminiReturnsHttp500()
    {
        var spy = new SpyCallLogRepo();
        var fakeRepo = new FakeProviderRepo(AiProvider.Gemini, "gemini-2.5-flash");
        var handler = new StubHandler(_ => Task.FromResult(
            new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError)
            {
                Content = new System.Net.Http.StringContent("server error"),
            }));
        var gemini = BuildGemini(handler, fakeRepo);
        var deepSeek = BuildDeepSeek(new StubHandler(_ => throw new Exception("no debe llamarse")), fakeRepo);

        var sut = new RoutingAiSummaryService(gemini, deepSeek, fakeRepo, spy);

        await Assert.ThrowsAnyAsync<Exception>(() => sut.GenerateSummaryAsync("Título", "Snippet"));

        Assert.Single(spy.Logged);
        Assert.False(spy.Logged[0].Success);
        Assert.Equal("Gemini", spy.Logged[0].Provider);
    }

    private sealed class NullCallLogRepo : IAiCallLogRepository
    {
        public Task AddAsync(AiCallLog entry, CancellationToken ct = default) => Task.CompletedTask;
        public Task<(IReadOnlyList<AiCallLog> Items, int Total)> GetPagedAsync(
            string? operation, string? provider, bool? success, int page, int pageSize, CancellationToken ct = default)
            => Task.FromResult<(IReadOnlyList<AiCallLog>, int)>(([], 0));
    }

    private sealed class SpyCallLogRepo : IAiCallLogRepository
    {
        public List<AiCallLog> Logged { get; } = [];
        public Task AddAsync(AiCallLog entry, CancellationToken ct = default) { Logged.Add(entry); return Task.CompletedTask; }
        public Task<(IReadOnlyList<AiCallLog> Items, int Total)> GetPagedAsync(
            string? operation, string? provider, bool? success, int page, int pageSize, CancellationToken ct = default)
            => Task.FromResult<(IReadOnlyList<AiCallLog>, int)>(([], 0));
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
