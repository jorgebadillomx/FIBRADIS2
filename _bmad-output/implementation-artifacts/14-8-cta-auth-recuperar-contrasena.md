# Story 14.8: CTAs de auth en LoginForm + flujo recuperar contraseña

Status: done

## Story

Como usuario anónimo que ve el formulario de inicio de sesión (en `/login` o `/portafolio`),
quiero ver un enlace "¿No tienes cuenta? Crear cuenta" y un enlace "¿Olvidaste tu contraseña?",
para que pueda registrarme o recuperar mi acceso sin tener que adivinar las rutas.

Como usuario registrado que olvidó su contraseña,
quiero ingresar mi email y recibir un link seguro para establecer una nueva contraseña,
para que pueda recuperar mi acceso sin intervención de un administrador.

## Acceptance Criteria

### Bloque A — CTAs en LoginForm

1. **Dado que** estoy en `/login` o en `/portafolio` (forma inline) sin autenticarme,
   **Entonces** debajo del botón "Iniciar sesión" aparece:
   - Enlace "¿Olvidaste tu contraseña?" → `/recuperar-contrasena`
   - Enlace "¿No tienes cuenta? **Crear cuenta**" → `/registro`

2. **Dado que** el ancho es ≥ 640px,
   **Entonces** ambos enlaces están en la misma fila: "¿Olvidaste tu contraseña?" a la izquierda y "¿No tienes cuenta? Crear cuenta" a la derecha.

3. **Dado que** el ancho es < 640px,
   **Entonces** ambos enlaces se apilan verticalmente.

4. **Dado que** estoy autenticado y el formulario no debería mostrarse,
   **Entonces** los enlaces no son relevantes (LoginPage ya redirige, el contexto no aplica).

### Bloque B — Flujo recuperar contraseña (frontend)

5. **Dado que** navego a `/recuperar-contrasena`,
   **Entonces** veo un formulario con campo email y botón "Enviar enlace".

6. **Dado que** ingreso un email y envío el formulario (sea cual sea el resultado en backend),
   **Entonces** el formulario desaparece y aparece el mensaje: "Si ese email está registrado, recibirás un enlace para restablecer tu contraseña. Revisa tu bandeja de entrada."

7. **Dado que** navego a `/nueva-contrasena?token=XXXX`,
   **Entonces** veo un formulario con campo "Nueva contraseña" (min 8 chars) y botón "Guardar contraseña".

8. **Dado que** ingreso una contraseña válida y la envío,
   **Entonces** veo mensaje de éxito "Tu contraseña fue actualizada. Ahora puedes iniciar sesión." con enlace → `/login`.

9. **Dado que** el token es inválido, expirado, o ya fue usado (contraseña ya cambió),
   **Entonces** el formulario de `/nueva-contrasena` muestra: "Este enlace no es válido o ya expiró. Solicita uno nuevo." con enlace → `/recuperar-contrasena`.

10. **Dado que** navego a `/nueva-contrasena` **sin** parámetro `token`,
    **Entonces** se muestra directamente el estado de error del AC-9.

### Bloque C — Backend: endpoints

11. **Dado que** hago `POST /api/v1/auth/forgot-password` con `{ "email": "user@example.com" }`,
    **Entonces** el endpoint siempre devuelve `200 OK` con `{ "message": "Si ese email está registrado, recibirás un enlace." }` — sin revelar si el email existe.

12. **Dado que** el email existe en la base de datos y está confirmado,
    **Entonces** el backend genera un token HMAC seguro y envía el email de reset (Resend template `PasswordReset`).

13. **Dado que** el email no existe o no está confirmado,
    **Entonces** el backend devuelve `200 OK` igualmente (sin email, para no revelar existencia).

14. **Dado que** hago `POST /api/v1/auth/reset-password` con `{ "token": "...", "newPassword": "..." }`,
    **Entonces** si el token es válido (firma correcta, no expirado, contraseña no cambiada desde emisión):
    - El backend actualiza la contraseña del usuario
    - Devuelve `200 OK { "message": "Contraseña actualizada." }`

15. **Dado que** el token es inválido o expirado,
    **Entonces** el backend devuelve `400 Bad Request { "code": "token_invalid" }`.

16. **Dado que** `newPassword` no cumple los requisitos (< 8 chars, falta mayúscula/minúscula/número/especial),
    **Entonces** el backend devuelve `400 Bad Request` con detalle del requisito incumplido.

### Bloque D — Email de reset

17. **Dado que** el backend envía el email de reset,
    **Entonces** el email usa el template `PasswordReset` de Resend con variables `{{ reset_url }}` y `{{ expiry_minutes }}` (60 minutos).
    El link en el email apunta a `GET /api/v1/auth/reset-password-redirect?token=...` (no a la ruta SPA directamente).

### Bloque E — Endpoint de redirección server-side (resiliencia en clientes de email sandboxed)

1. **Dado que** hago `GET /api/v1/auth/reset-password-redirect?token=<token_válido>`,
   **Entonces** el servidor devuelve `302` → `/nueva-contrasena?token=<token>`.

2. **Dado que** hago `GET /api/v1/auth/reset-password-redirect?token=<token_expirado>`,
   **Entonces** el servidor devuelve `302` → `/nueva-contrasena?status=expired`.

3. **Dado que** hago `GET /api/v1/auth/reset-password-redirect?token=<token_inválido_o_ausente>`,
   **Entonces** el servidor devuelve `302` → `/nueva-contrasena?status=invalid`.

4. **Dado que** navego a `/nueva-contrasena?status=expired`,
   **Entonces** la página muestra inmediatamente el error del AC-9 **sin llamar a ninguna API**.

5. **Dado que** navego a `/nueva-contrasena?status=invalid`,
   **Entonces** la página muestra inmediatamente el error del AC-9 **sin llamar a ninguna API**.

## Tasks / Subtasks

- [x] T1: CTAs en `LoginForm.tsx` (AC: 1, 2, 3)
  - [x] T1.1: Agregar debajo del `<Button type="submit">` una fila `flex justify-between items-center gap-2 flex-wrap text-sm`:
    - `<Link to="/recuperar-contrasena" className="text-muted-foreground hover:text-foreground">¿Olvidaste tu contraseña?</Link>`
    - `<Link to="/registro" className="font-medium hover:underline">¿No tienes cuenta? <span className="font-semibold">Crear cuenta</span></Link>`
  - [x] T1.2: Verificar que el layout se ve correcto en `/login` y en el form inline de `/portafolio`.

- [x] T2: Backend — token service (AC: 12, 14, 15)
  - [x] T2.1: Crear `src/Server/Application/Auth/IPasswordResetTokenService.cs`:
    ```csharp
    public interface IPasswordResetTokenService
    {
        string GenerateToken(Guid userId, string passwordHash);
        Guid? TryDecodeUserId(string token);
        PasswordResetTokenValidationResult ValidateToken(string token, string currentPasswordHash);
    }
    public enum PasswordResetTokenValidationResult { Valid, Invalid, Expired }
    ```
  - [x] T2.2: Crear `src/Server/Infrastructure/Security/PasswordResetTokenService.cs`:
    - Mismo patrón HMAC-SHA256 que `EmailConfirmationTokenService`.
    - Payload: `{userId}|{expiryUnix}` (expiry = ahora + 60 min).
    - **Clave HMAC**: `"fibradis-password-reset:" + Jwt:Secret + passwordHash.Substring(0, 12)` — así el token se invalida automáticamente si la contraseña cambia antes de usarlo.
    - `TryDecodeUserId`: decodifica el payload Base64Url sin verificar HMAC (solo para lookup en DB previo a validación completa).
    - `ValidateToken(token, currentPasswordHash)`: reconstruye la clave con el hash actual del usuario, verifica HMAC con `FixedTimeEquals`, verifica expiry.
  - [x] T2.3: Registrar en DI como Singleton en `ApiServiceExtensions.cs` (igual que `IEmailConfirmationTokenService`).

- [x] T3: Backend — email method (AC: 17)
  - [x] T3.1: Agregar a `IEmailService.cs`:
    ```csharp
    Task SendPasswordResetAsync(string toEmail, string resetUrl, CancellationToken ct);
    ```
  - [x] T3.2: Implementar en `ResendEmailService.cs` siguiendo el mismo patrón de `SendEmailConfirmationAsync`:
    - Template: `_options.Value.Templates.PasswordReset`
    - Variables (UPPER_CASE, igual que los 6 métodos existentes):
      - `RESET_URL` = la URL del endpoint redirect
      - `EXPIRY_MINUTES` = `"60"`
      - `PREVIEW` = `"Tienes 60 minutos para restablecer tu contraseña en Fibras Inmobiliarias."` (preheader text visible en clientes de email bajo el asunto)
  - [x] T3.3: Agregar `PasswordReset` a `ResendTemplateIds` record en `ResendOptions.cs`.
  - [x] T3.4: Agregar `"PasswordReset": ""` al bloque `Resend.Templates` de `appsettings.json`.

- [x] T4: Backend — endpoints (AC: 11, 12, 13, 14, 15, 16, E-1, E-2, E-3)
  - [x] T4.1: Agregar en `AuthEndpoints.cs` el método de mapeo `MapPasswordReset(this IEndpointRouteBuilder app)` y llamarlo desde donde se registran los demás endpoints:
    - `POST /api/v1/auth/forgot-password`
      - Request: `record ForgotPasswordRequest(string Email)`
      - Lookup user por email (case-insensitive, igual que login).
      - Si user existe y EmailConfirmedAt != null:
        1. `GenerateToken(user.Id, user.PasswordHash)`
        2. `resetUrl = $"{baseUrl}/api/v1/auth/reset-password-redirect?token={Uri.EscapeDataString(token)}"`
        3. `await emailService.SendPasswordResetAsync(user.Email, resetUrl, ct)` — loguear error pero no fallar.
      - Siempre: `return Results.Ok(new { message = "Si ese email está registrado, recibirás un enlace." })`
    - `POST /api/v1/auth/reset-password`
      - Request: `record ResetPasswordRequest(string Token, string NewPassword)`
      - `userId = tokenService.TryDecodeUserId(token)` → si null, `400 token_invalid`
      - Lookup user por userId → si no existe, `400 token_invalid`
      - `result = tokenService.ValidateToken(token, user.PasswordHash)`
      - Si `Invalid` o `Expired` → `400 { code: "token_invalid" }`
      - Validar `newPassword` con la misma lógica que `ChangePasswordAsync` (8+ chars, mayúscula, minúscula, dígito, especial) → si falla, `400` con detalle
      - Llamar `await userService.ResetPasswordAsync(userId, newPassword, ct)` (ver T5.1)
      - `return Results.Ok(new { message = "Contraseña actualizada." })`
  - [x] T4.2: Agregar `GET /api/v1/auth/reset-password-redirect` en el mismo mapeo (AC: E-1, E-2, E-3):
    - Solo valida el token (no cambia la contraseña — no tiene la nueva contraseña).
    - `userId = tokenService.TryDecodeUserId(token)` → si null, `302 /nueva-contrasena?status=invalid`
    - Lookup user → si no existe, `302 /nueva-contrasena?status=invalid`
    - `result = tokenService.ValidateToken(token, user.PasswordHash)`
    - Si `Expired` → `302 /nueva-contrasena?status=expired`
    - Si `Invalid` → `302 /nueva-contrasena?status=invalid`
    - Si `Valid` → `302 /nueva-contrasena?token={Uri.EscapeDataString(token)}`
    - `.AllowAnonymous()`, sin cuerpo JSON — solo `Produces(StatusCodes.Status302Found)`.

- [x] T5: Backend — método `ResetPasswordAsync` en UserService (AC: 14)
  - [x] T5.1: Agregar a `IUserService.cs`:
    ```csharp
    Task ResetPasswordAsync(Guid userId, string newPassword, CancellationToken ct);
    ```
  - [x] T5.2: Implementar en `UserService.cs`:
    - Lookup user por id → lanza si no existe (nunca debería pasar, ya validado en endpoint)
    - `user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword)`
    - `await _db.SaveChangesAsync(ct)`
    - Sin migración de EF necesaria — solo actualiza la columna `PasswordHash` existente.

- [x] T6: Frontend — páginas nuevas (AC: 5-10)
  - [x] T6.1: Crear `src/Web/Main/src/pages/RecuperarContrasenaPage.tsx`:
    - SEO: title "Recuperar contraseña | Fibras Inmobiliarias", robots noindex.
    - Estado: `idle | loading | sent`.
    - Form: input email + botón "Enviar enlace" (disabled durante loading).
    - En sent: card de confirmación con el mensaje del AC-6.
  - [x] T6.2: Crear `src/Web/Main/src/pages/NuevaContrasenaPage.tsx`:
    - Lee `token` y `status` de `useSearchParams()`.
    - **Prioridad de resolución** (igual que `ConfirmarEmailPage` en historia 14-9):
      - Si `status === 'expired'` o `status === 'invalid'` → mostrar estado error directamente, **sin llamar API** (AC: E-4, E-5).
      - Si `token` presente (sin status problemático) → mostrar formulario normalmente (AC: 7).
      - Si ninguno → mostrar estado error (AC-10).
    - Estado: `idle | loading | success | error`.
    - Form: input "Nueva contraseña" (type password, minLength=8) + botón "Guardar contraseña".
    - En success: mensaje AC-8 + `<Link to="/login">`.
    - En error: mensaje AC-9 + `<Link to="/recuperar-contrasena">`.

- [x] T7: Frontend — API methods (AC: 11, 14, 15)
  - [x] T7.1: Agregar en `authApi.ts`:
    ```ts
    export async function forgotPassword(email: string): Promise<void>
    export async function resetPassword(token: string, newPassword: string): Promise<void>
    ```
    - `forgotPassword`: `POST /api/v1/auth/forgot-password` — no arroja error (silent, siempre 200).
    - `resetPassword`: `POST /api/v1/auth/reset-password` — lanza `AuthApiError` con code `token_invalid` si 400.

- [x] T8: Routes (AC: 5, 7)
  - [x] T8.1: Agregar en `src/Web/Main/src/app/routes.tsx`:
    ```tsx
    { path: '/recuperar-contrasena', element: p(<RecuperarContrasenaPage />) },
    { path: '/nueva-contrasena', element: p(<NuevaContrasenaPage />) },
    ```

- [x] T9: Tests (AC: 12, 14, 15, 16, E-1, E-2, E-3)
  - [x] T9.1: Crear `tests/Unit/Infrastructure.Tests/Security/PasswordResetTokenServiceTests.cs`:
    - Token válido → `ValidateToken` retorna `Valid`.
    - Token expirado → retorna `Expired`.
    - Token manipulado → retorna `Invalid`.
    - Token generado con hash A, validado con hash B → retorna `Invalid` (simula reset ya usado).
    - `TryDecodeUserId` extrae userId correcto.
  - [x] T9.2: Tests unitarios para `RecuperarContrasenaPage`: render, submit → estado `sent`, mensaje correcto.
  - [x] T9.3: Tests unitarios para `NuevaContrasenaPage`:
    - Sin token ni status → error inmediato.
    - `status=expired` → error inmediato sin llamar API.
    - `status=invalid` → error inmediato sin llamar API.
    - `token` presente → muestra form; submit → success; respuesta 400 → error.
  - [x] T9.4: Tests de integración para `GET /reset-password-redirect` (patrón igual a `ConfirmEmailRedirectTests` de historia 14-9):
    - Token válido → 302, Location contiene `/nueva-contrasena?token=`.
    - Token expirado → 302, Location = `/nueva-contrasena?status=expired`.
    - Token inválido → 302, Location = `/nueva-contrasena?status=invalid`.
    - Sin token → 302, Location = `/nueva-contrasena?status=invalid`.

- [x] T10: Build y verificación
  - [x] T10.1: `dotnet build FIBRADIS.slnx` — 0 errores.
  - [x] T10.2: `dotnet test` — todos los tests verdes.
  - [x] T10.3: `npm run build --workspace=src/Web/Main` — 0 errores TypeScript.
  - [x] T10.4: `npm test --workspace=src/Web/Main` — tests verdes.

### Review Findings

- [x] [Review][Decision] DN1 — Timing oracle en /forgot-password — Diferencia de latencia entre usuario confirmado y desconocido. → Deferido: riesgo aceptable para MVP; requiere muchas muestras; documentado como deuda de seguridad.
- [x] [Review][Decision] DN2 — /reset-password-redirect fallback cuando App:BaseUrl no configurado → Resuelto como patch: usa URL relativa `/nueva-contrasena?status=invalid` cuando baseUrl está vacío; agrega try-catch alrededor de toda la lógica.
- [x] [Review][Decision] DN3 — Race condition dos POSTs concurrentes con mismo token — → Deferido para MVP: probabilidad extremadamente baja en uso real; requiere migración EF con RowVersion.
- [x] [Review][Decision] DN4 — forgotPassword() frontend silencia errores de red → No change: silenciar es intencional (AC-6 es "sea cual sea el resultado en backend"; UX sin información de user existence es prioritario).
- [x] [Review][Decision] DN5 — Usuarios inactivos con email confirmado pueden recibir reset email → Resuelto como patch: añadido `IsActive: true` en la condición de forgot-password.
- [x] [Review][Patch] P1 — /forgot-password devuelve 503 cuando App:BaseUrl falta — **APLICADO**: movido baseUrl check dentro del try-catch, devuelve 200 siempre [src/Server/Api/Endpoints/Public/AuthEndpoints.cs]
- [x] [Review][Patch] P2 — emailEncryptor.Encrypt y FirstOrDefaultAsync sin capturar en /forgot-password — **APLICADO**: wrapped todo el bloque lookup+send en try-catch [src/Server/Api/Endpoints/Public/AuthEndpoints.cs]
- [x] [Review][Patch] P3 — UserNotFoundException no capturada en /reset-password — **APLICADO**: añadido catch(UserNotFoundException) → 400 token_invalid [src/Server/Api/Endpoints/Public/AuthEndpoints.cs]
- [x] [Review][Patch] P4 — DbUpdateException no capturada en /reset-password — **APLICADO**: añadido catch(DbUpdateException) → 500 Problem con mensaje de retry [src/Server/Api/Endpoints/Public/AuthEndpoints.cs]
- [x] [Review][Patch] P5 — BCrypt 72-byte límite sin validación — **APLICADO**: `Encoding.UTF8.GetByteCount(newPassword) > 72` → InvalidUserDataException [src/Server/Infrastructure/Security/UserService.cs]
- [x] [Review][Patch] P6 — Test CreateToken hardcodea JWT secret — **APLICADO**: CreateToken ahora no-static, lee Jwt:Secret de IConfiguration del factory [tests/Integration/Api.Tests/AuthPasswordResetTests.cs]
- [x] [Review][Patch] P7 — FindAsync en /reset-password-redirect puede lanzar → 500 JSON — **APLICADO**: try-catch global en el endpoint, redirige a errorUrl en cualquier excepción [src/Server/Api/Endpoints/Public/AuthEndpoints.cs]
- [x] [Review][Patch] P8 — Password trimmed antes de enviarse al API — **APLICADO**: `resetPassword(token, newPassword)` en lugar de `normalizedPassword` [src/Web/Main/src/pages/NuevaContrasenaPage.tsx]
- [x] [Review][Defer] D1 — BCrypt prefix entropy solo ~5 chars random (primeros 7 chars siempre son $2b$10$) — deferred, diseño intencional per spec; entropía de 64^5 ≈ 10^9 suficiente para el propósito
- [x] [Review][Defer] D2 — Clock skew entre nodos puede expirar tokens antes del límite declarado de 60 min — deferred, concern de infraestructura no de código
- [x] [Review][Defer] D3 — NuevaContrasenaPage tokenError state inicializado una vez con useState — deferred, React Router desmonta el componente en navegación a nueva ruta

## Dev Notes

### Estado actual del código antes de esta historia

| Archivo | Estado |
|---|---|
| `src/Web/Main/src/modules/auth/LoginForm.tsx` | Form con email, contraseña, botón submit. **Sin CTAs secundarios debajo del botón**. Usado en `/login` (via `LoginPage.tsx`) y en `/portafolio` (inline en `PortafolioLanding.tsx` línea 159). |
| `src/Web/Main/src/modules/auth/LoginPage.tsx` | Wrapper de `LoginForm`. Redirige autenticados a `/portafolio`. Sin cambios necesarios. |
| `src/Web/Main/src/modules/portafolio/PortafolioLanding.tsx:159` | `<LoginForm redirectTo="/portafolio" titleAs="h2" className="shadow-md" />` — mismo `LoginForm`, sin CTAs. |
| `src/Web/Main/src/app/routes.tsx` | Ruta `/registro` en línea 67. **No existen** `/recuperar-contrasena` ni `/nueva-contrasena`. |
| `src/Web/Main/src/modules/auth/authApi.ts` | Tiene `loginMain`, `registerUser`, `confirmEmail`, `resendConfirmation`, `logoutMain`, `refreshMainSession`. **No tiene** `forgotPassword` ni `resetPassword`. |
| `src/Server/Api/Endpoints/Public/AuthEndpoints.cs` | Endpoints: `/register`, `/confirm-email`, `/login`, `/refresh`, `/resend-confirmation`, `/logout`. **No existen** `/forgot-password` ni `/reset-password`. |
| `src/Server/Application/Auth/IEmailConfirmationTokenService.cs` | Interface con `GenerateToken(Guid userId)` + `ValidateToken(string token)`. Sirve de plantilla exacta para `IPasswordResetTokenService`. |
| `src/Server/Infrastructure/Security/EmailConfirmationTokenService.cs` | Implementación HMAC-SHA256: payload `{userId}\|{expiryUnix}`, firma Base64Url, clave = prefijo + Jwt:Secret. **Replicar este patrón** con key que incorpora `passwordHash.Substring(0,12)`. |
| `src/Server/Application/Email/IEmailService.cs` | 6 métodos. Agregar `SendPasswordResetAsync`. |
| `src/Server/Infrastructure/Integrations/Email/ResendEmailService.cs` | 6 implementaciones. Agregar la 7a. |
| `src/Server/Infrastructure/Integrations/Email/ResendOptions.cs` | Record `ResendTemplateIds` con 6 propiedades. Agregar `PasswordReset`. |
| `src/Server/Api/appsettings.json` | `Resend.Templates` con 6 IDs vacíos. Agregar `"PasswordReset": ""`. |
| `src/Server/Application/Auth/IUserService.cs` | Tiene `ChangePasswordAsync` y `ChangeOwnPasswordAsync`. Agregar `ResetPasswordAsync`. |
| `src/Server/Infrastructure/Security/UserService.cs` | 405 líneas. Agregar `ResetPasswordAsync` siguiendo el mismo patrón de `ChangePasswordAsync` (sin verificar contraseña anterior). |

### Diseño del token de reset de contraseña

**Problema a resolver**: El token HMAC es stateless (sin columna en DB), pero debe invalidarse automáticamente después de que el usuario cambia su contraseña (para que un link interceptado no pueda reutilizarse).

**Solución**: incorporar un slice del `PasswordHash` actual en la clave HMAC:

```csharp
// Generación
var payload = $"{userId}|{DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds()}";
var key = Encoding.UTF8.GetBytes("fibradis-password-reset:" + _jwtSecret + passwordHash.Substring(0, 12));
var sig = HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(payload));
return $"{Base64UrlEncode(payload)}.{Base64UrlEncode(sig)}";

// Validación (ya tenemos el currentPasswordHash de la DB)
var key = Encoding.UTF8.GetBytes("fibradis-password-reset:" + _jwtSecret + currentPasswordHash.Substring(0, 12));
var expectedSig = HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(payload));
if (!CryptographicOperations.FixedTimeEquals(expectedSig, decodedSig)) return Invalid;
```

**Por qué funciona**: Si la contraseña fue cambiada después de emitir el token, `currentPasswordHash` es diferente → la clave HMAC es diferente → la firma no coincide → token inválido. Sin DB extra.

**TryDecodeUserId**: Decodifica payload Base64Url y extrae el userId SIN verificar HMAC (necesario para el lookup en DB antes de poder construir la clave con el hash actual):
```csharp
public Guid? TryDecodeUserId(string token)
{
    var parts = token.Split('.');
    if (parts.Length != 2) return null;
    var payload = Encoding.UTF8.GetString(Base64UrlDecode(parts[0]));
    var segments = payload.Split('|');
    if (segments.Length < 2 || !Guid.TryParse(segments[0], out var userId)) return null;
    return userId;
}
```

### Flujo de recuperar contraseña (end-to-end)

```
Usuario en /login → click "¿Olvidaste tu contraseña?" → /recuperar-contrasena
→ ingresa email → POST /api/v1/auth/forgot-password
→ backend genera token → envía email Resend con link a /api/v1/auth/reset-password-redirect?token=XXX
→ siempre 200 → SPA muestra "Si ese email está registrado, recibirás un enlace."

Usuario recibe email → click link →
  GET /api/v1/auth/reset-password-redirect?token=XXX  (server-side)
    → token válido   → 302 /nueva-contrasena?token=XXX
    → token expirado → 302 /nueva-contrasena?status=expired
    → token inválido → 302 /nueva-contrasena?status=invalid

SPA /nueva-contrasena:
  → status=expired/invalid → muestra error inmediato, sin API
  → token presente → muestra form; usuario escribe nueva contraseña
    → POST /api/v1/auth/reset-password con { token, newPassword }
      → backend valida token + hash → BCrypt.HashPassword → SaveChangesAsync → 200
    → SPA muestra "Tu contraseña fue actualizada. Ahora puedes iniciar sesión."
```

**Por qué el link va al endpoint y no a la SPA:** Si el cliente de email renderiza el link en un iframe sandboxed sin `allow-scripts`, React no carga y el usuario ve pantalla en blanco. Al pasar primero por el backend, el redirect HTTP 302 ocurre sin JS — el cliente de email muestra el destino correcto aunque sea en sandbox. El token válido sigue siendo necesario en la SPA para el submit, pero el estado de error (`expired`/`invalid`) ya no requiere JS para mostrarse.

### Endpoint de redirección — esqueleto de implementación

```csharp
group.MapGet("/reset-password-redirect", async (
    string? token,
    IPasswordResetTokenService tokenService,
    IUserService userService,
    IConfiguration config,
    CancellationToken ct) =>
{
    var baseUrl = config["App:BaseUrl"]?.TrimEnd('/') ?? string.Empty;
    var errorUrl = $"{baseUrl}/nueva-contrasena?status=invalid";

    var userId = tokenService.TryDecodeUserId(token ?? string.Empty);
    if (userId is null)
        return Results.Redirect(errorUrl);

    var user = await userService.FindByIdAsync(userId.Value, ct);
    if (user is null)
        return Results.Redirect(errorUrl);

    var result = tokenService.ValidateToken(token!, user.PasswordHash);
    return result switch
    {
        PasswordResetTokenValidationResult.Valid =>
            Results.Redirect($"{baseUrl}/nueva-contrasena?token={Uri.EscapeDataString(token!)}"),
        PasswordResetTokenValidationResult.Expired =>
            Results.Redirect($"{baseUrl}/nueva-contrasena?status=expired"),
        _ => Results.Redirect(errorUrl),
    };
})
.AllowAnonymous()
.Produces(StatusCodes.Status302Found);
```

### CTAs en LoginForm — cambio exacto

`LoginForm.tsx` — agregar debajo del `<Button type="submit">` (dentro del mismo `<form>`):

```tsx
<div className="flex flex-wrap justify-between items-center gap-2 text-sm">
  <Link
    to="/recuperar-contrasena"
    className="text-muted-foreground hover:text-foreground transition-colors"
  >
    ¿Olvidaste tu contraseña?
  </Link>
  <Link
    to="/registro"
    className="text-foreground hover:underline"
  >
    ¿No tienes cuenta?{' '}
    <span className="font-semibold">Crear cuenta</span>
  </Link>
</div>
```

- `justify-between` → texto izquierda y derecha en pantallas anchas.
- `flex-wrap` → se apilan en móvil.
- No agregar `<Link>` de react-router-dom a los imports si ya existe — verificar antes.

### Requisitos de contraseña (igual que ChangeOwnPasswordAsync)

```
- Mínimo 8 caracteres
- Al menos una mayúscula
- Al menos una minúscula
- Al menos un dígito
- Al menos un carácter especial
```

Reutilizar la lógica de validación ya existente en `UserService.cs` — no duplicar regex.

### No hacer

- No agregar campo "confirmar contraseña" en `NuevaContrasenaPage` — validación frontend es suficiente con minLength=8 y los requisitos del backend devuelven mensajes claros.
- No invalidar el refresh token del usuario al hacer reset — no hay sesión activa (olvidó su contraseña). Si hay sesión activa, la contraseña del usuario cambia pero el refresh token JWT existente seguirá válido hasta expirar (7 días). Esto es aceptable por ahora.
- No agregar rate limiting al endpoint `/forgot-password` en esta historia — es deuda conocida.
- No mostrar el template de email en esta historia — el ID de Resend se dejará vacío como los otros 6 templates existentes; se configura en Resend.com externamente.
- No tocar `PortafolioLanding.tsx` directamente — los CTAs se agregan a `LoginForm.tsx` y se propagan automáticamente a ambos contextos.

### Security Checklist

- [ ] **Privacy**: `/forgot-password` siempre devuelve 200 — no revela si el email existe.
- [ ] **Token replay**: token HMAC incluye passwordHash slice → inválido después del reset.
- [ ] **Token expiry**: 60 minutos — corto para seguridad, suficiente para el usuario.
- [ ] **HTTPS**: los tokens viajan en URLs → solo seguros sobre HTTPS (el dominio fibrasinmobiliarias.com ya fuerza HTTPS).
- [ ] **Password validation**: mismos requisitos que el registro — no relajar.
- [ ] **No timing attack**: usar `CryptographicOperations.FixedTimeEquals` en validación HMAC.

### References

- `IEmailConfirmationTokenService` (plantilla): `src/Server/Application/Auth/IEmailConfirmationTokenService.cs`
- `EmailConfirmationTokenService` (plantilla): `src/Server/Infrastructure/Security/EmailConfirmationTokenService.cs`
- `ResendEmailService` (plantilla para nuevo método): `src/Server/Infrastructure/Integrations/Email/ResendEmailService.cs`
- `AuthEndpoints.cs` (donde agregar endpoints): `src/Server/Api/Endpoints/Public/AuthEndpoints.cs`
- `LoginForm.tsx` (donde agregar CTAs): `src/Web/Main/src/modules/auth/LoginForm.tsx`
- `authApi.ts` (donde agregar funciones): `src/Web/Main/src/modules/auth/authApi.ts`
- `routes.tsx` (donde agregar rutas): `src/Web/Main/src/app/routes.tsx`
- Tests token servicio: `tests/Unit/Infrastructure.Tests/Security/EmailConfirmationTokenServiceTests.cs` (para ver el patrón de tests)
- Historia 14-2 (registro + confirmación email, mismos patrones): `_bmad-output/implementation-artifacts/14-2-registro-y-confirmacion-email.md`

## Dev Agent Record

### Agent Model Used

GPT-5

### Completion Notes List

- Implemented the password-reset HMAC token service, backend endpoints, and Resend email template wiring.
- Added the new public SPA flow for `/recuperar-contrasena` and `/nueva-contrasena`, plus the auth CTAs in `LoginForm`.
- Added backend, integration, and Playwright coverage for valid, expired, invalid, and replayed reset tokens; page behavior is covered via Playwright e2e.
- Validated with `dotnet build FIBRADIS.slnx`, `dotnet test FIBRADIS.slnx --no-build -m:1`, `npm run build --workspace=src/Web/Main`, `npm test --workspace=src/Web/Main`, and `npm run test:e2e:runner --workspace=src/Web/Main -- tests/e2e/auth-password-reset.spec.ts`.

### File List

- `src/Server/Application/Auth/IPasswordResetTokenService.cs`
- `src/Server/Infrastructure/Security/PasswordResetTokenService.cs`
- `src/Server/Api/CompositionRoot/ApiServiceExtensions.cs`
- `src/Server/Application/Email/IEmailService.cs`
- `src/Server/Infrastructure/Integrations/Email/ResendEmailService.cs`
- `src/Server/Infrastructure/Integrations/Email/ResendOptions.cs`
- `src/Server/Application/Auth/IUserService.cs`
- `src/Server/Infrastructure/Security/UserService.cs`
- `src/Server/Api/Endpoints/Public/AuthEndpoints.cs`
- `src/Server/Api/appsettings.json`
- `src/Web/Main/src/modules/auth/LoginForm.tsx`
- `src/Web/Main/src/modules/auth/authApi.ts`
- `src/Web/Main/src/app/routes.tsx`
- `src/Web/Main/src/pages/RecuperarContrasenaPage.tsx`
- `src/Web/Main/src/pages/NuevaContrasenaPage.tsx`
- `src/Web/Main/tests/e2e/auth-password-reset.spec.ts`
- `src/Web/Main/tests/e2e/fixtures/auth-reset-api.ts`
- `tests/Integration/Api.Tests/ApiWebFactory.cs`
- `tests/Integration/Api.Tests/AuthPasswordResetTests.cs`
- `tests/Unit/Infrastructure.Tests/Security/PasswordResetTokenServiceTests.cs`
- `tests/Unit/Infrastructure.Tests/Integrations/Email/ResendEmailServiceTests.cs`
- `tests/Unit/Infrastructure.Tests/Security/UserServiceTests.cs`
- `tests/Unit/Infrastructure.Tests/Jobs/Subscriptions/SubscriptionMaintenanceJobTests.cs`
- `scripts/codegen/Api.json`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

## Change Log

- 2026-06-20: Story creada — CTAs de auth en LoginForm + flujo completo recuperar contraseña.
- 2026-06-20: Añadido Bloque E — endpoint server-side `GET /reset-password-redirect` que valida el token y redirige con `?status=` (mismo patrón que historia 14-9), evitando fallo silencioso en clientes de email sandboxed.
- 2026-06-20: Implementado el flujo completo de reset de contraseña, incluidos CTAs, endpoints, email, páginas SPA, tests y validación final.
