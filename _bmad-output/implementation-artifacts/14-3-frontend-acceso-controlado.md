# Story 14.3: Frontend — acceso controlado y páginas de conversión

Status: done

## Story

Como visitante o usuario con trial expirado,
quiero ver una pantalla clara que explique mi estado y me dé los pasos concretos para activar o reactivar el acceso,
para que la fricción de conversión sea mínima.

## Acceptance Criteria

1. **Dado que** el usuario autenticado tiene `isActive = false` e intenta acceder a `/portafolio`, `/oportunidades`, `/herramientas`, `/reportes` o `/perfil`, **Entonces** es redirigido a `/activar` con `?reason=trial_expired` o `?reason=trial_not_started` según corresponda.

2. **Dado que** el usuario no autenticado intenta acceder a una ruta privada, **Entonces** es redirigido a `/login` (comportamiento existente, sin cambio).

3. **Dado que** navego a `/registro`, **Entonces** veo un formulario con campos Email (requerido), Contraseña (requerido), Nombre (opcional) y ¿Cómo nos encontraste? (select: Google / Redes sociales / Recomendación / Otro). Al enviar con éxito, la UI muestra "Revisa tu email para confirmar tu cuenta".

4. **Dado que** navego a `/confirmar-email?token=xxx`, **Entonces** la página llama al endpoint de confirmación y, si es exitoso, muestra "¡Cuenta confirmada! Tu prueba de 14 días ha comenzado" con un botón "Ir a mi portafolio" (ya implementado en 14.2, verificar que el botón diga "Ir a mi portafolio" y no "Iniciar sesión").

5. **Dado que** navego a `/activar` con `?reason=trial_expired`, **Entonces** veo el título "Tu prueba de 14 días ha terminado", los tres planes con sus precios (Mensual / Anual / Lifetime), las instrucciones de transferencia bancaria (CLABE + banco), y el botón "Ya pagué — notificar al equipo" que envía un email automático a portafoliodefibras@gmail.com con el UserId.

6. **Dado que** navego a `/activar` con `?reason=trial_not_started`, **Entonces** veo el título "Confirma tu email para comenzar tu prueba gratuita", una descripción de los 14 días, y un botón "Reenviar email de confirmación".

7. **Dado que** llamo `GET /api/v1/account/me` como usuario autenticado, **Entonces** la respuesta incluye `isActive: bool`, `trialEndsAt: string|null` y `paidAt: string|null` además de los campos existentes (`email`, `role`, `apodo`).

## Tasks / Subtasks

- [x] T1: Backend — extender `GET /api/v1/account/me` (AC: 7)
  - [x] T1.1: Extender `UserProfileData` en `src/Server/Application/Auth/UserProfileData.cs` — agregar `bool IsActive`, `DateTime? TrialEndsAt`, `DateTime? FechaPago` (alias para paidAt)
  - [x] T1.2: Actualizar `UserService.GetProfileAsync` — incluir los nuevos campos al construir `UserProfileData`
  - [x] T1.3: Extender `UserProfileResponse` en `src/Server/SharedApiContracts/Auth/UserProfileResponse.cs` — agregar `bool IsActive`, `string? TrialEndsAt` (ISO8601 UTC), `string? PaidAt`
  - [x] T1.4: Actualizar `AccountEndpoints.cs` — mapear los nuevos campos al construir `UserProfileResponse`

- [x] T2: Codegen API (AC: 7)
  - [x] T2.1: `npm run codegen:api` para regenerar `src/Web/SharedApiClient/schema.d.ts` con `UserProfileResponse.isActive`, `UserProfileResponse.trialEndsAt`, `UserProfileResponse.paidAt`

- [x] T3: AuthContext — exponer `isActive` y `trialEndsAt` (AC: 1, 2)
  - [x] T3.1: En `AuthContext.tsx`: agregar estado `isActive: boolean` y `trialEndsAt: string | null`
  - [x] T3.2: Cargar el perfil vía `fetchProfile()` después del bootstrap cuando el usuario está autenticado — almacenar `isActive` y `trialEndsAt` en el contexto
  - [x] T3.3: Exponer en `AuthContextValue`: `isActive: boolean`, `trialEndsAt: string | null`
  - [x] T3.4: Al hacer logout, resetear `isActive = false` y `trialEndsAt = null`

- [x] T4: ProtectedRoute — guard de `isActive` (AC: 1, 2)
  - [x] T4.1: En `ProtectedRoute.tsx`: cuando `status === 'authenticated'` y `isActive === false`, determinar `reason`:
    - `trialEndsAt === null` → `?reason=trial_not_started`
    - `trialEndsAt !== null` → `?reason=trial_expired`
    - Redirigir a `/activar?reason=<reason>` con `replace`
  - [x] T4.2: Solo cuando `isActive === true` renderizar `<Outlet />`

- [x] T5: PortafolioRoute — guard de `isActive` (AC: 1)
  - [x] T5.1: En `PortafolioRoute.tsx`: usar `isActive` del AuthContext — si `status === 'authenticated' && !isActive` → renderizar `<Navigate to="/activar?reason=..." replace />`
  - [x] T5.2: Actualizar `portafolio-route.ts` si la lógica de resolución necesita `isActive` como parámetro (N/A — lógica embebida directamente en PortafolioRoute.tsx)

- [x] T6: Página `/registro` — RegistroPage (AC: 3)
  - [x] T6.1: Crear `src/Web/Main/src/pages/RegistroPage.tsx`:
    - Formulario controlado con campos: Email, Contraseña, Nombre (opcional), ¿Cómo nos encontraste? (select)
    - Select options: `{ value: 'Google', label: 'Google' }`, `{ value: 'RedesSociales', label: 'Redes sociales' }`, `{ value: 'Recomendacion', label: 'Recomendación' }`, `{ value: 'Otro', label: 'Otro' }`
    - Al submit: llamar `registerUser(email, password, apodo, howDidYouHear)` en `authApi.ts`
    - Estado success: mostrar "Revisa tu email para confirmar tu cuenta"
    - Errores: 422 `disposable_email` → "Este dominio de email no está permitido", 409 `duplicate_email` → "Este email ya está registrado", genérico
    - Validación client-side mínima: email no vacío, contraseña ≥ 8 chars
    - SEO: `usePageTitle('Registro | Fibras Inmobiliarias', ..., { canonicalPath: '/registro', robotsDirectives: 'noindex,nofollow' })`
  - [x] T6.2: Agregar `registerUser` en `src/Web/Main/src/modules/auth/authApi.ts`:
    ```ts
    export async function registerUser(
      email: string, password: string, apodo?: string | null, howDidYouHear?: string | null
    ): Promise<RegisterResponse>
    ```
    - Usar `authClient['/api/v1/auth/register'].POST(...)` 
    - Relanzar error con `code` extraído del body para que la UI lo distinga

- [x] T7: Página `/activar` — ActivarPage (AC: 5, 6)
  - [x] T7.1: Crear `src/Web/Main/src/pages/ActivarPage.tsx`:
    - Leer `reason` del query string (`useSearchParams`)
    - **Si `reason === 'trial_expired'`**: título "Tu prueba de 14 días ha terminado", mostrar 3 planes con precios, instrucciones de pago (ver Dev Notes), botón "Ya pagué — notificar al equipo"
    - **Si `reason === 'trial_not_started'` (o ausente/desconocido)**: título "Confirma tu email para comenzar tu prueba gratuita", texto de 14 días, botón "Reenviar email de confirmación"
    - El botón "Ya pagué" llama a `POST /api/v1/auth/notify-payment` (nuevo endpoint) — ver T8
    - El botón "Reenviar email" llama a `POST /api/v1/auth/resend-confirmation` (nuevo endpoint) — ver T8
    - SEO: `usePageTitle('Activa tu cuenta | Fibras Inmobiliarias', ..., { robotsDirectives: 'noindex,nofollow' })`
  - [x] T7.2: Crear `src/Web/Main/src/modules/auth/subscriptionApi.ts` (o agregar a `authApi.ts`) con:
    - `notifyPayment(): Promise<void>` → `POST /api/v1/account/notify-payment` (autenticado)
    - `resendConfirmation(email: string): Promise<void>` → `POST /api/v1/auth/resend-confirmation` (anónimo, body: `{ email }`)

- [x] T8: Backend — endpoints auxiliares de suscripción (AC: 5, 6)
  - [x] T8.1: `POST /api/v1/account/notify-payment` (requiere auth) — envía email a `portafoliodefibras@gmail.com` con asunto "Notificación de pago" y cuerpo con `userId` del token. Responde 204 siempre (no fallar si Resend falla — loggear).
  - [x] T8.2: `POST /api/v1/auth/resend-confirmation` (anónimo, body: `{ email: string }`) — busca el usuario por email; si no existe o ya confirmó, igual responde 200 para no revelar estado. Si existe y no confirmado: genera nuevo token + envía email de confirmación. Responde `{ "message": "Si el email existe, recibirás un enlace." }`.
  - [x] T8.3: Agregar `IEmailService.SendPaymentNotificationAsync(Guid userId, CancellationToken ct)` e implementación en `ResendEmailService.cs`.
  - [x] T8.4: Agregar `ResendConfirmationAsync(string email, CancellationToken ct)` a `IUserService` + implementación en `UserService.cs`.

- [x] T9: Rutas SPA y SpaRouteCatalog (AC: 3, 5)
  - [x] T9.1: En `src/Web/Main/src/app/routes.tsx`: agregar lazy imports + rutas para `/registro` y `/activar`
    - `/registro` → `RegistroPage` (public, fuera de `ProtectedRoute`)
    - `/activar` → `ActivarPage` (public, fuera de `ProtectedRoute` — debe ser accesible por usuarios inactivos)
  - [x] T9.2: En `SpaRouteCatalog.cs`: agregar `"/registro"` y `"/activar"` a `KnownRoutes`
  - [x] T9.3: En `SpaMetadataProvider.cs` (o donde se registren rutas SEO): agregar entradas noindex para `/registro` y `/activar`

- [x] T10: Verificar `ConfirmarEmailPage` — AC 4 gap (AC: 4)
  - [x] T10.1: Verificar que el botón en el estado de éxito de `ConfirmarEmailPage` diga **"Ir a mi portafolio"** (no "Iniciar sesión"). Si dice "Iniciar sesión", cambiar a `<Link to="/portafolio">Ir a mi portafolio</Link>`.

- [x] T11: Unit tests (AC: 1, 7)
  - [x] T11.1: Test `UserProfileData` — verificar que `GetProfileAsync` devuelve `IsActive`, `TrialEndsAt`, `FechaPago` correctos (en `tests/Unit/Infrastructure.Tests/Security/UserServiceTests.cs`)
  - [x] T11.2: Test `ProtectedRoute` logic (si se usa utilidad pura) — N/A: lógica embebida directamente en componente (sin utilidad pura extraída); cubierta por T11.1 que valida los datos del backend.

- [x] T12: Build y verificación final
  - [x] T12.1: `dotnet build FIBRADIS.slnx` — 0 errores
  - [x] T12.2: `dotnet test tests/Unit/Infrastructure.Tests/` — 660 passed (1 fallo preexistente PortfolioPerformanceInpcTests ajeno a esta historia); UserServiceTests 38/38 verdes
  - [x] T12.3: `npm run build --workspace=src/Web/Main` — 0 errores; 188/188 frontend tests verdes

## Dev Notes

### Estado actual del código (prerequisito 14.2)

**14.2 ya mergeó a `main`?** No — el git log muestra que 14.2 está en el working tree como uncommitted changes (status: `done`). El branch actual `story/14-3-frontend-acceso-controlado` se bifurcó de `main` antes del merge de 14.2. Verificar si los cambios de 14.2 ya están en esta rama antes de empezar.

Si los cambios de 14.2 NO están, coordinar con el usuario para hacer el merge primero, o confirmar que el branch 14.3 se crea desde el working state correcto.

**Archivos ya implementados por 14.2 (NO recrear):**
- `src/Server/Application/Auth/IEmailConfirmationTokenService.cs`
- `src/Server/Application/Email/IEmailService.cs`
- `src/Server/Domain/Auth/DisposableEmailDomains.cs`
- `src/Server/Infrastructure/Integrations/Email/ResendEmailService.cs` (con `SendEmailConfirmationAsync`)
- `src/Server/Infrastructure/Security/EmailConfirmationTokenService.cs`
- `src/Server/SharedApiContracts/Auth/RegisterRequest.cs`, `RegisterResponse.cs`, `ConfirmEmailResponse.cs`
- `src/Server/Api/Endpoints/Public/AuthEndpoints.cs` (con `/register` y `/confirm-email`)
- `src/Web/Main/src/pages/ConfirmarEmailPage.tsx`
- `src/Web/Main/src/modules/auth/authApi.ts` (con `confirmEmail`, `registerUser` NO implementado aún)

### `UserProfileData` — extensión sin romper lo existente

`UserProfileData` es un record en `src/Server/Application/Auth/UserProfileData.cs`. Actualmente tiene 4 parámetros posicionales: `(Guid Id, string Email, string Role, string? Apodo)`.

Extender a:
```csharp
public sealed record UserProfileData(
    Guid Id,
    string Email,
    string Role,
    string? Apodo,
    bool IsActive,
    DateTime? TrialEndsAt,
    DateTime? FechaPago);
```

Actualizar el único sitio donde se construye (`UserService.GetProfileAsync`):
```csharp
return new UserProfileData(
    user.Id,
    emailEncryptor.Decrypt(user.Email),
    user.Role.ToString(),
    user.Apodo,
    user.IsActive,
    user.TrialEndsAt,
    user.FechaPago);
```

### `UserProfileResponse` — extensión del contrato

Actualmente: `public sealed record UserProfileResponse(string Email, string Role, string? Apodo);`

Extender a:
```csharp
public sealed record UserProfileResponse(
    string Email,
    string Role,
    string? Apodo,
    bool IsActive,
    string? TrialEndsAt,
    string? PaidAt);
```

El endpoint `GET /api/v1/account/me` en `AccountEndpoints.cs` pasa de:
```csharp
Results.Ok(new UserProfileResponse(profile.Email, profile.Role, profile.Apodo))
```
a:
```csharp
Results.Ok(new UserProfileResponse(
    profile.Email,
    profile.Role,
    profile.Apodo,
    profile.IsActive,
    profile.TrialEndsAt.HasValue
        ? DateTime.SpecifyKind(profile.TrialEndsAt.Value, DateTimeKind.Utc).ToString("O")
        : null,
    profile.FechaPago.HasValue
        ? DateTime.SpecifyKind(profile.FechaPago.Value, DateTimeKind.Utc).ToString("O")
        : null));
```

### AuthContext — estrategia de carga del perfil

El `AuthContext` actualmente **no llama** `GET /api/v1/account/me` — solo usa el access token (JWT claims) para saber si el usuario es `authenticated`. Para saber `isActive`, hay dos opciones:

**Opción A (recomendada): Llamar `fetchProfile()` después del bootstrap**
- En `bootstrapSession()`, después de restaurar el token, llamar `fetchProfile()` para cargar `isActive` y `trialEndsAt`.
- Almacenarlos en estado `[profileData, setProfileData]`.
- Si `fetchProfile()` falla (red), iniciar sesión de forma degradada con `isActive = true` (optimista) para no bloquear al usuario innecesariamente.

**Opción B: Incluir `isActive` como claim en el JWT**
- Requeriría cambiar el backend (`AuthService.LoginAsync`, `RefreshAsync`) para incluir el claim.
- Más eficiente pero más intrusivo — requiere modificar la generación de tokens.

La Opción A es la más segura para este scope (sin cambiar contratos de token). El ligero overhead del request adicional al perfil solo ocurre en bootstrap, no en cada render.

```typescript
// En AuthContext.tsx — después del bootstrap exitoso
const [isActive, setIsActive] = useState(true)  // optimistic default
const [trialEndsAt, setTrialEndsAt] = useState<string | null>(null)

// En bootstrapSession():
if (isAuth) {
  try {
    const profile = await fetchProfile()
    setIsActive(profile.isActive)
    setTrialEndsAt(profile.trialEndsAt ?? null)
  } catch {
    // Si falla el perfil, asumir activo (degraded mode)
    setIsActive(true)
    setTrialEndsAt(null)
  }
}
```

### ProtectedRoute — lógica de redirección

```tsx
if (status === 'authenticated' && !isActive) {
  const reason = trialEndsAt === null ? 'trial_not_started' : 'trial_expired'
  return <Navigate to={`/activar?reason=${reason}`} replace />
}
```

Nota: `PortafolioRoute` usa su propia lógica en `portafolio-route.ts` y NO extiende `ProtectedRoute`. Requiere el mismo guard de `isActive` aplicado en `PortafolioRoute.tsx` directamente.

### PortafolioRoute — extensión

`PortafolioRoute` actualmente recibe `status` de `useAuth()`. Agregar `isActive` y `trialEndsAt`:

```tsx
export function PortafolioRoute() {
  const { status, isActive, trialEndsAt } = useAuth()

  if (status === 'checking') return <PortafolioRouteLoader />

  if (status === 'authenticated' && !isActive) {
    const reason = trialEndsAt === null ? 'trial_not_started' : 'trial_expired'
    return <Navigate to={`/activar?reason=${reason}`} replace />
  }

  // landing ya resuelve si es anonymous
  return (
    <Suspense fallback={<PortafolioRouteLoader />}>
      {status === 'authenticated' ? <PortafolioPage /> : <PortafolioLanding />}
    </Suspense>
  )
}
```

No es necesario actualizar `portafolio-route.ts` — la lógica de `isActive` va directamente en `PortafolioRoute.tsx`.

### ActivarPage — contenido de planes y pago

Los precios exactos no están definidos en la arquitectura. Usar valores de referencia del PRD o epics. Si no están en los documentos, consultar con el usuario. Placeholder:
- **Mensual**: $299 MXN / mes
- **Anual**: $2,490 MXN / año (ahorra 30%)
- **Lifetime**: $6,999 MXN (pago único)

Instrucciones de transferencia:
- **CLABE**: consultar con el usuario antes de hardcodear — no está documentada en los planning artifacts
- **Banco**: consultar con el usuario
- **Contacto de confirmación**: `portafoliodefibras@gmail.com` (de AGENTS.md — marca Fibras Inmobiliarias)

**Si los precios y datos bancarios no están disponibles, usar placeholders visibles como `[PRECIO PENDIENTE]` y documentarlo en el Dev Agent Record — no inventar valores.**

### Endpoints auxiliares — diseño mínimo

**`POST /api/v1/account/notify-payment`** (requiere auth, en `AccountEndpoints.cs`):
```csharp
app.MapPost("/api/v1/account/notify-payment", async (
    IEmailService emailService,
    HttpContext ctx,
    ILogger<AccountEndpoints> logger,
    CancellationToken ct) =>
{
    var sub = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (!Guid.TryParse(sub, out var userId)) return Results.Unauthorized();
    
    await emailService.SendPaymentNotificationAsync(userId, ct);
    return Results.NoContent();
})
.RequireAuthorization()
.Produces(StatusCodes.Status204NoContent);
```

**`POST /api/v1/auth/resend-confirmation`** (anónimo, en `AuthEndpoints.cs`):
- Siempre responde 200 con el mismo mensaje (anti-enumeration)
- Si el usuario existe y no ha confirmado: generar nuevo token + enviar email
- Si el usuario no existe o ya confirmó: no hacer nada

```csharp
group.MapPost("/resend-confirmation", async (
    ResendConfirmationRequest request,
    IUserService userService,
    IEmailConfirmationTokenService tokenService,
    IEmailService emailService,
    IConfiguration config,
    CancellationToken ct) =>
{
    var baseUrl = config["App:BaseUrl"]?.TrimEnd('/');
    if (!string.IsNullOrWhiteSpace(baseUrl))
    {
        await userService.ResendConfirmationAsync(
            request.Email, tokenService, emailService, baseUrl, ct);
    }
    return Results.Ok(new { message = "Si el email existe, recibirás un enlace de confirmación." });
})
.AllowAnonymous();
```

Nuevo record: `public record ResendConfirmationRequest(string Email);`

### `IEmailService` — nuevo método

Agregar a la interfaz:
```csharp
Task SendPaymentNotificationAsync(Guid userId, CancellationToken ct);
```

Implementación en `ResendEmailService.cs`:
```csharp
public async Task SendPaymentNotificationAsync(Guid userId, CancellationToken ct)
{
    // Enviar a portafoliodefibras@gmail.com
    var payload = new { from = options.SenderEmail, to = new[] { "portafoliodefibras@gmail.com" },
        subject = "Notificación de pago — Fibras Inmobiliarias",
        html = $"<p>El usuario <strong>{userId}</strong> ha marcado su pago como realizado.</p>" };
    // ... mismo patrón que SendEmailConfirmationAsync
}
```

### `IUserService.ResendConfirmationAsync`

```csharp
Task ResendConfirmationAsync(
    string email,
    IEmailConfirmationTokenService tokenService,
    IEmailService emailService,
    string baseUrl,
    CancellationToken ct);
```

La implementación busca el usuario por email normalizado + encriptado. Si no existe o ya tiene `EmailConfirmedAt`, retorna silenciosamente. Si existe y no confirmado: genera token + envía email. Nunca lanza excepción al caller.

### SpaRouteCatalog — agregar rutas

En `SpaRouteCatalog.cs`, añadir a `KnownRoutes`:
```csharp
"/registro",
"/activar",
```

### SpaPageMeta / SpaMetadataProvider — agregar rutas SEO

En `SpaMetadataProvider.cs` o donde se registren las rutas con metadatos, agregar:
- `/registro` → `noindex,nofollow`, title "Registro | Fibras Inmobiliarias"
- `/activar` → `noindex,nofollow`, title "Activa tu cuenta | Fibras Inmobiliarias"

Ver patrón de `/confirmar-email` (ya existe) para referencia.

### `ConfirmarEmailPage` — gap de AC 4

Revisar `src/Web/Main/src/pages/ConfirmarEmailPage.tsx` en el estado de éxito. El spec de 14.3 pide botón **"Ir a mi portafolio"**. En la implementación actual de 14.2, el estado `token_already_used` tiene un botón "Iniciar sesión". Si el estado de éxito (200 OK) también dice "Iniciar sesión", corregirlo a "Ir a mi portafolio".

Verificar en el código actual:
```tsx
// Estado de éxito (confirmationQuery.isSuccess) debe tener:
<Button asChild>
  <Link to="/portafolio">Ir a mi portafolio</Link>
</Button>
```

### `registerUser` en `authApi.ts` — ya existe la interfaz

El tipo `RegisterResponse` ya está disponible en el schema generado. Agregar función:

```typescript
export async function registerUser(
  email: string,
  password: string,
  apodo?: string | null,
  howDidYouHear?: string | null,
): Promise<RegisterResponse> {
  const { data, error, response } = await authClient['/api/v1/auth/register'].POST({
    body: { email, password, apodo: apodo ?? null, howDidYouHear: howDidYouHear ?? null },
  })

  if (error) {
    const typedError = error as { code?: unknown }
    const code =
      typeof typedError.code === 'string'
        ? typedError.code
        : response.status === 422
          ? 'disposable_email'
          : response.status === 409
            ? 'duplicate_email'
            : 'register_failed'
    throw new AuthApiError(code, 'No se pudo completar el registro.')
  }

  if (!data) throw new AuthApiError('register_failed', 'La API no devolvió datos.')
  return data
}
```

### Estructura de archivos

Archivos a CREAR (NEW):
- `src/Web/Main/src/pages/RegistroPage.tsx`
- `src/Web/Main/src/pages/ActivarPage.tsx`
- `src/Server/SharedApiContracts/Auth/ResendConfirmationRequest.cs`

Archivos a MODIFICAR (UPDATE):
- `src/Server/Application/Auth/UserProfileData.cs` — agregar `IsActive`, `TrialEndsAt`, `FechaPago`
- `src/Server/Application/Auth/IUserService.cs` — agregar `ResendConfirmationAsync`
- `src/Server/Application/Email/IEmailService.cs` — agregar `SendPaymentNotificationAsync`
- `src/Server/Infrastructure/Security/UserService.cs` — actualizar `GetProfileAsync`, agregar `ResendConfirmationAsync`
- `src/Server/Infrastructure/Integrations/Email/ResendEmailService.cs` — agregar `SendPaymentNotificationAsync`
- `src/Server/SharedApiContracts/Auth/UserProfileResponse.cs` — agregar `IsActive`, `TrialEndsAt`, `PaidAt`
- `src/Server/Api/Endpoints/Private/AccountEndpoints.cs` — actualizar `GET /me`, agregar `POST /notify-payment`
- `src/Server/Api/Endpoints/Public/AuthEndpoints.cs` — agregar `POST /resend-confirmation`
- `src/Server/Api/Seo/SpaRouteCatalog.cs` — agregar `/registro`, `/activar`
- `src/Server/Api/Seo/SpaMetadataProvider.cs` — registrar metadatos de `/registro`, `/activar`
- `src/Web/Main/src/app/routes.tsx` — agregar lazy imports + rutas `/registro`, `/activar`
- `src/Web/Main/src/modules/auth/AuthContext.tsx` — agregar `isActive`, `trialEndsAt` al contexto
- `src/Web/Main/src/modules/auth/authApi.ts` — agregar `registerUser`, `notifyPayment`, `resendConfirmation`
- `src/Web/Main/src/modules/auth/ProtectedRoute.tsx` — guard de `isActive`
- `src/Web/Main/src/modules/portafolio/PortafolioRoute.tsx` — guard de `isActive`
- `src/Web/Main/src/pages/ConfirmarEmailPage.tsx` — corregir botón "Ir a mi portafolio" si aplica
- `src/Web/SharedApiClient/schema.d.ts` — regenerado por codegen
- `tests/Unit/Infrastructure.Tests/Security/UserServiceTests.cs` — tests de `GetProfileAsync`
- `tests/Integration/Api.Tests/ApiWebFactory.cs` — implementar `SendPaymentNotificationAsync` en `CapturingEmailService`

### Security Checklist

- [x] `POST /account/notify-payment` requiere `RequireAuthorization()` — no anónimo
- [x] `POST /auth/resend-confirmation` es anónimo pero responde igual en todos los casos (anti-enumeration)
- [x] El email de notificación de pago usa `portafoliodefibras@gmail.com` hardcodeado — no el email del usuario (no expone datos personales al admin si el email está encriptado en BD)
- [x] El guard de `isActive` en `ProtectedRoute` aplica SOLO a usuarios autenticados — no interfiere con el redirect a `/login` para usuarios anónimos
- [x] `/activar` y `/registro` son rutas públicas — un usuario inactivo debe poder acceder sin ser redirigido a `/login`

### Referencias

- Historia 14.2: `_bmad-output/implementation-artifacts/14-2-registro-y-confirmacion-email.md` — prerequisito; implementó `/register`, `/confirm-email`, `IEmailService`, `EmailConfirmationTokenService`
- Historia 14.1: `_bmad-output/implementation-artifacts/14-1-modelo-suscripcion-backend.md` — definió `IsActive`, `TrialEndsAt`, `ComputedIsActive` en el dominio
- `ProtectedRoute`: `src/Web/Main/src/modules/auth/ProtectedRoute.tsx`
- `AuthContext`: `src/Web/Main/src/modules/auth/AuthContext.tsx`
- `PortafolioRoute`: `src/Web/Main/src/modules/portafolio/PortafolioRoute.tsx`
- `SpaRouteCatalog`: `src/Server/Api/Seo/SpaRouteCatalog.cs`
- `SpaMetadataProvider`: `src/Server/Api/Seo/SpaMetadataProvider.cs`
- `IEmailService`: `src/Server/Application/Email/IEmailService.cs`
- `ResendEmailService`: `src/Server/Infrastructure/Integrations/Email/ResendEmailService.cs`
- Patrón `usePageTitle`: ver `src/Web/Main/src/pages/ConfirmarEmailPage.tsx`
- Marca contacto: `portafoliodefibras@gmail.com` (AGENTS.md)
- [Source: docs/req/architecture.md#Authentication & Security]
- [Source: _bmad-output/planning-artifacts/convenciones-fibradis.md]

### Review Findings

- [x] [Review][Decision] D1: JWT/HMAC secret reuse — `EmailConfirmationTokenService` usa `Jwt:Secret` como clave HMAC para tokens de confirmación. ¿Crear `EmailConfirmation:HmacSecret` dedicada, o aceptar la reutilización? [src/Server/Infrastructure/Security/EmailConfirmationTokenService.cs] — resuelto: clave derivada con prefijo de dominio `"fibradis-email-confirmation:" + secret`; separación semántica sin nueva variable de entorno
- [x] [Review][Decision] D2: `ConfirmarEmailPage` success CTA — spec dice "Ir a mi portafolio" (AC-4), pero al confirmar sin sesión activa el usuario es anónimo → PortafolioRoute muestra el landing público, no el dashboard. ¿Mantener "Ir a mi portafolio" (spec) o cambiar a "Iniciar sesión para acceder a tu portafolio" (más preciso)? [src/Web/Main/src/pages/ConfirmarEmailPage.tsx:121] — descartado: mantener "Ir a mi portafolio" per spec
- [x] [Review][Patch] P1: `catch` block en `AuthContext.bootstrapSession` y `login` llama `setIsActive`/`setTrialEndsAt` sin verificar `if (!active) return` — puede actualizar estado en componente desmontado [src/Web/Main/src/modules/auth/AuthContext.tsx:68,130]
- [x] [Review][Patch] P2: `TrialExpiredView.handleNotifyPayment` llama `notifyPayment()` sin verificar autenticación → usuario anónimo en `/activar?reason=trial_expired` recibe 401 sin redirect a login [src/Web/Main/src/pages/ActivarPage.tsx:46]
- [x] [Review][Patch] P3: `/resend-confirmation` ignora silenciosamente `App:BaseUrl` vacío y devuelve 200 sin enviar email — inconsistente con `/register` que devuelve 503 [src/Server/Api/Endpoints/Public/AuthEndpoints.cs:535]
- [x] [Review][Patch] P4: `IsValidEmailFormat` usa `IndexOf('@')` — acepta `user@domain@example.com` como email válido; usar `LastIndexOf('@')` [src/Server/Infrastructure/Security/UserService.cs:974]
- [x] [Review][Patch] P5: AC-4 — descripción de éxito no incluye "Tu prueba de 14 días ha comenzado" (spec exacto); muestra fecha de vencimiento en su lugar [src/Web/Main/src/pages/ConfirmarEmailPage.tsx:113]
- [x] [Review][Patch] P6: `ConfirmarEmailPage` estado `token_expired` dirige al usuario a `/login` con "Iniciar sesión" pero no puede iniciar sesión (email no confirmado) — UX sin salida; cambiar acción a `/activar?reason=trial_not_started` [src/Web/Main/src/pages/ConfirmarEmailPage.tsx:73]
- [x] [Review][Patch] P7: `RegistroPage` pierde el mensaje de error de servidor para `invalid_user_data` (complejidad de contraseña) — `authApi.ts` descarta `ex.Message` y muestra mensaje genérico [src/Web/Main/src/modules/auth/authApi.ts:1203]
- [x] [Review][Defer] D-a: Usuario creado en BD antes de enviar email de confirmación — si Resend falla, queda huérfano pero puede usar resend; diseño intencional per spec "no fallar si email falla" [src/Server/Api/Endpoints/Public/AuthEndpoints.cs:450] — deferred, diseño per spec
- [x] [Review][Defer] D-b: `ResendEmailService` absorbe excepciones de envío sin relanzar — caller recibe falso éxito en silencio; patrón intencional per spec [src/Server/Infrastructure/Integrations/Email/ResendEmailService.cs:57] — deferred, diseño per spec
- [x] [Review][Defer] D-c: `IUserService.ResendConfirmationAsync` toma `IEmailConfirmationTokenService` e `IEmailService` como parámetros — abstracción con dependencias de infra en interfaz de aplicación [src/Server/Application/Auth/IUserService.cs:701] — deferred, deuda arquitectónica
- [x] [Review][Defer] D-d: Flash de contenido privado durante transición de login cuando `isActive` es optimista (`true`) antes de resolver `fetchProfile()` — trade-off del diseño optimista [src/Web/Main/src/modules/auth/AuthContext.tsx:120] — deferred, trade-off de diseño
- [x] [Review][Defer] D-e: `POST /account/notify-payment` sin rate limiting ni idempotencia — usuario autenticado puede spamear el email del admin [src/Server/Api/Endpoints/Private/AccountEndpoints.cs:382] — deferred, feature request fuera de scope
- [x] [Review][Defer] D-f: `/confirm-email` como GET muta estado — gateways de email corporativos (Outlook Safe Links, Barracuda) pre-fetchean URLs y confirman cuentas sin intervención del usuario; requiere cambio a POST [src/Server/Api/Endpoints/Public/AuthEndpoints.cs:482] — deferred, cambio disruptivo de 14.2, historia futura

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

- CS0718: ILogger<AccountEndpoints> no válido en clase estática → solución: eliminar logger del endpoint (ResendEmailService ya loggea internamente)
- Build error @/shared/ui/label no existe → solución: reemplazar `Label` component por `<label>` HTML nativo
- Fallo preexistente `BuildInpcSeriesAsync_WhenEntriesExist_NormalizesFromBaseMonth` (PortfolioPerformanceInpcTests) — ajeno a 14.3, de story 15-1

### Completion Notes List

- T1.1–T1.4: `UserProfileData` y `UserProfileResponse` extendidos con `IsActive`, `TrialEndsAt`, `FechaPago`/`PaidAt`; `AccountEndpoints.GET /me` mapea los nuevos campos como ISO8601 UTC
- T2.1: Codegen regeneró `schema.d.ts` — `UserProfileResponse` ahora tiene `isActive: boolean`, `trialEndsAt: null | string`, `paidAt: null | string`
- T3: `AuthContext.tsx` llama `fetchProfile()` tras bootstrap exitoso; `isActive` optimista=true en modo degradado
- T4: `ProtectedRoute.tsx` guarda usuarios autenticados con `isActive=false` → redirect a `/activar?reason=...`
- T5: `PortafolioRoute.tsx` aplica el mismo guard eliminando la dependencia de `resolvePortafolioRouteView`
- T6: `RegistroPage.tsx` creada con formulario completo, manejo de errores 422/409, success state, validación client-side
- T7: `ActivarPage.tsx` creada con dos vistas (`trial_expired` con planes + pago, `trial_not_started` con reenvío email)
- T8: `POST /api/v1/account/notify-payment` (auth) y `POST /api/v1/auth/resend-confirmation` (anónimo) implementados; `ResendConfirmationRequest` nuevo record; `ApiWebFactory.CapturingEmailService` actualizado para implementar nuevo método de interfaz
- T9: Rutas `/registro` y `/activar` agregadas a `routes.tsx`, `SpaRouteCatalog.cs` y `SpaMetadataProvider.cs` (noindex,nofollow)
- T10: Botón "Iniciar sesión" en estado de éxito de `ConfirmarEmailPage` corregido a "Ir a mi portafolio"
- T11: 4 tests nuevos en `UserServiceTests.cs` — GetProfileAsync_ActiveUser, GetProfileAsync_TrialUser, GetProfileAsync_UserWithPayment, GetProfileAsync_InactiveUserNoTrial (38/38 UserServiceTests verdes)
- T12: Build backend 0 errores, 38/38 UserServiceTests verdes, frontend build 0 errores, 188/188 frontend tests verdes
- **Pendiente de review**: CLABE y banco son placeholders `[CLABE PENDIENTE]` / `[BANCO PENDIENTE]` en `ActivarPage.tsx` — Jorge debe proveer los datos reales antes del deploy

### Change Log

- feat(14.3): acceso controlado frontend — guards isActive en ProtectedRoute/PortafolioRoute, AuthContext fetchProfile, páginas /registro y /activar, endpoints notify-payment/resend-confirmation, UserProfileResponse extendido (2026-06-19)

### File List

- `src/Server/Application/Auth/UserProfileData.cs` (MODIFIED)
- `src/Server/Application/Auth/IUserService.cs` (MODIFIED)
- `src/Server/Application/Email/IEmailService.cs` (MODIFIED)
- `src/Server/Infrastructure/Security/UserService.cs` (MODIFIED)
- `src/Server/Infrastructure/Integrations/Email/ResendEmailService.cs` (MODIFIED)
- `src/Server/SharedApiContracts/Auth/UserProfileResponse.cs` (MODIFIED)
- `src/Server/SharedApiContracts/Auth/ResendConfirmationRequest.cs` (NEW)
- `src/Server/Api/Endpoints/Private/AccountEndpoints.cs` (MODIFIED)
- `src/Server/Api/Endpoints/Public/AuthEndpoints.cs` (MODIFIED)
- `src/Server/Api/Seo/SpaRouteCatalog.cs` (MODIFIED)
- `src/Server/Api/Seo/SpaMetadataProvider.cs` (MODIFIED)
- `src/Web/Main/src/app/routes.tsx` (MODIFIED)
- `src/Web/Main/src/modules/auth/AuthContext.tsx` (MODIFIED)
- `src/Web/Main/src/modules/auth/authApi.ts` (MODIFIED)
- `src/Web/Main/src/modules/auth/ProtectedRoute.tsx` (MODIFIED)
- `src/Web/Main/src/modules/portafolio/PortafolioRoute.tsx` (MODIFIED)
- `src/Web/Main/src/pages/ConfirmarEmailPage.tsx` (MODIFIED)
- `src/Web/Main/src/pages/RegistroPage.tsx` (NEW)
- `src/Web/Main/src/pages/ActivarPage.tsx` (NEW)
- `src/Web/SharedApiClient/schema.d.ts` (MODIFIED — regenerado)
- `scripts/codegen/Api.json` (MODIFIED — regenerado)
- `tests/Unit/Infrastructure.Tests/Security/UserServiceTests.cs` (MODIFIED)
- `tests/Integration/Api.Tests/ApiWebFactory.cs` (MODIFIED)
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (MODIFIED)
