export type FreshnessStatus = 'fresh' | 'stale' | 'off-hours' | 'critical'

interface StatusConfig {
  label: string
  className: string
  dotClassName: string
}

const STATUS_CONFIG: Record<FreshnessStatus, StatusConfig> = {
  fresh: {
    label: 'Fresh',
    className: 'bg-positive-muted text-positive border-positive/20',
    dotClassName: 'bg-positive animate-pulse',
  },
  stale: {
    label: 'Stale',
    className: 'bg-warning-muted text-warning border-warning/20',
    dotClassName: 'bg-warning',
  },
  'off-hours': {
    label: 'Fuera de horario',
    className: 'bg-muted text-muted-foreground border-border',
    dotClassName: 'bg-muted-foreground',
  },
  critical: {
    label: 'Crítico',
    className: 'bg-negative-muted text-negative border-negative/20',
    dotClassName: 'bg-negative',
  },
}

interface Props {
  status: FreshnessStatus
  lastUpdated?: string
}

export function FreshnessBadge({ status, lastUpdated }: Props) {
  const config = STATUS_CONFIG[status]
  return (
    <span
      className={`inline-flex items-center gap-1.5 rounded-full border px-2 py-0.5 text-xs font-medium ${config.className}`}
    >
      <span className={`size-1.5 rounded-full shrink-0 ${config.dotClassName}`} />
      {config.label}
      {lastUpdated && <span className="opacity-60">· {lastUpdated}</span>}
    </span>
  )
}
