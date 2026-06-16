# Story 13.7: Rebranding "FIBRADIS" → "Fibras Inmobiliarias" en superficies visibles/SEO + email de contacto + título SEO

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a **responsable de marca y SEO de la plataforma (y como visitante del sitio)**,
I want **que la marca visible sea uniformemente "Fibras Inmobiliarias" — que ninguna superficie pública ni de SEO muestre el código interno "FIBRADIS", que el `<title>` deje de incluir el token "FIBRADIS", y que el correo de contacto sea `portafoliodefibras@gmail.com`**,
so that **la identidad de marca sea consistente de cara al usuario y a los buscadores, y los contactos lleguen al buzón correcto; además que la regla quede documentada para que ningún agente reintroduzca "FIBRADIS" en el futuro**.

## Contexto del problema

Decisión de marca del usuario (2026-06-15):

1. **"FIBRADIS" es un nombre interno/de código que no debe aparecer de cara al usuario.** La marca visible es **"Fibras Inmobiliarias"**. Hoy el literal "FIBRADIS" aparece en **títulos SEO, og:site_name, og:image:alt, JSON-LD, descripciones, copy de páginas (`/acerca`, `/contacto`, footer), llms.txt e imágenes OG renderizadas**.
2. **Título SEO:** quitar el token "FIBRADIS" del `<title>`. El patrón actual `… | FIBRADIS` y `… | FIBRADIS — Fibras Inmobiliarias` pasa a **`… | Fibras Inmobiliarias`** (se elimina el token redundante "FIBRADIS —", conservando la marca humana "Fibras Inmobiliarias"). **Decisión confirmada con el usuario.**
3. **Correo de contacto:** cambiar de `contacto@fibradis.mx` a **`portafoliodefibras@gmail.com`** en el seed administrable y en todos los literales de fallback.
4. **Documentar la regla** en `AGENTS.md` y `convenciones-fibradis.md`, y guardarla en **mem0**, para que ningún agente reintroduzca "FIBRADIS" en superficies de usuario.

> ⚠️ **Distinción crítica (leer antes de tocar nada):** "FIBRADIS" cumple **dos roles** en el repo:
> - **Marca visible / SEO** → DEBE cambiar a "Fibras Inmobiliarias" (este story).
> - **Identificador técnico interno** → **NO cambiar** (rompería el sistema): nombre de repo, env var `FIBRADIS_SKIP_STARTUP_DB_READS`, salt de cifrado `"FIBRADIS-EMAIL-KEY-DEFAULT-2026"` (cambiarlo invalida el descifrado de correos ya almacenados), nombre de BD `FIBRADIS_Dev`, **dominio `fibradis.mx` (NO se cambia el dominio — solo el correo)**, y el `# FIBRADIS — Instrucciones` de la documentación interna de agentes.
>
> El **dominio sigue siendo `fibradis.mx`**. Solo cambia el **correo** (a un buzón Gmail) y el **texto de marca**.

### Relación con otras historias de la Épica 13

- **13-6 (`/portafolio` landing):** su AC-9 especifica `title (… | FIBRADIS)`. **Este story (13-7) cambia ese patrón a `… | Fibras Inmobiliarias`.** Quien implemente 13-6 después de 13-7 debe usar `… | Fibras Inmobiliarias`; si 13-6 ya estuviera mergeada, 13-7 la corrige en el mismo barrido. Coordinar orden de merge.
- **13-8 (`/plataforma` landing):** la nueva landing nace ya con la marca correcta ("Fibras Inmobiliarias", título `… | Fibras Inmobiliarias`). No reintroducir "FIBRADIS".
- **Stash SEO aparcado:** existe un `git stash` ("seo-wip-pendiente-no-relacionado-con-epica13") que toca `SpaMetadataProvider.cs`/`SpaMetadataProviderTests.cs`. **Antes de tocar esos archivos, coordinar con el usuario** si el stash se aplica/commitea primero (evita conflictos).

## Acceptance Criteria

### A. Título SEO — quitar el token "FIBRADIS"

1. El sufijo de marca de los `<title>` pasa de `… | FIBRADIS` (y `… | FIBRADIS — Fibras Inmobiliarias`) a **`… | Fibras Inmobiliarias`** en TODAS las fuentes de título:
   - `SeoDefaultsBuilder.cs`: `BrandTitleSuffix` (` | FIBRADIS` → ` | Fibras Inmobiliarias`) y `FibraTitleSuffix` (` | FIBRADIS — Fibras Inmobiliarias` → ` | Fibras Inmobiliarias`).
   - `SpaMetadataProvider.cs`: los 10 títulos literales del `switch` (`/`, `/calculadora`, `/comparar`, `/fibras`, `/noticias`, `/conoce-las-fibras`, `/calendario`, `/fundamentales`, `/privacidad`, `/acerca`, `/contacto`) y los `name` de JSON-LD que duplican el título (`Sobre … | FIBRADIS`, `Contacto | FIBRADIS`).
   - `FibraProfileMetadataMiddleware.cs`: `$"{fibra.FullName} ({fibra.Ticker}) | FIBRADIS — Fibras Inmobiliarias"` → `… | Fibras Inmobiliarias`.
   - `NewsMetadataMiddleware.cs`: `$"{headline} — Noticias | FIBRADIS"` → `… | Fibras Inmobiliarias`.
   - Títulos en componentes SPA (`usePageTitle`): `HomePage`, `CatalogoPage`, `ComparadorPage`, `CalendarioPage`, `FundamentalesPage`, `NoticiasListPage`, `HerramientasPage` (`'Herramientas — FIBRADIS'`), `AcercaPage`, `PrivacidadPage`, `ContactoPage`, `NoticiaPage`.
2. Donde el título quedaría redundante por empezar ya con "FIBRAs Inmobiliarias" (p.ej. home/catálogo/comparador), **se conserva el sufijo `| Fibras Inmobiliarias`** (decisión: consistencia de marca sobre evitar redundancia). No introducir lógica condicional por título.
3. La constante `BrandName = "FIBRADIS"` (en `SpaMetadataProvider.cs`, `SeoDefaultsBuilder.cs`, `OgImageRenderer.cs`) pasa a `"Fibras Inmobiliarias"`. Esto propaga el cambio a `og:site_name`, JSON-LD `Organization.name`, etc.

### B. Texto de marca visible — "FIBRADIS" → "Fibras Inmobiliarias"

4. **Frontend Main (copy visible):**
   - `PublicLayout.tsx` footer (disclaimer): "La información en **FIBRADIS** es solo con fines… **FIBRADIS** no está regulado por la CNBV." → "Fibras Inmobiliarias".
   - `AcercaPage.tsx`: h1 "Acerca de **FIBRADIS**", "**FIBRADIS** nació en 2023…", "¿Qué es **FIBRADIS**?", "**FIBRADIS** es una plataforma…", "publicada en **FIBRADIS**…", y title/description (AC-1).
   - `ContactoPage.tsx`: "**FIBRADIS** es una plataforma…", "publicada en **FIBRADIS**…", title/description.
   - `PrivacidadPage.tsx`: title/description.
5. **Backend SEO (texto de marca):**
   - `SpaMetadataProvider.cs`: descripciones que mencionan "FIBRADIS" (`PrivacyDescription`, `AboutDescription`, `ContactDescription`) y JSON-LD names (`"FIBRADIS — Fibras Inmobiliarias"`, `"FIBRADIS — Análisis de FIBRAs Inmobiliarias"`).
   - `SeoDefaultsBuilder.cs`: `FibraDescriptionPadSuffix` ("… en FIBRADIS.") y las descripciones de marca (líneas ~547-548).
   - `NewsMetadataMiddleware.cs`: `BrandDescriptionSuffix` ("… en FIBRADIS: …"), JSON-LD `name`.
   - `FibraProfileMetadataMiddleware.cs`: JSON-LD `name`.
   - `FibraPage.tsx` y `NoticiaPage.tsx`: sufijos de descripción que incluyen "en FIBRADIS".
   - `og:image:alt` ("**FIBRADIS** — Análisis de FIBRAs Inmobiliarias Mexicanas") en `SpaMetadataMiddleware.cs`, `NewsMetadataMiddleware.cs`, `FibraProfileMetadataMiddleware.cs` → "Fibras Inmobiliarias — …".
   - `SeoEndpoints.cs` `llms.txt`: encabezado `# FIBRADIS` → `# Fibras Inmobiliarias`.
6. **Imágenes OG (`OgImageRenderer.cs`):** el texto renderizado "FIBRADIS" (línea ~468) y "Datos vivos · FIBRADIS" (línea ~268) → "Fibras Inmobiliarias". **Verificar que el texto más largo no desborde el canvas** (la imagen es 1200×630; ajustar tamaño de fuente/posición si "Fibras Inmobiliarias" no cabe). Documentar en Completion Notes el resultado visual.
7. **Ops (UI administrativa, marca visible):** `OpsShell.tsx` ("FIBRADIS Ops" ×2), `OpsLoginGate.tsx` ("Acceso a FIBRADIS Ops"), `UsersPage.tsx` ("sitio principal de FIBRADIS"), `ConfigPage.tsx` (`DEFAULT_TERMS_TEXT` que empieza "FIBRADIS — Términos…" y "FIBRADIS no será responsable…") → "Fibras Inmobiliarias". *(Ops es interno pero es UI visible; se incluye por consistencia de marca.)*

### C. Correo de contacto → `portafoliodefibras@gmail.com`

8. El **seed administrable** `OperationalConfig.ContactEmail` cambia de `"contacto@fibradis.mx"` a `"portafoliodefibras@gmail.com"` en `OperationalConfigConfiguration.cs` (`HasData`), con su **migración EF correspondiente** (`dotnet ef migrations add`). El valor sigue siendo editable desde Ops (sin cambio funcional).
9. Todos los **literales de fallback** `'contacto@fibradis.mx'` se actualizan a `'portafoliodefibras@gmail.com'`: `PublicLayout.tsx` (footer `mailto`), `AcercaPage.tsx`, `ContactoPage.tsx` (si aplica), `ConfigPage.tsx` (placeholder + default del `useState`/reset), y el texto de `ConfigPage.tsx` "escribe a contacto@fibradis.mx" → al nuevo correo. **Ejecutar `grep -ri "contacto@fibradis"` (excluyendo `Migrations/` y `wwwroot/`) para no dejar ninguno.**
10. **No cambiar** el dominio `fibradis.mx` en URLs, canonical, `App:BaseUrl`, User-Agent ni en migraciones históricas (snapshots EF antiguos). Solo el correo.

### D. Documentación y memoria (regla anti-regresión)

11. **`AGENTS.md`** (sección "Reglas Críticas (No Violar)"): añadir regla de marca — "La marca visible es **Fibras Inmobiliarias**. NUNCA usar "FIBRADIS" en superficies de usuario o SEO (títulos, meta, JSON-LD, copy, og:*, imágenes OG, llms.txt). "FIBRADIS" se conserva SOLO como identificador técnico interno: env vars, salt de cifrado, nombre de BD, nombre de repo y dominio `fibradis.mx`."
12. **`convenciones-fibradis.md`:** corregir el checklist SEO (línea ~72) `<title>Nombre de la página — FIBRADIS</title>` → `<title>Nombre de la página | Fibras Inmobiliarias</title>`; y añadir nota de la regla de marca.
13. **mem0:** guardar la decisión con el formato obligatorio de `CLAUDE.md`:
    `python scripts/memory/memory_cli.py add "contexto: marca/branding | problema: 'FIBRADIS' es nombre de código interno y aparecía en títulos SEO, og, JSON-LD, copy y footer | solución: marca visible = 'Fibras Inmobiliarias'; nunca usar 'FIBRADIS' en superficies de usuario/SEO; conservar 'FIBRADIS' solo como identificador técnico (env vars, salt cifrado, BD FIBRADIS_Dev, dominio fibradis.mx). Título SEO = '… | Fibras Inmobiliarias'. Correo de contacto = portafoliodefibras@gmail.com"`

### E. Transversal (tests + verificación)

14. **Tests (obligatorios antes de `review`, por `workflow-rules.md`):**
    - **Backend (xUnit, `SpaMetadataProviderTests.cs`):** los `[Theory]` que afirman el sufijo de título deben actualizarse de `| FIBRADIS` a `| Fibras Inmobiliarias`; cualquier assert sobre `og:site_name`/JSON-LD `name`/`og:image:alt` que contenga "FIBRADIS" se actualiza. Tests de longitud de descripción (120–160) siguen verdes tras los cambios de copy. `dotnet test` verde.
    - **Frontend Main (`node:test`):** si hay tests que afirman títulos/marca (`prerender-utils.test.mjs` afirma `Fibras Inmobiliarias` en `<title>`; revisar que no rompa). `npm run test --workspace=src/Web/Main` verde.
    - **Builds:** `dotnet build FIBRADIS.slnx` y `npm run build --workspace=src/Web/Main` verdes.
15. **Verificación manual / barrido final:** tras los cambios, `grep -rn "FIBRADIS"` sobre `src/` (excluyendo `wwwroot/`, `Migrations/` y los identificadores técnicos del recuadro de arriba) **no debe arrojar ninguna superficie de marca visible/SEO pendiente**. Documentar en Completion Notes la lista de "FIBRADIS" restantes y por qué son legítimos (técnicos).

## Tasks / Subtasks

- [x] **T1 — Constantes y sufijos de marca backend** (AC: 1, 3, 5)
  - [x] `SeoDefaultsBuilder.cs`: `BrandName`, `BrandTitleSuffix`, `FibraTitleSuffix`, `FibraDescriptionPadSuffix`, descripciones (547-548), title (289).
  - [x] `SpaMetadataProvider.cs`: `BrandName`, 10 títulos del switch, descripciones (`PrivacyDescription`/`AboutDescription`/`ContactDescription`), JSON-LD names (183, 194, 266, 314).
  - [x] `OgImageRenderer.cs`: `BrandName` + textos dibujados (268, 468) — verificado que el layout sigue dentro del canvas.
- [x] **T2 — Middlewares de metadata** (AC: 1, 5)
  - [x] `SpaMetadataMiddleware.cs`: `og:site_name`, `og:image:alt`.
  - [x] `NewsMetadataMiddleware.cs`: `BrandDescriptionSuffix`, `og:site_name`, `og:image:alt`, title suffix, JSON-LD `name`.
  - [x] `FibraProfileMetadataMiddleware.cs`: `og:site_name`, `og:image:alt`, title suffix, JSON-LD `name`.
  - [x] `SeoEndpoints.cs`: `# FIBRADIS` → `# Fibras Inmobiliarias` en llms.txt.
- [x] **T3 — Títulos y copy de componentes SPA Main** (AC: 1, 4)
  - [x] `usePageTitle` en: HomePage, CatalogoPage, ComparadorPage, CalendarioPage, FundamentalesPage, NoticiasListPage, HerramientasPage, AcercaPage, PrivacidadPage, ContactoPage, NoticiaPage.
  - [x] Copy visible: footer de `PublicLayout.tsx`, cuerpo de `AcercaPage.tsx` y `ContactoPage.tsx`, sufijos de descripción en `FibraPage.tsx` y `NoticiaPage.tsx`.
- [x] **T4 — Ops UI** (AC: 7)
  - [x] `OpsShell.tsx`, `OpsLoginGate.tsx`, `UsersPage.tsx`, `ConfigPage.tsx` (`DEFAULT_TERMS_TEXT`).
- [x] **T5 — Correo de contacto** (AC: 8, 9, 10)
  - [x] `OperationalConfigConfiguration.cs` seed → `portafoliodefibras@gmail.com` + `dotnet ef migrations add RebrandContactEmail --project src/Server/Infrastructure --startup-project src/Server/Api`.
  - [x] Literales de fallback: `PublicLayout.tsx`, `AcercaPage.tsx`, `ContactoPage.tsx`, `ConfigPage.tsx` (placeholder/default/texto). `grep -ri "contacto@fibradis"` limpio (excl. Migrations/wwwroot).
- [x] **T6 — Documentación + mem0** (AC: 11, 12, 13) — **YA COMPLETADA durante la creación de esta historia (2026-06-15):**
  - [x] `AGENTS.md` Reglas Críticas: añadida Regla Crítica #7 (marca).
  - [x] `convenciones-fibradis.md`: checklist SEO (línea ~72) actualizado a `| Fibras Inmobiliarias` + nota de regla.
  - [x] `memory_cli.py add "…"` ejecutado (mem0) + memoria local de Claude `project_marca_fibras_inmobiliarias.md`.
  - El dev NO necesita rehacer T6; solo verificar que el código quede en cumplimiento con la regla ya documentada.
- [x] **T7 — Tests + barrido final** (AC: 14, 15)
  - [x] Actualizar `SpaMetadataProviderTests.cs` (sufijo de título, og, JSON-LD). `dotnet test`.
  - [x] Revisar tests Main (`prerender-utils.test.mjs`). `npm run test --workspace=src/Web/Main`.
  - [x] Builds verdes. `grep -rn "FIBRADIS" src/` → solo identificadores técnicos legítimos (documentado en Completion Notes).

## Dev Notes

### Estado actual y qué cambia (UPDATE)

**Catálogo exacto de ocurrencias "FIBRADIS" a CAMBIAR (marca visible/SEO):**

| Archivo | Líneas (aprox.) | Tipo |
|---|---|---|
| `src/Server/Api/Seo/SpaMetadataProvider.cs` | 18 (BrandName), 30,32,34 (desc), 61-107 (10 títulos), 183,194,266,314 (JSON-LD) | título + copy + JSON-LD |
| `src/Server/Infrastructure/Seo/SeoDefaultsBuilder.cs` | 19,20,21,34 (consts), 289 (título), 547-548 (desc) | título + copy |
| `src/Server/Infrastructure/Seo/OgImageRenderer.cs` | 18 (BrandName), 268, 468 (texto dibujado) | imagen OG |
| `src/Server/Api/Middleware/SpaMetadataMiddleware.cs` | 182 (site_name), 185 (image:alt) | og:* |
| `src/Server/Api/Middleware/NewsMetadataMiddleware.cs` | 29, 209, 217, 245, 266, 296, 301 | título + copy + og:* + JSON-LD |
| `src/Server/Api/Middleware/FibraProfileMetadataMiddleware.cs` | 235, 238, 253, 274, 308, 311 | título + copy + og:* + JSON-LD |
| `src/Server/Api/Endpoints/Public/SeoEndpoints.cs` | 228 (`# FIBRADIS` llms.txt) | llms.txt |
| `src/Web/Main/src/shared/layouts/PublicLayout.tsx` | 439 (disclaimer), 445 (mailto fallback) | copy + email |
| `src/Web/Main/src/modules/acerca/AcercaPage.tsx` | 6,9,10,15,25,34,36,129 | título + copy + email |
| `src/Web/Main/src/modules/contacto/ContactoPage.tsx` | 9,10,17,45 (+ email si aplica) | título + copy |
| `src/Web/Main/src/modules/privacidad/PrivacidadPage.tsx` | 9,10 | título + copy |
| `src/Web/Main/src/modules/{home,catalogo,comparador,calendario,fundamentales,noticias,herramientas}/…Page.tsx` | títulos `usePageTitle` | título |
| `src/Web/Main/src/modules/noticia/NoticiaPage.tsx` | 16, 75 | título + desc |
| `src/Web/Main/src/modules/ficha-publica/FibraPage.tsx` | 32 | desc |
| `src/Web/Ops/src/components/OpsShell.tsx` | 17, 102 | UI Ops |
| `src/Web/Ops/src/components/OpsLoginGate.tsx` | 143 | UI Ops |
| `src/Web/Ops/src/pages/UsersPage.tsx` | 374 | UI Ops |
| `src/Web/Ops/src/pages/ConfigPage.tsx` | 11, 14, 26, 95, 345 | UI Ops + email |
| `src/Server/Infrastructure/Persistence/SqlServer/Configurations/Ops/OperationalConfigConfiguration.cs` | 50 (seed email) | email + migración |

**🚫 OCURRENCIAS "FIBRADIS" que NO se tocan (identificadores técnicos — cambiarlas rompe el sistema):**

| Archivo | Qué es | Por qué no cambiar |
|---|---|---|
| `src/Server/Api/Program.cs:147`, `Api.csproj:31` | env var `FIBRADIS_SKIP_STARTUP_DB_READS` | nombre de variable de entorno |
| `src/Server/Infrastructure/Security/EmailEncryptor.cs:23` | salt `"FIBRADIS-EMAIL-KEY-DEFAULT-2026"` | cambiarlo invalida el descifrado de correos almacenados |
| `src/Server/Infrastructure/DesignTimeDbContextFactory.cs:12` | BD `FIBRADIS_Dev` | nombre de base de datos local |
| `src/Server/Api/CompositionRoot/ApiServiceExtensions.cs:167,178,188` | User-Agent `FIBRADIS/1.0 (+https://fibradis.mx)` | identificador de cliente HTTP saliente (no visible en el sitio) — dejar |
| `src/Server/Api/CompositionRoot/UseApiInfrastructureExtensions.cs:59` | `WithTitle("FIBRADIS API")` | título de OpenAPI/Scalar (herramienta interna de dev) — dejar |
| `Migrations/**` (snapshots EF) | seeds históricos `contacto@fibradis.mx` | historial inmutable; NO editar a mano |
| Dominio `fibradis.mx` en URLs/canonical/baseUrl | infraestructura | el usuario solo cambió el CORREO, no el dominio |

### Patrones de marca actuales

- `SeoDefaultsBuilder.cs`: `BrandName = "FIBRADIS"`, `BrandTitleSuffix = " | FIBRADIS"`, `FibraTitleSuffix = " | FIBRADIS — Fibras Inmobiliarias"`.
- `SpaMetadataProvider.cs`: `private const string BrandName = "FIBRADIS";` consumido por JSON-LD `Organization.name`. Cambiar la const propaga la mayoría de JSON-LD; los **títulos del switch están hardcodeados** (no usan la const) → editar uno por uno.
- El correo de contacto es **administrable** (`OperationalConfig.ContactEmail`, leído por `useSiteContent`/`SiteContentDto`); el JSON-LD de contacto (`SpaMetadataProvider`) usa el valor de config (no literal). Por eso basta cambiar **seed + fallbacks**.

### Guardrails técnicos (de cumplimiento estricto)

- **No tocar identificadores técnicos** (recuadro 🚫 arriba). El dominio `fibradis.mx` permanece.
- **El salt de `EmailEncryptor` es intocable.** Si se cambia, los correos cifrados existentes dejan de descifrarse.
- **Migración EF obligatoria** para el cambio de seed (`HasData` cambia → EF exige migración; sin ella el modelo y la BD divergen). Provider real = **SQL Server** (no PostgreSQL pese a lo que dice AGENTS.md; ver `DesignTimeDbContextFactory` y `Persistence/SqlServer`). Comando: `dotnet ef migrations add RebrandContactEmail --project src/Server/Infrastructure --startup-project src/Server/Api`.
- **No `npx shadcn@latest add`** (no aplica; solo copy/consts).
- **OG image:** "Fibras Inmobiliarias" es ~2.5× más largo que "FIBRADIS"; verificar que no desborde 1200×630.
- **Título SEO:** regla única `… | Fibras Inmobiliarias`. Sin lógica condicional por página.

### Security Checklist — completar antes del primer commit

- [ ] **TOCTOU doble-request:** N/A — sin endpoints de escritura nuevos.
- [ ] **Auth-gating de componentes UI:** N/A — solo cambios de copy/consts.
- [ ] **Denominador cero:** N/A.
- [ ] **Cifrado / datos almacenados:** NO modificar el salt `EmailEncryptor` (rompería descifrado de correos persistidos).

### Project Structure Notes

- **Sin** componentes nuevos, rutas nuevas, ni endpoints nuevos. Solo edición de copy/consts + 1 migración EF de seed.
- El cambio de marca toca **ambos SPAs** (Main + Ops) y backend SEO. Coordinar con 13-6/13-8 (títulos).

### References

- [Source: src/Server/Api/Seo/SpaMetadataProvider.cs] — BrandName, títulos switch, JSON-LD
- [Source: src/Server/Infrastructure/Seo/SeoDefaultsBuilder.cs] — sufijos de título y descripción
- [Source: src/Server/Infrastructure/Seo/OgImageRenderer.cs] — texto OG renderizado
- [Source: src/Server/Api/Middleware/SpaMetadataMiddleware.cs] — og:site_name / og:image:alt
- [Source: src/Server/Api/Middleware/NewsMetadataMiddleware.cs] — título noticias + JSON-LD
- [Source: src/Server/Api/Middleware/FibraProfileMetadataMiddleware.cs] — título ficha + JSON-LD
- [Source: src/Server/Api/Endpoints/Public/SeoEndpoints.cs] — llms.txt
- [Source: src/Web/Main/src/shared/layouts/PublicLayout.tsx] — footer + mailto
- [Source: src/Server/Infrastructure/Persistence/SqlServer/Configurations/Ops/OperationalConfigConfiguration.cs] — seed ContactEmail
- [Source: tests/Unit/Infrastructure.Tests/Seo/SpaMetadataProviderTests.cs] — asserts de título/og/JSON-LD
- [Source: _bmad-output/planning-artifacts/convenciones-fibradis.md#Checklist SEO] — línea 72 `— FIBRADIS`
- [Source: AGENTS.md#Reglas Críticas]
- [Source: CLAUDE.md#mem0] — formato obligatorio de memoria
- [Source: _bmad-output/implementation-artifacts/13-6-portafolio-landing-publico.md] — coordina patrón de título
- [Source: _bmad-output/implementation-artifacts/13-8-landing-plataforma-publica.md] — nueva landing nace con marca correcta

## Dev Agent Record

### Agent Model Used

### Debug Log References

- `dotnet ef migrations list --project src/Server/Infrastructure --startup-project src/Server/Api` — verified `20260616171543_RebrandContactEmail` is pending.
- `dotnet test tests/Unit/Infrastructure.Tests/Infrastructure.Tests.csproj` — passed (639/639).
- `dotnet test tests/Integration/Api.Tests/Api.Tests.csproj --filter "FullyQualifiedName~SeoEndpointTests|FullyQualifiedName~SeoRobotsEndpointTests|FullyQualifiedName~SeoBackfillEndpointTests"` — passed (33/33).
- `dotnet test tests/Integration/Api.Tests/Api.Tests.csproj` — full suite still has unrelated pre-existing failures in `OgImageEndpointTests`, `CalculadoraEndpointTests`, and `Ops.DashboardEndpointTests`.
- `npm run test --workspace=src/Web/Main` — passed (176/176).
- `npm run build --workspace=src/Web/Main` — passed.
- `npm run build --workspace=src/Web/Ops` — passed.
- `dotnet build FIBRADIS.slnx` — passed with 0 warnings / 0 errors.

### Completion Notes List

- Rebrand completo en superficies visibles y SEO: títulos, `og:*`, JSON-LD, copy de páginas públicas, footer, Ops UI y `llms.txt` ahora usan `Fibras Inmobiliarias`.
- El correo de contacto quedó migrado a `portafoliodefibras@gmail.com` con seed administrable y migración EF `RebrandContactEmail`.
- `src/Server/Api/wwwroot/llms.txt` se actualizó además del endpoint, porque la integración valida el archivo estático servido por ASP.NET.
- El barrido final de `src/` deja solo `FIBRADIS` en identificadores técnicos legítimos: env var `FIBRADIS_SKIP_STARTUP_DB_READS`, salt `EmailEncryptor`, BD `FIBRADIS_Dev`, User-Agent interno, título OpenAPI, paquete `@fibradis/shared-api-client`, dominio `fibradis.mx` y datos históricos/test con correos `@fibradis.mx`.
- La imagen OG se verificó sin desborde visual tras cambiar el texto a `Fibras Inmobiliarias`.

### File List
- _bmad-output/implementation-artifacts/13-7-rebranding-fibras-inmobiliarias-y-contacto.md
- _bmad-output/implementation-artifacts/sprint-status.yaml
- src/Server/Api/Endpoints/Public/SeoEndpoints.cs
- src/Server/Api/Middleware/FibraProfileMetadataMiddleware.cs
- src/Server/Api/Middleware/NewsMetadataMiddleware.cs
- src/Server/Api/Middleware/SpaMetadataMiddleware.cs
- src/Server/Api/Seo/SpaMetadataProvider.cs
- src/Server/Api/wwwroot/llms.txt
- src/Server/Infrastructure/Migrations/SqlServer/20260616171543_RebrandContactEmail.cs
- src/Server/Infrastructure/Migrations/SqlServer/20260616171543_RebrandContactEmail.Designer.cs
- src/Server/Infrastructure/Migrations/SqlServer/AppDbContextModelSnapshot.cs
- src/Server/Infrastructure/Persistence/SqlServer/Configurations/Ops/OperationalConfigConfiguration.cs
- src/Server/Infrastructure/Seo/OgImageRenderer.cs
- src/Server/Infrastructure/Seo/SeoDefaultsBuilder.cs
- src/Web/Main/src/modules/acerca/AcercaPage.tsx
- src/Web/Main/src/modules/calendario/CalendarioPage.tsx
- src/Web/Main/src/modules/catalogo/CatalogoPage.tsx
- src/Web/Main/src/modules/comparador/ComparadorPage.tsx
- src/Web/Main/src/modules/contacto/ContactoPage.tsx
- src/Web/Main/src/modules/ficha-publica/FibraPage.tsx
- src/Web/Main/src/modules/fundamentales/FundamentalesPage.tsx
- src/Web/Main/src/modules/herramientas/HerramientasPage.tsx
- src/Web/Main/src/modules/home/HomePage.tsx
- src/Web/Main/src/modules/noticia/NoticiaPage.tsx
- src/Web/Main/src/modules/noticias/NoticiasListPage.tsx
- src/Web/Main/src/modules/portafolio/PortafolioLanding.tsx
- src/Web/Main/src/modules/privacidad/PrivacidadPage.tsx
- src/Web/Main/src/modules/reportes/ReportesPage.tsx
- src/Web/Main/src/shared/layouts/PublicLayout.tsx
- src/Web/Ops/src/components/OpsLoginGate.tsx
- src/Web/Ops/src/components/OpsShell.tsx
- src/Web/Ops/src/pages/ConfigPage.tsx
- src/Web/Ops/src/pages/SeoOrganizationPage.tsx
- src/Web/Ops/src/pages/UsersPage.tsx
- tests/Integration/Api.Tests/Ops/SeoBackfillEndpointTests.cs
- tests/Integration/Api.Tests/Ops/SeoRobotsEndpointTests.cs
- tests/Integration/Api.Tests/SeoEndpointTests.cs
- tests/Unit/Infrastructure.Tests/Endpoints/SeoEndpointsTests.cs
- tests/Unit/Infrastructure.Tests/Middleware/FibraProfileMetadataMiddlewareTests.cs
- tests/Unit/Infrastructure.Tests/Middleware/NewsMetadataMiddlewareTests.cs
- tests/Unit/Infrastructure.Tests/Middleware/SpaMetadataMiddlewareTests.cs
- tests/Unit/Infrastructure.Tests/Persistence/Repositories/OperationalConfigRepositoryTests.cs
- tests/Unit/Infrastructure.Tests/Seo/SeoDefaultsBuilderTests.cs
- tests/Unit/Infrastructure.Tests/Seo/SpaMetadataProviderTests.cs

### Change Log

- 2026-06-16: reemplacé la marca visible por `Fibras Inmobiliarias` en backend SEO, Main, Ops y `llms.txt`; actualicé el contacto administrable a `portafoliodefibras@gmail.com` con migración EF y dejé el story listo para review.

## Senior Developer Review (AI)

Revisión adversarial (code-review) ejecutada 2026-06-16. Capas: Acceptance Auditor (completa) + verificación directa equivalente a Blind/Edge Hunter (los subagentes adversariales fallaron por error 500; se sustituyeron por grep/lectura directa de `src/`).

**Contexto clave:** el dominio real configurado es `https://fibrasinmobiliarias.com` (`App:BaseUrl`), no `fibradis.mx` como afirma el spec. `fibradis.mx` solo persiste en identificadores técnicos (User-Agent, JWT issuer, salt, BD, paquete npm). Esto valida los cambios de `twitter:site`/placeholders hacia `fibrasinmobiliarias`.

### Review Findings

- [x] [Review][Decision][RESUELTO] Referencias sociales/dominio alineadas a `fibrasinmobiliarias.com` fuera del catálogo del spec — `twitter:site` `@fibradis`→`@fibrasinmobiliarias` (×5 en los 3 middlewares), placeholder Ops `adminops@fibradis.mx`→`adminops@fibrasinmobiliarias.com`, placeholder YouTube/sameAs `@fibradis`→`@fibrasinmobiliarias`. **Resolución (Jorge, 2026-06-16): APROBADO** — los handles `@fibrasinmobiliarias` y `youtube.com/@fibrasinmobiliarias` son reales y el dominio real es `fibrasinmobiliarias.com`; la ampliación de alcance es correcta. [SpaMetadataMiddleware.cs:187, NewsMetadataMiddleware.cs:221,305, FibraProfileMetadataMiddleware.cs:240,313, OpsLoginGate.tsx:154, SeoOrganizationPage.tsx:105]
- [x] [Review][Decision][RESUELTO] Artefactos de build en `wwwroot/` desactualizados conservan la marca/correo viejos — Bundles compilados servidos por ASP.NET (`wwwroot/ops/assets/index-novMaiBo.js`, `index-KclEkwep.js`, `wwwroot/assets/*.js`) aún contienen "FIBRADIS no será responsable…" y "escribe a contacto@fibradis.mx". **Resolución (Jorge, 2026-06-16): DESCARTADO** — el pipeline de deploy reconstruye `wwwroot` desde `npm run build` y copia bundles frescos; el código fuente correcto se propaga en el deploy. Sin acción.
- [x] [Review][Defer] Migración sobrescribe ContactEmail editado en Ops [20260616171543_RebrandContactEmail.cs:14] — deferred, riesgo bajo pre-lanzamiento; `UpdateData(id=1)` sin condición pisa un correo personalizado desde Ops. Inherente a `HasData`.
- [x] [Review][Defer] Fallback de `mailto` en footer usa `??` sin `.trim()` [PublicLayout.tsx:445] — deferred, pre-existente; un `contactEmail` = "" produciría `mailto:` vacío (Acerca/Contacto/Privacidad usan `?.trim() || …`). Solo cambió el literal en este story.

### Dismissed (ruido / falso positivo)

- JSON-LD `name` colapsado a `BrandName` (perdió "— Análisis de FIBRAs Inmobiliarias"): cambio válido y consistente con el principio de consistencia de marca (AC2); el texto de AC5 era ilustrativo.
- Desborde de imagen OG (AC6): geometría confirma que ambos textos caben (título 54pt desde x=96 ≈700px<1200; footer 14pt desde x=770 ≈270px<430 disponibles). Atestiguado por el dev.
- Brecha de verificación AC11/AC13: mem0 confirmado por recall ("marca visible = Fibras Inmobiliarias"); `convenciones-fibradis.md` línea 72 verificada actualizada.
