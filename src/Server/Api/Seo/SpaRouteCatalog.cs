namespace Api.Seo;

/// <summary>
/// Catálogo de rutas conocidas del SPA Main (espejo de <c>src/Web/Main/src/app/routes.tsx</c>).
/// Lo usa el fallback de <c>Program.cs</c> para devolver 404 real en rutas inexistentes y evitar
/// soft-404 (HTTP 200 con shell SPA) que generan index bloat en Search Console.
/// </summary>
/// <remarks>
/// Las rutas dinámicas <c>/fibras/{slug}</c> y <c>/noticias/{slug}</c> NO se validan aquí: su
/// soft-404 por slug lo resuelven <c>FibraProfileMetadataMiddleware</c> y <c>NewsMetadataMiddleware</c>
/// antes del fallback. Este catálogo solo decide si una ruta arbitraria corresponde al SPA.
/// </remarks>
public static class SpaRouteCatalog
{
    // Rutas exactas servidas por el SPA (públicas + privadas tras ProtectedRoute).
    private static readonly HashSet<string> KnownRoutes = new(StringComparer.OrdinalIgnoreCase)
    {
        "/",
        "/fibras",
        "/comparar",
        "/calculadora",
        "/noticias",
        "/calendario",
        "/conoce-las-fibras",
        "/fundamentales",
        "/login",
        "/privacidad",
        "/acerca",
        "/contacto",
        "/portafolio",
        "/oportunidades",
        "/herramientas",
        "/reportes",
        "/perfil",
    };

    // Prefijos de rutas dinámicas con detalle por slug; la validez del slug la decide su middleware.
    private static readonly string[] DynamicPrefixes =
    [
        "/fibras/",
        "/noticias/",
    ];

    public static bool IsKnownSpaRoute(string? path)
    {
        var normalized = Normalize(path);

        if (KnownRoutes.Contains(normalized))
            return true;

        foreach (var prefix in DynamicPrefixes)
        {
            if (normalized.Length > prefix.Length
                && normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static string Normalize(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "/";

        var trimmed = path.TrimEnd('/');
        return trimmed.Length == 0 ? "/" : trimmed;
    }
}
