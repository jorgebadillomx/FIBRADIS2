import { Link } from 'react-router'
import { useQuery } from '@tanstack/react-query'
import { fetchFibraNews } from '@/api/fibraNewsApi'
import { formatRelativeTime } from '@/shared/lib/format-time'

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
    return (
      <div aria-busy="true" className="rounded-lg border border-border bg-surface-elevated divide-y divide-border overflow-hidden">
        {Array.from({ length: 2 }).map((_, index) => (
          <div key={index} className="px-4 py-3">
            <div className="space-y-2">
              <div className="h-4 w-3/4 animate-pulse rounded bg-muted/70" />
              <div className="h-3 w-40 animate-pulse rounded bg-muted/70" />
              <div className="h-3 w-5/6 animate-pulse rounded bg-muted/70" />
            </div>
          </div>
        ))}
      </div>
    )
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
        const headline = article.aiAnalysis?.headline ?? null

        return (
          <article key={article.id} className="px-4 py-3">
            <Link to={`/noticias/${article.slug ?? article.id}`} className="block">
              <div className="hover:text-brand transition-colors">
                <h3 className="text-sm font-medium leading-5">{article.title}</h3>
              </div>
            </Link>
            <p className="mt-1 text-xs text-muted-foreground">
              {article.source} · {formatRelativeTime(article.publishedAt)}
            </p>
            {headline ? (
              <p className="mt-1.5 text-xs text-muted-foreground line-clamp-2 leading-relaxed">
                {headline}
              </p>
            ) : null}
          </article>
        )
      })}
    </div>
  )
}
