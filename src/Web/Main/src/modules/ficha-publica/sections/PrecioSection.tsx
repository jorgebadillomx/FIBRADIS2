import { FreshnessBadge } from '@/shared/ui/freshness-badge'
import type { FreshnessStatus } from '@/shared/ui/freshness-badge'
import { toNum, formatRelativeTime } from '@/shared/lib/format-time'

interface PrecioSectionProps {
  lastPrice: number | string | null | undefined
  dailyChange: number | string | null | undefined
  dailyChangePct: number | string | null | undefined
  capturedAt: string | null | undefined
  freshnessStatus: string | null | undefined
}

export function PrecioSection({ lastPrice, dailyChange, dailyChangePct, capturedAt, freshnessStatus }: PrecioSectionProps) {
  const price = toNum(lastPrice)
  const changePct = toNum(dailyChangePct)
  const change = toNum(dailyChange)
  const hasData = price != null && freshnessStatus != null

  return (
    <div className="rounded-xl border border-border bg-surface-elevated px-5 py-4">
      <div className="flex items-end gap-3">
        {hasData ? (
          <>
            <span className="text-4xl font-bold tabular-nums tracking-tight">
              {price!.toFixed(2)}
            </span>
            <div className="pb-1 space-y-1">
              <FreshnessBadge
                status={freshnessStatus as FreshnessStatus}
                lastUpdated={capturedAt ? formatRelativeTime(capturedAt) : undefined}
              />
              {changePct != null && (
                <p className={`text-sm font-medium ${changePct >= 0 ? 'text-positive' : 'text-negative'}`}>
                  {changePct >= 0 ? '+' : ''}{changePct.toFixed(2)}%
                  {change != null && (
                    <span className="text-xs text-muted-foreground ml-1">
                      ({change >= 0 ? '+' : ''}{change.toFixed(2)})
                    </span>
                  )}
                </p>
              )}
            </div>
          </>
        ) : (
          <span className="text-4xl font-bold tabular-nums tracking-tight text-muted-foreground">—</span>
        )}
      </div>
    </div>
  )
}
