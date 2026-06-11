using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using System.Text.Unicode;
using Api.Seo;

namespace Api.Middleware;

public partial class SpaMetadataMiddleware(
    RequestDelegate next,
    ISpaMetadataProvider metadataProvider,
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

        var meta = metadataProvider.GetMetaForPath(path);
        if (meta is null)
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
        html = TitleTagRegex().Replace(html, string.Empty, count: 1);
        html = html.Replace(PrerenderMetaComment, BuildMetaBlock(meta, _baseUrl));

        context.Response.ContentType = "text/html; charset=utf-8";
        // El HTML inyectado cambia por deploy y pierde el ETag/304 de StaticFiles;
        // no-cache obliga a revalidar y evita que un CDN cachee bundles con hash viejo
        context.Response.Headers.CacheControl = "no-cache";
        await context.Response.WriteAsync(html, context.RequestAborted);
    }

    private static string BuildMetaBlock(SpaPageMeta meta, string baseUrl)
    {
        var title = Encoder.Encode(meta.Title);
        var description = Encoder.Encode(meta.Description);
        var canonicalUrl = Encoder.Encode($"{baseUrl}{meta.CanonicalPath}");

        var block = new StringBuilder()
            .Append($"<title>{title}</title>\n    ")
            .Append($"<meta name=\"description\" content=\"{description}\" />\n    ")
            .Append($"<link rel=\"canonical\" href=\"{canonicalUrl}\" />\n    ")
            .Append($"<meta property=\"og:title\" content=\"{title}\" />\n    ")
            .Append($"<meta property=\"og:description\" content=\"{description}\" />\n    ")
            .Append("<meta property=\"og:type\" content=\"website\" />\n    ")
            .Append($"<meta property=\"og:url\" content=\"{canonicalUrl}\" />");

        if (meta.JsonLd is not null)
        {
            // escapar < como \u003c (escape JSON válido): impide que un </script> en el contenido rompa el bloque
            var jsonLd = meta.JsonLd.Replace("<", "\\u003c");
            block.Append($"\n    <script type=\"application/ld+json\">{jsonLd}</script>");
        }

        return block.ToString();
    }
}
