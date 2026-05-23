using System.Net;
using System.Text.Json;
using Application.News;
using Domain.News;
using Infrastructure.Integrations.Ai;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Infrastructure.Tests.Integrations.Ai;

public class DeepSeekAiSummaryServiceTests
{
    [Fact]
    public async Task GenerateSummaryAsync_ReturnsNull_WhenApiKeyNotConfigured()
    {
        var sut = CreateService(new StubHandler(_ => throw new Exception("No debería llamarse")),
            settings: new Dictionary<string, string?> { ["DeepSeek:ApiKey"] = "" });

        var result = await sut.GenerateSummaryAsync("Título", "Snippet");

        Assert.Null(result);
    }

    [Fact]
    public async Task GenerateSummaryAsync_ThrowsAiProviderConfigurationException_WhenCredentialRejected()
    {
        var sut = CreateService(new StubHandler(_ => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent("{\"error\":\"invalid_api_key\"}"),
            })));

        await Assert.ThrowsAsync<AiProviderConfigurationException>(() =>
            sut.GenerateSummaryAsync("Título", "Snippet"));
    }

    [Fact]
    public async Task GenerateSummaryAsync_ReturnsText_WhenResponseIsValid()
    {
        const string expectedText = "Resumen analítico completo con suficientes oraciones para pasar el quality gate. Segunda oración aquí. Tercera oración.";
        var sut = CreateService(new StubHandler(_ => Task.FromResult(BuildResponse(expectedText))));

        var result = await sut.GenerateSummaryAsync("Título", "Snippet");

        Assert.NotNull(result);
        Assert.Contains("Resumen", result);
    }

    [Fact]
    public async Task GenerateSummaryAsync_ThrowsInvalidOperationException_WhenResponseHasNoChoices()
    {
        var sut = CreateService(new StubHandler(_ => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"choices\":[]}"),
            })));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.GenerateSummaryAsync("Título", "Snippet"));
    }

    [Fact]
    public async Task GenerateSummaryAsync_UsesDbModelForNews()
    {
        string? capturedModel = null;
        var sut = CreateService(new StubHandler(async request =>
        {
            using var doc = await JsonDocument.ParseAsync(await request.Content!.ReadAsStreamAsync());
            capturedModel = doc.RootElement.GetProperty("model").GetString();
            return BuildResponse("Resumen analítico completo con suficientes oraciones. Segunda oración aquí. Tercera oración.");
        }), modelId: "deepseek-v4-flash");

        await sut.GenerateSummaryAsync("Título", "Snippet", contentType: AiContentType.News);

        Assert.Equal("deepseek-v4-flash", capturedModel);
    }

    [Fact]
    public async Task GenerateSummaryAsync_UsesDefaultDocumentModelForDocuments()
    {
        string? capturedModel = null;
        var sut = CreateService(new StubHandler(async request =>
        {
            using var doc = await JsonDocument.ParseAsync(await request.Content!.ReadAsStreamAsync());
            capturedModel = doc.RootElement.GetProperty("model").GetString();
            return BuildResponse("El documento presenta una actualización material sobre activos, flujos y perspectivas. Segunda oración. Tercera oración.");
        }), modelId: "deepseek-v4-flash");

        await sut.GenerateSummaryAsync("Título documento", null, contentType: AiContentType.Document);

        Assert.Equal("deepseek-v4-pro", capturedModel);
    }

    private static DeepSeekAiSummaryService CreateService(
        HttpMessageHandler handler,
        IReadOnlyDictionary<string, string?>? settings = null,
        string modelId = "deepseek-v4-flash")
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings ?? new Dictionary<string, string?>
            {
                ["DeepSeek:ApiKey"] = "test-key",
            })
            .Build();

        var fakeRepo = new FakeProviderConfigRepository(modelId);

        return new DeepSeekAiSummaryService(
            new HttpClient(handler),
            configuration,
            fakeRepo,
            NullLogger<DeepSeekAiSummaryService>.Instance);
    }

    private static HttpResponseMessage BuildResponse(string text)
    {
        var json = $$$"""
        {
            "choices": [{
                "message": { "content": "{{{text}}}" }
            }]
        }
        """;
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json),
        };
    }

    private sealed class FakeProviderConfigRepository(string modelId) : IAiProviderConfigRepository
    {
        public Task<AiProviderConfig> GetConfigAsync(CancellationToken ct = default)
            => Task.FromResult(new AiProviderConfig { Id = 1, Provider = AiProvider.DeepSeek, ModelId = modelId });

        public Task SetProviderAsync(AiProvider provider, string m, string actor, CancellationToken ct = default)
            => Task.CompletedTask;
    }

    private sealed class StubHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> factory)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => factory(request);
    }
}
