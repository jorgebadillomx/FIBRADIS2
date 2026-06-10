# Historia 9.8: Ejecutar pipeline de fundamentales por FIBRA individual

Status: ready-for-dev

## Story

Como operador AdminOps,
quiero disparar el pipeline de fundamentales para una sola FIBRA directamente desde la página de Fundamentales en Ops,
para que pueda actualizar los datos de una FIBRA específica sin esperar la cadencia programada ni procesar las 20 FIBRAs completas.

## Acceptance Criteria

### AC1 — Botón "Ejecutar ahora" por FIBRA en FundamentalsPage

**Dado que** estoy en la página Fundamentales de Ops y he seleccionado una FIBRA (ej. FUNO11) en el formulario/historial,
**Cuando** hago clic en el botón "Ejecutar ahora" que aparece junto al historial de esa FIBRA,
**Entonces** la API encola el pipeline de fundamentales restringido a esa FIBRA, devuelve 202 Accepted, y el botón muestra estado de "Ejecutando…" mientras la mutación está pendiente.

### AC2 — Ejecución sin restricción de cadencia

**Dado que** el pipeline completo de fundamentales corrió hace 2 horas (menos de las 36h normales de cadencia),
**Cuando** disparo "Ejecutar ahora" para FUNO11,
**Entonces** el pipeline procesa solo FUNO11 sin verificar el tiempo transcurrido desde la última ejecución general.

### AC3 — Solo procesa la FIBRA seleccionada

**Dado que** hay 20 FIBRAs activas en catálogo,
**Cuando** disparo "Ejecutar ahora" para FUNO11,
**Entonces** el servicio de automatización procesa únicamente FUNO11 (una fibra, todas las fuentes aplicables),
**Y** en el `PipelineRunLog` resultante, `Details` incluye el campo `"fibra": "FUNO11"` para identificar que fue una corrida parcial.

### AC4 — Auditoría igual que Run Now global

**Dado que** soy el operador jorge@prototipo0.com y ejecuto "Ejecutar ahora" para FHIPO14,
**Cuando** la operación se encola,
**Entonces** se crea un registro `PipelineRunLog` con `Pipeline = "Fundamentals"`, `Status = "Queued"`, `TriggeredBy` = email descifrado del operador y `Details` con `{ "fibra": "FHIPO14", "mode": "single-fibra" }`.

### AC5 — La fibra debe ser activa

**Dado que** intento disparar el pipeline para un ticker que no existe o está inactivo,
**Cuando** el endpoint recibe `POST /api/v1/ops/fundamentals/{ticker}/run`,
**Entonces** devuelve 404 con mensaje `"FIBRA '{ticker}' no encontrada o no está activa."`.

### AC6 — Ningún cambio al Run Now global del Dashboard

**Dado que** el botón "Ejecutar ahora" en la tarjeta Fundamentals del DashboardPage sigue existiendo,
**Cuando** lo presiono,
**Entonces** sigue ejecutando el pipeline completo para todas las FIBRAs activas (comportamiento sin cambios).

## Tasks / Subtasks

- [ ] T1: Extender `IFundamentalsAutomationService` y su implementación (AC2, AC3)
  - [ ] T1.1: Añadir `Task<FundamentalsAutomationRunResult> ExecuteAsync(string ticker, CancellationToken ct)` a `Application.Fundamentals.IFundamentalsAutomationService`
  - [ ] T1.2: En `FundamentalsAutomationService`, extraer el bucle interno a método privado `RunForFibrasAsync(IReadOnlyList<Fibra> fibras, CancellationToken ct)`
  - [ ] T1.3: Implementar `ExecuteAsync(string ticker, ct)`: busca la fibra con `GetByTickerAsync`, valida que `fibra is not null && fibra.IsActive`, y llama a `RunForFibrasAsync([fibra], ct)`
  - [ ] T1.4: El método existente `ExecuteAsync(CancellationToken ct)` pasa a llamar `RunForFibrasAsync(await fibraRepo.GetAllActiveAsync(ct), ct)`
  - [ ] T1.5: Ejecutar `dotnet test tests/Unit/` — todos pasan

- [ ] T2: Añadir método `ExecuteForFibraAsync` a `FundamentalsPipelineJob` (AC2, AC3, AC4)
  - [ ] T2.1: Añadir método `public async Task ExecuteForFibraAsync(string ticker, CancellationToken ct = default)` — sin `[DisableConcurrentExecution]` para permitir corridas paralelas por FIBRA
  - [ ] T2.2: El método llama `automationService.ExecuteAsync(ticker, ct)` y al final escribe `PipelineRunLog` con `Details = JsonSerializer.Serialize(new { fibra = ticker, mode = "single-fibra" })`
  - [ ] T2.3: Si `automationService.ExecuteAsync(ticker)` lanza `InvalidOperationException` (ticker no activo), el catch registra status = "Failed" con el mensaje en Details

- [ ] T3: Endpoint `POST /api/v1/ops/fundamentals/{ticker}/run` (AC1, AC4, AC5)
  - [ ] T3.1: Añadir el endpoint en `OpsFundamentalsEndpoints.MapOpsFundamentals` — grupo existente `/api/v1/ops/fundamentals` con `RequireAuthorization("AdminOps")`
  - [ ] T3.2: Resolver `ticker` con `fibraRepo.GetByTickerAsync(ticker, ct)` — si null o `!fibra.IsActive`, return 404 Problem
  - [ ] T3.3: Encolar `jobClient.Enqueue<FundamentalsPipelineJob>(j => j.ExecuteForFibraAsync(ticker, CancellationToken.None))`
  - [ ] T3.4: Llamar `TryLogQueuedRunAsync` con los datos del actor — reutilizar el helper privado movido o duplicado (ver Dev Notes)
  - [ ] T3.5: Return `Results.Accepted()`
  - [ ] T3.6: Añadir `.Produces(202)`, `.ProducesProblem(404)`, `.ProducesProblem(401)`, `.ProducesProblem(403)`

- [ ] T4: Frontend — botón en `FundamentalsHistory` (AC1)
  - [ ] T4.1: Añadir prop `ticker: string` a `FundamentalsHistory` (ya recibe `fibraId`)
  - [ ] T4.2: Añadir función `runFundamentalsForFibra(ticker: string)` en `fundamentalsApi.ts` (o `dashboardApi.ts`) — `POST /api/v1/ops/fundamentals/{ticker}/run`
  - [ ] T4.3: Añadir `useMutation` en `FundamentalsHistory` que llame a esa función
  - [ ] T4.4: Renderizar botón "Ejecutar ahora" en el header de `FundamentalsHistory`, alineado a la derecha — deshabilitado mientras `mutation.isPending`; texto: "Ejecutar ahora" / "Ejecutando…"
  - [ ] T4.5: En `FundamentalsPage`, pasar `ticker` junto a `fibraId` a `FundamentalsHistory` — extraer ticker de la lista de FIBRAs del catálogo (que ya carga `FundamentalsImportForm`) o añadir prop `selectedFibraTicker` al estado existente

- [ ] T5: Regenerar cliente API y verificar tipos (AC1)
  - [ ] T5.1: `npm run codegen:api` desde raíz — el nuevo endpoint aparece en `paths` del contrato OpenAPI
  - [ ] T5.2: Actualizar `runFundamentalsForFibra` para usar el path tipado del client generado
  - [ ] T5.3: `npm run build` en Ops SPA — sin errores TS

- [ ] T6: Unit tests (AC2, AC3, AC5)
  - [ ] T6.1: `FundamentalsAutomationServiceTests` — test `ExecuteAsync(ticker)` con fibra activa: solo esa fibra pasa a `RunForFibrasAsync`
  - [ ] T6.2: `FundamentalsAutomationServiceTests` — test `ExecuteAsync(ticker)` con ticker inexistente: lanza `InvalidOperationException`
  - [ ] T6.3: `FundamentalsAutomationServiceTests` — test `ExecuteAsync(ticker)` con fibra inactiva: lanza `InvalidOperationException`

- [ ] T7: Actualizar sprint-status y story
  - [ ] T7.1: Marcar `9-8-fundamentales-ejecutar-por-fibra: in-progress` al empezar implementación
  - [ ] T7.2: Completar File List y Completion Notes antes de marcar review

## Dev Notes

### Contexto y motivación

El pipeline de fundamentales tiene hoy una sola palanca de control manual: el botón "Ejecutar ahora" en el Dashboard Operativo, que lanza `FundamentalsAutomationService.ExecuteAsync()` para las 20 FIBRAs activas. Una corrida completa puede tardar varios minutos (descubrimiento multi-fuente + descarga PDFs + llamadas IA por cada candidato nuevo).

El operador frecuentemente necesita refrescar una sola FIBRA (ej. acaban de subir el reporte de FUNO11 y quiere procesarlo sin esperar la corrida programada). Esta historia añade esa granularidad.

### Ubicación del botón en la UI

El botón **no va en DashboardPage** (esa tarjeta es para el pipeline global). Va en **FundamentalsPage > FundamentalsHistory**, en el header de la sección de historial, junto al título, alineado a la derecha:

```
┌─────────────────────────────────────────────────────┐
│  Historial — FUNO11           [Ejecutar ahora]      │
│  ─────────────────────────────────────────────────  │
│  Q1-2026 | processed | ...                          │
└─────────────────────────────────────────────────────┘
```

El botón solo aparece cuando `FundamentalsHistory` está montado (fibra seleccionada).

### Diseño del backend

#### No cambiar `[DisableConcurrentExecution]` en el método existente

`FundamentalsPipelineJob.ExecuteAsync(bool forceRun)` conserva `[DisableConcurrentExecution(timeoutInSeconds: 0)]` — sigue siendo un singleton. El nuevo método `ExecuteForFibraAsync(string ticker)` es un método independiente en la misma clase, **sin** el atributo, permitiendo que múltiples corridas por-FIBRA se encolen concurrentemente.

Riesgo aceptado: si una corrida completa y una corrida individual procesan la misma FIBRA simultáneamente, el manifest check existente (`GetBySourceAndPackageUrlAsync`) actúa como guard de idempotencia al nivel de candidatos — no se procesará dos veces el mismo PDF.

#### Extracción del bucle interno

Estado actual (simplificado):
```csharp
public async Task<FundamentalsAutomationRunResult> ExecuteAsync(CancellationToken ct)
{
    var fibras = await fibraRepo.GetAllActiveAsync(ct);
    // ... bucle foreach (var fibra in fibras) ...
}
```

Estado futuro:
```csharp
public async Task<FundamentalsAutomationRunResult> ExecuteAsync(CancellationToken ct)
    => await RunForFibrasAsync(await fibraRepo.GetAllActiveAsync(ct), ct);

public async Task<FundamentalsAutomationRunResult> ExecuteAsync(string ticker, CancellationToken ct)
{
    var fibra = await fibraRepo.GetByTickerAsync(ticker, ct);
    if (fibra is null || !fibra.IsActive)
        throw new InvalidOperationException($"FIBRA '{ticker}' no encontrada o no está activa.");
    return await RunForFibrasAsync([fibra], ct);
}

private async Task<FundamentalsAutomationRunResult> RunForFibrasAsync(
    IReadOnlyList<Fibra> fibras, CancellationToken ct)
{
    // TODO move existing logic here (variables locales, foreach, return)
}
```

No se cambia ningún comportamiento de la lógica interna — solo se envuelve.

#### `TryLogQueuedRunAsync` — helper compartido

El helper privado `TryLogQueuedRunAsync` está actualmente en `OpsMarketEndpoints`. Para reutilizarlo desde `OpsFundamentalsEndpoints`, hay dos opciones:

**Opción A (recomendada):** Duplicarlo en `OpsFundamentalsEndpoints` como método privado estático idéntico. Son ~20 líneas. No justifica extraerlo a una clase compartida.

**Opción B:** Moverlo a una clase utilitaria `OpsEndpointHelpers` y compartirlo. Más limpio a largo plazo, pero más cambio.

Usar **Opción A** para esta historia — no agregar abstracciones que no se necesitan ahora.

#### Endpoint

```
POST /api/v1/ops/fundamentals/{ticker}/run
Authorization: Bearer <AdminOps token>
→ 202 Accepted (siempre que el ticker exista y esté activo)
→ 404 Problem (ticker no encontrado o inactivo)
→ 401/403 sin token o rol inadecuado
```

El endpoint queda en el mismo `MapOpsFundamentals` en `OpsFundamentalsEndpoints.cs` — el grupo ya tiene `RequireAuthorization("AdminOps")`.

#### PipelineRunLog — Details de la corrida individual

Al encolar, se crea un registro "Queued" con:
```json
{ "fibra": "FUNO11", "mode": "single-fibra" }
```

Al completar, `FundamentalsPipelineJob.ExecuteForFibraAsync` escribe el log definitivo con los mismos counters que la corrida global (FibrasScanned=1, etc.).

#### `IBackgroundJobClient` en `OpsFundamentalsEndpoints`

El endpoint necesita `IBackgroundJobClient`. Inyectarlo como parámetro del lambda (mismo patrón que `OpsMarketEndpoints`):

```csharp
group.MapPost("/{ticker}/run", async (
    string ticker,
    IBackgroundJobClient jobClient,
    IFibraRepository fibraRepo,
    IPipelineRunLogRepository runLogRepo,
    IEmailEncryptor emailEncryptor,
    ILoggerFactory loggerFactory,
    HttpContext ctx,
    CancellationToken ct) =>
{
    var fibra = await fibraRepo.GetByTickerAsync(ticker, ct);
    if (fibra is null || !fibra.IsActive)
        return Results.Problem($"FIBRA '{ticker}' no encontrada o no está activa.", statusCode: 404);

    jobClient.Enqueue<FundamentalsPipelineJob>(j => j.ExecuteForFibraAsync(ticker, CancellationToken.None));
    await TryLogQueuedRunAsync("Fundamentals", ticker, ctx, runLogRepo, emailEncryptor, loggerFactory.CreateLogger("OpsFundamentals"), ct);
    return Results.Accepted();
})
```

### Frontend — pasar ticker a FundamentalsHistory

`FundamentalsPage` mantiene `selectedFibraId: string | null`. El `FundamentalsImportForm` ya carga el catálogo de FIBRAs para el dropdown — podemos añadir `onFibraTickerChange: (ticker: string) => void` al mismo callback `onFibraChange`, o añadir un estado separado `selectedFibraTicker: string | null`.

**Recomendación**: añadir `selectedFibraTicker` al estado existente de `FundamentalsPage`:

```typescript
interface State {
  editRecord: FundamentalRecordDto | null
  selectedFibraId: string | null
  selectedFibraTicker: string | null  // NUEVO
}
```

Y en `FundamentalsImportForm` añadir `onFibraTickerChange?: (ticker: string) => void` para propagarlo. Alternativa más simple: cuando el historial carga los `records`, el primer record tiene `fibraTicker` — tomar ese valor si está disponible.

**Alternativa más simple (recomendada para no tocar FundamentalsImportForm):** En `FundamentalsHistory`, una vez que carga la query de records, leer `records[0]?.fibraTicker` para mostrar en el header y pasar al botón. Como el componente ya tiene `fibraId` prop, el ticker se puede leer del primer registro:

```typescript
const ticker = records[0]?.fibraTicker ?? null
// botón solo habilitado si ticker !== null
```

Esto evita prop drilling y cambios en FundamentalsImportForm.

### API client — nuevo endpoint tipado

Tras `npm run codegen:api`, el path `/api/v1/ops/fundamentals/{ticker}/run` aparece en el contrato. La función en `fundamentalsApi.ts`:

```typescript
export async function runFundamentalsForFibra(ticker: string): Promise<void> {
  assertOpsAccessToken()
  const { error } = await apiClient['/api/v1/ops/fundamentals/{ticker}/run'].POST({
    headers: getOpsAuthHeaders(),
    params: { path: { ticker } },
  })
  if (error) throw new Error(getOpsApiErrorMessage(error, `Error al ejecutar fundamentales para ${ticker}`))
}
```

### Archivos a modificar / crear

| Acción | Archivo |
|--------|---------|
| MODIFICAR | `src/Server/Application/Fundamentals/IFundamentalsAutomationService.cs` |
| MODIFICAR | `src/Server/Infrastructure/Jobs/Fundamentals/FundamentalsAutomationService.cs` |
| MODIFICAR | `src/Server/Infrastructure/Jobs/Fundamentals/FundamentalsPipelineJob.cs` |
| MODIFICAR | `src/Server/Api/Endpoints/Ops/OpsFundamentalsEndpoints.cs` |
| MODIFICAR | `src/Web/Ops/src/api/fundamentalsApi.ts` (o nuevo archivo `runApi.ts`) |
| MODIFICAR | `src/Web/Ops/src/modules/fundamentals/FundamentalsHistory.tsx` |
| CREAR | `tests/Unit/Application.Tests/Fundamentals/FundamentalsAutomationServiceSingleFibraTests.cs` |

No se requiere migración de BD. No se cambia contrato OpenAPI existente (solo se añade un path nuevo).

### Security Checklist — completar antes del primer commit

- [x] **TOCTOU doble-click**: Dos clicks rápidos encolan dos jobs de Hangfire para el mismo ticker. El manifest check existente en `FundamentalsAutomationService` (`GetBySourceAndPackageUrlAsync`) garantiza idempotencia a nivel de candidato — el mismo PDF no se procesa dos veces. Riesgo residual aceptado.
- [x] **Auth-gating**: El endpoint está bajo `RequireAuthorization("AdminOps")`. El botón está en una página AdminOps-only. No hay surface pública.
- [x] **Denominador cero**: Esta historia no añade cálculo financiero.
- [x] **Ticker injection en URL**: El ticker viene de la selección del dropdown en la UI (valores del catálogo), no de texto libre del usuario. En el endpoint, se resuelve contra la BD (`GetByTickerAsync`) antes de usarlo — no se interpola en SQL ni en comandos del sistema.

### Project Structure Notes

```
Application.Fundamentals
  IFundamentalsAutomationService  ← añadir overload con ticker
  
Infrastructure.Jobs.Fundamentals
  FundamentalsAutomationService   ← extraer RunForFibrasAsync, implementar overload
  FundamentalsPipelineJob         ← añadir ExecuteForFibraAsync sin DisableConcurrentExecution

Api.Endpoints.Ops
  OpsFundamentalsEndpoints        ← añadir POST /{ticker}/run

Web/Ops
  api/fundamentalsApi.ts          ← añadir runFundamentalsForFibra
  modules/fundamentals/
    FundamentalsHistory.tsx       ← añadir prop ticker + botón + mutation
    
tests/Unit/Application.Tests/Fundamentals/
  FundamentalsAutomationServiceSingleFibraTests.cs  ← 3+ tests
```

### References

- `FundamentalsAutomationService.ExecuteAsync` — `src/Server/Infrastructure/Jobs/Fundamentals/FundamentalsAutomationService.cs:28`
- `FundamentalsPipelineJob.ExecuteAsync` — `src/Server/Infrastructure/Jobs/Fundamentals/FundamentalsPipelineJob.cs:19`
- `OpsMarketEndpoints` — patrón Run Now + TryLogQueuedRunAsync — `src/Server/Api/Endpoints/Ops/OpsMarketEndpoints.cs:91-132`
- `DashboardPage.tsx` — botón Run Now global existente (no tocar) — `src/Web/Ops/src/pages/DashboardPage.tsx:44-55`
- `FundamentalsHistory.tsx` — componente donde va el nuevo botón — `src/Web/Ops/src/modules/fundamentals/FundamentalsHistory.tsx`
- `IFibraRepository.GetByTickerAsync` — ya existe, retorna `Fibra?` — `src/Server/Application/Catalog/IFibraRepository.cs:14`

## Dev Agent Record

### Agent Model Used

(pendiente de implementación)

### Debug Log References

### Completion Notes List

### File List
