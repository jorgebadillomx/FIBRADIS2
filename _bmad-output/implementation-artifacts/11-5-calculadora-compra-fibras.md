# Story 11.5: Calculadora de Compra de FIBRAs

Status: done

## Story

Como visitante o inversionista,
quiero ingresar un presupuesto por FIBRA y ver cuántos CBFIs puedo comprar, qué distribución recibiría y cuánta renta bruta proyectada tendría,
para planificar mis inversiones en FIBRAs de forma rápida sin salir del sitio.

## Acceptance Criteria

1. **Dado que** visito `/calculadora`, **cuando** la página carga, **entonces** veo una tabla con todas las FIBRAs activas con las columnas: $ a calcular, Fibra, Periodo, CBFIs, $ Sobra, Precio, Dist. CBFI, Dist. CBFI Anual, Renta Bruta, Renta Bruta Anual.

2. **Dado que** ingreso un monto en "$ a calcular" para una FIBRA, **cuando** el valor cambia, **entonces** se actualizan en tiempo real: CBFIs = `floor(monto / precio)`, $ Sobra = `monto − CBFIs × precio`, Renta Bruta = `CBFIs × Dist. CBFI`, Renta Bruta Anual = `CBFIs × Dist. CBFI Anual`.

3. **Dado que** una FIBRA tiene distribuciones en la base de datos, **cuando** la tabla muestra el Periodo, **entonces** aparece en formato `Qn-YYYY` (ej. `Q1-2026`) correspondiente al trimestre de la distribución más reciente. Si ese trimestre tiene más de una distribución, se suman en `Dist. CBFI`.

4. **Dado que** el campo Dist. CBFI Anual, **cuando** la tabla muestra la FIBRA, **entonces** el valor es la suma de todas las distribuciones cuyo `PaymentDate` cae en el mismo año calendario que la distribución más reciente.

5. **Dado que** una FIBRA no tiene distribuciones registradas, **cuando** la tabla muestra esa FIBRA, **entonces** Periodo, Dist. CBFI y Dist. CBFI Anual muestran `—`, y Renta Bruta / Renta Bruta Anual muestran `—`.

6. **Dado que** el monto ingresado está vacío o es 0, **cuando** la tabla calcula, **entonces** CBFIs = 0, $ Sobra = 0, Renta Bruta = 0, Renta Bruta Anual = 0 (no `—`; el input está activo y listo para recibir valor).

7. **Dado que** existe un input de búsqueda sobre la tabla, **cuando** escribo texto en él, **entonces** la tabla filtra en tiempo real mostrando solo las FIBRAs cuyo ticker o nombre contiene el texto (case-insensitive). Al borrar el texto se restauran todas las filas.

8. **Dado que** hago clic en el header de cualquier columna numérica o de texto (Fibra, Periodo, CBFIs, $ Sobra, Precio, Dist. CBFI, Dist. CBFI Anual, Renta Bruta, Renta Bruta Anual), **cuando** hago clic, **entonces** la tabla se ordena por esa columna de forma ascendente. Un segundo clic ordena descendente. Un tercer clic vuelve al orden original (sin ordenamiento). El header muestra un indicador visual del estado: `↑` ascendente, `↓` descendente, sin indicador cuando no hay orden activo. La columna "$ a calcular" no es ordenable.

9. **Dado que** el contenido anterior de CalculadoraPage (ISR educativo + banner "próximamente") existe, **cuando** esta historia se implementa, **entonces** ese contenido es reemplazado completamente por la nueva calculadora interactiva.

10. La metadata SEO de `/calculadora` en `SpaMetadataProvider.cs` se actualiza: title y description reflejan la calculadora de compra de CBFIs, no la ISR.

## Tasks / Subtasks

- [x] T1 — Backend: nuevo endpoint `GET /api/v1/market/calculadora`
  - [x] T1.1 — Crear `src/Server/Api/Endpoints/Public/CalculadoraEndpoints.cs`; registrar el grupo `market` (prefijo `/api/v1/market`) y mapear `GET /calculadora` como `AllowAnonymous`
  - [x] T1.2 — Definir en `SharedApiContracts` (o inline si no aplica):
    ```csharp
    public record CalculadoraFibraDto(
        string Ticker,
        string Empresa,
        decimal? PrecioActual,
        string? UltimoPeriodo,      // "Q1-2026" | null
        decimal? DistCbfi,          // suma del último trimestre
        decimal? DistCbfiAnual,     // suma del año calendario del último dist.
        string FreshnessStatus
    );
    ```
  - [x] T1.3 — Implementar lógica de cálculo de distribuciones (helper estático, sin dependencia de `distribuciones.ts`):
    - Ordenar distribuciones por `PaymentDate` descendente
    - `lastDate` = primera distribución; `lastYear` y `lastQuarter = (month-1)/3+1`
    - `Dist. CBFI` = suma de `AmountPerUnit` donde `Year == lastYear && Quarter == lastQuarter`
    - `Dist. CBFI Anual` = suma de `AmountPerUnit` donde `Year == lastYear`
    - `UltimoPeriodo` = `$"Q{lastQuarter}-{lastYear}"`
    - Si sin distribuciones → todos null
  - [x] T1.4 — Usar window de al menos 400 días al llamar `GetDistributionsByFibrasAsync` para garantizar un año completo de datos
  - [x] T1.5 — Regenerar cliente TypeScript: `npm run codegen:api`

- [x] T2 — Funciones puras de cálculo frontend
  - [x] T2.1 — Crear `src/Web/Main/src/modules/calculadora/calculadora-logic.ts`:
    ```ts
    export function calcCbfis(monto: number, precio: number): number {
      if (precio <= 0) return 0
      return Math.floor(monto / precio)
    }
    export function calcSobra(monto: number, cbfis: number, precio: number): number {
      return monto - cbfis * precio
    }
    export function calcRentaBruta(cbfis: number, distCbfi: number | null | undefined): number | null {
      if (distCbfi == null) return null
      return cbfis * distCbfi
    }
    export function calcRentaBrutaAnual(cbfis: number, distCbfiAnual: number | null | undefined): number | null {
      if (distCbfiAnual == null) return null
      return cbfis * distCbfiAnual
    }
    ```
  - [x] T2.2 — Crear `src/Web/Main/src/modules/calculadora/calculadora-logic.test.ts` cubriendo:
    - `calcCbfis`: monto=1000, precio=22.34 → 44; monto=0 → 0; precio=0 → 0
    - `calcSobra`: monto=1000, cbfis=44, precio=22.34 → 17.04 (aprox)
    - `calcRentaBruta`: cbfis=44, distCbfi=0.60 → 26.40; distCbfi=null → null
    - `calcRentaBrutaAnual`: cbfis=44, distCbfiAnual=2.40 → 105.60; distCbfiAnual=null → null
    - `calcCbfis` con precio negativo → 0

- [x] T3 — Frontend: reemplazar CalculadoraPage.tsx con tabla interactiva
  - [x] T3.1 — Agregar función `fetchCalculadoraFibras()` al API client generado (usa el endpoint de T1)
  - [x] T3.2 — Reemplazar todo el contenido actual de `CalculadoraPage.tsx` con:
    - `useQuery(['calculadora'], fetchCalculadoraFibras)` de TanStack Query v5
    - Estado `const [montos, setMontos] = useState<Record<string, string>>({})` (ticker → string)
    - Tabla con columnas según AC-1; sticky header con scroll horizontal en mobile
    - Input numérico por fila: ancho ~100px, placeholder `"0"`, sin validación de formato
    - Valores calculados usando funciones de `calculadora-logic.ts`
    - Si monto vacío/NaN → tratar como 0 (AC-6)
    - Si distCbfi/distCbfiAnual null → mostrar `—` en Renta Bruta/Renta Bruta Anual (AC-5)
    - Formato: MXN 2 dec para $ sobra y rentas, enteros para CBFIs, 4 dec para distribuciones
  - [x] T3.3 — Filtro por fibra (AC-7):
    - Input de búsqueda sobre la tabla, placeholder `"Buscar fibra..."`
    - Estado `const [filtro, setFiltro] = useState('')`
    - Filtrar `data` por `ticker.includes(filtro) || empresa.includes(filtro)` (case-insensitive con `.toLowerCase()`)
    - El filtro se aplica antes del sort; los montos ingresados se preservan aunque la fila deje de ser visible
  - [x] T3.4 — Ordenamiento de columnas (AC-8):
    - Estado `const [sort, setSort] = useState<{ col: string; dir: 'asc' | 'desc' | null }>({ col: '', dir: null })`
    - Click en header de columna ordenable cicla: `null → 'asc' → 'desc' → null`
    - Columnas ordenables: Fibra (string), Periodo (string), Precio, Dist. CBFI, Dist. CBFI Anual, CBFIs, $ Sobra, Renta Bruta, Renta Bruta Anual (todas numéricas salvo Fibra y Periodo)
    - Columna "$ a calcular" sin indicador de sort y sin handler de click
    - Valores `null` van al final en cualquier dirección
    - Indicador visual en header: `↑` / `↓` / sin icono
  - [x] T3.5 — Estado de carga: skeleton de tabla mientras carga el query
  - [x] T3.6 — Estado de error: mensaje simple si el endpoint falla

- [x] T4 — Actualizar metadata SEO
  - [x] T4.1 — En `SpaMetadataProvider.cs`, actualizar la entrada `["/calculadora"]`:
    - Title: `"Calculadora de FIBRAs — ¿Cuántos CBFIs puedo comprar? | FIBRADIS"`
    - Description: `"Calcula cuántos CBFIs puedes comprar con tu presupuesto, qué distribución recibirías y tu renta bruta estimada para cada FIBRA inmobiliaria mexicana."`
    - JSON-LD: mantener tipo `SoftwareApplication`, actualizar `name` y `description` para reflejar calculadora de compra

- [x] T5 — Validación y build
  - [x] T5.1 — `cd src/Web/Main && npm test` — esperar: todos los tests pasan incluyendo ≥6 nuevos en `calculadora-logic.test.ts`
  - [x] T5.2 — `cd src/Web/Main && npx tsc --noEmit` — esperar: 0 errores
  - [x] T5.3 — `dotnet build FIBRADIS.slnx` — esperar: 0 errores

## Dev Notes

### Contexto del shell existente

`CalculadoraPage.tsx` actualmente es una página estática educativa sobre ISR con un banner "Calculadora interactiva próximamente". Esta historia reemplaza ese contenido por completo. El route `/calculadora` en `routes.tsx` **no cambia**. La inyección de metadata SSR en `SpaMetadataMiddleware` tampoco cambia; solo se actualiza el contenido en `SpaMetadataProvider.cs`.

### Por qué nuevo endpoint y no reusar snapshots

`GET /api/v1/market/snapshots` ya carga distribuciones internamente pero no las expone en `MarketSnapshotDto`. Extender ese DTO rompe el contrato existente. El endpoint nuevo reutiliza la misma infraestructura de repositorio con una ventana de 400 días para garantizar un año completo de datos.

### Cálculo de trimestre en backend

```csharp
var lastQuarter = (lastDate.Month - 1) / 3 + 1;
// Ene-Mar → 1, Abr-Jun → 2, Jul-Sep → 3, Oct-Dic → 4
```

FIBRAs con cadencia mensual: varias distribuciones por trimestre → se suman correctamente en `Dist. CBFI`. FIBRAs semi-anuales: `Dist. CBFI Anual` será menor que `Dist. CBFI × 4` porque solo hay 1-2 distribuciones en el año — correcto por diseño.

### NO usar distribuciones.ts del frontend

Las funciones `inferDistributionCadence` y `getDistributionPeriodLabel` de `ficha-publica/sections/distribuciones.ts` son para la sección de historial de la ficha pública. No usarlas en la calculadora; el backend ya retorna el periodo formateado.

### Registro del endpoint en Program.cs / EndpointGroups

Seguir el mismo patrón de registro que `MarketEndpoints.cs`. Verificar que `CalculadoraEndpoints.cs` se registra en el grupo público de mercado.

### Pipeline de datos en el componente

El orden de transformación sobre `data` (resultado del query) es:

```text
data (raw) → filteredData (filtro texto) → sortedData (sort columna) → render
```

Ambos se derivan con `useMemo` para evitar recálculos innecesarios. Los `montos` ingresados están en estado separado indexado por `ticker`, por lo que no se pierden al filtrar o reordenar.

### Formato de números en tabla

| Campo | Formato |
|---|---|
| $ a calcular (input) | texto libre |
| Precio | `$XX.XX` (MXN 2 dec) |
| Dist. CBFI | `$X.XXXX` (MXN 4 dec) |
| Dist. CBFI Anual | `$X.XXXX` (MXN 4 dec) |
| CBFIs | entero sin decimales |
| $ Sobra | `$XX.XX` (MXN 2 dec) |
| Renta Bruta | `$XX.XX` (MXN 2 dec) |
| Renta Bruta Anual | `$XX.XX` (MXN 2 dec) |

## Dev Agent Record

- Implementé el endpoint público `GET /api/v1/market/calculadora` con DTO nuevo, helper de distribución trimestral/anual y ventana de 400 días para cubrir un año completo de datos.
- Reemplacé `CalculadoraPage.tsx` por una tabla interactiva con búsqueda, ordenamiento, inputs persistentes por ticker y cálculos puros para CBFIs, sobra y rentas.
- Actualicé la metadata SEO de `/calculadora` para reflejar la calculadora de compra de FIBRAs y regeneré el cliente tipado con `codegen:api`.
- Añadí tests unitarios e integración para el helper y el endpoint, más cobertura de la lógica frontend.
- Validación ejecutada: `dotnet build FIBRADIS.slnx`, `npm run codegen:api`, `npm test --workspace=src/Web/Main`, `dotnet test tests/Unit/Infrastructure.Tests/Infrastructure.Tests.csproj`, `dotnet test tests/Integration/Api.Tests/Api.Tests.csproj`, `npx tsc --noEmit`, `npm run build --workspace=src/Web/Main`.

## Review Findings

### Senior Developer Review (AI)

**Patches (3):**

- [x] P1 `parseMonto` reemplaza solo la primera coma — `"1,000"` → `"1.000"` → NaN → 0 en vez de 1000. Cambiar a `.replace(/,/g, '')` (`CalculadoraPage.tsx`)
- [x] P2 `sobra` muestra el monto completo cuando no hay snapshot de precio — `precioActual=null` → `priceForCalc=0` → `sobra = monto − 0×0 = monto`. Agregar guard cuando `priceForCalc === 0` (`CalculadoraPage.tsx`)
- [x] P3 `calcCbfis` acepta `monto` negativo — `calcCbfis(-1000, 22.34)` devuelve `-45` CBFIs. Agregar guard `monto <= 0` (`calculadora-logic.ts` + test)

**Defers (4):**

- [x] D1 FIBRA sin pagos en los últimos 400 días muestra `distCbfi=null` igual que una FIBRA sin historial registrado; sin indicador de distinción (`CalculadoraEndpoints.cs`) — deferred, diseño actual
- [x] D2 `distCbfiAnual` puede aparecer casi igual a `distCbfi` cuando los pagos anuales están concentrados en un solo trimestre; UX confusa para FIBRAs de pago irregular (`CalculadoraDistributionCalculator.cs`) — deferred, diseño actual
- [x] D3 Ordenamiento por CBFIs/Sobra no distingue FIBRAs sin precio (muestran 0) de FIBRAs con monto vacío (también 0); ambas caen al mismo grupo en el sort (`CalculadoraPage.tsx`) — deferred, diseño actual
- [x] D4 Endpoint público `/api/v1/market/calculadora` sin output cache ejecuta 3 queries a BD por cada request anónimo; bajo carga puede ser costoso (`CalculadoraEndpoints.cs`) — deferred, mejora de rendimiento futura
