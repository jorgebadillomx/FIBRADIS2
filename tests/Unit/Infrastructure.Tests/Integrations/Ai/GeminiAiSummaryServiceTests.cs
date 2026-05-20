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
                      { "text": "Resumen generado por Gemini." }
                    ]
                  }
                }
              ]
            }
            """)));
        var service = CreateService(handler);

        var summary = await service.GenerateSummaryAsync("Titulo", "Snippet");

        Assert.Equal("Resumen generado por Gemini.", summary);
    }

    [Fact]
    public async Task GenerateSummaryAsync_UsesFlashModel_ForNewsContent()
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
                        "parts": [{ "text": "Resumen" }]
                      }
                    }
                  ]
                }
                """));
        });
        var service = CreateService(handler);

        await service.GenerateSummaryAsync("Titulo", "Snippet", AiContentType.News);

        Assert.NotNull(capturedUrl);
        Assert.Contains("gemini-2.5-flash", capturedUrl);
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
                        "parts": [{ "text": "Resumen de documento" }]
                      }
                    }
                  ]
                }
                """));
        });
        var service = CreateService(handler);

        await service.GenerateSummaryAsync("Titulo documento", null, AiContentType.Document);

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
                        "parts": [{ "text": "Resumen" }]
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

        await service.GenerateSummaryAsync("Titulo", "Snippet", AiContentType.News);

        Assert.NotNull(capturedUrl);
        Assert.Contains("gemini-custom-news-model", capturedUrl);
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
