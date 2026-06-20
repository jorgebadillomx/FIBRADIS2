using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text.Unicode;
using Application.Catalog;
using Application.Fundamentals;
using Application.Market;
using Application.Seo;
using Domain.Catalog;
using Domain.Fundamentals;
using Domain.News;
using Domain.Market;
using Domain.Seo;

namespace Infrastructure.Seo;

public partial class SeoDefaultsBuilder : ISeoDefaultsBuilder
{
    private const string BrandName = "Fibras Inmobiliarias";
    private const string BrandTitleSuffix = " | Fibras Inmobiliarias";
    private const string FibraTitleSuffix = " | Fibras Inmobiliarias";
    private const string OgImagePath = "/og-image.png";
    private const string OgLocale = "es_MX";
    private const string TwitterCard = "summary_large_image";
    private const string DefaultRobotsDirectives = "index,follow,max-image-preview:large,max-snippet:-1,max-video-preview:-1";
    private const string CompareDescription =
        "Compara hasta 4 FIBRAs mexicanas lado a lado en precio, yield, fundamentales y score de oportunidad público.";
    private const string FundamentalsDescription =
        "Dataset con las métricas fundamentales más recientes de FIBRAs mexicanas: Cap Rate, NAV por CBFI, LTV, NOI Margin y FFO Margin.";
    private const string NewsTitleSuffix = " — Noticias | " + BrandName;
    private const int NewsTitleMaxLength = 120;
    private const int NewsMaxDescriptionLength = 160;
    private const int NewsMinDescriptionLength = 120;
    private const int FibraMaxDescriptionLength = 155;
    private const int FibraMinDescriptionLength = 120;
    private const string FibraDescriptionPadSuffix = " Consulta su análisis completo en Fibras Inmobiliarias.";

    private static readonly JsonSerializerOptions JsonLdOptions = new()
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
    };

    [GeneratedRegex(@"^\s*\|?(\s*:?-+:?\s*\|)+\s*$", RegexOptions.Multiline)]
    private static partial Regex TableSeparatorRowRegex();

    [GeneratedRegex(@"\|")]
    private static partial Regex TablePipeRegex();

    [GeneratedRegex(@"\[([^\]]*)\]\([^)]*\)")]
    private static partial Regex MarkdownLinkRegex();

    [GeneratedRegex(@"[*_`#>]+")]
    private static partial Regex MarkdownSyntaxRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRunRegex();

    public string BuildBreadcrumbListJsonLd(string baseUrl, IReadOnlyList<SeoBreadcrumbItem> items)
    {
        var breadcrumbItems = items
            .Where(item => !string.IsNullOrWhiteSpace(item.Name) && !string.IsNullOrWhiteSpace(item.Path))
            .Select((item, index) => new Dictionary<string, object?>
            {
                ["@type"] = "ListItem",
                ["position"] = index + 1,
                ["name"] = item.Name.Trim(),
                ["item"] = $"{TrimBaseUrl(baseUrl)}{NormalizeRoutePath(item.Path)}",
            })
            .ToArray();

        if (breadcrumbItems.Length == 0)
            return string.Empty;

        return JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "BreadcrumbList",
            ["itemListElement"] = breadcrumbItems,
        }, JsonLdOptions);
    }

    public SeoMetadata BuildStaticPage(
        SeoPageType pageType,
        string entityKey,
        string title,
        string description,
        string canonicalPath,
        string? jsonLd,
        string baseUrl,
        DateTimeOffset updatedAt,
        string updatedBy = "system")
        => CreateBase(
            pageType,
            entityKey,
            title,
            description,
            canonicalPath,
            jsonLd,
            baseUrl,
            updatedAt,
            updatedBy,
            ogType: "website");

    public SeoMetadata BuildFibra(
        Fibra fibra,
        string baseUrl,
        DateTimeOffset updatedAt,
        string updatedBy = "system",
        FibraSeoMarketData? marketData = null)
    {
        var canonicalSlug = FibraSlug.Build(fibra.FullName, fibra.Ticker);
        var canonicalPath = $"/fibras/{canonicalSlug}";
        var title = $"{fibra.FullName} ({fibra.Ticker}){FibraTitleSuffix}";
        var description = BuildFibraDescription(fibra);
        var ogImageUrl = BuildFibraOgImageUrl(baseUrl, fibra.Ticker);

        var jsonLd = BuildFibraJsonLd(fibra, baseUrl, canonicalPath, description, marketData);

        return CreateBase(
            SeoPageType.Fibra,
            fibra.Ticker.Trim().ToUpperInvariant(),
            title,
            description,
            canonicalPath,
            jsonLd,
            baseUrl,
            updatedAt,
            updatedBy,
            ogType: "website",
            ogImageUrl: ogImageUrl);
    }

    public SeoMetadata BuildNews(
        NewsArticle article,
        string baseUrl,
        DateTimeOffset updatedAt,
        string updatedBy = "system")
    {
        var analysis = TryDeserializeAnalysis(article.AiAnalysisJson);
        var headline = analysis?.Headline ?? article.Title;
        var maxHeadlineLength = NewsTitleMaxLength - NewsTitleSuffix.Length - 1;
        if (headline.Length > maxHeadlineLength + 1)
            headline = TruncateAtWordBoundary(headline, maxHeadlineLength);
        var title = $"{headline}{NewsTitleSuffix}";
        var description = BuildNewsDescription(
            analysis?.SummaryMarkdown ?? article.AiSummary ?? article.Snippet ?? string.Empty);
        var canonicalKey = article.Slug ?? article.Id.ToString();
        var canonicalPath = $"/noticias/{canonicalKey}";
        var publishedIso = article.PublishedAt.ToString("o");

        var jsonLd = JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "NewsArticle",
            ["headline"] = headline,
            ["datePublished"] = publishedIso,
            ["author"] = new Dictionary<string, object?> { ["@type"] = "Organization", ["name"] = article.Source },
            ["publisher"] = new Dictionary<string, object?>
            {
                ["@type"] = "Organization",
                ["name"] = BrandName,
                ["url"] = TrimBaseUrl(baseUrl),
                ["logo"] = new Dictionary<string, object?>
                {
                    ["@type"] = "ImageObject",
                    ["url"] = $"{TrimBaseUrl(baseUrl)}/logo.png",
                    ["width"] = 512,
                    ["height"] = 512,
                },
            },
            ["url"] = $"{TrimBaseUrl(baseUrl)}{canonicalPath}",
            ["description"] = description,
        }, JsonLdOptions);

        return CreateBase(
            SeoPageType.News,
            canonicalKey,
            title,
            description,
            canonicalPath,
            jsonLd,
            baseUrl,
            updatedAt,
            updatedBy,
            ogType: "article",
            ogImageUrl: article.ImageUrl ?? $"{TrimBaseUrl(baseUrl)}{OgImagePath}");
    }

    public string BuildComparePageJsonLd(
        IReadOnlyList<(string FullName, string Ticker)> fibras,
        string baseUrl)
    {
        var trimmedBaseUrl = TrimBaseUrl(baseUrl);
        var activeFibras = fibras
            .Where(fibra => !string.IsNullOrWhiteSpace(fibra.FullName) && !string.IsNullOrWhiteSpace(fibra.Ticker))
            .OrderBy(fibra => fibra.Ticker, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var appNode = new Dictionary<string, object?>
        {
            ["@type"] = "WebApplication",
            ["@id"] = $"{trimmedBaseUrl}/comparar#app",
            ["name"] = "Comparador de FIBRAs",
            ["url"] = $"{trimmedBaseUrl}/comparar",
            ["applicationCategory"] = "FinanceApplication",
            ["operatingSystem"] = "Web",
            ["provider"] = new Dictionary<string, object?>
            {
                ["@id"] = $"{trimmedBaseUrl}/#organization",
            },
            ["description"] = CompareDescription,
        };

        var graph = new List<object?> { appNode };

        if (activeFibras.Length > 0)
        {
            graph.Add(new Dictionary<string, object?>
            {
                ["@type"] = "ItemList",
                ["name"] = "FIBRAs comparables",
                ["numberOfItems"] = activeFibras.Length,
                ["itemListOrder"] = "https://schema.org/ItemListOrderAscending",
                ["itemListElement"] = activeFibras
                    .Select((fibra, index) =>
                    {
                        var itemUrl = $"{trimmedBaseUrl}/fibras/{FibraSlug.Build(fibra.FullName, fibra.Ticker)}";
                        return new Dictionary<string, object?>
                        {
                            ["@type"] = "ListItem",
                            ["position"] = index + 1,
                            ["name"] = fibra.FullName,
                            ["item"] = itemUrl,
                        };
                    })
                    .ToArray(),
            });
        }

        return JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@graph"] = graph.ToArray(),
        }, JsonLdOptions);
    }

    public string BuildFundamentalesPageJsonLd(
        IReadOnlyList<(FundamentalRecord Record, string Ticker, string ShortName)> rows,
        string baseUrl)
    {
        var trimmedBaseUrl = TrimBaseUrl(baseUrl);
        var summaryRows = rows
            .Where(row => row.Record is not null && !string.IsNullOrWhiteSpace(row.Ticker))
            .ToArray();

        var coveredCount = summaryRows
            .Select(row => row.Ticker)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        var latestCapturedAt = summaryRows.Length > 0
            ? summaryRows.Max(row => row.Record.CapturedAt)
            : (DateTimeOffset?)null;

        var organizationNode = new Dictionary<string, object?>
        {
            ["@type"] = "Organization",
            ["@id"] = $"{trimmedBaseUrl}/#organization",
            ["name"] = BrandName,
            ["url"] = trimmedBaseUrl,
            ["logo"] = new Dictionary<string, object?>
            {
                ["@type"] = "ImageObject",
                ["url"] = $"{trimmedBaseUrl}/logo.png",
                ["width"] = 512,
                ["height"] = 512,
            },
        };

        var variableMeasured = new List<Dictionary<string, object?>>
        {
            new() { ["@type"] = "PropertyValue", ["name"] = "Cap Rate" },
            new() { ["@type"] = "PropertyValue", ["name"] = "NAV por CBFI" },
            new() { ["@type"] = "PropertyValue", ["name"] = "LTV" },
            new() { ["@type"] = "PropertyValue", ["name"] = "NOI Margin" },
            new() { ["@type"] = "PropertyValue", ["name"] = "FFO Margin" },
            new() { ["@type"] = "PropertyValue", ["name"] = "FIBRAs cubiertas", ["value"] = coveredCount },
        };

        var datasetNode = new Dictionary<string, object?>
        {
            ["@type"] = "Dataset",
            ["@id"] = $"{trimmedBaseUrl}/fundamentales#dataset",
            ["name"] = "Fundamentales FIBRAs — Cap Rate, NAV, NOI | Fibras Inmobiliarias",
            ["description"] = coveredCount > 0
                ? $"Dataset comparativo con métricas fundamentales de {coveredCount} FIBRAs mexicanas."
                : FundamentalsDescription,
            ["url"] = $"{trimmedBaseUrl}/fundamentales",
            ["creator"] = new Dictionary<string, object?> { ["@id"] = $"{trimmedBaseUrl}/#organization" },
            ["publisher"] = new Dictionary<string, object?> { ["@id"] = $"{trimmedBaseUrl}/#organization" },
            ["measurementTechnique"] = "Comparativa de fundamentales de FIBRAs mexicanas",
            ["variableMeasured"] = variableMeasured.ToArray(),
        };

        if (latestCapturedAt is not null)
            datasetNode["dateModified"] = latestCapturedAt.Value.ToString("o");

        return JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@graph"] = new object[] { organizationNode, datasetNode },
        }, JsonLdOptions);
    }

    public string BuildFaqPageJsonLd(IReadOnlyList<FaqItem> items)
    {
        var mainEntity = items
            .Where(item => item.IsActive)
            .OrderBy(item => item.Order)
            .ThenBy(item => item.Question, StringComparer.Ordinal)
            .Select(item => new Dictionary<string, object?>
            {
                ["@type"] = "Question",
                ["name"] = StripMarkdown(item.Question),
                ["acceptedAnswer"] = new Dictionary<string, object?>
                {
                    ["@type"] = "Answer",
                    ["text"] = StripMarkdown(item.Answer),
                },
            })
            .ToArray();

        if (mainEntity.Length == 0)
            return string.Empty;

        return JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "FAQPage",
            ["mainEntity"] = mainEntity,
        }, JsonLdOptions);
    }

    private static SeoMetadata CreateBase(
        SeoPageType pageType,
        string entityKey,
        string title,
        string description,
        string canonicalPath,
        string? jsonLd,
        string baseUrl,
        DateTimeOffset updatedAt,
        string updatedBy,
        string ogType,
        string? ogImageUrl = null)
        => new()
        {
            Id = Guid.NewGuid(),
            PageType = pageType,
            EntityKey = NormalizeEntityKey(entityKey),
            Title = title,
            MetaDescription = description,
            CanonicalPath = canonicalPath,
            OgTitle = title,
            OgDescription = description,
            OgType = ogType,
            OgImageUrl = ogImageUrl ?? $"{TrimBaseUrl(baseUrl)}{OgImagePath}",
            OgLocale = OgLocale,
            TwitterCard = TwitterCard,
            RobotsDirectives = DefaultRobotsDirectives,
            JsonLd = jsonLd,
            IsActive = true,
            UpdatedAt = updatedAt,
            UpdatedBy = updatedBy,
            TitleIsOverridden = false,
            MetaDescriptionIsOverridden = false,
            CanonicalPathIsOverridden = false,
            OgTitleIsOverridden = false,
            OgDescriptionIsOverridden = false,
            OgTypeIsOverridden = false,
            OgImageUrlIsOverridden = false,
            OgLocaleIsOverridden = false,
            TwitterCardIsOverridden = false,
            RobotsDirectivesIsOverridden = false,
            JsonLdIsOverridden = false,
        };

    private static string NormalizeEntityKey(string entityKey)
    {
        var normalized = entityKey.Trim();
        if (normalized.Length == 0)
            return normalized;

        return normalized == "/" ? "/" : normalized.TrimEnd('/');
    }

    private static string NormalizeRoutePath(string path)
    {
        var normalized = path.Trim();
        if (normalized.Length == 0)
            return "/";

        normalized = normalized.StartsWith('/') ? normalized : $"/{normalized}";
        return normalized == "/" ? "/" : normalized.TrimEnd('/');
    }

    private static string TrimBaseUrl(string baseUrl) => baseUrl.TrimEnd('/');

    private static string BuildFibraOgImageUrl(string baseUrl, string ticker)
        => $"{TrimBaseUrl(baseUrl)}/og/fibras/{ticker.Trim().ToUpperInvariant()}.png";

    private static string BuildFibraJsonLd(
        Fibra fibra,
        string baseUrl,
        string canonicalPath,
        string description,
        FibraSeoMarketData? marketData)
    {
        var trimmedBaseUrl = TrimBaseUrl(baseUrl);
        var productNode = new Dictionary<string, object?>
        {
            ["@type"] = "FinancialProduct",
            ["@id"] = $"{trimmedBaseUrl}{canonicalPath}#product",
            ["name"] = fibra.FullName,
            ["alternateName"] = fibra.Ticker,
            ["description"] = description,
            ["url"] = $"{trimmedBaseUrl}{canonicalPath}",
            ["provider"] = new Dictionary<string, object?>
            {
                ["@type"] = "Organization",
                ["name"] = BrandName,
                ["url"] = trimmedBaseUrl,
            },
            ["category"] = fibra.Sector,
            ["additionalType"] = "https://en.wikipedia.org/wiki/Real_estate_investment_trust",
        };

        if (marketData is not null)
        {
            var snapshot = marketData.LatestSnapshot;
            var lastPrice = snapshot?.LastPrice;

            if (snapshot is not null)
                productNode["dateModified"] = snapshot.CapturedAt.ToString("o");

            var additionalProperties = new List<Dictionary<string, object?>>();
            var asOfDate = marketData.AsOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

            if (lastPrice is > 0m)
            {
                // Precio de cotización modelado como PropertyValue, NO como Offer:
                // un CBFI no es "algo en venta" y la semántica de schema.org Offer es inapropiada
                // para un precio de mercado (decisión D1 del code review 2026-06-13).
                additionalProperties.Add(CreatePropertyValue(
                    "Precio de cotización",
                    lastPrice.Value,
                    string.IsNullOrWhiteSpace(fibra.Currency) ? "MXN" : fibra.Currency.Trim()));

                var annualizedYield = YieldCalculator.Calculate(marketData.Distributions, lastPrice, asOfDate);
                if (annualizedYield is not null)
                {
                    additionalProperties.Add(CreatePropertyValue(
                        "Yield TTM anualizado",
                        Math.Round(annualizedYield.Value * 100m, 2, MidpointRounding.AwayFromZero),
                        "%"));
                }

                if (marketData.QuarterlyDistribution is > 0m)
                {
                    additionalProperties.Add(CreatePropertyValue(
                        "Yield decretado",
                        Math.Round(marketData.QuarterlyDistribution.Value * 4m / lastPrice.Value * 100m, 2, MidpointRounding.AwayFromZero),
                        "%"));
                }

                if (snapshot?.Week52High is > 0m)
                {
                    additionalProperties.Add(CreatePropertyValue(
                        "Variación vs máximo 52 semanas",
                        Math.Round((lastPrice.Value - snapshot.Week52High.Value) / snapshot.Week52High.Value * 100m, 2, MidpointRounding.AwayFromZero),
                        "%"));
                }

                if (snapshot?.Week52Low is > 0m)
                {
                    additionalProperties.Add(CreatePropertyValue(
                        "Variación vs mínimo 52 semanas",
                        Math.Round((lastPrice.Value - snapshot.Week52Low.Value) / snapshot.Week52Low.Value * 100m, 2, MidpointRounding.AwayFromZero),
                        "%"));
                }
            }

            if (additionalProperties.Count > 0)
                productNode["additionalProperty"] = additionalProperties;
        }

        return JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@graph"] = new object[]
            {
                productNode,
            },
        }, JsonLdOptions);
    }

    private static Dictionary<string, object?> CreatePropertyValue(string name, decimal value, string unitText)
        => new()
        {
            ["@type"] = "PropertyValue",
            ["name"] = name,
            ["value"] = value,
            ["unitText"] = unitText,
        };

    private static string BuildFibraDescription(Fibra fibra)
    {
        var sectorClause = string.IsNullOrWhiteSpace(fibra.Sector)
            ? " Cotiza en la BMV."
            : $" Sector {fibra.Sector.Trim()} en la BMV.";

        var text = Sanitize(
            $"Análisis de {fibra.FullName} ({fibra.Ticker}): precio, yield, fundamentales (Cap Rate, NAV, LTV) y distribuciones.{sectorClause}");

        if (text.Length > FibraMaxDescriptionLength)
            return TruncateAtWordBoundary(text, FibraMaxDescriptionLength);

        // Piso 120 (convención §Middleware SEO, no negociable): nombres muy cortos sin sector
        // quedarían por debajo del mínimo; rellenar con una cola de marca como hace BuildNewsDescription.
        if (text.Length < FibraMinDescriptionLength)
        {
            var padded = Sanitize(text + FibraDescriptionPadSuffix);
            return padded.Length > FibraMaxDescriptionLength
                ? TruncateAtWordBoundary(padded, FibraMaxDescriptionLength)
                : padded;
        }

        return text;
    }

    private static string BuildNewsDescription(string rawDescription)
    {
        var text = StripMarkdown(rawDescription).Trim();

        if (text.Length > NewsMaxDescriptionLength)
            return TruncateWithEllipsis(text, NewsMaxDescriptionLength);

        if (text.Length >= NewsMinDescriptionLength)
            return text;

        var padded = text.Length > 0
            ? text + " — Análisis y noticias de FIBRAs inmobiliarias en Fibras Inmobiliarias: resultados, distribuciones y mercado inmobiliario bursátil de México."
            : "Análisis y noticias de FIBRAs inmobiliarias en Fibras Inmobiliarias: resultados, distribuciones y mercado inmobiliario bursátil de México.";

        return padded.Length > NewsMaxDescriptionLength
            ? TruncateWithEllipsis(padded, NewsMaxDescriptionLength)
            : padded;
    }

    private static string Sanitize(string text)
    {
        text = MarkdownSyntaxRegex().Replace(text, " ");
        return WhitespaceRunRegex().Replace(text, " ").Trim();
    }

    private static string StripMarkdown(string text)
    {
        text = TableSeparatorRowRegex().Replace(text, string.Empty);
        text = MarkdownLinkRegex().Replace(text, "$1");
        text = TablePipeRegex().Replace(text, " ");
        text = MarkdownSyntaxRegex().Replace(text, string.Empty);
        return WhitespaceRunRegex().Replace(text, " ").Trim();
    }

    private static string TruncateWithEllipsis(string text, int maxLength)
    {
        var cut = maxLength - 3;
        if (char.IsHighSurrogate(text[cut - 1]))
            cut--;

        return text[..cut] + "...";
    }

    private static string TruncateAtWordBoundary(string text, int maxLength)
    {
        var slice = text[..maxLength];
        if (char.IsHighSurrogate(slice[^1]))
            slice = slice[..^1];

        var lastSpace = slice.LastIndexOf(' ');
        if (lastSpace > 0)
            slice = slice[..lastSpace];

        return slice.TrimEnd(' ', ',', ';', ':', '.', '·') + "…";
    }

    private static NewsAnalysis? TryDeserializeAnalysis(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            return new NewsAnalysis(
                root.TryGetProperty("headline", out var headline) ? headline.GetString() : null,
                root.TryGetProperty("summaryMarkdown", out var summaryMarkdown) ? summaryMarkdown.GetString() : null);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed record NewsAnalysis(string? Headline, string? SummaryMarkdown);
}
