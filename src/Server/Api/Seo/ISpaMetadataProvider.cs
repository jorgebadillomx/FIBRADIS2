namespace Api.Seo;

public interface ISpaMetadataProvider
{
    Task<SpaPageMeta?> GetMetaForPathAsync(string path, CancellationToken ct = default);

    /// <summary>
    /// Rutas canónicas de las páginas fijas conocidas por el provider. Fuente de verdad para
    /// el seed/backfill de filas StaticPage en seo.SeoMetadata. Excluye rutas privadas (p.ej.
    /// /herramientas tras 11-6) que no deben indexarse ni seedearse.
    /// </summary>
    IReadOnlyList<string> KnownPaths { get; }
}
