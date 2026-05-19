import {
  areAllReportLinksMissing,
  getReportLinkItems,
  type ReportesSectionData,
} from './reportes'

type Props = ReportesSectionData

export function ReportesSection({ siteUrl, investorUrl, reportsUrl }: Props) {
  const reportes = {
    siteUrl,
    investorUrl,
    reportsUrl,
  }

  if (areAllReportLinksMissing(reportes)) {
    return <div className="text-sm text-muted-foreground">—</div>
  }

  return (
    <ul className="space-y-2">
      {getReportLinkItems(reportes).map(({ key, label, url }) => {
        return (
          <li key={key} className="text-sm">
            {url ? (
              <a
                href={url}
                target="_blank"
                rel="noopener noreferrer"
                className="text-primary hover:underline"
              >
                {label} ↗
              </a>
            ) : (
              <span className="text-muted-foreground">{label}: —</span>
            )}
          </li>
        )
      })}
    </ul>
  )
}
