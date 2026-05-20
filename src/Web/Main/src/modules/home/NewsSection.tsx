import { useQuery } from '@tanstack/react-query'
import { fetchLatestNews } from '@/api/newsApi'
import { formatRelativeTime } from '@/shared/lib/format-time'
import { getSafeExternalUrl } from '@/shared/lib/safe-external-url'

function NewsSectionSkeleton() {
  return (
    <div aria-label="Noticias recientes" className="rounded-xl border border-border bg-surface-elevated overflow-hidden h-full">
      <div className="px-4 pt-4 pb-2 flex items-center gap-3">
        <h3 className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Noticias</h3>
        <div className="flex-1 h-px bg-border" />
      </div>
      <div className="divide-y divide-border animate-pulse">
        {Array.from({ length: 4 }).map((_, i) => (
          <div key={i} className="px-4 py-3 space-y-2">
            <div className="h-3 w-full bg-muted rounded" />
            <div className="h-3 w-4/5 bg-muted rounded" />
            <div className="h-2.5 w-20 bg-muted rounded" />
          </div>
        ))}
      </div>
      <p className="px-4 py-2 text-xs text-muted-foreground/60 border-t border-border">
        Cargando noticias recientes
      </p>
    </div>
  )
}

export function NewsSection() {
  const { data: articles = [], isLoading, isError } = useQuery({
    queryKey: ['news', 'latest'],
    queryFn: fetchLatestNews,
    staleTime: 5 * 60_000,
  })

  if (isLoading) return <NewsSectionSkeleton />

  return (
    <div aria-label="Noticias recientes" className="rounded-xl border border-border bg-surface-elevated overflow-hidden h-full">
      <div className="px-4 pt-4 pb-2 flex items-center gap-3">
        <h3 className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Noticias</h3>
        <div className="flex-1 h-px bg-border" />
      </div>

      {isError ? (
        <div className="px-4 py-6 text-sm text-muted-foreground">
          No se pudieron cargar las noticias.
        </div>
      ) : articles.length === 0 ? (
        <div className="px-4 py-6 text-sm text-muted-foreground">
          Sin noticias disponibles.
        </div>
      ) : (
        <div className="divide-y divide-border">
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
                    <h4 className="text-sm font-medium leading-5">{article.title}</h4>
                  </a>
                ) : (
                  <h4 className="text-sm font-medium leading-5">{article.title}</h4>
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
      )}
    </div>
  )
}
