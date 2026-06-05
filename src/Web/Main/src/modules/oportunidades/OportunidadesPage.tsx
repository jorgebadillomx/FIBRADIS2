import { Fragment, useMemo, useState } from 'react'
import { ChevronRight, BarChart3, AlertTriangle } from 'lucide-react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import type { components } from '@fibradis/shared-api-client'
import { apiClient } from '@/api/fibrasApi'

type OpportunityFibraRowDto = components['schemas']['OpportunityFibraRowDto']
type OpportunityWeightsDto = components['schemas']['OpportunityWeightsDto']
type OpportunityRankingResponseDto = components['schemas']['OpportunityRankingResponseDto']

interface Weights {
  navDiscount: number
  dividendYield: number
  ltvInverted: number
  noiMargin: number
  pricevs52w: number
}

const PROFILES: Record<string, Weights & { label: string }> = {
  default: { label: 'Predeterminado', navDiscount: 30, dividendYield: 30, ltvInverted: 20, noiMargin: 10, pricevs52w: 10 },
  renta: { label: 'Renta', navDiscount: 20, dividendYield: 50, ltvInverted: 10, noiMargin: 20, pricevs52w: 0 },
  crecimiento: { label: 'Crecimiento', navDiscount: 40, dividendYield: 15, ltvInverted: 25, noiMargin: 10, pricevs52w: 10 },
}

function toNum(v: null | number | string | undefined): number {
  if (v == null) return 0
  return typeof v === 'string' ? parseFloat(v) : v
}

function calcLocalScore(row: OpportunityFibraRowDto, w: Weights): number {
  let score = 0
  if (row.navDiscountScore != null) score += toNum(row.navDiscountScore) * w.navDiscount
  if (row.dividendYieldScore != null) score += toNum(row.dividendYieldScore) * w.dividendYield
  if (row.ltvInvertedScore != null) score += toNum(row.ltvInvertedScore) * w.ltvInverted
  if (row.noiMarginScore != null) score += toNum(row.noiMarginScore) * w.noiMargin
  if (row.pricevs52wScore != null) score += toNum(row.pricevs52wScore) * w.pricevs52w
  return Math.round(score) / 100
}

function fmt(v: null | number | string | undefined, digits = 1, suffix = ''): string {
  const n = toNum(v)
  if (v == null) return '—'
  return `${n.toFixed(digits)}${suffix}`
}

function fmtPct(v: null | number | string | undefined): string {
  const n = toNum(v)
  if (v == null) return '—'
  return `${n.toFixed(1)}%`
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

function ComponentBar({ value, label }: { value: number | null; label: string }) {
  if (value == null) return <div className="text-xs text-muted-foreground">— Sin datos</div>
  const pct = Math.max(0, Math.min(100, value))
  return (
    <div className="flex items-center gap-2">
      <span className="w-28 shrink-0 text-xs text-muted-foreground">{label}</span>
      <div className="h-2 flex-1 rounded-full bg-muted">
        <div
          className="h-2 rounded-full bg-primary transition-all"
          style={{ width: `${pct}%` }}
        />
      </div>
      <span className="w-8 text-right text-xs tabular-nums">{pct.toFixed(0)}</span>
    </div>
  )
}

function WeightSlider({
  label,
  value,
  onChange,
}: {
  label: string
  value: number
  onChange: (v: number) => void
}) {
  return (
    <div className="flex items-center gap-3">
      <span className="w-32 shrink-0 text-sm">{label}</span>
      <input
        type="range"
        min={0}
        max={100}
        step={5}
        value={value}
        onChange={(e) => onChange(Number(e.target.value))}
        className="h-2 flex-1 cursor-pointer accent-primary"
      />
      <span className="w-10 text-right text-sm font-medium tabular-nums">{value}%</span>
    </div>
  )
}

function RankingTable({
  rows,
  weights,
  isLimited,
}: {
  rows: OpportunityFibraRowDto[]
  weights: Weights
  isLimited?: boolean
}) {
  const [expanded, setExpanded] = useState<Set<string>>(new Set())

  const sorted = useMemo(() => {
    return [...rows].map((r) => ({ ...r, _score: calcLocalScore(r, weights) }))
      .sort((a, b) => b._score - a._score)
  }, [rows, weights])

  const toggle = (id: string) =>
    setExpanded((prev) => {
      const s = new Set(prev)
      s.has(id) ? s.delete(id) : s.add(id)
      return s
    })

  if (sorted.length === 0)
    return <p className="py-6 text-center text-sm text-muted-foreground">Sin FIBRAs en esta sección.</p>

  return (
    <div className="overflow-x-auto rounded-lg border">
      <table className="w-full text-sm">
        <thead>
          <tr className="border-b bg-muted/50 text-xs font-medium text-muted-foreground">
            <th className="w-8 px-3 py-2" />
            <th className="px-3 py-2 text-left">FIBRA</th>
            <th className="px-3 py-2 text-right">Score</th>
            <th className="px-3 py-2 text-right">Desc. NAV</th>
            <th className="px-3 py-2 text-right">Yield</th>
            <th className="px-3 py-2 text-right">LTV inv.</th>
            <th className="px-3 py-2 text-right">NOI</th>
            <th className="px-3 py-2 text-right">Precio/52S</th>
            <th className="px-3 py-2 text-right">Precio</th>
          </tr>
        </thead>
        <tbody>
          {sorted.map((row, idx) => {
            const isOpen = expanded.has(row.fibraId)
            return (
              <Fragment key={row.fibraId}>
                <tr
                  className={`cursor-pointer border-b transition-colors hover:bg-muted/30 ${idx % 2 === 0 ? '' : 'bg-muted/10'}`}
                  onClick={() => toggle(row.fibraId)}
                >
                  <td className="px-3 py-2">
                    <ChevronRight
                      className={`size-4 text-muted-foreground transition-transform ${isOpen ? 'rotate-90' : ''}`}
                    />
                  </td>
                  <td className="px-3 py-2">
                    <span className="font-medium">{row.ticker}</span>
                    <span className="ml-2 text-xs text-muted-foreground">{row.nombre}</span>
                    {isLimited && (
                      <span className="ml-2 rounded bg-yellow-100 px-1 py-0.5 text-xs text-yellow-800">
                        Ref.
                      </span>
                    )}
                  </td>
                  <td className="px-3 py-2 text-right">
                    <ScoreBadge score={row._score} />
                  </td>
                  <td className="px-3 py-2 text-right tabular-nums text-xs">
                    {fmtPct(row.navDiscountPct)}
                  </td>
                  <td className="px-3 py-2 text-right tabular-nums text-xs">
                    {fmtPct(row.dividendYieldPct)}
                  </td>
                  <td className="px-3 py-2 text-right tabular-nums text-xs">
                    {row.ltvPct != null ? fmtPct(toNum(row.ltvPct)) : '—'}
                  </td>
                  <td className="px-3 py-2 text-right tabular-nums text-xs">
                    {fmtPct(row.noiMarginPct)}
                  </td>
                  <td className="px-3 py-2 text-right tabular-nums text-xs">
                    {fmtPct(row.priceVsAvg52wPct)}
                  </td>
                  <td className="px-3 py-2 text-right tabular-nums text-xs">
                    {row.precioActual != null ? `$${fmt(row.precioActual, 2)}` : '—'}
                  </td>
                </tr>
                {isOpen && (
                  <tr className="border-b bg-blue-50/30">
                    <td colSpan={9} className="px-6 py-4">
                      <div className="space-y-2">
                        <p className="text-xs font-medium text-muted-foreground uppercase tracking-wide mb-3">
                          Desglose por componente (percentil × peso)
                        </p>
                        <ComponentBar
                          value={row.navDiscountScore != null ? toNum(row.navDiscountScore) : null}
                          label={`Desc. NAV (${weights.navDiscount}%)`}
                        />
                        <ComponentBar
                          value={row.dividendYieldScore != null ? toNum(row.dividendYieldScore) : null}
                          label={`Yield (${weights.dividendYield}%)`}
                        />
                        <ComponentBar
                          value={row.ltvInvertedScore != null ? toNum(row.ltvInvertedScore) : null}
                          label={`LTV inv. (${weights.ltvInverted}%)`}
                        />
                        <ComponentBar
                          value={row.noiMarginScore != null ? toNum(row.noiMarginScore) : null}
                          label={`NOI (${weights.noiMargin}%)`}
                        />
                        <ComponentBar
                          value={row.pricevs52wScore != null ? toNum(row.pricevs52wScore) : null}
                          label={`P. vs 52S (${weights.pricevs52w}%)`}
                        />
                        {row.navPerCbfi != null && (
                          <p className="mt-2 text-xs text-muted-foreground">
                            NAV/CBFI: ${fmt(row.navPerCbfi, 2)} · Precio: ${fmt(row.precioActual, 2)}
                            {row.avg52w != null && ` · AVG 52S: $${fmt(row.avg52w, 2)}`}
                          </p>
                        )}
                      </div>
                    </td>
                  </tr>
                )}
              </Fragment>
            )
          })}
        </tbody>
      </table>
    </div>
  )
}

function detectProfile(w: Weights): string {
  for (const [key, p] of Object.entries(PROFILES)) {
    if (
      p.navDiscount === w.navDiscount &&
      p.dividendYield === w.dividendYield &&
      p.ltvInverted === w.ltvInverted &&
      p.noiMargin === w.noiMargin &&
      p.pricevs52w === w.pricevs52w
    ) return key
  }
  return 'custom'
}

export function OportunidadesPage() {
  const queryClient = useQueryClient()

  const rankingQuery = useQuery<OpportunityRankingResponseDto>({
    queryKey: ['opportunities'],
    queryFn: async () => {
      const { data, error } = await apiClient.GET('/api/v1/opportunities', {})
      if (error || !data) throw new Error('No se pudo cargar el ranking.')
      return data
    },
  })

  const serverWeights = rankingQuery.data?.weights
  const [localWeights, setLocalWeights] = useState<Weights | null>(null)

  const effectiveWeights: Weights = localWeights ?? {
    navDiscount: toNum(serverWeights?.navDiscount) || 30,
    dividendYield: toNum(serverWeights?.dividendYield) || 30,
    ltvInverted: toNum(serverWeights?.ltvInverted) || 20,
    noiMargin: toNum(serverWeights?.noiMargin) || 10,
    pricevs52w: toNum(serverWeights?.pricevs52w) || 10,
  }

  const weightSum = Object.values(effectiveWeights).reduce((a, b) => a + b, 0)
  const isValid = Math.abs(weightSum - 100) < 0.5

  const saveWeightsMutation = useMutation({
    mutationFn: async (w: Weights) => {
      const profile = detectProfile(w)
      const { error } = await apiClient.PUT('/api/v1/opportunities/weights', {
        body: {
          navDiscount: w.navDiscount,
          dividendYield: w.dividendYield,
          ltvInverted: w.ltvInverted,
          noiMargin: w.noiMargin,
          pricevs52w: w.pricevs52w,
          profile,
        },
      })
      if (error) throw new Error('No se pudieron guardar los pesos.')
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['opportunities'] })
      setLocalWeights(null)
    },
  })

  const handleWeightChange = (key: keyof Weights, value: number) => {
    setLocalWeights((prev) => {
      const base = prev ?? effectiveWeights
      return { ...base, [key]: value }
    })
  }

  const applyProfile = (profileKey: string) => {
    const p = PROFILES[profileKey]
    if (!p) return
    const w: Weights = {
      navDiscount: p.navDiscount,
      dividendYield: p.dividendYield,
      ltvInverted: p.ltvInverted,
      noiMargin: p.noiMargin,
      pricevs52w: p.pricevs52w,
    }
    setLocalWeights(w)
    saveWeightsMutation.mutate(w)
  }

  const handleSaveWeights = () => {
    if (!isValid) return
    saveWeightsMutation.mutate(effectiveWeights)
  }

  if (rankingQuery.isLoading) {
    return (
      <div className="flex h-64 items-center justify-center">
        <p className="text-muted-foreground">Cargando ranking…</p>
      </div>
    )
  }

  if (rankingQuery.isError) {
    return (
      <div className="mx-auto max-w-4xl px-4 py-8">
        <p className="text-destructive">Error al cargar el ranking de oportunidades.</p>
      </div>
    )
  }

  const ranked = rankingQuery.data?.ranked ?? []
  const limitedData = rankingQuery.data?.limitedData ?? []
  const currentProfile = detectProfile(effectiveWeights)
  const isDirty = localWeights !== null

  return (
    <div className="mx-auto max-w-6xl px-4 py-8 space-y-8">
      {/* Header */}
      <div className="flex items-center gap-3">
        <BarChart3 className="size-6 text-primary" />
        <div>
          <h1 className="text-2xl font-bold">Oportunidades</h1>
          <p className="text-sm text-muted-foreground">
            Ranking del universo activo de FIBRAs por score de oportunidad configurable
          </p>
        </div>
      </div>

      {/* Configurador de pesos */}
      <div className="rounded-lg border bg-card p-5 space-y-4">
        <div className="flex items-center justify-between">
          <h2 className="text-base font-semibold">Configurar pesos</h2>
          <div className="flex items-center gap-2">
            {/* Perfiles */}
            {Object.entries(PROFILES).map(([key, p]) => (
              <button
                key={key}
                onClick={() => applyProfile(key)}
                className={`rounded-full px-3 py-1 text-xs font-medium transition-colors ${
                  currentProfile === key && !isDirty
                    ? 'bg-primary text-primary-foreground'
                    : 'bg-muted hover:bg-muted/80'
                }`}
              >
                {p.label}
              </button>
            ))}
          </div>
        </div>

        <div className="space-y-3">
          <WeightSlider
            label="Descuento NAV"
            value={effectiveWeights.navDiscount}
            onChange={(v) => handleWeightChange('navDiscount', v)}
          />
          <WeightSlider
            label="Dividend Yield"
            value={effectiveWeights.dividendYield}
            onChange={(v) => handleWeightChange('dividendYield', v)}
          />
          <WeightSlider
            label="LTV invertido"
            value={effectiveWeights.ltvInverted}
            onChange={(v) => handleWeightChange('ltvInverted', v)}
          />
          <WeightSlider
            label="Margen NOI"
            value={effectiveWeights.noiMargin}
            onChange={(v) => handleWeightChange('noiMargin', v)}
          />
          <WeightSlider
            label="Precio vs AVG 52S"
            value={effectiveWeights.pricevs52w}
            onChange={(v) => handleWeightChange('pricevs52w', v)}
          />
        </div>

        <div className="flex items-center justify-between pt-1">
          <span className={`text-sm ${isValid ? 'text-muted-foreground' : 'text-destructive font-medium'}`}>
            Suma: {weightSum}% {!isValid && '(debe ser 100%)'}
          </span>
          {isDirty && (
            <button
              onClick={handleSaveWeights}
              disabled={!isValid || saveWeightsMutation.isPending}
              className="rounded-md bg-primary px-4 py-1.5 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-50"
            >
              {saveWeightsMutation.isPending ? 'Guardando…' : 'Guardar configuración'}
            </button>
          )}
        </div>
      </div>

      {/* Ranking principal */}
      <div className="space-y-3">
        <h2 className="text-base font-semibold">
          Ranking principal
          <span className="ml-2 text-sm font-normal text-muted-foreground">
            ({ranked.length} FIBRAs · ≥3 componentes disponibles)
          </span>
        </h2>
        <RankingTable rows={ranked} weights={effectiveWeights} />
      </div>

      {/* Datos limitados */}
      {limitedData.length > 0 && (
        <div className="space-y-3">
          <div className="flex items-center gap-2 rounded-lg border border-yellow-200 bg-yellow-50 px-4 py-3">
            <AlertTriangle className="size-4 shrink-0 text-yellow-600" />
            <p className="text-sm text-yellow-800">
              <strong>Score referencial</strong> — datos insuficientes para el ranking principal. Las siguientes FIBRAs tienen 1 o 2 componentes calculables.
            </p>
          </div>
          <RankingTable rows={limitedData} weights={effectiveWeights} isLimited />
        </div>
      )}
    </div>
  )
}
