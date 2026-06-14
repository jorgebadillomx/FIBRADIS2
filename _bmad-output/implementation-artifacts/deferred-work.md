# Deferred Work

Items deferred from story reviews. Each entry includes the source story, the finding, and why it was deferred.

---

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
