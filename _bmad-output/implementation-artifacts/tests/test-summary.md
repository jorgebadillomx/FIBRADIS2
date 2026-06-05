# Test Automation Summary — Épicas 6-7 y 6-1→6-4

**Fecha:** 2026-06-05
**Tests generados esta sesión:** 45 nuevos (22 OpsUser + 23 Portfolio)
**Estado general:** 245/250 passing, 5 pre-existentes fuera de scope

---

## Tests Generados

### API Tests — 6-7 Gestión Usuarios Ops

- [x] `tests/Integration/Api.Tests/Ops/OpsUserEndpointTests.cs` — 22 tests

| Endpoint | Tests |
|----------|-------|
| GET /api/v1/ops/users | 200+lista, 401, 403 |
| POST /api/v1/ops/users | Roles User/AdminOps, campos pago, pwd débil, rol inválido, email duplicado |
| PATCH /api/v1/ops/users/{id}/active | disable, enable, 404, 401 |
| PATCH /api/v1/ops/users/{id}/password | pwd fuerte, débil (422), 404, nueva pwd funciona en login |
| PATCH /api/v1/ops/users/{id}/payment | valores, null, 404 |
| POST /api/v1/auth/login | cuenta deshabilitada → ACCOUNT_DISABLED, idem con pwd incorrecto |

**Resultado: 22/22 ✅**

### API Tests — 6-1→6-4 Portafolio

- [x] `tests/Integration/Api.Tests/PortfolioEndpointTests.cs` — 23 tests

| Endpoint | Tests |
|----------|-------|
| GET /api/v1/portfolio/status | sin portafolio (false), 401 |
| POST /api/v1/portfolio/upload | CSV válido, status refleja portafolio, ticker inválido (400), solo headers (400), 401, merge mode |
| GET /api/v1/portfolio/ | vacío (null KPIs), con posiciones (KPIs+posiciones correctas), 401 |
| PATCH /api/v1/portfolio/positions/{id} | 204 válido, refleja valores, 0 títulos (400), fibra inexistente (404), 401 |
| DELETE /api/v1/portfolio/positions/{id} | 204 existente, portafolio queda vacío, 404, 401 |
| GET/PUT /api/v1/portfolio/column-config | vacío por default, persiste (204), columnas inválidas filtradas |

**Resultado: 23/23 ✅**

---

## Fixes Aplicados Durante Generación

### 1. `ApiWebFactory.SeedUsersAsync` — Encriptación de emails (regresión 6-7)
El seed almacenaba emails en texto plano pero `AuthService.LoginAsync` busca por email encriptado. Se resolvió usando `IEmailEncryptor` del contenedor DI para encriptar antes de guardar.

### 2. `PortfolioRepository.UpsertSettingsAsync` — InMemory no soporta `ExecuteUpdateAsync`
EF Core InMemory no implementa `ExecuteUpdateAsync` (lanza `InvalidOperationException`). Se reemplazó por `FirstOrDefaultAsync` + change tracking, que funciona con todos los providers y mantiene la misma semántica.

### 3. `PortfolioEndpointTests` — Aislamiento por test
Se cambió de `IClassFixture<ApiWebFactory>` (BD compartida entre tests) a `new ApiWebFactory()` por instancia de test, garantizando BD InMemory completamente aislada por test. Cada test crea usuario con email UUID único → sin contaminación de estado de portafolio.

---

## Cobertura

- Endpoints cubiertos: 11/11 rutas de las historias 6-1 a 6-7
- ACs verificados: todos los ACs con comportamiento de API observable
- Aislamiento: cada test crea usuario con email UUID único

## Estado Suite Completa

| Grupo | Passing | Failing |
|-------|---------|---------|
| Auth | 6 | 0 |
| OpsUser (nuevo) | 22 | 0 |
| Portfolio (nuevo) | 23 | 0 |
| Catalog | ~10 | 0 |
| Market | ~30 | 0 |
| Fundamentals | ~90 | 1* |
| Dashboard | ~40 | 4* |
| Otros | ~24 | 0 |
| **Total** | **245** | **5*** |

*Pre-existentes antes de esta sesión — no introducidos por estos cambios.

---

## Tests Anteriores (Épicas 2-4)

Los tests de épicas anteriores siguen pasando sin regresiones.
Ver historial: `_bmad-output/implementation-artifacts/tests/` (versiones anteriores en git).
