# Historia 4.5.3: Página de preview de noticia /noticias/:id

Status: done

## Story

Como visitante de FIBRADIS,
quiero ver una página de preview de una noticia en `/noticias/:id`,
para poder leer el resumen de IA y ver la imagen antes de decidir si visito la fuente original.

## Acceptance Criteria

1. **Ruta interna:** Dado que existe la ruta `/noticias/:id` en el router de Main SPA, cuando el usuario navega a esa URL, se muestra la página de preview con los datos del artículo.

2. **Cards apuntan a la página de preview:** Dado que el usuario hace click en el título o imagen de una noticia en `NewsSection` (Home) o en `NoticiasSection` (ficha de FIBRA), es navegado a `/noticias/:id` mediante navegación client-side (sin recarga de página).

3. **Imagen en el preview:** La página muestra la imagen del artículo (`imageUrl` de 4.5.1 ?? logo de FIBRA ?? imagen de sector) en aspect ratio 16:9 en la parte superior.

4. **Resumen IA:** Si el artículo tiene `aiSummary`, se muestra el texto completo del resumen (sin truncar). Si no hay resumen, se muestra el `snippet` del artículo.

5. **Botón "Leer más":** Siempre hay un botón/enlace prominente que abre la URL original en pestaña nueva (`target="_blank" rel="noopener noreferrer"`). Si la URL no es segura (falla validación de `getSafeExternalUrl`), el botón no se muestra.

6. **Metadata visible:** Se muestran: nombre de la fuente y fecha de publicación relativa.

7. **Estado de carga y 404:** La página muestra skeleton mientras carga. Si el artículo no existe (404 de API), muestra mensaje de error con botón "Volver al inicio".

8. **SEO mínimo:** La página incluye `<title>{article.title} — FIBRADIS</title>` y `<meta name="description">` con el primer fragmento del resumen IA o snippet.

9. **Endpoint individual en API:** `GET /api/v1/news/{id}` retorna `NewsArticleDto` completo. Retorna 404 si el artículo no existe.

## Tasks / Subtasks

- [x] Task 1: Backend — endpoint de artículo individual
  - [x] 1.1 Agregar `GetByIdAsync(Guid id, CancellationToken ct)` a `INewsRepository`:
    ```csharp
    Task<NewsArticle?> GetByIdAsync(Guid id, CancellationToken ct);
    ```
  - [x] 1.2 Implementar en `NewsRepository`:
    ```csharp
    public async Task<NewsArticle?> GetByIdAsync(Guid id, CancellationToken ct)
        => await _db.Set<NewsArticle>().FindAsync([id], ct);
    ```
  - [x] 1.3 Agregar endpoint en `NewsEndpoints.cs`:
    ```csharp
    group.MapGet("/{id:guid}", async (Guid id, INewsRepository newsRepo, CancellationToken ct) =>
    {
        var article = await newsRepo.GetByIdAsync(id, ct);
        return article is null ? Results.NotFound() : Results.Ok(ToDto(article));
    })
    .AllowAnonymous()
    .Produces<NewsArticleDto>(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status404NotFound);
    ```
  - [x] 1.4 Ejecutar `npm run codegen:api` para regenerar `SharedApiClient/schema.d.ts`

- [x] Task 2: Frontend — función `fetchArticleById` en API layer
  - [x] 2.1 Agregar a `src/Web/Main/src/api/newsApi.ts`:
    ```ts
    export async function fetchArticleById(id: string) {
      const { data, error, response } = await apiClient.GET('/api/v1/news/{id}', {
        params: { path: { id } },
      })
      if (response.status === 404) return null
      if (error) throw new Error(`Error al obtener artículo: ${JSON.stringify(error)}`)
      return data ?? null
    }
    ```

- [x] Task 3: Frontend — módulo `NoticiaPage`
  - [x] 3.1 Crear directorio `src/Web/Main/src/modules/noticia/`
  - [x] 3.2 Crear `src/Web/Main/src/modules/noticia/NoticiaPage.tsx` — ver Dev Notes para estructura completa

- [x] Task 4: Frontend — router y navegación
  - [x] 4.1 Agregar ruta en `src/Web/Main/src/app/routes.tsx`:
    ```tsx
    import { NoticiaPage } from '@/modules/noticia/NoticiaPage'
    // en el array children:
    { path: '/noticias/:id', element: <NoticiaPage /> },
    ```
  - [x] 4.2 Actualizar `NewsSection.tsx` — cambiar el `<a href={safeUrl}>` externo por `<Link to={`/noticias/${article.id}`}>` de react-router en el título e imagen de la card
  - [x] 4.3 Actualizar `NoticiasSection.tsx` — mismo cambio que 4.2

- [x] Task 5: Build, tests y smoke test manual
  - [x] 5.1 `dotnet build FIBRADIS.slnx` — 0 errores
  - [x] 5.2 `npm run build --workspace=src/Web/Main` — 0 errores TypeScript
  - [x] 5.3 Smoke test: `npm run dev:main` → Home → click en noticia → `/noticias/:id` → verificar imagen, resumen, botón "Leer más" → botón abre URL correcta en nueva pestaña

## Dev Notes

### Dependencias

- **4.5.1** — `imageUrl` en `NewsArticleDto`, función `getArticleImageUrl`, `SECTOR_IMAGES`. Esta historia depende de 4.5.1.
- **4.5.2** — CANCELADA. No hay `articleBody` ni `ContentState` en esta historia.

### NoticiaPage — estructura completa

```tsx
// src/Web/Main/src/modules/noticia/NoticiaPage.tsx
import { useParams, Link } from 'react-router'
import { useQuery } from '@tanstack/react-query'
import { fetchArticleById } from '@/api/newsApi'
import { getArticleImageUrl } from '@/shared/lib/news-image-fallback'
import { formatRelativeTime } from '@/shared/lib/format-time'
import { getSafeExternalUrl } from '@/shared/lib/safe-external-url'

export function NoticiaPage() {
  const { id } = useParams<{ id: string }>()

  const { data: article, isLoading, isError } = useQuery({
    queryKey: ['news', 'article', id],
    queryFn: () => fetchArticleById(id!),
    enabled: !!id,
    staleTime: 10 * 60_000,
  })

  if (isLoading) return <NoticiaPageSkeleton />

  if (isError || article === undefined) {
    return (
      <div className="container mx-auto px-4 py-16 text-center">
        <p className="text-muted-foreground mb-4">No se pudo cargar la noticia.</p>
        <Link to="/" className="text-brand underline">Volver al inicio</Link>
      </div>
    )
  }

  if (article === null) {
    return (
      <div className="container mx-auto px-4 py-16 text-center">
        <p className="text-muted-foreground mb-4">Noticia no encontrada.</p>
        <Link to="/" className="text-brand underline">Volver al inicio</Link>
      </div>
    )
  }

  const imageUrl = getArticleImageUrl(article.imageUrl, undefined, undefined)
  const safeExternalUrl = getSafeExternalUrl(article.url)
  const summary = article.aiSummary ?? article.snippet

  return (
    <>
      <title>{article.title} — FIBRADIS</title>
      <meta
        name="description"
        content={(summary ?? '').slice(0, 160)}
      />

      <div className="container mx-auto px-4 py-8 max-w-2xl">
        {/* Imagen */}
        <div className="w-full aspect-video overflow-hidden rounded-xl bg-muted mb-6">
          <img
            src={imageUrl}
            alt={article.title}
            className="w-full h-full object-cover"
            loading="eager"
            onError={(e) => {
              ;(e.target as HTMLImageElement).src = '/assets/sectors/otro.jpg'
            }}
          />
        </div>

        {/* Título */}
        <h1 className="text-2xl font-bold leading-tight mb-2">{article.title}</h1>

        {/* Metadata */}
        <p className="text-sm text-muted-foreground mb-6">
          {article.source} · {formatRelativeTime(article.publishedAt)}
        </p>

        {/* Resumen / snippet */}
        {summary && (
          <div className="mb-8">
            {article.aiSummary && (
              <p className="text-xs font-semibold uppercase tracking-wider text-brand mb-2">
                Resumen IA
              </p>
            )}
            <p className="text-base leading-relaxed text-foreground">{summary}</p>
          </div>
        )}

        {/* Botón Leer más */}
        {safeExternalUrl && (
          <a
            href={safeExternalUrl}
            target="_blank"
            rel="noopener noreferrer"
            className="inline-flex items-center gap-2 px-5 py-2.5 rounded-lg bg-brand text-white text-sm font-medium hover:opacity-90 transition-opacity"
          >
            Leer más en {article.source} →
          </a>
        )}
      </div>
    </>
  )
}

function NoticiaPageSkeleton() {
  return (
    <div className="container mx-auto px-4 py-8 max-w-2xl animate-pulse">
      <div className="w-full aspect-video rounded-xl bg-muted mb-6" />
      <div className="h-7 w-3/4 bg-muted rounded mb-2" />
      <div className="h-4 w-1/3 bg-muted rounded mb-6" />
      <div className="space-y-2 mb-8">
        <div className="h-4 w-full bg-muted rounded" />
        <div className="h-4 w-full bg-muted rounded" />
        <div className="h-4 w-4/5 bg-muted rounded" />
      </div>
      <div className="h-10 w-40 bg-muted rounded-lg" />
    </div>
  )
}
```

### Actualización de cards en NewsSection y NoticiasSection

Cambiar el `<a>` externo que envuelve el título por `<Link>` interno:

```tsx
import { Link } from 'react-router'

// Antes:
<a href={safeUrl} target="_blank" rel="noopener noreferrer" ...>
  <h4>{article.title}</h4>
</a>

// Después:
<Link to={`/noticias/${article.id}`} className="block hover:text-brand transition-colors">
  <h4>{article.title}</h4>
</Link>
```

El botón/link externo "Leer más" solo existe en `NoticiaPage` — no en las cards del listado.

### Anti-patrones a evitar

1. **NO** usar `<a>` para la navegación a `/noticias/:id` — usar `<Link>` de react-router
2. **NO** mostrar el botón "Leer más" si `getSafeExternalUrl` retorna null — el artículo puede tener URL inválida
3. **NO** intentar mostrar contenido scrapeado de terceros — la decisión es deliberada: preview + link externo

### Archivos nuevos

```
src/Web/Main/src/modules/noticia/NoticiaPage.tsx
```

### Archivos modificados

```
src/Server/Application/News/INewsRepository.cs           → + GetByIdAsync
src/Server/Infrastructure/Persistence/Repositories/News/NewsRepository.cs → + GetByIdAsync
src/Server/Api/Endpoints/Public/NewsEndpoints.cs         → + GET /{id}
src/Web/SharedApiClient/schema.d.ts                      → regenerado
src/Web/Main/src/app/routes.tsx                          → + /noticias/:id
src/Web/Main/src/api/newsApi.ts                          → + fetchArticleById
src/Web/Main/src/modules/home/NewsSection.tsx            → <a> externo → <Link> interno (imagen ya en 4.5.1)
src/Web/Main/src/modules/ficha-publica/sections/NoticiasSection.tsx → mismo cambio
_bmad-output/implementation-artifacts/sprint-status.yaml
```

### Referencias

- [Source: 4-5-1-scraping-imagen-ogimage-y-fallback-visual.md] — `imageUrl` en DTO, `getArticleImageUrl`, `SECTOR_IMAGES`, patrón de imagen 16:9
- [Source: 4-5-2-scraping-cuerpo-del-articulo.md] — CANCELADA, no hay dependencia

### Review Findings — Pasada 1 (2026-05-21)

- [x] [Review][Patch] `useDocumentMetadata` (useEffect) en lugar de JSX `<title>` + `<meta>` directo — Dev Notes especifica el enfoque JSX; useEffect es client-side only y no es compatible con prerender estático [src/Web/Main/src/modules/noticia/NoticiaPage.tsx:118]
- [x] [Review][Patch] `fetchArticleById` usa cast manual que bypasa el cliente tipado — `(apiClient as { GET: ... })` elude la inferencia de `paths`; si el schema cambia el compilador no detecta la divergencia [src/Web/Main/src/api/newsApi.ts:21]
- [x] [Review][Defer] AC 4.5.3/3 — logo de FIBRA en `NoticiaPage` requiere extender `NewsArticleDto` con asociación de FIBRA; alineado con decisión de AC2 en historia 4.5.1 — deferred para historia futura del módulo noticias
- [x] [Review][Defer] `GET /api/v1/news/{id}` retorna 404 bare sin `ProblemDetails` — inconsistencia menor con convención de la API; impacto nulo en comportamiento funcional [src/Server/Api/Endpoints/Public/NewsEndpoints.cs:39] — deferred
- [x] [Review][Defer] `staleTime: 10 min` cachea el sentinel `null` (404) el mismo tiempo que un artículo válido — aceptable dado que artículos no se crean retroactivamente en el mismo ID [src/Web/Main/src/modules/noticia/NoticiaPage.tsx:17] — deferred

### Review Findings — Cierre Final (2026-05-21)

- [x] [Review][Close] Pasada final sin hallazgos bloqueantes nuevos. Validado nuevamente con `NewsEndpointsTests`, `npm test --workspace=src/Web/Main` y `npm run build --workspace=src/Web/Main`. El fallback con logo de FIBRA en `NoticiaPage` permanece como deuda ya diferida y no bloquea el cierre.

## Dev Agent Record

### Debug Log

- 2026-05-20 16:41 - Implementado `GET /api/v1/news/{id}` y prueba de integración `NewsEndpointsTests`.
- 2026-05-20 16:41 - Agregada `NoticiaPage`, navegación interna desde Home/Fibra y `fetchArticleById` con fallback a `null` para 404.
- 2026-05-20 16:41 - Validado con `npm test --workspace=src/Web/Main`, `dotnet test tests/Integration/Api.Tests/Api.Tests.csproj --filter NewsEndpointsTests`, `dotnet build FIBRADIS.slnx --configuration Release`, `npm run build --workspace=src/Web/Main` y smoke browser local con Playwright.
- 2026-05-21 09:32 - Reemplazado `useDocumentMetadata` por `<title>` + `<meta>` inline en `NoticiaPage` para que el prerender extraiga metadata estática correctamente.
- 2026-05-21 09:32 - Eliminado el cast manual de `fetchArticleById`; la llamada a `apiClient.GET('/api/v1/news/{id}')` vuelve a quedar completamente tipada por `paths`.

### Completion Notes

- `INewsRepository.GetByIdAsync` y `NewsRepository.GetByIdAsync` ya estaban presentes en el branch base; la story añadió el endpoint público, el consumo frontend y la experiencia de preview end-to-end.
- `NoticiaPage` renderiza `<title>` y `<meta name="description">` en JSX para que la metadata funcione en prerender estático y navegación client-side, además de cubrir estados `loading`, `error` y `404`.
- Se agregaron pruebas para el endpoint individual y para `fetchArticleById`; el smoke browser local verificó Home → `/noticias/:id`, render de resumen IA y apertura del CTA externo en nueva pestaña.
- El build .NET se ejecutó con `--configuration Release` por el workaround del proyecto cuando `Debug` queda bloqueado por DLLs en uso.
- Pasada 1 de review resuelta: `NoticiaPage` vuelve al patrón SSR/prerender esperado con `<title>` y `<meta name="description">` renderizados en JSX, y `fetchArticleById` ya no bypasa el cliente tipado de OpenAPI.
- Validaciones ejecutadas en esta pasada: `npm test --workspace=src/Web/Main` OK (37/37), `dotnet test tests/Integration/Api.Tests/Api.Tests.csproj --filter "FullyQualifiedName~NewsEndpointsTests"` OK (2/2), `dotnet build FIBRADIS.slnx` OK, `npm run build --workspace=src/Web/Main` OK.
- `npm run lint --workspace=src/Web/Main` sigue fallando por una condición preexistente del script: ESLint incluye `src/Web/Main/dist-server/entry-server.js` generado y reclama reglas no instaladas, sin findings nuevos en `newsApi.ts` o `NoticiaPage.tsx`.

## File List

- `src/Server/Api/Endpoints/Public/NewsEndpoints.cs`
- `src/Web/Main/package.json`
- `src/Web/Main/src/api/newsApi.ts`
- `src/Web/Main/src/api/newsApi.test.ts`
- `src/Web/Main/src/app/routes.tsx`
- `src/Web/Main/src/modules/ficha-publica/sections/NoticiasSection.tsx`
- `src/Web/Main/src/modules/home/NewsSection.tsx`
- `src/Web/Main/src/modules/noticia/NoticiaPage.tsx`
- `src/Web/SharedApiClient/schema.d.ts`
- `tests/Integration/Api.Tests/NewsEndpointsTests.cs`
- `_bmad-output/implementation-artifacts/4-5-3-pagina-lectora-interna-noticias.md`

## Change Log

- 2026-05-20: Implementada la página interna `/noticias/:id`, endpoint público individual de noticias, navegación client-side desde cards y cobertura de pruebas/smoke.
- 2026-05-21: Pasada 1 code review resuelta — `NoticiaPage` usa metadata JSX compatible con prerender y `fetchArticleById` vuelve a depender del tipado de `openapi-fetch`. Validado con `npm test` Main (37/37), `NewsEndpointsTests` (2/2), `dotnet build FIBRADIS.slnx` y `npm run build --workspace=src/Web/Main`.
