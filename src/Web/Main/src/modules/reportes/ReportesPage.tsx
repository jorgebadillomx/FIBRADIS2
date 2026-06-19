import { useEffect, useMemo, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Search, X } from 'lucide-react'
import { usePageTitle } from '@/shared/hooks/usePageTitle'
import { Input } from '@/shared/ui/input'
import { fetchCalculadoraFibras } from '@/api/fibrasApi'
import { fetchFaqItems } from '@/api/faqApi'
import { FaqAccordion } from '@/shared/ui/FaqAccordion'
import {
  fetchFundamentalesAvailablePeriods,
  fetchFundamentalesReport,
  type FundamentalesReportDto,
} from '@/api/fundamentalesApi'
import {
  FundamentalAnalysis,
  FundamentalKpiTable,
} from '@/modules/ficha-publica/sections/FundamentalesContent'
import { FundamentalesSectionSkeleton } from '@/modules/ficha-publica/sections/FundamentalesSection'
import {
  type FundamentalesReportData,
  type FundamentalItem,
} from '@/modules/ficha-publica/sections/fundamentales'
import { KPI_DEFINITIONS, type KpiKey } from '@/shared/lib/kpi-definitions'
import {
  buildFibraSuggestions,
  buildFundamentalPeriodOptions,
  getDefaultFundamentalPeriod,
} from './reportes-logic'

const FUNDAMENTAL_KPI_KEYS: ReadonlyArray<KpiKey> = [
  'capRate',
  'navPerCbfi',
  'ltv',
  'noiMargin',
  'ffoMargin',
  'quarterlyDistribution',
]

export function ReportesPage() {
  const [search, setSearch] = useState('')
  const [isSearchFocused, setIsSearchFocused] = useState(false)
  const [selectedFibraTicker, setSelectedFibraTicker] = useState('')
  const [selectedPeriod, setSelectedPeriod] = useState('')

  const fibrasQuery = useQuery({
    queryKey: ['reportes', 'fibras'],
    queryFn: () => fetchCalculadoraFibras(),
    staleTime: 60 * 60_000,
    retry: false,
  })

  const fibras = fibrasQuery.data ?? []
  const fibraMap = useMemo(
    () => new Map(fibras.map((fibra) => [fibra.ticker.toUpperCase(), fibra])),
    [fibras],
  )

  const selectedFibra = selectedFibraTicker ? fibraMap.get(selectedFibraTicker.toUpperCase()) ?? null : null

  const fibraSuggestions = useMemo(
    () => buildFibraSuggestions(fibras, search, selectedFibraTicker),
    [fibras, search, selectedFibraTicker],
  )

  const periodsQuery = useQuery({
    queryKey: ['reportes', 'fundamentales-periods', selectedFibraTicker],
    queryFn: () => fetchFundamentalesAvailablePeriods(selectedFibraTicker),
    enabled: !!selectedFibraTicker,
    staleTime: 5 * 60_000,
    retry: false,
  })

  const sortedPeriods = useMemo(
    () => buildFundamentalPeriodOptions(periodsQuery.data ?? []),
    [periodsQuery.data],
  )

  useEffect(() => {
    if (!selectedFibraTicker) {
      setSelectedPeriod('')
      return
    }

    if (sortedPeriods.length === 0) {
      setSelectedPeriod('')
      return
    }

    if (!selectedPeriod || !sortedPeriods.includes(selectedPeriod)) {
      setSelectedPeriod(getDefaultFundamentalPeriod(sortedPeriods) ?? '')
    }
  }, [selectedFibraTicker, selectedPeriod, sortedPeriods])

  const reportQuery = useQuery({
    queryKey: ['reportes', 'fundamentales-report', selectedFibraTicker, selectedPeriod],
    queryFn: () => fetchFundamentalesReport(selectedFibraTicker, selectedPeriod || undefined),
    enabled: !!selectedFibraTicker && !!selectedPeriod,
    staleTime: 5 * 60_000,
    retry: false,
  })

  const reportData: FundamentalesReportData | undefined = reportQuery.data
    ? mapReportDtoToData(reportQuery.data)
    : undefined

  const showLoading = selectedFibra
    ? periodsQuery.isLoading || reportQuery.isLoading
    : fibrasQuery.isLoading
  const showFibraEmpty = fibrasQuery.isSuccess && fibras.length === 0
  const showNoSelection = !selectedFibra && !showFibraEmpty && !fibrasQuery.isLoading
  const showPeriodsEmpty = !!selectedFibra && periodsQuery.isSuccess && sortedPeriods.length === 0
  const showReportEmpty = !!selectedFibra && sortedPeriods.length > 0 && reportQuery.isSuccess && !reportData
  const showError = fibrasQuery.isError || periodsQuery.isError || reportQuery.isError

  usePageTitle('Reportes trimestrales privados — Fibras Inmobiliarias', 'Reportes trimestrales privados de FIBRAs con KPIs y análisis IA completo tras autenticación.')

  const faqQuery = useQuery({
    queryKey: ['faq', 'PrivatePage', '/reportes'],
    queryFn: () => fetchFaqItems('PrivatePage', '/reportes'),
    staleTime: 60 * 60_000,
  })

  return (
    <div className="relative overflow-hidden bg-[radial-gradient(circle_at_10%_10%,rgba(194,65,12,0.12),transparent_25%),radial-gradient(circle_at_90%_15%,rgba(15,118,110,0.10),transparent_22%),linear-gradient(180deg,rgba(10,14,26,0.02),transparent_30%)]">
      <div className="container mx-auto px-4 py-8 md:py-10">
        <header className="max-w-4xl space-y-4">
          <div className="inline-flex items-center gap-2 rounded-full border border-border bg-surface-elevated px-3 py-1 text-[11px] font-semibold uppercase tracking-[0.28em] text-primary shadow-sm">
            Privado
          </div>
          <h1 className="font-playfair text-4xl font-bold leading-tight text-foreground md:text-5xl">
            Reportes trimestrales de FIBRAs
          </h1>
          <p className="max-w-3xl text-sm leading-6 text-muted-foreground md:text-base">
            Selecciona una FIBRA, elige su trimestre y revisa en un solo lugar los KPIs
            fundamentales junto con el análisis IA completo: resumen, señales, alertas y
            perspectiva del analista.
          </p>
        </header>

        {showError ? (
          <div className="mt-8 rounded-2xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">
            No se pudo cargar la información del reporte. Intenta de nuevo en unos segundos.
          </div>
        ) : null}

        <section className="mt-8 rounded-3xl border border-border bg-surface-elevated/95 p-5 shadow-sm backdrop-blur">
          <div className="grid gap-6 lg:grid-cols-[minmax(0,1.1fr)_minmax(0,0.9fr)]">
            <div className="space-y-3">
              <div className="space-y-1">
                <p className="text-xs font-semibold uppercase tracking-[0.24em] text-muted-foreground">
                  Paso 1
                </p>
                <h2 className="font-playfair text-2xl font-semibold text-foreground">
                  Selecciona una FIBRA
                </h2>
                <p className="text-sm leading-6 text-muted-foreground">
                  Busca por ticker o por razón social. El selector usa el universo activo de la
                  plataforma para ayudarte a entrar rápido al reporte correcto.
                </p>
              </div>

              <label className="relative block max-w-2xl">
                <span className="sr-only">Buscar FIBRA para reportes</span>
                <Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
                <Input
                  aria-label="Buscar FIBRA para reportes"
                  className="h-11 pl-10"
                  onBlur={() => setIsSearchFocused(false)}
                  onChange={(e) => setSearch(e.target.value)}
                  onFocus={() => setIsSearchFocused(true)}
                  placeholder={fibrasQuery.isLoading ? 'Cargando universo...' : 'Ticker o empresa...'}
                  value={search}
                />

                {isSearchFocused && fibraSuggestions.length > 0 ? (
                  <div className="absolute z-20 mt-2 max-h-72 w-full overflow-auto rounded-2xl border border-border bg-background shadow-lg">
                    {fibraSuggestions.map((fibra) => (
                      <button
                        key={fibra.ticker}
                        type="button"
                        onMouseDown={(event) => event.preventDefault()}
                        onClick={() => {
                          setSelectedFibraTicker(fibra.ticker)
                          setSearch('')
                          setIsSearchFocused(false)
                          setSelectedPeriod('')
                        }}
                        className="flex w-full items-center justify-between gap-3 border-b border-border px-4 py-3 text-left text-sm transition-colors last:border-b-0 hover:bg-muted/50"
                      >
                        <div className="min-w-0">
                          <div className="flex items-center gap-2">
                            <span className="font-mono font-semibold text-primary">{fibra.ticker}</span>
                            <span className="text-xs text-muted-foreground">Abrir reporte</span>
                          </div>
                          <p className="truncate text-xs text-muted-foreground">{fibra.empresa}</p>
                        </div>
                      </button>
                    ))}
                  </div>
                ) : null}
              </label>

              {selectedFibra ? (
                <div className="inline-flex flex-wrap items-center gap-2 rounded-full border border-border bg-background px-3 py-1.5 text-sm">
                  <span className="font-mono font-semibold text-primary">{selectedFibra.ticker}</span>
                  <span className="text-muted-foreground">{selectedFibra.empresa}</span>
                  <button
                    type="button"
                    onClick={() => {
                      setSelectedFibraTicker('')
                      setSelectedPeriod('')
                    }}
                    className="ml-1 inline-flex items-center rounded-full p-1 text-muted-foreground transition-colors hover:bg-muted/70 hover:text-foreground"
                    aria-label="Cambiar FIBRA"
                  >
                    <X className="size-3.5" />
                  </button>
                </div>
              ) : null}
            </div>

            <div className="space-y-3">
              <div className="space-y-1">
                <p className="text-xs font-semibold uppercase tracking-[0.24em] text-muted-foreground">
                  Paso 2
                </p>
                <h2 className="font-playfair text-2xl font-semibold text-foreground">
                  Elige el período
                </h2>
                <p className="text-sm leading-6 text-muted-foreground">
                  El combo se llena con todos los reportes trimestrales procesados para la FIBRA
                  seleccionada y preselecciona el más reciente.
                </p>
              </div>

              <label className="block max-w-sm space-y-1.5">
                <span className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                  Período
                </span>
                <select
                  aria-label="Seleccionar período del reporte"
                  className="flex h-11 w-full rounded-xl border border-input bg-background px-3 text-sm text-foreground outline-none transition-colors focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 disabled:cursor-not-allowed disabled:bg-muted/40"
                  disabled={!selectedFibra || periodsQuery.isLoading || sortedPeriods.length === 0}
                  onChange={(event) => setSelectedPeriod(event.target.value)}
                  value={selectedPeriod}
                >
                  {sortedPeriods.map((period) => (
                    <option key={period} value={period}>
                      {period}
                    </option>
                  ))}
                </select>
              </label>
            </div>
          </div>
        </section>

        {showLoading ? (
          <div className="mt-8">
            <div className="rounded-3xl border border-border bg-surface-elevated p-5 shadow-sm">
              <div className="space-y-3">
                <div className="h-4 w-32 animate-pulse rounded bg-muted/70" />
                <div className="h-8 w-64 animate-pulse rounded bg-muted/70" />
                <div className="h-4 w-full max-w-2xl animate-pulse rounded bg-muted/70" />
              </div>
              <div className="mt-6">
                <FundamentalesSectionSkeleton />
              </div>
            </div>
          </div>
        ) : null}

        {showNoSelection ? (
          <div className="mt-8 rounded-3xl border border-dashed border-border bg-background px-6 py-12 text-center">
            <p className="text-sm font-medium text-foreground">
              Selecciona una FIBRA para ver sus reportes trimestrales.
            </p>
            <p className="mt-1 text-sm text-muted-foreground">
              El análisis premium queda detrás de login y aquí se consulta por trimestre.
            </p>
          </div>
        ) : null}

        {showFibraEmpty ? (
          <div className="mt-8 rounded-3xl border border-border bg-surface-elevated px-6 py-12 text-center shadow-sm">
            <p className="text-sm font-medium text-foreground">No hay FIBRAs disponibles.</p>
            <p className="mt-1 text-sm text-muted-foreground">
              El universo activo no devolvió opciones para el selector.
            </p>
          </div>
        ) : null}

        {showPeriodsEmpty ? (
          <div className="mt-8 rounded-3xl border border-border bg-surface-elevated px-6 py-12 text-center shadow-sm">
            <p className="text-sm font-medium text-foreground">
              Esta FIBRA todavía no tiene reportes trimestrales procesados.
            </p>
            <p className="mt-1 text-sm text-muted-foreground">
              Cuando existan períodos disponibles, aparecerán en el combo automáticamente.
            </p>
          </div>
        ) : null}

        {showReportEmpty ? (
          <div className="mt-8 rounded-3xl border border-border bg-surface-elevated px-6 py-12 text-center shadow-sm">
            <p className="text-sm font-medium text-foreground">
              No se encontró un reporte para el período seleccionado.
            </p>
            <p className="mt-1 text-sm text-muted-foreground">
              Revisa otro trimestre o vuelve a cargar la FIBRA para sincronizar los datos.
            </p>
          </div>
        ) : null}

        {selectedFibra && reportData ? (
          <section className="mt-8 space-y-6">
            <div className="rounded-3xl border border-border bg-surface-elevated p-5 shadow-sm">
              <div className="flex flex-col gap-2 border-b border-border pb-4 md:flex-row md:items-end md:justify-between">
                <div>
                  <p className="text-xs font-semibold uppercase tracking-[0.24em] text-muted-foreground">
                    Reporte activo
                  </p>
                  <h2 className="font-playfair text-2xl font-semibold text-foreground">
                    {selectedFibra.ticker} - {selectedFibra.empresa}
                  </h2>
                </div>
                <p className="text-sm text-muted-foreground">
                  {sortedPeriods.length > 0 ? `${sortedPeriods.length} períodos disponibles` : 'Sin períodos'}
                </p>
              </div>

              <div className="mt-5 space-y-5">
                <FundamentalKpiTable data={reportData} />
                <FundamentalAnalysis data={reportData} />
              </div>
            </div>
          </section>
        ) : null}

        {faqQuery.isSuccess && faqQuery.data.length > 0 ? (
          <div className="mt-10">
            <FaqAccordion
              items={faqQuery.data}
              kicker="FAQ de reportes"
              title="Preguntas frecuentes sobre los reportes"
              description="Qué contiene cada reporte, cómo leer los KPIs, cómo interpretar el análisis IA y con qué frecuencia se actualizan."
            />
          </div>
        ) : null}
      </div>
    </div>
  )
}

function mapReportDtoToData(dto: FundamentalesReportDto): FundamentalesReportData {
  return {
    period: dto.period,
    periodsAgo: typeof dto.periodsAgo === 'number' ? dto.periodsAgo : undefined,
    capturedAt: dto.capturedAt,
    summary: dto.summary ?? null,
    summaryMarkdown: dto.summaryMarkdown ?? null,
    investorTakeaway: dto.investorTakeaway ?? null,
    operationalSignals: dto.operationalSignals ?? [],
    financialSignals: dto.financialSignals ?? [],
    riskFlags: dto.riskFlags ?? [],
    items: FUNDAMENTAL_KPI_KEYS.map((key) => mapFundamentalItem(key, dto)),
  }
}

function mapFundamentalItem(key: KpiKey, dto: FundamentalesReportDto): FundamentalItem {
  const notes = dto.fieldNotes ?? {}

  return {
    label: KPI_DEFINITIONS[key].label,
    kpiKey: key,
    period: dto.period,
    value: toFundamentalNum(dto[key]),
    note: notes[key] ?? undefined,
  }
}

function toFundamentalNum(value: null | number | string | undefined): number | null {
  if (value == null) return null
  const normalized = Number(value)
  return Number.isFinite(normalized) ? normalized : null
}
