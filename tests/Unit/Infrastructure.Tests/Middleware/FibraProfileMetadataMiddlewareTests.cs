using Api.Middleware;
using Application.Catalog;
using Application.Seo;
using Domain.Catalog;
using Domain.Seo;
using Infrastructure.Seo;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace Infrastructure.Tests.Middleware;

public sealed class FibraProfileMetadataMiddlewareTests : IDisposable
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

    private readonly string _webRootPath;

    public FibraProfileMetadataMiddlewareTests()
    {
        _webRootPath = Path.Combine(Path.GetTempPath(), "fibradis-fibra-meta-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_webRootPath);
        File.WriteAllText(Path.Combine(_webRootPath, "index.html"), IndexHtmlTemplate);
    }

    public void Dispose()
    {
        try { Directory.Delete(_webRootPath, recursive: true); } catch { /* best effort */ }
    }

    private static Fibra CreateFibra(
        string ticker = "FUNO11",
        string fullName = "Fibra Uno",
        string sector = "Industrial",
        // Origen corrupto real de producción: dump de markdown con heading, emoji perdido como
        // "??" (0x3F 0x3F) y tabla. NO debe filtrarse a ninguna superficie de metadata.
        string? description = "  ?? Fibra Uno | FUNO11 Ticker: FUNO11 Fecha de constitución: 10 de enero de 2011 | Campo | Detalle | | --- | --- |") => new()
    {
        Id = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000001"),
        Ticker = ticker,
        FullName = fullName,
        ShortName = fullName,
        Sector = sector,
        Market = "BMV",
        Currency = "MXN",
        State = FibraState.Active,
        Description = description,
        CreatedAt = DateTimeOffset.UtcNow,
    };

    [Fact]
    public async Task InvokeAsync_FibraSlugPath_InjectsMetadata()
    {
        var (context, nextCalled) = await InvokeAsync("/fibras/fibra-uno-funo11", CreateFibra());
        var body = await ReadBodyAsync(context);

        Assert.False(nextCalled.Value);
        Assert.StartsWith("text/html", context.Response.ContentType);
        Assert.DoesNotContain("<!-- prerender-meta -->", body);
        Assert.Contains("<title>Fibra Uno (FUNO11) | FIBRADIS — Fibras Inmobiliarias</title>", body);
        Assert.Contains("<link rel=\"canonical\" href=\"https://fibrasinmobiliarias.com/fibras/fibra-uno-funo11\" />", body);
        Assert.Contains("\"@type\":\"FinancialProduct\"", body);
        Assert.Contains("<div id=\"root\"></div>", body);
    }

    [Fact]
    public async Task InvokeAsync_Description_IsCleanSentence_NoMarkdownNoEmojiDump()
    {
        var (context, _) = await InvokeAsync("/fibras/fibra-uno-funo11", CreateFibra());
        var body = await ReadBodyAsync(context);

        // La descripción se genera desde campos estructurados, no del markdown de Description
        Assert.Contains(
            "Análisis de Fibra Uno (FUNO11): precio, yield, fundamentales (Cap Rate, NAV, LTV) y distribuciones. Sector Industrial en la BMV.",
            body);

        // El defecto de encoding del origen (emoji → "??") no llega a ninguna parte del HTML
        Assert.DoesNotContain("??", body);

        // Los pipes/heading de la tabla markdown no aparecen en la description (el "|" del
        // <title> es un separador de marca legítimo, por eso se acota al valor de la meta)
        var description = ExtractMetaContent(body, "description");
        Assert.DoesNotContain("|", description);
        Assert.DoesNotContain("#", description);
        Assert.DoesNotContain("Ticker: FUNO11", description);
        Assert.DoesNotContain("Campo", description);
    }

    [Fact]
    public async Task InvokeAsync_SameDescription_InAllThreeSurfaces()
    {
        var (context, _) = await InvokeAsync("/fibras/fibra-uno-funo11", CreateFibra());
        var body = await ReadBodyAsync(context);

        var expected = "Análisis de Fibra Uno (FUNO11): precio, yield, fundamentales (Cap Rate, NAV, LTV) y distribuciones. Sector Industrial en la BMV.";

        Assert.Contains($"<meta name=\"description\" content=\"{expected}\" />", body);
        Assert.Contains($"<meta name=\"twitter:description\" content=\"{expected}\" />", body);
        // JSON-LD FinancialProduct: mismo texto limpio
        Assert.Contains($"\"description\":\"{expected}\"", body);
    }

    [Fact]
    public async Task InvokeAsync_SeoRepositoryRow_OverridesDefaults()
    {
        var seoMetadata = CreateSeoMetadata(
            title: "SEO manual de Fibra Uno",
            description: "Descripción SEO manual para Fibra Uno.",
            canonicalPath: "/fibras/fibra-uno-funo11",
            ogImageUrl: "https://cdn.example.com/funo11.png",
            jsonLd: """{"@type":"FinancialProduct","name":"Fibra Uno SEO"}""");

        var (context, _) = await InvokeAsync("/fibras/fibra-uno-funo11", CreateFibra(), seoMetadata: seoMetadata);
        var body = await ReadBodyAsync(context);

        Assert.Contains("<title>SEO manual de Fibra Uno</title>", body);
        Assert.Contains("<meta name=\"description\" content=\"Descripción SEO manual para Fibra Uno.\" />", body);
        Assert.Contains("<meta property=\"og:image\" content=\"https://cdn.example.com/funo11.png\" />", body);
        Assert.Contains("\"Fibra Uno SEO\"", body);
        Assert.DoesNotContain("Fibra Uno (FUNO11) | FIBRADIS — Fibras Inmobiliarias", body);
    }

    [Fact]
    public async Task InvokeAsync_InactiveSeoRow_FallsBackToDefaults()
    {
        var seoMetadata = CreateSeoMetadata(
            title: "SEO inactivo",
            description: "Descripción que no debe usarse.",
            canonicalPath: "/fibras/fibra-uno-funo11",
            isActive: false);

        var (context, _) = await InvokeAsync("/fibras/fibra-uno-funo11", CreateFibra(), seoMetadata: seoMetadata);
        var body = await ReadBodyAsync(context);

        Assert.Contains("<title>Fibra Uno (FUNO11) | FIBRADIS — Fibras Inmobiliarias</title>", body);
        Assert.DoesNotContain("SEO inactivo", body);
    }

    [Fact]
    public async Task InvokeAsync_EmptySector_OmitsSectorClause()
    {
        var (context, _) = await InvokeAsync("/fibras/fibra-uno-funo11", CreateFibra(sector: ""));
        var body = await ReadBodyAsync(context);

        Assert.Contains("y distribuciones. Cotiza en la BMV.", body);
        Assert.DoesNotContain("Sector  en la BMV", body);
    }

    [Fact]
    public async Task InvokeAsync_LongSector_TruncatesAtWordBoundary()
    {
        var longSector = string.Join(' ', Enumerable.Repeat("comercial", 30));
        var (context, _) = await InvokeAsync("/fibras/fibra-uno-funo11", CreateFibra(sector: longSector));
        var body = await ReadBodyAsync(context);

        var description = ExtractMetaContent(body, "description");

        Assert.True(description.Length <= 156, $"Longitud {description.Length} excede 155 + elipsis");
        Assert.EndsWith("…", description);
        // Corte en frontera de palabra: no termina con una palabra partida
        Assert.DoesNotContain("comercia…", description);
    }

    [Fact]
    public async Task InvokeAsync_InjectedHtml_ContainsExactlyOneTitle()
    {
        var (context, _) = await InvokeAsync("/fibras/fibra-uno-funo11", CreateFibra());
        var body = await ReadBodyAsync(context);

        Assert.DoesNotContain("<title>Fibras Inmobiliarias</title>", body);
        Assert.Equal(1, CountOccurrences(body, "<title>"));
    }

    [Fact]
    public async Task InvokeAsync_SetsCacheControlNoCache()
    {
        var (context, _) = await InvokeAsync("/fibras/fibra-uno-funo11", CreateFibra());

        Assert.Equal("no-cache", context.Response.Headers.CacheControl);
    }

    [Fact]
    public async Task InvokeAsync_UnknownTicker_PassesThrough()
    {
        var (_, nextCalled) = await InvokeAsync("/fibras/inexistente-xxxx99", article: null);

        Assert.True(nextCalled.Value);
    }

    [Fact]
    public async Task InvokeAsync_FibrasListPath_PassesThrough()
    {
        var (_, exactNext) = await InvokeAsync("/fibras", CreateFibra());
        var (_, trailingNext) = await InvokeAsync("/fibras/", CreateFibra());

        Assert.True(exactNext.Value);
        Assert.True(trailingNext.Value);
    }

    [Fact]
    public async Task InvokeAsync_ApiPath_PassesThrough()
    {
        var (_, nextCalled) = await InvokeAsync("/api/v1/fibras/fibra-uno-funo11", CreateFibra());

        Assert.True(nextCalled.Value);
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task InvokeAsync_NonGetOrHead_PassesThrough(string method)
    {
        var (_, nextCalled) = await InvokeAsync("/fibras/fibra-uno-funo11", CreateFibra(), method);

        Assert.True(nextCalled.Value);
    }

    [Fact]
    public async Task InvokeAsync_PrerenderCommentMissing_PassesThrough()
    {
        var htmlSinComentario = IndexHtmlTemplate.Replace("<!-- prerender-meta -->", string.Empty);
        File.WriteAllText(Path.Combine(_webRootPath, "index.html"), htmlSinComentario);

        var (_, nextCalled) = await InvokeAsync("/fibras/fibra-uno-funo11", CreateFibra());

        Assert.True(nextCalled.Value);
    }

    [Fact]
    public void Constructor_Throws_WhenBaseUrlMissing()
    {
        var emptyConfig = new ConfigurationBuilder().Build();

        var ex = Assert.Throws<InvalidOperationException>(() => new FibraProfileMetadataMiddleware(
            _ => Task.CompletedTask,
            new FakeWebHostEnvironment { WebRootPath = _webRootPath },
            emptyConfig,
            new SeoDefaultsBuilder(),
            BuildScopeFactory(null)));

        Assert.Contains("App:BaseUrl", ex.Message);
    }

    private async Task<(DefaultHttpContext Context, StrongBox<bool> NextCalled)> InvokeAsync(
        string path,
        Fibra? article,
        string method = "GET",
        SeoMetadata? seoMetadata = null)
    {
        var nextCalled = new StrongBox<bool>(false);
        var middleware = new FibraProfileMetadataMiddleware(
            _ =>
            {
                nextCalled.Value = true;
                return Task.CompletedTask;
            },
            new FakeWebHostEnvironment { WebRootPath = _webRootPath },
            BuildConfig(),
            new SeoDefaultsBuilder(),
            BuildScopeFactory(article, seoMetadata));

        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);
        return (context, nextCalled);
    }

    private static SeoMetadata CreateSeoMetadata(
        string title,
        string description,
        string canonicalPath,
        string? ogImageUrl = null,
        string? jsonLd = null,
        bool isActive = true) => new()
    {
        Id = Guid.NewGuid(),
        PageType = SeoPageType.Fibra,
        EntityKey = "FUNO11",
        Title = title,
        MetaDescription = description,
        CanonicalPath = canonicalPath,
        OgTitle = title,
        OgDescription = description,
        OgType = "website",
        OgImageUrl = ogImageUrl ?? "https://fibrasinmobiliarias.com/og-image.png",
        OgLocale = "es_MX",
        TwitterCard = "summary_large_image",
        RobotsDirectives = "index,follow",
        JsonLd = jsonLd,
        IsActive = isActive,
        UpdatedAt = DateTimeOffset.UtcNow,
        UpdatedBy = "adminops@test.com",
    };

    private static IServiceScopeFactory BuildScopeFactory(Fibra? fibra, SeoMetadata? seoMetadata = null)
    {
        var services = new ServiceCollection();
        services.AddScoped<IFibraRepository>(_ => new StubFibraRepository(fibra));
        services.AddScoped<ISeoMetadataRepository>(_ => new StubSeoMetadataRepository(seoMetadata));
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

    private static string ExtractMetaContent(string body, string name)
    {
        var marker = $"<meta name=\"{name}\" content=\"";
        var start = body.IndexOf(marker, StringComparison.Ordinal) + marker.Length;
        return body[start..body.IndexOf('"', start)];
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

    // Stub mínimo: el middleware solo resuelve la fibra por ticker (último segmento del slug)
    private sealed class StubFibraRepository(Fibra? fibra) : IFibraRepository
    {
        public Task<Fibra?> GetByTickerAsync(string ticker, CancellationToken ct = default)
            => Task.FromResult(string.Equals(fibra?.Ticker, ticker, StringComparison.OrdinalIgnoreCase) ? fibra : null);

        public Task AddAsync(Fibra fibra, CancellationToken ct = default) => throw new NotSupportedException();
        public Task UpdateAsync(Fibra fibra, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<bool> ExistsByTickerAsync(string ticker, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<Fibra> Items, int Total)> GetActivePagedAsync(FibraFilter filter, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Fibra?> GetByIdAsync(Guid id, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Fibra>> GetAllAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Fibra>> GetAllActiveAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<(string FullName, string Ticker)>> GetAllActiveForSitemapAsync(CancellationToken ct = default) => throw new NotSupportedException();
    }

    private sealed class StubSeoMetadataRepository(SeoMetadata? seoMetadata) : ISeoMetadataRepository
    {
        public Task<SeoMetadata?> GetAsync(SeoPageType pageType, string entityKey, CancellationToken ct = default)
            => Task.FromResult(
                seoMetadata is not null &&
                seoMetadata.PageType == pageType &&
                string.Equals(seoMetadata.EntityKey, entityKey, StringComparison.OrdinalIgnoreCase)
                    ? seoMetadata
                    : null);

        public Task<IReadOnlyList<SeoMetadata>> GetAllAsync(SeoMetadataQuery? query = null, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task<bool> ExistsAsync(SeoPageType pageType, string entityKey, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task<SeoMetadata> UpsertAsync(SeoMetadata metadata, bool overrideMode = false, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<(SeoPageType PageType, string EntityKey)>> GetExistingKeysAsync(
            IEnumerable<(SeoPageType PageType, string EntityKey)> keys,
            CancellationToken ct = default)
            => throw new NotSupportedException();
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
