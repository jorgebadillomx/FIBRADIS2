---
title: 'Umbral mínimo de body_text para llamadas de IA en noticias'
type: 'feature'
created: '2026-05-31'
status: 'done'
baseline_commit: '688f991e06a3bf81535c5c275015cecf5d479686'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Con AI_MODE=On, el pipeline manda artículos a la IA aunque no tengan `body_text` suficiente (o incluso sin cuerpo), desperdiciando tokens de API. Los artículos se guardan correctamente aunque falle la IA, pero la llamada ocurre aunque el contenido sea insuficiente para generar un análisis útil.

**Approach:** Agregar un campo configurable `MinBodyTextLengthForAi` (int, default 500) en `AiModeConfig`. El pipeline lo lee en cada ejecución y salta la llamada a IA para artículos cuyo `bodyText` no alcance el umbral, guardándolos como `Partial`. El valor se edita desde la sección "Modo AI" en Ops.

## Boundaries & Constraints

**Always:**
- Si `currentMode == On` y `bodyText.Length < MinBodyTextLengthForAi` (o bodyText es null) → el artículo se guarda como `Partial`, sin llamar a la IA. El registro nunca se pierde.
- El umbral NO aplica al endpoint manual `/ai-summary` ni `/ai-analysis` (disparos manuales desde Ops ya tienen body_text disponible).
- Valor válido: 0–10 000. `0` desactiva el gate de longitud (cualquier body_text no nulo pasa).
- El campo se persiste en `ai.AiModeConfig` junto con `Mode` y `NewsModel`.

**Ask First:** ninguno — la lógica es determinista.

**Never:**
- No cambiar el comportamiento de los endpoints manuales de análisis IA.
- No agregar un nuevo endpoint para este campo; se actualiza a través del PUT existente en `/api/v1/ops/ai-mode`.
- No exponer este campo en Main SPA.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Artículo con body_text suficiente | `Mode=On`, `bodyText.Length >= MinBodyTextLengthForAi` | Llama a IA, guarda `Processed` | Si IA falla → `Partial` (comportamiento existente) |
| Artículo con body_text insuficiente | `Mode=On`, `bodyText.Length < MinBodyTextLengthForAi` | Salta IA, guarda `Partial` | — |
| Artículo sin body_text | `Mode=On`, `bodyText == null` | Salta IA, guarda `Partial` | — |
| Mode=Off | Cualquier body_text | Comportamiento sin cambio (sin IA) | — |
| Umbral = 0 | `Mode=On`, `bodyText` no nulo | Llama a IA (gate desactivado) | — |
| PUT inválido | `minBodyTextLengthForAi = -1` o `> 10000` | 400 ValidationProblem | — |

</frozen-after-approval>

## Code Map

- `src/Server/Domain/News/AiModeConfig.cs` — entidad: agregar `MinBodyTextLengthForAi`
- `src/Server/Infrastructure/Persistence/SqlServer/Configurations/News/AiModeConfigConfiguration.cs` — EF config: nueva columna + seed
- `src/Server/Infrastructure/Persistence/Repositories/News/AiModeRepository.cs` — actualizar `UpdateConfigAsync`
- `src/Server/Application/News/IAiModeRepository.cs` — firma `UpdateConfigAsync` + nuevo método de lectura del umbral
- `src/Server/Infrastructure/Jobs/News/NewsPipelineJob.cs` — lógica de gate en el loop de artículos
- `src/Server/SharedApiContracts/News/AiModeDto.cs` — agregar `MinBodyTextLengthForAi`
- `src/Server/Api/Endpoints/Ops/AiModeEndpoints.cs` — GET devuelve umbral, PUT acepta y valida `minBodyTextLengthForAi`
- `src/Web/Ops/src/api/aiModeApi.ts` — agregar campo en `setAiConfig` y tipo `AiModeDto`
- `src/Web/Ops/src/modules/ai-mode/AiModeSection.tsx` — input numérico para editar el umbral

## Tasks & Acceptance

**Execution:**
- [ ] `src/Server/Domain/News/AiModeConfig.cs` — agregar `public int MinBodyTextLengthForAi { get; set; } = 500;`
- [ ] `src/Server/Infrastructure/Persistence/SqlServer/Configurations/News/AiModeConfigConfiguration.cs` — mapear columna `min_body_text_length_for_ai` (int, not null, default 500); actualizar seed con valor 500
- [ ] `src/Server/Infrastructure/Persistence/Migrations/` — generar migración `AddMinBodyTextLengthForAi` con `dotnet ef migrations add AddMinBodyTextLengthForAi --project src/Server/Infrastructure --startup-project src/Server/Api`
- [ ] `src/Server/Application/News/IAiModeRepository.cs` — agregar `int? minBodyTextLengthForAi` a `UpdateConfigAsync`
- [ ] `src/Server/Infrastructure/Persistence/Repositories/News/AiModeRepository.cs` — implementar actualización de `MinBodyTextLengthForAi` en `UpdateConfigAsync` (mismo patrón que `newsModel`)
- [ ] `src/Server/Infrastructure/Jobs/News/NewsPipelineJob.cs` — leer `config.MinBodyTextLengthForAi`; antes de llamar a la IA, evaluar `!string.IsNullOrWhiteSpace(bodyText) && bodyText.Length >= minBodyLength`; si no se cumple, setear `finalStatus = NewsArticleStatus.Partial` y loggear `Information` con longitud actual vs umbral
- [ ] `src/Server/SharedApiContracts/News/AiModeDto.cs` — agregar `int MinBodyTextLengthForAi` al record
- [ ] `src/Server/Api/Endpoints/Ops/AiModeEndpoints.cs` — GET: incluir `MinBodyTextLengthForAi` en `AiModeDto`; PUT: agregar `int? MinBodyTextLengthForAi` a `UpdateAiModeRequest`, validar rango 0–10 000, pasar a `UpdateConfigAsync`
- [ ] `src/Web/Ops/src/api/aiModeApi.ts` — agregar `minBodyTextLengthForAi?: number` en `setAiConfig` y en el tipo local del PUT body
- [ ] `src/Web/Ops/src/modules/ai-mode/AiModeSection.tsx` — mostrar valor actual del umbral; agregar input numérico (tipo `number`, min 0, max 10000, paso 50) que permita editarlo y guardarlo junto con la configuración existente; invalidar `['ai-mode']` al guardar
- [ ] `tests/Unit/Infrastructure.Tests/Jobs/NewsPipelineJobThresholdTests.cs` — al menos 3 unit tests: (1) body_text null salta IA y status=Partial, (2) body_text.Length < umbral salta IA y status=Partial, (3) body_text.Length >= umbral llama a IA

**Acceptance Criteria:**
- Given `AI_MODE=On` y `MinBodyTextLengthForAi=500`, when el pipeline procesa un artículo con `bodyText.Length=300`, then el artículo se guarda como `Partial` sin llamar a `IAiNewsAnalysisService`
- Given `AI_MODE=On` y `MinBodyTextLengthForAi=500`, when el pipeline procesa un artículo con `bodyText.Length=800`, then `IAiNewsAnalysisService.GenerateAnalysisAsync` se invoca
- Given `AI_MODE=Off`, when el pipeline corre con cualquier umbral, then ningún artículo llama a la IA (comportamiento existente sin cambio)
- Given un AdminOps en Ops, when actualiza el umbral a 200 y guarda, then el GET de `/api/v1/ops/ai-mode` devuelve `minBodyTextLengthForAi: 200`
- Given `PUT /api/v1/ops/ai-mode` con `minBodyTextLengthForAi: -5`, then responde 400 con error de validación en el campo `minBodyTextLengthForAi`

## Spec Change Log

## Design Notes

El gate se evalúa **después** de normalizar `bodyText` (ya hay `NormalizeBodyText` en el pipeline), sobre la longitud del texto ya limpio. Esto evita que ruido HTML inflado supere el umbral.

El campo vive en `AiModeConfig` (no en `AiProviderConfig`) porque es semántica del pipeline de noticias, no del proveedor de IA.

## Verification

**Commands:**
- `dotnet build FIBRADIS.slnx` -- expected: 0 errores, 0 warnings nuevos
- `dotnet test tests/Unit/Infrastructure.Tests --filter "NewsPipelineJobThreshold"` -- expected: 3/3 passed
- `dotnet ef database update --project src/Server/Infrastructure --startup-project src/Server/Api` -- expected: migración aplicada sin error

## Suggested Review Order

**Gate logic — entrada principal**

- Condición `hasEnoughBody` + ramas `if/else if` que sustituyen el `if (Mode == On)` anterior
  [`NewsPipelineJob.cs:156`](../../src/Server/Infrastructure/Jobs/News/NewsPipelineJob.cs#L156)

- Fallback `minBodyLength = 500` cuando falla lectura de config; consistente con entity default
  [`NewsPipelineJob.cs:46`](../../src/Server/Infrastructure/Jobs/News/NewsPipelineJob.cs#L46)

**Dominio y migración**

- Nueva propiedad con default 500 en entidad
  [`AiModeConfig.cs:11`](../../src/Server/Domain/News/AiModeConfig.cs#L11)

- Mapeo EF: columna `min_body_text_length_for_ai`, `HasDefaultValue(500)`, seed actualizado
  [`AiModeConfigConfiguration.cs:29`](../../src/Server/Infrastructure/Persistence/SqlServer/Configurations/News/AiModeConfigConfiguration.cs#L29)

- Migración: `AddColumn<int>` con `defaultValue: 500` + `UpdateData` para fila seed
  [`20260531194013_AddMinBodyTextLengthForAi.cs:13`](../../src/Server/Infrastructure/Persistence/Migrations/20260531194013_AddMinBodyTextLengthForAi.cs#L13)

**Repositorio**

- Firma actualizada: `int? minBodyTextLengthForAi` entre `newsModel` y `actor`
  [`IAiModeRepository.cs:10`](../../src/Server/Application/News/IAiModeRepository.cs#L10)

- Actualización condicional del campo (mismo patrón que `newsModel`)
  [`AiModeRepository.cs:68`](../../src/Server/Infrastructure/Persistence/Repositories/News/AiModeRepository.cs#L68)

**API contract**

- Validación rango 0–10 000 en PUT; empty-body guard ampliado al tercer campo
  [`AiModeEndpoints.cs:69`](../../src/Server/Api/Endpoints/Ops/AiModeEndpoints.cs#L69)

- DTO: nuevo campo posicional `int MinBodyTextLengthForAi` (último)
  [`AiModeDto.cs:8`](../../src/Server/SharedApiContracts/News/AiModeDto.cs#L8)

**Frontend**

- `saveThresholdMutation` + `pendingMinBodyLength` state; invalidación de `['ai-mode']`
  [`AiModeSection.tsx:23`](../../src/Web/Ops/src/modules/ai-mode/AiModeSection.tsx#L23)

- Input numérico (min=0, max=10000, step=50, aria-label); save button condicional
  [`AiModeSection.tsx:120`](../../src/Web/Ops/src/modules/ai-mode/AiModeSection.tsx#L120)

- `setAiConfig` actualizado con `minBodyTextLengthForAi?: number`
  [`aiModeApi.ts:31`](../../src/Web/Ops/src/api/aiModeApi.ts#L31)

**Tests**

- 3 nuevos tests del gate (null body, body corto, body suficiente) con `TrackingAiNewsAnalysisService`
  [`NewsPipelineJobThresholdTests.cs:1`](../../tests/Unit/Infrastructure.Tests/Jobs/News/NewsPipelineJobThresholdTests.cs#L1)

- 4 nuevos tests integración (GET campo, PUT persist AC4, PUT -5 → 400 AC5, PUT 10001 → 400)
  [`AiModeGetPutTests.cs:381`](../../tests/Integration/Api.Tests/AiModeGetPutTests.cs#L381)

- 2 tests existentes corregidos: `FakeArticleContentScraper(null)` → `new string('x', 600)`
  [`NewsPipelineJobTests.cs:69`](../../tests/Unit/Infrastructure.Tests/Jobs/News/NewsPipelineJobTests.cs#L69)
