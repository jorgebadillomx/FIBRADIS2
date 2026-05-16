# Historia 1.2: Backend API v1 con OpenAPI y cliente tipado para los SPAs

**Epic:** 1 — Fundación, Infraestructura y Acceso
**Story ID:** 1.2
**Story Key:** `1-2-backend-api-v1-con-openapi-y-cliente-tipado-para-los-spas`
**Status:** done
**Date:** 2026-05-15

---

## Historia de Usuario

Como desarrollador,
quiero el baseline del ASP.NET Core API configurado con rutas REST JSON bajo `/api/v1`, documento OpenAPI generado al arranque, contrato de error ProblemDetails con `domainCode` y `correlationId`, y un SharedApiClient TypeScript generado automáticamente para ambos SPAs,
para que el equipo pueda construir endpoints y consumirlos con tipos seguros sin ambigüedad de contrato.

---

## Criterios de Aceptación

**CA-1: Endpoint de salud funcional**
Dado que el backend está en ejecución,
Cuando hago `GET /api/v1/health`,
Entonces recibo `200 OK` con cuerpo `{"status":"healthy"}` y `Content-Type: application/json`.

**CA-2: Especificación OpenAPI disponible**
Dado que el backend está en ejecución en Development,
Cuando navego a `/openapi/v1.json`,
Entonces recibo el documento OpenAPI 3.x completo que incluye el endpoint `/api/v1/health`.
Cuando navego a `/swagger`,
Entonces la UI Scalar carga con el spec importado.

**CA-3: SharedApiClient tipado importable desde ambas SPAs**
Dado que el spec OpenAPI existe y el script `npm run codegen:api` se ejecutó,
Cuando hago `import type { paths } from '@fibradis/shared-api-client'` en cualquier archivo TypeScript de Main o Ops,
Entonces `tsc --noEmit` compila sin errores y el tipo `paths` refleja los endpoints del backend.

**CA-4: Contrato de error ProblemDetails extendido**
Dado que el backend retorna cualquier error (4xx o 5xx),
Cuando el cliente recibe la respuesta,
Entonces el cuerpo JSON cumple ProblemDetails estándar (`type`, `title`, `status`) más `domainCode` (string o null) y `correlationId` (string),
Y el header de respuesta `X-Correlation-Id` está presente.

---

## Tareas / Subtareas

- [x] Task 1: Agregar paquetes NuGet y refactorizar Program.cs (AC: CA-1, CA-2, CA-4)
  - [x] Agregar `Scalar.AspNetCore` (~2.3.x) y `Microsoft.Extensions.ApiDescription.Server` (10.0.8) en `Directory.Packages.props` y `Api.csproj`; seguir CPM — sin versiones inline en csproj
  - [x] Crear `src/Server/Api/Middleware/CorrelationIdMiddleware.cs` (lee X-Correlation-Id del request o genera GUID; escribe en HttpContext.Items["CorrelationId"] y en header de respuesta)
  - [x] Crear `src/Server/Api/CompositionRoot/ApiServiceExtensions.cs` con `AddApiInfrastructure(this WebApplicationBuilder builder)` que encapsule: AddOpenApi("v1"), AddProblemDetails+CustomizeProblemDetails, AddExceptionHandler<GlobalExceptionHandler>, AddCors SpaDev (solo en Development)
  - [x] Crear `src/Server/Api/CompositionRoot/UseApiInfrastructureExtensions.cs` con `UseApiInfrastructure(this WebApplication app)` que active: UseCorrelationId (custom middleware), UseExceptionHandler, UseCors SpaDev (dev), y en dev MapOpenApi() + MapScalarApiReference
  - [x] Refactorizar `src/Server/Api/Program.cs` para usar los extension methods; mantener el `AddDbContext` existente; el pipeline debe quedar limpio y ordenado

- [x] Task 2: Implementar endpoint de salud (AC: CA-1)
  - [x] Crear `src/Server/Api/Endpoints/Public/HealthEndpoint.cs` con método `MapHealth(this IEndpointRouteBuilder app)` que registre `GET /api/v1/health` via `MapGroup("/api/v1").MapGet("/health", ...)`; el handler retorna `Results.Ok(new { status = "healthy" })` con tag "Health" y Produces<object>(200)
  - [x] Registrar con `app.MapHealth()` en Program.cs
  - [x] Agregar `public partial class Program { }` al final de Program.cs (necesario para WebApplicationFactory en tests)

- [x] Task 3: Configurar contrato de error ProblemDetails (AC: CA-4)
  - [x] Crear `src/Server/Domain/Common/DomainException.cs`: clase abstracta con `string DomainCode` (base para excepciones de dominio futuras)
  - [x] Crear `src/Server/Api/CompositionRoot/GlobalExceptionHandler.cs` implementando `IExceptionHandler`; manejar solo `DomainException` → 422 con `domainCode` en ProblemDetails; retornar `false` para cualquier otra excepción (el pipeline estándar la maneja)
  - [x] Configurar `CustomizeProblemDetails` en `AddApiInfrastructure`: agregar `correlationId` de `HttpContext.Items["CorrelationId"]` y `domainCode = null` (si no fue establecido por el handler) a `ProblemDetails.Extensions` en todas las respuestas de error

- [x] Task 4: Configurar workspace npm SharedApiClient (AC: CA-3)
  - [x] Crear `src/Web/SharedApiClient/package.json` (ver spec en Dev Notes)
  - [x] Actualizar `package.json` raíz: agregar `"src/Web/SharedApiClient"` al array `workspaces` (quedan tres: Main, Ops, SharedApiClient); agregar script `"codegen:api": "pwsh -File scripts/codegen/generate-api-client.ps1"`; agregar `"openapi-typescript": "^7.0.0"` a devDependencies raíz
  - [x] Crear `scripts/codegen/generate-api-client.ps1` (ver spec en Dev Notes)
  - [x] Configurar `Api.csproj` y `Directory.Packages.props` para que `dotnet build` genere `scripts/codegen/Api.json` automáticamente via `Microsoft.Extensions.ApiDescription.Server` (nota: el tool genera el archivo con el nombre del ensamblado: Api.json)

- [x] Task 5: Generar schema TypeScript inicial (AC: CA-3)
  - [x] Ejecutar `dotnet build FIBRADIS.slnx` para generar `scripts/codegen/Api.json`; verificar que el archivo existe y contiene el spec del health endpoint
  - [x] Ejecutar `npm install` desde raíz para registrar el nuevo workspace `@fibradis/shared-api-client`
  - [x] Ejecutar `npm run codegen:api` para generar `src/Web/SharedApiClient/schema.d.ts`
  - [x] Crear `src/Web/SharedApiClient/index.ts` que re-exporta los tipos (ver spec en Dev Notes)

- [x] Task 6: Conectar SPAs con SharedApiClient y configurar proxy de desarrollo (AC: CA-3)
  - [x] Agregar `"@fibradis/shared-api-client": "*"` y `"openapi-fetch": "^0.14.0"` a dependencies de `src/Web/Main/package.json` y `src/Web/Ops/package.json`
  - [x] Agregar proxy `/api` y `/openapi` en `src/Web/Main/vite.config.ts` y `src/Web/Ops/vite.config.ts` apuntando a `http://localhost:5265`
  - [x] Ejecutar `npm install` desde raíz para resolver todas las dependencias del workspace
  - [x] Verificar que `npm run build:main` y `npm run build:ops` compilan sin errores TypeScript

- [x] Task 7: Tests de integración (AC: CA-1, CA-2, CA-4)
  - [x] Agregar `Microsoft.AspNetCore.Mvc.Testing` (10.0.8) a `Directory.Packages.props` y a `tests/Integration/Api.Tests/Api.Tests.csproj`
  - [x] Crear `tests/Integration/Api.Tests/ApiWebFactory.cs` con `WebApplicationFactory<Program>` que use entorno Development
  - [x] Test `HealthEndpointTests`: `GET /api/v1/health` → 200, body `{"status":"healthy"}`
  - [x] Test `OpenApiEndpointTests`: `GET /openapi/v1.json` → 200, body contiene `"openapi"` y `"/api/v1/health"`
  - [x] Test `ProblemDetailsTests`: `GET /ruta-que-no-existe` → 404, body ProblemDetails contiene key `correlationId` en extensions y header `X-Correlation-Id` presente
  - [x] `dotnet test tests/Integration/Api.Tests/` — todos pasan (4/4)

---

## Notas Técnicas para el Agente Dev

### Stack exacto — NO negociar versiones

| Componente | Versión requerida |
|---|---|
| .NET | 10 LTS (fijado en global.json con rollForward: latestMinor) |
| EF Core | 10.0.8 (ya en Directory.Packages.props — no cambiar) |
| Scalar.AspNetCore | ~2.3.x — verificar NuGet para latest estable compatible con .NET 10 |
| Microsoft.Extensions.ApiDescription.Server | 10.0.8 |
| Microsoft.AspNetCore.Mvc.Testing | 10.0.8 |
| openapi-typescript | ^7.0.0 — si hay v8+ en npm al momento de impl, verificar si el API de generación es compatible |
| openapi-fetch | ^0.14.0 — del mismo autor/repo que openapi-typescript |
| React / Vite | Sin cambios: React 19.2, Vite 7.3.3 (ya instalados, NO actualizar) |
| TypeScript | Sin cambios: ~6.0.2 (ya instalado) |

### Solución .slnx (aprendido en historia 1.1)

.NET 10 genera la solución como `FIBRADIS.slnx` (no `.sln`). Todos los comandos deben usar `FIBRADIS.slnx`:
```powershell
dotnet build FIBRADIS.slnx
dotnet test FIBRADIS.slnx
```

### Central Package Management (CPM) — regla obligatoria

**NUNCA** agregar versiones en los `<PackageReference>` de los `.csproj`. Solo declarar en `Directory.Packages.props`:
```xml
<!-- Directory.Packages.props — agregar aquí -->
<PackageVersion Include="Scalar.AspNetCore" Version="2.3.4" />
<PackageVersion Include="Microsoft.Extensions.ApiDescription.Server" Version="10.0.8" />
<PackageVersion Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.8" />
```
```xml
<!-- Api.csproj — solo el Include, sin Version -->
<PackageReference Include="Scalar.AspNetCore" />
<PackageReference Include="Microsoft.Extensions.ApiDescription.Server">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
```

### Estado actual de Program.cs que se va a modificar

```csharp
// src/Server/Api/Program.cs — ESTADO ACTUAL al inicio de la historia
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.Run();
```
Debe quedar como un archivo limpio que usa extension methods. El `AddOpenApi()` ya existente se reemplaza por `AddOpenApi("v1")` dentro del extension method. El `MapOpenApi()` ya provee `/openapi/v1.json` — solo hay que agregar Scalar para `/swagger`.

### Patrón de organización de endpoints (Minimal API)

Cada grupo de endpoints usa un static class con extension method. Este es el patrón normativo para todo el proyecto:
```csharp
// src/Server/Api/Endpoints/Public/HealthEndpoint.cs
public static class HealthEndpoint
{
    public static IEndpointRouteBuilder MapHealth(this IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/v1")
           .MapGet("/health", () => Results.Ok(new { status = "healthy" }))
           .WithName("GetHealth")
           .WithTags("Health")
           .Produces(StatusCodes.Status200OK);
        return app;
    }
}
```
En `Program.cs`: `app.MapHealth();`

### CorrelationIdMiddleware — implementación requerida

```csharp
// src/Server/Api/Middleware/CorrelationIdMiddleware.cs
public class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault()
                            ?? Guid.NewGuid().ToString("N");
        context.Items["CorrelationId"] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;
        await next(context);
    }
}
```
Registrar en `UseApiInfrastructure` como: `app.UseMiddleware<CorrelationIdMiddleware>();`
**Posición en pipeline**: ANTES de `UseExceptionHandler` para que los errores también tengan el header.

### ProblemDetails — contrato de respuesta exacto

Todos los errores deben retornar este shape. La `CustomizeProblemDetails` lo garantiza automáticamente:
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Not Found",
  "status": 404,
  "domainCode": null,
  "correlationId": "a1b2c3d4e5f6789abc"
}
```
Para `DomainException` (status 422):
```json
{
  "type": "https://fibradis.com/errors/fibra-not-found",
  "title": "FIBRA no encontrada",
  "status": 422,
  "domainCode": "FIBRA_NOT_FOUND",
  "correlationId": "a1b2c3d4e5f6789abc"
}
```

Implementación de `CustomizeProblemDetails`:
```csharp
options.CustomizeProblemDetails = ctx =>
{
    ctx.ProblemDetails.Extensions["correlationId"] =
        ctx.HttpContext.Items["CorrelationId"]?.ToString()
        ?? ctx.HttpContext.TraceIdentifier;

    if (!ctx.ProblemDetails.Extensions.ContainsKey("domainCode"))
        ctx.ProblemDetails.Extensions["domainCode"] = null;
};
```

### DomainException — clase base en Domain

```csharp
// src/Server/Domain/Common/DomainException.cs
namespace Domain.Common;

public abstract class DomainException(string message, string domainCode) : Exception(message)
{
    public string DomainCode { get; } = domainCode;
}
```
Clase abstracta en Domain. Las excepciones concretas se crearán en historias futuras cuando haya lógica de negocio real.

### GlobalExceptionHandler — en Api layer (no en Domain ni Application)

```csharp
// src/Server/Api/CompositionRoot/GlobalExceptionHandler.cs
using Domain.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Api.CompositionRoot;

public class GlobalExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        if (exception is not DomainException domainEx)
            return false; // el pipeline estándar maneja 500 y otros

        httpContext.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;

        await problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails =
            {
                Status = StatusCodes.Status422UnprocessableEntity,
                Title = domainEx.Message,
                Extensions = { ["domainCode"] = domainEx.DomainCode },
            },
            Exception = exception,
        });

        return true;
    }
}
```
**Nota**: `CustomizeProblemDetails` se ejecuta DESPUÉS de este handler y agrega `correlationId` automáticamente.

### Generación del spec OpenAPI en build time

Configurar `Api.csproj` para generar el spec durante `dotnet build`:
```xml
<PropertyGroup>
  <!-- ... props existentes ... -->
  <OpenApiDocumentsDirectory>$(MSBuildProjectDirectory)/../../scripts/codegen</OpenApiDocumentsDirectory>
  <OpenApiDocumentName>v1</OpenApiDocumentName>
</PropertyGroup>
```
Después de `dotnet build FIBRADIS.slnx`, existirá `scripts/codegen/v1.json` con el spec completo.

**Fallback si la generación en build falla**: Ejecutar el API con `dotnet run --project src/Server/Api` y obtener el spec manualmente:
```powershell
Invoke-WebRequest -Uri "http://localhost:5265/openapi/v1.json" -OutFile "scripts/codegen/v1.json"
```

### Script de codegen para SharedApiClient

```powershell
# scripts/codegen/generate-api-client.ps1
$schemaPath = Join-Path $PSScriptRoot "v1.json"
$outputPath = Join-Path $PSScriptRoot "../../src/Web/SharedApiClient/schema.d.ts"

if (-not (Test-Path $schemaPath)) {
    Write-Error "Schema file not found: $schemaPath`nEjecutar primero: dotnet build FIBRADIS.slnx"
    exit 1
}

npx openapi-typescript $schemaPath --output $outputPath
if ($LASTEXITCODE -ne 0) { Write-Error "openapi-typescript falló"; exit 1 }
Write-Host "✅ Generado: $outputPath"
```

### SharedApiClient — estructura del workspace npm

```
src/Web/SharedApiClient/
├── package.json   ← workspace package (NO editar manualmente)
├── schema.d.ts    ← AUTO-GENERADO por openapi-typescript — NO editar
└── index.ts       ← re-exports; SÍ está bajo control del dev
```

`src/Web/SharedApiClient/package.json`:
```json
{
  "name": "@fibradis/shared-api-client",
  "version": "0.0.0",
  "private": true,
  "type": "module",
  "exports": {
    ".": "./index.ts"
  },
  "types": "./index.ts"
}
```

`src/Web/SharedApiClient/index.ts`:
```typescript
export type { paths, components, operations, webhooks } from './schema.d.ts'
```
**Nota**: `openapi-typescript` v7 genera `paths`, `components`, `operations`, y posiblemente `webhooks`. Exportar todos los que existan en el schema generado.

Root `package.json` — cambios requeridos:
```json
{
  "workspaces": ["src/Web/Main", "src/Web/Ops", "src/Web/SharedApiClient"],
  "scripts": {
    "dev:main": "npm run dev --workspace=src/Web/Main",
    "dev:ops": "npm run dev --workspace=src/Web/Ops",
    "build:main": "npm run build --workspace=src/Web/Main",
    "build:ops": "npm run build --workspace=src/Web/Ops",
    "codegen:api": "pwsh -File scripts/codegen/generate-api-client.ps1"
  },
  "devDependencies": {
    "openapi-typescript": "^7.0.0"
  }
}
```

### Proxy Vite para desarrollo

Agregar en `vite.config.ts` de ambas SPAs (el backend HTTP dev corre en puerto 5265):
```typescript
server: {
  port: 5173, // 5174 para Ops
  proxy: {
    '/api': {
      target: 'http://localhost:5265',
      changeOrigin: true,
    },
    '/openapi': {
      target: 'http://localhost:5265',
      changeOrigin: true,
    },
  },
},
```

### CORS — solo desarrollo

El proxy Vite hace que en producción no haya CORS (ambas SPAs y la API son del mismo host). CORS solo se necesita en desarrollo cuando Vite corre en puertos separados:
```csharp
// En AddApiInfrastructure:
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(options =>
        options.AddPolicy("SpaDev", policy =>
            policy.WithOrigins("http://localhost:5173", "http://localhost:5174")
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials())); // AllowCredentials para cookies de refresh token (Historia 1.3)
}
```

### Tests de integración — configuración

Agregar a `Api.csproj` para que Program sea visible desde los tests:
```csharp
// Al final de Program.cs — NECESARIO para WebApplicationFactory
public partial class Program { }
```

`ApiWebFactory.cs`:
```csharp
public class ApiWebFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        // Sobrescribir connection string para tests
        builder.ConfigureAppConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=.;Database=FIBRADIS_Test;Trusted_Connection=True;TrustServerCertificate=True;"
            });
        });
    }
}
```

**Importante para CA-4 test**: El test de ProblemDetails hace un GET a una ruta inexistente (`/api/v1/ruta-inexistente`). El middleware ASP.NET Core debe retornar 404 con ProblemDetails shape y el correlationId en las extensions. Verificar que el pipeline incluye `UseStatusCodePages()` o que el manejo de 404 genera ProblemDetails (esto es automático con `AddProblemDetails()` + `UseExceptionHandler()`).

**Nota**: Si los tests de integración requieren un SQL Server activo (porque DbContext intenta conectarse en startup), considerar sobreescribir el DbContext en la factory para usar una base en memoria solo para los tests de la historia 1.2. El objetivo es que los tests de API no dependan de la BD para endpoints que no la usan (como `/health`).

### Scalar.AspNetCore — configuración en Program.cs

```csharp
// En UseApiInfrastructure:
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi(); // /openapi/v1.json
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("FIBRADIS API")
               .WithEndpointPrefix("/swagger/{documentName}");
    }); // /swagger/v1
}
```
La CA-2 acepta `/swagger` (que redirige a `/swagger/v1`) o la ruta exacta `/swagger/v1`. Verificar que el endpoint de Scalar queda accesible.

### Estructura de directorios relevante para esta historia

```
src/Server/
├── Api/
│   ├── CompositionRoot/
│   │   ├── ApiServiceExtensions.cs      ← NUEVO
│   │   ├── UseApiInfrastructureExtensions.cs  ← NUEVO
│   │   └── GlobalExceptionHandler.cs    ← NUEVO
│   ├── Middleware/
│   │   └── CorrelationIdMiddleware.cs   ← NUEVO
│   ├── Endpoints/
│   │   └── Public/
│   │       └── HealthEndpoint.cs        ← NUEVO
│   ├── Program.cs                       ← MODIFICAR
│   └── Api.csproj                       ← MODIFICAR
├── Domain/
│   └── Common/
│       └── DomainException.cs           ← NUEVO
src/Web/
├── SharedApiClient/
│   ├── package.json                     ← NUEVO
│   ├── schema.d.ts                      ← GENERADO por codegen
│   └── index.ts                         ← NUEVO
scripts/codegen/
│   ├── generate-api-client.ps1          ← NUEVO
│   └── v1.json                          ← GENERADO por dotnet build
tests/Integration/Api.Tests/
│   ├── ApiWebFactory.cs                 ← NUEVO
│   ├── HealthEndpointTests.cs           ← NUEVO
│   ├── OpenApiEndpointTests.cs          ← NUEVO
│   └── ProblemDetailsTests.cs           ← NUEVO
```

### Dependencias del epic que esta historia establece

- **Historia 1.3** (Auth JWT) depende de `Program.cs` refactorizado y de `GlobalExceptionHandler` para errores de autenticación
- **Historia 1.4** (Hangfire + health checks) agrega health checks al endpoint `/health` ya creado aquí
- **Épicas 2+** consumen `@fibradis/shared-api-client` y el patrón de endpoint extension methods
- `DomainException` es la base de todas las excepciones de negocio futuras

### Lo que NO debe hacer esta historia

- No implementar ningún endpoint de negocio (Épicas 2+)
- No configurar JWT auth (Historia 1.3)
- No configurar Hangfire ni health checks para pipelines (Historia 1.4)
- No agregar datos semilla ni modelos de dominio de negocio
- `GlobalExceptionHandler` solo maneja `DomainException` — no swallow-ear ni loguear otras excepciones (eso es Historia 1.4)
- El proxy Vite es solo para desarrollo — no hay cambio en la configuración de producción
- No agregar Swagger/Scalar en Production (solo Development)
- `SharedApiClient/schema.d.ts` es auto-generado: nunca editar manualmente

### Verificación final antes de mover a `review`

1. `dotnet build FIBRADIS.slnx` — exit code 0, sin errores ni advertencias
2. `curl http://localhost:5265/api/v1/health` → `{"status":"healthy"}`
3. `curl http://localhost:5265/openapi/v1.json` → JSON con `"openapi":"3"` y path `/api/v1/health`
4. Navegador en `http://localhost:5265/swagger` → Scalar UI carga correctamente
5. `curl http://localhost:5265/api/v1/no-existe` → ProblemDetails JSON con `correlationId` y header `X-Correlation-Id`
6. `dotnet test tests/Integration/Api.Tests/` → todos pasan
7. `dotnet build FIBRADIS.slnx` genera `scripts/codegen/v1.json` automáticamente
8. `npm run codegen:api` → genera `src/Web/SharedApiClient/schema.d.ts` sin errores
9. `npm run build:main` y `npm run build:ops` → exit code 0, sin errores TypeScript

---

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6 (create-story, 2026-05-15)

### Debug Log References

- Scalar.AspNetCore 2.3.4 no disponible en NuGet; se usó 2.4.1 (latest estable compatible .NET 10).
- Scalar 2.4.1 cambió el API: `WithEndpointPrefix` obsoleto; se usó parámetro `endpointPrefix` en `MapScalarApiReference("/swagger", ...)`.
- `MapScalarApiReference` en 2.4.1 no acepta `{documentName}` como placeholder en el prefix — se usa `/swagger` directamente.
- `Microsoft.Extensions.ApiDescription.Server` v10 genera el archivo con nombre del ensamblado (`Api.json`) no del documento (`v1.json`); el script de codegen se adaptó.
- El path `$(MSBuildProjectDirectory)/../../scripts/codegen` era incorrecto; corregido a `/../../../scripts/codegen` (3 niveles desde `src/Server/Api/`).
- `ApiWebFactory` usa InMemory EF Core para aislar los tests de integración de API de la base de datos SQL Server.

### Completion Notes List

- Todos los ACs satisfechos: CA-1 (health endpoint 200), CA-2 (OpenAPI en /openapi/v1.json + Scalar en /swagger), CA-3 (SharedApiClient tipado importable con tsc sin errores), CA-4 (ProblemDetails con domainCode + correlationId + header X-Correlation-Id).
- 4 tests de integración pasan: HealthEndpoint, OpenApi, ProblemDetails (x2).
- `dotnet build FIBRADIS.slnx` genera `scripts/codegen/Api.json` automáticamente.
- `npm run codegen:api` genera `src/Web/SharedApiClient/schema.d.ts` sin errores.
- `npm run build:main` y `npm run build:ops` compilan sin errores TypeScript.
- Scalar disponible como UI en `/swagger/v1` (accesible desde `/swagger`).

### Code Review Findings

#### Hallazgo 1 — Alto

El contrato OpenAPI no describe el cuerpo `200 OK` de `GET /api/v1/health`, por lo que el `SharedApiClient` generado no queda realmente tipado para consumir la respuesta JSON del endpoint.

- Evidencia: `src/Server/Api/Endpoints/Public/HealthEndpoint.cs` declara `.Produces(StatusCodes.Status200OK)` sin especificar el payload.
- Evidencia: `src/Web/SharedApiClient/schema.d.ts` genera `responses.200.content?: never` para `GetHealth`.
- Impacto: Se cumple que `paths` es importable, pero no se cumple la intención funcional de un cliente tipado de extremo a extremo para el body `{"status":"healthy"}`.
- Recomendación: Declarar un contrato explícito del response en el endpoint, por ejemplo con un DTO/record compartido y `.Produces<HealthResponse>(StatusCodes.Status200OK)`, luego regenerar `Api.json` y `schema.d.ts`.

#### Hallazgo 2 — Medio

No hay prueba de integración para la UI de Scalar en `/swagger`, aunque CA-2 la pide explícitamente.

- Evidencia: `src/Server/Api/CompositionRoot/UseApiInfrastructureExtensions.cs` registra `app.MapScalarApiReference("/swagger", ...)`.
- Evidencia: `tests/Integration/Api.Tests/OpenApiEndpointTests.cs` solo valida `/openapi/v1.json`.
- Impacto: La historia puede pasar con OpenAPI JSON correcto aunque la UI de documentación quede rota o deje de resolver el spec.
- Recomendación: Agregar un test que verifique `GET /swagger` o la ruta final servida por Scalar y confirme respuesta `200 OK` o la redirección esperada.

### File List

#### Nuevos
- `src/Server/Api/Middleware/CorrelationIdMiddleware.cs`
- `src/Server/Api/CompositionRoot/ApiServiceExtensions.cs`
- `src/Server/Api/CompositionRoot/UseApiInfrastructureExtensions.cs`
- `src/Server/Api/CompositionRoot/GlobalExceptionHandler.cs`
- `src/Server/Api/Endpoints/Public/HealthEndpoint.cs`
- `src/Server/Domain/Common/DomainException.cs`
- `src/Web/SharedApiClient/package.json`
- `src/Web/SharedApiClient/schema.d.ts` (auto-generado)
- `src/Web/SharedApiClient/index.ts`
- `scripts/codegen/generate-api-client.ps1`
- `scripts/codegen/Api.json` (auto-generado por dotnet build)
- `tests/Integration/Api.Tests/ApiWebFactory.cs`
- `tests/Integration/Api.Tests/HealthEndpointTests.cs`
- `tests/Integration/Api.Tests/OpenApiEndpointTests.cs`
- `tests/Integration/Api.Tests/ProblemDetailsTests.cs`

#### Modificados
- `src/Server/Api/Program.cs`
- `src/Server/Api/Api.csproj`
- `Directory.Packages.props`
- `package.json` (raíz)
- `src/Web/Main/package.json`
- `src/Web/Ops/package.json`
- `src/Web/Main/vite.config.ts`
- `src/Web/Ops/vite.config.ts`
- `tests/Integration/Api.Tests/Api.Tests.csproj`

### Change Log

- 2026-05-15: Implementada historia 1.2 completa — Backend API v1 con OpenAPI, endpoint de salud, ProblemDetails extendido con correlationId/domainCode, SharedApiClient TypeScript tipado, proxy Vite, 4 tests de integración. (claude-sonnet-4-6)
