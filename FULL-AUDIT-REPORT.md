# Auditoría SEO Completa — fibrasinmobiliarias.com
**Fecha:** 12 de junio de 2026  
**Auditor:** FIBRADIS SEO Audit (Claude Code)  
**URL:** https://fibrasinmobiliarias.com/

---

## Resumen Ejecutivo

### Puntuación SEO General: **71 / 100**

| Categoría | Peso | Puntuación | Contribución |
|-----------|------|-----------|--------------|
| SEO Técnico | 22% | 72 | 15.8 |
| Calidad de Contenido | 23% | 75 | 17.3 |
| On-Page SEO | 20% | 82 | 16.4 |
| Schema / Datos Estructurados | 10% | 40 | 4.0 |
| Rendimiento (CWV) | 10% | 78 | 7.8 |
| Disponibilidad para IA | 10% | 25 | 2.5 |
| Imágenes | 5% | 70 | 3.5 |
| **TOTAL** | 100% | **71** | **67.3** |

### Tipo de negocio detectado
**Plataforma de análisis financiero / SaaS de información** — Fintech B2C con nicho en FIBRAs (REITs mexicanos). Público objetivo: inversores minoristas y analistas que operan en la BMV.

### Top 5 Problemas Críticos
1. `logo.png` **404** — rompe rich results de artículos en Google
2. **Meta descriptions contienen Markdown crudo** en páginas de perfil de FIBRAs
3. **Schema `description` contiene Markdown crudo** en `FinancialProduct`
4. **CLS 0.11 en móvil** — falla el umbral "Bueno" de Google (≤0.10)
5. **Todos los crawlers de IA bloqueados** — bloquea visibilidad en búsqueda generativa

### Top 5 Quick Wins
1. Subir `logo.png` (o actualizar URL en schema) → desbloquea rich results inmediatamente
2. Limpiar Markdown en meta descriptions y schema → mejora CTR y calidad de datos
3. Añadir schema a `/conoce-las-fibras` → página educativa sin markup
4. Crear `llms.txt` → señal positiva para IA search (5 min de trabajo)
5. Añadir `lastmod` a páginas estáticas del sitemap → mejor crawl budget

---

## 1. SEO Técnico

### 1.1 Crawlabilidad

| Check | Estado | Detalle |
|-------|--------|---------|
| robots.txt | ✅ | Presente y válido |
| Sitemap declarado en robots.txt | ✅ | `Sitemap: https://fibrasinmobiliarias.com/sitemap.xml` |
| /ops/ bloqueado | ✅ | `Disallow: /ops/` |
| /api/ bloqueado | ✅ | `Disallow: /api/` |
| /hangfire/ bloqueado | ✅ | `Disallow: /hangfire/` |
| HTTP → HTTPS redirect | ✅ | 301 correcto |
| www → non-www | ⚠️ | Doble redirect: `http://www` → `https://www` → `https://` (2 saltos) |
| Canonicals presentes | ✅ | Todas las páginas auditadas tienen `<link rel="canonical">` |

**Problema: Doble redirect en www**  
`http://www.fibrasinmobiliarias.com/` pasa por dos redirects 301 antes de llegar al destino final. Debería resolverse en un solo salto (`http://www` → `https://fibrasinmobiliarias.com/`).

### 1.2 Indexabilidad

| Check | Estado | Detalle |
|-------|--------|---------|
| `lang` en `<html>` | ✅ | `lang="es"` |
| Viewport meta | ✅ | `width=device-width, initial-scale=1.0` |
| Canonical correcto | ✅ | |
| SPA / JS rendering | ⚠️ | React SPA — el contenido es 100% JavaScript-rendered |

**Nota sobre SPA:** El servidor entrega HTML shells de ~3–4 KB con metadatos renderizados correctamente del lado servidor (títulos, descripciones, schema, canonicals por página). Google puede renderizar JS, pero introduce latencia en la indexación. Los metadatos SSR amortiguan el problema pero no lo eliminan.

### 1.3 Seguridad y Headers

| Header | Estado | Detalle |
|--------|--------|---------|
| HTTPS | ✅ | |
| X-Content-Type-Options | ✅ | `nosniff` |
| X-Frame-Options | ✅ | `DENY` |
| Referrer-Policy | ✅ | `strict-origin-when-cross-origin` |
| Permissions-Policy | ✅ | `camera=(), microphone=(), geolocation=()` |
| Content-Security-Policy | ❌ | **Ausente** |
| Strict-Transport-Security (HSTS) | ❌ | **Ausente** |
| X-Powered-By | ⚠️ | Expuesto: `ASP.NET` — divulga stack tecnológico |
| HTTP/3 (QUIC) | ✅ | `alt-svc: h3=":443"` |
| CDN Cloudflare | ✅ | |

### 1.4 Recursos Faltantes

| Archivo | Estado |
|---------|--------|
| `/favicon.ico` | ❌ 404 — solo existe `/favicon.svg` |
| `/logo.png` | ❌ **404 CRÍTICO** — referenciado en Organization schema y NewsArticle publisher |
| `/og-image.png` | ✅ 200 |
| `/llms.txt` | ❌ Ausente |

---

## 2. Calidad de Contenido

### 2.1 E-E-A-T (Experiencia, Expertise, Autoridad, Confianza)

**Señales positivas:**
- Página `/acerca` con información del proyecto
- Página `/contacto` con formulario
- Datos financieros en tiempo real (precios, distribuciones)
- Noticias de fuentes reconocidas (El Economista, Yahoo Finance, etc.)
- Schema `Organization` presente

**Señales débiles:**
- No se identifican autores individuales con expertise nombrado
- No hay biografías de equipo con credenciales financieras
- No hay menciones de reguladores (CNBV, BMV) que validen la información
- Las noticias son curadas/agregadas, no contenido editorial propio

### 2.2 Contenido Delgado (Thin Content)

Las páginas de perfil de FIBRA entregan HTML shells de ~4.3 KB. Todo el contenido de fundamentales, precios y distribuciones es JS-rendered. Googlebot necesita renderizar para ver el contenido real.

La página `/conoce-las-fibras` tiene título y descripción SEO bien optimizados pero es una single page app sin contenido visible en el HTML.

### 2.3 Duplicados

- Todas las páginas estáticas de herramientas comparten la misma `og-image.png` — no es duplicado de contenido, pero sí de imagen social.
- Los 11 artículos de noticias más recientes tienen URLs únicas y lastmod apropiados ✅.
- 3 artículos son de 2019 o anteriores (hasta 2016) — contenido potencialmente desactualizado.

### 2.4 Legibilidad

Diseño limpio y jerárquico. Tipografía: Playfair Display (títulos) + IBM Plex Sans (cuerpo) — excelente legibilidad financiera. Uso adecuado de tablas de datos y jerarquía visual.

---

## 3. On-Page SEO

### 3.1 Titles y Meta Descriptions

| Página | Título | Descripción | Estado |
|--------|--------|-------------|--------|
| `/` | "FIBRAs Inmobiliarias — Análisis y Herramientas \| FIBRADIS" | "Plataforma de análisis de FIBRAs inmobiliarias mexicanas..." | ✅ |
| `/fibras/fibra-uno-funo11` | "Fibra Uno (FUNO11) \| FIBRADIS — Fibras Inmobiliarias" | `# ?? Fibra Uno \| FUNO11\n\n> **Ticker:** FUNO11...` | ❌ **Markdown crudo** |
| `/noticias/sancionan-rentas...` | "Sancionan rentas de locales comerciales..." | Descripción correcta | ✅ |
| `/conoce-las-fibras` | "¿Qué son las FIBRAs Inmobiliarias? Guía Completa \| FIBRADIS" | "Aprende qué son las FIBRAs inmobiliarias..." | ✅ |

**Problema crítico en páginas de FIBRA:** La meta description se genera directamente desde el campo de descripción de la base de datos que contiene Markdown sin procesar. Google mostrará en los resultados: `# ?? Fibra Uno | FUNO11 > **Ticker:** FUNO11 > **Fecha de constitución:...` — esto destruye el CTR.

### 3.2 OpenGraph / Social

| Tag | Estado | Detalle |
|-----|--------|---------|
| og:title | ✅ | Presente en todas las páginas |
| og:description | ⚠️ | Mismo problema de Markdown en páginas FIBRA |
| og:type | ✅ | `website` |
| og:url | ✅ | |
| og:image | ⚠️ | Misma imagen genérica para todas las páginas FIBRA |
| twitter:card | ❌ | **Ausente** |
| twitter:site | ❌ | **Ausente** |
| twitter:creator | ❌ | **Ausente** |

### 3.3 Heading Structure

El sitio renderiza su estructura de headings vía JS. El snapshot visual muestra H1 claro: "El universo de FIBRAs del mercado mexicano." — estructura semántica correcta.

---

## 4. Schema / Datos Estructurados

### 4.1 Implementación Actual

| Página | Schema Types | Estado |
|--------|-------------|--------|
| `/` | `Organization`, `WebSite` | ✅ Presente |
| `/fibras/fibra-uno-funo11` | `FinancialProduct`, `BreadcrumbList` | ⚠️ Descripción con Markdown crudo |
| `/noticias/[slug]` | `NewsArticle` | ⚠️ `publisher.logo` referencia `logo.png` inexistente (404) |
| `/conoce-las-fibras` | — | ❌ **Sin schema** |
| `/comparar` | — | ❌ Sin schema |
| `/calculadora` | — | ❌ Sin schema |

### 4.2 Errores de Validación

**Error crítico — logo.png 404:**
```json
{
  "@type": "Organization",
  "logo": {
    "@type": "ImageObject",
    "url": "https://fibrasinmobiliarias.com/logo.png"  // ← 404
  }
}
```
Este error aparece en `Organization` (homepage) y en el `publisher` de todos los `NewsArticle`. Google requiere este logo para mostrar rich results de artículos de noticias.

**Error — Markdown en descripción de FinancialProduct:**
```json
{
  "@type": "FinancialProduct",
  "description": "# ?? Fibra Uno | FUNO11\n\n> **Ticker:** FUNO11\n..."  // ← Markdown crudo
}
```

### 4.3 Oportunidades de Schema Faltante

| Página | Schema Recomendado | Impacto |
|--------|-------------------|---------|
| `/conoce-las-fibras` | `FAQPage` o `Article` | Alto — contenido educativo rankeable |
| `/calculadora` | `WebApplication` | Medio |
| `/comparar` | `Table` o `ItemList` | Medio |
| `/noticias` (listado) | `CollectionPage` + `ItemList` | Medio |
| Páginas FIBRA | `FinancialProduct` con `offers`, `aggregateRating` | Medio (cuando aplique) |

---

## 5. Rendimiento — Core Web Vitals (Lab)

### 5.1 Scores Lighthouse

| Métrica | Mobile | Desktop | Umbral "Bueno" | Estado |
|---------|--------|---------|----------------|--------|
| Accessibility | 96 | 96 | — | ✅ |
| Best Practices | 100 | 100 | — | ✅ |
| SEO (Lighthouse) | 100 | 100 | — | ✅ |
| Agentic Browsing | 94 | 100 | — | ✅ |

### 5.2 Core Web Vitals

| Métrica | Mobile | Desktop | Umbral "Bueno" | Estado |
|---------|--------|---------|----------------|--------|
| CLS | **0.11** | 0.051 | ≤ 0.10 | ❌ Falla móvil |

*Nota: LCP, FID/INP y FCP requieren Performance Trace — no incluido en esta sesión. CLS es la única métrica CWV con datos cuantificados aquí.*

**CLS 0.11 en móvil** supera el umbral "Needs Improvement" (0.10–0.25), lo que puede perjudicar el ranking móvil. La causa probable son los skeleton loaders o el swap de web fonts (Playfair Display + IBM Plex Sans con `display=swap`).

### 5.3 Optimizaciones de Rendimiento Aplicadas (correctas)

- GTM con carga diferida via `requestIdleCallback` ✅
- Google Fonts con carga no bloqueante (`preload` + `onload`) ✅
- `preconnect` a `fonts.googleapis.com` y `fonts.gstatic.com` ✅
- HTTP/3 QUIC habilitado ✅
- Cloudflare CDN ✅

---

## 6. Disponibilidad para IA (AI Search Readiness)

### 6.1 Acceso de Crawlers IA

| Crawler | Estado en robots.txt |
|---------|---------------------|
| Googlebot | ✅ Permitido |
| Google-Extended (Gemini/AI Overviews) | ❌ **Bloqueado** |
| GPTBot (ChatGPT) | ❌ **Bloqueado** |
| ClaudeBot (Claude AI) | ❌ **Bloqueado** |
| Amazonbot (Alexa/Bedrock) | ❌ **Bloqueado** |
| CCBot (Common Crawl / entrenamiento) | ❌ Bloqueado (intencional) |
| Bytespider (TikTok) | ❌ Bloqueado (razonable) |

**Análisis:** Los bloqueos son gestionados por Cloudflare (`# BEGIN Cloudflare Managed content`). El bloqueo de CCBot y Bytespider es defensivo y razonable. Sin embargo, **Google-Extended, GPTBot y ClaudeBot** son los crawlers que alimentan **AI Overviews de Google, ChatGPT Browse y Claude**. Bloquearlos significa que FIBRADIS no aparecerá en respuestas de IA generativa cuando un usuario pregunte sobre FIBRAs mexicanas.

Para un sitio de análisis financiero especializado, aparecer en respuestas de IA es una oportunidad de posicionamiento de autoridad.

**Content-Signal en robots.txt:**
```
Content-Signal: search=yes,ai-train=no
```
`ai-train=no` está correcto (no queremos que entrenen con nuestros datos). Pero `ai-input` (para RAG / búsqueda generativa) no está declarado explícitamente, y los bloqueos de agentes sobreescriben esta señal.

### 6.2 Citabilidad por IA

| Factor | Estado |
|--------|--------|
| llms.txt | ❌ Ausente |
| Contenido factual con datos cuantificados | ✅ (precios, rendimientos, distribuciones) |
| Estructura de headings clara | ✅ (visual, aunque JS-rendered) |
| Autores nombrados con credenciales | ❌ |
| Datos con fuentes citadas | ⚠️ Parcial (noticias con fuente, datos propios sin fuente) |
| Schema Organization completo | ⚠️ (logo.png 404) |

---

## 7. Imágenes

| Check | Estado | Detalle |
|-------|--------|---------|
| og-image.png | ✅ Existe y accesible | |
| logo.png | ❌ **404** | Crítico para schema |
| favicon.svg | ✅ | Formato moderno |
| favicon.ico | ❌ 404 | Algunos clientes lo requieren |
| OG images por FIBRA | ❌ | Imagen genérica para todos los perfiles |
| Alt text en imágenes | ⚠️ | JS-rendered — no evaluable en HTML shell |

---

## 8. Sitemap

| Check | Estado | Detalle |
|-------|--------|---------|
| Sitemap presente | ✅ | `/sitemap.xml` |
| Declarado en robots.txt | ✅ | |
| Total de URLs | ✅ | 49 URLs |
| FIBRA profiles con lastmod | ✅ | 19 páginas con `lastmod: 2026-06-12` |
| Artículos de noticias con lastmod | ✅ | Fechas correctas |
| Páginas estáticas con lastmod | ❌ | 11 páginas sin `<lastmod>` |
| `<changefreq>` | ❌ | Ausente |
| `<priority>` | ❌ | Ausente |
| Sitemap de imágenes | ❌ | Ausente |
| Artículos muy antiguos | ⚠️ | Artículos desde 2016 y 2019 incluidos |

---

## Apéndice — Inventario de URLs auditadas

**Páginas estáticas (11):** `/`, `/fibras`, `/comparar`, `/noticias`, `/conoce-las-fibras`, `/calendario`, `/fundamentales`, `/herramientas`, `/calculadora`, `/acerca`, `/contacto`, `/privacidad`

**Perfiles FIBRA (17):** DANHOS13, EDUCA18, FCFE18, FHIPO14, FIBRAMQ12, FIBRAPL14, FIBRAUP18, FIHO12, FINN13, FMTY14, FNOVA17, FPLUS16, FSHOP13, FUNO11, HCITY17, NEXT25, SOMA21, STORAGE18, VESTA15

**Noticias (21):** 21 artículos con fechas de 2016 a 2026-06-11

---

*Generado por Claude Code SEO Audit — fibrasinmobiliarias.com — 2026-06-12*
