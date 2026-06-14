using Domain.Catalog;
using Domain.News;
using Domain.Seo;
using Infrastructure.Seo;
using System.Text.Json;

namespace Infrastructure.Tests.Seo;

public class SeoDefaultsBuilderTests
{
    private readonly SeoDefaultsBuilder _builder = new();
    private static readonly DateTimeOffset Now = new(2026, 6, 13, 12, 0, 0, TimeSpan.Zero);

    // Convención §Middleware SEO (no negociable): toda meta description generada debe medir
    // entre 120 (piso) y 160 (techo) chars. Cubre nombres/snippets cortos (piso) y largos (techo).
    [Theory]
    [InlineData("Fibra Uno", "FUNO11", "Industrial")]                                   // caso típico
    [InlineData("Mq", "MQ", "")]                                                         // nombre corto + sin sector → fuerza el piso
    [InlineData("Concentradora Fibra Danhos con una razón social deliberadamente extensa", "DANHOS13", "Comercial y oficinas premium")] // fuerza el techo
    public void BuildFibra_Description_IsBetween120And160Chars(string fullName, string ticker, string sector)
    {
        var fibra = new Fibra
        {
            Id = Guid.NewGuid(),
            Ticker = ticker,
            FullName = fullName,
            ShortName = fullName,
            Sector = sector,
            Market = "BMV",
            Currency = "MXN",
            State = FibraState.Active,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var result = _builder.BuildFibra(fibra, "https://fibrasinmobiliarias.com", Now, "system");

        Assert.InRange(result.MetaDescription.Length, 120, 160);
        Assert.InRange(result.OgDescription.Length, 120, 160);
    }

    [Theory]
    [InlineData("Texto corto.")]                                                          // snippet corto → padding al piso
    [InlineData("")]                                                                      // sin contenido → frase de marca completa
    [InlineData("FUNO11 reporta resultados del segundo trimestre con un crecimiento sostenido en ingresos por arrendamiento, mejora del nivel de ocupación de su portafolio industrial y comercial, y un incremento relevante en la distribución por CBFI anunciada al mercado.")] // largo → truncado al techo
    public void BuildNews_Description_IsBetween120And160Chars(string snippet)
    {
        var article = new NewsArticle
        {
            Id = Guid.NewGuid(),
            Title = "Título de prueba",
            TitleNormalized = "titulo de prueba",
            Slug = "titulo-de-prueba",
            Source = "Fuente",
            PublishedAt = new DateTimeOffset(2026, 6, 10, 12, 0, 0, TimeSpan.Zero),
            Url = "https://example.com/nota",
            Snippet = snippet,
            Status = NewsArticleStatus.Processed,
            CapturedAt = DateTimeOffset.UtcNow,
        };

        var result = _builder.BuildNews(article, "https://fibrasinmobiliarias.com", Now, "system");

        Assert.InRange(result.MetaDescription.Length, 120, 160);
        Assert.InRange(result.OgDescription.Length, 120, 160);
    }

    [Fact]
    public void BuildStaticPage_MapsCurrentMetadataValues()
    {
        var result = _builder.BuildStaticPage(
            SeoPageType.Home,
            "/",
            "FIBRAs Inmobiliarias — Análisis y Herramientas | FIBRADIS",
            "Plataforma de análisis de FIBRAs inmobiliarias mexicanas. Precios en tiempo real, distribuciones, fundamentales y ranking de oportunidades.",
            "/",
            "{\"@type\":\"Organization\"}",
            "https://fibrasinmobiliarias.com",
            new DateTimeOffset(2026, 6, 13, 12, 0, 0, TimeSpan.Zero),
            "system");

        Assert.Equal(SeoPageType.Home, result.PageType);
        Assert.Equal("/", result.EntityKey);
        Assert.Equal("FIBRAs Inmobiliarias — Análisis y Herramientas | FIBRADIS", result.Title);
        Assert.Equal(result.Title, result.OgTitle);
        Assert.Equal("Plataforma de análisis de FIBRAs inmobiliarias mexicanas. Precios en tiempo real, distribuciones, fundamentales y ranking de oportunidades.", result.MetaDescription);
        Assert.Equal("/", result.CanonicalPath);
        Assert.Equal("website", result.OgType);
        Assert.Equal("https://fibrasinmobiliarias.com/og-image.png", result.OgImageUrl);
        Assert.Equal("es_MX", result.OgLocale);
        Assert.Equal("summary_large_image", result.TwitterCard);
        Assert.Equal("index,follow,max-image-preview:large,max-snippet:-1,max-video-preview:-1", result.RobotsDirectives);
        Assert.Equal("{\"@type\":\"Organization\"}", result.JsonLd);
        Assert.False(result.TitleIsOverridden);
        Assert.Equal("system", result.UpdatedBy);
    }

    [Fact]
    public void BuildFibra_BuildsCleanDescription_WithExactOutput()
    {
        var fibra = new Fibra
        {
            Id = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000001"),
            Ticker = "FUNO11",
            FullName = "Fibra Uno",
            ShortName = "Fibra Uno",
            Sector = "Industrial",
            Market = "BMV",
            Currency = "MXN",
            State = FibraState.Active,
            Description = "  ?? Fibra Uno | FUNO11 Ticker: FUNO11 Fecha de constitución: 10 de enero de 2011 | Campo | Detalle | | --- | --- |",
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var result = _builder.BuildFibra(
            fibra,
            "https://fibrasinmobiliarias.com",
            new DateTimeOffset(2026, 6, 13, 12, 0, 0, TimeSpan.Zero),
            "system");

        Assert.Equal(SeoPageType.Fibra, result.PageType);
        Assert.Equal("FUNO11", result.EntityKey);
        Assert.Equal("Fibra Uno (FUNO11) | FIBRADIS — Fibras Inmobiliarias", result.Title);
        Assert.Equal(result.Title, result.OgTitle);
        Assert.Equal("Análisis de Fibra Uno (FUNO11): precio, yield, fundamentales (Cap Rate, NAV, LTV) y distribuciones. Sector Industrial en la BMV.", result.MetaDescription);
        Assert.Equal("/fibras/fibra-uno-funo11", result.CanonicalPath);
        Assert.Equal("website", result.OgType);
        Assert.Equal("https://fibrasinmobiliarias.com/og-image.png", result.OgImageUrl);
        Assert.Contains("\"@type\":\"FinancialProduct\"", result.JsonLd);
        Assert.Contains("\"@type\":\"BreadcrumbList\"", result.JsonLd);
        Assert.Contains("\"description\":\"Análisis de Fibra Uno (FUNO11): precio, yield, fundamentales (Cap Rate, NAV, LTV) y distribuciones. Sector Industrial en la BMV.\"", result.JsonLd);
        Assert.Equal("system", result.UpdatedBy);
    }

    [Fact]
    public void BuildNews_UsesHeadlineAndShortSnippet_WithExactOutput()
    {
        var article = new NewsArticle
        {
            Id = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001"),
            Title = "FUNO11 reporta resultados del 2T25",
            TitleNormalized = "funo11 reporta resultados del 2t25",
            Slug = "funo11-reporta-resultados-del-2t25",
            Source = "El Economista",
            PublishedAt = new DateTimeOffset(2026, 6, 10, 12, 0, 0, TimeSpan.Zero),
            Url = "https://example.com/nota",
            Snippet = "Texto corto.",
            Status = NewsArticleStatus.Processed,
            CapturedAt = DateTimeOffset.UtcNow,
        };

        var result = _builder.BuildNews(
            article,
            "https://fibrasinmobiliarias.com",
            new DateTimeOffset(2026, 6, 13, 12, 0, 0, TimeSpan.Zero),
            "system");

        Assert.Equal(SeoPageType.News, result.PageType);
        Assert.Equal("funo11-reporta-resultados-del-2t25", result.EntityKey);
        Assert.Equal("FUNO11 reporta resultados del 2T25 — Noticias | FIBRADIS", result.Title);
        Assert.Equal(result.Title, result.OgTitle);
        Assert.Equal("Texto corto. — Análisis y noticias de FIBRAs inmobiliarias en FIBRADIS: resultados, distribuciones y mercado inmobiliario bursátil de México.", result.MetaDescription);
        Assert.Equal("/noticias/funo11-reporta-resultados-del-2t25", result.CanonicalPath);
        Assert.Equal("article", result.OgType);
        Assert.Equal("https://fibrasinmobiliarias.com/og-image.png", result.OgImageUrl);
        Assert.Contains("\"@type\":\"NewsArticle\"", result.JsonLd);
        Assert.Contains("\"headline\":\"FUNO11 reporta resultados del 2T25\"", result.JsonLd);
        Assert.Contains("\"description\":\"Texto corto. — Análisis y noticias de FIBRAs inmobiliarias en FIBRADIS: resultados, distribuciones y mercado inmobiliario bursátil de México.\"", result.JsonLd);
        Assert.Equal("system", result.UpdatedBy);
    }

    [Fact]
    public void BuildFaqPageJsonLd_UsesOnlyActiveItems_AndStripsMarkdown()
    {
        var items = new List<FaqItem>
        {
            new()
            {
                Id = Guid.NewGuid(),
                PageType = SeoPageType.StaticPage,
                EntityKey = "/fundamentales",
                Question = "¿Qué es Cap Rate?",
                Answer = "**Cap Rate** = NOI anualizado / Valor de propiedades de inversión",
                Order = 2,
                IsActive = true,
                UpdatedAt = Now,
                UpdatedBy = "system",
            },
            new()
            {
                Id = Guid.NewGuid(),
                PageType = SeoPageType.StaticPage,
                EntityKey = "/fundamentales",
                Question = "¿Qué es NAV por CBFI?",
                Answer = "NAV/CBFI = NAV / CBFIs en circulación",
                Order = 1,
                IsActive = true,
                UpdatedAt = Now,
                UpdatedBy = "system",
            },
            new()
            {
                Id = Guid.NewGuid(),
                PageType = SeoPageType.StaticPage,
                EntityKey = "/fundamentales",
                Question = "¿FAQ inactiva?",
                Answer = "No debe salir.",
                Order = 3,
                IsActive = false,
                UpdatedAt = Now,
                UpdatedBy = "system",
            },
        };

        var json = _builder.BuildFaqPageJsonLd(items);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.Equal("FAQPage", root.GetProperty("@type").GetString());

        var mainEntity = root.GetProperty("mainEntity").EnumerateArray().ToArray();
        Assert.Equal(2, mainEntity.Length);
        Assert.Equal("¿Qué es NAV por CBFI?", mainEntity[0].GetProperty("name").GetString());
        Assert.Equal("NAV/CBFI = NAV / CBFIs en circulación", mainEntity[0].GetProperty("acceptedAnswer").GetProperty("text").GetString());
        Assert.Equal("¿Qué es Cap Rate?", mainEntity[1].GetProperty("name").GetString());
        Assert.Equal("Cap Rate = NOI anualizado / Valor de propiedades de inversión", mainEntity[1].GetProperty("acceptedAnswer").GetProperty("text").GetString());
    }

    [Fact]
    public void BuildFaqPageJsonLd_ReturnsEmptyStringWhenNothingActive()
    {
        var json = _builder.BuildFaqPageJsonLd(
            [
                new FaqItem
                {
                    Id = Guid.NewGuid(),
                    PageType = SeoPageType.StaticPage,
                    EntityKey = "/fundamentales",
                    Question = "Pregunta",
                    Answer = "Respuesta",
                    Order = 1,
                    IsActive = false,
                    UpdatedAt = Now,
                    UpdatedBy = "system",
                },
            ]);

        Assert.Equal(string.Empty, json);
    }
}
