---
title: 'Fundamentales ops: bugs editar/pdf + cancelar + deshabilitar'
type: 'feature'
created: '2026-05-31'
status: 'in-progress'
baseline_commit: '7fbcd63'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** En ops/fundamentales hay dos bugs críticos (editar crea un nuevo registro en lugar de actualizar, y "Ver PDF" siempre retorna 500), no hay botón para cancelar la edición, y el listado no tiene forma de deshabilitar registros ni los filtra para ocultar los ya deshabilitados.

**Approach:** Corregir el `onSubmit` del formulario para usar `confirmMutation` en modo edición. Resolver el 500 usando ruta absoluta en `Results.File`. Agregar botón "Cancelar" en la página. Agregar endpoint `DELETE /{id}` + botón de papelera en el listado + filtro `DeletedAt == null` en el repositorio.

## Boundaries & Constraints

**Always:**
- Soft-disable únicamente — `SoftDeleteAsync(id, actor)` ya existe en el repositorio, usarlo.
- El filtro `DeletedAt == null` va en `GetByFibraAsync` (repositorio), no en el endpoint.
- El fix de ver-archivo aplica al GET `/{id}/pdf`; el path absoluto se obtiene con `Path.GetFullPath`.
- Cancelar edición en `FundamentalsPage` solo reset el `editRecord` a null (no hace petición de red).

**Ask First:**
- Ninguna.

**Never:**
- Hard-delete de FundamentalRecord.
- Mostrar registros con `DeletedAt != null` en el listado de ops (ya no vienen del servidor).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Editar + guardar sin PDF | Formulario en modo edición, sin PDF, click Guardar | Llama `patchKpis` + `confirmFundamentals` sobre el ID existente; no crea nuevo registro | Error visible en el formulario |
| Ver PDF | Click "Ver PDF" en fila con PDF | Descarga el archivo PDF | 404 si no existe en disco |
| Deshabilitar registro | Click icono papelera + confirmar "Sí" | Registro desaparece del listado inmediatamente | Error inline si falla la API |
| Cancelar edición | Click "Cancelar edición" en header del formulario | Formulario vuelve al modo "Nuevo registro", sin petición de red | — |

</frozen-after-approval>

## Code Map

- `src/Web/Ops/src/modules/fundamentals/FundamentalsImportForm.tsx` — bug `onSubmit`: usar `confirmMutation` cuando `isConfirmMode && !pdfFile`
- `src/Server/Api/Endpoints/Ops/OpsFundamentalsEndpoints.cs` — bug ver-archivo: `Path.GetFullPath`; agregar `DELETE /{id}`
- `src/Server/Infrastructure/Persistence/Repositories/Fundamentals/FundamentalRepository.cs` — filtro `DeletedAt == null` en `GetByFibraAsync`
- `src/Web/Ops/src/pages/FundamentalsPage.tsx` — botón "Cancelar edición" + callback `onCancel` al formulario
- `src/Web/Ops/src/modules/fundamentals/FundamentalsHistory.tsx` — botón papelera por fila con confirmación inline; quitar workaround visual `isDeleted`
- `src/Web/Ops/src/api/fundamentalsApi.ts` — agregar `disableFundamental(id)`

## Tasks & Acceptance

**Execution:**
- [ ] `src/Web/Ops/src/modules/fundamentals/FundamentalsImportForm.tsx` — En `onSubmit`, dentro del bloque `if (!pdfFile)`, verificar `if (isConfirmMode)` → `confirmMutation.mutate(values)` en lugar de `manualMutation.mutate(values)` — corrige el bug que crea nuevo registro al editar
- [ ] `src/Server/Api/Endpoints/Ops/OpsFundamentalsEndpoints.cs` — En `GET /{id}/pdf`, reemplazar `fullPath` con `Path.GetFullPath(fullPath)` antes de `File.Exists()` y `Results.File()` — corrige el 500 por ruta relativa
- [ ] `src/Server/Infrastructure/Persistence/Repositories/Fundamentals/FundamentalRepository.cs` — En `GetByFibraAsync`, agregar `.Where(r => r.FibraId == fibraId && r.DeletedAt == null)` — filtra registros deshabilitados del listado
- [ ] `src/Server/Api/Endpoints/Ops/OpsFundamentalsEndpoints.cs` — Agregar endpoint `DELETE /api/v1/ops/fundamentals/{id:guid}` que lee el actor del JWT y llama `repo.SoftDeleteAsync(id, actor, ct)`; retorna 204 o 404 — habilita soft-disable desde frontend
- [ ] `src/Web/Ops/src/api/fundamentalsApi.ts` — Agregar `disableFundamental(id: string): Promise<void>` llamando `DELETE /api/v1/ops/fundamentals/{id}` con `fetch()` directo y auth headers — contrato UI
- [ ] `src/Web/Ops/src/pages/FundamentalsPage.tsx` — Agregar botón "Cancelar edición" (visible solo cuando `state.editRecord != null`) junto al título del formulario; al hacer click hace `setState(s => ({...s, editRecord: null}))` y llama `reset()` en el form — permite salir del modo edición sin guardar
- [ ] `src/Web/Ops/src/modules/fundamentals/FundamentalsImportForm.tsx` — Agregar prop `onCancel?: () => void` a la interfaz; mostrar botón "Cancelar" en el footer del formulario solo cuando `isConfirmMode && onCancel`; al hacer click llama `resetAiState()` y `onCancel()` — botón cancelar en el formulario mismo
- [ ] `src/Web/Ops/src/modules/fundamentals/FundamentalsHistory.tsx` — Agregar estado `pendingDisableId` y `disableMutation`; en cada fila agregar botón papelera (SVG, compacto, igual al de noticias) solo cuando `!isDeleted`; al confirmar llama `disableFundamental(r.id)` e invalida `['fundamentals', fibraId]`; remover workaround visual `isDeleted` (opacity, strikethrough, badge "archivado") ya que el servidor filtra — reduce código muerto y oculta los deshabilitados

**Acceptance Criteria:**
- Dado un registro en el listado, cuando se hace click en "Editar", se llena el formulario y se hace click en "Guardar" sin PDF, entonces el registro existente se actualiza (no se crea uno nuevo).
- Dado un registro con PDF, cuando se hace click en "Ver PDF", entonces el PDF se descarga correctamente (sin error 500).
- Dado que el formulario está en modo edición, cuando se hace click en "Cancelar edición", entonces el formulario vuelve al modo "Nuevo registro" sin hacer ninguna petición de red.
- Dado un registro en el listado, cuando se hace click en el icono papelera y se confirma, entonces el registro desaparece del listado inmediatamente.
- Dado que se deshabilita un registro, cuando se recarga el listado, entonces el registro no aparece.

## Verification

**Commands:**
- `dotnet build FIBRADIS.slnx` — expected: Build succeeded, 0 errores
- `npx tsc --noEmit --project src/Web/Ops/tsconfig.json` — expected: sin errores
