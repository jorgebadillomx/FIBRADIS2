# Test Automation Summary — Épicas 6-8 (E2E + API)

**Última actualización:** 2026-06-05
**Tests generados (acumulado):** 45 API (épicas 6-7) + 48 E2E (épicas 6-8)
**Estado general:** 73 E2E passing (de ~99 total); 23 pre-existentes fuera de scope no regresionados

---

## Tests E2E — Épicas 6, 7, 8 (esta sesión)

Generados con Playwright + `page.route()` mocks. No requieren backend corriendo.

### Fixtures creados

| Fixture | Descripción |
|---------|-------------|
| `tests/e2e/fixtures/main-auth.ts` | Siembra sessionStorage con JWT falso; mockea `/auth/refresh` → 401 |
| `tests/e2e/fixtures/portfolio-api.ts` | Mock completo de `/portfolio`, upload, column-config, snapshot, positions |
| `tests/e2e/fixtures/opportunities-api.ts` | Mock de `/opportunities/ranking` y `/opportunities/weights` con coverage |
| `tests/e2e/fixtures/comparador-api.ts` | Mock de `/compare?tickers=...` con fixture FUNO11, DANHOS13, FMTY14 |

### Épica 6 — Portafolio (portafolio-posiciones.spec.ts + portafolio-upload.spec.ts)

**10 + 5 = 15 tests — 15/15 ✅**

| Test | Descripción |
|------|-------------|
| KPIs del portafolio se muestran con valores correctos | Inversión total, valor actual |
| Posiciones de la tabla se muestran correctamente | FUNO11, DANHOS13, nombres |
| La sección de posiciones contiene encabezados de tabla | heading, sort hint |
| Badge de señal Buy muestra color verde para descuento >10% sobre NAV | fila FUNO11 visible |
| Datos de mercado faltantes muestran — en lugar de errores | `precioActual: null` → `—` |
| Botón Favoritas primero aparece cuando hay posiciones | visible |
| Botón Archivar portafolio aparece cuando hay posiciones | visible |
| Diálogo de archivar pide confirmación antes de proceder | dialog + cancel |
| La ruta /portafolio carga correctamente (no existe /dashboard) | URL assertion |
| Al hacer clic en celda editable entra en modo edición | inline edit |
| Estado vacío muestra zona de carga con instrucciones | dropzone texto |
| Carga exitosa del archivo muestra el portafolio inmediatamente | 2-phase mock GET |
| Errores de validación muestran tabla de errores por fila | 422 + error table |
| Con portafolio activo muestra diálogo de reemplazo antes de subir | replace dialog |
| Banner de respaldo visible cuando existe snapshot | snapshot banner |

### Épica 7 — Oportunidades y Favoritos (oportunidades.spec.ts + favoritos.spec.ts)

**13 + 4 = 17 tests — 17/17 ✅**

| Test | Descripción |
|------|-------------|
| Página de oportunidades muestra header y tabs | heading, Universo, Promediar tabs |
| Ranking principal muestra todas las FIBRAs en orden descendente | 3 filas, FUNO11 primero |
| Sección de datos limitados aparece con advertencia | Score referencial, FINN13 |
| Configurador de pesos muestra sliders y perfiles | Predeterminado, Renta, Crecimiento |
| Al expandir fila se muestra desglose de contribución | Contribución al score, Desc. NAV, Yield |
| Banner "Universo degradado" cuando coverage.status es Degraded | texto + % |
| Ranking suspendido cuando cobertura cae por debajo del 50% | Ranking no disponible |
| Seleccionar perfil Renta actualiza los pesos | sliders visibles |
| FIBRAs excluidas muestran — en lugar de score | isExcluded fixture |
| Tab Promediar Posición muestra las posiciones del portafolio | FUNO11, DANHOS13 |
| Descargo de responsabilidad es visible en la vista Promediar | texto legal |
| Ingresar títulos adicionales muestra nuevo costo promedio | precio regex |
| Tab Promediar muestra estado vacío sin portafolio | texto vacío |
| Botón de favorito aparece en la ficha pública cuando autenticado | star button |
| Botón Favoritas primero en portafolio alterna estado visual | toggle visible |
| En oportunidades, botón Favoritas primero está presente | visible |
| Toggle de Favoritas primero alterna estado activo | click + visible |

### Épica 8 — Comparador público /comparar (comparador.spec.ts)

**15 tests — 15/15 ✅**

| Test | Descripción |
|------|-------------|
| Página /comparar carga con título correcto y estado vacío | title, heading, prompt text |
| El buscador de FIBRAs muestra sugerencias al escribir | autocompletado FUNO11 |
| Seleccionar dos FIBRAs actualiza URL y muestra tabla | columnheaders Mercado, Fundamentales, ... |
| Chips de FIBRAs seleccionadas aparecen con botón de quitar | chip FUNO11, DANHOS13 |
| Carga desde URL con query param ?fibras= muestra tabla | URL params → tabla directa |
| Datos faltantes muestran — en la celda | FMTY14 con nulls → — |
| Tabla muestra métricas de Mercado correctamente | Precio actual, Cambio día, ... |
| Tabla muestra métricas de Fundamentales correctamente | Cap Rate, NAV, LTV, ... |
| Tabla muestra métricas de Distribuciones correctamente | Distribución trimestral, Yield |
| Límite máximo de 4 FIBRAs deshabilita el input | badge 3, input state |
| No se puede quitar una FIBRA cuando solo quedan 2 | remove buttons disabled |
| El comparador funciona sin autenticación (ruta pública) | no redirect a /login |
| Meta description está presente en la página /comparar | `meta[name="description"]` |
| Sugerencias de autocompletado excluyen FIBRAs ya seleccionadas | FUNO11 desaparece post-select |
| El comparador en 360px no tiene overflow horizontal | scrollWidth check |

---

## Tests API — Épicas 6-7 (sesión anterior)

- [x] `tests/Integration/Api.Tests/Ops/OpsUserEndpointTests.cs` — 22 tests ✅
- [x] `tests/Integration/Api.Tests/PortfolioEndpointTests.cs` — 23 tests ✅

Ver detalles en versión anterior de este archivo (git history).

---

## Estado Suite E2E Completa

| Grupo | Passing | Pre-existing fail |
|-------|---------|-------------------|
| Épica 6 E2E (portafolio) | 15 | — |
| Épica 7 E2E (oportunidades + favoritos) | 17 | — |
| Épica 8 E2E (comparador) | 15 | — |
| Épicas 2-5 E2E (anteriores) | 26 | 23* |
| **Total nuevos** | **47** | — |
| **Total suite** | **73** | **23*** |

*Pre-existentes antes de esta sesión (news-ai-summary, news-epic4, noticias-reader, public-discovery, universe-table). No introducidos por estos cambios.

---

## Fixes de Locators Aplicados

| Archivo | Problema | Solución |
|---------|----------|----------|
| `comparador.spec.ts` | `getByText('Mercado')` → 2 elementos | `getByRole('columnheader', { name: 'Mercado' })` |
| `oportunidades.spec.ts` | `getByText('Ranking principal')` → 2 elementos | `getByRole('heading', { name: /Ranking principal/ })` |
| `oportunidades.spec.ts` | `getByText('Contribución al score por componente')` exact | regex `/Contribución al score por componente/` |
| `oportunidades.spec.ts` | `getByText(/Desc\. NAV/)` → 3 elementos | `.first()` |
| `portafolio-upload.spec.ts` | `getByText('Ticker no encontrado...')` → 2 celdas iguales | `.first()` |
