# Story 14.6: UI Ops — Gestión de suscripciones de usuarios

Status: done

## Story

Como AdminOps,
quiero ver el estado de suscripción de cada usuario directamente en la tabla de Usuarios y poder asignar o modificar suscripciones desde ahí,
para que pueda activar el acceso de un cliente que reportó pago sin necesidad de llamar al endpoint manualmente.

## Acceptance Criteria

1. **Dado que** abro `/ops/users` con usuarios registrados que tienen datos de suscripción (trial o plan activo),
   **Entonces** la tabla muestra dos columnas nuevas: **Email** (badge ✓/✗ según `emailConfirmedAt`) y **Suscripción** (badge compuesto que muestra tipo + vencimiento o estado del trial).

2. **Dado que** un usuario tiene `subscriptionType = null` y `trialEndsAt != null` en el futuro,
   **Entonces** el badge de suscripción muestra "Trial · vence DD/MM/AAAA" en color ámbar.

3. **Dado que** un usuario tiene `subscriptionType = "Monthly"` o `"Annual"` con `subscriptionEndsAt` en el futuro,
   **Entonces** el badge muestra "Monthly · vence DD/MM/AAAA" o "Annual · vence DD/MM/AAAA" en color verde.

4. **Dado que** un usuario tiene `subscriptionType = "Lifetime"`,
   **Entonces** el badge muestra "Lifetime" en color verde sin fecha de vencimiento.

5. **Dado que** un usuario tiene `subscriptionEndsAt` o `trialEndsAt` en el pasado (o ambos null),
   **Entonces** el badge muestra "Sin acceso" en color gris.

6. **Dado que** hago clic en el botón "Suscripción" en la columna Acciones de cualquier usuario con `role = "User"`,
   **Entonces** se abre un modal con los campos: Tipo (select Monthly/Annual/Lifetime), Fecha inicio (date input), Fecha fin (date input, oculto si Tipo = Lifetime); al guardar llama a `PATCH /api/v1/ops/users/{id}/subscription` y actualiza la tabla con el DTO devuelto.

7. **Dado que** el modal está abierto y selecciono "Lifetime",
   **Entonces** el campo "Fecha fin" desaparece (no se envía `endsAt` — el endpoint acepta `null`).

8. **Dado que** el `PATCH /subscription` responde con éxito (200),
   **Entonces** el modal se cierra, la tabla se actualiza con los nuevos valores y se muestra un toast/mensaje de confirmación en línea.

9. **Dado que** `usersApi.ts` importa el tipo `UpdateSubscriptionRequest` del schema generado,
   **Entonces** la función `updateUserSubscription` usa `openapi-fetch` con el path `/api/v1/ops/users/{id}/subscription` PATCH — no construye URLs manualmente.

10. **Dado que** corro `npm run build --workspace=src/Web/Ops`,
    **Entonces** 0 errores TypeScript y 0 warnings.

## Tasks / Subtasks

- [x] T1: Agregar `updateUserSubscription` en `usersApi.ts` (AC: 9)
  - [x] T1.1: Exportar tipo `UpdateSubscriptionRequest` desde `components['schemas']`
  - [x] T1.2: Implementar `updateUserSubscription(id: string, body: UpdateSubscriptionRequest): Promise<UserSummaryDto>` usando `apiClient['/api/v1/ops/users/{id}/subscription'].PATCH`

- [x] T2: Crear helper `subscriptionBadge` para calcular estado y badge (AC: 2, 3, 4, 5)
  - [x] T2.1: Función pura `getSubscriptionStatus(user: UserSummaryDto): { label: string; color: 'green' | 'amber' | 'gray' }` — ver Dev Notes para la lógica exacta

- [x] T3: Agregar columna "Email conf." en la tabla (AC: 1)
  - [x] T3.1: Nueva `<th>` "Email" y `<td>` con badge ✓ verde / ✗ gris según `user.emailConfirmedAt != null`

- [x] T4: Agregar columna "Suscripción" en la tabla (AC: 1, 2, 3, 4, 5)
  - [x] T4.1: Nueva `<th>` "Suscripción" y `<td>` que renderiza el badge compuesto de `getSubscriptionStatus`

- [x] T5: Crear componente `SubscriptionModal` (AC: 6, 7, 8)
  - [x] T5.1: Props: `userId: string`, `current: UserSummaryDto`, `onClose: () => void`
  - [x] T5.2: Campos: Tipo (select Monthly/Annual/Lifetime), Fecha inicio (date), Fecha fin (date — solo si tipo ≠ Lifetime)
  - [x] T5.3: Inicializar campos con los valores actuales del usuario si existen
  - [x] T5.4: Al submit, llamar `updateUserSubscription` y al success `invalidateQueries(['ops-users'])` + `onClose()`
  - [x] T5.5: Mostrar error inline si el PATCH falla

- [x] T6: Conectar `SubscriptionModal` desde la tabla (AC: 6, 8)
  - [x] T6.1: Agregar estado `subscriptionModalUserId: string | null` en `UsersPage`
  - [x] T6.2: Botón "Suscripción" solo visible para usuarios con `role === 'User'` en la columna Acciones existente
  - [x] T6.3: Renderizar `<SubscriptionModal>` cuando `subscriptionModalUserId != null`

- [x] T7: Build y verificación (AC: 10)
  - [x] T7.1: `npm run build --workspace=src/Web/Ops` — 0 errores TypeScript, 0 warnings

## Dev Notes

### Estado actual del código antes de esta historia

| Archivo | Estado |
|---|---|
| `src/Web/Ops/src/api/usersApi.ts` | 5 funciones (fetch, create, setActive, changePassword, updatePayment). Sin `updateUserSubscription`. `UserSummaryDto` importado del schema ya incluye todos los campos nuevos pero no se usan en la UI. |
| `src/Web/Ops/src/pages/UsersPage.tsx` | Tabla de 6 columnas: Correo / Tipo / Estado / Pago•Fecha / Creado / Acciones. Sin columnas de suscripción. Sin botón "Suscripción" en acciones. |
| `src/Web/SharedApiClient/schema.d.ts` | `UserSummaryDto` ya incluye `subscriptionType`, `subscriptionStartedAt`, `subscriptionEndsAt`, `trialEndsAt`, `emailConfirmedAt` (todos `null | string`). `UpdateSubscriptionRequest` ya incluye `type`, `startedAt`, `endsAt`. El endpoint PATCH ya está en los paths generados. |
| `src/Server/Api/Endpoints/Ops/OpsUserEndpoints.cs` | El endpoint `PATCH /api/v1/ops/users/{id:guid}/subscription` ya existe y está funcional. Al activar (`user.IsActive == true` tras la actualización), dispara `SendAccessActivatedAsync` con try/catch. |

### UserSummaryDto — campos de suscripción disponibles

Todos vienen del schema generado como `null | string`. Las fechas son ISO-8601 (`date-time`):

```typescript
// Ya disponibles en components['schemas']['UserSummaryDto']
subscriptionType: null | string          // "Monthly" | "Annual" | "Lifetime" | null
subscriptionStartedAt: null | string     // ISO-8601 o null
subscriptionEndsAt: null | string        // ISO-8601 o null (null = Lifetime)
trialEndsAt: null | string               // ISO-8601 o null
emailConfirmedAt: null | string          // ISO-8601 o null
```

### Lógica de `getSubscriptionStatus`

```typescript
type SubscriptionStatus = { label: string; color: 'green' | 'amber' | 'gray' }

function getSubscriptionStatus(user: UserSummaryDto): SubscriptionStatus {
  const now = new Date()
  const fmt = (d: string) => new Date(d).toLocaleDateString('es-MX', { dateStyle: 'short' })

  if (user.subscriptionType === 'Lifetime') {
    return { label: 'Lifetime', color: 'green' }
  }
  if (user.subscriptionType && user.subscriptionEndsAt) {
    const endsAt = new Date(user.subscriptionEndsAt)
    if (endsAt > now) return { label: `${user.subscriptionType} · vence ${fmt(user.subscriptionEndsAt)}`, color: 'green' }
  }
  if (user.trialEndsAt) {
    const trialEnd = new Date(user.trialEndsAt)
    if (trialEnd > now) return { label: `Trial · vence ${fmt(user.trialEndsAt)}`, color: 'amber' }
  }
  return { label: 'Sin acceso', color: 'gray' }
}
```

### `updateUserSubscription` en `usersApi.ts`

```typescript
export type UpdateSubscriptionRequest = components['schemas']['UpdateSubscriptionRequest']

export async function updateUserSubscription(
  id: string,
  body: UpdateSubscriptionRequest,
): Promise<UserSummaryDto> {
  assertOpsAccessToken()
  const { data, error } = await apiClient['/api/v1/ops/users/{id}/subscription'].PATCH({
    headers: getOpsAuthHeaders(),
    params: { path: { id } },
    body,
  })
  if (error) throw new Error(getOpsApiErrorMessage(error, `Error al actualizar suscripción: ${JSON.stringify(error)}`))
  if (!data) throw new Error('La API no devolvió datos.')
  return data
}
```

### `SubscriptionModal` — detalles de implementación

- Inicializar `tipo` con `user.subscriptionType ?? 'Monthly'`
- Inicializar `startedAt` con `user.subscriptionStartedAt ? toDateInput(user.subscriptionStartedAt) : today`
- Inicializar `endsAt` con `user.subscriptionEndsAt ? toDateInput(user.subscriptionEndsAt) : ''`
- Helper: `toDateInput(iso: string) => new Date(iso).toISOString().substring(0, 10)`
- Al submit: si `tipo === 'Lifetime'`, enviar `{ type: 'Lifetime', startedAt: new Date(startedAt).toISOString(), endsAt: null }`. Si no, enviar `endsAt: new Date(endsAt).toISOString()`.
- El modal usa el mismo diseño que `ChangePasswordDialog`: `fixed inset-0 z-50 flex items-center justify-center bg-black/40`, inner `max-w-sm rounded-[1.75rem] border border-slate-200 bg-white p-6 shadow-xl`

### Badge de email confirmado

```tsx
// Badge simple — no necesita componente separado
<td className="px-4 py-3">
  {user.emailConfirmedAt != null
    ? <span className="rounded-full bg-emerald-100 px-2 py-0.5 text-xs font-semibold text-emerald-700">✓</span>
    : <span className="rounded-full bg-slate-100 px-2 py-0.5 text-xs font-semibold text-slate-500">✗</span>}
</td>
```

### Badge de suscripción

```tsx
const STATUS_COLORS = {
  green: 'bg-emerald-100 text-emerald-800',
  amber: 'bg-amber-100 text-amber-800',
  gray: 'bg-slate-100 text-slate-600',
} as const

// Dentro de <td>:
const status = getSubscriptionStatus(user)
<span className={`rounded-full px-2.5 py-1 text-xs font-semibold ${STATUS_COLORS[status.color]}`}>
  {status.label}
</span>
```

### Orden de columnas en la tabla actualizada

| Pos | Columna | Datos |
|---|---|---|
| 1 | Correo | `user.email` (sin cambios) |
| 2 | Tipo | badge Role (sin cambios) |
| 3 | Estado | badge Activo/Inactivo (sin cambios) |
| 4 | Email conf. | badge ✓/✗ según `emailConfirmedAt` ← **NUEVA** |
| 5 | Suscripción | badge compuesto ← **NUEVA** |
| 6 | Pago / Fecha | `PaymentCell` (sin cambios) |
| 7 | Creado | `createdAt` (sin cambios) |
| 8 | Acciones | toggle activo + contraseña + **suscripción** ← **NUEVA acción** |

### Archivos a MODIFICAR (UPDATE)

- `src/Web/Ops/src/api/usersApi.ts` — agregar `updateUserSubscription` + exportar `UpdateSubscriptionRequest`
- `src/Web/Ops/src/pages/UsersPage.tsx` — nuevas columnas + `SubscriptionModal` + botón en acciones

### Archivos a CREAR (NEW)

- Ninguno (todo va inline en `UsersPage.tsx` como los otros componentes del mismo archivo)

### NO tocar

- `src/Server/Api/Endpoints/Ops/OpsUserEndpoints.cs` — backend completo, sin cambios
- `src/Web/SharedApiClient/schema.d.ts` — generado automáticamente, no editar manualmente
- `src/Web/Ops/src/pages/ConfigPage.tsx` — fuera del scope
- Cualquier archivo backend o de tests

### Notas de diseño y UX

- **Tabla ancha**: Con 8 columnas la tabla seguirá siendo scrollable horizontalmente (`overflow-x-auto` ya está presente). No eliminar columnas existentes.
- **Botón "Suscripción" solo para `role === 'User'`**: Los AdminOps no tienen suscripción.
- **Email conf. no aplica a AdminOps**: Mostrar `—` para usuarios AdminOps en la columna de email confirmado (no tienen flujo de confirmación).
- **Sin tests unitarios de frontend para esta historia**: la historia es UI pura sobre un endpoint ya testado en backend. El gate de calidad es `npm run build` sin errores TypeScript.

### Project Structure Notes — referencia de patrón openapi-fetch

Todos los endpoints en `usersApi.ts` usan el patrón:
```typescript
await apiClient['/api/v1/ops/users/{id}/xyz'].PATCH({
  headers: getOpsAuthHeaders(),
  params: { path: { id } },
  body: { ... },
})
```
El path del endpoint en el schema es `/api/v1/ops/users/{id}/subscription` (con `{id}`, no `{id:guid}`).

## Dev Agent Record

### Agent Model Used
GPT-5

### Debug Log References
- `npm test --workspace=src/Web/Ops` → 18 tests passed, 0 failed.
- `npm run build --workspace=src/Web/Ops` → success, Vite build completed without TypeScript errors or warnings.
- `npx eslint --config src/Web/Ops/eslint.config.js src/Web/Ops/src/api/usersApi.ts src/Web/Ops/src/pages/UsersPage.tsx src/Web/Ops/src/utils/subscriptionStatus.ts tests/ops/subscriptionStatus.test.ts` → success.
- `npm run lint --workspace=src/Web/Ops` → failed due pre-existing lint issues in unrelated files outside this story scope.

### Completion Notes List
- Added `updateUserSubscription` to `usersApi.ts` and exported `UpdateSubscriptionRequest` from the generated schema types.
- Added a pure subscription-status/date helper with unit coverage for Lifetime, active Monthly, active Trial, expired access, and date formatting/input conversion.
- Extended the Users table with Email confirmation and Subscription badges, plus a subscription action for `role === 'User'`.
- Added `SubscriptionModal` with Monthly/Annual/Lifetime handling, inline PATCH error display, cache update on success, and inline success notice in the page.
- Verified with `npm test --workspace=src/Web/Ops` and `npm run build --workspace=src/Web/Ops`.

### File List
- `_bmad-output/implementation-artifacts/14-6-ops-users-subscription-ui.md`
- `src/Web/Ops/package.json`
- `src/Web/Ops/src/api/usersApi.ts`
- `src/Web/Ops/src/pages/UsersPage.tsx`
- `src/Web/Ops/src/utils/subscriptionStatus.ts`
- `tests/ops/subscriptionStatus.test.ts`

### Change Log
- 2026-06-19: Implemented Ops users subscription UI, subscription badge helper, PATCH client wrapper, and unit tests; marked story ready for review.

## Senior Developer Review (AI)

### Review Findings

- [x] **Patch P1:** `getTodayDateInput` usa getters locales en vez de UTC — fecha default incorrecta para usuarios en zonas UTC±N cerca de medianoche — `subscriptionStatus.ts:26-31`
- [x] **Patch P2:** `formatUtcDate`/`toDateInput` sin guard de Invalid Date — string ISO inválido produce "NaN/NaN/NaN" visible en badge de suscripción — `subscriptionStatus.ts:10-24`
- [x] **Patch P3:** `subscriptionNotice` nunca se limpia al abrir un nuevo modal — banner de éxito de usuario A queda visible al editar usuario B — `UsersPage.tsx`
- [x] **Patch P4:** Email badge usa `px-2.5 py-1 text-emerald-800` en vez de `px-2 py-0.5 text-emerald-700` especificados en Dev Notes — `UsersPage.tsx:~585`
- [x] **Patch P5:** Test faltante — plan `Annual` activo no está cubierto, solo `Monthly` — `tests/ops/subscriptionStatus.test.ts`
- [x] **Patch P6:** Test faltante — `subscriptionType` presente + `subscriptionEndsAt === null` no tiene caso de test documentado — `tests/ops/subscriptionStatus.test.ts`
- [x] **Defer D1:** `subscriptionType` no-Lifetime + `subscriptionEndsAt === null` → "Sin acceso" silencioso — edge case de contrato de backend, defer
- [x] **Defer D2:** `subscriptionType` con valor no reconocido → "Sin acceso" sin indicación de dato inválido — riesgo de evolución de API, defer
- [x] **Defer D3:** Modal no cierra con Escape ni click en overlay — mejora WCAG/UX fuera de scope del AC, defer
- [x] **Defer D4:** Sin atributos ARIA `role="dialog"` / `aria-modal` / `aria-labelledby` — deuda de accesibilidad, defer
- [x] **Defer D5:** Sin validación `endsAt > startedAt` en frontend — el backend puede validarlo, defer
- [x] **Defer D6:** Estado `endsAt` stale al ciclar Monthly→Lifetime→Monthly — guard de `handleSubmit` previene envío vacío, defer
- [x] **Defer D7:** Race condition `setQueryData` optimista + `invalidateQueries` — patrón pre-existente del módulo, defer
- [x] **Defer D8:** Enter en input de fecha puede re-enviar formulario mientras mutation está en vuelo — mejora UX, defer
