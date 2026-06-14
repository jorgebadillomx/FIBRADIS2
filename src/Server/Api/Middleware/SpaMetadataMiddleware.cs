using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using System.Text.Unicode;
using Application.Seo;
using Api.Seo;
using Domain.Seo;

namespace Api.Middleware;

public partial class SpaMetadataMiddleware(
    RequestDelegate next,
    ISpaMetadataProvider metadataProvider,
    ISeoDefaultsBuilder seoDefaultsBuilder,
    IServiceScopeFactory scopeFactory,
    IWebHostEnvironment env,
    IConfiguration config)
{
    private const string PrerenderMetaComment = "<!-- prerender-meta -->";

    // UnicodeRanges.All deja pasar acentos/em-dash y solo escapa <, >, &, ", '
    private static readonly HtmlEncoder Encoder = HtmlEncoder.Create(UnicodeRanges.All);

    // og:url y canonical deben ser URLs absolutas; sin BaseUrl el middleware emitiría
    // metadata inválida en silencio — fail-fast al construir el pipeline
    private readonly string _baseUrl = !string.IsNullOrWhiteSpace(config["App:BaseUrl"])
        ? config["App:BaseUrl"]!.TrimEnd('/')
        : throw new InvalidOperationException(
            "App:BaseUrl es requerido por SpaMetadataMiddleware para construir canonical/og:url absolutos.");

    [GeneratedRegex("<title>.*?</title>", RegexOptions.Singleline)]
    private static partial Regex TitleTagRegex();

    public async Task InvokeAsync(HttpContext context)
    {
        // Solo GET/HEAD entregan documento; otros métodos siguen al pipeline (405/404 según ruta)
        if (!HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method))
        {
            await next(context);
            return;
        }

        var path = context.Request.Path.Value ?? "/";

        // Assets estáticos (.js, .css, .png, .svg, .ico, .woff2, ...) no se interceptan
        var ext = Path.GetExtension(path);
        if (!string.IsNullOrEmpty(ext))
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

        var normalizedPath = NormalizePath(path);
        var pageType = normalizedPath == "/" ? SeoPageType.Home : SeoPageType.StaticPage;

        SeoMetadata? seoMetadata;
        string faqJsonLdBlock = string.Empty;
        using var scope = scopeFactory.CreateScope();
        {
            var repo = scope.ServiceProvider.GetRequiredService<ISeoMetadataRepository>();
            seoMetadata = await repo.GetAsync(pageType, normalizedPath, context.RequestAborted);
        }

        if (seoMetadata is null || !seoMetadata.IsActive)
        {
            var meta = await metadataProvider.GetMetaForPathAsync(path, context.RequestAborted);
            if (meta is null)
            {
                await next(context);
                return;
            }

            seoMetadata = seoDefaultsBuilder.BuildStaticPage(
                pageType,
                meta.CanonicalPath,
                meta.Title,
                meta.Description,
                meta.CanonicalPath,
                meta.JsonLd,
                _baseUrl,
                DateTimeOffset.UtcNow,
                "system");
        }

        if (seoMetadata is null)
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
        catch (IOException)
        {
            // Redeploy en curso puede dejar el archivo bloqueado entre File.Exists y la lectura
            await next(context);
            return;
        }

        // Sin el placeholder no hay dónde inyectar; pasar intacto antes de mutar nada
        // (un build que elimine el comentario dejaría las rutas sin <title> alguno)
        if (!html.Contains(PrerenderMetaComment, StringComparison.Ordinal))
        {
            await next(context);
            return;
        }

        // Sustituir el <title> estático evita títulos duplicados en el HTML servido
        // (Google tomaría el primero, el genérico) — extensión de CA-3 aprobada por el usuario
        var faqRepo = scope.ServiceProvider.GetRequiredService<IFaqRepository>();
        var faqItems = await faqRepo.GetByPageAsync(pageType, normalizedPath, includeInactive: false, context.RequestAborted);
        if (faqItems.Count > 0)
            faqJsonLdBlock = SeoJsonLd.BuildScriptBlock(seoDefaultsBuilder.BuildFaqPageJsonLd(faqItems));

        html = TitleTagRegex().Replace(html, string.Empty, count: 1);
        html = html.Replace(PrerenderMetaComment, BuildMetaBlock(seoMetadata, _baseUrl, faqJsonLdBlock));

        context.Response.ContentType = "text/html; charset=utf-8";
        // El HTML inyectado cambia por deploy y pierde el ETag/304 de StaticFiles;
        // no-cache obliga a revalidar y evita que un CDN cachee bundles con hash viejo
        context.Response.Headers.CacheControl = "no-cache";
        await context.Response.WriteAsync(html, context.RequestAborted);
    }

    private static string BuildMetaBlock(SeoMetadata metadata, string baseUrl, string? extraJsonLdBlock = null)
    {
        var title = Encoder.Encode(metadata.Title);
        var description = Encoder.Encode(metadata.MetaDescription);
        var canonicalUrl = Encoder.Encode($"{baseUrl}{metadata.CanonicalPath}");
        var ogImage = Encoder.Encode(metadata.OgImageUrl);

        var block = new StringBuilder()
            .Append($"<title>{title}</title>\n    ")
            .Append($"<meta name=\"description\" content=\"{description}\" />\n    ")
            .Append($"<link rel=\"canonical\" href=\"{canonicalUrl}\" />\n    ")
            .Append($"<meta property=\"og:title\" content=\"{title}\" />\n    ")
            .Append($"<meta property=\"og:description\" content=\"{description}\" />\n    ")
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
            .Append($"<meta name=\"twitter:title\" content=\"{title}\" />\n    ")
            .Append($"<meta name=\"twitter:description\" content=\"{description}\" />\n    ")
            .Append($"<meta name=\"twitter:image\" content=\"{ogImage}\" />");

        var jsonLdBlock = SeoJsonLd.BuildScriptBlock(metadata.JsonLd);
        if (jsonLdBlock.Length > 0)
            block.Append($"\n    {jsonLdBlock}");

        if (!string.IsNullOrWhiteSpace(extraJsonLdBlock))
            block.Append($"\n    {extraJsonLdBlock}");

        return block.ToString();
    }

    private static string NormalizePath(string path)
    {
        var normalized = path.TrimEnd('/').ToLowerInvariant();
        return normalized.Length == 0 ? "/" : normalized;
    }
}
