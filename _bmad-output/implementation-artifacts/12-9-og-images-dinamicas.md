# Story 12.9: OG images dinámicas (social cards por fibra)

Status: ready-for-dev

<!-- Depende suavemente de 12-1/12-3 (el og:image se setea en los middlewares y la fila SeoMetadata). -->

## Story

As a **usuario que comparte una fibra en redes / motor de IA que muestra preview**,
I want **que cada ficha de fibra genere una imagen social (Open Graph) propia con logo + nombre + ticker + precio/yield**,
so that **el preview sea distintivo e informativo (no la imagen genérica), aumentando CTR en redes sociales y en respuestas de IA con tarjeta**.

## Dependencias y contexto
- **Estado actual:** `og:image` de fibras = genérica `/og-image.png` (1200×630) en [FibraProfileMetadataMiddleware.cs:188](src/Server/Api/Middleware/FibraProfileMetadataMiddleware.cs). Las **noticias ya usan su propia `ImageUrl`** ([NewsMetadataMiddleware.cs:216](src/Server/Api/Middleware/NewsMetadataMiddleware.cs)). El hueco real es **fibras** (y opcionalmente páginas fijas/fundamentales).
- **Datos para la tarjeta** (disponibles server-side, ver 12-3): `Fibra.FullName/Ticker/Sector`, `PriceSnapshot.LastPrice`, yield (`YieldCalculator`).
- og:image referenciado en el bloque meta de los middlewares y (con 12-1) en la fila `SeoMetadata.OgImageUrl`.

## Decisión de enfoque (a confirmar en spike)
Tres opciones; recomendado evaluar en T1:
1. **Render server-side a PNG bajo demanda + cache** (recomendado): endpoint `GET /og/fibras/{ticker}.png` que compone la tarjeta (logo + nombre + ticker + precio + yield) con una librería .NET de imágenes (p.ej. **ImageSharp** / SkiaSharp) o SVG→PNG, cacheado (CDN/`Cache-Control` + `IMemoryCache`/disco). Datos vivos al generar.
2. **Generación en build/al crear** (estática por fibra): genera el PNG al crear/editar la fibra (hook de 12-1) y lo guarda; sin precio vivo (quedaría stale).
3. **Servicio externo de OG image**: descartado (dependencia externa, costo, privacidad).

> Recomendado **opción 1** con datos cacheados 1-6h (el precio en la tarjeta no necesita ser de segundos). Confirmar la librería en spike (licencia, soporte de fuentes/acentos es-MX).

## Acceptance Criteria

**AC-1 — Endpoint de OG image por fibra.** `GET /og/fibras/{ticker}.png` (anónimo) devuelve un PNG 1200×630 con: logo FIBRADIS, nombre + ticker de la fibra, sector, y (si hay datos) precio y yield. `Content-Type: image/png`, `Cache-Control` adecuado (p.ej. `public, max-age=21600`). Ticker desconocido → fallback a `/og-image.png` (302 o servir genérica).

**AC-2 — Wiring en metadata.** `FibraProfileMetadataMiddleware` (y la fila `SeoMetadata.OgImageUrl` de 12-1, si no está override) apuntan `og:image`/`twitter:image` a `{baseUrl}/og/fibras/{ticker}.png`. Mantiene `og:image:width/height/alt`. Si `OgImageUrl` está override en Ops → respeta el manual (regla 12-1).

**AC-3 — Acentos/branding correctos.** La imagen renderiza correctamente acentos y "ñ" (es-MX), usa la tipografía/colores de marca y es legible a tamaño de preview. Validar con el Sharing Debugger / inspección.

**AC-4 — Performance y resiliencia.** La generación está cacheada (no se renderiza por cada request). Si la generación falla (sin datos, error de librería) → fallback a la imagen genérica, nunca 500 que rompa el preview. No bloquea el render del HTML (el `<meta>` solo apunta a la URL).

**AC-5 — Tests.** Unit del compositor (dado fibra+precio+yield → imagen no vacía, dimensiones 1200×630; caso sin precio; ticker desconocido → fallback). Integration del endpoint (200 image/png + headers; ticker inválido → fallback). Verdes antes de `done`.

## Tasks / Subtasks

- [ ] **T1 — Spike de librería (decisión enfoque)**: evaluar ImageSharp vs SkiaSharp vs SVG→PNG en .NET 10 (licencia, fuentes con acentos, tamaño de dependencia). Documentar elección en Dev Agent Record.
- [ ] **T2 — Compositor de tarjeta (AC-1, AC-3)**: servicio que compone el PNG 1200×630 (logo, nombre, ticker, sector, precio, yield) con fuentes de marca. Reusar lectura de datos vivos de 12-3 (`IMarketRepository` + `YieldCalculator`).
- [ ] **T3 — Endpoint + cache (AC-1, AC-4)**: `GET /og/fibras/{ticker}.png` en endpoints públicos; cache (memoria/disco) + `Cache-Control`; fallback a genérica en error/desconocido.
- [ ] **T4 — Wiring metadata (AC-2)**: apuntar `og:image`/`twitter:image` de fibra a la URL dinámica; coordinar con `SeoMetadata.OgImageUrl` de 12-1 (override gana).
- [ ] **T5 — Tests + validación (AC-5)**: unit compositor + integration endpoint; validar preview en Facebook Sharing Debugger / Twitter Card Validator en dev.

## Dev Notes
- **Stack real = SQL Server** (irrelevante; sin tablas nuevas, solo lectura de market + render de imagen).
- **No persistir el PNG en BD**: cachear en memoria/disco/CDN; el precio se recompone al regenerar (coherente con 12-3 "datos vivos no se guardan").
- **Coord. 12-3**: reusar exactamente la lectura de precio/yield (mismos repos y `YieldCalculator`) para que la tarjeta y el JSON-LD coincidan.
- **Coord. 12-1**: `OgImageUrl` es un campo administrable; si el AdminOps lo override con una imagen propia, NO regenerar. Default (no override) = URL dinámica.
- **Resiliencia primero**: una OG image rota es peor que la genérica. Fallback siempre.
- **Licencia de librería**: verificar (ImageSharp cambió a licencia comercial para ciertos usos; SkiaSharp es MIT). Documentar en spike.
- **Páginas fijas/fundamentales**: fuera de alcance de esta historia (solo fibras); se puede extender después.

### Security Checklist — antes del primer commit
- [ ] **TOCTOU / Auth-gating**: N/A (lectura, ruta pública).
- [ ] **DoS por render**: cachear agresivo; limitar render concurrente; ticker inválido no debe disparar render costoso (validar charset/longitud como en otros middlewares ≤256).
- [ ] **Denominador cero**: yield divide entre precio → si precio 0/null, omitir yield en la tarjeta (no excepción).

### References
- [FibraProfileMetadataMiddleware.cs:188](src/Server/Api/Middleware/FibraProfileMetadataMiddleware.cs) (og:image genérica actual), [NewsMetadataMiddleware.cs:216](src/Server/Api/Middleware/NewsMetadataMiddleware.cs) (noticias ya usan ImageUrl)
- [IMarketRepository.cs](src/Server/Application/Market/IMarketRepository.cs), [YieldCalculator.cs](src/Server/Application/Market/YieldCalculator.cs), [Fibra.cs](src/Server/Domain/Catalog/Fibra.cs)
- Story 12-1 (OgImageUrl administrable): [12-1-modulo-seo-administrable.md](_bmad-output/implementation-artifacts/12-1-modulo-seo-administrable.md); Story 12-3 (datos vivos): [12-3-datos-financieros-estructurados-ficha.md](_bmad-output/implementation-artifacts/12-3-datos-financieros-estructurados-ficha.md)
- 2026: [Meta Tags 2026 — webspidersolutions](https://webspidersolutions.com/what-are-meta-tags-seo-guide-marketers/)

## Hallazgos de auditoría SEO (2026-06-13)

> Auditoría completa (score 84/100): [seo-audit/FULL-AUDIT-REPORT.md](../../seo-audit/FULL-AUDIT-REPORT.md).

### 🟡 M4 — Las og:image de noticias apuntan a URLs externas de Google (extensión opcional)
La historia asume que "las noticias ya usan su propia `ImageUrl`" (correcto), pero la auditoría detectó que esas `ImageUrl` apuntan a **`lh3.googleusercontent.com/...`** (imágenes hospedadas por Google/fuente original). Riesgo: esas URLs **expiran o se rompen** (hotlink), dejando previews rotos en redes y tarjetas de IA.
- **Recomendación (extensión natural de esta historia):** proxear/cachear los thumbnails de noticias en el propio dominio (reusar el mismo mecanismo de cache/render que se construya para las OG de fibras), con fallback a la genérica si la fuente no está disponible. Si se mantiene fuera de alcance (la historia es solo fibras), **dejarlo documentado** como deuda para una historia futura — no es bloqueante pero degrada el preview social/GEO de las noticias.

## Dev Agent Record
### Agent Model Used
### Debug Log References
### Completion Notes List
### File List
