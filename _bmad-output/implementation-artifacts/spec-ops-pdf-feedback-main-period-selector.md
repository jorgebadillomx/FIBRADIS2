---
title: 'Fix descarga PDF en Ops + selector de período en Main/Fundamentales'
type: 'feature'
created: '2026-05-27'
status: 'done'
baseline_commit: '20bb16065640f3d618fdf2ecdde33fb63dc13a89'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** El botón "Ver PDF" en el historial de Ops traga errores silenciosamente (solo `console.error`), dejando al usuario sin feedback. En Main, la sección de Fundamentales siempre muestra el último período procesado sin posibilidad de ver datos históricos de otros períodos registrados en DB.

**Approach:** Agregar estado de error visible por fila en el historial de Ops. Añadir endpoint público que retorna los períodos procesados disponibles por FIBRA, y modificar el endpoint existente para aceptar un período opcional; en Main, mostrar un selector de período que carga solo los períodos con datos en DB.

## Boundaries & Constraints

**Always:**
- El selector de período solo muestra períodos con `status = "processed"` en DB.
- El endpoint de períodos es público (`AllowAnonymous`), igual que el de fundamentales.
- El error de descarga PDF en Ops debe mostrarse en la fila correspondiente, no en un toast global.
- No modificar `FundamentalesPublicDto` — ya tiene todos los campos necesarios.

**Ask First:** — (ninguna decisión pendiente)

**Never:**
- No exponer períodos `partial`, `pending` o `error` en el selector público.
- No reemplazar la lógica "latest" como default — si no se pasa período, sigue devolviendo el más reciente.
- Máximo 12 períodos en el selector — el endpoint retorna los 12 más recientes con `status = "processed"`.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Selector con períodos disponibles | FIBRA con 3 períodos procesados | Dropdown muestra `["Q1-2026", "Q4-2025", "Q3-2025"]`, seleccionado = último | — |
| Sin períodos procesados | FIBRA sin registros `processed` | Sección muestra "Sin fundamentales disponibles" (comportamiento actual) | — |
| Cambio de período | Usuario selecciona `Q4-2025` | KPIs se actualizan con datos de ese período | Loading state durante fetch |
| PDF no encontrado en disco | Record tiene `pdfReference` pero archivo borrado | Fila muestra error: "No se pudo descargar el PDF (404)" | Error visible en fila, sin crash |
| Descarga PDF exitosa | Archivo existe | Blob descargado, `<a>` dispara descarga del navegador | — |

</frozen-after-approval>

## Code Map

- `src/Server/Application/Fundamentals/IFundamentalRepository.cs` — agregar `GetProcessedPeriodsAsync`
- `src/Server/Infrastructure/Persistence/Repositories/Fundamentals/FundamentalRepository.cs` — implementar `GetProcessedPeriodsAsync`
- `src/Server/Api/Endpoints/Public/FundamentalsEndpoints.cs` — nuevo endpoint `GET /{ticker}/periods` + query param `?period=` en `/{ticker}/latest`
- `src/Web/Main/src/api/fundamentalesApi.ts` — agregar `fetchFundamentalesAvailablePeriods` + param opcional `period?` a `fetchFundamentalesPublic`
- `src/Web/Main/src/modules/ficha-publica/FibraPage.tsx` — estado `selectedPeriod`, fetch de períodos, selector UI inline en sección Fundamentales
- `src/Web/Ops/src/modules/fundamentals/FundamentalsHistory.tsx` — estado `pdfDownloadError` por fila con display visible

## Tasks & Acceptance

**Execution:**

- [x] `src/Server/Application/Fundamentals/IFundamentalRepository.cs` — agregar método `Task<IReadOnlyList<string>> GetProcessedPeriodsAsync(Guid fibraId, CancellationToken ct)` — necesario para el nuevo endpoint público
- [x] `src/Server/Infrastructure/Persistence/Repositories/Fundamentals/FundamentalRepository.cs` — implementar: query `db.FundamentalRecords.Where(r => r.FibraId == fibraId && r.Status == "processed").Select(r => r.Period).Distinct().OrderByDescending(...)` con mismo criterio de orden que `GetLatestProcessedByFibraAsync` (año desc, trimestre desc), `.Take(12)`
- [x] `src/Server/Api/Endpoints/Public/FundamentalsEndpoints.cs` — (1) añadir `[FromQuery] string? period` a `/{ticker}/latest`: si `period` es non-null llama `GetProcessedByFibraAndPeriodAsync`; si null mantiene `GetLatestProcessedByFibraAsync`; (2) nuevo endpoint `GET /{ticker}/periods` → `AllowAnonymous`, retorna `string[]` con la lista de períodos procesados
- [x] `src/Web/Main/src/api/fundamentalesApi.ts` — agregar `fetchFundamentalesAvailablePeriods(ticker: string): Promise<string[]>` llamando `GET /api/v1/fundamentals/{ticker}/periods`; actualizar `fetchFundamentalesPublic` para aceptar `period?: string` como query param opcional
- [x] `src/Web/Main/src/modules/ficha-publica/FibraPage.tsx` — añadir `useQuery` para períodos disponibles; estado `selectedPeriod` (default: primer elemento de la lista = más reciente); pasar `selectedPeriod` a `fetchFundamentalesPublic`; renderizar selector de período inline sobre `<FundamentalesSection>` (solo si `availablePeriods.length > 1`)
- [x] `src/Web/Ops/src/modules/fundamentals/FundamentalsHistory.tsx` — en `HistoryRow`, cambiar `pdfUploadError` por estado `pdfError` genérico; en `handlePdfDownload` del componente padre capturar el error y mostrarlo en la fila correspondiente usando un `Map<id, errorMsg>` en el state del componente principal `FundamentalsHistory`

**Acceptance Criteria:**

- Given una FIBRA con 2+ períodos procesados en Main, when el usuario abre la ficha pública, then el selector de período aparece con todos los períodos disponibles y el más reciente seleccionado por defecto.
- Given el selector visible, when el usuario cambia de período, then los KPIs se actualizan con los datos del período seleccionado.
- Given una FIBRA con exactamente 1 período procesado, when la sección se renderiza, then no se muestra el selector (solo el período único).
- Given el botón "Ver PDF" en Ops, when la descarga falla (404 o error de red), then la fila muestra el mensaje de error debajo del botón, igual que el error de subida de PDF ya existente.
- Given la API `GET /api/v1/fundamentals/{ticker}/latest` sin query param, when se llama, then devuelve el mismo resultado que antes (sin regresión).

## Spec Change Log

## Design Notes

**Ordenamiento de períodos:** El backend ordena por año desc, luego por número de trimestre desc (igual que `GetLatestProcessedByFibraAsync`). El frontend recibe la lista ya ordenada y selecciona el primer elemento como default.

**Selector en FibraPage vs FundamentalesSection:** El selector se renderiza dentro de `FibraPage.tsx` encima de `<FundamentalesSection>`, no dentro del componente de sección, para mantener la separación: la sección solo renderiza datos recibidos.

**Error PDF en Ops:** `FundamentalsHistory` ya tiene `pdfUploadError` como estado local en `HistoryRow`. Para errores de descarga, agregar un `Map<string, string>` en el componente padre (`FundamentalsHistory`) para rastrear errores por `record.id`, y pasarlo como prop a `HistoryRow`.

## Verification

**Commands:**
- `dotnet build FIBRADIS.slnx --no-restore -q` -- expected: 0 errors, 0 warnings
- `npm run codegen:api` -- expected: schema actualizado sin errores (solo si se agregan nuevos endpoints)

**Manual checks:**
- Main: abrir `/fibras/DANHOS13`, verificar que el selector aparece, cambiar período y confirmar que los KPIs cambian.
- Ops: abrir Fundamentales, ir al historial de DANHOS13, hacer clic en "Ver PDF" con un registro que tiene PDF — verificar descarga. Probar con un registro sin PDF en disco — verificar que aparece mensaje de error en la fila.

## Suggested Review Order

**Nuevo endpoint de períodos + query param en /latest**

- Contrato de la interfaz: método `GetProcessedPeriodsAsync` agregado al repositorio
  [`IFundamentalRepository.cs:10`](../../src/Server/Application/Fundamentals/IFundamentalRepository.cs#L10)

- Implementación con guard de longitud defensivo contra períodos malformados en DB
  [`FundamentalRepository.cs:23`](../../src/Server/Infrastructure/Persistence/Repositories/Fundamentals/FundamentalRepository.cs#L23)

- Endpoint `/periods` + extensión de `/latest` con query param `?period=`
  [`FundamentalsEndpoints.cs:14`](../../src/Server/Api/Endpoints/Public/FundamentalsEndpoints.cs#L14)

**Selector de período en Main**

- Fetch de períodos + derivación de `activePeriod`; `enabled` espera `periodsFetched` para evitar doble fetch
  [`FibraPage.tsx:83`](../../src/Web/Main/src/modules/ficha-publica/FibraPage.tsx#L83)

- Selector condicional (`length > 1`) alineado con `SectionHeader`
  [`FibraPage.tsx:221`](../../src/Web/Main/src/modules/ficha-publica/FibraPage.tsx#L221)

- API client: `fetchFundamentalesPublic` extendido + nueva `fetchFundamentalesAvailablePeriods`
  [`fundamentalesApi.ts:1`](../../src/Web/Main/src/api/fundamentalesApi.ts#L1)

**Error de descarga PDF en Ops**

- Estado `pdfDownloadErrors` en componente padre; limpieza antes de cada intento
  [`FundamentalsHistory.tsx:19`](../../src/Web/Ops/src/modules/fundamentals/FundamentalsHistory.tsx#L19)

- Prop `pdfDownloadError` en `HistoryRow` y display bajo botones de acción
  [`FundamentalsHistory.tsx:95`](../../src/Web/Ops/src/modules/fundamentals/FundamentalsHistory.tsx#L95)
