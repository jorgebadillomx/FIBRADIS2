# Historia 4.7: Editor manual de body_text en Ops

Status: review

## Story

Como operador AdminOps de FIBRADIS,
quiero poder ver y editar manualmente el `body_text` de cualquier noticia desde el panel Ops,
para corregir o limpiar body_text con ruido sin necesidad de esperar un nuevo ciclo del pipeline ni de intervención técnica.

## Contexto

La historia `4-5-4` implementó extracción semántica para mejorar la calidad del `body_text` automático. Sin embargo, algunos sitios (SPAs, paywalls, páginas sin HTML semántico) siguen produciendo body_text contaminado o incompleto. El operador necesita una herramienta de corrección manual como último recurso.

El `body_text` es un insumo interno que alimenta los resúmenes de IA (`ai_summary`). No se muestra al usuario final. Mejorar su calidad mejora directamente la utilidad de los resúmenes automáticos en modo `AI On`.

## Acceptance Criteria

1. **Listado de artículos en Ops**
   - Dado que el operador abre la sección de noticias en Ops,
   - entonces ve una tabla paginada de los artículos más recientes con: título, fuente, fecha de publicación, longitud del `body_text` (o "Sin cuerpo" si es null), y un botón de edición.

2. **Edición del body_text**
   - Dado que el operador hace clic en "Editar cuerpo" en un artículo,
   - entonces ve el `body_text` actual en un área de texto editable.

3. **Guardado**
   - Dado que el operador modifica el texto y guarda,
   - entonces el sistema persiste el nuevo `body_text` y muestra confirmación.

4. **Limpieza (null)**
   - Dado que el operador deja el área de texto vacía y guarda,
   - entonces el sistema persiste `body_text = null` para ese artículo.

5. **Seguridad**
   - Dado que los endpoints de listado y edición de body_text son llamados,
   - entonces requieren token AdminOps válido (igual que el resto de endpoints Ops).

6. **Sin cambio en la API pública**
   - La edición manual de `body_text` no altera los endpoints públicos ni el contrato `NewsArticleDto`.

7. **Compatibilidad con el trigger de regeneración de summary**
   - Dado que el operador edita el `body_text` y luego dispara el resumen de IA desde Ops,
   - entonces el nuevo `body_text` se usa como insumo para el resumen.

## Tasks / Subtasks

- [x] Task 1: Backend — DTO y endpoint GET `/api/v1/ops/news`
  - [x] 1.1 Crear `OpsNewsArticleDto` en `SharedApiContracts.News`
  - [x] 1.2 Agregar `GetPagedForOpsAsync(int page, int pageSize, CancellationToken ct)` a `INewsRepository`
  - [x] 1.3 Implementar `GetPagedForOpsAsync` en `NewsRepository` (ORDER BY published_at DESC)
  - [x] 1.4 Crear endpoint `GET /api/v1/ops/news?page=&pageSize=` en `AiModeEndpoints.cs`

- [x] Task 2: Backend — endpoint PUT `/api/v1/ops/news/{id}/body-text`
  - [x] 2.1 Crear `UpdateBodyTextRequest` en `SharedApiContracts.News`
  - [x] 2.2 Agregar endpoint `PUT /api/v1/ops/news/{id}/body-text` en `AiModeEndpoints.cs`
  - [x] 2.3 Reutilizar `UpdateBodyTextAsync` existente en `INewsRepository`

- [x] Task 3: Regenerar cliente API tipado
  - [x] 3.1 Ejecutar `npm run codegen:api` para incluir los nuevos endpoints

- [x] Task 4: Frontend — API layer
  - [x] 4.1 Agregar `fetchOpsNewsList(page, pageSize)` en `src/Web/Ops/src/api/newsApi.ts`
  - [x] 4.2 Agregar `updateNewsBodyText(id, bodyText)` en `src/Web/Ops/src/api/newsApi.ts`

- [x] Task 5: Frontend — componente `NewsBodyTextSection`
  - [x] 5.1 Crear `src/Web/Ops/src/modules/news-body/NewsBodyTextSection.tsx`
  - [x] 5.2 Tabla con artículos recientes (título, fuente, fecha, chars body_text / "Sin cuerpo")
  - [x] 5.3 Modal o inline editor: al hacer clic en "Editar cuerpo" → muestra textarea con body_text actual
  - [x] 5.4 Botón "Guardar" llama al PUT; confirmación visual (estado guardado / error)
  - [x] 5.5 Botón "Limpiar" setea body_text a null

- [x] Task 6: Frontend — integrar en App.tsx
  - [x] 6.1 Importar e insertar `NewsBodyTextSection` después de `AiModeSection` y blocklist

- [x] Task 7: Pruebas
  - [x] 7.1 Test unitario o de integración para `GET /api/v1/ops/news`
  - [x] 7.2 Test de integración para `PUT /api/v1/ops/news/{id}/body-text` (guardar, limpiar)

## Dev Notes

### Estado actual relevante

**Backend:**
- `INewsRepository` ya tiene `UpdateBodyTextAsync(Guid id, string? bodyText, CancellationToken ct)` — reutilizable para el PUT.
- `AiModeEndpoints.cs` ya tiene el grupo `/api/v1/ops/news` con el endpoint de AI summary. Los nuevos endpoints van en el mismo grupo.
- `SharedApiContracts.News.NewsArticleDto` es el DTO público. El nuevo `OpsNewsArticleDto` es distinto: incluye `bodyTextLength` y `bodyTextPreview` en lugar de `bodyText` completo (evitar payloads grandes).

**Frontend:**
- El frontend Ops es una SPA en `src/Web/Ops`, puerto 5174.
- Usa `openapi-fetch` con cliente tipado generado desde `npm run codegen:api`.
- Las secciones de Ops siguen el patrón `<section className="rounded-2xl border ...">` visto en `App.tsx`.
- Las mutaciones usan `useMutation` de React Query; los queries usan `useQuery`.
- Los errores se muestran con `<p className="text-sm text-destructive">`.

### Decisiones de diseño

- **Preview en el listado**: el DTO expone `bodyTextPreview` (primeros 200 chars del body_text) para mostrar un snippet en la tabla sin cargar el texto completo.
- **Textarea para edición**: no se necesita rich text editor; el body_text es texto plano. Un `<textarea>` simple es suficiente.
- **Paginación**: 20 artículos por página por defecto; la tabla muestra botones Anterior/Siguiente.
- **No hay search/filter en MVP**: el operador navega por fecha descendente. Si el volumen crece, se puede agregar.

### Archivos impactados

**Backend:**
- `src/Server/SharedApiContracts/News/OpsNewsArticleDto.cs` (nuevo)
- `src/Server/SharedApiContracts/News/UpdateBodyTextRequest.cs` (nuevo)
- `src/Server/Application/News/INewsRepository.cs` (modificado)
- `src/Server/Infrastructure/Persistence/Repositories/News/NewsRepository.cs` (modificado)
- `src/Server/Api/Endpoints/Ops/AiModeEndpoints.cs` (modificado)

**Frontend:**
- `src/Web/Ops/src/api/newsApi.ts` (modificado)
- `src/Web/Ops/src/modules/news-body/NewsBodyTextSection.tsx` (nuevo)
- `src/Web/Ops/src/App.tsx` (modificado)

**Codegen:**
- `scripts/codegen/Api.json` (regenerado por codegen)
- `src/Web/Main/src/api/client/` (regenerado)

### Testing

```bash
# Backend unit
dotnet test tests/Unit/Infrastructure.Tests/Infrastructure.Tests.csproj --configuration Release

# Backend integración
dotnet test tests/Integration/Api.Tests/Api.Tests.csproj --filter "AiModeOpsEndpointTests" --configuration Release

# Frontend dev
npm run dev:ops
```

## Dev Agent Record

### Implementation Notes

- Agregado `GET /api/v1/ops/news/{articleId}` (no estaba en el plan original) para cargar el body_text completo al abrir el editor — necesario para cumplir AC 2 sin truncar a 200 chars.
- `UpdateBodyTextAsync` migrado de `ExecuteUpdateAsync` a tracked entity update para compatibilidad con EF Core InMemory en tests.
- `OpsNewsBodyDto` creado como DTO mínimo para el endpoint GET by ID.
- `NewsBodyTextSection` usa inline editor (fila expandible) en lugar de modal para evitar dependencias de librería adicional.
- Tests pasan en ejecución secuencial (95/95). El fallo de `OpsMarketEndpointTests` en paralelo es una issue preexistente de Hangfire compartido — no regresión de esta historia.

## File List

- src/Server/SharedApiContracts/News/OpsNewsArticleDto.cs (nuevo)
- src/Server/SharedApiContracts/News/OpsNewsBodyDto.cs (nuevo)
- src/Server/SharedApiContracts/News/UpdateBodyTextRequest.cs (nuevo)
- src/Server/Application/News/INewsRepository.cs (modificado)
- src/Server/Infrastructure/Persistence/Repositories/News/NewsRepository.cs (modificado)
- src/Server/Api/Endpoints/Ops/AiModeEndpoints.cs (modificado)
- src/Web/Ops/src/api/newsApi.ts (modificado)
- src/Web/Ops/src/modules/news-body/NewsBodyTextSection.tsx (nuevo)
- src/Web/Ops/src/App.tsx (modificado)
- src/Web/SharedApiClient/schema.d.ts (regenerado)
- scripts/codegen/Api.json (regenerado)
- tests/Integration/Api.Tests/ApiWebFactory.cs (modificado)
- tests/Integration/Api.Tests/AiModeGetPutTests.cs (modificado)
- tests/Integration/Api.Tests/AiModeOpsEndpointTests.cs (modificado)
- tests/Unit/Infrastructure.Tests/Jobs/News/NewsPipelineJobTests.cs (modificado)

## Change Log

- 2026-05-22: Implementación completa de historia 4.7 — Editor manual de body_text en Ops.
