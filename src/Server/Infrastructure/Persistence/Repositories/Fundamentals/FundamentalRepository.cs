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
            .Where(r => r.FibraId == fibraId && r.Period == period && r.Status == "processed" && r.DeletedAt == null)
            .OrderByDescending(r => r.ConfirmedAt)
            .FirstOrDefaultAsync(ct);

    public async Task<FundamentalRecord?> GetLatestProcessedByFibraAsync(Guid fibraId, CancellationToken ct)
        => await db.FundamentalRecords
            .Where(r => r.FibraId == fibraId && r.Status == "processed" && r.DeletedAt == null)
            .OrderByDescending(r => r.Period.Substring(3, 4))
            .ThenByDescending(r => r.Period.Substring(1, 1))
            .ThenByDescending(r => r.ConfirmedAt)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<string>> GetProcessedPeriodsAsync(Guid fibraId, CancellationToken ct)
        => await db.FundamentalRecords
            .Where(r => r.FibraId == fibraId && r.Status == "processed" && r.DeletedAt == null && r.Period.Length == 7)
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
        if (record.Status == status && status == "processed")
            return;

        record.Status = status;
        record.ConfirmedBy = confirmedBy;
        record.ConfirmedAt = confirmedAt;
        if (status == "processed")
            record.ErrorReason = null;
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
        record.Summary = result.SummaryMarkdown ?? result.Summary;

        record.SetFieldNotes(new Dictionary<string, string?>
        {
            ["capRate"] = result.CapRateNote,
            ["navPerCbfi"] = result.NavPerCbfiNote,
            ["ltv"] = result.LtvNote,
            ["noiMargin"] = result.NoiMarginNote,
            ["ffoMargin"] = result.FfoMarginNote,
            ["quarterlyDistribution"] = result.QuarterlyDistributionNote,
        });

        record.SetAiAnalysis(new FundamentalAiAnalysis(
            SummaryMarkdown: result.SummaryMarkdown,
            InvestorTakeaway: result.InvestorTakeaway,
            OperationalSignals: result.OperationalSignals ?? Array.Empty<string>(),
            FinancialSignals: result.FinancialSignals ?? Array.Empty<string>(),
            RiskFlags: result.RiskFlags ?? Array.Empty<string>(),
            ExtractionNotes: result.ExtractionNotes));

        record.Status = result.Success ? "partial" : "error";
        record.ErrorReason = result.Success || string.IsNullOrWhiteSpace(result.ExtractionNotes)
            ? null
            : result.ExtractionNotes;

        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateKpisManualAsync(
        Guid id,
        decimal? capRate, decimal? navPerCbfi, decimal? ltv,
        decimal? noiMargin, decimal? ffoMargin, decimal? quarterlyDistribution,
        string? summary,
        CancellationToken ct)
    {
        var record = await db.FundamentalRecords.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (record is null) throw new InvalidOperationException($"FundamentalRecord {id} not found during manual KPI update.");

        record.CapRate = capRate;
        record.NavPerCbfi = navPerCbfi;
        record.Ltv = ltv;
        record.NoiMargin = noiMargin;
        record.FfoMargin = ffoMargin;
        record.QuarterlyDistribution = quarterlyDistribution;
        record.Summary = summary;

        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateFieldNotesAsync(Guid id, Dictionary<string, string?> notes, CancellationToken ct)
    {
        var record = await db.FundamentalRecords.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (record is null) throw new InvalidOperationException($"FundamentalRecord {id} not found during field notes update.");

        record.SetFieldNotes(notes.ToDictionary(kv => kv.Key, kv => string.IsNullOrWhiteSpace(kv.Value) ? null : kv.Value));
        await db.SaveChangesAsync(ct);
    }

    public async Task SoftDeleteAsync(Guid id, string deletedBy, CancellationToken ct)
    {
        await db.FundamentalRecords
            .Where(r => r.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.DeletedAt, DateTimeOffset.UtcNow)
                .SetProperty(r => r.DeletedBy, deletedBy), ct);
    }
}
