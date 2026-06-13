# Story 3.7: Indicadores Macro — TIIE 28d e INPC anualizado con historial de 12 meses

Status: done

## Story

Como sistema y como inversionista registrado,
quiero que la plataforma sincronice automáticamente la TIIE 28d (semanalmente) y el INPC mensual (con historial de 12 meses y lógica de catch-up ante ejecuciones fallidas),
para que las herramientas de análisis puedan mostrar tasas de referencia actualizadas y el contexto inflacionario real del mercado mexicano.

## Acceptance Criteria

1. **AC1 — TIIE 28d en BanxicoSyncJob:**
   Dado que `BanxicoSyncJob` se ejecuta cada miércoles a las 6 AM UTC, cuando Banxico responde correctamente con la serie `SF43783`, entonces `OperationalConfig.Tiie28dRate` y `Tiie28dRateUpdatedAt` se actualizan en la base de datos. Si el token no está configurado o la respuesta es inválida, el job loguea un warning y continúa sin fallar (igual que CETES).

2. **AC2 — TIIE 28d en el endpoint de indicadores:**
   Dado que el usuario autenticado llama `GET /api/v1/market/indicadores`, entonces la respuesta incluye el campo `tiie28d: decimal|null` junto a `cetes28d`. Un usuario anónimo sigue recibiendo `401`.

3. **AC3 — TIIE pre-llenada en Herramientas:**
   Dado que `HerramientasPage` carga y el endpoint retorna `tiie28d`, entonces el valor se muestra como indicador de contexto en la herramienta "FIBRAs vs CETES" (ej: badge o label de referencia "TIIE 28d: 9.25%"), para que el usuario pueda comparar su tasa CETES ingresada con la TIIE vigente. Si el dato no está disponible, el indicador no se muestra.

4. **AC4 — Tabla `ops.InpcMonthly`:**
   Dado que se aplica la migración EF, entonces existe la tabla `ops.InpcMonthly` con columnas `periodo` (DateOnly, PK), `inpc_index` (decimal(10,4), NOT NULL) y `captured_at` (DateTimeOffset, NOT NULL).

5. **AC5 — Primera corrida (tabla vacía):**
   Dado que `BanxicoMonthlySyncJob` se ejecuta y `ops.InpcMonthly` está vacía, cuando el job corre, entonces llama a Banxico con un rango de 25 meses atrás hasta hoy, y upserta todos los registros recibidos. Al final de la ejecución, la tabla contiene ≥12 registros con datos mensuales.

6. **AC6 — Corrida normal (mes a mes):**
   Dado que `BanxicoMonthlySyncJob` se ejecuta y ya hay registros previos, cuando el job corre, entonces llama a Banxico solo desde el mes siguiente al último `periodo` registrado hasta hoy. Si no hay meses nuevos (ya está al día), el job termina sin llamar a la API.

7. **AC7 — Catch-up (2-3 meses sin ejecutar):**
   Dado que `BanxicoMonthlySyncJob` no ha corrido en varios meses, cuando se ejecuta, entonces usa el mismo algoritmo de rango que AC6 — el rango simplemente cubre los meses faltantes. No requiere lógica especial. La idempotencia está garantizada por el PK `periodo` (upsert).

8. **AC8 — Job mensual registrado:**
   `BanxicoMonthlySyncJob` está registrado como `RecurringJob` con cron `"0 10 12 * *"` (día 12 de cada mes a las 10:00 AM UTC), para asegurar que el INPC publicado ~día 9 por INEGI ya esté disponible en la API de Banxico.

9. **AC9 — Endpoint retorna historial INPC:**
   Dado que `GET /api/v1/market/indicadores` es llamado por un usuario autenticado, entonces la respuesta incluye el campo `inpcHistory`: lista de hasta 12 objetos `{ periodo: "2025-07", anualPct: 4.21 }`, ordenados de más antiguo a más reciente. Las entradas con datos insuficientes para calcular la variación anual (no hay registro del mismo mes 12 meses atrás) se excluyen de la lista. Si hay menos de 12 pares calculables, se retornan los disponibles.

10. **AC10 — Ejecución manual desde Ops:**
    Dado que AdminOps hace `POST /api/v1/ops/banxico/sync-tiie/run` o `POST /api/v1/ops/banxico/sync-inpc/run`, entonces se encola el job correspondiente con Hangfire (`IBackgroundJobClient.Enqueue`) y retorna `202 Accepted`. Los endpoints requieren rol `AdminOps`.

## Tasks / Subtasks

- [x] **T1 — OperationalConfig: columnas TIIE** (AC1)
  - [x] T1.1 — En [OperationalConfig.cs](src/Server/Domain/Ops/OperationalConfig.cs), agregar:
    ```csharp
    public decimal? Tiie28dRate { get; set; }
    public DateTimeOffset? Tiie28dRateUpdatedAt { get; set; }
    ```
  - [x] T1.2 — En [OperationalConfigConfiguration.cs](src/Server/Infrastructure/Persistence/SqlServer/Configurations/Ops/OperationalConfigConfiguration.cs), agregar mapeo:
    ```csharp
    b.Property(x => x.Tiie28dRate).HasColumnName("tiie_28d_rate").HasColumnType("decimal(10,4)");
    b.Property(x => x.Tiie28dRateUpdatedAt).HasColumnName("tiie_28d_rate_updated_at");
    ```
  - [x] T1.3 — En [IOperationalConfigRepository.cs](src/Server/Application/Ops/IOperationalConfigRepository.cs), agregar:
    ```csharp
    Task UpdateTiieRateAsync(decimal rate, DateTimeOffset updatedAt, CancellationToken ct = default);
    ```
  - [x] T1.4 — Implementar `UpdateTiieRateAsync` en [OperationalConfigRepository.cs](src/Server/Infrastructure/Persistence/Repositories/Ops/OperationalConfigRepository.cs) usando `ExecuteUpdateAsync` (mismo patrón que `UpdateCetesRateAsync`).

- [x] **T2 — Migración EF: TIIE + tabla InpcMonthly** (AC1, AC4)
  - [x] T2.1 — Crear la entidad `InpcMonthlyEntry` en `src/Server/Domain/Ops/InpcMonthlyEntry.cs`:
    ```csharp
    public class InpcMonthlyEntry
    {
        public DateOnly Periodo { get; set; }         // PK
        public decimal InpcIndex { get; set; }        // valor del índice SP1
        public DateTimeOffset CapturedAt { get; set; }
    }
    ```
  - [x] T2.2 — Crear `InpcMonthlyConfiguration.cs` en `src/Server/Infrastructure/Persistence/SqlServer/Configurations/Ops/`:
    ```csharp
    b.ToTable("InpcMonthly", "ops");
    b.HasKey(x => x.Periodo);
    b.Property(x => x.Periodo).HasColumnName("periodo");
    b.Property(x => x.InpcIndex).HasColumnName("inpc_index").HasColumnType("decimal(10,4)");
    b.Property(x => x.CapturedAt).HasColumnName("captured_at");
    ```
  - [x] T2.3 — Agregar `DbSet<InpcMonthlyEntry> InpcMonthlyEntries` al `AppDbContext`.
  - [x] T2.4 — Crear migración EF:
    ```bash
    dotnet ef migrations add AddTiie28dAndInpcMonthly \
      --project src/Server/Infrastructure \
      --startup-project src/Server/Api \
      --configuration Release
    ```
    **Gate EF**: verificar que la migración aparece en la lista antes de continuar.

- [x] **T3 — IBanxicoClient: método para TIIE y método para INPC range** (AC1, AC5)
  - [x] T3.1 — Agregar a [IBanxicoClient.cs](src/Server/Application/Integrations/IBanxicoClient.cs):
    ```csharp
    Task<decimal?> GetTiie28dAsync(CancellationToken ct = default);
    Task<IReadOnlyList<(DateOnly Periodo, decimal InpcIndex)>> GetInpcHistoryAsync(
        DateOnly from, DateOnly to, CancellationToken ct = default);
    ```
  - [x] T3.2 — Implementar `GetTiie28dAsync` en [BanxicoClient.cs](src/Server/Infrastructure/Integrations/Banxico/BanxicoClient.cs):
    - Serie hardcodeada: `"SF43783"` (TIIE 28 días)
    - Misma lógica que `GetCetes28dAsync`: token guard, `/datos/oportuno`, parseo, rate ≤ 0 → null
    - El helper privado `TryGetDato` ya existe — reusarlo
  - [x] T3.3 — Implementar `GetInpcHistoryAsync` en `BanxicoClient`:
    - Serie hardcodeada: `"SP1"` (INPC general mensual, base 2018=100)
    - Endpoint: `GET .../series/SP1/datos/{from:yyyy-MM-dd}/{to:yyyy-MM-dd}`
    - Parsear el array `bmx.series[0].datos[]` completo:
      - `fecha` → `DateOnly.Parse(fecha, "dd/MM/yyyy")` → normalizar a primer día del mes  
      - `dato` → `decimal.TryParse` con InvariantCulture
      - Descartar entradas con `dato == "N/E"` o parse fallido
    - En error (HTTP non-2xx, timeout, parse): loguear warning y retornar lista vacía (no lanzar)
    - Token guard igual que los otros métodos

- [x] **T4 — Repositorio INPC** (AC5-AC7)
  - [x] T4.1 — Crear `IInpcRepository.cs` en `src/Server/Application/Ops/`:
    ```csharp
    public interface IInpcRepository
    {
        Task<DateOnly?> GetLatestPeriodoAsync(CancellationToken ct = default);
        Task UpsertManyAsync(IEnumerable<InpcMonthlyEntry> entries, CancellationToken ct = default);
        Task<IReadOnlyList<InpcMonthlyEntry>> GetLastAsync(int count, CancellationToken ct = default);
    }
    ```
  - [x] T4.2 — Crear `InpcRepository.cs` en `src/Server/Infrastructure/Persistence/Repositories/Ops/`:
    - `GetLatestPeriodoAsync`: `MaxAsync(x => (DateOnly?)x.Periodo)` → null si vacía
    - `UpsertManyAsync`: iterar y para cada entry usar `ExecuteUpdateAsync` si existe, `AddAsync` si no; o usar el patrón MERGE que ya existe en el repo para `UpsertDailySnapshotAsync`
    - `GetLastAsync(count)`: `OrderByDescending(x => x.Periodo).Take(count).ToListAsync()`
  - [x] T4.3 — Registrar `IInpcRepository` / `InpcRepository` en [ApiServiceExtensions.cs](src/Server/Api/CompositionRoot/ApiServiceExtensions.cs):
    ```csharp
    services.AddScoped<IInpcRepository, InpcRepository>();
    ```

- [x] **T5 — BanxicoSyncJob: agregar TIIE** (AC1)
  - [x] T5.1 — Actualizar [BanxicoSyncJob.cs](src/Server/Application/Jobs/BanxicoSyncJob.cs):
    ```csharp
    public class BanxicoSyncJob(IBanxicoClient banxico, IOperationalConfigRepository config, ILogger<BanxicoSyncJob> logger)
    {
        public async Task ExecuteAsync(CancellationToken ct)
        {
            // CETES (existente)
            var cetes = await banxico.GetCetes28dAsync(ct);
            if (cetes is null) logger.LogWarning("BanxicoSyncJob: no se obtuvo tasa CETES 28d");
            else await config.UpdateCetesRateAsync(cetes.Value, DateTimeOffset.UtcNow, ct);

            // TIIE (nuevo)
            var tiie = await banxico.GetTiie28dAsync(ct);
            if (tiie is null) logger.LogWarning("BanxicoSyncJob: no se obtuvo tasa TIIE 28d");
            else await config.UpdateTiieRateAsync(tiie.Value, DateTimeOffset.UtcNow, ct);

            logger.LogInformation("BanxicoSyncJob: CETES={Cetes} TIIE={Tiie}", cetes, tiie);
        }
    }
    ```
    **Importante**: las dos llamadas van en secuencia (no `Task.WhenAll`) — el `HttpClient` registrado es tipado por `IBanxicoClient` y no es thread-safe para concurrencia en el mismo job.

- [x] **T6 — BanxicoMonthlySyncJob: nuevo job INPC** (AC5-AC8)
  - [x] T6.1 — Crear `src/Server/Application/Jobs/BanxicoMonthlySyncJob.cs`:
    ```csharp
    public class BanxicoMonthlySyncJob(
        IBanxicoClient banxico,
        IInpcRepository inpcRepo,
        ILogger<BanxicoMonthlySyncJob> logger)
    {
        public async Task ExecuteAsync(CancellationToken ct)
        {
            var lastPeriodo = await inpcRepo.GetLatestPeriodoAsync(ct);
            DateOnly from;

            if (lastPeriodo is null)
            {
                // Primera corrida: 25 meses atrás para tener suficiente historia
                from = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-25);
                from = new DateOnly(from.Year, from.Month, 1); // primer día del mes
                logger.LogInformation("BanxicoMonthlySyncJob: primera corrida, desde {From}", from);
            }
            else
            {
                // Normal o catch-up: desde el mes siguiente al último registrado
                from = lastPeriodo.Value.AddMonths(1);
                from = new DateOnly(from.Year, from.Month, 1);

                var todayPeriod = new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
                if (from > todayPeriod)
                {
                    logger.LogInformation("BanxicoMonthlySyncJob: ya al día, nada que sincronizar");
                    return;
                }
                logger.LogInformation("BanxicoMonthlySyncJob: catch-up desde {From}", from);
            }

            var to = DateOnly.FromDateTime(DateTime.UtcNow);
            var history = await banxico.GetInpcHistoryAsync(from, to, ct);

            if (history.Count == 0)
            {
                logger.LogWarning("BanxicoMonthlySyncJob: Banxico no retornó datos INPC para el rango [{From}, {To}]", from, to);
                return;
            }

            var entries = history.Select(h => new InpcMonthlyEntry
            {
                Periodo = new DateOnly(h.Periodo.Year, h.Periodo.Month, 1), // normalizar a primer día
                InpcIndex = h.InpcIndex,
                CapturedAt = DateTimeOffset.UtcNow,
            }).ToList();

            await inpcRepo.UpsertManyAsync(entries, ct);
            logger.LogInformation("BanxicoMonthlySyncJob: {Count} registros INPC upsertados", entries.Count);
        }
    }
    ```
  - [x] T6.2 — Registrar en [Program.cs](src/Server/Api/Program.cs) como RecurringJob mensual:
    ```csharp
    recurringJobManager.AddOrUpdate<BanxicoMonthlySyncJob>(
        "banxico-inpc-sync",
        j => j.ExecuteAsync(CancellationToken.None),
        "0 10 12 * *"); // día 12 de cada mes, 10:00 AM UTC
    ```

- [x] **T7 — IndicadoresDto y endpoint** (AC2, AC9)
  - [x] T7.1 — Crear `InpcMonthlyDto.cs` en `src/Server/SharedApiContracts/Market/`:
    ```csharp
    public sealed record InpcMonthlyDto(string Periodo, decimal AnualPct);
    // Periodo en formato "YYYY-MM" (ej. "2025-07")
    ```
  - [x] T7.2 — Actualizar [IndicadoresDto.cs](src/Server/SharedApiContracts/Market/IndicadoresDto.cs):
    ```csharp
    public sealed record IndicadoresDto(
        decimal? Cetes28d,
        decimal? Tiie28d,
        DateTimeOffset? LastUpdated,
        IReadOnlyList<InpcMonthlyDto>? InpcHistory);
    ```
  - [x] T7.3 — Actualizar [IndicadoresEndpoints.cs](src/Server/Api/Endpoints/Private/IndicadoresEndpoints.cs):
    - Inyectar `IOperationalConfigRepository` e `IInpcRepository`
    - Leer `OperationalConfig` para `Cetes28d`, `Tiie28d`, `LastUpdated`
    - Llamar `inpcRepo.GetLastAsync(25)` para obtener suficientes datos
    - Computar `inpcHistory` en memoria (ver Dev Notes — algoritmo de variación anual)
    - Retornar los últimos 12 puntos con `AnualPct` calculado

- [x] **T8 — Endpoints manuales Ops** (AC10)
  - [x] T8.1 — En `src/Server/Api/Endpoints/Ops/OpsMarketEndpoints.cs` (o crear `OpsBanxicoEndpoints.cs`), agregar:
    ```csharp
    group.MapPost("/banxico/sync-tiie/run", async (IBackgroundJobClient jobs, CancellationToken ct) =>
    {
        jobs.Enqueue<BanxicoSyncJob>(j => j.ExecuteAsync(CancellationToken.None));
        return Results.Accepted();
    }).RequireAuthorization("AdminOps").Produces(StatusCodes.Status202Accepted);

    group.MapPost("/banxico/sync-inpc/run", async (IBackgroundJobClient jobs, CancellationToken ct) =>
    {
        jobs.Enqueue<BanxicoMonthlySyncJob>(j => j.ExecuteAsync(CancellationToken.None));
        return Results.Accepted();
    }).RequireAuthorization("AdminOps").Produces(StatusCodes.Status202Accepted);
    ```

- [x] **T9 — Frontend: TIIE en HerramientasPage** (AC3)
  - [x] T9.1 — Actualizar `fetchIndicadores` en [fibrasApi.ts](src/Web/Main/src/api/fibrasApi.ts) para mapear el nuevo campo `tiie28d` (el codegen lo hace automáticamente si se regenera el cliente).
  - [x] T9.2 — En [HerramientasPage.tsx](src/Web/Main/src/modules/herramientas/HerramientasPage.tsx), en la sección "FIBRAs vs CETES", agregar un badge/label de referencia debajo del campo Tasa CETES 28d:
    ```tsx
    {indicadoresQuery.data?.tiie28d != null && (
      <p className="text-xs text-muted-foreground">
        TIIE 28d vigente: {indicadoresQuery.data.tiie28d.toFixed(2)}%
      </p>
    )}
    ```
    Este label es informativo — no pre-llena ningún campo. Permite al usuario contrastar su tasa CETES ingresada con la TIIE actual.
  - [x] T9.3 — Regenerar cliente TypeScript: `npm run codegen:api`

- [x] **T10 — Unit tests** (AC1-AC10)
  - [x] T10.1 — `BanxicoClientTests.cs` — agregar:
    - `GetTiie28dAsync_WhenTokenMissing_ReturnsNull`
    - `GetTiie28dAsync_WhenValidResponse_ParsesCorrectly`
    - `GetInpcHistoryAsync_WhenTokenMissing_ReturnsEmptyList`
    - `GetInpcHistoryAsync_WhenValidRange_ReturnsParsedEntries`
    - `GetInpcHistoryAsync_WhenDatoIsNE_ExcludesEntry`
    - `GetInpcHistoryAsync_WhenHttpFails_ReturnsEmptyList`
  - [x] T10.2 — `BanxicoSyncJobTests.cs` — agregar:
    - `ExecuteAsync_WhenTiieAvailable_UpdatesTiieRate`
    - `ExecuteAsync_WhenTiieNull_LogsWarningAndDoesNotUpdate`
    - `ExecuteAsync_WhenBothAvailable_UpdatesBothRates` (cetes + tiie en el mismo job)
  - [x] T10.3 — `BanxicoMonthlySyncJobTests.cs` (nuevo archivo):
    - `ExecuteAsync_WhenTableEmpty_FetchesFrom25MonthsAgo` (AC5)
    - `ExecuteAsync_WhenLastPeriodoExists_FetchesFromNextMonth` (AC6)
    - `ExecuteAsync_WhenAlreadyUpToDate_DoesNotCallBanxico` (AC6 — from > today)
    - `ExecuteAsync_WhenBanxicoReturnsEmpty_LogsWarningAndReturns` (AC7 edge case)
    - `ExecuteAsync_WhenMissingMonths_FetchesCatchUpRange` (AC7)
  - [x] T10.4 — `InpcRepositoryTests.cs` (nuevo archivo):
    - `GetLatestPeriodoAsync_WhenEmpty_ReturnsNull`
    - `GetLatestPeriodoAsync_WhenHasRecords_ReturnsMaxPeriodo`
    - `UpsertManyAsync_WhenPeriodoExists_UpdatesInpcIndex`
    - `UpsertManyAsync_WhenPeriodoNew_InsertsRecord`
    - `GetLastAsync_ReturnsDescendingOrderedEntries`
  - [x] T10.5 — `OperationalConfigRepositoryTests.cs` — agregar:
    - `UpdateTiieRateAsync_UpdatesTiieColumns`

- [x] **T11 — Validación final**
  - [x] T11.1 — `dotnet test tests/Unit/` — 0 errores
  - [x] T11.2 — `dotnet test tests/Integration/` — 0 errores
  - [x] T11.3 — `cd src/Web/Main && npm test` — 0 errores
  - [x] T11.4 — `cd src/Web/Main && npx tsc --noEmit` — 0 errores TypeScript
  - [x] T11.5 — `dotnet build FIBRADIS.slnx` — 0 errores
  - [x] T11.6 — Gate EF: `dotnet ef migrations list --project src/Server/Infrastructure --startup-project src/Server/Api --configuration Release` — confirmar que `AddTiie28dAndInpcMonthly` aparece aplicada

## Dev Notes

### Contexto: qué ya existe y qué cambia

La historia 11.6 implementó `BanxicoClient`, `IBanxicoClient`, `BanxicoSyncJob` y el endpoint `GET /api/v1/market/indicadores`. Esta historia extiende esa infraestructura sin reescribirla:

- `BanxicoClient`: se agregan dos métodos nuevos (`GetTiie28dAsync`, `GetInpcHistoryAsync`) usando el mismo helper `TryGetDato` ya existente (para TIIE) y un nuevo parser para el range endpoint (para INPC).
- `BanxicoSyncJob`: se agrega una segunda llamada a `GetTiie28dAsync` después de la CETES existente. El job NO se divide — sigue siendo un único job semanal.
- `IndicadoresDto`: se extiende con `Tiie28d` y `InpcHistory`. El contrato es aditivo — los clientes existentes ignoran campos nuevos.

### Algoritmo de variación anual INPC (en el endpoint)

La tabla `InpcMonthly` almacena el índice INPC mensual puro (serie SP1). La variación anual se calcula en memoria en el endpoint:

```csharp
// En IndicadoresEndpoints, después de leer los últimos 25 registros:
var entries = await inpcRepo.GetLastAsync(25, ct);
// entries viene ordenado ascendente por Periodo (el repo hace OrderBy)

var inpcHistory = new List<InpcMonthlyDto>();
for (int i = 0; i < entries.Count; i++)
{
    var current = entries[i];
    // Buscar el registro de exactamente 12 meses atrás
    var yearAgo = entries.FirstOrDefault(e =>
        e.Periodo.Year == current.Periodo.Year - 1 &&
        e.Periodo.Month == current.Periodo.Month);

    if (yearAgo is null) continue; // no hay par — no incluir en la lista

    var anualPct = Math.Round((current.InpcIndex / yearAgo.InpcIndex - 1m) * 100m, 2);
    inpcHistory.Add(new InpcMonthlyDto(
        Periodo: current.Periodo.ToString("yyyy-MM"),
        AnualPct: anualPct));
}

// Retornar solo los últimos 12 puntos con variación calculada
var result = inpcHistory.TakeLast(12).ToList();
```

**Invariante crítico**: si la tabla tiene ≥25 meses de historia (logrado en la primera corrida), el endpoint siempre tendrá suficientes datos para calcular ≥12 puntos. Si hay menos (BD recién poblada con < 25 meses), retorna lo disponible sin error.

### Banxico SIE API — endpoint de rango

La URL para datos en rango es:
```
GET https://www.banxico.org.mx/SieAPIRest/service/v1/series/{series}/datos/{startDate}/{endDate}
```
- `{startDate}` y `{endDate}`: formato `YYYY-MM-DD`
- Headers: `Bmx-Token: {token}`, `Accept: application/json`
- Response: misma estructura `bmx.series[0].datos[]`, pero el array tiene múltiples entradas

```json
{
  "bmx": {
    "series": [{
      "idSerie": "SP1",
      "datos": [
        { "fecha": "30/04/2024", "dato": "134.1258" },
        { "fecha": "31/05/2024", "dato": "135.5190" },
        ...
      ]
    }]
  }
}
```

**Normalización de fecha**: el campo `fecha` del INPC mensual viene como último día del mes (`30/04/2024`). Normalizar siempre a primer día del mes al parsear: `new DateOnly(parsed.Year, parsed.Month, 1)`. Esto garantiza que el PK `periodo` sea consistente independientemente del formato de Banxico.

**Series IDs verificados:**
- TIIE 28d: `SF43783`
- INPC general mensual (índice): `SP1`

**Si `SP1` no funciona**: el dev debe verificar en el portal SIE de Banxico (https://www.banxico.org.mx/SieAPIRest/service/v1/doc/catalogoSeries#) la serie correcta para INPC general. El nombre de la serie configurable puede agregarse a `appsettings.json` igual que `Banxico:Series` para CETES.

### UpsertManyAsync — patrón de implementación

Preferir el mismo patrón bulk que ya existe en `MarketRepository.UpsertDailySnapshotAsync`. Si usa `ExecuteUpdateAsync` + `AddAsync` por entidad:

```csharp
public async Task UpsertManyAsync(IEnumerable<InpcMonthlyEntry> entries, CancellationToken ct = default)
{
    foreach (var entry in entries)
    {
        var updated = await db.InpcMonthlyEntries
            .Where(x => x.Periodo == entry.Periodo)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.InpcIndex, entry.InpcIndex)
                .SetProperty(x => x.CapturedAt, entry.CapturedAt), ct);

        if (updated == 0)
            db.InpcMonthlyEntries.Add(entry);
    }
    await db.SaveChangesAsync(ct);
}
```

Si el codebase tiene un patrón MERGE/ON CONFLICT más eficiente, usarlo. La idempotencia es el requisito: re-ejecutar el mismo rango de fechas no debe crear duplicados ni lanzar excepciones.

### Retención de datos INPC

No hay limpieza automática programada. Los datos INPC son escasos (~12 filas/año, tamaño despreciable) y podrían ser útiles para análisis histórico futuro. La decisión deliberada es **acumular sin borrar**. El endpoint `GetLastAsync(25)` limita el impacto en el query.

### Cadencia de publicación del INPC

- INEGI publica el INPC alrededor del día 8-10 de cada mes
- Banxico lo refleja en SIE con 1-2 días de retraso típico
- El cron `"0 10 12 * *"` (día 12, 10:00 AM UTC) da margen suficiente
- Si el job corre y Banxico aún no tiene el dato del mes actual → retorna el mes anterior como el más reciente → `from = mes_actual` y el rango `from > today_month` → el job termina sin llamar a la API (AC6 guard). El mes faltante se obtendrá en la siguiente corrida mensual o en un trigger manual.

### Review findings de 11.6 relevantes para esta historia

La historia 11.6 tiene hallazgos pendientes en los Review Findings. Los marcados como `Review/Patch` (no como Defer) aún no han sido resueltos porque la historia está `in-progress`. Algunos afectan directamente a `BanxicoClient` y `BanxicoSyncJob`:

- **`datos[0]` sin guard de array vacío** → El método `TryGetDato` existente ya usa `datos.GetArrayLength() == 0` como guard (línea 108 de BanxicoClient.cs). Confirmado resuelto en el código actual.
- **BanxicoSyncJob no registrado en DI** → Verificar que `BanxicoSyncJob` está registrado antes de agregar `BanxicoMonthlySyncJob`. Hangfire requiere que el tipo esté en el contenedor para resolver dependencias.
- **Sin timeout en HttpClient** → Al registrar los HttpClients en `ApiServiceExtensions`, agregar timeout explícito:
  ```csharp
  services.AddHttpClient<IBanxicoClient, BanxicoClient>(c => c.Timeout = TimeSpan.FromSeconds(30));
  ```

### Registro de BanxicoMonthlySyncJob en DI

Hangfire resuelve jobs por tipo desde el contenedor. El job necesita estar registrado aunque sea como transient:

```csharp
// En ApiServiceExtensions.cs, junto a BanxicoSyncJob:
services.AddTransient<BanxicoMonthlySyncJob>();
```

Verificar que `BanxicoSyncJob` también está registrado así — si no, agregarlo.

### Fakes para tests del nuevo job

`BanxicoMonthlySyncJobTests` usa un fake de `IBanxicoClient` e `IInpcRepository`. El fake de `IInpcRepository` necesita implementar los 3 métodos. Patrón mínimo:

```csharp
class FakeInpcRepository : IInpcRepository
{
    public DateOnly? LatestPeriodo { get; set; }
    public List<InpcMonthlyEntry> Upserted { get; } = [];
    public List<InpcMonthlyEntry> Data { get; set; } = [];

    public Task<DateOnly?> GetLatestPeriodoAsync(CancellationToken ct = default)
        => Task.FromResult(LatestPeriodo);

    public Task UpsertManyAsync(IEnumerable<InpcMonthlyEntry> entries, CancellationToken ct = default)
    {
        Upserted.AddRange(entries);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<InpcMonthlyEntry>> GetLastAsync(int count, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<InpcMonthlyEntry>>(Data.TakeLast(count).ToList());
}
```

### Security Checklist — completar antes del primer commit

- [x] **TOCTOU doble-request en endpoints manuales**: Los endpoints `POST .../sync-tiie/run` y `POST .../sync-inpc/run` enqueuen un job. Si se llaman dos veces, se encolan dos ejecuciones. Esto es aceptable — Hangfire los serializa y la segunda ejecución encuentra la tabla ya al día (AC6 guard: `from > today → return`). No requiere guard adicional.
- [x] **Auth-gating**: Los endpoints Ops requieren `RequireAuthorization("AdminOps")` — ya en el template de T8.
- [x] **Denominador cero en algoritmo de variación anual**: El cálculo `current.InpcIndex / yearAgo.InpcIndex` puede fallar si `yearAgo.InpcIndex == 0`. El INPC real nunca es cero, pero agregar guard:
  ```csharp
  if (yearAgo.InpcIndex == 0) continue;
  ```

### Project Structure — archivos afectados

```
src/Server/
  Domain/Ops/
    OperationalConfig.cs               UPDATE — +Tiie28dRate, +Tiie28dRateUpdatedAt
    InpcMonthlyEntry.cs                NEW

  Application/
    Integrations/
      IBanxicoClient.cs                UPDATE — +GetTiie28dAsync, +GetInpcHistoryAsync
    Ops/
      IOperationalConfigRepository.cs  UPDATE — +UpdateTiieRateAsync
      IInpcRepository.cs               NEW
    Jobs/
      BanxicoSyncJob.cs                UPDATE — agregar llamada TIIE
      BanxicoMonthlySyncJob.cs         NEW

  Infrastructure/
    Integrations/Banxico/
      BanxicoClient.cs                 UPDATE — +GetTiie28dAsync, +GetInpcHistoryAsync
    Persistence/
      SqlServer/Configurations/Ops/
        OperationalConfigConfiguration.cs  UPDATE — +tiie_28d_rate cols
        InpcMonthlyConfiguration.cs        NEW
      Repositories/Ops/
        OperationalConfigRepository.cs     UPDATE — +UpdateTiieRateAsync
        InpcRepository.cs                  NEW
      AppDbContext.cs                       UPDATE — +DbSet<InpcMonthlyEntry>
    Migrations/SqlServer/
      XXXXXX_AddTiie28dAndInpcMonthly.cs    NEW (generado por EF)

  Api/
    CompositionRoot/
      ApiServiceExtensions.cs          UPDATE — registrar IInpcRepository + DI jobs
    Endpoints/Private/
      IndicadoresEndpoints.cs          UPDATE — inyectar IInpcRepository + computar INPC
    Endpoints/Ops/
      OpsMarketEndpoints.cs (o nuevo)  UPDATE — 2 endpoints manuales
    Program.cs                         UPDATE — RecurringJob mensual INPC

  SharedApiContracts/Market/
    IndicadoresDto.cs                  UPDATE — +Tiie28d, +InpcHistory
    InpcMonthlyDto.cs                  NEW

src/Web/Main/src/
  api/fibrasApi.ts                     UPDATE — codegen actualiza tipos automáticamente
  modules/herramientas/
    HerramientasPage.tsx               UPDATE — badge TIIE de referencia
  shared/
    (no se requieren componentes nuevos — el badge es inline)
src/Web/SharedApiClient/
  schema.d.ts                          UPDATE — regenerado por codegen

tests/Unit/Infrastructure.Tests/
  Integrations/Banxico/
    BanxicoClientTests.cs              UPDATE — +6 tests TIIE + INPC
  Jobs/
    BanxicoSyncJobTests.cs             UPDATE — +3 tests TIIE
    BanxicoMonthlySyncJobTests.cs      NEW — 5 tests
  Persistence/Repositories/
    OperationalConfigRepositoryTests.cs  UPDATE — +1 test TIIE
    InpcRepositoryTests.cs               NEW — 5 tests
tests/Integration/Api.Tests/
  IndicadoresEndpointTests.cs          UPDATE — +test TIIE y INPC en respuesta
```

### Referencias

- BanxicoClient actual: [BanxicoClient.cs](src/Server/Infrastructure/Integrations/Banxico/BanxicoClient.cs) — helper `TryGetDato` reutilizable
- IBanxicoClient: [IBanxicoClient.cs](src/Server/Application/Integrations/IBanxicoClient.cs)
- BanxicoSyncJob existente: [BanxicoSyncJob.cs](src/Server/Application/Jobs/BanxicoSyncJob.cs)
- OperationalConfig: [OperationalConfig.cs](src/Server/Domain/Ops/OperationalConfig.cs)
- OperationalConfigRepository: [OperationalConfigRepository.cs](src/Server/Infrastructure/Persistence/Repositories/Ops/OperationalConfigRepository.cs) — patrón `UpdateCetesRateAsync`
- IndicadoresEndpoints: [IndicadoresEndpoints.cs](src/Server/Api/Endpoints/Private/IndicadoresEndpoints.cs)
- IndicadoresDto: [IndicadoresDto.cs](src/Server/SharedApiContracts/Market/IndicadoresDto.cs)
- Story previa con patrón INPC job: [3-6-daily-snapshot-incremental-y-benchmarks.md](_bmad-output/implementation-artifacts/3-6-daily-snapshot-incremental-y-benchmarks.md) — patrón incremental reutilizable
- Story de origen Banxico: [11-6-herramientas-hub-privado.md](_bmad-output/implementation-artifacts/11-6-herramientas-hub-privado.md) — infraestructura que esta historia extiende

## Dev Agent Record

### Agent Model Used

GPT-5 via Codex

### Debug Log References

- `dotnet ef migrations add AddTiie28dAndInpcMonthly --project src/Server/Infrastructure --startup-project src/Server/Api --configuration Release`
- `npm run codegen:api`
- `dotnet build FIBRADIS.slnx`
- `dotnet test tests/Unit/Infrastructure.Tests/Infrastructure.Tests.csproj`
- `dotnet test tests/Unit/Domain.Tests/Domain.Tests.csproj`
- `dotnet test tests/Unit/Application.Tests/Application.Tests.csproj`
- `dotnet test tests/Integration/Api.Tests/Api.Tests.csproj`
- `dotnet test tests/Integration/Jobs.Tests/Jobs.Tests.csproj`
- `dotnet test tests/Integration/Persistence.Tests/Persistence.Tests.csproj` (no tests disponibles)
- `dotnet test tests/Integration/Integrations.Tests/Integrations.Tests.csproj` (no tests disponibles)
- `npm test --workspace=src/Web/Main`
- `npm run build --workspace=src/Web/Main`
- `npx tsc --noEmit -p src/Web/Main/tsconfig.json`
- `dotnet ef migrations list --project src/Server/Infrastructure --startup-project src/Server/Api --configuration Release`

### Completion Notes List

- TIIE 28d quedó integrada en `BanxicoSyncJob`, `OperationalConfig` y `IndicadoresDto`.
- Se agregó `BanxicoMonthlySyncJob` con sincronización INPC mensual y algoritmo de catch-up simple.
- Se creó `ops.InpcMonthly` con migración EF real y `IInpcRepository` para upsert e historial.
- `HerramientasPage` ahora muestra la TIIE 28d vigente como referencia junto a CETES.
- `schema.d.ts` fue regenerado después del build para reflejar el contrato OpenAPI actualizado.
- Validaciones principales verdes: 500/500 unit tests de Infrastructure, 107/107 unit tests de Application, 8/8 unit tests de Domain, 288/288 API integration tests, 2/2 Jobs integration tests, `npm test`, `npm run build`, `tsc --noEmit`, y migración presente en `dotnet ef migrations list`.

### Change Log

- Backend: nuevos campos `Tiie28dRate` y `Tiie28dRateUpdatedAt`, nueva entidad `InpcMonthlyEntry`, nueva interfaz `IInpcRepository`, nuevo job `BanxicoMonthlySyncJob`, nuevo endpoint Ops para encolar sync manual, y actualización de `BanxicoClient` para TIIE/INPC.
- Datos: migración `AddTiie28dAndInpcMonthly` crea `ops.InpcMonthly` y columnas TIIE en `ops.OperationalConfig`.
- Contrato/UI: `IndicadoresDto` expone `Tiie28d` e `InpcHistory`; `HerramientasPage` muestra la TIIE vigente; cliente OpenAPI regenerado.
- Tests: cobertura nueva/actualizada para Banxico client, jobs, repositorios y endpoint privado de indicadores.

### Review Findings

- [x] [Review][Patch P1] `DateTime.UtcNow` → `DateTimeOffset.UtcNow` en `BanxicoMonthlySyncJob` [src/Server/Application/Jobs/BanxicoMonthlySyncJob.cs:15] — `now` se usa para `DateOnly.FromDateTime(now)` y `todayPeriod` pero `CapturedAt` llama `DateTimeOffset.UtcNow` por separado (dos llamadas al reloj en vez de una). Todo el codebase usa `DateTimeOffset`; capturar una sola vez y derivar `DateOnly` de ahí.
- [x] [Review][Defer D1] `UpsertManyAsync` usa N `FindAsync` en vez de `ExecuteUpdateAsync` — deferred, pre-existing concern [src/Server/Infrastructure/Persistence/Repositories/Ops/InpcRepository.cs:13] — Dev Notes indican patrón `ExecuteUpdateAsync + Add`. El código usa `FindAsync` (N round-trips al DB). Correcto funcionalmente para 25 entradas/mes pero diverge del patrón preferido del repo.
- [x] [Review][Defer D2] Naming: `/sync-tiie/run` y job ID `"banxico-cetes-sync"` ejecutan CETES+TIIE — deferred, constrained by Dev Notes single-job rule [src/Server/Api/Endpoints/Ops/OpsBanxicoEndpoints.cs:14, src/Server/Api/Program.cs:182] — Deuda de naming. Cambiar el job ID en Hangfire es operacionalmente riesgoso; Dev Notes obligan a job único CETES+TIIE.
- [x] [Review][Defer D3] `IReadOnlyList<InpcMonthlyDto>?` nullable pero `BuildInpcHistory` nunca retorna null — deferred, minor contract quality [src/Server/SharedApiContracts/Market/IndicadoresDto.cs:4] — Requeriría re-run de codegen y actualización de schema.d.ts.

### File List

- `src/Server/Domain/Ops/OperationalConfig.cs`
- `src/Server/Domain/Ops/InpcMonthlyEntry.cs`
- `src/Server/Application/Integrations/IBanxicoClient.cs`
- `src/Server/Application/Ops/IOperationalConfigRepository.cs`
- `src/Server/Application/Ops/IInpcRepository.cs`
- `src/Server/Application/Jobs/BanxicoSyncJob.cs`
- `src/Server/Application/Jobs/BanxicoMonthlySyncJob.cs`
- `src/Server/Infrastructure/Integrations/Banxico/BanxicoClient.cs`
- `src/Server/Infrastructure/Persistence/Repositories/Ops/OperationalConfigRepository.cs`
- `src/Server/Infrastructure/Persistence/Repositories/Ops/InpcRepository.cs`
- `src/Server/Infrastructure/Persistence/SqlServer/Configurations/Ops/OperationalConfigConfiguration.cs`
- `src/Server/Infrastructure/Persistence/SqlServer/Configurations/Ops/InpcMonthlyConfiguration.cs`
- `src/Server/Infrastructure/Persistence/SqlServer/AppDbContext.cs`
- `src/Server/Infrastructure/Migrations/SqlServer/20260613003417_AddTiie28dAndInpcMonthly.cs`
- `src/Server/Infrastructure/Migrations/SqlServer/20260613003417_AddTiie28dAndInpcMonthly.Designer.cs`
- `src/Server/Infrastructure/Migrations/SqlServer/AppDbContextModelSnapshot.cs`
- `src/Server/Api/CompositionRoot/ApiServiceExtensions.cs`
- `src/Server/Api/Endpoints/Private/IndicadoresEndpoints.cs`
- `src/Server/Api/Endpoints/Ops/OpsBanxicoEndpoints.cs`
- `src/Server/Api/Program.cs`
- `src/Server/SharedApiContracts/Market/IndicadoresDto.cs`
- `src/Server/SharedApiContracts/Market/InpcMonthlyDto.cs`
- `src/Web/Main/src/modules/herramientas/HerramientasPage.tsx`
- `src/Web/SharedApiClient/schema.d.ts`
- `scripts/codegen/Api.json`
- `tests/Unit/Infrastructure.Tests/Integrations/Banxico/BanxicoClientTests.cs`
- `tests/Unit/Infrastructure.Tests/Jobs/BanxicoSyncJobTests.cs`
- `tests/Unit/Infrastructure.Tests/Jobs/BanxicoMonthlySyncJobTests.cs`
- `tests/Unit/Infrastructure.Tests/Persistence/Repositories/OperationalConfigRepositoryTests.cs`
- `tests/Unit/Infrastructure.Tests/Persistence/Repositories/InpcRepositoryTests.cs`
- `tests/Integration/Api.Tests/IndicadoresEndpointTests.cs`
