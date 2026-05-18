# Historia 2.1: Catálogo Maestro de FIBRAs con Datos Semilla Iniciales

Status: done

## Historia

Como visitante público,
quiero ver un catálogo de FIBRAs con su ticker, nombre completo, sector, mercado, moneda, estado y URLs oficiales,
para que pueda encontrar e identificar cualquier FIBRA activa en la plataforma.

## Criterios de Aceptación

**CA-1: Listado paginado de FIBRAs activas**
Dado que el sistema está sembrado con al menos 10 FIBRAs activas,
Cuando hago `GET /api/v1/fibras`,
Entonces recibo `200 OK` con `{ items, page, pageSize, total }` donde cada item contiene `ticker`, `fullName`, `shortName`, `sector`, `market`, `currency`, `state` y `siteUrl`.

**CA-2: Detalle completo de una FIBRA por ticker**
Dado que hago `GET /api/v1/fibras/FUNO11`,
Entonces recibo `200 OK` con los metadatos completos de FUNO11, incluyendo todas las URLs (`siteUrl`, `investorUrl`, `reportsUrl`) y la lista de `nameVariants` para queries RSS.

**CA-3: FIBRAs inactivas excluidas del universo activo**
Dado que una FIBRA tiene `state=Inactive`,
Entonces queda excluida de `GET /api/v1/fibras` (universo activo),
Pero sus metadatos siguen siendo accesibles vía `GET /api/v1/fibras/{ticker}`.

**CA-4: Esquema `catalog` con tablas y columnas correctas**
Dado que el esquema del módulo de catálogo está en su lugar,
Entonces existen tablas en el esquema `catalog` (columnas: `id`, `ticker`, `full_name`, `short_name`, `sector`, `market`, `currency`, `state`, `site_url`, `investor_url`, `reports_url`, `name_variants`, `created_at`) sin referencias a esquemas de otros módulos.

**CA-5: Ticker inexistente devuelve 404**
Dado que hago `GET /api/v1/fibras/FAKE99`,
Entonces recibo `404 Not Found` con cuerpo `ProblemDetails` con `domainCode` y `correlationId`.

## Tareas / Subtareas

- [x] Task 1: Entidad de Dominio (AC: CA-4)
  - [x] Crear `src/Server/Domain/Catalog/FibraState.cs` — enum `Active` / `Inactive`
  - [x] Crear `src/Server/Domain/Catalog/Fibra.cs` — entidad con todas las propiedades

- [x] Task 2: Capa Application — contratos (AC: CA-1, CA-2, CA-3)
  - [x] Crear `src/Server/Application/Catalog/IFibraRepository.cs` — interfaz con `GetActivePagedAsync` y `GetByTickerAsync`
  - [x] Crear `src/Server/Application/Catalog/FibraFilter.cs` — parámetros de paginación/filtro

- [x] Task 3: SharedApiContracts — DTOs (AC: CA-1, CA-2)
  - [x] Crear `src/Server/SharedApiContracts/Common/PagedResult.cs` — wrapper genérico `{ items, page, pageSize, total }`
  - [x] Crear `src/Server/SharedApiContracts/Catalog/FibraListItem.cs` — DTO de lista
  - [x] Crear `src/Server/SharedApiContracts/Catalog/FibraDetail.cs` — DTO de detalle completo

- [x] Task 4: Persistencia — configuración EF Core y datos semilla (AC: CA-4)
  - [x] Crear `src/Server/Infrastructure/Persistence/SqlServer/Configurations/Catalog/FibraConfiguration.cs`
  - [x] Crear `src/Server/Infrastructure/Persistence/Seed/CatalogSeed.cs` — 10+ FIBRAs reales
  - [x] Actualizar `AppDbContext.cs` — agregar `DbSet<Fibra>` y llamar a `CatalogSeed.Seed()`
  - [x] Crear migración EF Core: `dotnet ef migrations add AddCatalogSchema -p src/Server/Infrastructure -s src/Server/Api`

- [x] Task 5: Implementación del repositorio (AC: CA-1, CA-2, CA-3)
  - [x] Crear `src/Server/Infrastructure/Persistence/Repositories/Catalog/FibraRepository.cs`
  - [x] Registrar `IFibraRepository → FibraRepository` en `ApiServiceExtensions.cs`

- [x] Task 6: Endpoint HTTP (AC: CA-1, CA-2, CA-3, CA-5)
  - [x] Crear `src/Server/Api/Endpoints/Public/CatalogEndpoints.cs` — `MapCatalog()` con dos rutas
  - [x] Actualizar `Program.cs` — agregar `app.MapCatalog()`

- [x] Task 7: Tests de integración (AC: CA-1, CA-2, CA-3, CA-5)
  - [x] Actualizar `ApiWebFactory.cs` — agregar `SeedCatalogAsync()` con FIBRAs de prueba
  - [x] Crear `tests/Integration/Api.Tests/CatalogEndpointTests.cs` — tests para los 5 CAs

- [x] Task 8: Validación final
  - [x] `dotnet build FIBRADIS.slnx` — exit code 0, 0 warnings
  - [x] `dotnet test tests/Integration/Api.Tests/` — todos los tests pasan (sin regresiones)
  - [x] `curl http://localhost:5265/api/v1/fibras` → JSON paginado con 10+ FIBRAs
  - [x] `curl http://localhost:5265/api/v1/fibras/FUNO11` → detalle completo con nameVariants

---

## Dev Notes

### Stack exacto — NO negociar versiones

| Componente | Versión |
|---|---|
| .NET | 10 LTS (ya fijado) |
| EF Core | 10.0.8 (ya en CPM — no cambiar) |
| SQL Server schema | `catalog` (minúscula singular) |
| Tabla | `Fibra` (PascalCase singular) |

### Regla obligatoria: Central Package Management (CPM)

Esta historia NO requiere nuevos paquetes NuGet. No agregar nada en `Directory.Packages.props`. Todos los paquetes necesarios (`Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.EntityFrameworkCore.Design`) ya existen.

### Task 1: Entidad de Dominio

```csharp
// src/Server/Domain/Catalog/FibraState.cs — NUEVO
namespace Domain.Catalog;

public enum FibraState { Active, Inactive }
```

```csharp
// src/Server/Domain/Catalog/Fibra.cs — NUEVO
namespace Domain.Catalog;

public class Fibra
{
    public Guid Id { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public string Sector { get; set; } = string.Empty;
    public string Market { get; set; } = string.Empty;     // e.g. "BMV"
    public string Currency { get; set; } = string.Empty;   // e.g. "MXN"
    public FibraState State { get; set; }
    public string? SiteUrl { get; set; }
    public string? InvestorUrl { get; set; }
    public string? ReportsUrl { get; set; }
    public List<string> NameVariants { get; set; } = [];   // para queries RSS
    public DateTime CreatedAt { get; set; }
}
```

### Task 2: Capa Application

```csharp
// src/Server/Application/Catalog/FibraFilter.cs — NUEVO
namespace Application.Catalog;

public record FibraFilter(int Page = 1, int PageSize = 20);
```

```csharp
// src/Server/Application/Catalog/IFibraRepository.cs — NUEVO
namespace Application.Catalog;

public interface IFibraRepository
{
    Task<(IReadOnlyList<Domain.Catalog.Fibra> Items, int Total)> GetActivePagedAsync(
        FibraFilter filter, CancellationToken ct = default);

    Task<Domain.Catalog.Fibra?> GetByTickerAsync(string ticker, CancellationToken ct = default);
}
```

### Task 3: SharedApiContracts — DTOs

```csharp
// src/Server/SharedApiContracts/Common/PagedResult.cs — NUEVO
namespace SharedApiContracts.Common;

public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int Total);
```

```csharp
// src/Server/SharedApiContracts/Catalog/FibraListItem.cs — NUEVO
namespace SharedApiContracts.Catalog;

public record FibraListItem(
    Guid Id,
    string Ticker,
    string FullName,
    string ShortName,
    string Sector,
    string Market,
    string Currency,
    string State,
    string? SiteUrl);
```

```csharp
// src/Server/SharedApiContracts/Catalog/FibraDetail.cs — NUEVO
namespace SharedApiContracts.Catalog;

public record FibraDetail(
    Guid Id,
    string Ticker,
    string FullName,
    string ShortName,
    string Sector,
    string Market,
    string Currency,
    string State,
    string? SiteUrl,
    string? InvestorUrl,
    string? ReportsUrl,
    IReadOnlyList<string> NameVariants,
    DateTime CreatedAt);
```

### Task 4: Configuración EF Core

```csharp
// src/Server/Infrastructure/Persistence/SqlServer/Configurations/Catalog/FibraConfiguration.cs — NUEVO
using Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace Infrastructure.Persistence.SqlServer.Configurations.Catalog;

public class FibraConfiguration : IEntityTypeConfiguration<Fibra>
{
    private static readonly JsonSerializerOptions _jsonOpts = new();

    public void Configure(EntityTypeBuilder<Fibra> builder)
    {
        builder.ToTable("Fibra", schema: "catalog");
        builder.HasKey(f => f.Id);
        builder.HasIndex(f => f.Ticker).IsUnique().HasDatabaseName("UX_Fibra_Ticker");

        builder.Property(f => f.Ticker).HasMaxLength(20).IsRequired().HasColumnName("ticker");
        builder.Property(f => f.FullName).HasMaxLength(256).IsRequired().HasColumnName("full_name");
        builder.Property(f => f.ShortName).HasMaxLength(64).IsRequired().HasColumnName("short_name");
        builder.Property(f => f.Sector).HasMaxLength(64).IsRequired().HasColumnName("sector");
        builder.Property(f => f.Market).HasMaxLength(32).IsRequired().HasColumnName("market");
        builder.Property(f => f.Currency).HasMaxLength(8).IsRequired().HasColumnName("currency");
        builder.Property(f => f.State).HasConversion<string>().HasMaxLength(16).HasColumnName("state");
        builder.Property(f => f.SiteUrl).HasMaxLength(512).HasColumnName("site_url");
        builder.Property(f => f.InvestorUrl).HasMaxLength(512).HasColumnName("investor_url");
        builder.Property(f => f.ReportsUrl).HasMaxLength(512).HasColumnName("reports_url");
        builder.Property(f => f.CreatedAt).HasColumnName("created_at");

        // name_variants almacenado como JSON (nvarchar(max)) — editable desde Ops (Historia 5.3)
        builder.Property(f => f.NameVariants)
            .HasConversion(
                v => JsonSerializer.Serialize(v, _jsonOpts),
                v => JsonSerializer.Deserialize<List<string>>(v, _jsonOpts) ?? new())
            .HasColumnType("nvarchar(max)")
            .HasColumnName("name_variants");
    }
}
```

**Importante:** EF Core 10 no infiere el nombre de columna para listas serializadas. El `HasColumnName("name_variants")` es necesario para respetar la convención snake_case del proyecto.

### Task 4: Datos semilla — `CatalogSeed.cs`

```csharp
// src/Server/Infrastructure/Persistence/Seed/CatalogSeed.cs — NUEVO
using Domain.Catalog;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Seed;

public static class CatalogSeed
{
    public static void Seed(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Fibra>().HasData(
            F("FUNO11",   "Fibra Uno",                         "Fibra Uno",   "Diversificado",  "BMV", "MXN", "https://fibra.uno",             "https://fibra.uno/inversionistas",    null, ["Fibra Uno", "FUNO"]),
            F("DANHOS13", "Fibra Danhos",                       "Danhos",      "Comercial",      "BMV", "MXN", "https://fibradanhos.com.mx",     "https://fibradanhos.com.mx/ri",       null, ["Danhos", "DANHOS"]),
            F("TERRA13",  "Fibra Terra",                        "Terra",       "Industrial",     "BMV", "MXN", "https://fibra-terra.com",        null,                                  null, ["Fibra Terra", "TERRA"]),
            F("FIBRAMQ12","Fibra Macquarie",                    "FibraMQ",     "Industrial",     "BMV", "MXN", "https://fibramacquarie.com.mx",  "https://fibramacquarie.com.mx/ri",    null, ["Fibra MQ", "Macquarie", "FIBRAMQ"]),
            F("FMTY14",   "Fibra Monterrey",                    "Fibra MTY",   "Industrial",     "BMV", "MXN", "https://fibramty.com",           "https://fibramty.com/inversionistas", null, ["Fibra Monterrey", "FibraMTY", "FMTY"]),
            F("FINN13",   "Fibra Inn",                          "Fibra Inn",   "Hotelero",       "BMV", "MXN", "https://fibrainn.com.mx",        null,                                  null, ["Fibra Inn", "FINN"]),
            F("FIHO12",   "Fibra Hotel",                        "Fibra Hotel", "Hotelero",       "BMV", "MXN", "https://fibrahotel.com",         null,                                  null, ["Fibra Hotel", "FIHO"]),
            F("VESTA15",  "Fibra Vesta",                        "Vesta",       "Industrial",     "BMV", "MXN", "https://fibravesta.com",         "https://fibravesta.com/ri",           null, ["Fibra Vesta", "VESTA"]),
            F("HCITY17",  "Fibra Hotel City Express",           "HC",          "Hotelero",       "BMV", "MXN", "https://hcity.com.mx",           null,                                  null, ["Hotel City Express", "HCITY", "HC"]),
            F("PLUS18",   "Fibra Plus",                         "Fibra Plus",  "Diversificado",  "BMV", "MXN", "https://fibraplus.mx",           null,                                  null, ["Fibra Plus", "PLUS"])
        );
    }

    private static Fibra F(
        string ticker, string fullName, string shortName,
        string sector, string market, string currency,
        string? siteUrl, string? investorUrl, string? reportsUrl,
        List<string> nameVariants)
        => new()
        {
            Id = GuidFromTicker(ticker),
            Ticker = ticker,
            FullName = fullName,
            ShortName = shortName,
            Sector = sector,
            Market = market,
            Currency = currency,
            State = FibraState.Active,
            SiteUrl = siteUrl,
            InvestorUrl = investorUrl,
            ReportsUrl = reportsUrl,
            NameVariants = nameVariants,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        };

    // Genera GUIDs deterministas para que las migraciones sean idempotentes
    private static Guid GuidFromTicker(string ticker)
        => new(0, 0, 0, 0, 0, 0, 0, 0,
               (byte)(ticker.Length > 0 ? ticker[0] : 0),
               (byte)(ticker.Length > 1 ? ticker[1] : 0),
               (byte)(ticker.Length > 2 ? ticker[2] : 0));
}
```

**CRÍTICO:** `HasData()` usa GUIDs fijos y deterministas para que la migración sea idempotente. El helper `GuidFromTicker` genera un GUID único pero reproducible por ticker. **NO usar `Guid.NewGuid()`** en `HasData` porque genera migraciones no deterministas que cambian en cada compilación.

### Task 4: Actualizar `AppDbContext.cs`

```csharp
// src/Server/Infrastructure/Persistence/SqlServer/AppDbContext.cs — MODIFICAR
using Domain.Auth;
using Domain.Catalog;              // ← AGREGAR
using Infrastructure.Persistence.Seed;  // ← AGREGAR
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.SqlServer;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Fibra> Fibras => Set<Fibra>();    // ← AGREGAR

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        CatalogSeed.Seed(modelBuilder);             // ← AGREGAR
    }
}
```

### Task 4: Migración EF Core

Ejecutar desde la raíz del repositorio:
```bash
dotnet ef migrations add AddCatalogSchema -p src/Server/Infrastructure -s src/Server/Api --context AppDbContext
```

La migración debe:
- Crear schema `catalog` si no existe
- Crear tabla `catalog.Fibra` con todas las columnas (snake_case) e índice `UX_Fibra_Ticker`
- Insertar los 10 registros de datos semilla vía `InsertData`

**NO modificar la migración a mano** — EF Core la genera completa. Si hay un error de generación, verificar que `FibraConfiguration` esté en el mismo assembly que `AppDbContext`.

### Task 5: Repositorio en Infrastructure

```csharp
// src/Server/Infrastructure/Persistence/Repositories/Catalog/FibraRepository.cs — NUEVO
using Application.Catalog;
using Domain.Catalog;
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories.Catalog;

public class FibraRepository(AppDbContext db) : IFibraRepository
{
    public async Task<(IReadOnlyList<Fibra> Items, int Total)> GetActivePagedAsync(
        FibraFilter filter, CancellationToken ct = default)
    {
        var query = db.Fibras
            .Where(f => f.State == FibraState.Active)
            .OrderBy(f => f.Ticker);

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<Fibra?> GetByTickerAsync(string ticker, CancellationToken ct = default)
        => await db.Fibras
            .FirstOrDefaultAsync(f => f.Ticker == ticker.ToUpper(), ct);
}
```

Registrar en `ApiServiceExtensions.cs` — agregar al bloque de servicios existente:
```csharp
// En ApiServiceExtensions.cs — agregar DESPUÉS de los servicios de Auth
builder.Services.AddScoped<IFibraRepository, FibraRepository>();
```

Usings a agregar:
```csharp
using Application.Catalog;
using Infrastructure.Persistence.Repositories.Catalog;
```

### Task 6: Endpoint HTTP

```csharp
// src/Server/Api/Endpoints/Public/CatalogEndpoints.cs — NUEVO
using Application.Catalog;
using SharedApiContracts.Catalog;
using SharedApiContracts.Common;

namespace Api.Endpoints.Public;

public static class CatalogEndpoints
{
    public static IEndpointRouteBuilder MapCatalog(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/fibras").WithTags("Catalog");

        group.MapGet("/", async (
            int page,
            int pageSize,
            IFibraRepository repo,
            CancellationToken ct) =>
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

            var (items, total) = await repo.GetActivePagedAsync(new FibraFilter(page, pageSize), ct);
            var dtos = items.Select(f => new FibraListItem(
                f.Id, f.Ticker, f.FullName, f.ShortName,
                f.Sector, f.Market, f.Currency, f.State.ToString(), f.SiteUrl)).ToList();

            return Results.Ok(new PagedResult<FibraListItem>(dtos, page, pageSize, total));
        })
        .AllowAnonymous()
        .Produces<PagedResult<FibraListItem>>(StatusCodes.Status200OK);

        group.MapGet("/{ticker}", async (
            string ticker,
            IFibraRepository repo,
            CancellationToken ct) =>
        {
            var fibra = await repo.GetByTickerAsync(ticker, ct);
            if (fibra is null)
                return Results.Problem(
                    title: "FIBRA no encontrada",
                    detail: $"No existe una FIBRA con ticker '{ticker}'.",
                    statusCode: StatusCodes.Status404NotFound,
                    extensions: new Dictionary<string, object?> { ["domainCode"] = "FIBRA_NOT_FOUND" });

            return Results.Ok(new FibraDetail(
                fibra.Id, fibra.Ticker, fibra.FullName, fibra.ShortName,
                fibra.Sector, fibra.Market, fibra.Currency, fibra.State.ToString(),
                fibra.SiteUrl, fibra.InvestorUrl, fibra.ReportsUrl,
                fibra.NameVariants.AsReadOnly(), fibra.CreatedAt));
        })
        .AllowAnonymous()
        .Produces<FibraDetail>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }
}
```

**Actualizar `Program.cs`** — agregar una línea:
```csharp
app.MapCatalog();   // ← AGREGAR después de app.MapOpsPing()
```

**Nota sobre query params con valores default:** Los parámetros `page` e `pageSize` se reciben como query string. ASP.NET Core los enlaza automáticamente desde `?page=1&pageSize=20`. Si no se proporcionan, toman el valor default del tipo (`int` = 0), por eso se normalizan en el handler.

### Task 7: Tests de integración

**Actualizar `ApiWebFactory.cs`** — agregar método de seeding de catálogo:

```csharp
// Agregar en ApiWebFactory.cs — DESPUÉS de SeedUsersAsync()
public async Task SeedCatalogAsync()
{
    using var scope = Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();

    if (!await db.Fibras.AnyAsync())
    {
        db.Fibras.AddRange(
            new Fibra
            {
                Id = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001"),
                Ticker = "FUNO11",
                FullName = "Fibra Uno",
                ShortName = "Fibra Uno",
                Sector = "Diversificado",
                Market = "BMV",
                Currency = "MXN",
                State = FibraState.Active,
                SiteUrl = "https://fibra.uno",
                NameVariants = ["Fibra Uno", "FUNO"],
                CreatedAt = DateTime.UtcNow,
            },
            new Fibra
            {
                Id = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000001"),
                Ticker = "DANHOS13",
                FullName = "Fibra Danhos",
                ShortName = "Danhos",
                Sector = "Comercial",
                Market = "BMV",
                Currency = "MXN",
                State = FibraState.Active,
                SiteUrl = "https://fibradanhos.com.mx",
                NameVariants = ["Danhos", "DANHOS"],
                CreatedAt = DateTime.UtcNow,
            },
            new Fibra
            {
                Id = Guid.Parse("cccccccc-0000-0000-0000-000000000001"),
                Ticker = "INACTIVA1",
                FullName = "Fibra Inactiva Test",
                ShortName = "Inactiva",
                Sector = "Diversificado",
                Market = "BMV",
                Currency = "MXN",
                State = FibraState.Inactive,
                NameVariants = [],
                CreatedAt = DateTime.UtcNow,
            });
        await db.SaveChangesAsync();
    }
}
```

**Usings a agregar en `ApiWebFactory.cs`:**
```csharp
using Domain.Catalog;
```

**Crear `tests/Integration/Api.Tests/CatalogEndpointTests.cs` — NUEVO:**

```csharp
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Domain.Catalog;

namespace Api.Tests;

public class CatalogEndpointTests(ApiWebFactory factory)
    : IClassFixture<ApiWebFactory>
{
    private readonly HttpClient _client = factory.CreateClient();
    private static bool _seeded;
    private static readonly SemaphoreSlim _lock = new(1, 1);

    private async Task EnsureSeededAsync()
    {
        await _lock.WaitAsync();
        try
        {
            if (!_seeded)
            {
                await factory.SeedCatalogAsync();
                _seeded = true;
            }
        }
        finally { _lock.Release(); }
    }

    [Fact]
    public async Task GetFibras_ReturnsOk_WithPagedResult()
    {
        await EnsureSeededAsync();
        var response = await _client.GetAsync("/api/v1/fibras");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("items", out var items));
        Assert.True(root.TryGetProperty("page", out _));
        Assert.True(root.TryGetProperty("pageSize", out _));
        Assert.True(root.TryGetProperty("total", out _));
        Assert.Equal(JsonValueKind.Array, items.ValueKind);
    }

    [Fact]
    public async Task GetFibras_ExcludesInactiveFibras()
    {
        await EnsureSeededAsync();
        var response = await _client.GetAsync("/api/v1/fibras");
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var items = doc.RootElement.GetProperty("items").EnumerateArray();

        Assert.DoesNotContain(items, item =>
            item.GetProperty("ticker").GetString() == "INACTIVA1");
    }

    [Fact]
    public async Task GetFibras_EachItemHasRequiredFields()
    {
        await EnsureSeededAsync();
        var response = await _client.GetAsync("/api/v1/fibras");
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var firstItem = doc.RootElement.GetProperty("items").EnumerateArray().First();

        Assert.True(firstItem.TryGetProperty("ticker", out _));
        Assert.True(firstItem.TryGetProperty("fullName", out _));
        Assert.True(firstItem.TryGetProperty("shortName", out _));
        Assert.True(firstItem.TryGetProperty("sector", out _));
        Assert.True(firstItem.TryGetProperty("market", out _));
        Assert.True(firstItem.TryGetProperty("currency", out _));
        Assert.True(firstItem.TryGetProperty("state", out _));
    }

    [Fact]
    public async Task GetFibraByTicker_ReturnsOk_WithFullDetail()
    {
        await EnsureSeededAsync();
        var response = await _client.GetAsync("/api/v1/fibras/FUNO11");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        Assert.Equal("FUNO11", root.GetProperty("ticker").GetString());
        Assert.True(root.TryGetProperty("nameVariants", out var variants));
        Assert.Equal(JsonValueKind.Array, variants.ValueKind);
        Assert.True(root.TryGetProperty("investorUrl", out _));
        Assert.True(root.TryGetProperty("reportsUrl", out _));
    }

    [Fact]
    public async Task GetFibraByTicker_InactiveFibra_IsAccessible()
    {
        await EnsureSeededAsync();
        var response = await _client.GetAsync("/api/v1/fibras/INACTIVA1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetFibraByTicker_NonExistentTicker_Returns404WithProblemDetails()
    {
        await EnsureSeededAsync();
        var response = await _client.GetAsync("/api/v1/fibras/FAKE99");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        Assert.True(doc.RootElement.TryGetProperty("domainCode", out _));
        Assert.True(doc.RootElement.TryGetProperty("correlationId", out _));
    }
}
```

**Nota sobre seeding en tests con InMemory:** Los datos semilla de `CatalogSeed.HasData()` no funcionan con InMemory EF Core (solo con migraciones reales). Por eso `ApiWebFactory` tiene `SeedCatalogAsync()` que inserta datos directamente. No depender de `HasData()` en tests.

### Estado actual de `Program.cs` (inicio de esta historia)

```csharp
using Api.CompositionRoot;
using Api.Endpoints.Ops;
using Api.Endpoints.Private;
using Api.Endpoints.Public;
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.AddApiInfrastructure();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();
app.UseApiInfrastructure();
app.UseHttpsRedirection();
app.MapAuth();
app.MapMe();
app.MapOpsPing();
app.Run();

public partial class Program { }
```

### Estado objetivo de `Program.cs` al finalizar

```csharp
using Api.CompositionRoot;
using Api.Endpoints.Ops;
using Api.Endpoints.Private;
using Api.Endpoints.Public;
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.AddApiInfrastructure();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();
app.UseApiInfrastructure();
app.UseHttpsRedirection();
app.MapAuth();
app.MapMe();
app.MapOpsPing();
app.MapCatalog();   // ← AGREGAR
app.Run();

public partial class Program { }
```

### Estructura de directorios — archivos creados/modificados

```
src/Server/
├── Domain/
│   └── Catalog/
│       ├── FibraState.cs              ← NUEVO
│       └── Fibra.cs                   ← NUEVO
├── Application/
│   └── Catalog/
│       ├── FibraFilter.cs             ← NUEVO
│       └── IFibraRepository.cs        ← NUEVO
├── SharedApiContracts/
│   ├── Common/
│   │   └── PagedResult.cs             ← NUEVO
│   └── Catalog/
│       ├── FibraListItem.cs           ← NUEVO
│       └── FibraDetail.cs             ← NUEVO
├── Infrastructure/
│   └── Persistence/
│       ├── SqlServer/
│       │   ├── AppDbContext.cs        ← MODIFICAR (agregar DbSet<Fibra> + CatalogSeed)
│       │   ├── Configurations/
│       │   │   └── Catalog/
│       │   │       └── FibraConfiguration.cs  ← NUEVO
│       │   └── Migrations/
│       │       └── [timestamp]_AddCatalogSchema.cs  ← GENERAR vía dotnet ef
│       ├── Repositories/
│       │   └── Catalog/
│       │       └── FibraRepository.cs ← NUEVO
│       └── Seed/
│           └── CatalogSeed.cs         ← NUEVO
└── Api/
    ├── CompositionRoot/
    │   └── ApiServiceExtensions.cs    ← MODIFICAR (registrar IFibraRepository)
    ├── Endpoints/
    │   └── Public/
    │       └── CatalogEndpoints.cs    ← NUEVO
    └── Program.cs                     ← MODIFICAR (app.MapCatalog())

tests/
└── Integration/
    └── Api.Tests/
        ├── ApiWebFactory.cs           ← MODIFICAR (agregar SeedCatalogAsync)
        └── CatalogEndpointTests.cs    ← NUEVO (6 tests)
```

### Convenciones de nomenclatura a respetar (Architecture.md)

- **BD:** schema `catalog` (minúscula), tabla `Fibra` (PascalCase), columnas `snake_case`
- **API:** ruta `/api/v1/fibras` (minúscula plural), JSON en `camelCase`
- **C#:** tipos/miembros públicos `PascalCase`, campos privados `_camelCase`
- **Enum en API:** serializar como string (`"Active"`, `"Inactive"`) — no como ordinal numérico

### Qué NO debe hacer esta historia

- **No implementar búsqueda/autocomplete** — eso va en Historia 2.2
- **No implementar datos de mercado (precio, frescura)** — eso va en Épica 3
- **No implementar la UI frontend** — la Home y la ficha pública van en Historias 2.2 y 2.3
- **No crear una ruta `/api/v1/catalogo`** — la ruta correcta es `/api/v1/fibras`
- **No agregar filtros complejos (por sector, mercado, etc.)** fuera del scope — solo paginación básica
- **No implementar gestión de catálogo desde Ops** — eso va en Historia 5.3
- **No agregar autenticación a los endpoints de catálogo** — son rutas públicas (`AllowAnonymous`)

### Aprendizajes de historias anteriores aplicados

1. **CPM obligatorio:** No agregar versiones en `.csproj`; esta historia no necesita paquetes nuevos.
2. **`ApiWebFactory` — seeding manual para InMemory:** `HasData()` en `OnModelCreating` aplica solo con migraciones reales. En tests usar `EnsureCreatedAsync()` + inserción directa (ver `SeedUsersAsync()` como modelo).
3. **`InMemoryDatabaseRoot` por instancia** ya está en `ApiWebFactory` — no modificar ese patrón.
4. **Enum a string en EF:** `HasConversion<string>()` como en `UserRole` → `FibraState`.
5. **Ruta del `GlobalExceptionHandler`:** Las excepciones no capturadas se convierten en `ProblemDetails` vía el handler existente. Para el 404, usar `Results.Problem(...)` directamente en el endpoint.
6. **`correlationId` en ProblemDetails:** El `GlobalExceptionHandler` y `ApiServiceExtensions` ya inyectan `correlationId` en todas las respuestas de error. Al usar `Results.Problem(...)`, el `correlationId` se agrega automáticamente si el endpoint está dentro del pipeline de ASP.NET Core con el middleware de correlation ID activo.

### Verificación final antes de mover a `review`

1. `dotnet build FIBRADIS.slnx` — exit code 0, sin warnings
2. `dotnet test tests/Integration/Api.Tests/` — todos los tests pasan (25 anteriores + 6 nuevos de catálogo)
3. `curl http://localhost:5265/api/v1/fibras` → `{"items":[...],"page":1,"pageSize":20,"total":10}`
4. `curl http://localhost:5265/api/v1/fibras/FUNO11` → objeto completo con `nameVariants`
5. `curl http://localhost:5265/api/v1/fibras/FAKE99` → `404` con `domainCode: "FIBRA_NOT_FOUND"`
6. Verificar en SQL Server que existe schema `catalog` con tabla `Fibra` y 10 filas de datos semilla

### Referencias

- Épicas: Historia 2.1 completa en [_bmad-output/planning-artifacts/epics.md#historia-21]
- FR-01, FR-02, FR-49: catálogo maestro con ticker, URLs y variantes de nombre
- Arquitectura: schema `catalog`, convenciones BD/API/C#, patrón de repositorio
- Historia 1.4 (patrones de ApiWebFactory, seeding, tests de integración): [_bmad-output/implementation-artifacts/1-4-hangfire-health-checks-y-observabilidad-minima.md]
- Historia 1.3 (patrón de autenticación y endpoints públicos/privados/ops): [_bmad-output/implementation-artifacts/1-3-autenticacion-jwt-y-autorizacion-por-roles.md]

---

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6 (create-story, 2026-05-16)
claude-sonnet-4-6 (dev-story, 2026-05-16)

### Debug Log References

- **Fix parámetros endpoint:** En .NET 10, parámetros `int page` sin default en minimal APIs son requeridos y lanzan `BadHttpRequestException`. Se cambiaron a `int? page = null, int? pageSize = null` con normalización explícita en el handler.
- **Fix seeding tests InMemory:** `CatalogSeed.HasData()` aplica en InMemory via `EnsureCreatedAsync()`, por lo que `AnyAsync()` devuelve true antes del seed manual. Se cambió la condición a buscar específicamente el ticker "INACTIVA1" para insertar sólo la fibra inactiva de test.

### Code Review Findings

- **Medium [RESUELTO]:** `CatalogSeed.GuidFromTicker()` no genera GUIDs únicos por ticker; solo deriva el valor de los primeros 3 caracteres. Cualquier ticker futuro con el mismo prefijo de 3 letras colisionará en la PK/seed data y hará frágil la evolución del catálogo. Referencia: `src/Server/Infrastructure/Persistence/Seed/CatalogSeed.cs`.
  - Fix: reemplazado con `MD5.HashData(Encoding.UTF8.GetBytes(ticker))` — garantiza unicidad basada en el ticker completo. Migración eliminada y regenerada con los GUIDs correctos. Build 0 warnings, 31/31 tests verdes.

### Code Review Outcome

- Revisión final completada sin hallazgos activos.
- Verificación ejecutada: `dotnet build FIBRADIS.slnx` y `dotnet test tests/Integration/Api.Tests/ --filter CatalogEndpointTests`.
- Resultado: historia aprobada y movida de `review` a `done`.

### Completion Notes List

- Implementada la entidad `Fibra` con enum `FibraState` (Active/Inactive) en Domain.Catalog.
- Creados contratos `IFibraRepository` y `FibraFilter` en Application.Catalog.
- Creados DTOs `PagedResult<T>`, `FibraListItem`, `FibraDetail` en SharedApiContracts.
- Configuración EF Core con schema `catalog`, tabla `Fibra`, columnas snake_case, `NameVariants` como JSON con `ValueComparer`.
- `CatalogSeed` con 10 FIBRAs reales usando GUIDs deterministas.
- Migración `AddCatalogSchema` generada por EF Core.
- `FibraRepository` implementado con paginación y filtrado por estado.
- Endpoints públicos `GET /api/v1/fibras` y `GET /api/v1/fibras/{ticker}` sin autenticación.
- 6 tests de integración nuevos, todos pasando. Total suite: 31/31 verde.

### File List

- src/Server/Domain/Catalog/FibraState.cs (nuevo)
- src/Server/Domain/Catalog/Fibra.cs (nuevo)
- src/Server/Application/Catalog/FibraFilter.cs (nuevo)
- src/Server/Application/Catalog/IFibraRepository.cs (nuevo)
- src/Server/SharedApiContracts/Common/PagedResult.cs (nuevo)
- src/Server/SharedApiContracts/Catalog/FibraListItem.cs (nuevo)
- src/Server/SharedApiContracts/Catalog/FibraDetail.cs (nuevo)
- src/Server/Infrastructure/Persistence/SqlServer/Configurations/Catalog/FibraConfiguration.cs (nuevo)
- src/Server/Infrastructure/Persistence/Seed/CatalogSeed.cs (nuevo)
- src/Server/Infrastructure/Persistence/SqlServer/AppDbContext.cs (modificado)
- src/Server/Infrastructure/Persistence/Migrations/20260517002021_AddCatalogSchema.cs (generado)
- src/Server/Infrastructure/Persistence/Migrations/20260517002021_AddCatalogSchema.Designer.cs (generado)
- src/Server/Infrastructure/Persistence/Repositories/Catalog/FibraRepository.cs (nuevo)
- src/Server/Api/CompositionRoot/ApiServiceExtensions.cs (modificado)
- src/Server/Api/Endpoints/Public/CatalogEndpoints.cs (nuevo)
- src/Server/Api/Program.cs (modificado)
- tests/Integration/Api.Tests/ApiWebFactory.cs (modificado)
- tests/Integration/Api.Tests/CatalogEndpointTests.cs (nuevo)

### Change Log

- 2026-05-16: Historia 2.1 implementada — catálogo maestro con endpoints públicos GET /api/v1/fibras y GET /api/v1/fibras/{ticker}, 10 FIBRAs reales en datos semilla, migración EF Core AddCatalogSchema, 6 tests de integración nuevos.
