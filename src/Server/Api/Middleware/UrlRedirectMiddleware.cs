using Application.Seo;
using Domain.Seo;
using Microsoft.Extensions.Caching.Memory;

namespace Api.Middleware;

public class UrlRedirectMiddleware(
    RequestDelegate next,
    IServiceScopeFactory scopeFactory,
    IMemoryCache cache)
{
    private const string CacheKey = "seo-url-redirects-active";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Response.HasStarted ||
            (!HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method)))
        {
            await next(context);
            return;
        }

        var path = context.Request.Path.Value ?? string.Empty;
        var normalizedPath = UrlRedirectPath.Normalize(path);

        // Bypass de assets y prefijos reservados. La lista de prefijos vive en
        // UrlRedirectPath.IsReservedSource (fuente única, compartida con la validación
        // de los endpoints Ops) para evitar que ambas copias diverjan.
        if (!string.IsNullOrEmpty(Path.GetExtension(path)) ||
            UrlRedirectPath.IsReservedSource(normalizedPath))
        {
            await next(context);
            return;
        }

        var redirects = await GetActiveRedirectsAsync(context.RequestAborted);
        var redirect = redirects.FirstOrDefault(item => item.FromPath == normalizedPath);
        if (redirect is null)
        {
            await next(context);
            return;
        }

        context.Response.StatusCode = redirect.StatusCode;
        context.Response.Headers.Location = redirect.ToPath + context.Request.QueryString;
    }

    private async Task<IReadOnlyList<UrlRedirect>> GetActiveRedirectsAsync(CancellationToken ct)
    {
        if (cache.TryGetValue(CacheKey, out IReadOnlyList<UrlRedirect>? redirects) && redirects is not null)
        {
            return redirects;
        }

        using var scope = scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IRedirectRepository>();
        redirects = await repo.GetActiveAsync(ct);
        cache.Set(CacheKey, redirects, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CacheTtl,
        });

        return redirects;
    }
}
