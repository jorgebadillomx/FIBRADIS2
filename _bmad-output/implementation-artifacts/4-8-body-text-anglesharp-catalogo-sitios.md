# Historia 4.8: Extracción body_text con AngleSharp + Catálogo de Sitios

Status: review

## Story

Como sistema de análisis de IA y como AdminOps,
quiero que `body_text` se extraiga usando un parser DOM real (AngleSharp) con soporte de selectores CSS y travesía correcta de árbol HTML,
y que exista un catálogo estático de sitios con selectores específicos para los principales portales de noticias financieras,
para que la calidad del `body_text` mejore significativamente sobre el enfoque de regex y las noticias lleguen limpias al pipeline de IA.

## Contexto y motivación

La historia 4.5.4 implementó una estrategia híbrida de extracción con regex. Fue un avance grande, pero tiene una **limitación arquitectural documentada**: regex no puede rastrear profundidad de anidamiento HTML. Esto produce tres fallas concretas en producción:

1. **`<article>` anidado** — WordPress y otros CMS ponen `<article>` en el bloque de "related posts"; el regex no-greedy cierra en el primer `</article>` y captura el bloque equivocado en lugar del cuerpo del artículo principal.
2. **Scan window truncado a 40k chars** — `TryExtractByContentClassStart` limita el análisis a 40,000 caracteres para evitar catastrofismo del regex; artículos largos (informes trimestrales) pierden la segunda mitad.
3. **Boilerplate anidado sobrevive** — Los regex de `<nav>` y `<header>` no alcanzan elementos semánticos anidados dentro de `<section>` o `<div class="layout">`.

La solución es reemplazar el parsing por regex con **AngleSharp** (parser DOM estándar W3C) y complementarlo con un **catálogo de selectores por sitio** que permita al equipo registrar manualmente el selector exacto de cada portal de noticias al descubrirlo.

## Acceptance Criteria

1. **AngleSharp reemplaza regex para parsing DOM**
   - Dado que el scraper descarga el HTML de un artículo,
   - cuando construye `body_text`,
   - entonces usa `AngleSharp` para parsear el HTML en un árbol DOM antes de cualquier extracción,
   - y los elementos de boilerplate (`nav`, `header`, `footer`, `aside`, `[role=navigation]`, `[role=banner]`, `[role=contentinfo]`, `[role=complementary]`, `[role=search]`) se eliminan del árbol DOM con `element.Remove()` antes de intentar la extracción.

2. **Bug de `<article>` anidado resuelto**
   - Dado que el HTML contiene un `<article>` principal con un bloque de "artículos relacionados" que incluye `<article>` anidados,
   - cuando el scraper extrae el contenido,
   - entonces el `body_text` contiene el cuerpo del artículo principal (el `<article>` externo) y no el texto de los artículos relacionados internos.
   - Este caso específico debe tener un test unitario explícito.

3. **Catálogo de sitios con selectores CSS específicos (`SiteExtractionCatalog`)**
   - Existe una clase estática `SiteExtractionCatalog` en `src/Server/Infrastructure/Integrations/Articles/SiteExtractionCatalog.cs`.
   - El catálogo contiene al menos **20 dominios** de portales de noticias financieras mexicanas e internacionales.
   - Para cada dominio se definen uno o más selectores CSS ordenados por especificidad (el más específico primero).
   - El catálogo **se consulta antes** de intentar selectores genéricos (`article`, `[itemprop=articleBody]`, `main`).
   - Si ningún selector del catálogo produce resultado útil, el extractor cae al flujo genérico de selección semántica y párrafos.
   - El matching de hostname soporta subdominio → dominio padre (ej. `economia.elfinanciero.com.mx` → `elfinanciero.com.mx`).

4. **Limpieza DOM de boilerplate por clase/atributo**
   - Además del boilerplate semántico (AC 1), se eliminan del DOM los nodos cuyas clases o atributos indican contenido no editorial:
     - Clases con términos: `related`, `newsletter`, `subscribe`, `cookie`, `share-bar`, `social`, `sidebar`, `comments`, `tags`, `advertisement`, `widget`, `promo`.
   - La eliminación ocurre antes de cualquier intento de extracción de contenido.

5. **Fallback de párrafos usa DOM**
   - El fallback heurístico extrae párrafos mediante `QuerySelectorAll("p")` sobre el DOM limpio en lugar de regex sobre el HTML raw.
   - El filtro `IsNavigationParagraph` existente se conserva aplicado sobre el `TextContent` de cada párrafo.

6. **Quality gate y longitud máxima preservados**
   - El quality gate existente (≥ 200 chars, no sentinel `"Google News"`) se mantiene sin cambios.
   - El límite de `MaxStoredChars = 16000` se mantiene.
   - El resultado retorna `null` cuando el texto no pasa el quality gate.

7. **Sin cambio en superficie pública ni BD**
   - `IArticleContentScraper.TryGetArticleTextAsync` mantiene su firma sin cambios.
   - `NewsPipelineJob`, `AiModeEndpoints` y los endpoints públicos no requieren modificación.
   - No hay migraciones EF Core en esta historia.

8. **Cobertura de pruebas actualizada**
   - Los tests existentes en `ArticleContentScraperTests.cs` siguen pasando (o se actualizan a la nueva API si cambia el comportamiento esperado).
   - Se agrega al menos un test que cubre el AC 2 (artículo anidado resuelto).
   - Se agrega al menos un test que cubre el AC 3 (selector del catálogo tiene prioridad sobre genérico).
   - Todos los tests de `dotnet test tests/Unit/Infrastructure.Tests/` pasan con 0 errores.

## Tasks / Subtasks

- [x] Task 1: Agregar dependencia AngleSharp al proyecto
  - [x] 1.1 En `Directory.Packages.props`, añadir:
    ```xml
    <PackageVersion Include="AngleSharp" Version="1.4.0" />
    ```
  - [x] 1.2 En `src/Server/Infrastructure/Infrastructure.csproj`, añadir:
    ```xml
    <PackageReference Include="AngleSharp" />
    ```
  - [x] 1.3 Verificar que `dotnet build FIBRADIS.slnx` pasa sin errores.

- [x] Task 2: Crear `SiteExtractionCatalog` con carga inicial de sitios
  - [x] 2.1 Crear `src/Server/Infrastructure/Integrations/Articles/SiteExtractionCatalog.cs` siguiendo el patrón de clase estática interna con diccionario privado.
  - [x] 2.2 Poblar el catálogo con los sitios definidos en Dev Notes (sección "Catálogo inicial de sitios").
  - [x] 2.3 Implementar `TryGetSelectors(string hostname, out string[] selectors)` con matching exacto y fallback a dominio padre (strip primer segmento hasta primer punto).
  - [x] 2.4 `dotnet build` pasa sin errores.

- [x] Task 3: Reescribir `ArticleContentScraper` con AngleSharp
  - [x] 3.1 Agregar `using AngleSharp; using AngleSharp.Dom;` al archivo.
  - [x] 3.2 Reemplazar `ExtractBodyText(string html)` para que:
    a. Parsee el HTML con `BrowsingContext.New(Configuration.Default).OpenAsync(req => req.Content(html))`.
    b. Llame a `RemoveBoilerplateNodes(document)` (nueva implementación DOM-based).
    c. Consulte `SiteExtractionCatalog` cuando `hostname` esté disponible — ver nota de diseño en Dev Notes.
    d. Intente selectores semánticos genéricos via `QuerySelector`.
    e. Caiga al fallback de párrafos via `QuerySelectorAll("p")`.
    f. Aplique quality gate sobre el texto resultante.
  - [x] 3.3 Implementar `RemoveBoilerplateNodes(IDocument document)`:
    - Remover por selectores semánticos: `nav, header, footer, aside, [role=navigation], [role=banner], [role=contentinfo], [role=complementary], [role=search]`
    - Remover por selectores de clase de boilerplate: ver lista completa en Dev Notes.
    - Usar `document.QuerySelectorAll(selector).ToList().ForEach(n => n.Remove())`.
  - [x] 3.4 Reemplazar `ExtractFromParagraphs` para usar `document.QuerySelectorAll("p")` y extraer `p.TextContent` de cada párrafo.
  - [x] 3.5 Eliminar los métodos `[GeneratedRegex]` que ya no se usen (los de `NavBlockRegex`, `HeaderBlockRegex`, `FooterBlockRegex`, `AsideBlockRegex`, `ArticleBlockRegex`, `ArticleBodyPropRegex`, `MainBlockRegex`, `ContentClassStartRegex`, `ParagraphContentRegex`). **Mantener** `TagRegex`, `WhitespaceRegex` y `NavKeywordRegex` que siguen siendo necesarios.
  - [x] 3.6 `dotnet build` pasa con 0 errores y 0 warnings relevantes.

- [x] Task 4: Actualizar y ampliar tests unitarios
  - [x] 4.1 Revisar todos los tests existentes en `ArticleContentScraperTests.cs`; la mayoría debería seguir pasando sin cambios si el comportamiento observable es el mismo. Si alguno falla por la nueva implementación, analizar si el test era incorrecto o si hay regresión.
  - [x] 4.2 Agregar test para AC 2: `TryGetArticleTextAsync_WithNestedArticle_ExtractsOuterArticleNotInner`
    - HTML: `<article>` principal con párrafos del artículo real, que contiene `<div class="related"><article>Artículo relacionado corto</article></div>`
    - Assert: el body contiene el texto principal, NO el texto del artículo relacionado anidado.
  - [x] 4.3 Agregar test para AC 3: `TryGetArticleTextAsync_WithCatalogMatch_UsesCatalogSelectorFirst`
    - Crear un `ArticleContentScraper` que reciba la URL de un dominio del catálogo (ej. `https://www.elfinanciero.com.mx/noticia`).
    - HTML: tiene `.nota-body` con contenido editorial Y un `<article>` con texto distinto.
    - Assert: el body contiene el texto de `.nota-body`, no el del `<article>`.
    - Nota de diseño: ver Dev Notes sobre cómo pasar el hostname al extractor.
  - [x] 4.4 Ejecutar `dotnet test tests/Unit/Infrastructure.Tests/Infrastructure.Tests.csproj` — deben pasar todos.

- [x] Task 5: Validación final
  - [x] 5.1 `dotnet build FIBRADIS.slnx` sin errores.
  - [x] 5.2 `dotnet test tests/Unit/Infrastructure.Tests/` — todos pasan. (84/84)
  - [x] 5.3 `dotnet test tests/Integration/Api.Tests/ --filter AiModeOpsEndpointTests` — todos pasan. (6/6)
  - [x] 5.4 Actualizar File List y Change Log de esta historia.
  - [x] 5.5 Actualizar `sprint-status.yaml`: `4-8-body-text-anglesharp-catalogo-sitios: review`.

## Dev Notes

### Dependencia AngleSharp

- **Paquete**: `AngleSharp` v1.4.0 (MIT, ~300 KB, sin transitive deps problemáticos)
- **No se necesita** `AngleSharp.Css` ni `AngleSharp.Io` — el parsing de HTML puro con `Configuration.Default` es suficiente.
- **Context7 ID**: `/anglesharp/anglesharp` — consultar si necesitas documentación actualizada durante la implementación.

### API clave de AngleSharp (confirmada con context7)

```csharp
using AngleSharp;
using AngleSharp.Dom;

// Parsear HTML string
IConfiguration config = Configuration.Default;
IBrowsingContext context = BrowsingContext.New(config);
IDocument document = await context.OpenAsync(req => req.Content(htmlString));

// Eliminar nodos del DOM
document.QuerySelectorAll("nav, header, footer, aside").ToList().ForEach(n => n.Remove());

// Selector único
IElement? article = document.QuerySelector("article");

// Texto del nodo (incluye whitespace, normalizar con regex existente)
string rawText = article?.TextContent ?? string.Empty;

// Párrafos
IHtmlCollection<IElement> paragraphs = document.QuerySelectorAll("p");
foreach (var p in paragraphs)
{
    string text = p.TextContent; // texto sin tags
}
```

**`TextContent` vs `Text()`**: usar `TextContent` (propiedad DOM estándar). Aplica el `WhitespaceRegex` existente para normalizar.

**`element.Remove()`**: disponible en `AngleSharp.Dom`. Desvincula el nodo del árbol sin lanzar excepción si ya fue removido.

### Diseño: pasar hostname al extractor (Task 3.3)

El método actual `ExtractBodyText(string html)` no conoce el hostname. Para que el catálogo funcione, hay dos opciones:

**Opción A (recomendada):** Cambiar la firma de `ExtractBodyText` a interna (ya es `internal static`) para aceptar un hostname opcional:
```csharp
internal static string? ExtractBodyText(string html, string? hostname = null)
```
`TryGetArticleTextAsync` ya conoce la `effectiveUrl` (después de resolver Google News), así que puede extraer el hostname y pasarlo:
```csharp
var host = Uri.TryCreate(effectiveUrl, UriKind.Absolute, out var u) ? u.Host : null;
var bodyText = ExtractBodyText(html, host);
```

**Opción B:** Refactorizar `ExtractBodyText` a un método de instancia. Más cambio, menos recomendado.

Usar **Opción A**.

### Diseño: `RemoveBoilerplateNodes` — selector CSS combinado

Usar un único `QuerySelectorAll` con selector combinado para máxima eficiencia:

```csharp
private static void RemoveBoilerplateNodes(IDocument document)
{
    // Elementos semánticos de boilerplate
    const string SemanticBoilerplate =
        "nav, header, footer, aside, " +
        "[role=navigation], [role=banner], [role=contentinfo], " +
        "[role=complementary], [role=search]";

    // Clases de boilerplate — buscar en class attribute con 'contains word' selector
    // AngleSharp soporta [class*=valor] pero es substring, no word boundary.
    // Alternativa: seleccionar por clases conocidas exactas o usar múltiples selectores.
    const string ClassBoilerplate =
        ".related-articles, .related-posts, .newsletter, .newsletter-cta, " +
        ".subscribe, .subscription-cta, .cookie-banner, .cookie-notice, " +
        ".share-bar, .social-share, .sidebar, .widget, .advertisement, " +
        ".promo-box, .tags-section, .comments-section, " +
        "[data-component=related-articles], [data-component=newsletter]";

    var combined = SemanticBoilerplate + ", " + ClassBoilerplate;
    document.QuerySelectorAll(combined).ToList().ForEach(n => n.Remove());
}
```

**Nota**: `[class*=related]` (substring) es menos preciso que selectores de clase exactos. Preferir nombres de clase completos cuando se conozcan. La lista puede extenderse sin tocar el extractor principal.

### Diseño: `TryExtractByContentClassStart` (Phase 2 en 4.5.4)

Esta fase se puede simplificar significativamente. En lugar de regex con scan window, AngleSharp permite buscar directamente por clases:

```csharp
private static IElement? TryExtractByContentClass(IDocument document)
{
    var selectors = new[]
    {
        // WordPress / genéricos
        ".article-body", ".article-content", ".article__body", ".article__content",
        ".entry-content", ".entry__content", ".post-content", ".post__content",
        ".story-body", ".story__body", ".content-body",
        // CMS españoles / latinoamericanos
        ".nota-body", ".nota-cuerpo", ".nota-contenido", ".cuerpo-nota",
        ".articulo-cuerpo", ".editorial-content",
        // Drupal
        ".field-body", ".field--name-body",
    };

    foreach (var sel in selectors)
    {
        var el = document.QuerySelector(sel);
        if (el != null) return el;
    }
    return null;
}
```

### Catálogo inicial de sitios (`SiteExtractionCatalog`)

El catálogo se pobla con **selectores seed** basados en estructuras CMS conocidas. Durante la implementación, el Dev Agent **debe verificar al menos 3-5 sitios** abriendo una URL real en DevTools (o con `curl | grep`) para confirmar que el selector existe en el HTML real. Los que no se puedan verificar se dejan como best-effort con nota.

```csharp
// Formato del diccionario: hostname (sin www) → string[] de selectores CSS ordenados
// El extractor intenta cada selector en orden, usa el primero que produce texto ≥ MinUsefulLength
private static readonly Dictionary<string, string[]> Catalog =
    new(StringComparer.OrdinalIgnoreCase)
{
    // ── Financieras mexicanas ───────────────────────────────────────────────
    ["elfinanciero.com.mx"]  = [".nota-body", ".article-body", ".entry-content", ".nota-contenido"],
    ["expansion.mx"]         = [".article-body", ".content-article", "[data-article-body]", ".entry-content"],
    ["eleconomista.com.mx"]  = [".story__content", ".notaContainer__body", ".nota-body", ".article-body"],
    ["milenio.com"]          = [".article-content", ".cuerpo-nota", ".nota-contenido", ".body-content"],
    ["excelsior.com.mx"]     = [".nota-body", ".news-body", ".article-body", ".article-text"],
    ["eluniversal.com.mx"]   = [".field-items", ".article-body", ".nota-cuerpo", ".entry-content"],
    ["reforma.com"]          = [".article-content", ".nota-body", ".notaBody", ".nota-text"],
    ["jornada.com.mx"]       = [".textonota", ".articulo", ".article-content", ".nota-body"],
    ["proceso.com.mx"]       = [".article-body", ".texto-nota", ".entry-content", ".nota-body"],
    ["heraldo.mx"]           = [".article-body", ".nota-body", ".content-article"],
    ["publimetro.com.mx"]    = [".article-body", ".entry-content", ".nota-body"],

    // ── Especializadas en real estate / finanzas MX ──────────────────────────
    ["inmobiliare.com"]      = [".entry-content", ".article-body", ".post-content"],
    ["realestate.com.mx"]    = [".entry-content", ".article-body", ".post-content"],
    ["obras.expansion.mx"]   = [".article-body", ".content-article", ".entry-content"],

    // ── Internacionales frecuentes en feeds de FIBRAs ───────────────────────
    ["infobae.com"]          = [".article-body", ".story-article-body", ".body-article", ".article__body"],
    ["reuters.com"]          = ["[data-testid='article-body']", ".article-body__content", ".StandardArticleBody_body"],
    ["bloomberg.com"]        = [".body-content", ".article-body", ".fence-body"],
    ["marketwatch.com"]      = [".article__body", ".article-content", ".full-story"],
    ["wsj.com"]              = [".article-content", "[data-type='article']", ".wsj-snippet-body"],
    ["investing.com"]        = [".articlePage", ".WYSIWYG", ".articleText"],
    ["ft.com"]               = [".article__content-body", ".n-content-body", ".article-body"],
    ["bnnbloomberg.ca"]      = [".article-body", ".article__body", ".content-body"],
};
```

**Metodología de verificación** (para el Dev Agent durante implementación):
1. Tomar una URL real de cada sitio (puede ser cualquier artículo de noticias financieras).
2. En la terminal: `curl -s "<url>" | grep -o 'class="[^"]*article[^"]*"' | head -20`
3. Identificar la clase del contenedor principal del artículo.
4. Si difiere del seed, actualizar el catálogo antes de commitear.

Los sitios que no se puedan verificar durante esta historia se marcan con un comentario `// best-effort — verificar` y se actualizan en stories futuras conforme se detecten misses.

### Preservar: `StripTagsAndNormalize`, `IsNavigationParagraph`, `NavKeywordRegex`

Estas funciones no dependen de regex de HTML y siguen siendo válidas. Solo cambia *cómo se alimentan* (texto desde `TextContent` en lugar de regex match).

```csharp
private static string StripTagsAndNormalize(string html)
{
    // En el nuevo flujo, este método solo se llama si hay HTML residual en TextContent
    // (normalmente TextContent ya está sin tags). Mantener por compatibilidad.
    var text = TagRegex().Replace(html, " ");
    text = WebUtility.HtmlDecode(text);
    return WhitespaceRegex().Replace(text, " ").Trim();
}
```

Para los párrafos extraídos con `p.TextContent` ya no es necesario `StripTagsAndNormalize` — `TextContent` retorna texto plano. Solo aplicar `WebUtility.HtmlDecode` + `WhitespaceRegex`.

### Archivos impactados

| Archivo | Cambio |
|---|---|
| `Directory.Packages.props` | Agregar `AngleSharp` v1.4.0 |
| `src/Server/Infrastructure/Infrastructure.csproj` | Agregar `<PackageReference Include="AngleSharp" />` |
| `src/Server/Infrastructure/Integrations/Articles/ArticleContentScraper.cs` | Reescribir lógica de extracción usando AngleSharp |
| `src/Server/Infrastructure/Integrations/Articles/SiteExtractionCatalog.cs` | **NUEVO** — catálogo estático de selectores por sitio |
| `tests/Unit/Infrastructure.Tests/Integrations/Articles/ArticleContentScraperTests.cs` | Ampliar con tests de AC 2 y AC 3 |

### Restricciones

- No hay cambios a `IArticleContentScraper` (contrato público)
- No hay migraciones EF Core
- No hay cambios en endpoints, pipelines ni frontend
- El guard SSRF / DNS / `IsAllowedHostAsync` no se toca — está por encima del HTML parsing
- `AngleSharp` es la única dependencia nueva; no agregar `HtmlAgilityPack`, `CsQuery` ni ninguna otra librería de parsing HTML
- Seguir convención CPM: versión solo en `Directory.Packages.props`, sin versión inline en `.csproj`

### Regresiones a vigilar

- `TryGetArticleTextAsync_WhenUrlIsGoogleNews_UsesDecodedPublisherUrl` — verifica integración con `IGoogleNewsUrlDecoder`; no debe romper
- `TryGetArticleTextAsync_WhenContentTooShort_ReturnsNull` — quality gate; no debe romper
- `ArticleContentScraper` sigue siendo `partial class` si quedan `[GeneratedRegex]` en uso

## Testing

```bash
dotnet test tests/Unit/Infrastructure.Tests/Infrastructure.Tests.csproj --configuration Release
dotnet test tests/Integration/Api.Tests/Api.Tests.csproj --filter AiModeOpsEndpointTests --configuration Release
```

## Project Structure Notes

- Branch de implementación: `story/4-8-body-text-anglesharp-catalogo` (creado desde `main`)
- Módulo: `News` — capa `Infrastructure/Integrations/Articles`
- Schema BD: sin cambios
- No se requieren cambios al contrato público `NewsArticleDto`

## Completion Note

Ultimate context engine analysis completed - comprehensive developer guide created.

## Dev Agent Record

### Completion Notes

- Reescrito `ArticleContentScraper` con AngleSharp v1.4.0: parsing DOM real W3C, sin regex de estructuras HTML.
- `ExtractBodyText(string)` → `ExtractBodyTextAsync(string, string?)` (async + hostname opcional para catálogo).
- `RemoveBoilerplateNodes` elimina `script/style/noscript/iframe/svg` + boilerplate semántico (nav/header/footer/aside/roles) + clases de contenido no editorial (`.related`, `.sidebar`, `.advertisement`, etc.).
- Pipeline de extracción: (1) catálogo por sitio → (2) selectores semánticos (article/[itemprop]/main) → (3) selectores de clase CMS → (4) densidad de párrafos.
- `SiteExtractionCatalog` creado con 22 dominios (11 financieras MX + 3 especializadas RE + 8 internacionales). `TryGetSelectors` con matching exacto + strip de primer subdomain.
- Bug de `<article>` anidado resuelto: la clase `.related` se elimina del DOM antes de extraer, evitando que el `<article>` interno del bloque relacionados contamine el TextContent del artículo principal.
- Eliminados 13 métodos `[GeneratedRegex]` obsoletos (NavBlock, HeaderBlock, FooterBlock, AsideBlock, ArticleBlock, ArticleBodyProp, MainBlock, ContentClassStart, ParagraphContent, Script, Style, Svg, HtmlComment). Conservados: `TagRegex`, `WhitespaceRegex`, `NavKeywordRegex`.
- **Tests**: 84/84 unitarios + 6/6 integración AiModeOps. 2 tests nuevos: AC 2 (nested article) y AC 3 (catalog priority).
- Comandos ejecutados: `dotnet test tests/Unit/Infrastructure.Tests/Infrastructure.Tests.csproj --configuration Release` y `dotnet test tests/Integration/Api.Tests/Api.Tests.csproj --filter AiModeOpsEndpointTests --configuration Release`.

## File List

- `Directory.Packages.props` — agregado `AngleSharp` v1.4.0
- `src/Server/Infrastructure/Infrastructure.csproj` — agregado `<PackageReference Include="AngleSharp" />`
- `src/Server/Infrastructure/Integrations/Articles/ArticleContentScraper.cs` — reescrito con AngleSharp DOM
- `src/Server/Infrastructure/Integrations/Articles/SiteExtractionCatalog.cs` — NUEVO, catálogo estático 22 dominios
- `tests/Unit/Infrastructure.Tests/Integrations/Articles/ArticleContentScraperTests.cs` — 2 tests nuevos (AC 2 y AC 3)

## Change Log

- feat(story-4.8): AngleSharp reemplaza regex para parsing DOM en ArticleContentScraper (2026-05-22)
- feat(story-4.8): SiteExtractionCatalog con 22 dominios de portales financieros MX e internacionales (2026-05-22)
- fix(story-4.8): bug de article anidado resuelto — remoción DOM de .related antes de extracción (2026-05-22)
- test(story-4.8): tests de AC 2 (nested article) y AC 3 (catalog priority) — 84/84 pasan (2026-05-22)
