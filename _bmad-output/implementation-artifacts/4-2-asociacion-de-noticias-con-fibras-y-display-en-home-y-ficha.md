# Historia 4.2: Asociación de noticias con FIBRAs y display en Home y ficha

Status: done

## Story

Como visitante público,
quiero que los items de noticias se vinculen automáticamente a FIBRAs relevantes basándose en coincidencia de ticker y variantes de nombre, y ver las 10 noticias más recientes en la Home y noticias específicas de cada FIBRA en su ficha,
para que pueda encontrar noticias relevantes para FIBRAs específicas sin curación manual.

## Acceptance Criteria

1. **Asociación automática por ticker:** Dado que el título de un item de noticias contiene "FUNO11", cuando se ejecuta el paso de asociación, entonces el item se vincula a FUNO11 y aparece en la sección de noticias de su ficha pública (últimas 10, orden cronológico inverso).

2. **Item sin asociación:** Dado que un item de noticias no contiene ningún ticker ni variante de nombre conocida, entonces tiene cero asociaciones a FIBRAs y aparece en el feed general de la Home pero no en ninguna sección específica de FIBRA.

3. **Feed general de Home:** Dado que veo la página de la Home, cuando carga la sección de noticias, entonces los 10 items más recientes (independientemente de su asociación a FIBRA) aparecen en orden de fecha de publicación.

4. **FIBRA sin noticias:** Dado que una FIBRA no tiene noticias asociadas, entonces la sección de noticias de su ficha muestra un estado vacío claro de "Sin noticias disponibles" — sin error.

## Tasks / Subtasks

- [x] Task 1: Backend — Entidad de asociación + EF Core (AC: #1, #2)
  - [x] 1.1 Crear `src/Server/Domain/News/NewsArticleFibra.cs` — tabla join many-to-many (ver Dev Notes)
  - [x] 1.2 Crear `src/Server/Infrastructure/Persistence/SqlServer/Configurations/News/NewsArticleFibraConfiguration.cs` (ver Dev Notes)
  - [x] 1.3 Actualizar `NewsArticle.cs` — agregar `ICollection<NewsArticleFibra> FibraLinks { get; set; } = [];`
  - [x] 1.4 Agregar `DbSet<NewsArticleFibra> NewsArticleFibras` en `AppDbContext.cs`
  - [x] 1.5 Crear migración: `dotnet ef migrations add AddNewsArticleFibra --project src/Server/Infrastructure --startup-project src/Server/Api --configuration Release`
  - [x] 1.6 Aplicar migración: `dotnet ef database update --project src/Server/Infrastructure --startup-project src/Server/Api`

- [x] Task 2: Backend — Lógica de asociación pura + tests (AC: #1, #2)
  - [x] 2.1 Crear `src/Server/Application/News/NewsAssociator.cs` — estático, sin dependencias (ver Dev Notes — algoritmo completo)
  - [x] 2.2 Crear `tests/Unit/Application.Tests/News/NewsAssociatorTests.cs` — mínimo 7 tests (ver Dev Notes — tests obligatorios)

- [x] Task 3: Backend — Actualizar pipeline para ejecutar asociación (AC: #1, #2)
  - [x] 3.1 Actualizar `INewsRepository` — agregar `AddWithLinksAsync(NewsArticle article, IEnumerable<Guid> fibraIds, CancellationToken ct)` y `GetLatestAsync(int count)` ya existe
  - [x] 3.2 Actualizar `NewsRepository` — implementar `AddWithLinksAsync` (ver Dev Notes)
  - [x] 3.3 Actualizar `NewsPipelineJob.ExecuteAsync` — pasar `fibras` al `NewsAssociator.Associate(item, fibras)` antes de persistir (ver Dev Notes — cambio mínimo)

- [x] Task 4: Backend — Contratos y endpoints públicos (AC: #1, #2, #3, #4)
  - [x] 4.1 Crear `src/Server/SharedApiContracts/News/NewsArticleDto.cs` (ver Dev Notes)
  - [x] 4.2 Actualizar `INewsRepository` — agregar `GetLatestForFibraAsync(Guid fibraId, int count, CancellationToken ct)` y `GetLatestAsync` que acepta count
  - [x] 4.3 Actualizar `NewsRepository` — implementar `GetLatestForFibraAsync` (ver Dev Notes)
  - [x] 4.4 Crear `src/Server/Api/Endpoints/Public/NewsEndpoints.cs` con dos rutas (ver Dev Notes — endpoints)
  - [x] 4.5 Registrar `app.MapNews()` en `Program.cs` (patrón igual que los otros `Map*` ya registrados)
  - [x] 4.6 Ejecutar `npm run codegen:api` desde raíz — regenerar `SharedApiClient/schema.d.ts`

- [x] Task 5: Frontend — Home NewsSection (AC: #3, #4)
  - [x] 5.1 Crear `src/Web/Main/src/api/newsApi.ts` — función `fetchLatestNews()` usando el cliente generado
  - [x] 5.2 Actualizar `src/Web/Main/src/modules/home/NewsSection.tsx` — reemplazar el placeholder actual con datos reales (ver Dev Notes — componente)

- [x] Task 6: Frontend — NoticiasSection en ficha pública (AC: #1, #2, #4)
  - [x] 6.1 Crear `src/Web/Main/src/api/fibraNewsApi.ts` — función `fetchFibraNews(ticker: string)` usando el cliente generado
  - [x] 6.2 Actualizar `src/Web/Main/src/modules/ficha-publica/sections/NoticiasSection.tsx` — reemplazar placeholder con datos reales (ver Dev Notes — componente)

- [x] Task 7: Backend — Build y tests (AC: todos)
  - [x] 7.1 `dotnet build FIBRADIS.slnx` — 0 errores
  - [x] 7.2 `dotnet test tests/Unit/Application.Tests/Application.Tests.csproj` — todos los tests pasan incluyendo `NewsAssociatorTests`

## Dev Notes

### Contexto de historia anterior (4.1)

Historia 4.1 estableció:
- `NewsArticle` en `src/Server/Domain/News/NewsArticle.cs` — sin relación con Fibra todavía
- `INewsRepository` en `src/Server/Application/News/INewsRepository.cs` — tiene `AddAsync`, `GetLatestAsync(count)`, `GetExistingUrlsAsync`, `GetRecentNormalizedTitlesAsync`
- `NewsRepository` en `src/Server/Infrastructure/Persistence/Repositories/News/NewsRepository.cs` — implementación concreta
- `NewsPipelineJob` en `src/Server/Infrastructure/Jobs/News/NewsPipelineJob.cs` — ya tiene `fibras` disponible al ejecutar; necesita pasar esa lista a `NewsAssociator`
- Configuración EF: `NewsArticleConfiguration.cs` — tabla `news.NewsArticle`, índice único en `url`
- `BlocklistEndpoints` registrado en `Program.cs` como `app.MapNewsBlocklist()`
- `NewsSection.tsx` (Home) — placeholder puro, listo para reemplazar
- `NoticiasSection.tsx` (ficha pública) — placeholder puro, listo para reemplazar

**CRÍTICO**: `NewsPipelineJob` llama `newsRepo.AddAsync(article, ct)` para cada item. Esta historia cambia esa llamada por `newsRepo.AddWithLinksAsync(article, fibraIds, ct)`. El `AddAsync` original puede quedar como fallback o eliminarse — eliminar si no hay otro uso.

---

### Entidad NewsArticleFibra (join table)

```csharp
// src/Server/Domain/News/NewsArticleFibra.cs
namespace Domain.News;

public class NewsArticleFibra
{
    public Guid NewsArticleId { get; set; }
    public Guid FibraId { get; set; }
    public NewsArticle NewsArticle { get; set; } = null!;
}
```

Nota: No se navega desde Fibra hacia NewsArticleFibra — el módulo News NO importa ni referencia `Domain.Catalog`. La relación es unidireccional: `NewsArticleFibra` tiene `FibraId` como FK sin propiedad de navegación hacia `Fibra`, para respetar la regla de módulos.

---

### EF Core Configuration — NewsArticleFibraConfiguration

```csharp
// src/Server/Infrastructure/Persistence/SqlServer/Configurations/News/NewsArticleFibraConfiguration.cs
namespace Infrastructure.Persistence.SqlServer.Configurations.News;

public class NewsArticleFibraConfiguration : IEntityTypeConfiguration<NewsArticleFibra>
{
    public void Configure(EntityTypeBuilder<NewsArticleFibra> builder)
    {
        builder.ToTable("NewsArticleFibra", schema: "news");
        builder.HasKey(x => new { x.NewsArticleId, x.FibraId });

        builder.Property(x => x.NewsArticleId).HasColumnName("news_article_id");
        builder.Property(x => x.FibraId).HasColumnName("fibra_id");

        builder.HasOne(x => x.NewsArticle)
            .WithMany(a => a.FibraLinks)
            .HasForeignKey(x => x.NewsArticleId)
            .OnDelete(DeleteBehavior.Cascade);

        // NO foreign key hacia catalog.Fibra — módulos separados
        builder.HasIndex(x => x.FibraId).HasDatabaseName("IX_NewsArticleFibra_FibraId");
    }
}
```

Registrar en `AppDbContext.OnModelCreating`:
```csharp
modelBuilder.ApplyConfiguration(new NewsArticleFibraConfiguration());
```
Y agregar `DbSet<NewsArticleFibra> NewsArticleFibras { get; set; }`.

---

### Algoritmo NewsAssociator (lógica pura)

```csharp
// src/Server/Application/News/NewsAssociator.cs
namespace Application.News;

public static class NewsAssociator
{
    // Devuelve IDs de FIBRAs cuyo ticker o alguna variante de nombre
    // aparece en el título o snippet del item (case-insensitive, substring match).
    public static IReadOnlyList<Guid> Associate(
        RssItem item,
        IReadOnlyList<FibraMatchInfo> fibras)
    {
        if (fibras.Count == 0) return [];

        var haystack = BuildHaystack(item.Title, item.Snippet);
        var matches = new List<Guid>();

        foreach (var fibra in fibras)
        {
            if (Matches(haystack, fibra.Ticker))
            {
                matches.Add(fibra.Id);
                continue;
            }
            if (fibra.NameVariants.Any(v => Matches(haystack, v)))
                matches.Add(fibra.Id);
        }

        return matches;
    }

    private static string BuildHaystack(string title, string? snippet)
        => $"{title} {snippet ?? string.Empty}".ToLowerInvariant();

    private static bool Matches(string haystack, string term)
        => !string.IsNullOrWhiteSpace(term) &&
           haystack.Contains(term.ToLowerInvariant(), StringComparison.Ordinal);
}
```

`FibraMatchInfo` es un record ligero para no pasar la entidad `Catalog.Fibra` al módulo News:

```csharp
// src/Server/Application/News/FibraMatchInfo.cs
namespace Application.News;

public sealed record FibraMatchInfo(Guid Id, string Ticker, IReadOnlyList<string> NameVariants);
```

**Principio de módulos**: `NewsAssociator` no importa `Domain.Catalog`. El pipeline le pasa `FibraMatchInfo` construido desde `Fibra` — esa conversión ocurre en `NewsPipelineJob` (Infrastructure, donde ambas capas son accesibles).

---

### Tests obligatorios — NewsAssociatorTests.cs

```
tests/Unit/Application.Tests/News/NewsAssociatorTests.cs
```

**7 tests mínimos:**

1. `Associate_MatchesByTickerInTitle` → item con "FUNO11" en título, fibra con ticker "FUNO11" → ID incluido
2. `Associate_MatchesByTickerInSnippet` → ticker en snippet, no en título → ID incluido
3. `Associate_MatchesByNameVariant` → ticker no aparece, pero variante "Fibra Uno" sí → ID incluido
4. `Associate_IsCaseInsensitive` → título con "funo11" (minúsculas), ticker "FUNO11" → ID incluido
5. `Associate_NoMatchReturnsEmpty` → title/snippet sin ningún ticker ni variante → resultado vacío
6. `Associate_MultipleMatches` → título con "FUNO11" y "DANHOS13" → ambos IDs incluidos
7. `Associate_TickerSubstringDoesNotFalsePositive` — **IMPORTANTE**: ticker "UN" no debe matchear "FUNO11". Usar un ticker corto y verificar que coincidencia de substring funciona como se espera. Si se permite substring libre, "UN" matcheará "FUNO11" — documentar la decisión: el proyecto acepta substring match para variantes pero los tickers cortos deben ser considerados. Implementar como está, documentar comportamiento esperado en el test.

---

### Actualización de NewsPipelineJob

Cambios mínimos en `NewsPipelineJob.ExecuteAsync`:

```csharp
// 1. Al inicio, convertir fibras a FibraMatchInfo
var fibraMatchInfos = fibras
    .Select(f => new FibraMatchInfo(f.Id, f.Ticker, f.NameVariants.AsReadOnly()))
    .ToList();

// 2. En el loop de guardado, reemplazar AddAsync por AddWithLinksAsync:
var fibraIds = NewsAssociator.Associate(item, fibraMatchInfos);
var article = new NewsArticle { ... }; // igual que antes
await newsRepo.AddWithLinksAsync(article, fibraIds, ct);
```

El `AddAsync` existente en `INewsRepository` puede mantenerse o eliminarse según si hay otros consumidores. En este proyecto no hay otros consumidores — se puede remover para mantener la interfaz limpia. **Verificar antes de eliminar que no hay otros usos con grep**.

---

### INewsRepository — métodos nuevos/actualizados

```csharp
// Agregar a Application.News.INewsRepository:
Task AddWithLinksAsync(NewsArticle article, IEnumerable<Guid> fibraIds, CancellationToken ct = default);
Task<IReadOnlyList<NewsArticle>> GetLatestForFibraAsync(Guid fibraId, int count, CancellationToken ct = default);
```

El método `GetLatestAsync(int count)` ya existe — se reutiliza tal cual para el endpoint de Home.

---

### NewsRepository — implementaciones nuevas

```csharp
public async Task AddWithLinksAsync(NewsArticle article, IEnumerable<Guid> fibraIds, CancellationToken ct = default)
{
    db.NewsArticles.Add(article);

    foreach (var fibraId in fibraIds)
    {
        db.NewsArticleFibras.Add(new NewsArticleFibra
        {
            NewsArticleId = article.Id,
            FibraId = fibraId,
        });
    }

    await db.SaveChangesAsync(ct);
}

public async Task<IReadOnlyList<NewsArticle>> GetLatestForFibraAsync(
    Guid fibraId, int count, CancellationToken ct = default)
    => await db.NewsArticleFibras
        .Where(l => l.FibraId == fibraId)
        .Select(l => l.NewsArticle)
        .Where(a => a.Status == NewsArticleStatus.Pending || a.Status == NewsArticleStatus.Processed)
        .OrderByDescending(a => a.PublishedAt)
        .Take(count)
        .ToListAsync(ct);
```

---

### SharedApiContracts — NewsArticleDto

```csharp
// src/Server/SharedApiContracts/News/NewsArticleDto.cs
namespace SharedApiContracts.News;

public sealed record NewsArticleDto(
    Guid Id,
    string Title,
    string Source,
    DateTimeOffset PublishedAt,
    string Url,
    string? Snippet
);
```

---

### Endpoints públicos de noticias

```csharp
// src/Server/Api/Endpoints/Public/NewsEndpoints.cs
namespace Api.Endpoints.Public;

public static class NewsEndpoints
{
    public static IEndpointRouteBuilder MapNews(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/news").WithTags("News");

        // Feed general — para Home (últimas 10)
        group.MapGet("/", async (INewsRepository newsRepo, CancellationToken ct) =>
        {
            var articles = await newsRepo.GetLatestAsync(10, ct);
            return Results.Ok(articles.Select(ToDto).ToList());
        })
        .AllowAnonymous()
        .Produces<IReadOnlyList<NewsArticleDto>>(StatusCodes.Status200OK);

        // Noticias por FIBRA — para ficha pública (últimas 10)
        group.MapGet("/fibras/{fibraId:guid}", async (
            Guid fibraId,
            INewsRepository newsRepo,
            CancellationToken ct) =>
        {
            var articles = await newsRepo.GetLatestForFibraAsync(fibraId, 10, ct);
            return Results.Ok(articles.Select(ToDto).ToList());
        })
        .AllowAnonymous()
        .Produces<IReadOnlyList<NewsArticleDto>>(StatusCodes.Status200OK);

        return app;
    }

    private static NewsArticleDto ToDto(NewsArticle a) =>
        new(a.Id, a.Title, a.Source, a.PublishedAt, a.Url, a.Snippet);
}
```

**Nota sobre la ruta de noticias por FIBRA**: El frontend tiene acceso al `ticker` de la FIBRA, no necesariamente al `Id`. La ficha pública (`/fibras/{ticker}`) usa el ticker. Dos opciones:
- Opción A (elegida): exponer `/api/v1/news/fibras/{fibraId:guid}` — el frontend llama primero a `/api/v1/fibras/{ticker}` que ya devuelve el `Id`, y lo reutiliza.
- Opción B: exponer `/api/v1/fibras/{ticker}/news` — más REST pero requiere que `NewsRepository` haga un JOIN con `catalog.Fibra` para resolver ticker → Id, lo que cruza límites de módulo.

**Opción A es la correcta** por regla de módulos. El frontend ya tiene el `fibraId` del detalle de FIBRA.

---

### Registro en Program.cs

Agregar junto a los otros `Map*`:
```csharp
app.MapNews();
```

---

### Frontend — NewsSection.tsx (Home)

Reemplazar el componente actual (`src/Web/Main/src/modules/home/NewsSection.tsx`) que tiene un placeholder `animate-pulse` con datos reales. Patron: igual que `TopMoversSection` o `PriceCarousel` que usan `useQuery` de TanStack Query.

```tsx
// src/Web/Main/src/api/newsApi.ts
import { client } from '@/lib/apiClient'; // o el import que use el proyecto — revisar otros archivos api/*.ts

export async function fetchLatestNews() {
  return client.GET('/api/v1/news');
}
```

El componente usa `useQuery`, muestra skeleton mientras carga, lista de artículos al éxito, y estado vacío si no hay noticias.

**Estructura de cada item de noticias en la lista**:
- Título (enlace externo que abre en nueva pestaña con `rel="noopener noreferrer"`)
- Fuente + fecha relativa (ej. "El Financiero · hace 2h")
- Snippet si está disponible (truncado a 2 líneas con `line-clamp-2`)

**Verificar el import del cliente API** mirando cómo lo importan otros archivos en `src/Web/Main/src/api/` antes de crear `newsApi.ts`.

---

### Frontend — NoticiasSection.tsx (ficha pública)

La ficha pública está en `src/Web/Main/src/modules/ficha-publica/`. El componente recibe el `fibraId` (Guid) que ya está disponible en el contexto de la ficha — verificar cómo lo pasan los otros componentes de sección (ej. `PriceSection.tsx`, `HistorialSection.tsx`) para seguir el mismo patrón.

```tsx
// src/Web/Main/src/api/fibraNewsApi.ts
export async function fetchFibraNews(fibraId: string) {
  return client.GET('/api/v1/news/fibras/{fibraId}', { params: { path: { fibraId } } });
}
```

El componente muestra:
- Estado vacío: "Sin noticias disponibles" cuando `articles.length === 0`
- Lista de hasta 10 artículos con título (enlace), fuente y fecha
- Skeleton durante carga

---

### Verificaciones antes de implementar

1. **Grep `AddAsync`** en `INewsRepository` y sus usos — si solo `NewsPipelineJob` lo usa, eliminar tras reemplazar por `AddWithLinksAsync`
2. **Verificar import del cliente API** en `src/Web/Main/src/api/` — mirar `marketApi.ts` u otro archivo existente
3. **Verificar cómo ficha pública pasa el `fibraId`** — revisar el componente padre o el hook que carga los datos de la ficha

---

### Convenciones relevantes

- Schema SQL: `news` (igual que historia 4.1)
- Tabla nueva: `news.NewsArticleFibra` — PK compuesta `(news_article_id, fibra_id)`
- API: rutas bajo `/api/v1/news/` — sin cruces de módulo con `catalog`
- Módulos: `Application.News` no referencia `Domain.Catalog` — usar `FibraMatchInfo` como puente
- TypeScript: componentes `PascalCase.tsx`, hooks `use*.ts`, api utils `kebab-case.ts`
- No FK de BD desde `news` schema hacia `catalog` schema — solo `FibraId` como Guid sin constraint de FK

---

### Estado inicial para tests unitarios

Los tests de `NewsAssociatorTests` son puramente en memoria — no requieren BD ni DI. El patrón exacto a seguir está en `tests/Unit/Application.Tests/News/NewsDeduplicatorTests.cs` (creado en 4.1).

---

## Dev Agent Record

### Decisiones tomadas
- `NewsAssociator` normaliza título y snippet con la misma rutina de deduplicación para hacer matching case-insensitive y tolerante a diacríticos.
- Los tickers y las variantes de nombre se matchean por token/frase completa delimitada en el haystack normalizado para evitar falsos positivos por substrings embebidos.
- La ruta pública de noticias por FIBRA quedó en `/api/v1/news/fibras/{fibraId}` para no cruzar el límite de módulo `news -> catalog`.

### Archivos modificados
- `_bmad-output/implementation-artifacts/4-2-asociacion-de-noticias-con-fibras-y-display-en-home-y-ficha.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `scripts/codegen/Api.json`
- `src/Server/Api/Endpoints/Public/NewsEndpoints.cs`
- `src/Server/Api/Program.cs`
- `src/Server/Application/News/FibraMatchInfo.cs`
- `src/Server/Application/News/INewsRepository.cs`
- `src/Server/Application/News/NewsAssociator.cs`
- `src/Server/Domain/News/NewsArticle.cs`
- `src/Server/Domain/News/NewsArticleFibra.cs`
- `src/Server/Infrastructure/Jobs/News/NewsPipelineJob.cs`
- `src/Server/Infrastructure/Persistence/Migrations/20260519185413_AddNewsArticleFibra.cs`
- `src/Server/Infrastructure/Persistence/Migrations/20260519185413_AddNewsArticleFibra.Designer.cs`
- `src/Server/Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`
- `src/Server/Infrastructure/Persistence/Repositories/News/NewsRepository.cs`
- `src/Server/Infrastructure/Persistence/SqlServer/AppDbContext.cs`
- `src/Server/Infrastructure/Persistence/SqlServer/Configurations/News/NewsArticleFibraConfiguration.cs`
- `src/Server/SharedApiContracts/News/NewsArticleDto.cs`
- `src/Web/Main/src/api/fibraNewsApi.ts`
- `src/Web/Main/src/api/newsApi.ts`
- `src/Web/Main/src/modules/ficha-publica/FibraPage.tsx`
- `src/Web/Main/src/modules/ficha-publica/sections/NoticiasSection.tsx`
- `src/Web/Main/src/modules/home/NewsSection.tsx`
- `src/Web/SharedApiClient/schema.d.ts`
- `tests/Unit/Application.Tests/News/NewsAssociatorTests.cs`
- `tests/Unit/Infrastructure.Tests/Jobs/News/NewsPipelineJobTests.cs`

### Tests ejecutados
- `dotnet test tests/Unit/Application.Tests/Application.Tests.csproj --filter NewsAssociatorTests` → 7 passed, 0 failed
- `dotnet ef migrations add AddNewsArticleFibra --project src/Server/Infrastructure --startup-project src/Server/Api --configuration Release` → migración generada
- `dotnet ef database update --project src/Server/Infrastructure --startup-project src/Server/Api` → migración aplicada
- `dotnet test tests/Unit/Infrastructure.Tests/Infrastructure.Tests.csproj --filter NewsPipelineJobTests` → 1 passed, 0 failed
- `dotnet build FIBRADIS.slnx` → 0 errores
- `npm run codegen:api` → `src/Web/SharedApiClient/schema.d.ts` regenerado
- `npm run build --workspace=src/Web/Main` → build OK
- `dotnet test tests/Unit/Application.Tests/Application.Tests.csproj` → 34 passed, 0 failed
- `dotnet test FIBRADIS.slnx --no-build` → suites OK; proyectos sin casos (`ApiCompatibility.Tests`, `Integrations.Tests`, `Persistence.Tests`) reportaron “No hay ninguna prueba disponible”
- `dotnet test tests/Unit/Application.Tests/Application.Tests.csproj --filter NewsAssociatorTests` → 8 passed, 0 failed
- `dotnet build FIBRADIS.slnx --configuration Release` → 0 errores
- `npm run lint --workspace=src/Web/Main` → OK
- `npm test --workspace=src/Web/Main` → 35 passed, 0 failed
- `npm run build --workspace=src/Web/Main` → build OK

### Completion Notes
- Se agregó la tabla join `news.NewsArticleFibra`, su configuración EF y la migración `AddNewsArticleFibra`.
- El pipeline de noticias ahora asocia automáticamente items RSS a FIBRAs por ticker/variantes y persiste los links junto con el artículo.
- Se expusieron endpoints públicos para feed general y noticias por FIBRA, y se regeneró el cliente OpenAPI compartido.
- La Home y la ficha pública dejaron de usar placeholders y ahora muestran noticias reales con loading, error y empty state.
- Se corrigió el hallazgo de review pendiente en `MatchesVariant` para evitar asociaciones falsas por substrings embebidos y se agregó test de regresión.
- Se corrigió el hallazgo pendiente de XSS en links de noticias validando que solo se rendericen `href` con scheme `http/https`; URLs inválidas degradan a texto plano.
- Para dejar el workspace listo para `review`, se limpió deuda de lint preexistente en `ReportesSection` y `button.tsx` sin cambiar comportamiento.

## File List

- `_bmad-output/implementation-artifacts/4-2-asociacion-de-noticias-con-fibras-y-display-en-home-y-ficha.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `scripts/codegen/Api.json`
- `src/Server/Api/Endpoints/Public/NewsEndpoints.cs`
- `src/Server/Api/Program.cs`
- `src/Server/Application/News/FibraMatchInfo.cs`
- `src/Server/Application/News/INewsRepository.cs`
- `src/Server/Application/News/NewsAssociator.cs`
- `src/Server/Domain/News/NewsArticle.cs`
- `src/Server/Domain/News/NewsArticleFibra.cs`
- `src/Server/Infrastructure/Jobs/News/NewsPipelineJob.cs`
- `src/Server/Infrastructure/Persistence/Migrations/20260519185413_AddNewsArticleFibra.cs`
- `src/Server/Infrastructure/Persistence/Migrations/20260519185413_AddNewsArticleFibra.Designer.cs`
- `src/Server/Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`
- `src/Server/Infrastructure/Persistence/Repositories/News/NewsRepository.cs`
- `src/Server/Infrastructure/Persistence/SqlServer/AppDbContext.cs`
- `src/Server/Infrastructure/Persistence/SqlServer/Configurations/News/NewsArticleFibraConfiguration.cs`
- `src/Server/SharedApiContracts/News/NewsArticleDto.cs`
- `src/Web/Main/src/api/fibraNewsApi.ts`
- `src/Web/Main/src/api/newsApi.ts`
- `src/Web/Main/package.json`
- `src/Web/Main/src/modules/ficha-publica/FibraPage.tsx`
- `src/Web/Main/src/modules/ficha-publica/sections/NoticiasSection.tsx`
- `src/Web/Main/src/modules/ficha-publica/sections/ReportesSection.tsx`
- `src/Web/Main/src/modules/home/NewsSection.tsx`
- `src/Web/Main/src/shared/lib/safe-external-url.test.ts`
- `src/Web/Main/src/shared/lib/safe-external-url.ts`
- `src/Web/Main/src/shared/ui/button-variants.ts`
- `src/Web/Main/src/shared/ui/button.tsx`
- `src/Web/SharedApiClient/schema.d.ts`
- `tests/Unit/Application.Tests/News/NewsAssociatorTests.cs`
- `tests/Unit/Infrastructure.Tests/Jobs/News/NewsPipelineJobTests.cs`

## Change Log

- 2026-05-19: Implementada la historia 4.2 con asociación automática de noticias a FIBRAs, endpoints públicos de noticias y consumo real en Home/ficha pública.
- 2026-05-19: Corregido hallazgo de review en `NewsAssociator` para variantes de nombre con falso positivo por substring.
- 2026-05-19: Corregido hallazgo de review por XSS en links de noticias y limpiada deuda de lint del workspace frontend.

## Senior Developer Review (AI)

### Review Findings

- [x] [Review][Patch] `MatchesVariant` sin word-boundary — falsos positivos al matchear substrings [`src/Server/Application/News/NewsAssociator.cs:MatchesVariant`] — Resuelto con delimitadores de frase en el haystack normalizado y test de regresión `Associate_NameVariantSubstringDoesNotFalsePositive`.

- [x] [Review][Defer] `GetLatestForFibraAsync` NullReferenceException teórico por filas huérfanas [`NewsRepository.cs:58-65`] — deferred, pre-existing; FK+cascade hace el caso imposible en práctica
- [x] [Review][Defer] AC2 sin test de integración explícito que verifique artículo sin asociación aparece en feed general [`NewsEndpoints.cs`] — deferred, lógica cubierta por `Associate_NoMatchReturnsEmpty`; test e2e es mejora futura
- [x] [Review][Defer] `JSON.stringify(error)` en mensajes de throw puede incluir detalles internos [`newsApi.ts`, `fibraNewsApi.ts`] — deferred, no llega al usuario final; patrón pre-existing en `fibrasApi.ts`

### Review Findings — 2ª pasada (2026-05-19)

- [x] [Review][Patch] XSS: `article.url` renderizado en `href` sin validación de scheme [`NewsSection.tsx`, `NoticiasSection.tsx`] — Resuelto con `getSafeExternalUrl()` y test de regresión para aceptar solo `http/https`

- [x] [Review][Defer] DbContext en estado sucio tras `SaveChangesAsync` fallido cascadea error a artículos subsecuentes del mismo batch [`NewsRepository.cs:AddWithLinksAsync`] — deferred, pre-existing; mismo patrón del AddAsync de story 4.1 ya deferido como "N round-trips + unique constraint"
- [x] [Review][Defer] Variante de nombre que normaliza a ≤2 chars matchea cualquier artículo con ese token [`NewsAssociator.cs:MatchesVariant`] — deferred, teórico; datos actuales de FIBRAs tienen tickers 6+ chars y variantes multi-word
