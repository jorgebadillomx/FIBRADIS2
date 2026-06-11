using Domain.Catalog;

namespace Infrastructure.Jobs.Market;

public interface IMasDividendosImporterService
{
    Task<MasDividendosImportResult> ImportAsync(IReadOnlyList<Fibra> fibras, CancellationToken ct = default);
}
