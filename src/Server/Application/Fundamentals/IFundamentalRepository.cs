using Domain.Fundamentals;

namespace Application.Fundamentals;

public interface IFundamentalRepository
{
    Task<FundamentalRecord?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<FundamentalRecord?> GetProcessedByFibraAndPeriodAsync(Guid fibraId, string period, CancellationToken ct);
    Task<FundamentalRecord?> GetLatestProcessedByFibraAsync(Guid fibraId, CancellationToken ct);
    Task<IReadOnlyList<FundamentalRecord>> GetByFibraAsync(Guid fibraId, CancellationToken ct);
    Task AddAsync(FundamentalRecord record, CancellationToken ct);
    Task UpdateStatusAsync(Guid id, string status, string? confirmedBy, DateTimeOffset? confirmedAt, CancellationToken ct);
    Task UpdatePdfReferenceAsync(Guid id, string pdfReference, CancellationToken ct);
}
