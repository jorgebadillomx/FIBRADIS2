# Historia 1.3: Autenticación JWT y autorización por roles

**Epic:** 1 — Fundación, Infraestructura y Acceso
**Story ID:** 1.3
**Story Key:** `1-3-autenticacion-jwt-y-autorizacion-por-roles`
**Status:** ready-for-dev
**Date:** 2026-05-15

---

## Historia de Usuario

Como usuario o AdminOps,
quiero autenticarme con correo y contraseña, recibir un token de acceso JWT de vida corta y un refresh token rotado, y tener mi rol aplicado en las rutas protegidas,
para que las superficies privadas y de operaciones sean inaccesibles sin credenciales válidas.

---

## Criterios de Aceptación

**CA-1: Login con JWT**
Dado que tengo credenciales válidas de una cuenta Usuario,
Cuando hago `POST /api/v1/auth/login`,
Entonces recibo `200 OK` con `{"accessToken": "<jwt>"}` en el cuerpo y una cookie `HttpOnly` con el refresh token.

**CA-2: Refresh token con rotación**
Dado que el token de acceso ha expirado,
Cuando hago `POST /api/v1/auth/refresh` (enviando la cookie del refresh token),
Entonces recibo `200 OK` con un nuevo `accessToken` en el cuerpo; el refresh token anterior queda invalidado en base de datos y se emite una nueva cookie con el nuevo refresh token.

**CA-3: Acceso a ruta privada con rol Usuario**
Dado que tengo un JWT válido de Usuario en el header `Authorization: Bearer <token>`,
Cuando hago `GET /api/v1/me` (ruta privada de prueba),
Entonces recibo `200 OK`.

**CA-4: Acceso a ruta Ops bloqueado para rol Usuario**
Dado que tengo un JWT válido de Usuario,
Cuando hago `GET /api/v1/ops/ping` (ruta de Ops de prueba),
Entonces recibo `403 Forbidden`.

**CA-5: Acceso a ruta Ops permitido para rol AdminOps**
Dado que tengo un JWT válido de AdminOps,
Cuando hago `GET /api/v1/ops/ping`,
Entonces recibo `200 OK`.

**CA-6: Acceso sin token**
Dado que no envío ningún token,
Cuando accedo a `GET /api/v1/health` (ruta pública),
Entonces recibo `200 OK`.
Cuando accedo a `GET /api/v1/me` (ruta privada),
Entonces recibo `401 Unauthorized`.

---

## Tareas / Subtareas

- [ ] Task 1: Agregar paquetes NuGet y actualizar CPM (AC: CA-1, CA-2, CA-3, CA-4, CA-5, CA-6)
  - [ ] Agregar `Microsoft.AspNetCore.Authentication.JwtBearer` (10.0.8) en `Directory.Packages.props`
  - [ ] Agregar `BCrypt.Net-Next` (4.0.3) en `Directory.Packages.props`
  - [ ] Agregar ambos `<PackageReference>` en `Api.csproj` (sin versión inline — CPM)
  - [ ] Agregar `BCrypt.Net-Next` también en `Infrastructure.csproj` (el hashing vive en Infrastructure/Security)
  - [ ] Agregar configuración de JWT en `appsettings.Development.json` (ver Dev Notes para esquema exacto)

- [ ] Task 2: Domain — Entidades, Value Objects y Excepciones (AC: CA-1, CA-2)
  - [ ] Crear `src/Server/Domain/Auth/User.cs` — entidad con `Id`, `Email`, `PasswordHash`, `Role`, `CreatedAt`, `IsActive`
  - [ ] Crear `src/Server/Domain/Auth/UserRole.cs` — enum con valores `User` y `AdminOps`
  - [ ] Crear `src/Server/Domain/Auth/Exceptions/InvalidCredentialsException.cs` — extiende `DomainException` con domainCode `"INVALID_CREDENTIALS"`
  - [ ] Crear `src/Server/Domain/Auth/Exceptions/InvalidRefreshTokenException.cs` — extiende `DomainException` con domainCode `"INVALID_REFRESH_TOKEN"`

- [ ] Task 3: Infrastructure — Persistencia y TokenService (AC: CA-1, CA-2)
  - [ ] Crear `src/Server/Infrastructure/Persistence/SqlServer/Configurations/Auth/UserConfiguration.cs` — EF Core `IEntityTypeConfiguration<User>` con tabla `[auth].[User]`, clave primaria, índice único en email
  - [ ] Crear `src/Server/Infrastructure/Persistence/SqlServer/Configurations/Auth/RefreshTokenConfiguration.cs` — tabla `[auth].[RefreshToken]`, FK a User, índice en `TokenHash`, campos `ExpiresAt` y `RevokedAt`
  - [ ] Crear `src/Server/Domain/Auth/RefreshToken.cs` — entidad con `Id`, `UserId`, `TokenHash`, `ExpiresAt`, `RevokedAt`, `CreatedAt`
  - [ ] Registrar `DbSet<User>` y `DbSet<RefreshToken>` en `AppDbContext` y aplicar las configuraciones en `OnModelCreating`
  - [ ] Crear `src/Server/Infrastructure/Security/TokenService.cs` — genera JWT (ver spec en Dev Notes) y refresh tokens (GUID criptográfico hasheado con BCrypt)
  - [ ] Crear migración EF Core: `dotnet ef migrations add AddAuthSchema --project src/Server/Infrastructure --startup-project src/Server/Api` — verificar que genera `[auth].[User]` y `[auth].[RefreshToken]`
  - [ ] Registrar `TokenService` como `ITokenService` en el contenedor DI (en `ApiServiceExtensions` o nuevo extension method en Infrastructure)

- [ ] Task 4: Application — AuthService y Contratos (AC: CA-1, CA-2)
  - [ ] Crear `src/Server/SharedApiContracts/Auth/LoginRequest.cs` — record con `Email` (string) y `Password` (string)
  - [ ] Crear `src/Server/SharedApiContracts/Auth/LoginResponse.cs` — record con `AccessToken` (string)
  - [ ] Crear `src/Server/SharedApiContracts/Auth/RefreshResponse.cs` — record con `AccessToken` (string)
  - [ ] Crear `src/Server/Application/Auth/IAuthService.cs` — interfaz con `LoginAsync` y `RefreshAsync`
  - [ ] Crear `src/Server/Application/Auth/AuthService.cs` — implementa `IAuthService`; busca usuario por email, verifica password con BCrypt, genera access token + refresh token, persiste RefreshToken hasheado en BD, revoca token anterior en refresh
  - [ ] Registrar `AuthService` en DI

- [ ] Task 5: API — Endpoints y Configuración de Auth (AC: CA-1, CA-2, CA-3, CA-4, CA-5, CA-6)
  - [ ] Crear `src/Server/Api/Authentication/AddAuthenticationExtensions.cs` — `AddFibradisAuthentication(this WebApplicationBuilder builder)` que configura JWT bearer con las opciones del Dev Notes
  - [ ] Crear `src/Server/Api/Authentication/AddAuthorizationExtensions.cs` — `AddFibradisAuthorization(this WebApplicationBuilder builder)` que define políticas `"User"` y `"AdminOps"` basadas en claim de rol
  - [ ] Actualizar `src/Server/Api/CompositionRoot/ApiServiceExtensions.cs` — llamar a `builder.AddFibradisAuthentication()` y `builder.AddFibradisAuthorization()`
  - [ ] Actualizar `src/Server/Api/CompositionRoot/UseApiInfrastructureExtensions.cs` — agregar `app.UseAuthentication()` y `app.UseAuthorization()` DESPUÉS de `UseExceptionHandler` y ANTES de mapear endpoints (ver orden exacto en Dev Notes)
  - [ ] Crear `src/Server/Api/Endpoints/Public/AuthEndpoints.cs` — con `MapAuth(this IEndpointRouteBuilder app)` que registra `POST /api/v1/auth/login` y `POST /api/v1/auth/refresh`; el endpoint de login llama a `IAuthService.LoginAsync` y setea la cookie HttpOnly; el refresh llama a `IAuthService.RefreshAsync`
  - [ ] Crear `src/Server/Api/Endpoints/Private/MeEndpoint.cs` — `GET /api/v1/me` con `[Authorize(Policy = "User")]` retorna `{"role": "<rol del usuario>"}` — endpoint de prueba mínimo para CA-3
  - [ ] Crear `src/Server/Api/Endpoints/Ops/OpsEndpoints.cs` — `GET /api/v1/ops/ping` con `[Authorize(Policy = "AdminOps")]` retorna `{"status": "ok"}` — endpoint de prueba mínimo para CA-4/CA-5
  - [ ] Actualizar `src/Server/Api/Program.cs` — registrar `app.MapAuth()`, `app.MapMe()`, `app.MapOpsPing()`

- [ ] Task 6: Extender GlobalExceptionHandler (AC: CA-1, CA-2)
  - [ ] Actualizar `src/Server/Api/CompositionRoot/GlobalExceptionHandler.cs` — agregar manejo de `InvalidCredentialsException` → 401 y `InvalidRefreshTokenException` → 401 con ProblemDetails shape + `domainCode`

- [ ] Task 7: Tests de integración (AC: CA-1, CA-2, CA-3, CA-4, CA-5, CA-6)
  - [ ] Actualizar `tests/Integration/Api.Tests/ApiWebFactory.cs` — sobreescribir `ConfigureWebHost` para agregar seed de usuarios de prueba en la BD InMemory: un usuario `User` y un usuario `AdminOps` con passwords conocidos
  - [ ] Crear `tests/Integration/Api.Tests/AuthLoginTests.cs` — CA-1: POST login con credenciales válidas → 200 + `accessToken` en body + cookie `refreshToken` presente
  - [ ] Crear `tests/Integration/Api.Tests/AuthRefreshTests.cs` — CA-2: POST refresh con cookie válida → 200 + nuevo `accessToken`; segundo refresh con el token antiguo → 401
  - [ ] Crear `tests/Integration/Api.Tests/AuthorizationTests.cs` — CA-3: GET `/api/v1/me` con JWT de Usuario → 200; CA-4: GET `/api/v1/ops/ping` con JWT de Usuario → 403; CA-5: GET `/api/v1/ops/ping` con JWT de AdminOps → 200; CA-6a: GET `/api/v1/health` sin token → 200; CA-6b: GET `/api/v1/me` sin token → 401
  - [ ] `dotnet test tests/Integration/Api.Tests/` — todos los tests existentes + nuevos pasan
  - [ ] `dotnet build FIBRADIS.slnx` — exit code 0, sin warnings

---

## Dev Notes

### Stack exacto — NO negociar versiones

| Componente | Versión requerida |
|---|---|
| .NET | 10 LTS (ya fijado) |
| EF Core | 10.0.8 (ya en CPM — no cambiar) |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 10.0.8 |
| `BCrypt.Net-Next` | 4.0.3 (última estable — verificar NuGet antes de usar) |
| Resto del stack | Sin cambios: React 19.2, Vite 7.3.3, TypeScript ~6.0.2 |

### Regla obligatoria: Central Package Management (CPM)

**NUNCA** agregar versiones en los `<PackageReference>` de los `.csproj`. Solo en `Directory.Packages.props`:

```xml
<!-- Directory.Packages.props — agregar -->
<PackageVersion Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.0.8" />
<PackageVersion Include="BCrypt.Net-Next" Version="4.0.3" />
```

```xml
<!-- Api.csproj — solo Include, sin Version -->
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" />
<PackageReference Include="BCrypt.Net-Next" />

<!-- Infrastructure.csproj — solo Include, sin Version -->
<PackageReference Include="BCrypt.Net-Next" />
```

### Estado actual de Program.cs (al inicio de esta historia)

```csharp
// src/Server/Api/Program.cs — ESTADO ACTUAL
using Api.CompositionRoot;
using Api.Endpoints.Public;
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.AddApiInfrastructure();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();
app.UseApiInfrastructure();
app.UseHttpsRedirection();
app.MapHealth();
app.Run();

public partial class Program { }
```

**Estado objetivo de Program.cs al finalizar la historia:**

```csharp
using Api.CompositionRoot;
using Api.Endpoints.Ops;
using Api.Endpoints.Private;
using Api.Endpoints.Public;
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.AddApiInfrastructure();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();
app.UseApiInfrastructure();
app.UseHttpsRedirection();
app.MapHealth();
app.MapAuth();
app.MapMe();
app.MapOpsPing();
app.Run();

public partial class Program { }
```

### Estado actual de ApiServiceExtensions.cs (al inicio)

```csharp
// ya tiene: AddOpenApi("v1"), AddProblemDetails, AddExceptionHandler<GlobalExceptionHandler>, AddCors SpaDev
// Esta historia agrega llamadas a AddFibradisAuthentication() y AddFibradisAuthorization()
```

**Agregar al final del método `AddApiInfrastructure`, antes del `return builder`:**

```csharp
builder.AddFibradisAuthentication();
builder.AddFibradisAuthorization();
```

### Estado actual de UseApiInfrastructureExtensions.cs (al inicio)

```csharp
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();
app.UseStatusCodePages();
// (dev) UseCors, MapOpenApi, MapScalarApiReference
```

**Orden OBLIGATORIO del pipeline tras este historia:**

```csharp
app.UseMiddleware<CorrelationIdMiddleware>();   // primero — correlation ID en todos los errores
app.UseExceptionHandler();                       // segundo
app.UseStatusCodePages();                        // tercero
if (dev) app.UseCors("SpaDev");                 // cuarto (solo dev)
app.UseAuthentication();                         // QUINTO — NUEVO
app.UseAuthorization();                          // SEXTO — NUEVO (siempre después de Authentication)
if (dev) { app.MapOpenApi(); app.MapScalarApiReference(...); }
```

**CRÍTICO:** `UseAuthentication()` debe ir ANTES de `UseAuthorization()`. Si están en orden incorrecto, las policies no se evalúan correctamente y todos los endpoints protegidos devuelven 401.

### Configuración JWT — AddAuthenticationExtensions.cs

```csharp
// src/Server/Api/Authentication/AddAuthenticationExtensions.cs
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Api.Authentication;

public static class AddAuthenticationExtensions
{
    public static WebApplicationBuilder AddFibradisAuthentication(this WebApplicationBuilder builder)
    {
        var jwtSection = builder.Configuration.GetSection("Jwt");
        var secret = jwtSection["Secret"]
            ?? throw new InvalidOperationException("JWT Secret no configurado en appsettings.");
        var issuer = jwtSection["Issuer"] ?? "fibradis";
        var audience = jwtSection["Audience"] ?? "fibradis-client";

        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,  // sin tolerancia — el token expira exactamente cuando dice
            };
        });

        return builder;
    }
}
```

### Configuración de Políticas — AddAuthorizationExtensions.cs

```csharp
// src/Server/Api/Authentication/AddAuthorizationExtensions.cs
using System.Security.Claims;

namespace Api.Authentication;

public static class AddAuthorizationExtensions
{
    public static WebApplicationBuilder AddFibradisAuthorization(this WebApplicationBuilder builder)
    {
        builder.Services.AddAuthorization(options =>
        {
            // Política "User": cualquier usuario autenticado (rol User o AdminOps)
            options.AddPolicy("User", policy =>
                policy.RequireAuthenticatedUser());

            // Política "AdminOps": solo el rol AdminOps
            options.AddPolicy("AdminOps", policy =>
                policy.RequireClaim(ClaimTypes.Role, "AdminOps"));
        });

        return builder;
    }
}
```

**Nota:** AdminOps también puede acceder a rutas `[Authorize(Policy = "User")]` porque `RequireAuthenticatedUser()` solo pide que el token sea válido. Si en el futuro se necesita restringir rutas privadas a `User` solo (sin AdminOps), cambiar a `policy.RequireClaim(ClaimTypes.Role, "User", "AdminOps")`.

### TokenService — generación de JWT y refresh tokens

```csharp
// src/Server/Infrastructure/Security/TokenService.cs
using Domain.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Security;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    string HashRefreshToken(string rawToken);
}

public class TokenService(IConfiguration configuration) : ITokenService
{
    private readonly string _secret = configuration["Jwt:Secret"]
        ?? throw new InvalidOperationException("JWT Secret no configurado.");
    private readonly string _issuer = configuration["Jwt:Issuer"] ?? "fibradis";
    private readonly string _audience = configuration["Jwt:Audience"] ?? "fibradis-client";
    private readonly int _accessTokenMinutes = int.Parse(
        configuration["Jwt:AccessTokenMinutes"] ?? "15");

    public string GenerateAccessToken(User user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_accessTokenMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    public string HashRefreshToken(string rawToken)
        => BCrypt.Net.BCrypt.HashPassword(rawToken);
}
```

### AuthService — lógica de autenticación

```csharp
// src/Server/Application/Auth/AuthService.cs
using Domain.Auth;
using Domain.Auth.Exceptions;
using Infrastructure.Persistence.SqlServer;
using Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace Application.Auth;

public interface IAuthService
{
    Task<(string AccessToken, string RefreshToken)> LoginAsync(
        string email, string password, CancellationToken ct = default);
    Task<(string AccessToken, string RefreshToken)> RefreshAsync(
        string rawRefreshToken, CancellationToken ct = default);
}

public class AuthService(AppDbContext db, ITokenService tokenService) : IAuthService
{
    public async Task<(string, string)> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email && u.IsActive, ct);

        if (user is null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            throw new InvalidCredentialsException();

        return await IssueTokensAsync(user, ct);
    }

    public async Task<(string, string)> RefreshAsync(string rawRefreshToken, CancellationToken ct = default)
    {
        // Buscar todos los tokens no revocados no expirados y verificar con BCrypt
        var candidates = await db.RefreshTokens
            .Include(rt => rt.User)
            .Where(rt => rt.RevokedAt == null && rt.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(ct);

        var stored = candidates.FirstOrDefault(rt =>
            BCrypt.Net.BCrypt.Verify(rawRefreshToken, rt.TokenHash));

        if (stored is null)
            throw new InvalidRefreshTokenException();

        // Revocar el token usado
        stored.RevokedAt = DateTime.UtcNow;

        return await IssueTokensAsync(stored.User!, ct);
    }

    private async Task<(string, string)> IssueTokensAsync(User user, CancellationToken ct)
    {
        var rawRefresh = tokenService.GenerateRefreshToken();
        var hashRefresh = tokenService.HashRefreshToken(rawRefresh);

        db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = hashRefresh,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
        });

        await db.SaveChangesAsync(ct);

        var accessToken = tokenService.GenerateAccessToken(user);
        return (accessToken, rawRefresh);
    }
}
```

**Nota de seguridad:** La búsqueda de refresh tokens carga candidatos en memoria para verificar con BCrypt (no se puede hacer en SQL porque el hash no es determinista). Para MVP esto es aceptable. Si el volumen crece, agregar un campo `UserId` o índice parcial en el token para filtrar candidatos antes de la búsqueda en memoria.

### Entidades de Dominio

```csharp
// src/Server/Domain/Auth/User.cs
namespace Domain.Auth;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }

    // Navigation
    public List<RefreshToken> RefreshTokens { get; set; } = [];
}

// src/Server/Domain/Auth/UserRole.cs
namespace Domain.Auth;
public enum UserRole { User, AdminOps }

// src/Server/Domain/Auth/RefreshToken.cs
namespace Domain.Auth;

public class RefreshToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation
    public User? User { get; set; }
}
```

### Excepciones de Dominio

```csharp
// src/Server/Domain/Auth/Exceptions/InvalidCredentialsException.cs
namespace Domain.Auth.Exceptions;

public class InvalidCredentialsException()
    : DomainException("Credenciales inválidas.", "INVALID_CREDENTIALS");

// src/Server/Domain/Auth/Exceptions/InvalidRefreshTokenException.cs
namespace Domain.Auth.Exceptions;

public class InvalidRefreshTokenException()
    : DomainException("Refresh token inválido o expirado.", "INVALID_REFRESH_TOKEN");
```

### Configuración EF Core — schema `auth`

```csharp
// src/Server/Infrastructure/Persistence/SqlServer/Configurations/Auth/UserConfiguration.cs
using Domain.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.SqlServer.Configurations.Auth;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("User", schema: "auth");
        builder.HasKey(u => u.Id);
        builder.HasIndex(u => u.Email).IsUnique().HasDatabaseName("UX_User_Email");
        builder.Property(u => u.Email).HasMaxLength(256).IsRequired();
        builder.Property(u => u.PasswordHash).HasMaxLength(512).IsRequired();
        builder.Property(u => u.Role).HasConversion<string>().HasMaxLength(32);
        builder.HasMany(u => u.RefreshTokens).WithOne(rt => rt.User).HasForeignKey(rt => rt.UserId);
    }
}

// src/Server/Infrastructure/Persistence/SqlServer/Configurations/Auth/RefreshTokenConfiguration.cs
public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshToken", schema: "auth");
        builder.HasKey(rt => rt.Id);
        builder.HasIndex(rt => new { rt.UserId, rt.RevokedAt })
               .HasDatabaseName("IX_RefreshToken_UserId_RevokedAt");
        builder.Property(rt => rt.TokenHash).HasMaxLength(512).IsRequired();
    }
}
```

**Registrar en `AppDbContext.OnModelCreating`:**
```csharp
modelBuilder.ApplyConfiguration(new UserConfiguration());
modelBuilder.ApplyConfiguration(new RefreshTokenConfiguration());
// Y los DbSet correspondientes:
public DbSet<User> Users => Set<User>();
public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
```

### Endpoint de Login — Cookie HttpOnly

```csharp
// src/Server/Api/Endpoints/Public/AuthEndpoints.cs
using Api.Contracts.Auth;
using Application.Auth;

namespace Api.Endpoints.Public;

public static class AuthEndpoints
{
    private const string RefreshTokenCookie = "refreshToken";

    public static IEndpointRouteBuilder MapAuth(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth").WithTags("Auth");

        group.MapPost("/login", async (
            LoginRequest request,
            IAuthService authService,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var (accessToken, refreshToken) = await authService.LoginAsync(request.Email, request.Password, ct);
            SetRefreshCookie(ctx, refreshToken);
            return Results.Ok(new LoginResponse(accessToken));
        })
        .AllowAnonymous()
        .Produces<LoginResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/refresh", async (
            IAuthService authService,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var rawToken = ctx.Request.Cookies[RefreshTokenCookie];
            if (string.IsNullOrEmpty(rawToken))
                return Results.Unauthorized();

            var (accessToken, newRefreshToken) = await authService.RefreshAsync(rawToken, ct);
            SetRefreshCookie(ctx, newRefreshToken);
            return Results.Ok(new RefreshResponse(accessToken));
        })
        .AllowAnonymous()
        .Produces<RefreshResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        return app;
    }

    private static void SetRefreshCookie(HttpContext ctx, string token)
    {
        ctx.Response.Cookies.Append(RefreshTokenCookie, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = !ctx.Request.Host.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase),
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(7),
        });
    }
}
```

### Endpoint de prueba Privado — /api/v1/me

```csharp
// src/Server/Api/Endpoints/Private/MeEndpoint.cs
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Api.Endpoints.Private;

public static class MeEndpoint
{
    public static IEndpointRouteBuilder MapMe(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/me", (ClaimsPrincipal user) =>
            Results.Ok(new { role = user.FindFirstValue(ClaimTypes.Role) }))
           .RequireAuthorization("User")
           .WithTags("Auth")
           .Produces<object>(StatusCodes.Status200OK)
           .ProducesProblem(StatusCodes.Status401Unauthorized);

        return app;
    }
}
```

### Endpoint de prueba Ops — /api/v1/ops/ping

```csharp
// src/Server/Api/Endpoints/Ops/OpsEndpoints.cs
namespace Api.Endpoints.Ops;

public static class OpsEndpoints
{
    public static IEndpointRouteBuilder MapOpsPing(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/ops/ping", () =>
            Results.Ok(new { status = "ok" }))
           .RequireAuthorization("AdminOps")
           .WithTags("Ops")
           .Produces<object>(StatusCodes.Status200OK)
           .ProducesProblem(StatusCodes.Status401Unauthorized)
           .ProducesProblem(StatusCodes.Status403Forbidden);

        return app;
    }
}
```

### Extensión de GlobalExceptionHandler para errores de Auth

```csharp
// Actualizar src/Server/Api/CompositionRoot/GlobalExceptionHandler.cs
// Agregar manejo de InvalidCredentialsException e InvalidRefreshTokenException
// antes del check de DomainException genérico:

if (exception is Domain.Auth.Exceptions.InvalidCredentialsException
    or Domain.Auth.Exceptions.InvalidRefreshTokenException)
{
    httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
    // usar problemDetailsService igual que con DomainException pero con status 401
}
```

**Alternativa más limpia:** Cambiar el handler para hacer el dispatch basado en el status code de la excepción, no en el tipo. Si `DomainException` tuviera un `HttpStatusCode` property, el handler sería genérico. Para MVP, el check explícito de tipos es aceptable.

### Configuración appsettings.Development.json

```json
{
  "Jwt": {
    "Secret": "dev-only-secret-key-must-be-at-least-32-chars-long!!",
    "Issuer": "fibradis",
    "Audience": "fibradis-client",
    "AccessTokenMinutes": "15"
  }
}
```

**CRÍTICO:** En producción, `Jwt:Secret` debe venir de variable de entorno o secret manager. NUNCA comitear un secret de producción. El valor dev es seguro para comitear porque es explícitamente para desarrollo.

**Mínimo de longitud del secret:** La clave HMAC-SHA256 necesita al menos 32 bytes (256 bits). El ejemplo de arriba tiene 51 caracteres — suficiente.

### Tests de integración — ApiWebFactory con seed de usuarios

```csharp
// Actualizar tests/Integration/Api.Tests/ApiWebFactory.cs
// Agregar seed de usuarios en la BD InMemory para los tests de auth

protected override void ConfigureWebHost(IWebHostBuilder builder)
{
    builder.UseEnvironment("Development");
    builder.ConfigureAppConfiguration(config =>
    {
        config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Server=.;...",
            // Configuración JWT para tests:
            ["Jwt:Secret"] = "test-secret-key-must-be-at-least-32-chars-long!!!",
            ["Jwt:Issuer"] = "fibradis",
            ["Jwt:Audience"] = "fibradis-client",
            ["Jwt:AccessTokenMinutes"] = "15",
        });
    });
    builder.ConfigureServices(services =>
    {
        // Seed de usuarios de prueba después de que DbContext esté configurado
        // Usar la factoría para obtener el scope y sembrar datos
    });
}
```

**Patrón para seed de usuarios en tests:** Crear un método de extensión en la factoría:

```csharp
public async Task SeedUsersAsync()
{
    using var scope = Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();

    if (!await db.Users.AnyAsync())
    {
        db.Users.AddRange(
            new User
            {
                Id = Guid.Parse("11111111-0000-0000-0000-000000000001"),
                Email = "user@test.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                Role = UserRole.User,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            },
            new User
            {
                Id = Guid.Parse("22222222-0000-0000-0000-000000000001"),
                Email = "adminops@test.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin456"),
                Role = UserRole.AdminOps,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            });
        await db.SaveChangesAsync();
    }
}
```

### Contratos en SharedApiContracts

```csharp
// src/Server/SharedApiContracts/Auth/LoginRequest.cs
namespace SharedApiContracts.Auth;
public record LoginRequest(string Email, string Password);

// src/Server/SharedApiContracts/Auth/LoginResponse.cs
namespace SharedApiContracts.Auth;
public record LoginResponse(string AccessToken);

// src/Server/SharedApiContracts/Auth/RefreshResponse.cs
namespace SharedApiContracts.Auth;
public record RefreshResponse(string AccessToken);
```

### Regenerar SharedApiClient TypeScript

Después de agregar los endpoints de auth, regenerar el schema:

```powershell
dotnet build FIBRADIS.slnx   # regenera scripts/codegen/Api.json
npm run codegen:api           # regenera src/Web/SharedApiClient/schema.d.ts
```

Verificar que `schema.d.ts` incluye las rutas `/api/v1/auth/login`, `/api/v1/auth/refresh`, `/api/v1/me`, `/api/v1/ops/ping`.

### Estructura de directorios relevante para esta historia

```
src/Server/
├── Api/
│   ├── Authentication/
│   │   ├── AddAuthenticationExtensions.cs   ← NUEVO
│   │   └── AddAuthorizationExtensions.cs    ← NUEVO
│   ├── CompositionRoot/
│   │   ├── ApiServiceExtensions.cs          ← MODIFICAR (agregar calls a auth extensions)
│   │   ├── UseApiInfrastructureExtensions.cs ← MODIFICAR (agregar UseAuthentication/UseAuthorization)
│   │   └── GlobalExceptionHandler.cs        ← MODIFICAR (agregar manejo 401)
│   ├── Endpoints/
│   │   ├── Public/
│   │   │   └── AuthEndpoints.cs             ← NUEVO
│   │   ├── Private/
│   │   │   └── MeEndpoint.cs               ← NUEVO
│   │   └── Ops/
│   │       └── OpsEndpoints.cs             ← NUEVO
│   └── Program.cs                          ← MODIFICAR
├── Application/
│   └── Auth/
│       ├── IAuthService.cs                  ← NUEVO
│       └── AuthService.cs                   ← NUEVO
├── Domain/
│   └── Auth/
│       ├── User.cs                          ← NUEVO
│       ├── UserRole.cs                      ← NUEVO
│       ├── RefreshToken.cs                  ← NUEVO
│       └── Exceptions/
│           ├── InvalidCredentialsException.cs  ← NUEVO
│           └── InvalidRefreshTokenException.cs ← NUEVO
├── Infrastructure/
│   ├── Persistence/
│   │   └── SqlServer/
│   │       └── Configurations/
│   │           └── Auth/
│   │               ├── UserConfiguration.cs       ← NUEVO
│   │               └── RefreshTokenConfiguration.cs ← NUEVO
│   └── Security/
│       └── TokenService.cs                  ← NUEVO
└── SharedApiContracts/
    └── Auth/
        ├── LoginRequest.cs                  ← NUEVO
        ├── LoginResponse.cs                 ← NUEVO
        └── RefreshResponse.cs               ← NUEVO

tests/Integration/Api.Tests/
├── ApiWebFactory.cs              ← MODIFICAR (agregar JWT config + seed users)
├── AuthLoginTests.cs             ← NUEVO
├── AuthRefreshTests.cs           ← NUEVO
└── AuthorizationTests.cs         ← NUEVO
```

### Archivos MODIFICADOS de historias previas

| Archivo | Qué cambia |
|---|---|
| `Directory.Packages.props` | Agregar JwtBearer y BCrypt |
| `Api.csproj` | Agregar `<PackageReference>` sin versión |
| `Infrastructure.csproj` | Agregar BCrypt para hashing |
| `AppDbContext.cs` | Agregar `DbSet<User>`, `DbSet<RefreshToken>` y configuraciones en `OnModelCreating` |
| `ApiServiceExtensions.cs` | Llamar a `AddFibradisAuthentication()` y `AddFibradisAuthorization()` |
| `UseApiInfrastructureExtensions.cs` | Agregar `UseAuthentication()` y `UseAuthorization()` en el orden correcto |
| `GlobalExceptionHandler.cs` | Manejar `InvalidCredentialsException` → 401 |
| `Program.cs` | Registrar nuevos endpoints `MapAuth()`, `MapMe()`, `MapOpsPing()` |
| `tests/.../ApiWebFactory.cs` | Agregar configuración JWT y seed de usuarios |

### Dependencias del epic que esta historia establece

- **Historia 1.4** (Hangfire + health checks) no depende de auth directamente, pero sí del pipeline HTTP limpio que esta historia extiende
- **Épicas 2+:** cualquier endpoint privado usará `RequireAuthorization("User")` o `RequireAuthorization("AdminOps")`
- El patrón `MapGroup("/api/v1")` en `AuthEndpoints.cs` es el modelo a seguir para todos los endpoints futuros

### Lo que NO debe hacer esta historia

- No implementar registro de usuarios (FR-37 es Growth, no MVP de esta historia)
- No implementar logout ni revocación manual por usuario (será parte de perfil, épica futura)
- No implementar rate limiting en el endpoint de login (se puede agregar en Historia 1.4 u observabilidad)
- No tocar los endpoints existentes (`/health`, `/openapi`, `/swagger`) — deben seguir siendo públicos
- No agregar UI de autenticación en los SPAs (eso es parte de historias de épicas posteriores)
- No implementar HTTPS Redirect en local dev — ya está en la lógica de cookie (`Secure = !localhost`)

### Aprendizajes de Historia 1.2 aplicados

1. **Verificar versiones en NuGet antes de usar:** Scalar 2.3.4 no existía en NuGet y se usó 2.4.1. Para JwtBearer 10.0.8 verificar disponibilidad antes de declarar.
2. **El path de generación del spec usa el nombre del ensamblado (`Api.json`), no el documento (`v1.json`)** — ya corregido, no tocar.
3. **CORS con `AllowCredentials()` ya está habilitado** — necesario para que las cookies HttpOnly funcionen en desarrollo cross-origin entre los SPAs en :5173/:5174 y la API en :5265.
4. **`ApiWebFactory` usa EF Core InMemory** — mantener ese patrón para los tests de auth.
5. **`public partial class Program { }`** — ya existe al final de Program.cs — no duplicar.

### Verificación final antes de mover a `review`

1. `dotnet build FIBRADIS.slnx` — exit code 0, sin warnings ni errores
2. `dotnet test tests/Integration/Api.Tests/` — todos los tests pasan (existentes + nuevos)
3. `curl -X POST http://localhost:5265/api/v1/auth/login -H "Content-Type: application/json" -d '{"email":"user@test.com","password":"password123"}'` → `{"accessToken":"<jwt>"}`
4. `curl http://localhost:5265/api/v1/me -H "Authorization: Bearer <token-de-usuario>"` → `200`
5. `curl http://localhost:5265/api/v1/ops/ping -H "Authorization: Bearer <token-de-usuario>"` → `403`
6. `curl http://localhost:5265/api/v1/ops/ping -H "Authorization: Bearer <token-de-adminops>"` → `200`
7. `curl http://localhost:5265/api/v1/health` (sin token) → `200`
8. `curl http://localhost:5265/api/v1/me` (sin token) → `401`
9. `dotnet build FIBRADIS.slnx` genera `scripts/codegen/Api.json` con las nuevas rutas de auth
10. `npm run codegen:api` → actualiza `src/Web/SharedApiClient/schema.d.ts` sin errores

---

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6 (create-story, 2026-05-15)

### Debug Log References

### Completion Notes List

### File List

#### Nuevos
- `src/Server/Api/Authentication/AddAuthenticationExtensions.cs`
- `src/Server/Api/Authentication/AddAuthorizationExtensions.cs`
- `src/Server/Api/Endpoints/Public/AuthEndpoints.cs`
- `src/Server/Api/Endpoints/Private/MeEndpoint.cs`
- `src/Server/Api/Endpoints/Ops/OpsEndpoints.cs`
- `src/Server/Application/Auth/IAuthService.cs`
- `src/Server/Application/Auth/AuthService.cs`
- `src/Server/Domain/Auth/User.cs`
- `src/Server/Domain/Auth/UserRole.cs`
- `src/Server/Domain/Auth/RefreshToken.cs`
- `src/Server/Domain/Auth/Exceptions/InvalidCredentialsException.cs`
- `src/Server/Domain/Auth/Exceptions/InvalidRefreshTokenException.cs`
- `src/Server/Infrastructure/Persistence/SqlServer/Configurations/Auth/UserConfiguration.cs`
- `src/Server/Infrastructure/Persistence/SqlServer/Configurations/Auth/RefreshTokenConfiguration.cs`
- `src/Server/Infrastructure/Security/TokenService.cs`
- `src/Server/SharedApiContracts/Auth/LoginRequest.cs`
- `src/Server/SharedApiContracts/Auth/LoginResponse.cs`
- `src/Server/SharedApiContracts/Auth/RefreshResponse.cs`
- `tests/Integration/Api.Tests/AuthLoginTests.cs`
- `tests/Integration/Api.Tests/AuthRefreshTests.cs`
- `tests/Integration/Api.Tests/AuthorizationTests.cs`

#### Modificados
- `Directory.Packages.props`
- `src/Server/Api/Api.csproj`
- `src/Server/Infrastructure/Infrastructure.csproj`
- `src/Server/Api/Program.cs`
- `src/Server/Api/CompositionRoot/ApiServiceExtensions.cs`
- `src/Server/Api/CompositionRoot/UseApiInfrastructureExtensions.cs`
- `src/Server/Api/CompositionRoot/GlobalExceptionHandler.cs`
- `src/Server/Infrastructure/Persistence/SqlServer/AppDbContext.cs`
- `tests/Integration/Api.Tests/ApiWebFactory.cs`

### Change Log

- 2026-05-15: Historia 1.3 creada — Autenticación JWT y autorización por roles (ready-for-dev). (claude-sonnet-4-6)
