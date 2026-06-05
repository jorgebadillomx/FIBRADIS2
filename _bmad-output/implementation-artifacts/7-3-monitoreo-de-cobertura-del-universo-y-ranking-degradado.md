# Historia 7.3: Monitoreo de cobertura del universo y ranking degradado

Status: done

## Story

Como usuario,
quiero recibir una advertencia cuando una porción significativa del universo activo de FIBRAs no tiene precio actual, y que el ranking se suspenda si la cobertura es críticamente baja,
para que no tome decisiones basadas en un ranking engañosamente parcial.

## Acceptance Criteria

### AC1 — Banner "Universo degradado" (umbral por defecto: 30%)

**Dado que** 8 de 25 FIBRAs activas (32%) no tienen precio actual,
**Cuando** veo el ranking del universo en Oportunidades,
**Entonces** aparece un banner prominente: "Universo degradado: 8 FIBRAs (32.0%) sin precio disponible. Último dato válido: [fecha/hora del snapshot más reciente con precio válido]." El ranking permanece visible debajo del banner.

### AC2 — Suspensión crítica del ranking (umbral fijo: 50%)

**Dado que** 13 de 25 FIBRAs activas (52%) no tienen precio actual,
**Cuando** veo Oportunidades,
**Entonces** la tabla de ranking es reemplazada por: "Ranking no disponible — cobertura insuficiente (52.0% de FIBRAs sin precio). El ranking se restaurará cuando la cobertura supere el 50%." El banner del AC1 no aparece (la suspensión lo reemplaza).

### AC3 — Umbral de degradación configurable desde Ops

**Dado que** AdminOps actualiza el umbral de degradación de 30% a 20% en la Configuración de Ops,
**Cuando** el endpoint de oportunidades evalúa el universo,
**Entonces** el nuevo umbral se aplica sin redespliegue, y con 22% de FIBRAs sin precio el universo muestra el banner de degradado.

## Tasks / Subtasks

### T1 — Backend: campo `UniverseDegradationThresholdPct` en OperationalConfig (AC: 3)

- [x] T1.1 — Agregar `public int UniverseDegradationThresholdPct { get; set; } = 30;` a `src/Server/Domain/Ops/OperationalConfig.cs`
- [x] T1.2 — Agregar `UniverseDegradationThresholdPct` a `SharedApiContracts/Ops/OperationalConfigDto.cs` (parámetro posicional nuevo al final del record)
- [x] T1.3 — Agregar `int? UniverseDegradationThresholdPct = null` a `SharedApiContracts/Ops/UpdateOperationalConfigRequest.cs`
- [x] T1.4 — Agregar parámetro `int? universeDegradationThresholdPct` a `IOperationalConfigRepository.UpdateAsync`
- [x] T1.5 — Actualizar `OperationalConfigRepository.UpdateAsync` en Infrastructure — seguir el patrón exacto de los otros campos opcionales
- [x] T1.6 — Actualizar `OpsConfigEndpoints.cs`: `ToDto`, llamada a `UpdateAsync`, y `ValidateRequest` (rango válido: 1–49)
- [x] T1.7 — Crear migración EF Core: `dotnet ef migrations add AddUniverseDegradationThreshold --project src/Server/Infrastructure --startup-project src/Server/Api`

### T2 — Backend: lógica de cobertura del universo (AC: 1, 2)

- [x] T2.1 — Crear `src/Server/Application/Opportunities/UniverseCoverageCalculator.cs` (clase estática pura)
- [x] T2.2 — Crear `src/Server/SharedApiContracts/Opportunities/UniverseCoverageDto.cs`
- [x] T2.3 — Actualizar `src/Server/SharedApiContracts/Opportunities/OpportunityRankingResponseDto.cs` — agregar `UniverseCoverageDto Coverage` como cuarto parámetro posicional
- [x] T2.4 — Actualizar `src/Server/Api/Endpoints/Private/OpportunityEndpoints.cs`:
  - Inyectar `IOperationalConfigRepository configRepo`
  - Agregar `configTask` al primer `Task.WhenAll`
  - Calcular cobertura usando `UniverseCoverageCalculator.Calculate`
  - Construir y pasar `UniverseCoverageDto` al `OpportunityRankingResponseDto`

### T3 — Unit tests: UniverseCoverageCalculator (AC: 1, 2)

- [x] T3.1 — Crear `tests/Unit/Application.Tests/Opportunities/UniverseCoverageCalculatorTests.cs` con los 7 casos descritos en Dev Notes

### T4 — Frontend Main: banner de cobertura en OportunidadesPage (AC: 1, 2)

- [x] T4.1 — Ejecutar `npm run codegen:api` para regenerar tipos con los nuevos campos del cliente tipado
- [x] T4.2 — Modificar `src/Web/Main/src/modules/oportunidades/OportunidadesPage.tsx`:
  - Leer `rankingQuery.data?.coverage`
  - En el tab "Universo": mostrar banner ámbar cuando `status === 'Degraded'`, encima de todo el contenido
  - Cuando `status === 'Suspended'`: reemplazar el área de la tabla de ranking por el mensaje de suspensión (el configurador de pesos se mantiene visible)
  - Cuando `status === 'Normal'` o undefined: sin cambios visibles

### T5 — Frontend Ops: campo umbral en ConfigPage (AC: 3)

- [x] T5.1 — Modificar `src/Web/Ops/src/pages/ConfigPage.tsx`:
  - Agregar `universeDegradationThresholdPct: number` a `FormValues`
  - Agregar el campo al `reset()` con valor de `configQuery.data.universeDegradationThresholdPct ?? 30`
  - Agregar al `onSubmit` con `dirtyFields` guard
  - Agregar `<Field>` con `<input type="number" min=1 max=49>` en la grilla de parámetros operativos

### T6 — Validación y build (AC: 1, 2, 3)

- [x] T6.1 — `dotnet build FIBRADIS.slnx` — 0 errores
- [x] T6.2 — `dotnet test tests/Unit/` — todos los tests verdes (incluyendo los 7 nuevos)
- [x] T6.3 — `npm run build --workspace=src/Web/Main` — 0 errores TypeScript
- [x] T6.4 — `npm run build --workspace=src/Web/Ops` — 0 errores TypeScript

### Review Findings

- [x] \[Review\]\[Patch\] configRepo agregado al Task.WhenAll viola convención DbContext — mover await de configRepo fuera del WhenAll `src/Server/Api/Endpoints/Private/OpportunityEndpoints.cs`
- [x] \[Review\]\[Patch\] Mensaje de suspensión ambiguo: en contexto de missingPct visible, "cuando la cobertura supere el 50%" puede leerse en sentido contrario `src/Web/Main/src/modules/oportunidades/OportunidadesPage.tsx`
- [x] \[Review\]\[Patch\] Etiqueta técnica `"universe_degradation_threshold_pct"` en ConfigPage — usar label legible en español `src/Web/Ops/src/pages/ConfigPage.tsx`
- [x] \[Review\]\[Defer\] Pre-existing Task.WhenAll con 3 repos compartiendo AppDbContext `src/Server/Api/Endpoints/Private/OpportunityEndpoints.cs` — deferred, pre-existing
- [x] \[Review\]\[Defer\] lastValidPriceAt itera todos los snapshots; fibrasWithPrice solo FIBRAs activas `src/Server/Api/Endpoints/Private/OpportunityEndpoints.cs` — deferred, pre-existing
- [x] \[Review\]\[Defer\] universeSize == 0 retorna "Normal" — comportamiento consciente y testeado `src/Server/Application/Opportunities/UniverseCoverageCalculator.cs` — deferred, pre-existing
- [x] \[Review\]\[Defer\] UpdateData en migración redundante con defaultValue: 30 `src/Server/Infrastructure/Migrations/20260605183529_AddUniverseDegradationThreshold.cs` — deferred, pre-existing
- [x] \[Review\]\[Defer\] `degradationThresholdPct = 0` desde SQL directo haría todo "Degraded" perpetuamente — backend valida 1–49 y GetAsync usa default=30; solo alcanzable vía manipulación directa de BD `src/Server/Application/Opportunities/UniverseCoverageCalculator.cs` — deferred, only via direct SQL
- [x] \[Review\]\[Defer\] Status strings `"Normal"/"Degraded"/"Suspended"` sin type union en TypeScript — frágiles a renombres silenciosos `src/Web/Main/src/modules/oportunidades/OportunidadesPage.tsx` — deferred, patrón pre-existente en proyecto
- [x] \[Review\]\[Defer\] Tests sin casos boundary: `fibrasWithPrice > universeSize`, `threshold=0`, propagación de `lastValidPriceAt` `tests/Unit/Application.Tests/Opportunities/UniverseCoverageCalculatorTests.cs` — deferred, mejoras incrementales

## Dev Notes

### Visión general — qué cambia

**Backend:**
- `OperationalConfig` gana un campo `int UniverseDegradationThresholdPct` (default 30)
- Nuevo `UniverseCoverageCalculator` calcula si el universo está Normal/Degraded/Suspended
- `OpportunityRankingResponseDto` agrega un campo `Coverage: UniverseCoverageDto`
- El endpoint `GET /api/v1/opportunities` lee la config y pasa la cobertura en la respuesta

**Frontend Main:**
- `OportunidadesPage.tsx` muestra banners contextuales en el tab Universo

**Frontend Ops:**
- `ConfigPage.tsx` expone el campo `universeDegradationThresholdPct` como input numérico

No hay cambios al score ni al algoritmo de percentiles. Solo se agrega metadata de cobertura a la respuesta existente.

---

### T1.1–T1.6: Extender OperationalConfig

#### OperationalConfig.cs (agregar al final de la clase):
```csharp
public int UniverseDegradationThresholdPct { get; set; } = 30;
```

#### OperationalConfigDto.cs — patrón posicional record:
```csharp
public sealed record OperationalConfigDto(
    decimal CommissionFactor,
    int AvgPeriods,
    int NewsCadenceMinutes,
    int FibraNewsMonths,
    int FundamentalsCadenceMinutes,
    int DistributionCadenceMinutes,
    DateTimeOffset UpdatedAt,
    string? UpdatedBy,
    bool TermsEnabled,
    string? TermsText,
    string? ContactEmail,
    int UniverseDegradationThresholdPct);  // NUEVO — siempre al final
```

#### UpdateOperationalConfigRequest.cs:
```csharp
public sealed record UpdateOperationalConfigRequest(
    decimal? CommissionFactor,
    int? AvgPeriods,
    int? NewsCadenceMinutes,
    int? FibraNewsMonths,
    int? FundamentalsCadenceMinutes = null,
    int? DistributionCadenceMinutes = null,
    bool? TermsEnabled = null,
    string? TermsText = null,
    string? ContactEmail = null,
    int? UniverseDegradationThresholdPct = null);  // NUEVO
```

#### IOperationalConfigRepository.cs — agregar parámetro al final:
```csharp
Task UpdateAsync(
    decimal? commissionFactor,
    int? avgPeriods,
    int? newsCadenceMinutes,
    int? fibraNewsMonths,
    int? fundamentalsCadenceMinutes,
    int? distributionCadenceMinutes,
    bool? termsEnabled,
    string? termsText,
    string? contactEmail,
    string actor,
    int? universeDegradationThresholdPct = null,  // NUEVO — con default para no romper callers
    CancellationToken ct = default);
```

#### OpsConfigEndpoints.cs — cambios mínimos necesarios:

1. En `ToDto`: agregar `config.UniverseDegradationThresholdPct` como último argumento al constructor del record.

2. En el `MapPut("/config")`, pasar `request.UniverseDegradationThresholdPct` al `repo.UpdateAsync`.

3. En `ValidateRequest`: agregar validación:
```csharp
if (request.UniverseDegradationThresholdPct is not null &&
    (request.UniverseDegradationThresholdPct < 1 || request.UniverseDegradationThresholdPct > 49))
{
    errors["universeDegradationThresholdPct"] =
        ["universeDegradationThresholdPct debe estar entre 1 y 49."];
}
```

4. También actualizar la validación de "ningún campo proporcionado" para incluir el nuevo campo:
```csharp
&& request.UniverseDegradationThresholdPct is null
```

#### OperationalConfigRepository.cs (Infrastructure):
Buscar el archivo en `src/Server/Infrastructure/`. Seguir exactamente el mismo patrón que los campos existentes para aplicar la actualización cuando el parámetro no es null.

#### T1.7 — Migración EF Core:

> ⚠️ Si el proceso de la API tiene los DLLs bloqueados, detenerlo primero. Si es necesario: `--configuration Release`.

```bash
dotnet ef migrations add AddUniverseDegradationThreshold --project src/Server/Infrastructure --startup-project src/Server/Api
```

Verificar que la migración generada agrega la columna con `defaultValue: 30` en la tabla `operational_config` del schema `ops`. El tipo debe ser `integer not null default 30`.

---

### T2.1: UniverseCoverageCalculator.cs

```csharp
// src/Server/Application/Opportunities/UniverseCoverageCalculator.cs
namespace Application.Opportunities;

public sealed record UniverseCoverage(
    int UniverseSize,
    int FibrasWithPrice,
    decimal MissingPct,
    int DegradationThresholdPct,
    string Status,               // "Normal" | "Degraded" | "Suspended"
    DateTimeOffset? LastValidPriceAt);

public static class UniverseCoverageCalculator
{
    public const decimal SuspensionThresholdPct = 50m;

    public static UniverseCoverage Calculate(
        int universeSize,
        int fibrasWithPrice,
        int degradationThresholdPct,
        DateTimeOffset? lastValidPriceAt)
    {
        if (universeSize == 0)
            return new UniverseCoverage(0, 0, 0m, degradationThresholdPct, "Normal", lastValidPriceAt);

        var missingPct = Math.Round(
            (decimal)(universeSize - fibrasWithPrice) / universeSize * 100m, 1);

        var status = missingPct >= SuspensionThresholdPct ? "Suspended"
            : missingPct >= degradationThresholdPct ? "Degraded"
            : "Normal";

        return new UniverseCoverage(
            universeSize, fibrasWithPrice, missingPct,
            degradationThresholdPct, status, lastValidPriceAt);
    }
}
```

---

### T2.2: UniverseCoverageDto.cs

```csharp
// src/Server/SharedApiContracts/Opportunities/UniverseCoverageDto.cs
namespace SharedApiContracts.Opportunities;

public sealed record UniverseCoverageDto(
    int UniverseSize,
    int FibrasWithPrice,
    decimal MissingPct,
    int DegradationThresholdPct,
    string Status,                    // "Normal" | "Degraded" | "Suspended"
    DateTimeOffset? LastValidPriceAt
);
```

---

### T2.3: OpportunityRankingResponseDto.cs actualizado

```csharp
namespace SharedApiContracts.Opportunities;

public sealed record OpportunityRankingResponseDto(
    IReadOnlyList<OpportunityFibraRowDto> Ranked,
    IReadOnlyList<OpportunityFibraRowDto> LimitedData,
    OpportunityWeightsDto Weights,
    UniverseCoverageDto Coverage   // NUEVO
);
```

---

### T2.4: OpportunityEndpoints.cs — cambios

Agregar `using Application.Ops;` al bloque de usings existente.

En `MapGet("/")`, agregar `IOperationalConfigRepository configRepo` como parámetro del lambda. El primer bloque de carga paralela queda así:

```csharp
var fibrasTask = fibraRepo.GetAllActiveAsync(ct);
var snapshotsTask = marketRepo.GetLatestSnapshotPerFibraAsync(ct);
var fundamentalsTask = fundamentalRepo.GetSummaryLatestAsync(ct);
var configTask = configRepo.GetAsync(ct);                         // NUEVO

await Task.WhenAll(fibrasTask, snapshotsTask, fundamentalsTask, configTask);

var fibras = await fibrasTask;
var snapshots = await snapshotsTask;
var fundamentals = await fundamentalsTask;
var config = await configTask;                                    // NUEVO
```

Después de `var scores = OpportunityScoreCalculator.Calculate(...)` y antes del `return`, agregar:

```csharp
// Coverage
var fibrasWithPrice = fibras.Count(f =>
    snapshotByFibra.TryGetValue(f.Id, out var snap) && snap.LastPrice is > 0m);
var lastValidPriceAt = snapshotByFibra.Values
    .Where(s => s.LastPrice is > 0m)
    .MaxBy(s => s.CapturedAt)?.CapturedAt;
var coverage = UniverseCoverageCalculator.Calculate(
    fibras.Count, fibrasWithPrice,
    config.UniverseDegradationThresholdPct, lastValidPriceAt);
var coverageDto = new UniverseCoverageDto(
    coverage.UniverseSize, coverage.FibrasWithPrice, coverage.MissingPct,
    coverage.DegradationThresholdPct, coverage.Status, coverage.LastValidPriceAt);
```

Actualizar el `return`:

```csharp
return Results.Ok(new OpportunityRankingResponseDto(ranked, limitedData, ToDto(weights), coverageDto));
```

---

### T3.1: UniverseCoverageCalculatorTests.cs — 7 casos

Archivo: `tests/Unit/Application.Tests/Opportunities/UniverseCoverageCalculatorTests.cs`

Casos obligatorios:

| # | Escenario | Entrada | `Status` esperado |
|---|-----------|---------|-------------------|
| 1 | Normal: 20% sin precio, umbral 30 | 5/25 sin precio | `"Normal"` |
| 2 | Degradado: 32% sin precio, umbral 30 | 8/25 sin precio | `"Degraded"` |
| 3 | Suspendido: 52% sin precio | 13/25 sin precio | `"Suspended"` |
| 4 | Exactamente en umbral de degradación | 7.5/25 = 30% sin precio (ej. 3/10 con umbral 30) | `"Degraded"` |
| 5 | Exactamente en 50% → Suspendido | 5/10 sin precio | `"Suspended"` |
| 6 | Umbral personalizado 20%: 22% sin precio | umbral=20, 22 sin precio de 100 | `"Degraded"` |
| 7 | Universo vacío (size=0) | universeSize=0 | `"Normal"` |

Verificar también el valor de `MissingPct` en los casos 1-6 para asegurar el redondeo a 1 decimal.

Patrón de test — copiar del archivo existente `OpportunityScoreCalculatorTests.cs`:
```csharp
using Application.Opportunities;

namespace Application.Tests.Opportunities;

public class UniverseCoverageCalculatorTests
{
    [Fact]
    public void Calculate_NormalState_WhenMissingBelowThreshold()
    {
        var result = UniverseCoverageCalculator.Calculate(
            universeSize: 25,
            fibrasWithPrice: 20,        // 5 sin precio = 20%
            degradationThresholdPct: 30,
            lastValidPriceAt: null);

        Assert.Equal("Normal", result.Status);
        Assert.Equal(20.0m, result.MissingPct);
    }
    // ... demás casos
}
```

---

### T4.2: OportunidadesPage.tsx — banners de cobertura

Los nuevos tipos del cliente generado expondrán `rankingQuery.data.coverage` con shape:
```ts
coverage: {
  universeSize: number
  fibrasWithPrice: number
  missingPct: number
  degradationThresholdPct: number
  status: string   // "Normal" | "Degraded" | "Suspended"
  lastValidPriceAt: string | null
}
```

Agregar helpers de formato local (no importar nada nuevo):
```ts
const coverage = rankingQuery.data?.coverage
const fibrasWithoutPrice = coverage ? coverage.universeSize - coverage.fibrasWithPrice : 0
```

**Banner Degradado** (ámbar) — insertar ANTES del configurador de pesos en el tab Universo:
```tsx
{coverage?.status === 'Degraded' && (
  <div className="rounded-xl border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-800">
    Universo degradado: {fibrasWithoutPrice} FIBRAs ({coverage.missingPct.toFixed(1)}%) sin precio disponible.
    {coverage.lastValidPriceAt && (
      <> Último dato válido:{' '}
        {new Date(coverage.lastValidPriceAt).toLocaleString('es-MX', {
          dateStyle: 'medium',
          timeStyle: 'short',
        })}.
      </>
    )}
  </div>
)}
```

**Estado Suspendido** — reemplaza solo el área de la tabla de ranking (el configurador de pesos se mantiene). Envolver el bloque de `<RankingTable ...>` (y la sección de `limitedData`) en:
```tsx
{coverage?.status === 'Suspended' ? (
  <div className="rounded-xl border border-rose-200 bg-rose-50 px-5 py-8 text-center text-sm text-rose-700">
    Ranking no disponible — cobertura insuficiente ({coverage.missingPct.toFixed(1)}% de FIBRAs sin precio).
    El ranking se restaurará cuando la cobertura supere el 50%.
  </div>
) : (
  <>
    {/* RankingTable ranked */}
    {/* RankingTable limitedData (si aplica) */}
  </>
)}
```

> **Estado Normal (`status === 'Normal'` o `coverage === undefined`):** sin cambios visuales — el comportamiento existente se mantiene intacto.

> **El banner Degradado y el estado Suspendido son mutuamente excluyentes** — solo uno de los dos aparece a la vez. No mostrar el banner ámbar cuando ya se muestra la suspensión.

---

### T5.1: ConfigPage.tsx en Ops SPA

Agregar a `FormValues`:
```ts
interface FormValues {
  commissionFactor: number
  avgPeriods: number
  newsCadenceMinutes: number
  fibraNewsMonths: number
  fundamentalsCadenceMinutes: number
  distributionCadenceMinutes: number
  universeDegradationThresholdPct: number  // NUEVO
}
```

Agregar al `defaultValues` del `useForm`:
```ts
universeDegradationThresholdPct: 30,
```

Agregar al `reset(...)` en el `useEffect`:
```ts
universeDegradationThresholdPct: Number(configQuery.data.universeDegradationThresholdPct ?? 30),
```

Agregar al `onSubmit`:
```ts
if (dirtyFields.universeDegradationThresholdPct)
  payload.universeDegradationThresholdPct = values.universeDegradationThresholdPct
```

Agregar al JSX en la grilla de parámetros (después del campo `distribution_cadence_minutes`):
```tsx
<Field label="universe_degradation_threshold_pct" error={errors.universeDegradationThresholdPct?.message} required>
  <input
    {...register('universeDegradationThresholdPct', {
      required: 'universe_degradation_threshold_pct es requerido.',
      min: { value: 1, message: 'Mínimo 1.' },
      max: { value: 49, message: 'Máximo 49 (el umbral de suspensión es fijo en 50%).' },
      valueAsNumber: true,
    })}
    className={inputClassName}
    type="number"
    min={1}
    max={49}
  />
</Field>
```

---

### T4.1 — Codegen es obligatorio antes de T4.2 y T5.1

Ejecutar codegen después de completar todos los cambios del backend (T1 + T2):
```bash
npm run codegen:api
```

Los tipos generados en `@fibradis/shared-api-client` incluirán los nuevos campos automáticamente. No editar los archivos `.gen.ts` a mano.

---

### Patrones reutilizados de historias anteriores

- `calcLocalScore`, `ScoreBadge`, `fmt`, `fmtPct`, `toNum` en `OportunidadesPage.tsx:25–80` — no tocar, solo agregar el banner alrededor del contenido existente
- Patrón de `OperationalConfig` extension: historia 6-9 agregó `TermsEnabled/TermsText/ContactEmail` — idéntico patrón para el nuevo campo
- `useForm` con `dirtyFields` guard en `ConfigPage.tsx:128–141` — patrón exacto para el nuevo campo
- Tests C# en `OpportunityScoreCalculatorTests.cs` — mismo patrón de helpers `MakeFibra`, `MakeSnapshot`, etc. (no son necesarios aquí pues `UniverseCoverageCalculator` no recibe esos objetos, solo ints)
- Patrón de error ámbar en OportunidadesPage: `className="rounded-xl border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-800"` — inspira el banner de degradación

---

### Restricciones importantes

- El umbral de suspensión (50%) es **fijo en código** (`UniverseCoverageCalculator.SuspensionThresholdPct = 50m`). No se expone en la configuración.
- El umbral de degradación configurable debe ser siempre `< 50` → validación en backend (1–49) y frontend (min=1 max=49).
- Esta historia es **100% aditiva**: no modifica el algoritmo de scoring ni el contrato de `ranked`/`limitedData`/`weights`. Solo agrega el campo `coverage` al response.
- `OpportunityRankingResponseDto` es un `record` posicional — agregar `Coverage` siempre como **cuarto parámetro** para preservar la compatibilidad.
- Después del codegen, el TypeScript verá `coverage` como optional (`coverage?: UniverseCoverageDto`) hasta que el generador lo marque como required — usar `coverage?.status` con optional chaining en la UI.

---

### Dónde NO cambiar

- `OpportunityScoreCalculator.cs` — sin cambios
- `OpportunityFibraRowDto.cs` — sin cambios
- `OpportunityWeightsDto.cs` — sin cambios
- Lógica de `PromediarTab.tsx` — sin cambios
- Tests existentes de `OpportunityScoreCalculatorTests.cs` — sin cambios

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

- Fix TypeScript: `coverage.universeSize - coverage.fibrasWithPrice` falló porque el schema genera `number | string`; se aplicó `toNum()` en ambos operandos.

### Completion Notes List

- T1: `UniverseDegradationThresholdPct` (default 30) agregado a `OperationalConfig`, `OperationalConfigDto`, `UpdateOperationalConfigRequest`, `IOperationalConfigRepository`, `OperationalConfigRepository` y `OpsConfigEndpoints`. Migración EF Core `AddUniverseDegradationThreshold` creada con `defaultValue: 30`.
- T2: `UniverseCoverageCalculator` (clase estática pura) y `UniverseCoverageDto` creados. `OpportunityRankingResponseDto` extendido con `Coverage` como cuarto parámetro. `OpportunityEndpoints` inyecta `IOperationalConfigRepository`, carga config en paralelo con el primer `Task.WhenAll`, y calcula `UniverseCoverageDto` antes del return.
- T3: 7 tests en `UniverseCoverageCalculatorTests.cs` — Normal/Degraded/Suspended + exactos en umbral + umbral personalizado + universo vacío. 47/47 unit tests verdes.
- T4: Codegen regenerado. `OportunidadesPage.tsx` lee `coverage` de la respuesta. Banner ámbar para Degraded (antes del configurador), mensaje rojo de suspensión para Suspended (reemplaza las tablas, el configurador permanece). Normal sin cambios visuales.
- T5: `ConfigPage.tsx` en Ops: `FormValues` + `defaultValues` + `reset()` + `onSubmit` + `<Field>` con `type=number min=1 max=49` para `universe_degradation_threshold_pct`.
- T6: `dotnet build` 0 errores, `dotnet test tests/Unit/` 47 passed, `npm run build` Main y Ops 0 errores TypeScript.

## File List

- `src/Server/Domain/Ops/OperationalConfig.cs` (modificado)
- `src/Server/SharedApiContracts/Ops/OperationalConfigDto.cs` (modificado)
- `src/Server/SharedApiContracts/Ops/UpdateOperationalConfigRequest.cs` (modificado)
- `src/Server/Application/Ops/IOperationalConfigRepository.cs` (modificado)
- `src/Server/Infrastructure/Persistence/Repositories/Ops/OperationalConfigRepository.cs` (modificado)
- `src/Server/Api/Endpoints/Ops/OpsConfigEndpoints.cs` (modificado)
- `src/Server/Infrastructure/Migrations/20260605183529_AddUniverseDegradationThreshold.cs` (nuevo)
- `src/Server/Infrastructure/Migrations/20260605183529_AddUniverseDegradationThreshold.Designer.cs` (nuevo)
- `src/Server/Infrastructure/Migrations/AppDbContextModelSnapshot.cs` (modificado — auto-generado)
- `src/Server/Application/Opportunities/UniverseCoverageCalculator.cs` (nuevo)
- `src/Server/SharedApiContracts/Opportunities/UniverseCoverageDto.cs` (nuevo)
- `src/Server/SharedApiContracts/Opportunities/OpportunityRankingResponseDto.cs` (modificado)
- `src/Server/Api/Endpoints/Private/OpportunityEndpoints.cs` (modificado)
- `tests/Unit/Application.Tests/Opportunities/UniverseCoverageCalculatorTests.cs` (nuevo)
- `src/Web/SharedApiClient/schema.d.ts` (modificado — regenerado por codegen)
- `src/Web/Main/src/modules/oportunidades/OportunidadesPage.tsx` (modificado)
- `src/Web/Ops/src/pages/ConfigPage.tsx` (modificado)

## Change Log

- 2026-06-05: Historia 7-3 implementada — monitoreo de cobertura del universo y ranking degradado. Campo `UniverseDegradationThresholdPct` en OperationalConfig (AC-3), `UniverseCoverageCalculator` con estados Normal/Degraded/Suspended (AC-1, AC-2), banner ámbar y suspensión del ranking en Oportunidades (AC-1, AC-2), campo configurable en ConfigPage Ops (AC-3). 7 nuevos unit tests. Migración EF Core creada.
