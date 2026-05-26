import { Link, useParams } from 'react-router'
import { useQuery } from '@tanstack/react-query'
import { fetchArticleById } from '@/api/newsApi'
import { formatRelativeTime } from '@/shared/lib/format-time'
import { getArticleImageUrl, SECTOR_IMAGES } from '@/shared/lib/news-image-fallback'
import { getSafeExternalUrl } from '@/shared/lib/safe-external-url'

const DEFAULT_TITLE = 'FIBRADIS'
const DEFAULT_DESCRIPTION = 'Preview de noticia de FIBRADIS.'

export function NoticiaPage() {
  const { id } = useParams<{ id: string }>()
  const { data: article, isLoading, isError } = useQuery({
    queryKey: ['news', 'article', id],
    queryFn: () => fetchArticleById(id!),
    enabled: !!id,
    staleTime: 10 * 60_000,
  })

  const summary = article?.aiSummary ?? article?.snippet ?? null
  const pageTitle = article ? `${article.title} — FIBRADIS` : DEFAULT_TITLE
  const pageDescription = summary ? summary.slice(0, 160) : DEFAULT_DESCRIPTION

  if (isLoading) {
    return (
      <>
        <title>{pageTitle}</title>
        <meta name="description" content={pageDescription} />
        <NoticiaPageSkeleton />
      </>
    )
  }

  if (isError || article === undefined) {
    return (
      <>
        <title>{pageTitle}</title>
        <meta name="description" content={pageDescription} />
        <div className="container mx-auto px-4 py-16 text-center">
          <p className="mb-4 text-muted-foreground">No se pudo cargar la noticia.</p>
          <Link to="/" className="text-brand underline">
            Volver al inicio
          </Link>
        </div>
      </>
    )
  }

  if (article === null) {
    return (
      <>
        <title>{pageTitle}</title>
        <meta name="description" content={pageDescription} />
        <div className="container mx-auto px-4 py-16 text-center">
          <p className="mb-4 text-muted-foreground">Noticia no encontrada.</p>
          <Link to="/" className="text-brand underline">
            Volver al inicio
          </Link>
        </div>
      </>
    )
  }

  const imageUrl = getArticleImageUrl(article)
  const safeExternalUrl = getSafeExternalUrl(article.url)

  return (
    <>
      <title>{pageTitle}</title>
      <meta name="description" content={pageDescription} />

      <div className="container mx-auto max-w-2xl px-4 py-8">
        <div className="mb-6 aspect-video overflow-hidden rounded-xl bg-muted">
          <img
            src={imageUrl}
            alt={article.title}
            className="h-full w-full object-cover"
            loading="eager"
            onError={(event) => {
              event.currentTarget.onerror = null
              event.currentTarget.src = SECTOR_IMAGES.otro
            }}
          />
        </div>

        <h1 className="mb-2 font-playfair text-3xl md:text-4xl font-bold leading-tight text-foreground">
          {article.title}
        </h1>

        <p className="mb-8 text-sm text-muted-foreground">
          {article.source} · {formatRelativeTime(article.publishedAt)}
        </p>

        {summary ? (
          <div className="mb-8 rounded-lg border border-border bg-card p-5">
            {article.aiSummary ? (
              <p className="mb-2 text-xs font-semibold uppercase tracking-wider text-primary">
                Resumen IA
              </p>
            ) : null}
            <p className="text-base leading-relaxed text-foreground">{summary}</p>
          </div>
        ) : null}

        {safeExternalUrl ? (
          <a
            href={safeExternalUrl}
            target="_blank"
            rel="noopener noreferrer"
            className="inline-flex items-center gap-2 rounded-md bg-primary px-5 py-2.5 text-sm font-medium text-primary-foreground transition-opacity hover:opacity-90 cursor-pointer"
          >
            Leer nota completa en {article.source} →
          </a>
        ) : null}
      </div>
    </>
  )
}

function NoticiaPageSkeleton() {
  return (
    <div className="container mx-auto max-w-2xl animate-pulse px-4 py-8">
      <div className="mb-6 aspect-video rounded-xl bg-muted" />
      <div className="mb-2 h-7 w-3/4 rounded bg-muted" />
      <div className="mb-6 h-4 w-1/3 rounded bg-muted" />
      <div className="mb-8 space-y-2">
        <div className="h-4 w-full rounded bg-muted" />
        <div className="h-4 w-full rounded bg-muted" />
        <div className="h-4 w-4/5 rounded bg-muted" />
      </div>
      <div className="h-10 w-40 rounded-lg bg-muted" />
    </div>
  )
}
