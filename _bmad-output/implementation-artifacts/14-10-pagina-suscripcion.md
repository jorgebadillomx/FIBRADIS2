# Story 14.10: Página de Suscripción en el menú de usuario

Status: done

## Story

Como usuario autenticado,
quiero ver mi estado de suscripción actual y las instrucciones de pago en cualquier momento desde el menú de mi cuenta,
para que pueda saber cuánto tiempo me queda en mi prueba, conocer mi plan activo, y saber cómo pagar sin tener que esperar a que mi acceso expire.

## Acceptance Criteria

1. **Dado que** soy un usuario autenticado y abro el menú de mi cuenta (dropdown con mi nombre en el header),
   **Entonces** veo una opción "Suscripción" como nuevo ítem del menú, entre "Mi perfil" y "Cerrar sesión".

2. **Dado que** navego a `/suscripcion` estando autenticado con `isActive = true`, `subscriptionType = null` y `trialEndsAt` en el futuro,
   **Entonces** veo un banner ámbar "Período de prueba activo — vence el DD/MM/AAAA (X días restantes)".

3. **Dado que** navego a `/suscripcion` con `subscriptionType = "Monthly"` o `"Annual"` e `isActive = true`,
   **Entonces** veo un banner verde "Plan {Monthly|Annual} activo — vence el DD/MM/AAAA".

4. **Dado que** navego a `/suscripcion` con `subscriptionType = "Lifetime"` e `isActive = true`,
   **Entonces** veo un banner verde "Acceso de por vida" sin fecha de vencimiento, y **no** se muestra la sección de pago.

5. **Dado que** navego a `/suscripcion` con `isActive = false`,
   **Entonces** veo un banner gris/rojo "Tu acceso ha expirado" o "Tu prueba ha terminado" según corresponda.

6. **Dado que** el usuario tiene cualquier estado de suscripción **excepto Lifetime activo** (ACs 2, 3, 5),
   **Entonces** la página muestra debajo del banner de estado: los tres planes con sus precios (Mensual / Anual / Lifetime), las instrucciones de pago (CLABE + banco + concepto + contacto), y el botón "Ya pagué — notificar al equipo".

7. **Dado que** hago clic en "Ya pagué — notificar al equipo" y el request es exitoso,
   **Entonces** el botón se reemplaza por el mensaje "✓ Notificación enviada. Te contactaremos para activar tu acceso." — el mismo comportamiento que en `/activar`.

8. **Dado que** navego a `/suscripcion` sin estar autenticado (status === 'anonymous'),
   **Entonces** soy redirigido a `/login?redirect=%2Fsuscripcion` — igual que cualquier ruta que requiere auth.

9. **Dado que** `GET /api/v1/account/me` responde,
   **Entonces** el body incluye `subscriptionType: string | null` y `subscriptionEndsAt: string | null` además de los campos actuales (`isActive`, `trialEndsAt`, `paidAt`, etc.).

10. **Dado que** corro `dotnet build FIBRADIS.slnx` y `npm run build --workspace=src/Web/Main`,
    **Entonces** 0 errores TypeScript y 0 warnings de compilación en ambos proyectos.

## Tasks / Subtasks

- [x] T1: Extender backend — agregar `SubscriptionType` y `SubscriptionEndsAt` a la respuesta de `/api/v1/account/me` (AC: 9)
  - [x] T1.1: `UserProfileData.cs` — agregar `string? SubscriptionType` y `DateTime? SubscriptionEndsAt` como parámetros del record
  - [x] T1.2: `UserService.GetProfileAsync` — mapear `user.SubscriptionType?.ToString()` y `user.SubscriptionEndsAt` al nuevo `UserProfileData`
  - [x] T1.3: `UserProfileResponse.cs` — agregar `string? SubscriptionType` y `string? SubscriptionEndsAt` al record
  - [x] T1.4: `AccountEndpoints.cs` (línea ~28) — incluir los dos nuevos campos en el `new UserProfileResponse(...)` usando el mismo patrón ISO-8601 de `TrialEndsAt` para `SubscriptionEndsAt`
  - [x] T1.5: Correr `npm run codegen:api` para regenerar el cliente tipado (actualiza `openapi.d.ts` en Main y Ops)

- [x] T2: Extender `AuthContext` — exponer `subscriptionType` y `subscriptionEndsAt` (AC: 9)
  - [x] T2.1: Agregar estado `subscriptionType: string | null` y `subscriptionEndsAt: string | null` al `AuthProvider`
  - [x] T2.2: Mapear desde `profile.subscriptionType ?? null` y `profile.subscriptionEndsAt ?? null` en todos los puntos donde se llama a `fetchProfile()` (bootstrap, `login`, `handleAuthRequired`, proactive refresh logout)
  - [x] T2.3: Exponer ambos campos en el `AuthContextValue` interface y en el `value` del Provider
  - [x] T2.4: En `handleAuthRequired` y `logout`, resetear ambos a `null`

- [x] T3: Crear `SuscripcionPage.tsx` en `src/Web/Main/src/pages/` (AC: 2, 3, 4, 5, 6, 7, 8)
  - [x] T3.1: Obtener `{ status, isActive, trialEndsAt, subscriptionType, subscriptionEndsAt }` desde `useAuth()`
  - [x] T3.2: Si `status === 'anonymous'` → `<Navigate to="/login?redirect=%2Fsuscripcion" replace />`
  - [x] T3.3: Si `status === 'checking'` → spinner de carga
  - [x] T3.4: Computar `daysRemaining` para trial: `Math.ceil((new Date(trialEndsAt).getTime() - Date.now()) / 86400000)`
  - [x] T3.5: Renderizar banner de estado según combinación de `isActive`/`subscriptionType`/`trialEndsAt` (ver Dev Notes para la lógica exacta)
  - [x] T3.6: Renderizar sección de pago condicional (ocultar solo si `subscriptionType === 'Lifetime' && isActive`) — reutilizar las constantes `PLANES` de `ActivarPage.tsx` (extraer a módulo compartido o duplicar)
  - [x] T3.7: Implementar `handleNotifyPayment` usando `notifyPayment()` de `authApi` — copiar lógica de `TrialExpiredView` en `ActivarPage.tsx`
  - [x] T3.8: `usePageTitle('Mi suscripción | Fibras Inmobiliarias', DESCRIPTION, { robotsDirectives: 'noindex,nofollow' })`

- [x] T4: Agregar ruta `/suscripcion` en `routes.tsx` (AC: 8)
  - [x] T4.1: Agregar `lazy` import de `SuscripcionPage`
  - [x] T4.2: Agregar `{ path: '/suscripcion', element: p(<SuscripcionPage />) }` **fuera** del bloque `<ProtectedRoute>` (la página maneja su propio guard de auth — ver Dev Notes)

- [x] T5: Agregar "Suscripción" al menú de cuenta en `PublicLayout.tsx` (AC: 1)
  - [x] T5.1: En el array `entries` del `DesktopMenu` del menú de cuenta (~línea 295), insertar `{ label: 'Suscripción', to: '/suscripcion' }` **antes** de `{ label: 'Mi perfil', ... }`
  - [x] T5.2: Hacer el mismo cambio en el menú móvil de `PublicLayout.tsx` si existe sección de cuenta en el drawer móvil — actualizado en `public-navigation.ts` (MAIN_ACCOUNT_LINKS)

- [x] T6: Unit tests (AC: 10)
  - [x] T6.1: Test backend `AccountEndpointTests.cs` — verificar que `GET /api/v1/account/me` devuelve `subscriptionType` y `subscriptionEndsAt` cuando el usuario tiene una suscripción activa (10/10 pasando)
  - [x] T6.2: Tests frontend en `SuscripcionPage.test.ts` — 8 tests de `resolveSubscriptionState`: trial/active/lifetime/expired, sección de pago condicional (200/200 pasando)

## Dev Notes

### Lógica de estados de la página

La página usa cuatro estados mutuamente excluyentes basados en los datos del AuthContext:

```typescript
type SubscriptionState =
  | { kind: 'trial'; trialEndsAt: string; daysRemaining: number }
  | { kind: 'active'; subscriptionType: 'Monthly' | 'Annual'; subscriptionEndsAt: string }
  | { kind: 'lifetime' }
  | { kind: 'expired'; hadTrial: boolean }
```

**Prioridad de evaluación** (en orden):
1. `isActive && subscriptionType === 'Lifetime'` → `kind: 'lifetime'`
2. `isActive && subscriptionType` → `kind: 'active'`
3. `isActive && !subscriptionType && trialEndsAt` → `kind: 'trial'`
4. `!isActive` → `kind: 'expired'`, `hadTrial: trialEndsAt !== null`

### Por qué la ruta NO va en `<ProtectedRoute>`

`ProtectedRoute` redirige a `/activar` cuando `!isActive`. Un usuario con acceso expirado debe poder ver `/suscripcion` (para ver su estado y pagar). Por eso la ruta debe vivir fuera de `ProtectedRoute` y manejar su propio guard:

```tsx
// SuscripcionPage.tsx — al inicio del render
if (status === 'anonymous') return <Navigate to="/login?redirect=%2Fsuscripcion" replace />
if (status === 'checking') return <LoadingSpinner />
// continúa con la lógica de estado...
```

Este patrón es el mismo que se usa en `ActivarPage.tsx` implícitamente (también está fuera de ProtectedRoute).

### Archivos a modificar (UPDATE)

| Archivo | Cambio |
|---------|--------|
| `src/Server/Application/Auth/UserProfileData.cs` | Agregar `string? SubscriptionType`, `DateTime? SubscriptionEndsAt` |
| `src/Server/Infrastructure/Security/UserService.cs` | En `GetProfileAsync` mapear los dos campos nuevos desde `user.SubscriptionType?.ToString()` y `user.SubscriptionEndsAt` |
| `src/Server/SharedApiContracts/Auth/UserProfileResponse.cs` | Agregar `string? SubscriptionType`, `string? SubscriptionEndsAt` |
| `src/Server/Api/Endpoints/Private/AccountEndpoints.cs` | Incluir campos en `new UserProfileResponse(...)` (~línea 28) |
| `src/Web/Main/src/modules/auth/AuthContext.tsx` | Agregar `subscriptionType` y `subscriptionEndsAt` al contexto |
| `src/Web/Main/src/shared/layouts/PublicLayout.tsx` | Agregar entry "Suscripción" en el dropdown de cuenta (~línea 295) |
| `src/Web/Main/src/app/routes.tsx` | Agregar ruta `/suscripcion` fuera de ProtectedRoute |

### Archivos a crear (NEW)

| Archivo | Descripción |
|---------|-------------|
| `src/Web/Main/src/pages/SuscripcionPage.tsx` | Página principal de suscripción |

### Codegen es obligatorio

Después de T1.3 o T1.4, correr:
```bash
npm run codegen:api
```
Esto regenera el schema TypeScript desde el OpenAPI spec del backend. El nuevo `UserProfileResponse` con los campos extras quedará tipado en `components['schemas']['UserProfileResponse']`, que es lo que usa `authApi.ts`.

### Estado del menú móvil

Verificar si `PublicLayout.tsx` tiene un drawer/menú móvil con una sección de cuenta autenticada. Si lo tiene, replicar el ítem "Suscripción" ahí también. Buscar el string "Cerrar sesión" en el componente para encontrar todos los puntos de menú de cuenta.

### Datos de pago actuales (hardcoded — igual que en ActivarPage)

```tsx
const PLANES = [
  { nombre: 'Mensual', precio: '$299 MXN / mes', descripcion: 'Acceso completo mensual' },
  { nombre: 'Anual', precio: '$2,490 MXN / año', descripcion: 'Ahorra un 30% vs. mensual' },
  { nombre: 'Lifetime', precio: '$6,999 MXN', descripcion: 'Pago único, acceso de por vida' },
]
// CLABE y banco están marcados como [PENDIENTE] en ActivarPage — mantener igual en esta página
```

### Patrón de formato de fecha

Usar el mismo patrón de `14-6-ops-users-subscription-ui` (subscriptionStatus.ts):
```typescript
const date = new Date(isoString)
const formatted = date.toLocaleDateString('es-MX', { day: '2-digit', month: '2-digit', year: 'numeric' })
```

### Security Checklist — completar antes del primer commit

- [ ] **Auth-gating**: El botón "Ya pagué" llama a `notifyPayment()` que requiere auth. El componente ya hace guard de `status === 'anonymous'` al inicio. Si por alguna razón el token expiró en medio de la sesión, `notifyPayment()` lanzará `AuthApiError` — manejar con mensaje de error visible (igual que en `ActivarPage`).
- [ ] **No hay endpoints nuevos de escritura** en esta historia — el `POST /api/v1/account/notify-payment` ya existe.
- [ ] **UserProfileResponse es solo lectura** — no hay TOCTOU en el GET.

### Project Structure Notes

- `SuscripcionPage.tsx` va en `src/pages/` (junto con `ActivarPage.tsx`, `ConfirmarEmailPage.tsx`, `RegistroPage.tsx`) — no en un módulo propio ya que es una página de ciclo de vida de usuario, no un módulo de feature.
- El enum `SubscriptionType` en C# usa `Monthly`, `Annual`, `Lifetime` — coincidir exactamente en los string literals del frontend.
- `UserProfileData` (Application layer) y `UserProfileResponse` (SharedApiContracts) son records separados. Ambos necesitan actualización.

### References

- `ActivarPage.tsx` — `TrialExpiredView` y `TrialNotStartedView` son la referencia de UX y lógica de pago. [Source: src/Web/Main/src/pages/ActivarPage.tsx]
- `AuthContext.tsx` — estado actual del contexto, puntos donde se llama `fetchProfile`. [Source: src/Web/Main/src/modules/auth/AuthContext.tsx]
- `AccountEndpoints.cs` — endpoint `/api/v1/account/me` y construcción de `UserProfileResponse`. [Source: src/Server/Api/Endpoints/Private/AccountEndpoints.cs:28]
- `UserProfileData.cs` — record Application layer. [Source: src/Server/Application/Auth/UserProfileData.cs]
- `UserProfileResponse.cs` — record SharedApiContracts. [Source: src/Server/SharedApiContracts/Auth/UserProfileResponse.cs]
- `UserService.GetProfileAsync` — mapeo actual (líneas 167-180). [Source: src/Server/Infrastructure/Security/UserService.cs:167]
- `ProtectedRoute.tsx` — explica por qué `/suscripcion` no puede ir ahí. [Source: src/Web/Main/src/modules/auth/ProtectedRoute.tsx:28]
- `PublicLayout.tsx` — ubicación del dropdown de cuenta. [Source: src/Web/Main/src/shared/layouts/PublicLayout.tsx:293]
- `routes.tsx` — estructura de rutas actual. [Source: src/Web/Main/src/app/routes.tsx:74]

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

### Completion Notes List

- T1: `UserProfileData.cs`, `UserProfileResponse.cs`, `UserService.cs`, `AccountEndpoints.cs` — campos `SubscriptionType` y `SubscriptionEndsAt` expuestos en ISO-8601. Codegen corrido; campos aparecen en schema.d.ts. Build backend 0 errores.
- T2: `AuthContext.tsx` — dos nuevos states + mapeo en bootstrap, login, logout, handleAuthRequired y proactive-refresh. `AuthContextValue` interface actualizada.
- T3: `SuscripcionPage.tsx` + `suscripcion-logic.ts` — lógica `resolveSubscriptionState` extraída a módulo puro. Cuatro banners de estado (trial ámbar, active/lifetime verde, expired gris). Sección de pago oculta en Lifetime. Guard anon → redirect a login. `usePageTitle` con noindex,nofollow.
- T4: `routes.tsx` — lazy import + ruta `/suscripcion` fuera de `ProtectedRoute`.
- T5: `PublicLayout.tsx` + `public-navigation.ts` — ítem "Suscripción" añadido en desktop y menú móvil antes de "Mi perfil". Test `PublicLayout.test.ts` actualizado.
- T6: 10/10 tests backend AccountEndpointTests verdes; 200/200 tests frontend verdes (8 nuevos en SuscripcionPage.test.ts). Build frontend 0 errores TypeScript.
- Nota: la lógica de suscripción se extrajo a `suscripcion-logic.ts` porque Node test runner (strip-types) no puede importar `.tsx`. El `SuscripcionPage.tsx` re-exporta la función y el tipo para compatibilidad.

### File List

**Modificados:**
- `src/Server/Application/Auth/UserProfileData.cs`
- `src/Server/SharedApiContracts/Auth/UserProfileResponse.cs`
- `src/Server/Infrastructure/Security/UserService.cs`
- `src/Server/Api/Endpoints/Private/AccountEndpoints.cs`
- `src/Web/SharedApiClient/schema.d.ts` (generado por codegen)
- `src/Web/Main/src/modules/auth/AuthContext.tsx`
- `src/Web/Main/src/shared/layouts/PublicLayout.tsx`
- `src/Web/Main/src/shared/layouts/public-navigation.ts`
- `src/Web/Main/src/shared/layouts/PublicLayout.test.ts`
- `src/Web/Main/src/app/routes.tsx`
- `src/Web/Main/package.json`
- `tests/Integration/Api.Tests/AccountEndpointTests.cs`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

**Creados:**
- `src/Web/Main/src/pages/SuscripcionPage.tsx`
- `src/Web/Main/src/pages/suscripcion-logic.ts`
- `src/Web/Main/src/pages/SuscripcionPage.test.ts`

### Change Log

- feat(14.10): página /suscripcion — estado trial/active/lifetime/expired + instrucciones de pago + botón notificar — 2026-06-20

## Senior Developer Review (AI)

### Review Findings

- [x] **Decision D1** — Orden del menú corregido per AC1: Mi perfil → Suscripción → Cerrar sesión. `PublicLayout.tsx` + `public-navigation.ts` + `PublicLayout.test.ts`
- [x] **Patch P1** (HIGH) `formatDate` ahora retorna `'—'` para string vacío o fecha inválida — `SuscripcionPage.tsx:22`
- [x] **Patch P2** (MEDIUM) `daysRemaining = Math.max(0, Math.ceil(…))` — nunca negativo — `suscripcion-logic.ts:24`
- [x] **Patch P3** (HIGH) Rama `if (isActive)` añadida antes del fallback `expired` — modo degradado retorna `kind: 'trial'` en lugar de `expired` — `suscripcion-logic.ts`
- [x] **Patch P4** (MEDIUM) Rama Lifetime ya no requiere `isActive` — cuenta Lifetime desactivada sigue mostrando "Acceso de por vida" — `suscripcion-logic.ts:13`
- [x] **Defer D2** — Catch block con ramas idénticas en `handleNotifyPayment` — ambas hacen `setNotifyStatus('error')`, copiado del patrón de ActivarPage.tsx; funcional pero ruidoso — `SuscripcionPage.tsx:77`
- [x] **Defer D3** — Test de integración muta seed user (`11111111-…-0001`) sin cleanup explícito — riesgo de interferencia si otro test en la clase lee ese usuario posterior — `AccountEndpointTests.cs:149`
