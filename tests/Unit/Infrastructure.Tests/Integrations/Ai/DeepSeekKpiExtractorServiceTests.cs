using System.Net;
using System.Text.Json;
using Application.News;
using Domain.News;
using Infrastructure.Integrations.Ai;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Infrastructure.Tests.Integrations.Ai;

public class DeepSeekKpiExtractorServiceTests
{
    [Fact]
    public async Task ExtractAsync_UsesJsonObjectMode_AndParsesResponse()
    {
        string? capturedModel = null;
        string? capturedResponseFormat = null;
        var service = CreateService(new StubHandler(async request =>
        {
            using var doc = await JsonDocument.ParseAsync(await request.Content!.ReadAsStreamAsync());
            capturedModel = doc.RootElement.GetProperty("model").GetString();
            capturedResponseFormat = doc.RootElement.GetProperty("response_format").GetProperty("type").GetString();

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                    {
                      "choices": [
                        {
                          "message": {
                            "content": "{\"capRate\":null,\"capRateNote\":null,\"navPerCbfi\":17.25,\"navPerCbfiNote\":\"NAV calculado con patrimonio y CBFIs.\",\"ltv\":0.41,\"ltvNote\":\"LTV calculado con deuda total y propiedades.\",\"noiMargin\":0.7,\"noiMarginNote\":\"NOI margin calculado con NOI e ingresos.\",\"ffoMargin\":null,\"ffoMarginNote\":null,\"quarterlyDistribution\":0.48,\"quarterlyDistributionNote\":\"Distribución declarada en el trimestre.\",\"summary\":\"Resumen DeepSeek.\",\"extractionNotes\":\"Se encontraron 4 KPIs y resumen.\"}"
                          }
                        }
                      ]
                    }
                    """),
            };
        }));

        var result = await service.ExtractAsync("markdown", CancellationToken.None);

        Assert.Equal("deepseek-v4-pro", capturedModel);
        Assert.Equal("json_object", capturedResponseFormat);
        Assert.True(result.Success);
        Assert.Equal(17.25m, result.NavPerCbfi);
        Assert.NotNull(result.NavPerCbfiNote);
        Assert.Equal(0.41m, result.Ltv);
        Assert.NotNull(result.LtvNote);
        Assert.Equal(0.7m, result.NoiMargin);
        Assert.NotNull(result.NoiMarginNote);
        Assert.Equal(0.48m, result.QuarterlyDistribution);
        Assert.NotNull(result.QuarterlyDistributionNote);
        Assert.Equal("Resumen DeepSeek.", result.Summary);
    }

    [Fact]
    public async Task ExtractAsync_ReturnsFailedResult_WhenJsonIsWrappedWithGarbage()
    {
        var service = CreateService(new StubHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""
                {
                  "choices": [
                    {
                      "message": {
                        "content": "texto previo {\"capRate\":0.08,\"capRateNote\":\"Cap rate explícito en reporte.\",\"navPerCbfi\":null,\"navPerCbfiNote\":null,\"ltv\":null,\"ltvNote\":null,\"noiMargin\":null,\"noiMarginNote\":null,\"ffoMargin\":null,\"ffoMarginNote\":null,\"quarterlyDistribution\":null,\"quarterlyDistributionNote\":null,\"summary\":null,\"extractionNotes\":\"Solo cap rate.\"} texto posterior"
                      }
                    }
                  ]
                }
                """),
        })));

        var result = await service.ExtractAsync("markdown", CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(0.08m, result.CapRate);
        Assert.Equal("Cap rate explícito en reporte.", result.CapRateNote);
        Assert.Equal("Solo cap rate.", result.ExtractionNotes);
    }

    private static DeepSeekKpiExtractorService CreateService(HttpMessageHandler handler)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["DeepSeek:ApiKey"] = "test-key" })
            .Build();

        return new DeepSeekKpiExtractorService(
            new HttpClient(handler),
            configuration,
            new FakeProviderRepo(),
            new FakePromptRepo(),
            NullLogger<DeepSeekKpiExtractorService>.Instance);
    }

    private sealed class FakeProviderRepo : IAiProviderConfigRepository
    {
        public Task<AiProviderConfig> GetConfigAsync(CancellationToken ct = default)
            => Task.FromResult(new AiProviderConfig
            {
                Id = 1,
                Provider = AiProvider.DeepSeek,
                ModelId = "deepseek-v4-pro",
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
