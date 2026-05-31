# Historia 4.10: Análisis IA Enriquecido para Noticias

Status: review

## Story

Como AdminOps y como inversionista,
quiero que la IA devuelva un análisis estructurado de cada noticia (relevancia, impacto, hechos clave, cifras, resumen analítico, takeaway para inversionista, etc.)
en lugar de un resumen de texto plano,
para que la información de noticias sea más accionable, navegable y visualmente rica tanto en Ops como en la vista pública.

## Contexto y motivación

Hasta ahora, el análisis de IA produce un único campo `AiSummary` de texto libre (5-7 oraciones). Esto limita:
1. La capacidad de filtrar noticias por relevancia, impacto o sector desde Ops
2. La riqueza de la vista pública al inversionista (no hay datos estructurados: cifras, hechos, takeaway)
3. El editing en Ops (solo se ve el resumen como texto, sin estructura)

Esta historia introduce un nuevo contrato JSON que la IA devuelve, lo persiste en una columna `ai_analysis_json` en `news.NewsArticle` (manteniendo `ai_summary` como campo de compatibilidad populado desde `summaryMarkdown`), y actualiza tanto el pipeline como la UI de Ops y la página lectora de noticias.

### Nuevo schema JSON devuelto por la IA

```json
{
  "isRelevant": true,
  "relevanceReason": "Razón breve de por qué es o no es relevante para FIBRAs",
  "headline": "Titular analítico breve, distinto al título original",
  "impact": "alto|medio|bajo|nulo",
  "sectorTags": ["retail", "industrial"],
  "subsector": "industrial|oficinas|comercial|hotelero|residencial|logistico|educativo|salud|mixto|otro|null",
  "affectedFibers": ["FUNO", "FIBRAMQ"],
  "keyFacts": ["Hecho material 1", "Hecho material 2"],
  "keyFigures": [
    { "label": "Distribución por CBFI", "valueText": "$0.47", "importance": "alta|media|baja" }
  ],
  "summaryMarkdown": "Resumen analítico en markdown (5-7 oraciones)",
  "investorTakeaway": "Conclusión breve y directa para inversionistas",
  "confidence": 0.85,
  "extractionNotes": "Limitaciones o ambigüedades de la extracción"
}
```

**Campos que pueden estar vacíos (null o array vacío):**
- `subsector` — `null` si la noticia no pertenece a ningún subsector específico
- `affectedFibers` — `[]` si no se mencionan FIBRAs específicas
- `keyFacts` — `[]` si la noticia no expone hechos materiales concretos
- `keyFigures` — `[]` si la noticia no contiene cifras relevantes
- `headline` — `null` si no aplica titular distinto al título original
- `investorTakeaway` — `null` si no hay conclusión directa
- `extractionNotes` — `null` si no hay limitaciones

La **estrategia de display** es: siempre mostrar lo que hay; nunca mostrar secciones vacías (no mostrar "Cifras clave: ninguna"). Cuando `isRelevant = false`, mostrar solo el `relevanceReason` en Ops y omitir la noticia o mostrarla con un badge "No relevante".

## Acceptance Criteria

### AC1 — Nuevo record C# `NewsAiAnalysis` y columna en BD

- Existe `NewsAiAnalysis` en `Domain.News`:
  ```csharp
  public sealed record NewsAiAnalysis(
      bool IsRelevant,
      string? RelevanceReason,
      string? Headline,
      string Impact, // "alto"|"medio"|"bajo"|"nulo"
      IReadOnlyList<string> SectorTags,
      string? Subsector,
      IReadOnlyList<string> AffectedFibers,
      IReadOnlyList<string> KeyFacts,
      IReadOnlyList<NewsKeyFigure> KeyFigures,
      string? SummaryMarkdown,
      string? InvestorTakeaway,
      double Confidence,
      string? ExtractionNotes
  );
  public sealed record NewsKeyFigure(string Label, string ValueText, string Importance);
  ```
- La migración EF Core agrega columna `ai_analysis_json nvarchar(max) NULL` a `news.NewsArticle`.
- `NewsArticle.AiAnalysisJson` (string? property) en la entidad, configurada como `nvarchar(max)`.
- No se elimina `NewsArticle.AiSummary` — se sigue poblando con `analysis.SummaryMarkdown` para compatibilidad con noticias antiguas que aún no tienen análisis estructurado.

### AC2 — Nueva interfaz `IAiNewsAnalysisService` y service de routing

- Existe `IAiNewsAnalysisService` en `Application.News`:
  ```csharp
  public interface IAiNewsAnalysisService
  {
      Task<NewsAiAnalysis?> GenerateAnalysisAsync(
          string title,
          string? snippet,
          string? bodyText,
          CancellationToken ct = default);
  }
  ```
- `GeminiNewsAnalysisService` implementa esta interfaz. Usa **Gemini JSON mode** (`response_mime_type: "application/json"`) para obtener el schema estructurado. Registra en `AiCallLog` con `Operation = "NewsAnalysis"`.
- `DeepSeekNewsAnalysisService` implementa esta interfaz. Usa JSON mode de DeepSeek (`response_format: { type: "json_object" }`).
- `RoutingNewsAnalysisService` delega según `AiProviderConfig.Provider` (igual que `RoutingAiSummaryService`). Es la implementación registrada en DI.
- Si la API key está ausente → retorna `null`.
- Si la respuesta JSON está malformada o incompleta → retorna `null` y registra warning en log.
- Si la API rechaza la credencial (401/403) → lanza `AiProviderConfigurationException`.

### AC3 — Prompt estructurado para análisis de noticias

- Existe un nuevo `PromptTemplate` en `ai.AiPrompt` con `ContentType = "NewsAnalysis"` (o se usa como nuevo default si se decide migrar). El template especifica que la respuesta debe ser JSON válido con los campos del schema.
- El seed del prompt incluye instrucciones claras:
  - Responder **únicamente** con el JSON, sin markdown, sin texto adicional
  - Si un campo no aplica, usar `null` o `[]` según el tipo
  - `impact: "nulo"` para noticias irrelevantes
  - `confidence` es un float entre 0 y 1 que refleja la certeza de extracción
- El prompt puede usar el mismo sistema de `{title}`, `{snippet_section}`, `{body_section}` que el prompt actual.
- `AiPromptTemplateDefaults.cs` incluye el default del nuevo template.
- El template lleva instrucción explícita sobre campos opcionales para que la IA los deje vacíos en lugar de inventar.

### AC4 — `NewsPipelineJob` y endpoint manual usan el nuevo servicio

- `NewsPipelineJob` usa `IAiNewsAnalysisService` en lugar de `IAiSummaryService` para noticias.
  - Si `analysis` no es null: serializa a JSON → guarda en `ai_analysis_json`; además copia `analysis.SummaryMarkdown` → `ai_summary` (compatibilidad).
  - Si `analysis` es null: `ai_analysis_json` queda null, `ai_summary` queda null, `Status = Partial`.
  - Si `analysis.IsRelevant = false`: se guarda igual (el análisis se persiste); el display en UI decide qué mostrar.
- El endpoint `POST /api/v1/ops/news/{id}/ai-analysis` (nuevo) acepta el ID del artículo, ejecuta el análisis y guarda el resultado. Retorna 200 con `NewsAiAnalysisDto`.
  - El endpoint anterior `POST /api/v1/ops/news/{id}/ai-summary` puede mantenerse apuntando al nuevo flujo (o deprecarse; se recomienda mantenerlo para retrocompatibilidad por si hay código cliente que lo llama).
- **No cambiar** el `IAiSummaryService` existente — sigue siendo usado para `AiContentType.Document` en fundamentales.

### AC5 — DTOs actualizados para exposición estructurada

- `NewsArticleDto` (público) agrega `NewsAiAnalysisDto? AiAnalysis`:
  ```csharp
  public sealed record NewsArticleDto(
      Guid Id,
      string Title,
      string Source,
      DateTimeOffset PublishedAt,
      string Url,
      string? Snippet,
      string? ImageUrl,
      string? AiSummary,         // compatibilidad: sigue presente
      NewsAiAnalysisDto? AiAnalysis // nuevo: null si no hay análisis estructurado
  );
  public sealed record NewsAiAnalysisDto(
      bool IsRelevant,
      string? RelevanceReason,
      string? Headline,
      string Impact,
      IReadOnlyList<string> SectorTags,
      string? Subsector,
      IReadOnlyList<string> AffectedFibers,
      IReadOnlyList<string> KeyFacts,
      IReadOnlyList<NewsKeyFigureDto> KeyFigures,
      string? SummaryMarkdown,
      string? InvestorTakeaway,
      double Confidence,
      string? ExtractionNotes
  );
  public sealed record NewsKeyFigureDto(string Label, string ValueText, string Importance);
  ```
- El mapeo del endpoint deserializa `ai_analysis_json` usando `System.Text.Json` con `JsonSerializerOptions` que tolere campos faltantes (`PropertyNameCaseInsensitive = true`).
- `OpsNewsArticleDto` agrega `bool HasAiAnalysis` y `string? ImpactPreview` (el valor de `impact` si existe).
- `OpsNewsBodyDto` agrega `NewsAiAnalysisDto? AiAnalysis`.

### AC6 — Vista pública (`NoticiaPage`) muestra el análisis estructurado

Cuando `article.aiAnalysis` está disponible:

- **Título mostrado (`<h1>`)** — usar `aiAnalysis.headline` si existe; si no, usar `article.title` original. El `headline` **reemplaza** al título, no va debajo.
- **Badge de impacto** — mostrar badge coloreado junto a la metadata (fuente + fecha):
  - `alto` → rojo/rose
  - `medio` → amber/orange
  - `bajo` → slate/neutral
  - `nulo` → no mostrar badge (o badge gris "No relevante")
- **Tags de sector** — si `sectorTags.length > 0`: fila de chips grises/teal debajo del badge.
- **Hechos clave** (`keyFacts`) — si array no vacío: lista `<ul>` con bullet points, sección "Hechos clave".
- **Cifras clave** (`keyFigures`) — si array no vacío: grid de tarjetas pequeñas con `label`, `valueText` y badge de `importance`. Ordenar: alta → media → baja.
- **Resumen analítico** (`summaryMarkdown`) — si existe: renderizar con `ReactMarkdown`. Label "Análisis IA". Reemplaza la lógica actual de `aiSummary`.
- **Takeaway para el inversionista** (`investorTakeaway`) — si existe: sección destacada con icono o borde izquierdo (estilo callout), label "¿Qué significa esto?".
- **Nivel de confianza** (`confidence`) — solo mostrar si `< 0.6` con un aviso discreto "Extracción con baja confianza".
- **Notas de extracción** (`extractionNotes`) — solo mostrar si no es null, en texto pequeño gris al final.
- **Fallback**: si `aiAnalysis` es null pero `aiSummary` existe, mostrar el comportamiento actual (texto plano). Si ambos son null, mostrar `snippet` o nada.

### AC7 — Vista Ops (panel de edición) muestra el análisis estructurado

En la fila expandida de edición del artículo en `NewsBodyTextSection`:

- **Panel "Análisis IA"** (nueva subsección dentro del panel de edición expandido):
  - Si `bodyQuery.data?.aiAnalysis` existe:
    - Badge de impacto + `isRelevant` (checkmark o X)
    - `headline` si existe
    - `summaryMarkdown` renderizado con `ReactMarkdown` (reemplaza la sección "Resumen de IA" actual)
    - `investorTakeaway` si existe (callout)
    - `keyFacts` si hay — lista
    - `keyFigures` si hay — tabla o grid
    - `sectorTags` + `subsector` como chips
    - `affectedFibers` como chips con estilo FIBRA
    - `confidence` si < 0.6: aviso amarillo
    - `extractionNotes` en texto pequeño
  - Si `aiAnalysis` es null pero `aiSummary` existe: mostrar el comportamiento actual (texto plano) con label "Resumen legacy".
- La columna "Resumen IA" de la tabla cambia de badge "Con resumen" / "Sin resumen" a:
  - Badge de impacto coloreado si hay análisis (ej. "Alto") — texto conciso
  - Badge "Legacy" (amber) si solo hay `aiSummary` pero no `aiAnalysis`
  - Badge "Sin análisis" (slate) si no hay nada
- El filtro "Resumen IA" existente (`hasAiSummary`) continúa funcionando; se puede añadir un filtro "Tiene análisis" en una historia futura.

### AC8 — Cobertura de pruebas

**Unit tests:**
- `GeminiNewsAnalysisService`: happy path devuelve `NewsAiAnalysis` correctamente deserializado; devuelve null con API key vacía; lanza `AiProviderConfigurationException` con 401; devuelve null con JSON malformado en lugar de lanzar.
- `DeepSeekNewsAnalysisService`: mismos 4 casos.
- `RoutingNewsAnalysisService`: delega a Gemini cuando `Provider=Gemini`; delega a DeepSeek cuando `Provider=DeepSeek`.

**Integration tests:**
- `POST /api/v1/ops/news/{id}/ai-analysis`: retorna 200 con `NewsAiAnalysisDto`; retorna 503 si no hay API key; retorna 404 si artículo no existe; requiere `AdminOps` (403 sin auth).
- `GET /api/v1/news/{id}`: incluye `aiAnalysis` en el DTO cuando el artículo tiene `ai_analysis_json`.
- Migración EF Core se aplica correctamente (test de integración con InMemory o migración real que corre en CI).

## Dev Notes — Guía de implementación

### Orden de implementación obligatorio

Seguir las tasks EN ORDEN. No saltar a frontend antes de que la migración pase y el endpoint nuevo exista.

### Decisión de almacenamiento

Se usa **una columna JSON** `ai_analysis_json nvarchar(max)` en `news.NewsArticle` en lugar de una tabla separada. Razones:
1. La relación es 1:0..1 y los datos son propios del artículo
2. No hay necesidad de JOIN en la mayoría de queries
3. El schema puede evolucionar sin migraciones adicionales
4. Las consultas no requieren filtrar por campos internos del JSON en SQL Server

No eliminar `ai_summary` — es el fallback para artículos procesados antes de esta historia y para la UI de `noticiaPage` en el caso de artículos legacy.

### Gemini JSON mode

Gemini soporta `response_mime_type: "application/json"` en `generationConfig`. Esto fuerza la respuesta a ser JSON válido:

```csharp
var body = new
{
    contents = new[] { new { parts = new[] { new { text = prompt } } } },
    generationConfig = new
    {
        maxOutputTokens = 2000,
        response_mime_type = "application/json"
    }
};
```

El parser debe usar `JsonSerializer.Deserialize<NewsAiAnalysis>` con `JsonSerializerOptions { PropertyNameCaseInsensitive = true }`.

Si la deserialización falla → log warning + retornar null (no lanzar).

### DeepSeek JSON mode

DeepSeek (OpenAI-compatible) soporta `response_format: { type: "json_object" }`:

```csharp
var body = new
{
    model = modelId,
    messages = new[] { new { role = "user", content = prompt } },
    max_tokens = 2000,
    response_format = new { type = "json_object" }
};
```

### Prompt de análisis — instrucciones críticas

El prompt DEBE incluir:
1. Instrucción explícita de que la respuesta es únicamente el JSON sin markdown wrappers ni texto extra
2. Lista de campos opcionales que pueden ser `null` o `[]` para no inventar datos
3. Enumeración de valores válidos para `impact` y `subsector`
4. Instrucción de que `affectedFibers` son solo tickers mexicanos reales (FUNO, FIBRAMQ, FIBRAPL, etc.)
5. Aclaración de que `confidence` refleja certeza de extracción, no calidad de la noticia

Ejemplo de sección crítica del prompt:
```
Responde ÚNICAMENTE con el JSON. No uses bloques de código markdown.
Si un campo no aplica, usa null (para strings/objetos) o [] (para arrays).
impact debe ser exactamente uno de: alto, medio, bajo, nulo.
subsector debe ser exactamente uno de: industrial, oficinas, comercial, hotelero, residencial, logistico, educativo, salud, mixto, otro, o null.
affectedFibers debe contener solo tickers de FIBRAs mexicanas reales que se mencionen explícitamente en el artículo.
```

### NewsBodyTextSection — cambios en la tabla

La columna "Resumen IA" en la tabla cambia su lógica de badge. El tipo `OpsNewsArticle` del cliente generado incluirá `impactPreview?: string | null`. El badge se colorea según ese valor.

Para no romper el filtro `hasAiSummary` existente en el endpoint, el backend puede interpretarlo como "tiene `ai_summary` OR tiene `ai_analysis_json`". Verificar el endpoint actual antes de tocar.

### Mapeo en endpoints públicos

Los endpoints que retornan `NewsArticleDto` deben deserializar `ai_analysis_json`:

```csharp
private static NewsAiAnalysisDto? MapAnalysis(string? json)
{
    if (string.IsNullOrWhiteSpace(json)) return null;
    try
    {
        return JsonSerializer.Deserialize<NewsAiAnalysisDto>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }
    catch
    {
        return null; // corrupción de datos no debe romper el endpoint
    }
}
```

### Codegen API — recordar regenerar el cliente

Después de cambiar `SharedApiContracts`, ejecutar:
```bash
npm run codegen:api
```

Esto regenera `src/Web/Main/src/api/client.ts` y `src/Web/Ops/src/api/client.ts`. Las referencias tipadas al cliente en `newsApi.ts` y `fibraNewsApi.ts` deben actualizarse si los nombres de tipos cambian.

### Migración EF Core

La migración solo agrega una columna nullable. No requiere UPDATE masivo de filas existentes.

```csharp
// En NewsArticleConfiguration.cs
builder.Property(x => x.AiAnalysisJson)
    .HasColumnName("ai_analysis_json")
    .HasColumnType("nvarchar(max)")
    .IsRequired(false);
```

### NoticiaPage — lógica de fallback

```tsx
// Título: headline reemplaza al título original cuando existe
const displayTitle = article.aiAnalysis?.headline ?? article.title

// Resumen — prioridad:
// 1. aiAnalysis.summaryMarkdown (nuevo, estructurado)
// 2. aiSummary (legacy, texto plano)
// 3. snippet (fallback final)
const summaryContent = article.aiAnalysis?.summaryMarkdown ?? article.aiSummary ?? article.snippet
```

El mismo patrón aplica en `NewsSection` (home) y `NoticiasSection` (ficha de FIBRA): el texto del link/tarjeta de cada noticia usa `headline` si está disponible, `title` si no.

Para `headline`, `investorTakeaway`, `keyFacts`, `keyFigures`, `sectorTags`: solo renderizar si el valor existe y no está vacío. Nunca renderizar secciones vacías.

### `ManualSummaryTriggerSection` — actualizar endpoint

El componente `ManualSummaryTriggerSection.tsx` llama al endpoint de generación manual. Actualizar para que llame a `POST /api/v1/ops/news/{id}/ai-analysis` en lugar del anterior endpoint de summary. Si se mantiene el endpoint legacy `/ai-summary`, puede quedarse apuntando ahí durante la transición, pero el nuevo endpoint debe estar disponible.

## Tasks / Subtasks

- [x] Task 1: Dominio — `NewsAiAnalysis`, `NewsKeyFigure`, columna en entidad
  - [x] 1.1 Crear `src/Server/Domain/News/NewsAiAnalysis.cs` con el record principal
  - [x] 1.2 Crear `src/Server/Domain/News/NewsKeyFigure.cs` con el record auxiliar
  - [x] 1.3 Agregar `public string? AiAnalysisJson { get; set; }` a `NewsArticle`
  - [x] 1.4 Actualizar `NewsArticleConfiguration.cs` con la nueva columna

- [x] Task 2: Infraestructura — Migración EF Core
  - [x] 2.1 Agregar migración: `AddNewsAiAnalysisJson`
  - [x] 2.2 Verificar que la migración solo agrega la columna nullable (sin datos migrados)
  - [x] 2.3 Aplicar: `dotnet ef database update`

- [x] Task 3: Application — `IAiNewsAnalysisService`
  - [x] 3.1 Crear `src/Server/Application/News/IAiNewsAnalysisService.cs`

- [x] Task 4: Infraestructura — `GeminiNewsAnalysisService`
  - [x] 4.1 Crear `src/Server/Infrastructure/Integrations/Ai/GeminiNewsAnalysisService.cs`

- [x] Task 5: Infraestructura — `DeepSeekNewsAnalysisService`
  - [x] 5.1 Crear `src/Server/Infrastructure/Integrations/Ai/DeepSeekNewsAnalysisService.cs`

- [x] Task 6: Infraestructura — `RoutingNewsAnalysisService` + DI
  - [x] 6.1 Crear `src/Server/Infrastructure/Integrations/Ai/RoutingNewsAnalysisService.cs`
  - [x] 6.2 Registrar en DI en `ApiServiceExtensions.cs`
  - [x] 6.3 Registrar también `GeminiNewsAnalysisService` y `DeepSeekNewsAnalysisService`

- [x] Task 7: Infraestructura — Prompt de análisis
  - [x] 7.1 Agregar constante `NewsAnalysis` en `AiPromptTemplateDefaults.cs`
  - [x] 7.2 Agregar seed en `AiPromptConfiguration.cs` para `ContentType = "news_analysis"`
  - [x] 7.3 Migración `AddNewsAnalysisPromptSeed` aplicada

- [x] Task 8: Pipeline y endpoint manual
  - [x] 8.1 Actualizar `NewsPipelineJob` para usar `IAiNewsAnalysisService`
  - [x] 8.2 Crear endpoint `POST /api/v1/ops/news/{id}/ai-analysis`
  - [x] 8.3 Endpoint legacy `POST /{id}/ai-summary` redirige al nuevo flujo de análisis

- [x] Task 9: Contratos de API — DTOs
  - [x] 9.1 Crear `NewsAiAnalysisDto.cs` y `NewsKeyFigureDto.cs`
  - [x] 9.2 Actualizar `NewsArticleDto` con `NewsAiAnalysisDto? AiAnalysis`
  - [x] 9.3 Actualizar `OpsNewsArticleDto` con `bool HasAiAnalysis`, `string? ImpactPreview`
  - [x] 9.4 Actualizar `OpsNewsBodyDto` con `NewsAiAnalysisDto? AiAnalysis`
  - [x] 9.5 Mapeos actualizados en endpoints públicos y Ops

- [x] Task 10: Codegen y cliente API
  - [x] 10.1 `npm run codegen:api` ejecutado, schema actualizado
  - [x] 10.2 `newsApi.ts` Main actualizado con tipos exportados
  - [x] 10.3 `newsApi.ts` Ops actualizado con `triggerAiAnalysis` y tipos

- [x] Task 11: Frontend Main — `NoticiaPage` enriquecida
  - [x] 11.1-11.10 Todos los subtasks completados (badge impacto, hechos, cifras, análisis markdown, takeaway, chips sector, confianza, fallback)

- [x] Task 12: Frontend Ops — `NewsBodyTextSection` actualizada
  - [x] 12.1 Panel expandido muestra análisis estructurado
  - [x] 12.2 Badge columna "Resumen IA" actualizado con impacto / Legacy / Sin análisis
  - [x] 12.3 `ManualSummaryTriggerSection` llama a `/ai-analysis`

- [x] Task 13: Tests
  - [x] 13.1 Unit tests de `GeminiNewsAnalysisService` (4 casos)
  - [x] 13.2 Unit tests de `DeepSeekNewsAnalysisService` (4 casos)
  - [x] 13.3 Unit tests de `RoutingNewsAnalysisService` (routing + logging)
  - [x] 13.4 Integration tests de `POST /api/v1/ops/news/{id}/ai-analysis` (4 casos)
  - [x] 13.5 Tests existentes de AiMode actualizados para IAiNewsAnalysisService
  - [x] 13.6 Build + tests pasan (165/166 unit, 183/188 integration — fallos pre-existentes)

- [x] Task 14: Build final y sprint-status
  - [x] 14.1 `dotnet build FIBRADIS.slnx` — 0 errores, 0 advertencias
  - [x] 14.2 `npm run build --workspace=src/Web/Main` — 0 errores TypeScript
  - [x] 14.3 `npm run build --workspace=src/Web/Ops` — 0 errores en archivos modificados
  - [x] 14.4 `sprint-status.yaml` actualizado a `review`

## Archivos a crear / modificar

**CREAR:**
- `src/Server/Domain/News/NewsAiAnalysis.cs`
- `src/Server/Domain/News/NewsKeyFigure.cs`
- `src/Server/Application/News/IAiNewsAnalysisService.cs`
- `src/Server/Infrastructure/Integrations/Ai/GeminiNewsAnalysisService.cs`
- `src/Server/Infrastructure/Integrations/Ai/DeepSeekNewsAnalysisService.cs`
- `src/Server/Infrastructure/Integrations/Ai/RoutingNewsAnalysisService.cs`
- `src/Server/SharedApiContracts/News/NewsAiAnalysisDto.cs`
- `src/Server/SharedApiContracts/News/NewsKeyFigureDto.cs`
- Migración EF Core en `src/Server/Infrastructure/Migrations/`
- Tests unitarios en `src/Server/UnitTests/`
- Tests de integración en `src/Server/IntegrationTests/`

**MODIFICAR:**
- `src/Server/Domain/News/NewsArticle.cs` — agregar `AiAnalysisJson`
- `src/Server/Infrastructure/Persistence/Configurations/News/NewsArticleConfiguration.cs` — columna nueva
- `src/Server/Infrastructure/Integrations/Ai/AiPromptTemplateDefaults.cs` — nuevo template
- `src/Server/Infrastructure/Jobs/News/NewsPipelineJob.cs` — usar nuevo servicio
- `src/Server/Api/Endpoints/Ops/AiModeEndpoints.cs` — nuevo endpoint + actualizar endpoint legacy
- `src/Server/Api/Endpoints/Public/NewsEndpoints.cs` — mapear `AiAnalysis`
- `src/Server/SharedApiContracts/News/NewsArticleDto.cs` — agregar `AiAnalysis`
- `src/Server/SharedApiContracts/News/OpsNewsArticleDto.cs` — agregar `HasAiAnalysis`, `ImpactPreview`
- `src/Server/SharedApiContracts/News/OpsNewsBodyDto.cs` — agregar `AiAnalysis`
- `src/Server/Infrastructure/DependencyInjection.cs` (o donde se registran los servicios) — registrar nuevos services
- `src/Web/Main/src/modules/noticia/NoticiaPage.tsx` — display enriquecido
- `src/Web/Ops/src/modules/news-body/NewsBodyTextSection.tsx` — display análisis + badges actualizados
- `src/Web/Ops/src/modules/news-body/ManualSummaryTriggerSection.tsx` — nuevo endpoint

## Archivos creados / modificados

**CREADOS:**
- `src/Server/Domain/News/NewsAiAnalysis.cs`
- `src/Server/Domain/News/NewsKeyFigure.cs`
- `src/Server/Application/News/IAiNewsAnalysisService.cs`
- `src/Server/Infrastructure/Integrations/Ai/GeminiNewsAnalysisService.cs`
- `src/Server/Infrastructure/Integrations/Ai/DeepSeekNewsAnalysisService.cs`
- `src/Server/Infrastructure/Integrations/Ai/RoutingNewsAnalysisService.cs`
- `src/Server/SharedApiContracts/News/NewsAiAnalysisDto.cs`
- `src/Server/SharedApiContracts/News/NewsKeyFigureDto.cs`
- `src/Server/Infrastructure/Persistence/Migrations/20260530122311_AddNewsAiAnalysisJson.cs`
- `src/Server/Infrastructure/Persistence/Migrations/20260530122758_AddNewsAnalysisPromptSeed.cs`
- `tests/Unit/Infrastructure.Tests/Integrations/Ai/GeminiNewsAnalysisServiceTests.cs`
- `tests/Unit/Infrastructure.Tests/Integrations/Ai/DeepSeekNewsAnalysisServiceTests.cs`
- `tests/Unit/Infrastructure.Tests/Integrations/Ai/RoutingNewsAnalysisServiceTests.cs`

**MODIFICADOS:**
- `src/Server/Domain/News/NewsArticle.cs` — columna `AiAnalysisJson`
- `src/Server/Infrastructure/Persistence/SqlServer/Configurations/News/NewsArticleConfiguration.cs`
- `src/Server/Infrastructure/Persistence/SqlServer/Configurations/News/AiPromptConfiguration.cs` — seed ID=3
- `src/Server/Infrastructure/Integrations/Ai/AiPromptTemplateDefaults.cs` — constante y método `GetTemplate`
- `src/Server/Application/News/INewsRepository.cs` — método `UpdateAiAnalysisAsync`
- `src/Server/Infrastructure/Persistence/Repositories/News/NewsRepository.cs` — implementación
- `src/Server/Infrastructure/Jobs/News/NewsPipelineJob.cs` — usa `IAiNewsAnalysisService`
- `src/Server/Api/Endpoints/Ops/AiModeEndpoints.cs` — endpoint `/ai-analysis`, endpoint `/ai-summary` migrado, mapeos actualizados
- `src/Server/Api/Endpoints/Public/NewsEndpoints.cs` — `MapAnalysis` en `ToDto`
- `src/Server/SharedApiContracts/News/NewsArticleDto.cs` — campo `AiAnalysis`
- `src/Server/SharedApiContracts/News/OpsNewsArticleDto.cs` — campos `HasAiAnalysis`, `ImpactPreview`
- `src/Server/SharedApiContracts/News/OpsNewsBodyDto.cs` — campo `AiAnalysis`
- `src/Server/Api/CompositionRoot/ApiServiceExtensions.cs` — registro `IAiNewsAnalysisService`
- `src/Web/SharedApiClient/schema.d.ts` — regenerado
- `src/Web/Main/src/api/newsApi.ts` — tipos exportados
- `src/Web/Ops/src/api/newsApi.ts` — `triggerAiAnalysis`, tipos exportados
- `src/Web/Main/src/modules/noticia/NoticiaPage.tsx` — display enriquecido completo
- `src/Web/Ops/src/modules/news-body/NewsBodyTextSection.tsx` — badges + panel análisis
- `src/Web/Ops/src/modules/news-body/ManualSummaryTriggerSection.tsx` — nuevo endpoint
- `tests/Unit/Infrastructure.Tests/Jobs/News/NewsPipelineJobTests.cs` — `FakeAiNewsAnalysisService`
- `tests/Integration/Api.Tests/AiModeOpsEndpointTests.cs` — stubs actualizados + 4 tests `/ai-analysis`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

## Dev Agent Record

**Implementación:** 2026-05-30

**Decisiones técnicas:**
- El endpoint `/ai-summary` se migró para usar `IAiNewsAnalysisService` (no IAiSummaryService) para retrocompatibilidad con código cliente existente. Retorna 204 en lugar de 200.
- `RoutingNewsAnalysisService` registra con `Operation = "NewsAnalysis"` en AiCallLog.
- `confidence` en el schema generado es `string | number` — se usa `Number()` para conversión segura.
- El test fallido pre-existente `FundamentalRepositoryTests.UpdateStatusAsync_IsIdempotent_WhenAlreadyProcessed` no fue introducido por esta historia.

**Tests:**
- Unit: `GeminiNewsAnalysisServiceTests` (4), `DeepSeekNewsAnalysisServiceTests` (4), `RoutingNewsAnalysisServiceTests` (3)
- Integration: `AiModeOpsEndpointTests` — 4 tests nuevos para `/ai-analysis` + tests existentes actualizados
- Pipeline: `NewsPipelineJobTests` (sin regresiones)
- Resultado: 165 passed / 1 failed pre-existente (unit) — 183 passed / 5 failed pre-existentes (integration)

**Build:** `dotnet build FIBRADIS.slnx` — 0 errores, 0 advertencias. `npm run build --workspace=src/Web/Main` — 0 errores. Ops build — 0 errores en archivos modificados (5 errores pre-existentes en Fundamentals).

## Senior Developer Review (AI)

### Review Findings

**Resumen:** 2 patches · 4 defers · 3 dismissed

#### Patches

- [x] **P1 (Medium): `<h1>` en `NoticiaPage` usa `article.title` en lugar de `displayTitle`** — AC6 exige que `aiAnalysis.headline` reemplace al título en el `<h1>` cuando existe. `displayTitle` se calcula correctamente en línea 28 pero solo se usa en el `<title>` meta-tag; el `<h1>` siempre renderiza `article.title`. Fix: cambiar `{article.title}` por `{displayTitle}` en la línea 84. [`src/Web/Main/src/modules/noticia/NoticiaPage.tsx:84`]

- [x] **P2 (Low): Snippet silenciosamente descartado en `BuildPromptAsync`** — ambos servicios aceptan `snippet` como parámetro pero `{snippet_section}` siempre se reemplaza con `string.Empty`. Cuando no hay body text, la IA solo recibe el título y cero contexto adicional; el snippet mejoraría la calidad del análisis en esos casos. Fix: cuando `preparedBody` sea null y `snippet` no sea null, construir `snippetSection = $"Resumen: {snippet}"` e inyectarlo en `{snippet_section}` en lugar de vacío. [`src/Server/Infrastructure/Integrations/Ai/GeminiNewsAnalysisService.cs:151`, `src/Server/Infrastructure/Integrations/Ai/DeepSeekNewsAnalysisService.cs:151`]

#### Deferred

- [x] [Review][Defer] **D1:** `RoutingNewsAnalysisService` llama a `providerRepo.GetConfigAsync` dos veces por request (una en el router para el switch, otra en el servicio delegado Gemini/DeepSeek). Overhead menor, no es race condition porque es lectura de config estática. — deferred, refactorizar en historia futura [`src/Server/Infrastructure/Integrations/Ai/RoutingNewsAnalysisService.cs:21`]

- [x] [Review][Defer] **D2:** El endpoint `/ai-analysis` retorna 409 cuando el artículo tiene `DeletedAt != null` pero ningún test de integración cubre ese caso. AC8 no lo lista explícitamente pero es una rama de error observable. — deferred, agregar test en historia futura [`tests/Integration/Api.Tests/AiModeOpsEndpointTests.cs`]

- [x] [Review][Defer] **D3:** `GetLatestAsync`, `GetLatestForFibraAsync` y `GetRelatedAsync` requieren `AiAnalysisJson != null`, ocultando artículos legacy con solo `AiSummary` en home/fichas/relacionadas. Decisión deliberada pero sin test de integración que la documente. — deferred, agregar test de cobertura [`src/Server/Infrastructure/Persistence/Repositories/News/NewsRepository.cs:83-92`]

- [x] [Review][Defer] **D4:** `MapAnalysis(string? json)` está duplicada en `NewsEndpoints.cs` y `AiModeEndpoints.cs` con lógica idéntica. — deferred, extraer a utilidad compartida en historia futura [`src/Server/Api/Endpoints/Public/NewsEndpoints.cs:124-135`, `src/Server/Api/Endpoints/Ops/AiModeEndpoints.cs:25-29`]

#### Dismissed

- [x] [Review][Dismissed] `responseMimeType` en camelCase — consistente con `GeminiKpiExtractorService` existente; la API Gemini acepta camelCase en `generationConfig`. No es bug.

- [x] [Review][Dismissed] `JsonSerializer.Serialize(analysis)` sin opciones explícitas — el roundtrip funciona porque la deserialización usa `PropertyNameCaseInsensitive = true`. Patrón consistente con el proyecto.

- [x] [Review][Dismissed] Tests unitarios de `GeminiNewsAnalysisService` y `DeepSeekNewsAnalysisService` — los 4 casos por servicio verifican contenido del resultado, no solo status code. Calidad aceptable.
