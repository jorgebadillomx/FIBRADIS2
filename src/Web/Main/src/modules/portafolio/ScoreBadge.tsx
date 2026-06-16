import { cn } from '@/shared/lib/utils'

function getToneClass(score: number): string {
  if (score >= 4) return 'border-emerald-200 bg-emerald-100 text-emerald-900'
  if (score >= 3) return 'border-amber-200 bg-amber-100 text-amber-900'
  return 'border-rose-200 bg-rose-100 text-rose-900'
}

export function ScoreBadge({ score }: { score: number }) {
  const normalized = score / 20

  return (
    <span
      className={cn(
        'inline-flex items-center rounded-full border px-2 py-0.5 text-[11px] font-semibold tabular-nums',
        getToneClass(normalized),
      )}
    >
      {normalized.toFixed(1)}/5
    </span>
  )
}
