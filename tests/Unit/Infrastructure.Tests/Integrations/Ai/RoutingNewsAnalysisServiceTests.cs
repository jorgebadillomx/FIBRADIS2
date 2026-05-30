using Application.Ai;
using Application.News;
using Domain.Ai;
using Domain.News;
using Infrastructure.Integrations.Ai;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Infrastructure.Tests.Integrations.Ai;

public class RoutingNewsAnalysisServiceTests
{
    private const string GeminiAnalysisJson = """
        {
            "candidates": [{
                "content": {
                    "parts": [{ "text": "{\"isRelevant\":true,\"relevanceReason\":\"Relevante\",\"headline\":null,\"impact\":\"medio\",\"sectorTags\":[],\"subsector\":null,\"affectedFibers\":[],\"keyFacts\":[],\"keyFigures\":[],\"summaryMarkdown\":\"Resumen Gemini.\",\"investorTakeaway\":null,\"confidence\":0.9,\"extractionNotes\":null}" }]
                }
            }]
        }
        """;

    private const string DeepSeekAnalysisJson = """
        {
            "choices": [{
                "message": { "content": "{\"isRelevant\":true,\"relevanceReason\":\"Relevante\",\"headline\":null,\"impact\":\"bajo\",\"sectorTags\":[],\"subsector\":null,\"affectedFibers\":[],\"keyFacts\":[],\"keyFigures\":[],\"summaryMarkdown\":\"Resumen DeepSeek.\",\"investorTakeaway\":null,\"confidence\":0.8,\"extractionNotes\":null}" }
            }]
        }
        """;

    [Fact]
    public async Task GenerateAnalysisAsync_DelegatesToGemini_WhenProviderIsGemini()
    {
        var geminiCalled = false;
        var geminiHandler = new StubHandler(() =>
        {
            geminiCalled = true;
            return new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new System.Net.Http.StringContent(GeminiAnalysisJson),
            };
        });
        var fakeRepo = new FakeProviderRepo(AiProvider.Gemini, "gemini-2.5-flash");
        var gemini = BuildGemini(geminiHandler, fakeRepo);
        var deepSeek = BuildDeepSeek(new StubHandler(_ => throw new Exception("DeepSeek no debe llamarse")), fakeRepo);

        var sut = new RoutingNewsAnalysisService(gemini, deepSeek, fakeRepo, new NullCallLogRepo());
        var result = await sut.GenerateAnalysisAsync("Título", "Snippet", null);

        Assert.True(geminiCalled);
        Assert.NotNull(result);
        Assert.Equal("Resumen Gemini.", result.SummaryMarkdown);
        Assert.Equal("medio", result.Impact);
    }

    [Fact]
    public async Task GenerateAnalysisAsync_DelegatesToDeepSeek_WhenProviderIsDeepSeek()
    {
        var deepSeekCalled = false;
        var deepSeekHandler = new StubHandler(() =>
        {
            deepSeekCalled = true;
            return new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new System.Net.Http.StringContent(DeepSeekAnalysisJson),
            };
        });
        var fakeRepo = new FakeProviderRepo(AiProvider.DeepSeek, "deepseek-v4-flash");
        var gemini = BuildGemini(new StubHandler(_ => throw new Exception("Gemini no debe llamarse")), fakeRepo);
        var deepSeek = BuildDeepSeek(deepSeekHandler, fakeRepo);

        var sut = new RoutingNewsAnalysisService(gemini, deepSeek, fakeRepo, new NullCallLogRepo());
        var result = await sut.GenerateAnalysisAsync("Título", null, null);

        Assert.True(deepSeekCalled);
        Assert.NotNull(result);
        Assert.Equal("Resumen DeepSeek.", result.SummaryMarkdown);
    }

    [Fact]
    public async Task GenerateAnalysisAsync_LogsOperation_WithNewsAnalysisName()
    {
        var spy = new SpyCallLogRepo();
        var fakeRepo = new FakeProviderRepo(AiProvider.Gemini, "gemini-2.5-flash");
        var handler = new StubHandler(() => new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new System.Net.Http.StringContent(GeminiAnalysisJson),
        });
        var gemini = BuildGemini(handler, fakeRepo);
        var deepSeek = BuildDeepSeek(new StubHandler(_ => throw new Exception("no debe llamarse")), fakeRepo);

        var sut = new RoutingNewsAnalysisService(gemini, deepSeek, fakeRepo, spy);
        await sut.GenerateAnalysisAsync("Título", "Snippet", null);

        Assert.Single(spy.Logged);
        Assert.True(spy.Logged[0].Success);
        Assert.Equal("NewsAnalysis", spy.Logged[0].Operation);
        Assert.Equal("Gemini", spy.Logged[0].Provider);
    }

    private static GeminiNewsAnalysisService BuildGemini(HttpMessageHandler handler, IAiProviderConfigRepository repo)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Gemini:ApiKey"] = "test-key" })
            .Build();
        return new GeminiNewsAnalysisService(
            new System.Net.Http.HttpClient(handler),
            config, repo, new FakePromptRepo(),
            NullLogger<GeminiNewsAnalysisService>.Instance);
    }

    private static DeepSeekNewsAnalysisService BuildDeepSeek(HttpMessageHandler handler, IAiProviderConfigRepository repo)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["DeepSeek:ApiKey"] = "test-key" })
            .Build();
        return new DeepSeekNewsAnalysisService(
            new System.Net.Http.HttpClient(handler),
            config, repo, new FakePromptRepo(),
            NullLogger<DeepSeekNewsAnalysisService>.Instance);
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
