using Domain.Fundamentals;

namespace Application.Fundamentals;

public interface IFundamentalRepository
{
    Task<FundamentalRecord?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<FundamentalRecord?> GetProcessedByFibraAndPeriodAsync(Guid fibraId, string period, CancellationToken ct);
    Task<FundamentalRecord?> GetLatestProcessedByFibraAsync(Guid fibraId, CancellationToken ct);
    Task<IReadOnlyList<string>> GetProcessedPeriodsAsync(Guid fibraId, CancellationToken ct);
    Task<IReadOnlyList<FundamentalRecord>> GetByFibraAsync(Guid fibraId, CancellationToken ct);
    Task AddAsync(FundamentalRecord record, CancellationToken ct);
    Task UpdateStatusAsync(Guid id, string status, string? confirmedBy, DateTimeOffset? confirmedAt, CancellationToken ct);
    Task UpdatePdfReferenceAsync(Guid id, string pdfReference, CancellationToken ct);
    Task UpdateMarkdownContentAsync(Guid id, string markdownContent, CancellationToken ct);
    Task UpdateKpiExtractionAsync(Guid id, KpiExtractionResult result, CancellationToken ct);
    Task UpdateKpisManualAsync(Guid id, decimal? capRate, decimal? navPerCbfi, decimal? ltv, decimal? noiMargin, decimal? ffoMargin, decimal? quarterlyDistribution, string? summary, CancellationToken ct);
    Task UpdateFieldNotesAsync(Guid id, Dictionary<string, string?> notes, CancellationToken ct);
    Task SoftDeleteAsync(Guid id, string deletedBy, CancellationToken ct);

    Task<IReadOnlyList<(FundamentalRecord Record, string Ticker, string ShortName)>> GetSummaryLatestAsync(CancellationToken ct = default);
    Task<IReadOnlyList<(FundamentalRecord Record, string Ticker, string ShortName)>> GetSummaryByPeriodAsync(string period, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetAllProcessedPeriodsAsync(CancellationToken ct = default);
}
