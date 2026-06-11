using System.Text.RegularExpressions;
using Application.Catalog;

namespace Api.Middleware;

/// <summary>
/// Redirige 301 las URLs de ficha no canónicas (/fibras/FUNO11, /fibras/slug-viejo-funo11)
/// a la URL slug canónica /fibras/{slugify(FullName)}-{ticker}. El ticker se extrae del
/// último segmento del slug (después del último guión) — los tickers son alfanuméricos
/// sin guiones, así que la extracción es no-ambigua y las URLs viejas por ticker pelado
/// resuelven gratis. Debe ejecutarse ANTES de SpaMetadataMiddleware: si el HTML se
/// sirviera primero, Google indexaría la URL vieja con contenido 200.
/// </summary>
public partial class FibraSlugRedirectMiddleware(
    RequestDelegate next,
    IServiceScopeFactory scopeFactory)
{
    [GeneratedRegex("^/fibras/([^/]+)$")]
    private static partial Regex FibraPathRegex();

    public async Task InvokeAsync(HttpContext context)
    {
        // Guard aprendido del review de 11.1: nunca mutar un response ya iniciado.
        // GET y HEAD (curl -I, validadores SEO) reciben el mismo 301 — igual que SpaMetadataMiddleware
        if (context.Response.HasStarted ||
            (!HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method)))
        {
            await next(context);
            return;
        }

        var path = context.Request.Path.Value ?? string.Empty;

        // Assets con extensión y prefijos privados no se interceptan
        if (!string.IsNullOrEmpty(Path.GetExtension(path)) ||
            path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/ops/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/hangfire/", StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        var match = FibraPathRegex().Match(path);
        if (!match.Success)
        {
            await next(context);
            return;
        }

        var slug = match.Groups[1].Value;
        var lastHyphen = slug.LastIndexOf('-');
        var tickerCandidate = (lastHyphen >= 0 ? slug[(lastHyphen + 1)..] : slug).ToUpperInvariant();
        if (tickerCandidate.Length == 0)
        {
            await next(context);
            return;
        }

        // IFibraRepository es Scoped y este middleware Singleton — mismo patrón que 11.4
        using var scope = scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IFibraRepository>();
        var fibra = await repo.GetByTickerAsync(tickerCandidate, context.RequestAborted);

        // Ticker desconocido → la SPA renderiza FibraNotFound
        if (fibra is null)
        {
            await next(context);
            return;
        }

        var canonical = $"/fibras/{FibraSlug.Build(fibra.FullName, fibra.Ticker)}";
        if (string.Equals(path, canonical, StringComparison.Ordinal))
        {
            await next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status301MovedPermanently;
        context.Response.Headers.Location = canonical + context.Request.QueryString;
    }
}
