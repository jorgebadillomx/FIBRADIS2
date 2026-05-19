import { useEffect, useRef } from 'react'
import { useQuery } from '@tanstack/react-query'
import { fetchMarketSnapshots, fetchAllFibras } from '@/api/fibrasApi'
import { toNum } from '@/shared/lib/format-time'

const CARD_WIDTH = 144 + 12 // w-36 (144px) + gap-3 (12px)
const AUTO_SCROLL_MS = 3000

export function PriceCarousel() {
  const { data: snapshots = [], isLoading } = useQuery({
    queryKey: ['market-snapshots'],
    queryFn: fetchMarketSnapshots,
    staleTime: 60_000,
    refetchInterval: 5 * 60_000,
  })

  const { data: fibras = [] } = useQuery({
    queryKey: ['fibras'],
    queryFn: fetchAllFibras,
    staleTime: 5 * 60_000,
  })

  const fibraByTicker = Object.fromEntries(fibras.map(f => [f.ticker, f]))
  const scrollRef = useRef<HTMLDivElement>(null)
  const timerRef = useRef<ReturnType<typeof setInterval> | null>(null)

  useEffect(() => {
    const el = scrollRef.current
    if (!el || snapshots.length === 0) return

    const advance = () => {
      if (!el) return
      const atEnd = el.scrollLeft + el.clientWidth >= el.scrollWidth - 4
      if (atEnd) {
        el.scrollTo({ left: 0, behavior: 'smooth' })
      } else {
        el.scrollBy({ left: CARD_WIDTH, behavior: 'smooth' })
      }
    }

    const start = () => {
      if (timerRef.current) clearInterval(timerRef.current)
      timerRef.current = setInterval(advance, AUTO_SCROLL_MS)
    }
    const stop = () => { if (timerRef.current) clearInterval(timerRef.current) }

    start()
    el.addEventListener('mouseenter', stop)
    el.addEventListener('mouseleave', start)

    return () => {
      stop()
      el.removeEventListener('mouseenter', stop)
      el.removeEventListener('mouseleave', start)
    }
  }, [snapshots.length])

  if (isLoading) {
    return (
      <div aria-label="Carrusel de precios" className="relative">
        <div className="flex gap-3 overflow-x-auto pb-1 scrollbar-none">
          {Array.from({ length: 9 }).map((_, i) => (
            <div
              key={i}
              className="shrink-0 rounded-lg border border-border bg-surface-elevated px-3 py-2 w-36 flex items-center justify-between gap-2 animate-pulse"
            >
              <div className="space-y-1">
                <div className="h-3 w-10 bg-muted rounded" />
                <div className="h-2 w-14 bg-muted rounded" />
              </div>
              <div className="space-y-1 items-end flex flex-col">
                <div className="h-3 w-10 bg-muted rounded" />
                <div className="h-2 w-8 bg-muted rounded" />
              </div>
            </div>
          ))}
        </div>
      </div>
    )
  }

  return (
    <div aria-label="Carrusel de precios" className="relative">
      <div ref={scrollRef} className="flex gap-3 overflow-x-auto pb-1 scrollbar-none">
        {snapshots.map((snap) => {
          const fibra = fibraByTicker[snap.ticker]
          const lastPrice = toNum(snap.lastPrice)
          const changePct = toNum(snap.dailyChangePct)
          const hasPrice = lastPrice != null
          return (
            <a
              key={snap.ticker}
              href={`/fibras/${snap.ticker}`}
              className="shrink-0 rounded-lg border border-border bg-surface-elevated px-3 py-2 w-36 flex items-center justify-between gap-2 hover:border-brand/50 transition-colors"
            >
              <div className="min-w-0">
                <p className="text-xs font-semibold">{snap.ticker}</p>
                {fibra?.shortName && (
                  <p className="text-[10px] text-muted-foreground truncate leading-tight">{fibra.shortName}</p>
                )}
              </div>
              <div className="text-right shrink-0">
                {hasPrice ? (
                  <>
                    <p className="text-sm font-bold tabular-nums leading-tight">
                      {lastPrice!.toFixed(2)}
                    </p>
                    {changePct != null && (
                      <p className={`text-[10px] font-medium tabular-nums ${changePct >= 0 ? 'text-positive' : 'text-negative'}`}>
                        {changePct >= 0 ? '+' : ''}{changePct.toFixed(2)}%
                      </p>
                    )}
                  </>
                ) : (
                  <p className="text-sm font-bold tabular-nums text-muted-foreground">—</p>
                )}
              </div>
            </a>
          )
        })}
      </div>
    </div>
  )
}
