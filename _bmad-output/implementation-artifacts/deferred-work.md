# Deferred Work

Items deferred from story reviews. Each entry includes the source story, the finding, and why it was deferred.

---

## Deferred from: code review of 13-6-portafolio-landing-publico (2026-06-16)

- JSON-LD `ItemList` del `CollectionPage` de `/portafolio` apunta a rutas privadas `/reportes` y `/oportunidades` (`SpaMetadataProvider.cs`). Esas rutas no están en el sitemap y el riesgo SEO de empujar crawl es marginal; las tarjetas del landing ya usan `#login`. Considerar apuntar el itemList a anclas públicas o a `#login` si en el futuro importa.
- El JSON-LD `CollectionPage`/`BreadcrumbList` inyectado server-side en el shell de `/portafolio` persiste en el DOM cuando un usuario autenticado ve el dashboard (`PortafolioPage`), porque `usePageTitle` solo gestiona title/description/canonical/og/robots y nunca `<script type="application/ld+json">`. Sin impacto SEO real (los crawlers nunca están autenticados); es inconsistencia de estado en el DOM del usuario logueado.
- Literales de `title`/`description` de `/portafolio` duplicados entre frontend (`PortafolioLanding.tsx` `PAGE_TITLE`/`PAGE_DESCRIPTION`) y backend (`SpaMetadataProvider.cs`). Hoy coinciden carácter a carácter; el riesgo es de drift futuro si se edita uno y no el otro. Patrón pre-existente en todas las páginas públicas (p.ej. `HomePage`), por eso se difiere en lugar de tratarse como bug de 13-6.

---

## Deferred from: code review of 13-5-reportes-trimestrales-privados (2026-06-16)

- Warning de datos obsoletos muerto: `PeriodsAgo` se fija `null` en ambos endpoints (`/latest` y `/report` en `FundamentalsEndpoints.cs`), por lo que `shouldShowFundamentalesWarning` (dispara en `periodsAgo >= 3`) nunca se cumple y el banner "datos podrían estar desactualizados" es inalcanzable en `/reportes` y en la ficha pública. Pre-existente (ya era `null` antes de 13-5). Fix: calcular `PeriodsAgo` real en el endpoint o retirar el banner.
- `fetchFundamentalesAvailablePeriods` (`if (!response.ok) return []`) y `fetchFundamentalesPublic` (`return null`) en `fundamentalesApi.ts` ocultan un error de servidor (500/red) como estado vacío ("Esta FIBRA todavía no tiene reportes trimestrales procesados") en lugar de un estado de error. `/periods` es público (sin dimensión 401). Util compartida con la ficha pública — endurecer (distinguir 404 de error) preservando el comportamiento de la ficha.
- Test `GetPublicLatest_NoLongerIncludesAiFields` no asevera sobre el JSON crudo que las claves IA (`summaryMarkdown`, `riskFlags`, etc.) estén ausentes; la garantía actual es solo el sistema de tipos (el DTO ya no las declara). Reforzar con una aserción negativa sobre el body crudo de la respuesta.

---

## Deferred from: code review of 13-4-tabla-universo-fibras-responsive-movil (2026-06-15)

- `expandedRows` (Set) no se purga al cambiar filtro/orden/período en `FibraUniverseTable` y `FundamentalesPage`: filas que salen del filtro reaparecen ya expandidas al volver, y el Set crece monótonamente (estado fantasma). Introducido por la historia; impacto UX bajo. Fix: limpiar/intersectar `expandedRows` con las filas visibles al cambiar filtro/orden/período.
- `MetricTile` está duplicado en `FibraUniverseTable.tsx` (`value: string` + `valueClassName`) y `FundamentalesPage.tsx` (`value: string | number`) con firmas divergentes → riesgo de drift. Extraer a un componente compartido.
- `SortIcon` usa emojis `⇅ ▲ ▼` y se reusó en los chips de orden móvil NUEVOS de esta historia (AC-6 "nada de emojis"). No se migró a `lucide-react` porque `SortIcon` también alimenta el desktop y AC-3 exige "≥md intacto". Migrar a iconos lucide en un follow-up que toque ambos breakpoints.
- Verificación manual chrome-devtools MCP a 768/1024/1440 no documentada en el Dev Agent Record (AC-8 la exige explícitamente); se usó Playwright a 375px. Documentar la verificación o tratarla como superada por la cobertura e2e ampliada (ver Patch de la review).

---

## Deferred from: code review of 13-3-accesibilidad-formularios-y-contraste (2026-06-15)

- **`name` auto-generado no determinista/colisionable en `Input`/`Textarea`** — cae a `input-<useId>` cuando no hay aria-label/placeholder, y dos inputs con el mismo placeholder resuelven al mismo `name`. Latente: ningún formulario tocado usa `FormData`; PerfilPage usa `autocomplete`. Revisitar si se introduce submission nativo/`FormData`.
- **`WeightSlider` deriva `id`/`name` del label sin `useId` ni sufijo único** (`OportunidadesPage.tsx:93`) — colisión si dos labels normalizan al mismo slug. Hoy los 5 labels son distintos. Alinear con el patrón `useId` del resto.
- **`NumberField` (`HerramientasPage.tsx:657-668`): el `name` no lleva el sufijo único que sí tiene el `id`** — colisión de `name` si dos labels iguales. Hoy distintos.
- **Emoji ⚠️ en `IsrCalculatorWidget.tsx:105`** — viola AC9/MASTER.md (sin emojis como iconos); línea pre-existente no tocada por el diff. Limpiar en una pasada de a11y futura.
- **Evidencia del Dev Agent Record imprecisa (13-3)** — ratios de contraste inflados (reportados vs reales; siguen ≥AA) y auditoría a11y omitió `/` y `/comparar`. Re-ejecutar la auditoría a11y tras aplicar los patches de id/name.

---

## Deferred from: code review of 13-1-reorganizacion-menus-navegacion (2026-06-15)

- Dropdowns desktop de Main con `role="menu"` sin navegación por flechas (patrón APG incompleto). AC2 se cumple literalmente (click/Escape/click-fuera); mejora de a11y, no bloqueante.
- SEO/internal-linking: las 3 rutas públicas movidas al dropdown "Más" (`/conoce-las-fibras`, `/calendario`, `/calculadora`) salen del DOM cuando el dropdown está cerrado (`{open ? … : null}`). Siguen indexables vía sitemap (`SeoEndpoints.cs`) y metadata server-side (`SpaMetadataProvider.cs`), pero pierden el enlace interno site-wide del header que antes existía. Tradeoff de diseño del decluttering del nav.
- Estado `checking`: el drawer móvil de Main muestra "Iniciar sesión" para un usuario autenticado durante la revalidación de sesión (flicker). Patrón de auth pre-existente.
- Drawer de Ops abre vía `onClick` manual (botón fuera de `DialogTrigger`) con retorno de foco manual (`previousOpenRef`); diverge del patrón de Main pero funciona. Consistencia/robustez.

---

## Deferred from: code review of 13-1-reorganizacion-menus-navegacion adenda "revertir Más" (2026-06-15)

- TA4 verificación manual responsive pendiente: la adenda reintroduce 7 enlaces planos + trigger "Mi inversión" en breakpoint `md` (768–1023px) con `gap-2 text-xs` y `whitespace-nowrap`, sin truncado/wrap. Es el escenario de overflow horizontal que motivó la 13.1 original; no cubierto por el test suite (node:test sin DOM). Requiere verificación visual en 375/768/1024/1440. [PublicLayout.tsx:339]
- Tests de navegación validan datos/funciones puras (`MAIN_PRIMARY_LINKS`, `buildMainMobileSections`, `shouldCloseMenuOnEscape`), no render de componentes. La decisión AC14 sigue pendiente; los breakpoints `md`/`lg` donde vive la regresión del buscador no tienen cobertura automatizada. Deuda pre-existente del toolchain, no empeorada por la adenda. [PublicLayout.test.ts]

---

## Deferred from: code review of 12-11-editor-robots-directives-por-pagina (2026-06-15)

- **TOCTOU / last-write-wins en el PUT sin concurrencia optimista** (OpsSeoEndpoints.cs:72-81 + SeoMetadataRepository.cs:54-90) — `GetByIdAsync` (AsNoTracking) seguido de `UpsertAsync(overrideMode:true)` reescribe TODA la fila desde el snapshot leído; dos editores concurrentes producen last-write-wins silencioso sin token de concurrencia. Aceptable en Ops single-writer; el diseño proviene de 12-1. Reconsiderar si se habilita edición multi-usuario.
- **Rutas estáticas sin fila en BD no son editables desde el editor** (SeoPage.tsx / SeoEndpoints.cs) — el editor solo lista filas existentes (`GetAllAsync`); las rutas estáticas servidas por default (sin fila) no aparecen y no se pueden marcar `noindex` desde Ops. Cobertura/scope; depende de que 12-1 siembre filas para esas rutas.
- **Contradicciones semánticas menores no detectadas** (SeoRobotsDirectives.cs) — `nosnippet`+`max-snippet:N` y `noindex`+`max-image-preview` se persisten tal cual sin marcarse como contradicción. No requerido por AC-3 (solo valida index/noindex y follow/nofollow).

---

## Deferred from: code review of 12-9-og-images-dinamicas (2026-06-14)

- **Overflow horizontal en primera línea de `WrapText`** (OgImageRenderer.cs) — una palabra única más ancha que la caja se dibuja sin truncar (solo `lines[^1]` pasa por `TruncateLine`). Borde raro (FullName de un solo token largo). Truncar/clipear todas las líneas como hardening visual.
- **Sin clip vertical en `DrawWrappedText`** (OgImageRenderer.cs) — no respeta `bounds.Bottom`; un título de 2 líneas más alto de lo previsto puede solapar el pill del ticker. Cosmético.
- **Test de integración `GetPngDimensions` sin guard de longitud** (tests/Integration/Api.Tests/OgImageEndpointTests.cs) — con bytes vacíos lanza `ArgumentOutOfRangeException` en vez de aserción limpia. Ligado al patch de PNG vacío; replicar el guard `bytes.Length >= 24` que sí tiene el unit test.
- **`asOfDate` desde `snapshot.CapturedAt` futuro infla yield TTM** (OgImageEndpoints.cs:64 / OgImageRenderer.cs:62) — un CapturedAt futuro ensancha la ventana de `YieldCalculator` (upper bound `<= today` controlado por el dato). Requiere snapshot corrupto/futuro; clamp a `DateTime.UtcNow` como defensa.
- **Tests faltantes: renderer exception→fallback y rendering de acentos es-MX** — AC-5 cubierto en agregado vía integración; sin test automatizado del branch `catch`→fallback ni del rendering de glifos acentuados. Deuda de test (esp. relevante por el riesgo de fuentes en Linux).

---

## Deferred from: code review of 12-4-eeat-autoridad-ymyl (2026-06-14)

- **Email hardcodeado `contacto@fibradis.mx` como fallback + flash en loading** (AcercaPage/ContactoPage/PrivacidadPage) — el spec T5 permite fallback razonable si `ContactEmail` es null; el flash del fallback durante el loading de `useSiteContent` es UX menor. Considerar centralizar el fallback en una constante y un guard `isLoading`.
- **Tres criterios distintos de dedup de URLs `sameAs`** — `Validate` deduplica por `uri.ToString()` (URL normalizada), `Normalize`/provider por string crudo `OrdinalIgnoreCase`. Borde de normalización (`https://x.com` vs `https://x.com/`), admin-only, baja probabilidad. Unificar el criterio.
- **PUT de `sameAs` sin límite de número de URLs ni longitud** (columna `nvarchar(max)`) — AdminOps-only; defensa en profundidad para evitar inyectar una lista enorme en el JSON-LD del home.
- **`twitter:site "@fibradis"` hardcodeado y no verificado** (SpaMetadataMiddleware.cs:167) — pre-existente, fuera del diff; AC-2b se limita a `sameAs`. Deuda YMYL: confirmar/retirar el handle de X junto con las URLs reales de redes sociales pendientes (§Pregunta abierta del story).

---

## Deferred from: code review of 12-10-redirects-administrables (2026-06-13)

- **Anti-loop solo detecta el par directo A↔B** — `HasReverseLoopAsync` (OpsSeoRedirectsEndpoints.cs:322-334) no rechaza cadenas multi-hop (A→B, B→C, C→A) ni un `ToPath` que ya es `FromPath` activo. El middleware limita a 1 salto por diseño (un solo `FirstOrDefault`, sin chain-following) → AC-5 "limita a 1 salto" cumplido, sin loop de servidor. Detección de cadenas en creación es mejora más allá del spec.
- **`Normalize` pasa `ToPath` a minúsculas** — Corrompería destinos case-sensitive (UrlRedirectPath.cs:24-31). Sin impacto hoy: todos los slugs del proyecto son lowercase. Reconsiderar si se introducen destinos sensibles a mayúsculas.
- **`Normalize` no decodifica `%xx` ni colapsa `//`/slash inicial** — Una regla creada con esos casos podría no matchear el path entrante (que Kestrel ya decodificó). Borde de baja probabilidad (admin escribe paths ASCII planos). Canonicalización idéntica write/emit como hardening futuro.
- **`ToPath` puede apuntar a prefijos reservados** (`/api/...`, `/ops/...`) — Solo `FromPath` corre por `IsReservedSource` (OpsSeoRedirectsEndpoints.cs:295). AdminOps es confiable y el spec solo exige no-colisión en `FromPath`; evaluar validar `ToPath` también.
- **`ValidateRequest` sobrescribe mensajes del mismo campo** — `errors[field] = [...]` en vez de acumular (OpsSeoRedirectsEndpoints.cs:267-302); con dos violaciones del mismo campo solo se reporta la última. La validación sigue rechazando correctamente; solo afecta el detalle del mensaje (UX baja).

---

## Deferred from: code review of spec-economatica-discovery-primero (2026-06-13)

- **Contaminación cross-FIBRA vía formas de name-variant** — Al volver `EconomaticaDiscoverySource` universal con formas de fallback, una `NameVariant` normalizada podría coincidir con el código Economatica de OTRA FIBRA, devolviendo PDFs ajenos etiquetados como de esta FIBRA. Amplificado porque Economatica corre PRIMERO + dedup por período suprime a la fuente autoritativa para ese período. Las formas 1-3 (derivadas del ticker propio) NO colisionan; el vector es solo la forma de name-variant (probabilidad baja). **Fix:** filtrar candidatos cuyo filename `{CODE}_RT_...` empiece con el `econTicker` consultado. Requiere fixture HTML para FVIA (los tests actuales reusan el de FHIPO contra una query FVIA, lo que rompería un guard de pertenencia).
- **Amplificación de tráfico HTTP** — La fuente pasó de 19 tickers en whitelist a universal × hasta `3 + NameVariants.Count` formas por FIBRA, casi todas 404 para FIBRAs ausentes de Economatica (timeout 30s c/u, secuencial). Aceptable en un job de fundamentales que corre cada ~36h, pero crece de ~19 a potencialmente cientos de requests/corrida. **Fix:** cache de URLs 404 por corrida, o límite/short-circuit de formas.

---

## Deferred from: code review of 12-2-faqpage-schema-y-qa-administrable (2026-06-13)

- **Normalización de case de `entityKey` inconsistente** — El middleware busca FAQ con el path en minúsculas (`SpaMetadataMiddleware.NormalizePath` → `ToLowerInvariant`) y con el ticker en mayúsculas (`FibraProfileMetadataMiddleware` → `ToUpperInvariant`), pero `FaqRepository.NormalizeEntityKey` y `OpsSeoFaqEndpoints.NormalizeEntityKey` solo hacen `Trim()`+`TrimEnd('/')` (sin case-folding). Los destinos por defecto (`PAGE_TARGETS` + seed) usan el case correcto, así que funciona de fábrica; el riesgo es captura manual con case incorrecto desde Ops → la FAQ se guarda pero ni el acordeón ni el JSON-LD la muestran. Requiere decidir normalización por `PageType` consistente con el módulo SEO de 12-1 (in-progress).
- **Seed sin validación de contenido vacío ni longitud** — `FaqRepository.AddIfMissingAsync` (usado por `POST /seed`) no valida Answer/Question no vacíos ni longitud ≤256 (a diferencia del endpoint Ops). Un `EditorialPage.Content` vacío produciría una FAQ con `text` vacío (FAQPage inválido para Google) y un `Title` >256 chars dispararía `DbUpdateException` que el `catch` cuenta silenciosamente como "skipped". No se dispara con las 5 secciones editoriales reales (tienen contenido), por eso se difiere.
- **`IFaqRepository.DeactivateAsync` es código muerto** — El `DELETE /api/v1/ops/seo/faq/{id}` reimplementa la desactivación inline (`current.IsActive=false; UpdateAsync`) en lugar de llamar `DeactivateAsync`, que solo queda cubierto por un unit test. Consolidar en una sola ruta (el endpoint necesita distinguir 404 de "ya inactiva", lo que `DeactivateAsync` no expone hoy).
- **`FaqAccordion` reabre el primer item tras cierre total** — El `useEffect([items])` (FaqAccordion.tsx:26-33) fuerza `items[0].id` cuando `openId` es `null`; si React Query devuelve una nueva referencia de `items` después de que el usuario cerró el acordeón, este se reabre solo. Trigger poco frecuente (los datos FAQ rara vez cambian en sesión); UX menor.

## From: spec-fix-ficha-precio-tooltip-distribuciones-mejoras (2026-06-12)

### D1 — Float accumulation en sumas de distribuciones mensuales

**Archivo:** `distribuciones.ts` — `groupDistributionsByPeriod`

IEEE-754 produce errores de redondeo al sumar 12 pagos mensuales (ej. 12× $0.15 = $1.8000000000000002). Con `.toFixed(4)` el impacto visual es menor, pero `calcPeriodDiff` puede producir diffs con error en el último decimal.

**Posible fix:** Acumular en enteros (×10000) y dividir al renderizar, o usar Kahan summation.

---

### D2 — IsrCalculatorWidget pre-rellena con pago individual, no con total del periodo

**Archivo:** `FibraPage.tsx` — `<IsrCalculatorWidget lastDistribution={toNum(history.distributions[0]?.amountPerUnit)} />`

El widget usa el pago individual más reciente (`distributions[0]?.amountPerUnit`), pero la tabla agrupada ahora muestra la suma del periodo. Para FIBRAs con múltiples pagos por periodo el valor pre-llenado es inconsistente con el total mostrado en la tabla.

**Posible fix:** Pasar `allGroups[0]?.total` al widget, o añadir nota en la UI explicando que es el último pago individual.

---

## From: code review of 12-1-modulo-seo-administrable (2026-06-13)

- **Slug-change deja huérfano el override SEO** — la clave de News es `slug ?? id`; cambiar el slug de una noticia con override administrado deja la fila huérfana (lookup por slug ≠ clave por id previa). Resolver el esquema de claves estables al implementar auto-llenado/backfill (T6/T7).
- **Sobrecargas muertas `BuildMetaBlock`** — `BuildMetaBlock(Fibra,…)` y `BuildMetaBlock(NewsArticle,…)` quedan sin invocar tras el refactor a `BuildMetaBlock(SeoMetadata)`. Eliminar tras confirmar que ningún test las usa (deuda anti-divergencia: lógica de generación duplicada).
- **Tests de repositorio en InMemory** — no validan índice único, longitudes `nvarchar` ni collation; P4 (detach tras DbUpdateException), P5 (casing) y el truncado de Title quedan sin cobertura. Añadir provider relacional/Testcontainers al implementar persistencia (T6/T7).
- **`TruncateWithEllipsis` sin guard `cut>=1`** — `text[cut-1]` con `maxLength<4` lanzaría IndexOutOfRange. No alcanzable hoy (solo se invoca con 160); añadir guard si se reutiliza.
- **Caracteres de control sin sanitizar** — `Title`/`OgImageUrl`/`CanonicalPath` no neutralizan whitespace de control (`HtmlEncoder` no lo hace). Relevante cuando AC-3 permita edición libre desde Ops.
- **`UpsertAsync` no-override no reactiva `IsActive=false`** — sin ruta de regen que reactive una fila desactivada manualmente. Confirmar si es el comportamiento deseado al implementar el flujo de escritura.

## From: code review of 12-5-jsonld-comparador-fundamentales-breadcrumbs (2026-06-14)

- **Referencia `@id` colgante a `#organization` en `/comparar`** — `WebApplication.provider` apunta a un nodo `Organization` que la página `/comparar` nunca define en su markup (sí lo define `/fundamentales`). Inconsistencia de completitud de structured data; considerar definir el nodo o referenciar uno global resoluble.
- **Ítem hoja del breadcrumb descartado en silencio si `CanonicalPath` vacío** — el filtro de `BuildBreadcrumbListJsonLd` elimina ítems con `Path` vacío, produciendo un breadcrumb sin la página actual. Hoy no disparable (canonical siempre seteado en ficha/noticia); añadir guard/aserción defensiva.
- **Sin try/catch en lecturas de BD del JSON-LD dinámico** — una falla transitoria de BD (`GetAllActiveForSitemapAsync`/`GetSummaryLatestAsync`) devuelve 500 en `/comparar` y `/fundamentales` en vez de servir el shell; el mínimo estático no se usa como fallback. Gap de resiliencia transversal del pipeline SEO (la lectura de `SeoMetadata` ya es sin guard).
- **Sobrecargas muertas `BuildMetaBlock(Fibra,…)` / `BuildMetaBlock(NewsArticle,…)`** — aún cargan `BreadcrumbList` inline sin call sites. Eliminar para evitar doble breadcrumb si un futuro caller las reutiliza (duplica deferral de 12-1, sigue pendiente).

## Deferred from: code review of 12-6-rastreo-indexacion-sitemap-index-llms-txt (2026-06-14)

- **AC-7: validación XSD más débil que el spec** — los tests verifican `XDocument.Parse` + raíz/namespace, no validan contra el XSD oficial de sitemaps.org. XML estructuralmente correcto; reforzar requiere cargar los .xsd y `XmlReaderSettings`.
- **H3: conflicto robots.txt gestionado por Cloudflare** — bloque "Managed robots.txt / Block AI bots" + doble `User-agent: *` no resueltos (config/infra, no código). Registrar política de bots en una sola fuente.
- **`<urlset>` vacío devuelve 200 en /sitemap-static.xml y /sitemap-fibras.xml** — vs 404 en noticias; urlset vacío inválido por XSD, solo alcanzable por URL directa (no referenciado por el índice).
- **`GetNewsPageCountAsync` materializa hasta 45k items solo para contar** — perf menor (cacheado 1h). Añadir ruta count-only.
- **llms.txt: título/descripción sin escapar para Markdown** — `]`/`)` en SeoMetadata (editable Ops) rompería el link. Títulos actuales limpios.
- **`lastmod`=hoy regenerado a diario para rutas estáticas** — señal débil; considerar fecha de build estática.

## Deferred from: code review of 12-7-core-web-vitals (2026-06-14)

- **M3 a11y pre-existente** — 43 fallos de contraste de color + `aria-expanded` inválido en `<tr>` de la tabla de distribuciones de la ficha; los skeletons nuevos heredan los mismos tokens `bg-muted/70` de bajo contraste. Marcado "adyacente, no CWV" en el spec; subir tokens `muted-foreground` a ≥4.5:1 y mover el affordance de fila expandible a `<button>`.
- **Test `cwv-loading.test.ts` tautológico** — re-afirma las constantes de `cwv-layout.ts` pero no ejercita los skeletons ni la geometría real; no protege contra el drift de layout (CLS) que motiva la historia. Falta un test que valide que los skeletons consumen las constantes y/o que la altura reservada ≈ contenido cargado.
- **`availablePeriods.length===0` dispara la query de fundamentales con `activePeriod=undefined`** — request desperdiciada y contrato implícito (gate `enabled: !!ticker && periodsFetched`). Pre-existente al cambio 12-7. Considerar gate adicional `availablePeriods.length > 0`.
- **AC-6 guardrail no implementado + AC-5 no verificado en navegador real** — sin Lighthouse CI ni check documentado en `convenciones-fibradis.md`; la verificación post-fix fue un probe mockeado local, no las 7 rutas reales con throttling móvil. Pendiente como guardrail opcional + smoke-check manual de no-regresión SSR/SEO.

## Deferred from: code review of 12-8-enlazado-interno (2026-06-14)

- **Links de "FIBRAs relacionadas" ausentes del HTML prerenderizado/SSR** — la query client-side de `FibrasRelacionadasSection` no tiene prefetch/dehydration, así que el HTML estático no incluye los enlaces internos por sector; crawlers sin ejecución de JS no los ven. Consistente con las demás secciones client-fetch (Noticias/Distribuciones) y son `<a>` reales que el crawler con JS de Google sí sigue. Revisitar si GSC muestra que no se rastrean (requeriría wiring de SSR prefetch, fuera del alcance de 12-8).
- **`/related` con ticker benchmark devuelve `200 []` en vez de 404** — `GetByTickerAsync` no filtra `State`, así que resuelve filas Inactive (`^MXX`/`^GSPC`, sector "Índice") y devuelve array vacío. Rasgo pre-existente compartido con `/{ticker}`. Benigno; considerar filtrar State/benchmarks si llega a importar.
- **`RelatedFibra.ShortName` over-fetch** — el campo se transporta y tipa pero el componente solo renderiza `fullName`. Eliminar (requiere re-codegen) o usar como label compacto en la tarjeta en una iteración futura.

## Deferred from: code review of 12-1-modulo-seo-administrable T5–T10 (2026-06-15)

- **Cambio de slug de noticia deja huérfano un override SEO** — `UpdateSlugAsync` (ruta de backfill de slugs legacy) no migra/regenera la fila `SeoMetadata` existente (clave por slug). Casi inalcanzable hoy: el slug se fija con `??=` al crear, y los artículos legacy sin slug no tienen fila SEO previa por clave-slug. Migración de clave estable es compleja; revisitar si aparecen overrides huérfanos.
- **Auto-llenado/regen SEO fuera de la transacción del contenido** — `PopulateSeoAsync`/`RegenerateSeoAsync` corren tras el `SaveChangesAsync` del artículo/fibra (best-effort con warning), no en la misma transacción (T6 pedía "cuando sea posible"). El backfill idempotente recupera filas faltantes, pero la regeneración de filas existentes desactualizadas no tiene red equivalente. Aceptable; revisitar si se observan inconsistencias en prod.
- **Sin test del regen tras update IA de noticias** — `UpdateSummaryAsync`/`UpdateAiAnalysisAsync` usan `ExecuteUpdateAsync`, no soportado por el provider InMemory de los tests (unit e integration). La mecánica regen-respeta-override queda cubierta por el test de `UpdateAsync` de fibra. Cubrir con provider relacional/Testcontainers en el futuro.
- **`GetMetaForPathAsync` baja a minúsculas pero `NormalizeEntityKey` no** — sin desajuste hoy (todas las `KnownPaths` en minúsculas); normalizar/documentar al añadir rutas fijas con mayúsculas.
- **Backfill SEO no atómico** — sin transacción global; mitigado por try/catch por-ítem (idempotente y reanudable). Un fallo parcial no corrompe datos pero el conteo devuelto puede ser parcial.

## Deferred from: code review of 13-7-rebranding-fibras-inmobiliarias-y-contacto (2026-06-16)

- **Migración sobrescribe ContactEmail editado en Ops** — `20260616171543_RebrandContactEmail.cs` hace `UpdateData(id=1, contact_email=gmail)` sin condición. Si un operador ya hubiera personalizado el correo desde Ops, la migración lo pisa al aplicarse. Riesgo bajo pre-lanzamiento; inherente a los seeds `HasData`. Revisitar si el correo se personaliza antes de aplicar la migración en prod.
- **Fallback de `mailto` en footer usa `??` sin `.trim()`** — `PublicLayout.tsx:445` usa `siteContent?.contactEmail ?? 'portafoliodefibras@gmail.com'`; un `contactEmail` = "" (cadena vacía) produciría `mailto:` vacío. Acerca/Contacto/Privacidad usan el patrón más robusto `?.trim() || …`. Pre-existente; este story solo cambió el literal del fallback. Unificar al patrón `?.trim() ||` cuando se toque el footer.

## Deferred from: code review of 13-8-landing-plataforma-publica (2026-06-16)

- **Conteos hardcodeados en `/plataforma`** — `BuildPlataformaJsonLd` declara `numberOfItems = 8` como literal independiente del arreglo de `ListItem`, y `PlataformaPage.tsx` usa `StatTile value="8"`/`"5"` en vez de `PUBLIC_FEATURES.length`/`PRIVATE_FEATURES.length`. Hoy coinciden; riesgo de desincronización silenciosa al editar los arreglos. Polish LOW; derivar de la longitud cuando se toque el archivo.
- **T5 — verificación manual a11y/responsive/SEO de `/plataforma` pendiente** — gating antes de `done` por el checklist SSR/SEO de convenciones: confirmar sin scroll horizontal en 375/768/1024/1440px, contraste AA, y curl al shell para validar `<title>`/canonical/JSON-LD/breadcrumb server-side + presencia en `sitemap.xml`/`sitemap-static.xml`. No lo cubre el test suite.
