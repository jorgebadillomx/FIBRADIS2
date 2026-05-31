# Story 4.11: Página de Listado de Noticias con Búsqueda y Paginación

Status: done

## Story

As a visitante público del sitio,
I want a página `/noticias` con listado paginado, buscador de texto y filtro por FIBRA,
so that puedo explorar y filtrar el corpus de noticias sin cargar miles de registros de golpe.

## Acceptance Criteria

1. **Ruta activa**: navegar a `/noticias` muestra la nueva página de listado (actualmente cae en NotFound).
2. **Paginación**: la página muestra 20 noticias por defecto, ordenadas por `publishedAt DESC`; hay controles de página anterior / siguiente y número de página actual; al cambiar de página el scroll regresa al top.
3. **Buscador**: hay un input de texto con debounce de 300 ms; al escribir se filtra por coincidencia en `title`; al limpiar el campo regresa el listado sin filtro; el cambio de texto resetea a página 1.
4. **Filtro por FIBRA**: hay un dropdown con todas las FIBRAs activas del catálogo; seleccionar una FIBRA filtra las noticias a las que están asociadas a ella; limpiar el dropdown muestra todas; el cambio de filtro resetea a página 1.
5. **Cards**: cada card muestra: título, fuente, fecha relativa (formato existente `formatRelativeTime`), snippet o `aiAnalysis.headline` (lo que esté disponible), imagen og si existe, y chips de los tickers de FIBRAs vinculadas (máx. 3, resto "+N más").
6. **Skeleton**: mientras carga muestra un skeleton de 6 cards.
7. **Estado vacío**: si la búsqueda no arroja resultados, se muestra mensaje "Sin resultados para «query»" con botón para limpiar filtros.
8. **Navegación hacia detalle**: hacer click en una card navega a `/noticias/:id` (ya existe `NoticiaPage`).
9. **Link de menú corregido**: el ítem "Noticias" del nav público apunta a `/noticias`.
10. **Endpoint paginado**: el backend expone `GET /api/v1/news/paged?pageNumber=1&pageSize=20&q=texto&fibraId=uuid` que devuelve `{ items, total, page, pageSize }`; solo retorna artículos con `Status = Processed` y `DeletedAt IS NULL`.
11. **Sin N+1**: los tickers de FIBRAs vinculadas se cargan en un batch único por página (no una consulta por artículo).
12. **Unit tests**: al menos 4 tests del nuevo método de repositorio `GetPagedPublicAsync` (happy path, búsqueda con q, filtro fibraId, página vacía).

## Tasks / Subtasks

- [x] T1 — Backend: método de repositorio paginado público (AC: 10, 11, 12)
  - [x] T1.1 Agregar `GetPagedPublicAsync(int page, int pageSize, string? q, Guid? fibraId, CancellationToken ct)` a `INewsRepository`
  - [x] T1.2 Implementar en `NewsRepository` con batch-load de tickers (ver Dev Notes §Backend)
  - [x] T1.3 Unit tests del nuevo método (4 casos mínimo)

- [x] T2 — Backend: nuevo endpoint público (AC: 10)
  - [x] T2.1 Agregar `NewsPagedResultDto` a `SharedApiContracts/News/`
  - [x] T2.2 Agregar `MapGet("/paged", ...)` en `NewsEndpoints.cs`
  - [x] T2.3 Correr `dotnet build FIBRADIS.slnx` — 0 errores

- [x] T3 — Frontend: regenerar cliente API (AC: 10)
  - [x] T3.1 `npm run codegen:api` desde raíz del repo

- [x] T4 — Frontend: función API y página listado (AC: 1–9)
  - [x] T4.1 Agregar `fetchNewsPaged` en `src/Web/Main/src/api/newsApi.ts`
  - [x] T4.2 Crear `src/Web/Main/src/modules/noticias/NoticiasListPage.tsx` (ver Dev Notes §Frontend)
  - [x] T4.3 Agregar ruta `/noticias` en `src/Web/Main/src/app/routes.tsx` (antes de `/noticias/:id`)
  - [x] T4.4 Corregir link "Noticias" en el nav (buscar `PublicLayout.tsx` o el componente de header)

- [ ] T5 — Verificación final
  - [x] T5.1 `dotnet test tests/Unit/` — todos pasan
  - [ ] T5.2 `npm run dev:main` — verificar `/noticias` carga, busca, filtra y pagina correctamente

### Review Findings

**Patches** (6 — ninguno bloqueante de AC como bug crítico, pero 3 de severidad media):

- [x] \[Review\]\[Patch\] P1 — `LinkedFibraDto.id` siempre `Guid.Empty` en el listado paginado — el batch query en `GetPagedPublicAsync` solo proyecta `Ticker`; añadir `FibraId` en la proyección del Join y pasarlo a `LinkedFibraDto` [`NewsRepository.cs:GetPagedPublicAsync`, `NewsEndpoints.cs:ToDtoWithTickerNames`]
- [x] \[Review\]\[Patch\] P2 — AC5: Cards no muestran imagen og — añadir `<img>` condicional con `getArticleImageUrl` en el loop de articles, siguiendo el patrón de `NewsSection.tsx` [`NoticiasListPage.tsx:~170`]
- [x] \[Review\]\[Patch\] P3 — SEO: faltan meta tags `og:title`, `og:description`, `og:type` — añadir junto a los `<title>` y `<meta name="description">` existentes (checklist convenciones para rutas públicas) [`NoticiasListPage.tsx:~62`]
- [x] \[Review\]\[Patch\] P4 — Skeleton flash en cada cambio de página — mostrar skeleton solo en `isLoading`, usar `isFetching` para indicador sutil (spinner/opacity) sin reemplazar el grid [`NoticiasListPage.tsx:137`]
- [x] \[Review\]\[Patch\] P5 — AC5: snippet vacío (`""`) bloquea `aiAnalysis.headline` — cambiar `article.snippet ?? article.aiAnalysis?.headline` por `(article.snippet?.trim() || article.aiAnalysis?.headline) ?? null` [`NoticiasListPage.tsx:164`]
- [x] \[Review\]\[Patch\] P6 — `toCount` no guarda contra `NaN` — añadir `Number.isNaN(n) ? 0 : n` en la función [`NoticiasListPage.tsx:14`]

**Defers** (7 — pre-existing o edge cases no alcanzables desde la UI):

- [x] \[Review\]\[Defer\] D1 — Page reset timing con debounce: ventana de 300ms genera un fetch extra con query antigua al limpiar filtros [`NoticiasListPage.tsx`] — deferred, comportamiento inherente al patrón debounce; no afecta estado final correcto
- [x] \[Review\]\[Defer\] D2 — `LIKE '%q%'` sin índice ni garantía de collation case-insensitive — pre-existente también en `GetPagedForOpsAsync` [`NewsRepository.cs`] — deferred, pre-existing
- [x] \[Review\]\[Defer\] D3 — `CountAsync` + `ToListAsync` sin snapshot isolation — patrón EF Core pre-existente en todo el proyecto [`NewsRepository.cs`] — deferred, pre-existing
- [x] \[Review\]\[Defer\] D4 — `fetchAllFibras` trunca silenciosamente a 100 FIBRAs — limitación pre-existente del endpoint del catálogo [`fibrasApi.ts`] — deferred, pre-existing; actualmente solo hay 6 FIBRAs en el sistema
- [x] \[Review\]\[Defer\] D5 — `fetchAllFibras` singleton a nivel de módulo vs factory pattern — inconsistencia pre-existente en `fibrasApi.ts` [`fibrasApi.ts`] — deferred, pre-existing
- [x] \[Review\]\[Defer\] D6 — Batch de tickers excluye FIBRAs inactivas pero filtro `fibraId` no lo hace — no alcanzable desde la UI (el dropdown solo muestra FIBRAs activas per AC4) [`NewsRepository.cs:GetPagedPublicAsync`] — deferred, edge case no alcanzable desde UI
- [x] \[Review\]\[Defer\] D7 — Test para fibraId inactiva ausente — AC12 ya cumplido con 5 tests (mínimo era 4) [`NewsRepositoryPublicPagedTests.cs`] — deferred, cobertura adicional más allá del mínimo requerido

## Dev Notes

### Contexto

La ruta `/noticias` actualmente no existe en `routes.tsx`; el nav tiene un link que cae en `NotFound`. El único endpoint público de noticias que devuelve listas es `GET /api/v1/news/` que retorna hardcoded las últimas 5 sin paginación ni filtros. El patrón paginado ya está implementado en Ops (`GetPagedForOpsAsync`) — esta historia lo replica para uso público.

No hay migración de BD. No hay nuevas columnas. Es puro backend query + frontend nuevo.

---

### Backend — `GetPagedPublicAsync`

**Archivo**: `src/Server/Infrastructure/Persistence/Repositories/News/NewsRepository.cs`
**Interfaz**: `src/Server/Application/News/INewsRepository.cs`

Firma a agregar en la interfaz:

```csharp
Task<(IReadOnlyList<NewsArticle> Items, int Total, IReadOnlyDictionary<Guid, IReadOnlyList<string>> TickersByArticleId)>
    GetPagedPublicAsync(int page, int pageSize, string? q, Guid? fibraId, CancellationToken ct = default);
```

Implementación (modelo a seguir de `GetPagedForOpsAsync`):

```csharp
public async Task<(IReadOnlyList<NewsArticle> Items, int Total, IReadOnlyDictionary<Guid, IReadOnlyList<string>> TickersByArticleId)>
    GetPagedPublicAsync(int page, int pageSize, string? q, Guid? fibraId, CancellationToken ct = default)
{
    var query = _db.NewsArticles
        .Where(a => a.DeletedAt == null && a.Status == NewsArticleStatus.Processed);

    if (!string.IsNullOrWhiteSpace(q))
        query = query.Where(a => EF.Functions.Like(a.Title, $"%{q}%"));

    if (fibraId.HasValue)
        query = query.Where(a => a.FibraLinks.Any(l => l.FibraId == fibraId.Value));

    var total = await query.CountAsync(ct);

    var items = await query
        .OrderByDescending(a => a.PublishedAt)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync(ct);

    // Batch-load fibra tickers — una sola consulta extra, sin N+1
    var ids = items.Select(a => a.Id).ToList();
    var links = await _db.NewsArticleFibras
        .Where(l => ids.Contains(l.NewsArticleId) && l.Fibra.DeletedAt == null)
        .Select(l => new { l.NewsArticleId, l.Fibra.Ticker })
        .ToListAsync(ct);

    var tickerMap = links
        .GroupBy(l => l.NewsArticleId)
        .ToDictionary(
            g => g.Key,
            g => (IReadOnlyList<string>)g.Select(l => l.Ticker).ToList());

    return (items, total, tickerMap);
}
```

> **Nota**: `NewsArticleFibra` tiene FK `FibraId` y `NewsArticleId`. La propiedad de navegación en `NewsArticle` es `FibraLinks`. Confirmar nombres exactos en `NewsArticleFibra.cs` antes de escribir el query; si la entidad usa `FibraId` vs `Fibra.Id`, ajustar.

---

### Backend — `NewsPagedResultDto`

**Archivo nuevo**: `src/Server/SharedApiContracts/News/NewsPagedResultDto.cs`

```csharp
namespace SharedApiContracts.News;

public sealed record NewsPagedResultDto(
    IReadOnlyList<NewsArticleDto> Items,
    int Total,
    int Page,
    int PageSize
);
```

---

### Backend — Endpoint `GET /api/v1/news/paged`

**Archivo**: `src/Server/Api/Endpoints/Public/NewsEndpoints.cs`

Agregar dentro de `MapNews`, después de los endpoints existentes:

```csharp
group.MapGet("/paged", async (
    [AsParameters] NewsPagedRequest req,
    INewsRepository newsRepo,
    CancellationToken ct) =>
{
    var page = Math.Max(1, req.PageNumber ?? 1);
    var pageSize = Math.Clamp(req.PageSize ?? 20, 1, 50);
    var (items, total, tickers) = await newsRepo.GetPagedPublicAsync(page, pageSize, req.Q, req.FibraId, ct);

    var dtos = items.Select(a =>
    {
        var fibras = tickers.TryGetValue(a.Id, out var t)
            ? t.Select(ticker => new LinkedFibraDto(Guid.Empty, ticker)).ToList()
            : null;
        return new NewsArticleDto(a.Id, a.Title, a.Source, a.PublishedAt, a.Url,
            a.Snippet, a.ImageUrl, a.AiSummary, MapAnalysis(a.AiAnalysisJson), fibras);
    }).ToList();

    return Results.Ok(new NewsPagedResultDto(dtos, total, page, pageSize));
})
.AllowAnonymous()
.Produces<NewsPagedResultDto>(StatusCodes.Status200OK);
```

Y el record de parámetros (puede ir en el mismo archivo o en SharedApiContracts):

```csharp
public sealed record NewsPagedRequest(
    [FromQuery(Name = "pageNumber")] int? PageNumber,
    [FromQuery(Name = "pageSize")]   int? PageSize,
    [FromQuery(Name = "q")]          string? Q,
    [FromQuery(Name = "fibraId")]    Guid? FibraId
);
```

> **Nota sobre LinkedFibraDto**: el `Id` del LinkedFibraDto se deja como `Guid.Empty` en el listado porque no se necesita en las cards (solo se usa el `Ticker`). Si en el futuro se necesita el Id real, ajustar la query del batch para incluirlo.

---

### Frontend — `fetchNewsPaged`

**Archivo**: `src/Web/Main/src/api/newsApi.ts` (agregar junto a las funciones existentes)

Después de regenerar el cliente con `codegen:api`, el tipo `NewsPagedResultDto` estará disponible en `@/generated/api`. Usar el cliente generado:

```ts
export async function fetchNewsPaged(
  page: number,
  pageSize: number,
  q?: string,
  fibraId?: string
): Promise<NewsPagedResultDto> {
  return apiClient.get('/api/v1/news/paged', {
    params: { pageNumber: page, pageSize, q: q || undefined, fibraId: fibraId || undefined },
  })
}
```

> Ajustar la sintaxis al patrón que usa el cliente generado actual (revisar cómo `fetchLatestNews` llama al cliente antes de escribir).

---

### Frontend — `NoticiasListPage.tsx`

**Archivo nuevo**: `src/Web/Main/src/modules/noticias/NoticiasListPage.tsx`

Estructura del componente:

```
NoticiasListPage
  ├── <title>Noticias — FIBRADIS</title>
  ├── Header: "Noticias" (h1 Playfair)
  ├── Barra de filtros
  │   ├── Input búsqueda (debounce 300ms, shadcn Input)
  │   └── Dropdown fibra (shadcn Select, opciones desde catálogo)
  ├── Grid de cards (2 col lg / 1 col sm)
  │   └── NewsCard (article): imagen → título → meta → snippet/headline → fibra chips
  ├── Paginación (shadcn Pagination o componente propio prev/next + "Página X de Y")
  └── Skeleton (6 cards durante isLoading)
```

**TanStack Query key**: `['news', 'paged', { page, pageSize, q, fibraId }]`
**staleTime**: `2 * 60_000` (2 min — noticias cambian más seguido que precios)

**Reset de página a 1 al cambiar filtros**:

```ts
const [page, setPage] = useState(1)
const [q, setQ] = useState('')
const [fibraId, setFibraId] = useState<string | undefined>()

// En los handlers de búsqueda y filtro:
function handleQueryChange(value: string) {
  setQ(value)
  setPage(1)
}
function handleFibraChange(value: string | undefined) {
  setFibraId(value)
  setPage(1)
}
```

**Debounce**: usar `useDeferredValue` de React 19 o un pequeño hook `useDebounce` (verificar si ya existe en `src/Web/Main/src/shared/hooks/`; si no, crear un mínimo de 5 líneas).

**Dropdown de FIBRAs**: el catálogo de FIBRAs ya tiene un endpoint público (usado por el buscador global del home). Buscar `fetchFibras` o similar en `src/Web/Main/src/api/`; si el endpoint devuelve `{ id, ticker, shortName }`, usar esos campos para el dropdown. Si no existe una función de catálogo adecuada, reusar el autocomplete del buscador global.

**Cards**: reutilizar el patrón visual de `NewsSection.tsx` (imagen con `getArticleImageUrl`, título, fuente, fecha, headline de aiAnalysis). Escalar a cards más grandes: imagen `aspect-video` visible (en `NewsSection` está desactivada con `{false && ...}` — en las cards del listado sí mostrarla si está disponible). Chips de fibras: `linkedFibras?.slice(0, 3).map(f => <Link to={/fibras/${f.ticker}}>{f.ticker}</Link>)`.

---

### Frontend — Ruta y Nav

**`src/Web/Main/src/app/routes.tsx`** — agregar ANTES de la ruta con `:id` para que el router no confunda "noticias" como un ID:

```ts
import { NoticiasListPage } from '@/modules/noticias/NoticiasListPage'

// en el array de children:
{ path: '/noticias', element: <NoticiasListPage /> },
{ path: '/noticias/:id', element: <NoticiaPage /> },
```

**Nav**: buscar `PublicLayout.tsx` en `src/Web/Main/src/shared/layouts/` y localizar el link que dice "Noticias". Actualizar el `href`/`to` a `/noticias` si actualmente apunta a otra cosa o está deshabilitado.

---

### Tests

**Archivo tests**: `tests/Unit/Infrastructure/News/NewsRepositoryPublicPagedTests.cs` (o similar, seguir convención del proyecto)

Casos mínimos:

| # | Descripción | Assertion |
|---|---|---|
| 1 | Sin filtros, 3 artículos Processed → devuelve total=3, items=3 | items.Count == 3 |
| 2 | q="FIBRA" coincide en título → solo retorna los que tienen "FIBRA" en title | items are all matching |
| 3 | fibraId filtra por fibra asociada | items solo tienen ese fibraId en sus links |
| 4 | Página vacía (page=999) → items vacíos pero total correcto | items.Count == 0, total >= 0 |
| Extra | Status=Pending/Error/Deleted NO aparece en resultados | total == 0 cuando solo hay artículos en esos estados |

Seguir el mismo patrón de los tests existentes de NewsRepository (InMemory o real DB según convención del proyecto; revisar otros tests de repositorio para ver si usan `WebApplicationFactory` o `DbContextOptionsBuilder` con InMemory).

---

### Project Structure Notes

- **Módulos frontend**: los módulos en `src/Web/Main/src/modules/` usan carpeta por módulo. El módulo de noticias ya existe como `noticia/` (singular, para el detalle). La nueva página puede ir en `noticia/NoticiasListPage.tsx` (misma carpeta) o en una carpeta `noticias/` nueva. Preferir la misma carpeta `noticia/` para mantener cohesión.
- **API path conflict**: `/api/v1/news/paged` no entra en conflicto con `/api/v1/news/{id:guid}` porque `"paged"` no es un Guid válido.
- **codegen**: ejecutar `npm run codegen:api` desde la raíz del repo (no desde `src/Web/Main/`) después de agregar el endpoint.
- **No tocar**: `GET /api/v1/news/` (home), `GET /api/v1/news/fibras/{fibraId}` (ficha), `GET /api/v1/news/{id}` (detalle), `GET /api/v1/news/{id}/related` — todos siguen igual.

### References

- Repositorio existente: `src/Server/Infrastructure/Persistence/Repositories/News/NewsRepository.cs`
- Interfaz: `src/Server/Application/News/INewsRepository.cs` — `GetPagedForOpsAsync` como modelo
- DTOs: `src/Server/SharedApiContracts/News/NewsArticleDto.cs`, `LinkedFibraDto`
- Endpoints existentes: `src/Server/Api/Endpoints/Public/NewsEndpoints.cs`
- Componentes frontend de referencia: `src/Web/Main/src/modules/home/NewsSection.tsx`, `src/Web/Main/src/modules/noticia/NoticiaPage.tsx`
- Rutas: `src/Web/Main/src/app/routes.tsx`
- Helper imagen: `src/Web/Main/src/shared/lib/news-image-fallback.ts` — `getArticleImageUrl`
- Helper tiempo: `src/Web/Main/src/shared/lib/format-time.ts` — `formatRelativeTime`

## Dev Agent Record

### Agent Model Used

gpt-5-codex

### Debug Log References

- `dotnet test tests/Unit/` falla en este repo si se ejecuta sobre el directorio porque no existe `.sln` ni `.csproj` agregador; se validó con los tres proyectos explícitos `Application.Tests`, `Domain.Tests` e `Infrastructure.Tests`.
- `npm run codegen:api` debía correrse después de un `dotnet build src/Server/Api/Api.csproj --configuration Release`; cuando se ejecuta antes, el cliente generado no incluye `/api/v1/news/paged`.
- La verificación manual en `dev` quedó bloqueada por deriva del entorno local: el API arranca con errores SQL sobre `ops.OperationalConfig.FibraNewsMonths` y `dotnet ef database update` no puede corregirlo porque `AppDbContext` reporta `PendingModelChangesWarning`.

### Completion Notes List

- Implementado `GetPagedPublicAsync` en `INewsRepository`/`NewsRepository` con filtros por `q` y `fibraId`, orden `PublishedAt DESC` y carga batch de tickers por página para evitar N+1.
- Agregado `GET /api/v1/news/paged` y `NewsPagedResultDto`; el contrato quedó regenerado en `src/Web/SharedApiClient/schema.d.ts`.
- Creada `NoticiasListPage` con debounce de 300 ms, filtro por FIBRA, skeleton de 6 cards, estado vacío y ruta pública `/noticias`.
- `npm run build --workspace=src/Web/Main` pasa; persiste warning no bloqueante por chunk > 500 kB.
- `dotnet build FIBRADIS.slnx --configuration Release` pasa después de alinear tests existentes de configuración Ops con firmas vigentes.
- Verificación manual parcial: `/noticias` renderiza título, buscador y dropdown en Vite local; la validación completa de datos/paginación quedó bloqueada por el estado actual de la BD/API de desarrollo.

### File List

- `src/Server/Application/News/INewsRepository.cs`
- `src/Server/Api/Endpoints/Public/NewsEndpoints.cs`
- `src/Server/Infrastructure/Persistence/Repositories/News/NewsRepository.cs`
- `src/Server/SharedApiContracts/News/NewsPagedResultDto.cs`
- `src/Web/Main/src/api/newsApi.ts`
- `src/Web/Main/src/app/routes.tsx`
- `src/Web/Main/src/modules/noticia/NoticiaPage.tsx`
- `src/Web/Main/src/modules/noticias/NoticiasListPage.tsx`
- `src/Web/Main/src/shared/hooks/useDebouncedValue.ts`
- `src/Web/Main/src/shared/layouts/PublicLayout.tsx`
- `src/Web/SharedApiClient/schema.d.ts`
- `tests/Integration/Api.Tests/AiModeOpsEndpointTests.cs`
- `tests/Integration/Api.Tests/Ops/OpsConfigEndpointTests.cs`
- `tests/Unit/Infrastructure.Tests/Jobs/News/NewsPipelineJobTests.cs`
- `tests/Unit/Infrastructure.Tests/Persistence/Repositories/NewsRepositoryPublicPagedTests.cs`
- `tests/Unit/Infrastructure.Tests/Persistence/Repositories/OperationalConfigRepositoryTests.cs`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

### Change Log

- 2026-05-31: Implementado listado público de noticias con endpoint paginado, búsqueda con debounce, filtro por FIBRA, ruta `/noticias` y tests de repositorio.
- 2026-05-31: Alineados tests de configuración Ops con firmas vigentes para recuperar `dotnet build FIBRADIS.slnx --configuration Release` y `dotnet test` de suites unitarias.
- 2026-05-31: Historia permanece `in-progress` porque la verificación manual completa en `dev` está bloqueada por deriva de esquema/migraciones en la BD local (`FibraNewsMonths` en `ops.OperationalConfig`).
- 2026-05-31: Historia movida a `review` por instrucción explícita del usuario; persiste bloqueo de validación manual completa en entorno `dev`.
