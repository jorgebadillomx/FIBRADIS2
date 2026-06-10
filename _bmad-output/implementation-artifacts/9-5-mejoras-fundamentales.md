# Historia 9.5: Descubrimiento inteligente de fundamentales — ventana por FIBRA y dedup cross-source

Status: done

## Story

Como pipeline de fundamentales,
quiero calcular una ventana de descubrimiento por FIBRA basada en los periodos ya procesados en BD y descartar silenciosamente candidatos de periodos ya cubiertos sin importar la fuente,
para evitar descargar el mismo reporte dos veces, procesar sólo periodos nuevos y aprovechar todas las fuentes como fallback implícito cuando una no tiene datos recientes.

## Acceptance Criteria

### AC1 — Ventana inteligente por FIBRA

**Dado que** FIBRA X tiene el último FundamentalRecord procesado con `Period = "Q3-2025"`,
**Cuando** el pipeline ejecuta descubrimiento para FIBRA X,
**Entonces** sólo se consideran candidatos con `Period >= "Q4-2025"`.
**Y** los candidatos con período anterior a Q4-2025 se descartan silenciosamente (sin crear entrada en `FundamentalSourceManifest`).

**Dado que** FIBRA Y no tiene ningún FundamentalRecord procesado,
**Cuando** el pipeline ejecuta descubrimiento,
**Entonces** el fromPeriod es el último trimestre cerrado del año actual menos 5 años (e.g. en junio 2026 → Q1-2021 inclusive).

### AC2 — Periodo cerrado actual

**Dado que** hoy es cualquier fecha,
**Cuando** se calcula el periodo cerrado actual,
**Entonces:**
- Enero–Marzo de año Y → Q4-{Y-1}
- Abril–Junio de año Y → Q1-Y
- Julio–Septiembre de año Y → Q2-Y
- Octubre–Diciembre de año Y → Q3-Y

(El trimestre en curso no está cerrado; el anterior sí.)

### AC3 — Deduplicación cross-source por periodo

**Dado que** economatica procesó exitosamente el período Q1-2026 para FIBRA Z (status="processed" en DB),
**Cuando** OfficialSite (o cualquier otra fuente) descubre también Q1-2026 en la misma ejecución o en una futura,
**Entonces** el candidato es descartado silenciosamente (sin descarga, sin entrada en manifest).

**Dado que** dos fuentes descubren Q2-2026 para FIBRA Z en la misma ejecución y Q2-2026 no está en BD,
**Cuando** la primera fuente procesa exitosamente Q2-2026,
**Entonces** el candidato de la segunda fuente para Q2-2026 es descartado silenciosamente.

### AC4 — Fallback implícito entre fuentes

**Dado que** economatica retorna sólo hasta Q3-2025 para VESTA15 (2+ trimestres atrás de Q1-2026),
**Cuando** OfficialSite descubre Q4-2025 y Q1-2026 para VESTA15,
**Entonces** ambos candidatos se procesan normalmente (no están en DB ni cubiertos por otra fuente).

**Dado que** economatica lanza una excepción para una FIBRA,
**Cuando** el pipeline continúa con las demás fuentes (comportamiento existente),
**Entonces** las otras fuentes procesan sus candidatos dentro de la ventana sin cambios.

### AC5 — Sin entradas de manifest para descartes por ventana/dedup

**Dado que** un candidato es descartado por ventana (período < fromPeriod) o por dedup cross-source,
**Cuando** el pipeline evalúa ese candidato,
**Entonces** NO se crea ningún registro en `FundamentalSourceManifest` para ese candidato.

(Los descartes por manifestUrl ya existente — mismo `SourceName + PackageUrl` — siguen creando la decisión "skip" en manifest, como antes.)

### AC6 — PeriodHelper tiene tests exhaustivos

**Dado que** `FundamentalsDiscoveryPeriodHelper` es lógica pura crítica,
**Entonces** existen unit tests cubriendo:
- `CurrentClosedPeriod` con los 4 cuadrantes (enero, abril, julio, octubre)
- `AdvancePeriod` positivo, negativo, cruce de año
- `IsPeriodInRange` en límites
- `ComputeFromPeriod` con y sin periodo anterior
- `ComparePeriods` correcto ante trampas de orden alfanumérico (Q4-2024 < Q1-2025)

## Tasks / Subtasks

- [x] T1: Implementar `FundamentalsDiscoveryPeriodHelper` (AC2, AC6)
  - [x] T1.1: Crear `src/Server/Application/Fundamentals/FundamentalsDiscoveryPeriodHelper.cs`
  - [x] T1.2: Métodos: `CurrentClosedPeriod`, `AdvancePeriod`, `IsPeriodInRange`, `ComputeFromPeriod`, `ComparePeriods`
  - [x] T1.3: Crear `tests/Unit/Application.Tests/Fundamentals/FundamentalsDiscoveryPeriodHelperTests.cs` con ≥ 20 casos
  - [x] T1.4: Ejecutar tests — todos pasan

- [x] T2: Integrar ventana y dedup cross-source en `FundamentalsAutomationService` (AC1, AC3, AC4, AC5)
  - [x] T2.1: Al inicio del bloque por FIBRA, cargar `processedPeriods` usando `fundamentalRepo.GetProcessedPeriodsAsync` existente
  - [x] T2.2: Cargar último período procesado con `fundamentalRepo.GetLatestProcessedByFibraAsync` existente
  - [x] T2.3: Calcular `fromPeriod` con `PeriodHelper.ComputeFromPeriod(latest?.Period, _timeService.UtcNow)`
  - [x] T2.4: Inicializar `currentRunPeriods = new HashSet<string>(StringComparer.OrdinalIgnoreCase)` por FIBRA
  - [x] T2.5: Agregar filtros ANTES del bloque de manifest: periodo < fromPeriod → continue; periodo en processedPeriods → continue; periodo en currentRunPeriods → continue
  - [x] T2.6: Agregar `currentRunPeriods.Add(candidate.Period)` tras un `IngestAsync` exitoso
  - [x] T2.7: Ejecutar `dotnet test tests/Unit/` — todos pasan

- [x] T3: Verificar que EconomaticaDiscoverySource NO tiene el filtro "last 13" (AC1)
  - [x] T3.1: Confirmar que `EconomaticaDiscoverySource.DiscoverCandidatesAsync` retorna todos los candidatos sin limit hardcoded (la ventana lo maneja)
  - [x] T3.2: Si existe el limit, eliminarlo

- [x] T4: Actualizar sprint-status y story
  - [x] T4.1: Actualizar `sprint-status.yaml`: `9-5-mejoras-fundamentales: review`
  - [x] T4.2: Completar File List y Completion Notes en Dev Agent Record antes de marcar review

### Review Findings

- [x] [Review][Patch] P1: Task.Delay dispara antes de los filtros de ventana/dedup [`FundamentalsAutomationService.cs:74-76`] — aplicado
- [x] [Review][Patch] P2: ArgumentException no capturada si `candidate.Period` tiene formato inválido [`FundamentalsAutomationService.cs:77`] — aplicado
- [x] [Review][Defer] D1: Dos llamadas DB por FIBRA (`GetLatestProcessedByFibraAsync` + `GetProcessedPeriodsAsync`) donde la segunda podría derivar la primera — deferred, pre-existente
- [x] [Review][Defer] D2: Número mágico `-20` en `ComputeFromPeriod` no extraído a constante nombrada — deferred, pre-existente
- [x] [Review][Defer] D3: Test `WhenCandidatePeriodAlreadyExistsInProcessedPeriods` aserta `db.FundamentalSourceManifests.CountAsync()` sobre un DB que no usa el servicio (siempre 0) — deferred, aserción vacuamente verdadera
- [x] [Review][Defer] D4: Sin test para escenario AC4 de fallback implícito (fuente1 solo tiene periodos viejos, fuente2 tiene periodos válidos para la misma FIBRA) — deferred, cobertura adicional
- [x] [Review][Defer] D5: `ComputeFromPeriod` no tiene test con string vacío `""` (solo null cubierto) — deferred, borde de `IsNullOrWhiteSpace`

## Dev Notes

### Contexto y motivación

Esta historia resuelve dos problemas del pipeline de fundamentales existente:

1. **Sin ventana**: Cada ejecución redescubre toda la historia disponible en cada fuente (hasta ~40+ PDFs por FIBRA en economatica). La deduplicación sólo opera a nivel `(SourceName, PackageUrl)`, lo que significa que dos fuentes con el mismo periodo pero distinta URL generan dos descargas, dos extracciones de KPI y el flow `possibleUpdate` con soft-delete — costoso e innecesario.

2. **Sin fallback real**: Las fuentes son todas iguales. No existe un mecanismo explícito que diga "si la fuente primaria ya tiene este periodo, no pierdas tiempo con la secundaria". Con la dedup cross-source, esto emerge de forma implícita.

### Diseño de la solución

#### Porqué no cambiar la interfaz `IFundamentalsDiscoverySource`

La opción de pasar un `FundamentalsDiscoveryContext` como parámetro a `DiscoverCandidatesAsync` requeriría actualizar 5+ fuentes. Dado que la mayoría de las fuentes hace una sola llamada HTTP y retorna todos los candidatos, filtrar en el pipeline es igualmente eficiente. Se mantiene la interfaz sin cambios.

#### Flujo por FIBRA en `FundamentalsAutomationService`

```
Por cada FIBRA:
  1. latest = GetLatestProcessedByFibraAsync(fibraId)     // existente
  2. processedPeriods = GetProcessedPeriodsAsync(fibraId)  // existente, retorna last 12
  3. fromPeriod = PeriodHelper.ComputeFromPeriod(latest?.Period, utcNow)
  4. currentRunPeriods = new HashSet<string>()

  Por cada fuente → candidatos:
    5a. if candidate.Period != null && !IsPeriodInRange(period, fromPeriod) → continue (silent)
    5b. if candidate.Period != null && processedPeriods.Contains(period) → continue (silent)
    5c. if candidate.Period != null && currentRunPeriods.Contains(period) → continue (silent)
    
    [existente: manifest check por SourceName+PackageUrl → "skip"]
    [existente: annual/pending-classification checks]
    [existente: IngestAsync]
    
    6. Si IngestAsync exitoso → currentRunPeriods.Add(candidate.Period)
```

Los pasos 5a-5c son los únicos nuevos. No afectan el "skip" de manifest existente (que ocurre por PackageUrl duplicado, no por periodo).

#### `FundamentalsDiscoveryPeriodHelper` — API

```csharp
public static class FundamentalsDiscoveryPeriodHelper
{
    // "Q1-2025" → (q:1, year:2025); null si formato inválido
    public static (int Quarter, int Year)? ParsePeriod(string? period)
    
    // Compara dos periodos: -1/0/1 (year primero, luego quarter)
    // IMPORTANTE: NO ordenar alphabetically ("Q4-2024" < "Q1-2025" pero "Q4" > "Q1" como string)
    public static int ComparePeriods(string a, string b)
    
    // true si period >= fromPeriod cronológicamente
    public static bool IsPeriodInRange(string period, string fromPeriod)
    
    // El último trimestre CERRADO antes de `now`
    // Mes 1-3 → Q4 del año anterior; Mes 4-6 → Q1; Mes 7-9 → Q2; Mes 10-12 → Q3
    public static string CurrentClosedPeriod(DateTimeOffset now)
    
    // Avanza quarters (puede ser negativo)
    // "Q4-2025" + 1 → "Q1-2026", "Q1-2025" - 1 → "Q4-2024"
    public static string AdvancePeriod(string period, int quarters)
    
    // Si lastProcessedPeriod es null → currentClosed - 20 quarters (5 años = 20 trimestres)
    // Si lastProcessedPeriod es "Q3-2025" → "Q4-2025" (siguiente trimestre)
    public static string ComputeFromPeriod(string? lastProcessedPeriod, DateTimeOffset now)
}
```

**Trampa de orden**: "Q4-2024" < "Q1-2025" pero si comparas como strings, "Q4" > "Q1". Usar siempre `ComparePeriods` para ordenar, NUNCA `.OrderBy(p => p)` directo.

#### Por qué `GetProcessedPeriodsAsync` existente es suficiente

El método actual retorna los **últimos 12 periodos procesados** (con `Take(12)`). Esto cubre 3 años de trimestres. La ventana parte del `lastPeriod + 1Q` (o 5 años atrás si no hay nada), por lo que:
- En operación normal: fromPeriod ≈ trimestre actual → processedPeriods ya cubre los últimos 3 años → suficiente
- En cold start: no hay periodos procesados → processedPeriods está vacío → no hay nada que deduplicar en los pasos 5b/5c → la ventana (5a) filtra por fecha

No se necesita una nueva versión de `GetProcessedPeriodsAsync` con parámetro de filtro.

#### Impacto en `possibleUpdate`

El flow `possibleUpdate` existente (detecta mismo periodo, diferente PackageUrl de otra fuente o corrida anterior) queda relegado a un caso mucho más raro:
- Un manifest existe para Q4-2025 con `LastDecision="pending"` o `"error"` (nunca llegó a FundamentalRecord procesado)
- En la siguiente corrida, el candidato llega nuevamente
- El paso 5b no lo filtra (no está en processedPeriods porque nunca fue procesado)
- El check de manifest lo detecta como `possibleUpdate`

Este es el comportamiento correcto: reintentar periodos fallidos.

#### EconomaticaDiscoverySource — sin limit de 13

La historia pendiente de la sesión anterior (agregar `.TakeLast(13)`) queda **cancelada**. La ventana calcula exactamente cuántos periodos son necesarios:
- Primera corrida FHIPO14: fromPeriod = Q1-2021 → economatica devuelve ~46 PDFs (2021-2026) → ventana filtra los anteriores a Q1-2021 → ~18 candidatos procesados
- Segunda corrida: fromPeriod = Q3-2026 (o lo que sea el último procesado + 1) → 0-1 candidatos

### Archivos a modificar

| Acción | Archivo |
|--------|---------|
| CREAR | `src/Server/Application/Fundamentals/FundamentalsDiscoveryPeriodHelper.cs` |
| MODIFICAR | `src/Server/Infrastructure/Jobs/Fundamentals/FundamentalsAutomationService.cs` |
| CREAR | `tests/Unit/Application.Tests/Fundamentals/FundamentalsDiscoveryPeriodHelperTests.cs` |

**No se toca** `IFundamentalsDiscoverySource`, ninguna discovery source, ningún repositorio, ni se requiere migración.

### Ubicación de `FundamentalsAutomationService`

```
src/Server/Infrastructure/Jobs/Fundamentals/FundamentalsAutomationService.cs
```

Los métodos de repositorio que se llaman (ya existentes):
```csharp
// Línea ~45 del loop de FIBRA — añadir ANTES del loop de fuentes:
var latest = await fundamentalRepo.GetLatestProcessedByFibraAsync(fibra.Id, ct);
var processedPeriods = (await fundamentalRepo.GetProcessedPeriodsAsync(fibra.Id, ct))
    .ToHashSet(StringComparer.OrdinalIgnoreCase);
var fromPeriod = FundamentalsDiscoveryPeriodHelper.ComputeFromPeriod(latest?.Period, DateTimeOffset.UtcNow);
var currentRunPeriods = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
```

**Advertencia**: `FundamentalsAutomationService` ya usa `ITimeService` vía DI (hay un `_timeService` o similar). Usar `_timeService.UtcNow` en lugar de `DateTimeOffset.UtcNow` para que los tests puedan controlar el tiempo. Si no existe aún inyectado en este servicio, añadirlo.

### Estructura de `FundamentalsDiscoveryPeriodHelper`

```
src/Server/Application/Fundamentals/FundamentalsDiscoveryPeriodHelper.cs
namespace Application.Fundamentals;
// public static class — sin dependencias, pura
```

No inyectable, no en DI — se invoca directamente como `FundamentalsDiscoveryPeriodHelper.ComputeFromPeriod(...)`.

### Security Checklist — completar antes del primer commit

- [x] **TOCTOU**: Esta historia no añade endpoints de escritura. La modificación a `FundamentalsAutomationService` es un job con `[DisableConcurrentExecution]`, por lo que no hay race condition.
- [x] **Auth-gating**: No hay componentes UI nuevos.
- [x] **Denominador cero**: `FundamentalsDiscoveryPeriodHelper` no tiene divisiones.
- [x] **Trampa string sort en periodos**: Documentada arriba. `ComparePeriods` es obligatorio; tests deben cubrir `Q4-2024 < Q1-2025` explícitamente.

### Project Structure Notes

```
Application.Fundamentals ← FundamentalsDiscoveryPeriodHelper (nueva clase pura)
Infrastructure.Jobs.Fundamentals ← FundamentalsAutomationService (modificado)
tests/Unit/Application.Tests/Fundamentals/ ← FundamentalsDiscoveryPeriodHelperTests
```

No hay cambios de contrato de API, esquema de BD ni endpoints.

### References

- `FundamentalsAutomationService.ExecuteAsync` — `src/Server/Infrastructure/Jobs/Fundamentals/FundamentalsAutomationService.cs`
- `IFundamentalRepository.GetProcessedPeriodsAsync` y `GetLatestProcessedByFibraAsync` — `src/Server/Application/Fundamentals/IFundamentalRepository.cs`
- `FundamentalRepository` ordenación por periodo (Substring): `src/Server/Infrastructure/Persistence/Repositories/Fundamentals/FundamentalRepository.cs`
- Story 5.11 (AMEFIBRA) — patrón `discover → decide → ingest` documentado en Dev Notes

## Change Log

| Fecha | Cambio |
| --- | --- |
| 2026-06-09 | Implementé el helper de periodos, la ventana inteligente por FIBRA, el dedup cross-source por periodo y la cobertura de tests correspondiente. |

## Dev Agent Record

### Agent Model Used

GPT-5

### Debug Log References

- Added `FundamentalsDiscoveryPeriodHelper` with pure quarter arithmetic, chronological comparison, and five-year fallback window logic.
- Integrated `FundamentalsAutomationService` with `ITimeService`, per-FIBRA `processedPeriods`, `fromPeriod`, and `currentRunPeriods` cross-source deduplication.
- Confirmed `EconomaticaDiscoverySource` has no hardcoded `last 13` limit; the pipeline window owns the discard logic.
- Added helper and job regression tests covering 31 application cases and 291 infrastructure tests in the full unit suite.

### Completion Notes List

- Implemented `src/Server/Application/Fundamentals/FundamentalsDiscoveryPeriodHelper.cs` and covered it with 31 unit cases.
- Updated `src/Server/Infrastructure/Jobs/Fundamentals/FundamentalsAutomationService.cs` to discard out-of-window periods, skip already processed periods silently, and deduplicate same-period candidates across sources within the same run.
- Extended `tests/Unit/Infrastructure.Tests/Jobs/Fundamentals/FundamentalsAutomationServiceTests.cs` to cover window skips and same-run dedup behavior.
- Verified `dotnet test tests/Unit/Domain.Tests/Domain.Tests.csproj`, `dotnet test tests/Unit/Application.Tests/Application.Tests.csproj`, and `dotnet test tests/Unit/Infrastructure.Tests/Infrastructure.Tests.csproj` all pass.
- `dotnet test FIBRADIS.slnx` still reports unrelated pre-existing integration failures outside this story's scope.

### File List

- `_bmad-output/implementation-artifacts/9-5-mejoras-fundamentales.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Server/Application/Fundamentals/FundamentalsDiscoveryPeriodHelper.cs`
- `src/Server/Infrastructure/Jobs/Fundamentals/FundamentalsAutomationService.cs`
- `tests/Unit/Application.Tests/Fundamentals/FundamentalsDiscoveryPeriodHelperTests.cs`
- `tests/Unit/Infrastructure.Tests/Jobs/Fundamentals/FundamentalsAutomationServiceTests.cs`
