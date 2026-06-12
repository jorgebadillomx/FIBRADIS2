---
title: 'Renombrar ruta /catalogo → /fibras con redirect 301'
type: 'refactor'
created: '2026-06-11'
status: 'done'
route: 'one-shot'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** La sección del catálogo de FIBRAs estaba expuesta en `/catalogo`, pero la URL canónica correcta para el dominio es `/fibras` (el segmento ya usado por las fichas individuales `/fibras/:slug`).

**Approach:** Renombrar la ruta en el router de la SPA principal, actualizar todos los links de navegación y breadcrumb, actualizar el backend SEO (sitemap, metadata, canonical), agregar redirect 301 desde `/catalogo` para preservar links existentes y link equity.

</frozen-after-approval>

## Suggested Review Order

1. [routes.tsx:27](../src/Web/Main/src/app/routes.tsx#L27) — ruta cambiada a `/fibras`
2. [PublicLayout.tsx:104](../src/Web/Main/src/shared/layouts/PublicLayout.tsx#L104) — nav desktop
3. [PublicLayout.tsx:218](../src/Web/Main/src/shared/layouts/PublicLayout.tsx#L218) — nav mobile
4. [FibraPage.tsx:269](../src/Web/Main/src/modules/ficha-publica/FibraPage.tsx#L269) — breadcrumb (label también corregido a "Fibras")
5. [FibraSlugRedirectMiddleware.cs:47](../src/Server/Api/Middleware/FibraSlugRedirectMiddleware.cs#L47) — redirect 301 `/catalogo` → `/fibras`
6. [SpaMetadataProvider.cs:54](../src/Server/Api/Seo/SpaMetadataProvider.cs#L54) — clave y canonical actualizados a `/fibras`
7. [SeoEndpoints.cs:20](../src/Server/Api/Endpoints/Public/SeoEndpoints.cs#L20) — sitemap actualizado a `/fibras`
8. [FibraSlugRedirectMiddlewareTests.cs:83](../tests/Unit/Infrastructure.Tests/Middleware/FibraSlugRedirectMiddlewareTests.cs#L83) — tests del nuevo redirect
9. [SeoEndpointsTests.cs:37](../tests/Unit/Infrastructure.Tests/Endpoints/SeoEndpointsTests.cs#L37) — test de sitemap actualizado
10. [SpaMetadataProviderTests.cs:13](../tests/Unit/Infrastructure.Tests/Seo/SpaMetadataProviderTests.cs#L13) — tests de metadata actualizados

## Dev Agent Record

**Files changed:**
- `src/Web/Main/src/app/routes.tsx` — path `/catalogo` → `/fibras`
- `src/Web/Main/src/shared/layouts/PublicLayout.tsx` — 2 nav links actualizados
- `src/Web/Main/src/modules/ficha-publica/FibraPage.tsx` — breadcrumb link + label
- `src/Server/Api/Middleware/FibraSlugRedirectMiddleware.cs` — redirect estático `/catalogo` → `/fibras`
- `src/Server/Api/Seo/SpaMetadataProvider.cs` — clave de diccionario y canonical
- `src/Server/Api/Endpoints/Public/SeoEndpoints.cs` — sitemap StaticRoutes
- `tests/Unit/Infrastructure.Tests/Middleware/FibraSlugRedirectMiddlewareTests.cs` — 3 nuevos casos
- `tests/Unit/Infrastructure.Tests/Endpoints/SeoEndpointsTests.cs` — assertion actualizada
- `tests/Unit/Infrastructure.Tests/Seo/SpaMetadataProviderTests.cs` — 2 InlineData actualizados

**Tests:** `dotnet test tests/Unit/Infrastructure.Tests/ --filter "SeoEndpointsTests|SpaMetadataProviderTests|FibraSlugRedirectMiddlewareTests"` → 59 passed, 0 failed
