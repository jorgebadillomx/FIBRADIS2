using Api.Middleware;
using Application.News;
using Domain.News;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace Infrastructure.Tests.Middleware;

public sealed class NewsMetadataMiddlewareTests : IDisposable
{
    private const string IndexHtmlTemplate = """
        <!doctype html>
        <html lang="es">
          <head>
            <meta charset="UTF-8" />
            <title>Fibras Inmobiliarias</title>
            <!-- prerender-meta -->
            <script type="module" crossorigin src="/assets/index-TEST.js"></script>
          </head>
          <body>
            <div id="root"></div>
          </body>
        </html>
        """;

    private static readonly Guid ArticleId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");

    private readonly string _webRootPath;

    public NewsMetadataMiddlewareTests()
    {
        _webRootPath = Path.Combine(Path.GetTempPath(), "fibradis-news-meta-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_webRootPath);
        File.WriteAllText(Path.Combine(_webRootPath, "index.html"), IndexHtmlTemplate);
    }

    public void Dispose()
    {
        try { Directory.Delete(_webRootPath, recursive: true); } catch { /* best effort */ }
    }

    private static NewsArticle CreateArticle(
        string? slug = "funo11-reporta-resultados-del-2t25",
        string? aiAnalysisJson = null,
        string? snippet = "Snippet de la noticia con suficiente largo para validar el comportamiento de la descripción meta en resultados de búsqueda de Google y otros.",
        string? imageUrl = null,
        DateTimeOffset? deletedAt = null) => new()
    {
        Id = ArticleId,
        Title = "FUNO11 reporta resultados del 2T25",
        TitleNormalized = "funo11 reporta resultados del 2t25",
        Slug = slug,
        Source = "El Economista",
        PublishedAt = new DateTimeOffset(2026, 6, 10, 12, 0, 0, TimeSpan.Zero),
        Url = "https://example.com/nota",
        Snippet = snippet,
        ImageUrl = imageUrl,
        AiAnalysisJson = aiAnalysisJson,
        Status = NewsArticleStatus.Processed,
        CapturedAt = DateTimeOffset.UtcNow,
        DeletedAt = deletedAt,
    };

    [Fact]
    public async Task InvokeAsync_NewsSlugPath_InjectsMetadata()
    {
        var article = CreateArticle();
        var (context, nextCalled) = await InvokeAsync("/noticias/funo11-reporta-resultados-del-2t25", article);
        var body = await ReadBodyAsync(context);

        Assert.False(nextCalled.Value);
        Assert.StartsWith("text/html", context.Response.ContentType);
        Assert.DoesNotContain("<!-- prerender-meta -->", body);
        Assert.Contains("<title>FUNO11 reporta resultados del 2T25 — Noticias | FIBRADIS</title>", body);
        Assert.Contains("<link rel=\"canonical\" href=\"https://fibrasinmobiliarias.com/noticias/funo11-reporta-resultados-del-2t25\" />", body);
        Assert.Contains("<meta property=\"og:type\" content=\"article\" />", body);
        Assert.Contains("<meta property=\"og:url\" content=\"https://fibrasinmobiliarias.com/noticias/funo11-reporta-resultados-del-2t25\" />", body);
        Assert.Contains("<script type=\"application/ld+json\">", body);
        Assert.Contains("\"@type\":\"NewsArticle\"", body);
        // el offset "+00:00" puede emitirse escapado (+) según el encoder — no asertarlo literal
        Assert.Contains("\"datePublished\":\"2026-06-10T12:00:00.0000000", body);
        // el resto del documento queda intacto
        Assert.Contains("<div id=\"root\"></div>", body);
    }

    [Fact]
    public async Task InvokeAsync_UsesHeadlineAndSummary_FromAiAnalysis()
    {
        var article = CreateArticle(aiAnalysisJson:
            """{"headline":"Fibra Uno supera expectativas del mercado","summaryMarkdown":"Resumen analítico de los resultados trimestrales con métricas de ocupación, ingresos y distribución para inversionistas de FIBRAs mexicanas."}""");
        var (context, _) = await InvokeAsync("/noticias/funo11-reporta-resultados-del-2t25", article);
        var body = await ReadBodyAsync(context);

        Assert.Contains("<title>Fibra Uno supera expectativas del mercado — Noticias | FIBRADIS</title>", body);
        Assert.Contains("Resumen analítico de los resultados trimestrales", body);
    }

    [Fact]
    public async Task InvokeAsync_GuidPath_InjectsMetadataWithSlugCanonical()
    {
        // links antiguos /noticias/{guid}: la metadata se sirve pero el canonical apunta al slug
        var article = CreateArticle();
        var (context, nextCalled) = await InvokeAsync($"/noticias/{ArticleId}", article);
        var body = await ReadBodyAsync(context);

        Assert.False(nextCalled.Value);
        Assert.Contains("<link rel=\"canonical\" href=\"https://fibrasinmobiliarias.com/noticias/funo11-reporta-resultados-del-2t25\" />", body);
    }

    [Fact]
    public async Task InvokeAsync_OgImage_EmittedOnlyWhenPresent()
    {
        var withImage = CreateArticle(imageUrl: "https://cdn.example.com/img.jpg");
        var (contextWith, _) = await InvokeAsync("/noticias/funo11-reporta-resultados-del-2t25", withImage);
        var bodyWith = await ReadBodyAsync(contextWith);

        var withoutImage = CreateArticle();
        var (contextWithout, _) = await InvokeAsync("/noticias/funo11-reporta-resultados-del-2t25", withoutImage);
        var bodyWithout = await ReadBodyAsync(contextWithout);

        Assert.Contains("<meta property=\"og:image\" content=\"https://cdn.example.com/img.jpg\" />", bodyWith);
        Assert.DoesNotContain("og:image", bodyWithout);
    }

    [Fact]
    public async Task InvokeAsync_ShortDescription_PadsWithBrandSuffix()
    {
        var article = CreateArticle(snippet: "Texto corto.");
        var (context, _) = await InvokeAsync("/noticias/funo11-reporta-resultados-del-2t25", article);
        var body = await ReadBodyAsync(context);

        Assert.Contains("Texto corto. — Análisis y noticias de FIBRAs inmobiliarias en FIBRADIS: resultados, distribuciones y mercado inmobiliario bursátil de México.", body);
    }

    [Fact]
    public async Task InvokeAsync_EmptySnippet_DescriptionMeetsMinLength()
    {
        // CA-6: la description debe quedar en 120-160 chars aun sin snippet ni summary
        var article = CreateArticle(snippet: null);
        var (context, _) = await InvokeAsync("/noticias/funo11-reporta-resultados-del-2t25", article);
        var body = await ReadBodyAsync(context);

        var marker = "<meta name=\"description\" content=\"";
        var start = body.IndexOf(marker, StringComparison.Ordinal) + marker.Length;
        var description = body[start..body.IndexOf('"', start)];

        Assert.InRange(description.Length, 120, 160);
    }

    [Fact]
    public async Task InvokeAsync_MarkdownInSummary_StrippedFromDescription()
    {
        var article = CreateArticle(aiAnalysisJson:
            """{"summaryMarkdown":"**FUNO** reporta [resultados](https://example.com) sólidos.\n\n## Detalle\nIngresos arriba del consenso de mercado para el segundo trimestre del año."}""");
        var (context, _) = await InvokeAsync("/noticias/funo11-reporta-resultados-del-2t25", article);
        var body = await ReadBodyAsync(context);

        Assert.Contains("FUNO reporta resultados sólidos. Detalle Ingresos arriba del consenso", body);
        Assert.DoesNotContain("**FUNO**", body);
        Assert.DoesNotContain("](https://example.com)", body);
    }

    [Fact]
    public async Task InvokeAsync_LongDescription_TruncatesAt160()
    {
        var longSnippet = new string('a', 300);
        var article = CreateArticle(snippet: longSnippet);
        var (context, _) = await InvokeAsync("/noticias/funo11-reporta-resultados-del-2t25", article);
        var body = await ReadBodyAsync(context);

        Assert.Contains($"content=\"{new string('a', 157)}...\"", body);
        Assert.DoesNotContain(new string('a', 200), body);
    }

    [Fact]
    public async Task InvokeAsync_InjectedHtml_ContainsExactlyOneTitle()
    {
        var (context, _) = await InvokeAsync("/noticias/funo11-reporta-resultados-del-2t25", CreateArticle());
        var body = await ReadBodyAsync(context);

        Assert.DoesNotContain("<title>Fibras Inmobiliarias</title>", body);
        Assert.Equal(1, CountOccurrences(body, "<title>"));
    }

    [Fact]
    public async Task InvokeAsync_EncodesHtml_AndEscapesJsonLd()
    {
        var article = CreateArticle(aiAnalysisJson:
            """{"headline":"Titular con \"comillas\" & <tag>","summaryMarkdown":"Descripción con </script><img src=x> dentro del texto para validar el escaping de la metadata generada por el middleware de noticias."}""");
        var (context, _) = await InvokeAsync("/noticias/funo11-reporta-resultados-del-2t25", article);
        var body = await ReadBodyAsync(context);

        Assert.Contains("Titular con &quot;comillas&quot; &amp; &lt;tag&gt;", body);
        Assert.DoesNotContain("</script><img src=x>", body);
    }

    [Fact]
    public async Task InvokeAsync_SetsCacheControlNoCache()
    {
        var (context, _) = await InvokeAsync("/noticias/funo11-reporta-resultados-del-2t25", CreateArticle());

        Assert.Equal("no-cache", context.Response.Headers.CacheControl);
    }

    [Fact]
    public async Task InvokeAsync_AssetPath_PassesThrough()
    {
        var (context, nextCalled) = await InvokeAsync("/assets/index-TEST.js", CreateArticle());
        var body = await ReadBodyAsync(context);

        Assert.True(nextCalled.Value);
        Assert.Equal(string.Empty, body);
    }

    [Fact]
    public async Task InvokeAsync_ApiPath_PassesThrough()
    {
        var (_, nextCalled) = await InvokeAsync("/api/v1/news/funo11-reporta-resultados-del-2t25", CreateArticle());

        Assert.True(nextCalled.Value);
    }

    [Fact]
    public async Task InvokeAsync_NoticiasListPath_PassesThrough()
    {
        // /noticias (listado) lo maneja SpaMetadataMiddleware — CA-7
        var (_, exactNext) = await InvokeAsync("/noticias", CreateArticle());
        var (_, trailingNext) = await InvokeAsync("/noticias/", CreateArticle());

        Assert.True(exactNext.Value);
        Assert.True(trailingNext.Value);
    }

    [Fact]
    public async Task InvokeAsync_SlugNotFound_ReturnsSpaShellWith404()
    {
        // Soft-404: pass-through devolvería 200 vía MapFallback y la URL nunca saldría del índice
        var (context, nextCalled) = await InvokeAsync("/noticias/slug-inexistente", article: null);
        var body = await ReadBodyAsync(context);

        Assert.False(nextCalled.Value);
        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
        Assert.StartsWith("text/html", context.Response.ContentType);
        Assert.Contains("<div id=\"root\"></div>", body);
    }

    [Fact]
    public async Task InvokeAsync_DeletedArticleByGuid_ReturnsSpaShellWith404()
    {
        var deleted = CreateArticle(deletedAt: DateTimeOffset.UtcNow);
        var (context, nextCalled) = await InvokeAsync($"/noticias/{ArticleId}", deleted);

        Assert.False(nextCalled.Value);
        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_OverlongIdentifier_Returns404WithoutLookup()
    {
        // slug es nvarchar(256): identificadores más largos no consultan la BD
        var longIdentifier = new string('a', 300);
        var article = CreateArticle(slug: longIdentifier);
        var (context, nextCalled) = await InvokeAsync($"/noticias/{longIdentifier}", article);

        Assert.False(nextCalled.Value);
        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task InvokeAsync_NonGetOrHead_PassesThrough(string method)
    {
        var (_, nextCalled) = await InvokeAsync("/noticias/funo11-reporta-resultados-del-2t25", CreateArticle(), method);

        Assert.True(nextCalled.Value);
    }

    [Fact]
    public async Task InvokeAsync_PrerenderCommentMissing_PassesThrough()
    {
        var htmlSinComentario = IndexHtmlTemplate.Replace("<!-- prerender-meta -->", string.Empty);
        File.WriteAllText(Path.Combine(_webRootPath, "index.html"), htmlSinComentario);

        var (_, nextCalled) = await InvokeAsync("/noticias/funo11-reporta-resultados-del-2t25", CreateArticle());

        Assert.True(nextCalled.Value);
    }

    [Fact]
    public async Task InvokeAsync_IndexHtmlMissing_PassesThrough()
    {
        File.Delete(Path.Combine(_webRootPath, "index.html"));

        var (_, nextCalled) = await InvokeAsync("/noticias/funo11-reporta-resultados-del-2t25", CreateArticle());

        Assert.True(nextCalled.Value);
    }

    [Fact]
    public void Constructor_Throws_WhenBaseUrlMissing()
    {
        var emptyConfig = new ConfigurationBuilder().Build();

        var ex = Assert.Throws<InvalidOperationException>(() => new NewsMetadataMiddleware(
            _ => Task.CompletedTask,
            new FakeWebHostEnvironment { WebRootPath = _webRootPath },
            emptyConfig,
            BuildScopeFactory(null)));

        Assert.Contains("App:BaseUrl", ex.Message);
    }

    private async Task<(DefaultHttpContext Context, StrongBox<bool> NextCalled)> InvokeAsync(
        string path,
        NewsArticle? article,
        string method = "GET")
    {
        var nextCalled = new StrongBox<bool>(false);
        var middleware = new NewsMetadataMiddleware(
            _ =>
            {
                nextCalled.Value = true;
                return Task.CompletedTask;
            },
            new FakeWebHostEnvironment { WebRootPath = _webRootPath },
            BuildConfig(),
            BuildScopeFactory(article));

        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);
        return (context, nextCalled);
    }

    private static IServiceScopeFactory BuildScopeFactory(NewsArticle? article)
    {
        var services = new ServiceCollection();
        services.AddScoped<INewsRepository>(_ => new StubNewsRepository(article));
        return services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
    }

    private static IConfiguration BuildConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["App:BaseUrl"] = "https://fibrasinmobiliarias.com",
            })
            .Build();

    private static async Task<string> ReadBodyAsync(DefaultHttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body, leaveOpen: true);
        return await reader.ReadToEndAsync();
    }

    private static int CountOccurrences(string text, string token)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(token, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += token.Length;
        }

        return count;
    }

    private sealed class StrongBox<T>(T value)
    {
        public T Value { get; set; } = value;
    }

    // Stub mínimo: solo GetByIdAsync/GetBySlugAsync se usan desde el middleware
    private sealed class StubNewsRepository(NewsArticle? article) : INewsRepository
    {
        public Task<NewsArticle?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult(article?.Id == id ? article : null);

        public Task<NewsArticle?> GetBySlugAsync(string slug, CancellationToken ct = default)
            => Task.FromResult(article?.Slug == slug && article?.DeletedAt == null ? article : null);

        public Task<bool> ExistsByUrlAsync(string url, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<string>> GetExistingUrlsAsync(IEnumerable<string> candidateUrls, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<string>> GetRecentNormalizedTitlesAsync(DateTimeOffset since, CancellationToken ct = default) => throw new NotSupportedException();
        public Task AddWithLinksAsync(NewsArticle a, IEnumerable<Guid> fibraIds, CancellationToken ct = default) => throw new NotSupportedException();
        public Task UpdateBodyTextAsync(Guid id, string? bodyText, CancellationToken ct = default) => throw new NotSupportedException();
        public Task UpdateSummaryAsync(Guid id, string? summary, NewsArticleStatus status, CancellationToken ct = default) => throw new NotSupportedException();
        public Task UpdateAiAnalysisAsync(Guid id, string? analysisJson, string? summary, NewsArticleStatus status, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<NewsArticle>> GetLatestAsync(int count, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<NewsArticle>> GetLatestForFibraAsync(Guid fibraId, int count, int months, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<NewsArticle>> GetRelatedAsync(Guid excludeId, int count, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<(Guid Id, string Ticker)>> GetLinkedFibrasAsync(Guid articleId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<NewsArticle> Items, int Total, IReadOnlyDictionary<Guid, IReadOnlyList<(Guid FibraId, string Ticker)>> TickersByArticleId)> GetPagedPublicAsync(int page, int pageSize, string? q, Guid? fibraId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<NewsArticle> Items, int Total)> GetPagedForOpsAsync(int page, int pageSize, string? search, bool? hasAiSummary, Guid? fibraId = null, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<(Guid Id, string Url)>> GetNullBodyTextArticlesAsync(int maxArticles, int daysBack, CancellationToken ct = default) => throw new NotSupportedException();
        public Task SoftDeleteAsync(Guid id, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<string> GenerateUniqueSlugAsync(string title, Guid? excludeId = null, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<NewsArticle>> GetArticlesWithoutSlugAsync(int batchSize, CancellationToken ct = default) => throw new NotSupportedException();
        public Task UpdateSlugAsync(Guid id, string slug, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<(string Slug, DateTimeOffset PublishedAt)>> GetArticlesForSitemapAsync(int limit, CancellationToken ct = default) => throw new NotSupportedException();
    }

    private sealed class FakeWebHostEnvironment : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = string.Empty;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ApplicationName { get; set; } = "Api.Tests";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = "Production";
    }
}
