---
title: 'Filtros de tipo de evento en el Calendario'
type: 'feature'
created: '2026-06-19'
status: 'done'
baseline_commit: '1b43539b973b548577fc1109c110a057fd8bd068'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** La página `/calendario` muestra todos los eventos (Pagos, Ex derechos, Avisos BMV) mezclados sin forma de filtrar, lo que dificulta enfocarse en un tipo específico.

**Approach:** Agregar tres pills toggle en la sección del calendario que filtren simultáneamente el grid mensual y la lista "Agenda de mes". Sin cambios de backend; filtrado 100% en cliente sobre los datos ya descargados.

## Boundaries & Constraints

**Always:**
- Los filtros son pills toggle tipo button (no checkboxes, no dropdown). Sin filtro activo = todos los eventos visibles.
- OR logic entre filtros activos: un evento pasa si cumple al menos uno de los filtros activos.
- El filtro afecta AMBOS: el `CalendarGrid` (vía `groupedEvents`) Y la lista "Agenda de mes" (vía `upcomingEvents`).
- Los chips de leyenda decorativa del hero (`Pagos`, `Ex derechos`, `Avisos BMV`) permanecen decorativos; los filtros son un elemento nuevo en la toolbar del calendario.
- Filtro "Avisos BMV" = `event.avisoUrl != null`. No es un `eventType` nuevo.
- Mantener el límite `slice(0, 10)` de `upcomingEvents` sobre los eventos **ya filtrados**.

**Ask First:** Si el contador de "Eventos" en `SummaryCard` debe reflejar el total original o solo los filtrados (decisión UX de consistencia).

**Never:**
- No modificar el backend (`CalendarEventDto`, endpoint `GET /api/v1/market/events`).
- No agregar un nuevo `EventType` en frontend/backend.
- No reemplazar el grid mensual por otra vista alternativa.
- No paginación ni lazy-load en la lista filtrada.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Sin filtro activo | Todos los eventos del mes | Grid y lista muestran todos | N/A |
| Solo "Pagos" activo | Eventos del mes | Solo `eventType === 'Pago'` en grid y lista | N/A |
| Solo "Ex derechos" activo | Eventos del mes | Solo `eventType === 'ExDerecho'` en grid y lista | N/A |
| Solo "Avisos BMV" activo | Eventos del mes | Solo eventos con `avisoUrl != null` en grid y lista | N/A |
| "Pagos" + "Avisos BMV" activos | Eventos del mes | Eventos que sean Pago OR que tengan avisoUrl (OR logic) | N/A |
| Mes sin eventos + filtro activo | Sin datos | Empty state igual que sin filtro activo: "Sin distribuciones registradas para este mes." | N/A |
| Filtro activo + mes cargando | `isLoading` | Spinner del calendario; pills visibles pero sin efecto visual (grid oculto) | N/A |

</frozen-after-approval>

## Code Map

- `src/Web/Main/src/modules/calendario/CalendarioPage.tsx` — página raíz: añade `activeFilters` state, lógica de filtrado, renderiza la FilterBar
- `src/Web/Main/src/modules/calendario/CalendarGrid.tsx` — recibe `groupedEvents` ya filtrado; sin cambios de interfaz
- `src/Web/Main/src/modules/calendario/EventChip.tsx` — chip visual; sin cambios
- `src/Web/Main/src/modules/calendario/useCalendarEvents.ts` — hook de datos; sin cambios
- `src/Web/Main/src/api/calendarApi.ts` — tipo `MarketCalendarEvent`; sin cambios

## Tasks & Acceptance

**Execution:**
- [x] `src/Web/Main/src/modules/calendario/CalendarioPage.tsx` — añadir `activeFilters: Set<'Pago' | 'ExDerecho' | 'Aviso'>` con `useState`, función `toggleFilter`, función pura `applyFilters(events, activeFilters)` que retorna los eventos filtrados con OR logic, y componente inline `FilterBar` con tres pills toggle (verde/Pagos, azul/Ex derechos, slate/Avisos BMV). Reemplazar `events` por `filteredEvents` en los derivados `groupedEvents` y `upcomingEvents`.

**Acceptance Criteria:**
- Dado que cargo `/calendario`, cuando no hay filtro activo, entonces el grid y la lista muestran todos los eventos del mes.
- Dado que hago clic en "Pagos", cuando el filtro se activa (pill con apariencia pressed), entonces el grid y la lista muestran únicamente eventos con `eventType === 'Pago'`.
- Dado que hago clic en "Ex derechos", cuando el filtro se activa, entonces el grid y la lista muestran únicamente eventos con `eventType === 'ExDerecho'`.
- Dado que hago clic en "Avisos BMV", cuando el filtro se activa, entonces el grid y la lista muestran únicamente eventos con `avisoUrl != null`.
- Dado que tengo "Pagos" y "Avisos BMV" activos simultáneamente, cuando aplica OR logic, entonces se muestran eventos que sean Pago O que tengan avisoUrl.
- Dado que vuelvo a hacer clic en un filtro activo, cuando se desactiva, entonces los eventos filtrados se re-evalúan (si quedan filtros activos) o se muestran todos.
- Dado que el mes no tiene eventos, cuando hay filtros activos, entonces el empty state "Sin distribuciones registradas para este mes." sigue apareciendo.

## Spec Change Log

## Design Notes

Los pills toggle deben reutilizar los colores semánticos ya establecidos:
- **Pagos**: activo = `bg-green-100 text-green-800 border-green-300`; inactivo = `bg-background text-muted-foreground border-border`
- **Ex derechos**: activo = `bg-blue-100 text-blue-800 border-blue-300`; inactivo = `bg-background text-muted-foreground border-border`
- **Avisos BMV**: activo = `bg-slate-100 text-slate-700 border-slate-300`; inactivo = `bg-background text-muted-foreground border-border`

Colocar la `FilterBar` entre la toolbar de navegación de mes (prev/hoy/next) y el `CalendarGrid` — así queda visualmente ligada al calendario.

Ejemplo de `applyFilters`:
```ts
function applyFilters(events: MarketCalendarEvent[], active: Set<string>): MarketCalendarEvent[] {
  if (active.size === 0) return events
  return events.filter(e =>
    (active.has('Pago') && e.eventType === 'Pago') ||
    (active.has('ExDerecho') && e.eventType === 'ExDerecho') ||
    (active.has('Aviso') && e.avisoUrl != null)
  )
}
```

## Verification

**Commands:**
- `npm run build --workspace=src/Web/Main` -- expected: 0 errores TypeScript

## Suggested Review Order

**Lógica de filtrado**

- Tipo union y estado inicial; OR logic en `applyFilters`
  [`CalendarioPage.tsx:16`](../../src/Web/Main/src/modules/calendario/CalendarioPage.tsx#L16)

- `filteredEvents` derivado que alimenta grid, lista y summary
  [`CalendarioPage.tsx:49`](../../src/Web/Main/src/modules/calendario/CalendarioPage.tsx#L49)

- Función pura `applyFilters` — OR entre tres condiciones
  [`CalendarioPage.tsx:312`](../../src/Web/Main/src/modules/calendario/CalendarioPage.tsx#L312)

**Interacción y UI**

- `toggleFilter` con updater funcional; estable gracias a `useCallback([])`
  [`CalendarioPage.tsx:23`](../../src/Web/Main/src/modules/calendario/CalendarioPage.tsx#L23)

- Punto de montaje de `FilterBar` entre nav de mes y grid
  [`CalendarioPage.tsx:141`](../../src/Web/Main/src/modules/calendario/CalendarioPage.tsx#L141)

- Constante `FILTER_OPTIONS` con clases por estado activo/inactivo
  [`CalendarioPage.tsx:262`](../../src/Web/Main/src/modules/calendario/CalendarioPage.tsx#L262)

- Componente `FilterBar`: pills toggle con `aria-pressed` y botón "Limpiar"
  [`CalendarioPage.tsx:268`](../../src/Web/Main/src/modules/calendario/CalendarioPage.tsx#L268)
