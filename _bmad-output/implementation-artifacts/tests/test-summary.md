# Test Automation Summary — Épicas 2, 3 y 4

Generado: 2026-05-21

## Tests Generados

### Correcciones en tests existentes

- [x] [news-epic4.spec.ts](../../../src/Web/Main/tests/e2e/news-epic4.spec.ts) — Actualizado test AI_MODE: renombre `Manual → On` (story 4-6). Botón ahora es `'On - generar resumen al ingestar'` y se verifica el mensaje de modo activo.
- [x] [fixtures/news-api.ts](../../../src/Web/Main/tests/e2e/fixtures/news-api.ts) — Añadido campo `imageUrl` al tipo `NewsArticle` y a los datos de default. Nuevas funciones: `mockNewsArticleByIdApi` y `mockNewsArticleByIdNotFound` para tests de NoticiaPage.

### E2E Tests (Playwright) — nuevos

- [x] [universe-table.spec.ts](../../../src/Web/Main/tests/e2e/universe-table.spec.ts) — **Épica 2, story 2-6** — FibraUniverseTable
  - Renderiza las 9 columnas de encabezado (Emisora, Precio, Var$, Var%, Volumen, Máx52S, Mín52S, Estado)
  - Muestra una fila por snapshot recibido
  - Filtro por ticker reduce filas visibles
  - Limpiar filtro restaura todas las filas
  - Filtro sin resultados muestra estado vacío
  - Columna Precio es ordenable (asc/desc por precio)

- [x] [distributions-section.spec.ts](../../../src/Web/Main/tests/e2e/distributions-section.spec.ts) — **Épica 3, story 3-4** — DistribucionesSection
  - Muestra yield anualizado formateado
  - Tabla con fecha de pago y monto por CBFI
  - Estado vacío cuando no hay distribuciones
  - Botón "Ver historial" visible cuando hay > 8 distribuciones
  - Expandir historial muestra todas las distribuciones

- [x] [noticias-reader.spec.ts](../../../src/Web/Main/tests/e2e/noticias-reader.spec.ts) — **Épica 4, stories 4-5-1 / 4-5-3 / 4-6** — Lector y og:image
  - Home renderiza hasta 5 artículos (story 4-6)
  - Artículo con imageUrl muestra `<img>` con alt del título
  - NoticiaPage en `/noticias/:id` renderiza título
  - NoticiaPage muestra fuente y fecha
  - NoticiaPage muestra resumen IA cuando existe
  - NoticiaPage fallback a snippet cuando no hay resumen IA
  - NoticiaPage enlace externo con `target="_blank" rel="noopener noreferrer"`
  - NoticiaPage muestra skeleton mientras carga
  - NoticiaPage error cuando artículo no existe (404)

### Integration Tests (C# xUnit) — nuevos

- [x] [NewsLatestEndpointTests.cs](../../../tests/Integration/Api.Tests/NewsLatestEndpointTests.cs) — **Épica 4** — GET /api/v1/news y GET /api/v1/news/fibras
  - `GET /api/v1/news` → 200 con array
  - Devuelve como máximo 5 artículos
  - Cada artículo tiene campos requeridos (id, title, source, publishedAt, url)
  - Incluye artículos sembrados
  - `GET /api/v1/news/fibras/{fibraId}` → 200 con array
  - Fibra sin noticias asociadas → array vacío

- [x] [AiModeGetPutTests.cs](../../../tests/Integration/Api.Tests/AiModeGetPutTests.cs) — **Épica 4** — GET/PUT /api/v1/ops/ai-mode
  - GET con token AdminOps → 200 con dto válido (mode Off|On)
  - GET respuesta contiene campos requeridos (mode, updatedAt)
  - PUT mode=On → 204 NoContent
  - PUT mode=On luego GET → mode refleja "On"
  - PUT mode=Off → 204 NoContent
  - PUT mode inválido → 400 BadRequest
  - GET sin token → 401 Unauthorized
  - PUT sin token → 401 Unauthorized
  - PUT con token User (no AdminOps) → 403 Forbidden

## Cobertura por Épica

### Épica 2 — Catálogo y Descubrimiento

| Feature | E2E | Integration |
|---|---|---|
| GET /api/v1/fibras (catálogo paginado) | ✅ | ✅ CatalogEndpointTests |
| GET /api/v1/fibras/{ticker} (ficha) | ✅ public-discovery | ✅ CatalogEndpointTests |
| Home: búsqueda global | ✅ public-discovery | — |
| Home: PriceCarousel | ✅ public-discovery | — |
| Home: GainersLosers | ✅ public-discovery | — |
| Home: FibraUniverseTable (sort, filter) | ✅ universe-table (nuevo) | — |
| SEO / prerender | ✅ public-discovery | — |

### Épica 3 — Mercado y Datos Históricos

| Feature | E2E | Integration |
|---|---|---|
| GET /api/v1/market/snapshots | ✅ market-freshness | ✅ MarketSnapshotsEndpointTests |
| GET /api/v1/market/fibras/{ticker}/history | ✅ price-history | ✅ MarketHistoryEndpointTests |
| FreshnessBadge estados | ✅ market-freshness | — |
| Selector de período (1M/3M/6M/1A) | ✅ price-history | ✅ MarketHistoryEndpointTests |
| Distribuciones en historial | ✅ distributions-section (nuevo) | ✅ MarketHistoryEndpointTests |
| POST /ops/market/daily-snapshot-historical/run | — | ✅ OpsMarketEndpointTests |
| POST /ops/market/distribution/run | — | ✅ OpsMarketEndpointTests |

### Épica 4 — Noticias y Contenido

| Feature | E2E | Integration |
|---|---|---|
| GET /api/v1/news (últimas 5) | ✅ news-epic4 | ✅ NewsLatestEndpointTests (nuevo) |
| GET /api/v1/news/fibras/{fibraId} | ✅ news-epic4 | ✅ NewsLatestEndpointTests (nuevo) |
| GET /api/v1/news/{id} | ✅ noticias-reader (nuevo) | ✅ NewsEndpointsTests |
| og:image en artículos | ✅ noticias-reader (nuevo) | — |
| NoticiaPage /noticias/:id | ✅ noticias-reader (nuevo) | — |
| Blocklist ops | ✅ news-epic4 | ✅ NewsBlocklistOpsEndpointTests |
| GET /api/v1/ops/ai-mode | — | ✅ AiModeGetPutTests (nuevo) |
| PUT /api/v1/ops/ai-mode | ✅ news-epic4 | ✅ AiModeGetPutTests (nuevo) |
| POST /api/v1/ops/news/{id}/ai-summary | ✅ news-epic4 | ✅ AiModeOpsEndpointTests |
| AI summary fallback a snippet | ✅ news-ai-summary | — |

## Resultados de Ejecución

| Suite | Antes | Después | Estado |
|---|---|---|---|
| Integration (Api.Tests, dotnet test) | 69/69 | 77/77 | +8 nuevos pasando |
| Unit Frontend (npm test) | 56/56 | 56/56 | sin regresiones |
| E2E Playwright (npm run test:e2e) | requiere servidor | — | ejecutar manualmente |

## Notas

- Los tests E2E de Playwright requieren `npm run test:e2e` con el servidor Vite corriendo.
- El runner de FIBRADIS (`scripts/run-e2e.mjs`) levanta el servidor automáticamente.
- Los nuevos tests E2E usan `page.route()` para mockear todas las APIs sin dependencia de backend real.
