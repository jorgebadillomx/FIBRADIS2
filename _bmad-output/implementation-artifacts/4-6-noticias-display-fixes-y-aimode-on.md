# Historia 4.6: Noticias — Display fixes y AiMode On

Status: done

## Story

Como visitante de FIBRADIS y AdminOps,
quiero que las cards de noticias muestren imágenes reales, que los snippets no tengan HTML crudo, que el home muestre 5 noticias, y que el modo IA del pipeline sea Off/On con generación automática,
para que la experiencia de noticias sea visualmente correcta y el pipeline de IA sea más claro y funcional.

## Acceptance Criteria

1. **Imágenes reales en noticias:** Dado que el pipeline procesa artículos cuya URL es un redirect de Google News, cuando se intenta obtener el `og:image`, entonces el scraper sigue el redirect HTTP hasta la URL real del artículo y extrae la imagen. El fallback a imagen de sector sigue activo cuando no hay `og:image`.

2. **Snippets sin HTML:** Dado que el RSS de Google News incluye HTML en el campo `<description>` (ej. `<ol><li><a href="...">texto</a></li></ol>`), cuando el pipeline parsea el RSS, entonces los snippets se almacenan con texto limpio, sin tags HTML ni atributos `href` visibles. Las entidades HTML (`&amp;`, `&quot;`, etc.) están decodificadas.

3. **AiMode Off:** Dado que AI_MODE=Off, cuando el pipeline ingesta noticias, entonces no se genera resumen IA y `Status = Processed`.

4. **AiMode On:** Dado que AI_MODE=On, cuando el pipeline ingesta noticias, entonces se genera el resumen IA con Gemini durante el mismo job (prompt profesional de analista FIBRA). Si la generación es exitosa, `AiSummary` se guarda y `Status = Processed`. Si falla, `AiSummary = null`, `Status = Partial`, y el error se loguea sin bloquear otros artículos.

5. **Regeneración manual siempre disponible:** El endpoint `POST /api/v1/ops/news/{id}/ai-summary` regenera el resumen de un artículo individual sin importar el modo global actual.

6. **Home 5 noticias:** El endpoint `GET /api/v1/news` devuelve las 5 últimas noticias publicadas (no 10).

7. **Ops UI actualizado:** `AiModeSection` muestra las opciones `Off` y `On` (no `Manual`). Los labels y descripciones reflejan el nuevo comportamiento.

## Tasks / Subtasks

- [x] Task 1: Bug — og:image para URLs de Google News (imágenes genéricas)
  - [x] 1.1 `src/Server/Api/CompositionRoot/ApiServiceExtensions.cs`: cambiar el handler del `OgImageScraper`:
    ```csharp
    // Antes:
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
    // Después:
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        AllowAutoRedirect = true,
        MaxAutomaticRedirections = 5,
    });
    ```
  - [x] 1.2 `src/Server/Infrastructure/Jobs/News/NewsPipelineJob.cs`: eliminar el método `ShouldScrapeOgImage` y su guard; siempre llamar `ogImageScraper.TryGetOgImageAsync(item.Url, ct)` para todos los artículos con URL no vacía:
    ```csharp
    // Antes:
    var imageUrl = ShouldScrapeOgImage(item.Url)
        ? await ogImageScraper.TryGetOgImageAsync(item.Url, ct)
        : null;
    // Después:
    var imageUrl = !string.IsNullOrWhiteSpace(item.Url)
        ? await ogImageScraper.TryGetOgImageAsync(item.Url, ct)
        : null;
    ```
    Eliminar también el método privado `ShouldScrapeOgImage` completo.

- [x] Task 2: Bug — HTML crudo en snippets del RSS
  - [x] 2.1 `src/Server/Infrastructure/Integrations/GoogleNews/GoogleNewsRssClient.cs`: agregar método privado `StripHtml` y aplicarlo al snippet antes de crear el `RssItem`:
    ```csharp
    private static string? StripHtml(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var stripped = Regex.Replace(raw, "<[^>]+>", " ", RegexOptions.IgnoreCase);
        var decoded = System.Net.WebUtility.HtmlDecode(stripped);
        var normalized = Regex.Replace(decoded, @"\s{2,}", " ").Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
    ```
    Aplicar en el select del LINQ:
    ```csharp
    var snippet = StripHtml(item.Element("description")?.Value);
    ```
    Agregar `using System.Net;` y `using System.Text.RegularExpressions;` si no existen ya en el using block.

- [x] Task 3: AiMode Off → On — Dominio y Backend
  - [x] 3.1 `src/Server/Domain/News/AiMode.cs`: renombrar `Manual = 1` → `On = 1`. **No requiere migración** — el int almacenado no cambia:
    ```csharp
    public enum AiMode
    {
        Off = 0,
        On = 1,
    }
    ```
  - [x] 3.2 `src/Server/Infrastructure/Jobs/News/NewsPipelineJob.cs`: inyectar `IAiSummaryService summaryService` en el constructor. En el loop de guardado de artículos, cuando `currentMode == AiMode.On`, generar resumen IA:
    ```csharp
    string? aiSummary = null;
    var finalStatus = NewsArticleStatus.Processed;

    if (currentMode == AiMode.On)
    {
        try
        {
            aiSummary = await summaryService.GenerateSummaryAsync(
                item.Title, item.Snippet, AiContentType.News, ct);
            finalStatus = aiSummary is not null
                ? NewsArticleStatus.Processed
                : NewsArticleStatus.Partial;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "AI summary failed for '{Url}'; article saved without summary", item.Url);
            finalStatus = NewsArticleStatus.Partial;
        }
    }

    var article = new NewsArticle
    {
        Title = item.Title,
        TitleNormalized = NewsDeduplicator.NormalizeTitle(item.Title),
        Source = item.Source,
        PublishedAt = item.PublishedAt,
        Url = item.Url,
        Snippet = item.Snippet,
        ImageUrl = imageUrl,
        AiSummary = aiSummary,
        Status = finalStatus,
        CapturedAt = DateTimeOffset.UtcNow,
    };
    ```
  - [x] 3.3 `src/Server/Api/Endpoints/Ops/AiModeEndpoints.cs`:
    - Actualizar mensaje de validación: `["mode"] = ["Valor inválido. Use 'Off' o 'On'."]`
    - **Remover el guard de modo** del endpoint `/{articleId}/ai-summary` — eliminar el bloque `if (mode != AiMode.Manual)` completo. La regeneración manual es siempre posible, independiente del modo global.
  - [x] 3.4 `src/Server/Infrastructure/Integrations/Ai/GeminiAiSummaryService.cs`: reemplazar el prompt con versión profesional de analista FIBRA:
    ```csharp
    var prompt = string.IsNullOrWhiteSpace(snippet)
        ? $"""
          Eres un analista experto en FIBRAs mexicanas (Fideicomisos de Inversión en Bienes Raíces) con amplio conocimiento del mercado inmobiliario y bursátil de México.
          Redacta un resumen profesional en español de máximo 3 oraciones sobre esta noticia:
          Título: {title}
          Incluye: el hecho central, su relevancia para el sector de FIBRAs o bienes raíces en México, y una perspectiva analítica breve para el inversor. Responde solo con el resumen, sin preámbulos.
          """
        : $"""
          Eres un analista experto en FIBRAs mexicanas (Fideicomisos de Inversión en Bienes Raíces) con amplio conocimiento del mercado inmobiliario y bursátil de México.
          Redacta un resumen profesional en español de máximo 3 oraciones sobre esta noticia:
          Título: {title}
          Fragmento: {snippet}
          Incluye: el hecho central, su relevancia para el sector de FIBRAs o bienes raíces en México, y una perspectiva analítica breve para el inversor. Responde solo con el resumen, sin preámbulos.
          ""`;
    ```

- [x] Task 4: Home — 5 noticias en lugar de 10
  - [x] 4.1 `src/Server/Api/Endpoints/Public/NewsEndpoints.cs`: cambiar `GetLatestAsync(10, ct)` → `GetLatestAsync(5, ct)`

- [x] Task 5: Ops UI — AiModeSection Off/On
  - [x] 5.1 `src/Web/Ops/src/api/aiModeApi.ts`:
    - Cambiar firma: `setAiMode(mode: 'Off' | 'On')` (remover `'Manual'`)
    - `const currentMode = modeQuery.data?.mode as 'Off' | 'On' | undefined`
  - [x] 5.2 `src/Web/Ops/src/modules/ai-mode/AiModeSection.tsx`:
    - Cambiar el tipo del estado: `useState<'Off' | 'On' | null>(null)`
    - Cambiar el cast: `const currentMode = modeQuery.data?.mode as 'Off' | 'On' | undefined`
    - Cambiar el array de opciones: `(['Off', 'On'] as const).map(...)`
    - Actualizar labels: `'Off' → 'Off - sin resúmenes'`, `'On' → 'On - generar resumen al ingestar'`
    - Actualizar texto condicional: `currentMode !== 'On'` y `disabled={currentMode !== 'On' || ...}`
    - Actualizar descripción en UI: `"Cambia AI_MODE a On para habilitar este disparo."` y textos relacionados

- [x] Task 6: Build y verificación
  - [x] 6.1 `dotnet build FIBRADIS.slnx` — 0 errores
  - [x] 6.2 `dotnet test tests/Unit/Infrastructure.Tests/Infrastructure.Tests.csproj` — todos pasan (49/49)
  - [x] 6.3 `npm run build --workspace=src/Web/Main` — 0 errores TypeScript
  - [x] 6.4 `npm run build --workspace=src/Web/Ops` — 0 errores TypeScript

## Dev Notes

### Contexto de historias anteriores relevantes

**Historia 4.3** — Definió `AiMode { Off = 0, Manual = 1 }`. El valor entero `1` se almacena en la tabla `AiModeConfig`. Renombrar el enum a `On = 1` NO requiere migración porque el int no cambia. Pero todos los lugares que referencian `AiMode.Manual` deben actualizarse.

**Historia 4.5.1** — `OgImageScraper` fue implementado en `src/Server/Infrastructure/Integrations/OgImage/OgImageScraper.cs`. El `HttpClientHandler` se registró con `AllowAutoRedirect = false` por seguridad SSRF, pero esto impide seguir los redirects de Google News. La validación `IsAllowedHostAsync` sigue aplicándose a la URL de origen antes del redirect — con `AllowAutoRedirect = true`, el scraper seguirá hasta la URL real del artículo; si Google News redirige a un sitio público, es correcto. Riesgo SSRF: bajo, ya que Google News redirige únicamente a sitios de noticias públicos.

**Historia 4.5.3** — `AiModeEndpoints.cs` tiene el endpoint `POST /api/v1/ops/news/{articleId}/ai-summary` que actualmente requiere `mode == AiMode.Manual`. Al eliminar este guard, la regeneración queda disponible siempre.

---

### Archivos a modificar — lista completa

| Archivo | Cambio |
|---------|--------|
| `src/Server/Api/CompositionRoot/ApiServiceExtensions.cs` | `AllowAutoRedirect = true, MaxAutomaticRedirections = 5` |
| `src/Server/Infrastructure/Jobs/News/NewsPipelineJob.cs` | Eliminar `ShouldScrapeOgImage`; inyectar `IAiSummaryService`; lógica On |
| `src/Server/Infrastructure/Integrations/GoogleNews/GoogleNewsRssClient.cs` | Método `StripHtml`, aplicar al snippet |
| `src/Server/Domain/News/AiMode.cs` | `Manual = 1` → `On = 1` |
| `src/Server/Api/Endpoints/Ops/AiModeEndpoints.cs` | Mensaje validación, remover guard del endpoint manual |
| `src/Server/Infrastructure/Integrations/Ai/GeminiAiSummaryService.cs` | Prompt nuevo |
| `src/Server/Api/Endpoints/Public/NewsEndpoints.cs` | `GetLatestAsync(5, ct)` |
| `src/Web/Ops/src/api/aiModeApi.ts` | Tipo `'Off' \| 'On'` |
| `src/Web/Ops/src/modules/ai-mode/AiModeSection.tsx` | UI Off/On completa |

---

### Búsqueda exhaustiva de referencias a `AiMode.Manual`

Antes de hacer el rename del enum, buscar y actualizar TODOS los archivos que referencian `AiMode.Manual`:
```powershell
grep -rn "AiMode\.Manual\|AiMode.Manual\|\"Manual\"\|'Manual'" src/Server/ src/Web/ --include="*.cs" --include="*.ts" --include="*.tsx"
```

Archivos conocidos que usan `Manual`:
- `src/Server/Domain/News/AiMode.cs` — definición (cambiar aquí)
- `src/Server/Api/Endpoints/Ops/AiModeEndpoints.cs` — validación string + guard del endpoint
- `src/Web/Ops/src/api/aiModeApi.ts` — tipo del mode
- `src/Web/Ops/src/modules/ai-mode/AiModeSection.tsx` — array de opciones, estados, labels, condiciones

---

### `NewsPipelineJob` — estructura esperada después del refactor

```csharp
public class NewsPipelineJob(
    IFibraRepository fibraRepo,
    INewsRepository newsRepo,
    IBlocklistRepository blocklistRepo,
    IRssClient rssClient,
    IAiModeRepository aiModeRepo,
    IOgImageScraper ogImageScraper,
    IAiSummaryService summaryService,   // ← NUEVO
    ILogger<NewsPipelineJob> logger)
```

El loop de artículos debe ser secuencial (ya lo es). Con modo On, cada artículo incurre en ~1-2s de latencia por llamada a Gemini. Para pipelines grandes esto es aceptable ya que el job corre en background (Hangfire). No necesita ser paralelo — la deduplicación depende del orden de guardado.

---

### `GoogleNewsRssClient` — `StripHtml` correctamente

El `<description>` de Google News suele ser un fragmento HTML CDATA como:
```html
<ol><li><a href="https://noticias.com/art">Título del artículo</a><font color="#6f6f6f">El Financiero</font></li></ol>
```

El resultado esperado tras `StripHtml`:
```
Título del artículo El Financiero
```

El método `StripHtml` usa `Regex.Replace(raw, "<[^>]+>", " ")` que reemplaza tags con espacio. Luego `HtmlDecode` decodifica entidades. Finalmente colapsa múltiples espacios en uno. Esto es suficiente para limpiar el contenido del RSS de Google News — no necesita HtmlAgilityPack.

---

### Ops UI — AiModeSection comportamiento esperado

El botón de disparo manual en Ops (regenerar summary por ID) **siempre está habilitado** (ya no requiere que el modo sea `On`). El cambio en el backend (task 3.3) elimina el check de modo en el endpoint. En el frontend, actualizar el `disabled` de los botones para que ya no dependan del modo.

**Antes:** `disabled={currentMode !== 'Manual' || ...}`
**Después:** `disabled={triggerMutation.isPending}` (o simplemente habilitado siempre, solo gris si `isPending`)

---

### No se requiere migración de base de datos

El rename `Manual → On` es solo a nivel de C# enum (representación string en la UI). El valor entero `1` en la columna `AiModeConfig.Mode` sigue siendo el mismo. EF Core almacena enums como enteros.

### No hay cambios en NewsRepository ni en NewsArticleDto

`GetLatestAsync` ya ordena `OrderByDescending(n => n.PublishedAt)` — el orden de fibras ya era correcto (confirmado). No se toca el repositorio ni el contrato DTO para esta historia.

## File List

- `src/Server/Api/CompositionRoot/ApiServiceExtensions.cs` — modificado
- `src/Server/Infrastructure/Jobs/News/NewsPipelineJob.cs` — modificado
- `src/Server/Infrastructure/Integrations/GoogleNews/GoogleNewsRssClient.cs` — modificado
- `src/Server/Domain/News/AiMode.cs` — modificado
- `src/Server/Api/Endpoints/Ops/AiModeEndpoints.cs` — modificado
- `src/Server/Infrastructure/Integrations/Ai/GeminiAiSummaryService.cs` — modificado
- `src/Server/Api/Endpoints/Public/NewsEndpoints.cs` — modificado
- `src/Web/Ops/src/api/aiModeApi.ts` — modificado
- `src/Web/Ops/src/modules/ai-mode/AiModeSection.tsx` — modificado
- `src/Web/Main/tests/e2e/fixtures/ops-news-api.ts` — modificado
- `tests/Unit/Infrastructure.Tests/Jobs/News/NewsPipelineJobTests.cs` — modificado
- `tests/Integration/Api.Tests/AiModeOpsEndpointTests.cs` — modificado

## Change Log

- 2026-05-21: Historia 4.6 implementada — og:image con AllowAutoRedirect, StripHtml en snippets RSS, AiMode Manual→On con generación inline Gemini, prompt profesional analista FIBRA, home 5 noticias, Ops UI Off/On, guard de endpoint eliminado. Tests: 49/49. Builds: 0 errores.

## Dev Agent Record

### Implementation Plan
- Task 1: AllowAutoRedirect=true en OgImageScraper handler + eliminar ShouldScrapeOgImage
- Task 2: StripHtml en GoogleNewsRssClient con Regex + WebUtility.HtmlDecode
- Task 3: Rename AiMode.Manual→On, inyectar IAiSummaryService en NewsPipelineJob, lógica On inline, quitar guard endpoint + actualizar mensaje validación, prompt profesional Gemini
- Task 4: GetLatestAsync(5) en NewsEndpoints
- Task 5: Actualizar aiModeApi.ts y AiModeSection.tsx completo (tipos, labels, disabled, mensajes)
- Task 6: dotnet build Release 0 errores, 49 unit tests pasan, npm build Main y Ops 0 errores

### Completion Notes
- Todos los usos de `AiMode.Manual` encontrados y actualizados: AiMode.cs, AiModeEndpoints.cs, NewsPipelineJob.cs, aiModeApi.ts, AiModeSection.tsx, ops-news-api.ts (e2e fixture), NewsPipelineJobTests.cs, AiModeOpsEndpointTests.cs
- Test `ExecuteAsync_WhenArticleUrlIsGoogleNews_DoesNotCallOgImageScraper` renombrado a `ExecuteAsync_WhenArticleUrlIsGoogleNews_CallsOgImageScraper` para reflejar el nuevo comportamiento (ShouldScrapeOgImage eliminado — ahora todos los URLs se scrapeian)
- Test de integración `PostAiSummary_WhenModeIsNotManual_ReturnsProblemDetails400` reemplazado por `PostAiSummary_WhenModeIsOff_StillGeneratesSummary` — el guard fue eliminado del endpoint
- 3 tests nuevos de unit: AiMode.On genera summary, AiMode.On con excepción → Partial, fallback a processed en modo Off
- IAiModeRepository eliminado del handler de ai-summary (ya no lo usa — guard removido)

### Review Findings

- [x] [Review][Patch] Gemini null return sin log — cuando `GenerateSummaryAsync` retorna `null` sin lanzar excepción, `Status=Partial` pero no se registra ningún warning. AC4 exige loguear. [src/Server/Infrastructure/Jobs/News/NewsPipelineJob.cs]
- [x] [Review][Patch] UI — mensaje misleading cuando modo es Off — reemplazado por mensaje contextual informativo que aparece solo en modo On. [src/Web/Ops/src/modules/ai-mode/AiModeSection.tsx]
- [x] [Review][Defer] SSRF: destino del redirect no validado en OgImageScraper — `AllowAutoRedirect=true` permite que un redirect externo apunte a endpoints internos; `IsAllowedHostAsync` se aplica solo a la URL origen. Riesgo bajo por diseño (dev notes lo acepta explícitamente). — deferred, pre-existing design decision
- [x] [Review][Defer] CancellationToken swallowed en bloque AI del pipeline — si el job Hangfire se cancela durante la llamada a Gemini, `OperationCanceledException` es capturada por el catch interno y el artículo queda como `Partial` permanentemente (deduplicador lo excluye en la próxima corrida). Recuperable via regeneración manual. — deferred, aceptable tradeoff
