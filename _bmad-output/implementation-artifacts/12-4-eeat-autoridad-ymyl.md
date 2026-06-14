# Story 12.4: Señales E-E-A-T / autoridad para dominio YMYL

Status: done

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

- [x] **T1 — Eliminar dominio hardcodeado en JSON-LD (AC-1)**: parametrizar los constants de `SpaMetadataProvider` con `App:BaseUrl` (o, si 12-1 ya migró a BD, ajustar el seed y el builder para usar `App:BaseUrl`). Test que falla si aparece un dominio literal.
- [x] **T2 — Organization enriquecida (AC-2)**: actualizar `HomepageJsonLd` (`email` desde `ContactEmail`, `knowsAbout`). Si requiere dato dinámico (email), componer en el builder/middleware (no constant puro).
- [x] **T2b — `sameAs` administrable (AC-2b)**: agregar `sameAs` como campo de la Organization editable desde el módulo SEO de Ops (12-1) — p.ej. fila `SeoMetadata` de `Home`/`Organization` con un campo lista o un sub-recurso `/api/v1/ops/seo/organization`. Eliminar los `sameAs` hardcodeados no verificados. Emitir `sameAs` solo si hay valores; cada URL validada http/https. Poblar con las URLs reales que confirme el usuario (YouTube/Instagram/FB/Wikidata + X/LinkedIn si aplican).
- [x] **T3 — Article con autoría + dateModified (AC-3)**: enriquecer `ConoceLasFibrasJsonLd` con `author`/`publisher`/`dateModified`. Como `UpdatedAt` es por sección, decidir: usar el `MAX(UpdatedAt)` de las 5 secciones (resolver vía `IEditorialPageRepository` en scope) o dateModified por tab. Documentar.
- [x] **T4 — dateModified en páginas de datos (AC-4)**: para fundamentales, agregar `dateModified` = `FundamentalesPublicDto.CapturedAt`; coordinar con 12-3 para fibra. Mostrar "Datos al {fecha}" visible donde falte (reusar `freshness-badge`/`CapturedAt`).
- [x] **T5 — Sincronizar contacto (AC-5)**: `AcercaPage.tsx` y `ContactoPage.tsx` consumen `useSiteContent()` ([useSiteContent.ts](src/Web/Main/src/shared/hooks/useSiteContent.ts)) para el email; quitar hardcode. Fallback razonable si `ContactEmail` null.
- [x] **T6 — AboutPage/ContactPage schema (AC-6)**: seed/builder de JSON-LD para `/acerca` (`AboutPage`) y `/contacto` (`ContactPage` + `ContactPoint`). Vía mecanismo DB-driven de 12-1.
- [x] **T7 — Tests + validación (AC-7)**: unit (no-dominio-hardcodeado, email/dateModified poblados), validación manual Rich Results, `npm run build`.

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

## Senior Developer Review (AI)

### Review Findings — code review 2026-06-14 (Opus 4.8)

Tres capas adversariales (Blind Hunter, Edge Case Hunter, Acceptance Auditor). Los 7 ACs se verificaron CUMPLIDOS contra el código; reglas no negociables del middleware SEO respetadas. 4 patches, 4 defers, 8 dismissed.

**Patches (resueltos en review 2026-06-14):**

- [x] [Review][Patch] El test de AC-1/T1 no detecta una regresión a dominio hardcodeado [tests/Unit/Infrastructure.Tests/Seo/SpaMetadataProviderTests.cs:110,154,250] — `BuildConfig()` fija `App:BaseUrl = "https://fibrasinmobiliarias.com"`, idéntico al dominio que antes estaba hardcodeado. Los asserts `Contains("https://fibrasinmobiliarias.com")` pasarían incluso si el código volviera a hardcodear el dominio. T1 exige explícitamente "test que falla si aparece un dominio literal". Fix: usar un BaseUrl distinto (p.ej. `https://test.fibradis.example`), assertar `Contains` de ese dominio y `DoesNotContain("fibrasinmobiliarias.com")`.
- [x] [Review][Patch] `NullReferenceException` por elemento `null` en el array de `sameAs` [src/Server/Api/Seo/SpaMetadataProvider.cs:362-368; src/Server/Api/Endpoints/Ops/OpsSeoOrganizationEndpoints.cs:81,102,116] — `JsonSerializer.Deserialize<string[]>` sobre `["https://x.com", null]` produce un elemento `null`; el subsiguiente `.Select(url => url.Trim())` lanza NRE, que el `catch (JsonException)` NO captura. En el endpoint, `Validate` (`urls[index].Trim()`) crashea con 500 ante un PUT `{"sameAs":[null]}` en vez de 400. Fix: filtrar `null` (`.Where(u => u is not null)`) antes de `.Trim()` en los 3 puntos (provider `GetOrganizationSameAsAsync`, endpoint `Validate` y `Normalize`/`ParseSameAs`).
- [x] [Review][Patch] Doble lectura de `OperationalConfig` en la home (ruta caliente, sin caché) [src/Server/Api/Seo/SpaMetadataProvider.cs:340-374,126-129] — `BuildHomepageJsonLdAsync` llama `GetContactEmailAsync` y `GetOrganizationSameAsAsync`, cada uno abre su propio `IServiceScope` y ejecuta `repo.GetAsync` sobre la MISMA fila → 2 round-trips redundantes en cada request de `/`. La convención del proyecto (12-3) ya exige reuso de scope. Fix: leer `OperationalConfig` una sola vez y derivar email + sameAs.
- [x] [Review][Patch] `getLatestCapturedAt`/`FundamentalesSection` sin guard de fecha inválida [src/Web/Main/src/modules/ficha-publica/sections/fundamentales.ts:32-39; FundamentalesSection.tsx:44] — la firma declara `capturedAt: string` pero la fuente `FundamentalesData.capturedAt` es `string | null`; si una fila llega con `capturedAt` nulo/inválido, `new Date(undefined)` produce `Invalid Date` que "se pega" como máximo y se renderiza "Invalid Date"/"1 ene 1970" al usuario. Severidad baja (CapturedAt es no-nulo en origen), fix defensivo: filtrar falsy + guard `isNaN(date.getTime())`.

**Defers (deuda documentada, no bloqueante):**

- [x] [Review][Defer] El email hardcodeado `contacto@fibradis.mx` permanece como fallback y hay flash durante loading [AcercaPage.tsx, ContactoPage.tsx, PrivacidadPage.tsx] — deferred: el spec T5 permite "fallback razonable si ContactEmail null"; pre-existente/aceptado.
- [x] [Review][Defer] Tres criterios distintos de dedup de URLs (Validate por `uri.ToString()` normalizado vs Normalize/provider por string crudo `OrdinalIgnoreCase`) [OpsSeoOrganizationEndpoints.cs:93,104; SpaMetadataProvider.cs:367] — deferred: borde de normalización de URL, admin-only, baja probabilidad.
- [x] [Review][Defer] El PUT no limita número de URLs ni longitud (columna `nvarchar(max)`) [OpsSeoOrganizationEndpoints.cs:33-68] — deferred: AdminOps-only, defensa en profundidad.
- [x] [Review][Defer] `twitter:site "@fibradis"` hardcodeado y no verificado [SpaMetadataMiddleware.cs:167] — deferred: pre-existente (fuera del diff), AC-2b se limita literalmente a `sameAs`; deuda YMYL a confirmar junto con las URLs de redes.

**Dismissed (8):** (1) "encoder JSON-LD no escapa `<>&`" — FALSO POSITIVO verificado empíricamente: `JavaScriptEncoder.Create(UnicodeRanges.All)` sí emite `<`/`>`, con test existente `EncodesHtmlInTitleAndDescription_AndEscapesJsonLd`. (2) `dateModified` con offset no-UTC — ISO 8601 con offset es válido en schema.org. (3) `GetActor`+`Decrypt` sobre claim no-email — patrón establecido idéntico a `OpsConfigEndpoints.cs:62`, `Decrypt` tiene fallback. (4) `BaseUrl="/"` → URLs relativas — misconfig implausible; fail-fast cubre null/empty. (5) `path` null en `NormalizePath` — el caller garantiza no-null (`?? "/"`). (6) migración `UpdateData value:null` — no-op inofensivo, columna nullable correcta, snapshot coherente. (7) persiste `"[]"` vs `null` ante input en blanco — funcionalmente equivalente. (8) `knowsAbout`/`foundingDate`/`author` hardcodeados — son hechos reales de la organización; AC-2 lista esos valores explícitamente.

## Dev Agent Record
### Agent Model Used
GPT-5 Codex
### Debug Log References
- `dotnet test tests/Unit/Infrastructure.Tests/Infrastructure.Tests.csproj --filter "FullyQualifiedName~SpaMetadataProviderTests|FullyQualifiedName~SpaMetadataMiddlewareTests|FullyQualifiedName~OperationalConfigRepositoryTests|FullyQualifiedName~BanxicoSyncJobTests"`
- `npm run build --workspace=src/Web/Ops`
- `dotnet test tests/Unit/Infrastructure.Tests/Infrastructure.Tests.csproj`
- `npm test --workspace=src/Web/Main`
- `npm run build --workspace=src/Web/Main`
- `dotnet ef migrations add AddOrganizationSameAsJson --project src/Server/Infrastructure --startup-project src/Server/Api --context AppDbContext`
### Completion Notes List
- Reemplacé el JSON-LD hardcodeado por generación dinámica con `App:BaseUrl` y datos reales de `OperationalConfig`, `EditorialPage` y `FundamentalRecord`.
- Eliminé `sameAs` hardcodeado y lo moví a un flujo administrable en Ops con persistencia en `OperationalConfig`, endpoint dedicado y página de edición.
- Añadí `dateModified` visible en fundamentales y la sincronización del email público de contacto en `/acerca`, `/contacto` y `/privacidad`.
- Añadí pruebas de provider, middleware y repositorio para cubrir `sameAs`, autoría, `dateModified` y auditoría.
- Generé la migración EF `AddOrganizationSameAsJson` para la nueva columna de configuración.
### File List
- `_bmad-output/implementation-artifacts/12-4-eeat-autoridad-ymyl.md`
- `scripts/codegen/Api.json`
- `src/Server/Api/Endpoints/Ops/OpsSeoOrganizationEndpoints.cs`
- `src/Server/Api/Middleware/SpaMetadataMiddleware.cs`
- `src/Server/Api/Program.cs`
- `src/Server/Api/Seo/ISpaMetadataProvider.cs`
- `src/Server/Api/Seo/SpaMetadataProvider.cs`
- `src/Server/Application/Ops/IOperationalConfigRepository.cs`
- `src/Server/Domain/Ops/OperationalConfig.cs`
- `src/Server/Infrastructure/Migrations/SqlServer/20260614061543_AddOrganizationSameAsJson.Designer.cs`
- `src/Server/Infrastructure/Migrations/SqlServer/20260614061543_AddOrganizationSameAsJson.cs`
- `src/Server/Infrastructure/Migrations/SqlServer/AppDbContextModelSnapshot.cs`
- `src/Server/Infrastructure/Persistence/Repositories/Ops/OperationalConfigRepository.cs`
- `src/Server/Infrastructure/Persistence/SqlServer/Configurations/Ops/OperationalConfigConfiguration.cs`
- `src/Web/Main/src/modules/acerca/AcercaPage.tsx`
- `src/Web/Main/src/modules/contacto/ContactoPage.tsx`
- `src/Web/Main/src/modules/ficha-publica/FibraPage.tsx`
- `src/Web/Main/src/modules/ficha-publica/sections/FundamentalesSection.tsx`
- `src/Web/Main/src/modules/ficha-publica/sections/fundamentales.test.ts`
- `src/Web/Main/src/modules/ficha-publica/sections/fundamentales.ts`
- `src/Web/Main/src/modules/fundamentales/FundamentalesPage.tsx`
- `src/Web/Main/src/modules/privacidad/PrivacidadPage.tsx`
- `src/Web/Ops/src/api/seoOrganizationApi.ts`
- `src/Web/Ops/src/components/OpsShell.tsx`
- `src/Web/Ops/src/main.tsx`
- `src/Web/Ops/src/pages/SeoOrganizationPage.tsx`
- `tests/Unit/Infrastructure.Tests/Jobs/BanxicoSyncJobTests.cs`
- `tests/Unit/Infrastructure.Tests/Middleware/SpaMetadataMiddlewareTests.cs`
- `tests/Unit/Infrastructure.Tests/Persistence/Repositories/OperationalConfigRepositoryTests.cs`
- `tests/Unit/Infrastructure.Tests/Seo/SpaMetadataProviderTests.cs`
### Change Log
- 2026-06-14: Implemented E-E-A-T/YMYL SEO hardening for story 12-4, including dynamic JSON-LD, Ops-managed `sameAs`, contact sync, fundamentals freshness labels, tests, and EF migration.
- 2026-06-14: Code review (Opus 4.8) — 4 patches aplicados: P1 test AC-1/T1 con BaseUrl distinto + DoesNotContain dominio viejo, P2 guard de elementos `null` en `sameAs` (provider + endpoint Validate/Normalize/ParseSameAs) + test de regresión, P3 lectura única de `OperationalConfig` en la home (helper `GetOrganizationContactDataAsync` + `ParseSameAs` estático), P4 guard de fecha inválida en `getLatestCapturedAt` y `FundamentalesSection`. 4 defers en deferred-work.md, 8 dismissed. 41/41 provider tests + 75/75 unit afectados + 162/162 frontend + Main build verdes. Status → done.
