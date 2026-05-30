---
title: 'Ops Noticias — mejoras al listado (ID, bool resumen, update parcial, filtro fibra)'
type: 'feature'
created: '2026-05-30'
status: 'in-review'
baseline_commit: '26f27f28e407c0c77cc763b45d0d5abffbdbb1bb'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** El listado de noticias en Ops carece del campo ID visible, muestra un preview de texto en la columna "Resumen IA" cuando basta con un indicador booleano, no actualiza la fila tras generar un resumen manualmente (requiere recarga completa de la página), y no permite filtrar por fibra.

**Approach:** Cuatro mejoras coordinadas sobre el mismo componente: (1) columna ID con GUID truncado; (2) columna "Resumen IA" como badge booleano; (3) mutación del cache de React Query tras generar resumen —sin refetch completo—; (4) filtro por fibra con JOIN en BD y dropdown en UI usando el catálogo existente.

## Boundaries & Constraints

**Always:**
- La columna ID muestra los primeros 8 chars del GUID; el GUID completo accesible via `title` (hover).
- El update parcial post-resumen solo muta `hasAiSummary: true` en el cache local — sin fetch adicional.
- El filtro por fibra usa `GET /api/v1/ops/catalog` (ya disponible) para poblar el dropdown; no se añade endpoint nuevo.
- El filtro por `fibraId` en BD se implementa con `WHERE EXISTS` sobre `NewsArticleFibras` — no se añade ningún campo al `OpsNewsArticleDto`.
- El stub de `GetPagedForOpsAsync` en los tests de integración debe actualizarse para compilar.

**Ask First:**
- Si el usuario quiere mostrar el ticker de la fibra asociada en cada fila del listado (actualmente solo se filtra, no se muestra).

**Never:**
- Mostrar el preview de texto del resumen IA en la columna del listado.
- Hacer refetch completo del listado después de generar un resumen — solo mutación de cache.
- Paginar el dropdown de fibras (el catálogo tiene < 30 fibras).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Filtro fibra activo | `fibraId` = UUID de fibra existente | Lista muestra solo artículos vinculados a esa fibra | Si la fibra no tiene artículos, lista vacía |
| Filtro fibra + fibra sin artículos | `fibraId` válido pero sin noticias asociadas | Lista vacía, paginación 0 | — |
| Generar resumen exitoso | `POST /ai-summary` devuelve 204 | Badge "Con resumen" aparece en la fila sin refetch de lista | Error del mutation visible en sección trigger |
| Artículo no vinculado a ninguna fibra | Filtro fibra activo | No aparece en lista | — |

</frozen-after-approval>

## Code Map

- `src/Server/Application/News/INewsRepository.cs` — interfaz; agregar `Guid? fibraId = null` al final de `GetPagedForOpsAsync`
- `src/Server/Infrastructure/Persistence/Repositories/News/NewsRepository.cs` — implementación; WHERE EXISTS sobre `NewsArticleFibras` cuando `fibraId != null`
- `src/Server/Api/Endpoints/Ops/AiModeEndpoints.cs:93` — endpoint `GET /api/v1/ops/news`; aceptar `Guid? fibraId = null` y pasarlo al repo
- `src/Web/Ops/src/api/newsApi.ts` — `fetchOpsNewsList` agrega `fibraId?: string` al param y al query del apiClient
- `src/Web/Ops/src/modules/news-body/NewsBodyTextSection.tsx` — columna ID, badge booleano resumen, dropdown fibras, `fibraId` en queryKey
- `src/Web/Ops/src/modules/news-body/ManualSummaryTriggerSection.tsx` — mutación de cache parcial en `onSuccess`
- `tests/Integration/Api.Tests/AiModeOpsEndpointTests.cs:374` — stub `GetPagedForOpsAsync`; actualizar firma para compilar

## Tasks & Acceptance

**Execution:**
- [x] `src/Server/Application/News/INewsRepository.cs` — añadir `Guid? fibraId = null` al final de la firma de `GetPagedForOpsAsync` — parámetro opcional para no romper callers existentes
- [x] `src/Server/Infrastructure/Persistence/Repositories/News/NewsRepository.cs` — en `GetPagedForOpsAsync`: cuando `fibraId != null`, añadir `.Where(a => db.NewsArticleFibras.Any(f => f.NewsArticleId == a.Id && f.FibraId == fibraId))` antes del OrderBy
- [x] `src/Server/Api/Endpoints/Ops/AiModeEndpoints.cs` — aceptar `Guid? fibraId = null` en la lambda del GET y pasarlo como nuevo último argumento a `newsRepo.GetPagedForOpsAsync`
- [x] `tests/Integration/Api.Tests/AiModeOpsEndpointTests.cs` — actualizar firma del stub `GetPagedForOpsAsync` añadiendo `Guid? fibraId = null`; comportamiento sin cambios
- [x] `src/Web/Ops/src/api/newsApi.ts` — añadir `fibraId?: string` a `fetchOpsNewsList` y al objeto `query` del apiClient call
- [x] `src/Web/Ops/src/modules/news-body/NewsBodyTextSection.tsx` — cinco cambios en el mismo archivo: (a) estado `fibraId` y reset en el `useEffect` de filtros; (b) query de fibras via `useQuery + fetchOpsCatalog`; (c) dropdown "Fibra" en el bloque de filtros; (d) `fibraId` en `queryKey` y `fetchOpsNewsList`; (e) columna `ID` con `article.id.slice(0, 8)` + botón de copiar GUID completo al portapapeles; (f) celda "Resumen IA" reemplazada por badge booleano
- [x] `src/Web/Ops/src/modules/news-body/ManualSummaryTriggerSection.tsx` — añadir `useQueryClient`; en `onSuccess` llamar `queryClient.setQueriesData({ queryKey: ['ops-news-list'], exact: false }, updater)` para marcar `hasAiSummary: true` en el item con el ID generado

**Acceptance Criteria:**
- Given el listado cargado, when el usuario lo visualiza, then aparece columna "ID" con los primeros 8 chars del GUID y un botón de copiar que copia el GUID completo al portapapeles.
- Given un artículo sin resumen IA, when se visualiza, then la columna "Resumen IA" muestra badge "Sin resumen" en gris; si tiene resumen, badge "Con resumen" en teal.
- Given el dropdown "Fibra" seleccionado, when el usuario elige una fibra, then el listado filtra y muestra solo artículos vinculados a esa fibra.
- Given el listado visible con el artículo X sin resumen, when se genera el resumen de X con éxito, then el badge de X cambia a "Con resumen" sin recargar toda la lista.
- Given tests de integración, when se compila el proyecto, then cero errores de compilación.

## Design Notes

Columna ID — truncado + botón copiar (sin librería externa, `navigator.clipboard`):
```tsx
function IdCell({ id }: { id: string }) {
  const [copied, setCopied] = useState(false)
  return (
    <div className="flex items-center gap-1.5 font-mono text-xs text-slate-600">
      <span title={id}>{id.slice(0, 8)}</span>
      <button
        type="button"
        title="Copiar GUID completo"
        onClick={() => { navigator.clipboard.writeText(id); setCopied(true); setTimeout(() => setCopied(false), 1500) }}
        className="rounded p-0.5 text-slate-400 hover:text-teal-600 transition"
      >
        {copied ? '✓' : '⎘'}
      </button>
    </div>
  )
}
```

Badge booleano resumen (reemplaza la celda actual con `aiSummaryPreview`):
```tsx
{article.hasAiSummary ? (
  <span className="rounded-full bg-teal-100 px-2 py-0.5 text-xs text-teal-700">Con resumen</span>
) : (
  <span className="rounded-full bg-slate-100 px-2 py-0.5 text-xs text-slate-500">Sin resumen</span>
)}
```

Mutación parcial de cache en `ManualSummaryTriggerSection` (onSuccess):
```tsx
queryClient.setQueriesData(
  { queryKey: ['ops-news-list'], exact: false },
  (old: OpsNewsPage | undefined) => old
    ? { ...old, items: old.items.map(item =>
        item.id === trimmedArticleId ? { ...item, hasAiSummary: true } : item
      )}
    : old,
)
```

## Verification

**Commands:**
- `dotnet build FIBRADIS.slnx` -- expected: 0 errores, 0 warnings nuevos
- `dotnet test tests/Integration/Api.Tests/` -- expected: todos los tests pasan
- `npm run build --workspace=src/Web/Ops` -- expected: 0 errores TypeScript
