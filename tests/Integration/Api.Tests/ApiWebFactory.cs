using Domain.Auth;
using Domain.Catalog;
using Domain.Fundamentals;
using Domain.Market;
using Domain.News;
using Infrastructure.Persistence.SqlServer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure; // IDbContextOptionsConfiguration
using Microsoft.EntityFrameworkCore.Storage; // InMemoryDatabaseRoot
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Api.Tests;

public class ApiWebFactory : WebApplicationFactory<Program>
{
    // Explicit root per-instance: aísla completamente el store en memoria
    // aunque EF Core InMemory use cachés estáticas por nombre.
    private readonly InMemoryDatabaseRoot _dbRoot = new();
    private readonly string _databaseName = $"ApiTests-{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
        });

        builder.ConfigureAppConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "test-secret-key-must-be-at-least-32-chars-long!!!",
                ["Jwt:Issuer"] = "fibradis",
                ["Jwt:Audience"] = "fibradis-client",
                ["Jwt:AccessTokenMinutes"] = "15",
                ["Hangfire:UseInMemoryStorage"] = "true",
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remover DbContextOptions, AppDbContext e IDbContextOptionsConfiguration<AppDbContext>
            // para evitar que EF Core aplique tanto la configuración de SqlServer como la de InMemory
            var optionsConfigType = typeof(IDbContextOptionsConfiguration<AppDbContext>);
            var toRemove = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>)
                         || d.ServiceType == typeof(AppDbContext)
                         || d.ServiceType == optionsConfigType)
                .ToList();
            foreach (var d in toRemove) services.Remove(d);

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName, _dbRoot));
        });
    }

    public async Task SeedUsersAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var encryptor = scope.ServiceProvider.GetRequiredService<Application.Auth.IEmailEncryptor>();
        await db.Database.EnsureCreatedAsync();

        if (!await db.Users.AnyAsync())
        {
            db.Users.AddRange(
                new User
                {
                    Id = Guid.Parse("11111111-0000-0000-0000-000000000001"),
                    Email = encryptor.Encrypt("user@test.com"),
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                    Role = UserRole.User,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                },
                new User
                {
                    Id = Guid.Parse("22222222-0000-0000-0000-000000000001"),
                    Email = encryptor.Encrypt("adminops@test.com"),
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("ops123"),
                    Role = UserRole.AdminOps,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                });
            await db.SaveChangesAsync();
        }
    }

    public static readonly Guid TestNewsArticleId = Guid.Parse("dddddddd-0000-0000-0000-000000000001");

    public async Task SeedNewsAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        if (!await db.NewsArticles.AnyAsync(n => n.Id == TestNewsArticleId))
        {
            db.NewsArticles.Add(new NewsArticle
            {
                Id = TestNewsArticleId,
                Title = "FUNO reporta crecimiento en NOI",
                TitleNormalized = "funo reporta crecimiento en noi",
                Source = "Test Source",
                PublishedAt = DateTimeOffset.UtcNow,
                Url = "https://example.com/noticia-ops-test",
                BodyText = "Cuerpo de texto de prueba para edición manual en Ops.",
                Status = NewsArticleStatus.Pending,
                CapturedAt = DateTimeOffset.UtcNow,
            });
            await db.SaveChangesAsync();
        }
    }

    public async Task SeedCatalogAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        // HasData() siembra las FIBRAs activas de producción via EnsureCreatedAsync.
        // Solo agregamos la FIBRA inactiva de test que no está en HasData.
        if (!await db.Fibras.AnyAsync(f => f.Ticker == "INACTIVA1"))
        {
            db.Fibras.Add(new Fibra
            {
                Id = Guid.Parse("cccccccc-0000-0000-0000-000000000001"),
                Ticker = "INACTIVA1",
                YahooTicker = "INACTIVA1.MX",
                FullName = "Fibra Inactiva Test",
                ShortName = "Inactiva",
                Sector = "Diversificado",
                Market = "BMV",
                Currency = "MXN",
                State = FibraState.Inactive,
                NameVariants = [],
                CreatedAt = DateTimeOffset.UtcNow,
            });
            await db.SaveChangesAsync();
        }
    }

    public async Task SeedMarketAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        // Obtener la fibra FUNO11 seeded por HasData para usar su ID real
        var funo = await db.Fibras.FirstOrDefaultAsync(f => f.Ticker == "FUNO11");
        if (funo is null) return;

        if (!await db.PriceSnapshots.AnyAsync(p => p.FibraId == funo.Id))
        {
            db.PriceSnapshots.Add(new PriceSnapshot
            {
                Id = Guid.NewGuid(),
                FibraId = funo.Id,
                Ticker = "FUNO11",
                LastPrice = 24.50m,
                DailyChange = 0.15m,
                DailyChangePct = 0.62m,
                Volume = 1_234_567L,
                Week52High = 28.10m,
                Week52Low = 20.80m,
                CapturedAt = DateTimeOffset.UtcNow - TimeSpan.FromMinutes(5),
                Status = MarketDataStatus.Processed,
            });
        }

        if (!await db.DailySnapshots.AnyAsync(d => d.FibraId == funo.Id))
        {
            // Datos en múltiples puntos temporales para poder distinguir períodos en tests:
            //   5 días  → dentro de 1m, 3m, 6m, 1y
            //  20 días  → dentro de 1m, 3m, 6m, 1y
            //  50 días  → fuera de 1m, dentro de 3m, 6m, 1y
            // 110 días  → fuera de 1m y 3m, dentro de 6m, 1y
            // 220 días  → fuera de 1m, 3m y 6m, dentro de 1y
            // 400 días  → fuera de todos los períodos
            var offsets = new[] { 5, 20, 50, 110, 220, 400 };
            foreach (var daysAgo in offsets)
            {
                db.DailySnapshots.Add(new DailySnapshot
                {
                    Id = Guid.NewGuid(),
                    FibraId = funo.Id,
                    Ticker = "FUNO11",
                    Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-daysAgo)),
                    Open = 24.00m,
                    High = 24.80m,
                    Low = 23.90m,
                    Close = 24.50m,
                    Volume = 1_000_000L,
                });
            }
        }

        await db.SaveChangesAsync();
    }

    public async Task SeedCompareAsync()
    {
        await SeedMarketAsync();

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        var funo = await db.Fibras.FirstAsync(f => f.Ticker == "FUNO11");
        var fmty = await db.Fibras.FirstAsync(f => f.Ticker == "FMTY14");
        var terra = await db.Fibras.FirstAsync(f => f.Ticker == "TERRA13");

        if (!await db.PriceSnapshots.AnyAsync(p => p.FibraId == fmty.Id))
        {
            db.PriceSnapshots.Add(new PriceSnapshot
            {
                Id = Guid.NewGuid(),
                FibraId = fmty.Id,
                Ticker = "FMTY14",
                LastPrice = 16.20m,
                DailyChange = -0.08m,
                DailyChangePct = -0.49m,
                Volume = 2_345_678L,
                Week52High = 17.80m,
                Week52Low = 15.10m,
                CapturedAt = DateTimeOffset.UtcNow - TimeSpan.FromMinutes(8),
                Status = MarketDataStatus.Processed,
            });
        }

        if (!await db.DailySnapshots.AnyAsync(d => d.FibraId == fmty.Id))
        {
            foreach (var daysAgo in new[] { 12, 62, 188 })
            {
                db.DailySnapshots.Add(new DailySnapshot
                {
                    Id = Guid.NewGuid(),
                    FibraId = fmty.Id,
                    Ticker = "FMTY14",
                    Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-daysAgo)),
                    Open = 15.90m,
                    High = 16.30m,
                    Low = 15.70m,
                    Close = daysAgo == 12 ? 16.20m : daysAgo == 62 ? 16.00m : 15.80m,
                    Volume = 1_500_000L + daysAgo * 1000L,
                });
            }
        }

        if (!await db.Distributions.AnyAsync(d => d.FibraId == fmty.Id))
        {
            foreach (var (date, amount) in new[]
            {
                (new DateOnly(2025, 3, 17), 0.34m),
                (new DateOnly(2025, 6, 16), 0.35m),
                (new DateOnly(2025, 9, 15), 0.36m),
                (new DateOnly(2025, 12, 15), 0.37m),
            })
            {
                db.Distributions.Add(new Distribution
                {
                    Id = Guid.NewGuid(),
                    FibraId = fmty.Id,
                    Ticker = "FMTY14",
                    PaymentDate = date,
                    AmountPerUnit = amount,
                    Currency = "MXN",
                    Source = "compare-seed",
                    CapturedAt = DateTimeOffset.UtcNow,
                });
            }
        }

        if (!await db.FundamentalRecords.AnyAsync(r => r.FibraId == funo.Id && r.Period == "Q4-2025"))
        {
            db.FundamentalRecords.Add(new FundamentalRecord
            {
                Id = Guid.NewGuid(),
                FibraId = funo.Id,
                Period = "Q4-2025",
                Status = "processed",
                ProcessingMode = "manual",
                CapRate = 0.071m,
                NavPerCbfi = 25.40m,
                Ltv = 0.31m,
                NoiMargin = 0.72m,
                FfoMargin = 0.64m,
                QuarterlyDistribution = 0.38m,
                Summary = "FUNO11 con datos completos para comparador.",
                CapturedAt = DateTimeOffset.UtcNow.AddMinutes(-3),
                ConfirmedAt = DateTimeOffset.UtcNow.AddMinutes(-1),
            });
        }

        if (!await db.FundamentalRecords.AnyAsync(r => r.FibraId == fmty.Id && r.Period == "Q4-2025"))
        {
            db.FundamentalRecords.Add(new FundamentalRecord
            {
                Id = Guid.NewGuid(),
                FibraId = fmty.Id,
                Period = "Q4-2025",
                Status = "processed",
                ProcessingMode = "manual",
                CapRate = 0.061m,
                NavPerCbfi = null,
                Ltv = null,
                NoiMargin = null,
                FfoMargin = null,
                QuarterlyDistribution = 0.36m,
                Summary = "FMTY14 con datos parciales para el comparador.",
                CapturedAt = DateTimeOffset.UtcNow.AddMinutes(-2),
                ConfirmedAt = DateTimeOffset.UtcNow,
            });
        }

        if (!await db.FundamentalRecords.AnyAsync(r => r.FibraId == terra.Id && r.Period == "Q4-2025"))
        {
            db.FundamentalRecords.Add(new FundamentalRecord
            {
                Id = Guid.NewGuid(),
                FibraId = terra.Id,
                Period = "Q4-2025",
                Status = "processed",
                ProcessingMode = "manual",
                CapRate = 0.065m,
                NavPerCbfi = 18.90m,
                Ltv = 0.28m,
                NoiMargin = 0.69m,
                FfoMargin = 0.61m,
                QuarterlyDistribution = 0.18m,
                Summary = "TERRA13 sin precio para validar exclusión de score.",
                CapturedAt = DateTimeOffset.UtcNow.AddMinutes(-4),
                ConfirmedAt = DateTimeOffset.UtcNow.AddMinutes(-2),
            });
        }

        await db.SaveChangesAsync();
    }
}
