interface Props {
  siteUrl: string | null
  investorUrl: string | null
  reportsUrl: string | null
}

interface LinkItem {
  label: string
  url: string
}

export function ReportesSection({ siteUrl, investorUrl, reportsUrl }: Props) {
  const links: LinkItem[] = [
    ...(siteUrl ? [{ label: 'Sitio web', url: siteUrl }] : []),
    ...(investorUrl ? [{ label: 'Relación con inversionistas', url: investorUrl }] : []),
    ...(reportsUrl ? [{ label: 'Reportes oficiales', url: reportsUrl }] : []),
  ]

  if (links.length === 0) {
    return (
      <div className="text-sm text-muted-foreground">—</div>
    )
  }

  return (
    <ul className="space-y-2">
      {links.map((link) => (
        <li key={link.url}>
          <a
            href={link.url}
            target="_blank"
            rel="noopener noreferrer"
            className="text-sm text-primary hover:underline"
          >
            {link.label} ↗
          </a>
        </li>
      ))}
    </ul>
  )
}
