# Historia 5.4: Configuración Operativa desde Ops sin Redespliegue

Status: done

## Story

Como AdminOps,
quiero editar todos los parámetros operativos clave desde la sección de configuración de Ops
— commission_factor, avg_periods y cadencia del pipeline de noticias —
con cada cambio completamente auditado y recuperable desde la vista de auditoría de Ops,
para que pueda ajustar el comportamiento del sistema en producción sin ningún redespliegue ni intervención de un desarrollador.

## Acceptance Criteria

### AC1 — Actualizar commission_factor

**Dado que** actualizo `commission_factor` de 0.006 a 0.008 en Configuración de Ops,
**Entonces**:
- El valor se persiste en la tabla `OperationalConfig` (schema `ops`)
- Los cálculos posteriores de lectura del portafolio (Épica 6) usarán 0.008
- Los valores de `costo_total_compra` ya persistidos en la BD no cambian
- El cambio queda registrado en `ConfigAuditLog` con actor, timestamp, campo `commission_factor`, valor anterior y nuevo

### AC2 — Actualizar avg_periods

**Dado que** actualizo `avg_periods` de 4 a 6,
**Entonces**:
- El valor se persiste en `OperationalConfig`
- Los cálculos subsiguientes de métricas AVG (Épica 6) usarán los últimos 6 períodos disponibles
- El cambio queda auditado en `ConfigAuditLog`

### AC3 — Blocklist de noticias (ya existe — verificar integración con audit view)

**Dado que** agrego "fibra cervical" al blocklist de noticias (endpoint existente `POST /api/v1/news/blocklist-terms`),
**Entonces** el siguiente ciclo de ingesta descarta cualquier item que coincida con ese término.

> Nota: El CRUD del blocklist ya está implementado (story 4-1). Esta historia no modifica esos endpoints. El AC3 se verifica con los tests de integración existentes.

### AC4 — Cadencia del pipeline de noticias

**Dado que** actualizo la cadencia del pipeline de noticias de 60 minutos a 30 minutos,
**Entonces**:
- El valor se persiste en `OperationalConfig.NewsCadenceMinutes = 30`
- El job de Hangfire `news-pipeline-hourly` se actualiza inmediatamente con cron `*/30 * * * *` (sin redespliegue)
- El cambio queda auditado en `ConfigAuditLog`
- En el siguiente arranque del proceso, la cadencia se lee desde la BD (no del valor hardcodeado)

**Validación**: `NewsCadenceMinutes` debe ser divisor de 60 en el rango 15–60 (valores válidos: 15, 20, 30, 60).

### AC5 — Log de auditoría recuperable desde Ops

**Dado que** se cambia cualquier campo de OperationalConfig (commission_factor, avg_periods, news_cadence_minutes),
**Entonces**:
- Se crea una entrada en `ConfigAuditLog` con: actor (email del JWT), timestamp (`DateTimeOffset.UtcNow`), nombre del campo, valor anterior (string), nuevo valor (string)
- `GET /api/v1/ops/audit-log` retorna la lista ordenada por timestamp descendente (más reciente primero)
- La pantalla de Configuración de Ops muestra el historial de cambios

### AC6 — Endpoints protegidos AdminOps

**Dado que** intento llamar a `GET /api/v1/ops/config` o `PUT /api/v1/ops/config` sin token o con rol `User`,
**Entonces** recibo `401` o `403` respectivamente.

### AC7 — Sin regresiones

Todos los tests existentes del catálogo, noticias y AI pasan tras los cambios.

---

## Tasks / Subtasks

### Backend — Dominio

- [x] **T1: OperationalConfig — entidad singleton**
  - [x] T1.1 Crear `src/Server/Domain/Ops/OperationalConfig.cs`:
    ```csharp
    namespace Domain.Ops;

    public class OperationalConfig
    {
        public int Id { get; set; } = 1;           // singleton
        public decimal CommissionFactor { get; set; } = 0.006m;
        public int AvgPeriods { get; set; } = 4;
        public int NewsCadenceMinutes { get; set; } = 60;
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
        public string? UpdatedBy { get; set; }
    }
    ```

- [x] **T2: ConfigAuditLog — entidad de auditoría**
  - [x] T2.1 Crear `src/Server/Domain/Ops/ConfigAuditLog.cs`:
    ```csharp
    namespace Domain.Ops;

    public class ConfigAuditLog
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Actor { get; set; } = string.Empty;
        public DateTimeOffset ChangedAt { get; set; } = DateTimeOffset.UtcNow;
        public string FieldName { get; set; } = string.Empty;
        public string? PreviousValue { get; set; }
        public string? NewValue { get; set; }
    }
    ```

### Backend — Infraestructura / Persistencia

- [x] **T3: EF Core — configuración de entidades**
  - [x] T3.1 Crear `src/Server/Infrastructure/Persistence/SqlServer/Configurations/Ops/OperationalConfigConfiguration.cs`:
    ```csharp
    using Domain.Ops;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    namespace Infrastructure.Persistence.SqlServer.Configurations.Ops;

    public class OperationalConfigConfiguration : IEntityTypeConfiguration<OperationalConfig>
    {
        public void Configure(EntityTypeBuilder<OperationalConfig> builder)
        {
            builder.ToTable("OperationalConfig", schema: "ops");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
            builder.Property(x => x.CommissionFactor).HasColumnName("commission_factor")
                .HasPrecision(10, 6);
            builder.Property(x => x.AvgPeriods).HasColumnName("avg_periods");
            builder.Property(x => x.NewsCadenceMinutes).HasColumnName("news_cadence_minutes");
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            builder.Property(x => x.UpdatedBy).HasColumnName("updated_by").HasMaxLength(256);

            builder.HasData(new OperationalConfig
            {
                Id = 1,
                CommissionFactor = 0.006m,
                AvgPeriods = 4,
                NewsCadenceMinutes = 60,
                UpdatedAt = new DateTimeOffset(2026, 5, 24, 0, 0, 0, TimeSpan.Zero),
                UpdatedBy = "system",
            });
        }
    }
    ```
  - [x] T3.2 Crear `src/Server/Infrastructure/Persistence/SqlServer/Configurations/Ops/ConfigAuditLogConfiguration.cs`:
    ```csharp
    using Domain.Ops;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    namespace Infrastructure.Persistence.SqlServer.Configurations.Ops;

    public class ConfigAuditLogConfiguration : IEntityTypeConfiguration<ConfigAuditLog>
    {
        public void Configure(EntityTypeBuilder<ConfigAuditLog> builder)
        {
            builder.ToTable("ConfigAuditLog", schema: "ops");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasColumnName("id");
            builder.Property(x => x.Actor).HasColumnName("actor").HasMaxLength(256).IsRequired();
            builder.Property(x => x.ChangedAt).HasColumnName("changed_at");
            builder.Property(x => x.FieldName).HasColumnName("field_name").HasMaxLength(64).IsRequired();
            builder.Property(x => x.PreviousValue).HasColumnName("previous_value").HasMaxLength(512);
            builder.Property(x => x.NewValue).HasColumnName("new_value").HasMaxLength(512);

            builder.HasIndex(x => x.ChangedAt);  // para ordenar eficientemente
        }
    }
    ```

- [x] **T4: Actualizar AppDbContext**
  - [x] T4.1 En `src/Server/Infrastructure/Persistence/SqlServer/AppDbContext.cs`, agregar:
    ```csharp
    // al inicio con otros usings:
    using Domain.Ops;

    // en las propiedades DbSet:
    public DbSet<OperationalConfig> OperationalConfigs => Set<OperationalConfig>();
    public DbSet<ConfigAuditLog> ConfigAuditLogs => Set<ConfigAuditLog>();
    ```

- [x] **T5: Migración EF Core**
  - [x] T5.1 Ejecutar (con la API detenida si está corriendo):
    ```bash
    dotnet ef migrations add AddOpsConfigAndAuditLog \
      --project src/Server/Infrastructure \
      --startup-project src/Server/Api
    ```
    Si los DLLs están en uso (API corriendo): agregar `--configuration Release` al comando (workaround documentado en `convenciones-fibradis.md`).
  - [x] T5.2 Verificar que la migración generada incluye:
    - `migrationBuilder.EnsureSchema("ops")` (EF Core lo genera automáticamente)
    - Creación de tabla `ops.OperationalConfig`
    - Creación de tabla `ops.ConfigAuditLog` con índice en `changed_at`
    - `InsertData` con el seed row de `OperationalConfig`
  - [x] T5.3 Aplicar migración: `dotnet ef database update --project src/Server/Infrastructure --startup-project src/Server/Api`

### Backend — Application Layer (repositorios)

- [x] **T6: IOperationalConfigRepository**
  - [x] T6.1 Crear `src/Server/Application/Ops/IOperationalConfigRepository.cs`:
    ```csharp
    using Domain.Ops;

    namespace Application.Ops;

    public interface IOperationalConfigRepository
    {
        Task<OperationalConfig> GetAsync(CancellationToken ct = default);
        Task UpdateAsync(
            decimal? commissionFactor,
            int? avgPeriods,
            int? newsCadenceMinutes,
            string actor,
            CancellationToken ct = default);
    }
    ```

- [x] **T7: OperationalConfigRepository**
  - [x] T7.1 Crear `src/Server/Infrastructure/Persistence/Repositories/Ops/OperationalConfigRepository.cs`:
    ```csharp
    using Application.Ops;
    using Domain.Ops;
    using Infrastructure.Persistence.SqlServer;
    using Microsoft.EntityFrameworkCore;

    namespace Infrastructure.Persistence.Repositories.Ops;

    public class OperationalConfigRepository(AppDbContext db) : IOperationalConfigRepository
    {
        public async Task<OperationalConfig> GetAsync(CancellationToken ct = default)
            => await db.OperationalConfigs.FindAsync([1], ct)
               ?? new OperationalConfig();

        public async Task UpdateAsync(
            decimal? commissionFactor,
            int? avgPeriods,
            int? newsCadenceMinutes,
            string actor,
            CancellationToken ct = default)
        {
            var config = await db.OperationalConfigs.FindAsync([1], ct);
            if (config is null)
            {
                config = new OperationalConfig { Id = 1 };
                db.OperationalConfigs.Add(config);
            }

            var auditEntries = new List<ConfigAuditLog>();
            var now = DateTimeOffset.UtcNow;

            if (commissionFactor.HasValue && config.CommissionFactor != commissionFactor.Value)
            {
                auditEntries.Add(new ConfigAuditLog
                {
                    Actor = actor,
                    ChangedAt = now,
                    FieldName = "commission_factor",
                    PreviousValue = config.CommissionFactor.ToString("G"),
                    NewValue = commissionFactor.Value.ToString("G"),
                });
                config.CommissionFactor = commissionFactor.Value;
            }

            if (avgPeriods.HasValue && config.AvgPeriods != avgPeriods.Value)
            {
                auditEntries.Add(new ConfigAuditLog
                {
                    Actor = actor,
                    ChangedAt = now,
                    FieldName = "avg_periods",
                    PreviousValue = config.AvgPeriods.ToString(),
                    NewValue = avgPeriods.Value.ToString(),
                });
                config.AvgPeriods = avgPeriods.Value;
            }

            if (newsCadenceMinutes.HasValue && config.NewsCadenceMinutes != newsCadenceMinutes.Value)
            {
                auditEntries.Add(new ConfigAuditLog
                {
                    Actor = actor,
                    ChangedAt = now,
                    FieldName = "news_cadence_minutes",
                    PreviousValue = config.NewsCadenceMinutes.ToString(),
                    NewValue = newsCadenceMinutes.Value.ToString(),
                });
                config.NewsCadenceMinutes = newsCadenceMinutes.Value;
            }

            if (auditEntries.Count == 0) return;

            config.UpdatedAt = now;
            config.UpdatedBy = actor;
            db.ConfigAuditLogs.AddRange(auditEntries);
            await db.SaveChangesAsync(ct);
        }
    }
    ```

- [x] **T8: IConfigAuditLogRepository**
  - [x] T8.1 Crear `src/Server/Application/Ops/IConfigAuditLogRepository.cs`:
    ```csharp
    using Domain.Ops;

    namespace Application.Ops;

    public interface IConfigAuditLogRepository
    {
        Task<IReadOnlyList<ConfigAuditLog>> GetRecentAsync(int limit = 50, CancellationToken ct = default);
    }
    ```
  - [x] T8.2 Crear `src/Server/Infrastructure/Persistence/Repositories/Ops/ConfigAuditLogRepository.cs`:
    ```csharp
    using Application.Ops;
    using Domain.Ops;
    using Infrastructure.Persistence.SqlServer;
    using Microsoft.EntityFrameworkCore;

    namespace Infrastructure.Persistence.Repositories.Ops;

    public class ConfigAuditLogRepository(AppDbContext db) : IConfigAuditLogRepository
    {
        public async Task<IReadOnlyList<ConfigAuditLog>> GetRecentAsync(int limit = 50, CancellationToken ct = default)
            => await db.ConfigAuditLogs
                .OrderByDescending(x => x.ChangedAt)
                .Take(limit)
                .ToListAsync(ct);
    }
    ```

### Backend — SharedApiContracts

- [x] **T9: DTOs**
  - [x] T9.1 Crear `src/Server/SharedApiContracts/Ops/OperationalConfigDto.cs`:
    ```csharp
    namespace SharedApiContracts.Ops;

    public sealed record OperationalConfigDto(
        decimal CommissionFactor,
        int AvgPeriods,
        int NewsCadenceMinutes,
        DateTimeOffset UpdatedAt,
        string? UpdatedBy);
    ```
  - [x] T9.2 Crear `src/Server/SharedApiContracts/Ops/UpdateOperationalConfigRequest.cs`:
    ```csharp
    namespace SharedApiContracts.Ops;

    public sealed record UpdateOperationalConfigRequest(
        decimal? CommissionFactor,
        int? AvgPeriods,
        int? NewsCadenceMinutes);
    ```
  - [x] T9.3 Crear `src/Server/SharedApiContracts/Ops/ConfigAuditLogDto.cs`:
    ```csharp
    namespace SharedApiContracts.Ops;

    public sealed record ConfigAuditLogDto(
        Guid Id,
        string Actor,
        DateTimeOffset ChangedAt,
        string FieldName,
        string? PreviousValue,
        string? NewValue);
    ```

### Backend — API Endpoints

- [x] **T10: OpsConfigEndpoints.cs**
  - [x] T10.1 Crear `src/Server/Api/Endpoints/Ops/OpsConfigEndpoints.cs`:

    **GET /api/v1/ops/config** — retorna la configuración actual:
    - Llama `IOperationalConfigRepository.GetAsync()`
    - Retorna `200 OK` con `OperationalConfigDto`
    - `RequireAuthorization("AdminOps")`

    **PUT /api/v1/ops/config** — actualiza uno o más campos:
    - Recibe `UpdateOperationalConfigRequest`
    - Validaciones:
      - Si todos los campos son null → `400 Bad Request`
      - `CommissionFactor`: si presente, debe ser > 0 y ≤ 0.1
      - `AvgPeriods`: si presente, debe ser 1–20
      - `NewsCadenceMinutes`: si presente, debe ser divisor de 60 en rango [15, 60] (valores válidos: 15, 20, 30, 60)
    - Extrae actor del JWT con `GetActor(ctx)` (patrón idéntico a `OpsCatalogEndpoints`)
    - Llama `IOperationalConfigRepository.UpdateAsync(...)`
    - Si `NewsCadenceMinutes` fue provisto y cambió → actualizar Hangfire schedule:
      ```csharp
      var jobManager = ctx.RequestServices.GetService<IRecurringJobManager>();
      if (jobManager is not null)
      {
          var cronExpr = $"*/{request.NewsCadenceMinutes} * * * *";
          jobManager.AddOrUpdate<NewsPipelineJob>(
              NewsPipelineSchedule.HourlyJobId,
              j => j.ExecuteAsync(CancellationToken.None),
              cronExpr,
              new RecurringJobOptions { TimeZone = MarketPipelineSchedule.GetMexicoTimeZone() });
      }
      ```
    - Retorna `204 No Content`
    - `RequireAuthorization("AdminOps")`

    **GET /api/v1/ops/audit-log** — últimas 50 entradas del audit log:
    - Llama `IConfigAuditLogRepository.GetRecentAsync(50)`
    - Retorna `200 OK` con `IReadOnlyList<ConfigAuditLogDto>`
    - `RequireAuthorization("AdminOps")`

  - [x] T10.2 Registrar en `src/Server/Api/Program.cs`:
    ```csharp
    app.MapOpsConfig();
    ```
    (Agregar después de `app.MapOpsCatalog()`)

- [x] **T11: Actualizar ApiServiceExtensions.cs** — registrar nuevos repositorios:
  ```csharp
  builder.Services.AddScoped<IOperationalConfigRepository, OperationalConfigRepository>();
  builder.Services.AddScoped<IConfigAuditLogRepository, ConfigAuditLogRepository>();
  ```
  Agregar los `using` correspondientes:
  ```csharp
  using Application.Ops;
  using Infrastructure.Persistence.Repositories.Ops;
  ```

- [x] **T12: Actualizar Program.cs — leer cadencia desde BD al arranque**
  - [x] T12.1 En el bloque `if (!useInMemoryHangfire ...)` de Program.cs, después de registrar el job de noticias con el cron estático, sobreescribir si la BD tiene un valor diferente:
    ```csharp
    // Leer cadencia desde BD para sobrescribir el default hardcodeado
    try
    {
        using var scope = app.Services.CreateScope();
        var opConfig = await scope.ServiceProvider
            .GetRequiredService<IOperationalConfigRepository>()
            .GetAsync();
        if (opConfig.NewsCadenceMinutes != 60)
        {
            var dynCron = $"*/{opConfig.NewsCadenceMinutes} * * * *";
            RecurringJob.AddOrUpdate<NewsPipelineJob>(
                NewsPipelineSchedule.HourlyJobId,
                j => j.ExecuteAsync(CancellationToken.None),
                dynCron,
                new RecurringJobOptions { TimeZone = mexicoTz });
        }
    }
    catch (Exception ex)
    {
        // Si la tabla no existe (primer despliegue antes de migración), usar default
        // El log de error da visibilidad sin romper el arranque
        var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
        startupLogger.LogError(ex, "No se pudo leer NewsCadenceMinutes desde BD al arranque. Usando default.");
    }
    ```
  - [x] T12.2 Agregar `using Application.Ops;` en Program.cs si no está presente

- [x] **T13: Regenerar SharedApiClient**
  - [x] T13.1 `npm run codegen:api` — actualiza `scripts/codegen/Api.json` y `src/Web/SharedApiClient/schema.d.ts`

### Backend — Fix deferred D5 de story 5-3

- [x] **T14: Agregar LogWarning cuando actor cae a "unknown"**
  - [x] T14.1 En `src/Server/Api/Endpoints/Ops/OpsCatalogEndpoints.cs`, agregar logger al método `GetActor` o inline en cada endpoint:
    ```csharp
    private static string GetActor(HttpContext ctx, ILogger logger)
    {
        var actor = ctx.User.Identity?.Name
            ?? ctx.User.FindFirstValue(ClaimTypes.Email)
            ?? ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (actor is null)
        {
            logger.LogWarning("GetActor: no identity claim found in JWT; using 'unknown'");
            return "unknown";
        }
        return actor;
    }
    ```
    Actualizar todos los call sites de `GetActor(ctx)` en `OpsCatalogEndpoints.cs` para pasar el logger.
  - [x] T14.2 Aplicar el mismo patrón en `OpsConfigEndpoints.cs` (implementarlo desde el inicio con la firma correcta)

### Frontend — Ops SPA

- [x] **T15: API client**
  - [x] T15.1 Crear `src/Web/Ops/src/api/configApi.ts`:
    - Patrón idéntico a `catalogApi.ts`: `createPathBasedClient<paths>({ baseUrl: '' })`
    - Usar `assertOpsAccessToken()` y `getOpsAuthHeaders()` en cada función
    - Exportar funciones:
      ```typescript
      fetchOpsConfig(): Promise<OperationalConfigDto>
      updateOpsConfig(payload: UpdateOperationalConfigRequest): Promise<void>
      fetchAuditLog(): Promise<ConfigAuditLogDto[]>
      ```

- [x] **T16: ConfigPage**
  - [x] T16.1 Crear `src/Web/Ops/src/pages/ConfigPage.tsx`:
    - `useQuery({ queryKey: ['ops-config'], queryFn: fetchOpsConfig })`
    - Formulario con tres campos:
      - `commission_factor`: número decimal (0.001–0.1), `step="0.001"`
      - `avg_periods`: entero (1–20)
      - `news_cadence_minutes`: select con opciones `[15, 20, 30, 60]`
    - `useMutation` para `updateOpsConfig`:
      - onSuccess: `queryClient.invalidateQueries(['ops-config'])` + `queryClient.invalidateQueries(['ops-audit-log'])` + mensaje "✓ Configuración guardada"
      - onError: mostrar mensaje de error
    - Solo enviar en el payload los campos que el usuario modificó (no enviar null para campos no tocados)
    - Sección de auditoría debajo del formulario: `useQuery({ queryKey: ['ops-audit-log'], queryFn: fetchAuditLog })` → tabla con columnas: Campo, Valor Anterior, Nuevo Valor, Actor, Timestamp
    - Sin dependencias npm nuevas; validación con React Hook Form (patrón de historias anteriores, sin `@hookform/resolvers`)
    - Mostrar timestamp en formato local con `new Date(entry.changedAt).toLocaleString('es-MX')`

- [x] **T17: Routing + navegación**
  - [x] T17.1 En `src/Web/Ops/src/main.tsx`: agregar ruta `{ path: 'config', element: <ConfigPage /> }` (última de la lista, antes del cierre)
  - [x] T17.2 En `src/Web/Ops/src/components/OpsShell.tsx`: agregar item de nav al final de la lista:
    ```typescript
    { label: 'Configuración', to: '/config', description: 'Parámetros operativos del sistema sin redespliegue.' },
    ```

### Tests

- [x] **T18: Unit tests backend**
  - [x] T18.1 Crear `tests/Unit/Infrastructure.Tests/Persistence/Repositories/OperationalConfigRepositoryTests.cs`:
    - `GetAsync_ReturnsDefault_WhenNoRow`: BD vacía → retorna `OperationalConfig` con defaults (commissionFactor=0.006, avgPeriods=4, newsCadenceMinutes=60)
    - `GetAsync_ReturnsSeedRow`: con datos seed → retorna el singleton
    - `UpdateAsync_CommissionFactor_UpdatesAndAudits`: cambiar commission_factor → valor actualizado en tabla + 1 entrada en ConfigAuditLog con fieldName="commission_factor", previousValue="0.006", newValue="0.008"
    - `UpdateAsync_AvgPeriods_UpdatesAndAudits`: cambiar avg_periods → auditado correctamente
    - `UpdateAsync_NewsCadenceMinutes_UpdatesAndAudits`: cambiar cadencia → auditado
    - `UpdateAsync_NoChanges_CreatesNoAuditEntries`: llamar con el mismo valor actual → 0 entradas de auditoría
    - `UpdateAsync_MultipleFields_CreatesSeparateAuditEntries`: cambiar 2 campos → 2 entradas en ConfigAuditLog (una por campo)

- [x] **T19: Integration tests backend**
  - [x] T19.1 Crear `tests/Integration/Api.Tests/Ops/OpsConfigEndpointTests.cs`:
    - `GET /ops/config` con token AdminOps → `200 OK`, campos `commissionFactor=0.006`, `avgPeriods=4`, `newsCadenceMinutes=60`
    - `GET /ops/config` sin token → `401`
    - `GET /ops/config` con rol User → `403`
    - `PUT /ops/config` con `{ "commissionFactor": 0.008 }` y token AdminOps → `204 No Content`; GET posterior devuelve `commissionFactor=0.008`
    - `PUT /ops/config` con todos null → `400 Bad Request`
    - `PUT /ops/config` con `commissionFactor = -0.001` → `400 Bad Request`
    - `PUT /ops/config` con `commissionFactor = 0.15` (> 0.1) → `400 Bad Request`
    - `PUT /ops/config` con `avgPeriods = 0` → `400 Bad Request`
    - `PUT /ops/config` con `avgPeriods = 25` (> 20) → `400 Bad Request`
    - `PUT /ops/config` con `newsCadenceMinutes = 45` (no divisor de 60) → `400 Bad Request`
    - `PUT /ops/config` con `newsCadenceMinutes = 30` → `204 No Content`; GET devuelve `newsCadenceMinutes=30`
    - `PUT /ops/config` sin token → `401`
    - `GET /ops/audit-log` con token AdminOps después de un cambio → `200 OK`, array con al menos 1 entrada con `fieldName` correcto
    - `GET /ops/audit-log` sin token → `401`

---

## Dev Notes

### Prerequisito: story 5-3 en estado `done` y mergeada a main

Esta historia parte del branch `main` post-merge de 5-3. Verificar que `main` incluye la migración `P13_FibraCreatedAt_DateTimeOffset` y los endpoints de catálogo Ops. El branch de esta historia es `story/5-4-configuracion-operativa`.

### Singleton OperationalConfig — patrón idéntico a AiModeConfig

`OperationalConfig` sigue exactamente el mismo patrón singleton que `AiModeConfig` (Id=1 fijo, `ValueGeneratedNever()`). El seed garantiza que la fila siempre existe. El repositorio hace fallback a un objeto en memoria con defaults si `FindAsync` retorna null (misma guardia que `AiModeRepository`).

```csharp
// BIEN — fila siempre existe por seed, pero el fallback es seguro
public async Task<OperationalConfig> GetAsync(CancellationToken ct = default)
    => await db.OperationalConfigs.FindAsync([1], ct) ?? new OperationalConfig();
```

### Actualización del Hangfire schedule — IRecurringJobManager condicional

`AddHangfire(...)` registra `IRecurringJobManager` en DI incluso en modo sin-storage (tests). Sin embargo, en modo InMemory (`Hangfire:UseInMemoryStorage=true`) la implementación real de Hangfire no tiene backing store y llamarla puede fallar.

**Patrón seguro**: leer el flag de configuración antes de intentar la actualización:
```csharp
// Leer desde IConfiguration (disponible siempre como singleton)
var isHangfireInMemory = ctx.RequestServices
    .GetRequiredService<IConfiguration>()
    .GetValue<bool>("Hangfire:UseInMemoryStorage");

if (!isHangfireInMemory && newsCadenceChanged)
{
    var jobManager = ctx.RequestServices.GetRequiredService<IRecurringJobManager>();
    var cronExpr = $"*/{request.NewsCadenceMinutes} * * * *";
    jobManager.AddOrUpdate<NewsPipelineJob>(
        NewsPipelineSchedule.HourlyJobId,
        j => j.ExecuteAsync(CancellationToken.None),
        cronExpr,
        new RecurringJobOptions { TimeZone = MarketPipelineSchedule.GetMexicoTimeZone() });
}
```

Este mismo flag ya se usa en `ApiServiceExtensions.cs:133` y `Program.cs:44` — es el patrón establecido en el proyecto para separar el comportamiento de test vs producción.

`IConfiguration` se resuelve con `GetRequiredService` (siempre está registrado). `IRecurringJobManager` se usa solo cuando Hangfire tiene SQL storage real.

### Validación de NewsCadenceMinutes

Solo se aceptan valores que sean divisores exactos de 60 en el rango [15, 60]:
```csharp
private static bool IsValidCadence(int minutes)
    => minutes is >= 15 and <= 60 && 60 % minutes == 0;
// Válidos: 15, 20, 30, 60
// Inválidos: 10 (< 15), 45 (60 % 45 != 0), 0, 120
```

El cron `*/N * * * *` funciona correctamente para N ∈ {15, 20, 30, 60}.

### commission_factor — Épica 6 consumirá este valor

El `commission_factor` almacenado en `OperationalConfig` es el que la Épica 6 (Portafolio) usará en sus cálculos de `costo_total_compra`. Para esta historia, solo persistimos y exponemos el valor. **No crear endpoints de portafolio en esta historia.**

### avg_periods — idem Épica 6

`avg_periods` es el número de períodos históricos para métricas promedio. Épica 6 lo consumirá. Para esta historia, solo persistimos y exponemos.

### Schema `ops` — nuevo en la BD

La migración creará el schema `ops` (SQL Server: `CREATE SCHEMA ops`). EF Core lo hace automáticamente al tener `builder.ToTable("...", schema: "ops")`. Verificar en la migración generada que aparece el schema creation. Si no aparece automáticamente, agregar manualmente en el Up():
```csharp
migrationBuilder.EnsureSchema(name: "ops");
```

### Audit log — solo OperationalConfig en esta historia

El `ConfigAuditLog` solo captura cambios a los campos de `OperationalConfig`. Los cambios al blocklist (`BlocklistTerm`) y a `AiModeConfig` **no** se agregan a esta tabla. Esos módulos tienen su propio mecanismo de tracking (logging y `AiModeConfig.UpdatedBy/UpdatedAt`). La unificación en un audit log global es deuda futura.

### Startup Program.cs — lectura de cadencia

El try/catch en startup es esencial porque en el primer despliegue (antes de ejecutar las migraciones) la tabla `OperationalConfig` no existe. La excepción es atrapada y se usa el default. El `LogError` da visibilidad sin romper el arranque:
- Primer despliegue: `dotnet ef database update` → tabla creada → arranque limpio
- Despliegues posteriores: tabla existe → cadencia leída correctamente

### Import using en Program.cs

`Program.cs` es un top-level statement file; los `using` globales en `ApiServiceExtensions.cs` no aplican. Agregar:
```csharp
using Application.Ops;
```
en Program.cs si se inyecta `IOperationalConfigRepository` ahí.

### noUnusedLocals: true en tsconfig del Ops SPA

Cada import declarado en los nuevos archivos `.tsx`/`.ts` DEBE usarse. Revisar antes de compilar.

### Frontend — patrón de formulario con campos opcionales

Solo enviar al backend los campos que el usuario modificó. Usar estado local:
```typescript
const [editedFields, setEditedFields] = useState<UpdateOperationalConfigRequest>({})
// solo agregar a editedFields los campos que cambiaron respecto al valor inicial
```
El endpoint acepta null para campos no modificados (no los actualiza). El payload debe tener al menos un campo no-null.

### Archivos a crear/modificar

**Nuevos (backend domain):**
- `src/Server/Domain/Ops/OperationalConfig.cs`
- `src/Server/Domain/Ops/ConfigAuditLog.cs`

**Nuevos (backend infrastructure):**
- `src/Server/Infrastructure/Persistence/SqlServer/Configurations/Ops/OperationalConfigConfiguration.cs`
- `src/Server/Infrastructure/Persistence/SqlServer/Configurations/Ops/ConfigAuditLogConfiguration.cs`
- `src/Server/Infrastructure/Persistence/Repositories/Ops/OperationalConfigRepository.cs`
- `src/Server/Infrastructure/Persistence/Repositories/Ops/ConfigAuditLogRepository.cs`
- `src/Server/Infrastructure/Persistence/Migrations/XXX_P14_OperationalConfig_AuditLog.cs` (generada)

**Nuevos (backend application):**
- `src/Server/Application/Ops/IOperationalConfigRepository.cs`
- `src/Server/Application/Ops/IConfigAuditLogRepository.cs`

**Nuevos (backend contracts + endpoints):**
- `src/Server/SharedApiContracts/Ops/OperationalConfigDto.cs`
- `src/Server/SharedApiContracts/Ops/UpdateOperationalConfigRequest.cs`
- `src/Server/SharedApiContracts/Ops/ConfigAuditLogDto.cs`
- `src/Server/Api/Endpoints/Ops/OpsConfigEndpoints.cs`

**Modificados (backend):**
- `src/Server/Infrastructure/Persistence/SqlServer/AppDbContext.cs` — agregar DbSets
- `src/Server/Api/CompositionRoot/ApiServiceExtensions.cs` — registrar repos
- `src/Server/Api/Program.cs` — `app.MapOpsConfig()` + startup cadence read
- `src/Server/Api/Endpoints/Ops/OpsCatalogEndpoints.cs` — fix D5 (LogWarning en GetActor)
- `scripts/codegen/Api.json` + `src/Web/SharedApiClient/schema.d.ts` — regenerar

**Nuevos (frontend Ops):**
- `src/Web/Ops/src/api/configApi.ts`
- `src/Web/Ops/src/pages/ConfigPage.tsx`

**Modificados (frontend Ops):**
- `src/Web/Ops/src/main.tsx` — agregar ruta `/config`
- `src/Web/Ops/src/components/OpsShell.tsx` — agregar item nav "Configuración"

**Tests nuevos:**
- `tests/Unit/Infrastructure.Tests/Persistence/Repositories/OperationalConfigRepositoryTests.cs`
- `tests/Integration/Api.Tests/Ops/OpsConfigEndpointTests.cs`

### Referencias

- `[Source: epics.md#Historia-5.4]` — AC completos (commission_factor, avg_periods, blocklist, AI_MODE, cadencia)
- `[Source: epics.md#FR-40]` — "La sección Configuración debe permitir editar sin redeploy: commission_factor, avg_periods, blocklist, AI_MODE, cadencia"
- `[Source: src/Server/Domain/News/AiModeConfig.cs]` — patrón singleton a replicar en OperationalConfig
- `[Source: src/Server/Infrastructure/Persistence/Repositories/News/AiModeRepository.cs]` — patrón repositorio singleton con fallback
- `[Source: src/Server/Infrastructure/Persistence/SqlServer/Configurations/News/AiModeConfigConfiguration.cs]` — patrón EF mapping singleton con seed
- `[Source: src/Server/Api/Endpoints/Ops/OpsCatalogEndpoints.cs]` — patrón GetActor + ValidationProblem + endpoints Ops
- `[Source: src/Server/Api/Program.cs]` — estructura actual del registro de Hangfire jobs en startup
- `[Source: src/Server/Infrastructure/Jobs/News/NewsPipelineSchedule.cs]` — JobId y CronExpression actuales del pipeline de noticias
- `[Source: src/Server/Api/CompositionRoot/ApiServiceExtensions.cs:132-163]` — conditional Hangfire config (SQL vs InMemory)
- `[Source: _bmad-output/implementation-artifacts/5-3-gestion-del-catalogo-de-fibras-desde-ops.md#Dev Notes "Auditoría en MVP"]` — "No crear tabla de auditoría en 5-3 — eso es parte de la historia 5-4"
- `[Source: _bmad-output/implementation-artifacts/deferred-work.md#D5-from-5-3]` — "añadir LogWarning cuando actor cae a 'unknown' en siguiente historia de auditoría (historia 5-4)"
- `[Source: tests/Integration/Api.Tests/Ops/CatalogOpsEndpointTests.cs]` — patrón de test de integración Ops más reciente
- `[Source: _bmad-output/planning-artifacts/convenciones-fibradis.md#EF Core nunca Task.WhenAll]` — queries secuenciales siempre

---

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- `dotnet test tests/Unit/Infrastructure.Tests/Infrastructure.Tests.csproj --filter OperationalConfigRepositoryTests`
- `dotnet test tests/Integration/Api.Tests/Api.Tests.csproj --filter OpsConfigEndpointTests -m:1`
- `dotnet ef migrations add AddOpsConfigAndAuditLog --project src/Server/Infrastructure --startup-project src/Server/Api --configuration Release`
- `dotnet ef database update --project src/Server/Infrastructure --startup-project src/Server/Api --configuration Release`
- `npm run codegen:api`
- `npm run build --workspace=src/Web/Main`
- `npm run build --workspace=src/Web/Ops`
- `dotnet test tests/Unit/Infrastructure.Tests/Infrastructure.Tests.csproj -m:1`
- `dotnet test tests/Integration/Api.Tests/Api.Tests.csproj -m:1`

### Completion Notes List

- Se implementó `OperationalConfig` singleton en schema `ops`, con auditoría por campo en `ConfigAuditLog`, repositorios dedicados y seed inicial `commission_factor=0.006`, `avg_periods=4`, `news_cadence_minutes=60`.
- Se agregaron `GET /api/v1/ops/config`, `PUT /api/v1/ops/config` y `GET /api/v1/ops/audit-log`, protegidos con `AdminOps`, validaciones de rango y actualización inmediata del cron de noticias cuando Hangfire usa SQL real.
- `Program.cs` ahora relee `NewsCadenceMinutes` desde BD al arranque y se añadió un guard de proceso para que la generación build-time de OpenAPI no intente leer tablas antes de migrar la BD.
- Se aplicó el deferred D5 de 5-3: `OpsCatalogEndpoints` y `OpsConfigEndpoints` registran `LogWarning` cuando el actor cae a `"unknown"`.
- Se añadió la pantalla `/config` en el Ops SPA con formulario parcial, mensaje de guardado, invalidez de queries y tabla de auditoría usando el cliente OpenAPI regenerado.
- Evidencia de pruebas: `OperationalConfigRepositoryTests` 7/7, `OpsConfigEndpointTests` 14/14, `Infrastructure.Tests` 130/130, `Api.Tests` 169/169, `npm run build --workspace=src/Web/Main` OK y `npm run build --workspace=src/Web/Ops` OK.

### File List

- `src/Server/Domain/Ops/OperationalConfig.cs`
- `src/Server/Domain/Ops/ConfigAuditLog.cs`
- `src/Server/Application/Ops/IOperationalConfigRepository.cs`
- `src/Server/Application/Ops/IConfigAuditLogRepository.cs`
- `src/Server/Infrastructure/Persistence/SqlServer/Configurations/Ops/OperationalConfigConfiguration.cs`
- `src/Server/Infrastructure/Persistence/SqlServer/Configurations/Ops/ConfigAuditLogConfiguration.cs`
- `src/Server/Infrastructure/Persistence/Repositories/Ops/OperationalConfigRepository.cs`
- `src/Server/Infrastructure/Persistence/Repositories/Ops/ConfigAuditLogRepository.cs`
- `src/Server/Infrastructure/Persistence/SqlServer/AppDbContext.cs`
- `src/Server/Infrastructure/Persistence/Migrations/20260525170030_AddOpsConfigAndAuditLog.cs`
- `src/Server/Infrastructure/Persistence/Migrations/20260525170030_AddOpsConfigAndAuditLog.Designer.cs`
- `src/Server/Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`
- `src/Server/SharedApiContracts/Ops/OperationalConfigDto.cs`
- `src/Server/SharedApiContracts/Ops/UpdateOperationalConfigRequest.cs`
- `src/Server/SharedApiContracts/Ops/ConfigAuditLogDto.cs`
- `src/Server/Api/Endpoints/Ops/OpsConfigEndpoints.cs`
- `src/Server/Api/Endpoints/Ops/OpsCatalogEndpoints.cs`
- `src/Server/Api/CompositionRoot/ApiServiceExtensions.cs`
- `src/Server/Api/Program.cs`
- `src/Server/Api/Api.csproj`
- `scripts/codegen/Api.json`
- `src/Web/SharedApiClient/schema.d.ts`
- `src/Web/Ops/src/api/configApi.ts`
- `src/Web/Ops/src/pages/ConfigPage.tsx`
- `src/Web/Ops/src/main.tsx`
- `src/Web/Ops/src/components/OpsShell.tsx`
- `tests/Unit/Infrastructure.Tests/Persistence/Repositories/OperationalConfigRepositoryTests.cs`
- `tests/Integration/Api.Tests/Ops/OpsConfigEndpointTests.cs`

### Change Log

- 2026-05-25: Implementada la historia 5.4 end-to-end: configuración operativa persistente + audit log Ops + reschedule de noticias + pantalla `/ops/config` + pruebas y builds en verde.

---

## Senior Developer Review (AI)

### Review Findings

- [x] [Review][Patch] Tests 403 faltantes: `PUT /ops/config` y `GET /ops/audit-log` con rol `User` no tienen cobertura — `_userClient` está disponible pero no se usa para estos dos endpoints. Agregar `PutConfig_WithUserRole_Returns403` y `GetAuditLog_WithUserRole_Returns403`. [`tests/Integration/Api.Tests/Ops/OpsConfigEndpointTests.cs`]
- [x] [Review][Patch] Startup `!= 60` guard causa divergencia de cron string — si el usuario revertió vía PUT a 60 (cron `*/60 * * * *`), en el siguiente arranque el startup usa el cron estático en lugar del valor de BD. El comportamiento es equivalente (ambos son horarios) pero viola la letra de AC4 ("la cadencia se lee desde la BD, no del valor hardcodeado"). Fix trivial: quitar la guarda `!= 60` y siempre aplicar el valor de BD; usar `opConfig.NewsCadenceMinutes == 60 ? "0 * * * *" : $"*/{opConfig.NewsCadenceMinutes} * * * *"`. [`src/Server/Api/Program.cs`]
- [x] [Review][Patch] `GetAuditLog_AfterChange_ReturnsEntries` no verifica el orden descendente — AC5 exige "lista ordenada por timestamp descendente". Agregar una segunda entrada de auditoría y un `Assert` que verifique `body[0].ChangedAt >= body[1].ChangedAt`. [`tests/Integration/Api.Tests/Ops/OpsConfigEndpointTests.cs`]
- [x] [Review][Defer] Sin transacción entre `SaveChangesAsync` y `Hangfire.AddOrUpdate` — si Hangfire falla post-commit, la BD tiene la nueva cadencia pero el job mantiene el schedule anterior; el próximo arranque lo corrige. [`src/Server/Api/Endpoints/Ops/OpsConfigEndpoints.cs`] — deferred, eventual consistency via startup
- [x] [Review][Defer] Race condition teórico: dos PUTs concurrentes pueden calcular `cadenceChanged = true` de forma independiente y ambos llamar a `Hangfire.AddOrUpdate` — `AddOrUpdate` es idempotente con el mismo cron; admin-only, probabilidad despreciable. [`src/Server/Api/Endpoints/Ops/OpsConfigEndpoints.cs`] — deferred, pre-existing
- [x] [Review][Defer] Validaciones de negocio (`commissionFactor > 0 && <= 0.1`, `avgPeriods` 1–20) solo en capa HTTP — callers internos del repositorio las bypasean; patrón ya aceptado en `AiModeConfig` y demás repositorios del proyecto. [`src/Server/Infrastructure/Persistence/Repositories/Ops/OperationalConfigRepository.cs`] — deferred, pre-existing pattern
- [x] [Review][Defer] `FIBRADIS_SKIP_STARTUP_DB_READS` env var es belt-and-suspenders sobre el try/catch existente — harmless; el try/catch ya cubre el caso de tabla inexistente en primer despliegue. [`src/Server/Api/Program.cs`] — deferred, over-engineering accepted
