import { Link } from 'react-router'
import { useQuery } from '@tanstack/react-query'
import { fetchLatestNews } from '@/api/newsApi'
import { formatRelativeTime } from '@/shared/lib/format-time'
import { getArticleImageUrl } from '@/shared/lib/news-image-fallback'

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
            <div className="aspect-video rounded-lg bg-muted" />
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
            const headline = article.aiAnalysis?.headline ?? null

            return (
              <article key={article.id} className="px-4 py-3">
                <Link to={`/noticias/${article.id}`} className="block">
                  {false && getArticleImageUrl(article) ? (
                    <div className="mb-3 aspect-video overflow-hidden rounded-lg bg-muted">
                      <img
                        src={getArticleImageUrl(article)!}
                        alt={`Imagen de la nota: ${article.title}`}
                        className="h-full w-full object-cover"
                        loading="lazy"
                        onError={(event) => {
                          event.currentTarget.onerror = null
                          event.currentTarget.style.display = 'none'
                          event.currentTarget.parentElement!.style.display = 'none'
                        }}
                      />
                    </div>
                  ) : null}
                  <div className="hover:text-brand transition-colors">
                    <h4 className="text-sm font-medium leading-5">{article.title}</h4>
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
      )}
    </div>
  )
}
