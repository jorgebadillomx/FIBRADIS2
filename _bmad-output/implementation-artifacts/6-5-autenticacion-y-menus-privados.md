# Historia 6.5: Autenticación del SPA Main y menús privados

Status: done

## Story

Como usuario registrado,
quiero poder iniciar sesión en FIBRADIS Main con mi correo y contraseña, que el sistema persista mi sesión entre recargas usando el refresh token, y que la navegación muestre secciones privadas (Portafolio) solo cuando estoy autenticado,
para que pueda acceder a mi portafolio sin pasos manuales repetidos y sin que usuarios no registrados vean rutas inaccesibles.

Como AdminOps,
quiero crear cuentas de usuario desde el panel Ops con rol `User`,
para que pueda habilitar el acceso privado a usuarios específicos sin tocar la base de datos.

## Acceptance Criteria

### AC1 — Página de login accesible y funcional

**Dado que** navego a `/login` sin estar autenticado,
**Entonces** veo un formulario con campos Email y Contraseña, un botón "Iniciar sesión" y el diseño respeta el `PublicLayout` existente (header con logo y nav pública).

### AC2 — Login exitoso → token almacenado y redirección

**Dado que** ingreso credenciales válidas (`dev@fibradis.mx` / `Fibradis2026!`) y hago clic en "Iniciar sesión",
**Entonces** el access token se almacena en `sessionStorage` bajo la key `fibradis.main.accessToken`, el refresh token queda en cookie HttpOnly gestionada por el servidor, y el usuario es redirigido a `/portafolio`.

### AC3 — Login con credenciales inválidas

**Dado que** ingreso contraseña incorrecta,
**Entonces** veo el mensaje "Credenciales incorrectas. Verifica tu correo y contraseña." sin limpiar el campo email. No se almacena ningún token.

### AC4 — Bootstrap de sesión al recargar

**Dado que** tengo una sesión activa (cookie de refresh presente) y recargo la página,
**Entonces** el sistema llama automáticamente a `POST /api/v1/auth/refresh`, almacena el nuevo access token y el usuario ve la UI autenticada sin pasar por el formulario de login.

### AC5 — Navegación diferenciada pública vs. privada

**Dado que** NO estoy autenticado,
**Entonces** el header muestra: Conoce las FIBRAs | Catálogo | Noticias | Fundamentales + botón "Iniciar sesión". NO aparece el enlace "Portafolio".

**Dado que** SÍ estoy autenticado,
**Entonces** el header muestra: Conoce las FIBRAs | Catálogo | Noticias | Fundamentales | **Portafolio** + botón "Cerrar sesión".

### AC6 — Ruta `/portafolio` protegida

**Dado que** intento navegar a `/portafolio` sin token válido,
**Entonces** soy redirigido a `/login?redirect=/portafolio`. Después de autenticarme correctamente, soy redirigido de vuelta a `/portafolio`.

### AC7 — Cerrar sesión

**Dado que** estoy autenticado y hago clic en "Cerrar sesión",
**Entonces** el token en `sessionStorage` se borra, la UI vuelve al estado no autenticado, y soy redirigido a `/`.

### AC8 — apiClient adjunta Authorization header automáticamente

**Dado que** estoy autenticado y la PortafolioPage hace `GET /api/v1/portfolio`,
**Entonces** la petición incluye el header `Authorization: Bearer <token>` y el servidor responde 200 con datos del portafolio.

**Dado que** el servidor responde 401 a cualquier llamada autenticada,
**Entonces** el token se borra, se emite el evento `fibradis:main-auth-required` y el usuario es redirigido a `/login`.

### AC9 — Refresh automático proactivo

**Dado que** han pasado 4 horas desde el último login o refresh,
**Entonces** el sistema renueva el access token en background sin interrumpir la sesión del usuario.

### AC10 — Creación de usuarios desde Ops (AdminOps)

**Dado que** soy AdminOps y navego a la nueva sección "Usuarios" en Ops,
**Entonces** veo un listado de usuarios existentes (email, rol, estado, fecha de creación) y un formulario para crear un nuevo usuario con campos: Email, Contraseña inicial, Rol (`User` únicamente en MVP).

**Dado que** creo un usuario con email `nuevo@ejemplo.com` y contraseña válida,
**Entonces** el usuario aparece en el listado con `IsActive=true` y puede autenticarse inmediatamente en el SPA Main.

**Dado que** intento crear un usuario con un email ya registrado,
**Entonces** veo el error "Ya existe una cuenta con ese correo electrónico."

### AC11 — PortafolioPage simplificada

**Dado que** accedo a `/portafolio` estando autenticado,
**Entonces** el componente `PortafolioPage` NO contiene lógica de `getStoredAccessToken()` ni redirección manual — toda la protección la maneja el `ProtectedRoute` wrapper. El `apiClient` inyecta el token automáticamente.

## Tasks / Subtasks

### T1 — Backend: endpoint de creación de usuarios (Ops)

- [x] T1.1 — Crear `CreateUserRequest` DTO en `src/Server/SharedApiContracts/Auth/`:
  ```csharp
  public sealed record CreateUserRequest(string Email, string Password);
  ```
- [x] T1.2 — Crear `CreateUserResponse` DTO:
  ```csharp
  public sealed record CreateUserResponse(Guid Id, string Email, UserRole Role, DateTime CreatedAt);
  ```
- [x] T1.3 — Crear `UserSummaryDto` para listado:
  ```csharp
  public sealed record UserSummaryDto(Guid Id, string Email, string Role, bool IsActive, DateTime CreatedAt);
  ```
- [x] T1.4 — Agregar método `CreateUserAsync(email, passwordHash, role)` a `IAuthRepository` o crear `IUserRepository` en `src/Server/Application/Auth/`
- [x] T1.5 — Implementar `CreateUserAsync` en `AuthRepository` / `UserRepository` (Infrastructure):
  - Verificar email único (throw o return error si duplicado)
  - Hash password con BCrypt
  - Persistir `User` con `Role = UserRole.User`, `IsActive = true`
- [x] T1.6 — Crear `UserService` en Infrastructure con `CreateUserAsync(email, password)` y `GetAllUsersAsync()`
- [x] T1.7 — Crear endpoints en `src/Server/Api/Endpoints/Ops/UserEndpoints.cs`:
  ```
  POST /api/v1/ops/users    → RequireAuthorization("AdminOps")
  GET  /api/v1/ops/users    → RequireAuthorization("AdminOps")
  ```
- [x] T1.8 — Registrar endpoints en el composition root (junto a los demás endpoints de Ops)
- [x] T1.9 — Agregar migración EF Core si se requieren cambios en el schema `auth` (revisar si la tabla `Users` ya existe de Historia 1.3 — muy probable que sí)
- [x] T1.10 — Unit tests en `tests/Unit/` para `UserService.CreateUserAsync`:
  - Happy path: crea usuario correctamente
  - Error: email duplicado → excepción tipada

### T2 — Backend: regenerar SharedApiClient

- [x] T2.1 — Ejecutar `npm run codegen:api` desde la raíz para que los nuevos endpoints aparezcan en `@fibradis/shared-api-client`
- [x] T2.2 — Verificar que `paths['/api/v1/ops/users']` aparece en el spec generado

### T3 — Frontend Main: módulo de autenticación

- [x] T3.1 — Crear `src/Web/Main/src/modules/auth/mainAuth.ts` con:
  ```typescript
  const AUTH_TOKEN_KEY = 'fibradis.main.accessToken'
  export const AUTH_REQUIRED_EVENT = 'fibradis:main-auth-required'

  export function getStoredMainAccessToken(): string | null
  export function storeMainAccessToken(token: string): void
  export function clearMainAccessToken(): void
  export function getMainAuthHeaders(): { Authorization: string } | {}
  export function notifyMainAuthRequired(): void
  ```
  - `getStoredMainAccessToken` → lee de `sessionStorage`
  - `storeMainAccessToken` → escribe en `sessionStorage`
  - `clearMainAccessToken` → borra de `sessionStorage`
  - `getMainAuthHeaders` → `{ Authorization: \`Bearer ${token}\` }` o `{}` si no hay token
  - `notifyMainAuthRequired` → dispatches `fibradis:main-auth-required` CustomEvent

- [x] T3.2 — Crear `src/Web/Main/src/modules/auth/authApi.ts`:
  ```typescript
  import { apiClient } from '@/api/fibrasApi'
  import { storeMainAccessToken, clearMainAccessToken, notifyMainAuthRequired } from './mainAuth'

  export async function loginMain(email: string, password: string): Promise<void>
  // POST /api/v1/auth/login → almacena token → throw en error

  export async function refreshMainSession(): Promise<boolean>
  // POST /api/v1/auth/refresh → almacena nuevo token → return true/false
  // Promise deduplication: mismo patrón que Ops (variable de vuelo única)

  export async function logoutMain(): Promise<void>
  // clearMainAccessToken() + redirect a /
  ```

- [x] T3.3 — Crear `src/Web/Main/src/modules/auth/AuthContext.tsx`:
  ```typescript
  type AuthStatus = 'checking' | 'anonymous' | 'authenticated'
  interface AuthContextValue {
    status: AuthStatus
    login: (email: string, password: string) => Promise<void>
    logout: () => void
  }
  export const AuthContext = createContext<AuthContextValue | null>(null)
  export function AuthProvider({ children }: { children: ReactNode })
  export function useAuth(): AuthContextValue
  ```
  - `AuthProvider` hace bootstrap refresh on mount (igual que `OpsLoginGate`)
  - Escucha `fibradis:main-auth-required` para forzar logout
  - Proactive refresh cada 4 horas cuando status === 'authenticated'

- [x] T3.4 — Crear `src/Web/Main/src/modules/auth/ProtectedRoute.tsx`:
  ```typescript
  export function ProtectedRoute({ children }: { children?: ReactNode })
  // Si status === 'checking' → skeleton/loader
  // Si status === 'anonymous' → <Navigate to={`/login?redirect=${location.pathname}`} replace />
  // Si status === 'authenticated' → <Outlet /> (o children)
  ```

### T4 — Frontend Main: página de login

- [x] T4.1 — Crear `src/Web/Main/src/modules/auth/LoginPage.tsx`:
  - Formulario con campos Email (type="email") y Contraseña (type="password")
  - Botón "Iniciar sesión" con estado loading durante la llamada
  - Mensaje de error inline bajo el formulario (sin limpiar email)
  - Al éxito: leer `?redirect` del search param y redirigir; si no hay redirect, ir a `/portafolio`
  - Diseño: centrado en la página, tarjeta con sombra, logo FIBRADIS arriba
  - Renderizado dentro de `PublicLayout` (el header siempre aparece)

### T5 — Frontend Main: actualizar router y layout

- [x] T5.1 — Actualizar `src/Web/Main/src/app/routes.tsx`:
  - Agregar ruta `/login` → `<LoginPage />` (pública, sin `ProtectedRoute`)
  - Envolver ruta `/portafolio` con `<ProtectedRoute>`:
    ```tsx
    { path: '/portafolio', element: <ProtectedRoute><PortafolioPage /></ProtectedRoute> }
    ```
  - Importar `LoginPage` y `ProtectedRoute`

- [x] T5.2 — Envolver la app con `AuthProvider` en `src/Web/Main/src/main.tsx` (o en `App.tsx`, verificar estructura actual):
  ```tsx
  <AuthProvider>
    <RouterProvider router={router} />
  </AuthProvider>
  ```

- [x] T5.3 — Actualizar `src/Web/Main/src/shared/layouts/PublicLayout.tsx`:
  - Consumir `useAuth()` para obtener `status` y `logout`
  - Nav privada: enlace `Portafolio` visible SOLO cuando `status === 'authenticated'`
  - Header derecho: si `status === 'checking'` → nada; si `anonymous` → "Iniciar sesión" (link a `/login`); si `authenticated` → botón "Cerrar sesión" (llama a `logout()`)
  - El enlace "Iniciar sesión" existente (`<a href="/login">`) cambiar a `<Link to="/login">` para evitar full-page reload

### T6 — Frontend Main: actualizar apiClient con auth headers

- [x] T6.1 — Actualizar `src/Web/Main/src/api/fibrasApi.ts`:
  - El `apiClient` de `openapi-fetch` debe incluir un middleware que inyecte el header `Authorization` en todas las peticiones que requieren auth
  - Opción: usar `apiClient.use()` con un middleware:
    ```typescript
    apiClient.use({
      async onRequest({ request }) {
        const token = getStoredMainAccessToken()
        if (token) {
          request.headers.set('Authorization', `Bearer ${token}`)
        }
        return request
      },
      async onResponse({ response }) {
        if (response.status === 401) {
          clearMainAccessToken()
          notifyMainAuthRequired()
        }
        return response
      },
    })
    ```
  - Verificar la API de middleware de `openapi-fetch` con context7 antes de implementar

- [x] T6.2 — Actualizar `src/Web/Main/src/modules/portafolio/PortafolioPage.tsx`:
  - Eliminar `getStoredAccessToken()`, `ACCESS_TOKEN_KEYS` y el `if (!accessToken) return <Navigate to="/login" />`
  - Las queries ya NO necesitan `enabled: Boolean(accessToken)` — el ProtectedRoute garantiza auth
  - El apiClient inyecta el header automáticamente (T6.1)

### T7 — Frontend Ops: página de gestión de usuarios

- [x] T7.1 — Crear `src/Web/Ops/src/pages/UsersPage.tsx`:
  - Listado de usuarios: tabla con Email, Rol, Estado (Activo/Inactivo), Fecha de creación
  - Formulario "Crear usuario" con: Email, Contraseña, Rol (solo "Usuario" en MVP; select o campo fijo)
  - Submit llama a `POST /api/v1/ops/users` con `getOpsAuthHeaders()`
  - Manejo de error 409 (email duplicado) → mensaje inline
  - Refresh del listado tras creación exitosa

- [x] T7.2 — Agregar ruta `UsersPage` en `src/Web/Ops/src/App.tsx` (junto a las demás páginas Ops)
- [x] T7.3 — Agregar enlace "Usuarios" en `OpsShell` sidebar (verificar archivo `OpsShell.tsx`)

### T8 — Tests unitarios

- [x] T8.1 — Backend `tests/Unit/`: `UserService.CreateUserAsync` — happy path y email duplicado (ya cubierto en T1.10)
- [x] T8.2 — Frontend Main: test para `mainAuth.ts`:
  - `getStoredMainAccessToken` lee de sessionStorage
  - `clearMainAccessToken` borra la key correcta
  - `notifyMainAuthRequired` dispatches el evento
  - Usar `vitest` + `@testing-library` (mismo stack que el resto del frontend)

## Dev Notes

### Patrón base a seguir: OpsLoginGate + opsAuth.ts (Ops SPA)

La Historia 1.3 implementó el backend completo de auth. El SPA Ops implementó después un flujo de auth completo en la Historia 4.3 (hardening). Este story replica ese patrón para el SPA Main con las siguientes diferencias:

| Aspecto | Ops SPA | Main SPA |
|---|---|---|
| Componente de guarda | `OpsLoginGate` (envuelve toda la app) | `ProtectedRoute` (solo rutas privadas) |
| Storage key | `fibradis.ops.accessToken` | `fibradis.main.accessToken` |
| Storage tipo | `sessionStorage` | `sessionStorage` |
| Auth event | `fibradis:ops-auth-required` | `fibradis:main-auth-required` |
| Contexto | Component local (no Context API) | `AuthContext` + `AuthProvider` |
| Rutas privadas | Toda la app (all-or-nothing) | Solo `/portafolio` + futuras privadas |

**Por qué `AuthContext` en Main y no componente monolítico:** El SPA Main tiene rutas públicas (Home, Catálogo, etc.) y privadas mezcladas. El estado de auth necesita estar disponible en `PublicLayout` (para el header) y en `ProtectedRoute`. Un Context es la solución correcta; en Ops toda la app era privada por eso bastaba con un gate wrapper.

### Backend: tabla `Users` ya existe

Verificado en Historia 1.3: la tabla `auth.Users` y `auth.RefreshTokens` ya existen con migración. El modelo `User` tiene `Id`, `Email`, `PasswordHash`, `Role`, `IsActive`, `CreatedAt`. Solo falta el endpoint de creación en Ops.

**Archivo del dominio:** `src/Server/Domain/Auth/User.cs`
**Repositorio:** buscar el patrón de `IAuthService` o `IAuthRepository` en `src/Server/Infrastructure/Security/AuthService.cs` — la creación de usuario puede ir en un nuevo método de `AuthService` o en un `UserService` separado.

### Endpoints de auth existentes (no tocar)

```
POST /api/v1/auth/login    → AllowAnonymous, devuelve { accessToken }
POST /api/v1/auth/refresh  → AllowAnonymous, lee cookie, devuelve { accessToken }
```

Estos ya están registrados. No duplicar. Solo agregar los de Ops.

### openapi-fetch middleware (T6.1)

Verificar con context7 la API de middleware de `openapi-fetch` antes de implementar. El cliente se crea con `createClient<paths>({ baseUrl: '' })`. El middleware se registra con:
```typescript
apiClient.use(middlewareObject)
```
donde `middlewareObject` implementa `{ onRequest?, onResponse? }`. Confirmar la firma exacta con la versión instalada en el proyecto.

**IMPORTANTE:** el middleware aplica a TODAS las peticiones del `apiClient` (incluyendo las públicas). El middleware debe ser no-destructivo cuando no hay token: solo añade el header si hay token, nunca falla.

### Redirect después de login (AC6)

La ruta `/login?redirect=/portafolio` usa `useSearchParams()` de React Router. En `LoginPage`, leer el param `redirect` y hacer `navigate(redirect ?? '/portafolio', { replace: true })` tras el login exitoso.

### PortafolioPage: queries sin `enabled`

Con `ProtectedRoute`, cuando `PortafolioPage` renderiza, el usuario está garantizadamente autenticado. Por tanto:
- Eliminar `enabled: Boolean(accessToken)` de las queries
- Eliminar toda la función `getStoredAccessToken()` y sus constantes del componente
- Las queries se habilitarán siempre (el ProtectedRoute ya garantiza auth)

### Dev seed user

Para pruebas en desarrollo: `dev@fibradis.mx` / `Fibradis2026!` (Role: `User`)
Creado en `Program.cs` si no existen usuarios — no tocar ese seed.

### Menú Ops: agregar "Usuarios"

Verificar `src/Web/Ops/src/components/OpsShell.tsx` para el patrón de items del sidebar. Añadir Usuarios siguiendo el mismo patrón de los demás items (icon, label, ruta).

### Creación de usuarios en MVP

El MVP solo soporta `Role = UserRole.User` en la creación desde Ops. No implementar auto-registro público ni cambio de contraseña en esta historia. El endpoint `POST /api/v1/ops/users` recibe email + password (en plaintext, se hashea en servidor). No retornar ni loggear la contraseña.

### Testing frontend (T8.2)

El stack de test del Main SPA usa `vitest` + `@testing-library/react`. Ver las convenciones en `src/Web/Main/` (buscar archivos `*.test.ts` o `*.test.tsx` existentes para confirmar setup). El archivo de test va en `src/Web/Main/src/modules/auth/mainAuth.test.ts`.

Para `notifyMainAuthRequired`, usar `vi.spyOn(window, 'dispatchEvent')` o escuchar el evento en `document`.

### Project Structure Notes

- Nuevos archivos Main SPA:
  - `src/Web/Main/src/modules/auth/mainAuth.ts`
  - `src/Web/Main/src/modules/auth/authApi.ts`
  - `src/Web/Main/src/modules/auth/AuthContext.tsx`
  - `src/Web/Main/src/modules/auth/ProtectedRoute.tsx`
  - `src/Web/Main/src/modules/auth/LoginPage.tsx`
  - `src/Web/Main/src/modules/auth/mainAuth.test.ts`
- Nuevos archivos Ops SPA:
  - `src/Web/Ops/src/pages/UsersPage.tsx`
- Archivos backend nuevos:
  - `src/Server/SharedApiContracts/Auth/CreateUserRequest.cs`
  - `src/Server/SharedApiContracts/Auth/CreateUserResponse.cs`
  - `src/Server/SharedApiContracts/Auth/UserSummaryDto.cs`
  - `src/Server/Api/Endpoints/Ops/UserEndpoints.cs`
  - `src/Server/Infrastructure/Security/UserService.cs` (si se crea separado)
  - `tests/Unit/Infrastructure/Security/UserServiceTests.cs`
- Archivos modificados:
  - `src/Web/Main/src/api/fibrasApi.ts` (middleware auth)
  - `src/Web/Main/src/app/routes.tsx` (ruta /login + ProtectedRoute)
  - `src/Web/Main/src/main.tsx` o `App.tsx` (AuthProvider)
  - `src/Web/Main/src/shared/layouts/PublicLayout.tsx` (nav condicional)
  - `src/Web/Main/src/modules/portafolio/PortafolioPage.tsx` (eliminar auth manual)
  - `src/Web/Ops/src/App.tsx` (ruta UsersPage)
  - `src/Web/Ops/src/components/OpsShell.tsx` (enlace Usuarios)

### References

- Ops auth patrón: `src/Web/Ops/src/components/OpsLoginGate.tsx`
- Ops auth utils: `src/Web/Ops/src/api/opsAuth.ts`
- Ops auth api: `src/Web/Ops/src/api/authApi.ts`
- Backend endpoints login/refresh: `src/Server/Api/Endpoints/Public/AuthEndpoints.cs`
- Backend TokenService: `src/Server/Infrastructure/Security/TokenService.cs`
- Backend AuthService: `src/Server/Infrastructure/Security/AuthService.cs`
- Backend User domain: `src/Server/Domain/Auth/User.cs`
- Backend RefreshToken: `src/Server/Domain/Auth/RefreshToken.cs`
- Auth policies: `src/Server/Api/Authentication/AddAuthorizationExtensions.cs`
- JWT config: `src/Server/Api/Authentication/AddAuthenticationExtensions.cs`
- Deferred work D3 (6-1): `_bmad-output/planning-artifacts/deferred-work.md` — auth guard abierto
- Deferred work D3 (6-2): `_bmad-output/planning-artifacts/deferred-work.md` — key correcta de token
- FR-42: mundo público sin auth, mundo privado con auth — `_bmad-output/planning-artifacts/epics.md`
- NFR-11: protección mundo privado y ops con roles User y AdminOps

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

### Completion Notes List

- T1: `IUserService`/`UserService` creados en Application/Infrastructure. `UserData` record en Application para evitar referencia cruzada a SharedApiContracts. `OpsUserEndpoints` mapea `UserData → UserSummaryDto` en la capa Api. 4/4 unit tests pasan.
- T2: `npm run codegen:api` regeneró el schema; `UserSummaryDto`, `CreateUserRequest` y `/api/v1/ops/users` disponibles en el cliente tipado.
- T3–T6: `mainAuth.ts`, `authApi.ts`, `AuthContext.tsx`, `ProtectedRoute.tsx`, `LoginPage.tsx` creados. Router actualizado con `/login` público y `/portafolio` protegido por `ProtectedRoute`. `AuthProvider` envuelve el árbol en `main.tsx`. `PublicLayout` consume `useAuth()` para nav condicional. `fibrasApi.ts` inyecta `Authorization` header vía `apiClient.use()`. `PortafolioPage` limpiada de toda lógica de auth manual.
- T7: `usersApi.ts`, `UsersPage.tsx` creados en Ops SPA. Ruta `/users` agregada a `main.tsx`. Enlace "Usuarios" agregado a `OpsShell.tsx`.
- T8.2: 6 tests de `mainAuth.test.ts` pasan (Node test runner con shim de `globalThis.window`). Suite completa: 72/72.
- TS check Main SPA: 0 errores. TS check Ops SPA: 0 errores.

### File List

**Nuevos — Backend:**
- src/Server/SharedApiContracts/Auth/CreateUserRequest.cs
- src/Server/SharedApiContracts/Auth/UserSummaryDto.cs
- src/Server/Application/Auth/UserData.cs
- src/Server/Application/Auth/IUserService.cs
- src/Server/Infrastructure/Security/UserService.cs
- src/Server/Api/Endpoints/Ops/OpsUserEndpoints.cs
- tests/Unit/Infrastructure.Tests/Security/UserServiceTests.cs

**Modificados — Backend:**
- src/Server/Api/CompositionRoot/ApiServiceExtensions.cs
- src/Server/Api/Program.cs
- src/Web/SharedApiClient/schema.d.ts

**Nuevos — Frontend Main:**
- src/Web/Main/src/modules/auth/mainAuth.ts
- src/Web/Main/src/modules/auth/authApi.ts
- src/Web/Main/src/modules/auth/AuthContext.tsx
- src/Web/Main/src/modules/auth/ProtectedRoute.tsx
- src/Web/Main/src/modules/auth/LoginPage.tsx
- src/Web/Main/src/modules/auth/mainAuth.test.ts

**Modificados — Frontend Main:**
- src/Web/Main/src/api/fibrasApi.ts
- src/Web/Main/src/app/routes.tsx
- src/Web/Main/src/main.tsx
- src/Web/Main/src/shared/layouts/PublicLayout.tsx
- src/Web/Main/src/modules/portafolio/PortafolioPage.tsx
- src/Web/Main/package.json

**Nuevos — Frontend Ops:**
- src/Web/Ops/src/api/usersApi.ts
- src/Web/Ops/src/pages/UsersPage.tsx

**Modificados — Frontend Ops:**
- src/Web/Ops/src/main.tsx
- src/Web/Ops/src/components/OpsShell.tsx

### Review Findings

- [x] `Review` `Patch` P1: Race condition TOCTOU en UserService.CreateUserAsync — AnyAsync + SaveChangesAsync separados sin manejo de DbUpdateException por índice único. Dos requests concurrentes con el mismo email pueden crear usuarios duplicados o lanzar 500 en lugar de DuplicateEmailException (422). `src/Server/Infrastructure/Security/UserService.cs`
- [x] `Review` `Patch` P2: Contraseña en texto plano en log del seed — LogInformation expone "Fibradis2026!" en logs persistentes (App Insights, Seq, archivos). Remover la contraseña del mensaje. `src/Server/Api/Program.cs`
- [x] `Review` `Patch` P3: Open redirect en LoginPage — navigate(searchParams.get('redirect')) sin validar que sea ruta relativa. Un atacante puede construir /login?redirect=https\://evil.com para redirigir tras login. Fix: validar que empiece con '/' y no con '//'. `src/Web/Main/src/modules/auth/LoginPage.tsx:27`
- [x] `Review` `Patch` P4: Sin validación server-side en CreateUserRequest — record(string Email, string Password) sin Required, EmailAddress, ni MinLength(8). Un POST directo con email vacío o contraseña nula llega a BCrypt sin rechazo explícito. `src/Server/SharedApiContracts/Auth/CreateUserRequest.cs`
- [x] `Review` `Patch` P5: AC7 — logout no redirige a '/' — logout() en AuthContext solo limpia estado, sin navegación. Cuando el usuario está en /portafolio y cierra sesión, ProtectedRoute ve status='anonymous' y lo manda a /login?redirect=/portafolio en vez de '/'. `src/Web/Main/src/shared/layouts/PublicLayout.tsx`
- [x] `Review` `Patch` P6: LoginPage llama navigate() durante render — if (status === 'authenticated') { void navigate('/portafolio') } ejecuta navigate en el cuerpo del render. Antipatrón React; mover a useEffect. `src/Web/Main/src/modules/auth/LoginPage.tsx:40-43`
- [x] `Review` `Patch` P7: fibrasApi middleware dispara logout en CUALQUIER 401 — incluyendo endpoints públicos. Un 401 en una ruta pública vacía el cache de React Query para usuarios anónimos y dispara el evento de auth. Fix: solo disparar si hay token activo. `src/Web/Main/src/api/fibrasApi.ts`
- [x] `Review` `Defer` D1: Bootstrap marca autenticado con token stale si el refresh falla por error de red — patrón consistente con OpsLoginGate. Flash de UI autenticada antes de logout por 401 subsiguiente. `src/Web/Main/src/modules/auth/AuthContext.tsx` — deferred, pre-existente en patrón Ops
- [x] `Review` `Defer` D2: Doble cleanup en logout — clearMainAccessToken/queryClient.clear/setStatus se ejecutan dos veces (una directa, una por el evento MAIN_AUTH_REQUIRED_EVENT). Idempotente, no rompe, pero es confuso. `src/Web/Main/src/modules/auth/AuthContext.tsx` — deferred, pre-existente
- [x] `Review` `Defer` D3: Seed sin guard de migración — db.Users.Any() lanza si la migración no se ha ejecutado, crash al arrancar en dev recién clonado. `src/Server/Api/Program.cs` — deferred, dev-only
- [x] `Review` `Defer` D4: GetAllUsersAsync sin paginación — materializa toda la tabla Users. Consistente con otros endpoints Ops. `src/Server/Infrastructure/Security/UserService.cs` — deferred, pre-existente patrón Ops
- [x] `Review` `Defer` D5: Access token en sessionStorage accesible por JS (riesgo XSS) — patrón idéntico al Ops SPA. `src/Web/Main/src/modules/auth/mainAuth.ts` — deferred, pre-existente
