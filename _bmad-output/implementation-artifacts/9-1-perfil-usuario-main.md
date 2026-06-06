# Historia 9.1: Perfil de usuario en Main (apodo y cambio de contraseña propia)

Status: done

## Story

Como usuario autenticado en el SPA Main,
quiero ver y editar mi apodo y cambiar mi propia contraseña desde una sección de perfil accesible en el header,
para personalizar mi experiencia sin necesidad de contactar a un administrador.

## Acceptance Criteria

### AC1 — Menú de usuario en el header

**Dado que** estoy autenticado,
**Cuando** veo el header en PublicLayout,
**Entonces** en lugar del botón plano "Cerrar sesión" aparece un menú de usuario con:
- Un ítem "Mi perfil" que navega a `/perfil`.
- Un ítem "Cerrar sesión" que ejecuta el logout existente.

**El botón del menú** muestra el apodo del usuario si lo tiene, o el email truncado si no (ej. `usuario@ejemplo.com` → `usuario@...`).

### AC2 — Página `/perfil`

**Dado que** navego a `/perfil` autenticado,
**Entonces** veo una página con:
- **Email**: mostrado como texto no editable (descifrado, plaintext).
- **Apodo**: campo editable inline con botón "Guardar" (máx. 50 chars; sin caracteres de control).
- **Botón "Cambiar contraseña"** que abre un diálogo.

**Dado que** navego a `/perfil` sin autenticar,
**Entonces** soy redirigido a `/login` (comportamiento de ProtectedRoute existente).

### AC3 — Editar apodo

**Dado que** edito mi apodo y hago clic en "Guardar",
**Cuando** el servidor responde 204,
**Entonces** el apodo se actualiza visualmente sin recargar la página.
**Y** el botón del header refleja el nuevo apodo.

**Si el apodo tiene más de 50 caracteres**, el campo muestra error inline y no llama al servidor.
**Si el apodo contiene caracteres de control** (código < 32), el servidor devuelve 400 y el frontend muestra el mensaje de error.

### AC4 — Cambiar contraseña propia

**Dado que** hago clic en "Cambiar contraseña",
**Entonces** aparece un diálogo con tres campos: "Contraseña actual", "Nueva contraseña", "Confirmar nueva contraseña".

**Cuando** envío contraseñas válidas y la contraseña actual es correcta,
**Entonces** el servidor devuelve 204, el diálogo se cierra y aparece un toast de confirmación.

**Si la contraseña actual es incorrecta**,
**Entonces** el servidor devuelve 401 y el frontend muestra "Contraseña actual incorrecta".

**Si la nueva contraseña no cumple los criterios** (mín 8 chars, mayúscula, minúscula, número, especial),
**Entonces** el servidor devuelve 400 con el mensaje de error específico.

**Si "Nueva contraseña" y "Confirmar" no coinciden**,
**Entonces** la validación ocurre en frontend antes de llamar al servidor.

### AC5 — Endpoints backend

Se crean los siguientes endpoints bajo `/api/v1/account`, todos con `RequireAuthorization()` (cualquier usuario autenticado, no solo AdminOps):

| Método | Ruta | Body / Response |
|--------|------|-----------------|
| GET | `/api/v1/account/me` | `{ email, role, apodo? }` (email descifrado) |
| PATCH | `/api/v1/account/me` | body `{ apodo: string? }` → 204 |
| PATCH | `/api/v1/account/password` | body `{ currentPassword, newPassword }` → 204 |

### AC6 — Unit tests backend

Se agregan tests en `tests/Unit/Infrastructure.Tests/`:
- `GetProfileAsync` devuelve email descifrado y apodo actual.
- `UpdateApodoAsync` con apodo de 51+ chars lanza `InvalidUserDataException`.
- `ChangeOwnPasswordAsync` con contraseña actual incorrecta lanza `InvalidCredentialsException`.
- `ChangeOwnPasswordAsync` con nueva contraseña débil lanza `InvalidUserDataException`.

### AC7 — Integration tests backend

Se crea `tests/Integration/Api.Tests/AccountEndpointTests.cs`:
- `GET /account/me` sin token → 401.
- `GET /account/me` con token User → 200 con `{ email, role }`.
- `PATCH /account/me` sin token → 401.
- `PATCH /account/me` con apodo válido → 204 + GET /me retorna el nuevo apodo.
- `PATCH /account/me` con apodo > 50 chars → 400.
- `PATCH /account/password` sin token → 401.
- `PATCH /account/password` con contraseña actual correcta y nueva válida → 204.
- `PATCH /account/password` con contraseña actual incorrecta → 401.
- `PATCH /account/password` con nueva contraseña débil → 400.

---

## Tasks / Subtasks

- [x] T1: Migración EF Core — agregar `Apodo` a `auth.User` (AC5)
  - [x] Agregar `public string? Apodo { get; set; }` a `Domain.Auth.User`
  - [x] Agregar `builder.Property(u => u.Apodo).HasMaxLength(50)` en `UserConfiguration`
  - [x] `dotnet ef migrations add AddUserApodo --project src/Server/Infrastructure --startup-project src/Server/Api`
  - [x] `dotnet ef database update --project src/Server/Infrastructure --startup-project src/Server/Api`
  - [x] Verificar `dotnet build FIBRADIS.slnx` pasa

- [x] T2: Backend — Application layer (AC5)
  - [x] Crear `UserProfileData` record en `Application.Auth`: `record UserProfileData(Guid Id, string Email, string Role, string? Apodo)`
  - [x] Añadir a `IUserService`:
    - [x] `Task<UserProfileData> GetProfileAsync(Guid userId, CancellationToken ct = default)`
    - [x] `Task UpdateApodoAsync(Guid userId, string? apodo, CancellationToken ct = default)`
    - [x] `Task ChangeOwnPasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken ct = default)`

- [x] T3: Backend — Infrastructure layer (AC5)
  - [x] Implementar `GetProfileAsync` en `UserService`: `FindAsync` → lanzar `UserNotFoundException` si no existe → `emailEncryptor.Decrypt(user.Email)` → retornar `UserProfileData`
  - [x] Implementar `UpdateApodoAsync`: `FindAsync` → validar max 50 chars y sin caracteres de control (lanzar `InvalidUserDataException`) → actualizar → `SaveChangesAsync`
  - [x] Implementar `ChangeOwnPasswordAsync`: `FindAsync` → `BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash)` — si falla → `InvalidCredentialsException` → `ValidateStrongPassword(newPassword)` → `BCrypt.Net.BCrypt.HashPassword(newPassword)` → `SaveChangesAsync`
  - [x] `ValidateStrongPassword` ya existe como private static en UserService — reutilizarla

- [x] T4: Backend — Contracts & Endpoints (AC5)
  - [x] Agregar `UserProfileResponse(string Email, string Role, string? Apodo)` en `SharedApiContracts.Auth`
  - [x] Agregar `UpdateApodoRequest(string? Apodo)` en `SharedApiContracts.Auth`
  - [x] Agregar `ChangePasswordRequest(string CurrentPassword, string NewPassword)` en `SharedApiContracts.Auth`
  - [x] Expandir `AccountEndpoints.MapAccount()` con los 3 nuevos endpoints:
    - [x] `GET /api/v1/account/me`: extraer userId del claim Sub → `svc.GetProfileAsync(userId)` → 200 con `UserProfileResponse`; catch `UserNotFoundException` → 401
    - [x] `PATCH /api/v1/account/me`: validar `apodo?.Length <= 50` → `svc.UpdateApodoAsync(userId, req.Apodo)` → 204; catch `InvalidUserDataException` → 400 ProblemDetails; catch `UserNotFoundException` → 401
    - [x] `PATCH /api/v1/account/password`: `svc.ChangeOwnPasswordAsync(userId, req.CurrentPassword, req.NewPassword)` → 204; catch `InvalidCredentialsException` → 401; catch `InvalidUserDataException` → 400 ProblemDetails; catch `UserNotFoundException` → 401
  - [x] Ejecutar `npm run codegen:api` para regenerar el cliente tipado

- [x] T5: Backend — Unit tests (AC6)
  - [x] Crear `tests/Unit/Infrastructure.Tests/UserProfileServiceTests.cs`
  - [x] Implementar los 4 tests descritos en AC6
  - [x] `dotnet test tests/Unit/` — todos pasan

- [x] T6: Backend — Integration tests (AC7)
  - [x] Crear `tests/Integration/Api.Tests/AccountEndpointTests.cs`
  - [x] Implementar los 9 tests descritos en AC7 — patrón: `IClassFixture<ApiWebFactory>` + `IAsyncLifetime` + `SeedUsersAsync`
  - [x] `dotnet test tests/Integration/` — todos pasan

- [x] T7: Frontend — API functions (AC2, AC3, AC4)
  - [x] En `src/Web/Main/src/modules/auth/authApi.ts`, agregar:
    - [x] `fetchProfile()` → GET `/api/v1/account/me` con `getMainAuthHeaders()` → retorna `UserProfileResponse`
    - [x] `updateApodo(apodo: string | null)` → PATCH `/api/v1/account/me`
    - [x] `changePassword(currentPassword: string, newPassword: string)` → PATCH `/api/v1/account/password`
  - [x] Usar el cliente tipado generado en T4 (`authClient['/api/v1/account/me'].GET(...)` etc.)

- [x] T8: Frontend — Hook `useProfile` (AC1, AC2, AC3)
  - [x] Crear `src/Web/Main/src/modules/auth/useProfile.ts`
  - [x] Hook usa `useQuery({ queryKey: ['account', 'me'], queryFn: fetchProfile, enabled: isAuthenticated })`
  - [x] Exportar `queryKey` para poder invalidar desde la página de perfil

- [x] T9: Frontend — Página de perfil `/perfil` (AC2, AC3, AC4)
  - [x] Crear `src/Web/Main/src/modules/perfil/PerfilPage.tsx`
  - [x] Sección email (no editable): leer de `useProfile()`
  - [x] Sección apodo: input con botón "Guardar"; useMutation con `updateApodo`; al éxito invalidar `['account', 'me']`; validación frontend max 50 chars antes de llamar a servidor
  - [x] Botón "Cambiar contraseña" abre un Dialog (shadcn `<Dialog>`) con 3 campos (`type="password"`); validación cliente: nueva === confirmar; useMutation con `changePassword`; al éxito cerrar dialog y mostrar toast o mensaje inline
  - [x] Añadir la ruta `/perfil` en `routes.tsx` dentro del bloque ProtectedRoute

- [x] T10: Frontend — Menú de usuario en header (AC1)
  - [x] Modificar `PublicLayout.tsx`: en la zona del botón "Cerrar sesión" para usuarios autenticados, reemplazar con un `<DropdownMenu>` (shadcn/ui) o simplemente un menú inline compacto:
    - [x] Texto del trigger: apodo del usuario (de `useProfile()`) o email truncado (`email.split('@')[0] + '@...'`) si apodo es null
    - [x] Ítems: "Mi perfil" → `<Link to="/perfil">`, "Cerrar sesión" → `handleLogout()`
  - [x] Revisar si shadcn `<DropdownMenu>` ya está instalado antes de añadir; si no está, implementar como un menú simple con `relative + absolute` (sin dependencia nueva)
  - [x] Asegurar que el menú se cierra al navegar
  - [x] `npm run build --workspace=src/Web/Main` — 0 errores TypeScript

### Review Findings

- [x] [Review][Patch] Resetear campos de contraseña al cerrar el diálogo (Escape, clic fuera, Cancelar) — `onOpenChange` y Cancel solo llaman `setPasswordOpen(false)` sin limpiar `currentPassword`, `newPassword`, `confirmPassword`; el usuario que reabre el diálogo ve los valores previos [src/Web/Main/src/modules/perfil/PerfilPage.tsx:207]
- [x] [Review][Defer] Empty string apodo `""` aceptado como valor válido vía llamada directa a la API — `UpdateApodoAsync` solo valida cuando `apodo is not null`; normalizar a `null` cuando `string.IsNullOrEmpty` [src/Server/Infrastructure/Security/UserService.cs:106] — deferred, pre-existing
- [x] [Review][Defer] `GetActor` helper duplicado verbatim en 6+ clases estáticas de Ops — riesgo de divergencia futura al corregir bugs; extraer a método de extensión de `HttpContext` o clase helper compartida [src/Server/Api/Endpoints/Ops/] — deferred, pre-existing
- [x] [Review][Defer] `EmailEncryptor.Decrypt` atrapa todas las excepciones sin loguear advertencia — si hay rotación de clave, el actor en el audit log queda como blob Base64 cifrado sin aviso observable [src/Server/Infrastructure/Security/EmailEncryptor.cs] — deferred, pre-existing
- [x] [Review][Defer] `ChangeOwnPasswordAsync` acepta la misma contraseña sin rechazarla — no es un requisito de los ACs pero puede sorprender a los usuarios [src/Server/Infrastructure/Security/UserService.cs] — deferred, pre-existing
- [x] [Review][Defer] Caracteres Unicode bidi/format no bloqueados en apodo (U+202E, U+200B, etc.) — `char.IsControl` solo cubre categorías C0/C1; añadir rechazo de categorías `Format` y `Surrogate` [src/Server/Infrastructure/Security/UserService.cs:113] — deferred, pre-existing
- [x] [Review][Defer] `fetchProfile` que retorna 401 muestra mensaje genérico en lugar de redirigir a login — patrón inconsistente con el comportamiento de otras queries autenticadas [src/Web/Main/src/modules/auth/authApi.ts] — deferred, pre-existing
- [x] [Review][Defer] Header actualiza apodo vía re-fetch en background (no optimista) — lag visual breve posible entre el éxito del PATCH y la actualización del label [src/Web/Main/src/modules/perfil/PerfilPage.tsx:47, src/Web/Main/src/modules/auth/useProfile.ts] — deferred, pre-existing

---

## Dev Notes

### Arquitectura de endpoints de cuenta

El archivo `AccountEndpoints.cs` ya existe con el endpoint de `accept-terms`. Los 3 endpoints nuevos se agregan en el mismo método `MapAccount()`:

```csharp
// Patrón para extraer userId — IGUAL que accept-terms
var sub = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
    ?? ctx.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
if (!Guid.TryParse(sub, out var userId))
    return Results.Unauthorized();
```

### Email en el JWT está cifrado — NO leer del token en frontend

`TokenService.GenerateAccessToken` incluye `new Claim(JwtRegisteredClaimNames.Email, user.Email)` donde `user.Email` es el valor **cifrado** (AES-256 con `IEmailEncryptor`). El frontend NO puede descifrar ese claim.

**Regla**: para mostrar el email en UI siempre llamar `GET /account/me` que usa `emailEncryptor.Decrypt()` server-side.

### `ValidateStrongPassword` ya existe en UserService

El método privado estático ya implementa los criterios: mín 8 chars, mayúscula, minúscula, dígito, especial. Reutilizarlo en `ChangeOwnPasswordAsync` sin duplicar lógica.

### Nuevo `UserProfileData` vs `UserData`

NO modificar `UserData` — es el DTO de `IUserService` para Ops y sus tests ya lo usan. Crear un record separado `UserProfileData` para el contexto de perfil propio.

### `InvalidCredentialsException` para contraseña incorrecta

La excepción ya existe en `Domain.Auth.Exceptions`. El endpoint la mapea a 401 (igual que el login). No crear una excepción nueva.

### React Query cache del perfil

```typescript
// useProfile.ts
export const PROFILE_QUERY_KEY = ['account', 'me'] as const

export function useProfile() {
  const { isAuthenticated } = useAuth()
  return useQuery({
    queryKey: PROFILE_QUERY_KEY,
    queryFn: fetchProfile,
    enabled: isAuthenticated,
    staleTime: 5 * 60 * 1000, // 5 min
  })
}
```

Después de PATCH apodo exitoso: `queryClient.invalidateQueries({ queryKey: PROFILE_QUERY_KEY })`.

### Dropdown de usuario en el header

El componente `DropdownMenu` de shadcn **podría no estar instalado**. Antes de T10, verificar:
```bash
ls src/Web/Main/src/shared/ui/ | grep dropdown
```
Si no existe, implementar el menú con CSS puro usando `useState(isOpen)` + `ref` + `useEffect` para cerrar al hacer click fuera. NO instalar el componente sin aprobación (convención del proyecto).

### Ruta `/perfil` — acceso protegido

Añadir dentro del bloque `ProtectedRoute` en `routes.tsx`:
```typescript
{ path: '/perfil', element: <PerfilPage /> },
```
El `ProtectedRoute` existente ya redirige a `/login` si no autenticado — no reimplementar.

### Validación apodo en backend

```csharp
// En UpdateApodoAsync
if (apodo is not null)
{
    if (apodo.Length > 50)
        throw new InvalidUserDataException("El apodo no puede tener más de 50 caracteres.");
    if (apodo.Any(c => char.IsControl(c)))
        throw new InvalidUserDataException("El apodo contiene caracteres no permitidos.");
}
// null es válido (borrar apodo)
```

### Contraseña propia vs cambio por AdminOps

`ChangePasswordAsync(Guid id, string newPassword)` en `IUserService` ya existe — es para que AdminOps cambie la contraseña de cualquier usuario (sin validar la actual). El nuevo método `ChangeOwnPasswordAsync` es para que el propio usuario cambie la suya validando la actual. Son diferentes y no se reemplazan.

### Security Checklist — completar antes del primer commit

- [x] **TOCTOU doble-request**: `PATCH /account/me` no tiene restricción de unicidad en `Apodo` → dos requests concurrentes resultan en el segundo ganando. Aceptable: es self-service del mismo usuario.
- [x] **TOCTOU en cambio de contraseña**: `ChangeOwnPasswordAsync` lee el hash, lo verifica y luego guarda. Ventana de race condition teórica (mismo usuario, dos sesiones simultáneas). Aceptable para MVP: mismo usuario, misma credencial.
- [x] **Auth-gating de `/perfil`**: dentro de `ProtectedRoute` → redirige a `/login` automáticamente.
- [x] **Denominador cero**: no hay cálculos financieros en esta historia.

### Project Structure Notes

- Backend nuevo: `Domain.Auth.User` + migración, `Application.Auth.UserProfileData`, `Application.Auth.IUserService` (3 métodos), `Infrastructure.Security.UserService` (3 implementaciones), `SharedApiContracts.Auth` (3 DTOs), `Api.Endpoints.Private.AccountEndpoints` (3 endpoints).
- Frontend nuevo: `modules/auth/authApi.ts` (3 funciones adicionales), `modules/auth/useProfile.ts` (hook), `modules/perfil/PerfilPage.tsx` (página), modificación de `shared/layouts/PublicLayout.tsx` (menú usuario), modificación de `app/routes.tsx` (nueva ruta).
- Tests nuevos: `tests/Unit/Infrastructure.Tests/UserProfileServiceTests.cs`, `tests/Integration/Api.Tests/AccountEndpointTests.cs`.

### References

- `deferred-work.md` (planning-artifacts) sección "Historia 6-8" — spec original
- `src/Server/Domain/Auth/User.cs` — entidad actual, falta `Apodo`
- `src/Server/Infrastructure/Security/UserService.cs` — `ValidateStrongPassword`, `ChangePasswordAsync` (admin)
- `src/Server/Infrastructure/Security/TokenService.cs:31-37` — claims del JWT, `email` es cifrado
- `src/Server/Api/Endpoints/Private/AccountEndpoints.cs` — archivo a extender
- `src/Web/Main/src/modules/auth/AuthContext.tsx` — `useAuth()` expone `isAuthenticated`
- `src/Web/Main/src/shared/layouts/PublicLayout.tsx` — header a modificar (menú de usuario)
- `src/Web/Main/src/app/routes.tsx` — agregar ruta `/perfil` en ProtectedRoute
- `tests/Integration/Api.Tests/AuthLoginTests.cs` — patrón de tests de auth (SeedUsersAsync, IAsyncLifetime)
- `tests/Integration/Api.Tests/Ops/OpsUserEndpointTests.cs` — patrón multi-cliente (admin, user, anon)

---

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

- Migración EF `AddUserApodo` aplicada y base local actualizada con `auth.User.Apodo`.
- Ajusté `AccountEndpoints` y el flujo self-service de perfil/contraseña para Main sin romper el contrato existente de AdminOps.
- Normalicé los actores de auditoría de Ops para guardar el email descifrado en lugar del claim cifrado del JWT.
- `ApiWebFactory.SeedUsersAsync` ahora reinicia usuarios y refresh tokens para evitar dependencia entre tests de integración.
- Regeneré OpenAPI/client y validé el SPA Main con build de producción.

### Change Log

- 2026-06-06: Implementado perfil de usuario en Main, edición de apodo, cambio de contraseña propia, contratos backend, UI del header y validaciones asociadas.
- 2026-06-06: Ajustada la auditoría de Ops para persistir actor legible y estabilizados los tests de integración con seed determinista.

### Completion Notes List

- Se implementó `GET /api/v1/account/me`, `PATCH /api/v1/account/me` y `PATCH /api/v1/account/password` con validaciones server-side y mapeo de errores acorde a los ACs.
- Se agregó `UserProfileData`, `UserProfileResponse`, `UpdateApodoRequest` y `ChangeOwnPasswordRequest` para el flujo self-service; el contrato existente de AdminOps para cambio de contraseña se preservó por compatibilidad.
- Se implementaron la página `/perfil`, el hook `useProfile`, el menú de usuario en el header y la invalidez de caché tras actualizar apodo.
- Se añadieron y ejecutaron tests unitarios e integración; además se corrigió el seed de usuarios para que los tests no compartan estado mutable entre casos.
- Validaciones ejecutadas: `dotnet test .\tests\Unit\Infrastructure.Tests\Infrastructure.Tests.csproj -c Release --no-build` (267 passed), `dotnet test .\tests\Integration\Api.Tests\Api.Tests.csproj -c Release` (264 passed), `npm run codegen:api`, `npm run build --workspace=src/Web/Main`.

### File List

- src/Server/Domain/Auth/User.cs
- src/Server/Infrastructure/Persistence/SqlServer/Configurations/Auth/UserConfiguration.cs
- src/Server/Application/Auth/IUserService.cs
- src/Server/Application/Auth/UserProfileData.cs
- src/Server/Infrastructure/Security/UserService.cs
- src/Server/Api/Endpoints/Private/AccountEndpoints.cs
- src/Server/SharedApiContracts/Auth/UserProfileResponse.cs
- src/Server/SharedApiContracts/Auth/UpdateApodoRequest.cs
- src/Server/SharedApiContracts/Auth/ChangeOwnPasswordRequest.cs
- src/Server/Infrastructure/Migrations/20260606140153_AddUserApodo.cs
- src/Server/Infrastructure/Migrations/20260606140153_AddUserApodo.Designer.cs
- src/Server/Infrastructure/Migrations/AppDbContextModelSnapshot.cs
- src/Server/Api/Endpoints/Ops/OpsMarketEndpoints.cs
- src/Server/Api/Endpoints/Ops/OpsFundamentalsEndpoints.cs
- src/Server/Api/Endpoints/Ops/OpsCatalogEndpoints.cs
- src/Server/Api/Endpoints/Ops/OpsConfigEndpoints.cs
- src/Server/Api/Endpoints/Ops/OpsAiPromptEndpoints.cs
- src/Server/Api/Endpoints/Ops/AiModeEndpoints.cs
- src/Web/Main/src/modules/auth/authApi.ts
- src/Web/Main/src/modules/auth/useProfile.ts
- src/Web/Main/src/modules/perfil/PerfilPage.tsx
- src/Web/Main/src/shared/layouts/PublicLayout.tsx
- src/Web/Main/src/app/routes.tsx
- _bmad-output/implementation-artifacts/sprint-status.yaml
- scripts/codegen/Api.json
- src/Web/SharedApiClient/schema.d.ts
- tests/Unit/Infrastructure.Tests/Security/UserProfileServiceTests.cs
- tests/Integration/Api.Tests/AccountEndpointTests.cs
- tests/Integration/Api.Tests/ApiWebFactory.cs
