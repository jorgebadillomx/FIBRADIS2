# Plan de Accion SEO - fibrasinmobiliarias.com
**Health Score inicial:** 57/100 | **Fecha:** Junio 2026

Scores por area:
- SEO Tecnico: 62/100
- Calidad de Contenido: 54/100
- Sitemap: 54/100
- Rendimiento/CWV: 52/100
- GEO/IA: 46/100
- SXO: 41/100
- Google Data (GSC/GA4): 34/100
- Arquitectura de Contenido: 34/100
- Backlinks y Autoridad: 25/100 (sitio nuevo)

---

## CRITICO - Implementar esta semana

### 1. Resolver conflicto robots.txt con Cloudflare
**Descripcion:** El bloque gestionado por Cloudflare inyecta Disallow para ClaudeBot, GPTBot, Google-Extended y
Applebot-Extended antes del bloque del app que los permite. Dependiendo del parser, estos bots pueden quedar
bloqueados. Desactivar la gestion automatica de robots.txt en Cloudflare Bot Management y servir el archivo
exclusivamente desde BuildRobotsTxt() en el backend. Anadir OAI-SearchBot con Allow.
**Impacto:** Elimina el riesgo de bloqueo de AI crawlers; restaura visibilidad en AI Overviews.
**Esfuerzo:** Bajo (30 min) | **Responsable:** Backend/DevOps

---

### 2. Desbloquear llms.txt y sitemap.xml ante bots (Cloudflare WAF 403)
**Descripcion:** llms.txt devuelve HTTP 403 a fetchers automatizados y sitemap.xml es inaccesible para clientes
desde datacenter IPs. Crear regla WAF en Cloudflare que permita acceso sin inspeccion a llms.txt, sitemap.xml,
sitemap-*.xml y robots.txt.
**Impacto:** Restaura acceso de Googlebot al sitemap completo; precondicion para cualquier mejora de indexacion.
**Esfuerzo:** Bajo (1h) | **Responsable:** DevOps

---

### 3. Enviar sitemap actual a Google Search Console
**Descripcion:** GSC solo tiene registrado el legacy /xmlsitemap/ (0 paginas indexadas de 38 enviadas, desde 2023).
La arquitectura actual nunca ha sido registrada. Enviar https://fibrasinmobiliarias.com/sitemap.xml en
GSC -> Sitemaps -> Agregar sitemap nuevo. Eliminar sitemaps legacy.
**Impacto:** Activa el descubrimiento formal de todas las paginas - accion de mayor palanca a costo cero.
**Esfuerzo:** Bajo (15 min) | **Responsable:** Marketing/SEO

---

### 4. Canonicalizar www a non-www y HTTP a HTTPS en Cloudflare
**Descripcion:** GSC muestra trafico fragmentado en 4 variantes de URL. La query mas valiosa (lista de fibras
en Mexico, posicion 4.8) aterriza en http://www.fibrasinmobiliarias.com/fibras. Configurar redirects 301
permanentes en Cloudflare Page Rules.
**Impacto:** Consolida toda la autoridad de enlace en un unico origen; impacto directo en rankings existentes.
**Esfuerzo:** Bajo (30 min) | **Responsable:** DevOps

---

### 5. Solicitar indexacion manual de /fibras en GSC
**Descripcion:** GSC reporta /fibras como URL desconocida a pesar de ser la segunda pagina de mayor trafico
organico (17 sesiones/mes, 115 impresiones). Usar Inspeccion de URL en GSC para solicitar rastreo manual.
**Impacto:** Recupera posicionamiento de la pagina con mayor inventario de keywords del sitio en dias.
**Esfuerzo:** Bajo (20 min) | **Responsable:** Marketing/SEO

---

### 6. ⚠️ ACCIÓN MANUAL — Deindexar artículos off-topic ya indexados por Google

**Contexto:** Los artículos sobre fibra óptica, dietética, etc. ya no existen en el sitio. Solo viven en el índice de Google como páginas cacheadas apuntando a soft-404s.

**Pasos manuales:**

1. GSC → Inspección de URL → pegar cada URL off-topic → solicitar eliminación
2. Alternativa: GSC → Eliminaciones → Nueva solicitud → eliminar por prefijo si comparten patrón de URL
3. Google las deindexará solas al ver 404 sostenido (semanas) — la eliminación manual acelera a horas

**Responsable:** Jorge (Google Search Console, sin cambios de código)

---

## ALTO - Implementar en 2 semanas

### 7. ✅ Corregir lastmod en sitemaps (siempre devuelve hoy)
**Descripcion:** GetGeneratedLastMod() devuelve DateTimeOffset.UtcNow para todas las paginas sin excepcion.
Modificar GetVisibleStaticRoutes en SeoEndpoints.cs para devolver tuplas (path, updatedAt) usando
SeoMetadata.UpdatedAt. Para FIBRAs usar Fibra.CreatedAt o timestamp de ultima actualizacion de mercado.
**Impacto:** Restaura fiabilidad de la senal de frescura; optimiza crawl budget en ~25 paginas estables.
**Esfuerzo:** Bajo (~20 lineas en SeoEndpoints.cs) | **Responsable:** Backend
**Implementado:** 2026-06-18 — SitemapVisibility.StaticRouteLastMod, GetVisibleStaticRoutes devuelve (Path, LastMod), fallback "2024-01-01"

---

### 8. ✅ Corregir link de pie de pagina al home
**Descripcion:** En PublicLayout.tsx linea 415, el nombre de marca en el copyright enlaza a la pagina de
plataforma en lugar de la raiz del sitio. Este link aparece en todas las paginas publicas y distribuye
PageRank hacia una ruta incorrecta.
**Impacto:** Consolida senales de autoridad interna hacia la homepage.
**Esfuerzo:** Bajo (5 min) | **Responsable:** Frontend
**Implementado:** 2026-06-18 — copyright → /, nuevo link "Plataforma" → /plataforma en footer

---

### 9. Autoria visible en /acerca (E-E-A-T YMYL)
**Descripcion:** La plataforma no tiene autor nombrado ni perfil editorial indexable. Google QRG requiere
senales de autoria para sitios financieros YMYL. Anadir nombre del fundador/equipo, anio de fundacion
(2023) y experiencia en mercados financieros mexicanos en la pagina /acerca.
**Impacto:** Mayor mejora de E-E-A-T disponible; diferencia frente a Finantres y FibrasMX que tienen autores.
**Esfuerzo:** Bajo (1-2h redaccion + 2h implementacion) | **Responsable:** Marketing/Contenido

---

### 10. ✅ Schema NewsArticle con author en paginas de noticia
**Descripcion:** NoticiaPage.tsx muestra solo fuente y fecha sin Schema de articulo. Anadir JSON-LD NewsArticle
con author.name, datePublished, headline y publisher en NewsMetadataMiddleware.
**Impacto:** Habilita elegibilidad para Google News y mejora trust score YMYL.
**Esfuerzo:** Medio (4-6h) | **Responsable:** Backend
**Implementado:** 2026-06-18 — guard "regenera si JsonLd null && !overridden" en NewsMetadataMiddleware; eliminado dead code BuildMetaBlock(NewsArticle) y métodos huérfanos (-155 líneas)

---

### 11. ✅ Lazy load de HomePage y separacion del bundle Recharts
**Descripcion:** HomePage es el unico componente importado estaticamente en routes.tsx. vendor-charts
(102 KB transferidos, 82 KB sin usar en homepage) se precarga incondicionalmente. Mover ambos a React.lazy().
**Impacto:** Reduccion estimada 1.3-1.8s en LCP mobile; mejora Lighthouse desde 49/100.
**Esfuerzo:** Medio (4-6h) | **Responsable:** Frontend
**Implementado:** 2026-06-18 — HomePage convertida a React.lazy() + p() en routes.tsx; chunk HomePage-*.js separado del entry bundle

---

### 12. ✅ Extension Google News en sitemap-noticias
**Descripcion:** sitemap-noticias-1.xml carece del namespace xmlns:news. Anadir news:publication,
news:publication_date y news:title en SeoEndpoints.cs. Los datos ya estan disponibles en NewsArticle.
**Impacto:** Mejora descubrimiento en Google News para 160 articulos indexados.
**Esfuerzo:** Bajo (~30 lineas en SeoEndpoints.cs) | **Responsable:** Backend
**Implementado:** 2026-06-18 — BuildNewsUrlSetXml con xmlns:news, news:publication y news:publication_date ISO 8601

---

### 13. Inline de CSS critico (337ms render-blocking en mobile)
**Descripcion:** index-ak8h6ly1.css (18.5 KB) bloquea el render 337ms en mobile y 135ms en desktop. Extraer
2-4 KB de reglas criticas above-the-fold e inlinarlas en el head. Implementable con vite-plugin-critical.
**Impacto:** Elimina 337ms de render-blocking; mejora FCP y LCP directamente.
**Esfuerzo:** Medio (4-8h incluyendo testing) | **Responsable:** Frontend/Build

---

## MEDIO - Implementar en 1 mes

### 14. Descripciones editoriales para FIBRAs (campo Description NULL)
Priorizar FUNO11, DANHOS13, FIBRAMQ12, FIBRAPL14, FMTY14 con 150-250 palabras cada una.
CatalogSeed.cs no tiene ningun valor de descripcion para las 20 FIBRAs activas.
**Esfuerzo:** Medio (8-12h redaccion + 2h seed) | **Responsable:** Contenido + Backend

### 15. Expandir respuestas FAQ a 134-167 palabras
FaqSeedFactory.cs promedia 40 palabras (rango 21-57). Rango optimo para AI engines: 134-167 palabras.
Reescribir con contexto, ejemplos numericos y referencias CNBV.
**Esfuerzo:** Medio (3-5h) | **Responsable:** Contenido

### 16. ✅ Self-hosting de Google Fonts
Descargar woff2 de Playfair Display e IBM Plex Sans y servirlos desde /assets/fonts/ con Cache-Control
max-age=31536000. Elimina 2 handshakes a dominios externos.
**Esfuerzo:** Bajo (2-3h) | **Responsable:** Frontend
**Implementado:** 2026-06-18 — 8 woff2 (subset latin) en public/assets/fonts/; @font-face en index.css; eliminados preconnect+preload+noscript de Google Fonts en index.html

### 17. Copy editorial en paginas de herramienta
/calculadora, /comparar y /calendario son UI pura sin texto indexable. Anadir 150-200 palabras de contexto
editorial en cada una explicando la herramienta e implicaciones financieras.
**Esfuerzo:** Medio (6-8h redaccion + 4h implementacion) | **Responsable:** Contenido + Frontend

### 18. Habilitar IndexNow
Generar clave IndexNow y anadir ping en el pipeline de publicacion de noticias y job de precios de FIBRAs.
**Esfuerzo:** Bajo (3-4h) | **Responsable:** Backend

### 19. ✅ Cache-Control de llms.txt: max-age=1 a max-age=86400
SeoEndpoints.cs linea ~188. Ampliar contenido de llms.txt con todas las rutas publicas del sitio.
**Esfuerzo:** Bajo (1h) | **Responsable:** Backend
**Implementado:** 2026-06-18 — max-age=86400; añadidas /acerca, /portafolio, /calculadora, /calendario

### 20. Disclaimer YMYL con fecha y referencia CNBV
Incluir fecha de revision, mencion de Ley del Mercado de Valores y referencia a AMEFIBRA.
**Esfuerzo:** Bajo (2-3h) | **Responsable:** Legal/Marketing

### 21. sameAs verificables en Organization Schema via Ops panel
Configurar LinkedIn y Twitter/X en OrganizationSameAsJson de OperationalConfig. La carga ya esta implementada.
**Esfuerzo:** Bajo (30 min) | **Responsable:** Marketing

---

## BAJO - Backlog

### 22. Seccion /aprende o /guias con contenido pilar
Competidor fibrasmx.com captura todas las queries informacionales de alta intencion. Crear 5 articulos pilar
(2,000-3,000 palabras) con estructura hub-and-spoke hacia fichas de FIBRAs individuales.
**Esfuerzo:** Alto (40-80h) | **Impacto:** Captura 80% del volumen informacional del nicho a 3-6 meses.

### 23. SSR o prerender parcial para crawlers sin JS
El HTML raw pesa 203 bytes (div id=root). LLM crawlers sin ejecucion de JS reciben pagina vacia.
Evaluar Vite SSR o prerender.io.
**Esfuerzo:** Alto (2-4 semanas) | **Impacto:** Elimina SPA rendering gap para todos los crawlers.

### 24. Dead code OG images por FIBRA (FibraProfileMetadataMiddleware.cs linea 251)
El endpoint /og/fibras/{ticker}.png existe pero el middleware referencia la imagen generica.
**Esfuerzo:** Medio (6-10h) | **Impacto:** CTR mejorado con rich cards por ticker.

### 25. CTA above-the-fold en homepage
Anadir boton de registro en la seccion hero, encima del data table de FIBRAs.
**Esfuerzo:** Bajo (3-4h) | **Impacto:** Mejora conversion de trafico organico de awareness.

### 26. Email corporativo contacto@fibrasinmobiliarias.com
portafoliodefibras@gmail.com senala organizacion no verificable para YMYL.
**Esfuerzo:** Bajo | **Impacto:** Credibilidad organizacional para knowledge graph.

### 27. ✅ Eliminar metodo BuildSitemapXml muerto en SeoEndpoints.cs
Codigo inalcanzable sin binding a ninguna ruta. Deuda tecnica, sin impacto SEO directo.
**Esfuerzo:** Bajo (15 min)
**Implementado:** 2026-06-18 — método eliminado, tests adaptados a BuildSitemapIndexXml y BuildNewsUrlSetXml

---

*Generado el 18 de junio de 2026 | Health Score proyectado tras items Criticos+Altos: 74-78/100*
