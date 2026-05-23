namespace Infrastructure.Integrations.Articles;

/// <summary>
/// Catálogo estático de selectores CSS por sitio para extracción de body_text.
/// Los selectores están ordenados del más específico al más genérico dentro de cada dominio.
/// </summary>
internal static class SiteExtractionCatalog
{
    // hostname (sin www, sin puerto) → selectores CSS ordenados por especificidad
    private static readonly Dictionary<string, string[]> Catalog =
        new(StringComparer.OrdinalIgnoreCase)
    {
        // ── Financieras mexicanas ─────────────────────────────────────────────
        ["elfinanciero.com.mx"]  = [".nota-body", ".article-body", ".entry-content", ".nota-contenido"],
        ["expansion.mx"]         = [".article-body", ".content-article", "[data-article-body]", ".entry-content"],
        ["eleconomista.com.mx"]  = [".story__content", ".notaContainer__body", ".nota-body", ".article-body"],
        ["milenio.com"]          = [".article-content", ".cuerpo-nota", ".nota-contenido", ".body-content"],
        ["excelsior.com.mx"]     = [".nota-body", ".news-body", ".article-body", ".article-text"],
        ["eluniversal.com.mx"]   = [".field-items", ".article-body", ".nota-cuerpo", ".entry-content"],
        ["reforma.com"]          = [".article-content", ".nota-body", ".notaBody", ".nota-text"],
        ["jornada.com.mx"]       = [".textonota", ".articulo", ".article-content", ".nota-body"],
        ["proceso.com.mx"]       = [".article-body", ".texto-nota", ".entry-content", ".nota-body"],
        ["heraldo.mx"]           = [".article-body", ".nota-body", ".content-article"],
        ["publimetro.com.mx"]    = [".article-body", ".entry-content", ".nota-body"],

        // ── Especializadas en real estate / finanzas MX ───────────────────────
        ["inmobiliare.com"]      = [".entry-content", ".article-body", ".post-content"],
        ["realestate.com.mx"]    = [".entry-content", ".article-body", ".post-content"],
        ["obras.expansion.mx"]   = [".article-body", ".content-article", ".entry-content"], // best-effort — verificar

        // ── Portales verificados en producción (feeds FIBRAS MX) ─────────────
        // bloomberglinea.com — verificado 2026-05-22, HTML: col...left-article-section
        ["bloomberglinea.com"]   = [".left-article-section", ".article-body", ".content-body"],
        // lasillarota.com — verificado 2026-05-22, HTML: article-content--cuerpo
        ["lasillarota.com"]      = [".article-content--cuerpo", ".article-content", ".main-article--body"],
        // centrourbano.com — verificado 2026-05-22, HTML: article.news-left div.entry
        ["centrourbano.com"]     = [".entry", "article.news-left", ".entry-content"],
        // elceo.com — Jannah/TIE WordPress theme — verificado estructura
        ["elceo.com"]            = [".entry-content", ".post-content", ".article-body"],
        // ejecentral.com.mx — WordPress — best-effort
        ["ejecentral.com.mx"]    = [".article-content", ".entry-content", ".post-content"],
        // forbes.com.mx — WordPress editorial
        ["forbes.com.mx"]        = [".article-body", ".entry-content", ".single-content", ".post-content"],
        // finance.yahoo.com / es.finance.yahoo.com — caas = Content As A Service (Yahoo CMS)
        ["finance.yahoo.com"]    = [".caas-body", ".caas-content", "[data-test-id='article-container']"],
        // bnamericas.com — best-effort (metadata noise en generic fallback)
        ["bnamericas.com"]       = [".bna-article__content", ".article-content", ".entry-content"],
        // wradio.com.mx
        ["wradio.com.mx"]        = [".article-body", ".nota-body", ".entry-content"],
        // opportimes.com — best-effort
        ["opportimes.com"]       = [".entry-content", ".article-body", ".post-content"],
        // elheraldodemexico.com (El Heraldo de México ya extrae bien, agregar por precisión)
        ["elheraldodemexico.com"] = [".article-body", ".nota-body", ".content-body"],
        // debate.com.mx — best-effort
        ["debate.com.mx"]        = [".article-body", ".news-content", ".entry-content"],
        // energiahoy.com.mx y energiaestrategica.com — WordPress
        ["energiahoy.com.mx"]    = [".entry-content", ".post-content", ".article-body"],
        ["energiaestrategica.com"] = [".entry-content", ".post-content", ".article-content"],

        // ── Internacionales frecuentes en feeds de FIBRAs ─────────────────────
        ["infobae.com"]          = [".article-body", ".story-article-body", ".body-article", ".article__body"],
        ["reuters.com"]          = ["[data-testid='article-body']", ".article-body__content", ".StandardArticleBody_body"],
        ["bloomberg.com"]        = [".body-content", ".article-body", ".fence-body"],
        ["marketwatch.com"]      = [".article__body", ".article-content", ".full-story"],
        ["wsj.com"]              = [".article-content", "[data-type='article']", ".wsj-snippet-body"],
        ["investing.com"]        = [".articlePage", ".WYSIWYG", ".articleText"],
        ["ft.com"]               = [".article__content-body", ".n-content-body", ".article-body"],
        ["bnnbloomberg.ca"]      = [".article-body", ".article__body", ".content-body"],
        // tradingview.com — artículos de noticias (usa CSS modules con hash, fallback a entry-content)
        ["tradingview.com"]      = [".entry-content", ".article-content", ".post-content"],

        // ── Portales MX verificados en producción (2026-05-22) ────────────────
        // realestatemarket.com.mx — Joomla + Gantry 5, div.item-page es el artículo completo
        ["realestatemarket.com.mx"] = [".item-page", ".entry-content", ".article-body"],
        // fundssociety.com — WordPress, post-content verificado
        ["fundssociety.com"]     = [".post-content", ".entry-content", ".article-body"],
        // larazon.com.mx — WordPress, entry-content estándar
        ["larazon.com.mx"]       = [".entry-content", ".post-content", ".article-body"],
        // reportur.com — WordPress travel/finance portal
        ["reportur.com"]         = [".entry-content", ".post-content", ".article-body"],
        // fashionnetwork.com — moda/lujo con acceso parcial (teaser ~500 chars verificado 2026-05-22)
        ["fashionnetwork.com"]   = [".article-content--texte", ".article-content"],
    };

    /// <summary>
    /// Busca selectores para el hostname dado.
    /// Intenta primero coincidencia exacta, luego dominio padre (stripping del primer segmento).
    /// </summary>
    public static bool TryGetSelectors(string hostname, out string[] selectors)
    {
        if (Catalog.TryGetValue(hostname, out selectors!))
            return true;

        // Strip subdomain: www.elfinanciero.com.mx → elfinanciero.com.mx
        var dotIndex = hostname.IndexOf('.');
        if (dotIndex > 0)
        {
            var parentDomain = hostname[(dotIndex + 1)..];
            if (Catalog.TryGetValue(parentDomain, out selectors!))
                return true;
        }

        selectors = [];
        return false;
    }
}
