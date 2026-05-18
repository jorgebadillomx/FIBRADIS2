interface Props {
  siteUrl: string | null
  investorUrl: string | null
  reportsUrl: string | null
}

const REPORTE_ITEMS = [
  { key: 'siteUrl', label: 'Sitio web' },
  { key: 'investorUrl', label: 'Relación con inversionistas' },
  { key: 'reportsUrl', label: 'Reportes oficiales' },
] as const

export function ReportesSection({ siteUrl, investorUrl, reportsUrl }: Props) {
  const values: Record<typeof REPORTE_ITEMS[number]['key'], string | null> = {
    siteUrl,
    investorUrl,
    reportsUrl,
  }

  const allNull = siteUrl === null && investorUrl === null && reportsUrl === null

  if (allNull) {
    return <div className="text-sm text-muted-foreground">—</div>
  }

  return (
    <ul className="space-y-2">
      {REPORTE_ITEMS.map(({ key, label }) => {
        const url = values[key]
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
