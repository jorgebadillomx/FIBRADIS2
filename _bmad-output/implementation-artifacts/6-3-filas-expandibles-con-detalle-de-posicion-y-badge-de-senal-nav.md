# Historia 6.3: Filas expandibles con detalle de posición y badge de señal NAV

Status: done

## Story

Como usuario,
quiero expandir cualquier fila del portafolio para ver secciones de detalle (Mi posición, Mercado, Fundamentales, Distribuciones) y un badge de señal de color mostrando NAV vs precio de mercado,
para que pueda analizar cada posición en profundidad directamente desde la tabla del portafolio.

## Acceptance Criteria

### AC1 — Filas expandibles con 4 secciones

**Dado que** hago clic para expandir la fila de FUNO11,
**Cuando** se renderiza la sección expandida,
**Entonces** veo cuatro secciones claramente etiquetadas:
- **Mi posición**: títulos, costo promedio, valor de mercado, plusvalía % y $
- **Mercado**: precio actual, cambio %, volumen, AVG 52S, máximo/mínimo 52S
- **Fundamentales**: Cap Rate, NAV, LTV, NOI, FFO con etiquetas de período
- **Distribuciones**: últimas 4 distribuciones, renta trimestral y anual, yield calculado y decretado

### AC2 — Badge señal verde (descuento > 10%)

**Dado que** el NAV por CBFI de FUNO11 es $120 y el precio actual es $100 (16.7% por debajo del NAV),
**Entonces** el badge de señal es **verde**.

### AC3 — Badge señal amarillo (±10%)

**Dado que** una posición donde el precio es $105 y el NAV es $100 (5% por encima del NAV, dentro de ±10%),
**Entonces** el badge es **amarillo**.

### AC4 — Badge señal rojo (premium > 10%)

**Dado que** una posición donde el precio es $115 y el NAV es $100 (15% por encima del NAV),
**Entonces** el badge es **rojo**.

### AC5 — Badge señal gris (sin datos de NAV)

**Dado que** los datos de NAV no están disponibles para una posición,
**Entonces** el badge es **gris**.

### AC6 — Tooltip en badge

**Y** todos los badges tienen un tooltip que explica el criterio: "Cotiza con descuento de X% respecto al NAV" / "Cotiza con prima de X% respecto al NAV" / "Cotiza dentro de ±10% del NAV" / "Sin datos de NAV".

## Tasks / Subtasks

### T1 — SharedApiContracts: extender `PortfolioPositionDto` y agregar `PortfolioDistributionDto`

- [x] T1.1 — Abrir `src/Server/SharedApiContracts/Portfolio/PortfolioResponseDto.cs`
- [x] T1.2 — Agregar al inicio del archivo el nuevo record:
  ```csharp
  public sealed record PortfolioDistributionDto(
      string PaymentDate,    // "YYYY-MM-DD" — DateOnly serializada como string
      decimal AmountPerUnit
  );
  ```
- [x] T1.3 — Extender `PortfolioPositionDto` con los nuevos campos (agregar al final del record, después de `Week52High`):
  ```csharp
  long? Volume,
  decimal? Week52Low,
  decimal? Week52Avg,
  string? FundamentalsPeriod,
  IReadOnlyList<PortfolioDistributionDto> RecentDistributions
  ```
  El record quedará:
  ```csharp
  public sealed record PortfolioPositionDto(
      Guid FibraId,
      string Ticker,
      string Nombre,
      int Titulos,
      decimal CostoPromedio,
      decimal CostoTotalCompra,
      decimal PctPortafolio,
      decimal? PrecioActual,
      decimal? ValorMercado,
      decimal? PlusvaliaFilaPct,
      decimal? PlusvaliaFilaMxn,
      decimal? RentaAnual,
      string? FreshnessStatus,
      decimal? CapRate,
      decimal? NavPerCbfi,
      decimal? Ltv,
      decimal? NoiMargin,
      decimal? FfoMargin,
      decimal? DailyChangePct,
      decimal? Week52High,
      long? Volume,
      decimal? Week52Low,
      decimal? Week52Avg,
      string? FundamentalsPeriod,
      IReadOnlyList<PortfolioDistributionDto> RecentDistributions
  );
  ```

### T2 — Application: `GetWeek52AvgByFibrasAsync` en `IMarketRepository`

- [x] T2.1 — Agregar a `src/Server/Application/Market/IMarketRepository.cs`:
  ```csharp
  Task<IReadOnlyDictionary<Guid, decimal>> GetWeek52AvgByFibrasAsync(
      IReadOnlyList<Guid> fibraIds, int days = 365, CancellationToken ct = default);
  ```

### T3 — Infrastructure: implementar `GetWeek52AvgByFibrasAsync` en `MarketRepository`

- [x] T3.1 — Agregar a `src/Server/Infrastructure/Persistence/Repositories/Market/MarketRepository.cs`:
  ```csharp
  public async Task<IReadOnlyDictionary<Guid, decimal>> GetWeek52AvgByFibrasAsync(
      IReadOnlyList<Guid> fibraIds, int days = 365, CancellationToken ct = default)
  {
      if (fibraIds.Count == 0)
          return new Dictionary<Guid, decimal>();

      var cutoff = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-days);
      var result = await db.DailySnapshots
          .Where(s => fibraIds.Contains(s.FibraId) && s.Date >= cutoff && s.Close.HasValue)
          .GroupBy(s => s.FibraId)
          .Select(g => new { FibraId = g.Key, Avg = g.Average(s => s.Close!.Value) })
          .ToListAsync(ct);

      return result.ToDictionary(r => r.FibraId, r => r.Avg);
  }
  ```
  **Verificar** que `db.DailySnapshots` existe en `AppDbContext`. Si el DbSet se llama diferente, ajustar.

### T4 — API: actualizar `PortfolioEndpoints.cs` (GET `/api/v1/portfolio`)

- [x] T4.1 — En `src/Server/Api/Endpoints/Private/PortfolioEndpoints.cs`, en el handler del `GET /`:
  - Agregar parámetro `IBmvSchedule bmvSchedule` si no está ya (ya está)
  - **Después** de obtener `latestFundamentals`, agregar la llamada a Week52Avg (secuencial):
    ```csharp
    var week52Avgs = await marketRepo.GetWeek52AvgByFibrasAsync(fibraIds, 365, ct);
    ```
  - En la proyección de `positionDtos`, poblar los nuevos campos:
    ```csharp
    week52Avgs.TryGetValue(row.FibraId, out var week52Avg);
    var recentDists = distsByFibra.TryGetValue(row.FibraId, out var dists)
        ? dists.Take(4)                 // distsByFibra ya está ordenado desc por PaymentDate
               .Select(d => new PortfolioDistributionDto(
                   d.PaymentDate.ToString("yyyy-MM-dd"),
                   d.AmountPerUnit))
               .ToArray()
        : Array.Empty<PortfolioDistributionDto>();

    return new PortfolioPositionDto(
        // ... campos existentes ...
        Volume: snapshot?.Volume,
        Week52Low: snapshot?.Week52Low,
        Week52Avg: week52Avg == 0 ? null : week52Avg,
        FundamentalsPeriod: fundamental?.Period,
        RecentDistributions: recentDists
    );
    ```
  - **CRÍTICO**: la llamada a `GetWeek52AvgByFibrasAsync` va DESPUÉS de `GetDistributionsByFibrasAsync` y ANTES de calcular los KPIs — mantener secuencial.

### T5 — Codegen: regenerar cliente API

- [x] T5.1 — Ejecutar `npm run codegen:api`
  - Si el proceso de la API está corriendo y bloquea los DLLs, detenerlo antes.
  - Verificar que `schema.d.ts` incluye los nuevos campos en `PortfolioPositionDto`.

### T6 — Frontend: crear `SignalBadge.tsx`

- [x] T6.1 — Crear `src/Web/Main/src/modules/portafolio/SignalBadge.tsx`:

  ```tsx
  type SignalStatus = 'verde' | 'amarillo' | 'rojo' | 'gris'

  interface SignalBadgeProps {
    navPerCbfi: number | null | undefined
    precioActual: number | null | undefined
  }

  const BADGE_CLASSES: Record<SignalStatus, string> = {
    verde:    'bg-green-100 text-green-800 border-green-300',
    amarillo: 'bg-yellow-100 text-yellow-800 border-yellow-300',
    rojo:     'bg-red-100 text-red-800 border-red-300',
    gris:     'bg-muted text-muted-foreground border-border',
  }

  const BADGE_LABELS: Record<SignalStatus, string> = {
    verde:    'Descuento',
    amarillo: 'Neutro',
    rojo:     'Prima',
    gris:     'Sin NAV',
  }

  export function calcSignal(
    navPerCbfi: number | null | undefined,
    precioActual: number | null | undefined
  ): { status: SignalStatus; tooltip: string } {
    if (!navPerCbfi || !precioActual || navPerCbfi <= 0) {
      return { status: 'gris', tooltip: 'Sin datos de NAV' }
    }
    const discount = (navPerCbfi - precioActual) / navPerCbfi
    const pct = Math.abs(discount * 100).toFixed(1)
    if (discount > 0.10) {
      return { status: 'verde', tooltip: `Cotiza con descuento de ${pct}% respecto al NAV` }
    }
    if (discount < -0.10) {
      return { status: 'rojo', tooltip: `Cotiza con prima de ${pct}% respecto al NAV` }
    }
    const sign = discount >= 0 ? '-' : '+'
    return {
      status: 'amarillo',
      tooltip: `Cotiza dentro de ±10% del NAV (${sign}${pct}%)`,
    }
  }

  export function SignalBadge({ navPerCbfi, precioActual }: SignalBadgeProps) {
    const { status, tooltip } = calcSignal(navPerCbfi, precioActual)
    return (
      <span
        className={`inline-flex items-center rounded-full border px-2 py-0.5 text-xs font-medium ${BADGE_CLASSES[status]}`}
        title={tooltip}
        aria-label={tooltip}
      >
        {BADGE_LABELS[status]}
      </span>
    )
  }
  ```

  **Nota**: `calcSignal` se exporta para uso en tests y en `PositionExpandedDetail`.

### T7 — Frontend: crear `PositionExpandedDetail.tsx`

- [x] T7.1 — Crear `src/Web/Main/src/modules/portafolio/PositionExpandedDetail.tsx`:
  - Props: `position: components['schemas']['PortfolioPositionDto']`
  - Cuatro secciones en grid (mobile: 1 col, desktop: 2 col por sección):

  **Sección "Mi posición"** (datos que ya tiene el usuario en la fila compacta pero con más detalle):
  - Títulos, Costo Promedio, Costo Total Compra, Valor Mercado, Plusvalía % y $, % Portafolio

  **Sección "Mercado"**:
  - Precio Actual, Cambio $ y %, Volumen (`volume.toLocaleString('es-MX')` o `—`), AVG 52S, Máx. 52S, Mín. 52S

  **Sección "Fundamentales"** (mostrar período de origen si está disponible):
  - Cap Rate, NAV/CBFI, LTV, Margen NOI, Margen FFO
  - Si `FundamentalsPeriod` disponible → mostrar `<span className="text-xs text-muted-foreground">{position.fundamentalsPeriod}</span>` como subtítulo de la sección

  **Sección "Distribuciones"**:
  - Lista de las últimas 4 distribuciones (`recentDistributions`): fecha + monto por CBFI
  - Renta Trimestral estimada = `RentaAnual / 4` (si rentaAnual != null)
  - Renta Anual (TTM): `RentaAnual`
  - Yield estimado = `(rentaAnual / (titulos * precioActual)) * 100` si precioActual != null
  - Ver Dev Notes para el cálculo de yield

  **Nota de formato**: usar `—` para valores null, `formatMoney` para MXN, `formatPercent` para %. Extraer helpers de `PositionsTable.tsx` o crear un `portfolio-format.ts` compartido (ver Dev Notes).

### T8 — Frontend: modificar `PositionsTable.tsx` (expandir filas + badge)

- [x] T8.1 — Agregar estado de expansión:
  ```tsx
  const [expandedRows, setExpandedRows] = useState<Set<string>>(new Set())

  function toggleRow(fibraId: string) {
    setExpandedRows(prev => {
      const next = new Set(prev)
      next.has(fibraId) ? next.delete(fibraId) : next.add(fibraId)
      return next
    })
  }
  ```

- [x] T8.2 — Calcular `colSpan` total dinámicamente para la fila expandida:
  ```tsx
  const totalCols = 10 + visibleOptionalColumns.length + 1  // +1 por columna de badge/expand
  ```

- [x] T8.3 — En el `<thead>`, agregar dos columnas al inicio:
  - Columna "Señal" (badge NAV): `<th className="px-2 py-3 text-left font-semibold text-foreground w-20">Señal</th>`
  - Columna "▸" (expand): `<th className="w-8 px-1 py-3"></th>` — solo el botón de expand

- [x] T8.4 — En cada `<tr>` de posición, agregar las dos celdas correspondientes al inicio:
  ```tsx
  <td className="px-2 py-3">
    <SignalBadge navPerCbfi={position.navPerCbfi} precioActual={position.precioActual} />
  </td>
  <td className="px-1 py-3">
    <button
      type="button"
      className="text-muted-foreground transition-transform hover:text-foreground"
      style={{ transform: isExpanded ? 'rotate(90deg)' : undefined }}
      onClick={() => toggleRow(position.fibraId)}
      aria-label={isExpanded ? 'Colapsar posición' : 'Expandir posición'}
    >
      ▶
    </button>
  </td>
  ```

- [x] T8.5 — Después de cada `<tr>` de posición, agregar la fila expandida (condicional):
  ```tsx
  {isExpanded && (
    <tr key={`${position.fibraId}-detail`}>
      <td colSpan={totalCols} className="bg-muted/20 px-4 py-4">
        <PositionExpandedDetail position={position} />
      </td>
    </tr>
  )}
  ```

### T9 — [Deuda D2 de 6-2] Fix `enabledColumns` en `PortafolioPage.tsx`

- [x] T9.1 — En `src/Web/Main/src/modules/portafolio/PortafolioPage.tsx`:
  - Eliminar el `useState<string[]>` de `enabledColumns`
  - Eliminar el `useEffect` que sincronizaba `enabledColumns` con `columnConfigQuery.data`
  - Derivar directamente: `const enabledColumns = columnConfigQuery.data?.columns ?? []`
  - Asegurarse que los imports de `useEffect` y `useState` para esta variable quedan limpios (si `useState` ya no se usa, removerlo del import)

### T10 — Tests unitarios: `GetWeek52AvgByFibrasAsync`

- [x] T10.1 — Crear `tests/Unit/Infrastructure.Tests/Market/MarketRepositoryWeek52AvgTests.cs`:
  - Usar el mismo patrón InMemory + `AppDbContext` de `PortfolioKpiCalculatorTests.cs`
  - Tests requeridos:
    - `GetWeek52Avg_WithMultipleFibras_ReturnsAverageClosePerFibra` — 2 fibras con 4 snapshots c/u, verifica promedio correcto por fibra
    - `GetWeek52Avg_WithNoSnapshots_ReturnsEmptyDictionary` — sin datos → diccionario vacío
    - `GetWeek52Avg_ExcludesSnapshotsOlderThan365Days` — un snapshot dentro de rango + uno fuera → solo incluye el de dentro
    - `GetWeek52Avg_SkipsSnapshotsWithNullClose` — snapshot con `Close = null` no afecta el promedio

- [x] T10.2 — Ejecutar:
  ```bash
  dotnet test tests/Unit/Infrastructure.Tests/ --filter "MarketRepositoryWeek52Avg" --configuration Release
  ```
  Resultado esperado: 4/4 pasando

### T11 — Build verification

- [x] T11.1 — `dotnet build FIBRADIS.slnx --configuration Release` → 0 errores
- [x] T11.2 — `npm run build --workspace=src/Web/Main` → 0 errores TypeScript

## Dev Notes

### Flujo de datos: todo viene del `GET /api/v1/portfolio` existente

No se crea ningún endpoint nuevo. El `GET /api/v1/portfolio` ya carga en memoria (secuencial, mismo DbContext scoped):
1. Posiciones del usuario
2. PriceSnapshot por fibra (ya incluye `Volume`, `Week52High`, `Week52Low`)
3. Distribuciones batch (365 días) — **ya disponibles en `distsByFibra`**
4. Catálogo de fibras
5. Fundamentales del último período (ya incluye `Period`, `NavPerCbfi`, etc.)
6. **NUEVO**: Week52Avg batch desde DailySnapshot

Solo se extiende el DTO para incluir los campos que el frontend necesita para la vista expandida. Ninguna llamada adicional al backend desde el frontend.

### Week52Avg — fuente y cálculo

`AVG(DailySnapshot.Close)` para los últimos 365 días. El nombre en la interfaz del AC es "AVG 52S" que es convencional; en Yahoo Finance se calcula como el promedio de cierres en las últimas 52 semanas.

El campo `Week52Avg` en el DTO devuelve `null` si no hay snapshots en el período (cero filas tras el filtro).

```csharp
// En el endpoint, después de GetDistributionsByFibrasAsync:
var week52Avgs = await marketRepo.GetWeek52AvgByFibrasAsync(fibraIds, 365, ct);
// ...en proyección:
week52Avgs.TryGetValue(row.FibraId, out var avgRaw);
decimal? week52Avg = avgRaw > 0 ? avgRaw : null;
```

### Lógica del badge de señal NAV

El badge se calcula enteramente en frontend. No hay backend para esto. La fórmula:

```
discount = (NavPerCbfi - PrecioActual) / NavPerCbfi

Verde    → discount > 0.10     → precio está > 10% por debajo del NAV (oportunidad)
Rojo     → discount < -0.10    → precio está > 10% por encima del NAV (premium)
Amarillo → -0.10 ≤ discount ≤ 0.10  → dentro de ±10%
Gris     → NavPerCbfi is null OR PrecioActual is null OR NavPerCbfi <= 0
```

La función `calcSignal` se exporta desde `SignalBadge.tsx` para ser testeada y reutilizada en `PositionExpandedDetail.tsx`.

**Casos límite**:
- `NavPerCbfi = 0`: devolver gris (evitar división por cero)
- `PrecioActual = null`: devolver gris (sin precio de mercado)
- `NavPerCbfi = null`: devolver gris (sin datos fundamentales)

### Cálculo de Yield en la sección Distribuciones

```
YieldAnual% = (RentaAnual / (Titulos × PrecioActual)) × 100
```

Solo mostrar si `RentaAnual != null && PrecioActual != null && PrecioActual > 0`.

"Yield decretado" = yield basado en la distribución del trimestre más reciente × 4:
```
YieldDecretado% = (RecentDistributions[0].AmountPerUnit × 4 / PrecioActual) × 100
```
Solo mostrar si hay al menos 1 distribución reciente y PrecioActual > 0.

"Renta trimestral" = `RentaAnual / 4` (aproximación TTM).

### Helpers de formato compartidos

Los helpers `formatMoney` y `formatPercent` están actualmente duplicados en `PositionsTable.tsx`. Para `PositionExpandedDetail.tsx` extraerlos a un archivo compartido:

```
src/Web/Main/src/modules/portafolio/portfolio-format.ts
```

```typescript
export function formatMoney(value: number | null | undefined): string {
  if (value == null) return '—'
  return value.toLocaleString('es-MX', { style: 'currency', currency: 'MXN' })
}

export function formatPercent(value: number | null | undefined): string {
  if (value == null) return '—'
  return `${value.toLocaleString('es-MX', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}%`
}

export function formatVolume(value: number | null | undefined): string {
  if (value == null) return '—'
  return value.toLocaleString('es-MX')
}
```

Actualizar `PositionsTable.tsx` para importar desde este módulo en lugar de redefinir las funciones localmente.

### DbSet de DailySnapshot

Verificar el nombre del DbSet antes de implementar T3.1:
```bash
grep -n "DailySnapshot" src/Server/Infrastructure/Persistence/SqlServer/AppDbContext.cs
```
Debería ser `db.DailySnapshots` (plural).

### Columnas en `PositionsTable` después de esta historia

Orden de columnas (de izquierda a derecha):
1. **Señal** (badge — siempre visible)
2. **▶** (expand button — siempre visible)
3. Ticker/Nombre
4. Títulos
5. Costo Promedio
6. Precio Actual
7. Valor de Mercado
8. Plusvalía %
9. Ganancia $
10. Renta Anual
11. % Portafolio
12. Columnas opcionales (capRate, navPerCbfi, ltv, etc.)

El `totalCols` para el `colSpan` de la fila expandida = 11 + `visibleOptionalColumns.length`.

### Responsividad mobile (UX-DR3)

La sección expandida debe mostrar las cuatro secciones sin ruptura de layout en mobile:
- **Desktop** (>= md): grid 2x2 o layout de 4 columnas
- **Mobile**: columna única, secciones apiladas verticalmente

```tsx
<div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-4">
  <section>...</section>
  <section>...</section>
  <section>...</section>
  <section>...</section>
</div>
```

### Chevron / expand icon

Si `lucide-react` está disponible en el proyecto, usar `<ChevronRight>` con `rotate-90` para el estado expandido. Si no está disponible, usar el carácter `▶` como texto (ver T8.4). Verificar:
```bash
grep -r "lucide-react" src/Web/Main/package.json
```

### Deuda resuelta de historia 6-2

- **D2** (`enabledColumns` via `useEffect`): resuelto en T9 — derivar directamente de `columnConfigQuery.data?.columns ?? []`.

Deferred que continúa:
- **D3** (keys hardcodeadas en `getStoredAccessToken`): sigue activo. El módulo de autenticación en Main SPA no tiene un contrato claro de almacenamiento de tokens. Resolver cuando se implemente el flujo completo de login en Main SPA.
- **D5** (UploadZone sin accesibilidad de teclado): sigue activo.

### Nota sobre `distsByFibra` — orden garantizado

`GetDistributionsByFibrasAsync` retorna las distribuciones ordenadas por `PaymentDate DESC`. El `GroupBy + ToDictionary` en el endpoint preserva ese orden dentro de cada lista, por lo que `dists.Take(4)` siempre devuelve las 4 más recientes. Documentar en Dev Agent Record al implementar.

### Convenciones obligatorias

- `—` para valores null financieros — nunca `0`, nunca `undefined`
- Imports con alias `@/` — nunca rutas relativas `../../`
- `noUnusedLocals: true` — cada import declarado DEBE usarse
- EF Core: llamadas al mismo DbContext son secuenciales (`await` one by one)
- El badge NO tiene color de fondo en modo dark con CSS mal definido → usar clases semánticas de Tailwind v4 (`bg-green-100 text-green-800`) y verificar que el diseño system no sobreescribe

### Verificar AppDbContext antes de implementar

```bash
grep -n "DailySnapshot\|DbSet" src/Server/Infrastructure/Persistence/SqlServer/AppDbContext.cs
```

### Referencias

- [FR-27: Filas expandibles con 4 secciones](../../planning-artifacts/epics.md#fr-27)
- [FR-28: Badge señal NAV vs Precio](../../planning-artifacts/epics.md#fr-28)
- [UX-DR3: Filas expandibles mobile](../../planning-artifacts/ux-design-specification.md)
- [Historia 6.2 — KPIs y tabla base](6-2-kpis-del-portafolio-y-tabla-con-multi-sort-y-columnas-configurables.md)
- [Historia 6.4 — Edición inline](../../planning-artifacts/epics.md#historia-64) — siguiente historia del mismo módulo
- [Convenciones FIBRADIS](../../planning-artifacts/convenciones-fibradis.md)
- [AGENTS.md — reglas críticas del proyecto](../../../AGENTS.md)

## Senior Developer Review (AI)

### Review Findings

**Patches:**
- [x] [Review][Patch] P1: calcSignal guard frágil — price negativo pasa el guard y produce señal verde falsa; NaN validado implícitamente con `!nav` es frágil. Agregar `|| price <= 0` y usar `Number.isFinite` [signal-badge.ts:15]
- [x] [Review][Patch] P2: `week52Avg == 0` confunde "sin datos" con "promedio cero" — `TryGetValue` devuelve `0` cuando la clave no existe; usar `TryGetValue` directamente para la asignación nullable [PortfolioEndpoints.cs:80,112]
- [x] [Review][Patch] P3: discount=0 en badge amarillo produce tooltip "-0.0%" — agregar guard para el caso exacto `discount === 0` [signal-badge.ts:30-34]
- [x] [Review][Patch] P4: `days = 365` default duplicado en interfaz e implementación C# — eliminar el default de la implementación, conservarlo solo en la interfaz [IMarketRepository.cs:14 / MarketRepository.cs:103]
- [x] [Review][Patch] P5: `toNumberOrNull('')` devuelve `0` en vez de `null` — `Number('') === 0` pasa `Number.isFinite`, producir `MX$0.00` en vez de `—` [portfolio-format.ts:3]
- [x] [Review][Patch] P6: Orden dentro de grupos en `GetDistributionsByFibrasAsync` depende de estabilidad implícita de LINQ `GroupBy` — hacer el `OrderByDescending(PaymentDate)` explícito a nivel de grupo en el endpoint [PortfolioEndpoints.cs:57-59]
- [x] [Review][Patch] P7: key de distribuciones no única si una fibra tiene dos distribuciones con la misma fecha y monto — agregar índice como tiebreaker [PositionExpandedDetail.tsx:108]

**Defers:**
- [x] [Review][Defer] D1: `GetUserId` lanza `FormatException`/`ArgumentNullException` si el claim está ausente o malformado — produce 500 en lugar de 401 [PortfolioEndpoints.cs:207-208] — deferred, pre-existente de 6-1/6-2
- [x] [Review][Defer] D2: `GetDistributionsByFibrasAsync` trae todas las distribuciones del año y `Take(4)` filtra en memoria — aceptable para portafolios pequeños de FIBRAs [PortfolioEndpoints.cs:56-86] — deferred, diseño aceptable
- [x] [Review][Defer] D3: `totalCols = 11` es un magic number sin relación explícita con el thead — frágil ante cambios de columnas fijas [PositionsTable.tsx:128] — deferred, bajo riesgo
- [x] [Review][Defer] D4: `declaredYield` multiplica ×4 asumiendo cadencia trimestral para todas las FIBRAs — incorrecto para fibras mensuales [PositionExpandedDetail.tsx:55-58] — deferred, limitación del spec; requiere campo de frecuencia de distribución
- [x] [Review][Defer] D5: `distribuciones.ts` etiqueta el período del pago, no el período económico al que corresponde — requiere heurística por cadencia o config por FIBRA [distribuciones.ts:60-77] — deferred, requiere config por FIBRA
- [x] [Review][Defer] D6: `GetLatestSnapshotPerFibraAsync` carga todos los snapshots y filtra en memoria en el endpoint — no se pasan `fibraIds` a la query [PortfolioEndpoints.cs:51-54] — deferred, pre-existente de 6-1/6-2
- [x] [Review][Defer] D7: `NormalizeColumns` whitelist no incluye `week52Low`, `week52Avg`, `volume` aunque están en el DTO [PortfolioEndpoints.cs:232-240] — deferred, pre-existente de 6-1/6-2
- [x] [Review][Defer] D8: `PortfolioDistributionDto.PaymentDate` como `string` fuerza formato `yyyy-MM-dd` en UI sin localización [PortfolioResponseDto.cs:14-17] — deferred, decisión de diseño de 6-1/6-2

## Dev Agent Record

### Debug Log
- 2026-06-03: Extendí `PortfolioPositionDto` con `Volume`, `Week52Low`, `Week52Avg`, `FundamentalsPeriod` y `RecentDistributions`, además de agregar `PortfolioDistributionDto`.
- 2026-06-03: Implementé `GetWeek52AvgByFibrasAsync` en `IMarketRepository` y `MarketRepository`, y actualicé `PortfolioEndpoints` para poblar el payload expandido del portafolio.
- 2026-06-03: Regeneré `src/Web/SharedApiClient/schema.d.ts` con `npm run codegen:api` para exponer los nuevos contratos al Main SPA.
- 2026-06-03: Añadí `portfolio-format.ts`, `signal-badge.ts`, `SignalBadge.tsx`, `PositionExpandedDetail.tsx` y actualicé `PositionsTable.tsx`, `PortafolioPage.tsx` y `KpiCards.tsx` para la vista expandible.
- 2026-06-03: Creé `SignalBadge.test.ts` y `MarketRepositoryWeek52AvgTests.cs`; también ajusté los fakes de jobs a la nueva interfaz de `IMarketRepository`.
- 2026-06-03: Durante la validación corregí `getDistributionPeriodLabel` para que use el periodo real de la distribución y no el periodo anterior.

### Completion Notes
- Implementé las filas expandibles del portafolio con cuatro secciones: Mi posición, Mercado, Fundamentales y Distribuciones.
- Agregué el badge de señal NAV con estados verde/amarillo/rojo/gris y tooltip explicativo, con lógica pura separada para tests.
- Extendí el contrato backend y el cliente OpenAPI para exponer `Volume`, `Week52Low`, `Week52Avg`, `FundamentalsPeriod` y `RecentDistributions`.
- Reemplacé el estado local de `enabledColumns` por datos derivados del query cache del backend.
- Tests ejecutados:
  - `dotnet test .\\tests\\Unit\\Infrastructure.Tests\\ --filter "MarketRepositoryWeek52Avg" --configuration Release` → `4/4` passing
  - `dotnet test .\\tests\\Unit\\Infrastructure.Tests\\ --configuration Release` → `214/214` passing
  - `npm test --workspace=src/Web/Main` → `66/66` passing
  - `dotnet build .\\FIBRADIS.slnx --configuration Release` → `0` errors
  - `npm run build --workspace=src/Web/Main` → `0` errors; Vite emitted only a chunk-size warning

## File List

- `src/Server/SharedApiContracts/Portfolio/PortfolioResponseDto.cs`
- `src/Server/Application/Market/IMarketRepository.cs`
- `src/Server/Infrastructure/Persistence/Repositories/Market/MarketRepository.cs`
- `src/Server/Api/Endpoints/Private/PortfolioEndpoints.cs`
- `scripts/codegen/Api.json`
- `src/Web/SharedApiClient/schema.d.ts`
- `src/Web/Main/package.json`
- `src/Web/Main/src/modules/portafolio/portfolio-format.ts`
- `src/Web/Main/src/modules/portafolio/signal-badge.ts`
- `src/Web/Main/src/modules/portafolio/SignalBadge.tsx`
- `src/Web/Main/src/modules/portafolio/SignalBadge.test.ts`
- `src/Web/Main/src/modules/portafolio/PositionExpandedDetail.tsx`
- `src/Web/Main/src/modules/portafolio/PositionsTable.tsx`
- `src/Web/Main/src/modules/portafolio/PortafolioPage.tsx`
- `src/Web/Main/src/modules/portafolio/KpiCards.tsx`
- `src/Web/Main/src/modules/ficha-publica/sections/distribuciones.ts`
- `tests/Unit/Infrastructure.Tests/Market/MarketRepositoryWeek52AvgTests.cs`
- `tests/Unit/Infrastructure.Tests/Jobs/Market/DailySnapshotHistoricalJobTests.cs`
- `tests/Unit/Infrastructure.Tests/Jobs/Market/DistributionPipelineJobTests.cs`
- `tests/Unit/Infrastructure.Tests/Jobs/Market/MarketPipelineJobTests.cs`

## Change Log

- 2026-06-03: Historia 6.3 creada — filas expandibles, badge señal NAV, extensión PortfolioPositionDto
- 2026-06-03: Historia 6.3 implementada — contrato backend extendido, endpoint `/api/v1/portfolio` enriquecido, fila expandible con badge NAV y tests unitarios/regresión verdes
- 2026-06-03: Ajuste de validación — `getDistributionPeriodLabel` ahora etiqueta el periodo real de la distribución y no el anterior
