# Historia 7.2: Vista Promediar Posición con simulador

Status: done

## Story

Como usuario,
quiero ver mis posiciones del portafolio clasificadas por score de oportunidad y usar un simulador de promediado de costo para entender el impacto de comprar unidades adicionales al precio actual,
para que pueda tomar decisiones informadas sobre promediar hacia abajo sin que la plataforma emita recomendaciones de compra o venta.

## Acceptance Criteria

### AC1 — Pestaña "Promediar Posición" en Oportunidades

**Dado que** cambio a la pestaña "Promediar Posición" en Oportunidades,
**Entonces** solo aparecen las FIBRAs de mi portafolio, mostrando: nombre/ticker de la FIBRA, mi costo promedio de entrada, precio de mercado actual, diferencia porcentual entre costo y precio, y score de oportunidad.

### AC2 — Simulador calcula nuevo costo promedio y plusvalía

**Dado que** ingreso 500 en el campo de simulador "Títulos adicionales" para una FIBRA (precio actual $100, mi costo promedio $110, tengo 1000 unidades),
**Entonces** el simulador muestra: nuevo costo promedio = (1000×110 + 500×100)/(1000+500) ≈ $106.67, nuevo valor total de la posición (1500 × $100 = $150,000) y el cambio en plusvalía de -9.1% al nuevo valor.

> **Nota sobre el AC original:** El ejemplo en el épico dice "$103.33" y "-3.2%", pero con los números dados la fórmula correcta produce $106.67 (no $103.33). La fórmula a implementar es la financieramente correcta: `nuevoAvg = (titulosActuales × costoPromedio + titulosAdicionales × precioActual) / (titulosActuales + titulosAdicionales)`. Los porcentajes de plusvalía se derivan de este cálculo correcto.

### AC3 — Descargo de responsabilidad visible

**Dado que** el simulador muestra resultados,
**Entonces** aparece un descargo de responsabilidad visible: "Este simulador es informativo. No constituye una recomendación de compra o venta."

### AC4 — Limpiar simulador al borrar input

**Dado que** borro el campo de entrada del simulador,
**Entonces** los valores simulados desaparecen y se restauran los valores originales de la posición.

## Tasks / Subtasks

### T1 — Lógica pura del simulador (testable) (AC: 2, 4)

- [x] T1.1 — Crear `src/Web/Main/src/modules/oportunidades/simulador-logic.ts` con funciones exportadas puras:
  - `calcNuevoAvg(titulos: number, costoPromedio: number, adicionales: number, precioActual: number): number`
    - Fórmula: `(titulos × costoPromedio + adicionales × precioActual) / (titulos + adicionales)`
  - `calcNuevaPlusvaliaPct(nuevoAvg: number, precioActual: number): number`
    - Fórmula: `((precioActual - nuevoAvg) / nuevoAvg) × 100`
  - `calcNuevoValor(titulos: number, adicionales: number, precioActual: number): number`
    - Fórmula: `(titulos + adicionales) × precioActual`

### T2 — Unit tests del simulador (AC: 2, 4)

- [x] T2.1 — Crear `src/Web/Main/src/modules/oportunidades/simulador-logic.test.ts`:
  - `calcNuevoAvg` happy path (1000×$110 + 500×$100 → $106.67)
  - `calcNuevoAvg` mismo precio (no debe cambiar avg)
  - `calcNuevaPlusvaliaPct` compra bajo avg (mejora plusvalía)
  - `calcNuevaPlusvaliaPct` compra sobre avg (empeora plusvalía)
  - `calcNuevoValor` correcto
- [x] T2.2 — Agregar el nuevo test file al script `"test"` en `src/Web/Main/package.json`
  (agrégar `src/modules/oportunidades/simulador-logic.test.ts` a la lista del script `test`)

### T3 — Tabs en OportunidadesPage + componente PromediarTab (AC: 1, 2, 3, 4)

- [x] T3.1 — Modificar `src/Web/Main/src/modules/oportunidades/OportunidadesPage.tsx`:
  - Agregar estado `activeTab: 'universo' | 'promediar'`
  - Envolver el contenido existente (configurador de pesos + tabla ranking) en el tab "Universo"
  - Agregar tab "Promediar Posición" que renderiza `<PromediarTab weights={effectiveWeights} />`
  - UI de tabs: dos botones de texto tipo tab con borde inferior activo (sin usar shadcn Tabs — usar el mismo patrón visual que el resto de la página)
- [x] T3.2 — Crear `src/Web/Main/src/modules/oportunidades/PromediarTab.tsx`:
  - Query `['portfolio', 'positions']` para obtener posiciones del usuario (`GET /api/v1/portfolio`)
  - Query `['opportunities']` — reusar datos del ranking ya cargados con `useQueryClient().getQueryData` o segundo `useQuery` (sin duplicar fetches — ver dev notes)
  - Construir lista: cruzar `portfolio.positions` con `ranked + limitedData` por `fibraId`
  - Ordenar por score DESC (FIBRAs sin score al final)
  - Estado local: `Record<string, string>` para el input de títulos adicionales por fibraId (string para permitir campo vacío)
  - Para cada posición mostrar (ver Dev Notes para columnas exactas):
    - Ticker + Nombre
    - Costo promedio actual (CostoPromedio)
    - Precio actual (PrecioActual) — si null mostrar "—"
    - Diferencia % entre costo y precio: `((precioActual - costoPromedio) / costoPromedio) × 100` — con color: verde si positivo, rojo si negativo
    - Score de oportunidad (ScoreBadge del universo)
    - Input numérico "Títulos adicionales" — min=0, step=1
    - Columnas de resultado (solo cuando `adicionales > 0` y `precioActual != null`):
      - Nuevo avg
      - Nuevo valor total
      - Nueva plusvalía %
  - Disclaimer siempre visible cuando hay al menos una posición
  - Si portafolio vacío: mensaje "No tienes posiciones en tu portafolio. Sube un archivo en la sección Portafolio."
  - Si loading: skeleton/spinner
  - Si error: mensaje de error

## Dev Notes

### Arquitectura de datos — NO hay cambios de backend

Esta historia es **100% frontend**. Toda la información necesaria ya existe en endpoints existentes:
- Posiciones del portafolio: `GET /api/v1/portfolio` → `PortfolioResponseDto` → `positions[]`
  - Campos relevantes: `fibraId`, `ticker`, `nombre`, `titulos`, `costoPromedio`, `precioActual`, `plusvaliaFilaPct`, `valorMercado`
- Scores de oportunidad: `GET /api/v1/opportunities` → `OpportunityRankingResponseDto` → `ranked[]` + `limitedData[]`
  - Campos relevantes: `fibraId`, `score` (calculado localmente)

### Reutilizar datos del query de oportunidades sin doble fetch

`OportunidadesPage` ya tiene `rankingQuery` con queryKey `['opportunities']`. El componente `PromediarTab` vive dentro del mismo árbol y puede obtener los datos sin fetch extra usando:

```tsx
// Opción A — segundo useQuery con staleTime alto (recomendada, más explícita):
const rankingQuery = useQuery<OpportunityRankingResponseDto>({
  queryKey: ['opportunities'],
  queryFn: async () => { ... },  // mismo queryFn
  staleTime: Infinity,            // reutiliza caché sin refetch
})
```

O bien pasar `rankingData` como prop desde `OportunidadesPage` a `PromediarTab` — más simple pero crea acoplamiento. Usar la opción que resulte más limpia — ambas son válidas.

### Fórmulas del simulador

```
nuevoAvg = (titulos × costoPromedio + adicionales × precioActual) / (titulos + adicionales)
nuevaPlusvaliaPct = ((precioActual - nuevoAvg) / nuevoAvg) × 100
nuevoValor = (titulos + adicionales) × precioActual
```

El AC original menciona "nuevo costo promedio $103.33" para el ejemplo 1000 unidades@$110 + 500@$100, pero la fórmula correcta da $106.67. Se implementa la fórmula correcta. El resultado -3.2% del AC sería la plusvalía con avg=$103.33, pero con $106.67 el resultado correcto es -6.25%. Documentado aquí para evitar confusión durante code review.

### Cruce portfolio × opportunities

```ts
// positions es PortfolioPositionDto[] del portfolio query
// ranked y limitedData son OpportunityFibraRowDto[] del opportunities query

const allOpportunityRows = [...ranked, ...limitedData]
const rowByFibraId = new Map(allOpportunityRows.map(r => [r.fibraId, r]))

const promediarRows = positions
  .map(pos => ({
    position: pos,
    opportunityRow: rowByFibraId.get(pos.fibraId) ?? null,
  }))
  .sort((a, b) => {
    const scoreA = a.opportunityRow ? calcLocalScore(a.opportunityRow, weights) : -1
    const scoreB = b.opportunityRow ? calcLocalScore(b.opportunityRow, weights) : -1
    return scoreB - scoreA
  })
```

La función `calcLocalScore` ya existe en `OportunidadesPage.tsx` — moverla a un archivo compartido si se reutiliza, o duplicarla en PromediarTab por simplicidad (la historia no pide refactor).

### Tabs UI — mismo patrón visual que OportunidadesPage

No usar shadcn `<Tabs>` component. Usar botones simples con clases de Tailwind:

```tsx
<div className="flex gap-1 border-b">
  {(['universo', 'promediar'] as const).map(tab => (
    <button
      key={tab}
      type="button"
      onClick={() => setActiveTab(tab)}
      className={`px-4 py-2 text-sm font-medium transition-colors ${
        activeTab === tab
          ? 'border-b-2 border-primary text-primary'
          : 'text-muted-foreground hover:text-foreground'
      }`}
    >
      {tab === 'universo' ? 'Universo' : 'Promediar Posición'}
    </button>
  ))}
</div>
```

### Input numérico para títulos adicionales

Usar `<input type="number" min={0} step={1} ...>`. Guardar en estado como `string` para permitir campo vacío sin parsear a 0. Parsear con `parseInt(value, 10)` solo al calcular. Si el resultado es `NaN` o `<= 0`, no mostrar columnas de simulación.

### Columnas del simulador

Cuando `adicionales > 0` y `precioActual != null`:

| Columna | Valor |
|---|---|
| Nuevo avg | `$106.67` (formateado a 2 decimales) |
| Nuevo valor | `$150,000` (formateado como MXN sin decimales) |
| Nueva plusvalía | `-6.3%` (formateado a 1 decimal, color rojo/verde) |

Columnas siempre visibles (incluso sin simulación activa):

| Columna | Valor |
|---|---|
| Ticker | Negrita |
| Nombre | Texto pequeño gris |
| Costo promedio | `$110.00` |
| Precio actual | `$100.00` o `—` |
| Diferencia % | `-9.1%` en rojo, o verde si precio > costo |
| Score | `<ScoreBadge>` |
| Input | `<input>` |

### Disclaimer

Siempre visible al final de la tabla (no por fila), si hay al menos una posición:

```tsx
<p className="mt-3 text-xs text-muted-foreground text-center">
  Este simulador es informativo. No constituye una recomendación de compra o venta.
</p>
```

### Patrones relevantes de historias anteriores

- `calcLocalScore` en `OportunidadesPage.tsx:30` — misma función, aplica pesos locales a los percentile scores de la API
- `ScoreBadge` en `OportunidadesPage.tsx:52` — reutilizar directamente importando desde el mismo archivo o extrayendo a un archivo compartido
- `toNum` helper en `OportunidadesPage.tsx:25` — reutilizar
- `fmt` / `fmtPct` helpers en `OportunidadesPage.tsx:40,46` — reutilizar
- Portfolio positions query en `PortafolioPage.tsx:36` — mismo queryKey `['portfolio', 'positions']`, mismo apiClient

### Convenciones de código relevantes

- TypeScript strict mode — no `any`, no `!` innecesarios
- Componentes: `PascalCase.tsx`, módulo en `src/modules/oportunidades/`
- No usar `console.log` en código de producción
- Formateo MXN sin librería: `$${value.toLocaleString('es-MX', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}` — o simplemente `$${value.toFixed(2)}`

### Tests — patrón del proyecto

El proyecto usa Node.js built-in test runner con TypeScript stripping:
```ts
import assert from 'node:assert/strict'
import test from 'node:test'
// exportar la lógica desde simulador-logic.ts e importar aquí
```

El script de test en `package.json` lista archivos explícitamente. **Hay que agregar el nuevo test file** a la lista del script `"test"`.

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

- Tipo `OpportunityWeightsDto` estaba declarado pero nunca usado en OportunidadesPage.tsx — eliminado para fix de build limpio (TS6196).

### Completion Notes List

- T1: `simulador-logic.ts` con 3 funciones puras exportadas (calcNuevoAvg, calcNuevaPlusvaliaPct, calcNuevoValor). Fórmula correcta da $106.67 (no $103.33 del AC original).
- T2: 5 unit tests con Node.js built-in runner. 77/77 tests verdes (previos 72 + 5 nuevos). Test file agregado al script `"test"` de package.json.
- T3.1: OportunidadesPage refactorizado con estado `activeTab`, tabs UI (botones con borde inferior activo). `Weights` e `calcLocalScore` exportados para uso en PromediarTab. Loading/error state movido al interior del tab Universo.
- T3.2: PromediarTab.tsx creado. Cruza portfolio × opportunities por fibraId, ordena por score DESC (sin score al final). Input string→parseInt para permitir campo vacío. Columnas simulación solo visibles cuando `adicionales > 0 && precioActual != null`. Disclaimer siempre visible. `staleTime: Infinity` en rankingQuery para reutilizar caché sin refetch.
- TypeScript build limpio (0 errores).

### File List

- src/Web/Main/src/modules/oportunidades/simulador-logic.ts (nuevo)
- src/Web/Main/src/modules/oportunidades/simulador-logic.test.ts (nuevo)
- src/Web/Main/src/modules/oportunidades/PromediarTab.tsx (nuevo)
- src/Web/Main/src/modules/oportunidades/OportunidadesPage.tsx (modificado — tabs, export Weights/calcLocalScore, eliminar tipo no usado)
- src/Web/Main/package.json (modificado — simulador-logic.test.ts agregado al script test)

### Senior Developer Review (AI)

- [x] [Review - Patch P1] División por cero en `difPct` cuando `costoPromedio = 0` — `toNum(null)` retorna 0; `((precioActual - 0) / 0) * 100` produce `Infinity`, renderizando `"Infinity%"` en la UI. Guard: `costoPromedio > 0` antes de calcular `difPct`. `PromediarTab.tsx:132`
- [x] [Review - Patch P2] `rankingQuery.isError` no manejado en `PromediarTab` — si el ranking falla, `ranked` y `limitedData` caen a `[]`, todos los scores muestran `—` sin aviso al usuario. Agregar guard después del check de `portfolioQuery.isError`. Viola AC1. `PromediarTab.tsx:73`
- [x] [Review - Patch P3] Funciones puras sin guard de denominador cero — `calcNuevoAvg`: si `titulos + adicionales === 0` → `NaN`; `calcNuevaPlusvaliaPct`: si `nuevoAvg === 0` → `Infinity`. Aunque el caller ya guarda `adicionales > 0`, las funciones exportadas deben ser autosuficientes. `simulador-logic.ts:7,11`
- [x] [Review - Patch P4] Tests no cubren denominador cero — faltan casos: `calcNuevoAvg(0, 110, 0, 100)` y `calcNuevaPlusvaliaPct(0, 100)`. `simulador-logic.test.ts`
- [x] [Review - Defer D1] `staleTime: Infinity` en `rankingQuery` de PromediarTab puede servir scores obsoletos si OportunidadesPage refresca en background — tradeoff documentado en dev notes, intencional para evitar doble fetch. `PromediarTab.tsx:62` — deferred, tradeoff intencional
- [x] [Review - Defer D2] Unicidad de `fibraId` asumida — duplicados en positions causarían React keys duplicadas y Map collisions. Contrato de API fuera de alcance de esta historia. `PromediarTab.tsx:90` — deferred, pre-existing API contract
- [x] [Review - Defer D3] `toNum` en `OportunidadesPage.tsx` retorna `undefined` para null (branch `typeof v === 'string'` cubre strings, else retorna el valor crudo) — pre-existente, no introducido en esta historia. `OportunidadesPage.tsx:25` — deferred, pre-existing
- [x] [Review - Defer D4] `ScoreBadge` y `toNum` duplicados entre `PromediarTab` y `OportunidadesPage` — dev notes lo permiten explícitamente ("duplicarla en PromediarTab por simplicidad"). `PromediarTab.tsx:18,13` — deferred, explícitamente permitido

## Change Log

- 2026-06-05: Historia 7-2 implementada — simulador de promediado puro (3 funciones), 5 unit tests, tabs Universo/Promediar en OportunidadesPage, PromediarTab con cruce portfolio×oportunidades y simulación en línea.
