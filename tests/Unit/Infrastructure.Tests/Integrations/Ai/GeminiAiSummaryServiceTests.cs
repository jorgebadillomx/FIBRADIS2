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
        var service = CreateService(handler);

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
    public async Task GenerateSummaryAsync_UsesProModel_ForNewsContent()
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
        var service = CreateService(handler);

        await service.GenerateSummaryAsync("Titulo", "Snippet", contentType: AiContentType.News);

        Assert.NotNull(capturedUrl);
        Assert.Contains("gemini-2.5-pro", capturedUrl);
    }

    [Fact]
    public async Task GenerateSummaryAsync_UsesProModel_ForDocumentContent()
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
        var service = CreateService(handler);

        await service.GenerateSummaryAsync("Titulo documento", null, contentType: AiContentType.Document);

        Assert.NotNull(capturedUrl);
        Assert.Contains("gemini-2.5-pro", capturedUrl);
    }

    [Fact]
    public async Task GenerateSummaryAsync_UsesConfiguredNewsModel_WhenOverridden()
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
        var service = CreateService(handler, new Dictionary<string, string?>
        {
            ["Gemini:ApiKey"] = "test-key",
            ["Gemini:NewsModel"] = "gemini-custom-news-model",
        });

        await service.GenerateSummaryAsync("Titulo", "Snippet", contentType: AiContentType.News);

        Assert.NotNull(capturedUrl);
        Assert.Contains("gemini-custom-news-model", capturedUrl);
    }

    [Fact]
    public async Task GenerateSummaryAsync_UsesDefaultNewsModel_WhenConfiguredValueIsEmpty()
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
        var service = CreateService(handler, new Dictionary<string, string?>
        {
            ["Gemini:ApiKey"] = "test-key",
            ["Gemini:NewsModel"] = "",
        });

        await service.GenerateSummaryAsync("Titulo", "Snippet", contentType: AiContentType.News);

        Assert.NotNull(capturedUrl);
        Assert.Contains("gemini-2.5-pro", capturedUrl);
    }

    [Fact]
    public async Task GenerateSummaryAsync_UsesDefaultDocumentModel_WhenConfiguredValueIsEmpty()
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
                        "parts": [{ "text": "El documento resume información financiera y operativa sustancial. También añade señales suficientes para una lectura analítica de continuidad, riesgos y materialidad para el inversionista." }]
                      }
                    }
                  ]
                }
                """));
        });
        var service = CreateService(handler, new Dictionary<string, string?>
        {
            ["Gemini:ApiKey"] = "test-key",
            ["Gemini:DocumentModel"] = "",
        });

        await service.GenerateSummaryAsync("Titulo", null, contentType: AiContentType.Document);

        Assert.NotNull(capturedUrl);
        Assert.Contains("gemini-2.5-pro", capturedUrl);
    }

    private static IAiSummaryService CreateService(
        HttpMessageHandler handler,
        IReadOnlyDictionary<string, string?>? settings = null)
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
}
