# Story 10.1: Calendario de Distribuciones

Status: ready-for-dev

## Story

Como visitante o usuario,
quiero ver en una vista mensual las distribuciones de todas las FIBRAs (fecha de pago y fecha ex-derecho),
para que pueda planificar mis decisiones de inversión sin rastrear múltiples fuentes.

## Acceptance Criteria

1. **Dado que** navego a `/calendario`, **Cuando** carga la página, **Entonces** veo un encabezado con el mes actual y botones mes anterior / mes siguiente, y el grid mensual muestra los eventos del mes ordenados por fecha.

2. **Dado que** hay distribuciones registradas para el mes (`market.Distribution.PaymentDate`), **Cuando** carga el calendario, **Entonces** cada distribución aparece como chip "Pago" en su fecha con ticker y monto.

3. **Dado que** una distribución tiene `ExDividendDate` registrada, **Cuando** carga el calendario, **Entonces** aparece un chip "ex-derecho" en esa fecha con el ticker correspondiente.

4. **Dado que** hago clic en cualquier chip, **Entonces** se muestra un popover con: ticker, empresa, monto total, desglose fiscal (`TaxableAmount` / `CapitalReturnAmount`) si disponible, fecha ex-derecho, y link al aviso BMV si disponible.

5. **Dado que** el pipeline de distribuciones (`DistributionPipelineJob`) corre o se dispara manualmente, **Entonces** los nuevos registros aparecen en el calendario sin redespliegue.

6. **Dado que** AdminOps dispara el sync manual desde Ops (`POST /api/v1/ops/distributions/sync`), **Entonces** el job se encola en Hangfire y responde 202.

7. **Dado que** AdminOps necesita corregir o añadir una distribución manualmente (fallback si la fuente falla), **Entonces** puede hacerlo desde la página Ops `/ops/distribuciones` con formulario que incluye los campos del desglose fiscal.

8. **Dado que** el mes no tiene eventos registrados, **Entonces** las celdas vacías solo muestran el número de día y aparece el mensaje "Sin distribuciones registradas para este mes."

9. La ruta `/calendario` es pública y tiene `<title>` y `<meta description>` correctos para SEO.

## Tasks / Subtasks

- [ ] T1 — Enriquecer entidad `Distribution` (AC: 2, 3, 4)
  - [ ] T1.1 — Agregar campos nullable a `Domain.Market.Distribution`:

    ```csharp
    public DateOnly? ExDividendDate { get; set; }
    public decimal? TaxableAmount { get; set; }
    public decimal? CapitalReturnAmount { get; set; }
    public string? AvisoUrl { get; set; }
    ```

  - [ ] T1.2 — Actualizar `DistributionConfiguration`: columnas `ex_dividend_date`, `taxable_amount` `decimal(18,6)`, `capital_return_amount` `decimal(18,6)`, `aviso_url` `nvarchar(500)`; todos nullable
  - [ ] T1.3 — Migración EF Core: `EnrichDistributionTaxBreakdown`
  - [ ] T1.4 — Agregar `GetDistributionsByRangeAsync(DateOnly from, DateOnly to, CancellationToken ct)` a `IMarketRepository` e implementar
  - [ ] T1.5 — Agregar `UpdateDistributionBreakdownAsync(Guid fibraId, DateOnly paymentDate, DateOnly? exDate, decimal? taxable, decimal? capital, string? avisoUrl, CancellationToken ct)` a `IMarketRepository` e implementar — solo actualiza si el campo destino está null O si el valor cambió

- [ ] T2 — Integración masdividendos.mx (fuente primaria de desglose fiscal) (AC: 5)
  - [ ] T2.1 — Crear `IMasDividendosClient` + `MasDividendosClient` en `Infrastructure/Integrations/MasDividendos/`:
    - Llama `GET https://app.masdividendos.mx/config/conexiones/database.php`
    - Header `Referer: https://app.masdividendos.mx/`
    - Deserializa a `MasDividendosRecord { Id, Empresa, Ticker, Monto, Comentario, FechaPago, FechaExDerecho, LinkAviso }`
    - Registrar como `AddHttpClient<IMasDividendosClient, MasDividendosClient>()` con timeout 30s
  - [ ] T2.2 — Crear `MasDividendosCommentParser` (clase estática) en `Infrastructure/Integrations/MasDividendos/`:
    - Método `Parse(string comentario, decimal totalAmount) → (decimal? taxable, decimal? capital)`
    - Implementar los 4 patrones documentados en Dev Notes
    - Normalizar encoding roto (Ã³ → ó, etc.) antes de parsear
    - Retornar `(null, null)` para "Concepto a confirmar" y similares
  - [ ] T2.3 — Crear `MasDividendosImporterService` en `Infrastructure/Jobs/Market/`:
    - Recibe `IReadOnlyList<Fibra>` (FIBRAs activas)
    - Llama `IMasDividendosClient.GetAllAsync()`
    - Normaliza tickers de masdividendos a tickers FIBRADIS (ver Dev Notes — mapping por prefijo)
    - Por cada registro que matchea una FIBRA activa: llama `UpdateDistributionBreakdownAsync`
    - Retorna `(updated, skipped, unmatched)` para logging

- [ ] T3 — Actualizar `DistributionPipelineJob` (AC: 5)
  - [ ] T3.1 — Cambiar `UpsertDistributionAsync` para que si el registro ya existe, **actualice** `AmountPerUnit` (Yahoo es fuente de verdad para el total) en lugar de retornar false sin cambios — agregar método `UpdateDistributionAmountAsync(Guid fibraId, DateOnly paymentDate, decimal amount)` a repositorio
  - [ ] T3.2 — En `DistributionPipelineJob.ExecuteAsync`: después del loop de Yahoo, invocar `MasDividendosImporterService` — primero Yahoo (establece/actualiza monto), luego masdividendos (enriquece desglose)
  - [ ] T3.3 — Lógica de rango: si `_isInitialLoad` (flag: no hay registros), descargar 1 año; si ya hay registros, solo mes actual (calculado como `new DateOnly(hoy.Year, hoy.Month, 1)` a fin de mes)
  - [ ] T3.4 — Registrar `DistributionPipelineJob` en Hangfire como `RecurringJob` diario a las 8:00 UTC (2:00am Mexico CST) con `JobId = "distribution-pipeline"` en `MarketPipelineSchedule`

- [ ] T4 — Endpoint Ops: sync manual + CRUD distribuciones (AC: 6, 7)
  - [ ] T4.1 — En `OpsMarketEndpoints`: agregar `POST /api/v1/ops/distributions/sync` → encola `DistributionPipelineJob` en Hangfire, responde 202 (patrón igual a `/api/v1/ops/market/run`)
  - [ ] T4.2 — `POST /api/v1/ops/distributions` body: `{ ticker, paymentDate, exDividendDate?, amountPerUnit, taxableAmount?, capitalReturnAmount?, avisoUrl? }` → busca FibraId por ticker, llama `AddDistributionAsync`, responde 201
  - [ ] T4.3 — `PUT /api/v1/ops/distributions/{id}` body igual → actualiza todos los campos, responde 200
  - [ ] T4.4 — `DELETE /api/v1/ops/distributions/{id}` → 204
  - [ ] T4.5 — Página `DistributionsPage.tsx` en Ops (`/ops/distribuciones`): tabla de distribuciones recientes (últimos 3 meses), botón "Ejecutar sync", formulario inline (Popover) para alta/edición manual
  - [ ] T4.6 — Agregar enlace "Distribuciones" al nav de Ops

- [ ] T5 — Endpoint público del calendario (AC: 1, 2, 3)
  - [ ] T5.1 — DTO `CalendarEventDto(string EventType, string Ticker, string Empresa, string Date, decimal AmountPerUnit, decimal? TaxableAmount, decimal? CapitalReturnAmount, string? AvisoUrl)` en `SharedApiContracts.Market`
  - [ ] T5.2 — `GET /api/v1/market/events?from=YYYY-MM-DD&to=YYYY-MM-DD` en `MarketEndpoints.cs`:
    - Llama `GetDistributionsByRangeAsync(from, to)`
    - Por cada distribución emite **hasta 2 eventos**: `EventType = "Pago"` en `PaymentDate`, y si `ExDividendDate != null` → `EventType = "ExDerecho"` en `ExDividendDate`
    - Devuelve lista unificada ordenada por `Date` ASC
    - Parámetros opcionales: default mes actual
  - [ ] T5.3 — Regenerar cliente API: `npm run codegen:api`

- [ ] T6 — Frontend: ruta `/calendario` (AC: 1–4, 8)
  - [ ] T6.1 — Crear módulo `src/Web/Main/src/modules/calendario/`
  - [ ] T6.2 — `calendarUtils.ts`: función pura `calcMonthRange(year, month): { from: string; to: string }` exportable y testeable
  - [ ] T6.3 — `useCalendarEvents(year, month)`: hook con `openapi-fetch`, `queryKey: ['calendar-events', year, month]`, llama `GET /api/v1/market/events`
  - [ ] T6.4 — `CalendarGrid.tsx`: grid 7×6 (Lun–Dom, convención mexicana), estado `{ year, month }` inicializado con `new Date()`, botones prev/next mes, agrupa eventos por fecha en `Map<string, CalendarEventDto[]>`
  - [ ] T6.5 — `EventChip.tsx`: chip "Pago" = `bg-green-100 text-green-800`; chip "ExDerecho" = `bg-blue-100 text-blue-800`; muestra ticker
  - [ ] T6.6 — Popover (shadcn) al clic en chip: ticker, empresa, monto, desglose fiscal si disponible, fecha ex-derecho, link aviso BMV
  - [ ] T6.7 — Empty state cuando no hay eventos
  - [ ] T6.8 — Registrar ruta `/calendario` en `App.tsx` (pública)
  - [ ] T6.9 — Agregar enlace "Calendario" al nav principal

- [ ] T7 — SEO (AC: 9)
  - [ ] T7.1 — `<title>Calendario de Distribuciones — FIBRADIS</title>`
  - [ ] T7.2 — `<meta name="description" content="Consulta el calendario de distribuciones de las 18 FIBRAs mexicanas con fechas de pago, ex-derecho y desglose fiscal. Planifica tus inversiones." />`
  - [ ] T7.3 — Tags OG básicos

- [ ] T8 — Unit tests (AC: 1, 2, 5)
  - [ ] T8.1 — `MasDividendosCommentParserTests`: un test por cada patrón documentado + los 4 casos "Concepto a confirmar" → `(null, null)`
  - [ ] T8.2 — `DistributionConfigurationTests`: verifica nuevos campos (`ex_dividend_date`, `taxable_amount`, etc.)
  - [ ] T8.3 — `MarketRepositoryTests.GetDistributionsByRangeAsync`: sin registros → lista vacía; con registros → solo los del rango
  - [ ] T8.4 — `MarketRepositoryTests.UpdateDistributionBreakdownAsync`: si campo null → actualiza; si ya tiene valor → no sobreescribe
  - [ ] T8.5 — Test endpoint `GET /api/v1/market/events`: distribución con ExDividendDate → retorna 2 eventos; sin ExDividendDate → 1 evento; ordenado por fecha
  - [ ] T8.6 — Frontend: `calcMonthRange(2026, 6)` → `{ from: '2026-06-01', to: '2026-06-30' }`

- [ ] T9 — Build final
  - [ ] T9.1 — `dotnet build FIBRADIS.slnx` sin errores
  - [ ] T9.2 — `npm run build --workspace=src/Web/Main` sin errores TypeScript
  - [ ] T9.3 — `npm run build --workspace=src/Web/Ops` sin errores TypeScript
  - [ ] T9.4 — `dotnet test tests/Unit/` todos verdes

## Dev Notes

### Arquitectura general

El calendario muestra únicamente distribuciones (`market.Distribution`). No hay `CorporateEvent`. La entidad `Distribution` se enriquece con tres campos nullable para el desglose fiscal y la fecha ex-derecho.

**Flujo de datos:**

```text
masdividendos.mx (PHP open endpoint)   →  desglose fiscal + ex-date + aviso BMV
Yahoo Finance (ya integrado)            →  AmountPerUnit (fuente de verdad para el total)
```

**Pipeline diario (Hangfire):**

1. Yahoo: upsert de `AmountPerUnit` para todos los tickers activos (actualiza si cambia)
2. masdividendos: enriquece `TaxableAmount`, `CapitalReturnAmount`, `ExDividendDate`, `AvisoUrl` — solo si el campo está null O si el valor cambió

**Regla de merge crítica:**

- Yahoo puede cambiar `AmountPerUnit` en cualquier momento (es la fuente de verdad)
- masdividendos NO sobreescribe un desglose fiscal ya guardado (se preserva lo que fue válido al momento de captura)
- Excepción: si el `comentario` de masdividendos tiene montos explícitos y el desglose guardado es null, sí escribe

### Endpoint masdividendos.mx

```text
GET https://app.masdividendos.mx/config/conexiones/database.php
Headers: Referer: https://app.masdividendos.mx/
```

Retorna array JSON con todos los registros (actual ~478). No requiere auth. Se usa el array completo y se filtran FIBRAs activas por ticker.

Campos del registro:

```json
{
  "id": "123",
  "empresa": "Fibra Uno",
  "ticker": "FUNO11",
  "monto": "$0.62",
  "comentario": "Resultado fiscal",
  "fecha_pago": "2026-05-11",
  "fecha_ex_derecho": "2026-05-09",
  "link_aviso": "https://www.bmv.com.mx/docs-pub/..."
}
```

**Normalización del monto:** `monto` puede tener `$`, `US$`, espacios; limpiar antes de parsear.

### Mapping de tickers masdividendos → FIBRADIS

masdividendos usa el ticker completo con serie: `FUNO11`, `DANHOS13`, `FIBRAMQ12`. FIBRADIS usa el ticker base. Estrategia de matching:

```csharp
// Normalizar: quitar dígitos del sufijo
private static string NormalizeTicker(string t) =>
    Regex.Replace(t.Trim().ToUpperInvariant(), @"\d+$", "");

// Lookup: buscar en FIBRAs activas por NormalizeTicker(fibra.Ticker) == NormalizeTicker(record.Ticker)
// Si no matchea exacto, intentar Contains (ej. "VESTA*" → buscar fibra cuyo ticker contiene "VESTA")
```

Casos especiales conocidos:

- `VESTA*` → ticker FIBRADIS: `VESTA`
- `FUNO11` → `FUNO`
- `DANHOS13` → `DANHOS`
- `FIBRAMQ12` → `FIBRAMQ`

### MasDividendosCommentParser — patrones

El `comentario` tiene 83 variantes únicas. El parser cubre 79/83 (95%); los 4 no clasificables son "Concepto a confirmar" / "Concepto no mencionado" → retornar `(null, null)`.

**Etiquetas → categoría fiscal:**

- TaxableAmount: `Resultado fiscal` (variantes: `Resultado Fiscal`, con dos puntos, con trailing espacio), `CUFIN` (cualquier variante), `Cuenta de Utilidad Fiscal Neta`, `Distribución de intereses`, `Utilidades`
- CapitalReturnAmount: `Reembolso de capital` (variantes mayúsculas), `Retorno de Capital`, `CUCA`, `Cuenta de Capital de Aportación`

**Orden de patrones a probar (primero que matchee gana):**

1. **Porcentajes:** `(\d+)%\s+(Label)` — calcular con `totalAmount * pct/100`
2. **$X Label** separado por `;` o `,`: regex `\$([\d.]+)\s+([^;,\n$]+)` — acumular por categoría
3. **Label: $X** o **Label: X**: regex `([A-Za-z\s]{4,40}):\s*[$]?\s*([\d.]+)` — acumular por categoría
4. **Etiqueta simple** (sin montos): toda la línea → clasificar y asignar `totalAmount`
5. **Sin clasificar:** retornar `(null, null)`

**Normalización de encoding** (antes de cualquier parsing):

```csharp
s = s.Replace("Ã³", "ó").Replace("Ã¡", "á").Replace("Ã©", "é").Replace("Ã¼", "ü");
```

**Casos CUFIN:** todas las variantes (`CUFIN 2013`, `CUFIN 2014 y posteriores`, `Otros CUFIN`, etc.) son 100% TaxableAmount.

**Caso `$X CUFIN A; $Y CUFIN B`:** ambas van a TaxableAmount (T = X + Y, C = null).

**Caso `50% Resultado fiscal, 50% Reembolso de capital`:** requiere `totalAmount` para calcular. Si `totalAmount` es null (no disponible en el contexto), retornar `(null, null)`.

### Lógica incremental del pipeline

```csharp
// En DistributionPipelineJob:
bool isInitialLoad = (await marketRepo.GetDistributionCountAsync(ct)) == 0;
DateOnly from = isInitialLoad
    ? DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1))
    : new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
DateOnly to = new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month,
    DateTime.DaysInMonth(DateTime.UtcNow.Year, DateTime.UtcNow.Month));
```

`GetDistributionCountAsync` → nuevo método en `IMarketRepository` que retorna `int` (`db.Distributions.CountAsync(ct)`).

### UpdateDistributionBreakdownAsync — regla de no sobreescritura

```csharp
// Solo actualiza si:
// 1. El campo destino es null, O
// 2. El valor nuevo es distinto al existente (con tolerancia 0.000001 para decimales)
// Nunca sobreescribe TaxableAmount o CapitalReturnAmount si ambos ya tienen valor
```

Implementación: `ExecuteUpdateAsync` con condiciones condicionales o `FindAsync` + update manual.

### Endpoint `GET /api/v1/market/events`

```
GET /api/v1/market/events?from=2026-06-01&to=2026-06-30
```

Genera dos eventos por distribución cuando tiene `ExDividendDate`:

```csharp
// Distribución con PaymentDate 2026-06-11 y ExDividendDate 2026-06-09:
yield return new CalendarEventDto("Pago",      ticker, empresa, "2026-06-11", amount, ...);
yield return new CalendarEventDto("ExDerecho", ticker, empresa, "2026-06-09", amount, ...);
```

Si `ExDividendDate` está fuera del rango `[from, to]`, no emitir el evento ExDerecho (no confundir el calendario del mes).

No usar `Task.WhenAll` — `FibradisDbContext` es Scoped y no es thread-safe.

### Frontend — CalendarGrid

- `currentMonth` state: `{ year: number; month: number }` — inicializar con `new Date()`
- Convención mexicana: columnas Lun–Dom (no Dom–Sab)
- Agrupar eventos por `date` en el hook: `Map<string, CalendarEventDto[]>`
- El `EventChip` muestra solo el ticker; el popover muestra el detalle completo
- El desglose fiscal en el popover: si `taxableAmount != null` mostrar "Resultado fiscal: $X / Reembolso: $Y"; si null mostrar "Desglose no disponible"

### Chip de desglose fiscal — cuándo mostrarlo

No todos los tickers tienen desglose. Regla de display:

- Si `taxableAmount != null || capitalReturnAmount != null` → mostrar desglose en popover
- Si ambos null → mostrar solo el monto total con nota "Clasificación fiscal pendiente"

### Ops — DistributionsPage

- Tabla: últimas 90 distribuciones ordenadas por `PaymentDate DESC`
- Columnas: Ticker, Fecha pago, Ex-derecho, Monto, Resultado Fiscal, Reembolso Capital, Fuente
- Botón "Sincronizar" → llama `POST /api/v1/ops/distributions/sync`, muestra toast de confirmación
- Formulario de alta (Popover): todos los campos, Ticker como select de FIBRAs activas
- El form de edición aparece al hacer clic en una fila

### Security Checklist

- [ ] `POST/PUT/DELETE /api/v1/ops/distributions/*` requieren rol `AdminOps`
- [ ] `GET /api/v1/market/events` es `AllowAnonymous`
- [ ] `AvisoUrl` debe validarse como URL absoluta de `bmv.com.mx` antes de persistir (prevenir XSS/open-redirect)
- [ ] El endpoint masdividendos es externo no confiable: deserializar con `JsonSerializerOptions` que ignora campos extra, capturar excepciones de parsing por registro individual

### Project Structure Notes

Archivos a crear (NEW):

- `src/Server/Infrastructure/Integrations/MasDividendos/IMasDividendosClient.cs`
- `src/Server/Infrastructure/Integrations/MasDividendos/MasDividendosClient.cs`
- `src/Server/Infrastructure/Integrations/MasDividendos/MasDividendosRecord.cs`
- `src/Server/Infrastructure/Integrations/MasDividendos/MasDividendosCommentParser.cs`
- `src/Server/Infrastructure/Jobs/Market/MasDividendosImporterService.cs`
- `src/Server/Infrastructure/Migrations/<timestamp>_EnrichDistributionTaxBreakdown.cs`
- `src/Web/Main/src/modules/calendario/CalendarioPage.tsx`
- `src/Web/Main/src/modules/calendario/CalendarGrid.tsx`
- `src/Web/Main/src/modules/calendario/EventChip.tsx`
- `src/Web/Main/src/modules/calendario/useCalendarEvents.ts`
- `src/Web/Main/src/modules/calendario/calendarUtils.ts`
- `src/Web/Ops/src/modules/distribuciones/DistributionsPage.tsx`

Archivos a modificar (UPDATE):

- `src/Server/Domain/Market/Distribution.cs` — 4 campos nuevos
- `src/Server/Infrastructure/Persistence/SqlServer/Configurations/Market/DistributionConfiguration.cs`
- `src/Server/Infrastructure/Persistence/FibradisDbContext.cs` — (si aplica)
- `src/Server/Application/Market/IMarketRepository.cs` — 3 métodos nuevos
- `src/Server/Infrastructure/Persistence/Repositories/Market/MarketRepository.cs`
- `src/Server/Infrastructure/Jobs/Market/DistributionPipelineJob.cs` — integrar masdividendos + lógica incremental
- `src/Server/Infrastructure/Jobs/Market/MarketPipelineSchedule.cs` — registrar job diario distribution
- `src/Server/Api/Endpoints/Ops/OpsMarketEndpoints.cs` — sync + CRUD distribuciones
- `src/Server/Api/Endpoints/Public/MarketEndpoints.cs` — endpoint GET /events
- `src/Server/SharedApiContracts/Market/` — agregar `CalendarEventDto`
- `src/Web/Main/src/App.tsx` — ruta `/calendario`
- `src/Web/Main/src/shared/layout/Nav.tsx` — enlace Calendario
- `src/Web/Ops/src/App.tsx` — ruta `/ops/distribuciones`
- `src/Web/Ops/src/shared/layout/Nav.tsx` — enlace Distribuciones

### References

- Patrón job Hangfire: [src/Server/Infrastructure/Jobs/Market/DistributionPipelineJob.cs]
- Patrón schedule: [src/Server/Infrastructure/Jobs/Market/MarketPipelineSchedule.cs]
- Patrón endpoint Ops + trigger manual: [src/Server/Api/Endpoints/Ops/OpsMarketEndpoints.cs]
- Patrón endpoint público: [src/Server/Api/Endpoints/Public/MarketEndpoints.cs]
- Patrón integración HTTP: [src/Server/Infrastructure/Integrations/Yahoo/]
- Entidad Distribution: [src/Server/Domain/Market/Distribution.cs]
- Configuración EF: [src/Server/Infrastructure/Persistence/SqlServer/Configurations/Market/DistributionConfiguration.cs]
- DbContext thread-safety: [convenciones-fibradis.md — EF Core — nunca Task.WhenAll]
- Checklist SEO: [convenciones-fibradis.md — Checklist de cierre para historias públicas]

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6 (bmad-create-story → revisado por bmad-dev-story prep)

### Debug Log References

### Completion Notes List

### File List
