---
title: 'masdividendos como fuente de eventos futuros del calendario'
type: 'feature'
created: '2026-06-23'
status: 'done'
context: []
baseline_commit: '4816f72bab32d1b2a021685872a7776b3b6077bd'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** El calendario solo muestra distribuciones que Yahoo ya descubrió (históricas) porque masdividendos únicamente *enriquece* filas existentes (`UpdateDistributionBreakdownAsync` nunca inserta). Los eventos **futuros anunciados** que masdividendos sí publica (ej. pago 30/06/2026) nunca aparecen, perdiendo el valor de un calendario prospectivo.

**Approach:** Permitir que el importer de masdividendos **inserte** distribuciones futuras de las FIBRAs del catálogo, sin romper el flujo actual: Yahoo sigue siendo fuente de verdad del monto y, al confirmar el pago, **reconcilia en la misma fila** (un solo registro por evento). Los eventos aún no confirmados por Yahoo se marcan como "anunciados" en el calendario.

## Boundaries & Constraints

**Always:**
- Insertar **solo** registros que matcheen una FIBRA activa del catálogo (reusar `TryMatchFibra`), con `FechaPago > hoy` (UTC) y `monto` parseable: `AmountPerUnit`=monto, `Source="masdividendos"`, `PaymentDate=FechaPago`, `ExDividendDate=FechaExDerecho`.
- Una sola fila por evento: Yahoo es fuente de verdad del monto. Al confirmar un pago que matchea una fila `masdividendos` (por `PaymentDate` **o** `ExDividendDate==fecha de Yahoo`), actualiza `AmountPerUnit` y pone `Source="yahoo"`; **no** crea fila nueva.
- Preservar la regla actual de no sobrescribir desglose fiscal ya guardado.

**Ask First:**
- Si se quiere insertar también eventos **pasados** que Yahoo nunca trajo (hoy fuera de alcance).
- Cualquier cambio al índice único `(FibraId, PaymentDate)` de `Distribution`.

**Never:**
- Insertar emisoras que no sean FIBRAs del catálogo (FMX23, FSOCIAL23, CMOCTEZ, etc. siguen sin importarse — por diseño).
- Crear una tabla/entidad nueva de eventos corporativos.
- Tocar la cadencia/registro Hangfire del `DistributionPipelineJob`.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Evento futuro nuevo | Record masdividendos matchea FIBRA, `FechaPago > hoy`, sin fila existente | Inserta `Distribution` Source=`masdividendos`, monto parseado, Pago+ExDerecho en calendario marcados "anunciado" | Si monto no parsea → skip (no insert) |
| Yahoo confirma después | Existe fila `masdividendos` futura; Yahoo trae el dividendo (su fecha = ex-derecho) | Match por `ExDividendDate == fechaYahoo`; actualiza monto + Source=`yahoo`; misma fila; deja de marcarse "anunciado" | N/A |
| Evento futuro ya insertado | Record masdividendos matchea fila `masdividendos` existente | Enriquece (desglose/aviso/ex-date) y actualiza monto; **no** duplica | N/A |
| Evento pasado sin fila | Record matchea FIBRA, `FechaPago <= hoy`, sin fila Yahoo | Comportamiento actual (unmatched/skip); **no** inserta | N/A |
| Record no-FIBRA | Ticker no matchea catálogo | `unmatched` (sin cambios) | N/A |

</frozen-after-approval>

## Code Map

- `src/Server/Domain/Market/Distribution.cs` -- entidad; campo `Source` ("seed"|"yahoo"|"masdividendos"|"manual"). Sin cambios de esquema.
- `src/Server/Application/Market/IMarketRepository.cs` -- contrato; agregar método de inserción anunciada.
- `src/Server/Infrastructure/Persistence/Repositories/Market/MarketRepository.cs` -- `UpsertDistributionAsync` (Yahoo, reconciliar), nuevo `InsertAnnouncedDistributionIfAbsentAsync`.
- `src/Server/Infrastructure/Jobs/Market/MasDividendosImporterService.cs` -- agregar rama de inserción futura.
- `src/Server/Infrastructure/Jobs/Market/MasDividendosImportResult.cs` -- agregar `Inserted`.
- `src/Server/Infrastructure/Jobs/Market/DistributionPipelineJob.cs` -- exponer `inserted` de masdividendos en logging/`details`.
- `src/Server/SharedApiContracts/Market/FibraHistoryDto.cs` -- `CalendarEventDto`: agregar `bool IsEstimated`.
- `src/Server/Api/Endpoints/Public/MarketEndpoints.cs` -- poblar `IsEstimated = Source=="masdividendos"`.
- `src/Web/Main/src/modules/calendario/EventChip.tsx`, `CalendarGrid.tsx`, `CalendarioPage.tsx` -- indicador "anunciado".

## Tasks & Acceptance

**Execution:**
- [x] `IMarketRepository.cs` + `MarketRepository.cs` -- agregar `InsertAnnouncedDistributionIfAbsentAsync(fibraId, ticker, paymentDate, exDate, amount, taxable, capital, avisoUrl, currency, ct)`: si existe fila por `(FibraId, paymentDate)` o `(FibraId, ExDividendDate==paymentDate)` o `(FibraId, PaymentDate==exDate)` → retorna `false`; si no → inserta `Source="masdividendos"` y retorna `true` (capturar SqlException 2627/2601 → false).
- [x] `MarketRepository.cs` -- modificar `UpsertDistributionAsync`: si no matchea por `PaymentDate`, intentar fallback por `ExDividendDate == dist.PaymentDate` (solo filas `Source=="masdividendos"`), actualizar `AmountPerUnit` y poner `Source="yahoo"`, conservando su `PaymentDate`; retorna `false` (no insertó). Mantener inserción cuando no hay match.
- [x] `MasDividendosImporterService.cs` -- tras `UpdateDistributionBreakdownAsync`, si no hubo fila a enriquecer **y** `FechaPago > hoy` **y** `amount != null`, llamar `InsertAnnouncedDistributionIfAbsentAsync`; contar `inserted`. Computar `hoy = DateOnly.FromDateTime(DateTime.UtcNow)`.
- [x] `MasDividendosImportResult.cs` -- agregar `int Inserted` y propagar en `DistributionPipelineJob` (log + `details.masDividendos.inserted`).
- [x] `FibraHistoryDto.cs` -- agregar `bool IsEstimated` al final de `CalendarEventDto`.
- [x] `MarketEndpoints.cs` -- en ambos eventos (Pago/ExDerecho) setear `IsEstimated = string.Equals(dist.Source, "masdividendos", OrdinalIgnoreCase)`.
- [x] `npm run codegen:api` -- regenerar cliente tipado.
- [x] `EventChip.tsx` + `CalendarGrid.tsx` + `CalendarioPage.tsx` -- cuando `event.isEstimated`: estilo punteado/secundario en el chip de celda y badge "Anunciado" en la tarjeta de agenda; en el popover, nota "Fecha anunciada (masdividendos), sujeta a confirmación".
- [x] Tests unitarios -- cubrir la I/O Matrix (3 tests de repo + 4 del importer en `MarketRepositoryDistributionTests` y `MasDividendosImporterServiceTests`; caso no-FIBRA ya cubierto por `TryMatchFibra`).

**Acceptance Criteria:**
- Given una FIBRA del catálogo con un anuncio futuro en masdividendos y sin fila previa, when corre el pipeline, then aparece en `/calendario` del mes futuro marcado como "anunciado".
- Given una fila `masdividendos` futura, when Yahoo confirma ese pago en una corrida posterior, then existe **una sola** fila con `Source="yahoo"` y deja de marcarse "anunciado".
- Given un record de masdividendos que no matchea ninguna FIBRA del catálogo, when corre el pipeline, then no se inserta nada (queda `unmatched`).

## Design Notes

Quirk clave: `YahooFinanceClient` entrega como `PaymentDate` realmente la **fecha ex-derecho** (ver comentario en `UpdateDistributionBreakdownAsync`). Por eso la reconciliación debe matchear contra `ExDividendDate` de la fila anunciada, no solo `PaymentDate`; la fila conserva su `PaymentDate` (pago real de masdividendos, más preciso que Yahoo).

Orden del pipeline sin cambios: Yahoo primero (históricos + reconcilia anunciados), luego masdividendos (enriquece + inserta futuros). Riesgo acotado: el fallback por `ExDividendDate` podría matchear un evento ajeno si coincide fibra+fecha exacta (improbable). Filas `masdividendos` que Yahoo nunca confirme quedan "anunciado" (limpieza fuera de alcance).

## Verification

**Commands:**
- `dotnet build FIBRADIS.slnx` -- expected: sin errores.
- `dotnet test tests/Unit/Infrastructure.Tests` -- expected: verdes; incluye nuevos tests de `MarketRepositoryDistributionTests` (insert anunciado + reconciliación Yahoo por ex-date) y `MasDividendosImporterService`/`DistributionPipelineJobTests` (futuro sin fila → inserta; pasado sin fila → no inserta; fila existente → enriquece sin duplicar; monto null → no inserta).
- `npm run build --workspace=src/Web/Main` -- expected: sin errores TypeScript tras `codegen:api`.
