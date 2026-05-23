---
title: 'Ops Session Stability — double-mount race + token lifetime + proactive refresh'
type: 'bugfix'
created: '2026-05-23'
status: 'done'
baseline_commit: '5b15e379eed6b37a4f4dfeba8a1d1e02ab8ebcda'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** La sesión de Ops muere en menos de un minuto. Dos bugs apilados: (1) React StrictMode double-mount hace que el bootstrap dispare dos llamadas simultáneas a `/auth/refresh` con el mismo cookie; la primera rota el refresh token y la segunda llega con el token ya revocado → 401 → `clearOpsAccessToken()` → pantalla en blanco / login. (2) Sin refresh proactivo, a los 15 min el access token expira y el siguiente API call también fuerza logout.

**Approach:** Tres correcciones: deduplicar llamadas concurrentes en `refreshOpsSession` (fix del race), no limpiar el token dentro de `refreshOpsSession` al recibir 401 (fix de la pérdida innecesaria de token válido), aumentar `AccessTokenMinutes` a 480 y agregar refresh proactivo cada 4 horas en `OpsLoginGate`.

## Boundaries & Constraints

**Always:**
- El dedup se implementa con una Promise a nivel de módulo en `authApi.ts` — una sola petición HTTP simultánea máximo.
- `clearOpsAccessToken()` solo se llama en: `handleAuthRequired` (evento 401 de API calls reales), `handleLogout`, y el callback del refresh proactivo cuando retorna `false`.
- El `active` flag del `useEffect` existente se replica en el interval callback.
- El intervalo de refresh proactivo solo corre cuando `authStatus === 'authenticated'`.

**Ask First:**
- Si el equipo tiene política de seguridad que prohíbe tokens >1 hora.

**Never:**
- No remover `React.StrictMode` de `main.tsx` — la solución debe ser resiliente, no evasiva.
- No modificar el backend de rotación de refresh tokens.
- No mover el access token de sessionStorage a localStorage (deferred explícito en deferred-work.md).
- No tocar la lógica del `OPS_AUTH_REQUIRED_EVENT` ni de `getOpsApiErrorMessage`.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Double-mount Strict Mode con cookie válida | Dos llamadas simultáneas a `/auth/refresh` | Una sola petición HTTP; ambos awaits reciben el mismo resultado | N/A |
| Bootstrap sin cookie (primera sesión) | Cookie ausente → 401 | `setAuthStatus('anonymous')`; sin `clearOpsAccessToken()` innecesario | N/A |
| Bootstrap con token en sessionStorage y refresh fallido por 401 | Cookie expirada pero token aún válido en sessionStorage | `restored = false`, token no borrado, `isAuthenticated = true` (token vivo) | N/A |
| Trabajo activo < 8 h | Token válido, API calls normales | Sin logout forzado | N/A |
| Refresh proactivo exitoso a las 4 h | Interval dispara, refresh token válido | Nuevo access token; sesión continúa sin interrupción | N/A |
| Refresh proactivo con refresh token expirado | `refreshOpsSession()` retorna `false` | `clearOpsAccessToken()` + `queryClient.clear()` + `setAuthStatus('anonymous')` | N/A |
| Error de red en refresh proactivo | `refreshOpsSession()` lanza excepción | Silenciado; el 401 real del siguiente API call maneja el caso | catch vacío |

</frozen-after-approval>

## Code Map

- `src/Web/Ops/src/api/authApi.ts:37` -- `refreshOpsSession()` — aquí va el dedup y el fix de clearToken
- `src/Web/Ops/src/components/OpsLoginGate.tsx:23` -- bootstrap `useEffect` — aquí va el refresh proactivo
- `src/Server/Api/appsettings.json:14` -- `AccessTokenMinutes: "15"` — aumentar a `"480"`
- `src/Web/Ops/src/main.tsx:5` -- `<StrictMode>` — confirmar presencia; NO modificar

## Tasks & Acceptance

**Execution:**
- [x] `src/Web/Ops/src/api/authApi.ts` -- agregar variable de módulo `let _refreshInFlight: Promise<boolean> | null = null`; en `refreshOpsSession()`, si `_refreshInFlight` existe, retornarla en lugar de hacer nueva petición; limpiar con `.finally()`; además, remover `clearOpsAccessToken()` de la rama 401 del error handler (solo retornar `false`)
- [x] `src/Server/Api/appsettings.json` -- cambiar `"AccessTokenMinutes": "15"` → `"AccessTokenMinutes": "480"`
- [x] `src/Web/Ops/src/components/OpsLoginGate.tsx` -- agregar `useEffect` con `setInterval` de 4 horas dependiendo de `[authStatus, queryClient]`; solo cuando `authStatus === 'authenticated'`; si retorna `false` → `clearOpsAccessToken() + queryClient.clear() + setPassword('') + setAuthStatus('anonymous')`; errores silenciados; cleanup con `clearInterval`

**Acceptance Criteria:**
- Dado que el usuario tiene una sesión válida (cookie presente) y recarga la página en dev (Strict Mode), cuando el bootstrap termina, entonces la app carga en estado `authenticated` — no en `anonymous`.
- Dado bootstrap con refresh token expirado pero access token aún en sessionStorage, cuando `refreshOpsSession()` retorna false, entonces el token NO se borra y `isAuthenticated` evalúa el token existente correctamente.
- Dado que el usuario está autenticado y han pasado 4 horas, cuando el refresh proactivo dispara, entonces obtiene un nuevo access token sin interrupción visible.
- Dado que el refresh proactivo falla (refresh token expirado), cuando `refreshOpsSession()` retorna `false`, entonces la app hace logout ordenado y muestra el login form.
- Dado un error de red en el refresh proactivo, cuando `refreshOpsSession()` lanza, entonces el error se silencia y la sesión continúa.
- Dado trabajo activo dentro de las primeras 8 horas, cuando el usuario hace cualquier acción API, entonces no hay redirect a login ni pantalla en blanco.

## Design Notes

**Promise dedup en `refreshOpsSession`:**
```typescript
let _refreshInFlight: Promise<boolean> | null = null

export async function refreshOpsSession(): Promise<boolean> {
  if (_refreshInFlight) return _refreshInFlight
  _refreshInFlight = (async () => {
    const { data, error } = await apiClient['/api/v1/auth/refresh'].POST({})
    if (error) {
      if (/* status 401 check */) return false  // NO clearOpsAccessToken aquí
      throw new Error(getOpsApiErrorMessage(error, 'No se pudo restaurar la sesión de Ops.'))
    }
    await persistAccessToken(data, 'La API no devolvió access token al refrescar la sesión.')
    return true
  })()
  return _refreshInFlight.finally(() => { _refreshInFlight = null })
}
```

Con Strict Mode, Effect 1 (active=false) y Effect 2 (active=true) comparten la misma promesa. Cuando resuelve, Effect 1 hace `if (!active) return` sin efecto; Effect 2 procesa correctamente el resultado.

**Por qué no borrar el token en `refreshOpsSession` al 401:**
Si el refresh token expiró pero el access token en sessionStorage aún es válido (emitido hace <8h con el nuevo lifetime), el usuario debería poder continuar trabajando. Borrar el token aquí fuerza un logout innecesario.

## Verification

**Commands:**
- `dotnet build FIBRADIS.slnx` -- expected: Build succeeded, 0 errors
- `cd src/Web/Ops && npx tsc --noEmit` -- expected: sin errores TypeScript

**Manual checks:**
- En dev (npm run dev:ops): recargar la página con sesión activa → la app debe cargar directamente en `authenticated`, sin pasar por login form.
- Verificar en Network tab: una sola request a `/api/v1/auth/refresh` en el bootstrap (no dos).

## Spec Change Log

## Suggested Review Order

**Race condition fix (core of the bug)**

- Dedup: module-level promise, single HTTP request per refresh cycle
  [`authApi.ts:39`](../../src/Web/Ops/src/api/authApi.ts#L39)

- Guard: reuse in-flight promise, prevents concurrent token rotation
  [`authApi.ts:42`](../../src/Web/Ops/src/api/authApi.ts#L42)

- Fix: `signOutOn401: false` — stop logout side effect in refresh error path
  [`authApi.ts:60`](../../src/Web/Ops/src/api/authApi.ts#L60)

**Proactive session renewal**

- Interval constant — 4 h gives 50% safety margin within 8-h token lifetime
  [`OpsLoginGate.tsx:11`](../../src/Web/Ops/src/components/OpsLoginGate.tsx#L11)

- Effect wires interval, active flag prevents setState after unmount
  [`OpsLoginGate.tsx:68`](../../src/Web/Ops/src/components/OpsLoginGate.tsx#L68)

- Logout path: only if refresh returns false; network errors silenced
  [`OpsLoginGate.tsx:72`](../../src/Web/Ops/src/components/OpsLoginGate.tsx#L72)

**Config**

- Token lifetime: 15 min → 480 min (8 hours)
  [`appsettings.json:13`](../../src/Server/Api/appsettings.json#L13)
