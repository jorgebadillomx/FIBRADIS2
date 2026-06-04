using Domain.Catalog;

namespace Application.Portfolio;

public interface IPortfolioUploadService
{
    Task<PortfolioUploadResult> ParseAndValidateAsync(
        Stream fileStream,
        string fileName,
        IReadOnlyList<Fibra> activeFibras,
        decimal commissionFactor,
        CancellationToken ct = default);
}
