using Application.Catalog;
using Application.Fundamentals;
using Application.Jobs;
using Domain.Catalog;
using Domain.Fundamentals;
using Domain.Jobs;
using Infrastructure.Jobs.Fundamentals;
using Infrastructure.Persistence.Repositories.Fundamentals;
using Infrastructure.Persistence.SqlServer;
using Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net;
using System.Net.Http.Headers;

namespace Infrastructure.Tests.Jobs.Fundamentals;

public class FundamentalsAutomationServiceTests
{
    private static readonly DateTimeOffset _utcNow = new(2026, 6, 9, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ExecuteAsync_WhenNoCandidates_ReturnsZeroCountsAndNoRecords()
    {
        await using var db = CreateDbContext();
        var fibra = SeedFibra(db, "FUNO11");
        var source = new FakeDiscoverySource("AMEFIBRA", [], ["FUNO11"]);
        var service = BuildService(db, [source], [fibra]);

        var result = await service.ExecuteAsync(CancellationToken.None);

        Assert.Equal(0, result.NewReports);
        Assert.Equal(0, result.Errors);
        Assert.Empty(await db.FundamentalSourceManifests.ToListAsync());
        Assert.Empty(await db.FundamentalRecords.ToListAsync());
    }

    [Fact]
    public async Task ExecuteAsync_WhenCandidateIsNew_CreatesProcessedRecordAndManifest()
    {
        await using var db = CreateDbContext();
        var fibra = SeedFibra(db, "FUNO11");
        var candidate = new FundamentalsDiscoveryCandidate(
            "AMEFIBRA", "2022 Reporte T4 FUNO",
            "https://amefibra.com/download/funo-q4-2022/",
            "https://amefibra.com/download/funo-q4-2022/report.pdf",
            "Q4-2022", "quarterly", DateTimeOffset.Parse("2023-02-28T00:00:00Z"));

        var source = new FakeDiscoverySource("AMEFIBRA", [candidate], ["FUNO11"]);
        var service = BuildService(db, [source], [fibra], pdfContent: ReadFixturePdf());

        var result = await service.ExecuteAsync(CancellationToken.None);

        Assert.Equal(1, result.NewReports);
        Assert.Equal(1, result.RecordsProcessed);
        Assert.Equal(1, await db.FundamentalSourceManifests.CountAsync());
        var record = await db.FundamentalRecords.SingleAsync();
        Assert.Equal("processed", record.Status);
        Assert.Equal("api", record.ProcessingMode);
        Assert.Equal("system:AMEFIBRA", record.ImportedBy);
        Assert.False(record.IsPossibleUpdate);
    }

    [Fact]
    public async Task ExecuteAsync_WithTickerProcessesOnlySelectedFibra()
    {
        await using var db = CreateDbContext();
        var fibraFuno = SeedFibra(db, "FUNO11");
        var fibraFmty = SeedFibra(db, "FMTY14");

        var candidate = new FundamentalsDiscoveryCandidate(
            "AMEFIBRA", "2026 Reporte T1",
            "https://amefibra.com/download/reporte-t1/",
            "https://amefibra.com/download/reporte-t1/report.pdf",
            "Q1-2026", "quarterly", DateTimeOffset.Parse("2026-04-30T00:00:00Z"));

        var source = new FakeDiscoverySource("AMEFIBRA", [candidate], ["FUNO11", "FMTY14"]);
        var service = BuildService(db, [source], [fibraFuno, fibraFmty], pdfContent: ReadFixturePdf());

        var result = await service.ExecuteAsync("FUNO11", CancellationToken.None);

        Assert.Equal(1, result.FibrasScanned);
        Assert.Single(await db.FundamentalRecords.ToListAsync());
        var record = await db.FundamentalRecords.SingleAsync();
        Assert.Equal(fibraFuno.Id, record.FibraId);
    }

    [Fact]
    public async Task ExecuteAsync_WithMissingTicker_ThrowsInvalidOperationException()
    {
        await using var db = CreateDbContext();
        var fibra = SeedFibra(db, "FUNO11");
        var service = BuildService(db, [new FakeDiscoverySource("AMEFIBRA", [], ["FUNO11"])], [fibra]);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ExecuteAsync("NOPE99", CancellationToken.None));

        Assert.Equal("FIBRA 'NOPE99' no encontrada o no está activa.", ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WithInactiveTicker_ThrowsInvalidOperationException()
    {
        await using var db = CreateDbContext();
        var fibra = SeedFibra(db, "INACTIVA1", state: FibraState.Inactive);
        var service = BuildService(db, [new FakeDiscoverySource("AMEFIBRA", [], ["INACTIVA1"])], [fibra]);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ExecuteAsync("INACTIVA1", CancellationToken.None));

        Assert.Equal("FIBRA 'INACTIVA1' no encontrada o no está activa.", ex.Message);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPackageAlreadyExists_SkipsWithoutCreatingRecord()
    {
        await using var db = CreateDbContext();
        var fibra = SeedFibra(db, "FUNO11");
        db.FundamentalSourceManifests.Add(new FundamentalSourceManifest
        {
            Id = Guid.NewGuid(),
            SourceName = "AMEFIBRA",
            FibraId = fibra.Id,
            SourceTitle = "2022 Reporte T4 FUNO",
            Period = "Q4-2022",
            ReportType = "quarterly",
            DiscoveryStatus = "eligible",
            PackageUrl = "https://amefibra.com/download/funo-q4-2022/",
            FirstSeenAt = DateTimeOffset.UtcNow.AddDays(-1),
            LastSeenAt = DateTimeOffset.UtcNow.AddDays(-1),
            LastDecision = "new",
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-1),
            UpdatedAt = DateTimeOffset.UtcNow.AddDays(-1),
        });
        await db.SaveChangesAsync();

        var candidate = new FundamentalsDiscoveryCandidate(
            "AMEFIBRA", "2022 Reporte T4 FUNO",
            "https://amefibra.com/download/funo-q4-2022/",
            "https://amefibra.com/download/funo-q4-2022/report.pdf",
            "Q4-2022", "quarterly", null);
        var source = new FakeDiscoverySource("AMEFIBRA", [candidate], ["FUNO11"]);
        var service = BuildService(db, [source], [fibra]);

        var result = await service.ExecuteAsync(CancellationToken.None);

        Assert.Equal(1, result.SkippedReports);
        Assert.Equal(0, result.RecordsProcessed);
        Assert.Empty(db.FundamentalRecords);
    }

    // T7.5 — multi-source tests

    [Fact]
    public async Task ExecuteAsync_TwoSources_OneNewCandidateEach_CreatesTwoRecords()
    {
        await using var db = CreateDbContext();
        var fibra = SeedFibra(db, "FHIPO14");

        var source1 = new FakeDiscoverySource("official:FHIPO14", [
            new FundamentalsDiscoveryCandidate("official:FHIPO14", "1T26 FHipo", "https://fhipo.com/1t26.pdf", "https://fhipo.com/1t26.pdf", "Q1-2026", "quarterly", null)
        ], ["FHIPO14"]);

        var source2 = new FakeDiscoverySource("official:FHIPO14-v2", [
            new FundamentalsDiscoveryCandidate("official:FHIPO14-v2", "4T25 FHipo", "https://fhipo.com/4t25.pdf", "https://fhipo.com/4t25.pdf", "Q4-2025", "quarterly", null)
        ], ["FHIPO14"]);

        var service = BuildService(db, [source1, source2], [fibra], pdfContent: ReadFixturePdf());

        var result = await service.ExecuteAsync(CancellationToken.None);

        Assert.Equal(2, result.NewReports);
        Assert.Equal(2, result.RecordsProcessed);
        Assert.Equal(2, await db.FundamentalRecords.CountAsync());
    }

    [Fact]
    public async Task ExecuteAsync_SameFibraAndPeriodFromTwoSources_SecondSkippedByCurrentRunDedup()
    {
        await using var db = CreateDbContext();
        var fibra = SeedFibra(db, "FHIPO14");

        var source1 = new FakeDiscoverySource("official:FHIPO14-a", [
            new FundamentalsDiscoveryCandidate("official:FHIPO14-a", "1T26 FHipo via A", "https://fhipo.com/a/1t26.pdf", "https://fhipo.com/a/1t26.pdf", "Q1-2026", "quarterly", null)
        ], ["FHIPO14"]);

        var source2 = new FakeDiscoverySource("official:FHIPO14-b", [
            new FundamentalsDiscoveryCandidate("official:FHIPO14-b", "1T26 FHipo via B", "https://fhipo.com/b/1t26.pdf", "https://fhipo.com/b/1t26.pdf", "Q1-2026", "quarterly", null)
        ], ["FHIPO14"]);

        var service = BuildService(db, [source1, source2], [fibra], pdfContent: ReadFixturePdf());

        var result = await service.ExecuteAsync(CancellationToken.None);

        Assert.Equal(1, result.NewReports);
        Assert.Equal(0, result.PossibleUpdates);
        Assert.Equal(1, result.RecordsProcessed);
        Assert.Equal(1, await db.FundamentalSourceManifests.CountAsync());
        Assert.Equal(1, await db.FundamentalRecords.CountAsync());
    }

    [Fact]
    public async Task ExecuteAsync_WhenSourceThrows_ErrorIsolatedAndRestContinues()
    {
        await using var db = CreateDbContext();
        var fibra1 = SeedFibra(db, "FHIPO14");
        var fibra2 = SeedFibra(db, "FCFE18", "CFE Fibra E");

        var failingSource = new FakeDiscoverySource("failing", null /* throws */, ["FHIPO14"]);
        var goodSource = new FakeDiscoverySource("official:FCFE18", [
            new FundamentalsDiscoveryCandidate("official:FCFE18", "1T26 FCFE", "https://cfecapital.com.mx/1t26.pdf", "https://cfecapital.com.mx/1t26.pdf", "Q1-2026", "quarterly", null)
        ], ["FCFE18"]);

        var service = BuildService(db, [failingSource, goodSource], [fibra1, fibra2], pdfContent: ReadFixturePdf());

        var result = await service.ExecuteAsync(CancellationToken.None);

        Assert.Equal(1, result.Errors);
        Assert.Equal(1, result.NewReports);
        Assert.Equal(1, await db.FundamentalRecords.CountAsync());
    }

    // T7.6 — regression: EmptySource returns [] without crashing
    [Fact]
    public async Task ExecuteAsync_WhenSourceReturnsEmpty_NoManifestCreated()
    {
        await using var db = CreateDbContext();
        var fibra = SeedFibra(db, "SOMA21");
        var source = new FakeDiscoverySource("official:SOMA21", [], ["SOMA21"]);
        var service = BuildService(db, [source], [fibra]);

        await service.ExecuteAsync(CancellationToken.None);

        Assert.Empty(await db.FundamentalSourceManifests.ToListAsync());
    }

    [Fact]
    public async Task ExecuteAsync_WhenCandidatePeriodIsBeforeWindow_SkipsBeforeManifest()
    {
        await using var db = CreateDbContext();
        var fibra = SeedFibra(db, "FUNO11");
        db.FundamentalRecords.Add(new FundamentalRecord
        {
            Id = Guid.NewGuid(),
            FibraId = fibra.Id,
            Period = "Q3-2025",
            Status = "processed",
            ProcessingMode = "manual",
            CapturedAt = _utcNow.AddDays(-3),
            ConfirmedAt = _utcNow.AddDays(-3),
        });
        await db.SaveChangesAsync();

        var candidate = new FundamentalsDiscoveryCandidate(
            "AMEFIBRA", "2025 T3 FUNO",
            "https://amefibra.com/download/funo-q3-2025/",
            "https://amefibra.com/download/funo-q3-2025/report.pdf",
            "Q3-2025", "quarterly", null);

        var service = new FundamentalsAutomationService(
            [new FakeDiscoverySource("AMEFIBRA", [candidate], ["FUNO11"])],
            new FakeFundamentalsFibraRepository([fibra]),
            new FundamentalRepository(db),
            new ThrowingManifestRepository(),
            new FakeKpiExtractorService(),
            new FakePipelineErrorLogRepository(),
            new ThrowingPdfHttpClientFactory(),
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Uploads:BasePath"] = Path.Combine(Path.GetTempPath(), $"fibradis-tests-{Guid.NewGuid():N}")
            }).Build(),
            new FakeTimeService(_utcNow),
            NullLogger<FundamentalsAutomationService>.Instance);

        var result = await service.ExecuteAsync(CancellationToken.None);

        Assert.Equal(1, result.ReportsDetected);
        Assert.Equal(0, result.RecordsProcessed);
        Assert.Equal(0, result.NewReports);
        Assert.Equal(0, result.SkippedReports);
        Assert.Empty(await db.FundamentalSourceManifests.ToListAsync());
        Assert.Equal(1, await db.FundamentalRecords.CountAsync());
    }

    [Fact]
    public async Task ExecuteAsync_WhenCandidatePeriodAlreadyExistsInProcessedPeriods_SkipsBeforeManifest()
    {
        await using var db = CreateDbContext();
        var fibra = SeedFibra(db, "FUNO11");

        var candidate = new FundamentalsDiscoveryCandidate(
            "AMEFIBRA", "2025 T4 FUNO",
            "https://amefibra.com/download/funo-q4-2025/",
            "https://amefibra.com/download/funo-q4-2025/report.pdf",
            "Q4-2025", "quarterly", null);

        var service = new FundamentalsAutomationService(
            [new FakeDiscoverySource("AMEFIBRA", [candidate], ["FUNO11"])],
            new FakeFundamentalsFibraRepository([fibra]),
            new FakeFundamentalRepository(latestProcessedPeriod: "Q2-2025", processedPeriods: ["Q4-2025"]),
            new ThrowingManifestRepository(),
            new FakeKpiExtractorService(),
            new FakePipelineErrorLogRepository(),
            new ThrowingPdfHttpClientFactory(),
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Uploads:BasePath"] = Path.Combine(Path.GetTempPath(), $"fibradis-tests-{Guid.NewGuid():N}")
            }).Build(),
            new FakeTimeService(_utcNow),
            NullLogger<FundamentalsAutomationService>.Instance);

        var result = await service.ExecuteAsync(CancellationToken.None);

        Assert.Equal(1, result.ReportsDetected);
        Assert.Equal(0, result.RecordsProcessed);
        Assert.Equal(0, result.NewReports);
        Assert.Equal(0, result.SkippedReports);
    }

    [Fact]
    public async Task ExecuteAsync_WhenSource1HasOldPeriodsAndSource2HasNewPeriod_Source1SkippedSource2Processed()
    {
        await using var db = CreateDbContext();
        var fibra = SeedFibra(db, "VESTA15");
        db.FundamentalRecords.Add(new FundamentalRecord
        {
            Id = Guid.NewGuid(),
            FibraId = fibra.Id,
            Period = "Q3-2025",
            Status = "processed",
            ProcessingMode = "manual",
            CapturedAt = _utcNow.AddDays(-30),
            ConfirmedAt = _utcNow.AddDays(-30),
        });
        await db.SaveChangesAsync();

        // Source1: tres candidatos anteriores a la ventana (fromPeriod = Q4-2025)
        var source1 = new FakeDiscoverySource("Economatica",
        [
            new("Economatica", "VESTA15 Q1-2025", "https://eco.com/q1-2025", "https://eco.com/q1-2025.pdf", "Q1-2025", "quarterly", null),
            new("Economatica", "VESTA15 Q2-2025", "https://eco.com/q2-2025", "https://eco.com/q2-2025.pdf", "Q2-2025", "quarterly", null),
            new("Economatica", "VESTA15 Q3-2025", "https://eco.com/q3-2025", "https://eco.com/q3-2025.pdf", "Q3-2025", "quarterly", null),
        ], ["VESTA15"]);

        // Source2: candidato dentro de la ventana — fallback implícito
        var source2 = new FakeDiscoverySource("OfficialSite",
        [
            new("OfficialSite", "VESTA15 Q4-2025", "https://vesta.com/q4-2025", "https://vesta.com/q4-2025.pdf", "Q4-2025", "quarterly", null),
        ], ["VESTA15"]);

        var service = BuildService(db, [source1, source2], [fibra], pdfContent: ReadFixturePdf());

        var result = await service.ExecuteAsync(CancellationToken.None);

        Assert.Equal(4, result.ReportsDetected); // 3 eco + 1 official
        Assert.Equal(1, result.NewReports);
        Assert.Equal(1, result.RecordsProcessed);
        Assert.Equal(2, await db.FundamentalRecords.CountAsync()); // 1 pre-existing + 1 new
        Assert.Equal(0, result.PossibleUpdates);
    }

    private static FundamentalsAutomationService BuildService(
        AppDbContext db,
        IEnumerable<IFundamentalsDiscoverySource> sources,
        IReadOnlyList<Fibra> fibras,
        byte[]? pdfContent = null,
        FakeTimeService? time = null)
        => new(
            sources,
            new FakeFundamentalsFibraRepository(fibras),
            new FundamentalRepository(db),
            new FundamentalSourceManifestRepository(db),
            new FakeKpiExtractorService(),
            new FakePipelineErrorLogRepository(),
            new FakePdfHttpClientFactory(pdfContent ?? []),
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Uploads:BasePath"] = Path.Combine(Path.GetTempPath(), $"fibradis-tests-{Guid.NewGuid():N}")
            }).Build(),
            time ?? new FakeTimeService(_utcNow),
            NullLogger<FundamentalsAutomationService>.Instance);

    private static AppDbContext CreateDbContext()
        => new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options);

    private static Fibra SeedFibra(AppDbContext db, string ticker, string fullName = "Test Fibra", FibraState state = FibraState.Active)
    {
        var fibra = new Fibra
        {
            Id = Guid.NewGuid(),
            Ticker = ticker,
            YahooTicker = $"{ticker}.MX",
            FullName = fullName,
            ShortName = ticker,
            Currency = "MXN",
            Market = "BMV",
            Sector = "Diversificado",
            State = state,
            NameVariants = [ticker],
            CreatedAt = DateTimeOffset.UtcNow,
        };
        db.Fibras.Add(fibra);
        db.SaveChanges();
        return fibra;
    }

    private static byte[] ReadFixturePdf()
        => File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Fixtures", "amefibra-sample.pdf"));
}

// FakeDiscoverySource — returns pre-configured candidates; null candidates list simulates exception
internal sealed class FakeDiscoverySource(
    string sourceName,
    IReadOnlyList<FundamentalsDiscoveryCandidate>? candidates,
    IReadOnlyList<string> tickers) : IFundamentalsDiscoverySource
{
    public string SourceName => sourceName;
    public IReadOnlyList<string> SupportedTickers => tickers;

    public Task<IReadOnlyList<FundamentalsDiscoveryCandidate>> DiscoverCandidatesAsync(Fibra fibra, CancellationToken ct)
    {
        if (candidates is null)
            throw new InvalidOperationException($"Simulated failure in source {sourceName}");
        return Task.FromResult(candidates);
    }
}

// Fake IHttpClientFactory that returns an HttpClient returning fixed PDF bytes
internal sealed class FakePdfHttpClientFactory(byte[] pdfContent) : IHttpClientFactory
{
    public HttpClient CreateClient(string name)
        => new(new FakePdfHandler(pdfContent));

    private sealed class FakePdfHandler(byte[] content) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(content),
                RequestMessage = request,
            };
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            return Task.FromResult(response);
        }
    }
}

internal sealed class FakeFundamentalsFibraRepository(IReadOnlyList<Fibra> fibras) : IFibraRepository
{
    public Task AddAsync(Fibra fibra, CancellationToken ct = default) => Task.CompletedTask;
    public Task UpdateAsync(Fibra fibra, CancellationToken ct = default) => Task.CompletedTask;
    public Task<bool> ExistsByTickerAsync(string ticker, CancellationToken ct = default) => Task.FromResult(false);
    public Task<(IReadOnlyList<Fibra> Items, int Total)> GetActivePagedAsync(FibraFilter filter, CancellationToken ct = default)
        => Task.FromResult<(IReadOnlyList<Fibra>, int)>((fibras, fibras.Count));
    public Task<Fibra?> GetByTickerAsync(string ticker, CancellationToken ct = default)
        => Task.FromResult(fibras.FirstOrDefault(f => f.Ticker == ticker));
    public Task<Fibra?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(fibras.FirstOrDefault(f => f.Id == id));
    public Task<IReadOnlyList<Fibra>> GetAllAsync(CancellationToken ct = default)
        => Task.FromResult(fibras);
    public Task<IReadOnlyList<Fibra>> GetAllActiveAsync(CancellationToken ct = default)
        => Task.FromResult(fibras);
}

internal sealed class FakeKpiExtractorService : IKpiExtractorService
{
    public Task<KpiExtractionResult> ExtractAsync(string markdownContent, CancellationToken ct, Guid? relatedEntityId = null)
        => Task.FromResult(new KpiExtractionResult(
            0.08m, null, 120m, null, 0.35m, null, 0.72m, null, 0.65m, null, 0.45m, null,
            "Resumen automático", "ok", true));
}

internal sealed class FakePipelineErrorLogRepository : IPipelineErrorLogRepository
{
    public List<PipelineErrorLog> Entries { get; } = [];
    public Task LogErrorAsync(PipelineErrorLog entry, CancellationToken ct = default)
    {
        Entries.Add(entry);
        return Task.CompletedTask;
    }

    public Task<(IReadOnlyList<PipelineErrorLog> Items, int Total)> GetPagedAsync(string? pipeline, int page, int pageSize, CancellationToken ct = default)
        => Task.FromResult<(IReadOnlyList<PipelineErrorLog>, int)>((Entries, Entries.Count));
}

internal sealed class FakeTimeService(DateTimeOffset utcNow) : ITimeService
{
    public DateTimeOffset UtcNow => utcNow;
}

internal sealed class FakeFundamentalRepository(
    string? latestProcessedPeriod = null,
    IReadOnlyList<string>? processedPeriods = null) : IFundamentalRepository
{
    public List<FundamentalRecord> AddedRecords { get; } = [];

    public Task<FundamentalRecord?> GetByIdAsync(Guid id, CancellationToken ct)
        => Task.FromResult<FundamentalRecord?>(null);

    public Task<FundamentalRecord?> GetProcessedByFibraAndPeriodAsync(Guid fibraId, string period, CancellationToken ct)
        => Task.FromResult<FundamentalRecord?>(null);

    public Task<FundamentalRecord?> GetLatestProcessedByFibraAsync(Guid fibraId, CancellationToken ct)
        => Task.FromResult<FundamentalRecord?>(latestProcessedPeriod is null
            ? null
            : new FundamentalRecord
            {
                FibraId = fibraId,
                Period = latestProcessedPeriod,
                Status = "processed",
            });

    public Task<IReadOnlyList<string>> GetProcessedPeriodsAsync(Guid fibraId, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<string>>(processedPeriods ?? []);

    public Task<IReadOnlyList<FundamentalRecord>> GetByFibraAsync(Guid fibraId, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<FundamentalRecord>>([]);

    public Task AddAsync(FundamentalRecord record, CancellationToken ct)
    {
        AddedRecords.Add(record);
        return Task.CompletedTask;
    }

    public Task UpdateStatusAsync(Guid id, string status, string? confirmedBy, DateTimeOffset? confirmedAt, CancellationToken ct)
        => Task.CompletedTask;

    public Task UpdatePdfReferenceAsync(Guid id, string pdfReference, CancellationToken ct)
        => Task.CompletedTask;

    public Task UpdateMarkdownContentAsync(Guid id, string markdownContent, CancellationToken ct)
        => Task.CompletedTask;

    public Task UpdateKpiExtractionAsync(Guid id, KpiExtractionResult result, CancellationToken ct)
        => Task.CompletedTask;

    public Task UpdateKpisManualAsync(Guid id, decimal? capRate, decimal? navPerCbfi, decimal? ltv, decimal? noiMargin, decimal? ffoMargin, decimal? quarterlyDistribution, string? summary, CancellationToken ct)
        => Task.CompletedTask;

    public Task UpdateFieldNotesAsync(Guid id, Dictionary<string, string?> notes, CancellationToken ct)
        => Task.CompletedTask;

    public Task SoftDeleteAsync(Guid id, string deletedBy, CancellationToken ct)
        => Task.CompletedTask;

    public Task<IReadOnlyList<(FundamentalRecord Record, string Ticker, string ShortName)>> GetSummaryLatestAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<(FundamentalRecord Record, string Ticker, string ShortName)>>([]);

    public Task<IReadOnlyList<(FundamentalRecord Record, string Ticker, string ShortName)>> GetSummaryByPeriodAsync(string period, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<(FundamentalRecord Record, string Ticker, string ShortName)>>([]);

    public Task<IReadOnlyList<(FundamentalRecord Record, string Ticker, string ShortName)>> GetSummaryForRecentPeriodsAsync(int count, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<(FundamentalRecord Record, string Ticker, string ShortName)>>([]);

    public Task<IReadOnlyList<string>> GetAllProcessedPeriodsAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<string>>([]);
}

internal sealed class ThrowingManifestRepository : IFundamentalSourceManifestRepository
{
    private static InvalidOperationException UnexpectedCall()
        => new("Manifest repository should not be called for skipped candidates.");

    public Task<FundamentalSourceManifest?> GetBySourceAndPackageUrlAsync(string sourceName, string? packageUrl, CancellationToken ct)
        => throw UnexpectedCall();

    public Task<FundamentalSourceManifest?> GetLatestByFibraAndPeriodAsync(Guid fibraId, string period, CancellationToken ct)
        => throw UnexpectedCall();

    public Task AddAsync(FundamentalSourceManifest manifest, CancellationToken ct)
        => throw UnexpectedCall();

    public Task UpdateAsync(FundamentalSourceManifest manifest, CancellationToken ct)
        => throw UnexpectedCall();
}

internal sealed class ThrowingPdfHttpClientFactory : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => new(new ThrowingPdfHandler());

    private sealed class ThrowingPdfHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => throw new InvalidOperationException("PDF downloader should not be called for skipped candidates.");
    }
}
