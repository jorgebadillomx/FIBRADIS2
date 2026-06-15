using System.Threading;

namespace Api.Seo;

public sealed class SeoSitemapCacheState
{
    private int _version;

    public int Version => Volatile.Read(ref _version);

    public void Invalidate() => Interlocked.Increment(ref _version);

    public string WithVersion(string cacheKey) => $"{cacheKey}:{Version}";
}
