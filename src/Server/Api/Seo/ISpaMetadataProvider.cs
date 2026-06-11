namespace Api.Seo;

public interface ISpaMetadataProvider
{
    SpaPageMeta? GetMetaForPath(string path);
}
