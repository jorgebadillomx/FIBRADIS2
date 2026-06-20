# Story 14.9: Confirmación de email resiliente (server-side redirect)

Status: done

## Story

Como usuario que hace clic en el enlace de confirmación desde un cliente de email,
quiero que mi cuenta se confirme aunque el enlace se abra en un contexto sandboxed (iframe sin `allow-scripts`),
para que el primer paso de activación no quede silenciosamente bloqueado por la sandbox del cliente de correo.

## Contexto y motivación

**El problema detectado en producción:** cuando un usuario abre el link de confirmación desde Opera (u otros clientes de email que renderizan links en iframes sandboxed), el navegador bloquea todos los scripts con:

```
Blocked script execution in '...confirmar-email?token=...' because the document's frame is sandboxed and the 'allow-scripts' permission is not set.
```

React nunca carga → `useQuery` nunca corre → la API de confirmación nunca recibe el token → **la cuenta queda sin confirmar**, aunque el token era válido. El usuario no recibe ningún mensaje de error porque el React tampoco monta.

**Alcance de la investigación:** se auditaron todos los flujos donde llegar desde un link externo dispara una acción crítica dependiente de JS:

| Página | Param crítico | API llamada | Riesgo |
|---|---|---|---|
| `/confirmar-email` | `?token=` | `GET /api/v1/auth/confirm-email` | **CRÍTICO** — falla silenciosamente |
| `/activar` | `?reason=` | POST (botones) | Bajo — sólo muestra vista incorrecta; el usuario llega desde el app, no desde email |
| `/portafolio`, `/oportunidades`, etc. | sin token sensible | N/A | Sin riesgo |

**Solución:** mover la confirmación al servidor con un nuevo endpoint de redirección. El link del email apunta al endpoint, que confirma server-side y redirige a la página React con un `?status=` en lugar del token. La página muestra el resultado sin necesidad de JS para llamar a la API.

## Acceptance Criteria

1. **Dado que** recibo el email de confirmación (registro nuevo o reenvío),
   **Entonces** el link que contiene apunta a `/api/v1/auth/confirm-email-redirect?token=<token>` (antes: `/confirmar-email?token=<token>`).

2. **Dado que** hago `GET /api/v1/auth/confirm-email-redirect?token=<token_válido>`,
   **Entonces** el servidor confirma el email, activa el trial, y devuelve `302 Location: /confirmar-email?status=confirmed&t=<ISO-date-URL-encoded>` donde `t` es el valor de `TrialEndsAt`.

3. **Dado que** hago `GET /api/v1/auth/confirm-email-redirect?token=<token_expirado>`,
   **Entonces** devuelve `302 Location: /confirmar-email?status=expired`.

4. **Dado que** hago `GET /api/v1/auth/confirm-email-redirect?token=<token_ya_usado>`,
   **Entonces** devuelve `302 Location: /confirmar-email?status=already_confirmed`.

5. **Dado que** hago `GET /api/v1/auth/confirm-email-redirect?token=<token_inválido_o_ausente>`,
   **Entonces** devuelve `302 Location: /confirmar-email?status=error`.

6. **Dado que** navego a `/confirmar-email?status=confirmed&t=<ISO>`,
   **Entonces** la página muestra "¡Cuenta confirmada!" con la fecha de fin de trial calculada a partir del param `t` — **sin llamar a ninguna API**.

7. **Dado que** navego a `/confirmar-email?status=expired`,
   **Entonces** la página muestra el estado de token expirado con botón "Reenviar confirmación" → `/activar?reason=trial_not_started`.

8. **Dado que** navego a `/confirmar-email?status=already_confirmed`,
   **Entonces** la página muestra "Tu cuenta ya fue confirmada" con botón "Iniciar sesión".

9. **Dado que** navego a `/confirmar-email?status=error`,
   **Entonces** la página muestra el estado de error genérico.

10. **Dado que** navego a `/confirmar-email?token=<token>` (link antiguo — compat. hacia atrás),
    **Entonces** el comportamiento actual se preserva: el React llama a `GET /api/v1/auth/confirm-email` y muestra el resultado. El endpoint JSON original **no se modifica**.

11. **Dado que** reviso el contrato OpenAPI,
    **Entonces** el nuevo endpoint aparece documentado. El cliente TypeScript generado por codegen **no necesita actualización** porque el nuevo endpoint no es llamado desde el frontend (sólo redirige).

## Tasks / Subtasks

- [x] T1: Nuevo endpoint `GET /api/v1/auth/confirm-email-redirect` en `AuthEndpoints.cs` (AC: 2, 3, 4, 5, 11)
  - [x] T1.1: Agregar `group.MapGet("/confirm-email-redirect", ...)` reutilizando la lógica de validación del endpoint existente.
  - [x] T1.2: Siempre devolver `Results.Redirect(redirectUrl)` (HTTP 302).
    - Éxito: `"/confirmar-email?status=confirmed&t=" + Uri.EscapeDataString(trialEndsAt.ToString("O"))`
    - Expirado: `"/confirmar-email?status=expired"`
    - Ya usado: `"/confirmar-email?status=already_confirmed"`
    - Inválido / ausente: `"/confirmar-email?status=error"`
  - [x] T1.3: Marcar `.AllowAnonymous()`. Sin `Produces<>` de cuerpo JSON — sólo declarar `Produces(StatusCodes.Status302Found)`.

- [x] T2: Actualizar las dos fuentes de `confirmationUrl` (AC: 1)
  - [x] T2.1: `src/Server/Api/Endpoints/Public/AuthEndpoints.cs` línea 60 — cambiar `/confirmar-email` → `/api/v1/auth/confirm-email-redirect` en la plantilla de la URL.
  - [x] T2.2: `src/Server/Infrastructure/Security/UserService.cs` línea 341 — mismo cambio en `ResendConfirmationAsync`.
  - [x] T2.3: Verificar que `baseUrl` ya incluye el dominio raíz sin trailing slash (lo hace hoy con `TrimEnd('/')`); el path resultante queda `{baseUrl}/api/v1/auth/confirm-email-redirect?token={token}`.

- [x] T3: Actualizar `ConfirmarEmailPage.tsx` para manejar `?status=` (AC: 6, 7, 8, 9, 10)
  - [x] T3.1: El componente lee `searchParams.get('status')` además de `searchParams.get('token')`.
  - [x] T3.2: Prioridad de resolución: si `status` está presente → flujo estático (sin API); si sólo `token` está presente → flujo API actual (sin cambios); si ninguno → "Enlace inválido".
  - [x] T3.3: Para `status === 'confirmed'`: leer `searchParams.get('t')` → parsear ISO date → formatear con `formatTrialEndsAt` existente → mostrar el mismo `ConfirmCard` de éxito que hoy.
  - [x] T3.4: Para `status === 'expired'`: mostrar `ConfirmCard` de token expirado (mismo que hoy en el branch `token_expired`).
  - [x] T3.5: Para `status === 'already_confirmed'`: mostrar `ConfirmCard` de ya confirmado (mismo que hoy en el branch `token_already_used`).
  - [x] T3.6: Para `status === 'error'` o cualquier valor desconocido: mostrar `ConfirmCard` de error genérico.

- [x] T4: Tests del nuevo endpoint (AC: 2, 3, 4, 5)
  - [x] T4.1: Test `ConfirmEmailRedirect_ValidToken_Redirects302ToConfirmed` — token válido → 302, Location contiene `/confirmar-email?status=confirmed&t=`.
  - [x] T4.2: Test `ConfirmEmailRedirect_ExpiredToken_Redirects302ToExpired` — token expirado → 302, Location = `/confirmar-email?status=expired`.
  - [x] T4.3: Test `ConfirmEmailRedirect_AlreadyUsedToken_Redirects302ToAlreadyConfirmed` — email ya confirmado → 302, Location = `/confirmar-email?status=already_confirmed`.
  - [x] T4.4: Test `ConfirmEmailRedirect_InvalidToken_Redirects302ToError` — token inválido → 302, Location = `/confirmar-email?status=error`.

- [x] T5: Verificación build (AC: todos)
  - [x] T5.1: `dotnet build FIBRADIS.slnx` — 0 errores.
  - [x] T5.2: `npm run build --workspace=src/Web/Main` — 0 errores TypeScript.
  - [x] T5.3: Smoke test manual: usando la URL de redirect directamente en el navegador verificar que la confirmación ocurre y la redirección llega a la página de éxito con la fecha correcta.

## Dev Notes

### Archivos a modificar (ninguno nuevo)

| Archivo | Cambio |
|---|---|
| `src/Server/Api/Endpoints/Public/AuthEndpoints.cs` | Añadir endpoint `/confirm-email-redirect` + cambiar `confirmationUrl` en `/register` (línea 60) |
| `src/Server/Infrastructure/Security/UserService.cs` | Cambiar `confirmationUrl` en `ResendConfirmationAsync` (línea 341) |
| `src/Web/Main/src/pages/ConfirmarEmailPage.tsx` | Manejar `?status=` además de `?token=` |

### Estado actual de los archivos que se modifican

**`AuthEndpoints.cs` línea 58-61** (cómo construye el link hoy):
```csharp
var token = tokenService.GenerateToken(user.Id);
var confirmationUrl = $"{baseUrl}/confirmar-email?token={Uri.EscapeDataString(token)}";
await emailService.SendEmailConfirmationAsync(user.Email, confirmationUrl, ct);
```
→ Cambiar `/confirmar-email` → `/api/v1/auth/confirm-email-redirect`.

**`UserService.cs` líneas 340-342** (mismo patrón en reenvío):
```csharp
var token = tokenService.GenerateToken(user.Id);
var confirmationUrl = $"{baseUrl}/confirmar-email?token={Uri.EscapeDataString(token)}";
await emailService.SendEmailConfirmationAsync(normalizedEmail, confirmationUrl, ct);
```
→ Mismo cambio.

**Endpoint `/confirm-email` existente** (líneas 89-125 de AuthEndpoints.cs): **NO tocar**. Preservar para compatibilidad con links viejos que el React aún puede manejar.

### Implementación del nuevo endpoint (esqueleto)

```csharp
group.MapGet("/confirm-email-redirect", async (
    string? token,
    IEmailConfirmationTokenService tokenService,
    IUserService userService,
    IConfiguration config,
    CancellationToken ct) =>
{
    var baseUrl = config["App:BaseUrl"]?.TrimEnd('/') ?? string.Empty;

    var validation = tokenService.ValidateToken(token ?? string.Empty);
    if (!validation.IsValid)
        return Results.Redirect($"{baseUrl}/confirmar-email?status=error");
    if (validation.IsExpired)
        return Results.Redirect($"{baseUrl}/confirmar-email?status=expired");

    var user = await userService.FindByIdAsync(validation.UserId, ct);
    if (user is null)
        return Results.Redirect($"{baseUrl}/confirmar-email?status=error");
    if (user.EmailConfirmedAt is not null)
        return Results.Redirect($"{baseUrl}/confirmar-email?status=already_confirmed");

    try
    {
        var confirmed = await userService.ConfirmEmailAsync(validation.UserId, ct);
        var trialEndsAt = confirmed.TrialEndsAt
            ?? throw new InvalidOperationException("ConfirmEmailAsync no asignó TrialEndsAt.");
        var dateParam = Uri.EscapeDataString(
            new DateTimeOffset(DateTime.SpecifyKind(trialEndsAt, DateTimeKind.Utc)).ToString("O"));
        return Results.Redirect($"{baseUrl}/confirmar-email?status=confirmed&t={dateParam}");
    }
    catch (EmailAlreadyConfirmedException)
    {
        return Results.Redirect($"{baseUrl}/confirmar-email?status=already_confirmed");
    }
})
.AllowAnonymous()
.Produces(StatusCodes.Status302Found);
```

### Cambio en `ConfirmarEmailPage.tsx` (lógica de prioridad)

```tsx
const [searchParams] = useSearchParams()
const status = searchParams.get('status')   // nuevo: status directo del redirect
const token = searchParams.get('token')     // legacy: token para llamada API
const trialParam = searchParams.get('t')    // acompañante de status=confirmed

// Flujo estático (status presente — no llama API)
if (status !== null) {
  if (status === 'confirmed') {
    const trialEndsAt = trialParam ? decodeURIComponent(trialParam) : null
    // mostrar éxito usando formatTrialEndsAt(trialEndsAt) si trialEndsAt != null
  }
  if (status === 'expired') { /* mostrar ConfirmCard expirado */ }
  if (status === 'already_confirmed') { /* mostrar ConfirmCard ya confirmado */ }
  /* cualquier otro status → error genérico */
}

// Flujo API (token presente — comportamiento legacy)
const confirmationQuery = useQuery({
  queryKey: ['auth', 'confirm-email', token],
  queryFn: () => confirmEmail(token ?? ''),
  enabled: Boolean(token) && status === null,  // desactivar si status ya está presente
  ...
})
```

### Invariante que debe preservarse

El endpoint existente `GET /api/v1/auth/confirm-email` (JSON) **no cambia**. Los links viejos (`/confirmar-email?token=xxx`) siguen funcionando por la rama `enabled: Boolean(token) && status === null`.

### Tests: ubicación y patrón

Agregar tests en `src/Server/Tests/Api/Auth/ConfirmEmailRedirectTests.cs` (crear archivo nuevo) usando `WebApplicationFactory` + cliente HTTP, igual que los tests de confirmación existentes en `ConfirmEmailTests.cs` o similar. Los tests verifican el status code 302 y el header `Location` del response.

### Resend templates (historia 14-5)

`emailService.SendEmailConfirmationAsync(email, confirmationUrl, ct)` recibe la URL ya construida. Los templates de Resend usan la URL que les pasamos — no hay variable de URL hardcodeada en el template mismo. Por lo tanto, sólo hay que cambiar las dos líneas donde se construye `confirmationUrl`.

### Parámetro `t` — formato y parsing

- Backend genera: `DateTimeOffset.ToString("O")` → `"2026-07-04T00:00:00.0000000+00:00"`, URL-encoded.
- Frontend parsea: `new Date(decodeURIComponent(t))` → válido como ISO 8601, compatible con `formatTrialEndsAt` existente que ya hace `new Date(value)`.

## Story Progress Notes

### Agent Model Used
GPT-5

### File List
- `_bmad-output/implementation-artifacts/14-9-confirmacion-email-resiliente.md`
- `scripts/codegen/Api.json`
- `src/Server/Api/Endpoints/Public/AuthEndpoints.cs`
- `src/Server/Infrastructure/Security/UserService.cs`
- `src/Web/Main/src/pages/ConfirmarEmailPage.tsx`
- `src/Web/Main/tests/e2e/auth-confirmation-redirect.spec.ts`
- `tests/Integration/Api.Tests/AuthRegisterTests.cs`
- `tests/Integration/Api.Tests/ConfirmEmailRedirectTests.cs`
- `tests/Integration/Api.Tests/OpenApiEndpointTests.cs`
- `tests/Unit/Infrastructure.Tests/Security/UserServiceTests.cs`

### Completion Notes
- Implementado `GET /api/v1/auth/confirm-email-redirect` con redirección 302 a `status=confirmed|expired|already_confirmed|error`.
- Actualizadas las dos fuentes de `confirmationUrl` para usar el endpoint de redirect en registro y reenvío.
- `ConfirmarEmailPage` ahora resuelve `status=` sin API y conserva el flujo legacy por `?token=`.
- Añadida cobertura backend para el endpoint, el email source de reenvío, y la documentación OpenAPI; añadida cobertura e2e para los estados estáticos y el flujo legacy.
- Verificaciones ejecutadas: `dotnet test tests/Integration/Api.Tests/Api.Tests.csproj` (362 passed), `dotnet test tests/Unit/Infrastructure.Tests/Infrastructure.Tests.csproj --filter UserServiceTests` (50 passed), `dotnet build FIBRADIS.slnx` (0 errores), `npm run build --workspace=src/Web/Main` (0 errores), `npx playwright test tests/e2e/auth-confirmation-redirect.spec.ts` (4 passed).
- El wrapper completo de e2e mostró fallos ajenos preexistentes en otras specs sin backend; el smoke de esta historia pasó de forma aislada.

### Change Log
- 2026-06-20: Historia creada — issue detectado en producción al abrir el link de confirmación en Opera (sandboxed iframe del email client).
- 2026-06-20: Implementado redirect server-side para confirmación de email, actualización de links de confirmación y soporte de `status=` en la página pública.

## Senior Developer Review (AI)

**Capas ejecutadas:** Acceptance Auditor (agente paralelo), Blind Hunter (inline), Edge Case Hunter (inline con acceso al proyecto).
**Capa `blind` y `edge` no disponibles como subagent_type** — ejecutadas inline en contexto del revisor.

### Action Items

- [x] [Review][Patch] P1: Falta test e2e para `status=error` (AC-9 sin cobertura e2e) [`src/Web/Main/tests/e2e/auth-confirmation-redirect.spec.ts`] — aplicado
- [x] [Review][Patch] P2: Sin guard para `t` param malformado — `formatTrialEndsAt` retorna `null` en lugar del string crudo para fechas inválidas [`src/Web/Main/src/pages/ConfirmarEmailPage.tsx:14`] — aplicado
- [x] [Review][Defer] D1: `DateTime.SpecifyKind(trialEndsAt, DateTimeKind.Utc)` asume que `TrialEndsAt` de EF es UTC — pre-existente en `/confirm-email` endpoint [`src/Server/Api/Endpoints/Public/AuthEndpoints.cs:125`] — deferred, pre-existing
- [x] [Review][Defer] D2: `formatTrialEndsAt` muestra hora en UTC (`timeZone: 'UTC'`) — usuarios mexicanos pueden ver fecha un día antes — pre-existente en la función [`src/Web/Main/src/pages/ConfirmarEmailPage.tsx`] — deferred, pre-existing
- [x] [Review][Defer] D3: Endpoint OpenAPI `/confirm-email-redirect` sin `operationId` — patrón de todo el proyecto [`scripts/codegen/Api.json`] — deferred, pre-existing
