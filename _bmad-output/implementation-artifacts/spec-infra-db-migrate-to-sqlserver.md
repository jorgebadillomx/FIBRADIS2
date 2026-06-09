---
title: 'Migrar DB provider: PostgreSQL → SQL Server (con switch por config)'
type: 'chore'
created: '2026-06-08'
status: 'done'
baseline_commit: '1286c2c2b0bc0969bc44d1ecbb65787614a0cdae'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** El proyecto migró a PostgreSQL pero el hosting es IIS + SQL Server. Volver a PostgreSQL en el futuro (Linux/VPS) debe ser una sola línea de configuración.

**Approach:** Cambiar packages, DI y migrations a SQL Server; implementar switch de provider vía `DatabaseProvider` en appsettings; mover migrations actuales de PostgreSQL a `Migrations/PostgreSQL/`; generar migrations nuevas en `Migrations/SqlServer/`; invertir el script `scripts/migrate-data` para copiar datos de PostgreSQL → SQL Server.

## Boundaries & Constraints

**Always:**
- Branch dedicado: `infra/db-migrate-to-sqlserver` desde `main`
- Preservar migraciones PostgreSQL en `Migrations/PostgreSQL/` — no borrar
- Mantener el switch `DatabaseProvider: SqlServer | PostgreSQL` en appsettings para swap futuro
- Base de destino: `LAPBADIS/FIBRADIS_Dev`, Windows Auth + `TrustServerCertificate=True`
- Respeta el orden FK en la migración de datos

**Ask First:**
- Si `dotnet ef migrations add` falla por tipo incompatible en alguna configuración — detener, no forzar

**Never:**
- No sincronización dual en tiempo real
- No tocar lógica de negocio ni endpoints
- No borrar las migraciones de PostgreSQL

</frozen-after-approval>

## Code Map

- `Directory.Packages.props` — cambiar `Npgsql.EntityFrameworkCore.PostgreSQL` → `Microsoft.EntityFrameworkCore.SqlServer 10.0.8`; `Hangfire.PostgreSql` → `Hangfire.SqlServer 1.8.23`
- `src/Server/Infrastructure/Infrastructure.csproj` — mismos package refs
- `src/Server/Api/appsettings.Development.json` — connection string SQL Server; key `DatabaseProvider: SqlServer`
- `src/Server/Api/appsettings.json` — key `DatabaseProvider: SqlServer`
- `src/Server/Api/Program.cs` — `UseNpgsql` → switch por `DatabaseProvider`
- `src/Server/Api/CompositionRoot/ApiServiceExtensions.cs` — `UsePostgreSqlStorage` → `UseSqlServerStorage`; using Hangfire.PostgreSql → Hangfire.SqlServer
- `src/Server/Infrastructure/Migrations/` — mover todo a `Migrations/PostgreSQL/`; generar nuevas en `Migrations/SqlServer/`
- `src/Server/Infrastructure/Persistence/Repositories/Market/MarketRepository.cs` — `PostgresException { SqlState: "23505" }` → `SqlException { Number: 2627 or 2601 }`
- `src/Server/Infrastructure/Persistence/Repositories/Portfolio/PortfolioRepository.cs` — misma corrección
- `src/Server/Infrastructure/Persistence/Repositories/Opportunities/OpportunityWeightsRepository.cs` — misma corrección
- `tests/Unit/Infrastructure.Tests/CatalogModelTests.cs` — `UseNpgsql` → `UseSqlServer`
- `scripts/migrate-data/Program.cs` — invertir: leer de PostgreSQL con NpgsqlCommand, escribir a SQL Server con SqlBulkCopy; completar tabla list con `auth.*`, `catalog.Fibra`, `portfolio.*`

## Tasks & Acceptance

**Execution:**

- [ ] `git stash` en rama actual (story/9-3) para preservar los cambios de planning artifacts; `git checkout main && git checkout -b infra/db-migrate-to-sqlserver`

**Packages:**
- [ ] `Directory.Packages.props` — reemplazar `Npgsql.EntityFrameworkCore.PostgreSQL Version="10.0.2"` con `Microsoft.EntityFrameworkCore.SqlServer Version="10.0.8"`; reemplazar `Hangfire.PostgreSql Version="1.21.1"` con `Hangfire.SqlServer Version="1.8.23"`
- [ ] `src/Server/Infrastructure/Infrastructure.csproj` — reemplazar `Hangfire.PostgreSql` → `Hangfire.SqlServer`; `Npgsql.EntityFrameworkCore.PostgreSQL` → `Microsoft.EntityFrameworkCore.SqlServer`

**Config / DI:**
- [ ] `src/Server/Api/appsettings.Development.json` — connection string: `"Server=LAPBADIS;Database=FIBRADIS_Dev;Trusted_Connection=True;TrustServerCertificate=True"`; agregar `"DatabaseProvider": "SqlServer"`
- [ ] `src/Server/Api/appsettings.json` — agregar `"DatabaseProvider": "SqlServer"` (sin connection string)
- [ ] `src/Server/Api/Program.cs` — reemplazar `UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))` con switch: si `DatabaseProvider == "PostgreSQL"` → `UseNpgsql(connStr)`, else → `UseSqlServer(connStr)`
- [ ] `src/Server/Api/CompositionRoot/ApiServiceExtensions.cs` — reemplazar `using Hangfire.PostgreSql` con `using Hangfire.SqlServer`; reemplazar `UsePostgreSqlStorage(...)` con `UseSqlServerStorage(hangfireConnStr, new SqlServerStorageOptions { SchemaName = "jobs", InvisibilityTimeout = TimeSpan.FromMinutes(5), QueuePollInterval = TimeSpan.FromSeconds(15) })`

**Repositorios — excepción constraint única:**
- [ ] `MarketRepository.cs` — cambiar ambos catch: `when (ex.InnerException is Npgsql.PostgresException { SqlState: "23505" })` → `when (ex.InnerException is Microsoft.Data.SqlClient.SqlException { Number: 2627 or 2601 })`
- [ ] `PortfolioRepository.cs` — misma corrección
- [ ] `OpportunityWeightsRepository.cs` — misma corrección (también ajustar `ConstraintName` check si aplica)

**Migrations:**
- [ ] Crear carpeta `src/Server/Infrastructure/Migrations/PostgreSQL/`; mover todos los archivos existentes en `Migrations/` (incluido `AppDbContextModelSnapshot.cs`) a `Migrations/PostgreSQL/`
- [ ] `dotnet ef migrations add InitialSqlServer --project src/Server/Infrastructure --startup-project src/Server/Api --output-dir Migrations/SqlServer` — genera migraciones limpias para SQL Server
- [ ] `dotnet ef database update --project src/Server/Infrastructure --startup-project src/Server/Api` — aplica sobre `LAPBADIS/FIBRADIS_Dev`

**Tests:**
- [ ] `tests/Unit/Infrastructure.Tests/CatalogModelTests.cs` — `UseNpgsql("Host=localhost;...")` → `UseSqlServer("Server=LAPBADIS;Database=fibradis_model_tests;Trusted_Connection=True;TrustServerCertificate=True")`

**Migración de datos (PostgreSQL → SQL Server):**
- [ ] `scripts/migrate-data/Program.cs` — invertir dirección: leer cada tabla con `NpgsqlCommand` + `NpgsqlDataReader`, escribir con `SqlBulkCopy`; completar lista de tablas agregando (en orden FK): `auth.User`, `auth.RefreshToken`, `catalog.Fibra`, `portfolio.PortfolioPosition`, `portfolio.PortfolioSnapshot`, `portfolio.UserPortfolioSettings`, `portfolio.UserOpportunityWeights`, `portfolio.UserFavorite` (verificar schema exacto con la snapshot de EF Core); truncar con `DELETE FROM [schema].[table]` en SQL Server antes de insertar (SQL Server no tiene `TRUNCATE ... CASCADE`)
- [ ] Ejecutar el script y verificar conteos de registros

**Acceptance Criteria:**
- Given `DatabaseProvider: SqlServer` en appsettings, when `dotnet build FIBRADIS.slnx`, then 0 errores (sin referencias a Npgsql sin resolver)
- Given `LAPBADIS/FIBRADIS_Dev` vacía, when `dotnet ef database update`, then todas las tablas se crean sin error
- Given datos en PostgreSQL, when el script de migración corre, then los conteos de registros coinciden en SQL Server
- Given upsert con key duplicada en cualquier repositorio, when `SaveChangesAsync` lanza `DbUpdateException`, then el catch captura `SqlException` sin NullReferenceException
- Given `dotnet test tests/Unit/Infrastructure.Tests`, then 3/3 pasan

## Spec Change Log

## Design Notes

**Switch de provider en Program.cs:**
```csharp
var provider = builder.Configuration["DatabaseProvider"] ?? "SqlServer";
var connStr = builder.Configuration.GetConnectionString("DefaultConnection")!;
builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (provider == "PostgreSQL") options.UseNpgsql(connStr);
    else options.UseSqlServer(connStr);
});
```

**SqlException duplicate key (SQL Server):** Error 2627 = PK violation; 2601 = unique index violation. El `ConstraintName` de `PostgresException` no tiene equivalente directo en `SqlException`; simplificar a capturar ambos números.

**Migración de datos — dirección inversa:** El script actual (SQL Server → PostgreSQL) usa `NpgsqlBinaryImport`. El nuevo usa `SqlBulkCopy` para SQL Server como destino: más eficiente que INSERTs individuales, maneja tipos automáticamente con `DataTable`.

**Hangfire.SqlServer SchemaName:** Hangfire crea sus propias tablas (`Job`, `State`, `Server`, etc.) en el schema `jobs` — no colisionan con `PipelineRunLog`, `PipelineErrorLog`, `AiCallLog` de la app.

## Verification

**Commands:**
- `dotnet build FIBRADIS.slnx` — expected: 0 errors
- `dotnet ef database update --project src/Server/Infrastructure --startup-project src/Server/Api` — expected: `Done`
- `dotnet test tests/Unit/Infrastructure.Tests` — expected: 3 passed, 0 failed
- `dotnet run --project src/Server/Api` — expected: arranque limpio, sin `NpgsqlException` en logs

## Suggested Review Order

**Punto de entrada — DI/provider swap**

- `UseSqlServer` hardcodeado; comment explica cómo volver a PostgreSQL en el futuro.
  [`Program.cs:18`](../../src/Server/Api/Program.cs#L18)

- Hangfire usa `UseSqlServerStorage` con schema `jobs`; `InvisibilityTimeout` removido (obsoleto).
  [`ApiServiceExtensions.cs:233`](../../src/Server/Api/CompositionRoot/ApiServiceExtensions.cs#L233)

**Paquetes**

- Swap de dependencias centrales: Npgsql → SqlServer, Hangfire.PostgreSql → Hangfire.SqlServer.
  [`Directory.Packages.props:9`](../../Directory.Packages.props#L9)

- csproj Infrastructure refleja el mismo swap.
  [`Infrastructure.csproj:11`](../../src/Server/Infrastructure/Infrastructure.csproj#L11)

**Migraciones SQL Server**

- `DesignTimeDbContextFactory` permite correr `dotnet ef` sin el startup project.
  [`DesignTimeDbContextFactory.cs:1`](../../src/Server/Infrastructure/DesignTimeDbContextFactory.cs#L1)

- Migración inicial SQL Server — `uniqueidentifier`, `NEWID()`, `GETUTCDATE()`, 26 tablas.
  [`InitialSqlServer.cs:1`](../../src/Server/Infrastructure/Migrations/SqlServer/20260609035327_InitialSqlServer.cs#L1)

**Configuraciones EF — funciones SQL Server**

- `NEWID()` y `GETUTCDATE()` como defaults; patrón replicado en 4 archivos más.
  [`AiCallLogConfiguration.cs:16`](../../src/Server/Infrastructure/Persistence/SqlServer/Configurations/Ai/AiCallLogConfiguration.cs#L16)

**Repositorios — manejo de constraint violations**

- `SqlException { Number: 2627 or 2601 }` reemplaza `PostgresException { SqlState: "23505" }`.
  [`MarketRepository.cs:38`](../../src/Server/Infrastructure/Persistence/Repositories/Market/MarketRepository.cs#L38)

- Mismo patrón en PortfolioRepository.
  [`PortfolioRepository.cs:162`](../../src/Server/Infrastructure/Persistence/Repositories/Portfolio/PortfolioRepository.cs#L162)

- Mismo patrón en OpportunityWeightsRepository.
  [`OpportunityWeightsRepository.cs:34`](../../src/Server/Infrastructure/Persistence/Repositories/Opportunities/OpportunityWeightsRepository.cs#L34)

**Script de migración de datos (PostgreSQL → SQL Server)**

- Toda la migración en una `SqlTransaction`; rollback automático si falla cualquier tabla.
  [`migrate-data/Program.cs:49`](../../scripts/migrate-data/Program.cs#L49)

- `DateOnly` convertido a `DateTime` antes de `SqlBulkCopy` (patch de review).
  [`migrate-data/Program.cs:111`](../../scripts/migrate-data/Program.cs#L111)

**Periféricos — tests y config**

- Test de modelo usa `UseSqlServer` apuntando a `LAPBADIS`.
  [`CatalogModelTests.cs:71`](../../tests/Unit/Infrastructure.Tests/CatalogModelTests.cs#L71)

- `DatabaseProvider: SqlServer` como key de config (documentación, no runtime switch).
  [`appsettings.json:2`](../../src/Server/Api/appsettings.json#L2)
