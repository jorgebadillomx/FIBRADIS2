using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using System.Text.Unicode;
using Application.Catalog;
using Application.Fundamentals;
using Application.Seo;
using Api.Seo;
using Domain.Seo;
using Microsoft.Extensions.Primitives;

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
        var noticiasPage = normalizedPath == "/noticias" ? GetNoticiasPageNumber(context.Request.Query["page"]) : 1;
        var robotsDirectives = normalizedPath == "/noticias" && noticiasPage > 1 ? "noindex,follow" : null;

        SeoMetadata? seoMetadata;
        string breadcrumbJsonLdBlock = string.Empty;
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

        if (normalizedPath == "/noticias" && noticiasPage > 1)
            seoMetadata.CanonicalPath = $"/noticias?page={noticiasPage}";

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

        // Recién aquí (confirmado el placeholder) se componen breadcrumb y JSON-LD dinámico:
        // hacerlo antes del guard gastaría lecturas a BD en peticiones que terminan en next()
        breadcrumbJsonLdBlock = BuildBreadcrumbJsonLdBlock(normalizedPath);
        if (!seoMetadata.JsonLdIsOverridden && (normalizedPath == "/comparar" || normalizedPath == "/fundamentales"))
        {
            seoMetadata.JsonLd = await BuildDynamicJsonLdAsync(scope.ServiceProvider, normalizedPath, context.RequestAborted);
        }

        // Sustituir el <title> estático evita títulos duplicados en el HTML servido
        // (Google tomaría el primero, el genérico) — extensión de CA-3 aprobada por el usuario
        var faqRepo = scope.ServiceProvider.GetRequiredService<IFaqRepository>();
        var faqItems = await faqRepo.GetByPageAsync(pageType, normalizedPath, includeInactive: false, context.RequestAborted);
        if (faqItems.Count > 0)
            faqJsonLdBlock = SeoJsonLd.BuildScriptBlock(seoDefaultsBuilder.BuildFaqPageJsonLd(faqItems));

        html = TitleTagRegex().Replace(html, string.Empty, count: 1);
        html = html.Replace(PrerenderMetaComment, BuildMetaBlock(seoMetadata, _baseUrl, breadcrumbJsonLdBlock, faqJsonLdBlock, robotsDirectives));

        context.Response.ContentType = "text/html; charset=utf-8";
        // El HTML inyectado cambia por deploy y pierde el ETag/304 de StaticFiles;
        // no-cache obliga a revalidar y evita que un CDN cachee bundles con hash viejo
        context.Response.Headers.CacheControl = "no-cache";
        await context.Response.WriteAsync(html, context.RequestAborted);
    }

    private static string BuildMetaBlock(SeoMetadata metadata, string baseUrl, string? breadcrumbJsonLdBlock = null, string? extraJsonLdBlock = null, string? robotsDirectives = null)
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

    private string BuildBreadcrumbJsonLdBlock(string normalizedPath)
    {
        IReadOnlyList<SeoBreadcrumbItem> items = normalizedPath switch
        {
            // Home no lleva BreadcrumbList: un breadcrumb de un solo ítem no aporta jerarquía y
            // Google Rich Results lo ignora; la home ya emite WebSite (decisión code review 12-5)
            "/calculadora" => new[] { new SeoBreadcrumbItem("Inicio", "/"), new SeoBreadcrumbItem("Calculadora", "/calculadora") },
            "/comparar" => new[] { new SeoBreadcrumbItem("Inicio", "/"), new SeoBreadcrumbItem("Comparar", "/comparar") },
            "/fibras" => new[] { new SeoBreadcrumbItem("Inicio", "/"), new SeoBreadcrumbItem("Fibras Inmobiliarias", "/fibras") },
            "/noticias" => new[] { new SeoBreadcrumbItem("Inicio", "/"), new SeoBreadcrumbItem("Noticias", "/noticias") },
            "/conoce-las-fibras" => new[] { new SeoBreadcrumbItem("Inicio", "/"), new SeoBreadcrumbItem("Conoce las FIBRAs", "/conoce-las-fibras") },
            "/calendario" => new[] { new SeoBreadcrumbItem("Inicio", "/"), new SeoBreadcrumbItem("Calendario", "/calendario") },
            "/fundamentales" => new[] { new SeoBreadcrumbItem("Inicio", "/"), new SeoBreadcrumbItem("Fundamentales", "/fundamentales") },
            "/privacidad" => new[] { new SeoBreadcrumbItem("Inicio", "/"), new SeoBreadcrumbItem("Privacidad", "/privacidad") },
            "/acerca" => new[] { new SeoBreadcrumbItem("Inicio", "/"), new SeoBreadcrumbItem("Acerca", "/acerca") },
            "/contacto" => new[] { new SeoBreadcrumbItem("Inicio", "/"), new SeoBreadcrumbItem("Contacto", "/contacto") },
            _ => Array.Empty<SeoBreadcrumbItem>(),
        };

        return SeoJsonLd.BuildScriptBlock(seoDefaultsBuilder.BuildBreadcrumbListJsonLd(_baseUrl, items));
    }

    private static async Task<string> BuildDynamicJsonLdAsync(IServiceProvider services, string normalizedPath, CancellationToken ct)
    {
        return normalizedPath switch
        {
            "/comparar" => await BuildCompareJsonLdAsync(services, ct),
            "/fundamentales" => await BuildFundamentalesJsonLdAsync(services, ct),
            _ => string.Empty,
        };
    }

    private static async Task<string> BuildCompareJsonLdAsync(IServiceProvider services, CancellationToken ct)
    {
        var fibraRepo = services.GetRequiredService<IFibraRepository>();
        var fibras = await fibraRepo.GetAllActiveForSitemapAsync(ct);
        var builder = services.GetRequiredService<ISeoDefaultsBuilder>();
        var config = services.GetRequiredService<IConfiguration>();
        var baseUrl = !string.IsNullOrWhiteSpace(config["App:BaseUrl"])
            ? config["App:BaseUrl"]!.TrimEnd('/')
            : throw new InvalidOperationException("App:BaseUrl es requerido para JSON-LD dinámico de /comparar.");

        return builder.BuildComparePageJsonLd(fibras, baseUrl);
    }

    private static async Task<string> BuildFundamentalesJsonLdAsync(IServiceProvider services, CancellationToken ct)
    {
        var fundamentalRepo = services.GetRequiredService<IFundamentalRepository>();
        var rows = await fundamentalRepo.GetSummaryLatestAsync(ct);
        var builder = services.GetRequiredService<ISeoDefaultsBuilder>();
        var config = services.GetRequiredService<IConfiguration>();
        var baseUrl = !string.IsNullOrWhiteSpace(config["App:BaseUrl"])
            ? config["App:BaseUrl"]!.TrimEnd('/')
            : throw new InvalidOperationException("App:BaseUrl es requerido para JSON-LD dinámico de /fundamentales.");

        return builder.BuildFundamentalesPageJsonLd(rows, baseUrl);
    }

    private static string NormalizePath(string path)
    {
        var normalized = path.TrimEnd('/').ToLowerInvariant();
        return normalized.Length == 0 ? "/" : normalized;
    }

    private static int GetNoticiasPageNumber(StringValues pageValue)
    {
        if (pageValue.Count == 0)
            return 1;

        return int.TryParse(pageValue[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var page) && page > 0
            ? page
            : 1;
    }
}
