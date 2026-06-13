# Story 12.4: Señales E-E-A-T / autoridad para dominio YMYL

Status: ready-for-dev

<!-- Parcialmente dependiente de 12-1 (consistencia de dominio en JSON-LD, dateModified). Tiene trabajo standalone. -->

## Story

As an **operador de FIBRADIS en un dominio financiero (YMYL)**,
I want **fortalecer las señales E-E-A-T del sitio: autoría/editorial, fechas de actualización visibles y estructuradas, Organization enriquecida y consistencia de marca/dominio/contacto**,
so that **Google confíe en FIBRADIS como fuente experta de información financiera (donde el estándar de confianza es máximo) y mejore ranking + citabilidad**.

## Dependencias y contexto
- **Finanzas = YMYL** ("Your Money Your Life"): Google exige el máximo nivel de Experience, Expertise, Authoritativeness, Trust. Esta historia ataca señales de confianza transversales.
- **Estado actual (hallazgos):**
  - `/acerca`, `/contacto`, `/privacidad` son **JSX estático** ([AcercaPage.tsx](src/Web/Main/src/modules/acerca/AcercaPage.tsx) ya tiene metodología rica: misión "independiente", fuentes BMV/CNBV, tabla de fórmulas, disclaimer YMYL; [PrivacidadPage.tsx](src/Web/Main/src/modules/privacidad/PrivacidadPage.tsx) LFPDPPP).
  - **Inconsistencias a corregir:**
    - El JSON-LD de [SpaMetadataProvider.cs](src/Server/Api/Seo/SpaMetadataProvider.cs) **hardcodea el dominio `https://fibrasinmobiliarias.com`** y `logo.png`, mientras canonical/og usan `App:BaseUrl`. Divergencia de dominio en señales de marca.
    - Email de contacto **duplicado y no sincronizado**: `contacto@fibradis.mx` hardcodeado en `AcercaPage`/`ContactoPage`, vs `OperationalConfig.ContactEmail` (DB, editable, expuesto en `GET /api/v1/site-content`).
    - `sameAs` usa `twitter.com`/`linkedin.com` (legacy) — verificar que sean reales antes de declararlos.
    - El `Article` de conoce-las-fibras es **estático sin `dateModified`/`author`**; `EditorialPage.UpdatedAt` está disponible pero no se usa.
  - **`dateModified` disponible server-side**: `MarketSnapshotDto.CapturedAt` (precio), `FundamentalesPublicDto.CapturedAt` (fundamentales), `EditorialPage.UpdatedAt` (educativo) — via [FreshnessClassifier.cs](src/Server/Application/Market/FreshnessClassifier.cs) y DTOs ya expuestos.

## Acceptance Criteria

**AC-1 — Dominio consistente en JSON-LD.** Los constants JSON-LD de `SpaMetadataProvider` (`HomepageJsonLd`, `ConoceLasFibrasJsonLd`, `CalculadoraJsonLd`) dejan de hardcodear `fibrasinmobiliarias.com`/`logo.png` y usan `App:BaseUrl`. (Si 12-1 ya movió estos a BD, el seed usa `App:BaseUrl`; si no, se parametriza en el provider). Cero dominios hardcodeados en señales de marca.

**AC-2 — Organization enriquecida.** El nodo `Organization` del home incluye: `email` (= `OperationalConfig.ContactEmail`, no hardcodeado), `knowsAbout` (FIBRAs, REITs México, inversión inmobiliaria), y `description`/`areaServed`/`foundingDate` actuales. Marca consistente ("FIBRADIS").

**AC-2b — `sameAs` administrable desde Ops, solo perfiles verificados.** Decisión del usuario:
- Los `sameAs` hardcodeados actuales (`https://twitter.com/fibradis`, `https://linkedin.com/company/fibradis`) **se eliminan** salvo que se confirmen como reales — son señal falsa en YMYL.
- `sameAs` se vuelve un **campo editable de la Organization desde el módulo SEO de Ops (12-1)** — no hardcodeado. Abrir/cerrar una red no requiere deploy.
- Se pueblan **solo perfiles oficiales verificados administrados por FIBRADIS** (el usuario indicó que los reales están en otras plataformas: YouTube/Instagram/Facebook/Wikidata). **URLs exactas pendientes de confirmar — ver §Pregunta abierta.**
- Si no hay perfiles confirmados al implementar → `sameAs` se omite (no se emite vacío ni con valores no verificados).

**AC-3 — Autoría + fechas en contenido editorial.** El `Article` JSON-LD de `/conoce-las-fibras` incluye `author`/`publisher` (Organization FIBRADIS) y `dateModified` = `EditorialPage.UpdatedAt`. La página muestra "Actualizado: {fecha}" visible (ya existe el dato en `ConoceLasFibrasPage`).

**AC-4 — "Última actualización" en páginas de datos.** Las páginas de datos (ficha de fibra, fundamentales) exponen `dateModified` en su JSON-LD desde `CapturedAt` (coordinado con 12-3 para fibra) y muestran "Datos al {fecha}" visible donde aún no esté. Señal de frescura/confianza.

**AC-5 — Contacto sincronizado.** El email de contacto en `/acerca` y `/contacto` se lee de `OperationalConfig.ContactEmail` (vía `useSiteContent` / `GET /api/v1/site-content`), eliminando el hardcode `contacto@fibradis.mx`. Una sola fuente de verdad.

**AC-6 — AboutPage/ContactPage schema.** `/acerca` emite `AboutPage` JSON-LD y `/contacto` `ContactPage` (con `ContactPoint`/email), reforzando la estructura de confianza. (Vía el mecanismo DB-driven de 12-1: seed con estos JSON-LD.)

**AC-7 — Tests + verificación.** Tests de que el JSON-LD usa `App:BaseUrl` (no dominio hardcodeado) y que `dateModified`/`email` se pueblan. Verificación manual en Rich Results Test. `npm run build` 0 errores. Verdes antes de `done`.

## Tasks / Subtasks

- [ ] **T1 — Eliminar dominio hardcodeado en JSON-LD (AC-1)**: parametrizar los constants de `SpaMetadataProvider` con `App:BaseUrl` (o, si 12-1 ya migró a BD, ajustar el seed y el builder para usar `App:BaseUrl`). Test que falla si aparece un dominio literal.
- [ ] **T2 — Organization enriquecida (AC-2)**: actualizar `HomepageJsonLd` (`email` desde `ContactEmail`, `knowsAbout`). Si requiere dato dinámico (email), componer en el builder/middleware (no constant puro).
- [ ] **T2b — `sameAs` administrable (AC-2b)**: agregar `sameAs` como campo de la Organization editable desde el módulo SEO de Ops (12-1) — p.ej. fila `SeoMetadata` de `Home`/`Organization` con un campo lista o un sub-recurso `/api/v1/ops/seo/organization`. Eliminar los `sameAs` hardcodeados no verificados. Emitir `sameAs` solo si hay valores; cada URL validada http/https. Poblar con las URLs reales que confirme el usuario (YouTube/Instagram/FB/Wikidata + X/LinkedIn si aplican).
- [ ] **T3 — Article con autoría + dateModified (AC-3)**: enriquecer `ConoceLasFibrasJsonLd` con `author`/`publisher`/`dateModified`. Como `UpdatedAt` es por sección, decidir: usar el `MAX(UpdatedAt)` de las 5 secciones (resolver vía `IEditorialPageRepository` en scope) o dateModified por tab. Documentar.
- [ ] **T4 — dateModified en páginas de datos (AC-4)**: para fundamentales, agregar `dateModified` = `FundamentalesPublicDto.CapturedAt`; coordinar con 12-3 para fibra. Mostrar "Datos al {fecha}" visible donde falte (reusar `freshness-badge`/`CapturedAt`).
- [ ] **T5 — Sincronizar contacto (AC-5)**: `AcercaPage.tsx` y `ContactoPage.tsx` consumen `useSiteContent()` ([useSiteContent.ts](src/Web/Main/src/shared/hooks/useSiteContent.ts)) para el email; quitar hardcode. Fallback razonable si `ContactEmail` null.
- [ ] **T6 — AboutPage/ContactPage schema (AC-6)**: seed/builder de JSON-LD para `/acerca` (`AboutPage`) y `/contacto` (`ContactPage` + `ContactPoint`). Vía mecanismo DB-driven de 12-1.
- [ ] **T7 — Tests + validación (AC-7)**: unit (no-dominio-hardcodeado, email/dateModified poblados), validación manual Rich Results, `npm run build`.

## Dev Notes
- **Stack real = SQL Server**. Reusa el módulo SEO de 12-1 para los JSON-LD administrables (AboutPage/ContactPage/Organization). Donde el dato es dinámico (email, dateModified), componer en builder/middleware, no en constant.
- **No duplicar fuentes de verdad**: `ContactEmail` vive en `OperationalConfig` (DB). Todo lo demás debe apuntar ahí.
- **`sameAs` administrable + solo verificado (decisión tomada)**: ya no se hardcodea; se administra desde Ops (módulo SEO 12-1). Un `sameAs` a una cuenta inexistente es señal negativa en YMYL → emitir solo perfiles oficiales reales.
  - **§Pregunta abierta — URLs pendientes:** el usuario indicó que los perfiles reales están en YouTube/Instagram/Facebook/Wikidata (no confirmó X/LinkedIn). **Falta que el usuario entregue las URLs/handles exactos** antes de poblar `sameAs`. Hasta entonces, el dev deja `sameAs` vacío/omitido y los `twitter.com`/`linkedin.com` actuales se eliminan.
- **No inventar credenciales/autores falsos**: E-E-A-T real, no simulado. El `author` es la Organization FIBRADIS (no una persona ficticia) salvo que exista un autor real identificable.
- **Coordinación con 12-3**: `dateModified` de la ficha de fibra lo implementa 12-3; aquí se asegura para fundamentales y editorial. Evitar doble implementación — si 12-3 va primero, esta historia solo cubre fundamentales/editorial/acerca.
- **Reglas middleware de 12-1 intactas**.

### Security Checklist — antes del primer commit
- [ ] **TOCTOU**: N/A (lectura / config existente).
- [ ] **Auth-gating UI**: N/A (rutas públicas); edición de `ContactEmail` ya es AdminOps en ConfigPage.
- [ ] **Exposición de datos**: `ContactEmail` es público por diseño (`GET /api/v1/site-content`). No exponer otros campos de `OperationalConfig`.
- [ ] **Denominador cero**: N/A.

### References
- [SpaMetadataProvider.cs](src/Server/Api/Seo/SpaMetadataProvider.cs) (JSON-LD con dominio hardcodeado a corregir; `sameAs` líneas 23-26)
- [AcercaPage.tsx](src/Web/Main/src/modules/acerca/AcercaPage.tsx), [ContactoPage.tsx](src/Web/Main/src/modules/contacto/ContactoPage.tsx), [PrivacidadPage.tsx](src/Web/Main/src/modules/privacidad/PrivacidadPage.tsx)
- [OperationalConfig.cs](src/Server/Domain/Ops/OperationalConfig.cs) (ContactEmail/TermsText), [OpsConfigEndpoints.cs:18-30](src/Server/Api/Endpoints/Ops/OpsConfigEndpoints.cs) (`GET /api/v1/site-content`), [useSiteContent.ts](src/Web/Main/src/shared/hooks/useSiteContent.ts)
- [FreshnessClassifier.cs](src/Server/Application/Market/FreshnessClassifier.cs), [MarketSnapshotDto.cs](src/Server/SharedApiContracts/Market/MarketSnapshotDto.cs) (CapturedAt), [FundamentalesPublicDto.cs](src/Server/SharedApiContracts/Fundamentals/FundamentalesPublicDto.cs) (CapturedAt)
- [EditorialPage.cs](src/Server/Domain/Ops/EditorialPage.cs) (UpdatedAt)
- Story 12-1: [12-1-modulo-seo-administrable.md](_bmad-output/implementation-artifacts/12-1-modulo-seo-administrable.md); Story 12-3: [12-3-datos-financieros-estructurados-ficha.md](_bmad-output/implementation-artifacts/12-3-datos-financieros-estructurados-ficha.md)
- 2026: [SEO Best Practices 2026 — ALM](https://almcorp.com/blog/seo-best-practices-complete-guide-2026/) · [GEO 2026 — Enrich Labs](https://www.enrichlabs.ai/blog/generative-engine-optimization-geo-complete-guide-2026)

## Hallazgos de auditoría SEO (2026-06-13)

> Auditoría completa (score 84/100): [seo-audit/FULL-AUDIT-REPORT.md](../../seo-audit/FULL-AUDIT-REPORT.md). La auditoría **confirma** que el área E-E-A-T del sitio es fuerte (disclaimer YMYL claro, metodología en `/acerca`, fundamentales con citas de fuente por página). Refuerzos detectados:

### 🟡 M5 — Falta atribución de autoría/experiencia visible
La ficha de fibra incluye un bloque **"PERSPECTIVA DEL ANALISTA"** y un "RESUMEN ANALÍTICO" (contenido de análisis financiero generado), pero **no hay autor/analista atribuido** en ninguna página. En YMYL, la expertise nombrada es señal de confianza de primer orden.
- **Refuerzo a AC-3:** además de `author`/`publisher` = Organization FIBRADIS, considerar una **autoría editorial/metodológica nombrada** (p.ej. "Análisis del equipo FIBRADIS", o una página de metodología con autoría) y reflejarla en el schema donde aplique. No inventar personas ficticias (regla ya presente en §Dev Notes) — pero si existe un equipo/analista real identificable, nombrarlo fortalece E-E-A-T del contenido analítico.
- El bloque "PERSPECTIVA DEL ANALISTA" en la ficha es buen candidato para mostrar "Análisis al {dateModified}" + atribución, reforzando frescura + autoría a la vez (coordina con AC-4 y 12-3).

## Dev Agent Record
### Agent Model Used
### Debug Log References
### Completion Notes List
### File List
