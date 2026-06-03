# Historia 6.4: Edición inline y eliminación de posiciones

Status: review

## Story

Como usuario,
quiero editar posiciones individuales del portafolio de forma inline haciendo doble clic en la celda de Qty o AvgCost, y eliminar posiciones con confirmación explícita,
para que pueda corregir errores de datos sin volver a subir el archivo completo.

## Acceptance Criteria

### AC1 — Edición inline de Qty

**Dado que** hago doble clic en la celda de Qty de la fila de FUNO11,
**Entonces** la celda entra en modo de edición mostrando el valor actual en un campo de entrada.

### AC2 — Guardar edición con recálculo

**Dado que** escribo 600 y presiono Enter,
**Entonces** la posición se guarda con 600 unidades, `costo_total_compra` se recalcula usando el `commission_factor` actual del servidor, y todos los KPIs afectados se actualizan al refrescar el query de portafolio.

### AC3 — Validación de Qty

**Dado que** escribo -50 en el campo de Qty,
**Entonces** la validación rechaza el valor con "La cantidad debe ser un entero positivo" y se restaura el valor original.

### AC4 — Cancelar con Escape

**Dado que** presiono Escape mientras edito,
**Entonces** la celda vuelve al modo de lectura con el valor original sin cambios.

### AC5 — Eliminar con confirmación

**Dado que** hago clic en el botón de eliminar para una posición,
**Entonces** aparece un diálogo de confirmación: "¿Eliminar posición FUNO11? Esta acción no se puede deshacer." Al confirmar, la posición se elimina y los KPIs se recalculan. Al cancelar, nada cambia.

## Tasks / Subtasks

### T1 — SharedApiContracts: DTO de request para edición

- [x] T1.1 — Agregar en `src/Server/SharedApiContracts/Portfolio/PortfolioResponseDto.cs` (al final del archivo):
  ```csharp
  public sealed record PortfolioPositionPatchDto(
      int Titulos,
      decimal CostoPromedio
  );
  ```

### T2 — Application: nuevos métodos en `IPortfolioRepository`

- [x] T2.1 — Agregar a `src/Server/Application/Portfolio/IPortfolioRepository.cs`:
  ```csharp
  Task<PortfolioPosition?> GetPositionAsync(Guid userId, Guid fibraId, CancellationToken ct = default);
  Task UpdatePositionAsync(PortfolioPosition position, CancellationToken ct = default);
  Task<bool> DeletePositionAsync(Guid userId, Guid fibraId, CancellationToken ct = default);
  ```

### T3 — Infrastructure: implementar en `PortfolioRepository`

- [x] T3.1 — Agregar a `src/Server/Infrastructure/Persistence/Repositories/Portfolio/PortfolioRepository.cs`:
  ```csharp
  public async Task<PortfolioPosition?> GetPositionAsync(Guid userId, Guid fibraId, CancellationToken ct)
      => await db.PortfolioPositions
          .FirstOrDefaultAsync(p => p.UserId == userId && p.FibraId == fibraId, ct);

  public async Task UpdatePositionAsync(PortfolioPosition position, CancellationToken ct)
  {
      db.PortfolioPositions.Update(position);
      await db.SaveChangesAsync(ct);
  }

  public async Task<bool> DeletePositionAsync(Guid userId, Guid fibraId, CancellationToken ct)
  {
      var deleted = await db.PortfolioPositions
          .Where(p => p.UserId == userId && p.FibraId == fibraId)
          .ExecuteDeleteAsync(ct);
      return deleted > 0;
  }
  ```

### T4 — API: endpoints PATCH y DELETE en `PortfolioEndpoints.cs`

- [x] T4.1 — En `src/Server/Api/Endpoints/Private/PortfolioEndpoints.cs`, dentro de `MapPortfolio`, ANTES del `return app;`, agregar los dos nuevos endpoints:

  ```csharp
  group.MapPatch("/positions/{fibraId:guid}", async (
      Guid fibraId,
      PortfolioPositionPatchDto request,
      IPortfolioRepository portfolioRepo,
      IOperationalConfigRepository configRepo,
      HttpContext ctx,
      CancellationToken ct) =>
  {
      if (request.Titulos <= 0)
          return Results.Problem("La cantidad debe ser un entero positivo.", statusCode: 400);
      if (request.CostoPromedio <= 0)
          return Results.Problem("El costo promedio debe ser mayor a cero.", statusCode: 400);

      var userId = GetUserId(ctx);
      var position = await portfolioRepo.GetPositionAsync(userId, fibraId, ct);
      if (position is null)
          return Results.NotFound();

      var config = await configRepo.GetAsync(ct);
      position.Titulos = request.Titulos;
      position.CostoPromedio = request.CostoPromedio;
      position.CostoTotalCompra = request.Titulos * request.CostoPromedio * (1 + config.CommissionFactor);

      await portfolioRepo.UpdatePositionAsync(position, ct);
      return Results.NoContent();
  })
  .Produces(StatusCodes.Status204NoContent)
  .ProducesProblem(StatusCodes.Status400BadRequest)
  .ProducesProblem(StatusCodes.Status404NotFound)
  .ProducesProblem(StatusCodes.Status401Unauthorized);

  group.MapDelete("/positions/{fibraId:guid}", async (
      Guid fibraId,
      IPortfolioRepository portfolioRepo,
      HttpContext ctx,
      CancellationToken ct) =>
  {
      var userId = GetUserId(ctx);
      var deleted = await portfolioRepo.DeletePositionAsync(userId, fibraId, ct);
      return deleted ? Results.NoContent() : Results.NotFound();
  })
  .Produces(StatusCodes.Status204NoContent)
  .ProducesProblem(StatusCodes.Status404NotFound)
  .ProducesProblem(StatusCodes.Status401Unauthorized);
  ```

- [x] T4.2 — Verificar que `IOperationalConfigRepository` ya está registrado en el DI (ya lo está, lo usa el endpoint de upload). No es necesario registrar nada nuevo.

### T5 — Codegen: regenerar cliente API

- [x] T5.1 — Ejecutar `npm run codegen:api`
  - Si el proceso de la API está corriendo y bloquea los DLLs, detenerlo antes.
  - Verificar que `schema.d.ts` incluye `PortfolioPositionPatchDto` y los nuevos paths PATCH/DELETE en `/api/v1/portfolio/positions/{fibraId}`.

### T6 — Frontend: crear `EditableCell.tsx`

- [x] T6.1 — Crear `src/Web/Main/src/modules/portafolio/EditableCell.tsx`:
  ```tsx
  import { useRef, useState } from 'react'
  import { Input } from '@/shared/ui/input'

  interface EditableCellProps {
    value: number
    format: (v: number) => string
    validate: (raw: string) => string | null
    parse: (raw: string) => number
    onSave: (newValue: number) => Promise<void>
    className?: string
  }

  export function EditableCell({ value, format, validate, parse, onSave, className }: EditableCellProps) {
    const [editing, setEditing] = useState(false)
    const [draft, setDraft] = useState('')
    const [error, setError] = useState<string | null>(null)
    const [saving, setSaving] = useState(false)
    const inputRef = useRef<HTMLInputElement>(null)

    function startEditing() {
      setDraft(String(value))
      setError(null)
      setEditing(true)
      setTimeout(() => inputRef.current?.select(), 0)
    }

    function cancel() {
      setEditing(false)
      setError(null)
    }

    async function save() {
      const err = validate(draft)
      if (err) {
        setError(err)
        return
      }
      setSaving(true)
      try {
        await onSave(parse(draft))
        setEditing(false)
        setError(null)
      } catch {
        setError('Error al guardar. Intenta de nuevo.')
      } finally {
        setSaving(false)
      }
    }

    if (!editing) {
      return (
        <span
          className={`cursor-text select-none rounded px-1 hover:bg-muted/60 ${className ?? ''}`}
          onDoubleClick={startEditing}
          title="Doble clic para editar"
        >
          {format(value)}
        </span>
      )
    }

    return (
      <span className="relative inline-flex flex-col gap-0.5">
        <Input
          ref={inputRef}
          value={draft}
          onChange={(e) => { setDraft(e.target.value); setError(null) }}
          onKeyDown={(e) => {
            if (e.key === 'Enter') { e.preventDefault(); void save() }
            if (e.key === 'Escape') cancel()
          }}
          onBlur={() => void save()}
          disabled={saving}
          className="h-7 w-28 text-right tabular-nums text-sm"
          autoFocus
        />
        {error && (
          <span className="absolute top-full left-0 z-10 mt-0.5 whitespace-nowrap rounded bg-destructive px-2 py-0.5 text-xs text-destructive-foreground shadow">
            {error}
          </span>
        )}
      </span>
    )
  }
  ```

  **Notas:**
  - `validate` recibe el string crudo del input y devuelve `null` si es válido o un mensaje de error.
  - `parse` convierte el string crudo al número (ej. `parseInt` o `parseFloat`).
  - `format` es la función de display cuando no está editando (ej. `formatVolume` para Titulos, `formatMoney` para CostoPromedio).
  - El `onBlur` también guarda — si hay error de validación, el foco se pierde pero la celda queda en modo de error visible brevemente y luego cancela. Considera omitir el save en blur si el draft no cambió vs el valor original.

### T7 — Frontend: crear `DeletePositionDialog.tsx`

- [x] T7.1 — Crear `src/Web/Main/src/modules/portafolio/DeletePositionDialog.tsx`:
  ```tsx
  import { Button } from '@/shared/ui/button'
  import {
    Dialog,
    DialogContent,
    DialogDescription,
    DialogFooter,
    DialogHeader,
    DialogTitle,
  } from '@/shared/ui/dialog'

  interface DeletePositionDialogProps {
    ticker: string
    open: boolean
    onOpenChange: (open: boolean) => void
    onConfirm: () => Promise<void>
    isLoading: boolean
  }

  export function DeletePositionDialog({
    ticker,
    open,
    onOpenChange,
    onConfirm,
    isLoading,
  }: DeletePositionDialogProps) {
    return (
      <Dialog open={open} onOpenChange={onOpenChange}>
        <DialogContent showCloseButton={false}>
          <DialogHeader>
            <DialogTitle>Eliminar posición {ticker}</DialogTitle>
            <DialogDescription>
              Esta acción no se puede deshacer. La posición {ticker} será eliminada de tu portafolio.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => onOpenChange(false)}
              disabled={isLoading}
            >
              Cancelar
            </Button>
            <Button
              variant="destructive"
              onClick={() => void onConfirm()}
              disabled={isLoading}
            >
              {isLoading ? 'Eliminando...' : 'Eliminar'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    )
  }
  ```

  **Nota:** El proyecto tiene `Dialog` en `@/shared/ui/dialog` con soporte para `showCloseButton`, `DialogFooter`, `DialogHeader`, `DialogTitle`, `DialogDescription`. AlertDialog NO existe en el proyecto — usar este patrón con `Dialog`.

### T8 — Frontend: mutations en `PortafolioPage.tsx`

- [x] T8.1 — Agregar las dos mutations en `PortafolioPage.tsx`:
  ```tsx
  import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'

  // ... dentro de PortafolioPage():
  const patchMutation = useMutation({
    mutationFn: async ({
      fibraId,
      titulos,
      costoPromedio,
    }: {
      fibraId: string
      titulos: number
      costoPromedio: number
    }) => {
      const { error } = await apiClient.PATCH('/api/v1/portfolio/positions/{fibraId}', {
        params: { path: { fibraId } },
        body: { titulos, costoPromedio },
      })
      if (error) throw new Error('No se pudo guardar la posición.')
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['portfolio', 'positions'] })
    },
  })

  const deleteMutation = useMutation({
    mutationFn: async (fibraId: string) => {
      const { error } = await apiClient.DELETE('/api/v1/portfolio/positions/{fibraId}', {
        params: { path: { fibraId } },
      })
      if (error) throw new Error('No se pudo eliminar la posición.')
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['portfolio', 'positions'] })
    },
  })
  ```

- [x] T8.2 — Pasar los callbacks a `PositionsTable`:
  ```tsx
  <PositionsTable
    positions={positions}
    enabledColumns={enabledColumns}
    onUpdate={(fibraId, titulos, costoPromedio) =>
      patchMutation.mutateAsync({ fibraId, titulos, costoPromedio })
    }
    onDelete={(fibraId) => deleteMutation.mutateAsync(fibraId)}
  />
  ```

### T9 — Frontend: modificar `PositionsTable.tsx`

- [x] T9.1 — Agregar props al interface:
  ```tsx
  interface PositionsTableProps {
    positions: PortfolioPositionDto[]
    enabledColumns: string[]
    onUpdate: (fibraId: string, titulos: number, costoPromedio: number) => Promise<void>
    onDelete: (fibraId: string) => Promise<void>
  }
  ```

- [x] T9.2 — Agregar estado para el diálogo de eliminación:
  ```tsx
  const [deletingFibraId, setDeletingFibraId] = useState<string | null>(null)
  const [deleteLoading, setDeleteLoading] = useState(false)

  async function handleDeleteConfirm() {
    if (!deletingFibraId) return
    setDeleteLoading(true)
    try {
      await onDelete(deletingFibraId)
      setDeletingFibraId(null)
    } finally {
      setDeleteLoading(false)
    }
  }
  ```

- [x] T9.3 — Actualizar `totalCols`:
  ```tsx
  const totalCols = 12 + visibleOptionalColumns.length  // +1 por columna Acciones
  ```

- [x] T9.4 — Agregar columna "Acciones" al `<thead>`:
  ```tsx
  <th className="w-16 px-2 py-3 text-center font-semibold text-foreground">Acc.</th>
  ```
  Colocarla DESPUÉS de las columnas opcionales (al final de `<tr>` del thead).

- [x] T9.5 — En la celda de Títulos, reemplazar el texto estático por `EditableCell`:
  ```tsx
  <td className="px-3 py-3 text-right tabular-nums">
    <EditableCell
      value={position.titulos}
      format={(v) => formatVolume(v)}
      validate={(raw) => {
        const n = parseInt(raw, 10)
        if (!Number.isInteger(n) || n <= 0 || String(n) !== raw.trim())
          return 'La cantidad debe ser un entero positivo'
        return null
      }}
      parse={(raw) => parseInt(raw, 10)}
      onSave={(newVal) => onUpdate(position.fibraId, newVal, position.costoPromedio)}
    />
  </td>
  ```

- [x] T9.6 — En la celda de Costo Promedio, reemplazar por `EditableCell`:
  ```tsx
  <td className="px-3 py-3 text-right tabular-nums">
    <EditableCell
      value={position.costoPromedio}
      format={(v) => formatMoney(v)}
      validate={(raw) => {
        const n = parseFloat(raw)
        if (!Number.isFinite(n) || n <= 0) return 'El costo promedio debe ser mayor a cero'
        return null
      }}
      parse={(raw) => parseFloat(raw)}
      onSave={(newVal) => onUpdate(position.fibraId, position.titulos, newVal)}
    />
  </td>
  ```

- [x] T9.7 — Agregar celda de Acciones después de las columnas opcionales:
  ```tsx
  <td className="px-2 py-3 text-center">
    <button
      type="button"
      className="rounded p-1 text-muted-foreground hover:bg-destructive/10 hover:text-destructive"
      onClick={() => setDeletingFibraId(position.fibraId)}
      aria-label={`Eliminar posición ${position.ticker}`}
      title={`Eliminar posición ${position.ticker}`}
    >
      🗑
    </button>
  </td>
  ```
  **Nota:** Si `lucide-react` está disponible, usar `<Trash2 className="h-4 w-4" />`. Verificar primero:
  ```bash
  grep "lucide-react" src/Web/Main/package.json
  ```

- [x] T9.8 — Agregar el `DeletePositionDialog` fuera del `<table>`, al final del return de `PositionsTable`:
  ```tsx
  {deletingFibraId && (
    <DeletePositionDialog
      ticker={positions.find(p => p.fibraId === deletingFibraId)?.ticker ?? deletingFibraId}
      open={deletingFibraId !== null}
      onOpenChange={(open) => { if (!open) setDeletingFibraId(null) }}
      onConfirm={handleDeleteConfirm}
      isLoading={deleteLoading}
    />
  )}
  ```

### T10 — Tests unitarios: `PortfolioRepositoryEditTests.cs`

- [x] T10.1 — Crear `tests/Unit/Infrastructure.Tests/Portfolio/PortfolioRepositoryEditTests.cs`:
  Usar el mismo patrón InMemory + `AppDbContext` del resto de tests de Infrastructure.
  - `GetPosition_ExistingPosition_ReturnsPosition` — insert 1 posición, GetPositionAsync → no null
  - `GetPosition_WrongUser_ReturnsNull` — insert con userId A, buscar con userId B → null
  - `UpdatePosition_ChangesPersistedCorrectly` — update titulos/costoTotalCompra, re-fetch → valores nuevos
  - `DeletePosition_RemovesFromDb` — delete, GetByUserIdAsync → 0 posiciones
  - `DeletePosition_WrongUser_ReturnsFalse` — intento de eliminar posición de otro usuario → false, posición intacta

- [x] T10.2 — Ejecutar:
  ```bash
  dotnet test tests/Unit/Infrastructure.Tests/ --filter "PortfolioRepositoryEdit" --configuration Release
  ```
  Resultado esperado: 5/5 pasando

### T11 — Build verification

- [x] T11.1 — `dotnet build FIBRADIS.slnx --configuration Release` → 0 errores
- [x] T11.2 — `npm run build --workspace=src/Web/Main` → 0 errores TypeScript

## Dev Notes

### Importante: estado git al comenzar esta historia

El branch `story/6-4-edicion-inline` fue creado desde `story/6-3-filas-expandibles` que tiene cambios sin commit de la historia 6-3. **Antes de implementar 6-4**, el dev agent debe confirmar que los cambios de 6-3 están committed. Si no lo están, hacer commit del trabajo de 6-3 primero (ver Dev Agent Record de 6-3 para la lista de archivos).

### Fórmula de `CostoTotalCompra`

La misma que se usa en el upload (historia 6-1):
```
CostoTotalCompra = Titulos × CostoPromedio × (1 + CommissionFactor)
```

`CommissionFactor` se lee de `IOperationalConfigRepository.GetAsync()`. El valor por defecto en BD es `0.006` (0.6%).

El PATCH endpoint hace exactamente lo mismo que el upload: recibe `Titulos` y `CostoPromedio`, lee el `CommissionFactor` actual del servidor, y recalcula `CostoTotalCompra`.

### Identificación de posición: `fibraId` como clave

Cada usuario tiene como máximo una posición por FIBRA. Por eso la ruta usa `fibraId` como identificador de la posición en lugar del `Id` interno de `PortfolioPosition`.

Los endpoints son:
- `PATCH /api/v1/portfolio/positions/{fibraId}` — ownership check implícito: filtramos por `userId + fibraId`
- `DELETE /api/v1/portfolio/positions/{fibraId}` — idem

No se expone el `Id` interno de `PortfolioPosition` al frontend.

### Invalidación de KPIs

Los KPIs del portafolio están calculados en el backend y se devuelven en el mismo endpoint `GET /api/v1/portfolio`. Después de un PATCH o DELETE exitoso, el frontend invalida `['portfolio', 'positions']` y React Query refresca automáticamente todo: KPIs y tabla de posiciones.

**No** hay optimistic updates — la UI espera el refetch completo. Este es el patrón del proyecto.

### `Dialog` vs `AlertDialog`

El proyecto **no tiene** `AlertDialog` instalado. Solo tiene `Dialog` (en `src/Web/Main/src/shared/ui/dialog.tsx`). Usar `Dialog` con `DialogFooter` para el diálogo de confirmación de eliminación. El componente `DeletePositionDialog` ya está diseñado para esto.

### Componente `EditableCell` — UX edge cases

- **Double-click en celda expandida**: si la fila está expandida y el usuario hace doble clic en Titulos o CostoPromedio, la edición funciona igual — el `EditableCell` es local a la celda de la fila compacta.
- **Click fuera (blur)**: el `onBlur` del input dispara `save()`. Si el valor no cambió, igual va al servidor. Para evitar la llamada innecesaria, comparar `parse(draft) !== value` antes de llamar `onSave`.
  - Si el valor no cambió → cancelar silenciosamente (solo salir del modo edición)
  - Si cambió → guardar normalmente
- **Estado `saving`**: durante el guardado, el input está `disabled`. Si el servidor devuelve error, el input vuelve a quedar activo con el error visible. El usuario puede reintentar o presionar Escape.
- **Input width**: `w-28` es 7rem, suficiente para cantidades razonables. Si el valor es muy largo, puede ajustarse.

### `lucide-react` en el proyecto

Verificar antes de implementar T9.7:
```bash
grep "lucide-react" src/Web/Main/package.json
```
Si está disponible, usar `<Trash2 className="h-4 w-4" />` en lugar del emoji 🗑. El `SignalBadge.tsx` de historia 6-3 ya verificó si estaba disponible.

### Deuda continuada de historias previas

- **D1** (de 6-3): `GetUserId` lanza `FormatException` si el claim está ausente — sigue activo, no aplica directamente a las operaciones de esta historia pero los nuevos endpoints heredan el mismo riesgo.
- **D7** (de 6-3): `NormalizeColumns` whitelist no incluye `week52Low`, `week52Avg`, `volume` — sigue activo, no aplica a esta historia.

### `IOperationalConfigRepository` — ya registrado en DI

Verificar en `ApiServiceExtensions.cs` o `Program.cs` que `IOperationalConfigRepository` ya está registrado como servicio. Lo usa el endpoint `/upload` de historia 6-1. No es necesario agregarlo.

### Orden de columnas en `PositionsTable` después de esta historia

1. **Señal** (badge — siempre visible)
2. **▶** (expand button — siempre visible)
3. Ticker/Nombre
4. Títulos ← editable inline (doble clic)
5. Costo Promedio ← editable inline (doble clic)
6. Precio Actual
7. Valor de Mercado
8. Plusvalía %
9. Ganancia $
10. Renta Anual
11. % Portafolio
12. Columnas opcionales (capRate, navPerCbfi, ltv, etc.)
13. **Acc.** (botón eliminar — siempre visible, al final)

`totalCols = 12 + visibleOptionalColumns.length`

### EF Core — llamadas secuenciales en endpoints nuevos

Los endpoints PATCH y DELETE son simples (1-2 llamadas al DbContext). No hay riesgo de `Task.WhenAll` con el mismo DbContext. Seguir el patrón `await` secuencial del resto de endpoints.

### Validación de input numérico en frontend

Para `Titulos` (entero):
```typescript
// Validar que sea entero positivo estricto
// Rechazar "1.5", "0", "-1", ""
const n = parseInt(raw.trim(), 10)
if (!Number.isInteger(n) || n <= 0 || String(n) !== raw.trim()) return 'La cantidad debe ser un entero positivo'
```

Para `CostoPromedio` (decimal):
```typescript
// Aceptar "47.5", "100", "0.5" — rechazar "0", "-47.5", ""
const n = parseFloat(raw.trim())
if (!Number.isFinite(n) || n <= 0) return 'El costo promedio debe ser mayor a cero'
```

### Referencias

- [FR-25: Confirmación de reemplazo + edición inline](../../planning-artifacts/epics.md#fr-25)
- [FR-53: Edición inline de posiciones (Qty/AvgCost)](../../planning-artifacts/epics.md#fr-53)
- [Historia 6.3 — Filas expandibles (contexto UI)](6-3-filas-expandibles-con-detalle-de-posicion-y-badge-de-senal-nav.md)
- [Historia 6.1 — Fórmula CostoTotalCompra + CommissionFactor](6-1-carga-y-validacion-del-portafolio.md)
- [Convenciones FIBRADIS](../../planning-artifacts/convenciones-fibradis.md)
- [AGENTS.md — reglas críticas del proyecto](../../../AGENTS.md)

## Senior Developer Review (AI)

_Pendiente — completar después de implementación._

## Dev Agent Record

### Debug Log

- 2026-06-03: Commiteé el trabajo previo de épicas 6-1/6-2/6-3 (61 archivos) antes de empezar la implementación — estaban sin commit en el working tree.
- 2026-06-03: `DeletePositionAsync` cambiado de `ExecuteDeleteAsync` a `Find + Remove + SaveChanges` porque InMemory provider no soporta `ExecuteDeleteAsync`. El comportamiento en SQL Server es idéntico para operaciones de fila única.
- 2026-06-03: Corregidos casts `Number()` en `PositionsTable.tsx` para los campos `titulos` y `costoPromedio` — el schema OpenAPI los genera como `number | string`.
- 2026-06-03: Añadido `<span className="sr-only">Expandir</span>` en `<th>` vacío para corregir hint de accesibilidad del IDE.

### Completion Notes

- Implementados endpoints `PATCH /api/v1/portfolio/positions/{fibraId}` y `DELETE /api/v1/portfolio/positions/{fibraId}`. Ambos aplican ownership check implícito vía `userId + fibraId`.
- PATCH recalcula `CostoTotalCompra = Titulos × CostoPromedio × (1 + CommissionFactor)` con el factor actual de `OperationalConfig`.
- Componente `EditableCell.tsx` con doble-clic para editar, Enter para guardar, Escape para cancelar, validación inline y skip si el valor no cambió.
- Componente `DeletePositionDialog.tsx` usando `Dialog` del proyecto (AlertDialog no existe).
- `PositionsTable.tsx` actualizado con columna Acc. (Trash2), props `onUpdate`/`onDelete`, Fragment wrapper para el diálogo.
- Tests ejecutados:
  - `dotnet test tests/Unit/Infrastructure.Tests/ --filter "PortfolioRepositoryEdit"` → `5/5 passing`
  - `dotnet test tests/Unit/Infrastructure.Tests/` → `219/219 passing`
  - `dotnet build FIBRADIS.slnx --configuration Release` → `0 errores`
  - `npm run build --workspace=src/Web/Main` → `0 errores TypeScript`

## File List

- `src/Server/SharedApiContracts/Portfolio/PortfolioResponseDto.cs`
- `src/Server/Application/Portfolio/IPortfolioRepository.cs`
- `src/Server/Infrastructure/Persistence/Repositories/Portfolio/PortfolioRepository.cs`
- `src/Server/Api/Endpoints/Private/PortfolioEndpoints.cs`
- `scripts/codegen/Api.json`
- `src/Web/SharedApiClient/schema.d.ts`
- `src/Web/Main/src/modules/portafolio/EditableCell.tsx`
- `src/Web/Main/src/modules/portafolio/DeletePositionDialog.tsx`
- `src/Web/Main/src/modules/portafolio/PortafolioPage.tsx`
- `src/Web/Main/src/modules/portafolio/PositionsTable.tsx`
- `tests/Unit/Infrastructure.Tests/Portfolio/PortfolioRepositoryEditTests.cs`
- `_bmad-output/implementation-artifacts/6-4-edicion-inline-y-eliminacion-de-posiciones.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

## Change Log

- 2026-06-03: Historia 6.4 creada — edición inline Qty/CostoPromedio y eliminación de posiciones con confirmación
- 2026-06-03: Historia 6.4 implementada — endpoints PATCH/DELETE portafolio, EditableCell, DeletePositionDialog, 5 unit tests verdes (219/219 total)
