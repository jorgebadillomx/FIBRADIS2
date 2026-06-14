# Deferred Work

Items deferred from story reviews. Each entry includes the source story, the finding, and why it was deferred.

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
