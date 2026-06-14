# Story 12.8: Enlazado interno — fibras relacionadas por sector + pillar→spoke

Status: done

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

- [x] **T1 — Repo (AC-1)**: `GetActiveBySectorAsync(sector, excludeId, count, ct)` en IFibraRepository + FibraRepository. Query secuencial, `AsNoTracking`, guard sector vacío/count≤0 → `[]`.
- [x] **T2 — Endpoint (AC-1)**: `GET /api/v1/fibras/{ticker}/related` en CatalogEndpoints (anónimo, 404 si ticker no existe, hasta 6 del sector). Nuevo DTO `RelatedFibra(Ticker, FullName, ShortName, Sector)`. Codegen ejecutado (`/related` + `RelatedFibra` en schema.d.ts).
- [x] **T3 — Sección ficha (AC-2, AC-4)**: `FibrasRelacionadasSection` (TanStack Query, `buildFibraSlug`, render condicional, `<nav>` accesible con anchor descriptivo = nombre fibra). Integrada en `FibraPage.tsx` al final (below-the-fold, coord. 12-7).
- [x] **T4 — Pillar→spoke (AC-3)**: bloque fijo "Explora las FIBRAs" al pie de `ConoceLasFibrasPage` con `<Link>` a `/fibras`, `/fundamentales`, `/comparar`. **Decisión: bloque fijo** (no markdown editable) para garantizar los enlaces independientemente del contenido editorial.
- [x] **T5 — Tests (AC-5)**: 5 unit repo (sector match, exclusión propia, excluye inactivas, count, sector vacío/count 0); 3 integration endpoint (200 + payload mismo-sector excluyendo self, campos de tarjeta, 404); frontend `shouldShowRelacionadas` (render condicional). 13/13 unit + 9/9 integration + 171/171 frontend verdes.

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
Claude Opus 4.8 (1M context)
### Debug Log References
`dotnet build FIBRADIS.slnx` (0 errores)
`dotnet test tests/Unit/Infrastructure.Tests --filter FibraRepositoryTests` → 13/13 Correctas
`dotnet test tests/Integration/Api.Tests --filter CatalogEndpointTests -m:1` → 9/9 Correctas
`npm run codegen:api` (regenera schema.d.ts con /related + RelatedFibra)
`npm run build --workspace=src/Web/Main` (0 errores TS) · `npm run test` → 171/171 · `npx eslint` limpio
### Completion Notes List
- AC-1/T1: `GetActiveBySectorAsync` con guard (sector vacío/count≤0 → []), `AsNoTracking`, orden por ticker, query secuencial.
- AC-1/T2: endpoint `/related` anónimo, 404 con ProblemDetails si el ticker no existe, hasta 6 del mismo sector excluyendo la propia. DTO `RelatedFibra` mínimo; el slug se construye en el cliente (paridad buildFibraSlug).
- Agregar el método a `IFibraRepository` rompió 8 fakes de test (CS0535); se añadió implementación stub (`=> Task.FromResult<...>([])`) a cada uno.
- AC-2/T3: `FibrasRelacionadasSection` self-contained (se oculta si no hay pares), `<nav aria-label>`, anchor = nombre de la fibra. Colocada below-the-fold al final de la ficha (coord. 12-7).
- AC-3/T4: **decisión = bloque fijo** "Explora las FIBRAs" en ConoceLasFibrasPage (no depende del markdown editable) con links a /fibras, /fundamentales, /comparar.
- AC-5: `shouldShowRelacionadas` quedó en helper puro (testeable bajo node --test, sin imports de valor `@/`); el mapeo de slug se hace en el componente con `buildFibraSlug` (ya cubierto por `fibra-slug.test.ts`), evitando importar el alias `@/` en un archivo testeado por node.
### File List
- `src/Server/Application/Catalog/IFibraRepository.cs` (M)
- `src/Server/Infrastructure/Persistence/Repositories/Catalog/FibraRepository.cs` (M)
- `src/Server/SharedApiContracts/Catalog/RelatedFibra.cs` (NEW)
- `src/Server/Api/Endpoints/Public/CatalogEndpoints.cs` (M)
- `src/Web/SharedApiClient/schema.d.ts` (M, codegen)
- `src/Web/Main/src/api/fibrasApi.ts` (M)
- `src/Web/Main/src/modules/ficha-publica/sections/fibras-relacionadas.ts` (NEW)
- `src/Web/Main/src/modules/ficha-publica/sections/FibrasRelacionadasSection.tsx` (NEW)
- `src/Web/Main/src/modules/ficha-publica/sections/fibras-relacionadas.test.ts` (NEW)
- `src/Web/Main/src/modules/ficha-publica/FibraPage.tsx` (M)
- `src/Web/Main/src/modules/conoce-las-fibras/ConoceLasFibrasPage.tsx` (M)
- `src/Web/Main/package.json` (M, test list)
- 8 fakes de IFibraRepository en `tests/Unit/Infrastructure.Tests/` (M, stub método nuevo)
- `tests/Unit/Infrastructure.Tests/Persistence/Repositories/FibraRepositoryTests.cs` (M, +5 tests)
- `tests/Integration/Api.Tests/CatalogEndpointTests.cs` (M, +3 tests)

### Change Log
- 2026-06-14: Implementada 12-8 (enlazado interno: fibras relacionadas por sector + pillar→spoke). Status → review.

## Review Findings (Code Review 2026-06-14)

> Revisión adversarial en 3 capas (Blind Hunter + Edge Case Hunter + Acceptance Auditor). 0 BLOCKER/HIGH/MEDIUM, 5/5 ACs satisfechos. Resultado: 1 patch, 3 defer, 6 dismissed.

### Patches

- [x] [Review][Patch] El test de integración `GetRelatedFibras_EachItemHasCardFields` solo verifica presencia de propiedades (`TryGetProperty(..., out _)`), no que tengan valor no-vacío — pasaría aunque serializaran `null`; endurecer a strings no vacíos [tests/Integration/Api.Tests/CatalogEndpointTests.cs]

### Deferred

- [x] [Review][Defer] Los links de "FIBRAs relacionadas" no aparecen en el HTML prerenderizado/SSR (la query client-side no tiene prefetch/dehydration), así que crawlers sin JS no los ven — consistente con las demás secciones client-fetch (Noticias, Distribuciones) y son `<a>` reales que Google (render JS) sí sigue. Revisitar si el coverage de GSC muestra que no se rastrean.
- [x] [Review][Defer] El endpoint `/related` con un ticker benchmark (`^MXX`/`^GSPC`, Inactive, sector "Índice") devuelve `200 []` en vez de 404 porque `GetByTickerAsync` no filtra `State` — rasgo pre-existente que también tiene `/{ticker}`; benigno (array vacío → sección oculta).
- [x] [Review][Defer] `RelatedFibra.ShortName` se transporta y tipa pero el componente solo renderiza `fullName` (anchor descriptivo para SEO) — over-fetch inocuo de un string; dejar para un posible label compacto en la tarjeta o eliminar en una limpieza futura (requiere re-codegen).

### Dismissed (no acción)

- Endpoint devuelve array y declara `IReadOnlyList<RelatedFibra>` — codegen verificado: emite `RelatedFibra[]`, consistente con el cliente.
- `fetchRelatedFibras` colapsa 404/error a `[]` → sección oculta — degradación intencional (AC-2 "sin estado vacío feo").
- Desviación DTO: slug construido en cliente (no en servidor) — intencional, evita slug divergente (gate A2).
- Convención `@/`: el split helper-puro/componente para node --test es tradeoff documentado, no introduce imports relativos `../../` de valor.
- Mapeo de slug sin test directo — composición de `buildFibraSlug` (ya cubierto por `fibra-slug.test.ts`) + template; sin lógica nueva.
- Match exacto de `Sector` sin normalización — los sectores son vocabulario controlado del seed (`CatalogSeed.cs`, valores canónicos limpios); el repo además guarda sector vacío.
