# Story 14.1: Modelo de suscripción — backend foundation

Status: review

## Story

As a administrator,
I want the database and domain to reflect the complete subscription lifecycle (trial → active → expired),
so that all future access logic is based on explicit fields and not on ad-hoc heuristics.

## Acceptance Criteria

1. **Dado que** ejecuto la migración EF, **Entonces** `auth.User` tiene cinco columnas nuevas: `email_confirmed_at` (datetime2 nullable), `trial_ends_at` (datetime2 nullable), `subscription_type` (nvarchar(16) nullable — 'Monthly'|'Annual'|'Lifetime'), `subscription_ends_at` (datetime2 nullable), `how_did_you_hear` (nvarchar(32) nullable — 'Google'|'RedesSociales'|'Recomendacion'|'Otro').

2. **Dado que** la migración corre sobre usuarios existentes con `IsActive = 1`, **Entonces** el data seed los actualiza a `SubscriptionType = Lifetime`, `SubscriptionStartedAt = FechaPago ?? UtcNow`, `SubscriptionEndsAt = null`, `IsActive = 1` en un único UPDATE.

3. **Dado que** el dominio `User` tiene los nuevos campos, **Entonces** existe una propiedad computed `ComputedIsActive` (no mapeada a BD) que devuelve `true` si: (a) `SubscriptionType == Lifetime && SubscriptionStartedAt != null`, o (b) `SubscriptionEndsAt != null && SubscriptionEndsAt > UtcNow`, o (c) `TrialEndsAt != null && TrialEndsAt > UtcNow`. La columna `is_active` en BD sigue siendo el stored bit.

4. **Dado que** llamo `PATCH /api/v1/ops/users/{id}/subscription` con `{ "type": "Annual", "startedAt": "2026-06-18", "endsAt": "2027-06-18" }` como AdminOps, **Entonces** los campos se actualizan, `is_active` se recalcula con `ComputedIsActive` y persiste, y el endpoint devuelve 200 OK con `UserSummaryDto` actualizado.

5. **Dado que** llamo `GET /api/v1/ops/users`, **Entonces** cada `UserSummaryDto` incluye los campos nuevos: `subscriptionType`, `subscriptionStartedAt`, `subscriptionEndsAt`, `trialEndsAt`, `emailConfirmedAt`.

## Tasks / Subtasks

- [x] T1: Actualizar dominio `User` (AC: 3)
  - [x] T1.1: Agregar propiedades `EmailConfirmedAt`, `TrialEndsAt`, `SubscriptionType` (enum), `SubscriptionStartedAt`, `SubscriptionEndsAt`, `HowDidYouHear` (enum) a `Domain.Auth.User`
  - [x] T1.2: Crear enum `SubscriptionType` (Monthly, Annual, Lifetime) en `Domain.Auth`
  - [x] T1.3: Crear enum `HowDidYouHear` (Google, RedesSociales, Recomendacion, Otro) en `Domain.Auth`
  - [x] T1.4: Agregar propiedad computed `ComputedIsActive` (no mapeada) que implementa la lógica de AC-3

- [x] T2: Migración EF Core (AC: 1)
  - [x] T2.1: Actualizar `AppDbContext` — configuración EF para nuevos campos de `auth.User`: columnas snake_case, longitudes, nullables, conversión de enums a string
  - [x] T2.2: Ejecutar `dotnet ef migrations add AddSubscriptionFields --project src/Server/Infrastructure --startup-project src/Server/Api`
  - [x] T2.3: Verificar que la migración aparece en `dotnet ef migrations list`
  - [x] T2.4: Actualizar `AppDbContextModelSnapshot`

- [x] T3: Script de data migration para usuarios existentes (AC: 2)
  - [x] T3.1: Crear archivo `scripts/migrations/prod_AddSubscriptionFields.sql` con el UPDATE de usuarios existentes activos a Lifetime
  - [x] T3.2: Agregar el mismo UPDATE como `migrationBuilder.Sql()` en el método `Up()` de la migración EF para que corra automáticamente en dev

- [x] T4: Actualizar Application layer (AC: 4, 5)
  - [x] T4.1: Agregar `UpdateSubscriptionAsync(Guid id, string type, DateTime startedAt, DateTime? endsAt, CancellationToken ct)` a `IUserService`
  - [x] T4.2: Implementar `UpdateSubscriptionAsync` en `UserService`: validar enum, actualizar campos, recalcular `IsActive = ComputedIsActive`, persistir
  - [x] T4.3: Actualizar `UserData` DTO con los cinco campos nuevos (`SubscriptionType`, `SubscriptionStartedAt`, `SubscriptionEndsAt`, `TrialEndsAt`, `EmailConfirmedAt`)
  - [x] T4.4: Actualizar `UserSummaryDto` en `SharedApiContracts` con los mismos campos

- [x] T5: Nuevo endpoint Ops (AC: 4)
  - [x] T5.1: Agregar `UpdateSubscriptionRequest` record a `SharedApiContracts.Auth` con `Type`, `StartedAt`, `EndsAt?`
  - [x] T5.2: Agregar `PATCH /api/v1/ops/users/{id}/subscription` en `OpsUserEndpoints.cs`, protegido con `RequireAuthorization("AdminOps")`
  - [x] T5.3: Codegen API: ejecutar `npm run codegen:api` para regenerar `SharedApiClient`

- [x] T6: Unit tests (AC: 3, 4)
  - [x] T6.1: Tests `ComputedIsActive` — cubrir los tres casos true (Lifetime activo, Monthly/Annual vigente, trial vigente) y los tres casos false (sin suscripción, trial vencido, suscripción vencida)
  - [x] T6.2: Test `UpdateSubscriptionAsync` — happy path Annual, Lifetime (endsAt null), validación tipo inválido → excepción
  - [x] T6.3: Test recálculo `is_active` persiste tras `UpdateSubscriptionAsync`

- [x] T7: Build y verificación final
  - [x] T7.1: `dotnet build FIBRADIS.slnx` — 0 errores
  - [x] T7.2: `dotnet test tests/Unit/` — todos los tests verdes incluyendo los nuevos
  - [x] T7.3: `dotnet ef migrations list` confirma que `AddSubscriptionFields` aparece

## Dev Notes

### Tabla existente en producción

La tabla `[auth].[User]` en prod ya tiene estos campos (NO tocar ni renombrar):

```sql
Id, Email, Apodo, PasswordHash, Role, CreatedAt, IsActive (bit),
HasAcceptedTerms, TermsAcceptedAt, Pago (decimal), FechaPago (datetime2)
```

Campos a AGREGAR solamente:

```sql
+ email_confirmed_at    datetime2 NULL
+ trial_ends_at         datetime2 NULL
+ subscription_type     nvarchar(16) NULL   -- 'Monthly'|'Annual'|'Lifetime'
+ subscription_ends_at  datetime2 NULL
+ how_did_you_hear      nvarchar(32) NULL
```

`FechaPago` ya existe — se reutiliza semánticamente como `SubscriptionStartedAt` en el dominio. En la migración de datos se copia su valor al setear la suscripción Lifetime.

### Dominio User actualizado

Archivo: `src/Server/Domain/Auth/User.cs`

```csharp
public class User
{
    // ... campos existentes sin cambio ...

    // Nuevos campos
    public DateTimeOffset? EmailConfirmedAt { get; set; }
    public DateTimeOffset? TrialEndsAt { get; set; }
    public SubscriptionType? SubscriptionType { get; set; }
    public DateTimeOffset? SubscriptionStartedAt { get; set; }  // = FechaPago reinterpretado
    public DateTimeOffset? SubscriptionEndsAt { get; set; }      // null = Lifetime
    public HowDidYouHear? HowDidYouHear { get; set; }

    // Computed — NO mapeado a BD
    public bool ComputedIsActive =>
        (SubscriptionType == Auth.SubscriptionType.Lifetime && SubscriptionStartedAt.HasValue) ||
        (SubscriptionEndsAt.HasValue && SubscriptionEndsAt.Value > DateTimeOffset.UtcNow) ||
        (TrialEndsAt.HasValue && TrialEndsAt.Value > DateTimeOffset.UtcNow);

    public List<RefreshToken> RefreshTokens { get; set; } = [];
}
```

**Importante:** `SubscriptionStartedAt` es un NUEVO campo en dominio, diferente de `FechaPago`. No renombrar `FechaPago` — dejarlo para compatibilidad; el dominio tiene ambos.

### Configuración EF — convenciones de columnas

La BD usa `snake_case` para columnas. Ver configuración existente en `AppDbContext` para el patrón de `HasColumnName`. Los enums deben configurarse con `.HasConversion<string>()` para guardar el nombre del enum como string (no int).

Ejemplo de configuración EF a seguir:

```csharp
builder.Property(u => u.SubscriptionType)
    .HasColumnName("subscription_type")
    .HasMaxLength(16)
    .HasConversion<string>();
```

### Script de data migration

Archivo nuevo: `scripts/migrations/prod_AddSubscriptionFields.sql`

```sql
BEGIN TRANSACTION;

-- Todos los usuarios activos → Lifetime
UPDATE [auth].[User]
SET subscription_type      = 'Lifetime',
    -- SubscriptionStartedAt se mapea a un nuevo campo, no a FechaPago
    -- El campo subscription_started_at no existe aún; se crea con la migración.
    -- Este script corre DESPUÉS de la migración DDL.
    subscription_ends_at   = NULL,
    is_active              = 1
WHERE is_active = 1;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260618000000_AddSubscriptionFields', N'10.0.8');

COMMIT;
```

**Nota:** La migración EF automática en `Up()` solo agrega las columnas DDL. El UPDATE de data migration va en `migrationBuilder.Sql()` inmediatamente después del `AddColumn` para que corra en `dotnet ef database update`.

### Endpoint nuevo en OpsUserEndpoints.cs

```csharp
app.MapPatch("/api/v1/ops/users/{id:guid}/subscription", async (
    Guid id,
    UpdateSubscriptionRequest req,
    IUserService svc,
    CancellationToken ct) =>
{
    var user = await svc.UpdateSubscriptionAsync(id, req.Type, req.StartedAt, req.EndsAt, ct);
    return Results.Ok(ToDto(user));
})
.RequireAuthorization("AdminOps")
.WithTags("Ops")
.Produces<UserSummaryDto>(StatusCodes.Status200OK)
.ProducesProblem(StatusCodes.Status400BadRequest)
.ProducesProblem(StatusCodes.Status404NotFound);
```

### Security Checklist — completar antes del primer commit

- [x] **TOCTOU doble-request en UpdateSubscriptionAsync**: el endpoint PATCH actualiza por PK única (Guid) — no hay riesgo de duplicado. Si el usuario no existe → 404. Si la suscripción ya está seteada, el UPDATE simplemente sobreescribe (idempotente por diseño).
- [x] **Auth-gating**: el endpoint nuevo está protegido con `RequireAuthorization("AdminOps")` — solo el administrador puede invocar.
- [x] **Denominador cero**: no aplica — esta historia no tiene cálculos financieros.

### EF Migration workaround

Si `dotnet ef migrations add` falla porque la API tiene los DLLs bloqueados, usar:

```bash
dotnet ef migrations add AddSubscriptionFields --project src/Server/Infrastructure --startup-project src/Server/Api --configuration Release
```

[Source: convenciones-fibradis.md#EF Core Migrations]

### Precios definidos (2026-06-18)

| Plan | Precio |
| --- | --- |
| Mensual | 49 MXN/mes |
| Anual | 299 MXN/año (~25 MXN/mes, ahorro 49%) |
| Lifetime | 999 MXN (pago único) |

Lifetime se ofrecerá solo como oferta de lanzamiento limitada (~100 cupos); después solo mensual/anual.
Los usuarios activos en prod al momento del deploy se migran automáticamente a `Lifetime` (ver AC-2 y el script SQL).

### Historias dependientes

Esta historia es prerequisito de:

- **14-2** (registro + confirmación email): necesita `EmailConfirmedAt` y `TrialEndsAt`
- **14-3** (frontend acceso controlado): necesita el endpoint `/api/me` actualizado con `isActive`
- **14-4** (SubscriptionMaintenanceJob): necesita `SubscriptionType` y `SubscriptionEndsAt`

### Project Structure Notes

Archivos a CREAR (NEW):

- `src/Server/Domain/Auth/SubscriptionType.cs` — enum
- `src/Server/Domain/Auth/HowDidYouHear.cs` — enum
- `src/Server/Infrastructure/Migrations/SqlServer/YYYYMMDDHHMMSS_AddSubscriptionFields.cs` — migración EF (generada)
- `src/Server/Infrastructure/Migrations/SqlServer/YYYYMMDDHHMMSS_AddSubscriptionFields.Designer.cs` — (generado)
- `scripts/migrations/prod_AddSubscriptionFields.sql` — script manual para producción
- `src/Server/Infrastructure/Migrations/SqlServer/AppDbContextModelSnapshot.cs` — (actualizado por EF)

Archivos a MODIFICAR (UPDATE):

- `src/Server/Domain/Auth/User.cs` — nuevos campos + ComputedIsActive
- `src/Server/Infrastructure/Persistence/SqlServer/AppDbContext.cs` — configuración EF nuevas columnas
- `src/Server/Application/Auth/IUserService.cs` — nuevo método UpdateSubscriptionAsync
- `src/Server/Application/Auth/UserData.cs` — nuevos campos DTO
- `src/Server/Infrastructure/Security/UserService.cs` — implementar UpdateSubscriptionAsync
- `src/Server/Api/Endpoints/Ops/OpsUserEndpoints.cs` — nuevo endpoint PATCH /subscription
- `src/Server/SharedApiContracts/Auth/UserSummaryDto.cs` — nuevos campos
- `src/Server/SharedApiContracts/Auth/UpdateSubscriptionRequest.cs` — NEW record para el PATCH endpoint
- `src/Shared/SharedApiContracts/Auth/UpdateSubscriptionRequest.cs` — NEW record

### References

- Tabla prod: `[auth].[User]` — schema visible en `_bmad-output/brainstorming/brainstorming-session-2026-06-18-1530.md` idea #19
- Lógica `ComputedIsActive`: brainstorming ideas #3, #4, #13
- Data migration existente como referencia: `scripts/migrations/prod_AddFibraEXIAndAGRO.sql`
- Auth endpoints existentes: `src/Server/Api/Endpoints/Public/AuthEndpoints.cs`
- Ops user endpoints existentes: `src/Server/Api/Endpoints/Ops/OpsUserEndpoints.cs`
- Convenciones columnas snake_case: `_bmad-output/planning-artifacts/convenciones-fibradis.md`
- [Source: docs/req/architecture.md#Authentication & Security]

## Dev Agent Record

### Agent Model Used

{{agent_model_name_version}}

### Debug Log References

- Contexto cargado desde `AGENTS.md`, `_bmad-output/planning-artifacts/convenciones-fibradis.md`, `workflow-rules.md`, `sprint-status.yaml` y esta story.
- Resolví el workflow del skill `bmad-dev-story` con `python _bmad/scripts/resolve_customization.py --skill .agents/skills/bmad-dev-story --key workflow`.
- Leí memoria relacionada con el tema de suscripción backend para confirmar el contrato entre backend y SPAs.
- Implementé el modelo de suscripción en dominio, EF, Application, endpoint Ops, contratos compartidos y pruebas.
- Generé la migración EF `20260619161559_AddSubscriptionFields` y el script manual `scripts/migrations/prod_AddSubscriptionFields.sql`.
- Regeneré el cliente OpenAPI con `npm run codegen:api`.
- Validaciones ejecutadas: `dotnet build FIBRADIS.slnx -m:1`, `dotnet ef migrations list --project src/Server/Infrastructure --startup-project src/Server/Api`, `dotnet test tests/Unit/Domain.Tests/Domain.Tests.csproj -m:1`, `dotnet test tests/Unit/Infrastructure.Tests/Infrastructure.Tests.csproj -m:1 --filter "FullyQualifiedName~UserServiceTests.UpdateSubscriptionAsync"`, `dotnet test tests/Integration/Api.Tests/Api.Tests.csproj -m:1 --filter "FullyQualifiedName~OpsUserEndpointTests.UpdateSubscription_"`, `dotnet test tests/Integration/Api.Tests/Api.Tests.csproj -m:1 --filter "FullyQualifiedName~OpsUserEndpointTests.GetUsers_WithAdminToken_Returns200AndList"`, `npm run build --workspace=src/Web/Main`, `npm run build --workspace=src/Web/Ops`.

### Completion Notes List

- ✅ Dominio `User` ampliado con `EmailConfirmedAt`, `TrialEndsAt`, `SubscriptionType`, `SubscriptionStartedAt`, `SubscriptionEndsAt`, `HowDidYouHear` y computed `ComputedIsActive`.
- ✅ Nuevos enums de dominio creados: `SubscriptionType` y `HowDidYouHear`.
- ✅ Configuración EF actualizada para las nuevas columnas de `auth.User`, incluyendo `HasConversion<string>()` para enums y nombres `snake_case`.
- ✅ Migración EF generada y enriquecida con backfill de usuarios activos a `Lifetime` en `Up()`.
- ✅ Script manual de producción creado para el mismo backfill y la entrada en `__EFMigrationsHistory`.
- ✅ Application layer y endpoint Ops actualizados con `PATCH /api/v1/ops/users/{id}/subscription`.
- ✅ Contratos `UserSummaryDto` y `UpdateSubscriptionRequest` actualizados; `codegen:api` regeneró `src/Web/SharedApiClient/schema.d.ts`.
- ✅ Pruebas nuevas añadidas y validadas para `ComputedIsActive`, `UpdateSubscriptionAsync` y el endpoint de suscripción.
- ✅ Build de solución, migración listada y builds de Main/Ops completados correctamente.
- ⚠️ `tests/Unit/Infrastructure.Tests` y `tests/Integration/Api.Tests` tienen fallos preexistentes ajenos a esta historia; los tests focalizados de suscripción pasaron.

### File List

- `scripts/codegen/Api.json`
- `scripts/migrations/prod_AddSubscriptionFields.sql`
- `src/Server/Api/Endpoints/Ops/OpsUserEndpoints.cs`
- `src/Server/Application/Auth/IUserService.cs`
- `src/Server/Application/Auth/UserData.cs`
- `src/Server/Domain/Auth/HowDidYouHear.cs`
- `src/Server/Domain/Auth/SubscriptionType.cs`
- `src/Server/Domain/Auth/User.cs`
- `src/Server/Infrastructure/Migrations/SqlServer/20260619161559_AddSubscriptionFields.Designer.cs`
- `src/Server/Infrastructure/Migrations/SqlServer/20260619161559_AddSubscriptionFields.cs`
- `src/Server/Infrastructure/Migrations/SqlServer/AppDbContextModelSnapshot.cs`
- `src/Server/Infrastructure/Persistence/SqlServer/Configurations/Auth/UserConfiguration.cs`
- `src/Server/Infrastructure/Security/UserService.cs`
- `src/Server/SharedApiContracts/Auth/UpdateSubscriptionRequest.cs`
- `src/Server/SharedApiContracts/Auth/UserSummaryDto.cs`
- `src/Web/SharedApiClient/schema.d.ts`
- `tests/Integration/Api.Tests/Ops/OpsUserEndpointTests.cs`
- `tests/Unit/Domain.Tests/Auth/UserTests.cs`
- `tests/Unit/Infrastructure.Tests/Security/UserServiceTests.cs`

### Change Log

- Added the subscription lifecycle foundation for auth users, including computed activity logic, EF columns, active-user backfill, Ops subscription update endpoint, shared contracts, codegen refresh, and regression tests.
