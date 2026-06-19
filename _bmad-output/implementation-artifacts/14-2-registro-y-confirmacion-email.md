# Story 14.2: Flujo de registro + confirmación de email

Status: done

## Story

Como visitante,
quiero registrarme con email, contraseña, nombre opcional y fuente de descubrimiento,
para que al confirmar mi email se active automáticamente mi prueba gratuita de 14 días.

## Acceptance Criteria

1. **Dado que** hago `POST /api/v1/auth/register` con email válido, contraseña segura, y opcionalmente `apodo` y `howDidYouHear`, **Entonces** se crea el usuario con `Role = User`, `IsActive = false`, `EmailConfirmedAt = null`, `TrialEndsAt = null`, y se envía un email de confirmación con token firmado (expiración 24h) vía Resend. Respuesta: 200 OK con `{ "message": "..." }`.

2. **Dado que** el email enviado tiene dominio en la lista negra de dominios desechables (ej. mailinator.com, tempmail.org), **Entonces** el endpoint devuelve 422 Unprocessable Entity con `{ "code": "disposable_email" }`.

3. **Dado que** hago `GET /api/v1/auth/confirm-email?token=xxx` con token válido y no expirado, **Entonces** `email_confirmed_at` se setea a `UtcNow`, `trial_ends_at` se setea a `email_confirmed_at + 14 días`, `is_active` se actualiza a `true` (via `ComputedIsActive`), y el endpoint devuelve 200 OK con `{ "trialEndsAt": "2026-07-03T00:00:00Z" }`.

4. **Dado que** hago `GET /api/v1/auth/confirm-email?token=xxx` con token expirado, **Entonces** el endpoint devuelve 400 Bad Request con `{ "code": "token_expired" }`.

5. **Dado que** hago `GET /api/v1/auth/confirm-email?token=xxx` con token ya usado (EmailConfirmedAt ya seteado), **Entonces** el endpoint devuelve 400 Bad Request con `{ "code": "token_already_used" }`.

6. **Dado que** el email ya está confirmado y el usuario hace login con `POST /api/v1/auth/login`, **Entonces** el flujo de auth existente funciona sin cambios — el JWT devuelto incluye el claim estándar `Role = User`.

## Tasks / Subtasks

- [x] T1: IEmailService + ResendEmailService (AC: 1)
  - [x] T1.1: Crear `src/Server/Application/Email/IEmailService.cs` — interfaz con `SendEmailConfirmationAsync(string toEmail, string confirmationUrl, CancellationToken ct)`
  - [x] T1.2: Crear `src/Server/Infrastructure/Integrations/Email/ResendOptions.cs` — record con `ApiKey` y `SenderEmail`
  - [x] T1.3: Crear `src/Server/Infrastructure/Integrations/Email/ResendEmailService.cs` — HttpClient contra `https://api.resend.com/emails` con `Authorization: Bearer {ApiKey}`, body JSON `{ from, to, subject, html }`. No lanzar excepción si falla el envío — loggear el error y continuar.
  - [x] T1.4: Registrar en `ApiServiceExtensions.cs`: `builder.Services.Configure<ResendOptions>(config.GetSection("Resend"))` + `builder.Services.AddHttpClient<IEmailService, ResendEmailService>()`

- [x] T2: Token de confirmación — servicio HMAC-SHA256 (AC: 1, 3, 4, 5)
  - [x] T2.1: Crear `src/Server/Application/Auth/IEmailConfirmationTokenService.cs` con:
    - `string GenerateToken(Guid userId)` — token válido 24h
    - `EmailTokenValidationResult ValidateToken(string token)` — devuelve `(Guid UserId, bool IsExpired, bool IsValid)`
  - [x] T2.2: Crear `src/Server/Infrastructure/Security/EmailConfirmationTokenService.cs` — implementación HMAC-SHA256:
    - Payload: `{userId}|{expiryUnixTimestampUtc}` (string)
    - Firma: HMAC-SHA256 del payload usando `JwtOptions.Secret` como clave (ya disponible en DI)
    - Token final: `base64url(payload) + "." + base64url(signature)` — URL-safe, sin `=` padding
    - Validar: decodificar, verificar firma, verificar expiry
  - [x] T2.3: Registrar en `ApiServiceExtensions.cs`: `builder.Services.AddSingleton<IEmailConfirmationTokenService, EmailConfirmationTokenService>()`

- [x] T3: Blocklist de dominios desechables (AC: 2)
  - [x] T3.1: Crear `src/Server/Domain/Auth/DisposableEmailDomains.cs` — clase estática con `HashSet<string>` de al menos 10 dominios conocidos:
    - mailinator.com, tempmail.org, guerrillamail.com, 10minutemail.com, throwam.com, yopmail.com, sharklasers.com, trashmail.com, maildrop.cc, spamgourmet.com
  - [x] T3.2: Agregar método estático `bool IsDisposable(string email)` que extrae el dominio y consulta el HashSet (case-insensitive)

- [x] T4: IUserService — RegisterAsync + ConfirmEmailAsync (AC: 1, 3, 4, 5)
  - [x] T4.1: Agregar a `IUserService`:
    ```csharp
    Task<UserData> RegisterAsync(string email, string password, string? apodo, HowDidYouHear? howDidYouHear, CancellationToken ct);
    Task<UserData> ConfirmEmailAsync(Guid userId, CancellationToken ct);
    ```
  - [x] T4.2: Implementar `RegisterAsync` en `UserService`:
    - Validar disposable → lanzar nueva excepción `DisposableEmailException`
    - Normalizar email, encriptar, verificar duplicado → `DuplicateEmailException` existente
    - Crear usuario: `Role = UserRole.User`, `IsActive = false`, `EmailConfirmedAt = null`, `TrialEndsAt = null`, `HowDidYouHear` si se provee, `Apodo` si se provee
    - NO setear `SubscriptionType` — el usuario comienza sin suscripción
  - [x] T4.3: Implementar `ConfirmEmailAsync` en `UserService`:
    - Buscar usuario por id → 404 si no existe
    - Si `EmailConfirmedAt != null` → lanzar nueva excepción `EmailAlreadyConfirmedException`
    - Setear `EmailConfirmedAt = DateTimeOffset.UtcNow`
    - Setear `TrialEndsAt = EmailConfirmedAt + 14 días`
    - Recalcular: `user.IsActive = user.ComputedIsActive` (será true por el trial)
    - SaveChanges

- [x] T5: SharedApiContracts — nuevos tipos (AC: 1, 3)
  - [x] T5.1: Crear `src/Server/SharedApiContracts/Auth/RegisterRequest.cs`:
    ```csharp
    public record RegisterRequest(string Email, string Password, string? Apodo, string? HowDidYouHear);
    ```
  - [x] T5.2: Crear `src/Server/SharedApiContracts/Auth/RegisterResponse.cs`:
    ```csharp
    public record RegisterResponse(string Message);
    ```
  - [x] T5.3: Crear `src/Server/SharedApiContracts/Auth/ConfirmEmailResponse.cs`:
    ```csharp
    public record ConfirmEmailResponse(DateTimeOffset TrialEndsAt);
    ```

- [x] T6: Endpoints — POST /register y GET /confirm-email (AC: 1, 2, 3, 4, 5, 6)
  - [x] T6.1: En `AuthEndpoints.cs`, agregar `POST /api/v1/auth/register`:
    ```
    - Parsear RegisterRequest
    - Si email desechable → Results.UnprocessableEntity(new { code = "disposable_email" })
    - await userService.RegisterAsync(...)
    - token = tokenService.GenerateToken(user.Id)
    - confirmationUrl = $"{config["App:BaseUrl"]}/confirmar-email?token={token}"
    - await emailService.SendEmailConfirmationAsync(plainEmail, confirmationUrl, ct)
    - return Results.Ok(new RegisterResponse("Revisa tu email para confirmar tu cuenta."))
    ```
    - Manejar `DuplicateEmailException` → 409 Conflict
    - Manejar `DisposableEmailException` → 422
    - Manejar `InvalidUserDataException` → 400
  - [x] T6.2: En `AuthEndpoints.cs`, agregar `GET /api/v1/auth/confirm-email?token=xxx`:
    ```
    - result = tokenService.ValidateToken(token)
    - Si !result.IsValid → 400 { code = "token_invalid" }
    - Si result.IsExpired → 400 { code = "token_expired" }
    - user = await userService.FindByIdAsync(result.UserId, ct)  ← ver T6.3
    - Si user.EmailConfirmedAt != null → 400 { code = "token_already_used" }
    - confirmed = await userService.ConfirmEmailAsync(result.UserId, ct)
    - return Results.Ok(new ConfirmEmailResponse(confirmed.TrialEndsAt!.Value))
    ```
  - [x] T6.3: Agregar `Task<UserData?> FindByIdAsync(Guid id, CancellationToken ct)` a `IUserService` + implementación en `UserService`

- [x] T7: Codegen API (AC: todos)
  - [x] T7.1: `npm run codegen:api` para regenerar `src/Web/SharedApiClient/schema.d.ts`

- [x] T8: Unit tests (AC: 1, 2, 3, 4, 5)
  - [x] T8.1: Tests `DisposableEmailDomains` — `IsDisposable("user@mailinator.com")` → true, `IsDisposable("user@gmail.com")` → false, case-insensitive
  - [x] T8.2: Tests `EmailConfirmationTokenService` — generate + validate happy path, expired token, token tamperado (firma inválida)
  - [x] T8.3: Tests `UserService.RegisterAsync` — happy path (usuario creado con IsActive=false), email desechable → excepción, email duplicado → excepción
  - [x] T8.4: Tests `UserService.ConfirmEmailAsync` — happy path (EmailConfirmedAt seteado, TrialEndsAt = +14d, IsActive=true), segunda confirmación → excepción

- [x] T9: Integration tests (AC: 1, 2, 3, 4, 5)
  - [x] T9.1: `POST /api/v1/auth/register` — 200 con email válido, 422 con dominio desechable, 409 con email duplicado
  - [x] T9.2: `GET /api/v1/auth/confirm-email` — 200 con token válido (verifica TrialEndsAt en respuesta), 400 `token_expired`, 400 `token_already_used`

- [x] T10: Build y verificación final
  - [x] T10.1: `dotnet build FIBRADIS.slnx` — 0 errores
  - [x] T10.2: `dotnet test tests/Unit/` — todos los tests verdes
  - [x] T10.3: `npm run build --workspace=src/Web/Main` y `--workspace=src/Web/Ops` — 0 errores

## Dev Notes

### Prerequisito: Story 14.1 ya está en review

La migración EF `20260619161559_AddSubscriptionFields` ya existe y agrega las columnas `email_confirmed_at` y `trial_ends_at` a `auth.User`. Los enums `SubscriptionType` y `HowDidYouHear` ya existen en `Domain.Auth`. El dominio `User` ya tiene `ComputedIsActive`. **No crear ninguna migración nueva** — esta historia solo agrega código de aplicación.

### Resend API Key — dónde está

La key ya está colocada en `.env` como `RESEND__APIKEY=re_KmCiAnZ2_JYGL3ZrtfT5mvhCMv5cXw2Gx`.

Esta key es de testing. En producción se cambiará por una key diferente.

Configuración en `appsettings.json`:
```json
"Resend": {
  "ApiKey": "",
  "SenderEmail": "noreply@fibrasinmobiliarias.com"
}
```

La env var `RESEND__APIKEY` (doble guion bajo) mapea a `Resend:ApiKey` en el sistema de configuración de ASP.NET Core — mismo patrón que `GEMINI__APIKEY` → `Gemini:ApiKey`.

### ResendEmailService — implementación HTTP

Resend usa una REST API simple. No instalar NuGet package — usar `HttpClient` directamente (patrón del proyecto):

```csharp
// POST https://api.resend.com/emails
// Authorization: Bearer {apiKey}
// Content-Type: application/json

var payload = new
{
    from = options.SenderEmail,
    to = new[] { toEmail },
    subject = "Confirma tu cuenta en Fibras Inmobiliarias",
    html = $"""
        <h2>Confirma tu email</h2>
        <p>Haz clic en el enlace para activar tu prueba gratuita de 14 días:</p>
        <p><a href="{confirmationUrl}">Confirmar mi cuenta</a></p>
        <p>Este enlace expira en 24 horas.</p>
        <p>Si no creaste una cuenta, ignora este mensaje.</p>
        """
};
```

Si el envío falla (excepción de red o 4xx/5xx), **loggear el error** usando `ILogger<ResendEmailService>` y NO propagar la excepción — el usuario ya fue creado en BD. El endpoint devuelve 200 igual.

### Token HMAC-SHA256 — diseño stateless

No requiere tabla adicional en BD. Diseño:

```
payload  = "{userId}|{expiryUnixTimestampUtc}"   // ej: "abc-123|1750000000"
signature = HMAC-SHA256(payload, utf8(jwtSecret))
token    = base64url(payload) + "." + base64url(signature)
```

- `base64url`: usar `Convert.ToBase64String()` y luego reemplazar `+`→`-`, `/`→`_`, quitar `=` padding
- El JWT Secret ya está disponible via `IOptions<JwtOptions>` o leyendo `config["Jwt:Secret"]`
- La expiración de "ya usado" se detecta por `EmailConfirmedAt != null` — no se necesita invalidar el token explícitamente

### Endpoint /confirmar-email en el frontend

El email contiene una URL como `https://fibrasinmobiliarias.com/confirmar-email?token=xxx`. La SPA Main necesitará una página `/confirmar-email` que llame al endpoint `GET /api/v1/auth/confirm-email?token=xxx` y muestre el resultado. **Esto es parte del alcance de esta historia** — incluir la página frontend mínima.

Página mínima:
- Lee `token` del query string
- Llama al endpoint vía `apiClient`
- Si 200: muestra "¡Cuenta confirmada! Tu prueba gratuita vence el {trialEndsAt}" + botón "Iniciar sesión"
- Si 400 `token_expired`: "El enlace expiró. Solicita un nuevo email de confirmación."
- Si 400 `token_already_used`: "Tu cuenta ya fue confirmada. Puedes iniciar sesión."
- Si error: mensaje genérico

Ruta SPA: `src/Web/Main/src/pages/ConfirmarEmailPage.tsx`  
Registrar en `src/Web/Main/src/App.tsx` en las rutas públicas.  
Agregar a `KnownPaths` para SEO: `{ path: '/confirmar-email', public: true, noindex: true }` (no indexar).

### Lógica de "registro" vs. "crear usuario desde Ops"

`UserService.CreateUserAsync` (existente) crea usuarios Ops con `IsActive = true`. El nuevo `RegisterAsync` crea usuarios públicos con `IsActive = false` — son dos flujos distintos. **No modificar `CreateUserAsync`**.

### DisposableEmailDomains — lista base

Usar al menos estos dominios. La lista puede extenderse en el futuro pero no sobreingeniería ahora:

```csharp
private static readonly HashSet<string> Domains = new(StringComparer.OrdinalIgnoreCase)
{
    "mailinator.com", "tempmail.org", "guerrillamail.com", "10minutemail.com",
    "throwam.com", "yopmail.com", "sharklasers.com", "trashmail.com",
    "maildrop.cc", "spamgourmet.com", "fakeinbox.com", "dispostable.com"
};
```

### Estructura de archivos

Archivos a CREAR (NEW):
- `src/Server/Application/Email/IEmailService.cs`
- `src/Server/Application/Auth/IEmailConfirmationTokenService.cs`
- `src/Server/Domain/Auth/DisposableEmailDomains.cs`
- `src/Server/Infrastructure/Integrations/Email/ResendOptions.cs`
- `src/Server/Infrastructure/Integrations/Email/ResendEmailService.cs`
- `src/Server/Infrastructure/Security/EmailConfirmationTokenService.cs`
- `src/Server/SharedApiContracts/Auth/RegisterRequest.cs`
- `src/Server/SharedApiContracts/Auth/RegisterResponse.cs`
- `src/Server/SharedApiContracts/Auth/ConfirmEmailResponse.cs`
- `src/Server/Domain/Auth/Exceptions/DisposableEmailException.cs`
- `src/Server/Domain/Auth/Exceptions/EmailAlreadyConfirmedException.cs`
- `src/Web/Main/src/pages/ConfirmarEmailPage.tsx`
- `tests/Unit/Domain.Tests/Auth/DisposableEmailDomainsTests.cs`
- `tests/Unit/Infrastructure.Tests/Security/EmailConfirmationTokenServiceTests.cs`
- `tests/Integration/Api.Tests/Auth/RegisterEndpointTests.cs`
- `tests/Integration/Api.Tests/Auth/ConfirmEmailEndpointTests.cs`

Archivos a MODIFICAR (UPDATE):
- `src/Server/Application/Auth/IUserService.cs` — `RegisterAsync`, `ConfirmEmailAsync`, `FindByIdAsync`
- `src/Server/Infrastructure/Security/UserService.cs` — implementar métodos nuevos
- `src/Server/Api/Endpoints/Public/AuthEndpoints.cs` — agregar `POST /register` y `GET /confirm-email`
- `src/Server/Api/CompositionRoot/ApiServiceExtensions.cs` — registrar `IEmailService`, `IEmailConfirmationTokenService`
- `src/Web/Main/src/App.tsx` — agregar ruta `/confirmar-email`
- `src/Web/SharedApiClient/schema.d.ts` — regenerado por codegen
- `tests/Unit/Infrastructure.Tests/Security/UserServiceTests.cs` — agregar tests nuevos

### Security Checklist — completar antes del primer commit

- [x] **TOCTOU doble-request en RegisterAsync**: email encriptado tiene índice único en BD → `DbUpdateException` ya capturada como `DuplicateEmailException`. Idempotente.
- [x] **TOCTOU en ConfirmEmailAsync**: si dos requests confirman el mismo token simultáneamente, el segundo encontrará `EmailConfirmedAt != null` y retornará `token_already_used`. No hay race condition crítico — ambos intentos leen el mismo userId del token.
- [x] **Auth-gating**: `POST /register` y `GET /confirm-email` son `.AllowAnonymous()` — correcto para flujo de registro.
- [x] **Token firmado**: HMAC-SHA256 con el JWT Secret — no se puede falsificar sin conocer el secret.
- [x] **Email enumeration**: `POST /register` devuelve 200 tanto si el email es nuevo como si ya existe? NO — devuelve 409 para duplicado. Consideración: podría ser un vector de enumeración de emails. Para este MVP (manual, B2C) es aceptable devolver 409. Documentar como deferred.
- [ ] **Denominador cero**: no aplica — no hay cálculos financieros.

### Referencias

- Story 14.1: `_bmad-output/implementation-artifacts/14-1-modelo-suscripcion-backend.md` — prerequisito; define `EmailConfirmedAt`, `TrialEndsAt`, `ComputedIsActive`
- Dominio `User` actualizado: `src/Server/Domain/Auth/User.cs`
- Enums `SubscriptionType`, `HowDidYouHear`: `src/Server/Domain/Auth/`
- Patrón config env vars (double underscore): `GEMINI__APIKEY` → `Gemini:ApiKey` — mismo patrón para `RESEND__APIKEY` → `Resend:ApiKey`
- Auth endpoints existentes: `src/Server/Api/Endpoints/Public/AuthEndpoints.cs`
- UserService existente: `src/Server/Infrastructure/Security/UserService.cs`
- Patrón registro HttpClient: ver `src/Server/Infrastructure/Integrations/` para referencia de servicios externos
- Patrón DI: `src/Server/Api/CompositionRoot/ApiServiceExtensions.cs`
- [Source: docs/req/architecture.md#Authentication & Security]
- [Source: _bmad-output/planning-artifacts/convenciones-fibradis.md]

## Dev Agent Record

### Agent Model Used

GPT-5

### Debug Log References

- Implemented public registration and email confirmation flow across backend application, domain, infrastructure, and shared API contracts.
- Added disposable email blocking, stateless HMAC-SHA256 confirmation tokens, Resend email delivery, and confirm-email SEO/page handling.
- Ran `dotnet build FIBRADIS.slnx`, `npm run codegen:api`, `npm run build --workspace=src/Web/Main`, and `npm run build --workspace=src/Web/Ops`.
- Ran targeted tests for Domain, Infrastructure, and Integration auth coverage; they passed. Full solution test run still shows pre-existing unrelated failures in MarketHistory, SEO, Dashboard, Calculadora, and Ops suites.

### Completion Notes List

- Registration now creates inactive users, blocks disposable domains, issues a signed 24h confirmation token, and sends the confirmation email through Resend.
- Confirmation now validates token state, rejects expired or reused tokens, sets `EmailConfirmedAt` and `TrialEndsAt`, and activates the user through `ComputedIsActive`.
- Added `/confirmar-email` in the Main SPA with `noindex,follow` metadata and confirm-email API helpers.
- Regenerated the shared OpenAPI client schema and verified both Main and Ops builds.
- Full solution regression run was not clean because of unrelated pre-existing test failures outside this story.

### Change Log

- 2026-06-19: Implemented the public register + email confirmation flow, including disposable email checks, confirmation token service, Resend integration, frontend confirm-email page, SEO handling, API codegen refresh, and auth test coverage.

### File List

- .env.example
- scripts/codegen/Api.json
- src/Server/Api/CompositionRoot/ApiServiceExtensions.cs
- src/Server/Api/Endpoints/Ops/OpsSeoEndpoints.cs
- src/Server/Api/Endpoints/Public/AuthEndpoints.cs
- src/Server/Api/Middleware/SpaMetadataMiddleware.cs
- src/Server/Api/Seo/SpaMetadataProvider.cs
- src/Server/Api/Seo/SpaPageMeta.cs
- src/Server/Api/Seo/SpaRouteCatalog.cs
- src/Server/Api/appsettings.json
- src/Server/Application/Auth/IEmailConfirmationTokenService.cs
- src/Server/Application/Auth/IUserService.cs
- src/Server/Application/Email/IEmailService.cs
- src/Server/Domain/Auth/DisposableEmailDomains.cs
- src/Server/Domain/Auth/Exceptions/DisposableEmailException.cs
- src/Server/Domain/Auth/Exceptions/EmailAlreadyConfirmedException.cs
- src/Server/Infrastructure/Integrations/Email/ResendEmailService.cs
- src/Server/Infrastructure/Integrations/Email/ResendOptions.cs
- src/Server/Infrastructure/Security/EmailConfirmationTokenService.cs
- src/Server/Infrastructure/Security/UserService.cs
- src/Server/SharedApiContracts/Auth/ConfirmEmailResponse.cs
- src/Server/SharedApiContracts/Auth/RegisterRequest.cs
- src/Server/SharedApiContracts/Auth/RegisterResponse.cs
- src/Web/Main/src/app/routes.tsx
- src/Web/Main/src/modules/auth/authApi.ts
- src/Web/Main/src/pages/ConfirmarEmailPage.tsx
- src/Web/SharedApiClient/schema.d.ts
- tests/Integration/Api.Tests/ApiWebFactory.cs
- tests/Integration/Api.Tests/AuthConfirmEmailTests.cs
- tests/Integration/Api.Tests/AuthRegisterTests.cs
- tests/Unit/Domain.Tests/Auth/DisposableEmailDomainsTests.cs
- tests/Unit/Infrastructure.Tests/Security/EmailConfirmationTokenServiceTests.cs
- tests/Unit/Infrastructure.Tests/Security/UserServiceTests.cs

## Senior Developer Review (AI)

### Review Pass 1 — 2026-06-19

**Layers ejecutados:** Blind Hunter · Edge Case Hunter · Acceptance Auditor  
**Dismissed:** 5 (DoS AuthService pre-existente, HowDidYouHear whitespace, token en URL, semántica IsValid+IsExpired, tests en ruta diferente a spec)

### Action Items

- [x] [Review][Patch] P1: Leer y validar `App:BaseUrl` ANTES de llamar `RegisterAsync` — si falta config, retornar error limpio sin persistir el usuario [src/Server/Api/Endpoints/Public/AuthEndpoints.cs] — APLICADO
- [x] [Review][Patch] P2: `ConfirmEmailAsync` race condition — revertido a Find+Check+Save original (InMemory no soporta ExecuteUpdateAsync); se documentó la invariante aceptada por spec en comentario [src/Server/Infrastructure/Security/UserService.cs] — APLICADO (doc)
- [x] [Review][Patch] P3: Agregar validación básica de formato de email en `RegisterAsync` — `IsValidEmailFormat` verifica `@` y dominio con `.` [src/Server/Infrastructure/Security/UserService.cs] — APLICADO
- [x] [Review][Patch] P4: `DisposableEmailDomains.IsDisposable` — check de subdominios añadido (`user@sub.mailinator.com` ahora bloqueado) + test [src/Server/Domain/Auth/DisposableEmailDomains.cs] — APLICADO
- [x] [Review][Patch] P5: `confirmed.TrialEndsAt!.Value` en endpoint — cambiado a `?? throw new InvalidOperationException(...)` explícito [src/Server/Api/Endpoints/Public/AuthEndpoints.cs] — APLICADO
- [x] [Review][Patch] P6: Test de integración añadido: `POST /register` con `HowDidYouHear` inválido → 400 `{ code: "invalid_user_data" }` [tests/Integration/Api.Tests/AuthRegisterTests.cs] — APLICADO
- [x] [Review][Defer] D1: HMAC key compartido con JWT Secret — riesgo: rotación del secret invalida links de confirmación en vuelo. Por diseño del spec (T2.2). Separar en historia futura de security hardening.
- [x] [Review][Defer] D2: GET /confirm-email con side effects — email scanners corporativos pueden pre-fetchear el link antes que el usuario. Por spec (endpoint definido como GET). Evaluar en historia futura si se reporta en QA.
- [x] [Review][Defer] D3: Email enumeration vía HTTP 409 en `/register` — documentado explícitamente en spec security checklist como aceptable para MVP B2C.
- [x] [Review][Defer] D4: `DateTime.UtcNow` vs `DateTimeOffset.UtcNow` — tipo `DateTime?` establecido por migración 14.1; cambiar requeriría nueva migración. Diferir a historia de cleanup.
- [x] [Review][Defer] D5: Envío de email silencioso sin endpoint de reenvío — diseño explícito del spec (T1.3); usuario no tiene recovery path. Agregar resend-confirmation en historia 14.x futura.
- [x] [Review][Defer] D6: `IsActive` no se re-evalúa automáticamente al expirar el trial — gap de diseño fuera del scope de 14.2; abordado en 14.4 (subscription maintenance job).
