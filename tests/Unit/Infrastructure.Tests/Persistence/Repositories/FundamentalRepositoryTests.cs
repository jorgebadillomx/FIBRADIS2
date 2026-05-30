using Domain.Catalog;
using Domain.Fundamentals;
using Application.Fundamentals;
using Infrastructure.Persistence.Repositories.Fundamentals;
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Tests.Persistence.Repositories;

public class FundamentalRepositoryTests
{
    private static AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options);

    private static Fibra CreateFibra(Guid id) => new()
    {
        Id = id,
        Ticker = "FUNO11",
        FullName = "Fibra Uno",
        ShortName = "Fibra Uno",
        Sector = "Diversificado",
        Market = "BMV",
        Currency = "MXN",
        State = FibraState.Active,
        NameVariants = [],
        CreatedAt = DateTime.UtcNow,
    };

    [Fact]
    public async Task AddAsync_PersistsRecord()
    {
        await using var db = CreateDbContext();
        var fibraId = Guid.NewGuid();
        db.Fibras.Add(CreateFibra(fibraId));
        await db.SaveChangesAsync();

        var repo = new FundamentalRepository(db);
        var record = new FundamentalRecord
        {
            Id = Guid.NewGuid(),
            FibraId = fibraId,
            Period = "Q3-2024",
            Status = "pending",
            ProcessingMode = "manual",
            CapRate = 0.08m,
            CapturedAt = DateTimeOffset.UtcNow,
        };

        await repo.AddAsync(record, CancellationToken.None);

        Assert.Equal(1, await db.FundamentalRecords.CountAsync());
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsRecord_WhenExists()
    {
        await using var db = CreateDbContext();
        var fibraId = Guid.NewGuid();
        db.Fibras.Add(CreateFibra(fibraId));
        var id = Guid.NewGuid();
        db.FundamentalRecords.Add(new FundamentalRecord
        {
            Id = id,
            FibraId = fibraId,
            Period = "Q3-2024",
            Status = "pending",
            ProcessingMode = "manual",
            CapturedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();

        var repo = new FundamentalRepository(db);
        var result = await repo.GetByIdAsync(id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Q3-2024", result!.Period);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotExists()
    {
        await using var db = CreateDbContext();
        var repo = new FundamentalRepository(db);

        var result = await repo.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetProcessedByFibraAndPeriodAsync_ReturnsOnlyProcessed()
    {
        await using var db = CreateDbContext();
        var fibraId = Guid.NewGuid();
        db.Fibras.Add(CreateFibra(fibraId));
        db.FundamentalRecords.AddRange(
            new FundamentalRecord
            {
                Id = Guid.NewGuid(), FibraId = fibraId, Period = "Q3-2024",
                Status = "processed", ProcessingMode = "manual",
                CapturedAt = DateTimeOffset.UtcNow,
            },
            new FundamentalRecord
            {
                Id = Guid.NewGuid(), FibraId = fibraId, Period = "Q3-2024",
                Status = "pending", ProcessingMode = "manual",
                CapturedAt = DateTimeOffset.UtcNow,
            });
        await db.SaveChangesAsync();

        var repo = new FundamentalRepository(db);
        var result = await repo.GetProcessedByFibraAndPeriodAsync(fibraId, "Q3-2024", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("processed", result!.Status);
    }

    [Fact]
    public async Task GetLatestProcessedByFibraAsync_ReturnsMostRecentProcessed()
    {
        await using var db = CreateDbContext();
        var fibraId = Guid.NewGuid();
        db.Fibras.Add(CreateFibra(fibraId));
        var older = DateTimeOffset.UtcNow.AddDays(-2);
        var newer = DateTimeOffset.UtcNow.AddDays(-1);
        db.FundamentalRecords.AddRange(
            new FundamentalRecord
            {
                Id = Guid.NewGuid(), FibraId = fibraId, Period = "Q2-2024",
                Status = "processed", ProcessingMode = "manual",
                CapturedAt = older,
            },
            new FundamentalRecord
            {
                Id = Guid.NewGuid(), FibraId = fibraId, Period = "Q3-2024",
                Status = "processed", ProcessingMode = "manual",
                CapturedAt = newer,
            });
        await db.SaveChangesAsync();

        var repo = new FundamentalRepository(db);
        var result = await repo.GetLatestProcessedByFibraAsync(fibraId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("Q3-2024", result!.Period);
    }

    [Fact]
    public async Task GetByFibraAsync_ReturnsAllRecordsOrderedByDateDesc()
    {
        await using var db = CreateDbContext();
        var fibraId = Guid.NewGuid();
        db.Fibras.Add(CreateFibra(fibraId));
        db.FundamentalRecords.AddRange(
            new FundamentalRecord
            {
                Id = Guid.NewGuid(), FibraId = fibraId, Period = "Q1-2024",
                Status = "processed", ProcessingMode = "manual",
                CapturedAt = DateTimeOffset.UtcNow.AddDays(-5),
            },
            new FundamentalRecord
            {
                Id = Guid.NewGuid(), FibraId = fibraId, Period = "Q3-2024",
                Status = "pending", ProcessingMode = "manual",
                CapturedAt = DateTimeOffset.UtcNow,
            });
        await db.SaveChangesAsync();

        var repo = new FundamentalRepository(db);
        var results = await repo.GetByFibraAsync(fibraId, CancellationToken.None);

        Assert.Equal(2, results.Count);
        Assert.Equal("Q3-2024", results[0].Period);
        Assert.Equal("Q1-2024", results[1].Period);
    }

    [Fact]
    public async Task UpdateStatusAsync_UpdatesStatusOnly()
    {
        await using var db = CreateDbContext();
        var fibraId = Guid.NewGuid();
        db.Fibras.Add(CreateFibra(fibraId));
        var id = Guid.NewGuid();
        db.FundamentalRecords.Add(new FundamentalRecord
        {
            Id = id,
            FibraId = fibraId,
            Period = "Q3-2024",
            Status = "pending",
            ProcessingMode = "manual",
            CapRate = 0.09m,
            CapturedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();

        var repo = new FundamentalRepository(db);
        await repo.UpdateStatusAsync(id, "processed", "jorge", DateTimeOffset.UtcNow, CancellationToken.None);

        var updated = await db.FundamentalRecords.FindAsync(id);
        Assert.NotNull(updated);
        Assert.Equal("processed", updated!.Status);
        Assert.Equal("jorge", updated.ConfirmedBy);
    }

    [Fact]
    public async Task UpdateStatusAsync_IsIdempotent_WhenAlreadyProcessed()
    {
        await using var db = CreateDbContext();
        var fibraId = Guid.NewGuid();
        db.Fibras.Add(CreateFibra(fibraId));
        var id = Guid.NewGuid();
        db.FundamentalRecords.Add(new FundamentalRecord
        {
            Id = id,
            FibraId = fibraId,
            Period = "Q3-2024",
            Status = "processed",
            ProcessingMode = "manual",
            CapturedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();

        var repo = new FundamentalRepository(db);
        await repo.UpdateStatusAsync(id, "processed", "jorge2", DateTimeOffset.UtcNow, CancellationToken.None);

        var record = await db.FundamentalRecords.FindAsync(id);
        Assert.Null(record!.ConfirmedBy);
    }

    [Fact]
    public async Task UpdateKpiExtractionAsync_PersistsAiAnalysis_AndClearsErrorReason_OnPartial()
    {
        await using var db = CreateDbContext();
        var fibraId = Guid.NewGuid();
        db.Fibras.Add(CreateFibra(fibraId));
        var id = Guid.NewGuid();
        db.FundamentalRecords.Add(new FundamentalRecord
        {
            Id = id,
            FibraId = fibraId,
            Period = "Q3-2024",
            Status = "pending",
            ProcessingMode = "ai",
            ErrorReason = "error previo",
            CapturedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();

        var repo = new FundamentalRepository(db);
        await repo.UpdateKpiExtractionAsync(id, new KpiExtractionResult(
            0.081m,
            "Cap rate explícito.",
            18.2m,
            "NAV explícito.",
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            "Resumen legacy",
            "Notas de extracción",
            true,
            SummaryMarkdown: "**Resumen** en markdown",
            InvestorTakeaway: "Takeaway",
            OperationalSignals: ["Ocupación alta"],
            FinancialSignals: ["LTV bajo"],
            RiskFlags: ["Riesgo puntual"]), CancellationToken.None);

        var updated = await db.FundamentalRecords.FindAsync(id);
        Assert.NotNull(updated);
        Assert.Equal("partial", updated!.Status);
        Assert.Null(updated.ErrorReason);
        Assert.Equal("**Resumen** en markdown", updated.Summary);

        var analysis = updated.GetAiAnalysis();
        Assert.NotNull(analysis);
        Assert.Equal("Takeaway", analysis!.InvestorTakeaway);
        Assert.Equal(["Ocupación alta"], analysis.OperationalSignals);
        Assert.Equal(["LTV bajo"], analysis.FinancialSignals);
        Assert.Equal(["Riesgo puntual"], analysis.RiskFlags);
        Assert.Equal("Notas de extracción", analysis.ExtractionNotes);
    }

    [Fact]
    public async Task UpdateKpiExtractionAsync_SetsPartial_WhenOnlyQualitativeDataExtracted()
    {
        await using var db = CreateDbContext();
        var fibraId = Guid.NewGuid();
        db.Fibras.Add(CreateFibra(fibraId));
        var id = Guid.NewGuid();
        db.FundamentalRecords.Add(new FundamentalRecord
        {
            Id = id,
            FibraId = fibraId,
            Period = "Q3-2024",
            Status = "pending",
            ProcessingMode = "ai",
            ErrorReason = "error previo",
            CapturedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();

        var repo = new FundamentalRepository(db);
        // Success=true pero sin KPIs numéricos — solo análisis cualitativo
        await repo.UpdateKpiExtractionAsync(id, new KpiExtractionResult(
            null, null, null, null, null, null, null, null, null, null, null, null,
            null,
            "Notas de extracción cualitativa",
            true,
            SummaryMarkdown: "**Resumen** sin KPIs numéricos",
            InvestorTakeaway: "Takeaway cualitativo",
            OperationalSignals: ["Ocupación estable"],
            FinancialSignals: [],
            RiskFlags: []), CancellationToken.None);

        var updated = await db.FundamentalRecords.FindAsync(id);
        Assert.NotNull(updated);
        Assert.Equal("partial", updated!.Status);
        Assert.Null(updated.ErrorReason);
        Assert.Equal("**Resumen** sin KPIs numéricos", updated.Summary);
    }

    [Fact]
    public async Task UpdateKpiExtractionAsync_SetsError_AndFillsErrorReason_WhenNothingExtracted()
    {
        await using var db = CreateDbContext();
        var fibraId = Guid.NewGuid();
        db.Fibras.Add(CreateFibra(fibraId));
        var id = Guid.NewGuid();
        db.FundamentalRecords.Add(new FundamentalRecord
        {
            Id = id,
            FibraId = fibraId,
            Period = "Q3-2024",
            Status = "pending",
            ProcessingMode = "ai",
            CapturedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();

        var repo = new FundamentalRepository(db);
        await repo.UpdateKpiExtractionAsync(id, new KpiExtractionResult(
            null, null, null, null, null, null, null, null, null, null, null, null,
            null,
            "No se pudo extraer ningún dato del reporte.",
            false), CancellationToken.None);

        var updated = await db.FundamentalRecords.FindAsync(id);
        Assert.NotNull(updated);
        Assert.Equal("error", updated!.Status);
        Assert.Equal("No se pudo extraer ningún dato del reporte.", updated.ErrorReason);
    }

    [Fact]
    public async Task UpdateFieldNotesAsync_ReplacesWholeDictionary()
    {
        await using var db = CreateDbContext();
        var fibraId = Guid.NewGuid();
        db.Fibras.Add(CreateFibra(fibraId));
        var id = Guid.NewGuid();
        var record = new FundamentalRecord
        {
            Id = id,
            FibraId = fibraId,
            Period = "Q3-2024",
            Status = "partial",
            ProcessingMode = "manual",
            CapturedAt = DateTimeOffset.UtcNow,
        };
        record.SetFieldNotes(new Dictionary<string, string?> { ["capRate"] = "nota anterior" });
        db.FundamentalRecords.Add(record);
        await db.SaveChangesAsync();

        var repo = new FundamentalRepository(db);
        await repo.UpdateFieldNotesAsync(id, new Dictionary<string, string?>
        {
            ["capRate"] = "nota nueva",
            ["ltv"] = null,
        }, CancellationToken.None);

        var updated = await db.FundamentalRecords.FindAsync(id);
        Assert.NotNull(updated);
        Assert.Equal("nota nueva", updated!.GetFieldNotes()!["capRate"]);
        Assert.False(updated.GetFieldNotes()!.ContainsKey("ltv"));
    }
}
