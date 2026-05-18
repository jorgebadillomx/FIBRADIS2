---
project: FIBRADIS
epics: [1]
version: 1.0
date: 2026-05-18
status: draft
prepared_by: QA Lead (bmad-testing-guide)
epic_title: Fundación, Infraestructura y Acceso
---

# Guía de Pruebas — Épica 1: Fundación, Infraestructura y Acceso
## FIBRADIS

---

## 1. Propósito de este documento

Este documento es una guía de pruebas funcionales para el sistema **FIBRADIS**. Está escrito para testers que no participaron en el desarrollo y que pueden no conocer el sistema previamente.

La Épica 1 entrega la base técnica invisible de la plataforma: autenticación, control de acceso por rol, endpoint de salud del sistema y el motor de jobs en background. No entrega pantallas de negocio visibles para el usuario final — entrega el plomería que hace funcionar todo lo demás.

El objetivo de esta guía es verificar que:
- Un usuario puede iniciar sesión y el sistema le da acceso según su rol
- El sistema rechaza correctamente accesos no autorizados
- El sistema reporta su estado de salud de forma correcta
- Los tokens de sesión funcionan correctamente (se renuevan, se invalidan)

No se requiere conocimiento del código. Solo necesitas una herramienta para hacer llamadas HTTP (como Postman, Insomnia, curl, o un navegador con extensión REST Client) y acceso al sistema desplegado.

---

## 2. Alcance de esta guía

### Épica 1: Fundación, Infraestructura y Acceso

Esta guía cubre exactamente lo que fue implementado en las 4 historias de la Épica 1:

**Autenticación y sesión (Historia 1.3)**
- Login con correo y contraseña → recibir token de acceso
- Renovar el token de acceso cuando expira (refresh)
- El sistema rechaza credenciales incorrectas
- Los refresh tokens son de un solo uso (rotan al usarse)

**Control de acceso por rol (Historia 1.3)**
- Las rutas privadas requieren autenticación → `401 Unauthorized` sin token
- Las rutas de Ops solo están disponibles para el rol `AdminOps` → `403 Forbidden` para usuarios normales
- Las rutas públicas son accesibles sin autenticación

**Endpoint de salud del sistema (Historia 1.4)**
- `GET /health` devuelve el estado del sistema con checks de base de datos y pipelines
- El sistema reporta estado `Healthy`, `Degraded` o `Unhealthy` según corresponda

**Formato de errores (Historia 1.2)**
- Todos los errores del sistema siguen un formato estándar con `domainCode` y `correlationId`
- El header `X-Correlation-Id` está presente en todas las respuestas de error

**Dashboard de Hangfire (Historia 1.4)**
- El dashboard de jobs en background existe en `/hangfire`
- En Development es accesible libremente; en Production solo para `AdminOps`

---

## 3. Fuera de alcance

Las siguientes áreas **no** son parte de esta guía y no deben evaluarse en esta ronda:

- **Catálogo de FIBRAs** — Cubierto en la Guía de Pruebas de la Épica 2
- **Home pública y búsqueda global** — Cubierto en la Guía de Pruebas de la Épica 2
- **Ficha pública de FIBRA** — Cubierto en la Guía de Pruebas de la Épica 2
- **Precios de mercado y datos en tiempo real** — Épica 3 (no implementada)
- **Noticias y contenido** — Épica 4 (no implementada)
- **Centro de Procesos (Ops)** — Épica 5 (no implementada)
- **Portafolio de usuario** — Épica 6 (no implementada)
- **Oportunidades y favoritos** — Épica 7 (no implementada)
- **Interfaz gráfica de usuario (frontend)** — La Épica 1 no entrega pantallas de usuario final

> **Nota importante:** Al hacer login, recibirás un `accessToken` (cadena de texto larga). Guárdalo porque lo necesitas para las pruebas de rutas protegidas. No intentes interpretar su contenido.

---

## 4. Antes de empezar

### 4.1 Prerrequisitos

**Acceso al sistema:**
- URL base del API (ejemplo: `http://localhost:5265` en desarrollo local, o la URL del servidor de pruebas)
- Una herramienta para hacer peticiones HTTP:
  - [Postman](https://www.postman.com/) (recomendado para principiantes)
  - [Insomnia](https://insomnia.rest/)
  - `curl` en terminal
  - La extensión REST Client de VS Code

**Datos que deben existir en el sistema:**
- Una cuenta con rol **`User`** (usuario normal)
  - Email: `user@fibradis.dev` / Contraseña: confirmar con el equipo
- Una cuenta con rol **`AdminOps`** (administrador)
  - Email: `admin@fibradis.dev` / Contraseña: confirmar con el equipo

> Si el sistema está recién desplegado, estas cuentas deben haber sido creadas durante el proceso de migración de base de datos. Si no existen, contacta al equipo de desarrollo antes de continuar.

**Herramientas:**
- Navegador web actualizado (para verificar el dashboard de Hangfire)
- Terminal o Postman para las llamadas HTTP

### 4.2 Roles de usuario disponibles

El sistema tiene dos tipos de usuario:

**`User` — Usuario normal**
- ¿Qué puede hacer?: Acceder a las secciones privadas de la plataforma (portafolio, oportunidades, etc. — disponibles en épicas futuras)
- Cómo reconocerlo: Su token JWT contiene el claim `role: "User"`

**`AdminOps` — Administrador de operaciones**
- ¿Qué puede hacer?: Todo lo que puede hacer `User`, más acceso al Centro de Procesos y al dashboard de Hangfire en producción
- Cómo reconocerlo: Su token JWT contiene el claim `role: "AdminOps"`

### 4.3 Cómo hacer las peticiones

Todas las peticiones van a la URL base del API. Por ejemplo:

```
POST http://localhost:5265/api/v1/auth/login
Content-Type: application/json

{
  "email": "user@fibradis.dev",
  "password": "TuContraseña"
}
```

Para peticiones que requieren autenticación, agrega el header:
```
Authorization: Bearer <aquí-va-el-accessToken-que-recibiste-al-hacer-login>
```

### 4.4 Glosario

| Término | Significado |
|---------|-------------|
| **API** | El servicio de backend que procesa las peticiones. Todas las peticiones van a `/api/v1/...` |
| **Access token** | Un texto largo (JWT) que prueba que iniciaste sesión. Tiene una vida corta (minutos). Úsalo en el header `Authorization: Bearer ...` |
| **Refresh token** | Una credencial de larga duración guardada en una cookie segura. Se usa para obtener un nuevo access token sin hacer login de nuevo |
| **Cookie HttpOnly** | Una cookie que el navegador guarda automáticamente y que no es visible desde JavaScript. El refresh token se guarda así |
| **JWT** | El formato del access token. Es una cadena de tres partes separadas por puntos. No intentes leerlo directamente |
| **Rol** | Tu nivel de permiso en el sistema: `User` (usuario normal) o `AdminOps` (administrador) |
| **401 Unauthorized** | El sistema no reconoció tu identidad. Necesitas hacer login o tu token expiró |
| **403 Forbidden** | El sistema reconoció tu identidad pero no tienes permiso para esa acción |
| **ProblemDetails** | El formato estándar de errores del sistema: incluye tipo de error, código de dominio y correlationId |
| **domainCode** | Un código de error del sistema en mayúsculas, como `INVALID_CREDENTIALS`. Útil para identificar el tipo exacto de error |
| **correlationId** | Un identificador único que permite rastrear una petición específica en los logs del sistema |
| **X-Correlation-Id** | El header HTTP que contiene el correlationId |
| **Health check** | Una verificación automática del estado de un componente del sistema (base de datos, pipelines, etc.) |
| **Hangfire** | El motor de jobs en background del sistema. Su dashboard está en `/hangfire` |
| **FIBRA** | Fideicomiso de Inversión en Bienes Raíces — el tipo de activo financiero que maneja FIBRADIS |

---

## 5. Escenarios de prueba

---

### 5.1. Autenticación — Login y tokens

**¿Qué es esto?**
El sistema permite que usuarios registrados inicien sesión con su correo y contraseña. Al hacerlo, reciben un access token (para usar en peticiones inmediatas) y un refresh token (para renovar el access token cuando expira sin necesidad de hacer login de nuevo).

**¿Quién lo usa?**
Cualquier usuario registrado en el sistema, tanto `User` como `AdminOps`.

---

#### Prueba 1.1: Login exitoso con credenciales válidas de usuario normal

**Objetivo:** Verificar que un usuario con credenciales correctas recibe un access token y una cookie con refresh token.

**Pasos:**

1. Haz una petición `POST` a `/api/v1/auth/login` con el siguiente cuerpo:
   ```json
   {
     "email": "user@fibradis.dev",
     "password": "<contraseña-del-usuario>"
   }
   ```
   → *Debes ver:* Respuesta `200 OK`

2. Revisa el cuerpo de la respuesta.
   → *Debes ver:* Un JSON con la propiedad `accessToken` que contiene una cadena larga de texto (el JWT)

3. Revisa las cookies de la respuesta.
   → *Debes ver:* Una cookie llamada `refreshToken` marcada como `HttpOnly` (no visible desde JavaScript)

**¿Qué debes ver al terminar?** El sistema devolvió un `accessToken` y estableció una cookie `refreshToken`.

**Criterio de éxito:** Status 200, cuerpo con `accessToken`, cookie `refreshToken` presente.

---

#### Prueba 1.2: Login exitoso con credenciales válidas de AdminOps

**Objetivo:** Verificar que un administrador también puede hacer login correctamente.

**Pasos:**

1. Haz una petición `POST` a `/api/v1/auth/login` con:
   ```json
   {
     "email": "admin@fibradis.dev",
     "password": "<contraseña-del-admin>"
   }
   ```
   → *Debes ver:* Respuesta `200 OK` con `accessToken`

**Criterio de éxito:** Status 200, cuerpo con `accessToken`. Guarda este token para las pruebas 3.2 y 3.3.

---

#### Prueba 1.3: Refresh de token — obtener nuevo access token

**Objetivo:** Verificar que el sistema emite un nuevo access token usando el refresh token, e invalida el refresh token anterior.

**Prerrequisitos específicos:**
- Haber completado la Prueba 1.1 (tienes la cookie `refreshToken` activa)

**Pasos:**

1. Haz una petición `POST` a `/api/v1/auth/refresh`.
   - Si usas Postman/Insomnia: asegúrate de que las cookies estén habilitadas (la cookie `refreshToken` se enviará automáticamente)
   - Si usas curl: incluye la cookie manualmente con `--cookie "refreshToken=<valor>"`
   → *Debes ver:* Respuesta `200 OK`

2. Revisa el cuerpo de la respuesta.
   → *Debes ver:* Un nuevo `accessToken` diferente al que recibiste en el login

3. Revisa las cookies de la respuesta.
   → *Debes ver:* Una nueva cookie `refreshToken` (el anterior fue invalidado)

**¿Qué debes ver al terminar?** El sistema emitió un nuevo access token y una nueva cookie de refresh.

**Criterio de éxito:** Status 200, nuevo `accessToken`, nueva cookie `refreshToken`.

---

#### Prueba 1.4: Refresh token de un solo uso (no se puede reutilizar)

**Objetivo:** Verificar que el refresh token anterior queda invalidado después de usarlo una vez.

**Prerrequisitos específicos:**
- Haber completado la Prueba 1.3 (ya usaste el refresh token una vez y tienes uno nuevo)
- Guardar el valor del refresh token ANTERIOR (el que usaste en la Prueba 1.3)

**Pasos:**

1. Intenta hacer `POST /api/v1/auth/refresh` usando el refresh token ANTERIOR (el que ya fue usado).
   → *Debes ver:* Respuesta `401 Unauthorized`

2. Revisa el cuerpo de la respuesta.
   → *Debes ver:* Un ProblemDetails con `domainCode: "INVALID_REFRESH_TOKEN"`

**Criterio de éxito:** Status 401, domainCode `INVALID_REFRESH_TOKEN`. El sistema rechazó el token ya usado.

---

### 5.2. Control de acceso por rol

**¿Qué es esto?**
El sistema protege diferentes secciones según el rol del usuario. Las rutas públicas no requieren login. Las rutas privadas requieren cualquier usuario autenticado. Las rutas de Ops (operaciones) solo están disponibles para `AdminOps`.

**¿Quién lo usa?**
Todos los usuarios del sistema en distintas situaciones.

---

#### Prueba 2.1: Acceder a ruta pública sin token

**Objetivo:** Verificar que las rutas públicas son accesibles sin autenticación.

**Pasos:**

1. Haz `GET /health` sin ningún header de autorización.
   → *Debes ver:* Respuesta `200 OK` con datos del estado del sistema

**Criterio de éxito:** Status 200 sin necesidad de token.

---

#### Prueba 2.2: Acceder a ruta privada sin token → debe fallar

**Objetivo:** Verificar que las rutas privadas rechazan acceso sin autenticación.

**Pasos:**

1. Haz `GET /api/v1/me` sin ningún header de autorización.
   → *Debes ver:* Respuesta `401 Unauthorized`

2. Revisa el cuerpo de la respuesta.
   → *Debes ver:* Un ProblemDetails con `correlationId` y el header `X-Correlation-Id` presente

**Criterio de éxito:** Status 401, ProblemDetails con correlationId.

---

#### Prueba 2.3: Acceder a ruta privada con token de usuario normal

**Objetivo:** Verificar que un usuario normal puede acceder a rutas privadas con token válido.

**Prerrequisitos específicos:**
- Access token del usuario normal (de la Prueba 1.1)

**Pasos:**

1. Haz `GET /api/v1/me` con el header:
   ```
   Authorization: Bearer <accessToken-del-usuario-normal>
   ```
   → *Debes ver:* Respuesta `200 OK`

**Criterio de éxito:** Status 200 con token de usuario normal.

---

#### Prueba 2.4: Usuario normal no puede acceder a rutas de Ops → debe fallar con 403

**Objetivo:** Verificar que el sistema bloquea el acceso a rutas de Ops para usuarios sin el rol adecuado.

**Prerrequisitos específicos:**
- Access token del usuario normal (de la Prueba 1.1)

**Pasos:**

1. Haz `GET /api/v1/ops/ping` con el header:
   ```
   Authorization: Bearer <accessToken-del-usuario-normal>
   ```
   → *Debes ver:* Respuesta `403 Forbidden`

2. Verifica que la respuesta incluye correlationId.
   → *Debes ver:* ProblemDetails con `correlationId` y header `X-Correlation-Id`

**Criterio de éxito:** Status 403 (no 401 — el sistema SÍ reconoció al usuario pero no tiene permiso).

---

#### Prueba 2.5: AdminOps puede acceder a rutas de Ops

**Objetivo:** Verificar que un administrador puede acceder a las rutas de Ops.

**Prerrequisitos específicos:**
- Access token de AdminOps (de la Prueba 1.2)

**Pasos:**

1. Haz `GET /api/v1/ops/ping` con el header:
   ```
   Authorization: Bearer <accessToken-del-admin>
   ```
   → *Debes ver:* Respuesta `200 OK`

**Criterio de éxito:** Status 200 con token de AdminOps.

---

### 5.3. Salud del sistema (Health Check)

**¿Qué es esto?**
El sistema expone un endpoint público que reporta su estado de salud. Incluye verificaciones de la base de datos y de los pipelines de datos. Este endpoint es usado por herramientas de monitoreo para saber si el sistema está operativo.

**¿Quién lo usa?**
Administradores de sistemas, herramientas de monitoreo, equipos de operaciones.

---

#### Prueba 3.1: Health check devuelve estructura correcta

**Objetivo:** Verificar que `/health` devuelve el estado del sistema con la estructura esperada.

**Pasos:**

1. Haz `GET /health` sin autenticación.
   → *Debes ver:* Respuesta `200 OK`

2. Revisa el cuerpo JSON de la respuesta.
   → *Debes ver:*
   - Una propiedad `status` con valor `"Healthy"`, `"Degraded"` o `"Unhealthy"`
   - Una propiedad `checks` que es un arreglo con al menos dos elementos

3. Dentro de `checks`, verifica que existen al menos estos dos:
   - Un check llamado `"database"` con su propio `status` y `description`
   - Un check llamado `"pipeline-freshness"` con su propio `status` y `description`

**¿Qué debes ver al terminar?**
```json
{
  "status": "Healthy",
  "checks": [
    { "name": "database", "status": "Healthy", "description": "..." },
    { "name": "pipeline-freshness", "status": "Healthy", "description": "..." }
  ]
}
```

**Criterio de éxito:** Status 200, estructura `{ status, checks }` con los dos checks esperados.

**Casos especiales a verificar:**
- Si `status` es `"Degraded"` o `"Unhealthy"`, el endpoint IGUAL devuelve `200 OK` (no cambia el código HTTP). El estado problemático está en el cuerpo JSON, no en el código HTTP.

---

### 5.4. Formato estándar de errores

**¿Qué es esto?**
Todos los errores del sistema siguen un formato estándar llamado ProblemDetails. Esto garantiza que cualquier cliente (una app, un script, un tester) pueda interpretar errores de forma consistente.

**¿Quién lo usa?**
Automáticamente usado por el sistema en todas las respuestas de error. No es una función de usuario final.

---

#### Prueba 4.1: Error de ruta no encontrada incluye correlationId

**Objetivo:** Verificar que rutas inexistentes devuelven ProblemDetails con correlationId.

**Pasos:**

1. Haz `GET /api/v1/ruta-que-no-existe` sin autenticación.
   → *Debes ver:* Respuesta `404 Not Found`

2. Revisa el cuerpo JSON.
   → *Debes ver:* Un objeto con al menos `status: 404`, `domainCode` (puede ser `null`) y `correlationId` (una cadena de texto no vacía)

3. Revisa los headers de la respuesta.
   → *Debes ver:* El header `X-Correlation-Id` con el mismo valor que `correlationId` en el cuerpo

**Criterio de éxito:** Status 404, cuerpo con `correlationId`, header `X-Correlation-Id` presente.

---

### 5.5. Dashboard de Hangfire

**¿Qué es esto?**
Hangfire es el sistema de jobs en background de FIBRADIS. Tiene un dashboard web que muestra los jobs que se han ejecutado, su estado y cualquier error. En ambiente de desarrollo, el dashboard es público. En producción, solo admins.

---

#### Prueba 5.1: Dashboard de Hangfire accesible en Development

**Objetivo:** Verificar que el dashboard existe y carga correctamente en ambiente de desarrollo.

**Pasos:**

1. Abre un navegador y navega a `http://localhost:5265/hangfire` (o la URL del servidor de desarrollo).
   → *Debes ver:* La interfaz web del dashboard de Hangfire cargando correctamente

2. Verifica que la página muestra secciones como "Dashboard", "Jobs", "Recurring" o similares.
   → *Debes ver:* Un dashboard funcional, no una página de error ni una pantalla en blanco

**Criterio de éxito:** El dashboard de Hangfire carga sin errores en el navegador.

**Casos especiales a verificar:**
- Este comportamiento es solo para Development. En un ambiente de producción, el acceso sin token de AdminOps debe devolver 401 o redirigir al login.

---

## 6. Pruebas negativas

Estas pruebas verifican que el sistema rechaza correctamente acciones inválidas. En todos los casos el sistema **no debe fallar silenciosamente** — debe mostrar una respuesta de error clara y estructurada.

---

#### Prueba N1: Login con contraseña incorrecta

**Qué intenta hacer el tester:** Hacer login con un email válido pero contraseña incorrecta.

**Petición:** `POST /api/v1/auth/login` con `{ "email": "user@fibradis.dev", "password": "ContraseñaIncorrecta123" }`

**Qué debe hacer el sistema:**
- Devolver `401 Unauthorized`
- Cuerpo con `domainCode: "INVALID_CREDENTIALS"`
- No revelar si el email existe o no (mismo mensaje para email inexistente)

**Criterio de éxito:** Status 401, domainCode `INVALID_CREDENTIALS`.

---

#### Prueba N2: Login con email que no existe

**Qué intenta hacer el tester:** Hacer login con un email que no está registrado.

**Petición:** `POST /api/v1/auth/login` con `{ "email": "noexiste@ejemplo.com", "password": "cualquier" }`

**Qué debe hacer el sistema:**
- Devolver `401 Unauthorized`
- El mensaje de error debe ser idéntico al de contraseña incorrecta (no revelar si el email existe)

**Criterio de éxito:** Status 401, misma respuesta que N1.

---

#### Prueba N3: Usar refresh token inválido (no existe en el sistema)

**Qué intenta hacer el tester:** Enviar un refresh token inventado o modificado.

**Petición:** `POST /api/v1/auth/refresh` con cookie `refreshToken=esto-es-un-token-falso`

**Qué debe hacer el sistema:**
- Devolver `401 Unauthorized`
- Cuerpo con `domainCode: "INVALID_REFRESH_TOKEN"`

**Criterio de éxito:** Status 401, domainCode `INVALID_REFRESH_TOKEN`.

---

#### Prueba N4: Access token expirado

**Qué intenta hacer el tester:** Usar un access token que ya no es válido (expirado o manipulado).

**Petición:** `GET /api/v1/me` con `Authorization: Bearer esto-no-es-un-jwt-valido`

**Qué debe hacer el sistema:**
- Devolver `401 Unauthorized`
- No dar detalles sobre por qué el token es inválido

**Criterio de éxito:** Status 401.

---

## 7. Checklist de aceptación

Antes de firmar que la prueba fue exitosa, verifica que todos los puntos siguientes están cubiertos:

### Autenticación
- [ ] Login con credenciales válidas de `User` devuelve 200 + `accessToken` + cookie `refreshToken`
- [ ] Login con credenciales válidas de `AdminOps` devuelve 200 + `accessToken`
- [ ] Login con credenciales inválidas devuelve 401 con `domainCode: INVALID_CREDENTIALS`
- [ ] Refresh de token devuelve nuevo `accessToken` y nueva cookie `refreshToken`
- [ ] Refresh token usado una vez queda invalidado (segundo uso devuelve 401)

### Control de acceso
- [ ] `GET /health` devuelve 200 sin autenticación
- [ ] `GET /api/v1/me` sin token devuelve 401
- [ ] `GET /api/v1/me` con token de `User` devuelve 200
- [ ] `GET /api/v1/ops/ping` con token de `User` devuelve **403** (no 401)
- [ ] `GET /api/v1/ops/ping` con token de `AdminOps` devuelve 200

### Salud del sistema
- [ ] `GET /health` devuelve estructura JSON con `status` y array `checks`
- [ ] El array `checks` incluye al menos `"database"` y `"pipeline-freshness"`
- [ ] El status HTTP de `/health` es siempre 200 (el estado problemático está en el JSON)

### Formato de errores
- [ ] Todas las respuestas de error incluyen `correlationId` en el cuerpo
- [ ] Todas las respuestas de error incluyen el header `X-Correlation-Id`
- [ ] El valor del header y del cuerpo son iguales

### Hangfire (Development)
- [ ] El dashboard `/hangfire` carga sin errores en el navegador en ambiente Development

---

## 8. Registro de resultados

| # Prueba | Nombre | Resultado | Observaciones | Fecha |
|----------|--------|-----------|---------------|-------|
| 1.1 | Login exitoso - User | ⬜ Pendiente | | |
| 1.2 | Login exitoso - AdminOps | ⬜ Pendiente | | |
| 1.3 | Refresh de token | ⬜ Pendiente | | |
| 1.4 | Refresh token de un solo uso | ⬜ Pendiente | | |
| 2.1 | Ruta pública sin token | ⬜ Pendiente | | |
| 2.2 | Ruta privada sin token → 401 | ⬜ Pendiente | | |
| 2.3 | Ruta privada con User → 200 | ⬜ Pendiente | | |
| 2.4 | Ruta Ops con User → 403 | ⬜ Pendiente | | |
| 2.5 | Ruta Ops con AdminOps → 200 | ⬜ Pendiente | | |
| 3.1 | Health check - estructura | ⬜ Pendiente | | |
| 4.1 | Error 404 con correlationId | ⬜ Pendiente | | |
| 5.1 | Dashboard Hangfire | ⬜ Pendiente | | |
| N1 | Login - contraseña incorrecta | ⬜ Pendiente | | |
| N2 | Login - email inexistente | ⬜ Pendiente | | |
| N3 | Refresh token inválido | ⬜ Pendiente | | |
| N4 | Access token expirado/inválido | ⬜ Pendiente | | |

**Hallazgos encontrados:**

| # | Descripción | Prueba | Severidad | Estado |
|---|-------------|--------|-----------|--------|
| 1 | | | | |

**Severidad:**
- **Crítica** — El sistema no funciona o expone datos que no debería
- **Alta** — Una funcionalidad de seguridad no opera como se espera
- **Media** — Comportamiento incorrecto pero existe alternativa o workaround
- **Baja** — Problema de formato o detalle menor

---

## 9. Notas finales

Esta guía cubre la infraestructura de seguridad de FIBRADIS. Aunque no hay pantallas visibles para el usuario final, es la base sobre la que descansa toda la seguridad del sistema.

**Importante sobre el ambiente de pruebas:** Asegúrate de probar en el ambiente correcto. El comportamiento de Hangfire y la validación del JWT secret son diferentes entre Development y Production.

**Si algo no funciona:** Anota el `correlationId` de la respuesta de error y compártelo con el equipo de desarrollo — es la forma más rápida de rastrear el problema en los logs.

---

*Documento generado por bmad-testing-guide el 2026-05-18.*
*Para preguntas sobre el alcance o los criterios, contactar al equipo de desarrollo.*
