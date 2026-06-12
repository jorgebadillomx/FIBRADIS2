import { useEffect, useMemo, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import type { components } from '@fibradis/shared-api-client'
import { apiClient } from '@/api/fibrasApi'
import { type Weights, calcLocalScore } from '@/modules/oportunidades/OportunidadesPage'
import {
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

function toNum(v: null | number | string | undefined): number {
  if (v == null) return 0
  return typeof v === 'string' ? parseFloat(v) : v
}

function ScoreBadge({ score }: { score: number }) {
  const cls =
    score >= 65 ? 'bg-green-100 text-green-800' :
    score >= 35 ? 'bg-yellow-100 text-yellow-800' :
    'bg-red-100 text-red-800'
  return (
    <span className={`inline-block rounded-full px-2 py-0.5 text-xs font-semibold ${cls}`}>
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

  // All hooks must be called before any conditional return
  const positions = portfolioQuery.data?.positions ?? []
  const ranked = rankingQuery.data?.ranked ?? []
  const limitedData = rankingQuery.data?.limitedData ?? []
  const allOpportunityRows = [...ranked, ...limitedData]
  const rowByFibraId = new Map(allOpportunityRows.map((r) => [r.fibraId, r]))

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
    if (!whatIfFibraId && promediarRows[0]) {
      setWhatIfFibraId(promediarRows[0].position.fibraId)
    }
  }, [promediarRows, whatIfFibraId])

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

  const selectedWhatIfRow = promediarRows.find((row) => row.position.fibraId === whatIfFibraId) ?? promediarRows[0] ?? null
  const commissionFactor = toNum(configQuery.data?.commissionFactor) ?? 0
  const additionalWhatIfTitles = parseInt(whatIfTitulos, 10)
  const currentTitulos = selectedWhatIfRow ? toNum(selectedWhatIfRow.position.titulos) : null
  const currentAvgCost = selectedWhatIfRow ? toNum(selectedWhatIfRow.position.costoPromedio) : null
  const currentPrice = selectedWhatIfRow ? toNum(selectedWhatIfRow.position.precioActual) : null
  const portfolioValue = toNum(portfolioQuery.data?.kpis?.valorTotal)
  const canSimulateWhatIf =
    selectedWhatIfRow != null &&
    Number.isInteger(additionalWhatIfTitles) &&
    additionalWhatIfTitles > 0 &&
    currentTitulos != null &&
    currentAvgCost != null &&
    currentPrice != null

  const newAvgCost = canSimulateWhatIf
    ? calcNewAvgCost(
        currentTitulos!,
        currentAvgCost!,
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
      ? (((currentTitulos! + additionalWhatIfTitles) * currentPrice!) /
          (portfolioValue + additionalWhatIfTitles * currentPrice!)) * 100
      : null

  const dividendYieldPct = selectedWhatIfRow?.opportunityRow?.dividendYieldPct != null
    ? toNum(selectedWhatIfRow.opportunityRow.dividendYieldPct)
    : null
  const currentRentaAnual = toNum(selectedWhatIfRow?.position.rentaAnual)

  const rentaAnualEstimada = canSimulateWhatIf && currentTitulos != null && currentPrice != null
    ? calcRentaProyectadaAnual(
        currentRentaAnual,
        additionalWhatIfTitles,
        currentPrice,
        dividendYieldPct,
        currentTitulos,
      )
    : null
  const rentaMensualEstimada = rentaAnualEstimada != null ? rentaAnualEstimada / 12 : null

  const targetRentaMensualNum = parseFloat(whatIfTargetRenta)
  const hasTargetRenta = !isNaN(targetRentaMensualNum) && targetRentaMensualNum > 0
  const titulosTotalesParaTarget = hasTargetRenta && selectedWhatIfRow != null
    ? calcTitulosParaRentaTarget(
        targetRentaMensualNum,
        currentPrice ?? 0,
        dividendYieldPct,
        currentTitulos ?? 0,
        currentRentaAnual,
      )
    : null
  const titulosAdicionalesParaTarget = titulosTotalesParaTarget != null && currentTitulos != null
    ? Math.max(0, titulosTotalesParaTarget - currentTitulos)
    : null
  const costoInversionParaTarget = titulosAdicionalesParaTarget != null
    ? titulosAdicionalesParaTarget * (currentPrice ?? 0) * (1 + commissionFactor)
    : null
  const targetYaCumplido = hasTargetRenta && titulosTotalesParaTarget != null
    && currentTitulos != null && currentTitulos >= titulosTotalesParaTarget
  const sinDatosRenta = hasTargetRenta && selectedWhatIfRow != null && titulosTotalesParaTarget === null

  return (
    <div className="space-y-4">
      <div className="overflow-x-auto rounded-lg border">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b bg-muted/50 text-xs font-medium text-muted-foreground">
              <th className="px-3 py-2 text-left">#</th>
              <th className="px-3 py-2 text-left">FIBRA</th>
              <th className="px-3 py-2 text-right">Costo prom.</th>
              <th className="px-3 py-2 text-right">Precio actual</th>
              <th className="px-3 py-2 text-right">Dif. %</th>
              <th className="px-3 py-2 text-right">Score</th>
              <th className="px-3 py-2 text-right">Yield</th>
              <th className="px-3 py-2 text-right">Títulos adicionales</th>
              <th className="px-3 py-2 text-right">Nuevo avg</th>
              <th className="px-3 py-2 text-right">Nuevo valor</th>
              <th className="px-3 py-2 text-right">Nueva plusvalía</th>
              <th className="px-3 py-2 text-right">Costo compra</th>
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
              const costoCompra = hasSimulacion && precioActual != null ? adicionalesNum * precioActual : null

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
                  <td className="px-3 py-2 text-right tabular-nums text-xs">
                    {costoCompra != null ? fmtMxnNoDecimals(costoCompra) : '—'}
                  </td>
                </tr>
              )
            })}
          </tbody>
        </table>
      </div>

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

          <div className="text-sm text-muted-foreground">
            Comisión aplicada: <span className="font-medium text-foreground">{formatPercent(commissionFactor * 100)}</span>
          </div>
        </div>

        <div className="mt-4 grid gap-4 lg:grid-cols-[minmax(0,1.5fr)_minmax(0,2fr)]">
          <div className="space-y-3 rounded-xl border border-border/70 bg-background p-4">
            <label className="space-y-1.5">
              <span className="text-xs font-semibold uppercase tracking-[0.18em] text-muted-foreground">
                FIBRA
              </span>
              <select
                value={whatIfFibraId}
                onChange={(e) => setWhatIfFibraId(e.target.value)}
                className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground outline-none transition focus:border-ring"
              >
                {promediarRows.map(({ position }) => (
                  <option key={position.fibraId} value={position.fibraId}>
                    {position.ticker} - {position.nombre}
                  </option>
                ))}
              </select>
            </label>

            <label className="space-y-1.5">
              <span className="text-xs font-semibold uppercase tracking-[0.18em] text-muted-foreground">
                Títulos a comprar
              </span>
              <input
                type="number"
                min={0}
                step={1}
                value={whatIfTitulos}
                onChange={(e) => setWhatIfTitulos(e.target.value)}
                className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm text-foreground outline-none transition focus:border-ring"
                placeholder="0"
              />
            </label>

            <p className="text-xs text-muted-foreground">
              El cálculo usa el promedio actual, el precio de mercado y la comisión de compra para recalcular tu costo promedio.
            </p>
          </div>

          <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
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
          </div>

          <div className="mt-4 border-t pt-4 space-y-3">
            <p className="text-xs font-semibold uppercase tracking-[0.18em] text-muted-foreground">
              Calcular objetivo de renta
            </p>
            <label className="block space-y-1.5">
              <span className="text-xs font-medium text-muted-foreground">
                Renta mensual objetivo (MXN)
              </span>
              <input
                type="number"
                min={0}
                step={100}
                value={whatIfTargetRenta}
                onChange={(e) => setWhatIfTargetRenta(e.target.value)}
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
        </div>
      </section>

      <p className="mt-3 text-xs text-muted-foreground text-center">
        Este simulador es informativo. No constituye una recomendación de compra o venta.
      </p>
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
