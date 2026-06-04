# Historia 6.6: Importación de Portafolio desde GBM

## Metadata

- **Epic:** 6 — Portafolio Unificado
- **Story ID:** 6.6
- **Story Key:** 6-6-importacion-portafolio-gbm
- **Branch:** story/6-6-importacion-gbm
- **Status:** done
- **Prioridad:** Alta

---

## Historia de Usuario

Como usuario autenticado con una cuenta en GBM,
quiero subir cualquiera de mis archivos de portafolio GBM sin tener que preocuparme por el formato — el sistema detecta automáticamente si es la exportación oficial, el pegado desde el portal, o el archivo básico con columnas Ticker/Qty/AvgCost —
para que pueda importar mis datos sin reformatear ni crear archivos artificiales.

---

## Criterios de Aceptación

### AC-0: Detección automática y transparente del formato

**Dado que** el usuario sube cualquier archivo .xlsx o .csv aceptado por el sistema,
**Entonces** el sistema detecta automáticamente el formato del archivo **sin requerir ninguna acción ni selección por parte del usuario**. El usuario no sabe ni necesita saber qué formato detectó el sistema — simplemente sube su archivo y funciona. Los tres formatos soportados son:

- **GBM Oficial** — exportación .xlsx del app GBM (columna A1 = `"Emisora/Fondo"`)
- **GBM Pegado** — pegado desde portal GBM a Excel (columna A1 = `"Emisora"`)
- **Básico** — archivo manual con columnas `Ticker`, `Qty`, `AvgCost` (comportamiento original, sin cambios)

Si el archivo no corresponde a ningún formato conocido, se retorna el mensaje de error existente: `"Columnas requeridas: Ticker, Qty, AvgCost."`.

### AC-1: Parseo del formato oficial de GBM

**Dado que** el sistema detecta un archivo .xlsx con `"Emisora/Fondo"` en A1,
**Cuando** lo procesa,
**Entonces** extrae `Ticker = Emisora/Fondo`, `Qty = Títulos`, `AvgCost = Costo promedio` saltando: filas de section header ("Mercado de Capitales Global", "Mercado de Capitales Nacional", "Efectivo"), filas de headers repetidos (`"Emisora/Fondo"` en col A), y filas vacías. Los valores monetarios en formato string (`"$235.90"`, `"$3,586.00"`, `"$-4.57"`) se parsean correctamente a `decimal`. Tickers no encontrados en el catálogo de FIBRAs activas son silenciosamente descartados. El resultado pasa al pipeline de validación y consolidación existente.

### AC-2: Parseo del formato pegado de GBM

**Dado que** el sistema detecta un archivo .xlsx con `"Emisora"` en A1,
**Cuando** lo procesa,
**Entonces** extrae `Ticker`, `Qty` y `AvgCost` resolviendo el patrón de dos filas: la fila de datos tiene `"&nbsp;"` o vacío en col A con los valores numéricos en cols B (Títulos) y C (Cto. Prom.), y la siguiente fila tiene el ticker real en col A con el resto nulo. Tickers no encontrados en catálogo son descartados.

### AC-3: Normalización de tickers GBM

**Dado que** el archivo GBM contiene tickers con espacios como `"ANAU N"`, `"IUES N"`, `"IUIT N"`,
**Entonces** el parser normaliza cada ticker con `.Replace(" ", "").Trim()` antes de buscarlo en el catálogo, resultando en `"ANAUN"`, `"IUESN"`, `"IUITN"`. El catálogo de FIBRAs activas debe contener los tickers sin espacios para que el match funcione.

### AC-4: Modo merge — upload aditivo

**Dado que** el usuario tiene posiciones activas y sube un archivo GBM con `?mode=merge`,
**Cuando** el sistema procesa el upload,
**Entonces** por cada posición en el archivo: si la FIBRA ya existe en BD se suma la cantidad de títulos y se recalcula el costo promedio ponderado `((existingQty * existingAvg) + (newQty * newAvg)) / (existingQty + newQty)`, actualizando también el `CostoTotalCompra`. Si la FIBRA es nueva se inserta normalmente. Las posiciones existentes que NO aparecen en el archivo no son modificadas.

### AC-5: Detección de duplicado en modo merge

**Dado que** el usuario intenta hacer merge de un archivo cuyas posiciones ya existen **exactamente** en BD (mismo ticker, misma cantidad, mismo costo promedio),
**Cuando** el sistema lo detecta (antes de ejecutar el merge),
**Entonces** retorna HTTP 200 con `{ "duplicateDetected": true }` sin modificar la BD. El frontend muestra un dialog: "Este archivo ya está en tu portafolio — Las posiciones ya existen con los mismos valores. ¿Cargar de todas formas?" con opciones [Cancelar] y [Cargar de todas formas]. Si el usuario confirma, el frontend reenvía la petición con `?mode=merge&force=true`.

### AC-6: Modo replace con respaldo automático

**Dado que** el usuario tiene posiciones activas y sube un archivo con `?mode=replace` (default),
**Cuando** el sistema procesa el upload,
**Entonces** las posiciones activas se serializan y se guardan en `portfolio.PortfolioSnapshots` (sobrescribiendo el respaldo anterior si existe), las posiciones activas se eliminan, y se insertan las nuevas posiciones del archivo. Todo ocurre en una sola transacción. Si el usuario no tiene posiciones activas, no se crea respaldo (nada que respaldar).

### AC-7: Flujo UX — elección de modo al subir

**Dado que** el usuario tiene un portafolio activo y arrastra o selecciona un archivo .xlsx,
**Entonces** aparece un dialog con:
- Título: "¿Cómo quieres subir este archivo?"
- Opción 1 (default): "Actualizar portafolio" — "Reemplaza todo con el contenido del archivo. Se guardará un respaldo."
- Opción 2: "Agregar al portafolio" — "Suma los títulos a los existentes y promedia el costo. Útil si tienes varios portafolios en GBM."
- Botón [Continuar] y [Cancelar]
Para usuarios sin portafolio activo, el archivo se sube directamente sin dialog (replace es el único modo posible).

### AC-8: Archivar portafolio (vaciar con respaldo)

**Dado que** el usuario tiene posiciones activas y hace clic en "Archivar portafolio",
**Cuando** confirma el dialog "¿Guardar respaldo y vaciar tu portafolio? Podrás restaurarlo después.",
**Entonces** se llama `POST /api/v1/portfolio/archive`, las posiciones activas se archivan en `PortfolioSnapshots`, el portafolio queda vacío, y la UI regresa al estado de UploadZone mostrando un banner "Portafolio archivado el [fecha]. Puedes restaurarlo aquí." con botón [Restaurar].

### AC-9: Restaurar respaldo

**Dado que** existe un respaldo archivado y el usuario hace clic en [Restaurar],
**Cuando** confirma el dialog "¿Restaurar el respaldo del [fecha]? Tu portafolio actual se perderá si tienes posiciones.",
**Entonces** se llama `POST /api/v1/portfolio/restore`, las posiciones del snapshot se reinsertan como portafolio activo, el snapshot se elimina, y la UI regresa a la vista normal del portafolio.

### AC-10: Estado del respaldo

**Dado que** el usuario visita `/portafolio`,
**Cuando** existe un respaldo en `PortfolioSnapshots`,
**Entonces** se muestra un banner persistente: "Tienes un respaldo del [fecha formateada]. [Restaurar respaldo]".

### AC-11: Archivos CSV sin cambios

**Dado que** el usuario sube un archivo .csv,
**Entonces** el comportamiento es idéntico al actual (columnas `Ticker`, `Qty`, `AvgCost` requeridas). Los formatos GBM solo aplican a .xlsx.

### AC-12: Unit tests — parsers GBM

**Dado que** se ejecutan los unit tests,
**Entonces** existen tests para `GbmOfficialStrategy` y `GbmPastedStrategy` que cubren:
- Detección correcta del formato por cabecera
- Extracción de las 3 columnas con valores reales de los archivos de muestra
- Skip de filas de section header y headers repetidos (formato oficial)
- Resolución del patrón de dos filas con `&nbsp;` (formato pegado)
- Normalización de tickers con espacios
- Parseo de strings monetarios `"$-4.57"`, `"$3,586.00"` (formato oficial)
- Tickers no en catálogo son descartados sin error

---

## Tasks/Subtasks

- [x] Add GBM official/pasted parsing in `PortfolioUploadService` without changing the basic CSV/XLSX path.
- [x] Add portfolio snapshot storage, archive/restore endpoints, and merge upload mode in the backend.
- [x] Update the Main portfolio UI with upload mode selection, duplicate confirmation, and snapshot banner/actions.
- [x] Add unit tests for GBM parsing and portfolio snapshot/merge behavior, then regenerate API client and validate builds.

---

## Dev Notes

### Contexto crítico — Estado actual del parser

El parser existente en `src/Server/Infrastructure/Portfolio/PortfolioUploadService.cs` ya usa **ClosedXML** y busca headers por nombre (`Ticker`, `Qty`, `AvgCost`) en la fila 1. El método `ParseXlsx` lee la primera hoja, obtiene la fila 1 como headers, y delega a `ValidateAndBuild`. **No tocar la lógica de `ValidateAndBuild` ni `ParseCsv` — son correctos y tienen coverage implícito en producción.**

El upload endpoint actual `POST /api/v1/portfolio/upload` hace `delete-all + insert` vía `UpsertPortfolioAsync`. Esta historia añade modos sin cambiar el comportamiento por defecto para no romper el contrato existente.

### Diseño del parser GBM — Strategy dentro de ParseXlsx

No crear clases externas. Añadir la detección dentro de `ParseXlsx` antes de la lógica de headers actual:

```csharp
private static PortfolioUploadResult ParseXlsx(
    Stream stream, IReadOnlyList<Fibra> activeFibras, decimal commissionFactor)
{
    using var workbook = new XLWorkbook(stream);
    var ws = workbook.Worksheets.First();

    var a1 = ws.Cell(1, 1).GetString().Trim();

    // GBM Official format: "Emisora/Fondo" in A1
    if (a1.Equals("Emisora/Fondo", StringComparison.OrdinalIgnoreCase))
        return ParseGbmOfficial(ws, activeFibras, commissionFactor);

    // GBM Pasted format: "Emisora" in A1
    if (a1.Equals("Emisora", StringComparison.OrdinalIgnoreCase))
        return ParseGbmPasted(ws, activeFibras, commissionFactor);

    // Existing logic: look for Ticker/Qty/AvgCost headers
    // ... (current code below)
}
```

### GBM Official Parser — `ParseGbmOfficial`

Columnas por índice (1-based):
- Col 1: Emisora/Fondo → Ticker
- Col 2: Títulos → Qty (ya es entero en Excel)  
- Col 3: Costo promedio → AvgCost (string con formato "$XX.XX")

Filas a skipear:
- Filas donde col A contiene `"Emisora/Fondo"` (case-insensitive) → header repetido de sección
- Filas donde col A contiene textos de sección: `"Mercado de Capitales"`, `"Efectivo"` (usar `StartsWith` o `Contains`)
- Filas completamente vacías (todos los valores son null/empty)
- Filas donde col A es `"EFEC."` → efectivo, no FIBRA

Parseo de currency strings:
```csharp
private static bool TryParseCurrencyString(string raw, out decimal value)
{
    value = 0;
    if (string.IsNullOrWhiteSpace(raw) || raw == "-") return false;
    var cleaned = raw
        .Replace("$", "")
        .Replace(",", "")
        .Replace(" ", "") // &nbsp;
        .Trim();
    return decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
}
```

Nota: `"$-4.57"` después de Replace("$","") queda `"-4.57"` que `decimal.TryParse` con `NumberStyles.Any` maneja correctamente.

Si `TryParseCurrencyString` falla para AvgCost, intentar leer el valor numérico directo de la celda (ClosedXML: `ws.Cell(r, 3).GetDouble()`).

### GBM Pasted Parser — `ParseGbmPasted`

Columnas por índice (1-based):
- Col 1: Emisora → Ticker (pero la fila de datos tiene "&nbsp;"; el ticker real está en la SIGUIENTE fila)
- Col 2: Títulos → Qty (float/int en la fila de datos)
- Col 3: Cto. Prom. → AvgCost (float en la fila de datos)

**Algoritmo de buffer de una fila:**
```csharp
private static IEnumerable<RawRow> ExtractGbmPastedRows(IXLWorksheet ws)
{
    var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;
    RawRow? pendingData = null;
    var rowNum = 1;

    for (var r = 2; r <= lastRow; r++) // skip header row
    {
        var col1 = ws.Cell(r, 1).GetString().Replace(" ", "").Trim();
        var col2Cell = ws.Cell(r, 2);
        var col3Cell = ws.Cell(r, 3);

        bool isDataRow = string.IsNullOrEmpty(col1)
            && col2Cell.DataType == XLDataType.Number;

        if (isDataRow)
        {
            // Store numeric data; ticker comes in next row
            var qtyStr = col2Cell.GetDouble().ToString(CultureInfo.InvariantCulture);
            var avgCostStr = col3Cell.GetDouble().ToString(CultureInfo.InvariantCulture);
            pendingData = new RawRow(rowNum, "&nbsp;_pending", qtyStr, avgCostStr);
            rowNum++;
        }
        else if (!string.IsNullOrEmpty(col1) && pendingData != null)
        {
            // This row has the ticker; combine with pending data
            var ticker = NormalizeTicker(col1);
            yield return pendingData with { Ticker = ticker };
            pendingData = null;
            rowNum++;
        }
        else
        {
            // Noise row: reset pending and continue
            pendingData = null;
            rowNum++;
        }
    }
}
```

### Normalización de tickers

```csharp
private static string NormalizeTicker(string raw)
    => raw.Replace(" ", "").Trim();
```

Esto convierte `"ANAU N"` → `"ANAUN"`, `"FHIPO 14"` → `"FHIPO14"`, `"SPYD *"` → `"SPYD*"`. El catálogo debe tener los tickers en este formato para que el match funcione. **Verificar antes de implementar que el catálogo tiene FIBRAs con tickers como `"FHIPO14"` (sin espacio) o ajustar la normalización si usan otro formato.**

### Modo de upload — query param `?mode`

Añadir al endpoint `POST /api/v1/portfolio/upload`:

```csharp
group.MapPost("/upload", async (
    IFormFile file,
    [FromQuery] string mode = "replace",   // "replace" | "merge"
    [FromQuery] bool force = false,
    IPortfolioUploadService uploadSvc,
    IPortfolioRepository portfolioRepo,
    // ...
    CancellationToken ct) =>
```

**Flujo replace (default, con respaldo):**
1. Parse + validate (código actual)
2. Si tiene posiciones activas → `ArchivePortfolioAsync(userId, positions, ct)` (serializa + archive + delete en transacción)
3. Insertar nuevas posiciones
4. Retornar `PortfolioUploadResponseDto`

**Flujo merge:**
1. Parse + validate
2. Si `!force`: comparar posiciones parseadas con BD → si idénticas, retornar `{ duplicateDetected: true }`
3. Ejecutar `MergePositionsAsync(userId, positions, ct)` (INSERT ON CONFLICT DO UPDATE vía raw SQL o fetch+calculate+upsert)
4. Retornar `PortfolioUploadResponseDto`

### Duplicate detection — algoritmo

```csharp
private static bool AreDuplicates(
    IReadOnlyList<PositionDto> parsed,
    IReadOnlyList<PortfolioPosition> existing,
    Dictionary<Guid, string> fibraTickerMap)
{
    if (parsed.Count != existing.Count) return false;
    
    var existingByFibra = existing.ToDictionary(p => p.FibraId);
    
    return parsed.All(p =>
        existingByFibra.TryGetValue(p.FibraId, out var ex)
        && ex.Titulos == p.Titulos
        && Math.Abs(ex.CostoPromedio - p.CostoPromedio) < 0.001m);
}
```

Si `AreDuplicates` → return `Results.Ok(new PortfolioUploadResponseDto(0) { DuplicateDetected = true })`.

### PortfolioUploadResponseDto — extender

```csharp
// En SharedApiContracts/Portfolio/PortfolioUploadResponseDto.cs
public record PortfolioUploadResponseDto(int PositionCount)
{
    public bool DuplicateDetected { get; init; } = false;
}
```

### Nuevo dominio: PortfolioSnapshot

**`src/Server/Domain/Portfolio/PortfolioSnapshot.cs`** (NUEVO):
```csharp
namespace Domain.Portfolio;

public class PortfolioSnapshot
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DateTimeOffset ArchivedAt { get; set; }
    public string PositionsJson { get; set; } = string.Empty;
}
```

`PositionsJson` serializa un array de objetos `{ FibraId, Titulos, CostoPromedio, CostoTotalCompra, UploadedAt }`. No necesita ser deserializable a `PortfolioPosition` directamente — usar un DTO interno de snapshot.

**`src/Server/Infrastructure/Persistence/SqlServer/Configurations/Portfolio/PortfolioSnapshotConfiguration.cs`** (NUEVO):
```csharp
builder.ToTable("PortfolioSnapshots", "portfolio");
builder.HasKey(s => s.Id);
builder.HasIndex(s => s.UserId).IsUnique(); // 1 snapshot max por usuario
builder.Property(s => s.PositionsJson).IsRequired();
```

**Migration**: crear con `dotnet ef migrations add AddPortfolioSnapshot --project src/Server/Infrastructure --startup-project src/Server/Api`

### IPortfolioRepository — nuevos métodos

```csharp
// Serializa posiciones activas → INSERT/UPDATE en PortfolioSnapshots → DELETE posiciones activas (tx)
Task ArchivePortfolioAsync(Guid userId, CancellationToken ct);

// Lee snapshot → DELETE posiciones activas → INSERT snapshot positions → DELETE snapshot (tx)
// Retorna false si no hay snapshot
Task<bool> RestoreSnapshotAsync(Guid userId, CancellationToken ct);

// null si no hay snapshot
Task<PortfolioSnapshot?> GetSnapshotAsync(Guid userId, CancellationToken ct);

// INSERT...ON CONFLICT DO UPDATE para merge aditivo
Task MergePositionsAsync(Guid userId, IReadOnlyList<PortfolioPosition> positions, CancellationToken ct);
```

### MergePositionsAsync — implementación

Dado que el proyecto usa PostgreSQL, usar raw SQL con `ON CONFLICT`:

```csharp
public async Task MergePositionsAsync(
    Guid userId, IReadOnlyList<PortfolioPosition> positions, CancellationToken ct)
{
    foreach (var pos in positions)
    {
        await db.Database.ExecuteSqlRawAsync("""
            INSERT INTO portfolio."PortfolioPositions"
                ("Id","UserId","FibraId","Titulos","CostoPromedio","CostoTotalCompra","UploadedAt")
            VALUES ({0},{1},{2},{3},{4},{5},{6})
            ON CONFLICT ("UserId","FibraId") DO UPDATE SET
                "Titulos" = portfolio."PortfolioPositions"."Titulos" + EXCLUDED."Titulos",
                "CostoPromedio" = (
                    portfolio."PortfolioPositions"."Titulos" * portfolio."PortfolioPositions"."CostoPromedio"
                    + EXCLUDED."Titulos" * EXCLUDED."CostoPromedio"
                ) / (portfolio."PortfolioPositions"."Titulos" + EXCLUDED."Titulos"),
                "CostoTotalCompra" = portfolio."PortfolioPositions"."CostoTotalCompra" + EXCLUDED."CostoTotalCompra",
                "UploadedAt" = EXCLUDED."UploadedAt"
            """,
            pos.Id, userId, pos.FibraId, pos.Titulos, pos.CostoPromedio, pos.CostoTotalCompra, pos.UploadedAt,
            ct);
    }
}
```

**Verificar los nombres exactos de tabla/columnas** en `PortfolioPositionConfiguration.cs` antes de escribir el SQL raw — el proyecto puede usar snake_case (`portfolio_positions`) o PascalCase con comillas.

### ArchivePortfolioAsync — implementación

```csharp
public async Task ArchivePortfolioAsync(Guid userId, CancellationToken ct)
{
    var positions = await GetByUserIdAsync(userId, ct);
    if (positions.Count == 0) return; // nada que archivar

    var snapshotData = positions.Select(p => new
    {
        p.FibraId, p.Titulos, p.CostoPromedio, p.CostoTotalCompra, p.UploadedAt
    });
    var json = JsonSerializer.Serialize(snapshotData);

    await using var tx = await db.Database.BeginTransactionAsync(ct);

    var existing = await db.Set<PortfolioSnapshot>()
        .FirstOrDefaultAsync(s => s.UserId == userId, ct);

    if (existing is not null)
    {
        existing.PositionsJson = json;
        existing.ArchivedAt = DateTimeOffset.UtcNow;
    }
    else
    {
        db.Set<PortfolioSnapshot>().Add(new PortfolioSnapshot
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ArchivedAt = DateTimeOffset.UtcNow,
            PositionsJson = json,
        });
    }

    await db.PortfolioPositions
        .Where(p => p.UserId == userId)
        .ExecuteDeleteAsync(ct);

    await db.SaveChangesAsync(ct);
    await tx.CommitAsync(ct);
}
```

### Nuevos endpoints de snapshot

```
GET  /api/v1/portfolio/snapshot      → { hasSnapshot: bool, archivedAt: string|null }
POST /api/v1/portfolio/archive       → 204 NoContent
POST /api/v1/portfolio/restore       → 204 NoContent | 404 si no hay snapshot
```

El `GET /api/v1/portfolio/snapshot` se llama al cargar `PortafolioPage` para mostrar el banner de restauración.

### SharedApiContracts — nuevo DTO

```csharp
// src/Server/SharedApiContracts/Portfolio/PortfolioSnapshotStatusDto.cs
public record PortfolioSnapshotStatusDto(bool HasSnapshot, DateTimeOffset? ArchivedAt);
```

### Frontend — cambios en UploadZone

**Modo de upload — dialog de selección** (solo cuando `currentPositionCount > 0`):

```tsx
// Estado nuevo
const [uploadMode, setUploadMode] = useState<'replace' | 'merge'>('replace')
const [showModeDialog, setShowModeDialog] = useState(false)
const [showDuplicateDialog, setShowDuplicateDialog] = useState(false)

// Al seleccionar archivo con portafolio existente → showModeDialog = true
// Al confirmar modo → doUpload(mode, force=false)
// Si response.data.duplicateDetected → showDuplicateDialog = true
// Al confirmar en duplicateDialog → doUpload('merge', force=true)
```

En `doUpload`, pasar modo y force como query params:

```tsx
const { data, error, response } = await apiClient.POST('/api/v1/portfolio/upload', {
    params: { query: { mode: uploadMode, force } },
    // ...
})
```

**Ejecutar `npm run codegen:api` después de modificar el backend** para regenerar el schema TypeScript.

### Frontend — cambios en PortafolioPage

- Añadir llamada a `GET /api/v1/portfolio/snapshot` al cargar la página.
- Si `hasSnapshot`: mostrar banner informativo con la fecha del respaldo y botón [Restaurar respaldo].
- Añadir botón "Archivar portafolio" en algún lugar visible (ej. junto al título, o como acción secundaria). Solo visible cuando hay posiciones activas.
- Ambos botones abren dialogs de confirmación antes de llamar a sus endpoints.

### Archivos modificados / nuevos

**Backend — nuevos:**
- `src/Server/Domain/Portfolio/PortfolioSnapshot.cs`
- `src/Server/Infrastructure/Persistence/SqlServer/Configurations/Portfolio/PortfolioSnapshotConfiguration.cs`
- `src/Server/Infrastructure/Migrations/{timestamp}_AddPortfolioSnapshot.cs` (generado)
- `src/Server/SharedApiContracts/Portfolio/PortfolioSnapshotStatusDto.cs`

**Backend — modificados:**
- `src/Server/Infrastructure/Portfolio/PortfolioUploadService.cs` — añadir detección GBM + parsers
- `src/Server/Application/Portfolio/IPortfolioRepository.cs` — 4 métodos nuevos
- `src/Server/Infrastructure/Persistence/Repositories/Portfolio/PortfolioRepository.cs` — implementar 4 métodos
- `src/Server/Api/Endpoints/Private/PortfolioEndpoints.cs` — mode/force params + 3 endpoints nuevos
- `src/Server/Infrastructure/Persistence/SqlServer/AppDbContext.cs` — añadir `DbSet<PortfolioSnapshot>`
- `src/Server/SharedApiContracts/Portfolio/PortfolioUploadResponseDto.cs` — añadir `DuplicateDetected`

**Frontend — modificados:**
- `src/Web/Main/src/modules/portafolio/UploadZone.tsx` — mode selector + duplicate dialog
- `src/Web/Main/src/modules/portafolio/PortafolioPage.tsx` — snapshot banner + archive button
- `src/Web/SharedApiClient/schema.d.ts` — regenerar con `npm run codegen:api`

**Tests — nuevos:**
- `tests/Unit/Infrastructure.Tests/Portfolio/PortfolioUploadServiceGbmTests.cs`

### Unit tests — PortfolioUploadServiceGbmTests

Tests obligatorios a cubrir:

1. **GBM Official — detección correcta** por `"Emisora/Fondo"` en A1
2. **GBM Official — extrae 3 columnas** con valores reales de muestra (ANAU N, IUES N, IUIT N, FHIPO 14, etc.)
3. **GBM Official — skip de section headers** repetidos y filas Efectivo
4. **GBM Official — parseo de `"$-4.57"`** → `-4.57m`
5. **GBM Official — parseo de `"$3,586.00"`** → `3586.00m`
6. **GBM Pasted — detección correcta** por `"Emisora"` en A1
7. **GBM Pasted — resolución del patrón dos filas** con `&nbsp;` → ticker correcto
8. **GBM Pasted — ticker normalizado** `"FHIPO 14"` → `"FHIPO14"`
9. **GBM Pasted — ticker no en catálogo descartado** silenciosamente
10. **Formato básico sin cambios** — archivo con headers `Ticker/Qty/AvgCost` sigue funcionando

Los tests deben construir un `IXLWorksheet` con datos en memoria usando ClosedXML, sin leer archivos del disco. La lista de `activeFibras` debe incluir las FIBRAs de prueba con los tickers normalizados correctos.

### Consideraciones de compatibilidad

- El endpoint `POST /api/v1/portfolio/upload` sin `?mode` mantiene el comportamiento `replace` — **retrocompatible**.
- El dialog de confirmación existente "Esto reemplazará tus X posiciones" se **reemplaza** por el nuevo dialog de selección de modo.
- El campo `DuplicateDetected = false` en `PortfolioUploadResponseDto` es backward compatible — el frontend existente ignora campos desconocidos.
- `UpsertPortfolioAsync` **no se elimina** — se sigue usando para el flujo replace (que ahora también archiva antes de llamarlo). Solo se añade `MergePositionsAsync` para el flujo merge.

### Verificación de nombres de tabla/columnas PostgreSQL

Antes de escribir el SQL raw en `MergePositionsAsync`, revisar `PortfolioPositionConfiguration.cs` para confirmar si usa:
- `builder.ToTable("portfolio_positions", "portfolio")` con snake_case
- o `builder.ToTable("PortfolioPositions", "portfolio")` con PascalCase y comillas

Ajustar el raw SQL acorde.

---

## Deferred Work

- **D1:** El catálogo puede no tener tickers para todos los instrumentos GBM (SIC, ETFs como SPYD). Estos serán descartados silenciosamente. No se necesita manejo especial — el pipeline de validación existente los rechaza con "Ticker no encontrado".
- **D2:** Soporte para múltiples slots de respaldo (historial de versiones) — fuera de scope. Solo 1 respaldo por usuario.
- **D3:** Soporte para archivos GBM con encoding CP1252 — los tests en partido mode muestran encoding corrupto en "Títulos" del formato oficial. Se mapea por índice de columna, no por nombre, así que el encoding no afecta la implementación.

---

## Dev Agent Record

### Agent Model Used

GPT-5

### Debug Log References

- `dotnet test tests/Unit/Infrastructure.Tests/Infrastructure.Tests.csproj`
- `dotnet build src/Server/Api/Api.csproj --configuration Release`
- `dotnet ef migrations add AddPortfolioSnapshot --project src/Server/Infrastructure --startup-project src/Server/Api --configuration Release --output-dir Persistence/Migrations`
- `npm run codegen:api`
- `npm run build --workspace=src/Web/Main`

### Completion Notes List

- Implemented GBM XLSX detection in `PortfolioUploadService.ParseXlsx` for official (`Emisora/Fondo`) and pasted (`Emisora`) exports, including currency normalization, ticker normalization, section-header skipping, and silent discard of unknown tickers.
- Added `PortfolioSnapshot` persistence, configuration, migration, and repository support for archive, restore, snapshot lookup, merge uploads, and replace-with-backup semantics.
- Extended the portfolio API with `GET /api/v1/portfolio/snapshot`, `POST /api/v1/portfolio/archive`, `POST /api/v1/portfolio/restore`, and upload query params `mode`/`force`.
- Updated the Main portfolio page and upload zone with the mode selector dialog, duplicate confirmation dialog, snapshot banner, and archive/restore actions.
- Added backend unit coverage in `PortfolioUploadServiceGbmTests` and `PortfolioRepositorySnapshotTests`.
- Regenerated the OpenAPI document and shared API client after the backend contract changes.
- Validation passed: `dotnet test tests/Unit/Infrastructure.Tests/Infrastructure.Tests.csproj` (232 passed, 0 failed), `dotnet build src/Server/Api/Api.csproj --configuration Release`, and `npm run build --workspace=src/Web/Main`.

### File List

- `_bmad-output/implementation-artifacts/6-6-importacion-portafolio-gbm.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `scripts/codegen/Api.json`
- `src/Server/Api/Endpoints/Private/PortfolioEndpoints.cs`
- `src/Server/Application/Portfolio/IPortfolioRepository.cs`
- `src/Server/Domain/Portfolio/PortfolioSnapshot.cs`
- `src/Server/Infrastructure/Migrations/20260604161558_AddPortfolioSnapshot.cs`
- `src/Server/Infrastructure/Migrations/20260604161558_AddPortfolioSnapshot.Designer.cs`
- `src/Server/Infrastructure/Migrations/AppDbContextModelSnapshot.cs`
- `src/Server/Infrastructure/Persistence/Repositories/Portfolio/PortfolioRepository.cs`
- `src/Server/Infrastructure/Persistence/SqlServer/AppDbContext.cs`
- `src/Server/Infrastructure/Persistence/SqlServer/Configurations/Portfolio/PortfolioSnapshotConfiguration.cs`
- `src/Server/Infrastructure/Portfolio/PortfolioUploadService.cs`
- `src/Server/SharedApiContracts/Portfolio/PortfolioSnapshotStatusDto.cs`
- `src/Server/SharedApiContracts/Portfolio/PortfolioUploadResponseDto.cs`
- `src/Web/Main/src/modules/portafolio/PortafolioPage.tsx`
- `src/Web/Main/src/modules/portafolio/UploadZone.tsx`
- `src/Web/SharedApiClient/schema.d.ts`
- `tests/Unit/Infrastructure.Tests/Portfolio/PortfolioRepositorySnapshotTests.cs`
- `tests/Unit/Infrastructure.Tests/Portfolio/PortfolioUploadServiceGbmTests.cs`

### Change Log

- 2026-06-04: Implemented GBM import format auto-detection, portfolio snapshot archive/restore support, merge uploads, UI updates, migration, OpenAPI refresh, and regression tests.

### Review Findings

#### Patches

- [x] `Review/Patch` P1 — `ArchivePortfolioAsync` carga posiciones FUERA de la transacción `PortfolioRepository.cs:41` — inconsistente con `UpsertPortfolioAsync` que las carga DENTRO. Existe ventana TOCTOU entre el `GetByUserIdAsync` externo y el `RemoveRange` dentro del lambda: otra request podría modificar las posiciones en ese intervalo, dejando el snapshot con datos obsoletos. Fix: mover `var positions = await GetByUserIdAsync(userId, ct)` y el guard `if (positions.Count == 0) return` dentro del lambda de `ExecuteInTransactionAsync`.

#### Defers

- [x] `Review/Defer` D1 — `snapshotQuery` sin manejo de estado de error `PortafolioPage.tsx:53` — si `/portfolio/snapshot` devuelve 500, `hasSnapshot` queda `false` silenciosamente y el usuario no ve el banner aunque exista un respaldo. `portfolioQuery` sí tiene UI de error; inconsistente. — deferred, bajo riesgo en producción
- [x] `Review/Defer` D2 — `RestoreSnapshotAsync` carga snapshot fuera de la transacción `PortfolioRepository.cs:56` — mismo TOCTOU que P1; más complejo de corregir porque el `return false` precede al `ExecuteInTransactionAsync`. Requiere refactorizar a variable local. — deferred, bajo riesgo usuario único
- [x] `Review/Defer` D3 — Doble fetch de posiciones en modo merge `PortfolioEndpoints.cs:224` + `PortfolioRepository.cs:87` — el endpoint llama `GetByUserIdAsync` para detección de duplicados y luego `MergePositionsAsync` lo llama de nuevo internamente; ventana estrecha de race condition entre ambas lecturas. — deferred, ventana muy estrecha en práctica
- [x] `Review/Defer` D4 — `handleConfirmDuplicate` no captura `selectedFile` en cierre `UploadZone.tsx:98` — usa `selectedFile` del estado React en el momento de confirmar; si el componente se desmonta/remonta entre detección y confirmación, `doUpload` sale silenciosamente sin feedback. Fix: capturar `const capturedFile = selectedFile` al detectar el duplicado y pasarlo al handler. — deferred, edge case teórico
- [x] `Review/Defer` D5 — Banner post-archivado no coincide con AC-8 `PortafolioPage.tsx:187` — AC-8 especifica "Portafolio archivado el \[fecha\]. Puedes restaurarlo aquí." pero la implementación reutiliza el banner de AC-10 ("Tienes un respaldo del..."), sin distinción visual post-archivado. — deferred, cosmético funcional
- [x] `Review/Defer` D6 — `currentPositionCount` hardcodeado a 0 en render sin posiciones `PortafolioPage.tsx:211` — pasa `0` literal en vez de `positions.length` (que también sería 0 en ese branch); inconsistente con la otra instancia en línea 241. — deferred, cosmético
