# Historia 11.2: SpaMetadataInjectionMiddleware + Static Shell Calculadora

Status: done

## Historia

Como SEO lead,
quiero que las rutas públicas del SPA entreguen `<title>`, `<meta name="description">`, `<link rel="canonical">` y JSON-LD en el HTML inicial (sin ejecutar JavaScript),
para que Googlebot pueda indexar las páginas del sitio — en particular `/calculadora` que tiene posición 4.3 en GSC pero está "Crawled - currently not indexed" desde marzo 2026 por ser una página CSR vacía.

## Criterios de Aceptación

**CA-1: Home entrega metadata en HTML inicial**
Dado que hago GET `/` (sin JavaScript),
Entonces el HTML incluye en `<head>`:
- `<title>FIBRAs Inmobiliarias — Análisis y Herramientas | FIBRADIS</title>`
- `<meta name="description" content="...">` con descripción de 120–160 caracteres
- `<link rel="canonical" href="https://fibrasinmobiliarias.com/">`
- `<meta property="og:title" ...>` y `<meta property="og:description" ...>`

**CA-2: /calculadora entrega metadata completa + JSON-LD**
Dado que hago GET `/calculadora` (sin JavaScript),
Entonces el HTML incluye en `<head>`:
- `<title>Calculadora ISR FIBRAs — Impuesto sobre la Renta | FIBRADIS</title>`
- `<meta name="description" content="...">` con descripción de 120–160 chars sobre la calculadora ISR
- `<link rel="canonical" href="https://fibrasinmobiliarias.com/calculadora">`
- Bloque `<script type="application/ld+json">` con schema FAQPage (al menos 2 preguntas sobre ISR y FIBRAs)
- `<meta property="og:*">` básicos

**CA-3: Inyección vía comentario prerender-meta**
Dado que `wwwroot/index.html` contiene el comentario `<!-- prerender-meta -->`,
Entonces el middleware reemplaza exactamente ese comentario con los tags de metadata (no modifica ninguna otra parte del HTML).

**CA-4: Rutas sin metadata configurada no son afectadas**
Dado que hago GET `/portafolio` o `/oportunidades`,
Entonces el middleware pasa la request sin modificar (el index.html original llega al cliente).

**CA-5: Assets estáticos no pasan por el middleware**
Dado que hago GET `/assets/index-1TzwM6fE.js`,
Entonces el middleware no intercepta la request (la extension `.js` la excluye).

**CA-6: Ruta /calculadora existe en React Router**
Dado que navego a `/calculadora` en el SPA,
Entonces el componente `CalculadoraPage` se renderiza con:
- `<title>` y `<meta name="description">` nativos de React 19
- Contenido estático descriptivo del calculador ISR (sin formulario interactivo — eso es historia 10-2)
- El checklist de cierre SEO de `convenciones-fibradis.md` se cumple

**CA-7: BaseUrl configurable desde appsettings**
Dado que `App:BaseUrl` está en `appsettings.json`,
Entonces el middleware usa ese valor para construir las URLs canónicas.
No hay URLs hardcodeadas en el código C#.

## Tareas / Subtareas

- [x] Task 1: Agregar configuración `App:BaseUrl` a `appsettings.json`
  - [x] Agregar en `src/Server/Api/appsettings.json`:
    ```json
    "App": {
      "BaseUrl": "https://fibrasinmobiliarias.com"
    }
    ```
  - [x] Agregar también en `appsettings.Development.json`:
    ```json
    "App": {
      "BaseUrl": "https://localhost:5001"
    }
    ```

- [x] Task 2: Crear modelos `SpaPageMeta` y `ISpaMetadataProvider`
  - [x] Crear `src/Server/Api/Seo/SpaPageMeta.cs`:
    ```csharp
    namespace Api.Seo;
    public record SpaPageMeta(
        string Title,
        string Description,
        string CanonicalPath,  // e.g. "/calculadora" — el middleware prefija con BaseUrl
        string? JsonLd = null
    );
    ```
  - [x] Crear `src/Server/Api/Seo/ISpaMetadataProvider.cs`:
    ```csharp
    namespace Api.Seo;
    public interface ISpaMetadataProvider
    {
        SpaPageMeta? GetMetaForPath(string path);
    }
    ```
  - [x] Crear `src/Server/Api/Seo/SpaMetadataProvider.cs` con implementación:
    - Dictionary readonly con las rutas conocidas (ver tabla abajo)
    - `GetMetaForPath` normaliza el path (lowercase, trailing slash strip) antes de buscar
    - Retorna `null` si no hay entrada para la ruta

- [x] Task 3: Crear `SpaMetadataMiddleware.cs`
  - [x] Crear `src/Server/Api/Middleware/SpaMetadataMiddleware.cs` en namespace `Api.Middleware`
  - [x] Constructor: `(RequestDelegate next, ISpaMetadataProvider metadataProvider, IWebHostEnvironment env, IConfiguration config)`
  - [x] `InvokeAsync`: 
    - Si el path tiene extensión (`.js`, `.css`, `.png`, etc.) → `await next(context)` y return
    - Si el path empieza con `/api/` o `/ops/` o `/hangfire/` → `await next(context)` y return
    - Llamar `metadataProvider.GetMetaForPath(path)`
    - Si es null → `await next(context)` y return
    - Si tiene metadata: leer `wwwroot/index.html`, reemplazar `<!-- prerender-meta -->`, escribir respuesta
  - [x] Usar `IWebHostEnvironment.WebRootPath` para localizar `index.html`
  - [x] Leer el archivo `index.html` con `File.ReadAllTextAsync`
  - [x] Construir el bloque HTML de metadata (ver spec en Dev Notes)
  - [x] Reemplazar `<!-- prerender-meta -->` con el bloque construido
  - [x] Setear `context.Response.ContentType = "text/html; charset=utf-8"` y escribir la respuesta
  - [x] Hacer return para short-circuit (no llamar `next`)

- [x] Task 4: Registrar middleware y servicio en `Program.cs` / `ApiServiceExtensions.cs`
  - [x] Agregar en `ApiServiceExtensions.cs`:
    ```csharp
    builder.Services.AddSingleton<ISpaMetadataProvider, SpaMetadataProvider>();
    ```
  - [x] En `Program.cs`, agregar `app.UseMiddleware<SpaMetadataMiddleware>();` DESPUÉS de `UseMiddleware<WwwToNonWwwMiddleware>()` (historia 11-1) y ANTES de `UseDefaultFiles()`
  - [x] El orden correcto del pipeline:
    ```
    UseHttpsRedirection → WwwToNonWwwMiddleware → SpaMetadataMiddleware → UseDefaultFiles → UseStaticFiles → UseRouting → ...
    ```
  - [x] NOTA: Si 11-1 no está implementada aún, colocar SpaMetadataMiddleware como primera línea después de `var app = builder.Build()`

- [x] Task 5: Crear `CalculadoraPage.tsx`
  - [x] Crear `src/Web/Main/src/modules/calculadora/CalculadoraPage.tsx`
  - [x] La página debe incluir React 19 native metadata:
    ```tsx
    <title>Calculadora ISR FIBRAs — Impuesto sobre la Renta | FIBRADIS</title>
    <meta name="description" content="Calcula el Impuesto Sobre la Renta (ISR) de tus distribuciones de FIBRAs inmobiliarias mexicanas. Herramienta gratuita con base en la Ley del ISR vigente." />
    <link rel="canonical" href="https://fibrasinmobiliarias.com/calculadora" />
    <meta property="og:title" content="Calculadora ISR FIBRAs | FIBRADIS" />
    <meta property="og:description" content="Calcula el ISR de tus distribuciones de FIBRAs inmobiliarias. Herramienta gratuita." />
    <meta property="og:type" content="website" />
    ```
  - [x] Contenido estático (NO formulario interactivo — el calculador real es historia 10-2):
    - H1: "Calculadora ISR para FIBRAs Inmobiliarias"
    - Párrafo introductorio explicando qué es el ISR en FIBRAs (~100 palabras)
    - Sección "¿Qué es el ISR en las distribuciones de FIBRAs?" con explicación
    - Sección "¿Cómo se calcula el ISR de distribuciones?" con la fórmula básica
    - Sección "Tasa de retención aplicable" (tabla simple con tasas)
    - Badge o banner: "Calculadora interactiva próximamente" (shadcn Badge o Alert)
  - [x] Usar clases Tailwind v4 — NO usar clases de v3 que no existan en v4
  - [x] Importar desde `@/shared/ui/` los componentes shadcn necesarios (Badge, etc.)

- [x] Task 6: Agregar ruta `/calculadora` a `routes.tsx`
  - [x] Agregar import: `import { CalculadoraPage } from '@/modules/calculadora/CalculadoraPage'`
  - [x] Agregar ruta en el array de `children` de PublicLayout:
    ```tsx
    { path: '/calculadora', element: <CalculadoraPage /> },
    ```
  - [x] Colocarla junto a las otras rutas públicas (antes de ProtectedRoute)

- [x] Task 7: Unit tests para `SpaMetadataMiddleware`
  - [x] Archivo: agregar a `tests/Unit/Infrastructure.Tests/` o crear clase en el proyecto de unit tests existente
  - [x] `InjectsMetadata_ForKnownPath()` — verifica que la response contiene el title correcto para `/calculadora`
  - [x] `PassesThrough_ForUnknownPath()` — verifica que se llama `next` para `/portafolio`
  - [x] `PassesThrough_ForAssets()` — verifica que se llama `next` para `/assets/main.js`
  - [x] `PassesThrough_ForApiPrefix()` — verifica que se llama `next` para `/api/v1/fibras`
  - [x] `ReplacesPrerendMetaComment()` — verifica que `<!-- prerender-meta -->` es reemplazado
  - [x] Usar mocks de `IWebHostEnvironment` con un `index.html` template en memoria o temp file

- [x] Task 8: Verificación build TypeScript
  - [x] Ejecutar `npm run build --workspace=src/Web/Main`
  - [x] Verificar 0 errores TypeScript y 0 advertencias de `noUnusedLocals`
  - [x] Ejecutar `dotnet build FIBRADIS.slnx` y verificar 0 errores

## Dev Notes

### Contexto SEO: por qué este middleware
fibrasinmobiliarias.com es una CSR SPA (Vite 7 + React 19.2). Googlebot puede ejecutar JavaScript pero:
1. El rendering JS ocurre con delay (crawl budget)
2. `/calculadora` ha estado "Crawled - currently not indexed" desde marzo 2026 — Googlebot la rastreó pero no la indexó, probablemente porque encontró contenido vacío o minimal
3. La solución correcta para producción es server-side metadata injection: el servidor modifica el HTML antes de enviarlo

### Patrón de inyección: reemplazar `<!-- prerender-meta -->`
El `wwwroot/index.html` actual ya contiene el comentario `<!-- prerender-meta -->` (puesto en historia 2-4 con este propósito exacto). El middleware lo reemplaza con los tags de metadata correctos para cada ruta.

### Bloque HTML de metadata a generar
Para cada ruta con metadata, generar el siguiente bloque:

```html
<title>{meta.Title}</title>
<meta name="description" content="{meta.Description}" />
<link rel="canonical" href="{baseUrl}{meta.CanonicalPath}" />
<meta property="og:title" content="{meta.Title}" />
<meta property="og:description" content="{meta.Description}" />
<meta property="og:type" content="website" />
<meta property="og:url" content="{baseUrl}{meta.CanonicalPath}" />
{Si meta.JsonLd != null: <script type="application/ld+json">{meta.JsonLd}</script>}
```

### Tabla de rutas con metadata (implementar en SpaMetadataProvider)

| Path | Title | Description (120-160 chars) |
|------|-------|---------------------------|
| `/` | `FIBRAs Inmobiliarias — Análisis y Herramientas \| FIBRADIS` | `Plataforma de análisis de FIBRAs inmobiliarias mexicanas. Precios en tiempo real, distribuciones, fundamentales y ranking de oportunidades.` |
| `/calculadora` | `Calculadora ISR FIBRAs — Impuesto sobre la Renta \| FIBRADIS` | `Calcula el Impuesto Sobre la Renta (ISR) de tus distribuciones de FIBRAs inmobiliarias mexicanas. Herramienta gratuita con base en la Ley del ISR vigente.` |
| `/comparar` | `Comparar FIBRAs Inmobiliarias — Análisis Comparativo \| FIBRADIS` | `Compara hasta 4 FIBRAs inmobiliarias en precio, yield, fundamentales y score de oportunidad. Toma mejores decisiones de inversión.` |
| `/catalogo` | `Catálogo de FIBRAs Inmobiliarias Mexicanas \| FIBRADIS` | `Directorio completo de FIBRAs inmobiliarias en México con descripción, sector, precio y datos fundamentales de cada fideicomiso.` |
| `/noticias` | `Noticias FIBRAs Inmobiliarias \| FIBRADIS` | `Últimas noticias y novedades sobre el mercado de FIBRAs inmobiliarias mexicanas. Actualización continua desde fuentes especializadas.` |
| `/conoce-las-fibras` | `¿Qué son las FIBRAs Inmobiliarias? Guía Completa \| FIBRADIS` | `Aprende qué son las FIBRAs inmobiliarias, cómo funcionan, cómo invertir y qué beneficios fiscales ofrecen. Guía para inversionistas.` |
| `/calendario` | `Calendario de Eventos Corporativos FIBRAs \| FIBRADIS` | `Próximas asambleas, distribuciones y eventos corporativos de FIBRAs inmobiliarias mexicanas. Mantente informado para tus decisiones.` |
| `/fundamentales` | `Fundamentales FIBRAs — Cap Rate, NAV, NOI \| FIBRADIS` | `Métricas fundamentales comparativas de FIBRAs: Cap Rate, NAV por CBFI, LTV, NOI Margin y más. Análisis cross-FIBRA actualizado.` |

### JSON-LD para /calculadora (FAQPage schema)
```json
{
  "@context": "https://schema.org",
  "@type": "FAQPage",
  "mainEntity": [
    {
      "@type": "Question",
      "name": "¿Las distribuciones de las FIBRAs pagan ISR?",
      "acceptedAnswer": {
        "@type": "Answer",
        "text": "Sí. Las distribuciones de FIBRAs están sujetas al ISR. La tasa de retención varía según el tipo de inversionista: personas físicas residentes en México tienen retención del 30%, y los residentes en el extranjero pueden tener tasas distintas según tratados fiscales."
      }
    },
    {
      "@type": "Question",
      "name": "¿Cómo se calcula el ISR de las distribuciones de FIBRAs?",
      "acceptedAnswer": {
        "@type": "Answer",
        "text": "El ISR se calcula sobre el monto de la distribución recibida por CBFI, multiplicado por el número de CBFIs que posees. La institución fiduciaria realiza la retención antes de depositar la distribución. La tasa efectiva depende de tu régimen fiscal y si presentas declaración anual."
      }
    },
    {
      "@type": "Question",
      "name": "¿Qué es el CBFI de una FIBRA?",
      "acceptedAnswer": {
        "@type": "Answer",
        "text": "CBFI es el Certificado Bursátil Fiduciario Inmobiliario, el instrumento de inversión de las FIBRAs. Cada CBFI representa una participación proporcional en el patrimonio del fideicomiso. Las distribuciones se calculan por CBFI."
      }
    }
  ]
}
```

### Exclusión de paths con extensión
Para detectar paths con extensión de archivo (assets), usar:
```csharp
var ext = Path.GetExtension(context.Request.Path.Value);
if (!string.IsNullOrEmpty(ext)) { await next(context); return; }
```

Esto excluye `.js`, `.css`, `.png`, `.svg`, `.ico`, `.woff2`, etc. automáticamente.

### SpaMetadataProvider — normalización de paths
El path puede llegar como `/calculadora`, `/calculadora/`, `/CALCULADORA`. Normalizar:
```csharp
var normalizedPath = context.Request.Path.Value?.TrimEnd('/').ToLowerInvariant() ?? "/";
if (normalizedPath == "") normalizedPath = "/";
```

### Archivo index.html
El `wwwroot/index.html` es el archivo compilado del frontend. Cuando el frontend se rebuilda (`npm run build`), este archivo se sobreescribe. El comentario `<!-- prerender-meta -->` debe estar presente en el `src/Web/Main/index.html` (fuente) y en el `wwwroot/index.html` (compilado). 

Verificar que ambos tienen el comentario — `src/Web/Main/index.html` es el que edita el desarrollador; `wwwroot/index.html` es el que sirve en producción.

### CalculadoraPage: nota sobre historia 10-2
La historia 10-2 ("calculadora-isr") implementará el formulario interactivo del calculador. Esta historia (11-2) solo crea el **static shell**: contenido informativo que Google puede indexar AHORA, antes de que el calculador esté implementado. Cuando 10-2 se implemente, reemplazará/extenderá este componente con la funcionalidad real.

## Dev Agent Record

### Archivos Creados/Modificados

**Nuevos:**
- `src/Server/Api/Seo/SpaPageMeta.cs` — record de metadata por ruta
- `src/Server/Api/Seo/ISpaMetadataProvider.cs` — interface del provider
- `src/Server/Api/Seo/SpaMetadataProvider.cs` — provider con las 8 rutas de la tabla + JSON-LD FAQPage para /calculadora
- `src/Server/Api/Middleware/SpaMetadataMiddleware.cs` — middleware de inyección server-side
- `src/Web/Main/src/modules/calculadora/CalculadoraPage.tsx` — static shell con contenido educativo ISR
- `tests/Unit/Infrastructure.Tests/Middleware/SpaMetadataMiddlewareTests.cs` — 10 tests del middleware
- `tests/Unit/Infrastructure.Tests/Seo/SpaMetadataProviderTests.cs` — tests del provider (normalización, longitudes, JSON-LD)

**Modificados:**
- `src/Server/Api/appsettings.json` — `App:BaseUrl = https://fibrasinmobiliarias.com`
- `src/Server/Api/appsettings.Development.json` — `App:BaseUrl = https://localhost:5001`
- `src/Server/Api/CompositionRoot/ApiServiceExtensions.cs` — registro `AddSingleton<ISpaMetadataProvider, SpaMetadataProvider>()`
- `src/Server/Api/Program.cs` — `UseMiddleware<SpaMetadataMiddleware>()` después de WwwToNonWww/HttpsRedirection y antes de `UseDefaultFiles()`
- `src/Web/Main/src/app/routes.tsx` — ruta `/calculadora` agregada junto a las rutas públicas
- `_bmad-output/implementation-artifacts/sprint-status.yaml` — 11-2 → review

**Modificados en code review (2026-06-11):**
- `src/Server/Api/Middleware/SpaMetadataMiddleware.cs` — guards GET/HEAD, comentario ausente, IOException; HTML-encoding; Cache-Control no-cache; BaseUrl fail-fast
- `src/Server/Api/Seo/SpaMetadataProvider.cs` — entrada `/herramientas` agregada (deuda 10-2)
- `src/Web/Main/src/modules/calculadora/CalculadoraPage.tsx` — metadata client-side retirada (el middleware es la fuente)
- `src/Web/Main/src/modules/herramientas/HerramientasPage.tsx` — metadata client-side retirada (incluía og:url con dominio incorrecto fibradis.mx)
- `src/Web/Main/src/modules/home/HomePage.tsx` y `src/Web/Main/src/modules/calendario/CalendarioPage.tsx` — canonical client-side con dominio incorrecto retirado
- `src/Web/Main/src/modules/ficha-publica/FibraPage.tsx` — dominio del canonical corregido a fibrasinmobiliarias.com
- `src/Web/Main/scripts/prerender.mjs` — nota LEGACY (el middleware es el mecanismo canónico)
- `tests/Unit/Infrastructure.Tests/Middleware/SpaMetadataMiddlewareTests.cs` — 8 tests nuevos de los patches
- `tests/Unit/Infrastructure.Tests/Seo/SpaMetadataProviderTests.cs` — `/herramientas` en theories, ruta dinámica de noticia como unknown

### Decisiones Tomadas

1. **Conflicto Task 5 con historia 10-2 (resuelto con el usuario):** la historia asumía que la calculadora interactiva (10-2) no existía, pero 10-2 ya está done y vive en `/herramientas`. El usuario decidió implementar el **story literal**: static shell en `/calculadora` con banner "Calculadora interactiva próximamente", sin embeber ni enlazar la calculadora de /herramientas.
2. **Extensión de CA-3 aprobada por el usuario:** `wwwroot/index.html` trae un `<title>Fibras Inmobiliarias</title>` estático; si solo se reemplazara el comentario, las rutas con metadata servirían DOS `<title>` y Google tomaría el primero (el genérico). Cuando hay metadata, el middleware también elimina el title estático (regex primera ocurrencia) para que quede exactamente uno. Rutas sin metadata reciben el HTML intacto.
3. **Formato de títulos (decisión del usuario):** keyword primero, marca al final (`Calculadora ISR FIBRAs — ... | FIBRADIS`), como define la tabla del story.
4. **Banner "próximamente" sin shadcn:** `Badge`/`Alert` no existen en `src/Web/Main/src/shared/ui/` y `npx shadcn add` está restringido por convenciones; se usó el mismo patrón de banner ámbar que HerramientasPage (div con `role="status"`).
5. **Guard de robustez en el middleware:** si `WebRootPath` es null/vacío o `index.html` no existe (p.ej. test host sin wwwroot), el middleware hace pass-through a `next` en lugar de lanzar excepción.
6. **Descripciones verificadas con contador:** las 8 meta descriptions de la tabla miden entre 127 y 154 caracteres (rango 120–160 exigido); hay test que lo garantiza (`Descriptions_AreBetween120And160Chars`).

### Tests Ejecutados

- TDD red-green: build del test project falló con CS0246 antes de implementar el middleware (red), 35/35 verdes después (green).
- `dotnet test tests/Unit/Infrastructure.Tests` → **364/364** (incluye 35 nuevos de SpaMetadata*)
- `dotnet test tests/Unit/Application.Tests` → **83/83**
- `dotnet test tests/Unit/Domain.Tests` → **8/8**
- `dotnet test tests/Integration/Api.Tests` → **272/272** (regresión del pipeline HTTP completa)
- `dotnet test tests/Integration/Jobs.Tests` → **2/2**
- `npm test --workspace=src/Web/Main` → **105/105**
- `dotnet build FIBRADIS.slnx` → 0 errores, 0 advertencias
- `npm run build --workspace=src/Web/Main` → 0 errores TypeScript
- Smoke test con curl en dev server (puerto 5265): `/calculadora` 200 con title/description/canonical/og/JSON-LD inyectados y un solo `<title>`; `/` con metadata de home; `/portafolio` con HTML original intacto (comentario presente); `/assets/*.js` servido como text/javascript sin intercepción; `/api/v1/fibras` 200.
- Nota: Persistence.Tests, Integrations.Tests y ApiCompatibility.Tests no tienen tests descubribles (condición pre-existente, no relacionada con esta historia).

### Change Log

- 2026-06-11 — Historia 11-2 implementada completa: middleware de inyección de metadata SEO server-side para 8 rutas públicas, configuración App:BaseUrl, static shell /calculadora con contenido educativo ISR, 45+ unit tests nuevos. Status → review.
- 2026-06-11 — Code review (3 capas adversariales): 6 patches de robustez al middleware (guard de comentario, GET/HEAD, HTML-encoding, Cache-Control, BaseUrl fail-fast, IOException), `/herramientas` agregada al provider resolviendo deuda 10-2 (og:url con dominio incorrecto), metadata client-side retirada de páginas cubiertas por el middleware, canonical de FibraPage corregido, prerender.mjs marcado LEGACY. 8 tests nuevos (377/377 Infrastructure, 272/272 Api integration, 105/105 frontend). Status → done.

## Senior Developer Review (AI)

**Fecha:** 2026-06-11 — Revisores: Blind Hunter, Edge Case Hunter, Acceptance Auditor (paralelos)

**Veredicto por CA:** CA-1 ✅ · CA-2 ✅ · CA-3 ✅ (con desviación aprobada Decisión 2) · CA-4 ✅ · CA-5 ✅ · CA-6 parcial (ver hallazgos D1/D2) · CA-7 ✅

### Review Findings

- [x] [Review][Decision→Defer] `/calculadora` es página huérfana — cero enlaces internos apuntan a ella. **Resolución del usuario:** se creará una funcionalidad posterior que la enlazará; mientras tanto el sitemap de 11-3 le dará descubribilidad. Registrado en deferred-work.md.
- [x] [Review][Decision→Patch] Doble fuente de metadata cliente/servidor (2 titles, 2 canonicals, og:title divergente tras hidratar React 19). **Resolución del usuario:** el servidor (middleware) es la única fuente de verdad en rutas cubiertas. Aplicado: `CalculadoraPage.tsx` y `HerramientasPage.tsx` ya no emiten title/meta/canonical/og; se retiraron los canonicals client-side con dominio incorrecto de `HomePage.tsx` y `CalendarioPage.tsx` (mismo defecto). El sweep del resto de meta client-side en páginas cubiertas quedó en deferred-work.md.
- [x] [Review][Decision→Patch] Coexistencia con `scripts/prerender.mjs`. **Resolución del usuario:** el middleware es el mecanismo canónico de producción; el deploy usa `npm run build` (no `build:full`). Documentado con nota LEGACY en la cabecera de `prerender.mjs`; el guard del comentario protege si se usara `build:full` por error.
- [x] [Review][Decision→Patch] [Deuda 10-2] `HerramientasPage.tsx:53` con `og:url` en dominio `fibradis.mx` y sin canonical. **Resolución del usuario:** parchear ahora. Aplicado: `/herramientas` agregada a `SpaMetadataProvider` (metadata server-side con dominio configurado) y la metadata client-side retirada del componente. Bonus del mismo defecto: `FibraPage.tsx:148` canonical corregido a fibrasinmobiliarias.com (ruta dinámica no cubierta por el middleware — su canonical client-side es el único).
- [x] [Review][Patch] Guard cuando falta `<!-- prerender-meta -->` — pass-through antes de mutar; test `PassesThrough_WhenPrerenderCommentMissing` [src/Server/Api/Middleware/SpaMetadataMiddleware.cs:87]
- [x] [Review][Patch] Filtrar método HTTP — solo GET/HEAD; tests `PassesThrough_ForNonGetOrHeadMethods` + `InjectsMetadata_ForHeadRequest` [src/Server/Api/Middleware/SpaMetadataMiddleware.cs:33]
- [x] [Review][Patch] HTML-encoding con `HtmlEncoder.Create(UnicodeRanges.All)` (deja pasar acentos/em-dash) y escape de `<` → `\u003c` en JsonLd; test `EncodesHtmlInTitleAndDescription_AndEscapesJsonLd` [src/Server/Api/Middleware/SpaMetadataMiddleware.cs:105-125]
- [x] [Review][Patch] `Cache-Control: no-cache` en la respuesta inyectada; test `SetsCacheControlNoCache_OnInjectedResponse` [src/Server/Api/Middleware/SpaMetadataMiddleware.cs:101]
- [x] [Review][Patch] `App:BaseUrl` requerido — fail-fast con `InvalidOperationException` al construir el pipeline; test `Constructor_Throws_WhenBaseUrlMissing` [src/Server/Api/Middleware/SpaMetadataMiddleware.cs:22-25]
- [x] [Review][Patch] `IOException` capturada en `File.ReadAllTextAsync` → pass-through; test `PassesThrough_WhenIndexHtmlLocked` [src/Server/Api/Middleware/SpaMetadataMiddleware.cs:74-83]
- [x] [Review][Defer] Caché en memoria de `index.html` — I/O de disco + regex en cada request de las 8 rutas (incluida `/`); el archivo solo cambia en deploy. Optimización, no bug — deferred
- [x] [Review][Defer] `Path.GetExtension` clasificará como asset cualquier slug con punto (`/noticias/fibra-sube-2.5`) — anotar como restricción de diseño en la historia 11-4 (slugs de noticias) — deferred
- [x] [Review][Defer] Variantes no normalizadas (`//calculadora`, `%2F`, punto final) sirven el template genérico sin canonical — contenido duplicado de bajo riesgo; el canonical de 11-3/sitemap mitiga — deferred
- [x] [Review][Defer] JSON-LD FAQPage incluye "¿Qué es el CBFI?" sin Q&A visible en la página — Google exige contenido visible para rich results FAQ; el JSON-LD vino literal del spec. Alinear contenido visible o recortar la pregunta cuando se toque la página — deferred
- [x] [Review][Defer] `GET /index.html` elude el middleware (extensión `.html`) y sirve el template crudo sin canonical — duplicado menor de la home; mitigable con 301 → `/` — deferred, pre-existing
