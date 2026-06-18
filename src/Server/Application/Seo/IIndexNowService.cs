namespace Application.Seo;

public interface IIndexNowService
{
    Task PingAsync(IEnumerable<string> urls, CancellationToken ct = default);
}
