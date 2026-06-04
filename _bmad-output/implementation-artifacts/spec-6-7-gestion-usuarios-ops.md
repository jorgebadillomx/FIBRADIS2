---
title: '6-7 Gestión completa de usuarios en Ops — tipos, deshabilitar, contraseña fuerte, campos de pago, cifrado y mensajes de login'
type: 'feature'
created: '2026-06-04'
status: 'in-review'
baseline_commit: '7285736be09249e3bc2e901864349def6eff66fe'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Ops solo permite crear usuarios tipo Main con contraseña débil y email en texto claro en BD. No hay soporte para crear AdminOps, deshabilitar cuentas, cambiar contraseña con criterios fuertes, registrar datos de pago en usuarios Main, ni dar mensajes de error diferenciados en el login de Main (cuenta deshabilitada vs. credenciales incorrectas). Además existe un usuario de desarrollo hardcodeado en `Program.cs`.

**Approach:** Eliminar el seed `dev@fibradis.mx`; agregar cifrado AES-256 para email en BD (vía `IEmailEncryptor`) y validación de contraseña fuerte; extender la entidad User con Pago/FechaPago; agregar endpoints PATCH para toggle de estado, cambio de contraseña y actualización de pago; diferenciar en `AuthService` los casos de cuenta deshabilitada vs. credenciales inválidas; ampliar `UsersPage.tsx` con selector de rol, campos de pago, botón deshabilitar y diálogo de cambio de contraseña; actualizar `LoginPage.tsx` con mensajes específicos por `domainCode`.

## Boundaries & Constraints

**Always:**

- Contraseña fuerte en creación Y en cambio: mínimo 8 chars, ≥1 mayúscula, ≥1 minúscula, ≥1 dígito, ≥1 carácter especial (no alfanumérico). Validado en backend; también en frontend con feedback visual. Almacenada como hash BCrypt (unidireccional, no reversible).
- Email cifrado con AES-256-CBC antes de persistir en BD; descifrado al leer. Formato almacenado: `base64(IV_16bytes || cipherBytes)` — IV derivado de `HMAC-SHA256(key, normalizedEmail)[..16]` para que sea determinista y permita lookup por igualdad. Clave de 32 bytes de `Encryption:EmailKey` (user secrets / env var — NO en source control).
- `AuthService.LoginAsync` distingue: cuenta deshabilitada → `AccountDisabledException` (401, `ACCOUNT_DISABLED`); credenciales inválidas (email no existe o password incorrecto) → `InvalidCredentialsException` (401, `INVALID_CREDENTIALS`).
- Todos los endpoints `/api/v1/ops/users/**` requieren policy `AdminOps`.
- `Pago` (decimal?) y `FechaPago` (DateTime?) son nullable en BD; en UI solo se muestran/editan para usuarios de rol `User`.
- `UserNotFoundException` → HTTP 404 con `domainCode: USER_NOT_FOUND`.
- Seed de dev `dev@fibradis.mx` eliminado de `Program.cs`.

**Ask First:** ninguna — todos los edge cases están cubiertos en la matriz.

**Never:**

- No eliminar usuarios — solo deshabilitar/habilitar.
- No exponer `PasswordHash` ni el email cifrado (solo el email descifrado) en DTOs.
- No saltarse la validación de contraseña fuerte en backend.
- No almacenar la clave de cifrado en `appsettings.json` commiteado al repo.

## I/O & Edge-Case Matrix

| Escenario | Input | Expected Output | Error |
| --- | --- | --- | --- |
| Crear AdminOps | POST /users `{role:"AdminOps", email, password fuerte}` | 201 `UserSummaryDto` role=AdminOps | — |
| Crear Main con pago | POST /users `{role:"User", email, pwd, pago:100.0, fechaPago:"2026-06-01"}` | 201 con pago/fechaPago | — |
| Password débil (sin mayúscula) | `{password:"abc1!def"}` | 422 INVALID_USER_DATA | mensaje "mayúscula" |
| Password débil (muy corta) | `{password:"Ab1!"}` | 422 INVALID_USER_DATA | mensaje "8 caracteres" |
| Role inválido | `{role:"SuperAdmin"}` | 422 INVALID_USER_DATA | — |
| Deshabilitar usuario | PATCH `/users/{id}/active` `{isActive:false}` | 200 `UserSummaryDto` isActive=false | — |
| Habilitar usuario | PATCH `/users/{id}/active` `{isActive:true}` | 200 `UserSummaryDto` isActive=true | — |
| Cambiar contraseña | PATCH `/users/{id}/password` `{newPassword:"Fuerte1!"}` | 204 No Content | — |
| Contraseña débil en cambio | PATCH `/users/{id}/password` `{newPassword:"12345678"}` | 422 INVALID_USER_DATA | — |
| Actualizar pago | PATCH `/users/{id}/payment` `{pago:250.0, fechaPago:"2026-07-01"}` | 200 `UserSummaryDto` | — |
| Usuario inexistente | PATCH `/users/{fake-guid}/active` | 404 USER_NOT_FOUND | — |
| Login cuenta deshabilitada | POST `/auth/login` email con IsActive=false | 401 ACCOUNT_DISABLED | "Tu cuenta está deshabilitada. Contacta al administrador." |
| Login email no registrado | POST `/auth/login` email inexistente | 401 INVALID_CREDENTIALS | genérico — no revelar existencia |
| Login pwd incorrecta | POST `/auth/login` email válido, pwd errónea | 401 INVALID_CREDENTIALS | genérico |

</frozen-after-approval>

## Code Map

- `src/Server/Domain/Auth/User.cs` — entidad User; agregar Pago, FechaPago
- `src/Server/Domain/Auth/UserRole.cs` — enum User/AdminOps (sin cambios, referencia)
- `src/Server/Domain/Auth/Exceptions/UserNotFoundException.cs` — nueva excepción 404
- `src/Server/Domain/Auth/Exceptions/AccountDisabledException.cs` — nueva excepción 401
- `src/Server/Domain/Common/DomainException.cs` — base (referencia)
- `src/Server/Application/Auth/UserData.cs` — record de transferencia; agregar Pago, FechaPago
- `src/Server/Application/Auth/IUserService.cs` — interfaz; ampliar con nuevos métodos
- `src/Server/Application/Auth/IEmailEncryptor.cs` — nueva interfaz de cifrado de email (Application layer)
- `src/Server/Infrastructure/Security/EmailEncryptor.cs` — implementación AES-256-CBC con IV determinista
- `src/Server/Infrastructure/Security/UserService.cs` — inyectar IEmailEncryptor; ValidateStrongPassword + nuevas ops
- `src/Server/Infrastructure/Security/AuthService.cs` — inyectar IEmailEncryptor; split-check login
- `src/Server/SharedApiContracts/Auth/CreateUserRequest.cs` — agregar Role, Pago?, FechaPago?
- `src/Server/SharedApiContracts/Auth/UserSummaryDto.cs` — agregar Pago, FechaPago
- `src/Server/SharedApiContracts/Auth/ChangePasswordRequest.cs` — nuevo DTO
- `src/Server/SharedApiContracts/Auth/SetUserActiveRequest.cs` — nuevo DTO
- `src/Server/SharedApiContracts/Auth/UpdatePaymentRequest.cs` — nuevo DTO
- `src/Server/Api/CompositionRoot/ApiServiceExtensions.cs` — registrar IEmailEncryptor → EmailEncryptor (Singleton)
- `src/Server/Api/CompositionRoot/GlobalExceptionHandler.cs` — agregar AccountDisabledException → 401, UserNotFoundException → 404
- `src/Server/Api/Endpoints/Ops/OpsUserEndpoints.cs` — agregar 3 endpoints PATCH
- `src/Server/Api/Program.cs` — eliminar bloque seed dev
- `src/Server/Infrastructure/Migrations/` — nueva migración AddUserPaymentFields
- `src/Web/Main/src/modules/auth/LoginPage.tsx` — mensajes específicos por domainCode
- `src/Web/Ops/src/api/usersApi.ts` — añadir setUserActive, changeUserPassword, updateUserPayment
- `src/Web/Ops/src/pages/UsersPage.tsx` — selector rol, campos pago, disable toggle, diálogo contraseña
- `tests/Unit/Infrastructure.Tests/Security/UserServiceTests.cs` — ampliar; FakeEmailEncryptor (identidad)
- `tests/Unit/Infrastructure.Tests/Security/AuthServiceTests.cs` — casos login con cuenta deshabilitada

## Tasks & Acceptance

**Execution:**

- [x] `src/Server/Domain/Auth/User.cs` — agregar `decimal? Pago` y `DateTime? FechaPago`
- [x] `src/Server/Domain/Auth/Exceptions/UserNotFoundException.cs` — crear: `class UserNotFoundException() : DomainException("Usuario no encontrado.", "USER_NOT_FOUND")`
- [x] `src/Server/Domain/Auth/Exceptions/AccountDisabledException.cs` — crear: `class AccountDisabledException() : DomainException("Tu cuenta está deshabilitada. Contacta al administrador.", "ACCOUNT_DISABLED")`
- [x] `src/Server/Application/Auth/UserData.cs` — añadir `decimal? Pago` y `DateTime? FechaPago` al record
- [x] `src/Server/Application/Auth/IUserService.cs` — reemplazar firma de `CreateUserAsync` (añadir `string role`, `decimal? pago`, `DateTime? fechaPago`); agregar `SetUserActiveAsync(Guid, bool, CancellationToken)`, `ChangePasswordAsync(Guid, string, CancellationToken)`, `UpdatePaymentAsync(Guid, decimal?, DateTime?, CancellationToken)`
- [x] `src/Server/Application/Auth/IEmailEncryptor.cs` — nueva interfaz: `string Encrypt(string plainEmail)` / `string Decrypt(string storedEmail)`
- [x] `src/Server/Infrastructure/Security/EmailEncryptor.cs` — implementación AES-256-CBC; clave de 32 bytes de `IConfiguration["Encryption:EmailKey"]` (base64); formato `base64(IV[16]||cipher)` con IV determinista (ver Design Notes); Encrypt y Decrypt simétricas
- [x] `src/Server/Infrastructure/Security/UserService.cs` — inyectar `IEmailEncryptor`; cifrar email antes de persistir, descifrar en `ToData()`; extraer `ValidateStrongPassword(string)` (upper+lower+digit+special+length, lanza `InvalidUserDataException` con mensaje específico); implementar `SetUserActiveAsync` (UserNotFoundException si no existe), `ChangePasswordAsync`, `UpdatePaymentAsync`; actualizar `CreateUserAsync` para aceptar rol + pago
- [x] `src/Server/Infrastructure/Security/AuthService.cs` — inyectar `IEmailEncryptor`; en `LoginAsync`: cifrar email candidato; query sin filtro IsActive; si nulo → `InvalidCredentialsException`; si `!user.IsActive` → `AccountDisabledException`; si password falla → `InvalidCredentialsException`
- [x] `src/Server/SharedApiContracts/Auth/CreateUserRequest.cs` — agregar `[Required] string Role`, `decimal? Pago`, `DateTime? FechaPago`
- [x] `src/Server/SharedApiContracts/Auth/UserSummaryDto.cs` — agregar `decimal? Pago` y `DateTime? FechaPago`
- [x] `src/Server/SharedApiContracts/Auth/ChangePasswordRequest.cs` — nuevo: `record ChangePasswordRequest([Required] string NewPassword)`
- [x] `src/Server/SharedApiContracts/Auth/SetUserActiveRequest.cs` — nuevo: `record SetUserActiveRequest(bool IsActive)`
- [x] `src/Server/SharedApiContracts/Auth/UpdatePaymentRequest.cs` — nuevo: `record UpdatePaymentRequest(decimal? Pago, DateTime? FechaPago)`
- [x] Migración — `dotnet ef migrations add AddUserPaymentFields --project src/Server/Infrastructure --startup-project src/Server/Api`; columnas `Pago decimal(18,2) NULL` y `FechaPago datetime2 NULL`; verificar que columna `Email` tenga MaxLength ≥256 chars para el base64 cifrado
- [x] `src/Server/Api/CompositionRoot/ApiServiceExtensions.cs` — registrar `services.AddSingleton<IEmailEncryptor, EmailEncryptor>()`; documentar en README/secrets que se necesita `Encryption:EmailKey`
- [x] `src/Server/Api/CompositionRoot/GlobalExceptionHandler.cs` — actualizar switch: `AccountDisabledException` → 401, `UserNotFoundException` → 404 (ver Design Notes)
- [x] `src/Server/Api/Endpoints/Ops/OpsUserEndpoints.cs` — agregar: `PATCH /api/v1/ops/users/{id}/active` (SetUserActiveRequest → 200/404), `PATCH /api/v1/ops/users/{id}/password` (ChangePasswordRequest → 204/404/422), `PATCH /api/v1/ops/users/{id}/payment` (UpdatePaymentRequest → 200/404)
- [x] `src/Server/Api/Program.cs` — eliminar bloque completo `if (app.Environment.IsDevelopment()) { ... }` (seed de `dev@fibradis.mx`, líneas 149-168)
- [x] `src/Web/Ops/src/api/usersApi.ts` — agregar `setUserActive(id, isActive)`, `changeUserPassword(id, newPassword)`, `updateUserPayment(id, pago, fechaPago)` usando el cliente openapi-fetch tipado
- [x] `src/Web/Ops/src/pages/UsersPage.tsx` — (a) selector "AdminOps"/"Main (portafolio)" en formulario crear; mostrar Pago/FechaPago cuando rol=Main; (b) botón disable/enable por fila; (c) botón "cambiar contraseña" por fila → diálogo con input + indicador de fortaleza client-side; (d) columnas Pago/FechaPago visibles para role=User, inline-editables
- [x] `src/Web/Main/src/modules/auth/LoginPage.tsx` — parsear `domainCode` de respuesta 401; si `ACCOUNT_DISABLED` → "Tu cuenta está deshabilitada. Contacta al administrador."; de lo contrario → "Correo o contraseña incorrectos."
- [x] `tests/Unit/Infrastructure.Tests/Security/UserServiceTests.cs` — inyectar `FakeEmailEncryptor` (identidad); cubrir: contraseña débil (sin mayúscula, sin especial, < 8 chars), role correcto en AdminOps, SetUserActiveAsync con id inexistente, ChangePasswordAsync con pwd débil, UpdatePaymentAsync guarda valores
- [x] `tests/Unit/Infrastructure.Tests/Security/AuthServiceTests.cs` — cubrir: login cuenta deshabilitada → AccountDisabledException, login email inexistente → InvalidCredentialsException, login pwd incorrecta → InvalidCredentialsException

**Acceptance Criteria:**

- Given AdminOps en Ops al crear usuario con rol "AdminOps" y contraseña fuerte, then aparece en lista con role "AdminOps"
- Given crear usuario con password sin mayúscula, when submit, then error que menciona "mayúscula"
- Given crear usuario Main con pago=150.0 y fechaPago, then `UserSummaryDto` devuelve esos valores
- Given fila de usuario activo, when click deshabilitar, then badge "Inactivo" sin recargar página
- Given diálogo cambiar contraseña con pwd débil, then error inline sin llamar API
- Given diálogo cambiar contraseña con pwd fuerte, then 204 y diálogo cierra
- Given fila de rol AdminOps, then columnas Pago/FechaPago muestran "—" (no editables)
- Given fila de rol User, then Pago/FechaPago son editables/guardables
- Given login en Main con cuenta deshabilitada, then mensaje "deshabilitada" + "Contacta al administrador"
- Given `Program.cs`, then no existe el bloque seed `dev@fibradis.mx`
- Given consulta directa a BD, then columna Email contiene base64 (no texto claro)

## Spec Change Log

- **Edit 2026-06-04 v1** — Finding: email y password deben estar cifrados en BD. Change: agregado `IEmailEncryptor`/`EmailEncryptor` con AES-256-CBC determinista para el email; documentado BCrypt para password. Avoids: email en texto claro en BD.
- **Edit 2026-06-04 v2** — Finding: quitar usuario hardcodeado `dev@fibradis.mx`; mensajes diferenciados en login (cuenta deshabilitada vs. credenciales inválidas). Change: `AccountDisabledException`, split-check en `AuthService`, eliminación seed `Program.cs`, `LoginPage.tsx` por domainCode, nuevos ACs. Avoids: seed en producción; mensaje genérico que no orienta al usuario deshabilitado.

## Design Notes

### Cifrado de email (AES-256-CBC determinista)

```csharp
// IV determinista: HMAC-SHA256(key, normalizedEmail)[..16]
// Stored format: base64( ivBytes[16] || cipherBytes )

// Encrypt:
var iv = HMACSHA256.HashData(keyBytes, Encoding.UTF8.GetBytes(normalizedEmail))[..16];
using var aes = Aes.Create();
aes.Key = keyBytes; aes.IV = iv;
var cipher = aes.EncryptCbc(emailBytes, iv);
return Convert.ToBase64String([..iv, ..cipher]);  // IV prepended

// Decrypt:
var raw = Convert.FromBase64String(stored);
var iv2 = raw[..16]; var cipher2 = raw[16..];
using var aes2 = Aes.Create();
aes2.Key = keyBytes; aes2.IV = iv2;
return Encoding.UTF8.GetString(aes2.DecryptCbc(cipher2, iv2));
```

Preponer el IV al ciphertext permite descifrar sin almacenarlo en columna aparte. Mismo email → mismo IV → mismo ciphertext → `WHERE Email = @encryptedCandidate` funciona para lookup. `UserService` cifra antes de `db.Users.Add()` y descifra en `ToData()`. `AuthService` cifra el email del login antes del query.

### Hash de password (BCrypt) — sin cambio

`BCrypt.Net.BCrypt.HashPassword(password)` ya en `UserService.CreateUserAsync`. `ChangePasswordAsync` usa el mismo método. BCrypt es unidireccional por diseño.

### Validación de contraseña fuerte

```text
if (password.Length < 8)                           → "debe tener al menos 8 caracteres"
if (!password.Any(char.IsUpper))                   → "debe contener al menos una letra mayúscula"
if (!password.Any(char.IsLower))                   → "debe contener al menos una letra minúscula"
if (!password.Any(char.IsDigit))                   → "debe contener al menos un número"
if (!password.Any(c => !char.IsLetterOrDigit(c)))  → "debe contener al menos un carácter especial"
```

Lanza `InvalidUserDataException` con el primer criterio fallido. El frontend replica la misma lógica para feedback inmediato sin llamada a la API.

### GlobalExceptionHandler actualizado

```csharp
httpContext.Response.StatusCode = exception switch {
    InvalidCredentialsException or InvalidRefreshTokenException
        or AccountDisabledException                   => 401,
    UserNotFoundException                             => 404,
    _                                                 => 422
};
```

### Auth error messages

`AuthService.LoginAsync` hace el lookup por email cifrado sin filtrar `IsActive`. Orden de checks: nulo → `InvalidCredentialsException`, deshabilitado → `AccountDisabledException`, password falla → `InvalidCredentialsException`. `LoginPage.tsx` lee `domainCode` del ProblemDetails y solo muestra mensaje diferenciado para `ACCOUNT_DISABLED`; cualquier otro 401 es genérico para no revelar enumeración de usuarios.

### Pago/FechaPago

El servicio no restringe la actualización por rol; la UI oculta los campos para AdminOps. BD uniforme, sin lógica de rol en persistencia.

### Clave de cifrado en desarrollo

```powershell
[Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
# Luego:
dotnet user-secrets set "Encryption:EmailKey" "<resultado>" --project src/Server/Api
```

## Verification

**Commands:**

- `dotnet build FIBRADIS.slnx` — expected: 0 errores
- `dotnet ef database update --project src/Server/Infrastructure --startup-project src/Server/Api` — expected: migración aplicada
- `dotnet test tests/Unit/Infrastructure.Tests --filter "FullyQualifiedName~UserService|FullyQualifiedName~AuthService"` — expected: todos pasan (≥18 tests)
- `npm run build --workspace src/Web/Ops` — expected: 0 errores
- `npm run codegen:api` — expected: schema.d.ts actualizado con nuevos endpoints y DTOs
