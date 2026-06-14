using Api.Middleware;
using Application.Catalog;
using Application.Seo;
using Api.Seo;
using Application.Fundamentals;
using Application.Ops;
using Domain.Seo;
using Domain.Fundamentals;
using Domain.Ops;
using Infrastructure.Seo;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Infrastructure.Tests.Middleware;

public sealed class SpaMetadataMiddlewareTests : IDisposable
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

    public SpaMetadataMiddlewareTests()
    {
        _webRootPath = Path.Combine(Path.GetTempPath(), "fibradis-spa-meta-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_webRootPath);
        File.WriteAllText(Path.Combine(_webRootPath, "index.html"), IndexHtmlTemplate);
    }

    public void Dispose()
    {
        try { Directory.Delete(_webRootPath, recursive: true); } catch { /* best effort */ }
    }

    [Fact]
    public async Task InjectsMetadata_ForKnownPath()
    {
        var (context, nextCalled) = await InvokeAsync("/calculadora");
        var body = await ReadBodyAsync(context);

        Assert.False(nextCalled.Value);
        Assert.StartsWith("text/html", context.Response.ContentType);
        Assert.Contains("<title>Calculadora de FIBRAs — ¿Cuántos CBFIs puedo comprar? | FIBRADIS</title>", body);
        Assert.Contains("<link rel=\"canonical\" href=\"https://fibrasinmobiliarias.com/calculadora\" />", body);
        Assert.Contains("<meta property=\"og:url\" content=\"https://fibrasinmobiliarias.com/calculadora\" />", body);
        Assert.Contains("<script type=\"application/ld+json\">", body);
        Assert.Contains("\"@type\":\"SoftwareApplication\"", body);
        Assert.Contains("\"@type\":\"BreadcrumbList\"", body);
    }

    [Fact]
    public async Task InjectsMetadata_ForNoticiasPageTwo_WithSelfCanonical_AndRobots()
    {
        var (context, nextCalled) = await InvokeAsync("/noticias", queryString: "?page=2");
        var body = await ReadBodyAsync(context);

        Assert.False(nextCalled.Value);
        Assert.Contains("<link rel=\"canonical\" href=\"https://fibrasinmobiliarias.com/noticias?page=2\" />", body);
        Assert.Contains("<meta name=\"robots\" content=\"noindex,follow\" />", body);
    }

    [Fact]
    public async Task InjectsMetadata_ForHome_WithRootCanonical()
    {
        var (context, nextCalled) = await InvokeAsync("/");
        var body = await ReadBodyAsync(context);

        Assert.False(nextCalled.Value);
        Assert.Contains("<title>FIBRAs Inmobiliarias — Análisis y Herramientas | FIBRADIS</title>", body);
        Assert.Contains("<link rel=\"canonical\" href=\"https://fibrasinmobiliarias.com/\" />", body);
        Assert.Contains("<meta name=\"description\"", body);
        Assert.Contains("<meta property=\"og:title\"", body);
        Assert.Contains("<meta property=\"og:description\"", body);
        // La home conserva su JSON-LD (WebSite) pero NO emite BreadcrumbList:
        // un breadcrumb de un solo ítem no aporta jerarquía (decisión code review 12-5)
        Assert.Contains("<script type=\"application/ld+json\">", body);
        Assert.DoesNotContain("\"@type\":\"BreadcrumbList\"", body);
    }

    [Fact]
    public async Task InjectsMetadata_ForComparar_WithLiveItemList_AndBreadcrumbs()
    {
        var (context, nextCalled) = await InvokeAsync(
            "/comparar",
            activeFibras:
            [
                ("Fibra Uno", "FUNO11"),
                ("Fibra Macquarie", "FIBRAMQ12"),
            ]);

        var body = await ReadBodyAsync(context);

        Assert.False(nextCalled.Value);
        Assert.Contains("<title>Comparar FIBRAs Inmobiliarias — Análisis Comparativo | FIBRADIS</title>", body);
        Assert.Contains("\"@type\":\"WebApplication\"", body);
        Assert.Contains("\"@type\":\"ItemList\"", body);
        Assert.Contains("https://fibrasinmobiliarias.com/fibras/fibra-macquarie-fibramq12", body);
        Assert.Contains("\"@type\":\"BreadcrumbList\"", body);
    }

    [Fact]
    public async Task InjectsMetadata_ForFundamentales_WithLiveDataset_AndBreadcrumbs()
    {
        var (context, nextCalled) = await InvokeAsync(
            "/fundamentales",
            fundamentalRows:
            [
                (
                    new FundamentalRecord
                    {
                        Id = Guid.NewGuid(),
                        FibraId = Guid.NewGuid(),
                        Period = "2T2026",
                        Status = "processed",
                        CapturedAt = new DateTimeOffset(2026, 6, 10, 14, 0, 0, TimeSpan.Zero),
                    },
                    "FUNO11",
                    "Fibra Uno"),
                (
                    new FundamentalRecord
                    {
                        Id = Guid.NewGuid(),
                        FibraId = Guid.NewGuid(),
                        Period = "2T2026",
                        Status = "processed",
                        CapturedAt = new DateTimeOffset(2026, 6, 13, 8, 45, 0, TimeSpan.Zero),
                    },
                    "DANHOS13",
                    "Danhos"),
            ]);

        var body = await ReadBodyAsync(context);

        Assert.False(nextCalled.Value);
        Assert.Contains("<title>Fundamentales FIBRAs — Cap Rate, NAV, NOI | FIBRADIS</title>", body);
        using var document = JsonDocument.Parse(ExtractJsonLdBlocks(body).Single(block => block.Contains("\"@type\":\"Dataset\"", StringComparison.Ordinal)));
        var dataset = document.RootElement.GetProperty("@graph").EnumerateArray().Single(node => node.GetProperty("@type").GetString() == "Dataset");
        Assert.Equal("FIBRAs cubiertas", dataset.GetProperty("variableMeasured")[5].GetProperty("name").GetString());
        Assert.Equal(2, dataset.GetProperty("variableMeasured")[5].GetProperty("value").GetInt32());
        Assert.Equal("2026-06-13T08:45:00.0000000+00:00", dataset.GetProperty("dateModified").GetString());
        Assert.Contains("\"@type\":\"BreadcrumbList\"", body);
    }

    [Fact]
    public async Task UsesSeoRepository_WhenActiveRowExists()
    {
        var seoMetadata = CreateSeoMetadata(
            title: "SEO manual de Calculadora",
            description: "Descripción SEO manual para la calculadora.",
            canonicalPath: "/calculadora",
            ogImageUrl: "https://cdn.example.com/calculadora.png",
            jsonLd: """{"@type":"SoftwareApplication","name":"Calculadora SEO"}""");

        var (context, nextCalled) = await InvokeAsync("/calculadora", seoMetadata: seoMetadata);
        var body = await ReadBodyAsync(context);

        Assert.False(nextCalled.Value);
        Assert.Contains("<title>SEO manual de Calculadora</title>", body);
        Assert.Contains("<meta name=\"description\" content=\"Descripción SEO manual para la calculadora.\" />", body);
        Assert.Contains("<link rel=\"canonical\" href=\"https://fibrasinmobiliarias.com/calculadora\" />", body);
        Assert.Contains("<meta property=\"og:image\" content=\"https://cdn.example.com/calculadora.png\" />", body);
        Assert.Contains("\"Calculadora SEO\"", body);
    }

    [Fact]
    public async Task FallsBackToProvider_WhenSeoRowIsInactive()
    {
        var seoMetadata = CreateSeoMetadata(
            title: "SEO inactivo",
            description: "Descripción que no debe usarse.",
            canonicalPath: "/calculadora",
            isActive: false);

        var (context, nextCalled) = await InvokeAsync("/calculadora", seoMetadata: seoMetadata);
        var body = await ReadBodyAsync(context);

        Assert.False(nextCalled.Value);
        Assert.Contains("<title>Calculadora de FIBRAs — ¿Cuántos CBFIs puedo comprar? | FIBRADIS</title>", body);
        Assert.DoesNotContain("SEO inactivo", body);
    }

    [Fact]
    public async Task PassesThrough_ForUnknownPath()
    {
        var (context, nextCalled) = await InvokeAsync("/portafolio");
        var body = await ReadBodyAsync(context);

        Assert.True(nextCalled.Value);
        Assert.Equal(string.Empty, body);
    }

    [Fact]
    public async Task PassesThrough_ForAssets()
    {
        var (_, nextCalled) = await InvokeAsync("/assets/index-1TzwM6fE.js");

        Assert.True(nextCalled.Value);
    }

    [Fact]
    public async Task PassesThrough_ForApiPrefix()
    {
        var (_, nextCalled) = await InvokeAsync("/api/v1/fibras");

        Assert.True(nextCalled.Value);
    }

    [Fact]
    public async Task PassesThrough_ForOpsAndHangfirePrefixes()
    {
        var (_, opsNext) = await InvokeAsync("/ops/dashboard");
        var (_, hangfireNext) = await InvokeAsync("/hangfire/jobs");

        Assert.True(opsNext.Value);
        Assert.True(hangfireNext.Value);
    }

    [Fact]
    public async Task ReplacesPrerendMetaComment()
    {
        var (context, _) = await InvokeAsync("/calculadora");
        var body = await ReadBodyAsync(context);

        Assert.DoesNotContain("<!-- prerender-meta -->", body);
        // el resto del documento queda intacto
        Assert.Contains("<script type=\"module\" crossorigin src=\"/assets/index-TEST.js\"></script>", body);
        Assert.Contains("<div id=\"root\"></div>", body);
        Assert.Contains("<html lang=\"es\">", body);
    }

    [Fact]
    public async Task InjectedHtml_ContainsExactlyOneTitle()
    {
        // decisión aprobada por el usuario: al inyectar se sustituye el <title> estático
        // para que Google no encuentre dos títulos (tomaría el primero, el genérico)
        var (context, _) = await InvokeAsync("/calculadora");
        var body = await ReadBodyAsync(context);

        Assert.DoesNotContain("<title>Fibras Inmobiliarias</title>", body);
        Assert.Equal(1, CountOccurrences(body, "<title>"));
    }

    [Fact]
    public async Task NormalizesPath_TrailingSlashAndCase()
    {
        var (context, nextCalled) = await InvokeAsync("/CALCULADORA/");
        var body = await ReadBodyAsync(context);

        Assert.False(nextCalled.Value);
        Assert.Contains("<title>Calculadora de FIBRAs — ¿Cuántos CBFIs puedo comprar? | FIBRADIS</title>", body);
    }

    [Fact]
    public async Task PassesThrough_WhenIndexHtmlMissing()
    {
        File.Delete(Path.Combine(_webRootPath, "index.html"));

        var (_, nextCalled) = await InvokeAsync("/calculadora");

        Assert.True(nextCalled.Value);
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    [InlineData("OPTIONS")]
    public async Task PassesThrough_ForNonGetOrHeadMethods(string method)
    {
        var (context, nextCalled) = await InvokeAsync("/calculadora", method);
        var body = await ReadBodyAsync(context);

        Assert.True(nextCalled.Value);
        Assert.Equal(string.Empty, body);
    }

    [Fact]
    public async Task InjectsMetadata_ForHeadRequest()
    {
        var (_, nextCalled) = await InvokeAsync("/calculadora", "HEAD");

        Assert.False(nextCalled.Value);
    }

    [Fact]
    public async Task PassesThrough_WhenPrerenderCommentMissing()
    {
        // un build que pierda el comentario no debe dejar la página sin <title> alguno
        var htmlSinComentario = IndexHtmlTemplate.Replace("<!-- prerender-meta -->", string.Empty);
        File.WriteAllText(Path.Combine(_webRootPath, "index.html"), htmlSinComentario);

        var (context, nextCalled) = await InvokeAsync("/calculadora");
        var body = await ReadBodyAsync(context);

        Assert.True(nextCalled.Value);
        Assert.Equal(string.Empty, body);
    }

    [Fact]
    public async Task SetsCacheControlNoCache_OnInjectedResponse()
    {
        var (context, _) = await InvokeAsync("/calculadora");

        Assert.Equal("no-cache", context.Response.Headers.CacheControl);
    }

    [Fact]
    public void Constructor_Throws_WhenBaseUrlMissing()
    {
        var emptyConfig = new ConfigurationBuilder().Build();

        var ex = Assert.Throws<InvalidOperationException>(() => new SpaMetadataMiddleware(
            _ => Task.CompletedTask,
            new SpaMetadataProvider(BuildConfig(), new SeoDefaultsBuilder(), BuildScopeFactory(null)),
            new SeoDefaultsBuilder(),
            BuildScopeFactory(null),
            new FakeWebHostEnvironment { WebRootPath = _webRootPath },
            emptyConfig));

        Assert.Contains("App:BaseUrl", ex.Message);
    }

    [Fact]
    public async Task EncodesHtmlInTitleAndDescription_AndEscapesJsonLd()
    {
        var meta = new SpaPageMeta(
            "Título con \"comillas\" & <tag>",
            "Descripción con \"comillas\" y <script>alert(1)</script> dentro.",
            "/calculadora",
            """{"text":"</script><img src=x>"}""");

        var (context, _) = await InvokeAsync("/calculadora", provider: new StubProvider(meta));
        var body = await ReadBodyAsync(context);

        Assert.DoesNotContain("<title>Título con \"comillas\" & <tag></title>", body);
        Assert.Contains("Título con &quot;comillas&quot; &amp; &lt;tag&gt;", body);
        Assert.DoesNotContain("</script><img src=x>", body);
        Assert.True(body.Contains("\\u003C/script\\u003E\\u003Cimg src=x\\u003E", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task PassesThrough_WhenIndexHtmlLocked()
    {
        var indexPath = Path.Combine(_webRootPath, "index.html");
        using var exclusiveLock = new FileStream(indexPath, FileMode.Open, FileAccess.Read, FileShare.None);

        var (_, nextCalled) = await InvokeAsync("/calculadora");

        Assert.True(nextCalled.Value);
    }

    private sealed class StubProvider(SpaPageMeta meta) : ISpaMetadataProvider
    {
        public Task<SpaPageMeta?> GetMetaForPathAsync(string path, CancellationToken ct = default)
            => Task.FromResult<SpaPageMeta?>(meta);
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
        PageType = SeoPageType.StaticPage,
        EntityKey = "/calculadora",
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

    private async Task<(DefaultHttpContext Context, StrongBox<bool> NextCalled)> InvokeAsync(
        string path,
        string method = "GET",
        string queryString = "",
        ISpaMetadataProvider? provider = null,
        SeoMetadata? seoMetadata = null,
        IReadOnlyList<(string FullName, string Ticker)>? activeFibras = null,
        IReadOnlyList<(FundamentalRecord Record, string Ticker, string ShortName)>? fundamentalRows = null)
    {
        var nextCalled = new StrongBox<bool>(false);
        var scopeFactory = BuildScopeFactory(seoMetadata, activeFibras, fundamentalRows);
        var middleware = new SpaMetadataMiddleware(
            _ =>
            {
                nextCalled.Value = true;
                return Task.CompletedTask;
            },
            provider ?? new SpaMetadataProvider(BuildConfig(), new SeoDefaultsBuilder(), scopeFactory),
            new SeoDefaultsBuilder(),
            scopeFactory,
            new FakeWebHostEnvironment { WebRootPath = _webRootPath },
            BuildConfig());

        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;
        context.Request.QueryString = new QueryString(queryString);
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);
        return (context, nextCalled);
    }

    private static IServiceScopeFactory BuildScopeFactory(
        SeoMetadata? seoMetadata,
        IReadOnlyList<(string FullName, string Ticker)>? activeFibras = null,
        IReadOnlyList<(FundamentalRecord Record, string Ticker, string ShortName)>? fundamentalRows = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(BuildConfig());
        services.AddScoped<ISeoMetadataRepository>(_ => new StubSeoMetadataRepository(seoMetadata));
        services.AddScoped<IFibraRepository>(_ => new StubFibraRepository(activeFibras ?? []));
        services.AddScoped<IFaqRepository>(_ => new StubFaqRepository());
        services.AddScoped<IOperationalConfigRepository>(_ => new StubOperationalConfigRepository("contacto@fibradis.mx"));
        services.AddScoped<IEditorialPageRepository>(_ => new StubEditorialPageRepository());
        services.AddScoped<IFundamentalRepository>(_ => new StubFundamentalRepository(fundamentalRows ?? []));
        services.AddSingleton<ISeoDefaultsBuilder, SeoDefaultsBuilder>();
        return services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
    }

    private sealed class StubOperationalConfigRepository(string? contactEmail) : IOperationalConfigRepository
    {
        public Task<Domain.Ops.OperationalConfig> GetAsync(CancellationToken ct = default)
            => Task.FromResult(new Domain.Ops.OperationalConfig { ContactEmail = contactEmail });

        public Task UpdateCetesRateAsync(decimal rate, DateTimeOffset updatedAt, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task UpdateTiieRateAsync(decimal rate, DateTimeOffset updatedAt, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task UpdateOrganizationSameAsAsync(string? organizationSameAsJson, string actor, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task UpdateAsync(decimal? commissionFactor, int? avgPeriods, int? newsCadenceMinutes, int? fibraNewsMonths, int? distributionCadenceMinutes, bool? termsEnabled, string? termsText, string? contactEmail, string actor, int? fundamentalsCadenceMinutes = null, int? universeDegradationThresholdPct = null, CancellationToken ct = default)
            => throw new NotSupportedException();
    }

    private sealed class StubEditorialPageRepository : IEditorialPageRepository
    {
        public Task<IReadOnlyList<Domain.Ops.EditorialPage>> GetAllAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<Domain.Ops.EditorialPage>>(
                [
                    new Domain.Ops.EditorialPage
                    {
                        Slug = "que-son-las-fibras",
                        Title = "¿Qué son?",
                        Content = "x",
                        Order = 0,
                        UpdatedAt = new DateTimeOffset(2026, 6, 12, 16, 30, 0, TimeSpan.Zero),
                    }
                ]);

        public Task<Domain.Ops.EditorialPage?> GetBySlugAsync(string slug, CancellationToken ct = default)
            => Task.FromResult<Domain.Ops.EditorialPage?>(null);

        public Task<int> UpdateContentAsync(string slug, string content, CancellationToken ct = default)
            => throw new NotSupportedException();
    }

    private sealed class StubFundamentalRepository : IFundamentalRepository
    {
        private readonly IReadOnlyList<(FundamentalRecord Record, string Ticker, string ShortName)> _rows;

        public StubFundamentalRepository(IReadOnlyList<(FundamentalRecord Record, string Ticker, string ShortName)> rows)
        {
            _rows = rows;
        }

        public Task<FundamentalRecord?> GetByIdAsync(Guid id, CancellationToken ct) => Task.FromResult<FundamentalRecord?>(null);
        public Task<FundamentalRecord?> GetProcessedByFibraAndPeriodAsync(Guid fibraId, string period, CancellationToken ct) => Task.FromResult<FundamentalRecord?>(null);
        public Task<FundamentalRecord?> GetLatestProcessedByFibraAsync(Guid fibraId, CancellationToken ct) => Task.FromResult<FundamentalRecord?>(null);
        public Task<IReadOnlyList<string>> GetProcessedPeriodsAsync(Guid fibraId, CancellationToken ct) => Task.FromResult<IReadOnlyList<string>>([]);
        public Task<IReadOnlyList<FundamentalRecord>> GetByFibraAsync(Guid fibraId, CancellationToken ct) => Task.FromResult<IReadOnlyList<FundamentalRecord>>([]);
        public Task AddAsync(FundamentalRecord record, CancellationToken ct) => Task.CompletedTask;
        public Task UpdateStatusAsync(Guid id, string status, string? confirmedBy, DateTimeOffset? confirmedAt, CancellationToken ct) => Task.CompletedTask;
        public Task UpdatePdfReferenceAsync(Guid id, string pdfReference, CancellationToken ct) => Task.CompletedTask;
        public Task UpdateMarkdownContentAsync(Guid id, string markdownContent, CancellationToken ct) => Task.CompletedTask;
        public Task UpdateKpiExtractionAsync(Guid id, KpiExtractionResult result, CancellationToken ct) => Task.CompletedTask;
        public Task UpdateKpisManualAsync(Guid id, decimal? capRate, decimal? navPerCbfi, decimal? ltv, decimal? noiMargin, decimal? ffoMargin, decimal? quarterlyDistribution, string? summary, CancellationToken ct) => Task.CompletedTask;
        public Task UpdateFieldNotesAsync(Guid id, Dictionary<string, string?> notes, CancellationToken ct) => Task.CompletedTask;
        public Task SoftDeleteAsync(Guid id, string deletedBy, CancellationToken ct) => Task.CompletedTask;
        public Task<IReadOnlyList<(FundamentalRecord Record, string Ticker, string ShortName)>> GetSummaryLatestAsync(CancellationToken ct = default)
            => Task.FromResult(_rows);
        public Task<IReadOnlyList<(FundamentalRecord Record, string Ticker, string ShortName)>> GetSummaryByPeriodAsync(string period, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<(FundamentalRecord Record, string Ticker, string ShortName)>>([]);
        public Task<IReadOnlyList<(FundamentalRecord Record, string Ticker, string ShortName)>> GetSummaryForRecentPeriodsAsync(int count, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<(FundamentalRecord Record, string Ticker, string ShortName)>>([]);
        public Task<IReadOnlyList<string>> GetAllProcessedPeriodsAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<string>>([]);
    }

    private sealed class StubFibraRepository(IReadOnlyList<(string FullName, string Ticker)> activeFibras) : IFibraRepository
    {
        public Task AddAsync(Domain.Catalog.Fibra fibra, CancellationToken ct = default) => throw new NotSupportedException();
        public Task UpdateAsync(Domain.Catalog.Fibra fibra, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<bool> ExistsByTickerAsync(string ticker, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<Domain.Catalog.Fibra> Items, int Total)> GetActivePagedAsync(FibraFilter filter, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Domain.Catalog.Fibra?> GetByTickerAsync(string ticker, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Domain.Catalog.Fibra?> GetByIdAsync(Guid id, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Domain.Catalog.Fibra>> GetAllAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Domain.Catalog.Fibra>> GetAllActiveAsync(CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Domain.Catalog.Fibra>> GetActiveBySectorAsync(string sector, Guid excludeId, int count, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Domain.Catalog.Fibra>>([]);
        public Task<IReadOnlyList<(string FullName, string Ticker)>> GetAllActiveForSitemapAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<(string FullName, string Ticker)>>(activeFibras);
    }

    private sealed class StubFaqRepository : IFaqRepository
    {
        public Task<IReadOnlyList<FaqItem>> GetByPageAsync(SeoPageType pageType, string entityKey, bool includeInactive = false, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<FaqItem>>([]);

        public Task<FaqItem?> GetByIdAsync(Guid id, CancellationToken ct = default) => Task.FromResult<FaqItem?>(null);

        public Task<FaqItem?> GetByNaturalKeyAsync(SeoPageType pageType, string entityKey, string question, CancellationToken ct = default) => Task.FromResult<FaqItem?>(null);

        public Task<bool> ExistsAsync(SeoPageType pageType, string entityKey, string question, CancellationToken ct = default) => Task.FromResult(false);

        public Task AddAsync(FaqItem item, CancellationToken ct = default) => Task.CompletedTask;

        public Task<bool> AddIfMissingAsync(FaqItem item, CancellationToken ct = default) => Task.FromResult(true);

        public Task UpdateAsync(FaqItem item, CancellationToken ct = default) => Task.CompletedTask;

        public Task<bool> DeactivateAsync(Guid id, string updatedBy, CancellationToken ct = default) => Task.FromResult(true);
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

    private static IReadOnlyList<string> ExtractJsonLdBlocks(string body)
    {
        const string pattern = "<script type=\"application/ld\\+json\">(?<json>.*?)</script>";
        return Regex.Matches(body, pattern, RegexOptions.Singleline)
            .Select(match => match.Groups["json"].Value)
            .ToArray();
    }

    private sealed class StrongBox<T>(T value)
    {
        public T Value { get; set; } = value;
    }

    private sealed class StubSeoMetadataRepository(SeoMetadata? seoMetadata) : ISeoMetadataRepository
    {
        public Task<SeoMetadata?> GetAsync(SeoPageType pageType, string entityKey, CancellationToken ct = default)
            => Task.FromResult(
                seoMetadata is not null &&
                seoMetadata.PageType == pageType &&
                string.Equals(NormalizeEntityKey(seoMetadata.EntityKey), NormalizeEntityKey(entityKey), StringComparison.OrdinalIgnoreCase)
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

        private static string NormalizeEntityKey(string entityKey)
        {
            var normalized = entityKey.Trim();
            if (normalized.Length == 0)
                return normalized;

            return normalized == "/" ? "/" : normalized.TrimEnd('/');
        }
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
