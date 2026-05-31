using System.Net;
using Application.Ai;
using Application.Fundamentals;
using Application.News;
using Domain.Ai;
using Domain.News;
using Infrastructure.Integrations.Ai;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Infrastructure.Tests.Integrations.Ai;

public class RoutingKpiExtractorServiceTests
{
    [Fact]
    public async Task ExtractAsync_LogsSuccess_WhenGeminiReturnsKpis()
    {
        var spy = new SpyCallLogRepo();
        var fakeRepo = new FakeProviderRepo(AiProvider.Gemini, "gemini-2.5-flash");

        var geminiHandler = new StubHandler(_ => Task.FromResult(BuildGeminiKpiResponse(
            """{"capRate":0.08,"capRateNote":"ok","summary":"Resumen.","extractionNotes":"5 campos."}""")));
        var gemini = BuildGemini(geminiHandler, fakeRepo);
        var deepSeek = BuildDeepSeek(new StubHandler(_ => throw new Exception("no debe llamarse")), fakeRepo);

        var sut = new RoutingKpiExtractorService(gemini, deepSeek, fakeRepo, spy, NullLogger<RoutingKpiExtractorService>.Instance);
        var result = await sut.ExtractAsync("markdown content", CancellationToken.None);

        Assert.Single(spy.Logged);
        Assert.Equal("KpiExtraction", spy.Logged[0].Operation);
        Assert.Equal("Gemini", spy.Logged[0].Provider);
        Assert.True(spy.Logged[0].Success);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task ExtractAsync_LogsFailure_AndRethrows_WhenGeminiThrows()
    {
        var spy = new SpyCallLogRepo();
        var fakeRepo = new FakeProviderRepo(AiProvider.Gemini, "gemini-2.5-flash");

        var geminiHandler = new StubHandler(_ => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("error"),
            }));
        var gemini = BuildGemini(geminiHandler, fakeRepo);
        var deepSeek = BuildDeepSeek(new StubHandler(_ => throw new Exception("no debe llamarse")), fakeRepo);

        var sut = new RoutingKpiExtractorService(gemini, deepSeek, fakeRepo, spy, NullLogger<RoutingKpiExtractorService>.Instance);

        await Assert.ThrowsAnyAsync<Exception>(() => sut.ExtractAsync("markdown", CancellationToken.None));

        Assert.Single(spy.Logged);
        Assert.False(spy.Logged[0].Success);
        Assert.Equal("Gemini", spy.Logged[0].Provider);
        Assert.Equal("KpiExtraction", spy.Logged[0].Operation);
    }

    [Fact]
    public async Task ExtractAsync_LogsSuccess_WhenDeepSeekReturnsKpis()
    {
        var spy = new SpyCallLogRepo();
        var fakeRepo = new FakeProviderRepo(AiProvider.DeepSeek, "deepseek-chat");

        var deepSeekHandler = new StubHandler(_ => Task.FromResult(BuildDeepSeekKpiResponse(
            """{"capRate":0.09,"capRateNote":"ok","summary":"Resumen.","extractionNotes":"ok."}""")));
        var gemini = BuildGemini(new StubHandler(_ => throw new Exception("no debe llamarse")), fakeRepo);
        var deepSeek = BuildDeepSeek(deepSeekHandler, fakeRepo);

        var sut = new RoutingKpiExtractorService(gemini, deepSeek, fakeRepo, spy, NullLogger<RoutingKpiExtractorService>.Instance);
        await sut.ExtractAsync("markdown", CancellationToken.None);

        Assert.Single(spy.Logged);
        Assert.Equal("DeepSeek", spy.Logged[0].Provider);
        Assert.True(spy.Logged[0].Success);
    }

    [Fact]
    public async Task ExtractAsync_ContinuesAndReturnsResult_WhenLogRepoThrows()
    {
        // AC5: logging failure must never propagate to the caller
        var failingRepo = new FailingCallLogRepo();
        var fakeRepo = new FakeProviderRepo(AiProvider.Gemini, "gemini-2.5-flash");

        var geminiHandler = new StubHandler(_ => Task.FromResult(BuildGeminiKpiResponse(
            """{"capRate":0.07,"capRateNote":"ok","summary":"Resumen.","extractionNotes":"ok."}""")));
        var gemini = BuildGemini(geminiHandler, fakeRepo);
        var deepSeek = BuildDeepSeek(new StubHandler(_ => throw new Exception("no debe llamarse")), fakeRepo);

        var sut = new RoutingKpiExtractorService(gemini, deepSeek, fakeRepo, failingRepo, NullLogger<RoutingKpiExtractorService>.Instance);

        // Should not throw even though the log repo always throws
        var result = await sut.ExtractAsync("markdown content", CancellationToken.None);

        Assert.True(result.Success);
        Assert.True(failingRepo.WasCalled);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static HttpResponseMessage BuildGeminiKpiResponse(string jsonContent)
    {
        var body = $$"""
            {
              "candidates": [{
                "content": { "parts": [{ "text": "{{jsonContent.Replace("\"", "\\\"")}}" }] }
              }]
            }
            """;
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) };
    }

    private static HttpResponseMessage BuildDeepSeekKpiResponse(string jsonContent)
    {
        var body = $$"""
            {
              "choices": [{
                "message": { "content": "{{jsonContent.Replace("\"", "\\\"")}}" },
                "finish_reason": "stop"
              }],
              "usage": { "completion_tokens": 100 }
            }
            """;
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) };
    }

    private static GeminiKpiExtractorService BuildGemini(HttpMessageHandler handler, IAiProviderConfigRepository repo)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Gemini:ApiKey"] = "test-key" })
            .Build();
        return new GeminiKpiExtractorService(
            new HttpClient(handler),
            config,
            repo,
            new FakePromptRepo(),
            NullLogger<GeminiKpiExtractorService>.Instance);
    }

    private static DeepSeekKpiExtractorService BuildDeepSeek(HttpMessageHandler handler, IAiProviderConfigRepository repo)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["DeepSeek:ApiKey"] = "test-key" })
            .Build();
        return new DeepSeekKpiExtractorService(
            new HttpClient(handler),
            config,
            repo,
            new FakePromptRepo(),
            NullLogger<DeepSeekKpiExtractorService>.Instance);
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

    private sealed class SpyCallLogRepo : IAiCallLogRepository
    {
        public List<AiCallLog> Logged { get; } = [];
        public Task AddAsync(AiCallLog entry, CancellationToken ct = default) { Logged.Add(entry); return Task.CompletedTask; }
        public Task<(IReadOnlyList<AiCallLog> Items, int Total)> GetPagedAsync(
            string? operation, string? provider, bool? success, int page, int pageSize, CancellationToken ct = default)
            => Task.FromResult<(IReadOnlyList<AiCallLog>, int)>(([], 0));
    }

    private sealed class FailingCallLogRepo : IAiCallLogRepository
    {
        public bool WasCalled { get; private set; }
        public Task AddAsync(AiCallLog entry, CancellationToken ct = default)
        {
            WasCalled = true;
            throw new InvalidOperationException("Simulated log repo failure (AC5 test).");
        }
        public Task<(IReadOnlyList<AiCallLog> Items, int Total)> GetPagedAsync(
            string? operation, string? provider, bool? success, int page, int pageSize, CancellationToken ct = default)
            => Task.FromResult<(IReadOnlyList<AiCallLog>, int)>(([], 0));
    }

    private sealed class StubHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> factory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => factory(request);
    }
}
