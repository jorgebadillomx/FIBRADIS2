import { KPI_DEFINITIONS, type KpiKey } from '@/shared/lib/kpi-definitions'

interface Props {
  kpiKey: KpiKey
  label?: string
}

export function KpiLabel({ kpiKey, label }: Props) {
  const definition = KPI_DEFINITIONS[kpiKey]
  const tooltipText = `${definition.formula}\n\n${definition.description}`

  return (
    <span className="inline-flex items-center gap-1">
      <span className="text-foreground">{label ?? definition.label}</span>
      <span
        className="cursor-help select-none text-xs text-muted-foreground/60"
        title={tooltipText}
        aria-label={`Definición de ${definition.label}`}
      >
        ⓘ
      </span>
    </span>
  )
}
