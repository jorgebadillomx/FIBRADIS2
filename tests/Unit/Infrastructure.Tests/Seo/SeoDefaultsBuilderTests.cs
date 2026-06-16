using Domain.Catalog;
using Domain.Fundamentals;
using Domain.News;
using Domain.Market;
using Domain.Seo;
using Application.Seo;
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
            "FIBRAs Inmobiliarias — Análisis y Herramientas | Fibras Inmobiliarias",
            "Plataforma de análisis de FIBRAs inmobiliarias mexicanas. Precios en tiempo real, distribuciones, fundamentales y ranking de oportunidades.",
            "/",
            "{\"@type\":\"Organization\"}",
            "https://fibrasinmobiliarias.com",
            new DateTimeOffset(2026, 6, 13, 12, 0, 0, TimeSpan.Zero),
            "system");

        Assert.Equal(SeoPageType.Home, result.PageType);
        Assert.Equal("/", result.EntityKey);
        Assert.Equal("FIBRAs Inmobiliarias — Análisis y Herramientas | Fibras Inmobiliarias", result.Title);
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
    public void BuildBreadcrumbListJsonLd_UsesExactHierarchy()
    {
        var json = _builder.BuildBreadcrumbListJsonLd(
            "https://fibrasinmobiliarias.com/",
            [
                new SeoBreadcrumbItem("Inicio", "/"),
                new SeoBreadcrumbItem("Comparar", "/comparar"),
                new SeoBreadcrumbItem("Detalle", "/comparar/segmento"),
            ]);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.Equal("BreadcrumbList", root.GetProperty("@type").GetString());
        var items = root.GetProperty("itemListElement").EnumerateArray().ToArray();
        Assert.Equal(3, items.Length);
        Assert.Equal("Inicio", items[0].GetProperty("name").GetString());
        Assert.Equal("https://fibrasinmobiliarias.com/", items[0].GetProperty("item").GetString());
        Assert.Equal(2, items[1].GetProperty("position").GetInt32());
        Assert.Equal("https://fibrasinmobiliarias.com/comparar", items[1].GetProperty("item").GetString());
        Assert.Equal("https://fibrasinmobiliarias.com/comparar/segmento", items[2].GetProperty("item").GetString());
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
        Assert.Equal("Fibra Uno (FUNO11) | Fibras Inmobiliarias", result.Title);
        Assert.Equal(result.Title, result.OgTitle);
        Assert.Equal("Análisis de Fibra Uno (FUNO11): precio, yield, fundamentales (Cap Rate, NAV, LTV) y distribuciones. Sector Industrial en la BMV.", result.MetaDescription);
        Assert.Equal("/fibras/fibra-uno-funo11", result.CanonicalPath);
        Assert.Equal("website", result.OgType);
        Assert.Equal("https://fibrasinmobiliarias.com/og/fibras/FUNO11.png", result.OgImageUrl);
        Assert.Contains("\"@type\":\"FinancialProduct\"", result.JsonLd);
        Assert.Contains("\"description\":\"Análisis de Fibra Uno (FUNO11): precio, yield, fundamentales (Cap Rate, NAV, LTV) y distribuciones. Sector Industrial en la BMV.\"", result.JsonLd);
        Assert.DoesNotContain("BreadcrumbList", result.JsonLd);
        Assert.Equal("system", result.UpdatedBy);
    }

    [Fact]
    public void BuildComparePageJsonLd_UsesActiveFibras_WithExactOutput()
    {
        var json = _builder.BuildComparePageJsonLd(
            [
                ("Fibra Uno", "FUNO11"),
                ("Fibra Macquarie", "FIBRAMQ12"),
            ],
            "https://fibrasinmobiliarias.com");

        using var document = JsonDocument.Parse(json);
        var graph = document.RootElement.GetProperty("@graph").EnumerateArray().ToArray();

        var app = graph.Single(node => node.GetProperty("@type").GetString() == "WebApplication");
        Assert.Equal("Comparador de FIBRAs", app.GetProperty("name").GetString());
        Assert.Equal("https://fibrasinmobiliarias.com/comparar", app.GetProperty("url").GetString());

        var itemList = graph.Single(node => node.GetProperty("@type").GetString() == "ItemList");
        Assert.Equal(2, itemList.GetProperty("numberOfItems").GetInt32());
        var items = itemList.GetProperty("itemListElement").EnumerateArray().ToArray();
        Assert.Equal("Fibra Macquarie", items[0].GetProperty("name").GetString());
        Assert.Equal("https://fibrasinmobiliarias.com/fibras/fibra-macquarie-fibramq12", items[0].GetProperty("item").GetString());
        Assert.Equal("Fibra Uno", items[1].GetProperty("name").GetString());
        Assert.Equal("https://fibrasinmobiliarias.com/fibras/fibra-uno-funo11", items[1].GetProperty("item").GetString());
    }

    [Fact]
    public void BuildComparePageJsonLd_ReturnsStaticMinimumWhenNoFibers()
    {
        var json = _builder.BuildComparePageJsonLd([], "https://fibrasinmobiliarias.com");

        using var document = JsonDocument.Parse(json);
        var graph = document.RootElement.GetProperty("@graph").EnumerateArray().ToArray();

        Assert.Single(graph);
        Assert.Equal("WebApplication", graph[0].GetProperty("@type").GetString());
        Assert.Equal("Comparador de FIBRAs", graph[0].GetProperty("name").GetString());
    }

    [Fact]
    public void BuildFundamentalesPageJsonLd_UsesSummary_WithExactOutput()
    {
        var rows = new List<(FundamentalRecord Record, string Ticker, string ShortName)>
        {
            (
                new FundamentalRecord
                {
                    Id = Guid.NewGuid(),
                    FibraId = Guid.NewGuid(),
                    Period = "2T2026",
                    Status = "processed",
                    CapRate = 8.75m,
                    NavPerCbfi = 27.10m,
                    Ltv = 34.20m,
                    NoiMargin = 71.40m,
                    FfoMargin = 63.80m,
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
                    CapRate = 9.10m,
                    NavPerCbfi = 31.50m,
                    Ltv = 29.80m,
                    NoiMargin = 68.10m,
                    FfoMargin = 60.20m,
                    CapturedAt = new DateTimeOffset(2026, 6, 13, 8, 45, 0, TimeSpan.Zero),
                },
                "DANHOS13",
                "Danhos"),
        };

        var json = _builder.BuildFundamentalesPageJsonLd(rows, "https://fibrasinmobiliarias.com");

        using var document = JsonDocument.Parse(json);
        var graph = document.RootElement.GetProperty("@graph").EnumerateArray().ToArray();

        var dataset = graph.Single(node => node.GetProperty("@type").GetString() == "Dataset");
        Assert.Equal("https://fibrasinmobiliarias.com/fundamentales#dataset", dataset.GetProperty("@id").GetString());
        Assert.Equal("2026-06-13T08:45:00.0000000+00:00", dataset.GetProperty("dateModified").GetString());

        var variables = dataset.GetProperty("variableMeasured").EnumerateArray().ToArray();
        Assert.Equal(6, variables.Length);
        Assert.Equal("Cap Rate", variables[0].GetProperty("name").GetString());
        Assert.Equal("NAV por CBFI", variables[1].GetProperty("name").GetString());
        Assert.Equal("FIBRAs cubiertas", variables[5].GetProperty("name").GetString());
        Assert.Equal(2, variables[5].GetProperty("value").GetInt32());
    }

    [Fact]
    public void BuildFundamentalesPageJsonLd_ReturnsStaticMinimumWhenNoRows()
    {
        var json = _builder.BuildFundamentalesPageJsonLd([], "https://fibrasinmobiliarias.com");

        using var document = JsonDocument.Parse(json);
        var graph = document.RootElement.GetProperty("@graph").EnumerateArray().ToArray();

        Assert.Contains(graph, node => node.GetProperty("@type").GetString() == "Dataset");
        Assert.DoesNotContain("BreadcrumbList", json);
    }

    // Convención §Testing Funciones de Cálculo Financiero: el caso denominador = 0 va ANTES que
    // cualquier otro escenario. lastPrice = 0 → sin precio, sin yields, sin variaciones, SIN excepción
    // (todas las divisiones por precio están protegidas por `lastPrice is > 0m`).
    [Fact]
    public void BuildFibra_WithZeroPrice_OmitsPriceAndYieldProperties_NoDivisionByZero()
    {
        var fibra = new Fibra
        {
            Id = Guid.NewGuid(),
            Ticker = "FUNO11",
            FullName = "Fibra Uno",
            ShortName = "Fibra Uno",
            Sector = "Industrial",
            Market = "BMV",
            Currency = "MXN",
            State = FibraState.Active,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var marketData = new FibraSeoMarketData(
            new PriceSnapshot
            {
                FibraId = fibra.Id,
                Ticker = fibra.Ticker,
                LastPrice = 0m,
                Week52High = 28.10m,
                Week52Low = 20.80m,
                CapturedAt = new DateTimeOffset(2026, 6, 13, 11, 30, 0, TimeSpan.Zero),
                Status = MarketDataStatus.Processed,
            },
            new List<Distribution>
            {
                new()
                {
                    FibraId = fibra.Id,
                    Ticker = fibra.Ticker,
                    PaymentDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-20),
                    AmountPerUnit = 0.52m,
                    Currency = "MXN",
                },
            },
            0.67m,
            DateOnly.FromDateTime(DateTime.UtcNow));

        var result = _builder.BuildFibra(
            fibra,
            "https://fibrasinmobiliarias.com",
            Now,
            "system",
            marketData);

        using var document = JsonDocument.Parse(result.JsonLd!);
        var product = document.RootElement
            .GetProperty("@graph")
            .EnumerateArray()
            .First(node => node.GetProperty("@type").GetString() == "FinancialProduct");

        Assert.False(product.TryGetProperty("offers", out _));
        Assert.False(product.TryGetProperty("additionalProperty", out _));
        Assert.Equal("2026-06-13T11:30:00.0000000+00:00", product.GetProperty("dateModified").GetString());
    }

    [Fact]
    public void BuildFibra_WithLiveData_AddsFinancialProductFields_WithExactOutput()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
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
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var marketData = new FibraSeoMarketData(
            new PriceSnapshot
            {
                FibraId = fibra.Id,
                Ticker = fibra.Ticker,
                LastPrice = 21.50m,
                Week52High = 28.10m,
                Week52Low = 20.80m,
                CapturedAt = new DateTimeOffset(2026, 6, 13, 11, 30, 0, TimeSpan.Zero),
                Status = MarketDataStatus.Processed,
            },
            new List<Distribution>
            {
                new()
                {
                    FibraId = fibra.Id,
                    Ticker = fibra.Ticker,
                    PaymentDate = today.AddDays(-20),
                    AmountPerUnit = 0.52m,
                    Currency = "MXN",
                },
                new()
                {
                    FibraId = fibra.Id,
                    Ticker = fibra.Ticker,
                    PaymentDate = today.AddDays(-110),
                    AmountPerUnit = 0.47m,
                    Currency = "MXN",
                },
            },
            0.67m,
            today);

        var result = _builder.BuildFibra(
            fibra,
            "https://fibrasinmobiliarias.com",
            new DateTimeOffset(2026, 6, 13, 12, 0, 0, TimeSpan.Zero),
            "system",
            marketData);

        using var document = JsonDocument.Parse(result.JsonLd!);
        var product = document.RootElement
            .GetProperty("@graph")
            .EnumerateArray()
            .First(node => node.GetProperty("@type").GetString() == "FinancialProduct");

        // Precio modelado como PropertyValue (no Offer) — decisión D1 code review.
        Assert.False(product.TryGetProperty("offers", out _));
        Assert.Equal("2026-06-13T11:30:00.0000000+00:00", product.GetProperty("dateModified").GetString());

        var additional = product.GetProperty("additionalProperty").EnumerateArray().ToArray();
        Assert.Equal(5, additional.Length);
        Assert.Equal("Precio de cotización", additional[0].GetProperty("name").GetString());
        Assert.Equal(21.50m, additional[0].GetProperty("value").GetDecimal());
        Assert.Equal("MXN", additional[0].GetProperty("unitText").GetString());
        Assert.Equal("Yield TTM anualizado", additional[1].GetProperty("name").GetString());
        Assert.Equal(4.60m, additional[1].GetProperty("value").GetDecimal());
        Assert.Equal("Yield decretado", additional[2].GetProperty("name").GetString());
        Assert.Equal(12.47m, additional[2].GetProperty("value").GetDecimal());
        Assert.Equal("Variación vs máximo 52 semanas", additional[3].GetProperty("name").GetString());
        Assert.Equal(-23.49m, additional[3].GetProperty("value").GetDecimal());
        Assert.Equal("Variación vs mínimo 52 semanas", additional[4].GetProperty("name").GetString());
        Assert.Equal(3.37m, additional[4].GetProperty("value").GetDecimal());
    }

    [Fact]
    public void BuildFibra_WithoutPrice_OmitsOffersAndYieldProperties()
    {
        var fibra = new Fibra
        {
            Id = Guid.NewGuid(),
            Ticker = "MQ",
            FullName = "Mq",
            ShortName = "Mq",
            Sector = "",
            Market = "BMV",
            Currency = "MXN",
            State = FibraState.Active,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var marketData = new FibraSeoMarketData(
            new PriceSnapshot
            {
                FibraId = fibra.Id,
                Ticker = fibra.Ticker,
                LastPrice = null,
                CapturedAt = new DateTimeOffset(2026, 6, 13, 11, 30, 0, TimeSpan.Zero),
                Status = MarketDataStatus.Processed,
            },
            [],
            null,
            DateOnly.FromDateTime(DateTime.UtcNow));

        var result = _builder.BuildFibra(
            fibra,
            "https://fibrasinmobiliarias.com",
            Now,
            "system",
            marketData);

        using var document = JsonDocument.Parse(result.JsonLd!);
        var product = document.RootElement
            .GetProperty("@graph")
            .EnumerateArray()
            .First(node => node.GetProperty("@type").GetString() == "FinancialProduct");

        Assert.False(product.TryGetProperty("offers", out _));
        Assert.False(product.TryGetProperty("additionalProperty", out _));
        Assert.Equal("2026-06-13T11:30:00.0000000+00:00", product.GetProperty("dateModified").GetString());
    }

    [Fact]
    public void BuildFibra_WithoutDistributions_OmitsYieldTtm_ButKeepsPriceAndDateModified()
    {
        var fibra = new Fibra
        {
            Id = Guid.NewGuid(),
            Ticker = "FUNO11",
            FullName = "Fibra Uno",
            ShortName = "Fibra Uno",
            Sector = "Industrial",
            Market = "BMV",
            Currency = "MXN",
            State = FibraState.Active,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var marketData = new FibraSeoMarketData(
            new PriceSnapshot
            {
                FibraId = fibra.Id,
                Ticker = fibra.Ticker,
                LastPrice = 21.50m,
                Week52High = 28.10m,
                Week52Low = 20.80m,
                CapturedAt = new DateTimeOffset(2026, 6, 13, 11, 30, 0, TimeSpan.Zero),
                Status = MarketDataStatus.Processed,
            },
            [],
            null,
            DateOnly.FromDateTime(DateTime.UtcNow));

        var result = _builder.BuildFibra(
            fibra,
            "https://fibrasinmobiliarias.com",
            Now,
            "system",
            marketData);

        using var document = JsonDocument.Parse(result.JsonLd!);
        var product = document.RootElement
            .GetProperty("@graph")
            .EnumerateArray()
            .First(node => node.GetProperty("@type").GetString() == "FinancialProduct");

        // Precio modelado como PropertyValue (no Offer) — decisión D1 code review.
        Assert.False(product.TryGetProperty("offers", out _));
        Assert.Equal("2026-06-13T11:30:00.0000000+00:00", product.GetProperty("dateModified").GetString());

        var additional = product.GetProperty("additionalProperty").EnumerateArray().ToArray();
        Assert.Equal("Precio de cotización", additional[0].GetProperty("name").GetString());
        Assert.Equal(21.50m, additional[0].GetProperty("value").GetDecimal());
        Assert.DoesNotContain(additional, property => property.GetProperty("name").GetString() == "Yield TTM anualizado");
        Assert.DoesNotContain(additional, property => property.GetProperty("name").GetString() == "Yield decretado");
        Assert.Contains(additional, property => property.GetProperty("name").GetString() == "Variación vs máximo 52 semanas");
        Assert.Contains(additional, property => property.GetProperty("name").GetString() == "Variación vs mínimo 52 semanas");
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
        Assert.Equal("FUNO11 reporta resultados del 2T25 — Noticias | Fibras Inmobiliarias", result.Title);
        Assert.Equal(result.Title, result.OgTitle);
        Assert.Equal("Texto corto. — Análisis y noticias de FIBRAs inmobiliarias en Fibras Inmobiliarias: resultados, distribuciones y mercado inmobiliario bursátil de México.", result.MetaDescription);
        Assert.Equal("/noticias/funo11-reporta-resultados-del-2t25", result.CanonicalPath);
        Assert.Equal("article", result.OgType);
        Assert.Equal("https://fibrasinmobiliarias.com/og-image.png", result.OgImageUrl);
        Assert.Contains("\"@type\":\"NewsArticle\"", result.JsonLd);
        Assert.Contains("\"headline\":\"FUNO11 reporta resultados del 2T25\"", result.JsonLd);
        Assert.Contains("\"description\":\"Texto corto. — Análisis y noticias de FIBRAs inmobiliarias en Fibras Inmobiliarias: resultados, distribuciones y mercado inmobiliario bursátil de México.\"", result.JsonLd);
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
