export interface ReportesSectionData {
  siteUrl: string | null
  investorUrl: string | null
  reportsUrl: string | null
}

export interface ReportLinkItem {
  key: 'siteUrl' | 'investorUrl' | 'reportsUrl'
  label: string
  url: string | null
}

const REPORT_ITEMS: Array<Pick<ReportLinkItem, 'key' | 'label'>> = [
  { key: 'siteUrl', label: 'Sitio web' },
  { key: 'investorUrl', label: 'Relación con inversionistas' },
  { key: 'reportsUrl', label: 'Reportes oficiales' },
]

export function areAllReportLinksMissing(data: ReportesSectionData): boolean {
  return data.siteUrl === null && data.investorUrl === null && data.reportsUrl === null
}

export function getReportLinkItems(data: ReportesSectionData): ReportLinkItem[] {
  return REPORT_ITEMS.map((item) => ({
    ...item,
    url: data[item.key],
  }))
}
