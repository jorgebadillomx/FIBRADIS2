# Historia 5.3: Gestión del Catálogo de FIBRAs desde Ops

Status: done

## Story

Como AdminOps,
quiero agregar nuevas FIBRAs, editar metadatos de FIBRAs existentes y realizar borrado suave (desactivación) de FIBRAs desde la sección de catálogo de Ops,
para que el universo activo de FIBRAs se mantenga preciso sin acceso directo a la base de datos ni redespliegue.

## Acceptance Criteria

### AC1 — Crear nueva FIBRA desde Ops

**Dado que** completo los campos requeridos para una nueva FIBRA (ticker, nombre completo, nombre corto, sector, mercado, moneda) y la envío,
**Entonces**:
- La FIBRA se agrega al catálogo con `State = Active` y `CreatedAt = DateTimeOffset.UtcNow`
- Aparece en `GET /api/v1/fibras` en el universo activo
- Está disponible inmediatamente para la ingesta del pipeline de mercado y la asociación de noticias
- La acción queda auditada con actor y timestamp

### AC2 — Editar metadatos de FIBRA existente

**Dado que** edito cualquier campo editable de FUNO11 (nombre completo, nombre corto, sector, mercado, moneda, SiteUrl, InvestorUrl, ReportsUrl, YahooTicker, variantes de nombre) y guardo,
**Entonces**:
- Los metadatos se actualizan en la base de datos
- El siguiente ciclo del pipeline de noticias usa las variantes de nombre actualizadas en sus queries RSS
- La respuesta del endpoint devuelve los datos actualizados

### AC3 — Desactivar FIBRA (soft delete)

**Dado que** desactivo DANHOS13 desde la pantalla de catálogo en Ops,
**Entonces**:
- `DANHOS13.State = Inactive`
- DANHOS13 queda excluida de `GET /api/v1/fibras` (que filtra por `State = Active`) y por tanto del pipeline de mercado y las queries de noticias
- Todos sus datos históricos (precios, fundamentales, noticias) siguen siendo accesibles vía URL directa `GET /api/v1/fibras/DANHOS13`
- La acción queda auditada

### AC4 — Validación de ticker duplicado

**Dado que** intento crear una nueva FIBRA con un ticker que ya existe en el catálogo,
**Entonces**:
- El endpoint retorna `409 Conflict` con `domainCode = "TICKER_ALREADY_EXISTS"` y `detail = "El ticker ya existe en el catálogo."`
- No se crea ningún registro

### AC5 — Validación de campos requeridos

**Dado que** envío un payload de creación con campos faltantes o inválidos (ticker vacío, ticker > 20 chars, moneda no reconocida, etc.),
**Entonces**:
- El endpoint retorna `400 Bad Request` con detalles de validación

### AC6 — Endpoints protegidos AdminOps

**Dado que** intento llamar a cualquier endpoint de catálogo Ops sin token o con rol `User`,
**Entonces** recibo `401` o `403` respectivamente.

### AC7 — Sin regresiones en catálogo público

Todos los tests existentes del catálogo público pasan tras los cambios.

---

## Tasks / Subtasks

### Backend — Repositorio

- [x] **T1: Extender `IFibraRepository` con métodos de escritura**
  - [x] T1.1 En `src/Server/Application/Catalog/IFibraRepository.cs`, agregar:
    ```csharp
    Task AddAsync(Domain.Catalog.Fibra fibra, CancellationToken ct = default);
    Task UpdateAsync(Domain.Catalog.Fibra fibra, CancellationToken ct = default);
    Task<bool> ExistsByTickerAsync(string ticker, CancellationToken ct = default);
    ```

- [x] **T2: Implementar métodos en `FibraRepository`**
  - [x] T2.1 En `src/Server/Infrastructure/Persistence/Repositories/Catalog/FibraRepository.cs`:
    ```csharp
    public async Task AddAsync(Fibra fibra, CancellationToken ct = default)
    {
        db.Fibras.Add(fibra);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Fibra fibra, CancellationToken ct = default)
    {
        db.Fibras.Update(fibra);
        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> ExistsByTickerAsync(string ticker, CancellationToken ct = default)
        => await db.Fibras.AnyAsync(f => f.Ticker == ticker.ToUpper(), ct);
    ```

  - [x] T2.2 Actualizar fake repos en tests unitarios (ver Dev Notes — Fake Repos):
    - `FakeFibraRepository` en `MarketPipelineJobTests.cs`
    - `FakeDistFibraRepository` en `DistributionPipelineJobTests.cs`
    - `FakeNewsFibraRepository` en `NewsPipelineJobTests.cs`
    - `FakeHistoricalFibraRepository` en `DailySnapshotHistoricalJobTests.cs`
    - Agregar implementaciones stub de los 3 métodos nuevos en cada uno

### Backend — SharedApiContracts

- [x] **T3: DTOs de Ops para catálogo**
  - [x] T3.1 Crear `src/Server/SharedApiContracts/Catalog/CreateFibraRequest.cs`:
    ```csharp
    public sealed record CreateFibraRequest(
        string Ticker,          // requerido, max 20, ToUpper en handler
        string YahooTicker,     // requerido, max 32
        string FullName,        // requerido, max 256
        string ShortName,       // requerido, max 64
        string Sector,          // requerido, max 64
        string Market,          // requerido, max 32
        string Currency,        // requerido, max 8
        string? SiteUrl,        // opcional, max 512
        string? InvestorUrl,    // opcional, max 512
        string? ReportsUrl,     // opcional, max 512
        IReadOnlyList<string>? NameVariants);  // opcional
    ```
  - [x] T3.2 Crear `src/Server/SharedApiContracts/Catalog/UpdateFibraRequest.cs`:
    ```csharp
    public sealed record UpdateFibraRequest(
        string YahooTicker,
        string FullName,
        string ShortName,
        string Sector,
        string Market,
        string Currency,
        string? SiteUrl,
        string? InvestorUrl,
        string? ReportsUrl,
        IReadOnlyList<string>? NameVariants);  // null = no modificar; lista vacía = borrar variantes
    ```
    **Nota**: el Ticker y State NO son editables vía `UpdateFibraRequest`. El Ticker es inmutable; el State se cambia con el endpoint de deactivate.

### Backend — API Endpoints

- [x] **T4: Crear `OpsCatalogEndpoints.cs`**
  - [x] T4.1 Crear `src/Server/Api/Endpoints/Ops/OpsCatalogEndpoints.cs`:

    **`GET /api/v1/ops/catalog`** (lista TODAS — activas e inactivas):
    - Llama `IFibraRepository.GetAllAsync()` (nuevo método — ver Dev Notes)
    - Retorna `IReadOnlyList<FibraDetail>` con todas las FIBRAs (incluyendo inactivas)
    - Útil para que el AdminOps vea y gestione el universo completo
    - `RequireAuthorization("AdminOps")`

    **`POST /api/v1/ops/catalog`** (AC1, AC4, AC5):
    - Recibe `CreateFibraRequest`
    - Valida campos: Ticker no vacío (max 20), YahooTicker no vacío (max 32), FullName (max 256), ShortName (max 64), Sector (max 64), Market (max 32), Currency (max 8)
    - Normaliza: `Ticker = request.Ticker.Trim().ToUpper()`, `YahooTicker = request.YahooTicker.Trim()`
    - Verifica duplicado: `await repo.ExistsByTickerAsync(ticker)` → si existe: `409 Conflict`
    - Extrae actor del JWT (patrón `GetActor(ctx)`)
    - Crea `new Fibra { Id = Guid.NewGuid(), ..., State = FibraState.Active, CreatedAt = DateTime.UtcNow }`
    - Llama `repo.AddAsync(fibra)`
    - Retorna `201 Created` con `FibraDetail` del objeto creado
    - `RequireAuthorization("AdminOps")`

    **`PUT /api/v1/ops/catalog/{ticker}`** (AC2):
    - Recibe `UpdateFibraRequest`
    - Carga fibra por ticker (incluyendo inactivas — `GetByTickerAsync` ya no filtra por estado)
    - Si no existe: `404`
    - Actualiza campos mutables en la entidad cargada: `fibra.YahooTicker = ...`, etc.
    - Si `request.NameVariants != null`: `fibra.NameVariants = request.NameVariants.ToList()`
    - Llama `repo.UpdateAsync(fibra)`
    - Retorna `200 OK` con `FibraDetail` actualizado
    - `RequireAuthorization("AdminOps")`

    **`POST /api/v1/ops/catalog/{ticker}/deactivate`** (AC3):
    - Carga fibra por ticker
    - Si no existe: `404`
    - Si ya está inactiva: `200 OK` (idempotente)
    - Establece `fibra.State = FibraState.Inactive`
    - Llama `repo.UpdateAsync(fibra)`
    - Retorna `200 OK` con `FibraDetail` actualizado
    - `RequireAuthorization("AdminOps")`

    **`POST /api/v1/ops/catalog/{ticker}/activate`** (simetría con deactivate, necesario para revertir):
    - Carga fibra por ticker
    - Si no existe: `404`
    - Si ya está activa: `200 OK` (idempotente)
    - Establece `fibra.State = FibraState.Active`
    - Llama `repo.UpdateAsync(fibra)`
    - Retorna `200 OK` con `FibraDetail` actualizado
    - `RequireAuthorization("AdminOps")`

  - [x] T4.2 Registrar en `src/Server/Api/Program.cs`:
    ```csharp
    app.MapOpsCatalog();
    ```

- [x] **T5: Extender `IFibraRepository` con `GetAllAsync`**
  - [x] T5.1 Agregar a `IFibraRepository.cs`:
    ```csharp
    Task<IReadOnlyList<Domain.Catalog.Fibra>> GetAllAsync(CancellationToken ct = default);
    ```
  - [x] T5.2 Implementar en `FibraRepository.cs`:
    ```csharp
    public async Task<IReadOnlyList<Fibra>> GetAllAsync(CancellationToken ct = default)
        => await db.Fibras.OrderBy(f => f.Ticker).ToListAsync(ct);
    ```
  - [x] T5.3 Agregar stub en cada fake repo de tests unitarios

- [x] **T6: Regenerar SharedApiClient**
  - [x] T6.1 `npm run codegen:api` — actualiza `scripts/codegen/Api.json` y `src/Web/SharedApiClient/schema.d.ts`

### Frontend — Ops SPA

- [x] **T7: API client**
  - [x] T7.1 Crear `src/Web/Ops/src/api/catalogApi.ts`:
    - Patrón idéntico a `fundamentalsApi.ts`: `createPathBasedClient<paths>({ baseUrl: '' })`
    - Usar `assertOpsAccessToken()` y `getOpsAuthHeaders()` en cada función
    - Exportar funciones:
      ```typescript
      fetchOpsCatalog(): Promise<FibraDetail[]>
      createFibra(payload: CreateFibraRequest): Promise<FibraDetail>
      updateFibra(ticker: string, payload: UpdateFibraRequest): Promise<FibraDetail>
      deactivateFibra(ticker: string): Promise<FibraDetail>
      activateFibra(ticker: string): Promise<FibraDetail>
      ```

- [x] **T8: Módulo CatalogPage**
  - [x] T8.1 Crear `src/Web/Ops/src/pages/CatalogPage.tsx`:
    - Encabezado: "Catálogo de FIBRAs"
    - Estado local: `{ mode: 'list' | 'create' | 'edit', selected: FibraDetail | null }`
    - Cuando `mode === 'list'`: renderiza `CatalogTable` + botón "Agregar FIBRA"
    - Cuando `mode === 'create'`: renderiza `FibraForm` (sin datos iniciales)
    - Cuando `mode === 'edit'`: renderiza `FibraForm` con datos de `selected` prellenados

  - [x] T8.2 Crear `src/Web/Ops/src/modules/catalog/CatalogTable.tsx`:
    - Props: `{ fibras: FibraDetail[], onEdit: (f: FibraDetail) => void, onToggleState: (f: FibraDetail) => void }`
    - Tabla con columnas: Ticker, Nombre completo, Sector, Mercado, Moneda, Estado (badge), Acciones
    - Badge: verde = "Active", gris = "Inactive"
    - Columna Acciones: botón "Editar" → `onEdit(f)`; botón "Desactivar" (si activa) o "Activar" (si inactiva) → `onToggleState(f)`
    - Usar `useMutation` para las llamadas de activar/desactivar, con `invalidateQueries(['ops-catalog'])` on success
    - Loading state en los botones de estado mientras la mutación está pendiente

  - [x] T8.3 Crear `src/Web/Ops/src/modules/catalog/FibraForm.tsx`:
    - Props: `{ initialData?: FibraDetail, onSuccess: () => void, onCancel: () => void }`
    - Si `initialData` presente: modo edición (título "Editar FIBRA"); si no: modo creación (título "Nueva FIBRA")
    - Campos:
      - Ticker: `string` — solo en modo creación (disabled en modo edición)
      - YahooTicker: `string`, requerido
      - FullName: `string`, requerido
      - ShortName: `string`, requerido
      - Sector: `string`, requerido
      - Market: `string`, requerido
      - Currency: `string`, requerido
      - SiteUrl: `string`, opcional
      - InvestorUrl: `string`, opcional
      - ReportsUrl: `string`, opcional
      - NameVariants: lista editable de strings (agregar/quitar variantes con botón "+")
    - Validación con React Hook Form (sin `@hookform/resolvers`, patrón igual que 5-2)
    - Submit → llama `createFibra` o `updateFibra` según modo
    - En éxito: muestra "✓ FIBRA guardada" y llama `onSuccess()` → vuelve a la lista
    - Error 409: muestra mensaje "El ticker ya existe en el catálogo."
    - Botón "Cancelar" → llama `onCancel()` sin guardar

- [x] **T9: Routing + navegación**
  - [x] T9.1 En `src/Web/Ops/src/main.tsx`: agregar ruta `{ path: 'catalog', element: <CatalogPage /> }`
  - [x] T9.2 En `src/Web/Ops/src/components/OpsShell.tsx`: agregar item de nav entre "Dashboard" y "AI Config":
    ```typescript
    { label: 'Catálogo', to: '/catalog', description: 'Agregar, editar y desactivar FIBRAs del universo.' },
    ```

### Tests

- [x] **T10: Unit tests backend**
  - [x] T10.1 Crear `tests/Unit/Infrastructure.Tests/Persistence/Repositories/FibraRepositoryTests.cs`:
    - `AddAsync_PersistsFibra`: crea fibra → `GetByTickerAsync` la retorna
    - `AddAsync_DuplicateTicker_Throws`: agregar dos FIBRAs con mismo ticker lanza excepción DB
    - `ExistsByTickerAsync_ExistingTicker_ReturnsTrue`
    - `ExistsByTickerAsync_NonExistentTicker_ReturnsFalse`
    - `ExistsByTickerAsync_CaseInsensitive`: "FUNO11" existe → "funo11" también retorna true
    - `UpdateAsync_UpdatesFields`: modificar `FullName` → `GetByTickerAsync` retorna nombre actualizado
    - `UpdateAsync_DeactivatesFibra`: cambiar `State = Inactive` → `GetAllActiveAsync` no la retorna
    - `GetAllAsync_ReturnsAllIncludingInactive`: incluye FIBRAs inactivas

- [x] **T11: Integration tests backend**
  - [x] T11.1 Crear `tests/Integration/Api.Tests/Ops/CatalogOpsEndpointTests.cs`:
    - `POST /ops/catalog` con payload completo y token AdminOps → `201 Created`, responde `FibraDetail` con ticker correcto
    - `POST /ops/catalog` con ticker duplicado → `409 Conflict`, `domainCode = "TICKER_ALREADY_EXISTS"`
    - `POST /ops/catalog` con campos requeridos vacíos → `400 Bad Request`
    - `POST /ops/catalog` sin token → `401`
    - `POST /ops/catalog` con rol User → `403`
    - `PUT /ops/catalog/{ticker}` actualiza nombre y variantes → `200 OK`, campo `fullName` actualizado en respuesta
    - `PUT /ops/catalog/FAKE999` → `404`
    - `POST /ops/catalog/DANHOS13/deactivate` → `200 OK`, campo `state` = "Inactive" en respuesta
    - `POST /ops/catalog/DANHOS13/deactivate` segunda vez (idempotente) → `200 OK`
    - `POST /ops/catalog/DANHOS13/activate` → `200 OK`, campo `state` = "Active"
    - `GET /ops/catalog` con token AdminOps → `200 OK`, lista incluye FIBRAs inactivas
    - `GET /ops/catalog` sin token → `401`
    - `GET /api/v1/fibras` tras desactivar DANHOS13 → lista pública NO contiene DANHOS13

---

## Dev Notes

### Prerequisito: story 5-2 en estado `done` (actualmente en `review`)

Story 5-2 está en review. Esta historia NO modifica archivos de 5-2 (schema `fundamentals`, endpoints de fundamentals). Sin embargo, sí extiende `IFibraRepository` que fue ampliada en 5-2 con `GetByIdAsync`. Verificar que el branch `story/5-3-catalogo-ops` está basado en `main` (post-merge de 5-2) antes de implementar. Si 5-2 aún no está mergeada, considerar hacer rebase del branch una vez mergeada.

### `GetByTickerAsync` ya consulta sin filtrar por State

Revisar `FibraRepository.GetByTickerAsync`:
```csharp
=> await db.Fibras.FirstOrDefaultAsync(f => f.Ticker == ticker.ToUpper(), ct);
```
No filtra por `State`, lo que es correcto: el endpoint público de ficha individual ya retorna FIBRAs inactivas por ticker directo (AC3). Los endpoints de Ops que necesitan operar sobre FIBRAs inactivas (PUT, deactivate, activate) pueden usar el mismo método.

### Extender `IFibraRepository` — impacto en fake repos

Cada vez que se agrega un método a `IFibraRepository`, los cuatro fake repos en los tests unitarios deben implementarlo. Son:
- `tests/Unit/Infrastructure.Tests/Jobs/Market/MarketPipelineJobTests.cs` — clase `FakeFibraRepository`
- `tests/Unit/Infrastructure.Tests/Jobs/Market/DistributionPipelineJobTests.cs` — clase `FakeDistFibraRepository`
- `tests/Unit/Infrastructure.Tests/Jobs/News/NewsPipelineJobTests.cs` — clase `FakeNewsFibraRepository`
- `tests/Unit/Infrastructure.Tests/Jobs/Market/DailySnapshotHistoricalJobTests.cs` — clase `FakeHistoricalFibraRepository`

Patrón de implementación stub (copiado de historia 5-2):
```csharp
public Task AddAsync(Fibra fibra, CancellationToken ct) => Task.CompletedTask;
public Task UpdateAsync(Fibra fibra, CancellationToken ct) => Task.CompletedTask;
public Task<bool> ExistsByTickerAsync(string ticker, CancellationToken ct) => Task.FromResult(false);
public Task<IReadOnlyList<Fibra>> GetAllAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<Fibra>>([]);
```

### Auditoría en MVP — logging en lugar de tabla dedicada

La historia requiere que las acciones queden "auditadas con actor y timestamp". Para MVP, la auditoría se implementa como logging estructurado en el handler (no como tabla de auditoría en BD). Ejemplo:
```csharp
logger.LogInformation("Ops {Action} FIBRA {Ticker} by {Actor} at {Timestamp}",
    "CREATE", fibra.Ticker, actor, DateTimeOffset.UtcNow);
```
**No** crear tabla de auditoría ni `IAuditRepository` en esta historia — eso es parte de la historia 5-4 (Configuración Operativa). Esta historia solo usa `ILogger` en los handlers.

Para inyectar el logger en el endpoint Minimal API:
```csharp
group.MapPost("/", async (
    CreateFibraRequest request,
    IFibraRepository repo,
    ILogger<OpsCatalogEndpoints> logger,
    HttpContext ctx,
    CancellationToken ct) => { ... })
```

### Extracción del actor en endpoints

Mismo helper `GetActor(HttpContext)` ya definido en `OpsMarketEndpoints.cs`:
```csharp
private static string GetActor(HttpContext ctx)
    => ctx.User.Identity?.Name
       ?? ctx.User.FindFirstValue(ClaimTypes.Email)
       ?? ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
       ?? "unknown";
```
Definirlo como `private static string GetActor(HttpContext ctx)` dentro de la clase `OpsCatalogEndpoints`.

### Actualizar `FibraDetail` o crear nuevo DTO

`FibraDetail` existente en `SharedApiContracts/Catalog/FibraDetail.cs` ya tiene todos los campos necesarios para la respuesta de los endpoints Ops (Id, Ticker, FullName, ShortName, Sector, Market, Currency, State, SiteUrl, InvestorUrl, ReportsUrl, NameVariants, CreatedAt). **No crear un nuevo DTO** — reusar `FibraDetail` para las respuestas de los endpoints Ops.

### Mapeo de respuesta FibraDetail

Helper privado en `OpsCatalogEndpoints.cs`:
```csharp
private static FibraDetail ToDto(Fibra f) => new(
    f.Id, f.Ticker, f.FullName, f.ShortName,
    f.Sector, f.Market, f.Currency, f.State.ToString(),
    f.SiteUrl, f.InvestorUrl, f.ReportsUrl,
    f.NameVariants.AsReadOnly(), f.CreatedAt);
```

### NameVariants — campo editable en frontend

`NameVariants` está configurado en `FibraConfiguration.cs` como JSON serializado en `nvarchar(max)`. EF Core lo deserializa automáticamente. El campo existe en la entidad como `List<string>`. **No requiere migración de BD** para hacerlo editable desde Ops — la columna ya existe.

### GET /ops/catalog — retorna FIBRAs activas + inactivas

El endpoint público `GET /api/v1/fibras` filtra por `State = Active`. El endpoint Ops `GET /api/v1/ops/catalog` debe retornar todas (activas e inactivas) para que el AdminOps pueda ver y reactivar FIBRAs desactivadas. Requiere el nuevo método `GetAllAsync` en el repositorio.

### Validación de Ticker — normalización

El ticker debe normalizarse a mayúsculas antes de la verificación de duplicados y la creación. EF Core ya aplica `ticker.ToUpper()` en `GetByTickerAsync` y `ExistsByTickerAsync`. En el handler de create:
```csharp
var ticker = request.Ticker.Trim().ToUpper();
if (await repo.ExistsByTickerAsync(ticker, ct))
    return Results.Problem(
        title: "Ticker duplicado",
        detail: "El ticker ya existe en el catálogo.",
        statusCode: 409,
        extensions: new Dictionary<string, object?> { ["domainCode"] = "TICKER_ALREADY_EXISTS" });
```

### Test de integración — setup de autenticación

Los tests de integración de Ops usan la misma factory `ApiWebFactory` y el patrón de autenticación ya establecido. Ver `FundamentalsImportTests.cs` como referencia directa del patrón más reciente:
```csharp
await factory.SeedUsersAsync();
var loginResp = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = "adminops@test.com", password = "ops123" });
var tokenJson = await loginResp.Content.ReadAsStringAsync();
using var tokenDoc = JsonDocument.Parse(tokenJson);
var token = tokenDoc.RootElement.GetProperty("accessToken").GetString()!;
client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
```

La fibra DANHOS13 ya existe en el seed de `CatalogSeed.HasData`. Para los tests de deactivate/activate, no es necesario seedear — solo llamar `await factory.SeedCatalogAsync()` para `EnsureCreatedAsync()` que dispara el `HasData`.

### `noUnusedLocals: true` en tsconfig del Ops SPA

Cada import declarado en los nuevos archivos `.tsx`/`.ts` DEBE usarse. Revisar antes de compilar.

### Variantes de nombre en FibraForm — sin dependencias adicionales

Implementar la lista editable de variantes con estado local de React (no instalar dependencias nuevas):
```typescript
const [variants, setVariants] = useState<string[]>(initialData?.nameVariants ?? [])
const addVariant = () => setVariants(v => [...v, ''])
const updateVariant = (i: number, val: string) => setVariants(v => v.map((s, idx) => idx === i ? val : s))
const removeVariant = (i: number) => setVariants(v => v.filter((_, idx) => idx !== i))
```

### Archivos a crear/modificar

**Nuevos (backend):**
- `src/Server/Api/Endpoints/Ops/OpsCatalogEndpoints.cs`
- `src/Server/SharedApiContracts/Catalog/CreateFibraRequest.cs`
- `src/Server/SharedApiContracts/Catalog/UpdateFibraRequest.cs`

**Modificados (backend):**
- `src/Server/Application/Catalog/IFibraRepository.cs` — agregar `AddAsync`, `UpdateAsync`, `ExistsByTickerAsync`, `GetAllAsync`
- `src/Server/Infrastructure/Persistence/Repositories/Catalog/FibraRepository.cs` — implementar los 4 métodos
- `src/Server/Api/Program.cs` — `app.MapOpsCatalog()`
- `scripts/codegen/Api.json` + `src/Web/SharedApiClient/schema.d.ts` — regenerar

**Nuevos (frontend Ops):**
- `src/Web/Ops/src/api/catalogApi.ts`
- `src/Web/Ops/src/modules/catalog/CatalogTable.tsx`
- `src/Web/Ops/src/modules/catalog/FibraForm.tsx`
- `src/Web/Ops/src/pages/CatalogPage.tsx`

**Modificados (frontend Ops):**
- `src/Web/Ops/src/main.tsx` — agregar ruta `/catalog`
- `src/Web/Ops/src/components/OpsShell.tsx` — agregar item de nav "Catálogo"

**Tests nuevos:**
- `tests/Unit/Infrastructure.Tests/Persistence/Repositories/FibraRepositoryTests.cs`
- `tests/Integration/Api.Tests/Ops/CatalogOpsEndpointTests.cs`

**Tests modificados (fake repos):**
- `tests/Unit/Infrastructure.Tests/Jobs/Market/MarketPipelineJobTests.cs`
- `tests/Unit/Infrastructure.Tests/Jobs/Market/DistributionPipelineJobTests.cs`
- `tests/Unit/Infrastructure.Tests/Jobs/News/NewsPipelineJobTests.cs`
- `tests/Unit/Infrastructure.Tests/Jobs/Market/DailySnapshotHistoricalJobTests.cs`

### Referencias

- `[Source: epics.md#FR-39]` — CRUD + soft delete de catálogo desde Ops
- `[Source: src/Server/Domain/Catalog/Fibra.cs]` — entidad con todos los campos editables
- `[Source: src/Server/Application/Catalog/IFibraRepository.cs]` — interfaz actual con `GetByIdAsync` añadido en 5-2
- `[Source: src/Server/Infrastructure/Persistence/Repositories/Catalog/FibraRepository.cs]` — implementación actual
- `[Source: src/Server/Api/Endpoints/Ops/AiModeEndpoints.cs]` — patrón GetActor + endpoints Ops
- `[Source: src/Server/Api/Endpoints/Ops/OpsFundamentalsEndpoints.cs]` — patrón más reciente de endpoints Ops (historia 5-2)
- `[Source: src/Server/Api/Endpoints/Public/CatalogEndpoints.cs]` — patrón endpoint público + ToDto
- `[Source: src/Server/SharedApiContracts/Catalog/FibraDetail.cs]` — DTO existente a reusar como respuesta
- `[Source: src/Server/Infrastructure/Persistence/SqlServer/Configurations/Catalog/FibraConfiguration.cs]` — name_variants ya mapeado como JSON
- `[Source: tests/Integration/Api.Tests/Fundamentals/FundamentalsImportTests.cs]` — patrón de test de integración Ops más reciente
- `[Source: _bmad-output/planning-artifacts/convenciones-fibradis.md#EF Core — nunca Task.WhenAll]` — queries secuenciales

---

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- `git checkout -b story/5-3-gestion-del-catalogo-de-fibras-desde-ops`
- `npm run codegen:api`
- `dotnet test .\tests\Unit\Infrastructure.Tests\Infrastructure.Tests.csproj --configuration Release`
- `dotnet test .\tests\Integration\Api.Tests\Api.Tests.csproj`
- `npm run build --workspace=src/Web/Ops`
- `npm run build --workspace=src/Web/Main`

### Completion Notes List

- Se agregó CRUD operativo para catálogo con endpoints AdminOps protegidos: listado completo, alta, edición, activate/deactivate idempotentes y auditoría por logging con actor + timestamp.
- `IFibraRepository` ahora soporta escritura (`AddAsync`, `UpdateAsync`, `ExistsByTickerAsync`, `GetAllAsync`) y se actualizaron todos los fake repos impactados.
- `FibraDetail` se amplió con `YahooTicker` para soportar edición completa desde Ops; se regeneró OpenAPI (`scripts/codegen/Api.json`, `src/Web/SharedApiClient/schema.d.ts`).
- Se implementó la pantalla `/catalog` en Ops con tabla, formulario create/edit, mutaciones de estado y feedback de éxito/error.
- Validaciones ejecutadas y pasando: `123/123` unit tests backend, `155/155` integration tests backend, `npm run build --workspace=src/Web/Ops`, `npm run build --workspace=src/Web/Main`.

### File List

- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `scripts/codegen/Api.json`
- `src/Server/Api/Endpoints/Ops/OpsCatalogEndpoints.cs`
- `src/Server/Api/Endpoints/Public/CatalogEndpoints.cs`
- `src/Server/Api/Program.cs`
- `src/Server/Application/Catalog/IFibraRepository.cs`
- `src/Server/Infrastructure/Persistence/Repositories/Catalog/FibraRepository.cs`
- `src/Server/SharedApiContracts/Catalog/CreateFibraRequest.cs`
- `src/Server/SharedApiContracts/Catalog/FibraDetail.cs`
- `src/Server/SharedApiContracts/Catalog/UpdateFibraRequest.cs`
- `src/Web/Ops/src/api/catalogApi.ts`
- `src/Web/Ops/src/components/OpsShell.tsx`
- `src/Web/Ops/src/main.tsx`
- `src/Web/Ops/src/modules/catalog/CatalogTable.tsx`
- `src/Web/Ops/src/modules/catalog/FibraForm.tsx`
- `src/Web/Ops/src/pages/CatalogPage.tsx`
- `src/Web/SharedApiClient/schema.d.ts`
- `tests/Integration/Api.Tests/Ops/CatalogOpsEndpointTests.cs`
- `tests/Unit/Infrastructure.Tests/Jobs/Market/DailySnapshotHistoricalJobTests.cs`
- `tests/Unit/Infrastructure.Tests/Jobs/Market/DistributionPipelineJobTests.cs`
- `tests/Unit/Infrastructure.Tests/Jobs/Market/MarketPipelineJobTests.cs`
- `tests/Unit/Infrastructure.Tests/Jobs/News/NewsPipelineJobTests.cs`
- `tests/Unit/Infrastructure.Tests/Persistence/Repositories/FibraRepositoryTests.cs`

### Change Log

- 2026-05-23: Implementado catálogo Ops 5.3 end-to-end: repositorio extensible con escritura, endpoints AdminOps CRUD + soft delete/reactivate auditados, DTOs OpenAPI regenerados, UI `/catalog` en Ops y cobertura nueva de tests unitarios/integración. Status → review.
- 2026-05-23: Code review AI — 13 patches aplicados (P1–P13), 5 findings diferidos (D1–D5). Migración `DateTimeOffset`, race-condition 409, validación moneda, seed test, normalización ticker, React key, pendingTicker stale, scheme URL, AddAsync normaliza ticker, cleanup DisposeAsync, tautología FibraForm, log idempotente, test 400 body. Fix adicional: `catalog-seed.ts` (Main SPA) con `yahooTicker` faltante. 115 unit + 155 integration tests verdes. Status → done.

---

## Senior Developer Review (AI)

### Review Findings

**Patches:**

- [x] [Review][Patch] P13 [Media]: Migrar `Fibra.CreatedAt` y `FibraDetail.CreatedAt` a `DateTimeOffset` — La Dev Note exige `DateTimeOffset.UtcNow`; el código actual usa `timestamp.UtcDateTime` perdiendo el offset. Requiere migración EF Core + actualizar seed y tests. Decisión: migrar ahora antes de que haya consumers externos en producción. [`FibraDetail.cs:13`, `OpsCatalogEndpoints.cs:79`, `ApiWebFactory.cs`]

- [x] [Review][Patch] P1 [Alta]: Race condition TOCTOU + DbUpdateException→500 — El endpoint hace `ExistsByTickerAsync` y luego `AddAsync`; entre ambas llamadas, una solicitud concurrente puede crear el mismo ticker. `AddAsync` tiene una segunda guarda que lanza `DbUpdateException`, pero el endpoint no la captura → el caller recibe 500 en lugar de 409. Fix: capturar `DbUpdateException` en el handler POST y retornar 409, o confiar únicamente en la restricción de BD. [`OpsCatalogEndpoints.cs:53`, `FibraRepository.cs:10`]
- [x] [Review][Patch] P2 [Alta]: Validación currency sobreescribe error de max-length — Si `currency = "PESOMEXICANO"` (> 8 chars y no en allowed list), `AddRequired` escribe el error de longitud y luego la validación de `AllowedCurrencies` lo sobreescribe con "Moneda no reconocida." El cliente nunca ve el mensaje correcto. Fix: agregar `else if` o verificar que el campo no tenga error previo antes de aplicar la validación de moneda. [`OpsCatalogEndpoints.cs:241`]
- [x] [Review][Patch] P3 [Media]: Seed INACTIVA1 sin YahooTicker — `ApiWebFactory.SeedCatalogAsync` crea `INACTIVA1` sin campo `YahooTicker`. Tras añadir `YahooTicker` como requerido en `FibraDetail`, `ToDto(INACTIVA1)` emite `null` para ese campo. En SQL Server real fallaría la constraint NOT NULL. Fix: añadir `YahooTicker = "INACTIVA1.MX"` al seed. [`ApiWebFactory.cs:131`]
- [x] [Review][Patch] P4 [Media]: Path ticker sin normalizar en PUT/deactivate/activate — El ticker del path se pasa sin `.ToUpperInvariant()` a `GetByTickerAsync`. La búsqueda funciona porque el repo normaliza internamente, pero los callers directos de la API con minúsculas quedan sin cobertura de tests y el patrón es inconsistente. Fix: normalizar en el handler antes del lookup. [`OpsCatalogEndpoints.cs:113,160,196`]
- [x] [Review][Patch] P5 [Media]: FibraForm variantes con key compuesta index+valor — `key={\`${index}-${variant}\`}` cambia en cada keystroke → React desmonta/remonta el `<input>`, perdiendo foco al escribir. Fix: usar solo `key={index}` (las variantes no se reordenan). [`FibraForm.tsx:244`]
- [x] [Review][Patch] P6 [Media]: pendingTicker con variables obsoletos en CatalogTable — TanStack Query v5 no limpia `variables` tras el éxito. Si `activateMutation.variables = "FUNO11"` (stale) y se ejecuta `deactivateMutation` para "DANHOS13", `pendingTicker` queda fijo en "FUNO11" → el botón de DANHOS13 no muestra el estado de carga. Fix: usar `activateMutation.isPending ? activateMutation.variables : deactivateMutation.isPending ? deactivateMutation.variables : null`. [`CatalogTable.tsx:29`]
- [x] [Review][Patch] P7 [Media]: AddOptionalUrl acepta schemes no-HTTP (SSRF) — `Uri.TryCreate` acepta `file://`, `ftp://`, `javascript:`, `data:` e IPs internas. Fix: validar que el scheme sea `https` (o `http`) antes de aceptar la URL. [`OpsCatalogEndpoints.cs:295`]
- [x] [Review][Patch] P8 [Media]: AddAsync no normaliza ticker antes de persistir — Si se llama `AddAsync` con un ticker en minúsculas directamente (fuera del endpoint), la entidad se persiste con casing incorrecto. Fix: añadir `fibra.Ticker = fibra.Ticker.ToUpperInvariant()` al inicio de `AddAsync`. [`FibraRepository.cs:10`]
- [x] [Review][Patch] P9 [Media]: Tests comparten estado mutable — `DisposeAsync` no limpia; `PublicCatalog_AfterDeactivate_ExcludesDanhos13` desactiva DANHOS13 sin revertir → tests posteriores que asumen DANHOS13 activo pueden fallar según orden de ejecución. Fix: reactivar DANHOS13 al final del test o resetear la BD en `DisposeAsync`. [`CatalogOpsEndpointTests.cs:36,193`]
- [x] [Review][Patch] P10 [Baja]: Condición 409 en FibraForm es tautología — Ambas ramas del ternario renderizan `mutation.error.message`. El condicional no hace nada. Fix: eliminar el ternario y mostrar directamente `{mutation.error.message}`. [`FibraForm.tsx:269`]
- [x] [Review][Patch] P11 [Baja]: Log de auditoría se dispara en no-op deactivate/activate — Cuando el estado ya es el solicitado (idempotente), el código omite `UpdateAsync` pero igual registra "DEACTIVATE"/"ACTIVATE" en el log. Fix: mover el `LogInformation` dentro del bloque `if (fibra.State != ...)`. [`OpsCatalogEndpoints.cs:172,208`]
- [x] [Review][Patch] P12 [Baja]: Test 400 no valida el cuerpo (AC5 parcial) — El test verifica el status code 400 pero no que la respuesta contenga detalles de validación por campo. Fix: deserializar `HttpValidationProblemDetails` y verificar que algún campo tenga error. [`CatalogOpsEndpointTests.cs:65`]

**Deferred:**

- [x] [Review][Defer] D1: GetAllAsync sin paginación ni límite [`FibraRepository.cs:53`] — deferred, aceptable para el tamaño actual del catálogo (~6 FIBRAs); añadir paginación cuando el catálogo crezca
- [x] [Review][Defer] D2: `State` serializado como `ToString()` sin contrato explícito [`OpsCatalogEndpoints.cs:349`] — deferred, patrón consistente en el proyecto; considerar JsonConverter si hay clients heterogéneos
- [x] [Review][Defer] D3: ILoggerFactory instanciado por request en vez de ILogger<T> [`OpsCatalogEndpoints.cs:43`] — deferred, impacto de performance despreciable para endpoint Ops de baja frecuencia
- [x] [Review][Defer] D4: `UpdateAsync` llama `db.Fibras.Update()` en entidad ya tracked [`FibraRepository.cs:21`] — deferred, genera UPDATE completo en vez de diferencial pero correcto; refactorizar con cambio de EF tracking en futura épica
- [x] [Review][Defer] D5: `GetActor` fallback a "unknown" sin log de advertencia [`OpsCatalogEndpoints.cs:352`] — deferred, riesgo bajo para MVP con AdminOps autenticado; añadir `LogWarning` en siguiente historia de auditoría
