import { useEffect, useMemo, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import type { components } from '@fibradis/shared-api-client'
import { apiClient } from '@/api/fibrasApi'
import { type Weights, calcLocalScore } from '@/modules/oportunidades/OportunidadesPage'
import { fetchFiscalRates } from '@/api/fiscalRatesApi'
import { fetchIndicadores } from '@/api/fibrasApi'
import { calcRealReturn, cumulative2yInflation } from '@/shared/lib/inflation-utils'
import {
  IVA_FACTOR,
  calcCostoPurchase,
  calcNewAvgCost,
  calcNuevoAvg,
  calcNuevaPlusvaliaPct,
  calcNuevoValor,
  calcRentaProyectadaAnual,
  calcTitulosParaRentaTarget,
} from '@/modules/oportunidades/simulador-logic'
import { formatMoney, formatPercent } from '@/modules/portafolio/portfolio-format'

type PortfolioPositionDto = components['schemas']['PortfolioPositionDto']
type PortfolioResponseDto = components['schemas']['PortfolioResponseDto']
type PortfolioConfigDto = components['schemas']['PortfolioConfigDto']
type OpportunityFibraRowDto = components['schemas']['OpportunityFibraRowDto']
type OpportunityRankingResponseDto = components['schemas']['OpportunityRankingResponseDto']
type FibraHistoryDto = components['schemas']['FibraHistoryDto']

function toNum(v: null | number | string | undefined): number {
  if (v == null) return 0
  return typeof v === 'string' ? parseFloat(v) : v
}

function ScoreBadge({ score }: { score: number }) {
  const cls =
    score >= 65 ? 'bg-green-100 text-green-900 border-green-200' :
    score >= 35 ? 'bg-yellow-100 text-yellow-900 border-yellow-200' :
    'bg-red-100 text-red-900 border-red-200'
  return (
    <span className={`inline-block rounded-full border px-2 py-0.5 text-xs font-semibold ${cls}`}>
      {score.toFixed(1)}
    </span>
  )
}

function fmtMxn(v: number): string {
  return `$${v.toLocaleString('es-MX', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`
}

function fmtMxnNoDecimals(v: number): string {
  return `$${v.toLocaleString('es-MX', { minimumFractionDigits: 0, maximumFractionDigits: 0 })}`
}

interface PromediarRow {
  position: PortfolioPositionDto
  opportunityRow: OpportunityFibraRowDto | null
}

export function PromediarTab({ weights }: { weights: Weights }) {
  const [adicionales, setAdicionales] = useState<Record<string, string>>({})
  const [whatIfFibraId, setWhatIfFibraId] = useState<string>('')
  const [whatIfTitulos, setWhatIfTitulos] = useState<string>('')
  const [whatIfTargetRenta, setWhatIfTargetRenta] = useState<string>('')

  const portfolioQuery = useQuery<PortfolioResponseDto>({
    queryKey: ['portfolio', 'positions'],
    queryFn: async () => {
      const { data, error } = await apiClient.GET('/api/v1/portfolio', {})
      if (error || !data) throw new Error('No se pudo cargar el portafolio.')
      return data
    },
  })

  const rankingQuery = useQuery<OpportunityRankingResponseDto>({
    queryKey: ['opportunities'],
    queryFn: async () => {
      const { data, error } = await apiClient.GET('/api/v1/opportunities', {})
      if (error || !data) throw new Error('No se pudo cargar el ranking.')
      return data
    },
    staleTime: Infinity,
  })

  const configQuery = useQuery<PortfolioConfigDto | null>({
    queryKey: ['portfolio', 'config'],
    queryFn: async () => {
      const { data, error } = await apiClient.GET('/api/v1/portfolio/config', {})
      if (error || !data) return null
      return data
    },
    staleTime: Infinity,
    retry: false,
  })

  const fiscalRatesQuery = useQuery({
    queryKey: ['fiscal-rates'],
    queryFn: fetchFiscalRates,
    staleTime: 10 * 60_000,
  })
  const ivaFactor = fiscalRatesQuery.data?.ivaRate ?? IVA_FACTOR

  const indicadoresQuery = useQuery({
    queryKey: ['opportunities', 'indicadores'],
    queryFn: fetchIndicadores,
    staleTime: 5 * 60_000,
  })
  const inflacion2y = cumulative2yInflation(indicadoresQuery.data?.inpcHistory)

  const positions = portfolioQuery.data?.positions ?? []
  const ranked = rankingQuery.data?.ranked ?? []
  const limitedData = rankingQuery.data?.limitedData ?? []
  const allOpportunityRows = [...ranked, ...limitedData]
  const rowByFibraId = new Map(allOpportunityRows.map((r) => [r.fibraId, r]))
  const positionByFibraId = new Map(positions.map((p) => [p.fibraId, p]))

  const promediarRows: PromediarRow[] = useMemo(() => (
    positions
      .map((pos) => ({
        position: pos,
        opportunityRow: rowByFibraId.get(pos.fibraId) ?? null,
      }))
      .sort((a, b) => {
        const scoreA = a.opportunityRow ? calcLocalScore(a.opportunityRow, weights) : -1
        const scoreB = b.opportunityRow ? calcLocalScore(b.opportunityRow, weights) : -1
        return scoreB - scoreA
      })
  // eslint-disable-next-line react-hooks/exhaustive-deps
  ), [positions, weights])

  useEffect(() => {
    if (!whatIfFibraId && allOpportunityRows[0]) {
      setWhatIfFibraId(allOpportunityRows[0].fibraId)
    }
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [allOpportunityRows, whatIfFibraId])

  // Find selected what-if opportunity row (all fibras)
  const selectedOpportunityRow = allOpportunityRows.find((r) => r.fibraId === whatIfFibraId) ?? allOpportunityRows[0] ?? null
  // Find portfolio position if user holds this fibra
  const selectedPosition = selectedOpportunityRow ? positionByFibraId.get(selectedOpportunityRow.fibraId) ?? null : null

  const selectedTicker = selectedOpportunityRow?.ticker ?? ''

  // History query for 2-year return panel
  const historyQuery = useQuery<FibraHistoryDto | null>({
    queryKey: ['market', 'history', selectedTicker, '2y'],
    queryFn: async () => {
      if (!selectedTicker) return null
      const { data, error } = await apiClient.GET('/api/v1/market/fibras/{ticker}/history', {
        params: { path: { ticker: selectedTicker }, query: { period: '2y' } },
      })
      if (error || !data) return null
      return data
    },
    enabled: !!selectedTicker,
    staleTime: 5 * 60 * 1000,
  })

  if (portfolioQuery.isLoading || rankingQuery.isLoading) {
    return (
      <div className="flex h-64 items-center justify-center">
        <p className="text-muted-foreground">Cargando posiciones…</p>
      </div>
    )
  }

  if (portfolioQuery.isError) {
    return <p className="text-destructive">Error al cargar el portafolio.</p>
  }

  if (rankingQuery.isError) {
    return <p className="text-destructive">Error al cargar el ranking de oportunidades.</p>
  }

  if (positions.length === 0) {
    return (
      <p className="py-8 text-center text-sm text-muted-foreground">
        No tienes posiciones en tu portafolio. Sube un archivo en la sección Portafolio.
      </p>
    )
  }

  const commissionFactor = toNum(configQuery.data?.commissionFactor) ?? 0
  const additionalWhatIfTitles = parseInt(whatIfTitulos, 10)
  const currentTitulos = selectedPosition ? toNum(selectedPosition.titulos) : 0
  const currentAvgCost = selectedPosition ? toNum(selectedPosition.costoPromedio) : 0
  const currentPrice = selectedOpportunityRow?.precioActual != null
    ? toNum(selectedOpportunityRow.precioActual)
    : (selectedPosition?.precioActual != null ? toNum(selectedPosition.precioActual) : null)
  const portfolioValue = toNum(portfolioQuery.data?.kpis?.valorTotal)

  const canSimulateWhatIf =
    selectedOpportunityRow != null &&
    Number.isInteger(additionalWhatIfTitles) &&
    additionalWhatIfTitles > 0 &&
    currentPrice != null

  const newAvgCost = canSimulateWhatIf
    ? calcNewAvgCost(
        currentTitulos,
        currentAvgCost,
        currentPrice!,
        additionalWhatIfTitles,
        commissionFactor,
      )
    : null

  const deltaVsPricePct =
    newAvgCost != null && currentPrice != null && currentPrice > 0
      ? ((newAvgCost - currentPrice) / currentPrice) * 100
      : null

  const newPortfolioPct =
    canSimulateWhatIf && portfolioValue != null && portfolioValue > 0
      ? (((currentTitulos + additionalWhatIfTitles) * currentPrice!) /
          (portfolioValue + additionalWhatIfTitles * currentPrice!)) * 100
      : null

  const dividendYieldPct = selectedOpportunityRow?.dividendYieldPct != null
    ? toNum(selectedOpportunityRow.dividendYieldPct)
    : null
  const currentRentaAnual = toNum(selectedPosition?.rentaAnual)

  const rentaAnualEstimada = canSimulateWhatIf && currentPrice != null
    ? calcRentaProyectadaAnual(
        currentRentaAnual,
        additionalWhatIfTitles,
        currentPrice,
        dividendYieldPct,
        currentTitulos,
      )
    : null
  const rentaMensualEstimada = rentaAnualEstimada != null ? rentaAnualEstimada / 12 : null

  const costoPurchaseWhatIf = canSimulateWhatIf && currentPrice != null
    ? calcCostoPurchase(currentPrice, additionalWhatIfTitles, commissionFactor, ivaFactor)
    : null

  const targetRentaMensualNum = parseFloat(whatIfTargetRenta)
  const hasTargetRenta = !isNaN(targetRentaMensualNum) && targetRentaMensualNum > 0

  const titulosTotalesParaTarget = hasTargetRenta && selectedOpportunityRow != null
    ? calcTitulosParaRentaTarget(
        targetRentaMensualNum,
        currentPrice ?? 0,
        dividendYieldPct,
        currentTitulos,
        currentRentaAnual,
      )
    : null
  const titulosAdicionalesParaTarget = titulosTotalesParaTarget != null
    ? Math.max(0, titulosTotalesParaTarget - currentTitulos)
    : null
  const costoInversionParaTarget = titulosAdicionalesParaTarget != null
    ? calcCostoPurchase(currentPrice ?? 0, titulosAdicionalesParaTarget, commissionFactor, ivaFactor)
    : null
  const targetYaCumplido = hasTargetRenta && titulosTotalesParaTarget != null
    && currentTitulos >= titulosTotalesParaTarget
  const sinDatosRenta = hasTargetRenta && selectedOpportunityRow != null && titulosTotalesParaTarget === null

  // Handlers with bidirectional sync
  function handleTitulosChange(value: string) {
    setWhatIfTitulos(value)
    const n = parseInt(value, 10)
    if (Number.isInteger(n) && n > 0 && currentPrice != null) {
      const renta = calcRentaProyectadaAnual(currentRentaAnual, n, currentPrice, dividendYieldPct, currentTitulos) / 12
      setWhatIfTargetRenta(String(Math.round(renta)))
    } else if (value === '' || value === '0') {
      setWhatIfTargetRenta('')
    }
  }

  function handleRentaChange(value: string) {
    setWhatIfTargetRenta(value)
    const target = parseFloat(value)
    if (!isNaN(target) && target > 0 && selectedOpportunityRow != null) {
      const total = calcTitulosParaRentaTarget(target, currentPrice ?? 0, dividendYieldPct, currentTitulos, currentRentaAnual)
      const adicionales = total != null ? Math.max(0, total - currentTitulos) : null
      setWhatIfTitulos(adicionales != null ? String(adicionales) : '')
    } else if (value === '' || value === '0') {
      setWhatIfTitulos('')
    }
  }

  // Panel retorno 2 años
  const priceHistory = historyQuery.data?.priceHistory ?? []
  const distributions = historyQuery.data?.distributions ?? []
  const twoYearsAgo = new Date()
  twoYearsAgo.setFullYear(twoYearsAgo.getFullYear() - 2)
  const twoYearsAgoStr = twoYearsAgo.toISOString().slice(0, 10)

  const precioInicial = priceHistory.length > 0 ? toNum(priceHistory[0].close) : null
  const invertidoPanel = Math.max(0, parseFloat(whatIfTargetRenta) || 0)
  const cbfisPanel = precioInicial != null && precioInicial > 0 && invertidoPanel > 0
    ? Math.floor(invertidoPanel / precioInicial)
    : 0
  const montoInvertidoReal = cbfisPanel > 0 && precioInicial != null ? cbfisPanel * precioInicial : 0

  const distIn2y = distributions.filter((d) => (d.date ?? '') >= twoYearsAgoStr)
  const dividendosPorCbfi = distIn2y.reduce((sum, d) => sum + toNum(d.amountPerUnit), 0)
  const dividendosRecibidos = cbfisPanel * dividendosPorCbfi
  const valorHoyPanel = cbfisPanel * (currentPrice ?? 0)
  const variacionCapital = montoInvertidoReal > 0 ? valorHoyPanel - montoInvertidoReal : 0
  const variacionPorcentual = montoInvertidoReal > 0 ? (variacionCapital / montoInvertidoReal) * 100 : 0
  const rendimientoTotalPesos = variacionCapital + dividendosRecibidos
  const rendimientoTotalPct = montoInvertidoReal > 0 ? (rendimientoTotalPesos / montoInvertidoReal) * 100 : 0
  const rendimientoAnualizado = rendimientoTotalPct > -100
    ? ((1 + rendimientoTotalPct / 100) ** 0.5 - 1) * 100
    : null

  const rendimientoRealAnualizado =
    rendimientoAnualizado != null && inflacion2y != null
      ? calcRealReturn(rendimientoAnualizado, inflacion2y > -100 ? ((1 + inflacion2y / 100) ** 0.5 - 1) * 100 : 0)
      : null

  const hasPanelData = precioInicial != null && priceHistory.length > 0

  const todayStr = new Date().toLocaleDateString('es-MX', { day: '2-digit', month: '2-digit', year: 'numeric' })
  const twoYearsAgoDisplay = twoYearsAgo.toLocaleDateString('es-MX', { day: '2-digit', month: '2-digit', year: 'numeric' })

  return (
    <div className="space-y-4">
      {/* Listing table */}
      <div className="overflow-x-auto rounded-lg border">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b bg-muted/50 text-xs font-medium text-muted-foreground">
              <th className="px-3 py-2 text-left" title="Posición en el ranking">#</th>
              <th className="px-3 py-2 text-left" title="Ticker y nombre de la FIBRA">FIBRA</th>
              <th className="px-3 py-2 text-right" title="Precio promedio de compra en tu portafolio">Costo prom.</th>
              <th className="px-3 py-2 text-right" title="Precio de mercado más reciente">Precio actual</th>
              <th className="px-3 py-2 text-right" title="Diferencia porcentual entre precio actual y costo promedio">Dif. %</th>
              <th className="px-3 py-2 text-right" title="Score de oportunidad calculado con los pesos configurados">Score</th>
              <th className="px-3 py-2 text-right" title="Dividend yield anualizado (distribuciones / precio)">Yield</th>
              <th className="px-3 py-2 text-right" title="Número de títulos adicionales a comprar en esta simulación">Títulos adicionales</th>
              <th className="px-3 py-2 text-right" title="Nuevo costo promedio ponderado incluyendo comisión">Nuevo avg</th>
              <th className="px-3 py-2 text-right" title="Valor total de la posición (actuales + adicionales) al precio actual">Nuevo valor</th>
              <th className="px-3 py-2 text-right" title="Plusvalía porcentual del nuevo costo promedio respecto al precio actual">Nueva plusvalía</th>
              <th className="px-3 py-2 text-right" title="Renta mensual estimada con los títulos adicionales (basada en yield anualizado)">Renta mens.</th>
              <th className="px-3 py-2 text-right" title={`Costo total de compra incluyendo comisión (${(commissionFactor * 100).toFixed(2)}%) e IVA (${(ivaFactor * 100).toFixed(0)}%)`}>Costo compra</th>
            </tr>
          </thead>
          <tbody>
            {promediarRows.map(({ position, opportunityRow }, idx) => {
              const costoPromedio = toNum(position.costoPromedio)
              const precioActual = position.precioActual != null ? toNum(position.precioActual) : null
              const titulos = toNum(position.titulos)
              const score = opportunityRow ? calcLocalScore(opportunityRow, weights) : null

              const adicionalesStr = adicionales[position.fibraId] ?? ''
              const adicionalesNum = parseInt(adicionalesStr, 10)
              const hasSimulacion = !isNaN(adicionalesNum) && adicionalesNum > 0 && precioActual != null

              const yieldPct = opportunityRow?.dividendYieldPct != null
                ? toNum(opportunityRow.dividendYieldPct)
                : null

              const costoCompraRow = hasSimulacion && precioActual != null
                ? calcCostoPurchase(precioActual, adicionalesNum, commissionFactor, ivaFactor)
                : null

              const rentaMensualRow = hasSimulacion && precioActual != null
                ? calcRentaProyectadaAnual(
                    toNum(position.rentaAnual),
                    adicionalesNum,
                    precioActual,
                    yieldPct,
                    titulos,
                  ) / 12
                : null

              const difPct = precioActual != null && costoPromedio > 0
                ? ((precioActual - costoPromedio) / costoPromedio) * 100
                : null

              const nuevoAvg = hasSimulacion
                ? calcNuevoAvg(titulos, costoPromedio, adicionalesNum, precioActual!)
                : null
              const nuevoValor = hasSimulacion
                ? calcNuevoValor(titulos, adicionalesNum, precioActual!)
                : null
              const nuevaPlusvalia = hasSimulacion && nuevoAvg != null
                ? calcNuevaPlusvaliaPct(nuevoAvg, precioActual!)
                : null

              return (
                <tr
                  key={position.fibraId}
                  className={`border-b ${idx % 2 === 0 ? '' : 'bg-muted/10'}`}
                >
                  <td className="px-3 py-2 text-muted-foreground tabular-nums text-xs">
                    #{idx + 1}
                  </td>
                  <td className="px-3 py-2">
                    <span className="font-medium">{position.ticker}</span>
                    <span className="ml-2 text-xs text-muted-foreground">{position.nombre}</span>
                  </td>
                  <td className="px-3 py-2 text-right tabular-nums">
                    {fmtMxn(costoPromedio)}
                  </td>
                  <td className="px-3 py-2 text-right tabular-nums">
                    {precioActual != null ? fmtMxn(precioActual) : '—'}
                  </td>
                  <td className={`px-3 py-2 text-right tabular-nums text-xs font-medium ${
                    difPct == null ? 'text-muted-foreground' :
                    difPct >= 0 ? 'text-green-700' : 'text-red-700'
                  }`}>
                    {difPct != null ? `${difPct.toFixed(1)}%` : '—'}
                  </td>
                  <td className="px-3 py-2 text-right">
                    {score != null ? <ScoreBadge score={score} /> : <span className="text-xs text-muted-foreground">—</span>}
                  </td>
                  <td className="px-3 py-2 text-right tabular-nums text-xs text-muted-foreground">
                    {yieldPct != null ? `${yieldPct.toFixed(1)}%` : '—'}
                  </td>
                  <td className="px-3 py-2 text-right">
                    <input
                      id={`adicionales-${position.fibraId}`}
                      name={`adicionales-${position.fibraId}`}
                      type="number"
                      min={0}
                      step={1}
                      value={adicionalesStr}
                      aria-label={`Títulos adicionales para ${position.ticker}`}
                      onChange={(e) =>
                        setAdicionales((prev) => ({ ...prev, [position.fibraId]: e.target.value }))
                      }
                      className="w-24 rounded border px-2 py-1 text-right text-sm tabular-nums focus:outline-none focus:ring-1 focus:ring-primary"
                    />
                  </td>
                  <td className="px-3 py-2 text-right tabular-nums text-xs">
                    {nuevoAvg != null ? fmtMxn(nuevoAvg) : '—'}
                  </td>
                  <td className="px-3 py-2 text-right tabular-nums text-xs">
                    {nuevoValor != null ? fmtMxnNoDecimals(nuevoValor) : '—'}
                  </td>
                  <td className={`px-3 py-2 text-right tabular-nums text-xs font-medium ${
                    nuevaPlusvalia == null ? 'text-muted-foreground' :
                    nuevaPlusvalia >= 0 ? 'text-green-700' : 'text-red-700'
                  }`}>
                    {nuevaPlusvalia != null ? `${nuevaPlusvalia.toFixed(1)}%` : '—'}
                  </td>
                  <td className="px-3 py-2 text-right tabular-nums text-xs text-muted-foreground">
                    {rentaMensualRow != null ? fmtMxnNoDecimals(rentaMensualRow) : '—'}
                  </td>
                  <td className="px-3 py-2 text-right tabular-nums text-xs">
                    {costoCompraRow != null ? fmtMxnNoDecimals(costoCompraRow) : '—'}
                  </td>
                </tr>
              )
            })}
          </tbody>
        </table>
      </div>

      {/* ¿Qué pasaría si? */}
      <section className="rounded-2xl border border-border bg-card p-4 shadow-sm">
        <div className="flex flex-col gap-3 lg:flex-row lg:items-end lg:justify-between">
          <div>
            <p className="text-xs font-semibold uppercase tracking-[0.24em] text-primary">
              ¿Qué pasaría si...?
            </p>
            <h3 className="mt-1 text-base font-semibold text-foreground">
              Simulación con comisión de compra
            </h3>
            <p className="mt-1 text-sm text-muted-foreground">
              La comisión configurada en Ops se aplica al costo de las nuevas adquisiciones.
            </p>
          </div>

          <div className="flex items-center gap-4 text-sm text-muted-foreground">
            <span>
              Comisión aplicada:{' '}
              <span className="font-semibold text-foreground">{(commissionFactor * 100).toFixed(2)}%</span>
            </span>
            <span>
              IVA:{' '}
              <span className="font-semibold text-foreground">{(ivaFactor * 100).toFixed(0)}%</span>
            </span>
          </div>
        </div>

        <div className="mt-4 grid gap-4 lg:grid-cols-[minmax(0,1.5fr)_minmax(0,2fr)]">
          {/* Left: FIBRA selector + inputs */}
          <div className="space-y-3 rounded-xl border border-border/70 bg-background p-4">
            <label className="space-y-1.5">
              <span className="text-xs font-semibold uppercase tracking-[0.18em] text-muted-foreground">
                FIBRA
              </span>
              <select
                id="promediar-fibra"
                name="whatIfFibraId"
                value={whatIfFibraId}
                onChange={(e) => setWhatIfFibraId(e.target.value)}
                className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground outline-none transition focus:border-ring"
              >
                {allOpportunityRows.map((row) => (
                  <option key={row.fibraId} value={row.fibraId}>
                    {row.ticker} - {row.nombre}
                    {positionByFibraId.has(row.fibraId) ? '' : ' (sin posición)'}
                  </option>
                ))}
              </select>
            </label>

            <label className="space-y-1.5">
              <span className="text-xs font-semibold uppercase tracking-[0.18em] text-muted-foreground">
                Títulos a comprar
              </span>
              <input
                id="promediar-titulos-a-comprar"
                name="whatIfTitulos"
                type="number"
                min={0}
                step={1}
                value={whatIfTitulos}
                onChange={(e) => handleTitulosChange(e.target.value)}
                className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground outline-none transition focus:border-ring"
                placeholder="0"
              />
            </label>

            <p className="text-xs text-muted-foreground">
              El cálculo usa el promedio actual, el precio de mercado y la comisión de compra para recalcular tu costo promedio.
            </p>
          </div>

          {/* Right: 5 metric cards */}
          <div className="grid gap-3 sm:grid-cols-3 lg:grid-cols-5">
            <MetricCard title="Nuevo costo promedio" value={formatMoney(newAvgCost)} />
            <MetricCard
              title="Delta vs precio actual"
              value={formatPercent(deltaVsPricePct)}
              toneClassName={
                deltaVsPricePct == null
                  ? 'text-foreground'
                  : deltaVsPricePct > 0
                    ? 'text-red-600'
                    : deltaVsPricePct < 0
                      ? 'text-green-600'
                      : 'text-foreground'
              }
            />
            <MetricCard
              title="Nuevo % portafolio"
              value={formatPercent(newPortfolioPct)}
            />
            <MetricCard
              title="Renta mensual estimada"
              value={formatMoney(rentaMensualEstimada)}
            />
            <MetricCard
              title="Costo compra"
              value={costoPurchaseWhatIf != null ? fmtMxnNoDecimals(costoPurchaseWhatIf) : '—'}
            />
          </div>

          {/* Calcular objetivo de renta + panel retorno */}
          <div className="col-span-full mt-4 grid gap-4 border-t pt-4 lg:grid-cols-2">
            {/* Left: renta objetivo */}
            <div className="space-y-3">
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-muted-foreground">
                Calcular objetivo de renta
              </p>
              <label className="block space-y-1.5">
                <span className="text-xs font-medium text-muted-foreground">
                  Renta mensual objetivo (MXN)
                </span>
                <input
                  id="promediar-renta-mensual-objetivo"
                  name="whatIfTargetRenta"
                  type="number"
                  min={0}
                  step={100}
                  value={whatIfTargetRenta}
                  onChange={(e) => handleRentaChange(e.target.value)}
                  className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground outline-none transition focus:border-ring"
                  placeholder="0"
                />
              </label>
              {targetYaCumplido && (
                <p className="text-sm text-green-700 font-medium">
                  Ya cumples el objetivo con tus posiciones actuales.
                </p>
              )}
              {sinDatosRenta && (
                <p className="text-sm text-muted-foreground">
                  Datos de renta no disponibles para esta FIBRA.
                </p>
              )}
              {!targetYaCumplido && !sinDatosRenta && titulosAdicionalesParaTarget != null && (
                <p className="text-sm text-foreground">
                  Necesitas{' '}
                  <span className="font-semibold">
                    {titulosAdicionalesParaTarget.toLocaleString('es-MX')}
                  </span>{' '}
                  títulos adicionales
                  {costoInversionParaTarget != null && costoInversionParaTarget > 0 && (
                    <span className="text-muted-foreground">
                      {' '}(~{fmtMxnNoDecimals(costoInversionParaTarget)} de inversión)
                    </span>
                  )}
                </p>
              )}
            </div>

            {/* Right: retorno histórico 2 años */}
            <RetornoPanel
              ticker={selectedTicker}
              invertido={invertidoPanel}
              fechaInicio={twoYearsAgoDisplay}
              fechaFin={todayStr}
              cbfis={cbfisPanel}
              precioInicial={precioInicial}
              precioFinal={currentPrice}
              montoInvertidoReal={montoInvertidoReal}
              dividendosPorCbfi={dividendosPorCbfi}
              dividendosRecibidos={dividendosRecibidos}
              valorHoy={valorHoyPanel}
              variacionCapital={variacionCapital}
              variacionPorcentual={variacionPorcentual}
              rendimientoTotalPesos={rendimientoTotalPesos}
              rendimientoTotalPct={rendimientoTotalPct}
              rendimientoAnualizado={rendimientoAnualizado}
              inflacion2y={inflacion2y}
              rendimientoRealAnualizado={rendimientoRealAnualizado}
              hasPanelData={hasPanelData}
              isLoading={historyQuery.isLoading}
            />
          </div>
        </div>
      </section>

      <p className="mt-3 text-xs text-muted-foreground text-center">
        Este simulador es informativo. No constituye una recomendación de compra o venta.
      </p>
    </div>
  )
}

interface RetornoPanelProps {
  ticker: string
  invertido: number
  fechaInicio: string
  fechaFin: string
  cbfis: number
  precioInicial: number | null
  precioFinal: number | null
  montoInvertidoReal: number
  dividendosPorCbfi: number
  dividendosRecibidos: number
  valorHoy: number
  variacionCapital: number
  variacionPorcentual: number
  rendimientoTotalPesos: number
  rendimientoTotalPct: number
  rendimientoAnualizado: number | null
  inflacion2y: number | null
  rendimientoRealAnualizado: number | null
  hasPanelData: boolean
  isLoading: boolean
}

function InfoTooltip({ text }: { text: string }) {
  return (
    <span className="group relative inline-flex items-center cursor-help">
      <span className="ml-0.5 text-[10px] opacity-50 select-none">ⓘ</span>
      <span className="pointer-events-none absolute bottom-full left-1/2 -translate-x-1/2 mb-2 w-52 rounded-md border border-border bg-popover px-2.5 py-1.5 text-[11px] leading-relaxed text-popover-foreground shadow-md opacity-0 group-hover:opacity-100 transition-opacity z-50 normal-case tracking-normal font-normal whitespace-normal">
        {text}
      </span>
    </span>
  )
}

function RetornoPanel({
  ticker,
  invertido,
  fechaInicio,
  fechaFin,
  cbfis,
  precioInicial,
  precioFinal,
  montoInvertidoReal,
  dividendosPorCbfi,
  dividendosRecibidos,
  valorHoy,
  variacionCapital,
  variacionPorcentual,
  rendimientoTotalPesos,
  rendimientoTotalPct,
  rendimientoAnualizado,
  inflacion2y,
  rendimientoRealAnualizado,
  hasPanelData,
  isLoading,
}: RetornoPanelProps) {
  return (
    <div className="rounded-xl border border-border bg-background p-4 shadow-sm">
      <div className="flex items-center justify-between mb-3">
        <p className="text-xs font-semibold uppercase tracking-[0.18em] text-muted-foreground">
          Retorno histórico — {ticker}
        </p>
        <span className="text-[11px] text-muted-foreground tabular-nums">
          {fechaInicio} → {fechaFin}
        </span>
      </div>

      {isLoading && (
        <p className="text-xs text-muted-foreground py-4 text-center">Cargando historial…</p>
      )}

      {!isLoading && !hasPanelData && (
        <p className="text-xs text-muted-foreground py-4 text-center">Sin datos históricos suficientes.</p>
      )}

      {!isLoading && hasPanelData && (
        <div className="grid grid-cols-2 gap-3">
          {/* Inversión inicial */}
          <div className="space-y-1.5 rounded-lg border border-border/60 bg-muted/20 p-3">
            <p className="text-[11px] font-semibold text-muted-foreground uppercase tracking-wide">
              Inversión inicial
            </p>
            <div className="space-y-1 text-xs">
              <div className="flex justify-between">
                <span className="text-muted-foreground">Precio CBFI:</span>
                <span className="tabular-nums font-medium">{precioInicial != null ? fmtMxn(precioInicial) : '—'}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-muted-foreground">Núm. CBFIs:</span>
                <span className="tabular-nums font-medium">{cbfis > 0 ? cbfis.toLocaleString('es-MX') : '—'}</span>
              </div>
              <div className="flex justify-between border-t border-border/40 pt-1 mt-1">
                <span className="text-muted-foreground">Monto invertido:</span>
                <span className="tabular-nums font-semibold">{montoInvertidoReal > 0 ? fmtMxn(montoInvertidoReal) : invertido > 0 ? fmtMxn(invertido) : '$0.00'}</span>
              </div>
            </div>
          </div>

          {/* Capital */}
          <div className="space-y-1.5 rounded-lg border border-border/60 bg-muted/20 p-3">
            <p className="text-[11px] font-semibold text-muted-foreground uppercase tracking-wide">
              Inversión del capital
            </p>
            <div className="space-y-1 text-xs">
              <div className="flex justify-between">
                <span className="text-muted-foreground">Precio CBFI final:</span>
                <span className="tabular-nums font-medium">{precioFinal != null ? fmtMxn(precioFinal) : '—'}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-muted-foreground">Valor hoy:</span>
                <span className="tabular-nums font-medium">{valorHoy > 0 ? fmtMxn(valorHoy) : '—'}</span>
              </div>
              <div className={`flex justify-between border-t border-border/40 pt-1 mt-1 font-medium ${variacionCapital >= 0 ? 'text-green-700' : 'text-red-700'}`}>
                <span>Variación:</span>
                <span className="tabular-nums">{montoInvertidoReal > 0 ? `${variacionPorcentual >= 0 ? '+' : ''}${variacionPorcentual.toFixed(2)}%` : '—'}</span>
              </div>
            </div>
          </div>

          {/* Dividendos */}
          <div className="space-y-1.5 rounded-lg border border-green-200 bg-green-50/50 p-3">
            <p className="text-[11px] font-semibold text-muted-foreground uppercase tracking-wide">
              Dividendos recibidos
            </p>
            <div className="space-y-1 text-xs">
              <div className="flex justify-between">
                <span className="text-muted-foreground">Por CBFI:</span>
                <span className="tabular-nums font-medium">{dividendosPorCbfi > 0 ? fmtMxn(dividendosPorCbfi) : '—'}</span>
              </div>
              <div className="flex justify-between border-t border-border/40 pt-1 mt-1 font-semibold text-green-800">
                <span>Total:</span>
                <span className="tabular-nums">{dividendosRecibidos > 0 ? fmtMxn(dividendosRecibidos) : '$0.00'}</span>
              </div>
            </div>
          </div>

          {/* Rendimiento total */}
          <div className="space-y-1.5 rounded-lg bg-primary p-3 text-primary-foreground">
            <p className="text-[11px] font-semibold uppercase tracking-wide opacity-80">
              Rendimiento total
            </p>
            <div className="mt-1 flex items-center gap-1 text-xl font-bold tabular-nums">
              {montoInvertidoReal > 0 ? `${rendimientoTotalPct >= 0 ? '+' : ''}${rendimientoTotalPct.toFixed(2)}%` : '—'}
              {montoInvertidoReal > 0 && (
                <InfoTooltip text="(Variación de precio + dividendos recibidos) ÷ monto invertido. Incluye tanto la ganancia o pérdida del precio del CBFI como los dividendos cobrados en 2 años." />
              )}
            </div>
            <div className="flex items-center gap-1 text-xs opacity-80 tabular-nums">
              {montoInvertidoReal > 0 ? fmtMxn(rendimientoTotalPesos) : ''}
              {montoInvertidoReal > 0 && (
                <InfoTooltip text={`En pesos: variación de capital (${fmtMxn(variacionCapital)}) + dividendos (${fmtMxn(dividendosRecibidos)}).`} />
              )}
            </div>
            {rendimientoAnualizado != null && montoInvertidoReal > 0 && (
              <div className="mt-2 border-t border-primary-foreground/20 pt-2 text-xs space-y-1">
                <div className="flex items-center gap-1">
                  Anualizado: <span className="font-semibold">{rendimientoAnualizado.toFixed(2)}%</span>
                  <InfoTooltip text="Tasa anual equivalente usando media geométrica del rendimiento total a 2 años: (1 + rendimiento)^0.5 − 1. Permite comparar con otras inversiones anualizadas." />
                </div>
                {inflacion2y != null && (
                  <div className="flex items-center gap-1 opacity-70">
                    Inflación 2a: <span className="font-semibold">{inflacion2y.toFixed(1)}%</span>
                    <InfoTooltip text="Inflación acumulada en los últimos 2 años, calculada componiendo las dos tasas anuales del INPC (Banxico). Referencia para medir si tu inversión ganó en términos reales." />
                  </div>
                )}
                {rendimientoRealAnualizado != null && (
                  <div className={`flex items-center gap-1 ${rendimientoRealAnualizado >= 0 ? 'text-green-300' : 'text-red-300'}`}>
                    Real anual: <span className="font-semibold">{rendimientoRealAnualizado >= 0 ? '+' : ''}{rendimientoRealAnualizado.toFixed(2)}%</span>
                    <InfoTooltip text="Rendimiento anualizado ajustado por inflación (fórmula Fisher): cuánto ganaste o perdiste en poder adquisitivo real. Positivo = superaste la inflación; negativo = perdiste poder de compra." />
                  </div>
                )}
              </div>
            )}
          </div>
        </div>
      )}
    </div>
  )
}

function MetricCard({
  title,
  value,
  toneClassName = 'text-foreground',
}: {
  title: string
  value: string
  toneClassName?: string
}) {
  return (
    <article className="rounded-xl border border-border/70 bg-background p-4 shadow-sm">
      <div className="text-[11px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">
        {title}
      </div>
      <div className={`mt-2 text-2xl font-semibold tracking-tight tabular-nums ${toneClassName}`}>
        {value}
      </div>
    </article>
  )
}
