# Auditoría SEO Completa — fibrasinmobiliarias.com
**Fecha:** 18 de junio de 2026  
**Plataforma:** Análisis de FIBRAs inmobiliarias mexicanas  
**Stack:** React + Vite SPA · ASP.NET Core · Cloudflare CDN  
**Puntuación global: 57/100**

---

## Tabla de Contenidos

1. [Resumen Ejecutivo](#1-resumen-ejecutivo)
2. [SEO Técnico](#2-seo-técnico)
3. [Calidad de Contenido](#3-calidad-de-contenido)
4. [SEO On-Page](#4-seo-on-page)
5. [Schema y Datos Estructurados](#5-schema-y-datos-estructurados)
6. [Rendimiento y Core Web Vitals](#6-rendimiento-y-core-web-vitals)
7. [Visibilidad en Búsqueda con IA (GEO)](#7-visibilidad-en-búsqueda-con-ia-geo)
8. [Backlinks y Autoridad de Dominio](#8-backlinks-y-autoridad-de-dominio)
9. [Imágenes](#9-imágenes)
10. [Arquitectura de Contenido y Clusters Semánticos](#10-arquitectura-de-contenido-y-clusters-semánticos)
11. [Tabla de Puntuación por Categoría](#11-tabla-de-puntuación-por-categoría)

---

## 1. Resumen Ejecutivo

### Estado General

fibrasinmobiliarias.com es una plataforma técnicamente competente con infraestructura SEO sólida (middleware de metadata server-side, JSON-LD rico, sitemap 3 niveles, robots.txt con política de bots AI explícita) pero con brechas estructurales graves que impiden la visibilidad orgánica real. La plataforma ofrece datos en tiempo real superiores a los competidores directos, pero carece del envoltorio editorial que Google y los motores de búsqueda AI requieren para rankear y citar contenido financiero YMYL.

**Tráfico orgánico actual (GA4, 30 días):** 54 sesiones · 50 usuarios · 2.2 sesiones/día  
**GSC 90 días:** 46 clics · 2,116 impresiones · 2.17% CTR  
**Páginas indexadas confirmadas:** / (homepage), /calculadora — el resto en estado desconocido o no indexado

### Top 5 Problemas Críticos

| # | Problema | Impacto | Categoría |
|---|---|---|---|
| 1 | **Conflicto robots.txt Cloudflare vs. app:** el bloque gestionado por Cloudflare bloquea ClaudeBot, GPTBot, Google-Extended y Applebot-Extended con `Disallow: /` ANTES del bloque custom que los permite. Comportamiento ambiguo dependiente del parser del crawler. | Riesgo de bloqueo de bots AI y Google-Extended; elimina visibilidad en AI Overviews | Técnico |
| 2 | **SPA sin SSR — cuerpo HTML vacío:** el HTML raw de cualquier URL pesa ~200 bytes (`<div id="root"></div>`). LCP mobile de 6.3s. Todos los bots AI que no ejecutan JS (OAI-SearchBot, PerplexityBot, ClaudeBot) reciben contenido vacío. | LCP mobile 2.5x el umbral; sin texto extraíble para citas AI | CWV + GEO |
| 3 | **YMYL sin autoría visible:** plataforma de análisis financiero sin ningún autor nombrado, credenciales ni perfil editorial en ninguna página pública. Google QRG requiere señales de autoría explícitas para sitios financieros. | Umbral E-E-A-T no alcanzado para categoría YMYL | E-E-A-T |
| 4 | **Sitemap HTTP 403 a bots automatizados + GSC nunca registró el sitemap actual:** los sitemaps activos (sitemap.xml, sitemap-static.xml, sitemap-fibras.xml, sitemap-noticias-1.xml) nunca han sido enviados a Google Search Console. Solo están registradas dos URLs de `/xmlsitemap/` del 2023 con 0 URLs indexadas. Google descubre páginas por rastreo orgánico, no por sitemap. | /fibras "desconocida para Google"; 20 fichas de FIBRA probablemente no indexadas | Técnico |
| 5 | **Arquitectura SPA pura sin contenido editorial:** el sitio completo son herramientas interactivas sin ninguna página pillar con texto indexable. Las 20 fichas de FIBRA tienen campo `description` en NULL. No hay blog, no hay artículos propios, no hay texto diferenciador. Los competidores (fibrasmx.com, finantres.mx) dominan el 100% de las queries informacionales e investigativas del nicho. | Cero rankings para "qué son las fibras", "mejores fibras", "fibras vs CETES" | Contenido |

### Top 5 Quick Wins (impacto alto, esfuerzo bajo)

| # | Acción | Esfuerzo | Impacto estimado |
|---|---|---|---|
| 1 | **Enviar sitemap.xml a GSC** y eliminar las entradas antiguas de /xmlsitemap/. Acción cero coste en Google Search Console. | 15 minutos | Las 20 fichas de FIBRA y 13 páginas estáticas comenzarán a indexarse en 1-2 semanas |
| 2 | **Resolver conflicto robots.txt Cloudflare:** desactivar "Cloudflare Managed Robots.txt" en el panel Cloudflare (Security > Bots) para que solo sirva el `BuildRobotsTxt()` del backend. | 30 minutos | Elimina la ambigüedad de acceso para Google-Extended, ClaudeBot y GPTBot |
| 3 | **Corregir `lastmod` en sitemaps estáticos:** reemplazar `GetGeneratedLastMod()` (que siempre devuelve hoy) con la fecha real de `SeoMetadata.UpdatedAt` para rutas estáticas y `Fibra.UpdatedAt` para fichas. Cambio de ~20 líneas en `SeoEndpoints.cs`. | 1-2 horas | Reducción inmediata de señales falsas de frescura; mejor gestión de crawl budget |
| 4 | **Noindex + eliminación de noticias off-topic:** marcar con noindex via panel Ops los ~8+ artículos de fibra dietética, culturismo y fibra óptica. El filtro de noindex ya existe en `GetArticlesForSitemapPageAsync`. | 1 hora (datos) | Mejora inmediata de autoridad topical para queries financieras |
| 5 | **Agregar párrafo de autoría en /acerca + corregir link de footer:** (a) escribir 100-150 palabras con nombre del fundador/equipo, año 2023, experiencia en mercados mexicanos; (b) en `PublicLayout.tsx` línea 415 cambiar `to="/plataforma"` por `to="/"`. | 2 horas | Mayor impacto E-E-A-T disponible sin trabajo técnico; corrige distribución interna de PageRank |

---

## 2. SEO Técnico

**Puntuación: 62/100**

### 2.1 Rastreabilidad

#### Conflicto robots.txt — CRÍTICO

El archivo `robots.txt` en producción combina dos bloques independientes que se contradicen:

**Bloque 1 — Cloudflare Managed (inyectado por CDN, va primero):**
```
# BEGIN Cloudflare Managed content
User-agent: ClaudeBot
Disallow: /

User-agent: GPTBot
Disallow: /

User-agent: Google-Extended
Disallow: /

User-agent: Applebot-Extended
Disallow: /
# END Cloudflare Managed content
```

**Bloque 2 — App-level (`BuildRobotsTxt` en `SeoEndpoints.cs`, va después):**
```
User-agent: GPTBot
Allow: /

User-agent: ClaudeBot
Allow: /

User-agent: Google-Extended
Allow: /
```

La RFC 9309 establece que cuando múltiples reglas coinciden con el mismo user-agent, gana la más específica, no la última. En la práctica la mayoría de crawlers aplica el último bloque coincidente, por lo que el `Allow: /` prevalece — pero el comportamiento es dependiente de la implementación y constituye una deuda técnica de alto riesgo. Google-Extended (usado para AI Overviews) y ClaudeBot tienen las dos instrucciones contradictorias.

**Solución:** Desactivar "Cloudflare Managed Robots.txt" en Security > Bots del panel Cloudflare. El endpoint `BuildRobotsTxt()` ya es correcto y autosuficiente.

#### llms.txt bloqueado — ALTO

El archivo `/llms.txt` existe en `SeoEndpoints.cs` y sirve correctamente a nivel de aplicación, pero devuelve **HTTP 403** desde fetchers automatizados porque el WAF de Cloudflare bloquea la petición antes de que llegue al backend. Solución: añadir una regla de excepción en Cloudflare Firewall para la ruta `/llms.txt`.

#### OAI-SearchBot sin entrada explícita

El bot de OpenAI para búsqueda web (ChatGPT Search) no tiene entrada propia en `BuildRobotsTxt()`. Hereda el wildcard `Allow: /`, lo cual es funcional pero no explícito. Agregar:

```
User-agent: OAI-SearchBot
Allow: /

User-agent: PerplexityBot
Allow: /
```

### 2.2 Indexabilidad

| Check | Estado | Archivo / Línea |
|---|---|---|
| Canonical tags server-side en todas las páginas | PASA | `SpaMetadataMiddleware.cs` |
| Redirección www a non-www | PASA | `WwwToNonWwwMiddleware.cs` |
| HTTPS redirect con 301 | PASA | `Program.cs` |
| Redirección slugs no-canónicos /fibras/ | PASA | `FibraSlugRedirectMiddleware.cs` |
| Noindex en /noticias?page=2+ | PASA | `SpaMetadataMiddleware.cs` |
| `lastmod` en sitemap estático y de FIBRAs | FALLA | `SeoEndpoints.cs` — `GetGeneratedLastMod()` siempre devuelve hoy |
| Sitemap.xml registrado en GSC | FALLA | Solo legacy /xmlsitemap/ del 2023 con 0 URLs indexadas |
| /fibras indexada | FALLA | GSC: "URL is unknown to Google" |
| Fragmentación de URLs (www vs https vs http) | FALLA | GSC muestra tráfico dividido en 4 variantes |

**Fragmentación de URL — Acción urgente:** Las queries más buscadas (ej. "lista de fibras en Mexico" con 115 impresiones) aterrizan en `http://www.fibrasinmobiliarias.com/fibras/` — la variante www sin SSL — no en la canónica `https://fibrasinmobiliarias.com/fibras`. Implementar forzado a nivel Cloudflare: regla `http://* → https://` y `www.* → non-www` para consolidar toda la equidad de enlace.

### 2.3 Headers de Seguridad

| Header | Valor | Estado |
|---|---|---|
| X-Content-Type-Options | nosniff | PASA |
| X-Frame-Options | DENY | PASA |
| Referrer-Policy | strict-origin-when-cross-origin | PASA |
| HSTS | max-age=31536000; includeSubDomains | PASA |
| Server header | suprimido | PASA |
| Content-Security-Policy | AUSENTE | FALTA — agregar en modo report-only como primer paso |

### 2.4 Estructura de URLs

- Slugs limpios y semánticamente ricos: `/fibras/fibra-uno-funo11`, `/noticias/{articulo-slug}`
- Formato `{nombre-slugificado}-{ticker-lowercase}` garantiza unicidad y resolución server-side
- **Problema:** sufijos de fuente en slugs de noticias (`-yahoo`, `-el-economista`, `-infobae`) reducen densidad de keywords y exponen el origen de agregación de datos

### 2.5 Arquitectura de Sitemap

| Archivo | URLs | Estado |
|---|---|---|
| /sitemap.xml | índice (3 hijos) | HTTP 403 a bots automatizados |
| /sitemap-static.xml | 13 rutas estáticas | HTTP 403 a bots automatizados |
| /sitemap-fibras.xml | 20 FIBRAs activas | HTTP 403 a bots automatizados |
| /sitemap-noticias-1.xml | 160 artículos | HTTP 200 — accesible |

**Problemas prioritarios:**
1. `lastmod` siempre igual a hoy en static y fibras — señal falsa de cambio diario en `GetGeneratedLastMod()` de `SeoEndpoints.cs`
2. Artículos off-topic en sitemap-noticias: al menos 8 URLs confirmadas sobre fibra dietética, culturismo, fibra óptica
3. Sitemap de noticias sin namespace Google News (`xmlns:news`) — Google News no puede procesar metadata de publicación
4. Código muerto `BuildSitemapXml()` en `SeoEndpoints.cs` — método inalcanzable que genera confusión de mantenimiento

### 2.6 Implementación JavaScript

El sitio usa renderizado híbrido:

**Servidor (antes de ejecutar JS):**
- `<title>`, `<meta name="description">`, `<link rel="canonical">`
- Todos los OG y Twitter card tags
- Todos los bloques JSON-LD de Schema

**Requiere JavaScript:**
- Todo el contenido visible del cuerpo (tablas, gráficos, datos de precio)
- El H1 de la homepage
- Menús de navegación
- Todos los outputs de herramientas interactivas (/comparar, /calculadora)

El módulo `entry-server.tsx` (SSR con `renderToString` + React Query dehydration) existe en el código pero no está conectado al pipeline de producción — es código muerto que debe activarse o eliminarse.

### 2.7 Recomendaciones Técnicas — Priorizadas

**Semana 1 (crítico):**
1. Desactivar Cloudflare Managed Robots.txt
2. Añadir regla WAF Cloudflare para permitir fetchers legítimos en /llms.txt y /sitemap*.xml
3. Enviar https://fibrasinmobiliarias.com/sitemap.xml a GSC; eliminar /xmlsitemap/ legacy
4. Forzar https + non-www en Cloudflare para consolidar variantes de URL
5. Solicitar indexación de /fibras via GSC URL Inspection (actualmente "unknown to Google")

**Mes 1:**
6. Corregir `lastmod` en `SeoEndpoints.cs`: usar `SeoMetadata.UpdatedAt` para estáticas y `Fibra.UpdatedAt` para fichas (~20 líneas de código)
7. Noindex de artículos off-topic via panel Ops (no requiere código)
8. Agregar namespace Google News a sitemap-noticias-1.xml
9. Corregir link de footer en `PublicLayout.tsx` línea 415: `to="/plataforma"` a `to="/"`
10. Agregar OAI-SearchBot y PerplexityBot explícitamente en `BuildRobotsTxt()`
11. Implementar IndexNow para eventos de publicación de noticias

**Trimestre 1:**
12. Activar o eliminar `entry-server.tsx`
13. Eliminar método `BuildSitemapXml()` inalcanzable de `SeoEndpoints.cs`
14. Migrar contacto de Gmail a correo corporativo @fibrasinmobiliarias.com
15. Agregar Content-Security-Policy en modo report-only

---

## 3. Calidad de Contenido

**Puntuación: 54/100**

### 3.1 Evaluación E-E-A-T para YMYL Financiero

#### Experience (Experiencia) — 8/20

La plataforma genera datos verificables en tiempo real (precios BMV, distribuciones, fundamentales trimestrales) pero carece de demostración de experiencia de primera mano:

- El campo `fibra.description` en `FibraPage.tsx` solo renderiza si `fibra!.description` es truthy — actualmente NULL para las 20 FIBRAs activas en `CatalogSeed.cs`
- Sin casos de uso propios, backtesting, ni análisis histórico narrativo
- Sin descripciones editoriales para ninguna de las 20 emisoras

#### Expertise (Pericia) — 14/25

**Señales presentes:** Metodología publicada en /acerca con fórmulas específicas (Cap Rate, NAV, NOI Margin, LTV, Yield TTM); tabla de fuentes (reporte trimestral, balance, precio BMV); Score de oportunidad explicado con 4 dimensiones; terminología mexicana correcta (CBFI, ex derecho, fideicomisario, CNBV).

**Señales débiles:** Disclaimer genérico sin referencia al marco CNBV específico; FAQs explican el uso de la interfaz, no la interpretación del dominio.

#### Authoritativeness (Autoridad) — 10/25

**Señales presentes:** Schema Organization + FinancialService en homepage; cobertura completa del universo BMV/BIVA con 20 FIBRAs; fuentes de noticias reconocidas.

**Señales ausentes:** Cero backlinks auditables desde fuentes financieras reconocidas; Schema `sameAs` en Organization sin URLs de redes sociales verificables; ninguna afiliación a AMIB, AMEFIBRA o BMV.

#### Trustworthiness (Confiabilidad) — 22/30

**Señales presentes:** HTTPS canónico en todas las páginas; disclaimer de no-asesoría explícito; política de privacidad; fuentes primarias citadas (BMV, CNBV, reportes trimestrales); declaración de no-compensación por emisoras.

**Señales débiles:** Email Gmail (@gmail.com) reduce credibilidad institucional para sitio YMYL financiero; análisis IA en artículos sin disclaimer por artículo; sin fecha de última actualización en /privacidad.

### 3.2 Análisis por Tipo de Página

#### Homepage (/) — Gap editorial

- Contenido editorial indexable sin JS: ~35 palabras
- Función: dashboard de datos puro, correcto para usuarios recurrentes
- Gap: sin introducción que explique qué son las FIBRAs para nuevos usuarios orgánicos
- El H1 "El universo de FIBRAs del mercado mexicano." describe la herramienta, no introduce el concepto para el buscador que no sabe qué es una FIBRA

#### /fibras/:ticker — 20 páginas con thin content (CRÍTICO)

- `fibra.description` NULL en las 20 fichas activas (confirmado en `CatalogSeed.cs`)
- La sección "Descripción" en `FibraPage.tsx` solo se renderiza si `fibra!.description` es truthy — actualmente nunca se muestra
- FAQPage por fibra tampoco sembrado
- Contenido editorial por ficha: 0 palabras (solo datos tabulares y numéricos)
- Riesgo: Google puede clasificar las 20 fichas como thin content programático sin diferenciador editorial

#### /conoce-las-fibras — Contenido valioso invisible a crawlers

- Contiene las 5 tabs educativas de mayor valor (qué son, historia, estructuración, por qué invertir, régimen fiscal)
- El contenido es Markdown servido por API y renderizado con ReactMarkdown — completamente invisible para crawlers sin JS
- El middleware solo inyecta title/meta en el shell; el Markdown no se serializa en el HTML estático

#### /comparar, /calculadora, /calendario — Herramientas sin contexto editorial

- Contenido indexable total: H1 + párrafo introductorio (~30-60 palabras) + FAQ 3 ítems (~150-200 palabras)
- /calendario en particular carece de texto que explique por qué importan las distribuciones, cómo leer fechas ex derecho, o implicaciones fiscales del calendario para un inversor

#### /noticias/:slug — Área de mayor madurez del sitio

- Estructura citation-ready: keyFacts + keyFigures + análisis IA + investorTakeaway
- Los `keyFigures` con label y valueText son exactamente el tipo de dato estructurado que un LLM citaría
- Gap: análisis IA sin badge de "contenido generado por IA" por artículo (solo disclaimer genérico en /acerca)

### 3.3 Prioridades de Contenido

1. **E-E-A-T financiero (máximo impacto):** agregar sección de autoría en /acerca con nombre del fundador/equipo, experiencia relevante y año de fundación. Sin cambios técnicos — solo redacción. ROI más alto disponible.
2. **Descripciones editoriales para FIBRAs principales:** sembrar 150-250 palabras para FUNO11, DANHOS13, FIBRAMQ12, FIBRAPL14, FMTY14 en `CatalogSeed.cs` o via panel Ops.
3. **Filtro anti-off-topic en pipeline de noticias:** rechazar artículos sin al menos un ticker de FIBRA activa vinculado en el análisis AI.
4. **NewsArticle JSON-LD con author:** agregar en `NewsMetadataMiddleware` para habilitar Google News eligibility.
5. **Badge de IA por artículo:** chip "Análisis generado por IA" en `NoticiaPage.tsx` cuando `aiAnalysis` existe.

---

## 4. SEO On-Page

### 4.1 Titles y Meta Descriptions

| Página | Title | Longitud | Estado |
|---|---|---|---|
| / | FIBRAs Inmobiliarias — Análisis y Herramientas \| Fibras Inmobiliarias | 68 chars | PASA |
| /fibras | FIBRAs Inmobiliarias Mexicanas — Catálogo Completo \| Fibras Inmobiliarias | 72 chars | PASA |
| /fibras/:slug | {ticker} — {fullName} \| Fibras Inmobiliarias | dinámico | PASA |
| /noticias/:slug | {headline} — Noticias \| Fibras Inmobiliarias | dinámico | PASA |

Meta descriptions: todas presentes, longitud controlada (120-160 chars) en `NewsMetadataMiddleware`. No se detecta keyword stuffing.

### 4.2 Estructura de Headings

- **H1 en homepage:** renderizado por React (no en HTML estático). Texto correcto pero invisible para crawlers sin JS.
- **H2 en homepage:** todos con clase `sr-only` — "Ganadores y perdedores del día", "Universo FIBRAS", "Noticias recientes". Crawlers los ven pero usuarios no. Reduce escaneabilidad visual.
- **H1 en fichas /fibras/:slug:** `sr-only` — solo accesible para lectores de pantalla y crawlers.

### 4.3 OG Tags y Twitter Cards

- OG tags completos y correctos para todos los tipos de página
- Fichas de FIBRA: imágenes OG dinámicas por ticker via `/og/fibras/{TICKER}.png` (confirmado en `FibraProfileMetadataMiddleware.cs` línea 161)
- Artículos de noticias: fallback a `/og-image.png` genérico cuando `article.ImageUrl` es null
- Twitter/X Cards: `summary_large_image` en todos los tipos. Handle `@fibrasinmobiliarias` configurado.
- Código muerto: overload legacy de `BuildMetaBlock` en `FibraProfileMetadataMiddleware.cs` línea 251 referencia `/og-image.png` directamente, bypaseando el endpoint dinámico

### 4.4 Atributo lang

`html lang="es-MX"` en index.html — correcto para audiencia mexicana de inversores.

### 4.5 Enlazado Interno

**Señales positivas:**
- Price carousel en header enlaza a las 20 fichas de FIBRA en cada carga de página
- FibrasRelacionadasSection en fichas individuales enlaza a peers del mismo sector
- BreadcrumbList en fichas y artículos de noticias

**Gaps críticos:**
- Link de footer apunta a `/plataforma` en lugar de `/` (`PublicLayout.tsx` línea 415) — distribuye PageRank a /plataforma en lugar de la homepage
- Sin enlace en la navegación principal a `/conoce-las-fibras`
- /acerca y /contacto solo enlazados desde footer — baja autoridad interna
- Las herramientas (/calculadora, /comparar, /calendario) no enlazan a contenido editorial relacionado

---

## 5. Schema y Datos Estructurados

**Puntuación: 78/100** (el área más madura del sitio)

### 5.1 Inventario de Schema Implementado

| Página | Tipos Schema | Inyección | Calidad |
|---|---|---|---|
| / | Organization + WebSite + FinancialService | Server-side | Buena — falta `sameAs` poblado |
| /fibras | BreadcrumbList + FAQPage | Server-side | Buena |
| /fibras/:slug | FinancialProduct + BreadcrumbList + FAQPage (opcional) | Server-side | Excelente — precio, yield TTM, variación 52s como PropertyValue |
| /fundamentales | Dataset + ItemList | Server-side | Excelente — variableMeasured con 6 KPIs |
| /comparar | WebApplication + BreadcrumbList + FAQPage | Server-side | Buena |
| /calculadora | SoftwareApplication + BreadcrumbList | Server-side | Aceptable |
| /noticias/:slug | NewsArticle + BreadcrumbList | Server-side | Buena — falta `dateModified` e `image` como ImageObject |
| /conoce-las-fibras | Article + BreadcrumbList + FAQPage | Server-side | Presente — `author: Organization` debilita E-E-A-T |
| /acerca | AboutPage | Server-side | Presente |
| /contacto | ContactPage + ContactPoint | Server-side | Presente |

### 5.2 Gaps de Schema

1. **Ausencia de schema Person para autor/analista:** ninguna página tiene schema de tipo `Person`. Para YMYL financiero, la ausencia de un autor humano identificable es el gap de schema más importante.

2. **ItemList ausente en /fibras:** el catálogo de 20 FIBRAs no tiene markup `ItemList`, siendo un activo indexable clave.

3. **Organization sin `sameAs` poblado:** el schema en la homepage no incluye URLs de redes sociales verificables. Google no puede confirmar la identidad de la entidad en el grafo de conocimiento.

4. **author: Organization en /conoce-las-fibras:** el Article tiene `author` con tipo Organization en lugar de Person. Google AIO favorece fuertemente artículos con autor humano identificable.

5. **NewsArticle incompleto:** falta `dateModified`, `image` con dimensiones (width/height) y `articleSection` para plena elegibilidad de Google News.

### 5.3 Calidad del Schema FAQPage — Crítico para AI

Los FAQs en JSON-LD están correctamente inyectados server-side. El problema es exclusivamente de contenido:

| FAQ | Palabras actuales | Objetivo citabilidad AI | Brecha |
|---|---|---|---|
| Columnas del universo de FIBRAs | 45 | 134-167 | -67% |
| Ganadores y Perdedores del día | 42 | 134-167 | -69% |
| Yield del catálogo | 46 | 134-167 | -66% |
| LTV (Loan-to-Value) | 21 | 134-167 | -84% |
| Fecha ex derecho | 57 | 134-167 | -57% |

**Archivo a modificar:** `src/Server/Application/Seo/FaqSeedFactory.cs`. Cada respuesta debe incluir: definición completa, fórmula si aplica, ejemplo numérico con datos reales de FIBRAs mexicanas, contexto de cuándo usar la métrica, y referencia a dónde encontrar el dato en la plataforma. Esfuerzo: 3-4 horas. Impacto inmediato en citabilidad por todos los motores AI.

### 5.4 Validación Rich Results

- **Breadcrumbs en /calculadora:** detectados por GSC como PASA
- **FAQPage:** técnicamente correcto pero contenido insuficiente para activar el rich result
- **FinancialProduct en fichas:** schema excelente pero las páginas están mayormente no indexadas por el conflicto de sitemap

---

## 6. Rendimiento y Core Web Vitals

**Puntuación: 52/100**

### 6.1 Datos de Laboratorio (PSI Lighthouse 13)

| Métrica | Mobile (lab) | Desktop (lab) | Umbral "Bueno" | Estado |
|---|---|---|---|---|
| **LCP** | **6.3s** | 1.1s | ≤2.5s | CRÍTICO mobile / PASA desktop |
| **FCP** | **5.1s** | 0.8s | ≤1.8s | POBRE mobile / PASA desktop |
| CLS | 0.0 | 0.053 | ≤0.1 | PASA ambos |
| TBT (proxy INP) | 1,100ms | 60ms | ≤200ms INP | POBRE mobile / PASA desktop |
| TTI | 6.4s | 1.4s | — | Referencia |
| **Lighthouse Performance** | **49/100** | 97/100 | — | Abismo mobile/desktop |
| Lighthouse SEO | 100/100 | 100/100 | — | Excelente |

**Datos CrUX:** No disponibles — tráfico orgánico insuficiente para el umbral de 28 días de Chrome.  
**TTFB:** 1ms — excepcional. Cloudflare CDN funcionando correctamente.

### 6.2 Causa Raíz del LCP de 6.3s — Cadena de Dependencias JS

```
0ms        HTML shell solicitado
232ms      HTML recibido (3.2 KB transferidos — solo <div id="root"></div>)
246ms      Inicio de descarga paralela:
           index-DRgiVaWg.js (108 KB transferidos / 377 KB sin comprimir)
           vendor-charts-COY5DSSL.js (102 KB / 373 KB) — modulepreload INNECESARIO en homepage
           index-ak8h6ly1.css (18.5 KB) — BLOQUEANTE
~2866ms    Long task: parse/eval de index.js (94ms)
~4979ms    Long task: procesamiento respuesta /metrics/ (68ms)
~5142ms    FCP — primera pintura
~6348ms    LCP — H1 del hero painted por React
~6423ms    Long task: Cloudflare RUM metrics script (128ms)
```

La causa raíz es la ausencia de SSR o prerender: el H1 no existe en el HTML hasta que React monta el componente.

### 6.3 Bundle Analysis

| Bundle | Transferido | Código no usado | Impacto en homepage |
|---|---|---|---|
| index-DRgiVaWg.js | 108 KB | 49 KB (45%) | Entry bundle bloqueante; `HomePage` importada estáticamente |
| vendor-charts-COY5DSSL.js | 102 KB | 82 KB (81%) | modulepreload innecesario — Recharts no se usa en homepage |
| GTM (gtm.js) | 119 KB | 88 KB (74%) | Diferido con `requestIdleCallback` — compite con LCP en mobile |
| Cloudflare RUM (metrics/) | 170 KB | 72 KB (43%) | Long task más costoso: 128ms a t=6.4s en mobile |

**Total JS transferido:** 686 KB en 9 requests. **Total página:** 851 KB en 44 requests.

**Problema clave en `routes.tsx`:** `HomePage` es el único componente importado estáticamente junto a los layouts. Todas las demás rutas usan `lazy()`. Esto incluye en el bundle de entrada el código de GainersLosers, FibraUniverseTable, NewsSection y sus dependencias transitivas — incluyendo Recharts.

### 6.4 CSS Render-Blocking

`index-ak8h6ly1.css` (18.5 KB) se sirve como stylesheet bloqueante en el `<head>`:
- **wastedMs mobile: 337ms** — contribuye al FCP de 5.1s
- **wastedMs desktop: 135ms**

El CSS crítico above-the-fold para el hero H1, header y layout base es probablemente menor a 4 KB.

### 6.5 Recomendaciones CWV — Priorizadas por Impacto

**P0 — Crítico para LCP mobile:**

1. **Hacer `HomePage` lazy en `routes.tsx`:**
   ```tsx
   const HomePage = lazy(() => import('@/modules/home/HomePage').then(m => ({ default: m.HomePage })))
   ```
   Estimado: -30 a -50 KB del bundle inicial, -0.5s LCP mobile.

2. **Identificar y romper la dependencia estática de Recharts en el critical path:**
   Ejecutar `npx vite-bundle-visualizer` para confirmar qué componente de `FibraUniverseTable` o `GainersLosers` importa Recharts. Envolver ese componente chart en `React.lazy()`.
   Estimado: -82 KB en critical path, -0.8s LCP mobile.

3. **Inline del CSS crítico above-the-fold:**
   Extraer ~2-4 KB de reglas críticas (header, H1, layout base) a un `<style>` inline. Diferir el resto de `index-ak8h6ly1.css` con `media="print"` + onload swap.
   Alternativa: `vite-plugin-critical`. Estimado: -337ms FCP/LCP mobile.

**P1 — Impacto alto, complejidad media:**

4. **Self-hosting de Google Fonts:** descargar archivos woff2 de Playfair Display + IBM Plex Sans con https://google-webfonts-helper.herokuapp.com/ y servir desde `/assets/fonts/`. Elimina conexiones a 2 dominios externos. Estimado: -150ms en conexiones lentas.

5. **Retrasar GTM al evento `load`:** `window.addEventListener('load', () => setTimeout(loadGTM, 0))` garantiza que GTM carga después de todos los recursos críticos, evitando competencia con el LCP en mobile.

**P2 — Impacto medio, complejidad baja:**

6. Aumentar Cache-Control de Cloudflare beacon.min.js de 1 día a 30 días via regla CDN.

---

## 7. Visibilidad en Búsqueda con IA (GEO)

**Puntuación: 46/100**

### 7.1 Estado por Plataforma AI

| Plataforma | Score Estimado | Barrera Principal |
|---|---|---|
| Google AI Overviews | 35/100 | SPA + FAQ muy cortos + E-E-A-T débil |
| ChatGPT Search (OAI-SearchBot) | 28/100 | No renderiza JS + sin sameAs verificable |
| Perplexity | 32/100 | No renderiza JS + buenas señales de metadatos |
| Bing Copilot | 40/100 | Renderizado parcial JS + robots.txt correcto |

### 7.2 Problema Central: SPA sin SSR

Los AI crawlers que no ejecutan JavaScript reciben:
```
GET https://fibrasinmobiliarias.com/fibras
raw_content: 203 bytes
extracted_text: "" (vacío — Trafilatura no extrae nada útil)
```

El middleware `SpaMetadataMiddleware` inyecta correctamente el `<head>` con title, meta description, canonical y JSON-LD — esto es lo único accesible sin JS. Todo el cuerpo visible (tablas, precios, FAQs renderizadas, contenido educativo de /conoce-las-fibras) requiere JavaScript.

### 7.3 Longitud de Respuestas FAQ — Crítico para Citabilidad AI

El rango óptimo de pasajes para cita AI es 134-167 palabras. Los 9 FAQs auditados tienen entre 21 y 57 palabras — promedio de 41 palabras, el 31% del mínimo recomendado. Ningún FAQ actual puede ser citado directamente como respuesta de calidad.

**Archivo:** `src/Server/Application/Seo/FaqSeedFactory.cs` — `BuildStaticPagesItems()` y `BuildFundamentalsItems()`. Esfuerzo: 3-4 horas. Impacto inmediato.

### 7.4 Estado de llms.txt

- HTTP 403 desde fetchers automatizados (WAF Cloudflare)
- `Cache-Control: max-age=1` — efectivamente sin caché (estándar: `max-age=86400`)
- Generado dinámicamente en `SeoEndpoints.cs`

**Fortalezas:** tabla de 19 FIBRAs con ticker, nombre y sector; definición de FIBRAs como equivalente mexicano de REITs; 9 páginas principales listadas.

**Debilidades:** 6 rutas ausentes (/portafolio, /acerca, /privacidad, /contacto, /calendario, fichas individuales); TERRA13 ausente en la tabla (el catálogo tiene 20 FIBRAs activas, no 19 declaradas).

### 7.5 Estado del robots.txt para AI Crawlers

| Crawler | Estado real | Observación |
|---|---|---|
| GPTBot | PERMITIDO (last-match wins) | Conflicto Cloudflare vs. app — riesgo residual |
| OAI-SearchBot | SIN ENTRADA EXPLÍCITA | Hereda wildcard — funcional pero no explícito |
| ClaudeBot | PERMITIDO (last-match wins) | Mismo conflicto que GPTBot |
| Google-Extended | PERMITIDO (last-match wins) | Controla AI Overviews — conflicto debe resolverse |
| PerplexityBot | SIN ENTRADA | Hereda wildcard |
| CCBot / Bytespider / meta-externalagent | BLOQUEADOS | Consistente en ambos bloques — correcto |

### 7.6 Recomendaciones GEO — Priorizadas

1. **Expandir FAQs a 134-167 palabras** en `FaqSeedFactory.cs`: impacto inmediato en citabilidad. Esfuerzo: 3-4 horas.
2. **Cambiar `max-age=1` a `max-age=86400`** en endpoint `/llms.txt` (`SeoEndpoints.cs` línea ~188): 5 minutos.
3. **Desbloquear /llms.txt en Cloudflare WAF**: regla de excepción para la ruta.
4. **Agregar OAI-SearchBot y PerplexityBot** en `BuildRobotsTxt()`: 10 minutos.
5. **Ampliar llms.txt:** agregar las 6 rutas faltantes y las 20 fichas individuales. Esfuerzo: 1 hora.
6. **Poblar `sameAs`** desde panel Ops con LinkedIn, Twitter/X y Wikidata. Esfuerzo: 30 minutos.
7. **Migrar `author: Organization` a `author: Person`** en Article schema de /conoce-las-fibras.
8. **Implementar SSR para páginas informacionales** (/conoce-las-fibras, fichas de FIBRA con datos básicos estáticos). Sin esto, ChatGPT y Perplexity nunca extraen contenido citable. Esfuerzo: 2-3 semanas.

---

## 8. Backlinks y Autoridad de Dominio

**Puntuación: 25/100** (estimado — sin datos Moz/Ahrefs directos)

### 8.1 Estado Actual

- **Backlinks verificados:** referidos desde Reddit (`reddit.com/best/communities/682/` y `/693/`) — descubiertos en referring URLs de GSC
- **Backlinks desde fuentes financieras mexicanas:** cero confirmados
- **Domain Rating estimado:** bajo — dominio fundado en 2023 sin presencia de prensa
- **Brand query "fibras inmobiliarias":** posición 21.7, 1 clic de 71 impresiones (1.41% CTR) — el sitio no rankea en página 1 para su propio nombre de marca

### 8.2 Contexto Competitivo de Autoridad

| Competidor | Perfil de Autoridad |
|---|---|
| finantres.mx | Años de contenido financiero, backlinks de bbva.mx, gbm.com, medios nacionales |
| fibrasmx.com | Analista nombrado, artículos semanales, backlinks desde emisoras |
| rankia.mx | Plataforma financiera establecida, backlinks institucionales |
| bbva.mx/educacion | Autoridad de banco (DA estimado >80) |

### 8.3 Estrategia de Link Building

La plataforma tiene un activo único que los competidores no pueden replicar: datos en tiempo real superiores (comparador, calendario de distribuciones, fundamentales trimestrales, portfolio tracker). La estrategia debe convertir esos datos en recursos citables:

1. **Comunicados de datos con análisis original:** publicar análisis trimestrales de resultados de FIBRAs con datos exclusivos. Las publicaciones financieras (El Financiero, El Economista) citan fuentes de datos originales.
2. **Widgets de precio embebibles:** desarrollar widgets de precio en tiempo real con backlink a la ficha de FIBRA, para integración en blogs financieros.
3. **Relaciones con AMEFIBRA y BMV:** participar en foros o publicaciones de la Asociación Mexicana de FIBRAs.
4. **FAQs expandidas a 134-167 palabras:** aumentan la probabilidad de cita en Google AIO, lo que genera tráfico de marca indirectamente.
5. **Infografías de distribuciones:** visualizaciones del calendario o comparativas de yield son altamente compartibles en comunidades de inversores.

---

## 9. Imágenes

**Puntuación: 55/100**

### 9.1 OG Images

- **Homepage y páginas genéricas:** imagen estática `/og-image.png` (1200x630) — una imagen para todas las páginas no diferenciadas
- **Fichas de FIBRA:** endpoint dinámico `/og/fibras/{TICKER}.png` implementado en `FibraProfileMetadataMiddleware.cs` línea 161 — excelente diferenciación por ticker
- **Artículos de noticias:** `article.ImageUrl ?? /og-image.png` — fallback correcto pero la imagen genérica reduce CTR en redes sociales
- **Código muerto:** overload legacy de `BuildMetaBlock` en línea 251 referencia `/og-image.png` directamente, bypaseando el endpoint dinámico. Confirmar que no se alcanza en producción.

### 9.2 Oportunidades

1. **Image sitemap:** agregar namespace `image:` al sitemap de fichas de FIBRA con la URL de la OG image dinámica — mejora indexación de imágenes de Google.
2. **Alt text:** confirmar que imágenes en componentes React tienen `alt` descriptivo. Puntuación de Accessibility es 96/100 — probable que esté bien pero debe verificarse en fichas.
3. **Infografías educativas:** una infografía sobre "cómo funcionan las distribuciones de FIBRAs" o "diferencia entre fibra industrial y comercial" sería altamente enlazable.
4. **No hay imagen hero en homepage** — el LCP candidate es el H1 de texto, lo cual es positivo (no hay imagen LCP que optimizar) pero elimina impacto visual para nuevos usuarios.

---

## 10. Arquitectura de Contenido y Clusters Semánticos

**Puntuación: 34/100** — el área de menor madurez del sitio

### 10.1 Estado Actual

El sitio completo son herramientas interactivas SPA sin arquitectura de contenido editorial. El sitio no rankea para ninguna de las queries informacionales o investigativas del mercado de FIBRAs:

| Query de alto volumen | fibrasinmobiliarias.com | fibrasmx.com | finantres.mx |
|---|---|---|---|
| "qué son las fibras inmobiliarias" | No rankea | Rankea | Rankea |
| "cómo invertir en fibras" | No rankea | Rankea | Rankea |
| "mejores fibras para invertir 2026" | No rankea | **Posición #1** | No rankea |
| "fibras vs cetes" | No rankea | Rankea (con calculadora) | No rankea |
| "distribuciones fibras cuándo pagan" | No rankea | Rankea | No rankea |
| "calculadora de fibras" | **Posición 6.8** | — | — |
| "lista de fibras en Mexico" | **Posición 4.8** (www) | — | — |

Los únicos rankings del sitio son para queries de herramientas directas (/calculadora, /fibras catálogo) y ticker navigacionals. Cero presencia en queries informacionales o investigativas de alto volumen.

### 10.2 Análisis Competitivo — Por qué fibrasmx.com gana

fibrasmx.com combina lo que fibrasinmobiliarias.com tiene por separado:
- Tipo de página híbrida (educación + herramienta) — alineado con SERP intent
- Analista nombrado (Misael) con artículos semanales y byline
- Calculadora FIBRA vs CETES dedicada — intercepta el cluster de comparación
- CTA "Crear cuenta gratis" above the fold — captura conversión awareness
- Fuentes declaradas explícitamente: "BMV y CNBV"

**Posición óptima para fibrasinmobiliarias.com:** sitio Híbrido con datos superiores en tiempo real + editorial que explique el valor de esos datos. La ventaja competitiva real son los datos — pero los datos sin contexto editorial no rankean.

### 10.3 Arquitectura de Clusters Propuesta

#### Cluster 1 — Fundamentos de las FIBRAs (Informacional)

**Pillar:** `/conoce-las-fibras` (URL ya existe en sitemap — PRIORIDAD MÁXIMA)
- Keyword primaria: "qué son las fibras inmobiliarias"
- Requiere: expandir a 3,000-3,500 palabras con H2 sections estructuradas
- Schema: Article + FAQPage + BreadcrumbList
- **Nota crítica:** esta URL recibe tráfico orgánico mínimo pero ya está en el sitemap. Al expandir su contenido el impacto en Google será inmediato (no hay que esperar indexación)

**Spokes (nuevos):**
- `/blog/como-invertir-en-fibras-paso-a-paso` — 1,500 palabras, CTA a /fibras y /comparar
- `/blog/ventajas-y-riesgos-de-invertir-en-fibras` — 1,400 palabras
- `/blog/impuestos-fibras-inmobiliarias-mexico` — 1,400 palabras, tabla de retención fiscal

#### Cluster 2 — Métricas y Fundamentales

**Pillar:** `/fundamentales` (URL ya existe — validar profundidad de contenido)
- Keyword primaria: "fundamentales fibras inmobiliarias NAV cap rate NOI FFO"

**Spokes (nuevos):**
- `/blog/yield-fibras-inmobiliarias-como-calcular` — CTA a /calculadora
- `/blog/cap-rate-nav-fibras-explicados` — CTA a /fundamentales
- `/blog/distribuciones-fibras-cuando-y-como-pagan` — CTA a /calendario

#### Cluster 3 — Análisis Sectorial

**Pillar (nuevo):** `/blog/fibras-industriales-vs-comerciales-vs-hoteleras-mexico` — 3,000 palabras

**Spokes (nuevos):**
- `/blog/fibras-industriales-mexico-nearshoring` — aprovecha nearshoring 2025-2026
- `/blog/fusion-fibra-prologis-terrafina-macquarie` — contenido de actualidad caliente
- `/blog/fibra-next-next25-analisis-ipo` — IPO reciente (julio 2025), aún en ventana de frescura

#### Cluster 4 — Rankings y Comparativas (Decisión)

**Pillar (nuevo):** `/blog/mejores-fibras-para-invertir-2026` — intercepta la query #1 de fibrasmx.com
- Keyword primaria: "mejores fibras para invertir 2026 México"
- Tabla de datos con yield, sector, cap rate + metodología + CTA a /comparar
- Actualización trimestral obligatoria

**Spokes (nuevos):**
- `/blog/fibras-vs-cetes-donde-invertir-2026` — CTA a /calculadora; segundo mayor volumen
- `/blog/portafolio-de-fibras-como-diversificar` — CTA a /portafolio
- `/blog/fibras-para-principiantes-guia-completa`

#### Cluster 5 — Fichas Individuales de FIBRA (Existente — Diferenciación editorial)

Las 20 fichas necesitan descripciones editoriales de 150-250 palabras. Prioridad:

| Ticker | Diferenciador editorial |
|---|---|
| FUNO11 (Fibra Uno) | Mayor REIT México, portafolio mixto 586 propiedades |
| FIBRAPL14 (Prologis) | Mayor FIBRA industrial post-fusión Terrafina |
| FMTY14 (Fibra Monterrey) | Balance yield/crecimiento, mix oficinas+industrial |
| DANHOS13 (Fibra Danhos) | FIBRA premium comercial/retail, resiliencia renta alta |
| FIBRAMQ12 (Macquarie) | Objetivo OPA Prologis, historia de consolidación |

### 10.4 Calendario de Contenidos — 12 Semanas

| Semana | Entregable | Keywords primarias objetivo |
|---|---|---|
| 1 | Expandir `/conoce-las-fibras` a 3,200 palabras | "qué son las fibras inmobiliarias" |
| 2 | `/blog/como-invertir-en-fibras-paso-a-paso` | "cómo invertir en fibras inmobiliarias paso a paso" |
| 3 | Expandir `/fundamentales` a 3,000 palabras | "fundamentales fibras inmobiliarias NAV cap rate" |
| 4 | `/blog/distribuciones-fibras-cuando-y-como-pagan` | "distribuciones fibras cuándo y cómo pagan" |
| 5 | `/blog/mejores-fibras-para-invertir-2026` | "mejores fibras para invertir 2026 México" |
| 6 | `/blog/fibras-vs-cetes-donde-invertir-2026` | "fibras vs cetes 2026 dónde invertir" |
| 7 | `/blog/yield-fibras-inmobiliarias-como-calcular` | "yield fibras inmobiliarias cómo se calcula" |
| 8 | Ventajas/riesgos + Impuestos (2 artículos) | Cobertura completa del Cluster 1 |
| 9 | Pillar sectorial + nearshoring + fusión Prologis | Cluster 3 — sector industrial |
| 10 | Fibra Next IPO + cap rate/NAV explicados | Cluster 3 spoke + Cluster 2 spoke |
| 11 | Portafolio + principiantes + 5 descripciones fichas | Completar Cluster 4; diferenciación fichas |
| 12 | Artículos restantes + enlazado interno completo | Cierre de brechas |

**Continuo (mensual):**
- "Resumen mensual de distribuciones" — target: "distribuciones fibras [mes] [año]"
- "Resultados trimestrales FIBRAs" — target: "resultados fibras Q3 2026"

### 10.5 Enlazado Interno Requerido Tool-to-Editorial

```
/calculadora  → /blog/yield-fibras-inmobiliarias-como-calcular
/calculadora  → /blog/fibras-vs-cetes-donde-invertir-2026
/comparar     → /blog/mejores-fibras-para-invertir-2026
/calendario   → /blog/distribuciones-fibras-cuando-y-como-pagan
/portafolio   → /blog/portafolio-de-fibras-como-diversificar
/fibras       → /conoce-las-fibras  (anchor: "¿Qué son las FIBRAs? Guía completa")
```

### 10.6 Prerequisito Técnico Antes de Publicar Blog

Verificar que la ruta `/blog/:slug` esté registrada en `routes.tsx` y que el sitemap de blog esté incluido en el índice `/sitemap.xml`. Sin esto, los artículos no serán rastreados correctamente.

---

## 11. Tabla de Puntuación por Categoría

| Categoría | Puntuación | Prioridad de Mejora | Plazo para impacto visible |
|---|---|---|---|
| Schema y Datos Estructurados | 78/100 | Media — Person schema y FAQs | 2-4 semanas |
| SEO Técnico | 62/100 | Alta — quick wins claros | 1-2 semanas |
| SEO On-Page | 65/100 | Media — ajustes puntuales | 1-2 semanas |
| Calidad de Contenido (E-E-A-T) | 54/100 | Muy alta — YMYL sin autor | 4-8 semanas |
| Rendimiento y CWV | 52/100 | Alta — LCP mobile crítico | 2-4 semanas |
| Visibilidad en IA (GEO) | 46/100 | Alta — FAQs + llms.txt + SSR | 4-12 semanas |
| Imágenes | 55/100 | Baja — oportunidades incrementales | Incremental |
| Backlinks y Autoridad | 25/100 | Media — estrategia a largo plazo | 3-6 meses |
| Arquitectura de Contenido | 34/100 | Muy alta — el gap principal | 8-16 semanas |
| **GLOBAL** | **57/100** | — | — |

### Roadmap de 90 Días para Alcanzar 75/100

| Semana | Acciones | Impacto en Puntuación |
|---|---|---|
| 1 | Enviar sitemap a GSC · Desactivar Cloudflare Managed Robots.txt · Forzar https+non-www · Solicitar indexación de /fibras · Corregir footer link `PublicLayout.tsx` línea 415 | +5 pts técnico |
| 2 | Corregir `lastmod` en `SeoEndpoints.cs` · Noindex artículos off-topic · Agregar OAI-SearchBot + PerplexityBot · Desbloquear /llms.txt · Cambiar `max-age=1` a `86400` en llms.txt | +3 pts técnico, +4 pts GEO |
| 3-4 | Agregar sección de autoría en /acerca · Sembrar descripciones FUNO11, FIBRAPL14, FMTY14, DANHOS13, FIBRAMQ12 · Expandir 5 FAQs prioritarios a 134-167 palabras · Poblar sameAs en panel Ops | +8 pts E-E-A-T, +6 pts GEO |
| 5-6 | Hacer `HomePage` lazy · Identificar y eliminar dependencia estática de Recharts · Inline CSS crítico above-the-fold | +8 pts CWV (LCP mobile estimado: 4.0-4.5s) |
| 7-8 | Expandir /conoce-las-fibras a 3,200 palabras · Agregar namespace Google News al sitemap · Registrar ruta /blog en React Router | +6 pts contenido, +3 pts técnico |
| 9-12 | Publicar 4-6 artículos editoriales (mejores FIBRAs 2026, fibras vs CETES, distribuciones, cómo invertir) · Crear Person schema para autor · Agregar enlazado interno tool-to-editorial | +10 pts contenido, +5 pts autoridad |

**Puntuación proyectada al final de la semana 12: 74-78/100**

---

*Auditoría SEO generada el 18 de junio de 2026*  
*Especialistas contribuyentes: seo-technical · seo-content-quality · sitemap-architecture-specialist · cwv-performance-specialist · GEO Audit Agent · SXO Analyst · google-seo-data · seo-cluster*
