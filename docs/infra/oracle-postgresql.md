# Infraestructura — Oracle Cloud Free Tier + PostgreSQL

**Fecha de decisión:** 2026-06-01  
**Reemplaza:** IIS compartido (Windows) + SQL Server

---

## Decisión

Migrar el hosting de FIBRADIS a **Oracle Cloud Free Tier** con **PostgreSQL** como base de datos.

### Contexto

El diseño original asumía IIS en hosting compartido (Windows) con SQL Server. Esa restricción fue una limitante de costo y no una decisión técnica vinculante. El stack real de FIBRADIS (ASP.NET Core .NET 10, EF Core, Hangfire, React/Vite) es completamente cross-platform; SQL Server es el único componente que ata al ecosistema Windows/licencias.

### Por qué Oracle Cloud Free Tier

- **Always Free permanente** — no es un trial de 90 días. Oracle lo mantiene como estrategia de adquisición de clientes.
- **Datacenter en Querétaro, México** — latencia óptima para usuarios mexicanos.
- **Recursos incluidos en free tier:**
  - 4 cores ARM Ampere A1 + 24 GB RAM + 200 GB storage (una sola VM)
  - 10 TB outbound/mes
  - 1 Load Balancer (10 Mbps)
- **Registro:** requiere tarjeta de crédito para verificación de identidad. No genera cargos si no se activan servicios de pago.

### Por qué PostgreSQL

- Open source, sin licencias, corre nativamente en Linux ARM64.
- EF Core tiene soporte de primera clase vía Npgsql.
- Hangfire tiene provider oficial (`Hangfire.PostgreSql`).
- Permite usar Oracle Cloud Free Tier ARM (SQL Server no tiene build para ARM Linux).

---

## Nuevo stack de infraestructura

```
Oracle Cloud VM (ARM Ampere A1 — 4 cores, 24 GB RAM, Querétaro MX)
├── Ubuntu 24.04 LTS
├── PostgreSQL 16          ← reemplaza SQL Server
├── .NET 10 runtime
├── Nginx                  ← reemplaza IIS (reverse proxy)
├── Certbot / Let's Encrypt ← SSL gratuito y automático
└── systemd                ← gestiona el proceso de la app
```

### Dominios y SSL

- La VM tiene **IP pública fija** asignada por Oracle.
- Cada dominio apunta a esa IP vía registro `A` en el registrador de dominio.
- Certbot genera y renueva certificados HTTPS automáticamente cada 90 días.
- Un solo servidor puede atender múltiples dominios (y proyectos) vía Nginx.

---

## Qué cambia en el código

### Dependencias NuGet

| Proyecto | Antes | Después |
|---|---|---|
| `Infrastructure` | `Microsoft.EntityFrameworkCore.SqlServer` | `Npgsql.EntityFrameworkCore.PostgreSQL` |
| `Infrastructure` | `Hangfire.SqlServer` | `Hangfire.PostgreSql` |

El resto de dependencias (`AngleSharp`, `PdfPig`, `BCrypt.Net-Next`, `YahooQuotesApi`) son cross-platform y no cambian.

### `Program.cs` / `DbContext` configuration

```csharp
// Antes
options.UseSqlServer(connectionString);

// Después
options.UseNpgsql(connectionString);
```

```csharp
// Hangfire — antes
GlobalConfiguration.Configuration.UseSqlServerStorage(connectionString);

// Después
GlobalConfiguration.Configuration.UsePostgreSqlStorage(connectionString);
```

### ⚠️ Gotcha crítico — identificadores y schemas

PostgreSQL trata los identificadores sin comillas como **lowercase**. La convención del proyecto (`PascalCase` en nombres de tabla) requiere atención:

- Npgsql EF Core auto-genera comillas cuando el nombre del identifier no es lowercase.
- **Opción recomendada:** agregar `UseNpgsqlSnakeCaseNamingConvention()` en el `DbContext` y alinear con la convención ya existente de `snake_case` en columnas. Esto convierte nombres de tabla de `PascalCase` → `snake_case` en PostgreSQL (ej. `FibraDetails` → `fibra_details`).
- Los schemas `catalog`, `market`, `news`, `fundamentals`, `portfolio`, `ai`, `jobs` son ya `lowercase` — funcionan sin cambios.
- **Verificar:** cualquier raw SQL o `FromSqlRaw()` usa los nombres de tabla nuevos.

### Migraciones EF Core

Las migraciones existentes fueron generadas para SQL Server. No son compatibles directamente:

1. Eliminar todas las migraciones existentes.
2. Regenerar la migración inicial con el provider de Npgsql.
3. Aplicar en la base de datos PostgreSQL nueva.

> El schema de BD se regenera desde cero; los datos de desarrollo no se migran (la BD de dev está vacía por convención del proyecto).

### Connection string

```
# Antes (SQL Server, Windows Auth)
Server=LAPBADIS;Database=FIBRADIS_Dev;Trusted_Connection=True;

# Después (PostgreSQL)
Host=localhost;Database=fibradis_dev;Username=fibradis_app;Password=<secret>
```

---

## Pasos de migración

### Fase 1 — Servidor (1 día)

El servidor usa Docker + Traefik como modelo de hosting multi-proyecto. Ver `docs/infra/hosting-architecture.md` para el setup completo del servidor (Traefik, red compartida, firewall).

Pasos específicos de FIBRADIS:

1. Completar el setup base del servidor según `hosting-architecture.md`.
2. Crear `/opt/projects/fibradis/` con el `docker-compose.yml` del patrón `.NET + PostgreSQL`.
3. PostgreSQL corre como contenedor dentro del stack — no requiere instalación bare metal.

### Fase 2 — Código (.NET) (3-5 días)

1. Swap NuGet packages (Infrastructure.csproj).
2. Actualizar `Program.cs` — UseNpgsql, Hangfire PostgreSQL storage.
3. Agregar `UseNpgsqlSnakeCaseNamingConvention()` al DbContext.
4. Eliminar migraciones EF existentes.
5. Generar migración inicial nueva: `dotnet ef migrations add InitialPostgres`.
6. Correr todos los tests; ajustar los que dependan de nombres de tabla o raw SQL.
7. Aplicar migración en PostgreSQL de desarrollo local para validar.

### Fase 3 — Deploy inicial (1 día)

1. Crear `Dockerfile` en la raíz del repo:
   ```dockerfile
   FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
   WORKDIR /src
   COPY . .
   RUN dotnet publish src/Server/Api -c Release -o /app

   FROM mcr.microsoft.com/dotnet/aspnet:10.0
   WORKDIR /app
   COPY --from=build /app .
   # SPAs buildadas se copian a wwwroot
   ENTRYPOINT ["dotnet", "Api.dll"]
   ```
2. Buildear y subir imagen:
   ```bash
   docker build -t ghcr.io/tu-usuario/fibradis-api:latest .
   docker push ghcr.io/tu-usuario/fibradis-api:latest
   ```
3. En el servidor:
   ```bash
   cd /opt/projects/fibradis
   docker compose pull && docker compose up -d
   ```
4. Apuntar DNS del registrador a la IP pública de Oracle. Traefik gestiona el SSL automáticamente.
5. Aplicar migración:
   ```bash
   docker compose exec api dotnet ef database update
   ```

### Fase 4 — SPAs (React/Vite)

Los frontends se buildean y se sirven directamente desde ASP.NET Core (igual que antes). El build ocurre dentro del `Dockerfile` (multi-stage) o como paso previo que copia los `dist/` al `wwwroot/` antes de construir la imagen.

---

## Qué NO cambia

- Toda la lógica de aplicación (Domain, Application, Api layers).
- Hangfire jobs — solo el provider de storage cambia.
- Los schemas de datos (`catalog`, `market`, `news`, etc.) — PostgreSQL los soporta nativamente.
- Los frontends React/Vite — son static files, independientes del OS.
- El contrato OpenAPI y el `SharedApiClient` generado.
- La arquitectura modular del monolito.
- Auth JWT — funciona igual en Linux.

---

## Capacidad del servidor para proyectos adicionales

Con 24 GB de RAM disponibles, FIBRADIS usa aproximadamente:

| Componente | RAM estimada |
|---|---|
| ASP.NET Core + Hangfire | ~500 MB |
| PostgreSQL (todas las DBs) | ~300 MB |
| Nginx | ~30 MB |
| **Total FIBRADIS** | **~830 MB** |
| **Disponible para otros proyectos** | **~23 GB** |

PostgreSQL maneja múltiples bases de datos en una sola instancia. Cada proyecto adicional agrega su propia DB con sus propias credenciales, aislado dentro del mismo proceso PostgreSQL.

---

## Riesgos y mitigaciones

| Riesgo | Mitigación |
|---|---|
| Oracle cambia términos del free tier | Arquitectura portable — Nginx + systemd + PostgreSQL corren en cualquier VPS Linux |
| ARM64 incompatibilidad en dependencias | Verificar builds ARM64 de PdfPig, YahooQuotesApi y AngleSharp antes de Fase 2 |
| Diferencias de comportamiento SQL Server → PostgreSQL | Tests de integración con PostgreSQL real desde el inicio de Fase 2 |
| Naming convention de tablas rompe queries existentes | Auditar `FromSqlRaw()` y cualquier raw SQL antes de deploy |

---

## Referencias

- EF Core + Npgsql: documentación oficial de `Npgsql.EntityFrameworkCore.PostgreSQL`
- Hangfire PostgreSQL: `Hangfire.PostgreSql` en NuGet
- Oracle Always Free: `cloud.oracle.com` → Free Tier
- Certbot: `certbot.eff.org`
