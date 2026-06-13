# Story 12.8: Enlazado interno — fibras relacionadas por sector + pillar→spoke

Status: ready-for-dev

<!-- Independiente de 12-1. Historia chica: solo cubre los 2 gaps reales de enlazado interno. -->

## Story

As a **buscador / usuario navegando FIBRADIS**,
I want **enlaces internos a fibras relacionadas (mismo sector) en la ficha y enlaces del contenido pillar hacia las fibras**,
so that **mejore el rastreo, la distribución de autoridad (link equity) y la autoridad temática, y los usuarios descubran contenido relacionado**.

## Contexto — qué YA existe (NO reinventar)
La exploración confirma que el enlazado interno está mayormente construido. **Solo hay 2 gaps reales**:

| Enlace interno | Estado |
|---|---|
| Ficha → noticias relacionadas (`#noticias`) | ✅ EXISTE (`GetLatestForFibraAsync`, [NoticiasSection.tsx](src/Web/Main/src/modules/ficha-publica/sections/NoticiasSection.tsx)) |
| Noticia → chips de fibras enlazadas | ✅ EXISTE (`GetLinkedFibrasAsync`, [NoticiaPage.tsx:142-154](src/Web/Main/src/modules/noticia/NoticiaPage.tsx)) |
| Noticia → noticias relacionadas | ✅ EXISTE (`GetRelatedAsync`, basado en fibras compartidas) |
| Home/catálogo/listado → detalle | ✅ EXISTE |
| **Ficha → fibras relacionadas (mismo sector)** | ❌ **GAP** — no hay query ni UI; `Fibra.Sector` existe |
| **Pillar (`/conoce-las-fibras`) → fibras** | ❌ **GAP** — solo markdown editorial, sin links programáticos |

Esta historia **solo** cierra esos dos gaps.

## Acceptance Criteria

**AC-1 — Fibras relacionadas por sector (backend).** Nuevo método `IFibraRepository.GetActiveBySectorAsync(sector, excludeId, count, ct)` (LINQ: `State == Active && Sector == sector && Id != excludeId`, `Take(count)`). Endpoint público `GET /api/v1/fibras/{ticker}/related` (o extensión del endpoint de ficha) que devuelve hasta N (p.ej. 4-6) fibras del mismo sector con datos mínimos para tarjeta (ticker, nombre, sector, slug).

**AC-2 — Sección "FIBRAs relacionadas" en la ficha.** `FibraPage` muestra una sección con enlaces `<Link to="/fibras/{slug}">` a las fibras del mismo sector. Cada enlace usa `buildFibraSlug` (paridad con el resto del sitio). Si no hay pares del sector → la sección se oculta (no estado vacío feo). Accesible (WCAG 2.1 AA).

**AC-3 — Pillar→spoke desde conoce-las-fibras.** El contenido educativo enlaza hacia páginas reales: como mínimo, links a `/fibras` (catálogo) y a `/fundamentales` desde las secciones relevantes. Opción recomendada: un bloque "Explora las FIBRAs" al pie de las secciones con enlaces a `/fibras`, `/fundamentales`, `/comparar` (no depender de que el markdown editorial los tenga hardcodeados). Documentar la decisión (bloque fijo vs. links en markdown editable).

**AC-4 — Sin romper lo existente.** Las secciones de noticias relacionadas / chips de fibra / related news siguen intactas. La nueva sección de fibras relacionadas no degrada el render ni la performance de la ficha (lazy/colocada below-the-fold; coord. con 12-7).

**AC-5 — Tests.** Unit del repo `GetActiveBySectorAsync` (filtra sector, excluye la propia, respeta count, solo activas). Integration del endpoint related (200 + payload). Frontend: render condicional de la sección. Verdes antes de `done`.

## Tasks / Subtasks

- [ ] **T1 — Repo (AC-1)**: `GetActiveBySectorAsync` en [IFibraRepository.cs](src/Server/Application/Catalog/IFibraRepository.cs) + [FibraRepository.cs](src/Server/Infrastructure/Persistence/Repositories/Catalog/FibraRepository.cs). Query secuencial (no Task.WhenAll).
- [ ] **T2 — Endpoint (AC-1)**: `GET /api/v1/fibras/{ticker}/related` en los endpoints públicos de catálogo/fibras. DTO mínimo para tarjeta (reusar un DTO existente de fibra si encaja). Anónimo. Codegen (`dotnet build` → `npm run codegen:api`).
- [ ] **T3 — Sección ficha (AC-2, AC-4)**: componente `FibrasRelacionadasSection` en `src/Web/Main/src/modules/ficha-publica/sections/`; TanStack Query; `buildFibraSlug`; render condicional; integrar en `FibraPage.tsx` (below-the-fold). Accesible.
- [ ] **T4 — Pillar→spoke (AC-3)**: bloque de enlaces internos al pie de `ConoceLasFibrasPage` (o en el layout de la sección) hacia `/fibras`, `/fundamentales`, `/comparar`. Documentar enfoque.
- [ ] **T5 — Tests (AC-5)**: unit repo (valores exactos: sector match, exclusión propia, count, solo activas), integration endpoint, frontend render condicional. `dotnet test tests/Unit/`, `dotnet test tests/Integration/ -m:1`, `npm run build`.

## Dev Notes
- **Stack real = SQL Server**. Sin tablas nuevas; solo una query + un endpoint + UI.
- **`Sector` es `string` plano** ([Fibra.cs:10](src/Server/Domain/Catalog/Fibra.cs)) — comparación exacta. Considerar normalización (trim/case) si los sectores tienen variantes; verificar valores reales en el catálogo. Si hay ruido, normalizar en la query.
- **Reusar `buildFibraSlug`** ([fibra-slug.ts](src/Web/Main/src/shared/lib/fibra-slug.ts)) — NO crear otra slugify (recordar gate A2 triple-slugify de Épica 11; aquí solo se consume, no se genera nueva semántica).
- **No reinventar related news**: ya existe `GetRelatedAsync`/`GetLatestForFibraAsync`. Esta historia NO toca noticias.
- **SEO**: los enlaces internos deben ser `<a>`/`<Link>` reales (no botones JS) para que los crawlers los sigan. Texto de ancla descriptivo (nombre de la fibra, no "click aquí").
- **Coord. 12-7**: colocar la sección below-the-fold y lazy para no dañar LCP/CLS.

### Security Checklist — antes del primer commit
- [ ] **TOCTOU / Auth-gating**: N/A (lectura, ruta pública).
- [ ] **Denominador cero**: N/A.
- [ ] **Inyección**: `sector` viene de la fibra resuelta (no input de usuario directo en la query del related por ticker).

### References
- [FibraPage.tsx:335](src/Web/Main/src/modules/ficha-publica/FibraPage.tsx) (sección noticias existente), [NoticiasSection.tsx](src/Web/Main/src/modules/ficha-publica/sections/NoticiasSection.tsx)
- [NoticiaPage.tsx:142-154](src/Web/Main/src/modules/noticia/NoticiaPage.tsx) (chips fibra existentes)
- [IFibraRepository.cs](src/Server/Application/Catalog/IFibraRepository.cs), [FibraRepository.cs](src/Server/Infrastructure/Persistence/Repositories/Catalog/FibraRepository.cs), [Fibra.cs](src/Server/Domain/Catalog/Fibra.cs)
- [ConoceLasFibrasPage.tsx](src/Web/Main/src/modules/conoce-las-fibras/ConoceLasFibrasPage.tsx)
- [fibra-slug.ts](src/Web/Main/src/shared/lib/fibra-slug.ts)
- Story 12-7 (coordinar perf): [12-7-core-web-vitals.md](_bmad-output/implementation-artifacts/12-7-core-web-vitals.md)
- 2026: [GEO 2026 — Frase](https://www.frase.io/blog/what-is-generative-engine-optimization-geo)

## Dev Agent Record
### Agent Model Used
### Debug Log References
### Completion Notes List
### File List
