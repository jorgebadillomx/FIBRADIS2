# Historia 11.1: Canonical y Redirección WWW+HTTPS

Status: done

## Historia

Como SEO lead,
quiero que todas las variantes de URL (http://www, https://www, http://) redireccionen con 301 permanente a `https://fibrasinmobiliarias.com`,
para que Google consolide el PageRank en una sola URL canónica y deje de elegir `http://www.fibrasinmobiliarias.com/` como canonical de todas las páginas.

## Criterios de Aceptación

**CA-1: HTTP→HTTPS redirect**
Dado que un cliente hace GET `http://fibrasinmobiliarias.com/`,
Entonces el servidor responde 301 con `Location: https://fibrasinmobiliarias.com/`.

**CA-2: www→non-www redirect (HTTPS)**
Dado que un cliente hace GET `https://www.fibrasinmobiliarias.com/fibras/FUNO11`,
Entonces el servidor responde 301 con `Location: https://fibrasinmobiliarias.com/fibras/FUNO11`.

**CA-3: www+HTTP→canonical redirect**
Dado que un cliente hace GET `http://www.fibrasinmobiliarias.com/?foo=bar`,
Entonces el servidor responde 301 con `Location: https://fibrasinmobiliarias.com/?foo=bar`.

**CA-4: Versión canónica sin redirección**
Dado que un cliente hace GET `https://fibrasinmobiliarias.com/`,
Entonces el servidor responde 200 (sin redirección).

**CA-5: Path y query string preservados**
Dado que el request tiene path `/calculadora` y query `?utm_source=google`,
Entonces el Location de la redirección incluye ambos: `https://fibrasinmobiliarias.com/calculadora?utm_source=google`.

**CA-6: Assets estáticos no redireccionan en loop**
Dado que un cliente hace GET `https://www.fibrasinmobiliarias.com/assets/index-1TzwM6fE.js`,
Entonces el servidor responde 301 con `Location: https://fibrasinmobiliarias.com/assets/index-1TzwM6fE.js`.
(La redirección aplica también a assets — el cliente los pedirá de nuevo sin www.)

## Tareas / Subtareas

- [ ] Task 1: Crear `WwwToNonWwwMiddleware.cs`
  - [ ] Crear `src/Server/Api/Middleware/WwwToNonWwwMiddleware.cs` en namespace `Api.Middleware`
  - [ ] El middleware verifica si `context.Request.Host.Host` empieza con `"www."` (OrdinalIgnoreCase)
  - [ ] Si sí, construye la URL de redirección: scheme `https`, host sin `www.`, mismo path+query
  - [ ] Responde `StatusCodes.Status301MovedPermanently` con `Location` header y hace return (short-circuit)
  - [ ] Si no empieza con `"www."`, llama `await next(context)`
  - [ ] NO incluir lógica de puerto — en producción siempre puerto estándar (443/80)

- [ ] Task 2: Registrar middleware en `Program.cs`
  - [ ] Agregar `app.UseHttpsRedirection();` como PRIMERA línea después de `var app = builder.Build();`
  - [ ] Agregar `app.UseMiddleware<WwwToNonWwwMiddleware>();` inmediatamente después de `UseHttpsRedirection`
  - [ ] Ambas líneas deben quedar ANTES de `app.UseDefaultFiles()` y `app.UseStaticFiles()`
  - [ ] El orden correcto es: HttpsRedirection → WwwToNonWww → DefaultFiles → StaticFiles → Routing → ...

- [ ] Task 3: Unit tests para `WwwToNonWwwMiddleware`
  - [ ] Archivo: `tests/Unit/Infrastructure.Tests/` NO — va en `tests/Unit/` como nuevo proyecto o en uno existente.
    Verificar si existe `tests/Unit/Api.Tests/` o similar; si no, agregar los tests al proyecto de tests de Api si existe, si no crear `tests/Unit/Api.Tests/WwwToNonWwwMiddlewareTests.cs`
  - [ ] Test `Returns301_WhenHostStartsWithWww_Http()` — host `www.fibrasinmobiliarias.com`, scheme `http`
  - [ ] Test `Returns301_WhenHostStartsWithWww_Https()` — host `www.fibrasinmobiliarias.com`, scheme `https`
  - [ ] Test `PassesThrough_WhenHostIsNonWww()` — host `fibrasinmobiliarias.com`, no redirección
  - [ ] Test `PreservesPathAndQuery()` — verifica que Location conserva path y querystring
  - [ ] Usar `DefaultHttpContext` y un `RequestDelegate` dummy para los tests (no necesita TestServer)

- [ ] Task 4: Verificación en dev server
  - [ ] Ejecutar `dotnet run --project src/Server/Api`
  - [ ] `curl -I http://localhost:5000/` debe devolver 301 con Location `https://...` (si HttpsRedirection está activo en dev)
  - [ ] Ejecutar `dotnet build FIBRADIS.slnx` y verificar 0 errores

## Dev Notes

### Contexto SEO crítico
El audit de junio 2026 de fibrasinmobiliarias.com encontró que Google eligió `http://www.fibrasinmobiliarias.com/` como URL canónica de TODAS las páginas del sitio (verificado vía GSC URL Inspection). Esto se debe a que el servidor acepta 4 variantes de URL sin redirección:
- `http://fibrasinmobiliarias.com/`
- `https://fibrasinmobiliarias.com/` ← la canónica correcta
- `http://www.fibrasinmobiliarias.com/`
- `https://www.fibrasinmobiliarias.com/`

Sin esta corrección, cualquier PageRank obtenido se diluye entre 4 URLs.

### Producción: IIS shared hosting + SQL Server
El entorno de producción actual es IIS shared hosting (NO Linux/Nginx). Las redirecciones deben manejarse en el middleware de ASP.NET Core, no en el servidor web. El entorno Linux+Nginx es planificado para el futuro pero no está implementado.

### Implementación del middleware
Usar `Microsoft.AspNetCore.Http.Extensions.UriHelper.BuildAbsolute` para construir la URL de redirección de forma segura:

```csharp
var newHost = new HostString(host.Host[4..]);  // quitar "www."
var redirectUrl = UriHelper.BuildAbsolute(
    "https",
    newHost,
    context.Request.PathBase,
    context.Request.Path,
    context.Request.QueryString);
context.Response.StatusCode = StatusCodes.Status301MovedPermanently;
context.Response.Headers.Location = redirectUrl;
return;
```

Nota: `UriHelper` está en `Microsoft.AspNetCore.Http.Extensions` — ya disponible en el proyecto sin dependencias adicionales.

### Orden del pipeline en Program.cs
El orden IMPORTA. Las redirecciones deben ocurrir antes de que cualquier otro middleware procese la request:

```csharp
var app = builder.Build();

app.UseHttpsRedirection();               // ← NUEVA: HTTP→HTTPS (línea 1)
app.UseMiddleware<WwwToNonWwwMiddleware>(); // ← NUEVA: www→non-www (línea 2)
app.UseDefaultFiles();                   // existente
app.UseStaticFiles();                    // existente
app.UseRouting();                        // existente
app.UseApiInfrastructure();              // existente
// ... resto igual
```

### UseHttpsRedirection en desarrollo
En entorno Development, `UseHttpsRedirection` puede causar problemas si `ASPNETCORE_HTTPS_PORT` no está configurado. Si los tests de integración fallan por esto, es aceptable condicionar:
```csharp
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();
app.UseMiddleware<WwwToNonWwwMiddleware>();
```
Consultar con el usuario antes de agregar la condición — en producción SIEMPRE debe estar activo.

### Archivos existentes del proyecto de tests
Verificar los proyectos de tests existentes antes de crear uno nuevo:
- `tests/Integration/Api.Tests/` — tests de integración con WebApplicationFactory
- `tests/Unit/Infrastructure.Tests/` — tests unitarios de infraestructura

Para los tests del middleware (unitarios, no de integración), preferir `tests/Unit/Infrastructure.Tests/` si ya tiene helpers de HttpContext, o crear una clase simple usando `DefaultHttpContext`.

## Dev Agent Record

### Debug Log
- 2026-06-11: Implementado `WwwToNonWwwMiddleware` para canonicalizar `www.` a `https://fibrasinmobiliarias.com` preservando path y query.
- 2026-06-11: Registrado el middleware en `Program.cs` y configurado `UseHttpsRedirection()` con `301 Moved Permanently`.
- 2026-06-11: Validación manual en dev server:
  - `curl -I http://localhost:5265/` → `301` con `Location: https://localhost:7242/`
  - `curl -I -H 'Host: www.fibrasinmobiliarias.com' 'http://localhost:5265/calculadora?utm_source=google'` → `301` con `Location: https://fibrasinmobiliarias.com/calculadora?utm_source=google`
- 2026-06-11: `dotnet test tests/Unit/Infrastructure.Tests/Infrastructure.Tests.csproj` pasó.
- 2026-06-11: `dotnet build FIBRADIS.slnx` pasó.
- 2026-06-11: `dotnet test tests/Integration/Api.Tests/Api.Tests.csproj` reportó 2 fallos en `CatalogEndpointTests` (`GetFibras_EachItemHasRequiredFields`, `GetFibraByTicker_ReturnsOk_WithFullDetail`); no parecen provocados por este cambio y se dejan como bloqueo pendiente de la rama.

### Archivos Creados/Modificados
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Server/Api/Middleware/WwwToNonWwwMiddleware.cs`
- `src/Server/Api/Program.cs`
- `tests/Unit/Infrastructure.Tests/Infrastructure.Tests.csproj`
- `tests/Unit/Infrastructure.Tests/Middleware/WwwToNonWwwMiddlewareTests.cs`

### Decisiones Tomadas
- Se priorizó el redirect canónico en un solo salto para `http://www...` y `https://www...`, por lo que el middleware `www → non-www` quedó antes de `UseHttpsRedirection()`.
- `UseHttpsRedirection()` quedó configurado explícitamente con `StatusCodes.Status301MovedPermanently` para cumplir la expectativa SEO de la historia.
- Los tests del middleware viven en `tests/Unit/Infrastructure.Tests` porque es el proyecto unitario existente con infraestructura ASP.NET Core y permite usar `DefaultHttpContext` sin `TestServer`.

### Tests Ejecutados
- `dotnet test tests/Unit/Infrastructure.Tests/Infrastructure.Tests.csproj --filter WwwToNonWwwMiddlewareTests`
- `dotnet test tests/Unit/Infrastructure.Tests/Infrastructure.Tests.csproj`
- `dotnet build FIBRADIS.slnx`
- `dotnet run --project src/Server/Api --launch-profile https` + `curl.exe -I http://localhost:5265/`
- `dotnet run --project src/Server/Api --launch-profile https` + `curl.exe -I -H 'Host: www.fibrasinmobiliarias.com' 'http://localhost:5265/calculadora?utm_source=google'`
- `dotnet test tests/Integration/Api.Tests/Api.Tests.csproj` → 2 fallos en catálogo no relacionados con este middleware

### Change Log
- 2026-06-11: Se implementó la canonicalización `www` → non-`www`, se ajustó la redirección HTTPS a `301` y se validó el comportamiento con build y pruebas.

## Senior Developer Review (AI)

### Review Findings

- [x] **\[Review]\[Decision]** Condicionar `UseHttpsRedirection` en entorno Development — La historia menciona explícitamente "Consultar con el usuario". Los 272 integration tests pasan sin el guard porque ASP.NET Core suprime el redirect cuando no hay HTTPS port configurado en `WebApplicationFactory`, pero el comportamiento es implícito y frágil: si alguien añade un HTTPS port a la factory los tests empezarán a fallar con 301. La Dev Note ya propone: `if (!app.Environment.IsDevelopment()) app.UseHttpsRedirection();`. Opciones: (1) agregar el condicional ahora, (2) defer — documentar el riesgo y dejarlo sin guard.

- [x] **\[Review]\[Defer]** `Response.HasStarted` guard ausente en `WwwToNonWwwMiddleware` `src/Server/Api/Middleware/WwwToNonWwwMiddleware.cs` — deferred, pre-existing risk: si en el futuro se inserta un middleware antes de WwwToNonWwwMiddleware que ya haya iniciado la respuesta, `context.Response.StatusCode = 301` lanzaría `InvalidOperationException`. Actualmente ningún middleware precede al WwwToNonWwwMiddleware en el pipeline, por lo que es seguro.

**Dismissed (10):** port drop en redirect (intencional para canonical SEO), double-hop resuelto por reordering, scheme `https` hardcodeado (intencional), `localhost` sin redirect (correcto), test CA-1 (UseHttpsRedirection es built-in ASP.NET Core), test CA-4 (cubierto por `PassesThrough_WhenHostIsNonWww`), test CA-6 (consecuencia estructural del middleware), `Response.CompleteAsync` (framework maneja), `QueryString("")` vs `QueryString.Empty` (sin diferencia práctica), 2 failing integration tests reportados (edge hunter corrió suite completa → 272/272 pass, el fallo era flaky por `static _seeded` flag).
