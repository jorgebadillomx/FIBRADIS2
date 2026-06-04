import { Fragment, useMemo, useState } from 'react'
import { Trash2 } from 'lucide-react'
import type { components } from '@fibradis/shared-api-client'
import { PositionExpandedDetail } from '@/modules/portafolio/PositionExpandedDetail'
import { PortfolioExpandIcon, SignalBadge } from '@/modules/portafolio/SignalBadge'
import { formatMoney, formatPercent, formatVolume } from '@/modules/portafolio/portfolio-format'
import { EditableCell } from '@/modules/portafolio/EditableCell'
import { DeletePositionDialog } from '@/modules/portafolio/DeletePositionDialog'

type PortfolioPositionDto = components['schemas']['PortfolioPositionDto']

interface PositionsTableProps {
  positions: PortfolioPositionDto[]
  enabledColumns: string[]
  onUpdate: (fibraId: string, titulos: number, costoPromedio: number) => Promise<void>
  onDelete: (fibraId: string) => Promise<void>
}

type SortDirection = 'asc' | 'desc'
type SortEntry = { column: string; dir: SortDirection }

type SortableValue = string | number | null | undefined

const OPTIONAL_COLUMNS = [
  { key: 'capRate', label: 'Cap Rate', sortKey: 'capRate' },
  { key: 'navPerCbfi', label: 'NAV/CBFI', sortKey: 'navPerCbfi' },
  { key: 'ltv', label: 'LTV', sortKey: 'ltv' },
  { key: 'noiMargin', label: 'Margen NOI', sortKey: 'noiMargin' },
  { key: 'ffoMargin', label: 'Margen FFO', sortKey: 'ffoMargin' },
  { key: 'dailyChangePct', label: 'Cambio % diario', sortKey: 'dailyChangePct' },
  { key: 'week52High', label: 'Máx. 52S', sortKey: 'week52High' },
] as const

function formatOptionalValue(columnKey: string, value: number | string | null | undefined): string {
  if (
    columnKey === 'capRate' ||
    columnKey === 'ltv' ||
    columnKey === 'noiMargin' ||
    columnKey === 'ffoMargin' ||
    columnKey === 'dailyChangePct'
  ) {
    return formatPercent(value)
  }

  return formatMoney(value)
}

function getComparableValue(row: PortfolioPositionDto, column: string): SortableValue {
  switch (column) {
    case 'ticker':
      return row.ticker
    case 'nombre':
      return row.nombre
    case 'titulos':
      return row.titulos
    case 'costoPromedio':
      return row.costoPromedio
    case 'precioActual':
      return row.precioActual
    case 'valorMercado':
      return row.valorMercado
    case 'plusvaliaFilaPct':
      return row.plusvaliaFilaPct
    case 'plusvaliaFilaMxn':
      return row.plusvaliaFilaMxn
    case 'rentaAnual':
      return row.rentaAnual
    case 'pctPortafolio':
      return row.pctPortafolio
    case 'capRate':
      return row.capRate
    case 'navPerCbfi':
      return row.navPerCbfi
    case 'ltv':
      return row.ltv
    case 'noiMargin':
      return row.noiMargin
    case 'ffoMargin':
      return row.ffoMargin
    case 'dailyChangePct':
      return row.dailyChangePct
    case 'week52High':
      return row.week52High
    default:
      return null
  }
}

function compareValues(a: SortableValue, b: SortableValue): number {
  if (a == null && b == null) return 0
  if (a == null) return 1
  if (b == null) return -1

  if (typeof a === 'string' || typeof b === 'string') {
    return String(a).localeCompare(String(b), 'es-MX', { sensitivity: 'base' })
  }

  return a - b
}

function SortArrow({ dir }: { dir: SortDirection }) {
  return <span className="ml-1 text-xs text-muted-foreground">{dir === 'asc' ? '↑' : '↓'}</span>
}

export function PositionsTable({ positions, enabledColumns, onUpdate, onDelete }: PositionsTableProps) {
  const [sortKeys, setSortKeys] = useState<SortEntry[]>([])
  const [expandedRows, setExpandedRows] = useState<Set<string>>(new Set())
  const [deletingFibraId, setDeletingFibraId] = useState<string | null>(null)
  const [deleteLoading, setDeleteLoading] = useState(false)

  const visibleOptionalColumns = useMemo(
    () => OPTIONAL_COLUMNS.filter((column) => enabledColumns.includes(column.key)),
    [enabledColumns]
  )

  const sortedPositions = useMemo(() => {
    if (sortKeys.length === 0) return positions

    return [...positions].sort((left, right) => {
      for (const sort of sortKeys) {
        const comparison = compareValues(
          getComparableValue(left, sort.column),
          getComparableValue(right, sort.column)
        )

        if (comparison !== 0) {
          return sort.dir === 'asc' ? comparison : -comparison
        }
      }

      return 0
    })
  }, [positions, sortKeys])

  const totalCols = 12 + visibleOptionalColumns.length

  async function handleDeleteConfirm() {
    if (!deletingFibraId) return
    setDeleteLoading(true)
    try {
      await onDelete(deletingFibraId)
      setDeletingFibraId(null)
    } finally {
      setDeleteLoading(false)
    }
  }

  function handleHeaderClick(column: string, shiftKey: boolean) {
    setSortKeys((current) => {
      if (shiftKey) {
        const existing = current.find((sort) => sort.column === column)
        if (existing) {
          return current.map((sort) =>
            sort.column === column
              ? { ...sort, dir: sort.dir === 'asc' ? 'desc' : 'asc' }
              : sort
          )
        }

        return [...current, { column, dir: 'desc' }]
      }

      const existing = current[0]?.column === column ? current[0] : current.find((sort) => sort.column === column)
      const nextDir: SortDirection = existing?.dir === 'desc' ? 'asc' : 'desc'
      return [{ column, dir: nextDir }]
    })
  }

  function toggleRow(fibraId: string) {
    setExpandedRows((current) => {
      const next = new Set(current)
      if (next.has(fibraId)) {
        next.delete(fibraId)
      } else {
        next.add(fibraId)
      }
      return next
    })
  }

  function renderHeader(label: string, columnKey: string) {
    const sort = sortKeys.find((entry) => entry.column === columnKey)

    return (
      <th className="px-3 py-3 text-left font-semibold text-foreground">
        <button
          type="button"
          className="inline-flex items-center gap-1 text-left transition-colors hover:text-primary"
          onClick={(event) => handleHeaderClick(columnKey, event.shiftKey)}
        >
          <span>{label}</span>
          {sort ? <SortArrow dir={sort.dir} /> : <span className="ml-1 text-xs text-transparent">↑</span>}
        </button>
      </th>
    )
  }

  return (
    <>
    <div className="overflow-hidden rounded-2xl border border-border bg-card shadow-sm">
      <div className="overflow-x-auto">
        <table className="min-w-full text-sm">
          <thead className="sticky top-0 z-10 bg-muted/70 text-muted-foreground backdrop-blur">
            <tr className="border-b border-border/80">
              <th className="w-20 px-2 py-3 text-left font-semibold text-foreground">Señal</th>
              <th className="w-8 px-1 py-3"><span className="sr-only">Expandir</span></th>
              {renderHeader('Ticker / Nombre', 'ticker')}
              {renderHeader('Títulos', 'titulos')}
              {renderHeader('Costo Promedio', 'costoPromedio')}
              {renderHeader('Precio Actual', 'precioActual')}
              {renderHeader('Valor de Mercado', 'valorMercado')}
              {renderHeader('Plusvalía %', 'plusvaliaFilaPct')}
              {renderHeader('Ganancia $', 'plusvaliaFilaMxn')}
              {renderHeader('Renta Anual', 'rentaAnual')}
              {renderHeader('% Portafolio', 'pctPortafolio')}
              {visibleOptionalColumns.map((column) => renderHeader(column.label, column.sortKey))}
              <th className="w-16 px-2 py-3 text-center font-semibold text-foreground">Acc.</th>
            </tr>
          </thead>
          <tbody>
            {sortedPositions.map((position) => {
              const isExpanded = expandedRows.has(position.fibraId)

              return (
                <Fragment key={position.fibraId}>
                  <tr
                    className="border-b border-border/70 transition-colors hover:bg-muted/40"
                  >
                    <td className="px-2 py-3">
                      <SignalBadge navPerCbfi={position.navPerCbfi} precioActual={position.precioActual} />
                    </td>
                    <td className="px-1 py-3">
                      <button
                        type="button"
                        className="text-muted-foreground transition-transform hover:text-foreground"
                        onClick={() => toggleRow(position.fibraId)}
                        aria-label={isExpanded ? 'Colapsar posición' : 'Expandir posición'}
                      >
                        <PortfolioExpandIcon isExpanded={isExpanded} />
                      </button>
                    </td>
                    <td className="px-3 py-3 align-top">
                      <div className="flex flex-col">
                        <span className="font-mono font-semibold text-foreground">{position.ticker}</span>
                        <span className="text-xs text-muted-foreground">{position.nombre}</span>
                      </div>
                    </td>
                    <td className="px-3 py-3 text-right tabular-nums">
                      <EditableCell
                        value={Number(position.titulos)}
                        format={(v) => formatVolume(v)}
                        validate={(raw) => {
                          const trimmed = raw.trim()
                          const n = parseInt(trimmed, 10)
                          if (!Number.isInteger(n) || n <= 0 || String(n) !== trimmed)
                            return 'La cantidad debe ser un entero positivo'
                          return null
                        }}
                        parse={(raw) => parseInt(raw.trim(), 10)}
                        onSave={(newVal) => {
                          const costo = Number(position.costoPromedio)
                          if (!Number.isFinite(costo) || costo <= 0) return Promise.reject(new Error('Datos de posición inválidos.'))
                          return onUpdate(position.fibraId, newVal, costo)
                        }}
                      />
                    </td>
                    <td className="px-3 py-3 text-right tabular-nums">
                      <EditableCell
                        value={Number(position.costoPromedio)}
                        format={(v) => formatMoney(v)}
                        validate={(raw) => {
                          const n = parseFloat(raw.trim())
                          if (!Number.isFinite(n) || n <= 0)
                            return 'El costo promedio debe ser mayor a cero'
                          return null
                        }}
                        parse={(raw) => parseFloat(raw.trim())}
                        onSave={(newVal) => {
                          const titulos = Number(position.titulos)
                          if (!Number.isInteger(titulos) || titulos <= 0) return Promise.reject(new Error('Datos de posición inválidos.'))
                          return onUpdate(position.fibraId, titulos, newVal)
                        }}
                      />
                    </td>
                    <td className="px-3 py-3 text-right tabular-nums">{formatMoney(position.precioActual)}</td>
                    <td className="px-3 py-3 text-right tabular-nums">{formatMoney(position.valorMercado)}</td>
                    <td className="px-3 py-3 text-right tabular-nums">{formatPercent(position.plusvaliaFilaPct)}</td>
                    <td className="px-3 py-3 text-right tabular-nums">{formatMoney(position.plusvaliaFilaMxn)}</td>
                    <td className="px-3 py-3 text-right tabular-nums">{formatMoney(position.rentaAnual)}</td>
                    <td className="px-3 py-3 text-right tabular-nums">{formatPercent(position.pctPortafolio)}</td>
                    {visibleOptionalColumns.map((column) => (
                      <td
                        key={`${position.fibraId}-${column.key}`}
                        className="px-3 py-3 text-right tabular-nums"
                      >
                        {formatOptionalValue(
                          column.key,
                          position[column.key as keyof PortfolioPositionDto] as
                            | number
                            | string
                            | null
                            | undefined
                        )}
                      </td>
                    ))}
                    <td className="px-2 py-3 text-center">
                      <button
                        type="button"
                        className="rounded p-1 text-muted-foreground hover:bg-destructive/10 hover:text-destructive"
                        onClick={() => setDeletingFibraId(position.fibraId)}
                        aria-label={`Eliminar posición ${position.ticker}`}
                        title={`Eliminar posición ${position.ticker}`}
                      >
                        <Trash2 className="h-4 w-4" />
                      </button>
                    </td>
                  </tr>
                  {isExpanded && (
                    <tr key={`${position.fibraId}-detail`}>
                      <td colSpan={totalCols} className="bg-muted/20 px-4 py-4">
                        <PositionExpandedDetail position={position} />
                      </td>
                    </tr>
                  )}
                </Fragment>
              )
            })}
          </tbody>
        </table>
      </div>
    </div>
    <DeletePositionDialog
      ticker={positions.find((p) => p.fibraId === deletingFibraId)?.ticker ?? (deletingFibraId ?? '')}
      open={deletingFibraId !== null}
      onOpenChange={(open) => { if (!open) setDeletingFibraId(null) }}
      onConfirm={handleDeleteConfirm}
      isLoading={deleteLoading}
    />
    </>
  )
}
