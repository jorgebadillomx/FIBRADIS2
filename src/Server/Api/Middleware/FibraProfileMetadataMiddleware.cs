using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text.Unicode;
using Application.Catalog;
using Application.Seo;
using Domain.Catalog;
using Domain.Seo;

namespace Api.Middleware;

/// <summary>
/// Inyecta metadata SEO dinámica (title, description, canonical, OG, JSON-LD FinancialProduct/BreadcrumbList)
/// en el HTML inicial de /fibras/{slug} para crawlers sin JavaScript. Complementa a
/// <see cref="SpaMetadataMiddleware"/> (que cubría /fibras con metadata genérica) con datos reales
/// por perfil. Debe ejecutarse DESPUÉS de <see cref="FibraSlugRedirectMiddleware"/>, que garantiza
/// que el slug recibido ya es canónico.
/// </summary>
public partial class FibraProfileMetadataMiddleware(
    RequestDelegate next,
    IWebHostEnvironment env,
    IConfiguration config,
    ISeoDefaultsBuilder seoDefaultsBuilder,
    IServiceScopeFactory scopeFactory)
{
    private const string PrerenderMetaComment = "<!-- prerender-meta -->";
    private const int MaxDescriptionLength = 155;
    private const int MaxSlugLength = 256;

    private static readonly HtmlEncoder Encoder = HtmlEncoder.Create(UnicodeRanges.All);

    private static readonly JsonSerializerOptions JsonLdOptions = new()
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
    };

    private readonly string _baseUrl = !string.IsNullOrWhiteSpace(config["App:BaseUrl"])
        ? config["App:BaseUrl"]!.TrimEnd('/')
        : throw new InvalidOperationException(
            "App:BaseUrl es requerido por FibraProfileMetadataMiddleware para construir canonical/og:url absolutos.");

    [GeneratedRegex("<title>.*?</title>", RegexOptions.Singleline)]
    private static partial Regex TitleTagRegex();

    [GeneratedRegex(@"[#|*>_`]+")]
    private static partial Regex MarkdownSyntaxRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRunRegex();

    public async Task InvokeAsync(HttpContext context)
    {
        if (!HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method))
        {
            await next(context);
            return;
        }

        var path = context.Request.Path.Value ?? "/";

        if (!string.IsNullOrEmpty(Path.GetExtension(path)))
        {
            await next(context);
            return;
        }

        if (path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/ops/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/hangfire/", StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        if (!path.StartsWith("/fibras/", StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        // Exactamente /fibras/{slug}; /fibras (listado) lo cubre SpaMetadataMiddleware
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length != 2)
        {
            await next(context);
            return;
        }

        var slug = segments[1];

        Fibra? fibra = null;
        if (slug.Length <= MaxSlugLength)
        {
            // Ticker siempre es el último segmento del slug (después del último guión);
            // FibraSlugRedirectMiddleware ya garantiza que el slug recibido es canónico
            var lastHyphen = slug.LastIndexOf('-');
            if (lastHyphen >= 0)
            {
                var tickerCandidate = slug[(lastHyphen + 1)..];
                using var scope = scopeFactory.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<IFibraRepository>();
                fibra = await repo.GetByTickerAsync(tickerCandidate, context.RequestAborted);
            }
        }

        // Ticker desconocido → pass-through; la SPA renderizará FibraNotFound
        if (fibra is null)
        {
            await next(context);
            return;
        }

        var indexPath = env.WebRootPath is { Length: > 0 }
            ? Path.Combine(env.WebRootPath, "index.html")
            : null;
        if (indexPath is null || !File.Exists(indexPath))
        {
            await next(context);
            return;
        }

        string html;
        try
        {
            html = await File.ReadAllTextAsync(indexPath, context.RequestAborted);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            await next(context);
            return;
        }

        if (!html.Contains(PrerenderMetaComment, StringComparison.Ordinal))
        {
            await next(context);
            return;
        }

        SeoMetadata? seoMetadata;
        string faqJsonLdBlock = string.Empty;
        using var metadataScope = scopeFactory.CreateScope();
        {
            var repo = metadataScope.ServiceProvider.GetRequiredService<ISeoMetadataRepository>();
            // El EntityKey de Fibra se almacena en MAYÚSCULAS (ver SeoDefaultsBuilder.BuildFibra);
            // normalizar aquí el ticker para que el lookup coincida bajo collation case-sensitive.
            var entityKey = fibra.Ticker.ToUpperInvariant();
            seoMetadata = await repo.GetAsync(SeoPageType.Fibra, entityKey, context.RequestAborted);

            var faqRepo = metadataScope.ServiceProvider.GetRequiredService<IFaqRepository>();
            var faqItems = await faqRepo.GetByPageAsync(SeoPageType.Fibra, entityKey, includeInactive: false, context.RequestAborted);
            if (faqItems.Count > 0)
                faqJsonLdBlock = SeoJsonLd.BuildScriptBlock(seoDefaultsBuilder.BuildFaqPageJsonLd(faqItems));
        }

        if (seoMetadata is null || !seoMetadata.IsActive)
        {
            seoMetadata = seoDefaultsBuilder.BuildFibra(fibra, _baseUrl, DateTimeOffset.UtcNow, "system");
        }

        html = TitleTagRegex().Replace(html, string.Empty, count: 1);
        html = html.Replace(PrerenderMetaComment, BuildMetaBlock(seoMetadata, _baseUrl, faqJsonLdBlock));

        context.Response.ContentType = "text/html; charset=utf-8";
        context.Response.Headers.CacheControl = "no-cache";
        await context.Response.WriteAsync(html, context.RequestAborted);
    }

    private static string BuildMetaBlock(SeoMetadata metadata, string baseUrl, string? extraJsonLdBlock = null)
    {
        var encodedTitle = Encoder.Encode(metadata.Title);
        var encodedDescription = Encoder.Encode(metadata.MetaDescription);
        var canonicalUrl = Encoder.Encode($"{baseUrl}{metadata.CanonicalPath}");
        var ogImage = Encoder.Encode(metadata.OgImageUrl);

        return new StringBuilder()
            .Append($"<title>{encodedTitle}</title>\n    ")
            .Append($"<meta name=\"description\" content=\"{encodedDescription}\" />\n    ")
            .Append($"<link rel=\"canonical\" href=\"{canonicalUrl}\" />\n    ")
            .Append($"<meta property=\"og:title\" content=\"{encodedTitle}\" />\n    ")
            .Append($"<meta property=\"og:description\" content=\"{encodedDescription}\" />\n    ")
            .Append($"<meta property=\"og:type\" content=\"{Encoder.Encode(metadata.OgType)}\" />\n    ")
            .Append($"<meta property=\"og:url\" content=\"{canonicalUrl}\" />\n    ")
            .Append($"<meta property=\"og:image\" content=\"{ogImage}\" />\n    ")
            .Append($"<meta property=\"og:locale\" content=\"{Encoder.Encode(metadata.OgLocale)}\" />\n    ")
            .Append("<meta property=\"og:site_name\" content=\"FIBRADIS\" />\n    ")
            .Append("<meta property=\"og:image:width\" content=\"1200\" />\n    ")
            .Append("<meta property=\"og:image:height\" content=\"630\" />\n    ")
            .Append("<meta property=\"og:image:alt\" content=\"FIBRADIS — Análisis de FIBRAs Inmobiliarias Mexicanas\" />\n    ")
            .Append($"<meta name=\"twitter:card\" content=\"{Encoder.Encode(metadata.TwitterCard)}\" />\n    ")
            .Append("<meta name=\"twitter:site\" content=\"@fibradis\" />\n    ")
            .Append($"<meta name=\"twitter:title\" content=\"{encodedTitle}\" />\n    ")
            .Append($"<meta name=\"twitter:description\" content=\"{encodedDescription}\" />\n    ")
            .Append($"<meta name=\"twitter:image\" content=\"{ogImage}\" />\n    ")
            .Append(SeoJsonLd.BuildScriptBlock(metadata.JsonLd))
            .Append(string.IsNullOrWhiteSpace(extraJsonLdBlock) ? string.Empty : $"\n    {extraJsonLdBlock}")
            .ToString();
    }

    private static string BuildMetaBlock(Fibra fibra, string canonicalSlug, string baseUrl)
    {
        var title = $"{fibra.FullName} ({fibra.Ticker}) | FIBRADIS — Fibras Inmobiliarias";
        var description = BuildDescription(fibra);
        var canonicalPath = $"{baseUrl}/fibras/{canonicalSlug}";
        var canonicalUrl = Encoder.Encode(canonicalPath);

        var jsonLd = JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@graph"] = new object[]
            {
                new Dictionary<string, object?>
                {
                    ["@type"] = "FinancialProduct",
                    ["@id"] = $"{canonicalPath}#product",
                    ["name"] = fibra.FullName,
                    ["alternateName"] = fibra.Ticker,
                    ["description"] = description,
                    ["url"] = canonicalPath,
                    ["provider"] = new Dictionary<string, object?>
                    {
                        ["@type"] = "Organization",
                        ["name"] = "FIBRADIS",
                        ["url"] = baseUrl,
                    },
                    ["category"] = fibra.Sector,
                    ["additionalType"] = "https://en.wikipedia.org/wiki/Real_estate_investment_trust",
                },
                new Dictionary<string, object?>
                {
                    ["@type"] = "BreadcrumbList",
                    ["itemListElement"] = new object[]
                    {
                        new Dictionary<string, object?> { ["@type"] = "ListItem", ["position"] = 1, ["name"] = "Inicio", ["item"] = $"{baseUrl}/" },
                        new Dictionary<string, object?> { ["@type"] = "ListItem", ["position"] = 2, ["name"] = "Fibras Inmobiliarias", ["item"] = $"{baseUrl}/fibras" },
                        new Dictionary<string, object?> { ["@type"] = "ListItem", ["position"] = 3, ["name"] = fibra.FullName, ["item"] = canonicalPath },
                    },
                },
            },
        }, JsonLdOptions);

        var encodedTitle = Encoder.Encode(title);
        var encodedDescription = Encoder.Encode(description);

        var ogImage = Encoder.Encode($"{baseUrl}/og-image.png");

        return new StringBuilder()
            .Append($"<title>{encodedTitle}</title>\n    ")
            .Append($"<meta name=\"description\" content=\"{encodedDescription}\" />\n    ")
            .Append($"<link rel=\"canonical\" href=\"{canonicalUrl}\" />\n    ")
            .Append($"<meta property=\"og:title\" content=\"{encodedTitle}\" />\n    ")
            .Append($"<meta property=\"og:description\" content=\"{encodedDescription}\" />\n    ")
            .Append("<meta property=\"og:type\" content=\"website\" />\n    ")
            .Append($"<meta property=\"og:url\" content=\"{canonicalUrl}\" />\n    ")
            .Append($"<meta property=\"og:image\" content=\"{ogImage}\" />\n    ")
            .Append("<meta property=\"og:locale\" content=\"es_MX\" />\n    ")
            .Append("<meta property=\"og:site_name\" content=\"FIBRADIS\" />\n    ")
            .Append("<meta property=\"og:image:width\" content=\"1200\" />\n    ")
            .Append("<meta property=\"og:image:height\" content=\"630\" />\n    ")
            .Append("<meta property=\"og:image:alt\" content=\"FIBRADIS — Análisis de FIBRAs Inmobiliarias Mexicanas\" />\n    ")
            .Append("<meta name=\"twitter:card\" content=\"summary_large_image\" />\n    ")
            .Append("<meta name=\"twitter:site\" content=\"@fibradis\" />\n    ")
            .Append($"<meta name=\"twitter:title\" content=\"{encodedTitle}\" />\n    ")
            .Append($"<meta name=\"twitter:description\" content=\"{encodedDescription}\" />\n    ")
            .Append($"<meta name=\"twitter:image\" content=\"{ogImage}\" />\n    ")
            .Append($"<script type=\"application/ld+json\">{jsonLd}</script>")
            .ToString();
    }

    // Plantilla limpia derivada de campos estructurados (FullName/Ticker/Sector), nunca del
    // markdown de Description: ese campo llega con heading, tablas "| Campo | Detalle |" y emoji
    // y se volcaba crudo en las 3 superficies SEO (meta description, twitter:description y el
    // "description" del JSON-LD FinancialProduct). La plantilla garantiza una frase legible,
    // sin sintaxis markdown ni pérdida de encoding ("??") al evitar el origen corrupto.
    private static string BuildDescription(Fibra fibra)
    {
        var sectorClause = string.IsNullOrWhiteSpace(fibra.Sector)
            ? " Cotiza en la BMV."
            : $" Sector {fibra.Sector.Trim()} en la BMV.";

        var text = Sanitize(
            $"Análisis de {fibra.FullName} ({fibra.Ticker}): precio, yield, fundamentales (Cap Rate, NAV, LTV) y distribuciones.{sectorClause}");

        return text.Length > MaxDescriptionLength ? TruncateAtWordBoundary(text) : text;
    }

    // FullName/Sector son texto libre de BD: eliminar sintaxis markdown residual (#, |, *, >, _, `)
    // y colapsar espacios para que ninguna de estas se filtre a la metadata servida.
    private static string Sanitize(string text)
    {
        text = MarkdownSyntaxRegex().Replace(text, " ");
        return WhitespaceRunRegex().Replace(text, " ").Trim();
    }

    private static string TruncateAtWordBoundary(string text)
    {
        var slice = text[..MaxDescriptionLength];
        // No partir un surrogate pair en el límite duro (dejaría un U+FFFD)
        if (char.IsHighSurrogate(slice[^1]))
            slice = slice[..^1];

        var lastSpace = slice.LastIndexOf(' ');
        if (lastSpace > 0)
            slice = slice[..lastSpace];

        return slice.TrimEnd(' ', ',', ';', ':', '.', '·') + "…";
    }
}
