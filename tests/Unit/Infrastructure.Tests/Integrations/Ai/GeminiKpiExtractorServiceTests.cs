using System.Net;
using System.Text.Json;
using Application.Fundamentals;
using Application.News;
using Domain.News;
using Infrastructure.Integrations.Ai;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Infrastructure.Tests.Integrations.Ai;

public class GeminiKpiExtractorServiceTests
{
    [Fact]
    public async Task ExtractAsync_UsesJsonMode_AndParsesResponse()
    {
        string? capturedPrompt = null;
        string? capturedMimeType = null;
        var service = CreateService(new StubHandler(async request =>
        {
            using var doc = await JsonDocument.ParseAsync(await request.Content!.ReadAsStreamAsync());
            capturedPrompt = doc.RootElement.GetProperty("contents")[0].GetProperty("parts")[0].GetProperty("text").GetString();
            capturedMimeType = doc.RootElement.GetProperty("generationConfig").GetProperty("responseMimeType").GetString();

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                    {
                      "candidates": [
                        {
                          "content": {
                            "parts": [
                              {
                                "text": "{\"capRate\":0.0812,\"capRateNote\":\"Cap rate calculado con NOI anualizado.\",\"navPerCbfi\":18.5,\"navPerCbfiNote\":\"NAV por CBFI calculado con patrimonio y CBFIs.\",\"ltv\":0.45,\"ltvNote\":\"LTV calculado con deuda y propiedades.\",\"noiMargin\":null,\"noiMarginNote\":null,\"ffoMargin\":0.58,\"ffoMarginNote\":\"FFO margin calculado con FFO e ingresos.\",\"quarterlyDistribution\":0.52,\"quarterlyDistributionNote\":\"Distribución trimestral declarada.\",\"summary\":\"Resumen válido.\",\"extractionNotes\":\"Se encontraron 5 campos y resumen.\"}"
                              }
                            ]
                          }
                        }
                      ]
                    }
                    """),
            };
        }));

        var result = await service.ExtractAsync("FUNO11 reportó cap rate 8.12%", CancellationToken.None);

        Assert.Equal("application/json", capturedMimeType);
        Assert.NotNull(capturedPrompt);
        Assert.Contains("FUNO11", capturedPrompt);
        Assert.True(result.Success);
        Assert.Equal(0.0812m, result.CapRate);
        Assert.NotNull(result.CapRateNote);
        Assert.Equal(18.5m, result.NavPerCbfi);
        Assert.NotNull(result.NavPerCbfiNote);
        Assert.Equal(0.45m, result.Ltv);
        Assert.Equal(0.58m, result.FfoMargin);
        Assert.NotNull(result.FfoMarginNote);
        Assert.Equal(0.52m, result.QuarterlyDistribution);
        Assert.NotNull(result.QuarterlyDistributionNote);
        Assert.Equal("Resumen válido.", result.Summary);
    }

    [Fact]
    public async Task ExtractAsync_ReturnsFailedResult_WhenJsonIsInvalid()
    {
        var service = CreateService(new StubHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""
                {
                  "candidates": [
                    {
                      "content": {
                        "parts": [
                          { "text": "respuesta no válida" }
                        ]
                      }
                    }
                  ]
                }
                """),
        })));

        var result = await service.ExtractAsync("markdown", CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("respuesta inválida", result.ExtractionNotes, StringComparison.OrdinalIgnoreCase);
    }

    private static GeminiKpiExtractorService CreateService(HttpMessageHandler handler)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Gemini:ApiKey"] = "test-key" })
            .Build();

        return new GeminiKpiExtractorService(
            new HttpClient(handler),
            configuration,
            new FakeProviderRepo(),
            new FakePromptRepo(),
            NullLogger<GeminiKpiExtractorService>.Instance);
    }

    private sealed class FakeProviderRepo : IAiProviderConfigRepository
    {
        public Task<AiProviderConfig> GetConfigAsync(CancellationToken ct = default)
            => Task.FromResult(new AiProviderConfig
            {
                Id = 1,
                Provider = AiProvider.Gemini,
                ModelId = "gemini-2.5-flash",
                UpdatedAt = DateTimeOffset.UtcNow,
                UpdatedBy = "test",
            });
        public Task SetProviderAsync(AiProvider provider, string modelId, string actor, CancellationToken ct = default)
            => Task.CompletedTask;
    }

    private sealed class FakePromptRepo : IAiPromptRepository
    {
        public Task<AiPrompt?> GetPromptAsync(string contentType, CancellationToken ct = default)
            => Task.FromResult<AiPrompt?>(null);
        public Task SetPromptAsync(string contentType, string template, string actor, CancellationToken ct = default)
            => Task.CompletedTask;
    }

    private sealed class StubHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> factory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => factory(request);
    }
}
