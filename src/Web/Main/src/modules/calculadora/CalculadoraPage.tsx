import { useMemo, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { fetchCalculadoraFibras } from '@/api/fibrasApi'
import { formatMoney } from '@/modules/portafolio/portfolio-format'
import { calcCbfis, calcRentaBruta, calcRentaBrutaAnual, calcSobra } from './calculadora-logic'

type SortKey =
  | ''
  | 'ticker'
  | 'periodo'
  | 'cbfis'
  | 'sobra'
  | 'precioActual'
  | 'distCbfi'
  | 'distCbfiAnual'
  | 'rentaBruta'
  | 'rentaBrutaAnual'

interface CalculadoraRow {
  ticker: string
  empresa: string
  precioActual: number | null
  ultimoPeriodo: string | null
  distCbfi: number | null
  distCbfiAnual: number | null
  freshnessStatus: string | null
  montoInput: string
  montoValue: number
  cbfis: number
  sobra: number
  rentaBruta: number | null
  rentaBrutaAnual: number | null
}

const DIST_FORMATTER = new Intl.NumberFormat('es-MX', {
  minimumFractionDigits: 4,
  maximumFractionDigits: 4,
})

const INT_FORMATTER = new Intl.NumberFormat('es-MX', {
  maximumFractionDigits: 0,
})

const SORTABLE_COLUMNS: Array<{ key: Exclude<SortKey, ''>; label: string; alignRight?: boolean }> = [
  { key: 'ticker', label: 'Fibra' },
  { key: 'periodo', label: 'Periodo' },
  { key: 'cbfis', label: 'CBFIs', alignRight: true },
  { key: 'sobra', label: '$ Sobra', alignRight: true },
  { key: 'precioActual', label: 'Precio', alignRight: true },
  { key: 'distCbfi', label: 'Dist. CBFI', alignRight: true },
  { key: 'distCbfiAnual', label: 'Dist. CBFI Anual', alignRight: true },
  { key: 'rentaBruta', label: 'Renta Bruta', alignRight: true },
  { key: 'rentaBrutaAnual', label: 'Renta Bruta Anual', alignRight: true },
]

export function CalculadoraPage() {
  const { data = [], isLoading, isError } = useQuery({
    queryKey: ['calculadora'],
    queryFn: fetchCalculadoraFibras,
  })
  const [montos, setMontos] = useState<Record<string, string>>({})
  const [filtro, setFiltro] = useState('')
  const [sort, setSort] = useState<{ col: SortKey; dir: 'asc' | 'desc' | null }>({ col: '', dir: null })

  const rows = useMemo(() => {
    return data.map<CalculadoraRow>((fibra) => {
      const montoInput = montos[fibra.ticker] ?? ''
      const montoValue = parseMonto(montoInput)
      const precioActual = toNumberOrNull(fibra.precioActual)
      const distCbfi = toNumberOrNull(fibra.distCbfi)
      const distCbfiAnual = toNumberOrNull(fibra.distCbfiAnual)
      const priceForCalc = precioActual ?? 0
      const cbfis = calcCbfis(montoValue, priceForCalc)
      const sobra = priceForCalc === 0 ? 0 : calcSobra(montoValue, cbfis, priceForCalc)

      return {
        ticker: fibra.ticker,
        empresa: fibra.empresa,
        precioActual,
        ultimoPeriodo: fibra.ultimoPeriodo ?? null,
        distCbfi,
        distCbfiAnual,
        freshnessStatus: fibra.freshnessStatus ?? null,
        montoInput,
        montoValue,
        cbfis,
        sobra,
        rentaBruta: calcRentaBruta(cbfis, distCbfi),
        rentaBrutaAnual: calcRentaBrutaAnual(cbfis, distCbfiAnual),
      }
    })
  }, [data, montos])

  const filteredRows = useMemo(() => {
    const query = filtro.trim().toLowerCase()
    if (!query) return rows
    return rows.filter((row) =>
      row.ticker.toLowerCase().includes(query) || row.empresa.toLowerCase().includes(query),
    )
  }, [filtro, rows])

  const sortedRows = useMemo(() => sortCalculadoraRows(filteredRows, sort), [filteredRows, sort])
  const cycleSort = (col: Exclude<SortKey, ''>) => {
    setSort((current) => {
      if (current.col !== col) return { col, dir: 'asc' as const }
      if (current.dir === 'asc') return { col, dir: 'desc' as const }
      return { col: '', dir: null }
    })
  }

  return (
    <>
      <div className="bg-[radial-gradient(circle_at_top_left,rgba(38,103,255,0.12),transparent_34%),linear-gradient(180deg,rgba(10,14,26,0.02),transparent_26%)]">
        <div className="container mx-auto px-4 py-8">
          <div className="mb-8 max-w-4xl space-y-4">
            <div className="inline-flex items-center gap-2 rounded-full border border-border bg-surface-elevated px-3 py-1 text-[11px] font-semibold uppercase tracking-[0.28em] text-primary">
              Herramienta pública
            </div>
            <h1 className="font-playfair text-4xl font-bold leading-tight text-foreground md:text-5xl">
              Calculadora de compra de FIBRAs
            </h1>
            <p className="max-w-3xl text-sm leading-6 text-muted-foreground md:text-base">
              Ingresa tu presupuesto por FIBRA para estimar cuántos CBFIs puedes comprar, cuánta distribución
              proyectarías y cuál sería tu renta bruta estimada para cada emisora activa.
            </p>
          </div>

          <section className="rounded-3xl border border-border bg-surface-elevated shadow-sm">
            <div className="flex flex-col gap-4 border-b border-border px-4 py-4 md:flex-row md:items-end md:justify-between">
              <div className="space-y-1">
                <p className="text-xs font-semibold uppercase tracking-[0.24em] text-muted-foreground">Tabla activa</p>
                <p className="text-sm leading-6 text-muted-foreground">
                  La tabla conserva los montos al filtrar u ordenar y responde en tiempo real a cada cambio de valor.
                </p>
              </div>

              <label className="w-full max-w-sm space-y-1.5" htmlFor="calculadora-search">
                <span className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                  Buscar fibra...
                </span>
                <input
                  id="calculadora-search"
                  name="filtro"
                  type="text"
                  aria-label="Buscar fibra por ticker o nombre"
                  value={filtro}
                  onChange={(event) => setFiltro(event.target.value)}
                  placeholder="Buscar fibra..."
                  className="flex h-10 w-full rounded-xl border border-input bg-background px-3 text-sm text-foreground outline-none transition-colors placeholder:text-muted-foreground/60 focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50"
                />
              </label>
            </div>

            {isLoading ? (
              <TableSkeleton />
            ) : isError ? (
              <div className="px-4 py-10 text-sm text-muted-foreground">
                No se pudo cargar la calculadora de FIBRAs. Intenta de nuevo más tarde.
              </div>
            ) : (
              <div className="overflow-x-auto">
                <table className="min-w-[1280px] w-full border-separate border-spacing-0 text-sm">
                  <thead className="sticky top-0 z-10 bg-surface-elevated/95 backdrop-blur supports-[backdrop-filter]:bg-surface-elevated/85">
                    <tr className="border-b border-border text-left text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                      <th className="sticky left-0 z-20 border-b border-border bg-surface-elevated px-4 py-3">$ a calcular</th>
                      {SORTABLE_COLUMNS.map((column) => (
                        <th
                          key={column.key}
                          className={`border-b border-border px-4 py-3 ${column.alignRight ? 'text-right' : 'text-left'}`}
                        >
                          <SortHeader
                            label={column.label}
                            active={sort.col === column.key}
                            dir={sort.dir}
                            onClick={() => cycleSort(column.key)}
                          />
                        </th>
                      ))}
                    </tr>
                  </thead>
                  <tbody>
                    {sortedRows.length === 0 ? (
                      <tr>
                        <td colSpan={10} className="px-4 py-12 text-center text-sm text-muted-foreground">
                          No hay FIBRAs que coincidan con la búsqueda.
                        </td>
                      </tr>
                    ) : (
                      sortedRows.map((row) => (
                        <tr key={row.ticker} className="group border-b border-border/70 hover:bg-muted/30">
                          <td className="sticky left-0 z-0 border-b border-border/70 bg-surface-elevated px-4 py-3 align-top">
                            <label className="sr-only" htmlFor={`monto-${row.ticker}`}>
                              Monto a calcular para {row.ticker}
                            </label>
                            <input
                              id={`monto-${row.ticker}`}
                              name={`monto-${row.ticker}`}
                              type="text"
                              inputMode="decimal"
                              autoComplete="off"
                              placeholder="0"
                              value={row.montoInput}
                              onChange={(event) =>
                                setMontos((current) => ({ ...current, [row.ticker]: event.target.value }))
                              }
                              className="h-10 w-[100px] rounded-lg border border-input bg-background px-3 text-right tabular-nums text-foreground outline-none transition-colors placeholder:text-muted-foreground/50 focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50"
                            />
                          </td>
                          <td className="border-b border-border/70 px-4 py-3 align-top">
                            <div className="space-y-0.5">
                              <div className="font-semibold text-foreground">{row.ticker}</div>
                              <div className="max-w-[18rem] text-xs leading-5 text-muted-foreground">{row.empresa}</div>
                            </div>
                          </td>
                          <td className="border-b border-border/70 px-4 py-3 align-top text-foreground">
                            {row.ultimoPeriodo ?? '—'}
                          </td>
                          <td className="border-b border-border/70 px-4 py-3 align-top text-right tabular-nums text-foreground">
                            {formatInteger(row.cbfis)}
                          </td>
                          <td className="border-b border-border/70 px-4 py-3 align-top text-right tabular-nums text-foreground">
                            {formatMoney(row.sobra)}
                          </td>
                          <td className="border-b border-border/70 px-4 py-3 align-top text-right tabular-nums text-foreground">
                            {formatMoney(row.precioActual)}
                          </td>
                          <td className="border-b border-border/70 px-4 py-3 align-top text-right tabular-nums text-foreground">
                            {formatDistribution(row.distCbfi)}
                          </td>
                          <td className="border-b border-border/70 px-4 py-3 align-top text-right tabular-nums text-foreground">
                            {formatDistribution(row.distCbfiAnual)}
                          </td>
                          <td className="border-b border-border/70 px-4 py-3 align-top text-right tabular-nums text-foreground">
                            {formatRenta(row.rentaBruta, row.distCbfi)}
                          </td>
                          <td className="border-b border-border/70 px-4 py-3 align-top text-right tabular-nums text-foreground">
                            {formatRenta(row.rentaBrutaAnual, row.distCbfiAnual)}
                          </td>
                        </tr>
                      ))
                    )}
                  </tbody>
                </table>
              </div>
            )}
          </section>
        </div>
      </div>
    </>
  )
}

function SortHeader({
  label,
  active,
  dir,
  onClick,
}: {
  label: string
  active: boolean
  dir: 'asc' | 'desc' | null
  onClick: () => void
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      className="inline-flex items-center gap-1.5 text-left transition-colors hover:text-foreground"
    >
      <span>{label}</span>
      <span className="text-[11px] leading-none text-muted-foreground">{active ? (dir === 'asc' ? '↑' : dir === 'desc' ? '↓' : '') : ''}</span>
    </button>
  )
}

function TableSkeleton() {
  return (
    <div className="overflow-x-auto">
      <div className="min-w-[1280px] divide-y divide-border">
        {Array.from({ length: 6 }).map((_, index) => (
          <div key={index} className="grid grid-cols-[110px_1.3fr_110px_100px_100px_110px_120px_130px_120px_140px] gap-0 px-4 py-3 animate-pulse">
            {Array.from({ length: 10 }).map((__, cellIndex) => (
              <div key={cellIndex} className={`h-4 rounded bg-muted ${cellIndex === 0 ? 'w-[76px]' : 'w-[70%]'}`} />
            ))}
          </div>
        ))}
      </div>
    </div>
  )
}

function parseMonto(value: string): number {
  const trimmed = value.trim()
  if (!trimmed) return 0
  const normalized = trimmed.replace(/,/g, '')
  const parsed = Number(normalized)
  return Number.isFinite(parsed) ? parsed : 0
}

function toNumberOrNull(value: number | string | null | undefined): number | null {
  if (value == null) return null
  const numeric = typeof value === 'string' ? Number(value) : value
  return Number.isFinite(numeric) ? numeric : null
}

function formatInteger(value: number | null): string {
  if (value == null) return '—'
  return INT_FORMATTER.format(value)
}

function formatDistribution(value: number | null): string {
  if (value == null) return '—'
  return `$${DIST_FORMATTER.format(value)}`
}

function formatRenta(value: number | null, distValue: number | null): string {
  if (distValue == null) return '—'
  return formatMoney(value)
}

function sortCalculadoraRows(rows: CalculadoraRow[], sort: { col: SortKey; dir: 'asc' | 'desc' | null }) {
  if (!sort.col || !sort.dir) return [...rows]

  return [...rows].sort((a, b) => {
    const left = getSortValue(a, sort.col as Exclude<SortKey, ''>)
    const right = getSortValue(b, sort.col as Exclude<SortKey, ''>)

    if (left == null && right == null) return a.ticker.localeCompare(b.ticker)
    if (left == null) return 1
    if (right == null) return -1

    if (typeof left === 'string' && typeof right === 'string') {
      const diff = left.localeCompare(right, 'es-MX')
      return sort.dir === 'asc' ? diff : -diff
    }

    const diff = Number(left) - Number(right)
    if (diff !== 0) return sort.dir === 'asc' ? diff : -diff
    return a.ticker.localeCompare(b.ticker)
  })
}

function getSortValue(row: CalculadoraRow, col: Exclude<SortKey, ''>): string | number | null {
  switch (col) {
    case 'ticker':
      return row.ticker
    case 'periodo':
      return parsePeriodo(row.ultimoPeriodo)
    case 'cbfis':
      return row.cbfis
    case 'sobra':
      return row.sobra
    case 'precioActual':
      return row.precioActual
    case 'distCbfi':
      return row.distCbfi
    case 'distCbfiAnual':
      return row.distCbfiAnual
    case 'rentaBruta':
      return row.rentaBruta
    case 'rentaBrutaAnual':
      return row.rentaBrutaAnual
    default:
      return null
  }
}

function parsePeriodo(periodo: string | null): number | null {
  if (!periodo) return null
  const match = /^Q([1-4])-(\d{4})$/.exec(periodo)
  if (!match) return null
  const quarter = Number(match[1])
  const year = Number(match[2])
  return year * 10 + quarter
}
