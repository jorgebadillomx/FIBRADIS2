# Story 5.10: Página pública de Fundamentales — comparación cross-FIBRA

Status: done

## Story

As a visitante público del sitio,
I want una página `/fundamentales` que muestre los indicadores financieros de todas las FIBRAs en una tabla comparativa con filtros de período y fibra,
so that puedo evaluar y comparar los fundamentales de todo el universo FIBRADIS de un vistazo sin tener que abrir la ficha de cada una.

## Acceptance Criteria

1. **Navegación renombrada**: el ítem "Mercado" del nav público (`PublicLayout.tsx`) se renombra a "Fundamentales" y apunta a `/fundamentales` usando `<Link to="/fundamentales">` (React Router, igual que "Noticias").
2. **Ruta activa**: navegar a `/fundamentales` muestra la nueva página; actualmente esa ruta cae en NotFound.
3. **Tabla comparativa**: la página muestra una tabla con una fila por FIBRA que tiene al menos un registro `status=processed`. Columnas: **Fibra** (ticker + nombre), **Período**, **Cap Rate**, **NAV/CBFI**, **LTV**, **NOI Margin**, **FFO Margin**, **Dist. Trimestral**.
4. **Vista "Último disponible"** (default): sin filtro de período activo, cada fila muestra el dato más reciente de esa FIBRA; el período puede diferir entre FIBRAs; la columna "Período" lo indica por fila.
5. **Filtro por período**: hay un selector de período cuya primera opción es "Último disponible"; las siguientes opciones son todos los períodos distintos con al menos un registro `processed` en el sistema, ordenados del más reciente al más antiguo; al seleccionar un período concreto, solo se muestran filas con datos para ese período (las FIBRAs sin datos para ese período se omiten).
6. **Filtro por fibra**: hay un input de texto que filtra las filas client-side por ticker o nombre (sin nueva llamada al backend); el filtro es case-insensitive y actualiza en tiempo real.
7. **Valores nulos**: los KPIs no disponibles muestran "—" sin error, usando el mismo formateador que `FundamentalesSection.tsx`.
8. **Enlace a ficha**: hacer click en el nombre/ticker de una fila navega a `/fibras/:ticker` (React Router `<Link>`).
9. **Skeleton**: mientras carga muestra 6 filas de skeleton en la tabla.
10. **Estado vacío**: si no hay datos para el período seleccionado (o si no hay ningún fundamental en el sistema), se muestra un mensaje informativo.
11. **Endpoint summary**: el backend expone `GET /api/v1/fundamentals/summary?period=...`; sin `period`, devuelve el registro más reciente por FIBRA; con `period`, devuelve todos los registros de ese período; responde `FundamentalesSummaryItemDto[]` (200 OK).
12. **Endpoint periods (global)**: el backend expone `GET /api/v1/fundamentals/periods` (sin ticker) que devuelve `string[]` con todos los períodos distintos con al menos un registro `processed` en todo el catálogo, ordenados del más reciente al más antiguo.
13. **Sin N+1**: el endpoint summary obtiene registros y datos de fibra con el mínimo de consultas (ver Dev Notes §Backend para el patrón recomendado).
14. **SEO**: `<title>Fundamentales — FIBRADIS</title>`, `<meta name="description">`, `og:title` y `og:description` en la página.
15. **Responsive**: la tabla es scrollable horizontalmente en pantallas pequeñas (overflow-x-auto sobre el contenedor).
16. **Unit tests**: al menos 4 tests de los nuevos métodos de repositorio (`GetSummaryLatestAsync` y `GetSummaryByPeriodAsync`).

## Tasks / Subtasks

- [x] T1 — Backend: nuevos métodos de repositorio (AC: 11, 12, 13, 16)
  - [x] T1.1 Agregar 3 métodos a `IFundamentalRepository` (ver firmas en Dev Notes §Backend)
  - [x] T1.2 Implementar los 3 métodos en `FundamentalRepository` (ver implementación en Dev Notes §Backend)
  - [x] T1.3 Agregar `FundamentalesSummaryItemDto` en `src/Server/SharedApiContracts/Fundamentals/` (ver Dev Notes §DTO)
  - [x] T1.4 Unit tests del repositorio (4 casos mínimo, ver Dev Notes §Tests)

- [x] T2 — Backend: nuevos endpoints públicos (AC: 11, 12)
  - [x] T2.1 Agregar `GET /api/v1/fundamentals/summary` en `FundamentalsEndpoints.cs`
  - [x] T2.2 Agregar `GET /api/v1/fundamentals/periods` (global, sin ticker) en `FundamentalsEndpoints.cs`
  - [x] T2.3 `dotnet build FIBRADIS.slnx` — 0 errores

- [x] T3 — Frontend: regenerar cliente API (AC: 11, 12)
  - [x] T3.1 `npm run codegen:api` desde raíz del repo

- [x] T4 — Frontend: funciones API y página (AC: 1–10, 14, 15)
  - [x] T4.1 Agregar `fetchFundamentalesSummary` y `fetchAllFundamentalesPeriods` en `src/Web/Main/src/api/fundamentalesApi.ts`
  - [x] T4.2 Crear `src/Web/Main/src/modules/fundamentales/FundamentalesPage.tsx` (ver estructura en Dev Notes §Frontend)
  - [x] T4.3 Agregar ruta `/fundamentales` en `src/Web/Main/src/app/routes.tsx`
  - [x] T4.4 Actualizar `PublicLayout.tsx`: renombrar "Mercado" → "Fundamentales" y cambiar `<a href="/mercado">` por `<Link to="/fundamentales">` (React Router)

- [x] T5 — Verificación final
  - [x] T5.1 `dotnet test tests/Unit/Infrastructure.Tests/` — 156 passed, 0 failed
  - [x] T5.2 `npm run build --workspace=src/Web/Main` — 0 errores TypeScript, 0 advertencias bloqueantes

## Dev Notes

### Contexto

La ruta `/fundamentales` no existe. El nav tiene `<a href="/mercado">Mercado</a>` que apunta a una ruta inexistente. Esta historia crea la página pública de comparación cross-FIBRA de fundamentales, análoga a la relación que tiene `/noticias` con `/noticias/:id`; la ficha completa por FIBRA ya existe en `/fibras/:ticker` → `FundamentalesSection`.

No hay migración de BD. Todos los datos necesarios están en `fundamentals.FundamentalRecord` (join con `catalog.Fibra`). La tabla tiene ~15 FIBRAs × ≤12 períodos = ~180 registros máximo.

Conflictos de ruta cero: los nuevos endpoints (`/summary`, `/periods`) tienen 1 segmento de path; los existentes (`/{ticker}/latest`, `/{ticker}/periods`) tienen 2 segmentos. ASP.NET Core minimal APIs resuelve literales antes que parámetros cuando los segmentos coinciden, y en este caso ni siquiera coinciden por conteo.

---

### Backend — Nuevos métodos en `IFundamentalRepository`

**Archivo**: `src/Server/Application/Fundamentals/IFundamentalRepository.cs`

Agregar al final de la interfaz:

```csharp
// Devuelve el registro processed más reciente por cada FIBRA activa
Task<IReadOnlyList<(FundamentalRecord Record, string Ticker, string ShortName)>> GetSummaryLatestAsync(CancellationToken ct = default);

// Devuelve todos los registros processed de un período específico
Task<IReadOnlyList<(FundamentalRecord Record, string Ticker, string ShortName)>> GetSummaryByPeriodAsync(string period, CancellationToken ct = default);

// Devuelve todos los períodos distintos con al menos un registro processed en todo el catálogo
Task<IReadOnlyList<string>> GetAllProcessedPeriodsAsync(CancellationToken ct = default);
```

---

### Backend — Implementación en `FundamentalRepository`

**Archivo**: `src/Server/Infrastructure/Persistence/Repositories/Fundamentals/FundamentalRepository.cs`

#### `GetSummaryLatestAsync`

Carga todos los registros processed con datos de fibra en una sola query y luego elige el más reciente por FIBRA en memoria (dataset pequeño, ~180 filas máximo):

```csharp
public async Task<IReadOnlyList<(FundamentalRecord Record, string Ticker, string ShortName)>> GetSummaryLatestAsync(CancellationToken ct = default)
{
    var all = await db.FundamentalRecords
        .Where(r => r.Status == "processed" && r.DeletedAt == null)
        .Join(
            db.Fibras.Where(f => f.DeletedAt == null),
            r => r.FibraId,
            f => f.Id,
            (r, f) => new { Record = r, f.Ticker, ShortName = f.ShortName ?? f.Name })
        .ToListAsync(ct);

    return all
        .GroupBy(x => x.Record.FibraId)
        .Select(g => g
            .OrderByDescending(x => x.Record.Period.Length >= 7 ? x.Record.Period.Substring(3, 4) : "0000")
            .ThenByDescending(x => x.Record.Period.Length >= 2 ? x.Record.Period.Substring(0, 1) : "0")
            .ThenByDescending(x => x.Record.ConfirmedAt)
            .First())
        .OrderBy(x => x.Ticker)
        .Select(x => (x.Record, x.Ticker, x.ShortName))
        .ToList();
}
```

> **Nota**: `f.ShortName ?? f.Name` — verifica el nombre del campo en `Fibra.cs`; puede ser `ShortName`, `Name` o `DisplayName`. Ajustar según el entity existente. Si la entidad tiene navegación `Fibra` en `FundamentalRecord`, se puede usar `Include` en lugar del `Join`, que es equivalente.

#### `GetSummaryByPeriodAsync`

```csharp
public async Task<IReadOnlyList<(FundamentalRecord Record, string Ticker, string ShortName)>> GetSummaryByPeriodAsync(string period, CancellationToken ct = default)
{
    var results = await db.FundamentalRecords
        .Where(r => r.Status == "processed" && r.DeletedAt == null && r.Period == period)
        .Join(
            db.Fibras.Where(f => f.DeletedAt == null),
            r => r.FibraId,
            f => f.Id,
            (r, f) => new { Record = r, f.Ticker, ShortName = f.ShortName ?? f.Name })
        .OrderBy(x => x.Ticker)
        .ToListAsync(ct);

    return results.Select(x => (x.Record, x.Ticker, x.ShortName)).ToList();
}
```

#### `GetAllProcessedPeriodsAsync`

Mismo patrón que `GetProcessedPeriodsAsync` pero sin filtro por fibra:

```csharp
public async Task<IReadOnlyList<string>> GetAllProcessedPeriodsAsync(CancellationToken ct = default)
    => await db.FundamentalRecords
        .Where(r => r.Status == "processed" && r.DeletedAt == null && r.Period.Length == 7)
        .Select(r => r.Period)
        .Distinct()
        .OrderByDescending(p => p.Substring(3, 4))
        .ThenByDescending(p => p.Substring(0, 1))
        .ToListAsync(ct);
```

> **Nota sobre ordering**: el patrón `Substring(3, 4)` extrae el año y `Substring(0, 1)` el número de trimestre. Verificar el formato exacto de `Period` (ej. `"2Q2025"` de 6 chars vs `"2Q 2025"` de 7 chars) contra los registros existentes en BD. El filtro `Period.Length == 7` ya existe en `GetProcessedPeriodsAsync` — mantener consistencia.

---

### Backend — DTO `FundamentalesSummaryItemDto`

**Archivo nuevo**: `src/Server/SharedApiContracts/Fundamentals/FundamentalesSummaryItemDto.cs`

```csharp
namespace SharedApiContracts.Fundamentals;

public sealed record FundamentalesSummaryItemDto(
    string Ticker,
    string Name,
    string Period,
    decimal? CapRate,
    decimal? NavPerCbfi,
    decimal? Ltv,
    decimal? NoiMargin,
    decimal? FfoMargin,
    decimal? QuarterlyDistribution,
    DateTimeOffset CapturedAt
);
```

---

### Backend — Nuevos endpoints en `FundamentalsEndpoints.cs`

**Archivo**: `src/Server/Api/Endpoints/Public/FundamentalsEndpoints.cs`

Agregar dentro de `MapFundamentalsPublic`, **antes** de los endpoints `/{ticker}/...` para que el router resuelva los literales primero:

```csharp
// GET /api/v1/fundamentals/summary?period=2Q2025
group.MapGet("/summary", async (
    [FromQuery] string? period,
    IFundamentalRepository fundamentalRepo,
    CancellationToken ct) =>
{
    IReadOnlyList<(FundamentalRecord Record, string Ticker, string ShortName)> rows =
        string.IsNullOrWhiteSpace(period)
            ? await fundamentalRepo.GetSummaryLatestAsync(ct)
            : await fundamentalRepo.GetSummaryByPeriodAsync(period.Trim().ToUpperInvariant(), ct);

    var dtos = rows.Select(r => new FundamentalesSummaryItemDto(
        Ticker: r.Ticker,
        Name: r.ShortName,
        Period: r.Record.Period,
        CapRate: r.Record.CapRate,
        NavPerCbfi: r.Record.NavPerCbfi,
        Ltv: r.Record.Ltv,
        NoiMargin: r.Record.NoiMargin,
        FfoMargin: r.Record.FfoMargin,
        QuarterlyDistribution: r.Record.QuarterlyDistribution,
        CapturedAt: r.Record.CapturedAt)).ToList();

    return Results.Ok(dtos);
})
.AllowAnonymous()
.Produces<IReadOnlyList<FundamentalesSummaryItemDto>>(StatusCodes.Status200OK);

// GET /api/v1/fundamentals/periods (global — todos los períodos del catálogo)
group.MapGet("/periods", async (
    IFundamentalRepository fundamentalRepo,
    CancellationToken ct) =>
{
    var periods = await fundamentalRepo.GetAllProcessedPeriodsAsync(ct);
    return Results.Ok(periods);
})
.AllowAnonymous()
.Produces<IReadOnlyList<string>>(StatusCodes.Status200OK);
```

> **Importante**: colocar los endpoints `/summary` y `/periods` ANTES de `/{ticker}/latest` y `/{ticker}/periods` en el código para claridad. En ASP.NET Core minimal APIs las rutas de un segmento (`/summary`) no compiten con las de dos segmentos (`/{ticker}/latest`), pero ordenarlos primero es convención del proyecto.

> **No tocar**: los endpoints existentes `/{ticker}/latest` y `/{ticker}/periods` siguen igual.

---

### Frontend — `fetchFundamentalesSummary` y `fetchAllFundamentalesPeriods`

**Archivo**: `src/Web/Main/src/api/fundamentalesApi.ts`

Revisar el patrón de `fetchFundamentalesPublic` antes de escribir. Agregar al final del archivo:

```ts
export async function fetchFundamentalesSummary(
  period?: string
): Promise<FundamentalesSummaryItemDto[]> {
  // usar el cliente openapi-fetch generado (mismo patrón que fetchFundamentalesPublic)
  // endpoint: GET /api/v1/fundamentals/summary?period={period}
}

export async function fetchAllFundamentalesPeriods(): Promise<string[]> {
  // endpoint: GET /api/v1/fundamentals/periods
  // en caso de error devolver []
}
```

Ajustar al patrón exacto de `openapi-fetch` que usa el archivo. Los tipos `FundamentalesSummaryItemDto` estarán disponibles en el cliente generado después de `codegen:api`.

---

### Frontend — `FundamentalesPage.tsx`

**Archivo nuevo**: `src/Web/Main/src/modules/fundamentales/FundamentalesPage.tsx`

Estructura del componente:

```
FundamentalesPage
  ├── <title>Fundamentales — FIBRADIS</title>
  ├── <meta name="description" content="Compara los indicadores...">
  ├── <meta property="og:title"> y <og:description>
  ├── Header: h1 "Fundamentales FIBRADIS" + subtítulo
  ├── Barra de filtros (flex, gap)
  │   ├── Selector de período: <select> HTML nativo (igual que FibraPage period selector)
  │   │   ├── opción "Último disponible" (value="")
  │   │   └── opciones de periods[] (value=period)
  │   └── Input de texto: filtro client-side por ticker/nombre
  ├── Tabla comparativa (overflow-x-auto)
  │   ├── <thead>: Fibra | Período | Cap Rate | NAV/CBFI | LTV | NOI Margin | FFO Margin | Dist. Trim.
  │   │   └── Cabeceras de KPI: reutilizar etiquetas de KPI_DEFINITIONS (kpi-definitions.ts)
  │   ├── <tbody>: rows filtradas
  │   │   └── FundamentalesRow: Link a /fibras/:ticker + periodo badge + 6 valores KPI
  │   └── Skeleton: 6 filas de <tr> con divs skeleton durante isLoading
  └── Estado vacío: si filteredRows.length === 0 tras filtros
```

**TanStack Query keys**:
- `['fundamentales', 'summary', { period }]` — staleTime: `5 * 60_000`
- `['fundamentales', 'periods']` — staleTime: `10 * 60_000` (los períodos cambian raro)

**Estado local**:
```ts
const [selectedPeriod, setSelectedPeriod] = useState('')        // '' = latest
const [fibraFilter, setFibraFilter] = useState('')              // client-side text filter
```

**Filtrado client-side** (no debounce necesario, operación barata):
```ts
const filteredRows = useMemo(() =>
  (summaryData ?? []).filter(row =>
    fibraFilter === '' ||
    row.ticker.toLowerCase().includes(fibraFilter.toLowerCase()) ||
    row.name.toLowerCase().includes(fibraFilter.toLowerCase())
  ), [summaryData, fibraFilter])
```

**Formato de valores KPI**: usar la misma función `formatFundamentalValue` de `fundamentales.ts` para consistencia con `FundamentalesSection`. Verificar cómo está formateando actualmente (puede ser raw decimal o porcentaje multiplicado) y replicar exactamente.

**Enlace por fila**: `<Link to={`/fibras/${row.ticker}`}>` en la celda de Fibra (solo el ticker/nombre es clickable; alternativamente hacer toda la fila clickable con `cursor-pointer` y `onClick`).

**Cabeceras de KPI con tooltip**: para consistencia se puede usar el componente `KpiLabel` existente en `src/Web/Main/src/shared/ui/KpiLabel.tsx` en el `<thead>`, pero si su layout no encaja en una celda de tabla, basta con mostrar la etiqueta corta de `KPI_DEFINITIONS[key].label` y el tooltip nativo HTML (`title`).

**Skeleton**: 6 filas con `<td>` que contienen un div `animate-pulse bg-muted rounded h-4` de ancho variable. Patrón ya usado en el proyecto (buscar `animate-pulse` en los módulos existentes).

---

### Frontend — Ruta y Nav

**`src/Web/Main/src/app/routes.tsx`** — agregar nueva ruta:

```ts
import { FundamentalesPage } from '@/modules/fundamentales/FundamentalesPage'

// En children de PublicLayout:
{ path: '/fundamentales', element: <FundamentalesPage /> },
```

No hay conflicto con otras rutas existentes.

**`src/Web/Main/src/shared/layouts/PublicLayout.tsx`** — cambio quirúrgico:

```tsx
// ANTES:
<a href="/mercado" className="hover:text-foreground transition-colors duration-150">Mercado</a>

// DESPUÉS:
<Link to="/fundamentales" className="hover:text-foreground transition-colors duration-150">Fundamentales</Link>
```

`Link` ya está importado de `'react-router'` en el archivo (se usa para "FIBRADIS" logo y "Noticias"). No agregar nuevo import si ya existe.

---

### Tests

**Archivo nuevo**: `tests/Unit/Infrastructure.Tests/Persistence/Repositories/FundamentalesRepositorySummaryTests.cs`

Seguir el patrón de `NewsRepositoryPublicPagedTests.cs` (InMemory o real DB según convención del proyecto).

Casos mínimos:

| # | Método | Descripción | Assertion |
|---|---|---|---|
| 1 | `GetSummaryLatestAsync` | 2 FIBRAs con processed → devuelve una fila por FIBRA | Count == 2 |
| 2 | `GetSummaryLatestAsync` | FIBRA con 2 períodos → devuelve solo el más reciente | row.Period == periodoMásReciente |
| 3 | `GetSummaryByPeriodAsync` | período existente → devuelve solo FIBRAs con ese período | todas con Period == filtro |
| 4 | `GetSummaryByPeriodAsync` | período sin datos → devuelve lista vacía | Count == 0 |
| 5 | `GetAllProcessedPeriodsAsync` | 3 registros con 2 períodos distintos → devuelve los 2 únicos | Count == 2 |

Extra: registros `status=pending` o `deleted_at IS NOT NULL` no aparecen en ningún método.

---

### Project Structure Notes

- **Nuevo módulo frontend**: `src/Web/Main/src/modules/fundamentales/` (plural, separado de `ficha-publica/` que es el detalle por FIBRA).
- **No tocar**: `FundamentalesSection.tsx`, `FibraPage.tsx`, los endpoints `/{ticker}/latest` y `/{ticker}/periods` — todo sigue igual.
- **`fetchAllFibras` no es necesario** para esta historia; la data de fibras viene embebida en el response de `/summary`.
- **codegen**: `npm run codegen:api` desde la raíz del repo DESPUÉS de `dotnet build`. Verificar que `FundamentalesSummaryItemDto` aparece en `src/Web/SharedApiClient/schema.d.ts` antes de usarla en el frontend.
- **SEO checklist** (convenciones del proyecto para rutas públicas): `<title>`, `<meta name="description">`, `og:title`, `og:description` — obligatorio antes de `done`.

### References

- Repositorio existente: `src/Server/Infrastructure/Persistence/Repositories/Fundamentals/FundamentalRepository.cs`
- Interfaz: `src/Server/Application/Fundamentals/IFundamentalRepository.cs`
- Endpoints existentes: `src/Server/Api/Endpoints/Public/FundamentalsEndpoints.cs`
- DTO público existente: `src/Server/SharedApiContracts/Fundamentals/FundamentalesPublicDto.cs`
- Entidad dominio: `src/Server/Domain/Fundamentals/FundamentalRecord.cs`
- Frontend: `src/Web/Main/src/modules/ficha-publica/FibraPage.tsx` — period selector patrón
- Frontend: `src/Web/Main/src/modules/ficha-publica/sections/FundamentalesSection.tsx` — formateo de valores
- Frontend: `src/Web/Main/src/shared/lib/kpi-definitions.ts` — etiquetas y tooltips de KPIs
- Frontend: `src/Web/Main/src/shared/ui/KpiLabel.tsx` — componente de etiqueta con tooltip
- Frontend: `src/Web/Main/src/api/fundamentalesApi.ts` — funciones API existentes
- Nav: `src/Web/Main/src/shared/layouts/PublicLayout.tsx`
- Rutas: `src/Web/Main/src/app/routes.tsx`
- Referencia de patrón (story análoga): `_bmad-output/implementation-artifacts/4-11-pagina-listado-noticias.md`

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

- `Fibra.DeletedAt` no existe — usa `State == FibraState.Active` para filtrar fibras activas en los joins.
- Dev Notes decían `Substring(0, 1)` para quarter pero el código existente usa `Substring(1, 1)` (formato `"Q3-2024"`, índice 1 = dígito del trimestre).
- `FundamentalRecord` es `class` no `record` — no se puede usar `with` en tests; se instanció manualmente.

### Completion Notes List

- T1: Añadidos `GetSummaryLatestAsync`, `GetSummaryByPeriodAsync` y `GetAllProcessedPeriodsAsync` a `IFundamentalRepository` y su implementación en `FundamentalRepository`. Lógica: carga todos los registros `processed` con join a `Fibra` activa, agrupa por FibraId y selecciona el más reciente en memoria (dataset ~180 filas máximo).
- T1.3: Nuevo DTO `FundamentalesSummaryItemDto` en `SharedApiContracts`.
- T1.4: 6 unit tests cubren GetSummaryLatestAsync (latest, multi-period, excludes non-processed/deleted), GetSummaryByPeriodAsync (match, empty) y GetAllProcessedPeriodsAsync (distinct). 156 passed, 0 failed.
- T2: Endpoints `/summary?period=` y `/periods` agregados antes de los existentes `/{ticker}/...` en `FundamentalsEndpoints.cs`. Ambos `AllowAnonymous`.
- T3: `codegen:api` regeneró `schema.d.ts` con `FundamentalesSummaryItemDto` y los nuevos paths.
- T4: `FundamentalesPage.tsx` con tabla comparativa, selector de período, filtro client-side por ticker/nombre, skeleton de 6 filas, estado vacío. `formatFundamentalValue` reutilizada de `fundamentales.ts`. Ruta `/fundamentales` registrada. Nav "Mercado" → "Fundamentales" con `<Link>`.
- T5: `dotnet build` 0 errores, `dotnet test` 156/156, `npm run build` 0 errores TypeScript.

### File List

- `src/Server/Application/Fundamentals/IFundamentalRepository.cs` (modificado)
- `src/Server/Infrastructure/Persistence/Repositories/Fundamentals/FundamentalRepository.cs` (modificado)
- `src/Server/SharedApiContracts/Fundamentals/FundamentalesSummaryItemDto.cs` (nuevo)
- `src/Server/Api/Endpoints/Public/FundamentalsEndpoints.cs` (modificado)
- `src/Web/SharedApiClient/schema.d.ts` (regenerado)
- `src/Web/Main/src/api/fundamentalesApi.ts` (modificado)
- `src/Web/Main/src/modules/fundamentales/FundamentalesPage.tsx` (nuevo)
- `src/Web/Main/src/app/routes.tsx` (modificado)
- `src/Web/Main/src/shared/layouts/PublicLayout.tsx` (modificado)
- `tests/Unit/Infrastructure.Tests/Persistence/Repositories/FundamentalesRepositorySummaryTests.cs` (nuevo)

### Change Log

- 2026-05-31: Implementada historia 5-10. Nuevos endpoints `/api/v1/fundamentals/summary` y `/api/v1/fundamentals/periods`. Nueva página pública `/fundamentales` con tabla comparativa cross-FIBRA, filtro de período y filtro client-side por ticker/nombre. Nav "Mercado" renombrado a "Fundamentales". 6 unit tests nuevos del repositorio.

## Senior Developer Review (AI)

### Review Findings

- [x] **Review/Patch P1 [HIGH]** `GetSummaryByPeriodAsync` falta deduplicación por FibraId — puede retornar múltiples filas para la misma FIBRA cuando existen varios registros `processed` del mismo período, rompiendo la invariante "una fila por FIBRA" y causando colisión de React `key={row.ticker}` — `FundamentalRepository.cs`
- [x] **Review/Patch P2 [MEDIUM]** Empty state message incorrecta cuando hay período seleccionado sin datos — `summaryData?.length === 0` se cumple tanto para "sin datos en el sistema" como para "sin datos del período elegido", mostrando siempre "No hay fundamentales procesados en el sistema" en lugar del mensaje apropiado — `FundamentalesPage.tsx`
- [x] **Review/Patch P3 [MEDIUM]** React key `row.ticker` no es suficientemente único — si la API retorna duplicados (ver P1), las colisiones de key causan rendering incorrecto; usar key compuesto `row.ticker + '-' + row.period` — `FundamentalesPage.tsx`
- [x] **Review/Patch P4 [MEDIUM]** `<input>` usa `>` en lugar de `/>` en JSX — falso positivo: el archivo real ya usa `/>` correctamente — `FundamentalesPage.tsx`
- [x] **Review/Patch P5 [LOW]** Test `GetAllProcessedPeriodsAsync_Returns_DistinctPeriods` no verifica el orden — solo aserta `Count == 2`, sin comprobar que los períodos estén ordenados del más reciente al más antiguo (AC 12) — `FundamentalesRepositorySummaryTests.cs`
- [x] **Review/Patch P6 [LOW]** `using System.Collections.Generic;` redundante en proyecto .NET 6+ con implicit usings habilitados — `FundamentalsEndpoints.cs`
- [x] **Review/Defer** `GetSummaryLatestAsync` carga todo en memoria antes de agrupar — deferred, Dev Notes aprueba explícitamente este patrón (~180 filas máx)
- [x] **Review/Defer** `GetAllProcessedPeriodsAsync` sin límite `Take` — deferred, AC 12 requiere retornar todos los períodos distintos del catálogo
- [x] **Review/Defer** `fetchFundamentalesSummary` silencia errores HTTP retornando `[]` — deferred, patrón pre-existente en el mismo archivo
- [x] **Review/Defer** Flash de tabla durante background refetch sin indicador — deferred, cosmético, fuera del scope
- [x] **Review/Defer** Tests con InMemory provider no validan semántica SQL de `Substring` en ordering — deferred, patrón pre-existente del proyecto
