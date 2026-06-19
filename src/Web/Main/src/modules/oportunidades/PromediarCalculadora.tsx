import { useMemo, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { fetchCalculadoraFibras } from '@/api/fibrasApi'
import { formatMoney } from '@/modules/portafolio/portfolio-format'
import {
  calcCbfisConComision,
  calcSobraConComision,
  calcRentaBruta,
  calcRentaBrutaAnual,
  isRecentQuarter,
} from '@/modules/calculadora/calculadora-logic'

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

interface CalculadoraConComisionRow {
  ticker: string
  empresa: string
  precioActual: number | null
  ultimoPeriodo: string | null
  distCbfi: number | null
  distCbfiAnual: number | null
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

const INT_FORMATTER = new Intl.NumberFormat('es-MX', { maximumFractionDigits: 0 })

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

export function PromediarCalculadora({
  commissionFactor,
  ivaFactor,
}: {
  commissionFactor: number
  ivaFactor: number
}) {
  const { data = [], isLoading, isError } = useQuery({
    queryKey: ['calculadora'],
    queryFn: fetchCalculadoraFibras,
  })

  const [montos, setMontos] = useState<Record<string, string>>({})
  const [sort, setSort] = useState<{ col: SortKey; dir: 'asc' | 'desc' | null }>({
    col: 'rentaBrutaAnual',
    dir: 'desc',
  })

  const rows = useMemo(() => {
    return data
      .filter((fibra) => isRecentQuarter(fibra.ultimoPeriodo ?? null))
      .map<CalculadoraConComisionRow>((fibra) => {
        const montoInput = montos[fibra.ticker] ?? '1000'
        const montoValue = parseMonto(montoInput)
        const precioActual = toNumberOrNull(fibra.precioActual)
        const distCbfi = toNumberOrNull(fibra.distCbfi)
        const distCbfiAnual = toNumberOrNull(fibra.distCbfiAnual)
        const priceForCalc = precioActual ?? 0
        const cbfis = calcCbfisConComision(montoValue, priceForCalc, commissionFactor, ivaFactor)
        const sobra = priceForCalc === 0
          ? montoValue
          : calcSobraConComision(montoValue, cbfis, priceForCalc, commissionFactor, ivaFactor)
        return {
          ticker: fibra.ticker,
          empresa: fibra.empresa,
          precioActual,
          ultimoPeriodo: fibra.ultimoPeriodo ?? null,
          distCbfi,
          distCbfiAnual,
          montoInput,
          montoValue,
          cbfis,
          sobra,
          rentaBruta: calcRentaBruta(cbfis, distCbfi),
          rentaBrutaAnual: calcRentaBrutaAnual(cbfis, distCbfiAnual),
        }
      })
  }, [data, montos, commissionFactor, ivaFactor])

  const sortedRows = useMemo(() => sortRows(rows, sort), [rows, sort])

  const cycleSort = (col: Exclude<SortKey, ''>) => {
    setSort((current) => {
      if (current.col !== col) return { col, dir: 'asc' as const }
      if (current.dir === 'asc') return { col, dir: 'desc' as const }
      return { col: '', dir: null }
    })
  }

  return (
    <section className="rounded-2xl border border-border bg-card shadow-sm">
      <div className="flex flex-col gap-3 border-b border-border px-4 py-4 lg:flex-row lg:items-end lg:justify-between">
        <div className="space-y-1">
          <p className="text-xs font-semibold uppercase tracking-[0.24em] text-primary">
            Calculadora con comisión
          </p>
          <h3 className="text-base font-semibold text-foreground">
            Rendimiento por FIBRA activa
          </h3>
          <p className="text-sm text-muted-foreground">
            FIBRAs con al menos una distribución en los últimos 4 trimestres.
          </p>
        </div>
        <div className="flex flex-wrap items-center gap-3 text-sm">
          <span className="inline-flex items-center gap-1.5 rounded-full border border-primary/30 bg-primary/10 px-3 py-1 text-xs font-semibold text-primary">
            Ya contempla comisión e IVA
          </span>
          <span className="text-muted-foreground">
            Comisión:{' '}
            <span className="font-semibold text-foreground">{(commissionFactor * 100).toFixed(2)}%</span>
          </span>
          <span className="text-muted-foreground">
            IVA:{' '}
            <span className="font-semibold text-foreground">{(ivaFactor * 100).toFixed(0)}%</span>
          </span>
        </div>
      </div>

      {isLoading ? (
        <TableSkeleton />
      ) : isError ? (
        <div className="px-4 py-10 text-sm text-muted-foreground">
          No se pudo cargar la calculadora. Intenta de nuevo más tarde.
        </div>
      ) : rows.length === 0 ? (
        <div className="px-4 py-10 text-sm text-muted-foreground text-center">
          No hay FIBRAs activas con distribuciones recientes.
        </div>
      ) : (
        <div className="overflow-x-auto">
          <table className="min-w-[1280px] w-full border-separate border-spacing-0 text-sm">
            <thead className="sticky top-0 z-10 bg-card/95 backdrop-blur supports-[backdrop-filter]:bg-card/85">
              <tr className="border-b border-border text-left text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                <th className="sticky left-0 z-20 border-b border-border bg-card px-4 py-3">$ a calcular</th>
                {SORTABLE_COLUMNS.map((col) => (
                  <th
                    key={col.key}
                    className={`border-b border-border px-4 py-3 ${col.alignRight ? 'text-right' : 'text-left'}`}
                  >
                    <SortHeader
                      label={col.label}
                      active={sort.col === col.key}
                      dir={sort.dir}
                      onClick={() => cycleSort(col.key)}
                    />
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {sortedRows.map((row) => (
                <tr key={row.ticker} className="group border-b border-border/70 hover:bg-muted/30">
                  <td className="sticky left-0 z-0 border-b border-border/70 bg-card px-4 py-3 align-top">
                    <label className="sr-only" htmlFor={`monto-comision-${row.ticker}`}>
                      Monto a calcular para {row.ticker}
                    </label>
                    <input
                      id={`monto-comision-${row.ticker}`}
                      name={`monto-comision-${row.ticker}`}
                      type="text"
                      inputMode="decimal"
                      autoComplete="off"
                      placeholder="0"
                      value={row.montoInput}
                      onChange={(e) =>
                        setMontos((prev) => ({ ...prev, [row.ticker]: e.target.value }))
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
                    {INT_FORMATTER.format(row.cbfis)}
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
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
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
      <span className="text-[11px] leading-none text-muted-foreground">
        {active ? (dir === 'asc' ? '↑' : dir === 'desc' ? '↓' : '') : ''}
      </span>
    </button>
  )
}

function TableSkeleton() {
  return (
    <div className="overflow-x-auto">
      <div className="min-w-[1280px] divide-y divide-border">
        {Array.from({ length: 6 }).map((_, index) => (
          <div
            key={index}
            className="grid grid-cols-[110px_1.3fr_110px_100px_100px_110px_120px_130px_120px_140px] gap-0 px-4 py-3 animate-pulse"
          >
            {Array.from({ length: 10 }).map((__, cellIndex) => (
              <div
                key={cellIndex}
                className={`h-4 rounded bg-muted ${cellIndex === 0 ? 'w-[76px]' : 'w-[70%]'}`}
              />
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

function formatDistribution(value: number | null): string {
  if (value == null) return '—'
  return `$${DIST_FORMATTER.format(value)}`
}

function formatRenta(value: number | null, distValue: number | null): string {
  if (distValue == null) return '—'
  return formatMoney(value)
}

function sortRows(
  rows: CalculadoraConComisionRow[],
  sort: { col: SortKey; dir: 'asc' | 'desc' | null },
): CalculadoraConComisionRow[] {
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

function getSortValue(
  row: CalculadoraConComisionRow,
  col: Exclude<SortKey, ''>,
): string | number | null {
  switch (col) {
    case 'ticker': return row.ticker
    case 'periodo': return parsePeriodo(row.ultimoPeriodo)
    case 'cbfis': return row.cbfis
    case 'sobra': return row.sobra
    case 'precioActual': return row.precioActual
    case 'distCbfi': return row.distCbfi
    case 'distCbfiAnual': return row.distCbfiAnual
    case 'rentaBruta': return row.rentaBruta
    case 'rentaBrutaAnual': return row.rentaBrutaAnual
    default: return null
  }
}

function parsePeriodo(periodo: string | null): number | null {
  if (!periodo) return null
  const match = /^Q([1-4])-(\d{4})$/.exec(periodo)
  if (!match) return null
  return Number(match[2]) * 10 + Number(match[1])
}
