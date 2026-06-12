using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text.Unicode;
using Application.News;
using Domain.News;
using SharedApiContracts.News;

namespace Api.Middleware;

/// <summary>
/// Inyecta metadata SEO dinámica (title, description, canonical, OG, JSON-LD NewsArticle)
/// en el HTML inicial de /noticias/{slug} para crawlers sin JavaScript. Complementa a
/// <see cref="SpaMetadataMiddleware"/>, que cubre las rutas públicas estáticas (incluido /noticias).
/// </summary>
public partial class NewsMetadataMiddleware(
    RequestDelegate next,
    IWebHostEnvironment env,
    IConfiguration config,
    IServiceScopeFactory scopeFactory)
{
    private const string PrerenderMetaComment = "<!-- prerender-meta -->";
    // El sufijo mide >120 chars por sí solo: garantiza el piso de la meta description
    // (checklist SSR/SEO 120-160) aun con snippets cortos o vacíos
    private const string BrandDescriptionSuffix = " — Análisis y noticias de FIBRAs inmobiliarias en FIBRADIS: resultados, distribuciones y mercado inmobiliario bursátil de México.";
    private const int MaxDescriptionLength = 160;
    private const int MinDescriptionLength = 120;
    private const int MaxSlugLength = 256;

    // UnicodeRanges.All deja pasar acentos/em-dash y solo escapa <, >, &, ", '
    private static readonly HtmlEncoder Encoder = HtmlEncoder.Create(UnicodeRanges.All);

    private static readonly JsonSerializerOptions AnalysisDeserializeOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    // El encoder escapa control chars y los HTML-sensibles (< > &) como \uXXXX: el JSON-LD
    // queda válido (RFC 8259) ante tabs/controles en títulos scrapeados y un </script>
    // embebido no puede romper el bloque; acentos quedan legibles
    private static readonly JsonSerializerOptions JsonLdOptions = new()
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
    };

    // og:url y canonical deben ser URLs absolutas; sin BaseUrl el middleware emitiría
    // metadata inválida en silencio — fail-fast al construir el pipeline
    private readonly string _baseUrl = !string.IsNullOrWhiteSpace(config["App:BaseUrl"])
        ? config["App:BaseUrl"]!.TrimEnd('/')
        : throw new InvalidOperationException(
            "App:BaseUrl es requerido por NewsMetadataMiddleware para construir canonical/og:url absolutos.");

    [GeneratedRegex("<title>.*?</title>", RegexOptions.Singleline)]
    private static partial Regex TitleTagRegex();

    public async Task InvokeAsync(HttpContext context)
    {
        // Solo GET/HEAD entregan documento; otros métodos siguen al pipeline
        if (!HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method))
        {
            await next(context);
            return;
        }

        var path = context.Request.Path.Value ?? "/";

        // Assets estáticos (.js, .css, .png, ...) no se interceptan
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

        if (!path.StartsWith("/noticias/", StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        // Exactamente /noticias/{identificador}; el listado /noticias lo cubre SpaMetadataMiddleware
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length != 2)
        {
            await next(context);
            return;
        }

        var identifier = segments[1];

        // La columna slug es nvarchar(256): un identificador más largo no puede existir —
        // evita que paths arbitrariamente largos de bots viajen al WHERE de SQL
        NewsArticle? article = null;
        if (identifier.Length <= MaxSlugLength)
        {
            // INewsRepository es Scoped y el middleware es Singleton — resolver vía scope propio
            using var scope = scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<INewsRepository>();
            article = Guid.TryParse(identifier, out var id)
                ? await repo.GetByIdAsync(id, context.RequestAborted)
                : await repo.GetBySlugAsync(identifier, context.RequestAborted);
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
            // Redeploy en curso puede dejar el archivo bloqueado o con ACL transitoria
            // entre File.Exists y la lectura
            await next(context);
            return;
        }

        // GetByIdAsync no filtra soft-delete (a diferencia de GetBySlugAsync)
        if (article is null || article.DeletedAt is not null)
        {
            // Soft-404: pass-through terminaría en MapFallbackToFile con 200 y las noticias
            // borradas ya indexadas nunca saldrían del índice — servir el shell SPA con 404 real
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            context.Response.ContentType = "text/html; charset=utf-8";
            context.Response.Headers.CacheControl = "no-cache";
            await context.Response.WriteAsync(html, context.RequestAborted);
            return;
        }

        // Sin el placeholder no hay dónde inyectar; pasar intacto antes de mutar nada
        if (!html.Contains(PrerenderMetaComment, StringComparison.Ordinal))
        {
            await next(context);
            return;
        }

        // Sustituir el <title> estático evita títulos duplicados en el HTML servido
        html = TitleTagRegex().Replace(html, string.Empty, count: 1);
        html = html.Replace(PrerenderMetaComment, BuildMetaBlock(article, _baseUrl));

        context.Response.ContentType = "text/html; charset=utf-8";
        // El HTML inyectado varía por artículo y deploy — forzar revalidación
        context.Response.Headers.CacheControl = "no-cache";
        await context.Response.WriteAsync(html, context.RequestAborted);
    }

    private static string BuildMetaBlock(NewsArticle article, string baseUrl)
    {
        var aiAnalysis = TryDeserializeAnalysis(article.AiAnalysisJson);

        var headline = aiAnalysis?.Headline ?? article.Title;
        var title = $"{headline} — Noticias | FIBRADIS";
        // Misma cadena de fallback que el cliente (NoticiaPage): summaryMarkdown ?? aiSummary ?? snippet
        var description = BuildDescription(aiAnalysis?.SummaryMarkdown ?? article.AiSummary ?? article.Snippet ?? string.Empty);

        // Artículos sin slug (backlog pre-backfill alcanzado por GUID): canonical cae al ID
        var canonicalPath = $"{baseUrl}/noticias/{article.Slug ?? article.Id.ToString()}";
        var canonicalUrl = Encoder.Encode(canonicalPath);
        var publishedIso = article.PublishedAt.ToString("o");

        // Serialización JSON real: escapa control chars, comillas y < (un EscapeJson artesanal
        // dejaba pasar tabs/U+0000-001F y Google descartaría el bloque NewsArticle completo)
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
                ["name"] = "FIBRADIS",
                ["url"] = baseUrl,
                ["logo"] = new Dictionary<string, object?>
                {
                    ["@type"] = "ImageObject",
                    ["url"] = $"{baseUrl}/logo.png",
                    ["width"] = 512,
                    ["height"] = 512,
                },
            },
            ["url"] = canonicalPath,
            ["description"] = description,
        }, JsonLdOptions);

        var encodedTitle = Encoder.Encode(title);
        var encodedDescription = Encoder.Encode(description);

        var block = new StringBuilder()
            .Append($"<title>{encodedTitle}</title>\n    ")
            .Append($"<meta name=\"description\" content=\"{encodedDescription}\" />\n    ")
            .Append($"<link rel=\"canonical\" href=\"{canonicalUrl}\" />\n    ")
            // og:title debe ser el mismo texto que <title> (checklist SSR/SEO) y que el cliente
            .Append($"<meta property=\"og:title\" content=\"{encodedTitle}\" />\n    ")
            .Append($"<meta property=\"og:description\" content=\"{encodedDescription}\" />\n    ")
            .Append("<meta property=\"og:type\" content=\"article\" />\n    ")
            .Append($"<meta property=\"og:url\" content=\"{canonicalUrl}\" />");

        var ogImage = article.ImageUrl ?? $"{baseUrl}/og-image.png";
        block.Append($"\n    <meta property=\"og:image\" content=\"{Encoder.Encode(ogImage)}\" />");
        block.Append("\n    <meta property=\"og:locale\" content=\"es_MX\" />");
        block.Append("\n    <meta property=\"og:site_name\" content=\"FIBRADIS\" />");
        if (article.ImageUrl is null)
        {
            block.Append("\n    <meta property=\"og:image:width\" content=\"1200\" />");
            block.Append("\n    <meta property=\"og:image:height\" content=\"630\" />");
            block.Append("\n    <meta property=\"og:image:alt\" content=\"FIBRADIS — Análisis de FIBRAs Inmobiliarias Mexicanas\" />");
        }

        block.Append("\n    <meta name=\"twitter:card\" content=\"summary_large_image\" />\n    ")
             .Append("<meta name=\"twitter:site\" content=\"@fibradis\" />\n    ")
             .Append($"<meta name=\"twitter:title\" content=\"{encodedTitle}\" />\n    ")
             .Append($"<meta name=\"twitter:description\" content=\"{encodedDescription}\" />\n    ")
             .Append($"<meta name=\"twitter:image\" content=\"{Encoder.Encode(ogImage)}\" />");

        block.Append($"\n    <script type=\"application/ld+json\">{jsonLd}</script>");

        return block.ToString();
    }

    private static string BuildDescription(string rawDescription)
    {
        var text = StripMarkdown(rawDescription).Trim();

        if (text.Length > MaxDescriptionLength)
            return TruncateWithEllipsis(text);

        if (text.Length >= MinDescriptionLength)
            return text;

        // El sufijo solo (sin guión inicial) sirve de descripción genérica cuando no hay contenido
        var padded = text.Length > 0
            ? text + BrandDescriptionSuffix
            : BrandDescriptionSuffix.TrimStart(' ', '—', ' ');
        return padded.Length > MaxDescriptionLength
            ? TruncateWithEllipsis(padded)
            : padded;
    }

    private static string TruncateWithEllipsis(string text)
    {
        var cut = MaxDescriptionLength - 3;
        // No partir un surrogate pair (emoji) en el corte: dejaría un U+FFFD al final
        if (char.IsHighSurrogate(text[cut - 1]))
            cut--;
        return text[..cut] + "...";
    }

    [GeneratedRegex(@"\[([^\]]*)\]\([^)]*\)")]
    private static partial Regex MarkdownLinkRegex();

    [GeneratedRegex(@"[*_`#>]+")]
    private static partial Regex MarkdownSyntaxRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRunRegex();

    // SummaryMarkdown llega con sintaxis Markdown que se vería literal en los SERPs
    private static string StripMarkdown(string text)
    {
        text = MarkdownLinkRegex().Replace(text, "$1");
        text = MarkdownSyntaxRegex().Replace(text, string.Empty);
        return WhitespaceRunRegex().Replace(text, " ");
    }

    private static NewsAiAnalysisDto? TryDeserializeAnalysis(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            return JsonSerializer.Deserialize<NewsAiAnalysisDto>(json, AnalysisDeserializeOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
