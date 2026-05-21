import { Link } from 'react-router'
import { useQuery } from '@tanstack/react-query'
import { fetchFibraNews } from '@/api/fibraNewsApi'
import { formatRelativeTime } from '@/shared/lib/format-time'
import { getArticleImageUrl, getSectorImageUrl } from '@/shared/lib/news-image-fallback'

interface NoticiasSectionProps {
  fibraId: string
  fibra?: {
    logoUrl?: string | null
    sector?: string | null
  } | null
}

export function NoticiasSection({ fibraId, fibra }: NoticiasSectionProps) {
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
        const summary = article.aiSummary ?? article.snippet

        return (
          <article key={article.id} className="px-4 py-3">
            <Link to={`/noticias/${article.id}`} className="block">
              <div className="mb-3 aspect-video overflow-hidden rounded-lg bg-muted">
                <img
                  src={getArticleImageUrl(article, fibra)}
                  alt={`Imagen de la nota: ${article.title}`}
                  className="h-full w-full object-cover"
                  loading="lazy"
                  onError={(event) => {
                    event.currentTarget.onerror = null
                    event.currentTarget.src = getSectorImageUrl(fibra?.sector)
                  }}
                />
              </div>
              <div className="hover:text-brand transition-colors">
                <h3 className="text-sm font-medium leading-5">{article.title}</h3>
              </div>
            </Link>
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
