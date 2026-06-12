# Plan de Acción SEO — fibrasinmobiliarias.com
**Generado:** 12 de junio de 2026  
**Basado en:** FULL-AUDIT-REPORT.md

---

## Prioridad CRÍTICA — Bloquean rich results o dañan CTR (hacer hoy)

### C1. Subir o corregir `logo.png` (404)
**Impacto:** Desbloquea rich results de NewsArticle en Google  
**Esfuerzo:** 15 min  
**Archivos afectados:**
- Schema `Organization` en `src/` (homepage)
- Schema `NewsArticle` publisher logo en todas las noticias

**Acción:** Subir el logo de FIBRADIS como `/logo.png` (PNG, mínimo 112×112px, recomendado 512×512px), o actualizar las URLs en el schema al archivo correcto (ej. `/favicon.svg` o un PNG existente).

---

### C2. Eliminar Markdown crudo de meta descriptions en páginas FIBRA
**Impacto:** Mejora CTR directamente — Google muestra `# ?? Fibra Uno | FUNO11 > **Ticker:**...` en resultados  
**Esfuerzo:** 1–2 horas  
**Archivos afectados:** Generador de metadata SSR de páginas `/fibras/[slug]`

**Acción:** En el servidor, al generar la `<meta name="description">`:
1. Extraer solo el primer párrafo limpio del campo Markdown
2. Stripear todos los tokens Markdown (`#`, `**`, `>`, `_`, `\n`)
3. Truncar a 155 caracteres

```csharp
// Ejemplo de limpieza
string CleanMarkdownForMeta(string markdown) {
    // Quitar headings, bold, quotes, etc.
    var clean = Regex.Replace(markdown, @"[#*>`\[\]_]", "");
    clean = Regex.Replace(clean, @"\s+", " ").Trim();
    return clean.Length > 155 ? clean[..152] + "..." : clean;
}
```

---

### C3. Eliminar Markdown crudo del campo `description` en schema `FinancialProduct`
**Impacto:** Calidad de datos estructurados — Google valida el schema  
**Esfuerzo:** 30 min (mismo fix que C2, aplicado al generador de schema)  
**Acción:** Aplicar la misma limpieza de Markdown antes de incluir el valor en el JSON-LD.

---

## Prioridad ALTA — Impacto significativo en rankings (esta semana)

### H1. Corregir CLS 0.11 en móvil (falla umbral Google ≤ 0.10)
**Impacto:** Core Web Vital — afecta ranking móvil  
**Esfuerzo:** 2–4 horas  
**Causa probable:** Font swap de Google Fonts (Playfair Display / IBM Plex Sans con `display=swap`)

**Acciones a investigar:**
1. Añadir `font-display: optional` en lugar de `swap` para eliminar el re-layout
2. O hacer preload explícito de los woff2 críticos para que el swap sea instantáneo
3. Reservar espacio con `font-size-adjust` o fallback metrics que coincidan con las web fonts
4. Revisar skeleton loaders de los precio-cards — si cambian de tamaño al cargar datos, contribuyen al CLS

---

### H2. Permitir crawlers de IA search (Google-Extended, GPTBot)
**Impacto:** Visibilidad en AI Overviews de Google y ChatGPT Browse  
**Esfuerzo:** 5 min  
**Archivo:** El robots.txt es gestionado por Cloudflare — verificar si se puede añadir regla manual debajo del bloque `# END Cloudflare Managed Content`

**Acción recomendada** — añadir al final del robots.txt:
```
# Re-allow AI search crawlers (but NOT AI training)
User-agent: Google-Extended
Allow: /

User-agent: GPTBot
Allow: /

User-agent: ClaudeBot
Allow: /
```

**Nota:** El `Content-Signal: ai-train=no` ya protege contra el entrenamiento. Permitir el crawler no implica que Google/OpenAI usen el contenido para entrenar modelos.

---

### H3. Crear `llms.txt`
**Impacto:** Señal positiva para IA search — guía a LLMs sobre qué contenido es citable  
**Esfuerzo:** 30 min  
**Archivo a crear:** `/public/llms.txt` o equivalente en el servidor

**Contenido sugerido:**
```markdown
# FIBRADIS — Análisis de FIBRAs Inmobiliarias Mexicanas

> Plataforma de análisis de FIBRAs (Real Estate Investment Trusts) mexicanas.
> Datos de precios, distribuciones, fundamentales y noticias del sector.

## Páginas principales

- [Inicio](https://fibrasinmobiliarias.com/) — Dashboard con precios y ganadores/perdedores del día
- [Conoce las FIBRAs](https://fibrasinmobiliarias.com/conoce-las-fibras) — Guía completa sobre qué son las FIBRAs
- [Comparar FIBRAs](https://fibrasinmobiliarias.com/comparar) — Herramienta de comparación
- [Calculadora](https://fibrasinmobiliarias.com/calculadora) — Calculadora de rendimientos
- [Noticias](https://fibrasinmobiliarias.com/noticias) — Noticias del sector inmobiliario

## FIBRAs disponibles

FUNO11, FMTY14, FIBRAMQ12, FIBRAPL14, DANHOS13, FSHOP13, FIHO12, FINN13,
FHIPO14, FCFE18, EDUCA18, FNOVA17, FPLUS16, FIBRAUP18, HCITY17, NEXT25,
SOMA21, STORAGE18, VESTA19
```

---

### H4. Añadir schema a `/conoce-las-fibras`
**Impacto:** Oportunidad de rich result FAQ / Article en Google  
**Esfuerzo:** 1 hora  
**Acción:** Añadir `FAQPage` o `Article` schema en el componente SSR de esa página

```json
{
  "@context": "https://schema.org",
  "@type": "Article",
  "headline": "¿Qué son las FIBRAs Inmobiliarias? Guía Completa",
  "description": "Aprende qué son las FIBRAs inmobiliarias, cómo funcionan...",
  "url": "https://fibrasinmobiliarias.com/conoce-las-fibras",
  "publisher": { "@id": "https://fibrasinmobiliarias.com/#organization" },
  "inLanguage": "es-MX"
}
```

---

### H5. Añadir headers de seguridad faltantes
**Impacto:** Seguridad + señal de confianza para bots  
**Esfuerzo:** 1 hora (en Cloudflare o middleware ASP.NET)  
**Acciones:**

1. **HSTS** (vía Cloudflare o ASP.NET):
   ```
   Strict-Transport-Security: max-age=31536000; includeSubDomains; preload
   ```

2. **CSP básico** (empezar con report-only para no romper nada):
   ```
   Content-Security-Policy-Report-Only: default-src 'self' https:; script-src 'self' 'unsafe-inline' https://www.googletagmanager.com https://fonts.googleapis.com; style-src 'self' 'unsafe-inline' https://fonts.googleapis.com https://fonts.gstatic.com; img-src * data:; font-src 'self' https://fonts.gstatic.com; report-uri /api/csp-report
   ```

3. **Ocultar X-Powered-By** en `Program.cs`:
   ```csharp
   app.UseHsts();
   // En WebApplication builder:
   builder.WebHost.ConfigureKestrel(o => o.AddServerHeader = false);
   ```

---

## Prioridad MEDIA — Optimizaciones (este mes)

### M1. Corregir doble redirect www
**Esfuerzo:** 15 min (regla Cloudflare)  
**Acción:** Crear Page Rule o Redirect Rule en Cloudflare:  
`http://www.fibrasinmobiliarias.com/*` → `https://fibrasinmobiliarias.com/$1` (301)  
Actualmente hay dos saltos; debe resolverse en uno.

---

### M2. Añadir `favicon.ico`
**Esfuerzo:** 5 min  
**Acción:** Generar un `favicon.ico` 32×32 desde el SVG existente y colocarlo en `/public/favicon.ico`.

---

### M3. Añadir `<lastmod>` a páginas estáticas en sitemap
**Esfuerzo:** 30 min  
**Páginas afectadas:** `/`, `/fibras`, `/comparar`, `/noticias`, `/conoce-las-fibras`, `/calendario`, `/fundamentales`, `/herramientas`, `/calculadora`, `/acerca`, `/contacto`, `/privacidad`

**Acción:** En el generador de sitemap, asignar la fecha del último deploy como `lastmod` para páginas estáticas, o usar la fecha del último dato cambiado para páginas de datos.

---

### M4. OG images específicas por FIBRA
**Esfuerzo:** 1–2 días (generación dinámica)  
**Impacto:** CTR en redes sociales al compartir análisis de una FIBRA específica  
**Acción:** Implementar generación dinámica de OG images en el servidor con el logo, ticker y precio actual de cada FIBRA. Alternativa rápida: imágenes estáticas por FIBRA con template.

---

### M5. Añadir Twitter Card meta tags
**Esfuerzo:** 30 min  
**Acción:** Añadir en el `<head>` SSR:
```html
<meta name="twitter:card" content="summary_large_image" />
<meta name="twitter:site" content="@fibradis" />
<meta name="twitter:title" content="..." />
<meta name="twitter:description" content="..." />
<meta name="twitter:image" content="..." />
```

---

## Prioridad BAJA — Backlog

### L1. Corregir contraste de texto (Lighthouse Accessibility 96)
**Elementos afectados:**
- `p.mt-3` en sección hero (texto secundario)
- `span.opacity-60` en tarjetas de FIBRA
- `p.text-xs` en tarjetas de resumen

**Acción:** Aumentar contraste de texto gris/verde sobre fondos claros para cumplir WCAG AA (ratio 4.5:1 para texto normal, 3:1 para texto grande).

---

### L2. Sitemap de imágenes
**Acción:** Para las páginas de FIBRA con logos/gráficas, añadir `<image:image>` en el sitemap cuando se implementen OG images por FIBRA.

---

### L3. Señales de autoridad / E-E-A-T
**Acciones sugeridas:**
- Página `/acerca` con perfiles del equipo y credenciales financieras
- Menciones de que los datos provienen de BMV/BIVA (fuentes reguladas)
- Añadir `sameAs` en el schema Organization apuntando a LinkedIn, Twitter, etc.

---

## Resumen de Prioridades

| # | Tarea | Prioridad | Esfuerzo | Impacto |
|---|-------|-----------|----------|---------|
| C1 | logo.png 404 | CRÍTICA | 15 min | Rich results Google |
| C2 | Markdown en meta descriptions | CRÍTICA | 2h | CTR directo |
| C3 | Markdown en schema description | CRÍTICA | 30 min | Calidad schema |
| H1 | CLS móvil 0.11 | ALTA | 4h | Ranking móvil |
| H2 | Permitir crawlers IA | ALTA | 5 min | AI search visibility |
| H3 | Crear llms.txt | ALTA | 30 min | AI search authority |
| H4 | Schema /conoce-las-fibras | ALTA | 1h | Rich result FAQ |
| H5 | HSTS + CSP headers | ALTA | 1h | Seguridad |
| M1 | Doble redirect www | MEDIA | 15 min | Crawl efficiency |
| M2 | favicon.ico | MEDIA | 5 min | Compatibilidad |
| M3 | lastmod en sitemap estático | MEDIA | 30 min | Crawl budget |
| M4 | OG images por FIBRA | MEDIA | 2 días | CTR social |
| M5 | Twitter Card tags | MEDIA | 30 min | CTR social |
| L1 | Contraste texto | BAJA | 2h | Accesibilidad |
| L2 | Image sitemap | BAJA | 1h | Imagen SEO |
| L3 | E-E-A-T / equipo | BAJA | 1 día | Autoridad |

---

*Generado por Claude Code SEO Audit — 2026-06-12*
