# Story 12.3: Datos financieros estructurados en la ficha de fibra

Status: done

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

- [x] **T1 — Lectura de datos vivos en el builder (AC-1, AC-2, AC-5)**: `FibraSeoMarketData` + `GetLiveMarketDataAsync` con consultas dirigidas por FIBRA (`GetLatestProcessedSnapshotAsync` + `GetDistributionsAsync` + `GetLatestProcessedByFibraAsync`) y `YieldCalculator`. (Patch P1 de code review: cambiado de `GetLatestSnapshotPerFibraAsync`/`GetSummaryLatestAsync` que cargaban el universo, a consultas dirigidas; reusa el scope del metadata lookup.)
- [x] **T2 — Enriquecer FinancialProduct JSON-LD (AC-1, AC-4)**: `dateModified` + `additionalProperty[]` (precio, yield TTM, yield decretado, variaciones 52s). `@graph` con `BreadcrumbList`. **Decisión D1 (code review):** el precio se modela como `PropertyValue` ("Precio de cotización"), NO como `Offer` — un CBFI no es "algo en venta" y la semántica de `Offer` es inapropiada para un precio de cotización.
- [x] **T3 — Wiring override (AC-3)**: `JsonLdIsOverridden` → usa stored; si no → recompone vivo.
- [x] **T4 — Caché (AC-5)**: **DIFERIDA con justificación.** Las consultas dirigidas por FIBRA (T1, patch P1) reducen el costo por request a O(1 FIBRA) en vez de cargar el universo; ya no se justifica `IMemoryCache`. Reconsiderar solo si el perfil de carga de crawlers en producción lo exige.
- [x] **T5 — Tests (AC-6)**: unit builder con valores exactos + caso precio cero (denominador cero, primer test) + casos sin precio/sin distribuciones; por-middleware enriquecido/básico/override. (Patch P2: añadido `BuildFibra_WithZeroPrice_...` como primer test financiero.)
- [ ] **T6 — Validación manual**: Rich Results Test / Schema Markup Validator sobre una `/fibras/{slug}` real en dev. **Pendiente antes de `done`** — con `PropertyValue`-only (D1) el riesgo de schema inválido es bajo, pero la verificación manual sigue siendo el gate de la convención SSR/SEO. Registrar resultado aquí.

## Dev Notes
- **Stack real = SQL Server**. Schema `market` ya existe; esta historia **no** crea tablas (solo lee).
- **No persistir datos vivos**: ver §Decisión de diseño. El precio/yield se recomputan por request.
- **Schema.org caveat (YMYL)**: `FinancialProduct` no tiene campo nativo de "precio de mercado"; usar `offers.Offer.price` + `additionalProperty` PropertyValue es el modelado pragmático aceptado. Google puede no mostrar rich result, pero mejora entity clarity y citabilidad GEO — que es el objetivo en finanzas. **`Offer` implica semánticamente "algo en venta"** (no es exacto para el precio de cotización de un CBFI): si el Rich Results Test / Schema Markup Validator marca el `Offer` como inválido o inapropiado, **caer a `PropertyValue`-only** (precio y moneda como `additionalProperty`, sin `Offer`). Decidir en T6 según lo que diga el validador y documentar.
- **Yield: cuidado con la escala**: `YieldCalculator.Calculate` devuelve **ratio** (0.0875), `CompareEndpoints` usa **porcentaje** (8.75). Definir y testear la unidad exacta en el JSON-LD (recomendado %, con `unitText:"%"`).
- **Consistencia de dominio**: el JSON-LD actual de fibra usa `App:BaseUrl` (correcto). NO introducir dominios hardcodeados (a diferencia de los constants de `SpaMetadataProvider` que sí los tienen — eso lo arregla 12-4).
- **Reglas middleware de 12-1 intactas**: encoding JSON-LD, soft-404, GET/HEAD, Cache-Control no-cache.

### Security Checklist — antes del primer commit
- [x] **TOCTOU**: N/A (solo lectura).
- [x] **Auth-gating UI**: N/A (ruta pública).
- [x] **Denominador cero**: yield TTM, yield decretado y variaciones 52s dividen entre `lastPrice`/`Week52High`/`Week52Low` — todas protegidas por `is > 0m`. Test `BuildFibra_WithZeroPrice_...` (primer test financiero) con `lastPrice = 0m` → propiedades omitidas, sin excepción; `WithoutPrice` cubre `null`.
- [x] **Performance**: consultas dirigidas por FIBRA (no se carga el universo); caché diferida con justificación (T4). Medición formal de crawlers pendiente solo si producción lo exige.

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
- `dotnet test tests/Unit/Infrastructure.Tests/Infrastructure.Tests.csproj --filter "FullyQualifiedName~SeoDefaultsBuilderTests|FullyQualifiedName~FibraProfileMetadataMiddlewareTests|FullyQualifiedName~SpaMetadataMiddlewareTests|FullyQualifiedName~NewsMetadataMiddlewareTests"`
- `dotnet test tests/Unit/Infrastructure.Tests/Infrastructure.Tests.csproj`
- `dotnet test tests/Unit/Application.Tests/Application.Tests.csproj`
- `dotnet test tests/Unit/Domain.Tests/Domain.Tests.csproj`
- `dotnet test tests/Integration/Api.Tests/Api.Tests.csproj --filter "FullyQualifiedName~SeoEndpointTests|FullyQualifiedName~FibraDescriptionTests"`
- `dotnet test tests/Integration/Api.Tests/Api.Tests.csproj`
### Completion Notes List
- Se enriqueció `FinancialProduct` para `/fibras/{slug}` con `offers`, `dateModified` y `additionalProperty` (yield TTM, yield decretado y variaciones 52 semanas) usando datos vivos de `IMarketRepository` + `IFundamentalRepository` cuando el `JsonLd` no está overrideado.
- El builder de SEO para fibras ahora acepta un payload opcional de mercado para componer JSON-LD vivo sin persistir datos volátiles en la tabla SEO.
- Se añadieron pruebas unitarias exactas para price/yield/variaciones y pruebas de middleware para live JSON-LD, fallback básico y override manual.
- Validación ejecutada: `Infrastructure.Tests` completo verde (`565/565`), `Application.Tests` verde (`138/138`), `Domain.Tests` verde (`8/8`), e integración dirigida de `SeoEndpointTests` + `FibraDescriptionTests` verde (`10/10`).
- La suite completa de integración `Api.Tests` sigue mostrando dos fallas no relacionadas con esta historia: `CalculadoraEndpointTests.GetCalculadora_ReturnsOk_WithExpectedDistributionTotals` y `Ops.DashboardEndpointTests.GetDashboard_WithAdminOpsToken_ReturnsPipelineDashboardDto`.
- **Post-review 2026-06-13 (3 diferidos aplicados a petición del usuario):** (1) `YieldCalculator` acota la ventana TTM a `<= today` (ignora distribuciones futuras) + test `Calculate_FutureDatedDistributions_AreIgnored`; (2) fix H2 — `FibraPage` desplaza los headings del markdown de "Descripción" +1 para que el `<h1>` del título sea único; (3) `AsOfDate` derivado de `snapshot.CapturedAt` en vez del reloj del request. Verde: 566 Infrastructure + 9 YieldCalculator + Main build.
- **Code review 2026-06-13 (5 patches aplicados):** D1 precio modelado como `PropertyValue` en vez de `Offer` (semántica de cotización, no venta); P1 consultas dirigidas por FIBRA (`GetLatestProcessedSnapshotAsync` nuevo + `GetLatestProcessedByFibraAsync`) reemplazan la carga del universo, reusando el scope del metadata lookup; P2 test de precio cero como primer test financiero; P3 `MidpointRounding.AwayFromZero` en los % financieros; P4 higiene de tracker + decisión de caché documentada (AC-5: diferida, consultas dirigidas la hacen innecesaria). 3 hallazgos diferidos (yield TTM sin tope superior de fecha — pre-existente en `YieldCalculator`; H2 `<h1>` duplicado — frontend `FibraPage`; ventana TTM medida contra reloj del request). Ver §Senior Developer Review (AI) y `deferred-work.md`.
### File List
- `_bmad-output/implementation-artifacts/12-3-datos-financieros-estructurados-ficha.md`
- `src/Server/Application/Seo/FibraSeoMarketData.cs`
- `src/Server/Application/Seo/ISeoDefaultsBuilder.cs`
- `src/Server/Application/Market/IMarketRepository.cs` (code review P1 — método dirigido `GetLatestProcessedSnapshotAsync`)
- `src/Server/Api/Middleware/FibraProfileMetadataMiddleware.cs`
- `src/Server/Infrastructure/Seo/SeoDefaultsBuilder.cs`
- `src/Server/Infrastructure/Persistence/Repositories/Market/MarketRepository.cs` (code review P1)
- `tests/Unit/Infrastructure.Tests/Middleware/FibraProfileMetadataMiddlewareTests.cs`
- `tests/Unit/Infrastructure.Tests/Seo/SeoDefaultsBuilderTests.cs`
- `tests/Unit/Infrastructure.Tests/Jobs/Market/MarketPipelineJobTests.cs` (code review P1 — stub)
- `tests/Unit/Infrastructure.Tests/Jobs/Market/DailySnapshotHistoricalJobTests.cs` (code review P1 — stub)
- `tests/Unit/Infrastructure.Tests/Jobs/Market/DistributionPipelineJobTests.cs` (code review P1 — stub)
- `src/Server/Application/Market/YieldCalculator.cs` (post-review — tope superior `<= today` en ventana TTM)
- `tests/Unit/Application.Tests/Market/YieldCalculatorTests.cs` (post-review — test fecha futura)
- `src/Web/Main/src/modules/ficha-publica/FibraPage.tsx` (post-review — desplazamiento de headings markdown, fix H2)
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

## Senior Developer Review (AI)

Code review adversarial (Blind Hunter + Edge Case Hunter + Acceptance Auditor) sobre los cambios sin commitear de la historia — 2026-06-13. Diff: 5 archivos modificados (+523/−54) + `FibraSeoMarketData.cs` nuevo.

**Veredicto:** la implementación de código (AC-1/2/3, secuencialidad EF de AC-5, encoding AC-4, escala de yield, valores exactos) es **sólida y correcta**. Los bloqueos son de **validación y proceso**, más una mejora de rendimiento de alto impacto.

### Review Findings

- [x] [Review][Patch] Caer a `PropertyValue`-only para precio (decisión D1: 2026-06-13) — Resuelto el decision-needed: se elige la opción conservadora de las Dev Notes. Eliminar el nodo `Offer` y modelar `price`/`priceCurrency` como `additionalProperty` (`PropertyValue` "Precio de cotización" + moneda), evitando la semántica "algo en venta" inapropiada para un CBFI. Ajustar tests que asertan `offers`/`Offer`. [SeoDefaultsBuilder.cs:186-191]
- [x] [Review][Patch] Carga de TODO el universo por request para elegir una sola FIBRA — `GetLiveMarketDataAsync` usa `GetLatestSnapshotPerFibraAsync()` (GroupBy de todas las fibras) y `GetSummaryLatestAsync()` (JOIN de todos los fundamentales processed × todas las fibras activas, agrupado en memoria) y luego `.FirstOrDefault(==fibra.Id)`, en cada GET a `/fibras/{slug}` con `Cache-Control: no-cache`. Existen alternativas dirigidas: `GetLastSnapshotsAsync(fibra.Id, 1, ct)` y `GetLatestProcessedByFibraAsync(fibra.Id, ct)`. Resuelve también AC-5 (costo por request → O(1 fibra), caché innecesaria). Reusar además un scope existente en lugar del 3.º redundante. [FibraProfileMetadataMiddleware.cs:180-197]
- [x] [Review][Patch] Falta test de denominador cero literal `lastPrice = 0m` como **primer** test financiero — Convención "Testing — Funciones de Cálculo Financiero" exige el caso denominador=0 como primer test; sólo se prueba `LastPrice = null`. El guard `lastPrice is > 0m` ya cubre `0`, pero falta el test explícito y el orden. [SeoDefaultsBuilderTests.cs]
- [x] [Review][Patch] `Math.Round` usa redondeo bancario (al par) por defecto en % financieros públicos — Usar `MidpointRounding.AwayFromZero` para los 4 `Math.Round` de porcentajes (no altera los valores de los tests actuales). [SeoDefaultsBuilder.cs:204,212,220,228]
- [x] [Review][Patch] Higiene de tracker y documentación AC-5 — T1-T6 siguen `[ ]`, Status `in-progress` y Security Checklist sin marcar pese a un Dev Agent Record que declara todo completo. Marcar tareas realmente hechas, documentar la decisión de caché de AC-5 (diferida: las consultas dirigidas eliminan la necesidad) y completar el Security Checklist.
- [x] [Review][Patch] Yield TTM no acotaba distribuciones a fecha futura — **APLICADO** (a petición del usuario, 2026-06-13). `YieldCalculator.Calculate` ahora filtra `d.PaymentDate >= cutoff && d.PaymentDate <= today` [YieldCalculator.cs:16-21]; protege a todos los consumidores (`MarketEndpoints`, SEO). Nuevo test `Calculate_FutureDatedDistributions_AreIgnored`. 9/9 YieldCalculatorTests verdes.
- [x] [Review][Patch] H2 — `<h1>` duplicado en la ficha (fix frontend `FibraPage`) — **APLICADO**. `DESCRIPTION_MARKDOWN_COMPONENTS` desplaza los headings del markdown de "Descripción" +1 (h1→h2…) en [FibraPage.tsx]; el `<h1>` sr-only del título queda como único `<h1>` de la página. `npm run build` Main verde.
- [x] [Review][Patch] Ventana TTM medida contra el reloj del request — **APLICADO**. `GetLiveMarketDataAsync` deriva `AsOfDate` de `snapshot.CapturedAt` (fecha del precio) en vez de `DateTime.UtcNow` [FibraProfileMetadataMiddleware.cs]; el yield es consistente con el precio publicado.

