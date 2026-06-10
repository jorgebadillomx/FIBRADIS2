import { useState } from 'react'
import { apiClient } from '@/api/fibrasApi'
import { Button } from '@/shared/ui/button'
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from '@/shared/ui/popover'

interface ColumnPickerProps {
  enabledColumns: string[]
  onEnabledColumnsChange: (columns: string[]) => void
}

const COLUMN_GROUPS = [
  {
    title: 'Fundamentales',
    columns: [
      { key: 'capRate', label: 'Cap Rate' },
      { key: 'navPerCbfi', label: 'NAV/CBFI' },
      { key: 'ltv', label: 'LTV' },
      { key: 'noiMargin', label: 'Margen NOI' },
      { key: 'ffoMargin', label: 'Margen FFO' },
      { key: 'yoc', label: 'YOC' },
    ],
  },
  {
    title: 'Mercado',
    columns: [
      { key: 'dailyChangePct', label: 'Cambio % diario' },
      { key: 'week52High', label: 'Máx. 52S' },
    ],
  },
] as const satisfies {
  title: string
  columns: ReadonlyArray<{ key: string; label: string }>
}[]

function uniqueColumns(columns: string[]) {
  return Array.from(new Set(columns))
}

export function ColumnPicker({ enabledColumns, onEnabledColumnsChange }: ColumnPickerProps) {
  const [isSaving, setIsSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function persistColumns(nextColumns: string[]) {
    const previousColumns = enabledColumns
    onEnabledColumnsChange(nextColumns)
    setIsSaving(true)
    setError(null)

    const { error: requestError } = await apiClient.PUT('/api/v1/portfolio/column-config', {
      body: { columns: nextColumns },
    })

    setIsSaving(false)

    if (requestError) {
      onEnabledColumnsChange(previousColumns)
      setError('No se pudo guardar la configuración de columnas.')
    }
  }

  function toggleColumn(columnKey: string) {
    if (isSaving) return

    const nextColumns = enabledColumns.includes(columnKey)
      ? enabledColumns.filter((column) => column !== columnKey)
      : uniqueColumns([...enabledColumns, columnKey])

    void persistColumns(nextColumns)
  }

  return (
    <Popover>
      <PopoverTrigger asChild>
        <Button variant="outline" size="sm" className="shrink-0">
          Columnas
          <span className="ml-2 rounded-full bg-muted px-2 py-0.5 text-[11px] font-medium tabular-nums text-muted-foreground">
            {enabledColumns.length}
          </span>
        </Button>
      </PopoverTrigger>
      <PopoverContent align="end" className="w-80">
        <div className="space-y-4">
          <div>
            <div className="text-sm font-semibold">Columnas configurables</div>
            <p className="mt-1 text-xs text-muted-foreground">
              Activa las columnas que quieres ver en la tabla del portafolio.
            </p>
          </div>

          {COLUMN_GROUPS.map((group) => (
            <div key={group.title} className="space-y-2">
              <div className="text-xs font-semibold uppercase tracking-[0.2em] text-muted-foreground">
                {group.title}
              </div>
              <div className="space-y-1">
                {group.columns.map((column) => {
                  const checked = enabledColumns.includes(column.key)
                  return (
                    <label
                      key={column.key}
                      className="flex cursor-pointer items-center gap-2 rounded-lg px-2 py-1.5 text-sm transition-colors hover:bg-muted/60"
                    >
                      <input
                        type="checkbox"
                        className="size-4 rounded border-border text-primary focus:ring-0"
                        checked={checked}
                        disabled={isSaving}
                        onChange={() => toggleColumn(column.key)}
                      />
                      <span>{column.label}</span>
                    </label>
                  )
                })}
              </div>
            </div>
          ))}

          {error && (
            <p className="rounded-lg border border-destructive/20 bg-destructive/5 px-3 py-2 text-xs text-destructive">
              {error}
            </p>
          )}
        </div>
      </PopoverContent>
    </Popover>
  )
}
