# Test Automation Summary

## Scope

Cobertura aplicada a la **Épica 4 completa** tras releer historias `4.1`, `4.2`, `4.3` y la retrospectiva `epic-4-retro-2026-05-20.md`.

## Generated Tests

### API Tests
- [ ] No se generaron tests API nuevos en esta pasada. La épica ya cuenta con integración backend en `tests/Integration/Api.Tests/NewsBlocklistOpsEndpointTests.cs` y `tests/Integration/Api.Tests/AiModeOpsEndpointTests.cs`.

### E2E Tests
- [x] `src/Web/Main/tests/e2e/news-epic4.spec.ts` - Cubre Home, ficha pública y Ops para los flujos visibles de la épica.
- [x] `src/Web/Main/tests/e2e/news-ai-summary.spec.ts` - Mantiene cobertura focalizada del fallback `aiSummary ?? snippet`.
- [x] `src/Web/Main/tests/e2e/fixtures/news-api.ts` - Mock reusable para feed general y noticias por FIBRA.
- [x] `src/Web/Main/tests/e2e/fixtures/ops-news-api.ts` - Mock reusable para blocklist Ops, AI_MODE y trigger manual de resumen.

## Coverage Matrix

- [x] Historia 4.1 - AC5 blocklist actualizable desde Ops: alta/edición/eliminación visible en UI Ops.
- [x] Historia 4.2 - Feed general de Home: artículos ordenados y visibles en mundo público.
- [x] Historia 4.2 - Item sin asociación: aparece en Home y no aparece en ficha específica.
- [x] Historia 4.2 - FIBRA sin noticias: estado vacío `"Sin noticias disponibles"`.
- [x] Historia 4.3 - Cambio de AI_MODE en Ops con auditoría visible.
- [x] Historia 4.3 - Trigger manual disponible solo en modo Manual.
- [x] Historia 4.3 - Main muestra `aiSummary` cuando existe y `snippet` como fallback.

## Gaps Confirmed By Retrospective

- [ ] Smoke test con **Google News RSS real**: sigue pendiente.
- [ ] Smoke test con **Gemini real + API key real**: sigue pendiente.
- [ ] Flujo cross-surface real `Ops -> trigger manual -> resumen persistido -> visible en Main` contra backend real: sigue pendiente.

## Validation

- `npx playwright test tests/e2e/news-ai-summary.spec.ts` ejecutado en `src/Web/Main` con Vite local en `http://127.0.0.1:5173`
- Resultado: `2 passed, 0 failed`
- `npx playwright test tests/e2e/news-epic4.spec.ts` ejecutado con Main en `http://127.0.0.1:5173` y Ops en `http://127.0.0.1:4174`
- Resultado: `4 passed, 0 failed`

## Next Steps

- Ejecutar el smoke real que la retrospectiva marcó como critical path: RSS real + Gemini real.
- Si el equipo quiere CI de Ops, extraer esta cobertura a una config Playwright dedicada para `src/Web/Ops` en una pasada futura.
