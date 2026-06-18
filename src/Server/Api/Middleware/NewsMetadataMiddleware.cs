using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using System.Text.Unicode;
using Application.Seo;
using Application.News;
using Domain.News;
using Domain.Seo;

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
    ISeoDefaultsBuilder seoDefaultsBuilder,
    IServiceScopeFactory scopeFactory)
{
    private const string PrerenderMetaComment = "<!-- prerender-meta -->";
    private const int MaxSlugLength = 256;

    // UnicodeRanges.All deja pasar acentos/em-dash y solo escapa <, >, &, ", '
    private static readonly HtmlEncoder Encoder = HtmlEncoder.Create(UnicodeRanges.All);

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
        using var scope = scopeFactory.CreateScope();
        NewsArticle? article = null;
        if (identifier.Length <= MaxSlugLength)
        {
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
        SeoMetadata? seoMetadata;
        string breadcrumbJsonLdBlock = string.Empty;
        var seoRepo = scope.ServiceProvider.GetRequiredService<ISeoMetadataRepository>();
        var entityKey = article.Slug ?? article.Id.ToString();
        seoMetadata = await seoRepo.GetAsync(SeoPageType.News, entityKey, context.RequestAborted);

        if (seoMetadata is null || !seoMetadata.IsActive)
        {
            seoMetadata = seoDefaultsBuilder.BuildNews(article, _baseUrl, DateTimeOffset.UtcNow, "system");
        }

        // Artículos con fila BD pero sin JSON-LD (creados antes del schema de NewsArticle):
        // regenerar desde los datos actuales del artículo si no fue overrideado por Ops.
        if (!seoMetadata.JsonLdIsOverridden && string.IsNullOrEmpty(seoMetadata.JsonLd))
            seoMetadata.JsonLd = seoDefaultsBuilder.BuildNews(article, _baseUrl, DateTimeOffset.UtcNow, "system").JsonLd;

        var faqRepo = scope.ServiceProvider.GetRequiredService<IFaqRepository>();
        var faqItems = await faqRepo.GetByPageAsync(SeoPageType.News, entityKey, includeInactive: false, context.RequestAborted);
        var faqJsonLdBlock = faqItems.Count > 0
            ? SeoJsonLd.BuildScriptBlock(seoDefaultsBuilder.BuildFaqPageJsonLd(faqItems))
            : string.Empty;

        breadcrumbJsonLdBlock = SeoJsonLd.BuildScriptBlock(
            seoDefaultsBuilder.BuildBreadcrumbListJsonLd(
                _baseUrl,
                [
                    new SeoBreadcrumbItem("Inicio", "/"),
                    new SeoBreadcrumbItem("Noticias", "/noticias"),
                    new SeoBreadcrumbItem(article.Title, seoMetadata.CanonicalPath),
                ]));

        html = html.Replace(PrerenderMetaComment, BuildMetaBlock(seoMetadata, _baseUrl, breadcrumbJsonLdBlock, faqJsonLdBlock, seoMetadata.RobotsDirectives));

        context.Response.ContentType = "text/html; charset=utf-8";
        // El HTML inyectado varía por artículo y deploy — forzar revalidación
        context.Response.Headers.CacheControl = "no-cache";
        await context.Response.WriteAsync(html, context.RequestAborted);
    }

    private static string BuildMetaBlock(SeoMetadata metadata, string baseUrl, string? breadcrumbJsonLdBlock = null, string? extraJsonLdBlock = null, string? robotsDirectives = null)
    {
        var encodedTitle = Encoder.Encode(metadata.Title);
        var encodedDescription = Encoder.Encode(metadata.MetaDescription);
        var canonicalUrl = Encoder.Encode($"{baseUrl}{metadata.CanonicalPath}");
        var ogImage = Encoder.Encode(metadata.OgImageUrl);

        var block = new StringBuilder()
            .Append($"<title>{encodedTitle}</title>\n    ")
            .Append($"<meta name=\"description\" content=\"{encodedDescription}\" />\n    ")
            .Append($"<link rel=\"canonical\" href=\"{canonicalUrl}\" />\n    ")
            .Append($"<meta property=\"og:title\" content=\"{encodedTitle}\" />\n    ")
            .Append($"<meta property=\"og:description\" content=\"{encodedDescription}\" />\n    ")
            .Append($"<meta property=\"og:type\" content=\"{Encoder.Encode(metadata.OgType)}\" />\n    ")
            .Append($"<meta property=\"og:url\" content=\"{canonicalUrl}\" />");

        block.Append($"\n    <meta property=\"og:image\" content=\"{ogImage}\" />");
        block.Append($"\n    <meta property=\"og:locale\" content=\"{Encoder.Encode(metadata.OgLocale)}\" />");
        block.Append("\n    <meta property=\"og:site_name\" content=\"Fibras Inmobiliarias\" />");
        // Solo el OG default tiene dimensiones conocidas (1200×630). Las imágenes propias de
        // artículo (article.ImageUrl) son de tamaño arbitrario: emitir width/height para ellas
        // sería mentir. Comparación exacta contra el default — no por sufijo de nombre.
        if (string.Equals(metadata.OgImageUrl, $"{baseUrl}/og-image.png", StringComparison.OrdinalIgnoreCase))
        {
            block.Append("\n    <meta property=\"og:image:width\" content=\"1200\" />");
            block.Append("\n    <meta property=\"og:image:height\" content=\"630\" />");
            block.Append("\n    <meta property=\"og:image:alt\" content=\"Fibras Inmobiliarias — Análisis de FIBRAs Inmobiliarias Mexicanas\" />");
        }

        block.Append($"\n    <meta name=\"twitter:card\" content=\"{Encoder.Encode(metadata.TwitterCard)}\" />\n    ")
             .Append("<meta name=\"twitter:site\" content=\"@fibrasinmobiliarias\" />\n    ")
             .Append($"<meta name=\"twitter:title\" content=\"{encodedTitle}\" />\n    ")
             .Append($"<meta name=\"twitter:description\" content=\"{encodedDescription}\" />\n    ")
             .Append(string.IsNullOrWhiteSpace(robotsDirectives) ? string.Empty : $"<meta name=\"robots\" content=\"{Encoder.Encode(robotsDirectives)}\" />\n    ")
             .Append($"<meta name=\"twitter:image\" content=\"{ogImage}\" />");

        var jsonLdBlock = SeoJsonLd.BuildScriptBlock(metadata.JsonLd);
        if (jsonLdBlock.Length > 0)
            block.Append($"\n    {jsonLdBlock}");

        if (!string.IsNullOrWhiteSpace(breadcrumbJsonLdBlock))
            block.Append($"\n    {breadcrumbJsonLdBlock}");

        if (!string.IsNullOrWhiteSpace(extraJsonLdBlock))
            block.Append($"\n    {extraJsonLdBlock}");

        return block.ToString();
    }

}
