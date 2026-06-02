---
title: 'Opción "Todas las disponibles" en selector de período — Fundamentales'
type: 'feature'
created: '2026-06-02'
status: 'done'
baseline_commit: 'af99042187e36ac8f09cab8b3a26eb526ee94965'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** La página `/fundamentales` no permite comparar múltiples períodos a la vez; el selector solo muestra un período o "Último disponible", sin forma de ver la evolución histórica de todas las FIBRAs en una sola vista.

**Approach:** Agregar la opción "Todas las disponibles" (`value="all"`) al selector de período. Cuando está activa, la tabla muestra una fila por (FIBRA × período) para los últimos 12 períodos disponibles, ordenadas por período más reciente primero. Se extiende el endpoint `/summary` con el query param `?recent=N` y se agrega un método de repositorio que lo soporta.

## Boundaries & Constraints

**Always:**
- Usar el mismo DTO `FundamentalesSummaryItemDto` — sin campos nuevos ni codegen.
- Ordenar: período descendente, luego ticker ascendente.
- Dedup por `(FibraId, Period)` tomando el registro con `ConfirmedAt` más reciente (igual que `GetSummaryByPeriodAsync`).
- El filtro de texto `fibraFilter` sigue funcionando sobre los resultados en el modo "all".
- `?recent` tiene precedencia sobre `?period` en el endpoint si ambos llegan (el frontend nunca envía ambos a la vez).

**Ask First:**
- Si al implementar se detecta que `periods.Contains(r.Period)` no se traduce correctamente a SQL con EF Core (verificar antes de assumir).

**Never:**
- Modificar los endpoints `/{ticker}/latest` y `/{ticker}/periods`.
- Cambiar los tests existentes del repositorio.
- Agregar paginación ni límite configurable por el usuario.
- Correr `npm run codegen:api` — el contrato DTO no cambia.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| `?recent=12`, hay 15 períodos | 15 períodos disponibles en BD | Solo registros de los 12 más recientes; ordenados período desc + ticker asc | N/A |
| `?recent=12`, hay 3 períodos | 3 períodos disponibles en BD | Todos los registros de esos 3 períodos | N/A |
| `?recent=12`, sin processed | BD sin registros processed | `[]` (200 OK) | N/A |
| Frontend "Todas" + fibraFilter | `selectedPeriod === 'all'` + texto en filtro | Filas filtradas client-side por ticker/nombre igual que los otros modos | — |
| Frontend "Todas" sin datos | `selectedPeriod === 'all'` + API devuelve `[]` | Mensaje "Sin datos disponibles." | — |

</frozen-after-approval>

## Code Map

- `src/Server/Application/Fundamentals/IFundamentalRepository.cs` — agregar firma `GetSummaryForRecentPeriodsAsync`
- `src/Server/Infrastructure/Persistence/Repositories/Fundamentals/FundamentalRepository.cs` — implementar el nuevo método
- `src/Server/Api/Endpoints/Public/FundamentalsEndpoints.cs` — extender handler `/summary` con param `[FromQuery] int? recent`
- `src/Web/Main/src/api/fundamentalesApi.ts` — agregar `recent?: number` a `fetchFundamentalesSummary`
- `src/Web/Main/src/modules/fundamentales/FundamentalesPage.tsx` — opción "all" en select + lógica de query actualizada

## Tasks & Acceptance

**Execution:**
- [x] `src/Server/Application/Fundamentals/IFundamentalRepository.cs` -- agregar al final: `Task<IReadOnlyList<(FundamentalRecord Record, string Ticker, string ShortName)>> GetSummaryForRecentPeriodsAsync(int count, CancellationToken ct = default);` -- mantener consistencia de firma con los otros métodos summary
- [x] `src/Server/Infrastructure/Persistence/Repositories/Fundamentals/FundamentalRepository.cs` -- implementar `GetSummaryForRecentPeriodsAsync`: (1) llamar `GetAllProcessedPeriodsAsync(ct)` y tomar `.Take(count)`, (2) si vacío devolver `Array.Empty`, (3) query con `periods.Contains(r.Period)` + join a fibras activas, (4) dedup por `(FibraId, Period)` con `ConfirmedAt` más reciente, (5) ordenar `OrderByDescending(period desc).ThenBy(ticker asc)` -- misma estructura que `GetSummaryByPeriodAsync`
- [x] `src/Server/Api/Endpoints/Public/FundamentalsEndpoints.cs` -- en el handler `MapGet("/summary", ...)` agregar `[FromQuery] int? recent`; cuando `recent > 0`, llamar `GetSummaryForRecentPeriodsAsync(recent.Value)` antes de evaluar `period`; la lógica existente `period` / sin parámetros queda inalterada -- extensión no destructiva del handler
- [x] `dotnet build FIBRADIS.slnx` -- 0 errores, 0 advertencias nuevas (file-lock warnings son del proceso en ejecución, no de compilación)
- [x] `src/Web/Main/src/api/fundamentalesApi.ts` -- función `fetchFundamentalesSummary`: aceptar `{ period?: string; recent?: number }` en lugar del parámetro `period?: string`; construir URL con `?recent=N` cuando `recent` esté presente, `?period=X` cuando `period` esté presente, sin params cuando ninguno -- backward-compatible: el argumento anterior era solo `period`, ahora es un objeto options
- [x] `src/Web/Main/src/modules/fundamentales/FundamentalesPage.tsx` -- (a) agregar `<option value="all">Todas las disponibles</option>` inmediatamente después de la opción "Último disponible"; (b) cuando `selectedPeriod === 'all'`, pasar `{ recent: 12 }` a `fetchFundamentalesSummary`; (c) query key: `['fundamentales', 'summary', { recent: 12 }]` para el modo "all"; (d) empty state específico para modo "all": `"Sin datos disponibles."` -- la funcionalidad de búsqueda por fibra sigue igual en todos los modos
- [x] `npm run build --workspace=src/Web/Main` -- solo error preexistente en NoticiasListPage.tsx (no introducido por estos cambios)

**Acceptance Criteria:**
- Dado que hay al menos un período procesado, cuando el usuario selecciona "Todas las disponibles", la tabla muestra múltiples filas por FIBRA (una por período), ordenadas por período más reciente primero.
- Dado que el usuario escribe texto en "Buscar FIBRA" con el modo "Todas las disponibles" activo, las filas se filtran client-side por ticker/nombre.
- Dado que se llama `GET /api/v1/fundamentals/summary?recent=12` y hay 15 períodos en la BD, la respuesta incluye solo registros de los 12 períodos más recientes (puede tener más de una fila por FIBRA, una por cada período).
- Los modos "Último disponible" y períodos específicos no cambian en comportamiento.

## Spec Change Log

## Design Notes

La firma de `fetchFundamentalesSummary` cambia de `(period?: string)` a `(opts?: { period?: string; recent?: number })`. Todos los call sites existentes en `FundamentalesPage.tsx` pasan `selectedPeriod || undefined` — actualizar esos call sites al nuevo objeto `{ period: selectedPeriod || undefined }`.

El orden de evaluación en el endpoint: `recent > 0` → `GetSummaryForRecentPeriodsAsync`; de lo contrario, lógica existente.

## Verification

**Commands:**
- `dotnet build FIBRADIS.slnx` -- expected: `Build succeeded. 0 Error(s)`
- `npm run build --workspace=src/Web/Main` -- expected: sin errores TypeScript

**Manual checks:**
- Abrir `/fundamentales`, seleccionar "Todas las disponibles": la tabla debe mostrar múltiples filas para fibras que tienen varios períodos procesados.
- El filtro de texto debe seguir funcionando en el modo "all".
- Seleccionar un período específico (ej. "2Q2025") sigue mostrando solo ese período.

## Suggested Review Order

**Nuevo contrato de repositorio**

- Firma del método nuevo; único punto de extensión de la interfaz.
  [`IFundamentalRepository.cs:23`](../../src/Server/Application/Fundamentals/IFundamentalRepository.cs#L23)

- Implementación: 2 queries (períodos → registros), dedup por `(FibraId, Period)`, sort año+trimestre desc.
  [`FundamentalRepository.cs:204`](../../src/Server/Infrastructure/Persistence/Repositories/Fundamentals/FundamentalRepository.cs#L204)

**Endpoint público**

- Rama `recent > 0` tiene precedencia; lógica existente `period`/latest inalterada.
  [`FundamentalsEndpoints.cs:22`](../../src/Server/Api/Endpoints/Public/FundamentalsEndpoints.cs#L22)

**Cliente API + lógica de query**

- Firma cambia de `period?: string` a objeto `opts`; construye URL según `recent` o `period`.
  [`fundamentalesApi.ts:24`](../../src/Web/Main/src/api/fundamentalesApi.ts#L24)

- `isAllPeriods` determina queryKey y queryFn; TanStack Query cachea el modo "all" por separado.
  [`FundamentalesPage.tsx:13`](../../src/Web/Main/src/modules/fundamentales/FundamentalesPage.tsx#L13)

**UI**

- Nueva opción `value="all"` en el select, inmediatamente después de "Último disponible".
  [`FundamentalesPage.tsx:73`](../../src/Web/Main/src/modules/fundamentales/FundamentalesPage.tsx#L73)

- Empty state diferenciado: "Sin datos disponibles." solo en modo "all".
  [`FundamentalesPage.tsx:116`](../../src/Web/Main/src/modules/fundamentales/FundamentalesPage.tsx#L116)
