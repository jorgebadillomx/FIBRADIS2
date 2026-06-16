using Api.Seo;
using Application.Fundamentals;
using Application.Ops;
using Domain.Fundamentals;
using Domain.Ops;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Infrastructure.Seo;
using System.Text.Json;

namespace Infrastructure.Tests.Seo;

public class SpaMetadataProviderTests
{
    [Theory]
    [InlineData("/")]
    [InlineData("/calculadora")]
    [InlineData("/comparar")]
    [InlineData("/fibras")]
    [InlineData("/noticias")]
    [InlineData("/conoce-las-fibras")]
    [InlineData("/calendario")]
    [InlineData("/fundamentales")]
    [InlineData("/plataforma")]
    [InlineData("/portafolio")]
    [InlineData("/privacidad")]
    [InlineData("/acerca")]
    [InlineData("/contacto")]
    public async Task GetMetaForPathAsync_ReturnsMeta_ForKnownRoutes(string path)
    {
        var provider = CreateProvider();
        var meta = await provider.GetMetaForPathAsync(path);

        Assert.NotNull(meta);
        Assert.EndsWith("| Fibras Inmobiliarias", meta!.Title);
        Assert.Equal(NormalizePath(path), meta.CanonicalPath);
    }

    [Theory]
    [InlineData("/oportunidades")]
    [InlineData("/fibras/FUNO11")]
    [InlineData("/noticias/abc-123")]
    [InlineData("/login")]
    [InlineData("/herramientas")]
    public async Task GetMetaForPathAsync_ReturnsNull_ForUnknownRoutes(string path)
    {
        var provider = CreateProvider();
        Assert.Null(await provider.GetMetaForPathAsync(path));
    }

    [Theory]
    [InlineData("/calculadora/")]
    [InlineData("/CALCULADORA")]
    [InlineData("/Calculadora/")]
    public async Task GetMetaForPathAsync_NormalizesTrailingSlashAndCase(string path)
    {
        var provider = CreateProvider();
        var meta = await provider.GetMetaForPathAsync(path);

        Assert.NotNull(meta);
        Assert.Equal("/calculadora", meta!.CanonicalPath);
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/calculadora")]
    [InlineData("/comparar")]
    [InlineData("/fibras")]
    [InlineData("/noticias")]
    [InlineData("/conoce-las-fibras")]
    [InlineData("/calendario")]
    [InlineData("/fundamentales")]
    [InlineData("/plataforma")]
    [InlineData("/portafolio")]
    [InlineData("/privacidad")]
    [InlineData("/acerca")]
    [InlineData("/contacto")]
    public async Task Descriptions_AreBetween120And160Chars(string path)
    {
        var provider = CreateProvider();
        var meta = await provider.GetMetaForPathAsync(path);

        Assert.NotNull(meta);
        Assert.InRange(meta!.Description.Length, 120, 160);
    }

    [Fact]
    public async Task Calculadora_HasSoftwareApplicationJsonLd()
    {
        var provider = CreateProvider();
        var meta = await provider.GetMetaForPathAsync("/calculadora");

        Assert.NotNull(meta);
        Assert.NotNull(meta!.JsonLd);
        Assert.Contains("\"@type\":\"SoftwareApplication\"", meta.JsonLd);
        Assert.Contains("\"name\":\"Calculadora de compra de FIBRAs\"", meta.JsonLd);
        Assert.Contains("Calcula cuántos CBFIs puedes comprar con tu presupuesto", meta.JsonLd);
    }

    [Fact]
    public async Task Portafolio_HasCollectionPageJsonLd_WithBaseUrlReferences()
    {
        var provider = CreateProvider();
        var meta = await provider.GetMetaForPathAsync("/portafolio");

        Assert.NotNull(meta);
        Assert.NotNull(meta!.JsonLd);
        using var document = JsonDocument.Parse(meta.JsonLd);
        var graph = document.RootElement.GetProperty("@graph").EnumerateArray().ToArray();

        var collectionPage = graph.Single(element => element.GetProperty("@type").GetString() == "CollectionPage");
        Assert.Equal($"{TestBaseUrl}/portafolio#page", collectionPage.GetProperty("@id").GetString());
        Assert.Equal($"{TestBaseUrl}/portafolio", collectionPage.GetProperty("url").GetString());
        Assert.Equal($"{TestBaseUrl}/#website", collectionPage.GetProperty("isPartOf").GetProperty("@id").GetString());
        Assert.Equal($"{TestBaseUrl}/#organization", collectionPage.GetProperty("publisher").GetProperty("@id").GetString());
        Assert.Equal("ItemList", collectionPage.GetProperty("mainEntity").GetProperty("@type").GetString());
        Assert.Contains("\"Reportes trimestrales\"", meta.JsonLd);
    }

    [Fact]
    public async Task Plataforma_HasCollectionPageJsonLd_WithBaseUrlReferences()
    {
        var provider = CreateProvider();
        var meta = await provider.GetMetaForPathAsync("/plataforma");

        Assert.NotNull(meta);
        Assert.NotNull(meta!.JsonLd);
        using var document = JsonDocument.Parse(meta.JsonLd);
        var graph = document.RootElement.GetProperty("@graph").EnumerateArray().ToArray();

        var collectionPage = graph.Single(element => element.GetProperty("@type").GetString() == "CollectionPage");
        Assert.Equal($"{TestBaseUrl}/plataforma#page", collectionPage.GetProperty("@id").GetString());
        Assert.Equal($"{TestBaseUrl}/plataforma", collectionPage.GetProperty("url").GetString());
        Assert.Equal($"{TestBaseUrl}/#website", collectionPage.GetProperty("isPartOf").GetProperty("@id").GetString());
        Assert.Equal($"{TestBaseUrl}/#organization", collectionPage.GetProperty("publisher").GetProperty("@id").GetString());
        Assert.Equal("ItemList", collectionPage.GetProperty("mainEntity").GetProperty("@type").GetString());
        Assert.Equal(7, collectionPage.GetProperty("mainEntity").GetProperty("numberOfItems").GetInt32());
        Assert.Contains("\"Catálogo y fichas de FIBRAs\"", meta.JsonLd);
        Assert.Contains("\"Guía ¿Qué son las FIBRAs?\"", meta.JsonLd);
    }

    [Fact]
    public async Task Homepage_HasOrganizationWebSiteAndFinancialServiceJsonLd_WithConfigEmail()
    {
        var provider = CreateProvider(contactEmail: "equipo@fibradis.mx");
        var meta = await provider.GetMetaForPathAsync("/");

        Assert.NotNull(meta);
        Assert.NotNull(meta!.JsonLd);
        Assert.Contains("\"@type\":\"Organization\"", meta.JsonLd);
        Assert.Contains("\"@type\":\"WebSite\"", meta.JsonLd);
        Assert.Contains("\"@type\":\"FinancialService\"", meta.JsonLd);
        Assert.Contains("\"name\":\"Fibras Inmobiliarias\"", meta.JsonLd);
        Assert.Contains("\"email\":\"equipo@fibradis.mx\"", meta.JsonLd);
        Assert.DoesNotContain("twitter.com", meta.JsonLd, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("linkedin.com", meta.JsonLd, StringComparison.OrdinalIgnoreCase);
        // AC-1/T1: el JSON-LD usa App:BaseUrl, no un dominio hardcodeado
        Assert.Contains(TestBaseUrl, meta.JsonLd);
        Assert.DoesNotContain("fibrasinmobiliarias.com", meta.JsonLd, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Homepage_EmitsSameAs_FromOperationalConfig()
    {
        var provider = CreateProvider(
            contactEmail: "equipo@fibradis.mx",
            sameAsJson: """["https://www.youtube.com/@fibradis","https://www.instagram.com/fibradis"]""");

        var meta = await provider.GetMetaForPathAsync("/");

        Assert.NotNull(meta);
        using var document = JsonDocument.Parse(meta!.JsonLd!);
        var organization = document.RootElement
            .GetProperty("@graph")
            .EnumerateArray()
            .First(element => element.GetProperty("@type").GetString() == "Organization");
        var sameAs = organization.GetProperty("sameAs").EnumerateArray().Select(item => item.GetString()!).ToArray();

        Assert.Equal([
            "https://www.youtube.com/@fibradis",
            "https://www.instagram.com/fibradis",
        ], sameAs);
    }

    [Fact]
    public async Task Homepage_ToleratesNullAndInvalidSameAsEntries()
    {
        // Un elemento null o sin esquema http/https no debe lanzar NRE ni emitir señal inválida
        var provider = CreateProvider(
            contactEmail: "equipo@fibradis.mx",
            sameAsJson: """["https://www.youtube.com/@fibradis", null, "  ", "ftp://x", "javascript:alert(1)"]""");

        var meta = await provider.GetMetaForPathAsync("/");

        Assert.NotNull(meta);
        using var document = JsonDocument.Parse(meta!.JsonLd!);
        var organization = document.RootElement
            .GetProperty("@graph")
            .EnumerateArray()
            .First(element => element.GetProperty("@type").GetString() == "Organization");
        var sameAs = organization.GetProperty("sameAs").EnumerateArray().Select(item => item.GetString()!).ToArray();

        Assert.Equal(["https://www.youtube.com/@fibradis"], sameAs);
    }

    [Fact]
    public async Task ConoceLasFibras_HasArticleJsonLd_WithAuthorPublisherAndLatestDateModified()
    {
        var provider = CreateProvider(
            editorialPages:
            [
                new EditorialPage { Slug = "que-son-las-fibras", Title = "¿Qué son?", Content = "x", Order = 0, UpdatedAt = new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero) },
                new EditorialPage { Slug = "historia", Title = "Historia", Content = "x", Order = 1, UpdatedAt = new DateTimeOffset(2026, 6, 12, 16, 30, 0, TimeSpan.Zero) },
            ]);

        var meta = await provider.GetMetaForPathAsync("/conoce-las-fibras");

        Assert.NotNull(meta);
        Assert.NotNull(meta!.JsonLd);
        using var document = JsonDocument.Parse(meta.JsonLd);
        Assert.Equal("Article", document.RootElement.GetProperty("@type").GetString());
        Assert.Equal("2026-06-12T16:30:00.0000000+00:00", document.RootElement.GetProperty("dateModified").GetString());
        Assert.Equal("Organization", document.RootElement.GetProperty("author").GetProperty("@type").GetString());
        Assert.Equal($"{TestBaseUrl}/#organization", document.RootElement.GetProperty("publisher").GetProperty("@id").GetString());
    }

    [Fact]
    public async Task Compare_HasWebApplicationJsonLd()
    {
        var provider = CreateProvider();
        var meta = await provider.GetMetaForPathAsync("/comparar");

        Assert.NotNull(meta);
        Assert.NotNull(meta!.JsonLd);
        using var document = JsonDocument.Parse(meta.JsonLd);
        var graph = document.RootElement.GetProperty("@graph").EnumerateArray().ToArray();

        Assert.Contains(graph, element => element.GetProperty("@type").GetString() == "WebApplication");
        Assert.DoesNotContain("BreadcrumbList", meta.JsonLd);
    }

    [Fact]
    public async Task Fundamentals_HasDatasetJsonLd()
    {
        var provider = CreateProvider(
            fundamentalsRows:
            [
                new Tuple<FundamentalRecord, string, string>(
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
                new Tuple<FundamentalRecord, string, string>(
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

        var meta = await provider.GetMetaForPathAsync("/fundamentales");

        Assert.NotNull(meta);
        Assert.NotNull(meta!.JsonLd);
        using var document = JsonDocument.Parse(meta.JsonLd);
        var graph = document.RootElement.GetProperty("@graph").EnumerateArray().ToArray();

        Assert.Contains(graph, element => element.GetProperty("@type").GetString() == "Dataset");
        Assert.DoesNotContain("BreadcrumbList", meta.JsonLd);
    }

    [Fact]
    public async Task AboutAndContactPages_HaveSchemaAndUseOperationalEmail()
    {
        var provider = CreateProvider(contactEmail: "ops@fibradis.mx");

        var about = await provider.GetMetaForPathAsync("/acerca");
        var contact = await provider.GetMetaForPathAsync("/contacto");

        Assert.NotNull(about);
        Assert.NotNull(about!.JsonLd);
        Assert.Contains("\"@type\":\"AboutPage\"", about.JsonLd);
        Assert.Contains("\"ContactAction\"", about.JsonLd);

        Assert.NotNull(contact);
        Assert.NotNull(contact!.JsonLd);
        Assert.Contains("\"@type\":\"ContactPage\"", contact.JsonLd);
        Assert.Contains("\"ContactPoint\"", contact.JsonLd);
        Assert.Contains("\"email\":\"ops@fibradis.mx\"", contact.JsonLd);
    }

    [Theory]
    [InlineData("/noticias")]
    [InlineData("/fibras")]
    [InlineData("/calendario")]
    [InlineData("/privacidad")]
    public async Task ContentRoutes_HaveNoJsonLd(string path)
    {
        var provider = CreateProvider();
        var meta = await provider.GetMetaForPathAsync(path);

        Assert.NotNull(meta);
        Assert.Null(meta!.JsonLd);
    }

    private static SpaMetadataProvider CreateProvider(
        string? contactEmail = "portafoliodefibras@gmail.com",
        string? sameAsJson = null,
        IReadOnlyList<EditorialPage>? editorialPages = null,
        IReadOnlyList<Tuple<FundamentalRecord, string, string>>? fundamentalsRows = null)
    {
        var services = new ServiceCollection();
        services.AddScoped<IOperationalConfigRepository>(_ => new StubOperationalConfigRepository(contactEmail, sameAsJson));
        services.AddScoped<IEditorialPageRepository>(_ => new StubEditorialPageRepository(editorialPages ?? []));
        services.AddScoped<IFundamentalRepository>(_ => new StubFundamentalRepository(fundamentalsRows ?? []));

        var provider = services.BuildServiceProvider();
        return new SpaMetadataProvider(
            BuildConfig(),
            new SeoDefaultsBuilder(),
            provider.GetRequiredService<IServiceScopeFactory>());
    }

    // Dominio de prueba DISTINTO del que estaba hardcodeado en el provider
    // (fibrasinmobiliarias.com). Cualquier reaparición de ese literal en el JSON-LD
    // significa que se reintrodujo un dominio hardcodeado → AC-1/T1.
    private const string TestBaseUrl = "https://test.fibradis.example";

    private static IConfiguration BuildConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["App:BaseUrl"] = TestBaseUrl,
            })
            .Build();

    private static string NormalizePath(string path)
    {
        var normalized = path.TrimEnd('/').ToLowerInvariant();
        return normalized.Length == 0 ? "/" : normalized;
    }

    private sealed class StubOperationalConfigRepository(string? contactEmail, string? sameAsJson) : IOperationalConfigRepository
    {
        public Task<OperationalConfig> GetAsync(CancellationToken ct = default)
            => Task.FromResult(new OperationalConfig
            {
                ContactEmail = contactEmail,
                OrganizationSameAsJson = sameAsJson,
            });

        public Task UpdateCetesRateAsync(decimal rate, DateTimeOffset updatedAt, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task UpdateTiieRateAsync(decimal rate, DateTimeOffset updatedAt, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task UpdateOrganizationSameAsAsync(string? organizationSameAsJson, string actor, CancellationToken ct = default)
            => throw new NotSupportedException();

        public Task UpdateAsync(decimal? commissionFactor, int? avgPeriods, int? newsCadenceMinutes, int? fibraNewsMonths, int? distributionCadenceMinutes, bool? termsEnabled, string? termsText, string? contactEmail, string actor, int? fundamentalsCadenceMinutes = null, int? universeDegradationThresholdPct = null, CancellationToken ct = default)
            => throw new NotSupportedException();
    }

    private sealed class StubEditorialPageRepository(IReadOnlyList<EditorialPage> pages) : IEditorialPageRepository
    {
        public Task<IReadOnlyList<EditorialPage>> GetAllAsync(CancellationToken ct = default)
            => Task.FromResult(pages);

        public Task<EditorialPage?> GetBySlugAsync(string slug, CancellationToken ct = default)
            => Task.FromResult<EditorialPage?>(pages.FirstOrDefault(page => page.Slug == slug));

        public Task<int> UpdateContentAsync(string slug, string content, CancellationToken ct = default)
            => throw new NotSupportedException();
    }

    private sealed class StubFundamentalRepository(IReadOnlyList<Tuple<FundamentalRecord, string, string>> rows) : IFundamentalRepository
    {
        public Task<FundamentalRecord?> GetByIdAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();
        public Task<FundamentalRecord?> GetProcessedByFibraAndPeriodAsync(Guid fibraId, string period, CancellationToken ct) => throw new NotSupportedException();
        public Task<FundamentalRecord?> GetLatestProcessedByFibraAsync(Guid fibraId, CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlyList<string>> GetProcessedPeriodsAsync(Guid fibraId, CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlyList<FundamentalRecord>> GetByFibraAsync(Guid fibraId, CancellationToken ct) => throw new NotSupportedException();
        public Task AddAsync(FundamentalRecord record, CancellationToken ct) => throw new NotSupportedException();
        public Task UpdateStatusAsync(Guid id, string status, string? confirmedBy, DateTimeOffset? confirmedAt, CancellationToken ct) => throw new NotSupportedException();
        public Task UpdatePdfReferenceAsync(Guid id, string pdfReference, CancellationToken ct) => throw new NotSupportedException();
        public Task UpdateMarkdownContentAsync(Guid id, string markdownContent, CancellationToken ct) => throw new NotSupportedException();
        public Task UpdateKpiExtractionAsync(Guid id, KpiExtractionResult result, CancellationToken ct) => throw new NotSupportedException();
        public Task UpdateKpisManualAsync(Guid id, decimal? capRate, decimal? navPerCbfi, decimal? ltv, decimal? noiMargin, decimal? ffoMargin, decimal? quarterlyDistribution, string? summary, CancellationToken ct) => throw new NotSupportedException();
        public Task UpdateFieldNotesAsync(Guid id, Dictionary<string, string?> notes, CancellationToken ct) => throw new NotSupportedException();
        public Task SoftDeleteAsync(Guid id, string deletedBy, CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlyList<(FundamentalRecord Record, string Ticker, string ShortName)>> GetSummaryLatestAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<(FundamentalRecord Record, string Ticker, string ShortName)>>(
                rows.Select(row => (row.Item1, row.Item2, row.Item3)).ToList());
        public Task<IReadOnlyList<(FundamentalRecord Record, string Ticker, string ShortName)>> GetSummaryByPeriodAsync(string period, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<(FundamentalRecord Record, string Ticker, string ShortName)>> GetSummaryForRecentPeriodsAsync(int count, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<string>> GetAllProcessedPeriodsAsync(CancellationToken ct = default) => throw new NotSupportedException();
    }
}
