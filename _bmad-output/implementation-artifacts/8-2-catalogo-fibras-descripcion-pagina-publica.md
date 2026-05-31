# Story 8.2: Catálogo de FIBRAs — Campo Descripción y Página Pública

Status: ready-for-dev

## Story

As a visitante público del sitio,
I want una página `/catalogo` que lista todas las FIBRAs activas con sus datos clave y descripción editorial,
so that puedo explorar, filtrar y comparar emisoras de forma ordenada antes de entrar a la ficha completa de cada una.

## Acceptance Criteria

1. **Campo Description en entidad**: `Fibra` tiene una propiedad `string? Description` persistida como `nvarchar(max)` con columna `description` en `catalog.Fibra`. La migración EF Core se aplica sin errores.

2. **DTO lista (FibraListItem)**: el contrato `FibraListItem` incluye `bool HasDescription`. Es `true` si `Description` no es null ni vacío.

3. **DTO detalle (FibraDetail)**: el contrato `FibraDetail` incluye `string? Description`. El endpoint público `GET /api/v1/fibras/{ticker}` retorna el campo.

4. **Endpoint PUT Ops acepta Description**: `PUT /api/v1/ops/catalog/{ticker}` acepta el campo `description` en `UpdateFibraRequest`. Máx 10 000 caracteres; si se supera, responde 400 con campo `description` en el error de validación.

5. **Endpoint POST Ops acepta Description (opcional)**: `POST /api/v1/ops/catalog` acepta `description?` en `CreateFibraRequest`. Aplica la misma validación de longitud.

6. **Ops — lista: indicador de descripción**: `CatalogTable.tsx` muestra una nueva columna "Descripción" con badge verde "Con texto" cuando `hasDescription = true` y badge gris "Sin texto" cuando es `false`.

7. **Ops — formulario: editar descripción**: `FibraForm.tsx` incluye un bloque editable para el campo `description` (debajo del bloque NameVariants): `<textarea rows={20}>` con contador de caracteres y aviso cuando supere 10 000.

8. **Página pública `/catalogo`**: nueva ruta activa que muestra todas las FIBRAs activas en grid de tarjetas. Incluye buscador por ticker o nombre (debounced, client-side) y filtros por Sector y Mercado.

9. **Tarjeta de catálogo**: cada tarjeta muestra Ticker (tipografía Playfair, color `primary`), nombre completo, chips de Sector / Mercado / Moneda, y un indicador visual ("Con descripción" / "Sin descripción"). Contiene CTA "Ver ficha →" que navega a `/fibra/{ticker}`.

10. **FibraPage — sección Descripción y breadcrumb**: `FibraPage.tsx` incluye (a) una sección "Descripción" que renderiza `fibra.description` con `ReactMarkdown + prose` cuando el campo tiene contenido (si es null, la sección no se renderiza), y (b) un breadcrumb fijo en el encabezado de la página: `← Catálogo / {ticker}` donde "Catálogo" es un `<Link to="/catalogo">`, permitiendo volver al listado desde cualquier ficha.

11. **SEO `/catalogo`**: `<title>Catálogo de FIBRAs — FIBRADIS</title>` con `<meta name="description">` apropiada. La ruta responde 200 en hit directo (SPA fallback configurado).

12. **Unit/Integration tests**: mínimo 3 tests verificando (a) `HasDescription = true` cuando la fibra tiene descripción, (b) `HasDescription = false` cuando es null, y (c) el PUT actualiza `Description` correctamente en BD.

## Tasks / Subtasks

- [ ] T1 — Backend: campo + configuración EF (AC: 1)
  - [ ] T1.1 Agregar `public string? Description { get; set; }` a `src/Server/Domain/Catalog/Fibra.cs`
  - [ ] T1.2 En `FibraConfiguration.cs` agregar: `builder.Property(f => f.Description).HasColumnType("nvarchar(max)").HasColumnName("description");`
  - [ ] T1.3 Crear migración: `dotnet ef migrations add AddFibraDescription --project src/Server/Infrastructure --startup-project src/Server/Api`

- [ ] T2 — Backend: contratos API (AC: 2, 3, 4, 5)
  - [ ] T2.1 Actualizar `FibraListItem.cs`: agregar `bool HasDescription` al final del record
  - [ ] T2.2 Actualizar `FibraDetail.cs`: agregar `string? Description` al final del record
  - [ ] T2.3 Actualizar `UpdateFibraRequest.cs`: agregar `string? Description` al final del record
  - [ ] T2.4 Actualizar `CreateFibraRequest.cs`: agregar `string? Description` al final del record

- [ ] T3 — Backend: endpoints (AC: 3, 4, 5)
  - [ ] T3.1 `CatalogEndpoints.cs` — lista: actualizar mapping para incluir `HasDescription = !string.IsNullOrWhiteSpace(f.Description)` en `FibraListItem`
  - [ ] T3.2 `CatalogEndpoints.cs` — detalle: actualizar mapping `FibraDetail` para incluir `fibra.Description`
  - [ ] T3.3 `OpsCatalogEndpoints.cs` — `ToDto()`: agregar `fibra.Description` al `FibraDetail` mapeado
  - [ ] T3.4 `OpsCatalogEndpoints.cs` — PUT handler: asignar `fibra.Description = NormalizeOptional(request.Description)` y agregar validación de longitud máx 10 000 en `ValidateUpdateRequest`
  - [ ] T3.5 `OpsCatalogEndpoints.cs` — POST handler: asignar `Description = NormalizeOptional(request.Description)` al nuevo `Fibra` y validar en `ValidateCreateRequest`
  - [ ] T3.6 `dotnet build FIBRADIS.slnx` — 0 errores

- [ ] T4 — Regenerar cliente API (AC: 2, 3)
  - [ ] T4.1 `npm run codegen:api` desde raíz del repo

- [ ] T5 — Ops: indicador en tabla (AC: 6)
  - [ ] T5.1 En `CatalogTable.tsx` agregar columna `<th>Descripción</th>` entre "Estado" y "Acciones"
  - [ ] T5.2 Agregar badge condicional en cada fila: verde "Con texto" si `fibra.hasDescription`, gris "Sin texto" si no (ver Dev Notes §Ops Badge)
  - [ ] T5.3 `npm run build --workspace=src/Web/Ops` — 0 errores TypeScript

- [ ] T6 — Ops: textarea en formulario (AC: 7)
  - [ ] T6.1 En `FibraForm.tsx` agregar al estado `description: string` (inicializado con `initialData?.description ?? ''`)
  - [ ] T6.2 Agregar bloque de descripción debajo del bloque NameVariants (ver Dev Notes §Ops Form)
  - [ ] T6.3 Incluir `description` en el payload de `UpdateFibraRequest` y `CreateFibraRequest`
  - [ ] T6.4 `npm run build --workspace=src/Web/Ops` — 0 errores TypeScript

- [ ] T7 — Main: página Catálogo (AC: 8, 9, 11)
  - [ ] T7.1 Crear `src/Web/Main/src/modules/catalogo/CatalogoPage.tsx` (ver Dev Notes §CatalogoPage)
  - [ ] T7.2 Agregar ruta `/catalogo` en `src/Web/Main/src/app/routes.tsx`
  - [ ] T7.3 Verificar que el `<Link to="/catalogo">` en `PublicLayout.tsx` ya existe (no duplicar)
  - [ ] T7.4 `npm run build --workspace=src/Web/Main` — 0 errores TypeScript

- [ ] T8 — Main: breadcrumb + sección Descripción en FibraPage (AC: 10)
  - [ ] T8.1 En `FibraPage.tsx` agregar breadcrumb `← Catálogo / {ticker}` en el encabezado (ver Dev Notes §FibraPage Breadcrumb)
  - [ ] T8.2 Importar `ReactMarkdown` (ya instalado en el proyecto — verificar import)
  - [ ] T8.3 Agregar entrada `{ href: '#descripcion', label: 'Descripción' }` en `SECTION_LABELS` SOLO si `fibra.description` tiene contenido
  - [ ] T8.4 Agregar sección `#descripcion` entre el encabezado de precio y `#mercado` (ver Dev Notes §FibraPage Descripción)
  - [ ] T8.5 `npm run build --workspace=src/Web/Main` — 0 errores TypeScript

- [ ] T9 — Tests y verificación final (AC: 12)
  - [ ] T9.1 Crear tests de integración o unit tests (ver Dev Notes §Tests)
  - [ ] T9.2 `dotnet test tests/Unit/` — todos pasan
  - [ ] T9.3 `dotnet ef database update --project src/Server/Infrastructure --startup-project src/Server/Api` — migración aplica
  - [ ] T9.4 `npm run dev:main` — verificar `/catalogo` carga, cards muestran indicador correcto, búsqueda y filtros funcionan
  - [ ] T9.5 `npm run dev:ops` — verificar que el formulario muestra textarea y la tabla muestra la columna de descripción
  - [ ] T9.6 Verificar `/fibra/{ticker}` muestra sección Descripción cuando hay contenido

---

## Dev Notes

### Contexto General

Esta historia extiende el catálogo de FIBRAs con un campo editorial largo (`description`, markdown, ~10 000 chars) y crea la página pública `/catalogo` que estaba vacía desde la historia 2.x. La ruta `/catalogo` ya tiene su `<Link>` en `PublicLayout.tsx`; falta la página.

El patrón de los módulos `Main` sigue: `src/Web/Main/src/modules/<nombre>/NombrePage.tsx`.  
El módulo `catalogo` ya existe como carpeta (`.gitkeep`).

---

### Backend — Cambios mínimos en entidad y configuración

```csharp
// src/Server/Domain/Catalog/Fibra.cs — agregar al final:
public string? Description { get; set; }

// FibraConfiguration.cs — agregar dentro de Configure():
builder.Property(f => f.Description)
    .HasColumnType("nvarchar(max)")
    .HasColumnName("description");
```

No cambiar el resto de la configuración existente.

---

### Backend — Actualización de contratos

```csharp
// FibraListItem.cs — agregar al final del record:
public record FibraListItem(
    Guid Id,
    string Ticker,
    string FullName,
    string ShortName,
    string Sector,
    string Market,
    string Currency,
    string State,
    string? SiteUrl,
    bool HasDescription);   // <-- nuevo campo

// FibraDetail.cs — agregar al final:
public record FibraDetail(
    Guid Id,
    string Ticker,
    string YahooTicker,
    string FullName,
    string ShortName,
    string Sector,
    string Market,
    string Currency,
    string State,
    string? SiteUrl,
    string? InvestorUrl,
    string? ReportsUrl,
    IReadOnlyList<string> NameVariants,
    DateTimeOffset CreatedAt,
    string? Description);   // <-- nuevo campo

// UpdateFibraRequest.cs — agregar al final:
public sealed record UpdateFibraRequest(
    string YahooTicker,
    string FullName,
    string ShortName,
    string Sector,
    string Market,
    string Currency,
    string? SiteUrl,
    string? InvestorUrl,
    string? ReportsUrl,
    IReadOnlyList<string>? NameVariants,
    string? Description);   // <-- nuevo campo

// CreateFibraRequest.cs — agregar al final:
public sealed record CreateFibraRequest(
    string Ticker,
    string YahooTicker,
    ...
    IReadOnlyList<string>? NameVariants,
    string? Description);   // <-- nuevo campo
```

---

### Backend — Validación de longitud en endpoints

En `OpsCatalogEndpoints.cs`, dentro de `ValidateUpdateRequest` y `ValidateCreateRequest`:

```csharp
// Validación de Description (añadir al final de ambas funciones):
if (request.Description is not null && request.Description.Length > 10_000)
{
    errors["description"] = ["La descripción no puede superar 10 000 caracteres."];
}
```

En el PUT handler, asignar el campo:
```csharp
// Dentro del bloque de asignación del PUT:
fibra.Description = NormalizeOptional(request.Description);
```

En el POST handler, en la inicialización del `Fibra`:
```csharp
Description = NormalizeOptional(request.Description),
```

Actualizar `ToDto()`:
```csharp
private static FibraDetail ToDto(Fibra fibra) => new(
    fibra.Id,
    fibra.Ticker,
    fibra.YahooTicker,
    fibra.FullName,
    fibra.ShortName,
    fibra.Sector,
    fibra.Market,
    fibra.Currency,
    fibra.State.ToString(),
    fibra.SiteUrl,
    fibra.InvestorUrl,
    fibra.ReportsUrl,
    fibra.NameVariants.AsReadOnly(),
    fibra.CreatedAt,
    fibra.Description);  // <-- nuevo
```

En `CatalogEndpoints.cs`, actualizar los mapeos:
```csharp
// Lista:
var dtos = items.Select(f => new FibraListItem(
    f.Id, f.Ticker, f.FullName, f.ShortName,
    f.Sector, f.Market, f.Currency, f.State.ToString(), f.SiteUrl,
    !string.IsNullOrWhiteSpace(f.Description))).ToList();  // <-- HasDescription

// Detalle:
return Results.Ok(new FibraDetail(
    fibra.Id, fibra.Ticker, fibra.YahooTicker, fibra.FullName, fibra.ShortName,
    fibra.Sector, fibra.Market, fibra.Currency, fibra.State.ToString(),
    fibra.SiteUrl, fibra.InvestorUrl, fibra.ReportsUrl,
    fibra.NameVariants.AsReadOnly(), fibra.CreatedAt,
    fibra.Description));  // <-- nuevo
```

---

### Ops — Badge en CatalogTable (T5)

Agregar columna nueva entre "Estado" y "Acciones":

```tsx
// Header:
<th className="px-4 py-3 font-medium">Descripción</th>

// Celda por fila:
<td className="px-4 py-4">
  <span
    className={`inline-flex rounded-full px-3 py-1 text-xs font-semibold ${
      fibra.hasDescription
        ? 'bg-emerald-100 text-emerald-700'
        : 'bg-slate-200 text-slate-500'
    }`}
  >
    {fibra.hasDescription ? 'Con texto' : 'Sin texto'}
  </span>
</td>
```

El tipo `FibraDetail` que usa la tabla incluye `hasDescription` después de regenerar el cliente.  
**Verificar**: `catalogApi.ts` en Ops expone el tipo `FibraDetail` generado; si este no incluye `hasDescription` post-codegen, actualizarlo manualmente como prop derivada del campo `description`.

---

### Ops — Bloque Descripción en FibraForm (T6)

Agregar fuera del `useForm` (es estado libre, como `variants`):
```tsx
const [description, setDescription] = useState<string>(initialData?.description ?? '')
const MAX_DESC = 10_000
```

Agregar bloque debajo del bloque NameVariants (antes del botón de envío):
```tsx
<section className="rounded-2xl border border-slate-200 bg-slate-50/70 p-4 space-y-3">
  <div className="flex items-center justify-between gap-4">
    <div>
      <h3 className="text-sm font-semibold uppercase tracking-[0.18em] text-slate-600">
        Descripción editorial
      </h3>
      <p className="mt-1 text-sm text-slate-500">
        Texto largo en formato Markdown (~10 000 chars). Visible en la ficha pública y catálogo.
      </p>
    </div>
    <span className={`text-xs font-mono ${description.length > MAX_DESC ? 'text-rose-600' : 'text-slate-400'}`}>
      {description.length} / {MAX_DESC}
    </span>
  </div>
  <textarea
    className="w-full rounded-xl border border-slate-200 bg-white px-3 py-2.5 text-sm text-slate-900 font-mono outline-none transition focus:border-teal-600 resize-y"
    onChange={(e) => setDescription(e.target.value)}
    placeholder="# Título&#10;&#10;Describe la FIBRA en Markdown..."
    rows={20}
    value={description}
  />
  {description.length > MAX_DESC ? (
    <p className="text-xs text-rose-600">
      La descripción supera el límite de {MAX_DESC} caracteres.
    </p>
  ) : null}
</section>
```

En el `mutationFn`, incluir el campo:
```tsx
// En UpdateFibraRequest:
const payload: UpdateFibraRequest = {
  ...todosLosOtrosCampos,
  description: description.trim() === '' ? null : description,
}
// En CreateFibraRequest:
const payload: CreateFibraRequest = {
  ...todosLosOtrosCampos,
  description: description.trim() === '' ? null : description,
}
```

Bloquear el submit si `description.length > MAX_DESC` (agregar condición en el `disabled` del botón).

---

### Main — CatalogoPage (T7)

Diseño: grid de tarjetas con búsqueda client-side y filtros.

```tsx
// src/Web/Main/src/modules/catalogo/CatalogoPage.tsx
import { useMemo, useState } from 'react'
import { Link } from 'react-router'
import { useQuery } from '@tanstack/react-query'
import { fetchAllFibras } from '@/api/fibrasApi'
import { Input } from '@/shared/ui/input'

export function CatalogoPage() {
  const { data: fibras = [], isLoading } = useQuery({
    queryKey: ['fibras', 'all'],
    queryFn: fetchAllFibras,
    staleTime: 5 * 60_000,
  })

  const [search, setSearch] = useState('')
  const [sector, setSector] = useState('')
  const [market, setMarket] = useState('')

  // Valores únicos para filtros (calculados una vez por data)
  const sectors = useMemo(
    () => Array.from(new Set(fibras.map((f) => f.sector))).sort(),
    [fibras],
  )
  const markets = useMemo(
    () => Array.from(new Set(fibras.map((f) => f.market))).sort(),
    [fibras],
  )

  const filtered = useMemo(() => {
    const q = search.toLowerCase().trim()
    return fibras.filter((f) => {
      const matchesSearch =
        !q || f.ticker.toLowerCase().includes(q) || f.fullName.toLowerCase().includes(q)
      const matchesSector = !sector || f.sector === sector
      const matchesMarket = !market || f.market === market
      return matchesSearch && matchesSector && matchesMarket
    })
  }, [fibras, search, sector, market])

  const hasFilters = search || sector || market

  return (
    <>
      <title>Catálogo de FIBRAs — FIBRADIS</title>
      <meta
        name="description"
        content="Explora el universo completo de FIBRAs inmobiliarias mexicanas con datos clave, sector, mercado y descripción editorial."
      />

      <div className="container mx-auto px-4 py-8">
        {/* Encabezado */}
        <div className="mb-8 space-y-2">
          <p className="text-xs font-semibold uppercase tracking-[0.24em] text-primary">
            Universo FIBRAS
          </p>
          <h1 className="font-playfair text-4xl font-bold leading-tight text-foreground md:text-5xl">
            Catálogo de FIBRAs
          </h1>
          <p className="max-w-2xl text-sm leading-6 text-muted-foreground md:text-base">
            {isLoading
              ? 'Cargando...'
              : `${fibras.length} emisoras activas en el universo FIBRADIS.`}
          </p>
        </div>

        {/* Filtros */}
        <div className="mb-6 flex flex-col gap-3 sm:flex-row sm:items-center">
          <Input
            className="sm:max-w-xs"
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Buscar por ticker o nombre..."
            value={search}
          />
          <select
            className="rounded-lg border border-border bg-background px-3 py-2 text-sm text-foreground outline-none transition focus:border-ring cursor-pointer"
            onChange={(e) => setSector(e.target.value)}
            value={sector}
          >
            <option value="">Todos los sectores</option>
            {sectors.map((s) => <option key={s} value={s}>{s}</option>)}
          </select>
          <select
            className="rounded-lg border border-border bg-background px-3 py-2 text-sm text-foreground outline-none transition focus:border-ring cursor-pointer"
            onChange={(e) => setMarket(e.target.value)}
            value={market}
          >
            <option value="">Todos los mercados</option>
            {markets.map((m) => <option key={m} value={m}>{m}</option>)}
          </select>
          {hasFilters ? (
            <button
              className="text-sm text-muted-foreground underline underline-offset-2 transition hover:text-foreground cursor-pointer"
              onClick={() => { setSearch(''); setSector(''); setMarket('') }}
              type="button"
            >
              Limpiar filtros
            </button>
          ) : null}
        </div>

        {/* Grid de tarjetas */}
        {isLoading ? (
          <div className="grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
            {Array.from({ length: 9 }).map((_, i) => (
              <div key={i} className="h-48 animate-pulse rounded-2xl bg-muted" />
            ))}
          </div>
        ) : filtered.length === 0 ? (
          <div className="py-16 text-center">
            <p className="text-muted-foreground">
              No se encontraron FIBRAs con esos filtros.
            </p>
          </div>
        ) : (
          <div className="grid gap-5 sm:grid-cols-2 lg:grid-cols-3">
            {filtered.map((fibra) => (
              <FibraCard key={fibra.id} fibra={fibra} />
            ))}
          </div>
        )}

        {/* Contador al pie */}
        {!isLoading && filtered.length > 0 && hasFilters ? (
          <p className="mt-6 text-center text-xs text-muted-foreground">
            Mostrando {filtered.length} de {fibras.length} emisoras
          </p>
        ) : null}
      </div>
    </>
  )
}

interface FibraCardProps {
  fibra: {
    id: string
    ticker: string
    fullName: string
    sector: string
    market: string
    currency: string
    hasDescription: boolean
  }
}

function FibraCard({ fibra }: FibraCardProps) {
  return (
    <div className="group flex flex-col justify-between rounded-2xl border border-border bg-card p-5 shadow-sm transition hover:shadow-md hover:-translate-y-0.5">
      {/* Cabecera */}
      <div className="space-y-2">
        <div className="flex items-start justify-between gap-2">
          <span className="font-playfair text-2xl font-bold text-primary leading-none">
            {fibra.ticker}
          </span>
          <DescriptionBadge has={fibra.hasDescription} />
        </div>
        <p className="text-sm font-medium text-foreground leading-snug line-clamp-2">
          {fibra.fullName}
        </p>
        {/* Chips */}
        <div className="flex flex-wrap gap-1.5 pt-1">
          <Chip>{fibra.sector}</Chip>
          <Chip>{fibra.market}</Chip>
          <Chip>{fibra.currency}</Chip>
        </div>
      </div>

      {/* CTA */}
      <div className="mt-5">
        <Link
          className="inline-flex items-center gap-1 text-sm font-semibold text-primary transition hover:text-primary/80 cursor-pointer"
          to={`/fibra/${fibra.ticker}`}
        >
          Ver ficha
          <svg aria-hidden="true" className="h-3.5 w-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path d="M9 18l6-6-6-6" strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} />
          </svg>
        </Link>
      </div>
    </div>
  )
}

function DescriptionBadge({ has }: { has: boolean }) {
  if (has) {
    return (
      <span className="inline-flex items-center gap-1 rounded-full bg-emerald-50 px-2 py-0.5 text-xs font-medium text-emerald-700 whitespace-nowrap border border-emerald-200">
        <span className="h-1.5 w-1.5 rounded-full bg-emerald-500" />
        Con descripción
      </span>
    )
  }
  return (
    <span className="inline-flex rounded-full bg-muted px-2 py-0.5 text-xs text-muted-foreground whitespace-nowrap">
      Sin descripción
    </span>
  )
}

function Chip({ children }: { children: React.ReactNode }) {
  return (
    <span className="rounded-full border border-border bg-background px-2 py-0.5 text-xs text-muted-foreground">
      {children}
    </span>
  )
}
```

**Notas de implementación:**
- `fetchAllFibras()` ya existe en `fibrasApi.ts` — no crear una función nueva. Después de la regeneración del cliente API, el tipo retornado incluirá `hasDescription`.
- `Input` está disponible en `@/shared/ui/input` (shadcn/ui).
- `line-clamp-2` requiere Tailwind v4 — ya está disponible en el proyecto.
- El componente usa los CSS variables del design system (`text-primary`, `bg-card`, `text-foreground`, etc.) que ya están configurados en `tailwind.config` del proyecto.
- `font-playfair` es la clase de utilidad de Playfair Display, ya usada en `NoticiasListPage` y otras páginas — verificar que el alias esté en el config antes de usarlo.

---

### Main — Breadcrumb en FibraPage (T8.1)

Agregar un breadcrumb en la parte superior del contenido de `FibraPage.tsx`, antes del encabezado de precio. Es el único punto de entrada visible para volver a `/catalogo` desde cualquier ficha.

```tsx
// Agregar este bloque justo antes del primer <section> de contenido (encabezado de precio):
<nav aria-label="breadcrumb" className="mb-4 flex items-center gap-1.5 text-sm text-muted-foreground">
  <Link
    className="flex items-center gap-1 transition hover:text-foreground cursor-pointer"
    to="/catalogo"
  >
    <svg aria-hidden="true" className="h-3.5 w-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
      <path d="M15 18l-6-6 6-6" strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} />
    </svg>
    Catálogo
  </Link>
  <span aria-hidden="true">/</span>
  <span className="font-medium text-foreground">{ticker}</span>
</nav>
```

- Usar `Link` de `react-router` (ya importado en la página o en sus secciones).
- El breadcrumb es estático — siempre apunta a `/catalogo`, independiente del historial del navegador.
- Sin lógica condicional: aparece en todas las fichas públicas.

---

### Main — Sección Descripción en FibraPage (T8.2–T8.4)

`FibraPage.tsx` ya tiene el patrón `SectionHeader` y el patrón de query `useQuery(['fibra', ticker])`. El tipo retornado por `fetchFibraByTicker` incluirá `description?: string | null` tras regenerar el cliente.

Agregar en el array `SECTION_LABELS` (condicional):
```tsx
// Construir la lista de secciones dinámicamente dentro del render:
const sectionLabels = [
  ...(fibra?.description ? [{ href: '#descripcion', label: 'Descripción' }] : []),
  { href: '#mercado', label: 'Mercado' },
  { href: '#fundamentales', label: 'Fundamentales' },
  { href: '#distribuciones', label: 'Distribuciones' },
  { href: '#noticias', label: 'Noticias' },
  { href: '#reportes', label: 'Reportes' },
]
```

**Importante**: el actual código usa `SECTION_LABELS` como constante top-level. Para hacerlo dependiente de `fibra`, convertirlo en una variable local dentro del return o en un `useMemo`. Elegir la solución más simple y usar `sectionLabels` en lugar de `SECTION_LABELS` en todos los usos existentes de la constante dentro de `FibraPage`.

Agregar la sección en el render, antes de `#mercado`:
```tsx
{fibra.description ? (
  <section className="space-y-4" id="descripcion">
    <SectionHeader title="Descripción" />
    <div className="rounded-2xl border border-border bg-card p-6">
      <ReactMarkdown className="prose prose-slate max-w-none text-sm leading-7">
        {fibra.description}
      </ReactMarkdown>
    </div>
  </section>
) : null}
```

`ReactMarkdown` ya está instalado en el proyecto (usado en `NoticiaPage.tsx` y `ConoceLasFibrasPage.tsx`). No re-instalar.

---

### Tests (T9)

Los tests van en `tests/Integration/Api.Tests/` siguiendo el patrón de integración del proyecto (ver `AiModeOpsEndpointTests.cs` o `OpsConfigEndpointTests.cs` como referencia). Alternativamente pueden ser tests unitarios en `tests/Unit/Infrastructure.Tests/Persistence/Repositories/` si se prefiere el patrón de repositorio.

**3 tests mínimos requeridos:**

```csharp
// Test 1 — HasDescription = true cuando hay descripción
// Seed una fibra con Description = "# Test"
// GET /api/v1/fibras → FibraListItem.hasDescription debe ser true

// Test 2 — HasDescription = false cuando description es null
// Seed una fibra con Description = null
// GET /api/v1/fibras → FibraListItem.hasDescription debe ser false

// Test 3 — PUT actualiza description correctamente
// PUT /api/v1/ops/catalog/{ticker} con { ..., description: "# Texto actualizado" }
// GET /api/v1/fibras/{ticker} → FibraDetail.description debe ser "# Texto actualizado"
```

---

### Checklist SEO (aplica para `/catalogo`)

- [ ] La ruta `/catalogo` responde 200 en hit directo (SPA fallback configurado en backend)
- [ ] `<title>` y `<meta name="description">` están presentes en el HTML
- [ ] `npm run build` pasa con 0 errores TypeScript

---

### Archivos a crear

**Backend:**
- `src/Server/Infrastructure/Persistence/Migrations/XXXXXX_AddFibraDescription.cs` (generado por EF)

**Frontend Main:**
- `src/Web/Main/src/modules/catalogo/CatalogoPage.tsx`

### Archivos a modificar

**Backend:**
- `src/Server/Domain/Catalog/Fibra.cs`
- `src/Server/Infrastructure/Persistence/SqlServer/Configurations/Catalog/FibraConfiguration.cs`
- `src/Server/SharedApiContracts/Catalog/FibraListItem.cs`
- `src/Server/SharedApiContracts/Catalog/FibraDetail.cs`
- `src/Server/SharedApiContracts/Catalog/UpdateFibraRequest.cs`
- `src/Server/SharedApiContracts/Catalog/CreateFibraRequest.cs`
- `src/Server/Api/Endpoints/Public/CatalogEndpoints.cs`
- `src/Server/Api/Endpoints/Ops/OpsCatalogEndpoints.cs`

**Frontend Ops:**
- `src/Web/Ops/src/modules/catalog/CatalogTable.tsx`
- `src/Web/Ops/src/modules/catalog/FibraForm.tsx`

**Frontend Main:**
- `src/Web/Main/src/app/routes.tsx` (nueva ruta `/catalogo`)
- `src/Web/Main/src/modules/ficha-publica/FibraPage.tsx` (sección Descripción)

---

## File List

_(Se actualiza durante implementación)_

## Change Log

| Fecha | Cambio |
|-------|--------|
| 2026-05-31 | Story creada — ready-for-dev |

## Dev Agent Record

_(Se completa durante implementación)_

## Senior Developer Review (AI)

_(Se completa en code review)_
