using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text.Unicode;
using Application.Catalog;
using Domain.Catalog;

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
    IServiceScopeFactory scopeFactory)
{
    private const string PrerenderMetaComment = "<!-- prerender-meta -->";
    private const string BrandDescriptionSuffix = " — Análisis de rendimientos, distribuciones, fundamentales y precio histórico en FIBRADIS, la plataforma de referencia para FIBRAs inmobiliarias en México.";
    private const int MaxDescriptionLength = 160;
    private const int MinDescriptionLength = 120;
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

    [GeneratedRegex(@"\[([^\]]*)\]\([^)]*\)")]
    private static partial Regex MarkdownLinkRegex();

    [GeneratedRegex(@"[*_`#>]+")]
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

        var canonicalSlug = FibraSlug.Build(fibra.FullName, fibra.Ticker);

        html = TitleTagRegex().Replace(html, string.Empty, count: 1);
        html = html.Replace(PrerenderMetaComment, BuildMetaBlock(fibra, canonicalSlug, _baseUrl));

        context.Response.ContentType = "text/html; charset=utf-8";
        context.Response.Headers.CacheControl = "no-cache";
        await context.Response.WriteAsync(html, context.RequestAborted);
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

    private static string StripMarkdown(string text)
    {
        text = MarkdownLinkRegex().Replace(text, "$1");
        text = MarkdownSyntaxRegex().Replace(text, string.Empty);
        return WhitespaceRunRegex().Replace(text, " ");
    }

    private static string BuildDescription(Fibra fibra)
    {
        var text = StripMarkdown(fibra.Description?.Trim() ?? string.Empty);

        if (text.Length > MaxDescriptionLength)
            return TruncateWithEllipsis(text);

        if (text.Length >= MinDescriptionLength)
            return text;

        var padded = text.Length > 0
            ? text + BrandDescriptionSuffix
            : $"{fibra.FullName} ({fibra.Ticker}){(string.IsNullOrWhiteSpace(fibra.Sector) ? string.Empty : $" · {fibra.Sector}")}{BrandDescriptionSuffix}";

        return padded.Length > MaxDescriptionLength
            ? TruncateWithEllipsis(padded)
            : padded;
    }

    private static string TruncateWithEllipsis(string text)
    {
        var cut = MaxDescriptionLength - 3;
        if (char.IsHighSurrogate(text[cut - 1]))
            cut--;
        return text[..cut] + "...";
    }
}
