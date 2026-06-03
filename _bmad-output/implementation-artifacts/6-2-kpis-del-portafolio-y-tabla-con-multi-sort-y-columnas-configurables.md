# Historia 6.2: KPIs del portafolio y tabla con multi-sort y columnas configurables

Status: done

## Story

Como usuario,
quiero ver los KPIs de mi portafolio (Inversión Total, Valor Total, Plusvalía, Ganancia, Rentas) y una tabla de posiciones compacta con multi-sort y columnas configurables que cargue en menos de un segundo,
para que pueda evaluar la salud de mi portafolio de un vistazo y profundizar en los datos que más me importan.

## Acceptance Criteria

### AC1 — KPIs correctos con precios disponibles

**Dado que** mi portafolio tiene 3 posiciones y todas tienen precios de mercado actuales,
**Cuando** carga la página `/portafolio`,
**Entonces** el encabezado de KPIs muestra valores correctos:
- Inversión Total = sum(costo_total_compra)
- Valor Total = sum(titulos × precio_mercado)
- Plusvalía % = (Valor Total - Inversión Total) / Inversión Total × 100
- Ganancia Total $ = Valor Total - Inversión Total
- Rentas Anuales Brutas = sum(titulos_i × distribuciones_TTM_i por FIBRA)
- Rentas Reales Brutas = mismo cálculo (rentas pagadas en los últimos 365 días)
- % Rentas del Portafolio = Rentas Anuales Brutas / Inversión Total × 100

### AC2 — Tiempo de respuesta P95 < 1 segundo

**Dado que** la página carga con datos precalculados en el backend,
**Entonces** el tiempo de respuesta P95 del endpoint `GET /api/v1/portfolio` es inferior a 1 segundo.

### AC3 — Multi-sort funcional

**Dado que** hago clic en el encabezado de la columna Plusvalía una vez,
**Entonces** la tabla ordena por Plusvalía descendente (primer clic = desc).

**Dado que** luego hago clic en el encabezado de Ganancia mientras sostengo Shift,
**Entonces** la tabla ordena por Plusvalía como criterio primario y Ganancia como secundario.

**Dado que** hago clic en el mismo encabezado sin Shift,
**Entonces** se limpia el sort anterior y se aplica el nuevo criterio solo.

### AC4 — Columnas configurables con persistencia

**Dado que** abro el panel de configuración de columnas y activo la columna "Cap Rate",
**Entonces** la columna aparece en la tabla inmediatamente.

**Dado que** recargo la página (o me deslogueo y vuelvo a entrar),
**Entonces** la columna "Cap Rate" sigue activa — la config se persiste por usuario en el servidor.

### AC5 — Posición sin precio de mercado muestra `—`

**Dado que** una posición no tiene precio de mercado actual,
**Entonces** las columnas Precio Actual, Valor de Mercado, Plusvalía % y Ganancia $ muestran `—` para esa fila.
**Y** los KPIs que incluyen esa posición muestran anotación `(parcial)` junto al valor.

### AC6 — No existe ruta `/dashboard`

**Y** no existe la ruta `/dashboard` — toda la funcionalidad del portafolio está bajo `/portafolio`.

## Tasks / Subtasks

### T1 — Dominio: entidad `UserPortfolioSettings` (AC4)

- [x] T1.1 — Crear `src/Server/Domain/Portfolio/UserPortfolioSettings.cs`:
  - Campos: `UserId` (Guid, PK), `ColumnConfigJson` (string?), `UpdatedAt` (DateTimeOffset)
  - Representa la configuración de columnas opcionales activas del portafolio del usuario

### T2 — Infraestructura: configuración EF + migración (AC4)

- [x] T2.1 — Crear `src/Server/Infrastructure/Persistence/SqlServer/Configurations/Portfolio/UserPortfolioSettingsConfiguration.cs`:
  - Schema: `portfolio`, tabla: `UserPortfolioSettings`
  - Columnas: `user_id` (PK), `column_config_json` (text, nullable), `updated_at`
  - FK a `auth.Users(id)` con `DeleteBehavior.Cascade`
- [x] T2.2 — Agregar `DbSet<UserPortfolioSettings> UserPortfolioSettings` en `AppDbContext.cs`
- [x] T2.3 — Generar y aplicar migración EF:
  ```bash
  dotnet ef migrations add AddUserPortfolioSettings \
    --project src/Server/Infrastructure \
    --startup-project src/Server/Api
  ```

### T3 — Application: extender `IPortfolioRepository` (AC1, AC4)

- [x] T3.1 — Agregar a `src/Server/Application/Portfolio/IPortfolioRepository.cs`:
  ```csharp
  Task<UserPortfolioSettings?> GetSettingsAsync(Guid userId, CancellationToken ct);
  Task UpsertSettingsAsync(Guid userId, string? columnConfigJson, CancellationToken ct);
  ```
- [x] T3.2 — Implementar en `src/Server/Infrastructure/Persistence/Repositories/Portfolio/PortfolioRepository.cs`:
  - `GetSettingsAsync`: `FirstOrDefaultAsync(s => s.UserId == userId, ct)` sobre `db.UserPortfolioSettings`
  - `UpsertSettingsAsync`: UPSERT atómico — `ExecuteUpdateAsync` si existe, `Add` si no existe

### T4 — Application: método batch en `IMarketRepository` (AC1)

- [x] T4.1 — Agregar a `src/Server/Application/Market/IMarketRepository.cs`:
  ```csharp
  Task<IReadOnlyList<Distribution>> GetDistributionsByFibrasAsync(
      IReadOnlyList<Guid> fibraIds, int days, CancellationToken ct = default);
  ```
- [x] T4.2 — Implementar en `src/Server/Infrastructure/Persistence/Repositories/Market/MarketRepository.cs`:
  ```csharp
  public async Task<IReadOnlyList<Distribution>> GetDistributionsByFibrasAsync(
      IReadOnlyList<Guid> fibraIds, int days, CancellationToken ct = default)
  {
      var cutoff = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-days);
      return await db.Distributions
          .Where(d => fibraIds.Contains(d.FibraId) && d.PaymentDate >= cutoff)
          .OrderByDescending(d => d.PaymentDate)
          .ToListAsync(ct);
  }
  ```

### T5 — Application: calculadora de KPIs (AC1, AC2, AC5)

- [x] T5.1 — Crear `src/Server/Application/Portfolio/PortfolioKpiCalculator.cs`:
  - Clase estática con método:
    ```csharp
    PortfolioKpiResult Calculate(
        IReadOnlyList<PortfolioPosition> positions,
        IReadOnlyDictionary<Guid, PriceSnapshot> snapshotByFibra,
        IReadOnlyDictionary<Guid, IReadOnlyList<Distribution>> distsByFibra,
        IReadOnlyDictionary<Guid, Fibra> fibraById)
    ```
  - Returns `PortfolioKpiResult` con campos de KPIs agregados + lista de `PortfolioPositionRow`
  - Ver Dev Notes para la lógica exacta de cálculo

- [x] T5.2 — Crear `src/Server/Application/Portfolio/PortfolioKpiResult.cs`:
  ```csharp
  public record PortfolioKpiResult(
      decimal InversionTotal,
      decimal? ValorTotal,
      decimal? PlusvaliaTotal_Pct,
      decimal? PlusvaliaTotal_Mxn,
      decimal RentasAnualesBrutas,
      decimal PctRentasPortafolio,
      bool IsPartial,
      IReadOnlyList<PortfolioPositionRow> Positions);
  
  public record PortfolioPositionRow(
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
      string? FreshnessStatus);
  ```

### T6 — SharedApiContracts: DTOs del portafolio (AC1, AC5)

- [x] T6.1 — Crear `src/Server/SharedApiContracts/Portfolio/PortfolioResponseDto.cs`:
  ```csharp
  public record PortfolioKpisDto(
      decimal InversionTotal,
      decimal? ValorTotal,
      decimal? PlusvaliaTotal_Pct,
      decimal? PlusvaliaTotal_Mxn,
      decimal RentasAnualesBrutas,
      decimal PctRentasPortafolio,
      bool IsPartial);
  
  public record PortfolioPositionDto(
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
      string? FreshnessStatus);
  
  public record PortfolioResponseDto(
      PortfolioKpisDto? Kpis,
      IReadOnlyList<PortfolioPositionDto> Positions);
  
  public record PortfolioColumnConfigDto(IReadOnlyList<string> Columns);
  ```

### T7 — API: nuevos endpoints portfolio (AC1, AC4)

- [x] T7.1 — Agregar a `src/Server/Api/Endpoints/Private/PortfolioEndpoints.cs`:

  **`GET /api/v1/portfolio`** — retorna portafolio enriquecido:
  - Lee posiciones: `portfolioRepo.GetByUserIdAsync(userId, ct)`
  - Si no hay posiciones → `{ kpis: null, positions: [] }`
  - Lee precios: `marketRepo.GetLatestSnapshotPerFibraAsync(ct)` (filtrar por fibraIds del usuario)
  - Lee distribuciones (batch): `marketRepo.GetDistributionsByFibrasAsync(fibraIds, 365, ct)`
  - Lee catálogo: `fibraRepo.GetAllActiveAsync(ct)` (filtrar por fibraIds)
  - Calcula con `PortfolioKpiCalculator.Calculate(...)`
  - Retorna `PortfolioResponseDto`
  - **CRÍTICO**: todas las calls son secuenciales (misma instancia DbContext, ver convenciones)

  **`GET /api/v1/portfolio/column-config`** — retorna config de columnas del usuario:
  - Lee `portfolioRepo.GetSettingsAsync(userId, ct)`
  - Si null → `{ columns: [] }` (sin columnas opcionales activas)
  - Si tiene → deserializar `ColumnConfigJson` → `{ columns: [...] }`

  **`PUT /api/v1/portfolio/column-config`** — guarda config:
  - Body: `{ columns: ["capRate", "ltv", ...] }`
  - Serializar a JSON → `portfolioRepo.UpsertSettingsAsync(userId, json, ct)`
  - Retorna `204 No Content`

- [x] T7.2 — Registrar en `ApiServiceExtensions.cs` si hay nuevos servicios (el repositorio ya está registrado)

### T8 — Frontend: KpiCards (AC1, AC5)

- [x] T8.1 — Crear `src/Web/Main/src/modules/portafolio/KpiCards.tsx`:
  - Props: `kpis: PortfolioKpisDto | null | undefined`
  - Si `kpis === null` → no renderizar nada (portafolio vacío)
  - Tarjetas con: Inversión Total, Valor Total, Plusvalía %, Ganancia $, Rentas Anuales, % Rentas
  - Si `kpis.isPartial` → mostrar `(parcial)` junto al Valor Total, Plusvalía y Ganancia
  - Formato moneda: `toLocaleString('es-MX', { style: 'currency', currency: 'MXN' })`
  - Plusvalía y Ganancia: verde si positivo, rojo si negativo (clase Tailwind `text-green-600` / `text-red-600`)

### T9 — Frontend: `PositionsTable` con multi-sort y columnas configurables (AC3, AC4, AC5)

- [x] T9.1 — Reemplazar completamente `src/Web/Main/src/modules/portafolio/PositionsTable.tsx`:
  - Props: `positions: PortfolioPositionDto[]`, `enabledColumns: string[]`
  - Multi-sort con `useMemo` y `useState<{ column: string; dir: 'asc' | 'desc' }[]>` (ver Dev Notes)
  - Columnas default (siempre visibles): Ticker/Nombre, Títulos, Costo Promedio, Precio Actual, Valor de Mercado, Plusvalía %, Ganancia $, Renta Anual, % Portafolio
  - Columnas opcionales (visibles si `enabledColumns` las incluye): `capRate`, `navPerCbfi`, `ltv`, `noiMargin`, `ffoMargin`, `dailyChangePct`, `week52High`
  - Header de columna ordenable: muestra flecha `↑` / `↓` / sin flecha según estado de sort
  - Click header: si no Shift → replace sort. Si Shift → agrega/toggle secondary sort
  - `—` para valores null (nunca `0` o `undefined` visibles como número)
  - `tabular-nums` en todas las celdas numéricas
  - Nota: las columnas opcionales de fundamentales requieren que el endpoint GET /portfolio las incluya en el DTO (ver T5); para esta historia, incluir `capRate`, `navPerCbfi`, `ltv` en `PortfolioPositionDto` aunque vengan null

- [x] T9.2 — Crear `src/Web/Main/src/modules/portafolio/ColumnPicker.tsx`:
  - Popover o panel con checkboxes agrupados por sección:
    - **Fundamentales**: Cap Rate, NAV/CBFI, LTV, Margen NOI, Margen FFO
    - **Mercado**: Cambio % diario, Máx. 52S
  - Al marcar/desmarcar → llama `PUT /api/v1/portfolio/column-config` + actualiza estado local
  - Usa componentes shadcn/ui existentes (verificar con `ls src/Web/Main/src/shared/ui/`)

### T10 — Frontend: `PortafolioPage` rediseño (AC1–AC6)

- [x] T10.1 — Reemplazar completamente `src/Web/Main/src/modules/portafolio/PortafolioPage.tsx`:
  - Auth guard: leer token de localStorage → si no hay, `<Navigate to="/login" />`
  - Query principal: `useQuery(['portfolio', 'positions'], () => apiClient.GET('/api/v1/portfolio', {}))`
  - Query column-config: `useQuery(['portfolio', 'column-config'], () => apiClient.GET('/api/v1/portfolio/column-config', {}))`
  - Estado vacío: si `data.positions.length === 0` → mostrar `UploadZone`
  - Estado con posiciones: mostrar `KpiCards`, `ColumnPicker` + `PositionsTable`
  - La `UploadZone` sigue disponible en el estado con posiciones (para reemplazo)
  - El `onUploadSuccess` de `UploadZone` invalida `['portfolio']` y refetch

- [x] T10.2 — Eliminar el patrón `uploadedPositions` state de `PortafolioPage` (era el Defer D1 de 6.1)
  - El estado ahora viene del servidor vía `useQuery(['portfolio', 'positions'])`

### T11 — Regenerar cliente API

- [x] T11.1 — Ejecutar `npm run codegen:api` para regenerar `schema.d.ts` y `fibrasApi.ts`
  - Si el proceso API está en ejecución con DLLs bloqueados: detener antes del codegen
  - Actualizar `scripts/codegen/Api.json` con los nuevos endpoints

### T12 — Tests unitarios del calculador de KPIs (AC1, AC5)

- [x] T12.1 — Crear `tests/Unit/Infrastructure.Tests/Portfolio/PortfolioKpiCalculatorTests.cs`:
  - `Calculate_AllPositionsWithPrice_ReturnsCorrectKpis` — 2 posiciones con precio, verifica los 7 KPIs
  - `Calculate_OnePositionMissingPrice_IsPartialTrue` — una posición sin precio → `IsPartial = true`, `ValorTotal` excluye esa posición, Ganancia$ y Plusvalia% son null
  - `Calculate_WithDistributions_ReturnsCorrectRentas` — seed de distribuciones con AmountPerUnit conocido
  - `Calculate_NoPriceAnyPosition_ValorTotalNull` — todas sin precio → `ValorTotal = null`
  - `Calculate_PctPortafolio_BasedOnCostoPromedio` — verifica fórmula FR-43: `(titulos × costoPromedio) / sum(titulos_i × costoPromedio_i)`
  - `Calculate_EmptyPositions_ReturnsEmptyResult`

- [x] T12.2 — Ejecutar: `dotnet test tests/Unit/Infrastructure.Tests/ --filter "PortfolioKpi" --configuration Release`
  - Resultado esperado: 6/6 pasando

### T13 — Build verification

- [x] T13.1 — `dotnet build FIBRADIS.slnx --configuration Release` → 0 errores
- [x] T13.2 — `npm run build --workspace=src/Web/Main` → 0 errores TypeScript

## Dev Notes

### Decisión arquitectónica: KPIs calculados en backend

Del sprint-status.yaml:
> "decisión arquitectónica epic-6: cálculo NAV en backend (Opción A) — endpoint portafolio lee OperationalConfig directamente en servidor"

**Toda la aritmética de KPIs ocurre en backend.** El frontend solo formatea y presenta. Esto aplica a:
- Plusvalía %, $
- Rentas Anuales Brutas
- % Rentas del Portafolio
- % Portafolio por posición

### Fórmulas exactas de KPIs

```
InversionTotal = sum(position.CostoTotalCompra)

// Por posición:
ValorMercado_i = Titulos_i × PrecioActual_i     (null si PrecioActual_i es null)
PlusvaliaFilaMxn_i = ValorMercado_i - CostoTotalCompra_i  (null si PrecioActual_i es null)
PlusvaliaFilaPct_i = PlusvaliaFilaMxn_i / CostoTotalCompra_i × 100  (null si PrecioActual_i es null)

// TTM = trailing twelve months: suma de distribuciones de los últimos 365 días
RentaAnual_i = Titulos_i × sum(dist.AmountPerUnit for PaymentDate >= today-365 for FibraId_i)
             = null si no hay distribuciones en los últimos 365 días

// Agregados:
ValorTotal = sum(ValorMercado_i para posiciones CON precio)
             null si NINGUNA posición tiene precio
             — si hay mezcla: sum de las que tienen precio, marcado IsPartial = true

PlusvaliaTotal_Mxn = ValorTotal - InversionTotal  (null si ValorTotal es null)
PlusvaliaTotal_Pct = PlusvaliaTotal_Mxn / InversionTotal × 100  (null si ValorTotal es null)

RentasAnualesBrutas = sum(RentaAnual_i para posiciones CON distribuciones)
PctRentasPortafolio = RentasAnualesBrutas / InversionTotal × 100

IsPartial = true si al menos una posición no tiene precio de mercado

// PctPortafolio por posición — FR-43: usa costo promedio, NO costo total compra
// Base = sum(Titulos_i × CostoPromedio_i) — excluyendo commission_factor
PctPortafolio_i = (Titulos_i × CostoPromedio_i) / sum(Titulos_j × CostoPromedio_j) × 100
```

**Nota**: `RentasAnualesBrutas` y `RentasRealesBrutas` son el mismo valor en MVP (ambas = TTM de distribuciones históricas reales de Yahoo Finance). No hay distinción entre "estimado" y "real" en esta historia; el frontend puede mostrar ambos KPIs con el mismo valor.

### Endpoint `GET /api/v1/portfolio` — patrón completo

```csharp
group.MapGet("/", async (
    IPortfolioRepository portfolioRepo,
    IMarketRepository marketRepo,
    IFibraRepository fibraRepo,
    HttpContext ctx,
    CancellationToken ct) =>
{
    var userId = GetUserId(ctx);

    // 1. Posiciones del usuario
    var positions = await portfolioRepo.GetByUserIdAsync(userId, ct);
    if (positions.Count == 0)
        return Results.Ok(new PortfolioResponseDto(null, []));

    // 2. Precios de mercado (todos, filtrar en memoria — solo ~20 FIBRAs)
    var fibraIds = positions.Select(p => p.FibraId).ToList();
    var allSnapshots = await marketRepo.GetLatestSnapshotPerFibraAsync(ct);
    var snapshotByFibra = allSnapshots
        .Where(s => fibraIds.Contains(s.FibraId))
        .ToDictionary(s => s.FibraId);

    // 3. Distribuciones en batch (último año)
    var distributions = await marketRepo.GetDistributionsByFibrasAsync(fibraIds, 365, ct);
    var distsByFibra = distributions
        .GroupBy(d => d.FibraId)
        .ToDictionary(g => g.Key, g => (IReadOnlyList<Distribution>)g.ToList());

    // 4. Catálogo de fibras
    var allFibras = await fibraRepo.GetAllActiveAsync(ct);
    var fibraById = allFibras
        .Where(f => fibraIds.Contains(f.Id))
        .ToDictionary(f => f.Id);

    // 5. Calcular KPIs
    var result = PortfolioKpiCalculator.Calculate(positions, snapshotByFibra, distsByFibra, fibraById);

    // 6. Mapear a DTO
    var kpisDto = new PortfolioKpisDto(...);
    var positionDtos = result.Positions.Select(p => new PortfolioPositionDto(...)).ToList();
    return Results.Ok(new PortfolioResponseDto(kpisDto, positionDtos));
})
.Produces<PortfolioResponseDto>(StatusCodes.Status200OK)
.ProducesProblem(StatusCodes.Status401Unauthorized);
```

**CRÍTICO**: Todas las llamadas a repos son SECUENCIALES — nunca `Task.WhenAll` con el mismo DbContext scoped.

### Column config — formato JSON

```json
// GET /api/v1/portfolio/column-config response
{ "columns": ["capRate", "ltv"] }

// PUT /api/v1/portfolio/column-config body
{ "columns": ["capRate", "navPerCbfi", "ltv"] }
```

Claves válidas para columnas opcionales:
- Fundamentales: `"capRate"`, `"navPerCbfi"`, `"ltv"`, `"noiMargin"`, `"ffoMargin"`
- Mercado: `"dailyChangePct"`, `"week52High"`

**UPSERT de settings — patrón recomendado:**
```csharp
public async Task UpsertSettingsAsync(Guid userId, string? columnConfigJson, CancellationToken ct)
{
    var existing = await db.UserPortfolioSettings
        .FirstOrDefaultAsync(s => s.UserId == userId, ct);
    
    if (existing is null)
    {
        db.UserPortfolioSettings.Add(new UserPortfolioSettings
        {
            UserId = userId,
            ColumnConfigJson = columnConfigJson,
            UpdatedAt = DateTimeOffset.UtcNow
        });
    }
    else
    {
        existing.ColumnConfigJson = columnConfigJson;
        existing.UpdatedAt = DateTimeOffset.UtcNow;
    }
    await db.SaveChangesAsync(ct);
}
```

### Multi-sort en frontend — patrón sin TanStack Table

`@tanstack/react-table` NO está instalado. Implementar con estado local:

```typescript
type SortEntry = { column: string; dir: 'asc' | 'desc' }

const [sortKeys, setSortKeys] = useState<SortEntry[]>([])

function handleHeaderClick(column: string, e: React.MouseEvent) {
  if (e.shiftKey) {
    // Agregar/toggle secondary sort
    setSortKeys(prev => {
      const existing = prev.find(s => s.column === column)
      if (existing) {
        return prev.map(s =>
          s.column === column ? { ...s, dir: s.dir === 'asc' ? 'desc' : 'asc' } : s
        )
      }
      return [...prev, { column, dir: 'desc' }]
    })
  } else {
    // Replace sort
    setSortKeys(prev => {
      const existing = prev.find(s => s.column === column)
      const newDir: 'asc' | 'desc' = existing?.dir === 'desc' ? 'asc' : 'desc'
      return [{ column, dir: newDir }]
    })
  }
}

// Aplicar sort con useMemo
const sortedPositions = useMemo(() => {
  if (sortKeys.length === 0) return positions
  return [...positions].sort((a, b) => {
    for (const { column, dir } of sortKeys) {
      const av = (a as Record<string, unknown>)[column] ?? -Infinity
      const bv = (b as Record<string, unknown>)[column] ?? -Infinity
      if (av < bv) return dir === 'asc' ? -1 : 1
      if (av > bv) return dir === 'asc' ? 1 : -1
    }
    return 0
  })
}, [positions, sortKeys])
```

**Comportamiento de null en sort**: columnas con valor `null` van al final (tratadas como `-Infinity` en la comparación).

### Auth guard en `/portafolio`

El Defer D3 de historia 6.1 se resuelve aquí. La ruta `/portafolio` necesita verificar que hay un token JWT antes de cargar:

```typescript
// Al inicio de PortafolioPage:
const token = localStorage.getItem('access_token')  // o la key que use el proyecto
if (!token) return <Navigate to="/login" replace />
```

**Verificar la key exacta de localStorage antes de implementar:**
```bash
grep -r "localStorage" src/Web/Main/src/modules/ --include="*.tsx" -l
```
La lógica de login probablemente está en `src/Web/Main/src/modules/auth/` o similar. Buscar cómo se guarda el token para usar la misma key.

### PortfolioPositionDto — columnas opcionales de fundamentales

Las columnas opcionales de fundamentales (Cap Rate, NAV, LTV) requieren datos del módulo Fundamentals. **Para esta historia**, el endpoint `GET /api/v1/portfolio` incluirá en el `PortfolioPositionDto` los campos de fundamentales del último período procesado:
- `capRate: decimal?`
- `navPerCbfi: decimal?`
- `ltv: decimal?`
- `noiMargin: decimal?`
- `ffoMargin: decimal?`

Esto requiere una consulta adicional al módulo Fundamentals en el endpoint:
```csharp
// Después de obtener las posiciones y su catálogo:
var latestFundamentals = await fundamentalRepo.GetSummaryLatestAsync(ct);
var fundByFibraId = latestFundamentals
    .Where(r => fibraIds.Contains(r.Record.FibraId))
    .ToDictionary(r => r.Record.FibraId, r => r.Record);
```

Luego incluir estos datos en el `PortfolioPositionDto`.

**NOTA**: Esta es una lectura cruzada de módulos (Portfolio lee de Fundamentals), lo que es aceptable porque es solo lectura vía contrato Application. El módulo Portfolio no escribe en el schema Fundamentals.

### PositionsTable — columnas opcionales en el DTO

El `PortfolioPositionDto` debe incluir los campos opcionales aunque vengan `null` para el usuario que no tiene fundamentales:

```typescript
// Columna "Cap Rate" visible si enabledColumns.includes("capRate")
{enabledColumns.includes('capRate') && (
  <th className="...">Cap Rate</th>
)}
{enabledColumns.includes('capRate') && (
  <td className="...">{fmt(p.capRate, 'pct')}</td>
)}
```

### Componentes shadcn disponibles

Antes de crear componentes propios, verificar qué está en `src/Web/Main/src/shared/ui/`:
```bash
ls src/Web/Main/src/shared/ui/
```
Los componentes `Dialog`, `Button` ya están disponibles (usados en `UploadZone`). Para el `ColumnPicker`, verificar si `Popover` o `Sheet` están disponibles; si no, implementar como un panel colapsable simple.

### Deuda resuelta de historia 6.1

- **D1** (PositionsTable nunca se popula): resuelto — PortafolioPage ahora usa `GET /api/v1/portfolio`
- **D3** (sin auth guard en /portafolio): resuelto en T10.1

Deferred que continúa:
- **D4** (ordenamiento sin significado en GetByUserIdAsync): aceptable, el frontend reordena

### Convenciones obligatorias

- `—` (em dash, no guión) para valores null financieros — nunca `0`, nunca `undefined`
- Imports con alias `@/` — nunca rutas relativas `../../`
- `react-router` v7: `import { Navigate } from 'react-router'`
- `noUnusedLocals: true` — cada import declarado DEBE usarse
- EF Core: todos los calls al mismo DbContext son secuenciales (`await` one by one)
- Proyección completa en queries JOIN — documentar columnas esperadas en Dev Notes del Dev Agent Record

### Tests — qué verificar en `PortfolioKpiCalculatorTests.cs`

```csharp
// Ejemplo de setup para el test de KPIs correctos
var fibra1 = new Fibra { Id = Guid.NewGuid(), Ticker = "FUNO11", ShortName = "Fibra Uno" };
var pos1 = new PortfolioPosition
{
    FibraId = fibra1.Id, UserId = userId,
    Titulos = 800, CostoPromedio = 46.25m, CostoTotalCompra = 37278.25m
};
var snap1 = new PriceSnapshot { FibraId = fibra1.Id, LastPrice = 50.00m };
var dist1 = new Distribution { FibraId = fibra1.Id, AmountPerUnit = 1.00m, PaymentDate = today.AddDays(-30) };
var dist2 = new Distribution { FibraId = fibra1.Id, AmountPerUnit = 1.00m, PaymentDate = today.AddDays(-120) };
var dist3 = new Distribution { FibraId = fibra1.Id, AmountPerUnit = 1.00m, PaymentDate = today.AddDays(-210) };
var dist4 = new Distribution { FibraId = fibra1.Id, AmountPerUnit = 1.00m, PaymentDate = today.AddDays(-300) };
// RentaAnual esperado: 800 × 4.00 = 3,200
// ValorMercado: 800 × 50 = 40,000
// PlusvaliaFilaMxn: 40,000 - 37,278.25 = 2,721.75
```

### Referencias

- [Épica 6: Historia 6.2](../../planning-artifacts/epics.md#historia-62)
- [Story 6.1 — módulo Portfolio desde cero](6-1-carga-y-validacion-del-portafolio.md)
- [Convenciones FIBRADIS](../../planning-artifacts/convenciones-fibradis.md)
- [AGENTS.md — reglas críticas del proyecto](../../../AGENTS.md)
- [FR-26, FR-43, FR-44, FR-48](../../planning-artifacts/epics.md)

## Senior Developer Review (AI)

### Review Findings

- [x] [Review][Patch] P1: `FreshnessStatus` en `PortfolioPositionRow` es dead code — el calculator lo computa (`snapshot.Status.ToString().ToLowerInvariant()`) pero el endpoint lo ignora completamente y usa `FreshnessClassifier.Classify(snapshot, isMarketOpen, utcNow)`. Eliminar `FreshnessStatus` de `PortfolioPositionRow` (PortfolioKpiResult.cs) y del calculator (PortfolioKpiCalculator.cs:87). El endpoint ya lo popula correctamente vía Freshness Classifier. [src/Server/Application/Portfolio/PortfolioKpiResult.cs / src/Server/Application/Portfolio/PortfolioKpiCalculator.cs:87]
- [x] [Review][Patch] P2: Floating promise en `handleConfirm` — `doUpload()` se llama sin `void` creando una promesa no gestionada. Cambiar a `void doUpload()`. [src/Web/Main/src/modules/portafolio/UploadZone.tsx:93]
- [x] [Review][Defer] D1: `PlusvaliaTotal_Mxn`/`PlusvaliaTotal_Pct` son null cuando IsPartial=true — El spec dice `PlusvaliaTotal_Mxn = ValorTotal - InversionTotal (null si ValorTotal es null)`, y en el caso parcial ValorTotal no es null. Sin embargo, mostrar la diferencia (ValorTotal_parcial - InversionTotal_total) produciría un número engañoso (pérdida aparente por posiciones sin precio). Decisión consciente de mostrar `—` + badge `(parcial)`. [src/Server/Application/Portfolio/PortfolioKpiCalculator.cs:97-103] — deferred, pre-existing
- [x] [Review][Defer] D2: `enabledColumns` sincronizado vía `useEffect` — flash de columnas vacías al cargar y race condition si columnConfigQuery refetchea mientras el PUT de ColumnPicker está en vuelo. Considerar derivar de `columnConfigQuery.data?.columns ?? []` directamente. [src/Web/Main/src/modules/portafolio/PortafolioPage.tsx:56-58] — deferred, pre-existing
- [x] [Review][Defer] D3: `getStoredAccessToken()` intenta 3 keys de localStorage sin verificar contra el módulo auth real — si ninguna coincide, redirige a login aunque el usuario esté autenticado. [src/Web/Main/src/modules/portafolio/PortafolioPage.tsx:16-28] — deferred, pre-existing
- [x] [Review][Defer] D4: Error del PUT de `ColumnPicker` no visible si el Popover está cerrado — el mensaje de error se renderiza dentro del Popover que ya puede estar cerrado cuando el API falla. [src/Web/Main/src/modules/portafolio/ColumnPicker.tsx:58-61] — deferred, pre-existing
- [x] [Review][Defer] D5: Drop zone sin accesibilidad de teclado — el `div` exterior de UploadZone no tiene `tabIndex` ni `onKeyDown`, incumple WCAG 2.1 AA para el área de drag-and-drop. El `<input>` interior sí es accesible. [src/Web/Main/src/modules/portafolio/UploadZone.tsx:102-127] — deferred, pre-existing

## Dev Agent Record

### Debug Log
- Implementé el endpoint `GET /api/v1/portfolio` con cálculo backend secuencial sobre posiciones, precios, distribuciones y catálogo.
- Agregué persistencia por usuario para la configuración de columnas con `UserPortfolioSettings`, migración EF y endpoints `GET/PUT /api/v1/portfolio/column-config`.
- Rediseñé `/portafolio` con guard de autenticación, tarjetas de KPI, selector de columnas y tabla con multi-sort.
- Regeneré el cliente API y ajusté los contratos compartidos para exponer el nuevo payload del portafolio.
- Añadí pruebas unitarias para `PortfolioKpiCalculator` y actualicé los fakes de los jobs de mercado por la nueva firma batch.

### Completion Notes
- `GET /api/v1/portfolio` devuelve KPIs y filas enriquecidas; las posiciones sin precio quedan con `—` y el estado parcial se refleja en los KPIs.
- La tabla soporta orden multi-criterio con Shift + clic y persiste columnas opcionales por usuario.
- El guard de `/portafolio` redirige a `/login` si no hay token en `localStorage`.
- Validaciones ejecutadas: `dotnet test tests/Unit/Infrastructure.Tests/ --configuration Release` (`210/210`), `dotnet test tests/Integration/Api.Tests/ --configuration Release` (`205/205`), `dotnet build FIBRADIS.slnx --configuration Release`, `npm run build --workspace=src/Web/Main`, `dotnet build src/Server/Infrastructure/Infrastructure.csproj --configuration Release`, `dotnet build src/Server/Api/Api.csproj --configuration Release`.

## File List
- `_bmad-output/implementation-artifacts/6-2-kpis-del-portafolio-y-tabla-con-multi-sort-y-columnas-configurables.md`
- `scripts/codegen/Api.json`
- `src/Server/Api/Endpoints/Private/PortfolioEndpoints.cs`
- `src/Server/Application/Market/IMarketRepository.cs`
- `src/Server/Application/Portfolio/IPortfolioRepository.cs`
- `src/Server/Application/Portfolio/PortfolioKpiCalculator.cs`
- `src/Server/Application/Portfolio/PortfolioKpiResult.cs`
- `src/Server/Domain/Portfolio/UserPortfolioSettings.cs`
- `src/Server/Infrastructure/Migrations/20260603161621_AddUserPortfolioSettings.cs`
- `src/Server/Infrastructure/Migrations/20260603161621_AddUserPortfolioSettings.Designer.cs`
- `src/Server/Infrastructure/Migrations/AppDbContextModelSnapshot.cs`
- `src/Server/Infrastructure/Persistence/Repositories/Market/MarketRepository.cs`
- `src/Server/Infrastructure/Persistence/Repositories/Portfolio/PortfolioRepository.cs`
- `src/Server/Infrastructure/Persistence/SqlServer/AppDbContext.cs`
- `src/Server/Infrastructure/Persistence/SqlServer/Configurations/Portfolio/UserPortfolioSettingsConfiguration.cs`
- `src/Server/SharedApiContracts/Portfolio/PortfolioResponseDto.cs`
- `src/Server/SharedApiContracts/Portfolio/PortfolioUploadResponseDto.cs`
- `src/Web/Main/src/api/fibrasApi.ts`
- `src/Web/Main/src/modules/portafolio/ColumnPicker.tsx`
- `src/Web/Main/src/modules/portafolio/KpiCards.tsx`
- `src/Web/Main/src/modules/portafolio/PortafolioPage.tsx`
- `src/Web/Main/src/modules/portafolio/PositionsTable.tsx`
- `src/Web/Main/src/modules/portafolio/UploadZone.tsx`
- `src/Web/SharedApiClient/schema.d.ts`
- `tests/Unit/Infrastructure.Tests/Jobs/Market/DailySnapshotHistoricalJobTests.cs`
- `tests/Unit/Infrastructure.Tests/Jobs/Market/DistributionPipelineJobTests.cs`
- `tests/Unit/Infrastructure.Tests/Jobs/Market/MarketPipelineJobTests.cs`
- `tests/Unit/Infrastructure.Tests/Portfolio/PortfolioKpiCalculatorTests.cs`

## Change Log

- 2026-06-03: Historia 6.2 creada — KPIs del portafolio y tabla con multi-sort y columnas configurables
- 2026-06-03: Implementación completada — KPIs backend, persistencia de configuración de columnas, multi-sort frontend, codegen y pruebas verdes
