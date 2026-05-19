# Test Automation Summary

## Generated Tests

### E2E Tests
- [x] `src/Web/Main/tests/e2e/public-discovery.spec.ts` - Home publica, busqueda global, navegacion a ficha, 404 y smoke responsive
- [x] `src/Web/Main/tests/e2e/market-freshness.spec.ts` — FreshnessBadge: 4 estados + null + precio con badge (7 tests)
- [x] `src/Web/Main/tests/e2e/price-history.spec.ts` — Selector de período, métricas 52sem/vol, valores numéricos (8 tests)

### API Integration Tests (xUnit)
- [x] `tests/Integration/Api.Tests/MarketSnapshotsEndpointTests.cs` — GET /api/v1/market/snapshots (6 tests)
- [x] `tests/Integration/Api.Tests/MarketHistoryEndpointTests.cs` — GET /api/v1/market/fibras/{ticker}/history (13 tests)

### Test Infrastructure
- [x] `tests/Integration/Api.Tests/ApiWebFactory.cs` — `SeedMarketAsync()` agrega PriceSnapshot y DailySnapshot para FUNO11
- [x] `src/Web/Main/tests/e2e/fixtures/market-api.ts` — `mockMarketApi()` intercepta snapshots e historial con datos sintéticos

## Coverage

| Endpoint / Feature | Cobertura |
|---|---|
| `GET /api/v1/market/snapshots` | ✅ 6 tests (estructura, freshnessStatus, null case, campos requeridos) |
| `GET /api/v1/market/fibras/{ticker}/history` | ✅ 13 tests (happy path, períodos, 404, yield, case-insensitive) |
| FreshnessBadge (Fresh/Stale/Crítico/Fuera de horario/null) | ✅ 7 tests E2E |
| Selector de período 1M/3M/6M/1A + aria-pressed | ✅ 4 tests E2E |
| Métricas Máx/Mín 52sem y Volumen | ✅ 2 tests E2E |
| Home publica, búsqueda global, ficha 404 | ✅ (Épica 2, previo) |

## Verification

### 2026-05-19 — Stories 3.1–3.3 (Market Data Pipeline)
- [x] `dotnet test tests/Integration/Api.Tests/` → **50 passed, 0 failed** (31 prev + 19 nuevos)
- [x] E2E market-freshness.spec.ts → **7 passed, 0 failed**
- [x] E2E price-history.spec.ts → **8 passed, 0 failed**

> **Nota:** `public-discovery.spec.ts:8` falla porque el texto placeholder `"Precios de mercado — disponible en Épica 3"` ya no existe tras implementar Epic 3. No es regresión de esta sesión.

### Anterior — Stories Épica 2
- [x] Unit/frontend checks: `15 passed, 0 failed`
- [x] E2E Playwright: `4 passed, 0 failed`

## Commands

```bash
# Integration tests (backend)
dotnet build tests/Integration/Api.Tests/ --no-dependencies
dotnet test tests/Integration/Api.Tests/ --no-build

# E2E tests (frontend — inicia servidor en 127.0.0.1:5173)
cd src/Web/Main
node scripts/run-e2e.mjs tests/e2e/market-freshness.spec.ts tests/e2e/price-history.spec.ts
```

## Notes

- Los E2E interceptan `/api/v1/market/snapshots` y `/api/v1/market/fibras/**` via `page.route()` — sin dependencia del API real.
- `SeedMarketAsync()` en ApiWebFactory usa EnsureCreatedAsync para aplicar HasData (fibras + distribuciones) y añade un PriceSnapshot/DailySnapshot específico de test para FUNO11.
- Los integration tests de history verifican yield calculado con distribuciones de seed real (YieldCalculator TTM).
- `public-discovery.spec.ts` necesita actualización para reflejar la UI de Épica 3 (el placeholder ya no existe).

## Next Steps

- Corregir `public-discovery.spec.ts:8` para adaptarse al UI actual de Épica 3.
- Agregar tests unitarios para `YieldCalculator` con fechas borde (distribuciones cerca del cutoff de 365 días).
- Considerar tests de integración para `MarketPipelineJob` (Hangfire) cuando se implemente la Épica 4.
