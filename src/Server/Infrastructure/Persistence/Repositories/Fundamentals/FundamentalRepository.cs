using Application.Fundamentals;
using Domain.Fundamentals;
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories.Fundamentals;

public class FundamentalRepository(AppDbContext db) : IFundamentalRepository
{
    public async Task<FundamentalRecord?> GetByIdAsync(Guid id, CancellationToken ct)
        => await db.FundamentalRecords.FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<FundamentalRecord?> GetProcessedByFibraAndPeriodAsync(Guid fibraId, string period, CancellationToken ct)
        => await db.FundamentalRecords
            .FirstOrDefaultAsync(r => r.FibraId == fibraId && r.Period == period && r.Status == "processed", ct);

    public async Task<FundamentalRecord?> GetLatestProcessedByFibraAsync(Guid fibraId, CancellationToken ct)
        => await db.FundamentalRecords
            .Where(r => r.FibraId == fibraId && r.Status == "processed")
            .OrderByDescending(r => r.Period.Substring(3, 4))
            .ThenByDescending(r => r.Period.Substring(1, 1))
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<string>> GetProcessedPeriodsAsync(Guid fibraId, CancellationToken ct)
        => await db.FundamentalRecords
            .Where(r => r.FibraId == fibraId && r.Status == "processed" && r.Period.Length == 7)
            .Select(r => r.Period)
            .Distinct()
            .OrderByDescending(p => p.Substring(3, 4))
            .ThenByDescending(p => p.Substring(1, 1))
            .Take(12)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<FundamentalRecord>> GetByFibraAsync(Guid fibraId, CancellationToken ct)
        => await db.FundamentalRecords
            .Where(r => r.FibraId == fibraId)
            .OrderByDescending(r => r.CapturedAt)
            .ToListAsync(ct);

    public async Task AddAsync(FundamentalRecord record, CancellationToken ct)
    {
        db.FundamentalRecords.Add(record);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateStatusAsync(Guid id, string status, string? confirmedBy, DateTimeOffset? confirmedAt, CancellationToken ct)
    {
        var record = await db.FundamentalRecords.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (record is null) throw new InvalidOperationException($"FundamentalRecord {id} not found during status update.");
        if (record.Status == "processed") return;
        record.Status = status;
        record.ConfirmedBy = confirmedBy;
        record.ConfirmedAt = confirmedAt;
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdatePdfReferenceAsync(Guid id, string pdfReference, CancellationToken ct)
    {
        var record = await db.FundamentalRecords.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (record is null) throw new InvalidOperationException($"FundamentalRecord {id} not found during PDF update.");
        record.PdfReference = pdfReference;
        record.PdfUploadedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateMarkdownContentAsync(Guid id, string markdownContent, CancellationToken ct)
    {
        var record = await db.FundamentalRecords.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (record is null) throw new InvalidOperationException($"FundamentalRecord {id} not found during markdown update.");
        record.MarkdownContent = markdownContent;
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateKpiExtractionAsync(Guid id, KpiExtractionResult result, CancellationToken ct)
    {
        var record = await db.FundamentalRecords.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (record is null) throw new InvalidOperationException($"FundamentalRecord {id} not found during KPI extraction update.");

        record.CapRate = result.CapRate;
        record.NavPerCbfi = result.NavPerCbfi;
        record.Ltv = result.Ltv;
        record.NoiMargin = result.NoiMargin;
        record.FfoMargin = result.FfoMargin;
        record.QuarterlyDistribution = result.QuarterlyDistribution;
        record.Summary = result.Summary;

        record.SetFieldNotes(new Dictionary<string, string?>
        {
            ["capRate"] = result.CapRateNote,
            ["navPerCbfi"] = result.NavPerCbfiNote,
            ["ltv"] = result.LtvNote,
            ["noiMargin"] = result.NoiMarginNote,
            ["ffoMargin"] = result.FfoMarginNote,
            ["quarterlyDistribution"] = result.QuarterlyDistributionNote,
            ["extractionNotes"] = result.ExtractionNotes,
        });

        var hasAnyKpi = result.CapRate.HasValue || result.NavPerCbfi.HasValue || result.Ltv.HasValue
            || result.NoiMargin.HasValue || result.FfoMargin.HasValue || result.QuarterlyDistribution.HasValue;

        record.Status = hasAnyKpi ? "partial" : "error";

        await db.SaveChangesAsync(ct);
    }
}
