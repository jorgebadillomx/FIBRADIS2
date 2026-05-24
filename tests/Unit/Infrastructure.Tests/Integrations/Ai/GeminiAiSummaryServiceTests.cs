using System.Net;
using System.Text.Json;
using Application.News;
using Domain.News;
using Infrastructure.Integrations.Ai;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Infrastructure.Tests.Integrations.Ai;

public class GeminiAiSummaryServiceTests
{
    [Fact]
    public async Task GenerateSummaryAsync_ReturnsNull_WhenApiKeyNotConfigured()
    {
        var handler = new StubHttpMessageHandler(_ => throw new InvalidOperationException("Should not call API"));
        var service = CreateService(handler, new Dictionary<string, string?>
        {
            ["Gemini:ApiKey"] = "",
        });

        var summary = await service.GenerateSummaryAsync("Titulo", "Snippet");

        Assert.Null(summary);
    }

    [Fact]
    public async Task GenerateSummaryAsync_Throws_WhenCandidatesEmpty()
    {
        var handler = new StubHttpMessageHandler(_ => Task.FromResult(CreateJsonResponse("""
            {
              "candidates": []
            }
            """)));
        var service = CreateService(handler);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GenerateSummaryAsync("Titulo", "Snippet"));
    }

    [Fact]
    public async Task GenerateSummaryAsync_Throws_WhenCandidateHasNoContent()
    {
        var handler = new StubHttpMessageHandler(_ => Task.FromResult(CreateJsonResponse("""
            {
              "candidates": [
                { "finishReason": "STOP" }
              ]
            }
            """)));
        var service = CreateService(handler);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GenerateSummaryAsync("Titulo", "Snippet"));
    }

    [Fact]
    public async Task GenerateSummaryAsync_Throws_WhenCandidateTextIsMissing()
    {
        var handler = new StubHttpMessageHandler(_ => Task.FromResult(CreateJsonResponse("""
            {
              "candidates": [
                {
                  "content": {
                    "parts": [
                      { "inlineData": { "mimeType": "text/plain", "data": "Zm9v" } }
                    ]
                  },
                  "finishReason": "SAFETY"
                }
              ],
              "promptFeedback": {
                "blockReason": "SAFETY"
              }
            }
            """)));
        var service = CreateService(handler);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GenerateSummaryAsync("Titulo", "Snippet"));
    }

    [Fact]
    public async Task GenerateSummaryAsync_ReturnsText_WhenResponseIsValid()
    {
        var handler = new StubHttpMessageHandler(_ => Task.FromResult(CreateJsonResponse("""
            {
              "candidates": [
                {
                      "content": {
                        "parts": [
                      { "text": "Fibra Danhos reportó una actualización operativa relevante para sus inversionistas. La noticia refuerza la lectura de estabilidad en generación de flujo dentro del segmento inmobiliario mexicano." }
                        ]
                      }
                    }
              ]
            }
            """)));
        var service = CreateService(handler);

        var summary = await service.GenerateSummaryAsync("Titulo", "Snippet");

        Assert.Contains("actualización operativa relevante", summary);
    }

    [Fact]
    public async Task GenerateSummaryAsync_Retries_WhenFirstSummaryIsTooShort()
    {
        var responses = new Queue<HttpResponseMessage>([
            CreateJsonResponse("""
                {
                  "candidates": [
                    {
                      "content": {
                        "parts": [{ "text": "Fibra Danhos anunció el pago de" }]
                      }
                    }
                  ]
                }
                """),
            CreateJsonResponse("""
                {
                  "candidates": [
                    {
                      "content": {
                        "parts": [
                          {
                            "text": "Fibra Danhos anunció el pago de un dividendo para sus tenedores, reforzando su política de distribución de efectivo. La noticia es relevante porque confirma estabilidad operativa y capacidad de generación de flujo dentro del segmento inmobiliario mexicano. Para el inversionista, esto sugiere continuidad en retornos y disciplina financiera en el corto plazo."
                          }
                        ]
                      }
                    }
                  ]
                }
                """),
        ]);

        var handler = new StubHttpMessageHandler(_ => Task.FromResult(responses.Dequeue()));
        var service = CreateService(handler);

        var summary = await service.GenerateSummaryAsync("Titulo", "Snippet");

        Assert.NotNull(summary);
        Assert.Contains("disciplina financiera", summary);
        Assert.True(summary.Length >= 180);
    }

    [Fact]
    public async Task GenerateSummaryAsync_ReturnsLongerRetry_WhenFirstSummaryDoesNotEndCleanly()
    {
        var responses = new Queue<HttpResponseMessage>([
            CreateJsonResponse("""
                {
                  "candidates": [
                    {
                      "content": {
                        "parts": [
                          {
                            "text": "Fibra Danhos reportó avances relevantes para su estrategia de ingresos recurrentes en activos comerciales y de oficinas"
                          }
                        ]
                      }
                    }
                  ]
                }
                """),
            CreateJsonResponse("""
                {
                  "candidates": [
                    {
                      "content": {
                        "parts": [
                          {
                            "text": "Fibra Danhos reportó avances relevantes para su estrategia de ingresos recurrentes en activos comerciales y de oficinas. La actualización refuerza la lectura de resiliencia operativa dentro del mercado inmobiliario mexicano. Para el inversionista, esto apunta a una ejecución consistente y mejor visibilidad sobre la generación de flujo."
                          }
                        ]
                      }
                    }
                  ]
                }
                """),
        ]);

        var handler = new StubHttpMessageHandler(_ => Task.FromResult(responses.Dequeue()));
        var service = CreateService(handler);

        var summary = await service.GenerateSummaryAsync("Titulo", "Snippet");

        Assert.NotNull(summary);
        Assert.EndsWith(".", summary);
        Assert.True(summary.Length >= 180);
    }

    [Fact]
    public async Task GenerateSummaryAsync_ForLongBody_ThrowsWhenProStillReturnsIncompleteText()
    {
        var requestedUrls = new List<string>();
        var responses = new Queue<HttpResponseMessage>([
            CreateJsonResponse("""
                {
                  "candidates": [
                    {
                      "content": {
                        "parts": [
                          {
                            "text": "Fibra Danhos anunció una distribución relevante para sus inversionistas"
                          }
                        ]
                      }
                    }
                  ]
                }
                """),
            CreateJsonResponse("""
                {
                  "candidates": [
                    {
                      "content": {
                        "parts": [
                          {
                            "text": "Fibra Danhos anunció una distribución relevante para sus inversionistas y confirmó que mantiene una posición operativa sólida en su portafolio comercial"
                          }
                        ]
                      }
                    }
                  ]
                }
                """),
        ]);

        var handler = new StubHttpMessageHandler(request =>
        {
            requestedUrls.Add(request.RequestUri!.ToString());
            return Task.FromResult(responses.Dequeue());
        });
        var service = CreateService(handler, modelId: "gemini-2.5-pro");

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GenerateSummaryAsync("Titulo", "Snippet", new string('A', 3000)));

        Assert.Equal(2, requestedUrls.Count);
        Assert.Contains("gemini-2.5-pro", requestedUrls[0]);
        Assert.Contains("gemini-2.5-pro", requestedUrls[1]);
    }

    [Fact]
    public async Task GenerateSummaryAsync_ThrowsConfigurationException_WhenProviderRejectsApiKey()
    {
        var handler = new StubHttpMessageHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Forbidden)
        {
            Content = new StringContent("""
                {
                  "error": {
                    "code": 403,
                    "message": "Your API key was reported as leaked. Please use another API key.",
                    "status": "PERMISSION_DENIED"
                  }
                }
                """),
        }));
        var service = CreateService(handler);

        var ex = await Assert.ThrowsAsync<AiProviderConfigurationException>(() => service.GenerateSummaryAsync("Titulo", "Snippet"));

        Assert.Contains("Gemini:ApiKey", ex.Message);
    }

    [Fact]
    public async Task GenerateSummaryAsync_UsesModelFromRepository_ForNewsContent()
    {
        string? capturedUrl = null;
        var handler = new StubHttpMessageHandler(request =>
        {
            capturedUrl = request.RequestUri?.ToString();
            return Task.FromResult(CreateJsonResponse("""
                {
                  "candidates": [
                    {
                      "content": {
                        "parts": [{ "text": "Fibra Danhos reportó resultados operativos favorables. La actualización mantiene una lectura constructiva para el inversionista por la visibilidad sobre flujo y ocupación." }]
                      }
                    }
                  ]
                }
                """));
        });
        var service = CreateService(handler, modelId: "gemini-2.5-pro");

        await service.GenerateSummaryAsync("Titulo", "Snippet", contentType: AiContentType.News);

        Assert.NotNull(capturedUrl);
        Assert.Contains("gemini-2.5-pro", capturedUrl);
    }

    [Fact]
    public async Task GenerateSummaryAsync_UsesModelFromRepository_ForDocumentContent()
    {
        string? capturedUrl = null;
        var handler = new StubHttpMessageHandler(request =>
        {
            capturedUrl = request.RequestUri?.ToString();
            return Task.FromResult(CreateJsonResponse("""
                {
                  "candidates": [
                    {
                      "content": {
                        "parts": [{ "text": "El documento presenta una actualización material sobre activos, flujos y perspectivas operativas. Para el análisis financiero, aporta contexto suficiente para evaluar riesgos y continuidad en distribución." }]
                      }
                    }
                  ]
                }
                """));
        });
        var service = CreateService(handler, modelId: "gemini-2.5-pro");

        await service.GenerateSummaryAsync("Titulo documento", null, contentType: AiContentType.Document);

        Assert.NotNull(capturedUrl);
        Assert.Contains("gemini-2.5-pro", capturedUrl);
    }

    [Fact]
    public async Task GenerateSummaryAsync_UsesCustomModelFromRepository()
    {
        string? capturedUrl = null;
        var handler = new StubHttpMessageHandler(request =>
        {
            capturedUrl = request.RequestUri?.ToString();
            return Task.FromResult(CreateJsonResponse("""
                {
                  "candidates": [
                    {
                      "content": {
                        "parts": [{ "text": "Fibra Danhos informó una novedad relevante para el mercado. El contenido sugiere una implicación concreta para valuación, distribución o flujo del portafolio." }]
                      }
                    }
                  ]
                }
                """));
        });
        var service = CreateService(handler, modelId: "gemini-custom-model");

        await service.GenerateSummaryAsync("Titulo", "Snippet", contentType: AiContentType.News);

        Assert.NotNull(capturedUrl);
        Assert.Contains("gemini-custom-model", capturedUrl);
    }

    [Fact]
    public async Task GenerateSummaryAsync_UsesPromptTemplateFromRepository_WhenAvailable()
    {
        string? capturedPrompt = null;
        var handler = new StubHttpMessageHandler(async request =>
        {
            using var doc = await JsonDocument.ParseAsync(await request.Content!.ReadAsStreamAsync());
            capturedPrompt = doc.RootElement.GetProperty("contents")[0].GetProperty("parts")[0].GetProperty("text").GetString();
            return CreateJsonResponse("""
                {
                  "candidates": [
                    {
                      "content": {
                        "parts": [{ "text": "Resumen suficientemente largo para pasar el control de calidad. Segunda oración completa aquí. Tercera oración completa aquí." }]
                      }
                    }
                  ]
                }
                """);
        });
        var service = CreateService(handler, promptTemplate: "Plantilla custom\nTítulo: {title}\n{snippet_section}\n{body_section}\n{strictness_instruction}");

        await service.GenerateSummaryAsync("Titulo", "Snippet");

        Assert.NotNull(capturedPrompt);
        Assert.Contains("Plantilla custom", capturedPrompt);
        Assert.Contains("Título: Titulo", capturedPrompt);
    }

    [Fact]
    public async Task GenerateSummaryAsync_UsesFallbackPrompt_WhenRepositoryReturnsNull()
    {
        string? capturedPrompt = null;
        var handler = new StubHttpMessageHandler(async request =>
        {
            using var doc = await JsonDocument.ParseAsync(await request.Content!.ReadAsStreamAsync());
            capturedPrompt = doc.RootElement.GetProperty("contents")[0].GetProperty("parts")[0].GetProperty("text").GetString();
            return CreateJsonResponse("""
                {
                  "candidates": [
                    {
                      "content": {
                        "parts": [{ "text": "Resumen suficientemente largo para pasar el control de calidad. Segunda oración completa aquí. Tercera oración completa aquí." }]
                      }
                    }
                  ]
                }
                """);
        });
        var service = CreateService(handler);

        await service.GenerateSummaryAsync("Titulo", "Snippet");

        Assert.NotNull(capturedPrompt);
        Assert.Contains("Eres un analista experto en FIBRAs mexicanas", capturedPrompt);
    }

    private static IAiSummaryService CreateService(
        HttpMessageHandler handler,
        IReadOnlyDictionary<string, string?>? settings = null,
        string modelId = "gemini-2.5-flash",
        string? promptTemplate = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings ?? new Dictionary<string, string?>
            {
                ["Gemini:ApiKey"] = "test-key",
            })
            .Build();

        return new GeminiAiSummaryService(
            new HttpClient(handler),
            configuration,
            new FakeAiProviderConfigRepository(modelId),
            new FakeAiPromptRepository(promptTemplate),
            NullLogger<GeminiAiSummaryService>.Instance);
    }

    private static HttpResponseMessage CreateJsonResponse(string json)
        => new(HttpStatusCode.OK)
        {
            Content = new StringContent(json),
        };

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> responseFactory)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => responseFactory(request);
    }

    internal sealed class FakeAiProviderConfigRepository(string modelId) : IAiProviderConfigRepository
    {
        public Task<AiProviderConfig> GetConfigAsync(CancellationToken ct = default)
            => Task.FromResult(new AiProviderConfig { Id = 1, Provider = AiProvider.Gemini, ModelId = modelId });

        public Task SetProviderAsync(AiProvider provider, string modelId, string actor, CancellationToken ct = default)
            => Task.CompletedTask;
    }

    internal sealed class FakeAiPromptRepository(string? template = null) : IAiPromptRepository
    {
        public Task<AiPrompt?> GetPromptAsync(string contentType, CancellationToken ct = default)
            => Task.FromResult(template is null
                ? null
                : new AiPrompt
                {
                    Id = 1,
                    ContentType = contentType,
                    PromptTemplate = template,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    UpdatedBy = "test",
                });

        public Task SetPromptAsync(string contentType, string template, string actor, CancellationToken ct = default)
            => Task.CompletedTask;
    }
}
