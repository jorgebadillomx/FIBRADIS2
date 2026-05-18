export interface FundamentalItem {
  label: string
  period: string
  value: number | null
}

export interface FundamentalesData {
  periodsAgo?: number
  items?: FundamentalItem[]
}

export function shouldShowFundamentalesWarning(data?: FundamentalesData): boolean {
  return data?.periodsAgo !== undefined && data.periodsAgo >= 3
}

export function hasFundamentalesItems(data?: FundamentalesData): boolean {
  return (data?.items?.length ?? 0) > 0
}

export function formatFundamentalValue(value: number | null | undefined): number | string {
  return value !== null && value !== undefined ? value : '—'
}
