import { useEffect, useId, useMemo, useState } from 'react'
import { usePageTitle } from '@/shared/hooks/usePageTitle'
import { Link } from 'react-router'
import { ArrowUpRight, Plus, Search, X } from 'lucide-react'
import { useQuery } from '@tanstack/react-query'
import { fetchCalculadoraFibras, fetchIndicadores, type CalculadoraFibraDto } from '@/api/fibrasApi'
import { formatMoney, formatPercent } from '@/modules/portafolio/portfolio-format'
import {
  calcFibraVsCetes,
  calcMetaRenta,
  calcRetornoTotal,
  parseNumberInput,
} from './herramientas-logic'

const HORIZON_OPTIONS = [1, 3, 5, 10] as const
const MAX_FIBRAS = 4

type FibraWithYield = CalculadoraFibraDto & { yieldPct: number | null }

export function HerramientasPage() {
  const [selectedTickers, setSelectedTickers] = useState<string[]>([])
  const [search, setSearch] = useState('')
  const [isSearchFocused, setIsSearchFocused] = useState(false)
  const [fibraMonto, setFibraMonto] = useState('100000')
  const [fibraCetes, setFibraCetes] = useState('')
  const [fibraHorizonte, setFibraHorizonte] = useState<(typeof HORIZON_OPTIONS)[number]>(5)
  const [metaRentaMensual, setMetaRentaMensual] = useState('5000')
  const [retornoPrecioCompra, setRetornoPrecioCompra] = useState('20')
  const [retornoIsr, setRetornoIsr] = useState('0')
  const [cetesTouched, setCetesTouched] = useState(false)

  const indicadoresQuery = useQuery({
    queryKey: ['herramientas', 'indicadores'],
    queryFn: fetchIndicadores,
    staleTime: 5 * 60 * 1000,
    retry: false,
  })
  const cetesApiValue = normalizeRate(indicadoresQuery.data?.cetes28d)
  const tiieApiValue = normalizeRate(indicadoresQuery.data?.tiie28d)

  const calculadoraQuery = useQuery({
    queryKey: ['herramientas', 'calculadora'],
    queryFn: async () => {
      try {
        return await fetchCalculadoraFibras()
      } catch {
        return []
      }
    },
    staleTime: 60 * 60 * 1000,
    retry: false,
  })

  useEffect(() => {
    if (cetesTouched) return
    if (cetesApiValue == null) return
    setFibraCetes(cetesApiValue.toFixed(2))
  }, [cetesApiValue, cetesTouched])

  const allFibras = calculadoraQuery.data ?? []

  const calculadoraByTicker = useMemo(
    () => new Map(allFibras.map((f) => [f.ticker.toUpperCase(), f])),
    [allFibras],
  )

  const selectedSet = useMemo(() => new Set(selectedTickers), [selectedTickers])

  const selectedFibrasWithYield = useMemo<FibraWithYield[]>(
    () =>
      selectedTickers
        .map((t) => calculadoraByTicker.get(t))
        .filter((f): f is CalculadoraFibraDto => f != null)
        .map((f) => {
          const precio = f.precioActual != null ? Number(f.precioActual) : null
          const distAnual = f.distCbfiAnual != null ? Number(f.distCbfiAnual) : null
          const yieldPct =
            precio != null && precio > 0 && distAnual != null && distAnual > 0
              ? (distAnual / precio) * 100
              : null
          return { ...f, yieldPct }
        }),
    [selectedTickers, calculadoraByTicker],
  )

  const suggestionRows = useMemo(() => {
    const term = search.trim().toLowerCase()
    const pool = allFibras.filter((f) => !selectedSet.has(f.ticker.toUpperCase()))
    if (!term) return pool.slice(0, 8)
    return pool
      .filter(
        (f) =>
          f.ticker.toLowerCase().includes(term) || f.empresa.toLowerCase().includes(term),
      )
      .slice(0, 8)
  }, [allFibras, search, selectedSet])

  function addTicker(ticker: string) {
    if (selectedTickers.length >= MAX_FIBRAS) return
    setSelectedTickers((prev) => [...prev, ticker.toUpperCase()])
    setSearch('')
    setIsSearchFocused(selectedTickers.length + 1 < MAX_FIBRAS)
  }

  function removeTicker(ticker: string) {
    setSelectedTickers((prev) => prev.filter((t) => t !== ticker))
  }

  const selectorDisabled = selectedTickers.length >= MAX_FIBRAS
  const showSuggestions = isSearchFocused && suggestionRows.length > 0
  const hasSelection = selectedTickers.length > 0

  const fibraMontoValue = parseNumberInput(fibraMonto)
  const fibraCetesValue = parseNumberInput(fibraCetes)
  const metaRentaMensualValue = parseNumberInput(metaRentaMensual)
  const retornoPrecioCompraValue = parseNumberInput(retornoPrecioCompra)
  const retornoIsrValue = parseNumberInput(retornoIsr)

  const cetesScenario = useMemo(() => {
    if (fibraMontoValue == null || fibraCetesValue == null) return null
    return calcFibraVsCetes(fibraMontoValue, 0, fibraCetesValue, fibraHorizonte).cetes
  }, [fibraMontoValue, fibraCetesValue, fibraHorizonte])

  const fibraScenarios = useMemo(
    () =>
      selectedFibrasWithYield.map((f) => ({
        ticker: f.ticker,
        yieldPct: f.yieldPct,
        scenario:
          f.yieldPct != null && fibraMontoValue != null
            ? calcFibraVsCetes(fibraMontoValue, f.yieldPct, fibraCetesValue ?? 0, fibraHorizonte)
                .fibra
            : null,
      })),
    [selectedFibrasWithYield, fibraMontoValue, fibraCetesValue, fibraHorizonte],
  )

  const metaResults = useMemo(
    () =>
      selectedFibrasWithYield.map((f) => {
        const precioNum = f.precioActual != null ? Number(f.precioActual) : undefined
        return {
          ticker: f.ticker,
          yieldPct: f.yieldPct,
          precioActual: precioNum,
          result:
            f.yieldPct != null && metaRentaMensualValue != null
              ? calcMetaRenta(metaRentaMensualValue, f.yieldPct, precioNum)
              : null,
        }
      }),
    [selectedFibrasWithYield, metaRentaMensualValue],
  )

  const retornoResults = useMemo(
    () =>
      selectedFibrasWithYield.map((f) => {
        const precioActual = f.precioActual != null ? Number(f.precioActual) : null
        const distAnual = f.distCbfiAnual != null ? Number(f.distCbfiAnual) : null
        return {
          ticker: f.ticker,
          precioActual,
          distCbfiAnual: distAnual,
          result:
            precioActual != null &&
            distAnual != null &&
            retornoPrecioCompraValue != null &&
            retornoIsrValue != null
              ? calcRetornoTotal(retornoPrecioCompraValue, precioActual, distAnual, retornoIsrValue)
              : null,
        }
      }),
    [selectedFibrasWithYield, retornoPrecioCompraValue, retornoIsrValue],
  )

  usePageTitle('Herramientas — Fibras Inmobiliarias')

  return (
    <>
      <div className="relative overflow-hidden bg-[radial-gradient(circle_at_15%_10%,rgba(194,65,12,0.14),transparent_28%),radial-gradient(circle_at_85%_20%,rgba(15,118,110,0.10),transparent_24%),linear-gradient(180deg,rgba(10,14,26,0.02),transparent_28%)]">
        <div className="container mx-auto px-4 py-8 md:py-10">
          <header className="max-w-4xl space-y-4">
            <div className="inline-flex items-center gap-2 rounded-full border border-border bg-surface-elevated px-3 py-1 text-[11px] font-semibold uppercase tracking-[0.28em] text-primary shadow-sm">
              Hub privado
            </div>
            <h1 className="font-playfair text-4xl font-bold leading-tight text-foreground md:text-5xl">
              Herramientas para decidir con más contexto
            </h1>
            <p className="max-w-3xl text-sm leading-6 text-muted-foreground md:text-base">
              Cruza rendimiento, ingreso objetivo y retorno total sin salir de la plataforma. Los
              accesos rápidos conectan con las superficies donde ya existe contexto detallado.
            </p>
          </header>

          <section className="mt-8 rounded-3xl border border-border bg-surface-elevated/95 p-5 shadow-sm backdrop-blur">
            <div className="flex items-start justify-between gap-4">
              <div className="space-y-2">
                <p className="text-xs font-semibold uppercase tracking-[0.24em] text-muted-foreground">
                  Accesos rápidos
                </p>
                <h2 className="font-playfair text-2xl font-semibold text-foreground">
                  Entra a las superficies clave
                </h2>
              </div>
            </div>
            <div className="mt-5 grid gap-4 md:grid-cols-3">
              <HubLinkCard
                href="/comparar"
                title="Comparador de FIBRAs"
                description="Compara precio, yield, fundamentales y score de hasta 4 emisoras lado a lado."
              />
              <HubLinkCard
                href="/fibras"
                title="Fichas de FIBRAs"
                description="Explora el catálogo completo con precio, distribuciones, fundamentales y análisis."
              />
              <HubLinkCard
                href="/oportunidades"
                title="Promediar en Oportunidades"
                description="Simula cuántos títulos adicionales necesitas para mejorar tu costo de entrada."
              />
            </div>
          </section>

          {/* Selector de FIBRAs */}
          <section className="mt-8 rounded-3xl border border-border bg-surface-elevated/95 p-5 shadow-sm backdrop-blur">
            <SectionHeader
              eyebrow="Referencia de datos"
              title="Selecciona tus FIBRAs"
              description="Elige de 1 a 4 emisoras. Sus datos reales de precio, distribuciones TTM y yield se usarán automáticamente en las calculadoras."
            />

            <div className="mt-5">
              <div className="relative max-w-2xl">
                <label className="space-y-1.5">
                  <span className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                    Agregar FIBRA
                  </span>
                  <div className="relative">
                    <Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
                    <input
                      aria-label="Buscar FIBRAs para las herramientas"
                      disabled={selectorDisabled}
                      onBlur={() => setIsSearchFocused(false)}
                      onChange={(e) => setSearch(e.target.value)}
                      onFocus={() => setIsSearchFocused(true)}
                      placeholder={
                        selectorDisabled ? 'Máximo 4 FIBRAs seleccionadas' : 'Ticker o nombre...'
                      }
                      value={search}
                      className="flex h-11 w-full rounded-xl border border-input bg-background px-10 text-sm text-foreground placeholder:text-muted-foreground outline-none transition-colors focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 disabled:cursor-not-allowed disabled:bg-muted/40"
                    />
                    {selectorDisabled ? (
                      <span className="pointer-events-none absolute right-3 top-1/2 -translate-y-1/2 rounded-full border border-border bg-muted px-2 py-0.5 text-[10px] font-semibold uppercase tracking-[0.2em] text-muted-foreground">
                        Límite 4
                      </span>
                    ) : null}
                  </div>
                </label>

                {showSuggestions ? (
                  <div className="absolute z-20 mt-2 max-h-72 w-full overflow-auto rounded-xl border border-border bg-background shadow-lg">
                    {suggestionRows.map((fibra) => (
                      <button
                        key={fibra.ticker}
                        type="button"
                        onMouseDown={(e) => e.preventDefault()}
                        onClick={() => addTicker(fibra.ticker)}
                        className="flex w-full items-center justify-between gap-3 border-b border-border px-4 py-3 text-left text-sm transition-colors last:border-b-0 hover:bg-muted/50"
                      >
                        <div className="min-w-0">
                          <div className="flex items-center gap-2">
                            <span className="font-mono font-semibold text-primary">
                              {fibra.ticker}
                            </span>
                            <span className="text-xs text-muted-foreground">Agregar</span>
                          </div>
                          <p className="truncate text-xs text-muted-foreground">{fibra.empresa}</p>
                        </div>
                        <Plus className="size-4 shrink-0 text-muted-foreground" />
                      </button>
                    ))}
                  </div>
                ) : null}

                <p className="mt-2 text-xs text-muted-foreground">
                  {calculadoraQuery.isLoading
                    ? 'Cargando universo activo...'
                    : 'Usa ticker o nombre de la emisora.'}
                </p>
              </div>

              {selectedTickers.length > 0 ? (
                <div className="mt-4 flex flex-wrap gap-2">
                  {selectedTickers.map((ticker) => {
                    const fibra = calculadoraByTicker.get(ticker)
                    const withYield = selectedFibrasWithYield.find((f) => f.ticker === ticker)
                    return (
                      <div
                        key={ticker}
                        className="inline-flex items-center gap-2.5 rounded-full border border-border bg-background px-3 py-1.5"
                      >
                        <div className="min-w-0">
                          <span className="font-mono text-xs font-semibold text-primary">
                            {ticker}
                          </span>
                          {withYield?.yieldPct != null ? (
                            <span className="ml-2 text-xs text-muted-foreground">
                              {withYield.yieldPct.toFixed(2)}% TTM
                            </span>
                          ) : null}
                          {fibra?.empresa ? (
                            <span className="ml-1 hidden text-xs text-muted-foreground sm:inline">
                              · {fibra.empresa}
                            </span>
                          ) : null}
                        </div>
                        <button
                          type="button"
                          title={`Quitar ${ticker}`}
                          onClick={() => removeTicker(ticker)}
                          className="rounded-full p-1 text-muted-foreground transition-colors hover:bg-muted/70 hover:text-foreground"
                        >
                          <X className="size-3.5" />
                        </button>
                      </div>
                    )
                  })}
                </div>
              ) : null}
            </div>
          </section>

          {!hasSelection ? (
            <div className="mt-8 rounded-3xl border border-dashed border-border bg-background px-6 py-12 text-center">
              <p className="text-sm font-medium text-foreground">
                Selecciona al menos una FIBRA para activar las calculadoras.
              </p>
              <p className="mt-1 text-sm text-muted-foreground">
                Sus datos de precio y distribuciones se usarán automáticamente.
              </p>
            </div>
          ) : (
            <>
              <div className="mt-8 grid gap-6 xl:grid-cols-[minmax(0,1.1fr)_minmax(0,0.9fr)]">
                {/* FIBRAs vs CETES */}
                <section className="rounded-3xl border border-border bg-surface-elevated p-5 shadow-sm">
                  <SectionHeader
                    eyebrow="Rentabilidad comparada"
                    title="FIBRAs vs CETES"
                    description="Compara crecimiento compuesto con tasas netas estimadas y horizonte flexible."
                  />

                  <div className="mt-5 grid gap-4 sm:grid-cols-3">
                    <NumberField
                      label="Monto (MXN)"
                      value={fibraMonto}
                      onChange={setFibraMonto}
                      placeholder="100000"
                    />
                    <div className="space-y-1.5">
                      <NumberField
                        label="Tasa CETES 28d (%)"
                        value={fibraCetes}
                        onChange={(value) => {
                          setCetesTouched(true)
                          setFibraCetes(value)
                        }}
                        placeholder="ej. 9.50"
                      />
                      {tiieApiValue != null && (
                        <p className="text-xs text-muted-foreground">
                          TIIE 28d vigente: {tiieApiValue.toFixed(2)}%
                        </p>
                      )}
                    </div>
                    <label className="space-y-1.5">
                      <span className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                        Horizonte
                      </span>
                    <select
                      id="herramientas-horizonte"
                      name="horizonte"
                      value={fibraHorizonte}
                      onChange={(e) =>
                        setFibraHorizonte(
                            Number(e.target.value) as (typeof HORIZON_OPTIONS)[number],
                          )
                        }
                        className="flex h-11 w-full rounded-xl border border-input bg-background px-3 text-sm text-foreground outline-none transition-colors focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50"
                      >
                        {HORIZON_OPTIONS.map((o) => (
                          <option key={o} value={o}>
                            {o} año{o === 1 ? '' : 's'}
                          </option>
                        ))}
                      </select>
                    </label>
                  </div>

                  <div className="mt-5 overflow-x-auto rounded-2xl border border-border">
                    <table className="w-full min-w-max text-sm">
                      <thead className="bg-muted/30 text-xs font-semibold uppercase tracking-[0.18em] text-muted-foreground">
                        <tr>
                          <th className="px-4 py-3 text-left">Métrica</th>
                          {fibraScenarios.map((f) => (
                            <th key={f.ticker} className="px-4 py-3 text-right">
                              <span className="font-mono text-primary">{f.ticker}</span>
                              {f.yieldPct != null ? (
                                <span className="ml-1 text-[10px] font-normal text-muted-foreground">
                                  {f.yieldPct.toFixed(1)}%
                                </span>
                              ) : null}
                            </th>
                          ))}
                          <th className="px-4 py-3 text-right">CETES</th>
                        </tr>
                      </thead>
                      <tbody className="divide-y divide-border">
                        <tr>
                          <th className="px-4 py-3 text-left font-medium text-foreground">
                            Capital final estimado
                          </th>
                          {fibraScenarios.map((f) => (
                            <td
                              key={f.ticker}
                              className="px-4 py-3 text-right tabular-nums text-foreground"
                            >
                              {f.scenario ? formatMoney(f.scenario.capitalFinal) : '—'}
                            </td>
                          ))}
                          <td className="px-4 py-3 text-right tabular-nums text-foreground">
                            {cetesScenario ? formatMoney(cetesScenario.capitalFinal) : '—'}
                          </td>
                        </tr>
                        <tr>
                          <th className="px-4 py-3 text-left font-medium text-foreground">
                            Renta acumulada neta de ISR
                          </th>
                          {fibraScenarios.map((f) => (
                            <td
                              key={f.ticker}
                              className="px-4 py-3 text-right tabular-nums text-foreground"
                            >
                              {f.scenario ? formatMoney(f.scenario.rentaAcumuladaNeta) : '—'}
                            </td>
                          ))}
                          <td className="px-4 py-3 text-right tabular-nums text-foreground">
                            {cetesScenario ? formatMoney(cetesScenario.rentaAcumuladaNeta) : '—'}
                          </td>
                        </tr>
                        <tr>
                          <th className="px-4 py-3 text-left font-medium text-foreground">
                            Rendimiento total %
                          </th>
                          {fibraScenarios.map((f) => (
                            <td
                              key={f.ticker}
                              className="px-4 py-3 text-right tabular-nums text-foreground"
                            >
                              {f.scenario ? formatPercent(f.scenario.rendimientoTotalPct) : '—'}
                            </td>
                          ))}
                          <td className="px-4 py-3 text-right tabular-nums text-foreground">
                            {cetesScenario
                              ? formatPercent(cetesScenario.rendimientoTotalPct)
                              : '—'}
                          </td>
                        </tr>
                      </tbody>
                    </table>
                  </div>

                  <p className="mt-3 text-xs leading-5 text-muted-foreground">
                    El yield de cada FIBRA es TTM (últimos 12 meses). Neto aplica 70% sobre
                    distribuciones FIBRA y 80% sobre CETES.
                  </p>
                </section>

                {/* Meta de renta */}
                <section className="rounded-3xl border border-border bg-surface-elevated p-5 shadow-sm">
                  <SectionHeader
                    eyebrow="Ingreso objetivo"
                    title="Meta de renta"
                    description="Capital requerido para la renta mensual objetivo, calculado por cada FIBRA seleccionada."
                  />

                  <div className="mt-5">
                    <NumberField
                      label="Renta mensual objetivo (MXN)"
                      value={metaRentaMensual}
                      onChange={setMetaRentaMensual}
                      placeholder="5000"
                    />
                  </div>

                  <div className="mt-5 overflow-hidden rounded-2xl border border-border">
                    <table className="w-full text-sm">
                      <thead className="bg-muted/30 text-xs font-semibold uppercase tracking-[0.18em] text-muted-foreground">
                        <tr>
                          <th className="px-4 py-3 text-left">FIBRA</th>
                          <th className="px-4 py-3 text-right">Yield TTM</th>
                          <th className="px-4 py-3 text-right">Capital necesario</th>
                          <th className="px-4 py-3 text-right">CBFIs est.</th>
                        </tr>
                      </thead>
                      <tbody className="divide-y divide-border">
                        {metaResults.map((r) => (
                          <tr key={r.ticker}>
                            <th className="px-4 py-3 text-left font-mono font-semibold text-primary">
                              {r.ticker}
                            </th>
                            <td className="px-4 py-3 text-right tabular-nums text-foreground">
                              {r.yieldPct != null ? formatPercent(r.yieldPct) : '—'}
                            </td>
                            <td className="px-4 py-3 text-right tabular-nums text-foreground">
                              {r.result ? formatMoney(r.result.capitalNecesario) : '—'}
                            </td>
                            <td className="px-4 py-3 text-right tabular-nums text-foreground">
                              {r.result?.cbfisEstimados != null
                                ? formatInteger(r.result.cbfisEstimados)
                                : '—'}
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                </section>
              </div>

              {/* Retorno total */}
              <section className="mt-6 rounded-3xl border border-border bg-surface-elevated p-5 shadow-sm">
                <SectionHeader
                  eyebrow="Retorno real"
                  title="Retorno total"
                  description="Integra precio de compra e ISR retenido con el precio actual y distribuciones TTM de cada emisora."
                />

                <div className="mt-5 grid gap-4 sm:grid-cols-2">
                  <NumberField
                    label="Precio de compra"
                    value={retornoPrecioCompra}
                    onChange={setRetornoPrecioCompra}
                    placeholder="20"
                  />
                  <NumberField
                    label="ISR retenido total"
                    value={retornoIsr}
                    onChange={setRetornoIsr}
                    placeholder="0"
                  />
                </div>

                <div className="mt-5 overflow-x-auto rounded-2xl border border-border">
                  <table className="w-full min-w-max text-sm">
                    <thead className="bg-muted/30 text-xs font-semibold uppercase tracking-[0.18em] text-muted-foreground">
                      <tr>
                        <th className="px-4 py-3 text-left">FIBRA</th>
                        <th className="px-4 py-3 text-right">Precio actual</th>
                        <th className="px-4 py-3 text-right">Dist TTM</th>
                        <th className="px-4 py-3 text-right">Plusvalía %</th>
                        <th className="px-4 py-3 text-right">Yield neto %</th>
                        <th className="px-4 py-3 text-right">Retorno total %</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-border">
                      {retornoResults.map((r) => (
                        <tr key={r.ticker}>
                          <th className="px-4 py-3 text-left font-mono font-semibold text-primary">
                            {r.ticker}
                          </th>
                          <td className="px-4 py-3 text-right tabular-nums text-foreground">
                            {r.precioActual != null ? formatMoney(r.precioActual) : '—'}
                          </td>
                          <td className="px-4 py-3 text-right tabular-nums text-foreground">
                            {r.distCbfiAnual != null ? formatMoney(r.distCbfiAnual) : '—'}
                          </td>
                          <td className="px-4 py-3 text-right tabular-nums text-foreground">
                            {r.result ? formatPercent(r.result.plusvaliaPct) : '—'}
                          </td>
                          <td className="px-4 py-3 text-right tabular-nums text-foreground">
                            {r.result ? formatPercent(r.result.yieldNetoPct) : '—'}
                          </td>
                          <td className="px-4 py-3 text-right tabular-nums text-foreground">
                            {r.result ? formatPercent(r.result.retornoTotalPct) : '—'}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </section>
            </>
          )}
        </div>
      </div>
    </>
  )
}

function SectionHeader({
  eyebrow,
  title,
  description,
}: {
  eyebrow: string
  title: string
  description: string
}) {
  return (
    <div className="space-y-2">
      <p className="text-xs font-semibold uppercase tracking-[0.24em] text-muted-foreground">
        {eyebrow}
      </p>
      <h2 className="font-playfair text-2xl font-semibold text-foreground">{title}</h2>
      <p className="max-w-2xl text-sm leading-6 text-muted-foreground">{description}</p>
    </div>
  )
}

function HubLinkCard({
  href,
  title,
  description,
}: {
  href: string
  title: string
  description: string
}) {
  return (
    <Link
      to={href}
      className="group rounded-2xl border border-border bg-background/80 p-4 transition-all duration-150 hover:-translate-y-0.5 hover:border-primary hover:shadow-md"
    >
      <div className="flex items-start justify-between gap-3">
        <div className="space-y-2">
          <p className="text-base font-semibold text-foreground">{title}</p>
          <p className="text-sm leading-6 text-muted-foreground">{description}</p>
        </div>
        <ArrowUpRight className="size-4 shrink-0 text-muted-foreground transition-transform duration-150 group-hover:translate-x-0.5 group-hover:-translate-y-0.5 group-hover:text-primary" />
      </div>
    </Link>
  )
}

function NumberField({
  label,
  value,
  onChange,
  placeholder,
}: {
  label: string
  value: string
  onChange: (value: string) => void
  placeholder: string
}) {
  const autoId = useId().replace(/:/g, '')
  const id = `number-field-${buildFieldName(label)}-${autoId}`
  const name = buildFieldName(label)

  return (
    <div className="space-y-1.5">
      <label htmlFor={id} className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
        {label}
      </label>
      <input
        id={id}
        name={name}
        type="number"
        inputMode="decimal"
        step="any"
        value={value}
        onChange={(e) => onChange(e.target.value)}
        placeholder={placeholder}
        className="flex h-11 w-full rounded-xl border border-input bg-background px-3 text-sm text-foreground placeholder:text-muted-foreground outline-none transition-colors focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50"
      />
    </div>
  )
}

function buildFieldName(seed: string) {
  const normalized = seed
    .normalize('NFKD')
    .replace(/[\u0300-\u036f]/g, '')
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '')

  return normalized || 'field'
}

function formatInteger(value: number): string {
  return value.toLocaleString('es-MX', { maximumFractionDigits: 0 })
}

function normalizeRate(value: number | string | null | undefined): number | null {
  if (value == null) return null
  const normalized = typeof value === 'number' ? value : Number(value)
  return Number.isFinite(normalized) ? normalized : null
}
