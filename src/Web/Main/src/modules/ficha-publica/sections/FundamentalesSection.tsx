import { FIBRA_PAGE_LOADING_COUNTS, FUNDAMENTALES_SECTION_LOADING_SHELL } from '../cwv-layout'
import { FundamentalKpiTable } from './FundamentalesContent'
import type { FundamentalesPublicData } from './fundamentales'

interface Props {
  data?: FundamentalesPublicData
  isLoading?: boolean
}

export function FundamentalesSectionSkeleton() {
  return (
    <div aria-busy="true" className="space-y-4">
      <div className="rounded-xl border border-border bg-surface-elevated overflow-hidden">
        <div className="flex items-center justify-between gap-2 border-b border-border px-4 py-3">
          <div className={`h-5 animate-pulse rounded bg-muted/70 ${FUNDAMENTALES_SECTION_LOADING_SHELL.headerTitleWidthClass}`} />
          <div className={`h-4 animate-pulse rounded bg-muted/70 ${FUNDAMENTALES_SECTION_LOADING_SHELL.headerMetaWidthClass}`} />
        </div>

        <div className="divide-y divide-border">
          {Array.from({ length: FIBRA_PAGE_LOADING_COUNTS.fundamentalsRows }).map((_, index) => (
            <div
              key={index}
              className="grid gap-3 px-4 py-2.5 sm:grid-cols-[minmax(0,1.2fr)_7rem_minmax(0,1fr)] sm:items-center"
            >
              <div className={`h-5 animate-pulse rounded bg-muted/70 ${FUNDAMENTALES_SECTION_LOADING_SHELL.rowLabelWidthClass}`} />
              <div className={`ml-auto h-5 animate-pulse rounded bg-muted/70 ${FUNDAMENTALES_SECTION_LOADING_SHELL.rowValueWidthClass}`} />
              <div className={`h-4 animate-pulse rounded bg-muted/70 ${FUNDAMENTALES_SECTION_LOADING_SHELL.rowNoteWidthClass}`} />
            </div>
          ))}
        </div>
      </div>
    </div>
  )
}

export function FundamentalesSection({ data, isLoading = false }: Props) {
  if (isLoading) {
    return <FundamentalesSectionSkeleton />
  }

  return <FundamentalKpiTable data={data} />
}
