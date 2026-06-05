import { Fragment, useMemo, useState } from 'react'
import { ChevronRight, BarChart3, AlertTriangle, Star } from 'lucide-react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import type { components } from '@fibradis/shared-api-client'
import { apiClient } from '@/api/fibrasApi'
import { PromediarTab } from '@/modules/oportunidades/PromediarTab'
import { StarButton } from '@/modules/oportunidades/StarButton'
import { useFavorites } from '@/modules/oportunidades/useFavorites'

type OpportunityFibraRowDto = components['schemas']['OpportunityFibraRowDto']
type OpportunityRankingResponseDto = components['schemas']['OpportunityRankingResponseDto']

export interface Weights {
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

export function calcLocalScore(row: OpportunityFibraRowDto, w: Weights): number {
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

function ComponentBar({ value, label, weight }: { value: number | null; label: string; weight: number }) {
  if (value == null) return <div className="text-xs text-muted-foreground">— Sin datos</div>
  const pct = Math.max(0, Math.min(100, value))
  const contribution = Math.round(pct * weight) / 100
  return (
    <div className="flex items-center gap-2">
      <span className="w-28 shrink-0 text-xs text-muted-foreground">{label}</span>
      <div className="h-2 flex-1 rounded-full bg-muted">
        <div
          className="h-2 rounded-full bg-primary transition-all"
          style={{ width: `${pct}%` }}
        />
      </div>
      <span className="w-14 text-right text-xs tabular-nums">{contribution.toFixed(1)} pts</span>
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
  favoriteIds,
  onToggleFavorite,
  favoritasFirst,
  isAuthenticated,
  isLimited,
}: {
  rows: OpportunityFibraRowDto[]
  weights: Weights
  favoriteIds: Set<string>
  onToggleFavorite: (fibraId: string) => void
  favoritasFirst: boolean
  isAuthenticated: boolean
  isLimited?: boolean
}) {
  const [expanded, setExpanded] = useState<Set<string>>(new Set())

  const sorted = useMemo(() => {
    return [...rows]
      .map((r, index) => ({
        ...r,
        _score: calcLocalScore(r, weights),
        _index: index,
        _isFavorite: favoriteIds.has(r.fibraId),
      }))
      .sort((a, b) => {
        if (favoritasFirst) {
          const af = a._isFavorite ? 0 : 1
          const bf = b._isFavorite ? 0 : 1
          if (af !== bf) return af - bf
        }

        if (b._score !== a._score) return b._score - a._score
        return a._index - b._index
      })
  }, [rows, weights, favoriteIds, favoritasFirst])

  const toggle = (id: string) =>
    setExpanded((prev) => {
      const s = new Set(prev)
      s.has(id) ? s.delete(id) : s.add(id)
      return s
    })

  if (sorted.length === 0)
    return <p className="py-6 text-center text-sm text-muted-foreground">Sin FIBRAs en esta sección.</p>

  const hasNonFavorites = sorted.some((row) => !favoriteIds.has(row.fibraId))

  return (
    <div className="overflow-x-auto rounded-lg border">
      <table className="w-full text-sm">
        <thead>
          <tr className="border-b bg-muted/50 text-xs font-medium text-muted-foreground">
            <th className="w-8 px-3 py-2" />
            {isAuthenticated && <th className="w-10 px-3 py-2"><span className="sr-only">Favorita</span></th>}
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
            const isFavorite = favoriteIds.has(row.fibraId)
            const nextIsFavorite = idx + 1 < sorted.length && favoriteIds.has(sorted[idx + 1].fibraId)
            const showSeparator = favoritasFirst && isFavorite && !nextIsFavorite && hasNonFavorites
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
                  {isAuthenticated && (
                    <td className="px-3 py-2">
                      <StarButton
                        fibraId={row.fibraId}
                        isFavorite={isFavorite}
                        onToggle={onToggleFavorite}
                      />
                    </td>
                  )}
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
                    <td colSpan={isAuthenticated ? 10 : 9} className="px-6 py-4">
                      <div className="space-y-2">
                        <p className="text-xs font-medium text-muted-foreground uppercase tracking-wide mb-3">
                          Contribución al score por componente (puntos)
                        </p>
                        <ComponentBar
                          value={row.navDiscountScore != null ? toNum(row.navDiscountScore) : null}
                          label={`Desc. NAV (${weights.navDiscount}%)`}
                          weight={weights.navDiscount}
                        />
                        <ComponentBar
                          value={row.dividendYieldScore != null ? toNum(row.dividendYieldScore) : null}
                          label={`Yield (${weights.dividendYield}%)`}
                          weight={weights.dividendYield}
                        />
                        <ComponentBar
                          value={row.ltvInvertedScore != null ? toNum(row.ltvInvertedScore) : null}
                          label={`LTV inv. (${weights.ltvInverted}%)`}
                          weight={weights.ltvInverted}
                        />
                        <ComponentBar
                          value={row.noiMarginScore != null ? toNum(row.noiMarginScore) : null}
                          label={`NOI (${weights.noiMargin}%)`}
                          weight={weights.noiMargin}
                        />
                        <ComponentBar
                          value={row.pricevs52wScore != null ? toNum(row.pricevs52wScore) : null}
                          label={`P. vs 52S (${weights.pricevs52w}%)`}
                          weight={weights.pricevs52w}
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
                {showSeparator && (
                  <tr aria-hidden="true">
                    <td colSpan={isAuthenticated ? 10 : 9} className="px-3 py-0">
                      <div className="border-t-2 border-dashed border-primary/20" />
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
  const [activeTab, setActiveTab] = useState<'universo' | 'promediar'>('universo')
  const [favoritasFirst, setFavoritasFirst] = useState(false)
  const queryClient = useQueryClient()
  const { favoriteIds, toggle: toggleFavorite, isAuthenticated } = useFavorites()

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

  const effectiveWeights: Weights = localWeights ?? (serverWeights != null ? {
    navDiscount: toNum(serverWeights.navDiscount),
    dividendYield: toNum(serverWeights.dividendYield),
    ltvInverted: toNum(serverWeights.ltvInverted),
    noiMargin: toNum(serverWeights.noiMargin),
    pricevs52w: toNum(serverWeights.pricevs52w),
  } : { navDiscount: 30, dividendYield: 30, ltvInverted: 20, noiMargin: 10, pricevs52w: 10 })

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
    onError: () => {
      // error surfaced via saveWeightsMutation.isError in JSX
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

  const ranked = rankingQuery.data?.ranked ?? []
  const limitedData = rankingQuery.data?.limitedData ?? []
  const coverage = rankingQuery.data?.coverage
  const fibrasWithoutPrice = coverage ? toNum(coverage.universeSize) - toNum(coverage.fibrasWithPrice) : 0
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

      {/* Tabs */}
      <div className="flex gap-1 border-b">
        {(['universo', 'promediar'] as const).map((tab) => (
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

      {activeTab === 'universo' && (
        <>
          {rankingQuery.isLoading && (
            <div className="flex h-64 items-center justify-center">
              <p className="text-muted-foreground">Cargando ranking…</p>
            </div>
          )}

          {rankingQuery.isError && (
            <div className="mx-auto max-w-4xl">
              <p className="text-destructive">Error al cargar el ranking de oportunidades.</p>
            </div>
          )}

          {!rankingQuery.isLoading && !rankingQuery.isError && (
            <>
              {/* Banner Universo Degradado */}
              {coverage?.status === 'Degraded' && (
                <div className="rounded-xl border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-800">
                  Universo degradado: {fibrasWithoutPrice} FIBRAs ({toNum(coverage.missingPct).toFixed(1)}%) sin precio disponible.
                  {coverage.lastValidPriceAt && (
                    <> Último dato válido:{' '}
                      {new Date(coverage.lastValidPriceAt).toLocaleString('es-MX', {
                        dateStyle: 'medium',
                        timeStyle: 'short',
                      })}.
                    </>
                  )}
                </div>
              )}

              {/* Configurador de pesos */}
              <div className="rounded-lg border bg-card p-5 space-y-4">
                <div className="flex items-center justify-between">
                  <h2 className="text-base font-semibold">Configurar pesos</h2>
                  <div className="flex flex-wrap items-center gap-2">
                    {Object.entries(PROFILES).map(([key, p]) => (
                      <button
                        type="button"
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
                    <button
                      type="button"
                      onClick={() => setFavoritasFirst((value) => !value)}
                      className={`flex items-center gap-1.5 rounded-md border px-3 py-1.5 text-sm transition-colors ${
                        favoritasFirst
                          ? 'border-primary bg-primary/10 text-primary'
                          : 'border-input text-muted-foreground hover:text-foreground'
                      }`}
                    >
                      <Star size={14} className={favoritasFirst ? 'fill-primary text-primary' : ''} />
                      Favoritas primero
                    </button>
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
                  <div className="flex flex-col items-end gap-1">
                    {saveWeightsMutation.isError && (
                      <p className="text-xs text-destructive">Error al guardar. Intenta de nuevo.</p>
                    )}
                    {isDirty && (
                      <button
                        type="button"
                        onClick={handleSaveWeights}
                        disabled={!isValid || saveWeightsMutation.isPending}
                        className="rounded-md bg-primary px-4 py-1.5 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-50"
                      >
                        {saveWeightsMutation.isPending ? 'Guardando…' : 'Guardar configuración'}
                      </button>
                    )}
                  </div>
                </div>
              </div>

              {/* Ranking o mensaje de suspensión */}
              {coverage?.status === 'Suspended' ? (
                <div className="rounded-xl border border-rose-200 bg-rose-50 px-5 py-8 text-center text-sm text-rose-700">
                  Ranking no disponible — cobertura insuficiente ({toNum(coverage.missingPct).toFixed(1)}% de FIBRAs sin precio).
                  El ranking se restaurará cuando menos del 50% de FIBRAs esté sin precio.
                </div>
              ) : (
                <>
                  {/* Ranking principal */}
                  <div className="space-y-3">
                    <h2 className="text-base font-semibold">
                      Ranking principal
                      <span className="ml-2 text-sm font-normal text-muted-foreground">
                        ({ranked.length} FIBRAs · ≥3 componentes disponibles)
                      </span>
                    </h2>
                    <RankingTable
                      rows={ranked}
                      weights={effectiveWeights}
                      favoriteIds={favoriteIds}
                      onToggleFavorite={toggleFavorite}
                      favoritasFirst={favoritasFirst}
                      isAuthenticated={isAuthenticated}
                    />
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
                      <RankingTable
                        rows={limitedData}
                        weights={effectiveWeights}
                        favoriteIds={favoriteIds}
                        onToggleFavorite={toggleFavorite}
                        favoritasFirst={favoritasFirst}
                        isAuthenticated={isAuthenticated}
                        isLimited
                      />
                    </div>
                  )}
                </>
              )}
            </>
          )}
        </>
      )}

      {activeTab === 'promediar' && (
        <PromediarTab weights={effectiveWeights} />
      )}
    </div>
  )
}
