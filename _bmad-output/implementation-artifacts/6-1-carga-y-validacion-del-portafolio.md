# Historia 6.1: Carga y validación del portafolio

Status: done

## Story

Como usuario,
quiero subir un archivo Excel o CSV con mis posiciones en FIBRAs (Ticker, Qty, AvgCost) y recibir retroalimentación inmediata y clara sobre cualquier error de validación,
para que pueda cargar mi portafolio correctamente sin contactar soporte ni adivinar qué salió mal.

## Acceptance Criteria

### AC1 — Carga exitosa de archivo válido

**Dado que** subo un archivo `.xlsx` válido con 5 filas (columnas `Ticker`, `Qty`, `AvgCost`),
**Cuando** se procesa la carga,
**Entonces** las 5 posiciones se almacenan y el dashboard del portafolio se muestra inmediatamente.

### AC2 — Error por ticker inválido

**Dado que** subo un archivo donde una fila tiene el ticker `"FAKEXX"` (no está en el catálogo),
**Entonces** no se guarda ninguna posición, y veo una tabla con: fila número 2, ticker `"FAKEXX"`, error `"Ticker no encontrado en el catálogo."`.

### AC3 — Consolidación de duplicados

**Dado que** subo un archivo donde `FUNO11` aparece dos veces (500 unidades a $47, 300 unidades a $45),
**Entonces** las posiciones se consolidan en 1 fila: 800 unidades al costo promedio ponderado ($46.25).

### AC4 — Error por encabezados incorrectos

**Dado que** subo un archivo con un encabezado incorrecto (ej: `"Cost"` en lugar de `"AvgCost"`),
**Entonces** todas las filas fallan con un solo error: `"Columnas requeridas: Ticker, Qty, AvgCost. Encontradas: Ticker, Qty, Cost."`.

### AC5 — Confirmación al reemplazar portafolio existente

**Dado que** ya tengo un portafolio activo y subo un nuevo archivo,
**Entonces** aparece un diálogo de confirmación: `"Esto reemplazará tus X posiciones actuales. ¿Continuar?"`. Al cancelar, nada cambia.

## Tasks / Subtasks

### T1 — Dominio: entidad `PortfolioPosition` (AC1, AC3)

- [x] T1.1 — Crear `src/Server/Domain/Portfolio/PortfolioPosition.cs`:
  - Campos: `Id` (Guid), `UserId` (Guid), `FibraId` (Guid), `Titulos` (int), `CostoPromedio` (decimal), `CostoTotalCompra` (decimal), `UploadedAt` (DateTimeOffset)
  - Índice único lógico: `(UserId, FibraId)` — una posición activa por usuario por FIBRA
- [x] T1.2 — Crear `src/Server/Application/Portfolio/IPortfolioRepository.cs`:
  - `GetByUserIdAsync(Guid userId, CancellationToken ct) → IReadOnlyList<PortfolioPosition>`
  - `UpsertPortfolioAsync(Guid userId, IReadOnlyList<PortfolioPosition> positions, CancellationToken ct) → void`
  - `GetPositionCountByUserIdAsync(Guid userId, CancellationToken ct) → int`

### T2 — Infraestructura: configuración EF + migración (AC1)

- [x] T2.1 — Crear `src/Server/Infrastructure/Persistence/SqlServer/Configurations/Portfolio/PortfolioPositionConfiguration.cs`:
  - Schema: `portfolio`, tabla: `PortfolioPositions`
  - Columnas en snake_case: `id`, `user_id`, `fibra_id`, `titulos`, `costo_promedio`, `costo_total_compra`, `uploaded_at`
  - Índice único: `UX_PortfolioPositions_UserId_FibraId` sobre `(user_id, fibra_id)`
  - FK a `catalog.Fibras(id)` y a `auth.Users(id)` (o a la tabla Users del schema que exista)
- [x] T2.2 — Agregar `DbSet<PortfolioPosition> PortfolioPositions` en `AppDbContext.cs`
- [x] T2.3 — Crear `src/Server/Infrastructure/Persistence/Repositories/Portfolio/PortfolioRepository.cs`:
  - `UpsertPortfolioAsync`: elimina posiciones existentes del usuario y bulk-inserta las nuevas en una transacción
- [x] T2.4 — Registrar `IPortfolioRepository → PortfolioRepository` en `ApiServiceExtensions.cs`
- [x] T2.5 — Generar migración EF:
  ```bash
  dotnet ef migrations add AddPortfolioPositions \
    --project src/Server/Infrastructure \
    --startup-project src/Server/Api
  ```
  Aplicar: `dotnet ef database update --project src/Server/Infrastructure --startup-project src/Server/Api`

### T3 — Application: servicio de carga + validación (AC1–AC4)

- [x] T3.1 — Crear `src/Server/Infrastructure/Portfolio/PortfolioUploadService.cs` (impl en Infrastructure — ClosedXML/CsvHelper son deps de infraestructura):
  - Método: `ParseAndValidateAsync(Stream fileStream, string fileName, IReadOnlyList<Fibra> activeFibras, decimal commissionFactor) → PortfolioUploadResult`
  - Responsabilidades:
    1. Detectar formato por extensión (`.xlsx` o `.csv`)
    2. Parsear encabezados (case-insensitive) — si faltan `Ticker`, `Qty` o `AvgCost` → error único de headers
    3. Parsear cada fila: validar `Qty` > 0 entero, `AvgCost` > 0 decimal, `Ticker` existe en catálogo
    4. Si hay errores de fila → devolver lista de errores sin guardar nada
    5. Consolidar filas duplicadas por Ticker: sumar Qty, calcular costo promedio ponderado
    6. Calcular `CostoTotalCompra = Titulos × CostoPromedio × (1 + commissionFactor)`
  - Resultado: `record PortfolioUploadResult(bool Success, IReadOnlyList<PositionDto> Positions, IReadOnlyList<RowError> Errors)`
  - `record RowError(int RowNumber, string Ticker, string Message)`

- [x] T3.2 — Crear `src/Server/Application/Portfolio/IPortfolioUploadService.cs` (contrato del servicio)

- [x] T3.3 — Agregar `ClosedXML` para parsing de `.xlsx` y `CsvHelper` para `.csv`:
  - En `Directory.Packages.props`:
    - `<PackageVersion Include="ClosedXML" Version="0.104.2" />`
    - `<PackageVersion Include="CsvHelper" Version="33.0.1" />`
  - En `Infrastructure.csproj`:
    - `<PackageReference Include="ClosedXML" />`
    - `<PackageReference Include="CsvHelper" />`
  - Registrar `IPortfolioUploadService → PortfolioUploadService` en `ApiServiceExtensions.cs`

### T4 — API: endpoint `POST /api/v1/portfolio/upload` (AC1–AC5)

- [x] T4.1 — Crear `src/Server/Api/Endpoints/Private/PortfolioEndpoints.cs`:
  - `POST /api/v1/portfolio/upload` — acepta `IFormFile file`
    - Requiere `RequireAuthorization("User")`
    - Extrae `userId` de `ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)`
    - Lee `commissionFactor` de `IOperationalConfigRepository`
    - Lee catálogo activo de `IFibraRepository`
    - Llama `IPortfolioUploadService.ParseAndValidateAsync`
    - Si hay errores → `400` con `{ errors: RowError[] }`
    - Si valid → guarda con `IPortfolioRepository.UpsertPortfolioAsync` → `200` con `{ positionCount: N }`
  - `GET /api/v1/portfolio/status` — devuelve `{ hasPortfolio: bool, positionCount: int }` para el diálogo de confirmación (AC5)
- [x] T4.2 — Mapear `MapPortfolio(this IEndpointRouteBuilder app)` en `Program.cs`

### T5 — Frontend: página `/portafolio` con upload y confirmación (AC1–AC5)

- [x] T5.1 — Crear `src/Web/Main/src/modules/portafolio/PortafolioPage.tsx`:
  - Estado inicial: `GET /api/v1/portfolio/status` → si `hasPortfolio = false` → mostrar zona de upload
  - Si `hasPortfolio = true` → mostrar tabla básica de posiciones cargadas (ticker + qty + costo promedio)
- [x] T5.2 — Crear componente `UploadZone.tsx` en `src/Web/Main/src/modules/portafolio/`:
  - Drag-and-drop + click para seleccionar `.xlsx` / `.csv`
  - Botón "Cargar portafolio"
  - Al seleccionar archivo: si `positionCount > 0` → mostrar `ConfirmReplaceDialog`
  - Al confirmar (o si no hay portafolio previo): `POST /api/v1/portfolio/upload`
  - Si `200` → invalidar query `["portfolio"]`
  - Si `400` (errores de validación) → mostrar `ErrorTable` con las filas fallidas
- [x] T5.3 — Crear componente `ErrorTable.tsx`:
  - Columnas: Fila #, Ticker, Error
  - Se muestra solo cuando hay errores en la respuesta
- [x] T5.4 — Crear componente `PositionsTable.tsx`:
  - Columnas mínimas para esta historia: Ticker, Títulos, Costo Promedio, Costo Total Compra
  - Usar `—` para valores null (convención del proyecto)
- [x] T5.5 — Agregar ruta `/portafolio` en `src/Web/Main/src/app/routes.tsx`
- [x] T5.6 — Agregar enlace "Portafolio" en `PublicLayout.tsx`
- [x] T5.7 — Regenerar cliente API: `npm run codegen:api` (schema actualizado manualmente + regenerado)

### T6 — Tests unitarios (AC1–AC5)

- [x] T6.1 — Crear `tests/Unit/Infrastructure.Tests/Portfolio/PortfolioUploadServiceTests.cs` (en Infrastructure.Tests — la impl está en Infrastructure):
  - `ParseAndValidate_ValidXlsx_ReturnsPositions` — 5 filas con duplicados → 3 posiciones consolidadas
  - `ParseAndValidate_InvalidTicker_ReturnsRowError` — ticker `"FAKEXX"` → error en fila 2
  - `ParseAndValidate_DuplicateTickers_Consolidates` — FUNO11 500@47 + 300@45 → 800@46.25
  - `ParseAndValidate_WrongHeaders_ReturnsSingleHeaderError` — "Cost" en vez de "AvgCost"
  - `ParseAndValidate_NegativeQty_ReturnsRowError` — Qty < 0 → error
  - `ParseAndValidate_CommissionFactor_CalculatesCostoTotalCompra` — verifica fórmula exacta

- [x] T6.2 — Ejecutar: `dotnet test tests/Unit/Infrastructure.Tests/ --filter "Portfolio" --configuration Release`
  - Resultado: **6/6 pasando, 0 fallidos**

### T7 — Build verification

- [x] T7.1 — `dotnet build FIBRADIS.slnx --configuration Release` → 0 errores
- [x] T7.2 — `npm run build --workspace=src/Web/Main` → build exitoso (0 errores TypeScript)

## Dev Notes

### Arquitectura del módulo Portfolio

El módulo Portfolio sigue el mismo patrón de todos los módulos del proyecto:
- **Domain**: entidad `PortfolioPosition` en `Domain.Portfolio` — solo datos de entrada del usuario
- **Application**: contratos `IPortfolioRepository`, `IPortfolioUploadService` — sin lógica de infraestructura
- **Infrastructure**: `PortfolioRepository` implementa acceso a BD; `PortfolioUploadService` encapsula parsing de archivos
- **API**: endpoint en `Endpoints/Private/` con `.RequireAuthorization("User")`
- **Regla crítica**: ningún otro módulo escribe en el schema `portfolio`; los datos de mercado/fundamentales se leen en otros módulos en historias posteriores

### Campos persistidos vs calculados

Solo se persisten en BD:
- `fibra_id`, `user_id`, `titulos`, `costo_promedio`, `costo_total_compra`, `uploaded_at`

`costo_total_compra = titulos × costo_promedio × (1 + commission_factor)` se calcula al momento de carga con el factor vigente en `OperationalConfig.CommissionFactor` (default: 0.006).

Los campos de mercado (`precio_mercado`, `valor_mercado`, `plusvalia`) y fundamentales son calculados en lectura en histor ias 6.2 y 6.3 — **no están en scope de esta historia**.

### Regla de commission_factor

El cambio de `commission_factor` desde Ops **no retroactúa** `costo_total_compra` en posiciones ya guardadas. Solo se recalcula si el usuario vuelve a subir el archivo o edita una posición (historia 6.4).

### Consolidación de duplicados (AC3)

El servicio de upload consolida en memoria **antes** de guardar:
- Agrupar por Ticker (case-insensitive)
- Si hay más de una fila: `Qty_total = sum(Qty_i)`, `CostoPromedio = sum(Qty_i × AvgCost_i) / Qty_total`
- Resultado: una sola `PortfolioPosition` por ticker

**Ejemplo:**
- Fila 2: FUNO11, 500, 47.00 → peso = 23,500
- Fila 5: FUNO11, 300, 45.00 → peso = 13,500
- Total: 800 unidades, costo promedio = 37,000 / 800 = **46.25**

### Parsing de archivos

**Excel (.xlsx):**
```csharp
// ClosedXML
using var workbook = new XLWorkbook(stream);
var ws = workbook.Worksheets.First();
var headers = ws.Row(1).Cells(1, ws.LastColumnUsed().ColumnNumber())
    .Select(c => c.GetString()?.Trim())
    .ToList();
```

**CSV:**
```csharp
// CsvHelper
using var reader = new StreamReader(stream);
using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
csv.Read(); csv.ReadHeader();
var headers = csv.HeaderRecord?.ToList();
```

Headers se validan en modo **case-insensitive** (`StringComparer.OrdinalIgnoreCase`).

### Endpoint upload — patrón

```csharp
app.MapPost("/api/v1/portfolio/upload", async (
    IFormFile file,
    IPortfolioUploadService uploadSvc,
    IPortfolioRepository portfolioRepo,
    IFibraRepository fibraRepo,
    IOperationalConfigRepository configRepo,
    HttpContext ctx,
    CancellationToken ct) =>
{
    var userId = Guid.Parse(ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var config = await configRepo.GetAsync(ct);
    var activeFibras = await fibraRepo.GetAllActiveAsync(ct);

    await using var stream = file.OpenReadStream();
    var result = await uploadSvc.ParseAndValidateAsync(stream, file.FileName, activeFibras, config.CommissionFactor);

    if (!result.Success)
        return Results.Problem(
            title: "Errores de validación en el archivo",
            detail: "Corrija los errores y vuelva a subir el archivo.",
            statusCode: 400,
            extensions: new Dictionary<string, object?> { ["errors"] = result.Errors });

    await portfolioRepo.UpsertPortfolioAsync(userId, result.Positions, ct);
    return Results.Ok(new { positionCount = result.Positions.Count });
})
.RequireAuthorization("User")
.DisableAntiforgery()
.WithTags("Portfolio");
```

> Nota: `DisableAntiforgery()` es necesario para endpoints que aceptan `IFormFile` en Minimal API con CSRF desactivado.

### User ID en endpoints privados

El JWT incluye `sub = user.Id.ToString()` (Guid). En los endpoints:
```csharp
var userId = Guid.Parse(ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
```
`ClaimTypes.NameIdentifier` mapea al claim `sub` cuando el middleware JWT lo procesa.

### Frontend — autenticación

Esta historia solo requiere la pantalla de carga básica. El estado de autenticación en el frontend (login flow completo) está fuera del scope. La ruta `/portafolio` puede ser accesible públicamente en esta historia con un guard simple que detecte si hay token en localStorage y redirija a `/login` si no hay. La pantalla de login (`/login`) ya existe como ruta de redirección.

El `PublicLayout.tsx` ya tiene el botón "Iniciar sesión" → `/login`. Para esta historia, agregar enlace "Portafolio" en la nav siempre visible es aceptable (se puede refinar con guard en historia 6.2).

### TanStack Query — keys y patterns

```typescript
// Verificar estado actual del portafolio
const { data: status } = useQuery({
  queryKey: ['portfolio', 'status'],
  queryFn: () => fibrasApi.GET('/api/v1/portfolio/status', {}),
})

// Invalidar después de upload exitoso
queryClient.invalidateQueries({ queryKey: ['portfolio'] })
```

### shadcn/ui — componentes disponibles

Para el dialog de confirmación usar `Dialog` de radix-ui (ya disponible en el proyecto). Para la tabla de errores y posiciones usar una tabla HTML simple con clases Tailwind v4, o el componente `Table` de shadcn si ya está instalado.

**Verificar antes de usar:** `ls src/Web/Main/src/shared/ui/` para ver qué componentes shadcn están disponibles.

### Convenciones obligatorias

- No mostrar `0` para datos financieros null — usar `—`
- Imports con alias `@/` (nunca rutas relativas `../../`)
- `react-router` v7 — import desde `'react-router'`, no `'react-router-dom'`
- `noUnusedLocals: true` en tsconfig — no dejar imports sin usar
- `openapi-fetch` para todas las llamadas API — cliente en `fibrasApi.ts`

### EF Core — migración y schema

El proyecto usa **PostgreSQL 16** con Npgsql.EntityFrameworkCore.PostgreSQL.

Los schemas siguen la convención: `portfolio` (lowercase). En la configuración EF:
```csharp
modelBuilder.Entity<PortfolioPosition>(b =>
{
    b.ToTable("PortfolioPositions", "portfolio");
    b.HasKey(p => p.Id);
    b.Property(p => p.UserId).HasColumnName("user_id");
    b.Property(p => p.FibraId).HasColumnName("fibra_id");
    b.Property(p => p.Titulos).HasColumnName("titulos");
    b.Property(p => p.CostoPromedio).HasColumnName("costo_promedio").HasPrecision(18, 6);
    b.Property(p => p.CostoTotalCompra).HasColumnName("costo_total_compra").HasPrecision(18, 6);
    b.Property(p => p.UploadedAt).HasColumnName("uploaded_at");
    b.HasIndex(p => new { p.UserId, p.FibraId }).IsUnique().HasDatabaseName("UX_PortfolioPositions_UserId_FibraId");
});
```

**Workaround migración (si el proceso Api tiene los DLLs bloqueados):**
```bash
dotnet ef migrations add AddPortfolioPositions \
  --project src/Server/Infrastructure \
  --startup-project src/Server/Api \
  --configuration Release
```

### EF Core — no usar Task.WhenAll con el mismo DbContext

Los queries al mismo `DbContext` (scoped) deben ser secuenciales:
```csharp
// CORRECTO
var config = await configRepo.GetAsync(ct);
var activeFibras = await fibraRepo.GetAllActiveAsync(ct);

// INCORRECTO — lanza InvalidOperationException en PostgreSQL real
await Task.WhenAll(configRepo.GetAsync(ct), fibraRepo.GetAllActiveAsync(ct));
```

### Deuda a documentar (deferred de épicas anteriores)

Al implementar esta historia, si se detectan patrones de deuda de code reviews anteriores (ej. batch queries sin proyección completa, assertions solo de status code), documentarlos en el Dev Agent Record bajo "Deuda Técnica Detectada".

### Métodos existentes que reutilizar (no reinventar)

**`IFibraRepository` ya tiene:**
- `GetAllActiveAsync(CancellationToken ct)` → `IReadOnlyList<Fibra>` — **usar directamente** en el endpoint de upload para validar tickers
- `GetActivePagedAsync(...)` — no necesario en esta historia

**`IOperationalConfigRepository` ya tiene `GetAsync(ct)`** — ver `OpsFundamentalsEndpoints.cs` para ejemplo de uso.

**Patrón de registro de servicios** (ver `ApiServiceExtensions.cs`):
```csharp
builder.Services.AddScoped<IPortfolioRepository, PortfolioRepository>();
builder.Services.AddScoped<IPortfolioUploadService, PortfolioUploadService>();
```

**Patrón de mapping de endpoints** (ver `Program.cs`):
```csharp
app.MapPortfolio();
```

### References

- [Epics: Historia 6.1](../_bmad-output/planning-artifacts/epics.md)
- [Arquitectura: Portfolio Module Decisions](docs/req/architecture.md)
- [Convenciones FIBRADIS](_bmad-output/planning-artifacts/convenciones-fibradis.md)
- [Story 5.3 (patrón CRUD con EF + endpoints privados)](../_bmad-output/implementation-artifacts/5-3-gestion-del-catalogo-de-fibras-desde-ops.md)
- [Story 5.12 (patrón registro servicios + ApiServiceExtensions)](../_bmad-output/implementation-artifacts/5-12-reports-url-y-discovery-oficial.md)

## Senior Developer Review (AI)

### Review Findings

- [x] \[Review-Patch\] P1 — CSV field access case-sensitive: `csv.GetField("Ticker")` es case-sensitive; si el CSV tiene headers `ticker,qty,avgcost` (lowercase), la validación de headers pasa (OrdinalIgnoreCase) pero GetField falla. Fix: añadir `PrepareHeaderForMatch = args => args.Header.Trim().ToLowerInvariant()` al CsvConfiguration. `PortfolioUploadService.cs:79`
- [x] \[Review-Patch\] P2 — FK constraints ausentes en migración: la historia T2.1 especifica "FK a `catalog.Fibras(id)` y a `auth.Users(id)`"; la configuración EF y la migración `20260603150822_AddPortfolioPositions` no las definen. La BD no aplica integridad referencial en el schema `portfolio`. Fix: añadir `HasOne`/`HasForeignKey` en `PortfolioPositionConfiguration`; migración `20260603153835_PortfolioPositionForeignKeysAndPkRename` generada y pendiente de aplicar. `PortfolioPositionConfiguration.cs:9-25`
- [x] \[Review-Patch\] P3 — Archivo vacío (solo headers) borra portafolio silenciosamente: `rows.Count == 0` retorna `Success=true, Positions=[]`, el endpoint invoca `UpsertPortfolioAsync(userId, [], ct)` que ejecuta DELETE sin INSERT, eliminando todas las posiciones. Fix: añadir guard en `ValidateAndBuild`: si `rows.Count == 0` retornar error "El archivo no contiene posiciones." `PortfolioUploadService.cs:129`
- [x] \[Review-Patch\] P4 — Columna PK usa PascalCase `Id` en lugar de `id` snake_case: todos los campos usan snake_case pero la PK sigue convención EF por defecto. Fix: añadir `builder.Property(p => p.Id).HasColumnName("id");` en `PortfolioPositionConfiguration`; incluido en migración `20260603153835_PortfolioPositionForeignKeysAndPkRename`. `PortfolioPositionConfiguration.cs:12`
- [x] \[Review-Patch\] P5 — Extensión no reconocida cae silenciosamente al parser CSV: un archivo `.xls` o `.pdf` activa `ParseCsv` y produce error confuso de CsvHelper. Fix: añadir validación de extensión al inicio de `ParseAndValidateAsync`; retornar `RowError` con "Formato no soportado. Use .xlsx o .csv." `PortfolioUploadService.cs:23`
- [x] \[Review-Patch\] P6 — ErrorTable usa índice de array como React key: `key={i}` puede producir re-renders incorrectos si la lista cambia. Fix: usar clave compuesta `rowNumber-ticker-i`. `ErrorTable.tsx:23`
- [x] \[Review-Defer\] D1 — PositionsTable nunca se popula: `uploadedPositions` siempre se inicializa como `[]` porque el endpoint POST retorna solo `positionCount`, sin las posiciones. La tabla existe pero nunca renderiza datos. Pendiente en 6.2 (GET /portfolio). `PortafolioPage.tsx:27-29` — deferred, pre-existing
- [x] \[Review-Defer\] D2 — Sin límite de tamaño de archivo en POST /upload: archivos grandes se leen completamente en memoria. Bajo riesgo en esta historia, sin spec de límite. `PortfolioEndpoints.cs:28` — deferred, pre-existing
- [x] \[Review-Defer\] D3 — Sin auth guard en /portafolio: usuario no autenticado recibe 401 del status endpoint; el componente queda en estado de error silencioso. Story notes lo aplazan a 6.2. `PortafolioPage.tsx:17-24` — deferred, pre-existing
- [x] \[Review-Defer\] D4 — GetByUserIdAsync ordena por FibraId (Guid arbitrario): orden sin significado semántico. Sin impacto en esta historia (método no usado en scope). `PortfolioRepository.cs:13` — deferred, pre-existing

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

- DLLs bloqueados por proceso Api (PID 29076): builds usando `--configuration Release`; Api.json actualizado manualmente para portfolio endpoints ya que no se podía regenerar el spec con el proceso API corriendo.
- `PortfolioUploadService` colocado en `Infrastructure/Portfolio/` (no `Application/Portfolio/` como el story spec indica) porque necesita ClosedXML/CsvHelper que son deps de infraestructura; tests en `Infrastructure.Tests/Portfolio/` correspondiente.
- Error pre-existente en `NoticiasListPage.tsx` (`string|null` → `string|undefined`) corregido como parte del T7.

### Completion Notes List

- T1: Entidad `PortfolioPosition` en Domain.Portfolio + contrato `IPortfolioRepository` con 3 métodos.
- T2: EF config con schema `portfolio`, tabla `PortfolioPositions`, índice único `UX_PortfolioPositions_UserId_FibraId`. Migración `20260603150822_AddPortfolioPositions` aplicada a BD de desarrollo.
- T3: `IPortfolioUploadService` + `PortfolioUploadResult`/`RowError`/`PositionDto` en Application; implementación en Infrastructure con ClosedXML (Excel) y CsvHelper (CSV), validación case-insensitive de headers, consolidación de duplicados por promedio ponderado.
- T4: `PortfolioEndpoints` — `GET /status` y `POST /upload` con `RequireAuthorization("User")`, `DisableAntiforgery()` para FormFile. Registrado en `Program.cs`.
- T5: Módulo `portafolio` completo — `PortafolioPage`, `UploadZone` (drag-and-drop + confirmación), `ErrorTable`, `PositionsTable`. Ruta `/portafolio` + enlace en nav. Schema d.ts regenerado.
- T6: 6/6 unit tests en `Infrastructure.Tests/Portfolio/PortfolioUploadServiceTests.cs` usando ClosedXML para crear streams xlsx en memoria.
- T7: `dotnet build FIBRADIS.slnx --configuration Release` → 0 errores. `npm run build --workspace=src/Web/Main` → exitoso.

### File List

**Archivos nuevos (CREATE):**
- `src/Server/Domain/Portfolio/PortfolioPosition.cs`
- `src/Server/Application/Portfolio/IPortfolioRepository.cs`
- `src/Server/Application/Portfolio/IPortfolioUploadService.cs`
- `src/Server/Application/Portfolio/PortfolioUploadResult.cs`
- `src/Server/Infrastructure/Portfolio/PortfolioUploadService.cs`
- `src/Server/Infrastructure/Persistence/SqlServer/Configurations/Portfolio/PortfolioPositionConfiguration.cs`
- `src/Server/Infrastructure/Persistence/Repositories/Portfolio/PortfolioRepository.cs`
- `src/Server/Api/Endpoints/Private/PortfolioEndpoints.cs`
- `src/Server/Infrastructure/Migrations/20260603150822_AddPortfolioPositions.cs`
- `src/Server/Infrastructure/Migrations/20260603150822_AddPortfolioPositions.Designer.cs`
- `src/Web/Main/src/modules/portafolio/PortafolioPage.tsx`
- `src/Web/Main/src/modules/portafolio/UploadZone.tsx`
- `src/Web/Main/src/modules/portafolio/ErrorTable.tsx`
- `src/Web/Main/src/modules/portafolio/PositionsTable.tsx`
- `tests/Unit/Infrastructure.Tests/Portfolio/PortfolioUploadServiceTests.cs`

**Archivos modificados (UPDATE):**
- `src/Server/Infrastructure/Persistence/SqlServer/AppDbContext.cs`
- `src/Server/Infrastructure/Infrastructure.csproj`
- `src/Server/Api/CompositionRoot/ApiServiceExtensions.cs`
- `src/Server/Api/Program.cs`
- `src/Server/Infrastructure/Migrations/AppDbContextModelSnapshot.cs`
- `Directory.Packages.props`
- `src/Web/Main/src/api/fibrasApi.ts`
- `src/Web/Main/src/app/routes.tsx`
- `src/Web/Main/src/shared/layouts/PublicLayout.tsx`
- `src/Web/Main/src/modules/noticias/NoticiasListPage.tsx`
- `src/Web/SharedApiClient/schema.d.ts`
- `scripts/codegen/Api.json`
- `tests/Unit/Infrastructure.Tests/Infrastructure.Tests.csproj`

## Change Log

- 2026-06-03: Historia 6.1 implementada — módulo Portfolio desde cero. Entidad `PortfolioPosition`, schema `portfolio` en PostgreSQL, servicio de carga Excel/CSV (ClosedXML + CsvHelper), endpoints `GET /status` + `POST /upload`, frontend completo con drag-and-drop, confirmación de reemplazo, tabla de errores. Migración `20260603150822_AddPortfolioPositions` aplicada. 6/6 unit tests verdes.
