---
title: 'A2 — Consolidar triple slugify: extracción de normalización Unicode compartida'
type: 'refactor'
created: '2026-06-11'
status: 'done'
baseline_commit: '877a3e52e6e3b6e21fc59af1f103fc816a460862'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Los tres slugificadores del sistema (`FibraSlug.cs`, `SlugGenerator.cs`, `fibra-slug.ts`) copian la misma lógica de normalización Unicode NFD→strip combining marks→NFC→lowercase. El riesgo no es que produzcan outputs distintos hoy, sino que la normalización diverja en el futuro si alguien corrige un bug en una copia sin tocar las otras.

**Approach:** Extraer la normalización Unicode compartida a `SlugHelper.cs` (Application layer, C#). `FibraSlug.cs` y `SlugGenerator.cs` delegan a `SlugHelper.NormalizeText` — sus APIs públicas y algoritmos de slugify permanecen sin cambios. `fibra-slug.ts` no se toca: ya es la contraparte TS en paridad con FibraSlug y tiene sus propios tests de paridad cross-verificados.

## Boundaries & Constraints

**Always:**
- Las APIs públicas de `FibraSlug.Build` y `SlugGenerator.Generate` permanecen idénticas — zero cambios en callers
- Los 12 tests de FibraSlugTests y los 8 de SlugGeneratorTests deben pasar sin modificación
- `fibra-slug.ts` no se modifica — cambiarla rompería la paridad con FibraSlug y requeriría verificación contra slugs en BD
- Los slugs producidos para los datos existentes en BD deben ser byte-a-byte idénticos antes y después — verificar con `[ManualCheck]` al final

**Ask First:**
- Si al refactorizar se detecta que la normalización en uno de los archivos NO es idéntica a los otros (bug oculto que el test de paridad exponga) → HALT, mostrar la diferencia antes de proceder

**Never:**
- No fusionar los algoritmos de slugify de FibraSlug y SlugGenerator — sus estrategias post-normalización son intencionalmente distintas (`[^a-z0-9]+` vs space-first)
- No cambiar el scope para incluir `fibra-slug.ts`
- No mover `SlugHelper` fuera de la capa Application

</frozen-after-approval>

## Code Map

- `src/Server/Application/Catalog/FibraSlug.cs` — contiene `Slugify(string)` privada con normalización inline; `Build(fullName, ticker)` público usado por SeoEndpoints y FibraSlugRedirectMiddleware
- `src/Server/Application/News/SlugGenerator.cs` — contiene `Generate(string, int)` con normalización inline idéntica a FibraSlug; callers: NewsRepository.GenerateUniqueSlugAsync
- `src/Web/Main/src/shared/lib/fibra-slug.ts` — contraparte TS de FibraSlug; NO tocar
- `tests/Unit/Application.Tests/Catalog/FibraSlugTests.cs` — 12 tests; deben pasar sin cambios
- `tests/Unit/Application.Tests/News/SlugGeneratorTests.cs` — 8 tests; deben pasar sin cambios

## Tasks & Acceptance

**Execution:**
- [x] `src/Server/Application/Catalog/SlugHelper.cs` -- CREAR con `public static string NormalizeText(string input)`: NFD → strip NonSpacingMark → NFC → lowercase. Clase `public static`.
- [x] `src/Server/Application/Catalog/FibraSlug.cs` -- MODIFICAR `Slugify`: reemplaza bloque inline por `SlugHelper.NormalizeText(text)`. Removes unused `System.Globalization` + `System.Text` usings.
- [x] `src/Server/Application/News/SlugGenerator.cs` -- MODIFICAR `Generate`: reemplaza bloque inline por `SlugHelper.NormalizeText(text)`. Adds `using Application.Catalog`.
- [x] `tests/Unit/Application.Tests/Catalog/SlugHelperTests.cs` -- CREAR 4 tests de paridad: Theory con 5 inputs (incluye U+0483), IsConsistentWithFibraSlug, IsConsistentWithSlugGenerator.

**Acceptance Criteria:**
- Dado que existen 10 tests en FibraSlugTests, cuando se corre la suite, entonces todos pasan sin modificación.
- Dado que existen 7 tests en SlugGeneratorTests, cuando se corre la suite, entonces todos pasan sin modificación.
- Dado `SlugHelper.NormalizeText("FIBRA Uno S.A. de C.V.")`, cuando se llama, entonces retorna `"fibra uno s.a. de c.v."` (post-normalización, pre-slugify).
- Dado `SlugHelper.NormalizeText("Distribución")`, cuando se llama, entonces retorna `"distribucion"` (acento removido, lowercase).
- Dado `SlugHelper.NormalizeText("Fibra҃uno")` (U+0483), cuando se llama, entonces retorna `"fibrauno"` (marca combinante no-latina removida).
- `[ManualCheck N/A]` ~~Verificar slug almacenado en BD~~ — `catalog.Fibra` no tiene columna `slug`; el slug se computa on-the-fly en `FibraSlugRedirectMiddleware` y `SeoEndpoints` por cada request/sitemap. No hay estado persistido que pueda divergir. Riesgo: ninguno.

## Verification

**Commands:**
- `dotnet test tests/Unit/Application.Tests --filter "FibraSlugTests|SlugGeneratorTests|SlugHelperTests"` -- expected: todos los tests verdes, 0 fallos
- `dotnet build FIBRADIS.slnx` -- expected: 0 errores, 0 advertencias

## Suggested Review Order

**Extracción del helper compartido**

- Nuevo helper: algoritmo NFD→strip NonSpacingMark→NFC→lowercase con null guard defensivo
  [`SlugHelper.cs:1`](../../src/Server/Application/Catalog/SlugHelper.cs#L1)

- FibraSlug delega normalización; algoritmo slugify `[^a-z0-9]+` sin cambios
  [`FibraSlug.cs:27`](../../src/Server/Application/Catalog/FibraSlug.cs#L27)

- SlugGenerator delega normalización; algoritmo space-first sin cambios
  [`SlugGenerator.cs:14`](../../src/Server/Application/News/SlugGenerator.cs#L14)

**Tests**

- Theory con 5 inputs (incluye U+0483) + 2 tests de consistencia con callers
  [`SlugHelperTests.cs:1`](../../tests/Unit/Application.Tests/Catalog/SlugHelperTests.cs#L1)
