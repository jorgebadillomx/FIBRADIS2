import { useMemo, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import type { components } from '@fibradis/shared-api-client'
import { apiClient } from '@/api/fibrasApi'
import { type Weights, calcLocalScore } from '@/modules/oportunidades/OportunidadesPage'
import { fetchFiscalRates } from '@/api/fiscalRatesApi'
import {
  IVA_FACTOR,
  calcCostoPurchase,
  calcNuevoAvg,
  calcNuevaPlusvaliaPct,
  calcNuevoValor,
  calcRentaProyectadaAnual,
} from '@/modules/oportunidades/simulador-logic'
import { PromediarCalculadora } from '@/modules/oportunidades/PromediarCalculadora'

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

      <PromediarCalculadora commissionFactor={commissionFactor} ivaFactor={ivaFactor} />

      <p className="mt-3 text-xs text-muted-foreground text-center">
        Este simulador es informativo. No constituye una recomendación de compra o venta.
      </p>
    </div>
  )
}
