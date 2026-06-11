# Historia 11.2: SpaMetadataInjectionMiddleware + Static Shell Calculadora

Status: backlog

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

- [ ] Task 1: Agregar configuración `App:BaseUrl` a `appsettings.json`
  - [ ] Agregar en `src/Server/Api/appsettings.json`:
    ```json
    "App": {
      "BaseUrl": "https://fibrasinmobiliarias.com"
    }
    ```
  - [ ] Agregar también en `appsettings.Development.json`:
    ```json
    "App": {
      "BaseUrl": "https://localhost:5001"
    }
    ```

- [ ] Task 2: Crear modelos `SpaPageMeta` y `ISpaMetadataProvider`
  - [ ] Crear `src/Server/Api/Seo/SpaPageMeta.cs`:
    ```csharp
    namespace Api.Seo;
    public record SpaPageMeta(
        string Title,
        string Description,
        string CanonicalPath,  // e.g. "/calculadora" — el middleware prefija con BaseUrl
        string? JsonLd = null
    );
    ```
  - [ ] Crear `src/Server/Api/Seo/ISpaMetadataProvider.cs`:
    ```csharp
    namespace Api.Seo;
    public interface ISpaMetadataProvider
    {
        SpaPageMeta? GetMetaForPath(string path);
    }
    ```
  - [ ] Crear `src/Server/Api/Seo/SpaMetadataProvider.cs` con implementación:
    - Dictionary readonly con las rutas conocidas (ver tabla abajo)
    - `GetMetaForPath` normaliza el path (lowercase, trailing slash strip) antes de buscar
    - Retorna `null` si no hay entrada para la ruta

- [ ] Task 3: Crear `SpaMetadataMiddleware.cs`
  - [ ] Crear `src/Server/Api/Middleware/SpaMetadataMiddleware.cs` en namespace `Api.Middleware`
  - [ ] Constructor: `(RequestDelegate next, ISpaMetadataProvider metadataProvider, IWebHostEnvironment env, IConfiguration config)`
  - [ ] `InvokeAsync`: 
    - Si el path tiene extensión (`.js`, `.css`, `.png`, etc.) → `await next(context)` y return
    - Si el path empieza con `/api/` o `/ops/` o `/hangfire/` → `await next(context)` y return
    - Llamar `metadataProvider.GetMetaForPath(path)`
    - Si es null → `await next(context)` y return
    - Si tiene metadata: leer `wwwroot/index.html`, reemplazar `<!-- prerender-meta -->`, escribir respuesta
  - [ ] Usar `IWebHostEnvironment.WebRootPath` para localizar `index.html`
  - [ ] Leer el archivo `index.html` con `File.ReadAllTextAsync`
  - [ ] Construir el bloque HTML de metadata (ver spec en Dev Notes)
  - [ ] Reemplazar `<!-- prerender-meta -->` con el bloque construido
  - [ ] Setear `context.Response.ContentType = "text/html; charset=utf-8"` y escribir la respuesta
  - [ ] Hacer return para short-circuit (no llamar `next`)

- [ ] Task 4: Registrar middleware y servicio en `Program.cs` / `ApiServiceExtensions.cs`
  - [ ] Agregar en `ApiServiceExtensions.cs`:
    ```csharp
    builder.Services.AddSingleton<ISpaMetadataProvider, SpaMetadataProvider>();
    ```
  - [ ] En `Program.cs`, agregar `app.UseMiddleware<SpaMetadataMiddleware>();` DESPUÉS de `UseMiddleware<WwwToNonWwwMiddleware>()` (historia 11-1) y ANTES de `UseDefaultFiles()`
  - [ ] El orden correcto del pipeline:
    ```
    UseHttpsRedirection → WwwToNonWwwMiddleware → SpaMetadataMiddleware → UseDefaultFiles → UseStaticFiles → UseRouting → ...
    ```
  - [ ] NOTA: Si 11-1 no está implementada aún, colocar SpaMetadataMiddleware como primera línea después de `var app = builder.Build()`

- [ ] Task 5: Crear `CalculadoraPage.tsx`
  - [ ] Crear `src/Web/Main/src/modules/calculadora/CalculadoraPage.tsx`
  - [ ] La página debe incluir React 19 native metadata:
    ```tsx
    <title>Calculadora ISR FIBRAs — Impuesto sobre la Renta | FIBRADIS</title>
    <meta name="description" content="Calcula el Impuesto Sobre la Renta (ISR) de tus distribuciones de FIBRAs inmobiliarias mexicanas. Herramienta gratuita con base en la Ley del ISR vigente." />
    <link rel="canonical" href="https://fibrasinmobiliarias.com/calculadora" />
    <meta property="og:title" content="Calculadora ISR FIBRAs | FIBRADIS" />
    <meta property="og:description" content="Calcula el ISR de tus distribuciones de FIBRAs inmobiliarias. Herramienta gratuita." />
    <meta property="og:type" content="website" />
    ```
  - [ ] Contenido estático (NO formulario interactivo — el calculador real es historia 10-2):
    - H1: "Calculadora ISR para FIBRAs Inmobiliarias"
    - Párrafo introductorio explicando qué es el ISR en FIBRAs (~100 palabras)
    - Sección "¿Qué es el ISR en las distribuciones de FIBRAs?" con explicación
    - Sección "¿Cómo se calcula el ISR de distribuciones?" con la fórmula básica
    - Sección "Tasa de retención aplicable" (tabla simple con tasas)
    - Badge o banner: "Calculadora interactiva próximamente" (shadcn Badge o Alert)
  - [ ] Usar clases Tailwind v4 — NO usar clases de v3 que no existan en v4
  - [ ] Importar desde `@/shared/ui/` los componentes shadcn necesarios (Badge, etc.)

- [ ] Task 6: Agregar ruta `/calculadora` a `routes.tsx`
  - [ ] Agregar import: `import { CalculadoraPage } from '@/modules/calculadora/CalculadoraPage'`
  - [ ] Agregar ruta en el array de `children` de PublicLayout:
    ```tsx
    { path: '/calculadora', element: <CalculadoraPage /> },
    ```
  - [ ] Colocarla junto a las otras rutas públicas (antes de ProtectedRoute)

- [ ] Task 7: Unit tests para `SpaMetadataMiddleware`
  - [ ] Archivo: agregar a `tests/Unit/Infrastructure.Tests/` o crear clase en el proyecto de unit tests existente
  - [ ] `InjectsMetadata_ForKnownPath()` — verifica que la response contiene el title correcto para `/calculadora`
  - [ ] `PassesThrough_ForUnknownPath()` — verifica que se llama `next` para `/portafolio`
  - [ ] `PassesThrough_ForAssets()` — verifica que se llama `next` para `/assets/main.js`
  - [ ] `PassesThrough_ForApiPrefix()` — verifica que se llama `next` para `/api/v1/fibras`
  - [ ] `ReplacesPrerendMetaComment()` — verifica que `<!-- prerender-meta -->` es reemplazado
  - [ ] Usar mocks de `IWebHostEnvironment` con un `index.html` template en memoria o temp file

- [ ] Task 8: Verificación build TypeScript
  - [ ] Ejecutar `npm run build --workspace=src/Web/Main`
  - [ ] Verificar 0 errores TypeScript y 0 advertencias de `noUnusedLocals`
  - [ ] Ejecutar `dotnet build FIBRADIS.slnx` y verificar 0 errores

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

_(A completar durante la implementación)_

### Archivos Creados/Modificados
- (pendiente)

### Decisiones Tomadas
- (pendiente)

### Tests Ejecutados
- (pendiente)

## Senior Developer Review (AI)

_(A completar durante el code review)_
