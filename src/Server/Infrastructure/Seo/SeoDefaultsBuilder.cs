using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text.Unicode;
using Application.Catalog;
using Application.Seo;
using Domain.Catalog;
using Domain.News;
using Domain.Seo;

namespace Infrastructure.Seo;

public partial class SeoDefaultsBuilder : ISeoDefaultsBuilder
{
    private const string BrandName = "FIBRADIS";
    private const string BrandTitleSuffix = " | FIBRADIS";
    private const string FibraTitleSuffix = " | FIBRADIS — Fibras Inmobiliarias";
    private const string OgImagePath = "/og-image.png";
    private const string OgLocale = "es_MX";
    private const string TwitterCard = "summary_large_image";
    private const string DefaultRobotsDirectives = "index,follow,max-image-preview:large,max-snippet:-1,max-video-preview:-1";
    private const int NewsMaxDescriptionLength = 160;
    private const int NewsMinDescriptionLength = 120;
    private const int FibraMaxDescriptionLength = 155;
    private const int FibraMinDescriptionLength = 120;
    private const string FibraDescriptionPadSuffix = " Consulta su análisis completo en FIBRADIS.";

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
        string updatedBy = "system")
    {
        var canonicalSlug = FibraSlug.Build(fibra.FullName, fibra.Ticker);
        var canonicalPath = $"/fibras/{canonicalSlug}";
        var title = $"{fibra.FullName} ({fibra.Ticker}){FibraTitleSuffix}";
        var description = BuildFibraDescription(fibra);

        var jsonLd = JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@graph"] = new object[]
            {
                new Dictionary<string, object?>
                {
                    ["@type"] = "FinancialProduct",
                    ["@id"] = $"{TrimBaseUrl(baseUrl)}{canonicalPath}#product",
                    ["name"] = fibra.FullName,
                    ["alternateName"] = fibra.Ticker,
                    ["description"] = description,
                    ["url"] = $"{TrimBaseUrl(baseUrl)}{canonicalPath}",
                    ["provider"] = new Dictionary<string, object?>
                    {
                        ["@type"] = "Organization",
                        ["name"] = BrandName,
                        ["url"] = TrimBaseUrl(baseUrl),
                    },
                    ["category"] = fibra.Sector,
                    ["additionalType"] = "https://en.wikipedia.org/wiki/Real_estate_investment_trust",
                },
                new Dictionary<string, object?>
                {
                    ["@type"] = "BreadcrumbList",
                    ["itemListElement"] = new object[]
                    {
                        new Dictionary<string, object?> { ["@type"] = "ListItem", ["position"] = 1, ["name"] = "Inicio", ["item"] = $"{TrimBaseUrl(baseUrl)}/" },
                        new Dictionary<string, object?> { ["@type"] = "ListItem", ["position"] = 2, ["name"] = "Fibras Inmobiliarias", ["item"] = $"{TrimBaseUrl(baseUrl)}/fibras" },
                        new Dictionary<string, object?> { ["@type"] = "ListItem", ["position"] = 3, ["name"] = fibra.FullName, ["item"] = $"{TrimBaseUrl(baseUrl)}{canonicalPath}" },
                    },
                },
            },
        }, JsonLdOptions);

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
            ogType: "website");
    }

    public SeoMetadata BuildNews(
        NewsArticle article,
        string baseUrl,
        DateTimeOffset updatedAt,
        string updatedBy = "system")
    {
        var analysis = TryDeserializeAnalysis(article.AiAnalysisJson);
        var headline = analysis?.Headline ?? article.Title;
        var title = $"{headline} — Noticias | {BrandName}";
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

    private static string TrimBaseUrl(string baseUrl) => baseUrl.TrimEnd('/');

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
            ? text + " — Análisis y noticias de FIBRAs inmobiliarias en FIBRADIS: resultados, distribuciones y mercado inmobiliario bursátil de México."
            : "Análisis y noticias de FIBRAs inmobiliarias en FIBRADIS: resultados, distribuciones y mercado inmobiliario bursátil de México.";

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
