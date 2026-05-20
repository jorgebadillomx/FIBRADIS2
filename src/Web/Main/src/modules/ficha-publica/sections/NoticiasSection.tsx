import { useQuery } from '@tanstack/react-query'
import { fetchFibraNews } from '@/api/fibraNewsApi'
import { formatRelativeTime } from '@/shared/lib/format-time'
import { getSafeExternalUrl } from '@/shared/lib/safe-external-url'

interface NoticiasSectionProps {
  fibraId: string
}

export function NoticiasSection({ fibraId }: NoticiasSectionProps) {
  const { data: articles = [], isLoading, isError } = useQuery({
    queryKey: ['fibra-news', fibraId],
    queryFn: () => fetchFibraNews(fibraId),
    enabled: !!fibraId,
    staleTime: 5 * 60_000,
  })

  if (isLoading) {
    return <div className="rounded-lg border border-border bg-muted/20 animate-pulse h-32" />
  }

  if (isError) {
    return (
      <div className="rounded-lg border border-border bg-muted/20 flex items-center justify-center h-32">
        <p className="text-sm text-muted-foreground">Error al cargar noticias</p>
      </div>
    )
  }

  if (articles.length === 0) {
    return (
      <div className="rounded-lg border border-border bg-muted/20 flex items-center justify-center h-32">
        <p className="text-sm text-muted-foreground">Sin noticias disponibles</p>
      </div>
    )
  }

  return (
    <div className="rounded-lg border border-border bg-surface-elevated divide-y divide-border overflow-hidden">
      {articles.map(article => {
        const safeUrl = getSafeExternalUrl(article.url)
        const summary = article.aiSummary ?? article.snippet

        return (
          <article key={article.id} className="px-4 py-3">
            {safeUrl ? (
              <a
                href={safeUrl}
                target="_blank"
                rel="noopener noreferrer"
                className="block hover:text-brand transition-colors"
              >
                <h3 className="text-sm font-medium leading-5">{article.title}</h3>
              </a>
            ) : (
              <h3 className="text-sm font-medium leading-5">{article.title}</h3>
            )}
            <p className="mt-1 text-xs text-muted-foreground">
              {article.source} · {formatRelativeTime(article.publishedAt)}
            </p>
            {summary ? (
              <p className="mt-2 text-sm text-muted-foreground line-clamp-2">
                {summary}
              </p>
            ) : null}
          </article>
        )
      })}
    </div>
  )
}
