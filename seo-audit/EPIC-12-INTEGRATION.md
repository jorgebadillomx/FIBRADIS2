# Integración de hallazgos de auditoría SEO → Épica 12

**Auditoría:** 2026-06-13 · **Score:** 84/100 · **Sitio:** https://fibrasinmobiliarias.com/
**Fuentes:** [FULL-AUDIT-REPORT.md](FULL-AUDIT-REPORT.md) · [ACTION-PLAN.md](ACTION-PLAN.md)

Cada hallazgo de la auditoría se integró como sección **"Hallazgos de auditoría SEO (2026-06-13)"** dentro de la historia correspondiente (justo antes de `## Dev Agent Record`). Esta tabla es el índice de trazabilidad.

| # | Hallazgo | Severidad | Historia | Tipo de integración |
|---|----------|-----------|----------|---------------------|
| C1 | Meta description de fichas = volcado de markdown (pipes, `...`) en meta + twitter + JSON-LD `FinancialProduct` | 🔴 Critical | **12-1** | **Amienda AC-5/T3**: el builder debe limpiar, no replicar el bug. Test con valores exactos como gate. |
| C2 | Emoji corrupto `??` (bytes `0x3F`, pérdida de encoding) en meta y contenido | 🔴 Critical | **12-1** | Mismo fix que C1 (UTF-8 / strip emoji). Visible también en frontend → ver H2. |
| H1 | Soft 404: rutas desconocidas devuelven HTTP 200 con `<title>` vacío | 🟠 High | **12-6** | Nuevo alcance: validar ruta antes del fallback SPA, responder 404. Añadir a AC-7. |
| H2 | `<h1>` duplicado en ficha (título + heading del markdown "Descripción") | 🟠 High | **12-3** | Fix frontend Main: desplazar headings markdown +1. |
| H3 | robots.txt en conflicto: bloque Cloudflare-managed `Disallow`ea bots IA que la app re-`Allow`ea; doble `User-agent: *` | 🟠 High | **12-6** | Resolver política en una sola fuente (desactivar managed robots de Cloudflare o alinear). Coord. AC-6. |
| M1 | CLS 0.195 en páginas data-heavy (home 0.08 OK); LCP home 355ms OK | 🟡 Medium | **12-7** | Baseline lab medido como input al spike T1; prioriza T3 (reservar espacio gráficas/tablas). |
| M2 | `/comparar`, `/fundamentales`, `/noticias` sin JSON-LD; faltan breadcrumbs universales | 🟡 Medium | **12-5** | Confirma premise; añade `ItemList` para listado `/noticias` + copy en `/comparar`. |
| M3 | A11y: 43 fallos de contraste + `aria-expanded` inválido en `<tr>` | 🟡 Medium | **12-7** | Adyacente al spike Lighthouse; decidir si se aborda aquí o en historia aparte. |
| M4 | og:image de noticias apunta a `lh3.googleusercontent.com` (puede romperse) | 🟡 Medium | **12-9** | Extensión opcional: proxear/cachear thumbnails de noticias; si no, dejar como deuda documentada. |
| M5 | Sin atribución de autoría/experiencia (bloque "Perspectiva del analista" sin autor) — YMYL | 🟡 Medium | **12-4** | Refuerza AC-3: autoría editorial/metodológica nombrada (real, no ficticia). |
| L1 | Falta `Content-Security-Policy` | 🟢 Low | **12-6** | Nota menor (security headers). |
| L2 | `X-Powered-By: ASP.NET` expuesto | 🟢 Low | **12-6** | Nota menor (suprimir header). |
| L3 | Sitemap: rutas core sin `<lastmod>` | 🟢 Low | **12-6** | Nota menor al construir sub-sitemaps. |
| L4 | `<title>` servidor ≠ cliente (`usePageTitle` reescribe) | 🟢 Low | **12-1** | Nota: formato canónico único. |
| L5 | `FAQPage` ausente (mayor oportunidad GEO) | 🟢 Low | **12-2** | Confirmación de prioridad (es el objetivo de la historia). |
| L6 | `Dataset` schema para fundamentales | 🟢 Low | **12-5** | Ya cubierto por AC-2 de la historia. |
| L7 | `/comparar` con poco texto indexable | 🟢 Low | **12-5** | Nota menor (copy de apoyo). |
| L8 | `sameAs` / citaciones de marca para autoridad de entidad | 🟢 Low | **12-4** | Ya cubierto por AC-2b de la historia. |

## Lo que la auditoría confirmó que YA está bien (no requiere historia)
- HTTPS forzado + 301 (`http://` y `www.`) · HSTS, X-Frame-Options, X-Content-Type-Options, Referrer-Policy, Permissions-Policy.
- `llms.txt` completo y bien estructurado (ejemplar).
- Inyección de meta por ruta en servidor (title/description/canonical/OG/Twitter únicos por página).
- Schema fuerte: Organization, WebSite, FinancialService, FinancialProduct, BreadcrumbList, NewsArticle, Article, ImageObject.
- Disclaimer YMYL claro + metodología documentada + fundamentales con citas de fuente por página.
- Lighthouse SEO 100, Best Practices 100; LCP/CLS de home en verde.

## Hallazgos sin historia dedicada (decisión pendiente)
- **M3 (a11y contraste/aria):** anotado en 12-7 pero no es CWV. Si se quiere tratar a fondo, amerita su propia historia de accesibilidad (no existe u 12-x de a11y).
- **M4 (proxy de imágenes de noticias):** anotado en 12-9 como extensión opcional; si se mantiene fuera de alcance, queda como deuda para historia futura.
