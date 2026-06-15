using Application.Catalog;
using Application.Fundamentals;
using Application.Market;
using Application.Seo;
using Domain.Catalog;
using Domain.Fundamentals;
using Domain.Market;
using Microsoft.Extensions.Caching.Memory;

namespace Api.Endpoints.Public;

public static class OgImageEndpoints
{
    private const string CacheControlHeader = "public, max-age=21600, s-maxage=21600";
    private const string FallbackCacheKey = "og:fibras:fallback";
    private const int MaxTickerLength = 20;

    public static IEndpointRouteBuilder MapOgImages(this IEndpointRouteBuilder app)
    {
        app.MapMethods("/og/fibras/{ticker}.png", [HttpMethods.Get, HttpMethods.Head], async (
            string ticker,
            IFibraRepository fibraRepo,
            IMarketRepository marketRepo,
            IFundamentalRepository fundamentalRepo,
            IOgImageRenderer renderer,
            IMemoryCache cache,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            if (!TryNormalizeTicker(ticker, out var normalizedTicker))
            {
                var fallback = await GetFallbackAsync(renderer, cache, httpContext, ct);
                return Results.File(fallback, "image/png");
            }

            var cacheKey = $"og:fibras:{normalizedTicker}";
            if (cache.TryGetValue(cacheKey, out byte[]? cached) && cached is { Length: > 0 })
            {
                httpContext.Response.Headers.CacheControl = CacheControlHeader;
                return Results.File(cached, "image/png");
            }

            var fibra = await fibraRepo.GetByTickerAsync(normalizedTicker, ct);
            if (fibra is null)
            {
                // No materializar una copia del fallback por cada ticker desconocido: eso permitiría
                // inundar IMemoryCache con claves distintas. El fallback ya se cachea una sola vez
                // bajo FallbackCacheKey dentro de GetFallbackAsync.
                var fallback = await GetFallbackAsync(renderer, cache, httpContext, ct);
                return Results.File(fallback, "image/png");
            }

            var marketData = await LoadLiveMarketDataAsync(fibra, marketRepo, fundamentalRepo, ct);
            var pngBytes = await renderer.RenderFibraCardAsync(fibra, marketData, ct);

            // Solo cachear un render válido: nunca persistir un PNG vacío (fallo transitorio) por 6h.
            if (pngBytes is { Length: > 0 })
                cache.Set(cacheKey, pngBytes, TimeSpan.FromHours(6));

            httpContext.Response.Headers.CacheControl = CacheControlHeader;
            return Results.File(pngBytes, "image/png");
        })
        .AllowAnonymous()
        .ExcludeFromDescription();

        return app;
    }

    private static async Task<FibraSeoMarketData> LoadLiveMarketDataAsync(
        Fibra fibra,
        IMarketRepository marketRepo,
        IFundamentalRepository fundamentalRepo,
        CancellationToken ct)
    {
        // Consultas secuenciales: el scope es único y EF Core no es thread-safe.
        var snapshot = await marketRepo.GetLatestProcessedSnapshotAsync(fibra.Id, ct);
        var distributions = await marketRepo.GetDistributionsAsync(fibra.Id, maxDays: 365, ct);
        var fundamental = await fundamentalRepo.GetLatestProcessedByFibraAsync(fibra.Id, ct);
        var asOfDate = snapshot is not null
            ? DateOnly.FromDateTime(snapshot.CapturedAt.UtcDateTime)
            : DateOnly.FromDateTime(DateTime.UtcNow);

        return new FibraSeoMarketData(snapshot, distributions, fundamental?.QuarterlyDistribution, asOfDate);
    }

    private static async Task<byte[]> GetFallbackAsync(
        IOgImageRenderer renderer,
        IMemoryCache cache,
        HttpContext context,
        CancellationToken ct)
    {
        if (!cache.TryGetValue(FallbackCacheKey, out byte[]? fallbackBytes))
        {
            fallbackBytes = await renderer.RenderFibraCardAsync(null, null, ct);
            cache.Set(FallbackCacheKey, fallbackBytes, TimeSpan.FromHours(6));
        }

        context.Response.Headers.CacheControl = CacheControlHeader;
        return fallbackBytes!;
    }

    private static bool TryNormalizeTicker(string ticker, out string normalizedTicker)
    {
        normalizedTicker = string.Empty;

        if (string.IsNullOrWhiteSpace(ticker))
            return false;

        var trimmed = ticker.Trim();
        if (trimmed.Length is < 1 or > MaxTickerLength)
            return false;

        if (!trimmed.All(char.IsAsciiLetterOrDigit))
            return false;

        normalizedTicker = trimmed.ToUpperInvariant();
        return true;
    }
}
