import { cn } from '@/shared/lib/utils'

function getToneClass(score: number): string {
  if (score >= 4) return 'border-emerald-200 bg-emerald-50 text-emerald-700'
  if (score >= 3) return 'border-amber-200 bg-amber-50 text-amber-700'
  return 'border-rose-200 bg-rose-50 text-rose-700'
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
