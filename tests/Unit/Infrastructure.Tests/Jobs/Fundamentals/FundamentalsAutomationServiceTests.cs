using Application.Catalog;
using Application.Fundamentals;
using Application.Jobs;
using Domain.Catalog;
using Domain.Fundamentals;
using Domain.Jobs;
using Infrastructure.Jobs.Fundamentals;
using Infrastructure.Persistence.Repositories.Fundamentals;
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Infrastructure.Tests.Jobs.Fundamentals;

public class FundamentalsAutomationServiceTests
{
    [Fact]
    public async Task ExecuteAsync_WhenListingsEmpty_ReturnsZeroCountsAndNoRecords()
    {
        await using var db = CreateDbContext();
        var fibra = SeedFibra(db, "FUNO11");
        var service = BuildService(
            db,
            new FakeAmefibraDiscoveryClient([], "", DateTimeOffset.UtcNow, []),
            [fibra]);

        var result = await service.ExecuteAsync(CancellationToken.None);

        Assert.Equal(0, result.FibrasScanned);
        Assert.Equal(0, result.ReportsDetected);
        Assert.Equal(0, result.NewReports);
        Assert.Equal(0, result.Errors);
        Assert.Empty(await db.FundamentalSourceManifests.ToListAsync());
        Assert.Empty(await db.FundamentalRecords.ToListAsync());
    }

    [Fact]
    public async Task ExecuteAsync_WhenListingIsNew_CreatesProcessedRecordAndManifest()
    {
        await using var db = CreateDbContext();
        var fibra = SeedFibra(db, "FUNO11");
        var service = BuildService(
            db,
            new FakeAmefibraDiscoveryClient(
                [new AmefibraListingItem("2022 Reporte T4 FUNO", "https://amefibra.com/download/funo-q4-2022/", "https://amefibra.com/download/funo-q4-2022/?wpdmdl=1&refresh=abc")],
                "https://amefibra.com/download/funo-q4-2022/",
                DateTimeOffset.Parse("2023-02-28T00:00:00Z"),
                ReadFixturePdf()),
            [fibra]);

        var result = await service.ExecuteAsync(CancellationToken.None);

        Assert.Equal(1, result.NewReports);
        Assert.Equal(1, result.RecordsProcessed);
        Assert.Equal(1, await db.FundamentalSourceManifests.CountAsync());
        var record = await db.FundamentalRecords.SingleAsync();
        Assert.Equal("processed", record.Status);
        Assert.Equal("api", record.ProcessingMode);
        Assert.False(record.IsPossibleUpdate);
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

        var service = BuildService(
            db,
            new FakeAmefibraDiscoveryClient(
                [new AmefibraListingItem("2022 Reporte T4 FUNO", "https://amefibra.com/download/funo-q4-2022/", "https://amefibra.com/download/funo-q4-2022/?wpdmdl=1&refresh=abc")],
                "https://amefibra.com/download/funo-q4-2022/",
                DateTimeOffset.Parse("2023-02-28T00:00:00Z"),
                ReadFixturePdf()),
            [fibra]);

        var result = await service.ExecuteAsync(CancellationToken.None);

        Assert.Equal(1, result.SkippedReports);
        Assert.Equal(0, result.RecordsProcessed);
        Assert.Empty(db.FundamentalRecords);
    }

    [Fact]
    public async Task ExecuteAsync_WhenSamePeriodHasDifferentPackage_CreatesPossibleUpdateAndSoftDeletesPreviousProcessed()
    {
        await using var db = CreateDbContext();
        var fibra = SeedFibra(db, "FUNO11");
        var oldRecordId = Guid.NewGuid();
        db.FundamentalRecords.Add(new FundamentalRecord
        {
            Id = oldRecordId,
            FibraId = fibra.Id,
            Period = "Q4-2022",
            Status = "processed",
            ProcessingMode = "manual",
            CapturedAt = DateTimeOffset.UtcNow.AddDays(-2),
            ConfirmedAt = DateTimeOffset.UtcNow.AddDays(-2),
        });
        db.FundamentalSourceManifests.Add(new FundamentalSourceManifest
        {
            Id = Guid.NewGuid(),
            SourceName = "AMEFIBRA",
            FibraId = fibra.Id,
            SourceTitle = "2022 Reporte T4 FUNO",
            Period = "Q4-2022",
            ReportType = "quarterly",
            DiscoveryStatus = "eligible",
            PackageUrl = "https://amefibra.com/download/funo-q4-2022-v1/",
            FirstSeenAt = DateTimeOffset.UtcNow.AddDays(-2),
            LastSeenAt = DateTimeOffset.UtcNow.AddDays(-2),
            LastDecision = "new",
            LastProcessedRecordId = oldRecordId,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-2),
            UpdatedAt = DateTimeOffset.UtcNow.AddDays(-2),
        });
        await db.SaveChangesAsync();

        var service = BuildService(
            db,
            new FakeAmefibraDiscoveryClient(
                [new AmefibraListingItem("2022 Reporte T4 FUNO", "https://amefibra.com/download/funo-q4-2022-v2/", "https://amefibra.com/download/funo-q4-2022-v2/?wpdmdl=2&refresh=def")],
                "https://amefibra.com/download/funo-q4-2022-v2/",
                DateTimeOffset.Parse("2023-03-01T00:00:00Z"),
                ReadFixturePdf()),
            [fibra]);

        var result = await service.ExecuteAsync(CancellationToken.None);

        Assert.Equal(1, result.RecordsProcessed);
        Assert.Equal(2, await db.FundamentalRecords.CountAsync());

        var records = await db.FundamentalRecords.OrderBy(x => x.CapturedAt).ToListAsync();
        Assert.NotNull(records[0].DeletedAt);
        Assert.Equal("processed", records[1].Status);
        Assert.True(records[1].IsPossibleUpdate);
        Assert.Equal(1, result.PossibleUpdates);
    }

    [Fact]
    public async Task ExecuteAsync_WhenFibrasPrologisAndPlusArePresent_FibraPlusTitleMatchesFibraPlus()
    {
        await using var db = CreateDbContext();

        var prologis = new Fibra
        {
            Id = Guid.NewGuid(),
            Ticker = "FIBRAPL14",
            YahooTicker = "FIBRAPL14.MX",
            FullName = "Fibra Prologis",
            ShortName = "Prologis",
            Currency = "MXN",
            Market = "BMV",
            Sector = "Industrial",
            State = FibraState.Active,
            NameVariants = ["Fibra Prologis", "Prologis", "FIBRAPL"],
            CreatedAt = DateTimeOffset.UtcNow,
        };
        var plus = new Fibra
        {
            Id = Guid.NewGuid(),
            Ticker = "FPLUS16",
            YahooTicker = "FPLUS16.MX",
            FullName = "Fibra Plus",
            ShortName = "Fibra Plus",
            Currency = "MXN",
            Market = "BMV",
            Sector = "Diversificado",
            State = FibraState.Active,
            NameVariants = ["Fibra Plus", "FPLUS", "FPLUS16"],
            CreatedAt = DateTimeOffset.UtcNow,
        };
        db.Fibras.AddRange(prologis, plus);
        await db.SaveChangesAsync();

        // Prologis seeded first — reproduces the original bug where Prologis stole Plus matches
        var service = BuildService(
            db,
            new FakeAmefibraDiscoveryClient(
                [new AmefibraListingItem("Fibra Plus Reporte Trimestral T4 2023", "https://amefibra.com/fibra-plus-q4-2023/", "https://amefibra.com/fibra-plus-q4-2023/?wpdmdl=99")],
                "https://amefibra.com/fibra-plus-q4-2023/",
                DateTimeOffset.Parse("2024-02-15T00:00:00Z"),
                ReadFixturePdf()),
            [prologis, plus]);

        await service.ExecuteAsync(CancellationToken.None);

        var record = await db.FundamentalRecords.SingleAsync();
        Assert.Equal(plus.Id, record.FibraId);
    }

    private static FundamentalsAutomationService BuildService(
        AppDbContext db,
        IAmefibraDiscoveryClient discoveryClient,
        IReadOnlyList<Fibra> fibras)
        => new(
            discoveryClient,
            new FakeFundamentalsFibraRepository(fibras),
            new FundamentalRepository(db),
            new FundamentalSourceManifestRepository(db),
            new FakeKpiExtractorService(),
            new FakePipelineErrorLogRepository(),
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Uploads:BasePath"] = Path.Combine(Path.GetTempPath(), $"fibradis-tests-{Guid.NewGuid():N}")
            }).Build(),
            NullLogger<FundamentalsAutomationService>.Instance);

    private static AppDbContext CreateDbContext()
        => new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options);

    private static Fibra SeedFibra(AppDbContext db, string ticker)
    {
        var fibra = new Fibra
        {
            Id = Guid.NewGuid(),
            Ticker = ticker,
            YahooTicker = $"{ticker}.MX",
            FullName = "Fibra Uno",
            ShortName = "FUNO",
            Currency = "MXN",
            Market = "BMV",
            Sector = "Diversificado",
            State = FibraState.Active,
            NameVariants = ["Fibra Uno", "FUNO"],
            CreatedAt = DateTimeOffset.UtcNow,
        };
        db.Fibras.Add(fibra);
        db.SaveChanges();
        return fibra;
    }

    private static byte[] ReadFixturePdf()
        => File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Fixtures", "amefibra-sample.pdf"));
}

internal sealed class FakeAmefibraDiscoveryClient(
    IReadOnlyList<AmefibraListingItem> listings,
    string packageUrl,
    DateTimeOffset publishedAt,
    byte[] pdfContent) : IAmefibraDiscoveryClient
{
    public Task<IReadOnlyList<AmefibraListingItem>> GetListingItemsAsync(CancellationToken ct)
        => Task.FromResult(listings);

    public Task<AmefibraPackageDetails> GetPackageDetailsAsync(string requestedPackageUrl, CancellationToken ct)
        => Task.FromResult(new AmefibraPackageDetails(
            requestedPackageUrl,
            listings.First(x => x.PackageUrl == requestedPackageUrl).DownloadUrl,
            requestedPackageUrl == packageUrl ? publishedAt : null));

    public Task<(byte[] Content, string? PdfUrl, string? FileName)> DownloadPdfAsync(string requestedPackageUrl, string downloadUrl, CancellationToken ct)
        => Task.FromResult<(byte[] Content, string? PdfUrl, string? FileName)>((pdfContent, $"{requestedPackageUrl.TrimEnd('/')}.pdf", "report.pdf"));
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
