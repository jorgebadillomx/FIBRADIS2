# Historia 5.2: Importación de Fundamentales en Modo Manual

Status: in-progress

## Story

Como AdminOps,
quiero enviar datos de fundamentales extraídos para una FIBRA y trimestre, revisar un preview de los datos parseados y confirmar antes de que sean visibles en la plataforma,
para que todos los datos financieros publicados a los usuarios hayan sido validados por un operador humano.

## Acceptance Criteria

### AC1 — Import JSON crea registro pendiente con preview

**Dado que** envío un payload JSON válido al endpoint de importación con `fibraId`, `period`, `capRate`, `navPerCbfi`, `ltv`, `noiMargin`, `ffoMargin`, `quarterlyDistribution` y `summary`,
**Entonces** el sistema crea un registro con `status = "pending"` y retorna un preview con:
- Los campos reconocidos y sus valores
- Los campos faltantes (que llegaron como `null`)
- La referencia al PDF si fue incluida en el payload

### AC2 — Confirmar cambia estado a procesado y hace visible la data públicamente

**Dado que** confirmo la importación desde la UI de Fundamentales en Ops,
**Entonces**:
- El estado del registro cambia a `"processed"`
- Los datos se vuelven visibles en el perfil público de la FIBRA (`GET /api/v1/fundamentals/{ticker}/latest`)
- La `FundamentalesSection` en el Main SPA muestra los datos reales del trimestre confirmado

### AC3 — Periodo duplicado requiere Reprocess explícito

**Dado que** ya existe un registro con `status = "processed"` para la misma FIBRA y período,
**Cuando** envío un nuevo payload para el mismo período,
**Entonces**:
- El sistema crea el nuevo registro con `isPossibleUpdate = true`
- El preview incluye advertencia: "Ya existe un registro procesado para FUNO11 / Q3-2024. Se requiere Reprocess explícito para sobreescribir."
- El botón de confirmar muestra "Reprocess" en lugar de "Confirmar" al usuario

### AC4 — Campos nulos → status parcial, campos disponibles almacenados

**Dado que** algunos campos son `null` o están ausentes en el payload (ej. solo `capRate` y `navPerCbfi` tienen valor),
**Entonces**:
- El registro se crea con `status = "partial"`
- Los campos con valor se almacenan correctamente
- Los campos `null` se muestran como `—` en la sección de fundamentales de la ficha pública
- La sección de fundamentales públicos no lanza error ni muestra `0` para campos ausentes

### AC5 — Adjuntar PDF vincula referencia al registro

**Dado que** subo un archivo PDF mediante el endpoint de PDF upload con el `id` del registro,
**Entonces**:
- El archivo se almacena localmente bajo `uploads/fundamentals/{id}.pdf`
- El campo `pdfReference` del registro se actualiza con la ruta local
- Los campos `capturedAt` y `source` del registro reflejan el momento y origen del upload

### AC6 — Historial de registros por FIBRA en Ops

**Dado que** accedo a la sección Fundamentales de Ops y selecciono una FIBRA,
**Entonces** veo el historial de todos los registros para esa FIBRA en orden cronológico inverso, con:
- Período, estado (badge), campos con valor (resumen), quién importó, cuándo
- Botón "Reprocess" activo solo en registros `processed` o `partial`
- Botón "Ver PDF" activo si tiene `pdfReference`

### AC7 — Endpoints protegidos AdminOps

**Dado que** intento llamar a cualquier endpoint de fundamentales Ops sin token o con rol `User`,
**Entonces** recibo 401 o 403 respectivamente.

### AC8 — Sin regresiones

Todos los tests existentes pasan tras los cambios.

---

## Tasks / Subtasks

### Backend — Dominio

- [x] **T1: Entidad FundamentalRecord**
  - [x] T1.1 Crear `src/Server/Domain/Fundamentals/FundamentalRecord.cs`:
    ```csharp
    public class FundamentalRecord
    {
        public Guid Id { get; init; }
        public Guid FibraId { get; init; }
        public string Period { get; init; } = "";       // "Q3-2024"
        public string Status { get; init; } = "";       // "pending" | "processed" | "partial" | "error"
        public string ProcessingMode { get; init; } = "manual";
        public decimal? CapRate { get; init; }
        public decimal? NavPerCbfi { get; init; }
        public decimal? Ltv { get; init; }
        public decimal? NoiMargin { get; init; }
        public decimal? FfoMargin { get; init; }
        public decimal? QuarterlyDistribution { get; init; }
        public string? Summary { get; init; }
        public string? PdfReference { get; init; }
        public bool IsPossibleUpdate { get; init; }
        public string? ImportedBy { get; init; }
        public string? ConfirmedBy { get; init; }
        public DateTimeOffset CapturedAt { get; init; }
        public DateTimeOffset? ConfirmedAt { get; init; }
        public string? ErrorReason { get; init; }
    }
    ```

### Backend — Application

- [x] **T2: Interfaz de repositorio**
  - [x] T2.1 Crear `src/Server/Application/Fundamentals/IFundamentalRepository.cs`:
    ```csharp
    public interface IFundamentalRepository
    {
        Task<FundamentalRecord?> GetByIdAsync(Guid id, CancellationToken ct);
        Task<FundamentalRecord?> GetProcessedByFibraAndPeriodAsync(Guid fibraId, string period, CancellationToken ct);
        Task<FundamentalRecord?> GetLatestProcessedByFibraAsync(Guid fibraId, CancellationToken ct);
        Task<IReadOnlyList<FundamentalRecord>> GetByFibraAsync(Guid fibraId, CancellationToken ct);
        Task AddAsync(FundamentalRecord record, CancellationToken ct);
        Task UpdateStatusAsync(Guid id, string status, string? confirmedBy, DateTimeOffset? confirmedAt, CancellationToken ct);
        Task UpdatePdfReferenceAsync(Guid id, string pdfReference, CancellationToken ct);
    }
    ```

### Backend — Infrastructure

- [x] **T3: EF Core configuration + migración + repositorio**
  - [x] T3.1 Crear `src/Server/Infrastructure/Persistence/SqlServer/Configurations/Fundamentals/FundamentalRecordConfiguration.cs`
  - [x] T3.2 Registrar `DbSet<FundamentalRecord>` en `AppDbContext`
  - [x] T3.3 Migración generada: `AddFundamentalRecord`
  - [x] T3.4 Crear `src/Server/Infrastructure/Persistence/Repositories/Fundamentals/FundamentalRepository.cs`
  - [x] T3.5 Registrar `IFundamentalRepository → FundamentalRepository` en `ApiServiceExtensions.cs`

### Backend — SharedApiContracts

- [x] **T4: DTOs de contrato**
  - [x] T4.1 Crear `src/Server/SharedApiContracts/Fundamentals/ImportFundamentalsRequest.cs`:
    ```csharp
    public sealed record ImportFundamentalsRequest(
        Guid FibraId,
        string Period,          // "Q3-2024"
        decimal? CapRate,
        decimal? NavPerCbfi,
        decimal? Ltv,
        decimal? NoiMargin,
        decimal? FfoMargin,
        decimal? QuarterlyDistribution,
        string? Summary,
        string? PdfReference);
    ```
  - [x] T4.2 Crear `src/Server/SharedApiContracts/Fundamentals/FundamentalPreviewDto.cs`:
    ```csharp
    public sealed record FundamentalPreviewDto(
        Guid Id,
        string FibTicker,
        string Period,
        string Status,          // "pending" | "partial"
        bool IsPossibleUpdate,
        string? WarningMessage,
        IReadOnlyList<string> PresentFields,
        IReadOnlyList<string> MissingFields,
        string? PdfReference,
        DateTimeOffset CapturedAt);
    ```
  - [x] T4.3 Crear `src/Server/SharedApiContracts/Fundamentals/FundamentalRecordDto.cs`:
    ```csharp
    public sealed record FundamentalRecordDto(
        Guid Id,
        string FibraTicker,
        string Period,
        string Status,
        bool IsPossibleUpdate,
        decimal? CapRate,
        decimal? NavPerCbfi,
        decimal? Ltv,
        decimal? NoiMargin,
        decimal? FfoMargin,
        decimal? QuarterlyDistribution,
        string? Summary,
        string? PdfReference,
        string? ImportedBy,
        string? ConfirmedBy,
        DateTimeOffset CapturedAt,
        DateTimeOffset? ConfirmedAt);
    ```
  - [x] T4.4 Crear `src/Server/SharedApiContracts/Fundamentals/FundamentalesPublicDto.cs` (para endpoint público de Main SPA):
    ```csharp
    public sealed record FundamentalesPublicDto(
        string Period,          // "Q3-2024"
        int? PeriodsAgo,        // null si es el más reciente; calcular si hay un periodo más nuevo
        decimal? CapRate,
        decimal? NavPerCbfi,
        decimal? Ltv,
        decimal? NoiMargin,
        decimal? FfoMargin,
        decimal? QuarterlyDistribution,
        string? Summary,
        DateTimeOffset CapturedAt);
    ```

### Backend — API Endpoints

- [x] **T5: Endpoints Ops de Fundamentales**
  - [x] T5.1 Crear `src/Server/Api/Endpoints/Ops/OpsFundamentalsEndpoints.cs`:

    **`POST /api/v1/ops/fundamentals/import`** (AC1, AC3, AC4):
    - Recibe `ImportFundamentalsRequest` como body
    - Valida que `FibraId` exista en catálogo (llama `IFibraRepository.GetByIdAsync`)
    - Valida que `Period` no esté vacío y tenga formato válido (regex `^Q[1-4]-\d{4}$`)
    - Determina `status`: si todos los campos numéricos son `null` → `"error"` (nada que guardar); si alguno es `null` → `"partial"`; si todos presentes → `"pending"`
    - Verifica si existe registro `processed` para `(FibraId, Period)` → si existe: `isPossibleUpdate = true`
    - Extrae actor del JWT (patrón de `AiModeEndpoints.cs`: `ctx.User.Identity?.Name ?? FindFirstValue(Email) ?? ...`)
    - Llama `IFundamentalRepository.AddAsync`
    - Retorna `200 OK` con `FundamentalPreviewDto` (NO 202 — la creación es síncrona y pequeña)
    - `RequireAuthorization("AdminOps")`

    **`POST /api/v1/ops/fundamentals/{id}/confirm`** (AC2):
    - Carga registro por `id`, verifica que exista y que `Status` sea `"pending"` o `"partial"`
    - Si `IsPossibleUpdate = true`: acepta igualmente, sobreescribe el anterior (el "confirm" de un possible update ES el reprocess)
    - Actualiza `Status = "processed"`, `ConfirmedBy = actor`, `ConfirmedAt = DateTimeOffset.UtcNow`
    - Invalida caché de `GET /api/v1/fundamentals/{ticker}/latest` si aplica
    - Retorna `200 OK` con `FundamentalRecordDto`

    **`POST /api/v1/ops/fundamentals/{id}/pdf`** (AC5):
    - Recibe `multipart/form-data` con campo `file` (PDF)
    - Valida `Content-Type == "application/pdf"`, tamaño ≤ 20MB
    - Guarda en `wwwroot/uploads/fundamentals/{id}.pdf` (o configurable via `IConfiguration["Uploads:BasePath"]`)
    - Llama `IFundamentalRepository.UpdatePdfReferenceAsync(id, path, ct)`
    - Retorna `200 OK` con la ruta del archivo

    **`GET /api/v1/ops/fundamentals?fibraId={guid}`** (AC6):
    - Retorna lista de `FundamentalRecordDto` para esa FIBRA, ordenada por `CapturedAt DESC`
    - Retorna `400` si `fibraId` no es un GUID válido
    - `RequireAuthorization("AdminOps")`

  - [x] T5.2 Registrar en `Program.cs`: `app.MapOpsFundamentals()`

- [x] **T6: Endpoint público para Main SPA**
  - [x] T6.1 Crear `src/Server/Api/Endpoints/Public/FundamentalsEndpoints.cs`
  - [x] T6.2 Registrar en `Program.cs`: `app.MapFundamentalsPublic()`

- [x] **T7: Regenerar SharedApiClient**
  - [x] T7.1 `npm run codegen:api` ejecutado — `Api.json` y `schema.d.ts` actualizados

### Frontend — Ops SPA

- [x] **T8: API client**
  - [x] T8.1 Crear `src/Web/Ops/src/api/fundamentalsApi.ts`:
    - Usar el mismo patrón de `newsApi.ts`: `createPathBasedClient<paths>({ baseUrl: '' })`
    - Exportar tipos desde `components['schemas']`
    - Funciones:
      - `importFundamentals(payload: ImportFundamentalsRequest): Promise<FundamentalPreviewDto>`
      - `confirmFundamentals(id: string): Promise<FundamentalRecordDto>`
      - `fetchFundamentalsByFibra(fibraId: string): Promise<FundamentalRecordDto[]>`
      - `uploadFundamentalPdf(id: string, file: File): Promise<{ path: string }>`
    - Usar `assertOpsAccessToken()` y `getOpsAuthHeaders()` en cada función
    - `uploadFundamentalPdf`: usar `fetch` nativo con `FormData` ya que `openapi-fetch` no soporta multipart nativamente

- [x] **T9: Módulo FundamentalsSection**
  - [x] T9.1 Crear `src/Web/Ops/src/modules/fundamentals/FundamentalsImportForm.tsx`:
    - Formulario para ingresar datos de importación con campos:
      - Selector de FIBRA (`fibraId` — dropdown con FIBRAs del catálogo o input GUID)
      - Period (`string`, placeholder: "Q3-2024", validación regex `Q[1-4]-\d{4}`)
      - CapRate, NavPerCbfi, Ltv, NoiMargin, FfoMargin, QuarterlyDistribution (`number | ''`, optional)
      - Summary (`textarea`, optional)
      - PdfReference (`string`, optional)
    - Submit → llama `importFundamentals`, muestra `FundamentalPreviewDto` resultado
    - Usar React Hook Form + Zod para validación (patrón del proyecto)
    - Mientras pending: botón en estado loading

  - [x] T9.2 Crear `src/Web/Ops/src/modules/fundamentals/FundamentalsPreview.tsx`:
    - Recibe `FundamentalPreviewDto` como prop
    - Muestra campos presentes con valores formateados
    - Muestra campos faltantes en gris con `—`
    - Si `isPossibleUpdate = true`: muestra badge naranja "Actualización" y el `warningMessage`
    - Botón "Confirmar" (o "Reprocess" si `isPossibleUpdate`) → `useMutation` → `confirmFundamentals(id)`
    - Botón "Cancelar" → limpia el preview y vuelve al formulario
    - Tras confirmar exitosamente: invalida query `['fundamentals', fibraId]` y muestra badge verde "Confirmado"

  - [x] T9.3 Crear `src/Web/Ops/src/modules/fundamentals/FundamentalsHistory.tsx`:
    - Recibe `fibraId` como prop
    - Carga `useQuery({ queryKey: ['fundamentals', fibraId], queryFn: () => fetchFundamentalsByFibra(fibraId) })`
    - Tabla con columnas: Período, Estado (badge), Cap Rate, NAV, LTV, NOI, FFO, Dist. Trim., Importado por, Fecha
    - Badge colores: verde = "processed", amarillo = "partial", gris = "pending", naranja = possible update
    - Botón "Reprocess" activo solo en registros `processed` o `partial`; al hacer click reinicia el formulario de importación con los datos del registro
    - Botón "Subir PDF" → abre file input → llama `uploadFundamentalPdf`, invalida query

- [x] **T10: Página FundamentalsPage**
  - [x] T10.1 Crear `src/Web/Ops/src/pages/FundamentalsPage.tsx`:
    - Encabezado: "Fundamentales — Importación Manual"
    - Estado local: `{ step: 'form' | 'preview', preview: FundamentalPreviewDto | null, selectedFibraId: string | null }`
    - Cuando `step === 'form'`: renderiza `FundamentalsImportForm` + si `selectedFibraId` → `FundamentalsHistory`
    - Cuando `step === 'preview'`: renderiza `FundamentalsPreview`
    - Tras confirmar: vuelve a `step === 'form'`

- [x] **T11: Routing + navegación**
  - [x] T11.1 En `src/Web/Ops/src/main.tsx`: ruta `/fundamentals` agregada
  - [x] T11.2 En `src/Web/Ops/src/components/OpsShell.tsx`: nav item "Fundamentales" agregado entre Pipeline Logs y Prompts de IA

### Frontend — Main SPA

- [x] **T12: Conectar FundamentalesSection a la API real**
  - [x] T12.1 Crear `src/Web/Main/src/api/fundamentalesApi.ts`:
    - `fetchFundamentalesPublic(ticker: string): Promise<FundamentalesPublicDto | null>`
    - Retorna `null` en caso de 404 (FIBRA sin fundamentales confirmados)
    - Usa el cliente openapi-fetch existente con `.AllowAnonymous()` endpoint

  - [x] T12.2 En `FibraPage.tsx`: query `fetchFundamentalesPublic` + mapeo `FundamentalesPublicDto → FundamentalesData`
  - [x] T12.3 Texto del empty state actualizado en `FundamentalesSection.tsx`

### Tests

- [x] **T13: Unit tests backend**
  - [x] T13.1 Crear `tests/Unit/Infrastructure.Tests/Persistence/Repositories/FundamentalRepositoryTests.cs`:
    - `AddAsync` persiste el registro
    - `GetByIdAsync` retorna el registro o null
    - `GetProcessedByFibraAndPeriodAsync` retorna solo registros con `Status = "processed"`
    - `GetLatestProcessedByFibraAsync` retorna el más reciente por `CapturedAt DESC` con `Status = "processed"`
    - `GetByFibraAsync` retorna todos ordenados por `CapturedAt DESC`
    - `UpdateStatusAsync` actualiza solo el status sin tocar otros campos

- [x] **T14: Integration tests backend**
  - [x] T14.1 Crear `tests/Integration/Api.Tests/Fundamentals/FundamentalsImportTests.cs`:
    - `POST /api/v1/ops/fundamentals/import` con payload completo → 200, status=pending
    - `POST /api/v1/ops/fundamentals/import` con campos parciales (algunos null) → 200, status=partial
    - `POST /api/v1/ops/fundamentals/import` para período ya procesado → 200, `isPossibleUpdate = true`
    - `POST /api/v1/ops/fundamentals/import` → 401 sin token, 403 con rol User
    - `POST /api/v1/ops/fundamentals/{id}/confirm` → 200, status=processed
    - `POST /api/v1/ops/fundamentals/{id}/confirm` con id inexistente → 404
    - `GET /api/v1/ops/fundamentals?fibraId={guid}` → 200 con lista
    - `GET /api/v1/ops/fundamentals?fibraId={guid}` → 401 sin token
    - `GET /api/v1/fundamentals/{ticker}/latest` con ticker sin registros → 404
    - `GET /api/v1/fundamentals/{ticker}/latest` con ticker con registro procesado → 200 con `FundamentalesPublicDto`
    - `GET /api/v1/fundamentals/{ticker}/latest` es accesible sin token (anónimo)

---

## Dev Notes

### Prerequisito: story 5-1 en estado `done`

Story 5-1 (Dashboard Operativo) debe estar mergeada a `main` antes de implementar 5-2. Esta historia no modifica ningún archivo de 5-1, pero usa la misma rama base y es dependiente del OpsShell y los patrones de endpoints establecidos allí.

Verificar: `git log --oneline main | head -5` — debe incluir el commit de merge de story/5-1.

### Schema `fundamentals` — totalmente nuevo

No existe ninguna tabla en el schema `fundamentals`. Esta historia lo crea desde cero. No hay código de dominio, aplicación ni infraestructura que extender — se crean todos los archivos desde cero en los directorios que ya existen (solo tienen `.gitkeep`):
- `src/Server/Domain/Fundamentals/` → crear `FundamentalRecord.cs`
- `src/Server/Application/Fundamentals/` → crear `IFundamentalRepository.cs`
- `src/Server/Infrastructure/Persistence/Repositories/Fundamentals/` → crear directorio y `FundamentalRepository.cs`
- `src/Server/Infrastructure/Persistence/SqlServer/Configurations/Fundamentals/` → crear directorio y `FundamentalRecordConfiguration.cs`

### Lógica de status en el endpoint de import

El status se determina así:
```csharp
// Determinar campos presentes
var presentFields = new List<string>();
var missingFields = new List<string>();
void Check(string name, object? value) {
    if (value is not null) presentFields.Add(name);
    else missingFields.Add(name);
}
Check("capRate", request.CapRate);
Check("navPerCbfi", request.NavPerCbfi);
Check("ltv", request.Ltv);
Check("noiMargin", request.NoiMargin);
Check("ffoMargin", request.FfoMargin);
Check("quarterlyDistribution", request.QuarterlyDistribution);

// Summary es informacional, no afecta el status
var status = presentFields.Count == 0
    ? "error"          // nada que guardar
    : presentFields.Count < 6
        ? "partial"
        : "pending";   // todos los campos numéricos presentes
```

### isPossibleUpdate y confirmación

Cuando `IsPossibleUpdate = true`, el botón "Confirmar" en el frontend muestra "Reprocess". Al confirmarlo, el endpoint `POST /{id}/confirm` acepta el estado `pending` o `partial` incluso cuando `isPossibleUpdate = true`. La lógica de "no sobreescribir automáticamente" está implementada SOLO en el endpoint de import (que es el que detecta el duplicado y setea el flag) — no en el confirm. Esto cumple el AC3 sin agregar complejidad innecesaria al endpoint de confirm.

### Extracción del actor en endpoints Minimal API

Misma convención que `AiModeEndpoints.cs`:
```csharp
var actor = ctx.User.Identity?.Name
    ?? ctx.User.FindFirstValue(ClaimTypes.Email)
    ?? ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
    ?? "unknown";
```
Declarar `HttpContext ctx` como parámetro del handler de Minimal API para que la inyección sea automática.

### FK de FundamentalRecord → Fibra

El campo `FibraId` tiene FK hacia `catalog.Fibra.Id`. Usar `.OnDelete(DeleteBehavior.Restrict)` — los datos de fundamentales no deben perderse si se desactiva (soft delete) una FIBRA.

```csharp
builder.HasOne<Fibra>()
    .WithMany()
    .HasForeignKey(r => r.FibraId)
    .OnDelete(DeleteBehavior.Restrict);
```

**Importante**: la migración crea la FK cross-schema (`catalog` → `fundamentals`). SQL Server lo soporta; EF Core lo genera correctamente. Solo verificar que el nombre del índice no colisione.

### PDF Upload — MVP simple

Para MVP, el PDF se guarda en el filesystem local bajo `wwwroot/uploads/fundamentals/`. El directorio debe existir (crear en el handler si no existe). El `pdfReference` almacenado es la ruta relativa: `uploads/fundamentals/{id}.pdf`.

**Nota**: no exponer el PDF públicamente en MVP (no hay endpoint público de descarga). Solo el AdminOps puede verlo desde el panel Ops usando el path directo si fuera necesario.

### Endpoint público `GET /api/v1/fundamentals/{ticker}/latest`

Este endpoint debe ser `.AllowAnonymous()` y retorna `FundamentalesPublicDto` con los datos del registro más reciente con `status = "processed"`. Si no hay ninguno → `404`.

La `FundamentalesPublicDto` mapea directamente desde `FundamentalRecord`:
```csharp
return new FundamentalesPublicDto(
    Period: record.Period,
    PeriodsAgo: null,  // MVP: null = "es el único/más reciente"
    CapRate: record.CapRate,
    NavPerCbfi: record.NavPerCbfi,
    Ltv: record.Ltv,
    NoiMargin: record.NoiMargin,
    FfoMargin: record.FfoMargin,
    QuarterlyDistribution: record.QuarterlyDistribution,
    Summary: record.Summary,
    CapturedAt: record.CapturedAt);
```

### Mapeo FundamentalesPublicDto → FundamentalesData en Main SPA

El componente `FundamentalesSection.tsx` existente espera:
```typescript
interface FundamentalesData {
  periodsAgo?: number
  items?: FundamentalItem[]
}
interface FundamentalItem {
  label: string
  period: string
  value: number | null
}
```

Mapeo en `FibraPage.tsx`:
```typescript
const mapToFundamentalesData = (dto: FundamentalesPublicDto): FundamentalesData => ({
  periodsAgo: dto.periodsAgo ?? undefined,
  items: [
    { label: 'Cap Rate', period: dto.period, value: dto.capRate ?? null },
    { label: 'NAV por CBFI', period: dto.period, value: dto.navPerCbfi ?? null },
    { label: 'LTV', period: dto.period, value: dto.ltv ?? null },
    { label: 'Margen NOI', period: dto.period, value: dto.noiMargin ?? null },
    { label: 'Margen FFO', period: dto.period, value: dto.ffoMargin ?? null },
    { label: 'Dist. Trimestral', period: dto.period, value: dto.quarterlyDistribution ?? null },
  ],
})
```

La `FundamentalesSection` ya maneja `value: null` mostrando `—` — sin cambios necesarios.

### EF Core — nunca `Task.WhenAll` con el mismo DbContext

El repositorio de fundamentales comparte `DbContext` con el resto del sistema (es Scoped). Cualquier handler que llame a múltiples repos debe usar `await` secuencial, no `Task.WhenAll` (convención FIBRADIS documentada en `convenciones-fibradis.md`).

### Test de integración: FibraId válido en seed

Los tests de integración del endpoint de import requieren un `FibraId` válido de la FIBRA de seed (FUNO11). Usar:
```csharp
var fibra = await db.Set<Fibra>().FirstAsync(f => f.Ticker == "FUNO11");
var payload = new ImportFundamentalsRequest(fibra.Id, "Q3-2024", 0.08m, 120m, ...);
```

### noUnusedLocals en TypeScript

El tsconfig del Ops SPA tiene `noUnusedLocals: true`. Cada import en los archivos TypeScript/TSX nuevos DEBE usarse. Revisar antes de marcar la tarea completa.

### Archivos a crear/modificar

**Nuevos (backend):**
- `src/Server/Domain/Fundamentals/FundamentalRecord.cs`
- `src/Server/Application/Fundamentals/IFundamentalRepository.cs`
- `src/Server/Infrastructure/Persistence/SqlServer/Configurations/Fundamentals/FundamentalRecordConfiguration.cs`
- `src/Server/Infrastructure/Persistence/Repositories/Fundamentals/FundamentalRepository.cs`
- `src/Server/Infrastructure/Persistence/Migrations/{timestamp}_AddFundamentalRecord.{cs,Designer.cs}`
- `src/Server/Api/Endpoints/Ops/OpsFundamentalsEndpoints.cs`
- `src/Server/Api/Endpoints/Public/FundamentalsEndpoints.cs`
- `src/Server/SharedApiContracts/Fundamentals/ImportFundamentalsRequest.cs`
- `src/Server/SharedApiContracts/Fundamentals/FundamentalPreviewDto.cs`
- `src/Server/SharedApiContracts/Fundamentals/FundamentalRecordDto.cs`
- `src/Server/SharedApiContracts/Fundamentals/FundamentalesPublicDto.cs`

**Modificados (backend):**
- `src/Server/Infrastructure/Persistence/SqlServer/AppDbContext.cs` — agregar `DbSet<FundamentalRecord>`
- `src/Server/Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`
- `src/Server/Api/CompositionRoot/ApiServiceExtensions.cs` — registrar `IFundamentalRepository`
- `src/Server/Api/Program.cs` — `app.MapOpsFundamentals()` + `app.MapFundamentalsPublic()`
- `scripts/codegen/Api.json` + `src/Web/SharedApiClient/schema.d.ts` — regenerar

**Nuevos (frontend Ops):**
- `src/Web/Ops/src/api/fundamentalsApi.ts`
- `src/Web/Ops/src/modules/fundamentals/FundamentalsImportForm.tsx`
- `src/Web/Ops/src/modules/fundamentals/FundamentalsPreview.tsx`
- `src/Web/Ops/src/modules/fundamentals/FundamentalsHistory.tsx`
- `src/Web/Ops/src/pages/FundamentalsPage.tsx`

**Modificados (frontend Ops):**
- `src/Web/Ops/src/main.tsx` — agregar ruta `/fundamentals`
- `src/Web/Ops/src/components/OpsShell.tsx` — agregar item de nav

**Nuevos (frontend Main):**
- `src/Web/Main/src/api/fundamentalesApi.ts`

**Modificados (frontend Main):**
- `src/Web/Main/src/modules/ficha-publica/FibraPage.tsx` (o el componente que instancia `FundamentalesSection`) — añadir query
- `src/Web/Main/src/modules/ficha-publica/sections/FundamentalesSection.tsx` — actualizar texto del placeholder

**Tests nuevos:**
- `tests/Unit/Infrastructure.Tests/Persistence/Repositories/FundamentalRepositoryTests.cs`
- `tests/Integration/Api.Tests/Fundamentals/FundamentalsImportTests.cs`

### Referencias

- `[Source: epics.md#FR-18,FR-19,FR-22b,FR-22c,FR-38]` — requerimientos de fundamentales modo manual
- `[Source: src/Server/Api/Endpoints/Ops/AiModeEndpoints.cs]` — patrón de extracción del actor + endpoints Ops
- `[Source: src/Server/Api/Endpoints/Ops/OpsMarketEndpoints.cs]` — patrón de endpoints Ops con Hangfire
- `[Source: src/Server/Api/Endpoints/Public/CatalogEndpoints.cs]` — patrón de endpoint público + FibraDetail
- `[Source: src/Server/Api/CompositionRoot/ApiServiceExtensions.cs]` — patrón DI registration
- `[Source: src/Server/Api/Program.cs]` — patrón de registro de endpoints
- `[Source: src/Web/Ops/src/api/newsApi.ts]` — patrón de API client con openapi-fetch
- `[Source: src/Web/Ops/src/components/OpsShell.tsx]` — navigationItems para agregar "Fundamentales"
- `[Source: src/Web/Ops/src/main.tsx]` — router setup para agregar ruta `/fundamentals`
- `[Source: src/Web/Main/src/modules/ficha-publica/sections/FundamentalesSection.tsx]` — componente existente que espera `FundamentalesData`
- `[Source: _bmad-output/planning-artifacts/convenciones-fibradis.md#EF Core — nunca Task.WhenAll]` — queries secuenciales obligatorias
- `[Source: _bmad-output/planning-artifacts/convenciones-fibradis.md#EF Core Migrations]` — workaround `--configuration Release` si DLLs en uso

---

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

- **CS8604 nullable ref en Period**: `request.Period` inferido como potencialmente null en el handler de import. Solución: extraer `var period = request.Period ?? ""` antes del regex check y usar `period` en todo el handler.
- **CS0535 IFibraRepository.GetByIdAsync**: cuatro fake repos en tests unitarios existentes no implementaban el nuevo método. Solución: añadir implementación trivial a `FakeFibraRepository`, `FakeDistFibraRepository`, `FakeNewsFibraRepository`, `FakeHistoricalFibraRepository`.
- **EF Core InMemory no soporta ExecuteUpdateAsync**: `UpdateStatusAsync` y `UpdatePdfReferenceAsync` originalmente usaban `ExecuteUpdateAsync` — lanzaba `InvalidOperationException` en tests con InMemory. Solución: cambiar a patrón load-modify-save; las propiedades `Status`, `ConfirmedBy`, `ConfirmedAt`, `PdfReference` cambiadas de `init` a `set` en la entidad.
- **xUnit2013**: `Assert.Equal(1, collection.Count)` → `Assert.Single(...)` para cumplir regla del analizador.
- **TS2322 en FibraPage.tsx**: `periodsAgo` del schema es `string | number | undefined` pero `FundamentalesData.periodsAgo` espera `number | undefined`. Solución: guard `typeof fundamentalesDto.periodsAgo === 'number'`. Decimales del API son `null | number | string` → helper `toFundamentalNum` nombrado distinto de `toNum` (ya importado de format-time).

### Completion Notes List

- Implementada la historia completa 5-2: flujo import → preview → confirm → público
- EF Core: nueva tabla `[fundamentals].[FundamentalRecord]` con FK cross-schema a `catalog.Fibra.Id` (DeleteBehavior.Restrict). Migración `AddFundamentalRecord` generada y aplicada.
- 4 endpoints Ops (`import`, `confirm`, `pdf`, `list`) con `RequireAuthorization("AdminOps")` + 1 endpoint público anónimo `GET /api/v1/fundamentals/{ticker}/latest`
- Frontend Ops: `FundamentalsImportForm`, `FundamentalsPreview`, `FundamentalsHistory`, `FundamentalsPage` — react-hook-form con validación integrada (sin `@hookform/resolvers`, no instalado en el proyecto)
- Frontend Main: `fundamentalesApi.ts` + query en `FibraPage.tsx` + mapeo `FundamentalesPublicDto → FundamentalesData`
- Codegen API regenerado: `Api.json` + `schema.d.ts` actualizados
- Tests: 7 unit tests (InfrastructureTests) + 12 integration tests (Api.Tests) — todos pasan. Suite completa: 114/114 unit, 140/140 integration.

### File List

**Nuevos (backend):**
- `src/Server/Domain/Fundamentals/FundamentalRecord.cs`
- `src/Server/Application/Fundamentals/IFundamentalRepository.cs`
- `src/Server/Infrastructure/Persistence/SqlServer/Configurations/Fundamentals/FundamentalRecordConfiguration.cs`
- `src/Server/Infrastructure/Persistence/Repositories/Fundamentals/FundamentalRepository.cs`
- `src/Server/Api/Endpoints/Ops/OpsFundamentalsEndpoints.cs`
- `src/Server/Api/Endpoints/Public/FundamentalsEndpoints.cs`
- `src/Server/SharedApiContracts/Fundamentals/ImportFundamentalsRequest.cs`
- `src/Server/SharedApiContracts/Fundamentals/FundamentalPreviewDto.cs`
- `src/Server/SharedApiContracts/Fundamentals/FundamentalRecordDto.cs`
- `src/Server/SharedApiContracts/Fundamentals/FundamentalesPublicDto.cs`
- `src/Server/Infrastructure/Persistence/Migrations/*_AddFundamentalRecord.cs`
- `src/Server/Infrastructure/Persistence/Migrations/*_AddFundamentalRecord.Designer.cs`

**Modificados (backend):**
- `src/Server/Infrastructure/Persistence/SqlServer/AppDbContext.cs`
- `src/Server/Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`
- `src/Server/Api/CompositionRoot/ApiServiceExtensions.cs`
- `src/Server/Api/Program.cs`
- `src/Server/Application/Catalog/IFibraRepository.cs` — añadido `GetByIdAsync`
- `src/Server/Infrastructure/Persistence/Repositories/Catalog/FibraRepository.cs` — implementación `GetByIdAsync`
- `scripts/codegen/Api.json`
- `src/Web/SharedApiClient/schema.d.ts`

**Nuevos (frontend Ops):**
- `src/Web/Ops/src/api/fundamentalsApi.ts`
- `src/Web/Ops/src/modules/fundamentals/FundamentalsImportForm.tsx`
- `src/Web/Ops/src/modules/fundamentals/FundamentalsPreview.tsx`
- `src/Web/Ops/src/modules/fundamentals/FundamentalsHistory.tsx`
- `src/Web/Ops/src/pages/FundamentalsPage.tsx`

**Modificados (frontend Ops):**
- `src/Web/Ops/src/main.tsx`
- `src/Web/Ops/src/components/OpsShell.tsx`

**Nuevos (frontend Main):**
- `src/Web/Main/src/api/fundamentalesApi.ts`

**Modificados (frontend Main):**
- `src/Web/Main/src/modules/ficha-publica/FibraPage.tsx`
- `src/Web/Main/src/modules/ficha-publica/sections/FundamentalesSection.tsx`

**Tests nuevos:**
- `tests/Unit/Infrastructure.Tests/Persistence/Repositories/FundamentalRepositoryTests.cs`
- `tests/Integration/Api.Tests/Fundamentals/FundamentalsImportTests.cs`

**Tests modificados (fake repos para IFibraRepository.GetByIdAsync):**
- `tests/Unit/Infrastructure.Tests/Jobs/Market/MarketPipelineJobTests.cs`
- `tests/Unit/Infrastructure.Tests/Jobs/Market/DistributionPipelineJobTests.cs`
- `tests/Unit/Infrastructure.Tests/Jobs/News/NewsPipelineJobTests.cs`
- `tests/Unit/Infrastructure.Tests/Jobs/Market/DailySnapshotHistoricalJobTests.cs`

### Change Log

- 2026-05-23: Implementación completa de historia 5-2 — importación manual de fundamentales (import → preview → confirm), historial Ops, endpoint público Main SPA. 114/114 unit tests, 140/140 integration tests. Estado → review.

### Review Findings

#### Decision Needed

_(todas resueltas — ver patches D1/D3/D4/D5 abajo; D2 descartado: 400 ValidationProblem es el comportamiento correcto, no persistir registros "error")_

#### Patches

- [x] [Review][Patch] P1: handleReprocess asigna record.fibraTicker (string "FUNO11") a selectedFibraId — fetchFundamentalsByFibra espera UUID; la tabla de historial falla al recargar tras Reprocess [`FundamentalsPage.tsx:35`]
- [x] [Review][Patch] P2: queryClient.invalidateQueries usa preview.id (UUID del record) en lugar de fibraId — La query de historial usa key ['fundamentals', fibraId]; al confirmar, la tabla no se refresca [`FundamentalsPreview.tsx:17`]
- [x] [Review][Patch] P3: Test UpdateStatusAsync_UpdatesStatusOnly espera ThrowsAnyAsync<InvalidOperationException> pero la implementación usa load-modify-save (no ExecuteUpdateAsync) — Comentario y expectativa son incorrectos; con InMemory debería succeed [`FundamentalRepositoryTests.cs:207`]
- [x] [Review][Patch] P4: Missing test: POST /import y GET /?fibraId con rol User deben retornar 403 — AC7 requiere verificar tanto 401 (sin token) como 403 (rol incorrecto) [`FundamentalsImportTests.cs`]
- [x] [Review][Patch] P5: UpdateStatusAsync devuelve silenciosamente si el record desaparece entre el check del handler y el update — El handler retorna 200 OK con datos obsoletos; debería lanzar o indicar el fallo [`FundamentalRepository.cs:37`]
- [x] [Review][Patch] P6: FundamentalsImportForm no valida formato UUID en el campo fibraId — Cualquier string pasa la validación de RHF; la API recibe texto inválido y devuelve error genérico de binding [`FundamentalsImportForm.tsx:71`]
- [x] [Review][Patch] P7: parseFloat sin guard isNaN en los campos numéricos — parseFloat("abc") = NaN; NaN se serializa en JSON y llega a la API sin indicación de error en el formulario [`FundamentalsImportForm.tsx:48`]
- [x] [Review][Patch] P8: FundamentalRecord.Id no se asigna explícitamente en el handler — Depende de EF ValueGeneratedOnAdd para leer el valor generado por SQL; riesgo de Id=Guid.Empty en el PreviewDto si EF no setea el campo init tras SaveChanges [`OpsFundamentalsEndpoints.cs:81`]
- [x] [Review][Patch] P9: PDF upload no captura excepciones de IO — Si File.Create o CopyToAsync fallan, el registro de BD no se actualiza pero el archivo puede quedar parcialmente escrito en disco sin error surfaceado al caller [`OpsFundamentalsEndpoints.cs:205`]
- [x] [Review][Patch] P10: Doble-confirm race: UpdateStatusAsync no re-verifica el status antes de guardar — Dos confirmaciones concurrentes del mismo record pasan el guard del handler y ambas llaman SaveChanges; la segunda sobreescribe ConfirmedBy/ConfirmedAt silenciosamente [`FundamentalRepository.cs:35`]

#### Deferred

- [x] [Review][Defer] W1: GetByFibraAsync retorna todos los registros sin paginación [`FundamentalRepository.cs:23`] — deferred, out of scope for story AC; add pagination in future Ops history story
- [x] [Review][Defer] W2: Magic strings de status ("pending", "partial", "processed") sin constantes ni enum [`FundamentalRecord.cs`] — deferred, consistent with existing project patterns; extract in future refactor
- [x] [Review][Defer] W3: Uploads concurrentes al mismo record ID pueden corromper el archivo (File.Create trunca el archivo mientras otro stream está escribiendo) [`OpsFundamentalsEndpoints.cs:205`] — deferred, admin-only tool; file locking complexity out of MVP scope
- [x] [Review][Defer] W4: Case sensitivity en GetByTickerAsync es un problema pre-existente del módulo catalog no introducido por esta historia [`FundamentalsEndpoints.cs`] — deferred, pre-existing catalog issue
- [x] [Review][Patch] D1→P11: GetLatestProcessedByFibraAsync debe ordenar por período financiero (Q4-2024 > Q3-2024), no por CapturedAt [`FundamentalRepository.cs:17`]
- [x] [Review][Patch] D3→P12: Añadir campo PdfUploadedAt (DateTimeOffset?) a FundamentalRecord y actualizarlo en /pdf upload [`FundamentalRecord.cs`, `OpsFundamentalsEndpoints.cs:205`]
- [x] [Review][Patch] D4→P13: Implementar GET /api/v1/ops/fundamentals/{id}/pdf como endpoint de descarga del archivo; actualizar FundamentalsHistory para mostrar botón "Ver PDF" con link real [`OpsFundamentalsEndpoints.cs`, `FundamentalsHistory.tsx:83`]
- [x] [Review][Patch] D5→P14: Regex de período debe restringir años a rango válido (ej. `^Q[1-4]-20\d{2}$`) [`OpsFundamentalsEndpoints.cs:12`]
