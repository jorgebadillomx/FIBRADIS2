---
title: 'Calendario portafolio — distribuciones confirmadas con desglose fiscal e ISR'
type: 'feature'
created: '2026-06-18'
status: 'done'
baseline_commit: 'd333626a3809300e2439de40e1a0f94c8ccb5d4a'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** El calendario del portafolio proyecta pagos futuros algorítmicamente detectando cadencia de las últimas 4 distribuciones históricas, produciendo fechas estimadas inexactas sin desglose fiscal (componente CUFIN sujeto a retención ISR vs. retorno de capital CUCA sin retención inmediata).

**Approach:** Crear `GET /api/v1/portfolio/calendar` (privado) que devuelva distribuciones confirmadas de `market.Distribution` para las FIBRAs del portafolio del usuario en la ventana [hoy-2 meses, hoy+1 mes], con `taxableAmount` y `capitalReturnAmount`. Reemplazar la proyección algorítmica en `PortafolioCalendario.tsx` con datos reales e incluir retención ISR del 30% sobre el componente CUFIN.

## Boundaries & Constraints

**Always:**
- Ventana fija: `hoy - 2 meses` a `hoy + 1 mes` (parámetros opcionales para flexibilidad futura)
- ISR_RATE = 0.30 como constante nombrada en el frontend; aplica solo a `taxableAmount`
- `capitalReturnAmount` se muestra íntegro sin retención (reduce costo base, no hay retención inmediata)
- Si ambos `taxableAmount` y `capitalReturnAmount` son `null`, mostrar monto bruto con etiqueta "clasificación fiscal pendiente" sin calcular ISR
- El endpoint devuelve solo distribuciones de FIBRAs que el usuario tiene en portafolio
- Usar `GetDistributionsByRangeAsync` existente + filtro en memoria por FibraIds del usuario (no añadir método nuevo al repositorio)
- El cálculo ISR vive en el frontend; el backend solo expone montos brutos por componente

**Ask First:**
- Si se quiere tasa ISR configurable por usuario (ahora es constante 30%)

**Never:**
- No proyectar fechas: solo distribuciones confirmadas en BD
- No modificar `PortfolioDistributionDto` existente ni `PortfolioResponseDto` (rompe el contrato del endpoint `/portfolio`)
- No tocar el pipeline de ingesta de distribuciones

## I/O & Edge-Case Matrix

| Scenario | Input / Estado | Salida esperada | Error |
|----------|---------------|-----------------|-------|
| Distribución con desglose completo | `taxableAmount=0.35`, `capitalReturnAmount=0.47`, `Titulos=1000` | `TotalAmount=820`, `TotalTaxable=350`, `TotalCapital=470`; frontend muestra ISR=105, neto estimado=715 | N/A |
| Sin clasificación fiscal | `taxableAmount=null`, `capitalReturnAmount=null`, `AmountPerUnit=0.82`, `Titulos=500` | `TotalAmount=410`, `TotalTaxable=null`, `TotalCapital=null`; frontend muestra badge "clasificación pendiente" | N/A |
| Portafolio vacío | sin posiciones | lista vacía; frontend muestra "No hay distribuciones en este período" | N/A |
| Sin autenticación | sin token | 401 | N/A |

</frozen-after-approval>

## Code Map

- `src/Server/SharedApiContracts/Portfolio/PortfolioResponseDto.cs` — añadir `PortfolioCalendarEventDto` (no tocar `PortfolioDistributionDto`)
- `src/Server/Api/Endpoints/Private/PortfolioEndpoints.cs` — añadir `GET /api/v1/portfolio/calendar`
- `src/Server/Application/Market/IMarketRepository.cs` — usar `GetDistributionsByRangeAsync(DateOnly, DateOnly)` existente
- `src/Server/Application/Portfolio/IPortfolioRepository.cs` — usar `GetUserPortfolioAsync(userId)` para FibraIds y `Titulos`
- `src/Web/Main/src/modules/portafolio/PortafolioCalendario.tsx` — reemplazar `projectNextPayments` por `useQuery` al nuevo endpoint; agregar desglose fiscal e ISR
- `src/Web/Main/src/modules/portafolio/portfolio-calendar.ts` — eliminar (reemplazado por datos reales del endpoint)
- `src/Web/Main/src/api/portfolioCalendarApi.ts` — nueva función `fetchPortfolioCalendar(from?, to?)` usando `apiClient`

## Tasks & Acceptance

**Execution:**
- [x] `src/Server/SharedApiContracts/Portfolio/PortfolioResponseDto.cs` -- añadir `PortfolioCalendarEventDto(Ticker, Nombre, LogoUrl?, PaymentDate, AmountPerUnit, TaxableAmount?, CapitalReturnAmount?, Titulos, TotalAmount, TotalTaxable?, TotalCapital?)` -- DTO para el endpoint de calendario; el ISR se calcula en el frontend
- [x] `src/Server/Api/Endpoints/Private/PortfolioEndpoints.cs` -- añadir `GET /api/v1/portfolio/calendar?from=DateOnly&to=DateOnly` (defaults: hoy-2m / hoy+1m). Obtener posiciones del usuario, llamar `GetDistributionsByRangeAsync(from, to)`, filtrar por FibraIds del usuario, cruzar con `Titulos` por FibraId, proyectar DTO con `TotalAmount = AmountPerUnit × Titulos`, ordenar por PaymentDate asc luego Ticker -- nuevo endpoint privado; filtro en memoria sobre el resultado de `GetDistributionsByRangeAsync`
- [x] `tests/Integration/Api.Tests/PortfolioEndpointTests.cs` -- 3 tests: GetCalendar_WithoutToken_Returns401, GetCalendar_WithNoPositions_ReturnsEmptyList, GetCalendar_WithPositionAndDistribution_ReturnsTotalAmountCorrect — 3/3 verdes
- [x] `npm run codegen:api` -- regenerar cliente tipado con el nuevo endpoint -- PortfolioCalendarEventDto presente en schema.d.ts
- [x] `src/Web/Main/src/api/portfolioCalendarApi.ts` -- crear `fetchPortfolioCalendar(from?, to?)` con ventana default calculada aquí; devuelve `PortfolioCalendarEventDto[]` -- encapsula el cálculo de la ventana temporal
- [x] `src/Web/Main/src/modules/portafolio/PortafolioCalendario.tsx` -- reemplazar `projectNextPayments(positions)` por `useQuery` usando `fetchPortfolioCalendar()`; agrupar por mes; por evento mostrar: ticker+logo, fecha, AmountPerUnit/CBFI, Titulos, TotalAmount; si hay desglose: componente CUFIN + ISR 30% (negativo, rojo) + componente CUCA + neto estimado; si no: badge "clasificación pendiente"; estado vacío cuando lista vacía -- ISR_RATE = 0.30 como constante nombrada
- [x] `src/Web/Main/src/modules/portafolio/portfolio-calendar.ts` -- eliminado; también portfolio-calendar.test.ts (tests de proyección algorítmica obsoletos)

**Acceptance Criteria:**
- AC-1: Dado usuario autenticado con posiciones, cuando abre la tab Calendario, entonces ve distribuciones reales agrupadas por mes en la ventana [hoy-2 meses, hoy+1 mes]
- AC-2: Para cada distribución con desglose completo, el neto estimado mostrado = `(TotalCapital + TotalTaxable × 0.70)` y coincide con el ISR indicado
- AC-3: Para distribuciones sin desglose fiscal (`TaxableAmount` y `CapitalReturnAmount` null), el calendario muestra el TotalAmount bruto con badge "clasificación fiscal pendiente", sin ISR
- AC-4: `GET /api/v1/portfolio/calendar` sin token retorna 401
- AC-5: El archivo `portfolio-calendar.ts` ya no existe en el repositorio

## Design Notes

**Patrón visual por distribución con desglose:**
```
FUNO  ·  15 jul 2026  ·  1,200 CBFIs
  Bruto:            $0.8200/CBFI  →  $984.00
  Componente CUFIN: $0.5000       →  $600.00   ISR 30%: -$180.00
  Retorno capital:  $0.3200       →  $384.00   (reduce costo base)
  ───────────────────────────────────────────
  Neto estimado:                      $804.00
```
**Sin clasificación:**
```
FIBRAMQ  ·  08 jun 2026  ·  500 CBFIs
  Bruto: $0.9100/CBFI  →  $455.00   ⚠ clasificación fiscal pendiente
```

**Cruce posiciones-distribuciones (backend):**
```csharp
var posMap = portfolio.Positions.ToDictionary(p => p.FibraId);
var events = distributions
    .Where(d => posMap.ContainsKey(d.FibraId))
    .Select(d => {
        var pos = posMap[d.FibraId];
        return new PortfolioCalendarEventDto(
            pos.Ticker, pos.Nombre, pos.LogoUrl,
            d.PaymentDate.ToString("yyyy-MM-dd"),
            d.AmountPerUnit,
            d.TaxableAmount, d.CapitalReturnAmount,
            pos.Titulos,
            d.AmountPerUnit * pos.Titulos,
            d.TaxableAmount * pos.Titulos,
            d.CapitalReturnAmount * pos.Titulos);
    })
    .OrderBy(e => e.PaymentDate).ThenBy(e => e.Ticker)
    .ToList();
```

## Verification

**Commands:**
- `dotnet build FIBRADIS.slnx` -- expected: 0 errores
- `dotnet test --filter "PortfolioCalendar"` -- expected: tests del cruce y 401 pasan
- `npm run build --prefix src/Web/Main` -- expected: 0 errores TS
- `npm test --prefix src/Web/Main -- --testPathPattern portafolio` -- expected: tests ISR pasan
