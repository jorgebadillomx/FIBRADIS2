import type { KpiKey } from '@/shared/lib/kpi-definitions'

export interface FundamentalItem {
  label: string
  kpiKey: KpiKey
  period: string
  value: number | null
  note?: string
}

export interface FundamentalesData {
  period?: string
  periodsAgo?: number
  capturedAt?: string | null
  summary?: string | null
  summaryMarkdown?: string | null
  investorTakeaway?: string | null
  operationalSignals?: string[]
  financialSignals?: string[]
  riskFlags?: string[]
  items?: FundamentalItem[]
}

export function shouldShowFundamentalesWarning(data?: FundamentalesData): boolean {
  return data?.periodsAgo !== undefined && data.periodsAgo >= 3
}

export function hasFundamentalesItems(data?: FundamentalesData): boolean {
  return (data?.items?.length ?? 0) > 0
}

export function getLatestCapturedAt(rows: Array<{ capturedAt?: string | null }>): Date | null {
  return rows.reduce<Date | null>((latest, row) => {
    if (!row.capturedAt) return latest
    const capturedAt = new Date(row.capturedAt)
    if (Number.isNaN(capturedAt.getTime())) return latest
    return latest === null || capturedAt > latest ? capturedAt : latest
  }, null)
}

export function formatFundamentalValue(value: number | null | undefined): number | string {
  return value !== null && value !== undefined ? value : '—'
}
