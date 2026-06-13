# Story 12.3: Datos financieros estructurados en la ficha de fibra

Status: ready-for-dev

<!-- Depende de 12-1 (módulo SEO administrable + ISeoDefaultsBuilder + inyección DB-driven en FibraProfileMetadataMiddleware). -->

## Story

As a **buscador / motor generativo que indexa FIBRADIS**,
I want **que la ficha de cada fibra (`/fibras/{slug}`) emita JSON-LD enriquecido con precio actual, yield TTM, distribuciones y fecha de actualización**,
so that **Google y los motores de IA entiendan a FIBRADIS como fuente estructurada y confiable de datos financieros de FIBRAs mexicanas (rich results + citabilidad GEO en un dominio YMYL)**.

## Dependencias y contexto
- **Requiere 12-1 done**: `FibraProfileMetadataMiddleware` ya quedó DB-driven con `ISeoDefaultsBuilder` y override por campo. Esta historia **enriquece el JSON-LD `FinancialProduct`** que hoy es básico ([FibraProfileMetadataMiddleware.cs:150-211](src/Server/Api/Middleware/FibraProfileMetadataMiddleware.cs)).
- **Datos disponibles server-side** (el middleware ya crea un `IServiceScopeFactory` scope en `:94`, puede resolver más repos):
  - **Precio actual**: `PriceSnapshot` (`LastPrice`, `DailyChangePct`, `Week52High/Low`, `CapturedAt`) en schema `market`. No hay método "último por fibra"; usar `IMarketRepository.GetLatestSnapshotPerFibraAsync(ct)` + `.FirstOrDefault(s => s.FibraId == fibra.Id)` (patrón de [MarketEndpoints.cs:86-87](src/Server/Api/Endpoints/Public/MarketEndpoints.cs)) o `GetLastSnapshotsAsync(fibraId, 1, ct)`. Moneda: `Fibra.Currency` (no hay currency por snapshot).
  - **Yield TTM (server-side, C#)**: `YieldCalculator.Calculate(distributions, lastPrice, today)` → ratio TTM redondeado a 4 decimales (×100 para %). Alimentar con `IMarketRepository.GetDistributionsAsync(fibraId, maxDays, ct)`. Patrón en [MarketEndpoints.cs:84-90](src/Server/Api/Endpoints/Public/MarketEndpoints.cs). Yield decretado (forward) = `QuarterlyDistribution*4/lastPrice` (ver [CompareEndpoints.cs:124-131](src/Server/Api/Endpoints/Public/CompareEndpoints.cs)).
  - **dateModified**: `PriceSnapshot.CapturedAt` (precio) / `FundamentalRecord.CapturedAt` (fundamentales).

## ⚠️ Decisión de diseño crítica: datos vivos NO se guardan en BD
El precio y el yield **cambian a diario**. Si se serializaran dentro del campo `JsonLd` de `SeoMetadata` (12-1), quedarían **stale**. Por tanto:
- Para fibras, cuando el campo JSON-LD **NO está override**, el `ISeoDefaultsBuilder` **recompone el FinancialProduct con datos vivos en cada request** (lee market repo en el scope del middleware). No se persiste el precio en la fila SEO.
- Cuando un AdminOps marca override del JSON-LD, se respeta el valor manual (regla de 12-1).
- La fila `SeoMetadata` sigue guardando title/description/canonical/og (estables); solo el bloque JSON-LD financiero es dinámico.

## Acceptance Criteria

**AC-1 — FinancialProduct enriquecido.** El JSON-LD de `/fibras/{slug}` incluye, cuando hay datos: `offers` (Offer con `price` = `LastPrice`, `priceCurrency` = `Fibra.Currency`), `dateModified` (= `PriceSnapshot.CapturedAt` ISO 8601), y `additionalProperty` (PropertyValue) para: yield TTM anualizado (%), yield decretado (%), variación 52 semanas (high/low). Mantiene `name`, `alternateName`, `description`, `provider`, `category`, `additionalType` actuales + `BreadcrumbList`.

**AC-2 — Datos vivos en request.** Los valores de precio/yield se leen del repositorio market en cada request (no de la BD SEO). Si no hay snapshot (`LastPrice == null`) o no hay distribuciones, se omiten los campos correspondientes sin romper el JSON-LD (graceful).

**AC-3 — Respeta override de 12-1.** Si el JSON-LD de esa fibra está marcado override, se usa el almacenado; no se recompone. Si no, se recompone con datos vivos.

**AC-4 — Validez schema.org.** El JSON-LD pasa el Rich Results Test / Schema Markup Validator (estructura `FinancialProduct` válida; `Offer`/`PropertyValue` bien tipados). Encoding con `JsonSerializer` + `JavaScriptEncoder` (regla convenciones).

**AC-5 — Sin Task.WhenAll.** Las lecturas EF (snapshot + distribuciones) son secuenciales (regla del proyecto). El costo extra por request se mide; considerar `IMemoryCache` corto (p.ej. 5 min) para `GetLatestSnapshotPerFibraAsync` si el perfil de carga lo amerita (documentar decisión).

**AC-6 — Tests.** Unit del builder financiero con **valores exactos** (dado un snapshot+distribuciones de ejemplo → JSON-LD esperado con price/yield correctos; casos: sin precio, sin distribuciones, yield null). Por-middleware: fibra con datos → JSON-LD enriquecido; fibra sin datos → FinancialProduct básico (no crash). Verdes antes de `done`.

## Tasks / Subtasks

- [ ] **T1 — Lectura de datos vivos en el builder (AC-1, AC-2, AC-5)**: extender `ISeoDefaultsBuilder` (de 12-1) para la rama Fibra: resolver `IMarketRepository` en el scope, obtener latest snapshot + distribuciones, calcular yield con `YieldCalculator`. Reusar exactamente la composición de [MarketEndpoints.cs:84-90] y la math de yield de [CompareEndpoints.cs:124-131].
- [ ] **T2 — Enriquecer FinancialProduct JSON-LD (AC-1, AC-4)**: en `FibraProfileMetadataMiddleware`/builder, agregar `offers`, `dateModified`, `additionalProperty[]`. Mantener `@graph` con `BreadcrumbList`. Tipos schema.org: `Offer{price, priceCurrency}`, `PropertyValue{name, value, unitText:"%"}`.
- [ ] **T3 — Wiring override (AC-3)**: si `SeoMetadata.JsonLd_IsOverridden` → usar stored; si no → recomponer vivo. (Depende del flag de override de 12-1.)
- [ ] **T4 — Caché opcional (AC-5)**: si la medición lo justifica, `IMemoryCache` para el dict de latest snapshots (TTL corto). Documentar en Dev Agent Record si se implementa o se difiere.
- [ ] **T5 — Tests (AC-6)**: unit builder (valores exactos, casos null), por-middleware enriquecido vs básico. `dotnet test tests/Unit/`, `dotnet test tests/Integration/ -m:1`.
- [ ] **T6 — Validación manual**: Rich Results Test sobre una `/fibras/{slug}` real en dev (curl + validador). Registrar en Dev Agent Record.

## Dev Notes
- **Stack real = SQL Server**. Schema `market` ya existe; esta historia **no** crea tablas (solo lee).
- **No persistir datos vivos**: ver §Decisión de diseño. El precio/yield se recomputan por request.
- **Schema.org caveat (YMYL)**: `FinancialProduct` no tiene campo nativo de "precio de mercado"; usar `offers.Offer.price` + `additionalProperty` PropertyValue es el modelado pragmático aceptado. Google puede no mostrar rich result, pero mejora entity clarity y citabilidad GEO — que es el objetivo en finanzas. **`Offer` implica semánticamente "algo en venta"** (no es exacto para el precio de cotización de un CBFI): si el Rich Results Test / Schema Markup Validator marca el `Offer` como inválido o inapropiado, **caer a `PropertyValue`-only** (precio y moneda como `additionalProperty`, sin `Offer`). Decidir en T6 según lo que diga el validador y documentar.
- **Yield: cuidado con la escala**: `YieldCalculator.Calculate` devuelve **ratio** (0.0875), `CompareEndpoints` usa **porcentaje** (8.75). Definir y testear la unidad exacta en el JSON-LD (recomendado %, con `unitText:"%"`).
- **Consistencia de dominio**: el JSON-LD actual de fibra usa `App:BaseUrl` (correcto). NO introducir dominios hardcodeados (a diferencia de los constants de `SpaMetadataProvider` que sí los tienen — eso lo arregla 12-4).
- **Reglas middleware de 12-1 intactas**: encoding JSON-LD, soft-404, GET/HEAD, Cache-Control no-cache.

### Security Checklist — antes del primer commit
- [ ] **TOCTOU**: N/A (solo lectura).
- [ ] **Auth-gating UI**: N/A (ruta pública).
- [ ] **Denominador cero**: `YieldCalculator` y yield decretado dividen entre `lastPrice` — primer test con `lastPrice = 0/null` → yield omitido, no excepción. (Regla convenciones "funciones de cálculo financiero".)
- [ ] **Performance**: lectura extra por request — medir y cachear si aplica (AC-5).

### References
- [FibraProfileMetadataMiddleware.cs:94,133,150-211](src/Server/Api/Middleware/FibraProfileMetadataMiddleware.cs)
- [PriceSnapshot.cs](src/Server/Domain/Market/PriceSnapshot.cs), [Distribution.cs](src/Server/Domain/Market/Distribution.cs)
- [IMarketRepository.cs](src/Server/Application/Market/IMarketRepository.cs), [YieldCalculator.cs](src/Server/Application/Market/YieldCalculator.cs), [MarketRepository.cs](src/Server/Infrastructure/Persistence/Repositories/Market/MarketRepository.cs)
- [MarketEndpoints.cs:84-90](src/Server/Api/Endpoints/Public/MarketEndpoints.cs), [CompareEndpoints.cs:124-131](src/Server/Api/Endpoints/Public/CompareEndpoints.cs)
- [Fibra.cs](src/Server/Domain/Catalog/Fibra.cs) (Currency)
- Story 12-1: [12-1-modulo-seo-administrable.md](_bmad-output/implementation-artifacts/12-1-modulo-seo-administrable.md)
- 2026: [HTML Tags for SEO 2026](https://www.clickrank.ai/html-tags-for-seo/) · [GEO 2026 — Frase](https://www.frase.io/blog/what-is-generative-engine-optimization-geo)

## Hallazgos de auditoría SEO (2026-06-13)

> Auditoría completa (score 84/100): [seo-audit/FULL-AUDIT-REPORT.md](../../seo-audit/FULL-AUDIT-REPORT.md). Hallazgos que tocan la ficha de fibra.

### 🟠 H2 — `<h1>` duplicado en la ficha (fix frontend Main)
La auditoría confirmó **dos elementos `<h1>`** en `/fibras/{slug}`: el título de la ficha (correcto) y un segundo `<h1>` proveniente del markdown de la sección "Descripción", que empieza con `# 🏢 Fibra Uno | FUNO11` (el mismo origen del bug C1/C2 de 12-1; el emoji se ve como `??`). Dos H1 rompen la jerarquía de encabezados.
- **Fix (frontend Main):** al renderizar el markdown de la sección "Descripción" en `FibraPage`, **desplazar los niveles de heading +1** (markdown `#` → `<h2>`/`<h3>`), de modo que el título de la ficha sea el único `<h1>`. Verificar: exactamente un `<h1>` por ficha.
- Relacionado: el fix de encoding del emoji (C2) en 12-1 elimina el `??` también del contenido visible; coordinar para no dejar el emoji corrupto en pantalla.

### Nota sobre AC-1 (FinancialProduct.description)
Esta historia enriquece el `FinancialProduct` con `offers`/`dateModified`/`additionalProperty`, pero **conserva `description`**. Asegurar que la `description` que se reusa sea la **limpia** que produce el builder corregido de 12-1 (C1) — hoy ese campo del JSON-LD también está contaminado con el volcado de markdown. No serializar la description cruda actual.

## Dev Agent Record
### Agent Model Used
### Debug Log References
### Completion Notes List
### File List
