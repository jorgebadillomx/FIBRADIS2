import { FreshnessBadge } from '@/shared/ui/freshness-badge'
import type { FreshnessStatus } from '@/shared/ui/freshness-badge'
import { toNum, formatRelativeTime } from '@/shared/lib/format-time'
import { PRECIO_SECTION_LOADING_SHELL } from '../cwv-layout'

interface PrecioSectionProps {
  lastPrice: number | string | null | undefined
  dailyChange: number | string | null | undefined
  dailyChangePct: number | string | null | undefined
  capturedAt: string | null | undefined
  freshnessStatus: string | null | undefined
  isLoading?: boolean
  isError?: boolean
}

function PrecioSectionSkeleton() {
  return (
    <div aria-busy="true" className="rounded-xl border border-border bg-surface-elevated px-5 py-4">
      <div className="flex min-h-14 items-end gap-3">
        <div className={`h-10 animate-pulse rounded bg-muted/70 ${PRECIO_SECTION_LOADING_SHELL.priceWidthClass}`} />
        <div className={`${PRECIO_SECTION_LOADING_SHELL.metadataWidthClass} pb-1 space-y-1`}>
          <div className={`h-6 animate-pulse rounded-full bg-muted/70 ${PRECIO_SECTION_LOADING_SHELL.badgeWidthClass}`} />
          <div className={`h-4 animate-pulse rounded bg-muted/70 ${PRECIO_SECTION_LOADING_SHELL.detailWidthClass}`} />
        </div>
      </div>
    </div>
  )
}

// Mantiene la misma altura (min-h-14) que el skeleton y el estado cargado para no provocar shift (CLS).
function PrecioSectionError() {
  return (
    <div role="alert" className="rounded-xl border border-border bg-surface-elevated px-5 py-4">
      <div className="flex min-h-14 items-center gap-3">
        <span className={`text-4xl font-bold tabular-nums tracking-tight text-muted-foreground ${PRECIO_SECTION_LOADING_SHELL.priceFallbackWidthClass}`}>
          —
        </span>
        <p className="text-sm text-muted-foreground">No se pudo cargar el precio. Intenta de nuevo más tarde.</p>
      </div>
    </div>
  )
}

export function PrecioSection({
  lastPrice,
  dailyChange,
  dailyChangePct,
  capturedAt,
  freshnessStatus,
  isLoading = false,
  isError = false,
}: PrecioSectionProps) {
  const price = toNum(lastPrice)
  const changePct = toNum(dailyChangePct)
  const change = toNum(dailyChange)
  const hasData = price != null && freshnessStatus != null

  if (isLoading) {
    return <PrecioSectionSkeleton />
  }

  if (isError && !hasData) {
    return <PrecioSectionError />
  }

  return (
    <div className="rounded-xl border border-border bg-surface-elevated px-5 py-4">
      <div className="flex min-h-14 items-end gap-3">
        {hasData ? (
          <>
            <span className={`text-4xl font-bold tabular-nums tracking-tight ${PRECIO_SECTION_LOADING_SHELL.priceFallbackWidthClass}`}>
              {price!.toFixed(2)}
            </span>
            <div className={`${PRECIO_SECTION_LOADING_SHELL.metadataWidthClass} pb-1 space-y-1`}>
              <FreshnessBadge
                status={freshnessStatus as FreshnessStatus}
                lastUpdated={capturedAt ? formatRelativeTime(capturedAt) : undefined}
              />
              {changePct != null && (
                <p className={`text-sm font-medium ${changePct >= 0 ? 'text-positive' : 'text-negative'}`}>
                  {changePct >= 0 ? '+' : ''}{changePct.toFixed(2)}%
                  {change != null && (
                    <span className="ml-1 text-xs text-muted-foreground">
                      ({change >= 0 ? '+' : ''}{change.toFixed(2)})
                    </span>
                  )}
                </p>
              )}
            </div>
          </>
        ) : (
          <>
            <span className={`text-4xl font-bold tabular-nums tracking-tight text-muted-foreground ${PRECIO_SECTION_LOADING_SHELL.priceFallbackWidthClass}`}>
              —
            </span>
            <div className={`${PRECIO_SECTION_LOADING_SHELL.metadataWidthClass} pb-1 space-y-1`} aria-hidden="true">
              <div className={`h-6 rounded-full opacity-0 ${PRECIO_SECTION_LOADING_SHELL.badgeWidthClass}`} />
              <div className={`h-4 rounded opacity-0 ${PRECIO_SECTION_LOADING_SHELL.detailWidthClass}`} />
            </div>
          </>
        )}
      </div>
    </div>
  )
}
