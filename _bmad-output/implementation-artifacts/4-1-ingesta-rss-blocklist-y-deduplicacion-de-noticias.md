# Historia 4.1: Ingesta RSS, blocklist y deduplicación de noticias

Status: in-progress

## Story

Como visitante público,
quiero que la plataforma ingiera automáticamente noticias relacionadas con FIBRAs cada hora desde Google News RSS, filtrando contenido irrelevante y eliminando duplicados,
para que solo aparezcan noticias limpias y relevantes sobre FIBRAs inmobiliarias en la plataforma.

## Acceptance Criteria

1. **Ingesta y normalización:** Dado que el pipeline de noticias se ejecuta por primera vez, cuando se obtienen items usando queries específicas por FIBRA (ej: "FUNO11 FIBRA") y queries generales de mercado (ej: "FIBRAs Mexico BMV"), entonces todos los items se normalizan con título, fuente, fecha, URL y snippet, y se persisten con `status=pending`.
2. **Blocklist aplicado:** Dado que un item contiene "fibra óptica" en su título o snippet, cuando se aplica el blocklist, entonces el item se descarta y no se guarda en la base de datos.
3. **Deduplicación exacta por URL:** Dado que dos items comparten exactamente la misma URL, entonces solo se almacena la primera ocurrencia; el duplicado se descarta.
4. **Deduplicación probable por título:** Dado que dos items tienen títulos casi idénticos publicados en un período de 24 horas, entonces solo se almacena uno; el duplicado probable se descarta.
5. **Blocklist actualizable desde Ops:** Dado que el blocklist en Ops se actualiza para agregar un nuevo término, entonces el siguiente ciclo del pipeline aplica el blocklist actualizado sin redespliegue.

## Tasks / Subtasks

- [x] Task 1: Backend — Entidades de dominio (AC: #1, #2, #3, #4)
  - [x] 1.1 Crear `src/Server/Domain/News/NewsArticleStatus.cs`: enum `Pending, Processed, Partial, Error`
  - [x] 1.2 Crear `src/Server/Domain/News/NewsArticle.cs` (ver Dev Notes — Entidad NewsArticle)
  - [x] 1.3 Crear `src/Server/Domain/News/BlocklistTerm.cs` (ver Dev Notes — Entidad BlocklistTerm)

- [x] Task 2: Backend — EF Core: configuraciones y DbContext (AC: #1, #5)
  - [x] 2.1 Crear `src/Server/Infrastructure/Persistence/SqlServer/Configurations/News/NewsArticleConfiguration.cs` (ver Dev Notes)
  - [x] 2.2 Crear `src/Server/Infrastructure/Persistence/SqlServer/Configurations/News/BlocklistTermConfiguration.cs` (ver Dev Notes)
  - [x] 2.3 Agregar `DbSet<NewsArticle> NewsArticles` y `DbSet<BlocklistTerm> BlocklistTerms` a `AppDbContext.cs` (patrón existente en líneas 9-17 del archivo)
  - [x] 2.4 Crear `src/Server/Infrastructure/Persistence/Seeds/NewsSeed.cs` con blocklist inicial (ver Dev Notes — Seed)
  - [x] 2.5 Registrar `NewsSeed.Seed(modelBuilder)` en `AppDbContext.OnModelCreating`
  - [x] 2.6 Crear migración: `dotnet ef migrations add AddNewsSchema --project src/Server/Infrastructure --startup-project src/Server/Api --configuration Release`
  - [x] 2.7 Aplicar migración: `dotnet ef database update --project src/Server/Infrastructure --startup-project src/Server/Api`

- [x] Task 3: Backend — Contratos de Application (AC: #1, #2, #3, #4, #5)
  - [x] 3.1 Crear `src/Server/Application/News/INewsRepository.cs` (ver Dev Notes)
  - [x] 3.2 Crear `src/Server/Application/News/IBlocklistRepository.cs` (ver Dev Notes)
  - [x] 3.3 Crear `src/Server/Application/News/IRssClient.cs` con record `RssItem` (ver Dev Notes)

- [x] Task 4: Backend — `NewsDeduplicator` lógica pura + tests (AC: #2, #3, #4)
  - [x] 4.1 Crear `src/Server/Application/News/NewsDeduplicator.cs` con métodos `NormalizeTitle`, `MatchesBlocklist`, `Filter` (ver Dev Notes — algoritmo completo)
  - [x] 4.2 Crear `tests/Unit/Application.Tests/News/NewsDeduplicatorTests.cs` — 8 tests (ver Dev Notes — Tests obligatorios)

- [x] Task 5: Backend — Implementaciones Infrastructure (AC: #1, #3, #4, #5)
  - [x] 5.1 Crear `src/Server/Infrastructure/Persistence/Repositories/News/NewsRepository.cs` (ver Dev Notes)
  - [x] 5.2 Crear `src/Server/Infrastructure/Persistence/Repositories/News/BlocklistRepository.cs` (ver Dev Notes)
  - [x] 5.3 Crear `src/Server/Infrastructure/Integrations/GoogleNews/GoogleNewsRssClient.cs` — `HttpClient` + `XDocument` (ver Dev Notes)

- [x] Task 6: Backend — `NewsPipelineJob` + schedule (AC: #1, #2, #3, #4, #5)
  - [x] 6.1 Crear `src/Server/Infrastructure/Jobs/News/NewsPipelineJob.cs` (ver Dev Notes — flujo completo)
  - [x] 6.2 Crear `src/Server/Infrastructure/Jobs/News/NewsPipelineSchedule.cs` — cron `"0 * * * *"` (hourly) con `JobId = "news-pipeline-hourly"`

- [x] Task 7: Backend — Registro DI y Hangfire (AC: #1, #5)
  - [x] 7.1 Agregar en `ApiServiceExtensions.cs` (ver Dev Notes — Registro DI)
  - [x] 7.2 Agregar en `Program.cs` el `RecurringJob.AddOrUpdate<NewsPipelineJob>` (ver Dev Notes)

- [x] Task 8: Backend — Build y tests (AC: todos)
  - [x] 8.1 `dotnet build FIBRADIS.slnx` — 0 errores
  - [x] 8.2 `dotnet test tests/Unit/Application.Tests/Application.Tests.csproj` — todos los tests pasan incluyendo `NewsDeduplicatorTests`

## Dev Notes

### Contexto de historias previas

**Épica 3** creó el patrón completo de job Hangfire:
- `MarketPipelineJob.cs` → patrón exacto a replicar: ctor con DI, `ExecuteAsync(CancellationToken)`, log structured al inicio y fin
- `MarketPipelineSchedule.cs` → patrón de `JobId` + `GetRecurringJobs()` + `GetMexicoTimeZone()` — replicar para noticias
- Registro en `ApiServiceExtensions.cs`: `builder.Services.AddScoped<MarketPipelineJob>()` y configuración Hangfire con SQL Server schema `jobs`
- Registro en `Program.cs`: bloque `RecurringJob.AddOrUpdate<T>` con zona horaria

**Catálogo**: `Fibra.NameVariants` (List<string>) ya existe en la entidad — el pipeline de noticias lo usa para construir las queries RSS por FIBRA.

**DB vacía en dev**: La base `FIBRADIS_Dev` en LAPBADIS (Windows Auth) no tiene datos de mercado seeded. Las distribuciones que existen son del seed EF (datos estáticos). El pipeline de noticias empezará sin datos históricos — los tests unitarios no requieren BD real.

**Módulo News**: Los directorios `src/Server/Domain/News/`, `src/Server/Application/News/`, `src/Server/Infrastructure/Jobs/News/` ya existen con `.gitkeep`. Crear los archivos directamente en esas rutas.

**Placeholders de UI listos**: `NoticiasSection.tsx` (ficha pública) y `NewsSection.tsx` (Home) ya tienen el esqueleto correcto. Esta historia NO las conecta — eso es historia 4.2.

---

### Entidad NewsArticle

```csharp
// src/Server/Domain/News/NewsArticle.cs
namespace Domain.News;

public class NewsArticle
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string TitleNormalized { get; set; } = string.Empty;  // lowercase, sin puntuación — para dedup
    public string Source { get; set; } = string.Empty;
    public DateTimeOffset PublishedAt { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? Snippet { get; set; }
    public NewsArticleStatus Status { get; set; } = NewsArticleStatus.Pending;
    public DateTimeOffset CapturedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? ErrorReason { get; set; }
}
```

---

### Entidad BlocklistTerm

```csharp
// src/Server/Domain/News/BlocklistTerm.cs
namespace Domain.News;

public class BlocklistTerm
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Term { get; set; } = string.Empty;  // lowercase, normalizado al guardar
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
```

---

### EF Core Configurations

```csharp
// NewsArticleConfiguration.cs
builder.ToTable("NewsArticle", schema: "news");
builder.HasKey(x => x.Id);
builder.Property(x => x.Title).HasMaxLength(512).IsRequired();
builder.Property(x => x.TitleNormalized).HasMaxLength(512).IsRequired();
builder.Property(x => x.Source).HasMaxLength(256).IsRequired();
builder.Property(x => x.Url).HasMaxLength(1024).IsRequired();
builder.Property(x => x.Snippet).HasMaxLength(2048);
builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(16).IsRequired();
builder.Property(x => x.ErrorReason).HasMaxLength(512);
// Índice único por URL (dedup exacto a nivel DB)
builder.HasIndex(x => x.Url).IsUnique().HasDatabaseName("IX_NewsArticle_Url");
// Índice para dedup por título normalizado + fecha
builder.HasIndex(x => new { x.TitleNormalized, x.CapturedAt }).HasDatabaseName("IX_NewsArticle_TitleNormalized_CapturedAt");
```

```csharp
// BlocklistTermConfiguration.cs
builder.ToTable("BlocklistTerm", schema: "news");
builder.HasKey(x => x.Id);
builder.Property(x => x.Term).HasMaxLength(256).IsRequired();
builder.HasIndex(x => x.Term).IsUnique().HasDatabaseName("IX_BlocklistTerm_Term");
```

---

### Seed del blocklist

```csharp
// src/Server/Infrastructure/Persistence/Seeds/NewsSeed.cs
namespace Infrastructure.Persistence.Seeds;

public static class NewsSeed
{
    // Términos que filtran noticias NO relacionadas con FIBRAs inmobiliarias
    private static readonly string[] DefaultBlocklist =
    [
        "fibra óptica",
        "fibra optica",
        "fibra alimentaria",
        "fibra dietética",
        "fibra dietetica",
        "fibra muscular",
        "fibra textil",
        "fibra de carbono",
        "internet fibra",
        "fibra de vidrio",
    ];

    public static void Seed(ModelBuilder modelBuilder)
    {
        // GUIDs deterministas para idempotencia
        var terms = DefaultBlocklist.Select((term, i) =>
        {
            var guid = new Guid(MD5.HashData(Encoding.UTF8.GetBytes($"blocklist-{term}")));
            return new BlocklistTerm { Id = guid, Term = term, CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero) };
        });

        modelBuilder.Entity<BlocklistTerm>().HasData(terms);
    }
}
```

Usings necesarios: `using System.Security.Cryptography;`, `using System.Text;`, `using Domain.News;`, `using Microsoft.EntityFrameworkCore;`

---

### Contratos de Application

```csharp
// INewsRepository.cs
namespace Application.News;
public interface INewsRepository
{
    Task<bool> ExistsByUrlAsync(string url, CancellationToken ct = default);
    Task<bool> ExistsBySimilarTitleAsync(string titleNormalized, DateTimeOffset since, CancellationToken ct = default);
    Task AddAsync(NewsArticle article, CancellationToken ct = default);
    Task<IReadOnlyList<NewsArticle>> GetLatestAsync(int count, CancellationToken ct = default);
}

// IBlocklistRepository.cs
namespace Application.News;
public interface IBlocklistRepository
{
    Task<IReadOnlyList<string>> GetAllTermsAsync(CancellationToken ct = default);
}

// IRssClient.cs
namespace Application.News;

public record RssItem(
    string Title,
    string Source,
    DateTimeOffset PublishedAt,
    string Url,
    string? Snippet
);

public interface IRssClient
{
    Task<IReadOnlyList<RssItem>> FetchAsync(string query, CancellationToken ct = default);
}
```

---

### NewsDeduplicator — lógica pura (sin dependencias)

```csharp
// src/Server/Application/News/NewsDeduplicator.cs
namespace Application.News;

public static class NewsDeduplicator
{
    // Normaliza: minúsculas, remueve puntuación y diacríticos, colapsa espacios
    public static string NormalizeTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title)) return string.Empty;
        var normalized = title.ToLowerInvariant();
        // Remover puntuación y caracteres no alfanuméricos (conservar espacios)
        normalized = Regex.Replace(normalized, @"[^\w\s]", " ");
        // Colapsar múltiples espacios
        normalized = Regex.Replace(normalized, @"\s+", " ").Trim();
        return normalized;
    }

    // Devuelve true si el texto (título O snippet) contiene algún término del blocklist
    public static bool MatchesBlocklist(string title, string? snippet, IReadOnlyList<string> terms)
    {
        if (terms.Count == 0) return false;
        var titleLower = title.ToLowerInvariant();
        var snippetLower = snippet?.ToLowerInvariant() ?? string.Empty;
        return terms.Any(t =>
            titleLower.Contains(t, StringComparison.OrdinalIgnoreCase) ||
            snippetLower.Contains(t, StringComparison.OrdinalIgnoreCase));
    }

    // Filtra una lista de RssItems en base a:
    //   existingUrls: set de URLs ya en DB (dedup exacta)
    //   recentTitles: TitleNormalized de artículos persistidos en las últimas 24h (dedup probable)
    //   blocklistTerms: términos del blocklist (en minúsculas)
    // Retorna solo los items que pasan todos los filtros, eliminando también duplicados internos del batch
    public static IReadOnlyList<RssItem> Filter(
        IReadOnlyList<RssItem> items,
        IReadOnlySet<string> existingUrls,
        IReadOnlyList<string> recentTitles,
        IReadOnlyList<string> blocklistTerms)
    {
        var result = new List<RssItem>();
        var seenUrlsInBatch = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seenTitlesInBatch = new List<string>();

        foreach (var item in items)
        {
            // 1. Blocklist
            if (MatchesBlocklist(item.Title, item.Snippet, blocklistTerms))
                continue;

            // 2. Dedup exacto por URL (DB + batch actual)
            if (existingUrls.Contains(item.Url) || !seenUrlsInBatch.Add(item.Url))
                continue;

            // 3. Dedup probable por título normalizado (DB últimas 24h + batch actual)
            var normalized = NormalizeTitle(item.Title);
            if (recentTitles.Any(t => t == normalized) || seenTitlesInBatch.Any(t => t == normalized))
                continue;

            seenTitlesInBatch.Add(normalized);
            result.Add(item);
        }

        return result;
    }
}
```

`using System.Text.RegularExpressions;` en el using del archivo.

---

### Tests obligatorios — NewsDeduplicatorTests.cs

```
tests/Unit/Application.Tests/News/NewsDeduplicatorTests.cs
```

**8 tests a implementar:**

1. `NormalizeTitle_LowercasesAndRemovesPunctuation` → "FUNO11, Fibra!" → "funo11 fibra"
2. `MatchesBlocklist_ReturnsTrueWhenTitleContainsTerm` → title="noticias fibra óptica" → true
3. `MatchesBlocklist_ReturnsTrueWhenSnippetContainsTerm` → title limpio, snippet="fibra óptica" → true
4. `MatchesBlocklist_ReturnsFalseWhenNoMatch` → title="FUNO11 sube 3%" → false
5. `MatchesBlocklist_IsCaseInsensitive` → term="fibra optica", title="FIBRA OPTICA" → true
6. `Filter_RemovesItemsMatchingBlocklist` → item con "fibra óptica" → no en resultado
7. `Filter_RemovesExactDuplicateUrl` → dos items misma URL → solo uno en resultado
8. `Filter_RemovesProbableDuplicateTitle` → dos items mismo título normalizado en batch → solo uno en resultado
9. `Filter_KeepsTitleDupOutside24hWindow` — este test valida la LÓGICA DE PASAR el `recentTitles` vacío: si `recentTitles` está vacío (vendrían de DB con filtro 24h), el mismo título en el batch se filtra igualmente (batch dedup). **NOTA**: el filtrado por 24h está en el repositorio (la query a DB); el `Filter` recibe el resultado ya filtrado. Separar concerns.
10. `Filter_AllowsCleanItemThrough` → item limpio, sin URL ni título dup, sin blocklist → en resultado

Son 9 tests mínimos; 10 si se agrega caso límite.

---

### Implementaciones Infrastructure

#### NewsRepository.cs

```csharp
// src/Server/Infrastructure/Persistence/Repositories/News/NewsRepository.cs
namespace Infrastructure.Persistence.Repositories.News;

public class NewsRepository(AppDbContext db) : INewsRepository
{
    public async Task<bool> ExistsByUrlAsync(string url, CancellationToken ct = default)
        => await db.NewsArticles.AnyAsync(n => n.Url == url, ct);

    public async Task<bool> ExistsBySimilarTitleAsync(string titleNormalized, DateTimeOffset since, CancellationToken ct = default)
        => await db.NewsArticles.AnyAsync(n => n.TitleNormalized == titleNormalized && n.CapturedAt >= since, ct);

    public async Task AddAsync(NewsArticle article, CancellationToken ct = default)
    {
        db.NewsArticles.Add(article);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<NewsArticle>> GetLatestAsync(int count, CancellationToken ct = default)
        => await db.NewsArticles
            .Where(n => n.Status == NewsArticleStatus.Pending || n.Status == NewsArticleStatus.Processed)
            .OrderByDescending(n => n.PublishedAt)
            .Take(count)
            .ToListAsync(ct);
}
```

#### BlocklistRepository.cs

```csharp
// src/Server/Infrastructure/Persistence/Repositories/News/BlocklistRepository.cs
namespace Infrastructure.Persistence.Repositories.News;

public class BlocklistRepository(AppDbContext db) : IBlocklistRepository
{
    public async Task<IReadOnlyList<string>> GetAllTermsAsync(CancellationToken ct = default)
        => await db.BlocklistTerms.Select(t => t.Term).ToListAsync(ct);
}
```

---

### GoogleNewsRssClient.cs

Usar `HttpClient` + `XDocument` (sin NuGet nuevo). `System.Xml.Linq` está en la BCL de .NET.

Google News RSS URL: `https://news.google.com/rss/search?q={Uri.EscapeDataString(query)}&hl=es-419&gl=MX&ceid=MX:es-419`

```csharp
// src/Server/Infrastructure/Integrations/GoogleNews/GoogleNewsRssClient.cs
namespace Infrastructure.Integrations.GoogleNews;

public class GoogleNewsRssClient(HttpClient http, ILogger<GoogleNewsRssClient> logger) : IRssClient
{
    private const string BaseUrl = "https://news.google.com/rss/search";

    public async Task<IReadOnlyList<RssItem>> FetchAsync(string query, CancellationToken ct = default)
    {
        var url = $"{BaseUrl}?q={Uri.EscapeDataString(query)}&hl=es-419&gl=MX&ceid=MX:es-419";

        try
        {
            var xml = await http.GetStringAsync(url, ct);
            var doc = XDocument.Parse(xml);
            XNamespace media = "http://search.yahoo.com/mrss/";

            var items = doc.Descendants("item")
                .Select(item =>
                {
                    var title = item.Element("title")?.Value ?? string.Empty;
                    var link = item.Element("link")?.Value ?? string.Empty;
                    var snippet = item.Element("description")?.Value;
                    var source = item.Element("source")?.Value ?? string.Empty;
                    var pubDateStr = item.Element("pubDate")?.Value ?? string.Empty;

                    DateTimeOffset.TryParse(pubDateStr, out var pubDate);

                    return new RssItem(
                        Title: title,
                        Source: source,
                        PublishedAt: pubDate == default ? DateTimeOffset.UtcNow : pubDate,
                        Url: link,
                        Snippet: snippet
                    );
                })
                .Where(i => !string.IsNullOrWhiteSpace(i.Url))
                .ToList();

            return items;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "RSS fetch failed for query '{Query}'", query);
            return [];
        }
    }
}
```

Usar: `using System.Xml.Linq;`

---

### NewsPipelineJob.cs — flujo completo

```csharp
// src/Server/Infrastructure/Jobs/News/NewsPipelineJob.cs
namespace Infrastructure.Jobs.News;

public class NewsPipelineJob(
    IFibraRepository fibraRepo,
    INewsRepository newsRepo,
    IBlocklistRepository blocklistRepo,
    IRssClient rssClient,
    ILogger<NewsPipelineJob> logger)
{
    // Queries generales de mercado (seed fijo para MVP — configurable en historia 5.4)
    private static readonly string[] GeneralQueries =
    [
        "FIBRAs Mexico BMV",
        "mercado inmobiliario México renta",
    ];

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var fibras = await fibraRepo.GetAllActiveAsync(ct);
        var blocklistTerms = await blocklistRepo.GetAllTermsAsync(ct);
        var since24h = DateTimeOffset.UtcNow.AddHours(-24);

        var allItems = new List<RssItem>();

        // Queries específicas por FIBRA (Ticker + NameVariants)
        foreach (var fibra in fibras)
        {
            var queries = BuildFibraQueries(fibra);
            foreach (var query in queries)
            {
                var items = await rssClient.FetchAsync(query, ct);
                allItems.AddRange(items);
            }
        }

        // Queries generales de mercado
        foreach (var query in GeneralQueries)
        {
            var items = await rssClient.FetchAsync(query, ct);
            allItems.AddRange(items);
        }

        // Cargar URLs existentes y títulos recientes para dedup
        // IMPORTANTE: queries secuenciales (mismo DbContext — no thread-safe)
        var existingUrls = new HashSet<string>(
            (await newsRepo.GetExistingUrlsAsync(ct)),
            StringComparer.OrdinalIgnoreCase);

        var recentTitles = await newsRepo.GetRecentNormalizedTitlesAsync(since24h, ct);

        // Filtrar con lógica pura
        var filtered = NewsDeduplicator.Filter(allItems, existingUrls, recentTitles, blocklistTerms);

        int saved = 0, errors = 0;
        foreach (var item in filtered)
        {
            try
            {
                var article = new NewsArticle
                {
                    Title = item.Title,
                    TitleNormalized = NewsDeduplicator.NormalizeTitle(item.Title),
                    Source = item.Source,
                    PublishedAt = item.PublishedAt,
                    Url = item.Url,
                    Snippet = item.Snippet,
                    Status = NewsArticleStatus.Pending,
                    CapturedAt = DateTimeOffset.UtcNow,
                };
                await newsRepo.AddAsync(article, ct);
                saved++;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to save news article '{Url}'", item.Url);
                errors++;
            }
        }

        logger.LogInformation(
            "News pipeline complete — fetched: {Fetched}, filtered_in: {FilteredIn}, saved: {Saved}, errors: {Errors}",
            allItems.Count, filtered.Count, saved, errors);
    }

    private static IEnumerable<string> BuildFibraQueries(Fibra fibra)
    {
        // Query primaria: ticker + "FIBRA"
        yield return $"{fibra.Ticker} FIBRA";
        // Queries adicionales por variantes (si existen y son distintas del ticker)
        foreach (var variant in fibra.NameVariants.Where(v => !string.Equals(v, fibra.Ticker, StringComparison.OrdinalIgnoreCase)))
            yield return $"{variant} FIBRA México";
    }
}
```

**NOTA**: Este método requiere dos métodos adicionales en `INewsRepository` que NO están en el contrato anterior:
- `GetExistingUrlsAsync` → devuelve todas las URLs en DB (o un HashSet)
- `GetRecentNormalizedTitlesAsync(since, ct)` → devuelve TitleNormalized de artículos desde `since`

Actualizar `INewsRepository` con estos dos métodos y sus implementaciones en `NewsRepository`.

**Contrato INewsRepository completo** (reemplaza el anterior):

```csharp
public interface INewsRepository
{
    Task<bool> ExistsByUrlAsync(string url, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetExistingUrlsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetRecentNormalizedTitlesAsync(DateTimeOffset since, CancellationToken ct = default);
    Task AddAsync(NewsArticle article, CancellationToken ct = default);
    Task<IReadOnlyList<NewsArticle>> GetLatestAsync(int count, CancellationToken ct = default);
}
```

`ExistsByUrlAsync` queda para uso futuro (historia 4.2). `GetExistingUrlsAsync` es la que usa el pipeline.

**NewsRepository — métodos adicionales:**

```csharp
public async Task<IReadOnlyList<string>> GetExistingUrlsAsync(CancellationToken ct = default)
    => await db.NewsArticles.Select(n => n.Url).ToListAsync(ct);

public async Task<IReadOnlyList<string>> GetRecentNormalizedTitlesAsync(DateTimeOffset since, CancellationToken ct = default)
    => await db.NewsArticles
        .Where(n => n.CapturedAt >= since)
        .Select(n => n.TitleNormalized)
        .ToListAsync(ct);
```

---

### NewsPipelineSchedule.cs

```csharp
// src/Server/Infrastructure/Jobs/News/NewsPipelineSchedule.cs
namespace Infrastructure.Jobs.News;

public static class NewsPipelineSchedule
{
    public const string HourlyJobId = "news-pipeline-hourly";

    // Cada hora, todos los días (sin restricción de horario BMV — noticias son 24/7)
    public const string CronExpression = "0 * * * *";
}
```

---

### Registro DI — ApiServiceExtensions.cs

En `ApiServiceExtensions.cs`, dentro del bloque existente de servicios de Application/Infrastructure, agregar:

```csharp
// News
builder.Services.AddScoped<NewsPipelineJob>();
builder.Services.AddScoped<INewsRepository, NewsRepository>();
builder.Services.AddScoped<IBlocklistRepository, BlocklistRepository>();

// HttpClient para Google News RSS
builder.Services.AddHttpClient<IRssClient, GoogleNewsRssClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("FIBRADIS/1.0 (+https://fibradis.mx)");
});
```

**NOTA:** `AddHttpClient<IRssClient, GoogleNewsRssClient>` registra `IRssClient` como typed HTTP client. `GoogleNewsRssClient` recibe `HttpClient` en el constructor por inyección.

---

### Registro Hangfire — Program.cs

En `Program.cs`, dentro del bloque `if (!useInMemoryHangfire && ...)`, agregar junto al registro del job de mercado:

```csharp
RecurringJob.AddOrUpdate<NewsPipelineJob>(
    NewsPipelineSchedule.HourlyJobId,
    j => j.ExecuteAsync(CancellationToken.None),
    NewsPipelineSchedule.CronExpression,
    new RecurringJobOptions { TimeZone = mexicoTz });
```

---

### Using statements necesarios (en los nuevos archivos)

- `Domain.News`: `using Domain.News;`
- `Application.News`: `using Application.News;`
- `Infrastructure.Integrations.GoogleNews`: `using System.Xml.Linq;`, `using Application.News;`, `using Microsoft.Extensions.Logging;`
- `Infrastructure.Jobs.News`: `using Application.Catalog;`, `using Application.News;`, `using Domain.News;`, `using Microsoft.Extensions.Logging;`
- `Infrastructure.Persistence.Seeds`: `using System.Security.Cryptography;`, `using System.Text;`, `using Domain.News;`, `using Microsoft.EntityFrameworkCore;`

---

### Patrón EF migrations — workaround obligatorio

**ANTES** de ejecutar `dotnet ef migrations add`, detener el proceso de la API en Debug.  
Si no es posible, usar `--configuration Release` (convención del proyecto — ver `convenciones-fibradis.md`).

---

### Anti-patrones a evitar

1. **NO** usar `Task.WhenAll` con el mismo `DbContext` — ver regla en `convenciones-fibradis.md`. Todas las queries al `newsRepo` y `blocklistRepo` van secuenciales (mismo DbContext Scoped).
2. **NO** instalar `System.ServiceModel.Syndication` — usar `XDocument`/`XElement` (System.Xml.Linq, ya en BCL).
3. **NO** agregar dependencias npm para esta historia — es puramente backend.
4. **NO** conectar `NoticiasSection.tsx` ni `NewsSection.tsx` — eso es historia 4.2.
5. **NO** crear schema `news` manualmente en SQL — la migración EF lo crea con `modelBuilder.Entity<NewsArticle>().ToTable("NewsArticle", "news")`.
6. **NO** crear archivo de tests nuevo en directorio nuevo — usar `tests/Unit/Application.Tests/` existente; agregar carpeta `News/` dentro.
7. **NO** hacer sanitización del HTML en el snippet — Google News devuelve texto plano en el campo `description`; si viene con HTML, guardarlo tal cual (se limpiará en display — historia 4.2).
8. **NO** hacer la migración en memoria — aplicar contra FIBRADIS_Dev real (SQL Server LAPBADIS, Windows Auth).

---

### Archivos nuevos

```
src/Server/Domain/News/NewsArticleStatus.cs
src/Server/Domain/News/NewsArticle.cs
src/Server/Domain/News/BlocklistTerm.cs
src/Server/Application/News/INewsRepository.cs
src/Server/Application/News/IBlocklistRepository.cs
src/Server/Application/News/IRssClient.cs
src/Server/Application/News/NewsDeduplicator.cs
src/Server/Infrastructure/Persistence/SqlServer/Configurations/News/NewsArticleConfiguration.cs
src/Server/Infrastructure/Persistence/SqlServer/Configurations/News/BlocklistTermConfiguration.cs
src/Server/Infrastructure/Persistence/Repositories/News/NewsRepository.cs
src/Server/Infrastructure/Persistence/Repositories/News/BlocklistRepository.cs
src/Server/Infrastructure/Persistence/Seeds/NewsSeed.cs
src/Server/Infrastructure/Integrations/GoogleNews/GoogleNewsRssClient.cs
src/Server/Infrastructure/Jobs/News/NewsPipelineJob.cs
src/Server/Infrastructure/Jobs/News/NewsPipelineSchedule.cs
src/Server/Infrastructure/Persistence/Migrations/[timestamp]_AddNewsSchema.cs
tests/Unit/Application.Tests/News/NewsDeduplicatorTests.cs
```

### Archivos modificados

```
src/Server/Infrastructure/Persistence/SqlServer/AppDbContext.cs
  → DbSet<NewsArticle> NewsArticles + DbSet<BlocklistTerm> BlocklistTerms
  → NewsSeed.Seed(modelBuilder) en OnModelCreating

src/Server/Api/CompositionRoot/ApiServiceExtensions.cs
  → AddScoped NewsPipelineJob, INewsRepository, IBlocklistRepository
  → AddHttpClient<IRssClient, GoogleNewsRssClient>

src/Server/Api/Program.cs
  → RecurringJob.AddOrUpdate<NewsPipelineJob>

_bmad-output/implementation-artifacts/sprint-status.yaml
```

---

### Referencias

- [Source: epics.md#Historia 4.1] — user story y ACs originales (FR-13, FR-14)
- [Source: epics.md#FR-13] — ingesta RSS Google News, queries por FIBRA y generales, configurable sin redeploy
- [Source: epics.md#FR-14] — blocklist global + dedupe exacto + dedupe probable 24h
- [Source: epics.md#NFR-05] — cadencia default 1 hora, configurable desde Ops
- [Source: src/Server/Infrastructure/Jobs/Market/MarketPipelineJob.cs] — patrón exacto de Hangfire job
- [Source: src/Server/Infrastructure/Jobs/Market/MarketPipelineSchedule.cs] — patrón de schedule
- [Source: src/Server/Api/CompositionRoot/ApiServiceExtensions.cs] — registro DI y Hangfire
- [Source: src/Server/Domain/Catalog/Fibra.cs] — Fibra.NameVariants disponible para queries RSS
- [Source: convenciones-fibradis.md] — no Task.WhenAll con DbContext, workaround migrations, no deps nuevas
- [Source: epic-3-retro-2026-05-19.md] — preparación para Épica 4: verificar System.ServiceModel.Syndication → decisión: usar XDocument (sin nueva dep)

### Review Findings

- [x] [Review][Patch] AC5 — Endpoint Ops + UI blocklist — implementado (GET/POST/DELETE + tabla SPA Ops)
- [x] [Review][Patch] `GetExistingUrlsAsync` → batch query — implementado (`WHERE url IN (candidateUrls)`) [NewsRepository.cs]
- [x] [Review][Patch] Logger: `ex.Message` → `query` en argumento estructurado — corregido [GoogleNewsRssClient.cs:43]
- [x] [Review][Patch] Título vacío como sumidero de dedup — corregido con guard `!string.IsNullOrEmpty` [NewsDeduplicator.cs:Filter]
- [x] [Review][Patch] `pubDate` inparseable sin logging — corregido con `ParsePublishedAt` + `LogWarning` [GoogleNewsRssClient.cs]

### Review Findings — 2ª pasada (2026-05-19)

- [x] [Review][Patch] `newsApi.ts` sin auth — `apiClient` creado sin headers `Authorization`; todos los requests a `/api/v1/news/blocklist-terms` devuelven 401 en runtime [src/Web/Ops/src/api/newsApi.ts]
- [x] [Review][Patch] `Term` sin validación de longitud máxima en POST — strings largos pasan la validación de `IsNullOrWhiteSpace` y lanzarían `DbUpdateException` (columna `nvarchar(256)`) devolviendo un 409 engañoso en lugar de 400 [NewsBlocklistEndpoints.cs:POST]
- [x] [Review][Patch] `ParsePublishedAt`: `pubDate` vacío/nulo usa `UtcNow` silenciosamente — el LogWarning añadido solo cubre valores no-vacíos inválidos; `pubDate` ausente no se loguea [GoogleNewsRssClient.cs:ParsePublishedAt]
- [x] [Review][Patch] Alias `BlocklistTermDto` en `newsApi.ts` no se usa — se declara pero las funciones devuelven `BlocklistTerm` local (tipo duplicado); el cast `as BlocklistTermDto` descarta las garantías del schema generado [newsApi.ts]
- [x] [Review][Defer] Cobertura de tests para 401/409/404 en endpoints de blocklist — caminos de error sin tests de integración [NewsBlocklistOpsEndpointTests.cs] — deferred, no bloqueante
- [x] [Review][Defer] `deleteMutation` compartido para todas las filas — deshabilita todos los botones simultáneamente [App.tsx] — deferred, UX menor aceptado
- [x] [Review][Defer] GUIDs de seed generados con MD5 — si se modifica o reordena `DefaultBlocklist`, los GUIDs cambian y EF emite DELETE+INSERT en migración [NewsSeed.cs:GuidFromKey] — deferred, riesgo de workflow
- [x] [Review][Defer] `PublishedAt` sin valor por defecto — queda como `DateTimeOffset.MinValue` si un código futuro no lo asigna [NewsArticle.cs] — deferred, sin impacto inmediato
- [x] [Review][Defer] RSS fetches secuenciales — N fibras × M queries = N×M llamadas HTTP en serie; intencional por limitación de rate y patrón del proyecto [NewsPipelineJob.cs] — deferred, por diseño
- [x] [Review][Defer] `CancellationToken.None` en Hangfire — sin cancelación graceful en shutdown; mismo patrón que `MarketPipelineJob` [Program.cs:50] — deferred, patrón existente en el proyecto
- [x] [Review][Defer] `AddAsync` con `SaveChangesAsync` individual — N round-trips a SQL Server en lugar de un batch; race condition manejada por unique constraint [NewsRepository.cs:AddAsync] — deferred, aceptable en volumen MVP
- [x] [Review][Defer] `Status` como `nvarchar(16)` — frágil si futuros valores del enum superan 16 chars [NewsArticleConfiguration.cs] — deferred, alcance futuro

### Review Findings — 3ª pasada (2026-05-19)

- [x] [Review][Decision] AC4: ventana de dedup usa `CapturedAt` — decisión deliberada. Más robusto que `PublishedAt` para re-sindicación de artículos viejos. AC4 se interpreta como "capturados en las últimas 24h" [NewsRepository.cs:30]
- [ ] [Review][Patch] `BlocklistRepository.NormalizeTerm` no elimina diacríticos — `NormalizeTerm` solo lowercasea y colapsa espacios; `MatchesBlocklist` aplica `NormalizeTitle` (que sí elimina diacríticos) al comparar. Resultado: "fibra óptica" y "fibra optica" se almacenan como entradas distintas pero hacen match idéntico en runtime, permitiendo duplicados semánticos en el blocklist [BlocklistRepository.cs:NormalizeTerm]
- [ ] [Review][Patch] `DbUpdateException` catch demasiado amplio en POST blocklist — cualquier `DbUpdateException` (truncación, FK, constraint de otro tipo) se devuelve como 409 Conflict, enmascarando errores reales sin loguear. Fix: loguear el exception antes de devolver Conflict [NewsBlocklistEndpoints.cs:72]
- [x] [Review][Defer] `GetExistingUrlsAsync` alcanzable límite 2100 parámetros SQL IN — EF traduce `.Contains()` en IN-clause; SQL Server limita a ~2100 parámetros. Con escalado de fibras podría romperse. Fix futuro: chunking del array [NewsRepository.cs:23] — deferred, MVP scope
- [x] [Review][Defer] `FetchAsync` traga `OperationCanceledException` — `catch (Exception)` captura cancelación; alineado con patrón CancellationToken.None ya diferido [GoogleNewsRssClient.cs:17] — deferred, patrón existente
- [x] [Review][Defer] Rate-limit/bloqueo de Google News silencioso — si Google bloquea la IP, todos los FetchAsync devuelven [], el job reporta saved=0/errors=0 sin alerta diferenciada [NewsPipelineJob.cs] — deferred, operacional
- [x] [Review][Defer] `[DisableConcurrentExecution]` no declarado en `NewsPipelineJob` — Hangfire puede ejecutar instancias superpuestas si la anterior tarda más de un ciclo; el unique index en URL absorbe duplicados pero genera errores espurios [NewsPipelineJob.cs] — deferred, bajo riesgo MVP
- [x] [Review][Defer] Test del pipeline no valida que se emiten las general queries — `FakeRssClient` retorna el mismo set para cualquier query; una regresión que elimine el loop de GeneralQueries no sería detectada [NewsPipelineJobTests.cs] — deferred, cobertura futura

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- `dotnet test tests/Unit/Application.Tests/Application.Tests.csproj --filter NewsDeduplicatorTests` → 10 passed, 0 failed
- `dotnet ef migrations add AddNewsSchema --project src/Server/Infrastructure --startup-project src/Server/Api --configuration Release` → OK
- `dotnet ef database update --project src/Server/Infrastructure --startup-project src/Server/Api` → OK
- `dotnet build FIBRADIS.slnx` → 0 errores, 0 warnings
- `dotnet test tests/Unit/Application.Tests/Application.Tests.csproj` → 26 passed, 0 failed
- `dotnet test FIBRADIS.slnx` → solución en verde; suites con pruebas activas: Application 26, Domain 9, Infrastructure 16, Jobs 2, Api 59
- `dotnet test FIBRADIS.slnx` (re-run con patches de review) → tests funcionales verdes pero el host de pruebas cayó al final por shutdown de Hangfire/EventLog tras OpenAPI; se revalidó por proyectos
- `dotnet test tests/Unit/Application.Tests/Application.Tests.csproj` → 27 passed, 0 failed
- `dotnet test tests/Unit/Infrastructure.Tests/Infrastructure.Tests.csproj` → 18 passed, 0 failed
- `dotnet test tests/Integration/Api.Tests/Api.Tests.csproj` → 61 passed, 0 failed
- `dotnet test tests/Integration/Api.Tests/Api.Tests.csproj --filter NewsBlocklistOpsEndpointTests` → 2 passed, 0 failed
- `dotnet test tests/Integration/Jobs.Tests/Jobs.Tests.csproj` → 2 passed, 0 failed
- `npm run codegen:api` → `src/Web/SharedApiClient/schema.d.ts` regenerado
- `npm run build --workspace=src/Web/Ops` → build OK
- `dotnet test tests/Integration/Api.Tests/Api.Tests.csproj --filter NewsBlocklistOpsEndpointTests --configuration Release` → 3 passed, 0 failed
- `dotnet test tests/Unit/Infrastructure.Tests/Infrastructure.Tests.csproj --configuration Release` → 19 passed, 0 failed
- `dotnet build FIBRADIS.slnx --configuration Release` → 0 errores, 0 warnings
- `npm run build --workspace=src/Web/Ops` → build OK (re-run tras patches finales)

### Completion Notes List

- Implementé el módulo backend de noticias con entidades `NewsArticle` y `BlocklistTerm`, contratos de aplicación, repositorios EF Core, cliente RSS de Google News y job Hangfire horario.
- La deduplicación quedó encapsulada en `NewsDeduplicator`, con normalización de títulos, blocklist contra título/snippet y dedupe exacto por URL más dedupe probable por título normalizado.
- Registré el schema `news`, seed inicial de términos bloqueados y la migración `20260519163418_AddNewsSchema`, aplicada contra `FIBRADIS_Dev`.
- Integré el pipeline al arranque del API vía DI y `RecurringJob.AddOrUpdate<NewsPipelineJob>`.
- Agregué 10 pruebas unitarias para `NewsDeduplicatorTests` y validé la solución completa sin regresiones.
- Resueltos los patches de review activos de la historia: CRUD Ops para blocklist, lookup de URLs limitado al batch actual, warning explícito para `pubDate` inválido y deduplicación segura cuando el título viene vacío.
- La SPA Ops quedó conectada al OpenAPI generado con `openapi-fetch` path-based client para administrar términos sin redespliegue.
- Resueltos los 4 patches pendientes de la segunda pasada de review: el cliente Ops ahora envía `Authorization` usando el access token de AdminOps almacenado en `fibradis.ops.accessToken`, el POST de blocklist valida longitud máxima antes de EF, `GoogleNewsRssClient` loguea también `pubDate` ausente y `newsApi.ts` usa el tipo generado por OpenAPI sin casts redundantes.
- Agregué cobertura de regresión para `400 BadRequest` por términos >256 caracteres y para fallback + warning cuando el RSS omite `pubDate`.

### File List

- `src/Server/Domain/News/NewsArticleStatus.cs`
- `src/Server/Domain/News/NewsArticle.cs`
- `src/Server/Domain/News/BlocklistTerm.cs`
- `src/Server/Application/News/INewsRepository.cs`
- `src/Server/Application/News/IBlocklistRepository.cs`
- `src/Server/Application/News/IRssClient.cs`
- `src/Server/Application/News/NewsDeduplicator.cs`
- `src/Server/SharedApiContracts/News/BlocklistTermDto.cs`
- `src/Server/SharedApiContracts/News/CreateBlocklistTermRequest.cs`
- `src/Server/Infrastructure/Persistence/SqlServer/Configurations/News/NewsArticleConfiguration.cs`
- `src/Server/Infrastructure/Persistence/SqlServer/Configurations/News/BlocklistTermConfiguration.cs`
- `src/Server/Infrastructure/Persistence/SqlServer/AppDbContext.cs`
- `src/Server/Infrastructure/Persistence/Repositories/News/NewsRepository.cs`
- `src/Server/Infrastructure/Persistence/Repositories/News/BlocklistRepository.cs`
- `src/Server/Infrastructure/Persistence/Seeds/NewsSeed.cs`
- `src/Server/Infrastructure/Integrations/GoogleNews/GoogleNewsRssClient.cs`
- `src/Server/Infrastructure/Jobs/News/NewsPipelineJob.cs`
- `src/Server/Infrastructure/Jobs/News/NewsPipelineSchedule.cs`
- `src/Server/Api/Endpoints/Ops/NewsBlocklistEndpoints.cs`
- `src/Server/Infrastructure/Persistence/Migrations/20260519163418_AddNewsSchema.cs`
- `src/Server/Infrastructure/Persistence/Migrations/20260519163418_AddNewsSchema.Designer.cs`
- `src/Server/Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`
- `src/Server/Api/CompositionRoot/ApiServiceExtensions.cs`
- `src/Server/Api/Program.cs`
- `tests/Unit/Application.Tests/News/NewsDeduplicatorTests.cs`
- `tests/Unit/Infrastructure.Tests/Integrations/GoogleNews/GoogleNewsRssClientTests.cs`
- `tests/Unit/Infrastructure.Tests/Jobs/News/NewsPipelineJobTests.cs`
- `tests/Integration/Api.Tests/NewsBlocklistOpsEndpointTests.cs`
- `tests/Integration/Api.Tests/OpenApiEndpointTests.cs`
- `src/Web/Ops/src/api/newsApi.ts`
- `src/Web/Ops/src/main.tsx`
- `src/Web/Ops/src/App.tsx`
- `src/Web/SharedApiClient/schema.d.ts`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

### Change Log

- 2026-05-19 — Implementado pipeline de noticias RSS con blocklist, deduplicación, persistencia EF Core, schedule Hangfire y cobertura unitaria; historia movida a `review`.
- 2026-05-19 — Aplicados patches de code review para historia 4.1: CRUD Ops del blocklist, optimización de dedup por URL, warning para `pubDate` inválido, edge case de títulos vacíos y regeneración de OpenAPI/SPA Ops; historia vuelve a `review`.
- 2026-05-19 — Resueltos los 4 patches pendientes de la segunda pasada de review: auth header en SPA Ops, validación 400 por longitud de blocklist, warning para `pubDate` ausente y tipado OpenAPI sin casts; validado con `dotnet build` Release, `Infrastructure.Tests` (19/19), `NewsBlocklistOpsEndpointTests` (3/3) y build de Ops.
