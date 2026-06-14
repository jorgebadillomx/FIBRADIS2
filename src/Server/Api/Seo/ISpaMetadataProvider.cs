namespace Api.Seo;

public interface ISpaMetadataProvider
{
    Task<SpaPageMeta?> GetMetaForPathAsync(string path, CancellationToken ct = default);
}
