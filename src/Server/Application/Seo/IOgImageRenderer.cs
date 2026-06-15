using Domain.Catalog;
using Domain.Seo;

namespace Application.Seo;

public interface IOgImageRenderer
{
    Task<byte[]> RenderFibraCardAsync(
        Fibra? fibra,
        FibraSeoMarketData? marketData,
        CancellationToken ct = default);
}
