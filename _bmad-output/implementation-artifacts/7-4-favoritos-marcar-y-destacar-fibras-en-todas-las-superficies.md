# Historia 7.4: Favoritos — marcar y destacar FIBRAs en todas las superficies

Status: done

## Story

Como usuario,
quiero marcar cualquier FIBRA como favorita usando un ícono de estrella desde su perfil público (M5), mi portafolio (M8) u Oportunidades (M9), y ver mis favoritos destacados al inicio de las tablas en M8 y M9,
para que pueda acceder rápidamente a las FIBRAs que sigo más de cerca sin filtrado manual.

## Acceptance Criteria

### AC1 — Marcar favorita desde perfil público (M5)

**Dado que** estoy autenticado y hago clic en el ícono de estrella del perfil público de FUNO11,
**Entonces** FUNO11 se marca como favorita (la estrella se rellena inmediatamente — optimistic update), la preferencia se persiste en mi cuenta y el cambio es visible de forma reactiva en M5 (estrella rellena), M8 (FUNO11 al inicio de posiciones) y M9 (FUNO11 al inicio del ranking).

### AC2 — Favoritas al inicio de la tabla de portafolio (M8)

**Dado que** veo mi portafolio (M8) y tengo FUNO11 marcada como favorita,
**Entonces** FUNO11 aparece al inicio de la tabla de posiciones con un ícono de estrella visible, visualmente separada del resto de posiciones.

### AC3 — Favoritas al inicio del ranking de oportunidades (M9)

**Dado que** veo Oportunidades (M9) con FUNO11 como favorita,
**Entonces** FUNO11 aparece al inicio de la tabla del ranking del universo, antes que las FIBRAs no favoritas independientemente del orden de score.

### AC4 — Quitar favorita desde cualquier superficie

**Dado que** hago clic en la estrella rellena de FUNO11 en cualquier superficie,
**Entonces** FUNO11 regresa inmediatamente a su posición sin destacar en las tablas de M8 y M9, y el cambio persiste al recargar la página.

### AC5 — Sin favoritos: orden predeterminado sin encabezado

**Dado que** no tengo ningún favorito marcado,
**Entonces** las tablas de M8 y M9 muestran todas las posiciones/FIBRAs en su orden predeterminado sin encabezado de sección "Favoritas" ni toggle "Favoritas primero" activado.

## Tasks / Subtasks

### T1 — Dominio y repositorio (AC: 1, 4)

- [x] T1.1 — Crear `src/Server/Domain/Portfolio/UserFavorite.cs`:
  ```csharp
  namespace Domain.Portfolio;

  public class UserFavorite
  {
      public Guid UserId { get; set; }
      public Guid FibraId { get; set; }
      public DateTimeOffset AddedAt { get; set; }
  }
  ```

- [x] T1.2 — Crear `src/Server/Application/Portfolio/IUserFavoritesRepository.cs`:
  ```csharp
  namespace Application.Portfolio;

  public interface IUserFavoritesRepository
  {
      Task<IReadOnlyList<Guid>> GetFavoriteIdsAsync(Guid userId, CancellationToken ct);
      Task AddAsync(Guid userId, Guid fibraId, CancellationToken ct);
      Task RemoveAsync(Guid userId, Guid fibraId, CancellationToken ct);
  }
  ```

### T2 — Infraestructura: EF Core + migración (AC: 1, 4)

- [x] T2.1 — Agregar `DbSet<UserFavorite> UserFavorites` a `AppDbContext.cs`. Buscar el archivo en `src/Server/Infrastructure/Persistence/SqlServer/AppDbContext.cs` y agregar la propiedad junto a los otros DbSets del schema `portfolio`.

- [x] T2.2 — Agregar configuración EF en `OnModelCreating` (o crear `UserFavoriteConfiguration.cs` si el proyecto usa `IEntityTypeConfiguration<T>`):
  ```csharp
  modelBuilder.Entity<UserFavorite>(b =>
  {
      b.ToTable("UserFavorites", "portfolio");
      b.HasKey(e => new { e.UserId, e.FibraId });
      b.Property(e => e.UserId).HasColumnName("user_id");
      b.Property(e => e.FibraId).HasColumnName("fibra_id");
      b.Property(e => e.AddedAt).HasColumnName("added_at");
  });
  ```

- [x] T2.3 — Crear `src/Server/Infrastructure/Persistence/Repositories/Portfolio/UserFavoritesRepository.cs`:
  ```csharp
  using Application.Portfolio;
  using Domain.Portfolio;
  using Infrastructure.Persistence.SqlServer;
  using Microsoft.EntityFrameworkCore;

  namespace Infrastructure.Persistence.Repositories.Portfolio;

  public class UserFavoritesRepository(AppDbContext db) : IUserFavoritesRepository
  {
      public async Task<IReadOnlyList<Guid>> GetFavoriteIdsAsync(Guid userId, CancellationToken ct)
          => await db.UserFavorites
              .Where(f => f.UserId == userId)
              .Select(f => f.FibraId)
              .ToListAsync(ct);

      public async Task AddAsync(Guid userId, Guid fibraId, CancellationToken ct)
      {
          var exists = await db.UserFavorites
              .AnyAsync(f => f.UserId == userId && f.FibraId == fibraId, ct);
          if (exists) return;
          db.UserFavorites.Add(new UserFavorite
          {
              UserId = userId,
              FibraId = fibraId,
              AddedAt = DateTimeOffset.UtcNow,
          });
          await db.SaveChangesAsync(ct);
      }

      public async Task RemoveAsync(Guid userId, Guid fibraId, CancellationToken ct)
      {
          await db.UserFavorites
              .Where(f => f.UserId == userId && f.FibraId == fibraId)
              .ExecuteDeleteAsync(ct);
      }
  }
  ```

- [x] T2.4 — Registrar `IUserFavoritesRepository` → `UserFavoritesRepository` en DI. Buscar en `src/Server/Infrastructure/DependencyInjection.cs` (o donde se registran los otros repositorios del módulo Portfolio) y agregar la línea análoga a las demás.

- [x] T2.5 — Generar migración EF y aplicar:
  ```bash
  dotnet ef migrations add AddUserFavoritesTable --project src/Server/Infrastructure --startup-project src/Server/Api
  dotnet ef database update --project src/Server/Infrastructure --startup-project src/Server/Api
  ```

### T3 — API Endpoints (AC: 1, 4)

- [x] T3.1 — Crear `src/Server/SharedApiContracts/Portfolio/UserFavoritesDto.cs`:
  ```csharp
  namespace SharedApiContracts.Portfolio;

  public record UserFavoritesDto(IReadOnlyList<Guid> FibraIds);
  ```

- [x] T3.2 — Crear `src/Server/Api/Endpoints/Private/FavoriteEndpoints.cs`:
  ```csharp
  using System.Security.Claims;
  using Application.Portfolio;
  using SharedApiContracts.Portfolio;

  namespace Api.Endpoints.Private;

  public static class FavoriteEndpoints
  {
      public static IEndpointRouteBuilder MapFavorites(this IEndpointRouteBuilder app)
      {
          var group = app.MapGroup("/api/v1/portfolio/favorites")
              .RequireAuthorization("User")
              .WithTags("Favorites");

          group.MapGet("/", async (
              IUserFavoritesRepository repo,
              HttpContext ctx,
              CancellationToken ct) =>
          {
              var userId = GetUserId(ctx);
              var ids = await repo.GetFavoriteIdsAsync(userId, ct);
              return Results.Ok(new UserFavoritesDto(ids));
          })
          .Produces<UserFavoritesDto>(StatusCodes.Status200OK)
          .ProducesProblem(StatusCodes.Status401Unauthorized);

          group.MapPut("/{fibraId:guid}", async (
              Guid fibraId,
              IUserFavoritesRepository repo,
              HttpContext ctx,
              CancellationToken ct) =>
          {
              var userId = GetUserId(ctx);
              await repo.AddAsync(userId, fibraId, ct);
              return Results.NoContent();
          })
          .Produces(StatusCodes.Status204NoContent)
          .ProducesProblem(StatusCodes.Status401Unauthorized);

          group.MapDelete("/{fibraId:guid}", async (
              Guid fibraId,
              IUserFavoritesRepository repo,
              HttpContext ctx,
              CancellationToken ct) =>
          {
              var userId = GetUserId(ctx);
              await repo.RemoveAsync(userId, fibraId, ct);
              return Results.NoContent();
          })
          .Produces(StatusCodes.Status204NoContent)
          .ProducesProblem(StatusCodes.Status401Unauthorized);

          return app;
      }

      private static Guid GetUserId(HttpContext ctx) =>
          Guid.Parse(ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
  }
  ```

- [x] T3.3 — Registrar `FavoriteEndpoints` en `Program.cs` con `app.MapFavorites()`, siguiendo el mismo patrón que `app.MapOpportunities()` y `app.MapPortfolio()`.

### T4 — Codegen cliente API tipado

- [x] T4.1 — Compilar backend y regenerar cliente tipado:
  ```bash
  dotnet build FIBRADIS.slnx
  npm run codegen:api
  ```
  Verificar que `src/Web/SharedApiClient/schema.d.ts` incluye los nuevos endpoints `/api/v1/portfolio/favorites`.

### T5 — Hook `useFavorites` y componente `StarButton` (Frontend)

- [x] T5.1 — Crear `src/Web/Main/src/modules/oportunidades/useFavorites.ts`:
  ```ts
  import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
  import { apiClient } from '@/api/fibrasApi'
  import { useAuth } from '@/modules/auth/AuthContext'

  export function useFavorites() {
    const { isAuthenticated } = useAuth()
    const qc = useQueryClient()

    const favoritesQuery = useQuery({
      queryKey: ['favorites'],
      queryFn: async () => {
        const { data, error } = await apiClient.GET('/api/v1/portfolio/favorites', {})
        if (error || !data) throw new Error('No se pudieron cargar los favoritos.')
        return (data.fibraIds ?? []) as string[]
      },
      enabled: isAuthenticated,
      staleTime: 60_000,
    })

    const toggleMutation = useMutation({
      mutationFn: async ({ fibraId, removing }: { fibraId: string; removing: boolean }) => {
        if (removing) {
          const { error } = await apiClient.DELETE('/api/v1/portfolio/favorites/{fibraId}', {
            params: { path: { fibraId } },
          })
          if (error) throw error
        } else {
          const { error } = await apiClient.PUT('/api/v1/portfolio/favorites/{fibraId}', {
            params: { path: { fibraId } },
          })
          if (error) throw error
        }
      },
      onMutate: async ({ fibraId, removing }) => {
        await qc.cancelQueries({ queryKey: ['favorites'] })
        const prev = qc.getQueryData<string[]>(['favorites']) ?? []
        const next = removing ? prev.filter(id => id !== fibraId) : [...prev, fibraId]
        qc.setQueryData(['favorites'], next)
        return { prev }
      },
      onError: (_err, _vars, ctx) => {
        if (ctx?.prev != null) qc.setQueryData(['favorites'], ctx.prev)
      },
      onSettled: () => qc.invalidateQueries({ queryKey: ['favorites'] }),
    })

    const favoriteIds = new Set(favoritesQuery.data ?? [])

    return {
      favoriteIds,
      isLoading: favoritesQuery.isLoading,
      toggle: (fibraId: string) =>
        toggleMutation.mutate({ fibraId, removing: favoriteIds.has(fibraId) }),
    }
  }
  ```

- [x] T5.2 — Crear `src/Web/Main/src/modules/oportunidades/StarButton.tsx`:
  ```tsx
  import { Star } from 'lucide-react'

  interface StarButtonProps {
    fibraId: string
    isFavorite: boolean
    onToggle: (fibraId: string) => void
    size?: number
  }

  export function StarButton({ fibraId, isFavorite, onToggle, size = 18 }: StarButtonProps) {
    return (
      <button
        type="button"
        aria-pressed={isFavorite}
        aria-label={isFavorite ? 'Quitar de favoritas' : 'Marcar como favorita'}
        onClick={(e) => {
          e.stopPropagation()
          onToggle(fibraId)
        }}
        className="rounded p-1 transition-colors hover:bg-muted/60"
      >
        <Star
          size={size}
          className={isFavorite ? 'fill-yellow-400 text-yellow-400' : 'text-muted-foreground'}
        />
      </button>
    )
  }
  ```

### T6 — Ficha pública M5 (AC: 1)

- [x] T6.1 — Modificar `src/Web/Main/src/modules/ficha-publica/FibraPage.tsx`:
  - Importar `useFavorites` y `StarButton` desde `@/modules/oportunidades/...`
  - Importar `useAuth` desde `@/modules/auth/AuthContext`
  - Llamar hooks al inicio del componente:
    ```tsx
    const { isAuthenticated } = useAuth()
    const { favoriteIds, toggle } = useFavorites()
    ```
  - Localizar el sticky header (donde aparece el ticker y nombre de la FIBRA) y agregar:
    ```tsx
    {isAuthenticated && fibra != null && (
      <StarButton
        fibraId={fibra.id}
        isFavorite={favoriteIds.has(fibra.id)}
        onToggle={toggle}
        size={22}
      />
    )}
    ```
  - La estrella se omite completamente para usuarios no autenticados (no mostrar disabled, simplemente no renderizar).
  - Posición en el header: junto al ticker/nombre, en la misma línea del `h1` o inmediatamente a la derecha del nombre.

### T7 — Oportunidades M9 (AC: 3, 4, 5)

- [x] T7.1 — Modificar `src/Web/Main/src/modules/oportunidades/OportunidadesPage.tsx`:
  - Importar `useFavorites`, `StarButton` (desde los archivos del T5)
  - Agregar al componente `OportunidadesPage`:
    ```tsx
    const { favoriteIds, toggle } = useFavorites()
    const [favoritasFirst, setFavoritasFirst] = useState(false)
    ```
  - Pasar `favoriteIds`, `toggle`, `favoritasFirst` a `<RankingTable>` como nuevas props.
  - Agregar toggle "Favoritas primero" en el área del configurador de pesos o cerca de la cabecera del ranking (ver Dev Notes para ubicación y estilo).

- [x] T7.2 — Actualizar `RankingTable` dentro de `OportunidadesPage.tsx`:
  - Extender props: `favoriteIds: Set<string>`, `onToggleFavorite: (id: string) => void`, `favoritasFirst: boolean`
  - Actualizar el `useMemo` de sorting para respetar `favoritasFirst`:
    ```ts
    const sorted = useMemo(() => {
      const scored = [...rows].map(r => ({ ...r, _score: calcLocalScore(r, weights) }))
      scored.sort((a, b) => {
        if (favoritasFirst) {
          const af = favoriteIds.has(a.fibraId) ? 0 : 1
          const bf = favoriteIds.has(b.fibraId) ? 0 : 1
          if (af !== bf) return af - bf
        }
        return b._score - a._score
      })
      return scored
    }, [rows, weights, favoriteIds, favoritasFirst])
    ```
  - Agregar columna ★ en el `<thead>` (columna angosta, antes o después del chevron).
  - En cada `<tr>` de fila, agregar `<StarButton>` en la celda ★.
  - Separador visual después de la última favorita cuando `favoritasFirst && favoriteIds.size > 0` (ver Dev Notes para el JSX).

- [x] T7.3 — Toggle "Favoritas primero" en OportunidadesPage:
  ```tsx
  <button
    type="button"
    onClick={() => setFavoritasFirst(v => !v)}
    className={`flex items-center gap-1.5 rounded-md border px-3 py-1.5 text-sm transition-colors ${
      favoritasFirst
        ? 'border-primary bg-primary/10 text-primary'
        : 'border-input text-muted-foreground hover:text-foreground'
    }`}
  >
    <Star size={14} className={favoritasFirst ? 'fill-primary text-primary' : ''} />
    Favoritas primero
  </button>
  ```
  Posicionar en la barra de controles que aparece entre el configurador de pesos y la tabla de ranking.

### T8 — Portafolio M8 (AC: 2, 4, 5)

- [x] T8.1 — Leer `src/Web/Main/src/modules/portafolio/PortafolioPage.tsx` y `PositionsTable.tsx` (si existe como componente separado) para identificar la estructura de la tabla y el número de columnas antes de modificar.

- [x] T8.2 — Modificar el componente de tabla de posiciones del portafolio (ya sea `PortafolioPage.tsx` directamente o `PositionsTable.tsx`):
  - Importar `useFavorites`, `StarButton`
  - Llamar `useFavorites()` en el componente que contiene la tabla
  - Agregar estado `favoritasFirst: boolean = false`
  - Agregar columna ★ en el header y `<StarButton>` en cada fila
  - Ordenar posiciones: si `favoritasFirst`, las filas con `favoriteIds.has(pos.fibraId)` van al inicio; dentro de cada grupo, mantener el orden original de la respuesta del servidor
  - Toggle "Favoritas primero ★" en la barra de controles (junto a "Columnas", "Multi-sort"):
    ```tsx
    <button
      type="button"
      onClick={() => setFavoritasFirst(v => !v)}
      className={`flex items-center gap-1.5 rounded-md border px-3 py-1.5 text-sm transition-colors ${
        favoritasFirst
          ? 'border-primary bg-primary/10 text-primary'
          : 'border-input text-muted-foreground hover:text-foreground'
      }`}
    >
      <Star size={14} className={favoritasFirst ? 'fill-primary text-primary' : ''} />
      Favoritas primero
    </button>
    ```
  - Separador visual entre última favorita y primera no-favorita (mismo patrón que T7.2).

- [x] T8.3 — Agregar KPI de favoritas en la fila de KPIs de PortafolioPage (si hay espacio y posiciones favoritas marcadas):
  - Calcular `favoritasEnPortafolio = positions.filter(p => favoriteIds.has(p.fibraId)).length`
  - Mostrar como KPI simple "Favoritas ★: N" — solo visible cuando N > 0.

### T9 — Unit tests del repositorio (AC: 1, 4)

- [x] T9.1 — Crear `tests/Unit/Application.Tests/Portfolio/UserFavoritesRepositoryTests.cs` usando EF InMemory (mismo patrón que otros tests en `Application.Tests`):

  ```csharp
  using Domain.Portfolio;
  using Infrastructure.Persistence.Repositories.Portfolio;
  using Infrastructure.Persistence.SqlServer;
  using Microsoft.EntityFrameworkCore;

  namespace Application.Tests.Portfolio;

  public class UserFavoritesRepositoryTests
  {
      private static AppDbContext CreateDb() =>
          new(new DbContextOptionsBuilder<AppDbContext>()
              .UseInMemoryDatabase(Guid.NewGuid().ToString())
              .Options);

      [Fact]
      public async Task GetFavoriteIds_Empty_ReturnsEmptyList()
      {
          using var db = CreateDb();
          var repo = new UserFavoritesRepository(db);
          var result = await repo.GetFavoriteIdsAsync(Guid.NewGuid(), default);
          Assert.Empty(result);
      }

      [Fact]
      public async Task Add_AddsToList()
      {
          using var db = CreateDb();
          var repo = new UserFavoritesRepository(db);
          var userId = Guid.NewGuid();
          var fibraId = Guid.NewGuid();
          await repo.AddAsync(userId, fibraId, default);
          var result = await repo.GetFavoriteIdsAsync(userId, default);
          Assert.Contains(fibraId, result);
      }

      [Fact]
      public async Task Add_Idempotent_DoesNotThrow()
      {
          using var db = CreateDb();
          var repo = new UserFavoritesRepository(db);
          var userId = Guid.NewGuid();
          var fibraId = Guid.NewGuid();
          await repo.AddAsync(userId, fibraId, default);
          await repo.AddAsync(userId, fibraId, default); // no exception
          var result = await repo.GetFavoriteIdsAsync(userId, default);
          Assert.Single(result);
      }

      [Fact]
      public async Task Remove_RemovesFromList()
      {
          using var db = CreateDb();
          var repo = new UserFavoritesRepository(db);
          var userId = Guid.NewGuid();
          var fibraId = Guid.NewGuid();
          await repo.AddAsync(userId, fibraId, default);
          await repo.RemoveAsync(userId, fibraId, default);
          var result = await repo.GetFavoriteIdsAsync(userId, default);
          Assert.Empty(result);
      }

      [Fact]
      public async Task Remove_Idempotent_DoesNotThrow()
      {
          using var db = CreateDb();
          var repo = new UserFavoritesRepository(db);
          var userId = Guid.NewGuid();
          var fibraId = Guid.NewGuid();
          await repo.RemoveAsync(userId, fibraId, default); // no exception when not found
      }

      [Fact]
      public async Task GetFavoriteIds_ReturnsOnlyForUserId()
      {
          using var db = CreateDb();
          var repo = new UserFavoritesRepository(db);
          var userA = Guid.NewGuid();
          var userB = Guid.NewGuid();
          var fibraId = Guid.NewGuid();
          await repo.AddAsync(userA, fibraId, default);
          var result = await repo.GetFavoriteIdsAsync(userB, default);
          Assert.Empty(result);
      }
  }
  ```

  **Nota EF InMemory**: `ExecuteDeleteAsync` no está soportado por el provider InMemory de EF Core. Si el test de `Remove` falla por esto, reemplazar en `UserFavoritesRepository` el `ExecuteDeleteAsync` por el patrón load-and-remove:
  ```csharp
  public async Task RemoveAsync(Guid userId, Guid fibraId, CancellationToken ct)
  {
      var entity = await db.UserFavorites
          .FirstOrDefaultAsync(f => f.UserId == userId && f.FibraId == fibraId, ct);
      if (entity is null) return;
      db.UserFavorites.Remove(entity);
      await db.SaveChangesAsync(ct);
  }
  ```
  Esta versión funciona en InMemory Y en SQL Server. Usar esta implementación directamente para evitar el problema.

### T10 — Build y validación final

- [x] T10.1 — `dotnet build FIBRADIS.slnx` — 0 errores
- [x] T10.2 — `dotnet test tests/Unit/` — todos los tests pasan (incluye los 6 nuevos de T9)
- [x] T10.3 — `npm run build --workspace=src/Web/Main` — 0 errores TypeScript

## Dev Notes

### Contexto del branch — estado del working tree

Este branch `story/7-4-favoritos` fue creado desde `story/7-3-monitoreo-cobertura` mientras los cambios de la historia 7-3 estaban sin commitear. El working tree ya contiene los cambios de 7-3:
- `OportunidadesPage.tsx` tiene los banners de degradación/suspensión del universo
- `OpportunityRankingResponseDto.cs` tiene el campo `Coverage`
- `AppDbContextModelSnapshot.cs` tiene la migración de `UniverseDegradationThresholdPct`

**Esto es normal** — trabaja sobre el estado actual del working tree, que incluye todo lo anterior. Los cambios de 7-4 se acumularán encima.

### Esquema de la tabla nueva

**Tabla:** `portfolio.UserFavorites`

| Columna | Tipo SQL Server | Constraints |
|---|---|---|
| `user_id` | `uniqueidentifier` | PK parte 1 |
| `fibra_id` | `uniqueidentifier` | PK parte 2 |
| `added_at` | `datetimeoffset` | NOT NULL |

- PK compuesta `(user_id, fibra_id)` garantiza unicidad: un usuario no puede tener el mismo fibraId dos veces.
- No se declaran FKs explícitas — siguiendo el patrón del módulo Portfolio que tampoco declara FKs (ver `PortfolioPositions` y `UserOpportunityWeights` en el snapshot).
- Schema `portfolio` (mismo que `PortfolioPositions`, `UserOpportunityWeights`, `UserPortfolioSettings`).

### Patrón de endpoints en el proyecto

Todos los endpoints privados siguen el patrón de `OpportunityEndpoints.cs`:
- `static class` con método de extensión `MapXxx(this IEndpointRouteBuilder app)`
- `MapGroup(ruta).RequireAuthorization("User").WithTags("...")`
- `GetUserId(ctx)` vía `Guid.Parse(ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)!)`
- Registrados en `Program.cs` con `app.MapXxx()`

Ver `PortfolioEndpoints.cs` para la estructura exacta. El patrón `MapFavorites` es idéntico.

### Estrategia de caché en el frontend — fuente única de verdad

El hook `useFavorites()` usa `queryKey: ['favorites']` como fuente única. Los tres módulos (M5, M8, M9) llaman al mismo hook y comparten el caché de React Query — **no hay triple fetch**.

**Optimistic update:**
1. `onMutate`: actualiza el caché `['favorites']` inmediatamente antes de la petición al servidor
2. `onError`: revierte al estado previo guardado en `ctx.prev`
3. `onSettled`: invalida el query para sincronizar con el servidor

Este patrón es idéntico al usado en `OportunidadesPage.tsx` (ver `saveWeightsMutation`).

### Separador visual "Favoritas primero"

Cuando `favoritasFirst = true` y hay al menos una favorita, mostrar un borde doble sutil entre la última favorita y la primera no-favorita:

```tsx
{sorted.map((row, idx) => {
  const isFav = favoriteIds.has(row.fibraId)
  const nextIsFav = idx + 1 < sorted.length && favoriteIds.has(sorted[idx + 1].fibraId)
  const isLastFav = isFav && !nextIsFav && sorted.some(r => !favoriteIds.has(r.fibraId))

  return (
    <Fragment key={row.fibraId}>
      <tr className={`... ${isLastFav && favoritasFirst ? 'border-b-2 border-primary/30' : ''}`}>
        ...
      </tr>
    </Fragment>
  )
})}
```

No se necesita un `<thead>` secundario ni etiqueta "Favoritas" — la separación visual y el ícono relleno son suficientes.

### StarButton — e.stopPropagation() obligatorio

Las tablas tienen filas clicables (`onClick={() => toggle(row.fibraId)}` para expandir). El `StarButton` debe usar `e.stopPropagation()` para que el clic en la estrella no expanda/colapse la fila al mismo tiempo.

### Ubicación de `useFavorites` y `StarButton` en el árbol de archivos

Ambos archivos se crean en `src/modules/oportunidades/` (junto a `OportunidadesPage.tsx`, `PromediarTab.tsx`, etc.). Los módulos `ficha-publica` y `portafolio` importarán desde esa ubicación usando el alias `@/modules/oportunidades/...`. Esta es la solución más simple para el MVP — no crear carpeta `shared/` por dos archivos.

### AuthContext en M5

`FibraPage.tsx` es una ruta **pública** — puede abrirse sin autenticar. La estrella solo se renderiza si `isAuthenticated`. La forma de obtener `isAuthenticated` es la misma que `PortafolioPage.tsx` ya usa: `const { isAuthenticated } = useAuth()` importado de `@/modules/auth/AuthContext`.

El hook `useFavorites()` ya tiene `enabled: isAuthenticated` — si el usuario no está autenticado, el query no se ejecuta y `favoriteIds` es un `Set` vacío.

### EF InMemory — `ExecuteDeleteAsync` no soportado

El provider `UseInMemoryDatabase` de EF Core no implementa `ExecuteDeleteAsync`. Usar el patrón load-and-remove en `UserFavoritesRepository.RemoveAsync` (ver código en T2.3 — la implementación incluye esta decisión). Esta versión funciona tanto en InMemory para tests como en SQL Server en producción.

### Migración EF — orden de comandos

Usar siempre `--startup-project src/Server/Api` porque el proyecto Infrastructure no tiene `appsettings.json` por sí solo:
```bash
dotnet ef migrations add AddUserFavoritesTable --project src/Server/Infrastructure --startup-project src/Server/Api
dotnet ef database update --project src/Server/Infrastructure --startup-project src/Server/Api
```

### Convenciones de código

- TypeScript strict — no `any`, no `!` innecesarios
- `StarButton`: `type="button"` explícito, `aria-pressed` para accesibilidad (requerimiento UX spec)
- Íconos: `lucide-react` (`Star`) — ya importado en `OportunidadesPage.tsx`
- No usar `console.log` en código de producción
- Columna nueva en tablas: `<th className="w-10 px-3 py-2" />` (ancho fijo pequeño)

### UX — comportamiento preciso de la estrella (de la especificación)

- Estrella no marcada: `Star` con `className="text-muted-foreground"` (solo contorno)
- Estrella marcada: `Star` con `className="fill-yellow-400 text-yellow-400"` (rellena en amarillo)
- Target de toque mínimo: el `<button>` wrapper con `p-1` da ~44px efectivos en pantallas táctiles
- Cambio visual inmediato (optimistic) — no esperar respuesta del servidor para cambiar el ícono

### Reglas críticas que NO violar

1. No escribir en el schema `portfolio` desde otro módulo — `FavoriteEndpoints` pertenece al módulo Portfolio.
2. No duplicar datos: `UserFavorites` solo almacena la relación usuario–fibra; la información de la fibra se obtiene del módulo Catalog.
3. OpenAPI es la fuente de tipos — siempre regenerar `schema.d.ts` después de cambios en el backend antes de escribir código frontend.

### Historial relevante de historias anteriores

- **7-1** (done): Estableció `OpportunityScoreCalculator`, `UserOpportunityWeights`, `OpportunityEndpoints`. Patrón de referencia para endpoints privados con pesos por usuario.
- **7-2** (done): Añadió tabs a `OportunidadesPage`, `PromediarTab`, `simulador-logic.ts` con tests. El `calcLocalScore` y `ScoreBadge` están en `OportunidadesPage.tsx` — importar desde ahí en lugar de duplicar.
- **7-3** (en review): Agregó `UniverseCoverageCalculator`, banners de degradación/suspensión en `OportunidadesPage`, campo `UniverseDegradationThresholdPct` en `OperationalConfig`. Estos cambios ya están en el working tree de este branch.

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

### Completion Notes List

- Implementé `portfolio.UserFavorites` con entidad, configuración EF, repositorio idempotente y endpoints privados `GET/PUT/DELETE` para favoritos.
- Añadí `useFavorites()` con optimistic update compartido, `StarButton`, estrella en M5 y orden/filtro visual por favoritos en M8 y M9.
- Generé la migración `AddUserFavoritesTable` y regeneré el cliente tipado OpenAPI para exponer `/api/v1/portfolio/favorites`.
- Validación ejecutada: `dotnet build FIBRADIS.slnx`, `dotnet test tests/Unit/Application.Tests/Application.Tests.csproj`, `dotnet test tests/Unit/Infrastructure.Tests/Infrastructure.Tests.csproj`, `dotnet test tests/Unit/Domain.Tests/Domain.Tests.csproj`, `npm run build --workspace=src/Web/Main`, `npm test --workspace=src/Web/Main`.
- `dotnet test FIBRADIS.slnx --no-build` dejó 5 fallos de integración preexistentes fuera del alcance de esta historia, todos relacionados con aserciones de email en `Api.Tests` y no con favoritos.

### File List

**Nuevos:**
- src/Server/Domain/Portfolio/UserFavorite.cs
- src/Server/Application/Portfolio/IUserFavoritesRepository.cs
- src/Server/Infrastructure/Persistence/SqlServer/Configurations/Portfolio/UserFavoriteConfiguration.cs
- src/Server/Infrastructure/Persistence/Repositories/Portfolio/UserFavoritesRepository.cs
- src/Server/SharedApiContracts/Portfolio/UserFavoritesDto.cs
- src/Server/Api/Endpoints/Private/FavoriteEndpoints.cs
- src/Server/Infrastructure/Migrations/AddUserFavoritesTable.cs (generado por EF)
- src/Server/Infrastructure/Migrations/AddUserFavoritesTable.Designer.cs (generado por EF)
- src/Web/Main/src/modules/oportunidades/useFavorites.ts
- src/Web/Main/src/modules/oportunidades/StarButton.tsx
- tests/Unit/Infrastructure.Tests/Portfolio/UserFavoritesRepositoryTests.cs

**Modificados:**
- src/Server/Infrastructure/Persistence/SqlServer/AppDbContext.cs (DbSet + OnModelCreating)
- src/Server/Api/CompositionRoot/ApiServiceExtensions.cs (registro DI)
- src/Server/Infrastructure/Migrations/AppDbContextModelSnapshot.cs (actualizado por EF)
- src/Server/Api/Program.cs (registrar MapFavorites)
- scripts/codegen/Api.json (regenerado)
- src/Web/SharedApiClient/schema.d.ts (regenerado)
- src/Web/Main/src/modules/auth/AuthContext.tsx (exponer isAuthenticated)
- src/Web/Main/src/modules/ficha-publica/FibraPage.tsx (star en header)
- src/Web/Main/src/modules/oportunidades/OportunidadesPage.tsx (useFavorites, toggle, favoritasFirst)
- src/Web/Main/src/modules/portafolio/PortafolioPage.tsx (useFavorites, toggle, favoritasFirst, KPI)
- src/Web/Main/src/modules/portafolio/PositionsTable.tsx (estrella, orden por favoritos, separador visual)

### Change Log

- 2026-06-05: Implementación completa de favoritos para M5/M8/M9, con persistencia en `portfolio.UserFavorites`, contrato OpenAPI actualizado, hook compartido y UI reactiva con optimistic update.
- 2026-06-05: Generé la migración EF `AddUserFavoritesTable`, la regeneración del cliente tipado y la validación de builds/tests locales.

### Review Findings

#### Decision Needed

- [x] **\[Review\]\[Decision\]** AC3 — ¿Favoritas en M9 elevadas por defecto o solo con toggle? — **Resuelto: toggle opt-in (favoritasFirst = false por defecto). AC3 se interpreta como comportamiento cuando el toggle está activo. Sin cambio de código.**

#### Patches

- [x] **\[Review\]\[Patch\]** P1 (HIGH): AddAsync TOCTOU — `DbUpdateException` no capturada en inserción concurrente — dos PUT simultáneos del mismo usuario pasan el `AnyAsync` antes de que ninguno confirme; la segunda inserción falla con violación de PK compuesta y el error llega como 500 al cliente — `UserFavoritesRepository.cs:AddAsync`
- [x] **\[Review\]\[Patch\]** P2 (HIGH): StarButton visible para no autenticados en M8/M9 — `PositionsTable.tsx:279` y `OportunidadesPage.tsx:193` renderizan `<StarButton>` sin guardia `isAuthenticated`; Dev Notes especifican "la estrella se omite completamente para usuarios no autenticados"
- [x] **\[Review\]\[Patch\]** P3 (LOW): `UniverseCoverageCalculator` sin guarda contra `fibrasWithPrice > universeSize` — si se invoca con parámetros incorrectos produce `MissingPct` negativo sin error explícito — `UniverseCoverageCalculator.cs:Calculate`

#### Deferred

- [x] **\[Review\]\[Defer\]** D1: `Task.WhenAll` 4 repos compartiendo `AppDbContext` — pre-existing, ya documentado en deferred-work.md — `OpportunityEndpoints.cs:37`
- [x] **\[Review\]\[Defer\]** D2: `GetUserId` sin guarda si falta el claim `NameIdentifier` — patrón pre-existente en todos los endpoints privados, protegido por `RequireAuthorization` — `FavoriteEndpoints.cs:57`
- [x] **\[Review\]\[Defer\]** D3: Sin FK para `UserFavorite.FibraId`/`UserId` → favoritos huérfanos si se elimina FIBRA — convención del módulo Portfolio (ninguna entidad del módulo declara FK)
- [x] **\[Review\]\[Defer\]** D4: `isAuthenticated` no migrado en 4 callers existentes que usan `status === 'authenticated'` inline — inconsistencia cosmética pre-existente — `PublicLayout.tsx, LoginPage.tsx`
- [x] **\[Review\]\[Defer\]** D5: Separador visual desplazado cuando la última fila favorita está expandida — edge case visual bajo, no funcional — `OportunidadesPage.tsx:181, PositionsTable.tsx:254`
- [x] **\[Review\]\[Defer\]** D6: Gaps de tests — concurrencia en `AddAsync`, aislamiento cross-user en `Remove`, orden estable por `AddedAt` — mejoras incrementales
- [x] **\[Review\]\[Defer\]** D7: `fibrasWithoutPrice` puede producir `NaN` si `coverage.universeSize` llega como string inválido — teórico con serialización tipada
- [x] **\[Review\]\[Defer\]** D8: `useFavorites()` se instancia en rutas públicas para usuarios anónimos — query deshabilitada, overhead mínimo
