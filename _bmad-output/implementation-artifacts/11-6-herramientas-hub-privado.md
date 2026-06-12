# Story 11.6: Herramientas — Hub privado con calculadoras de valor e indicadores de mercado

Status: done

## Story

Como inversionista registrado,
quiero que la página `/herramientas` sea un hub privado con calculadoras reales (FIBRAs vs CETES, meta de renta mensual y retorno total real) que usan la tasa CETES actualizada automáticamente,
para tomar mejores decisiones de inversión sin salir de la plataforma.

## Acceptance Criteria

1. **Dado que** soy un usuario anónimo y visito `/herramientas`, **cuando** la página carga, **entonces** soy redirigido a `/login` (ProtectedRoute igual que `/portafolio`).

2. **Dado que** soy un usuario autenticado y visito `/herramientas`, **cuando** la página carga, **entonces** veo la página de herramientas sin ninguna mención de la palabra "calculadora" en títulos, eyebrows, descripciones o etiquetas visibles del usuario.

3. **Dado que** la página carga, **cuando** la veo, **entonces** hay una sección hub con 3 accesos directos a: Comparador de FIBRAs (`/comparar`), Fichas de FIBRAs (`/fibras`), y Oportunidades — tab Promediar (`/oportunidades`). Cada acceso muestra título + descripción de una línea (no repite funcionalidad de los cálculos en la misma página).

4. **Dado que** la página carga, **cuando** la veo, **entonces** hay una herramienta "FIBRAs vs CETES" con: campo Monto (MXN), campo Yield FIBRA (%), campo Tasa CETES 28d (%) pre-llenado desde el backend si disponible, selector de Horizonte (1 / 3 / 5 / 10 años). La salida muestra una tabla con 2 columnas (FIBRA / CETES), 3 filas: Capital final estimado, Renta acumulada neta de ISR, y Rendimiento total %.

5. **Dado que** la tasa CETES está disponible en el backend, **cuando** la herramienta carga, **entonces** el campo Tasa CETES 28d aparece pre-llenado con el valor más reciente (ej. `9.50`). Si el endpoint falla o no hay dato, el campo aparece vacío con placeholder `"ej. 9.50"`.

6. **Dado que** la página carga, **cuando** la veo, **entonces** hay una herramienta "Meta de renta" con: campo Renta mensual objetivo (MXN), campo Yield estimado (%). La salida muestra: Capital necesario (MXN) y CBFIs estimados (usando el precio promedio del catálogo — ver Dev Notes).

7. **Dado que** la página carga, **cuando** la veo, **entonces** hay una herramienta "Retorno total" con: campos Precio de compra, Precio actual, Distribuciones recibidas TTM, ISR retenido total. La salida muestra: Plusvalía %, Yield neto recibido %, y Retorno total %.

8. **Dado que** existe el endpoint `GET /api/v1/market/indicadores`, **cuando** lo llama un usuario autenticado (rol `User`), **entonces** responde `200` con `{ "cetes28d": decimal|null, "lastUpdated": "ISO8601"|null }`. Un usuario anónimo recibe `401`.

9. **Dado que** el job `BanxicoSyncJob` se ejecuta, **cuando** Banxico responde correctamente con la serie SF43936, **entonces** `OperationalConfig.Cetes28dRate` y `Cetes28dRateUpdatedAt` se actualizan en la base de datos. El job está registrado como `RecurringJob` con cron `"0 6 * * 3"` (todos los miércoles a las 6 AM UTC).

10. **Dado que** el token de Banxico no está configurado (`Banxico:Token` vacío o ausente), **cuando** el job se ejecuta, **entonces** loguea un warning y termina sin excepción — el campo del formulario muestra vacío con placeholder.

11. **Dado que** `/herramientas` ahora es una ruta privada, **cuando** se genera el sitemap (`/sitemap.xml`), **entonces** `/herramientas` **no** aparece en él. Idem: no está en `SpaMetadataProvider.cs`.

12. **Dado que** el usuario es anónimo, **cuando** ve la navegación principal, **entonces** el enlace "Herramientas" **no** es visible. Cuando el usuario está autenticado, el enlace "Herramientas" aparece en el bloque `{status === 'authenticated'}` junto a Portafolio y Oportunidades (también en el menú móvil).

## Tasks / Subtasks

- [x] T1 — Backend: extender OperationalConfig y crear migración EF
  - [x] T1.1 — En `src/Server/Domain/Ops/OperationalConfig.cs` agregar:
    ```csharp
    public decimal? Cetes28dRate { get; set; }
    public DateTimeOffset? Cetes28dRateUpdatedAt { get; set; }
    ```
  - [x] T1.2 — Crear migración EF:
    ```bash
    dotnet ef migrations add AddCetes28dToOperationalConfig \
      --project src/Server/Infrastructure \
      --startup-project src/Server/Api
    ```
    Verificar que la migración aparece en la lista antes de continuar (Gate EF).
  - [x] T1.3 — En `src/Server/Application/Ops/IOperationalConfigRepository.cs` agregar método:
    ```csharp
    Task UpdateCetesRateAsync(decimal rate, DateTimeOffset updatedAt, CancellationToken ct = default);
    ```
  - [x] T1.4 — Implementar `UpdateCetesRateAsync` en `src/Server/Infrastructure/Persistence/Repositories/Ops/OperationalConfigRepository.cs` usando `ExecuteUpdateAsync` (patrón del repo existente).

- [x] T2 — Backend: BanxicoClient
  - [x] T2.1 — Crear `src/Server/Infrastructure/Integrations/Banxico/BanxicoClient.cs`:
    - Inyectar `HttpClient` + `IConfiguration`
    - Leer token desde `configuration["Banxico:Token"]`
    - Si token nulo o vacío → retornar `null` (no lanzar excepción)
    - Llamar `GET https://www.banxico.org.mx/SieAPIRest/service/v1/series/{series}/datos/oportuno` con header `Bmx-Token: {token}` y `Accept: application/json`
    - Parsear respuesta: `body.bmx.series[0].datos[0].dato` → `decimal.Parse(dato, CultureInfo.InvariantCulture)`
    - En error (HTTP non-2xx, parse error, timeout) → loguear warning, retornar `null`
  - [x] T2.2 — Crear interfaz `IBanxicoClient` con método `Task<decimal?> GetCetes28dAsync(CancellationToken ct)` en `src/Server/Application/Integrations/IBanxicoClient.cs`
  - [x] T2.3 — Registrar en `src/Server/Api/CompositionRoot/ApiServiceExtensions.cs`:
    ```csharp
    services.AddHttpClient<IBanxicoClient, BanxicoClient>();
    ```
  - [x] T2.4 — Agregar sección a `appsettings.json` (no a `appsettings.Development.json`):
    ```json
    "Banxico": {
      "Token": "",
      "Series": "SF43936"
    }
    ```

- [x] T3 — Backend: BanxicoSyncJob (Hangfire)
  - [x] T3.1 — Crear `src/Server/Application/Jobs/BanxicoSyncJob.cs`:
    ```csharp
    public class BanxicoSyncJob(IBanxicoClient banxico, IOperationalConfigRepository config, ILogger<BanxicoSyncJob> logger)
    {
        public async Task ExecuteAsync(CancellationToken ct)
        {
            var rate = await banxico.GetCetes28dAsync(ct);
            if (rate is null) { logger.LogWarning("BanxicoSyncJob: no se obtuvo tasa CETES"); return; }
            await config.UpdateCetesRateAsync(rate.Value, DateTimeOffset.UtcNow, ct);
            logger.LogInformation("BanxicoSyncJob: CETES 28d actualizado a {Rate}", rate);
        }
    }
    ```
  - [x] T3.2 — Registrar job recurrente en `src/Server/Api/Program.cs` (misma sección donde están los demás recurring jobs):
    ```csharp
    recurringJobManager.AddOrUpdate<BanxicoSyncJob>(
        "banxico-cetes-sync",
        j => j.ExecuteAsync(CancellationToken.None),
        "0 6 * * 3"); // miércoles 6 AM UTC
    ```

- [x] T4 — Backend: endpoint GET /api/v1/market/indicadores
  - [x] T4.1 — Crear `src/Server/Api/Endpoints/Private/IndicadoresEndpoints.cs`:
    ```csharp
    // GET /api/v1/market/indicadores — requiere auth User
    // Respuesta: { cetes28d: decimal?, lastUpdated: string? }
    ```
    Registrar en el grupo de endpoints privados (con `.RequireAuthorization("User")`).
  - [x] T4.2 — Definir `IndicadoresDto` en `SharedApiContracts`:
    ```csharp
    public record IndicadoresDto(decimal? Cetes28d, DateTimeOffset? LastUpdated);
    ```
  - [x] T4.3 — Regenerar cliente TypeScript: `npm run codegen:api`

- [x] T5 — SEO: retirar /herramientas de rutas públicas indexadas
  - [x] T5.1 — En `src/Server/Api/Endpoints/Public/SeoEndpoints.cs`, eliminar `"/herramientas"` del array `StaticRoutes`.
  - [x] T5.2 — En `src/Server/Api/Seo/SpaMetadataProvider.cs`, eliminar la entrada `["/herramientas"]` completa.

- [x] T6 — Frontend: lógica pura + tests
  - [x] T6.1 — Crear `src/Web/Main/src/modules/herramientas/herramientas-logic.ts` con las funciones:
    ```ts
    // FIBRAs vs CETES
    export function calcFibraVsCetes(monto: number, yieldFibraPct: number, cetesPct: number, horizonte: number) {
      // fibraNeto = yieldFibraPct / 100 * 0.70 (ISR 30%)
      // cetesNeto = cetesPct / 100 * 0.80 (ISR 20%)
      // capitalFinal = monto * (1 + tasa)^horizonte
      // rentaAcumulada = capitalFinal - monto
    }
    // Meta de renta mensual
    export function calcMetaRenta(rentaMensual: number, yieldPct: number): { capitalNecesario: number | null }
    // Retorno total real
    export function calcRetornoTotal(precioCompra: number, precioActual: number, distribuciones: number, isr: number): { plusvaliaPct: number | null; yieldNetoPct: number | null; retornoTotalPct: number | null }
    ```
  - [x] T6.2 — Crear `src/Web/Main/src/modules/herramientas/herramientas-logic.test.ts` con los casos exactos del Dev Notes (ver abajo). Los tests de denominador=0 van **primero** en cada `describe`.

- [x] T7 — Frontend: reescribir HerramientasPage.tsx
  - [x] T7.1 — Reemplazar el contenido completo de `HerramientasPage.tsx`:
    - Importar `useQuery` (TanStack Query v5) para llamar al endpoint de indicadores
    - Estado local para los 3 calculadores (inputs separados por herramienta)
    - Sección hub (3 cards linkables)
    - Sección herramienta FIBRAs vs CETES
    - Sección herramienta Meta de renta
    - Sección herramienta Retorno total real
    - Sin ninguna mención de "calculadora" en JSX visible al usuario
  - [x] T7.2 — La tasa CETES del API pre-llena el campo `cetes` del estado local al resolverse el query. El usuario puede sobreescribir el valor. Si el query falla o retorna null, el campo queda vacío.
  - [x] T7.3 — Mostrar `—` (no `0`) cuando el resultado de una función de cálculo es `null` (denominador cero o inputs vacíos).

- [x] T8 — Frontend: rutas y navegación
  - [x] T8.1 — En `src/Web/Main/src/app/routes.tsx`, mover `/herramientas` al bloque `ProtectedRoute`:
    ```tsx
    {
      element: <ProtectedRoute />,
      children: [
        { path: '/portafolio', element: p(<PortafolioPage />) },
        { path: '/oportunidades', element: p(<OportunidadesPage />) },
        { path: '/herramientas', element: p(<HerramientasPage />) }, // ← movido
        { path: '/perfil', element: p(<PerfilPage />) },
      ],
    }
    ```
  - [x] T8.2 — En `PublicLayout.tsx`, mover el link `<Link to="/herramientas">Herramientas</Link>` fuera del bloque público (línea 106) y dentro del bloque `{status === 'authenticated'}` (líneas 110-119). Lo mismo en el menú móvil (alrededor de línea 237).

- [x] T9 — Validación y build
  - [x] T9.1 — `dotnet test tests/Unit/` — 0 errores (incluyendo tests del BanxicoClient si existen)
  - [x] T9.2 — `dotnet test tests/Integration/` — 0 errores (incluyendo test del endpoint `/api/v1/market/indicadores`)
  - [x] T9.3 — `cd src/Web/Main && npm test` — 0 errores, ≥9 tests nuevos en `herramientas-logic.test.ts`
  - [x] T9.4 — `cd src/Web/Main && npx tsc --noEmit` — 0 errores TypeScript
  - [x] T9.5 — `dotnet build FIBRADIS.slnx` — 0 errores
  - [x] T9.6 — Gate EF: `dotnet ef migrations list --project src/Server/Infrastructure --startup-project src/Server/Api` — confirmar que `AddCetes28dToOperationalConfig` aparece aplicada

## Dev Notes

### Lógica de cálculo — valores exactos para tests

#### calcFibraVsCetes

ISR FIBRAs: 30% retención sobre distribuciones. ISR CETES: retención provisional 0.97% del capital anual (equivale aprox. 20% del rendimiento para tasas ~9%). Usamos la aproximación 20% para mantener la función pura y comparable.

```ts
// Caso base
it('calcula FIBRA vs CETES con monto=100000, yieldFibra=10, cetes=9.5, horizonte=5', () => {
  const r = calcFibraVsCetes(100000, 10, 9.5, 5)
  // fibraNeto = 0.07, cesteNeto = 0.076
  expect(r.fibra.capitalFinal).toBeCloseTo(140255, 0)
  expect(r.cetes.capitalFinal).toBeCloseTo(144395, 0)
  expect(r.fibra.rendimientoTotalPct).toBeCloseTo(40.25, 0)
  expect(r.cetes.rendimientoTotalPct).toBeCloseTo(44.40, 0)
})
// Denominador / cero (primer test obligatorio)
it('devuelve null cuando monto es 0', () => {
  const r = calcFibraVsCetes(0, 10, 9.5, 5)
  expect(r.fibra.capitalFinal).toBe(0)
  expect(r.cetes.capitalFinal).toBe(0)
})
it('devuelve null cuando horizonte es 0', () => {
  const r = calcFibraVsCetes(100000, 10, 9.5, 0)
  expect(r.fibra.rentaAcumuladaNeta).toBe(0)
})
```

#### calcMetaRenta

```ts
// Denominador cero — primer test obligatorio
it('retorna null cuando yieldPct es 0', () => {
  expect(calcMetaRenta(5000, 0).capitalNecesario).toBeNull()
})
it('retorna null cuando yieldPct es negativo', () => {
  expect(calcMetaRenta(5000, -1).capitalNecesario).toBeNull()
})
it('calcula con rentaMensual=5000, yield=9', () => {
  // capitalNecesario = 5000 * 12 / 0.09 = 666666.67
  expect(calcMetaRenta(5000, 9).capitalNecesario).toBeCloseTo(666667, 0)
})
it('calcula con rentaMensual=10000, yield=10', () => {
  expect(calcMetaRenta(10000, 10).capitalNecesario).toBeCloseTo(1200000, 0)
})
```

#### calcRetornoTotal

```ts
// Denominador cero — primer test obligatorio
it('retorna null cuando precioCompra es 0', () => {
  const r = calcRetornoTotal(0, 22, 2.4, 0.72)
  expect(r.plusvaliaPct).toBeNull()
  expect(r.yieldNetoPct).toBeNull()
  expect(r.retornoTotalPct).toBeNull()
})
it('calcula con precioCompra=20, precioActual=22, dist=2.4, isr=0.72', () => {
  // plusvalia = (22-20)/20 = 10%
  // yieldNeto = (2.4 - 0.72) / 20 = 8.4%
  // retornoTotal = 18.4%
  const r = calcRetornoTotal(20, 22, 2.4, 0.72)
  expect(r.plusvaliaPct).toBeCloseTo(10, 1)
  expect(r.yieldNetoPct).toBeCloseTo(8.4, 1)
  expect(r.retornoTotalPct).toBeCloseTo(18.4, 1)
})
it('maneja plusvalía negativa correctamente', () => {
  const r = calcRetornoTotal(25, 20, 2.4, 0.72)
  expect(r.plusvaliaPct).toBeCloseTo(-20, 1)
  expect(r.retornoTotalPct).toBeCloseTo(-11.6, 1)
})
```

### CBFIs estimados en Meta de renta

Para mostrar "CBFIs estimados" en la herramienta Meta de renta, **no llamar a ningún endpoint adicional**. Usar un precio de referencia configurable en la función: parámetro opcional `precioRef?: number`. Si el dev agent quiere pre-llenar con el precio de una FIBRA específica, puede agregar un selector de FIBRA en una iteración posterior. Por ahora el usuario introduce el precio de referencia (o se muestra solo el capital necesario si no hay precio).

Actualización: simplificar la herramienta Meta de renta a solo 2 outputs: Capital necesario (MXN) y Renta mensual bruta estimada inversa (validación). El campo CBFIs requiere precio de referencia externo — diferirlo a una historia futura si agrega complejidad.

### Banxico SIE API — estructura de respuesta

El endpoint `GET https://www.banxico.org.mx/SieAPIRest/service/v1/series/SF43936/datos/oportuno` con header `Bmx-Token: {token}` responde:
```json
{
  "bmx": {
    "series": [{
      "idSerie": "SF43936",
      "titulo": "Tasa de fondeo bancario a 1 día",
      "datos": [{
        "fecha": "12/06/2026",
        "dato": "9.50"
      }]
    }]
  }
}
```

El campo `dato` es un string que puede ser `"N/E"` cuando no hay dato disponible — tratar como null. El campo `fecha` está en formato `DD/MM/YYYY`.

**Token Banxico:** el token se registra gratis en el portal SIE de Banxico. Para desarrollo local, dejar vacío — la herramienta muestra el campo de CETES vacío con placeholder. Para producción, configurar en secrets o variables de entorno como `Banxico__Token`.

El 400 que puede devolver sin token es esperado — el `BanxicoClient` lo maneja retornando `null` sin lanzar excepción.

### Coordinación SEO ↔ auth (convención activa)

Según `convenciones-fibradis.md`, mover una ruta de pública a privada requiere en **el mismo deploy**:
1. ProtectedRoute en `routes.tsx` ✓ (T8.1)
2. Eliminar de `SpaMetadataProvider.cs` ✓ (T5.2)
3. Eliminar del `sitemap.xml` / `SeoEndpoints.StaticRoutes` ✓ (T5.1)

Las 3 tareas deben completarse antes de hacer push — no en commits separados.

### Patrón de registro del endpoint privado

Ver `src/Server/Api/Endpoints/Private/OpportunityEndpoints.cs` como referencia para el grupo privado con `.RequireAuthorization("User")`.

### Hub section — qué no repetir

La sección hub muestra 3 cards de acceso a herramientas existentes. Las descripciones de cada card deben describir qué hace la herramienta destino, **no** lo que hacen las calculadoras de esta misma página. Ejemplos correctos:
- Comparador: "Compara precio, yield y fundamentales de hasta 4 FIBRAs lado a lado"
- Fichas: "Explora la ficha completa de cada FIBRA: precio, distribuciones, fundamentales y análisis IA"
- Promediar: "Simula cuántos CBFIs adicionales necesitas para promediar tu costo de entrada"

### Nota sobre la herramienta anterior (Yield + ISR)

Las calculadoras Yield y ISR que existían en `HerramientasPage.tsx` **se eliminan**. El widget ISR sigue disponible en cada ficha pública. El cálculo yield sigue en `/herramientas` implícitamente a través de la herramienta FIBRAs vs CETES. No se necesitan rutas de redirección — son herramientas internas sin URLs externas indexadas.

### Sobre la ruta /calculadora

Esta historia **no toca** `/calculadora` (ya tiene la calculadora de compra de 11-5). El nav público sigue mostrando "Calculadora" para usuarios anónimos si está en la nav; eso es scope de otra historia.

### Tests de integración para el endpoint

```csharp
[Fact]
public async Task GetIndicadores_WhenAuthenticated_Returns200WithCetes()
{
    // Requiere un usuario autenticado tipo User
    // Si Cetes28dRate es null en config → retorna { cetes28d: null, lastUpdated: null }
    // Si Cetes28dRate tiene valor → retorna el decimal y la fecha
}
[Fact]
public async Task GetIndicadores_WhenAnonymous_Returns401() { }
```

## Review Findings

- [x] `Review/Patch` BanxicoSyncJob no registrado en DI — Hangfire no puede resolverlo en producción (`ApiServiceExtensions.cs`)
- [x] `Review/Patch` BanxicoClient sin timeout configurado — default 100s puede bloquear el job thread (`ApiServiceExtensions.cs`)
- [x] `Review/Patch` `datos[0]` usa último elemento del array para datos más recientes cuando Banxico retorna múltiples (`BanxicoClient.cs`)
- [x] `Review/Patch` `UpdateCetesRateAsync` lanza excepción sin catch en el job — Hangfire reintenta en loop (`BanxicoSyncJob.cs`)
- [x] `Review/Patch` Tasa ≤ 0 de Banxico no tiene guardia antes de persistir (`BanxicoClient.cs`)
- [x] `Review/Patch` Sin test de yield negativo en `calcFibraVsCetes` — `sanitizeNonNegative` clamps sin error visible (`herramientas-logic.test.ts`)
- [x] `Review/Patch` `BanxicoClientTests` cubre `""` pero no token `null` (`BanxicoClientTests.cs`)
- [x] `Review/Patch` Integration test muta `OperationalConfig` compartido sin cleanup entre tests (`IndicadoresEndpointTests.cs`)
- [x] `Review/Defer` Link Oportunidades no deep-linkea al tab Promediar — limitación arquitectónica pre-existente (tab usa useState, no URL params) — deferred, pre-existing
- [x] `Review/Defer` Token Banxico no usa secrets store — preocupación de infraestructura/deploy, fuera del scope del story — deferred, pre-existing
- [x] `Review/Defer` `UtcNow` en job en vez de fecha publicada por Banxico — spec define explícitamente `DateTimeOffset.UtcNow` — deferred, pre-existing
- [x] `Review/Defer` Sin caching en endpoint `/api/v1/market/indicadores` — optimización no requerida por AC — deferred, pre-existing
- [x] `Review/Defer` Sin estado de error en UI cuando API falla — campo vacío cumple AC5 — deferred, pre-existing
- [x] `Review/Defer` ISR factors hardcoded sin disclaimer adicional — el spec define explícitamente los factores — deferred, pre-existing
- [x] `Review/Defer` `IsRelational()` en repositorio de producción — patrón pre-existente en el codebase — deferred, pre-existing
- [x] `Review/Defer` DB unavailable en endpoint sin try/catch — manejado por el problem-details middleware del framework — deferred, pre-existing

## Dev Agent Record

### Debug Log
- `npm run codegen:api` regeneró `src/Web/SharedApiClient/schema.d.ts` con `GET /api/v1/market/indicadores` y `IndicadoresDto`.
- Validación backend: `dotnet test tests/Unit/Infrastructure.Tests/Infrastructure.Tests.csproj`, `dotnet test tests/Unit/Application.Tests/Application.Tests.csproj`, `dotnet test tests/Unit/Domain.Tests/Domain.Tests.csproj`, `dotnet test tests/Integration/Api.Tests/Api.Tests.csproj`, `dotnet test tests/Integration/Jobs.Tests/Jobs.Tests.csproj`, `dotnet test tests/Integration/Integrations.Tests/Integrations.Tests.csproj`, `dotnet test tests/Integration/Persistence.Tests/Persistence.Tests.csproj`.
- Validación frontend: `cd src/Web/Main && npm test` y `cd src/Web/Main && npx tsc --noEmit`.
- Validación global: `dotnet build FIBRADIS.slnx` y `dotnet ef migrations list --project src/Server/Infrastructure --startup-project src/Server/Api --configuration Release`.
- `tests/Integration/Integrations.Tests` y `tests/Integration/Persistence.Tests` no contienen pruebas detectables en este repositorio; los comandos completaron sin errores de infraestructura.

### Completion Notes
- Se convirtió `/herramientas` en un hub privado autenticado con tres accesos directos y tres herramientas funcionales: FIBRAs vs CETES, Meta de renta y Retorno total.
- Se agregó sincronización automática de CETES 28d desde Banxico con job recurrente Hangfire y persistencia en `OperationalConfig`.
- Se expuso `GET /api/v1/market/indicadores` para prellenar la tasa CETES desde el backend.
- Se retiró `/herramientas` de sitemap y metadata pública, y se movió la ruta y navegación al bloque autenticado.
- Los tests nuevos y la batería de validación quedaron verdes.

## File List

- `_bmad-output/implementation-artifacts/11-6-herramientas-hub-privado.md`
- `Directory.Packages.props`
- `scripts/codegen/Api.json`
- `src/Server/Api/CompositionRoot/ApiServiceExtensions.cs`
- `src/Server/Api/Endpoints/Private/IndicadoresEndpoints.cs`
- `src/Server/Api/Endpoints/Public/SeoEndpoints.cs`
- `src/Server/Api/Program.cs`
- `src/Server/Api/Seo/SpaMetadataProvider.cs`
- `src/Server/Api/appsettings.json`
- `src/Server/Application/Application.csproj`
- `src/Server/Application/Integrations/IBanxicoClient.cs`
- `src/Server/Application/Jobs/BanxicoSyncJob.cs`
- `src/Server/Application/Ops/IOperationalConfigRepository.cs`
- `src/Server/Domain/Ops/OperationalConfig.cs`
- `src/Server/Infrastructure/Integrations/Banxico/BanxicoClient.cs`
- `src/Server/Infrastructure/Migrations/SqlServer/20260612225925_AddCetes28dToOperationalConfig.Designer.cs`
- `src/Server/Infrastructure/Migrations/SqlServer/20260612225925_AddCetes28dToOperationalConfig.cs`
- `src/Server/Infrastructure/Migrations/SqlServer/AppDbContextModelSnapshot.cs`
- `src/Server/Infrastructure/Persistence/Repositories/Ops/OperationalConfigRepository.cs`
- `src/Server/Infrastructure/Persistence/SqlServer/Configurations/Ops/OperationalConfigConfiguration.cs`
- `src/Server/SharedApiContracts/Market/IndicadoresDto.cs`
- `src/Web/Main/package.json`
- `src/Web/Main/src/api/fibrasApi.ts`
- `src/Web/Main/src/app/routes.tsx`
- `src/Web/Main/src/modules/herramientas/HerramientasPage.tsx`
- `src/Web/Main/src/modules/herramientas/herramientas-logic.test.ts`
- `src/Web/Main/src/modules/herramientas/herramientas-logic.ts`
- `src/Web/Main/src/shared/layouts/PublicLayout.tsx`
- `src/Web/SharedApiClient/schema.d.ts`
- `tests/Integration/Api.Tests/IndicadoresEndpointTests.cs`
- `tests/Integration/Api.Tests/OpenApiEndpointTests.cs`
- `tests/Unit/Infrastructure.Tests/Endpoints/SeoEndpointsTests.cs`
- `tests/Unit/Infrastructure.Tests/Integrations/Banxico/BanxicoClientTests.cs`
- `tests/Unit/Infrastructure.Tests/Jobs/BanxicoSyncJobTests.cs`
- `tests/Unit/Infrastructure.Tests/Persistence/Repositories/OperationalConfigRepositoryTests.cs`
- `tests/Unit/Infrastructure.Tests/Seo/SpaMetadataProviderTests.cs`

## Change Log

- 2026-06-12: Implemented private herramientas hub with CETES sync, indicators endpoint, SEO/auth routing updates, shared API regeneration, and validation coverage.
