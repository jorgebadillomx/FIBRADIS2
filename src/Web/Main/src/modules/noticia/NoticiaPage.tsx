import { Link, useParams } from 'react-router'
import { useQuery } from '@tanstack/react-query'
import ReactMarkdown from 'react-markdown'
import { fetchArticleById, fetchRelatedNews, type NewsKeyFigure } from '@/api/newsApi'
import { formatRelativeTime } from '@/shared/lib/format-time'
import { getSafeExternalUrl } from '@/shared/lib/safe-external-url'
import { useFibraSlugMap } from '@/shared/hooks/useFibraSlugMap'

const DEFAULT_TITLE = 'Fibras Inmobiliarias'
const DEFAULT_DESCRIPTION = 'Preview de noticia de Fibras Inmobiliarias.'

const IMPACT_BADGE: Record<string, string> = {
  alto: 'bg-rose-100 text-rose-700 dark:bg-rose-900/30 dark:text-rose-400',
  medio: 'bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400',
  bajo: 'bg-slate-100 text-slate-600 dark:bg-slate-800 dark:text-slate-400',
}

export function NoticiaPage() {
  const { id } = useParams<{ id: string }>()
  const { slugFor } = useFibraSlugMap()
  const { data: article, isLoading, isError } = useQuery({
    queryKey: ['news', 'article', id],
    queryFn: () => fetchArticleById(id!),
    enabled: !!id,
    staleTime: 10 * 60_000,
  })

  const aiAnalysis = article?.aiAnalysis ?? null
  const summaryContent = aiAnalysis?.summaryMarkdown ?? article?.aiSummary ?? article?.snippet ?? null
  const displayTitle = aiAnalysis?.headline ?? article?.title
  const pageTitle = displayTitle ? `${displayTitle} — Fibras Inmobiliarias` : DEFAULT_TITLE
  const pageDescription = summaryContent ? summaryContent.slice(0, 160) : DEFAULT_DESCRIPTION

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

  const safeExternalUrl = getSafeExternalUrl(article.url)

  return (
    <>
      <title>{pageTitle}</title>
      <meta name="description" content={pageDescription} />

      <div className="container mx-auto px-4 py-8">
        <div className="grid grid-cols-1 gap-6 xl:grid-cols-[minmax(0,1fr)_22rem] xl:items-start">
        <div className="min-w-0">

        <h1 className="mb-2 font-playfair text-3xl md:text-4xl font-bold leading-tight text-foreground">
          {displayTitle}
        </h1>

        {/* Metadata: fuente, fecha + badge de impacto */}
        <div className="mb-4 flex flex-wrap items-center gap-2">
          <p className="text-sm text-muted-foreground">
            {article.source} · {formatRelativeTime(article.publishedAt)}
          </p>
          {aiAnalysis && aiAnalysis.impact && aiAnalysis.impact !== 'nulo' ? (
            <span className={`inline-block rounded-full px-2 py-0.5 text-xs font-semibold capitalize ${IMPACT_BADGE[aiAnalysis.impact] ?? 'bg-muted text-muted-foreground'}`}>
              Impacto {aiAnalysis.impact}
            </span>
          ) : null}
        </div>

        {/* Chips de sector */}
        {aiAnalysis && aiAnalysis.sectorTags && aiAnalysis.sectorTags.length > 0 ? (
          <div className="mb-4 flex flex-wrap gap-1.5">
            {aiAnalysis.sectorTags.map((tag) => (
              <span key={tag} className="rounded-full border border-border bg-muted px-2 py-0.5 text-xs text-muted-foreground capitalize">
                {tag}
              </span>
            ))}
          </div>
        ) : null}

        {/* Links a FIBRAs relacionadas */}
        {article.linkedFibras && article.linkedFibras.length > 0 ? (
          <div className="mb-4 flex flex-wrap gap-2">
            {article.linkedFibras.map(fibra => (
              <Link
                key={fibra.id}
                to={`/fibras/${slugFor(fibra.ticker)}#noticias`}
                className="inline-flex items-center gap-1 rounded-full border border-primary/30 bg-primary/5 px-3 py-1 text-xs font-semibold text-primary hover:bg-primary/10 transition-colors"
              >
                {fibra.ticker} — ver noticias →
              </Link>
            ))}
          </div>
        ) : null}

        {/* Análisis estructurado */}
        {aiAnalysis ? (
          <div className="mb-6 space-y-4">
            {/* Hechos clave */}
            {aiAnalysis.keyFacts && aiAnalysis.keyFacts.length > 0 ? (
              <div className="rounded-lg border border-border bg-card p-4">
                <p className="mb-2 text-xs font-semibold uppercase tracking-wider text-primary">Hechos clave</p>
                <ul className="list-disc pl-5 space-y-1">
                  {aiAnalysis.keyFacts.map((fact, i) => (
                    <li key={i} className="text-sm leading-relaxed text-foreground">{fact}</li>
                  ))}
                </ul>
              </div>
            ) : null}

            {/* Cifras clave */}
            {aiAnalysis.keyFigures && aiAnalysis.keyFigures.length > 0 ? (
              <div className="rounded-lg border border-border bg-card p-4">
                <p className="mb-2 text-xs font-semibold uppercase tracking-wider text-primary">Cifras clave</p>
                <div className="grid grid-cols-2 gap-2 sm:grid-cols-3">
                  {sortedFigures(aiAnalysis.keyFigures).map((fig, i) => (
                    <div key={i} className="rounded-md border border-border bg-background p-2">
                      <p className="text-xs text-muted-foreground leading-tight">{fig.label}</p>
                      <p className="mt-0.5 text-sm font-semibold text-foreground">{fig.valueText}</p>
                    </div>
                  ))}
                </div>
              </div>
            ) : null}

            {/* Resumen analítico */}
            {summaryContent ? (
              <div className="rounded-lg border border-border bg-card p-5">
                <p className="mb-2 text-xs font-semibold uppercase tracking-wider text-primary">Análisis IA</p>
                <div className="text-base leading-relaxed text-foreground [&>p]:mb-3 [&>p:last-child]:mb-0 [&>ul]:mb-3 [&>ul]:list-disc [&>ul]:pl-5 [&>ol]:mb-3 [&>ol]:list-decimal [&>ol]:pl-5 [&>li]:mb-1 [&>strong]:font-semibold">
                  <ReactMarkdown>{summaryContent}</ReactMarkdown>
                </div>
              </div>
            ) : null}

            {/* Takeaway del inversionista */}
            {aiAnalysis.investorTakeaway ? (
              <div className="rounded-lg border-l-4 border-primary bg-primary/5 p-4">
                <p className="mb-1 text-xs font-semibold uppercase tracking-wider text-primary">¿Qué significa esto?</p>
                <p className="text-sm leading-relaxed text-foreground">{aiAnalysis.investorTakeaway}</p>
              </div>
            ) : null}

            {/* Baja confianza */}
            {Number(aiAnalysis.confidence) < 0.6 ? (
              <p className="text-xs text-muted-foreground">
                ⚠ Extracción con baja confianza ({Math.round(Number(aiAnalysis.confidence) * 100)}%)
              </p>
            ) : null}

            {/* Notas de extracción */}
            {aiAnalysis.extractionNotes ? (
              <p className="text-xs text-muted-foreground">{aiAnalysis.extractionNotes}</p>
            ) : null}
          </div>
        ) : summaryContent ? (
          /* Fallback: sin análisis estructurado, mostrar summary/snippet */
          <div className="mb-8 rounded-lg border border-border bg-card p-5">
            {article.aiSummary ? (
              <p className="mb-2 text-xs font-semibold uppercase tracking-wider text-primary">Resumen IA</p>
            ) : null}
            {article.aiSummary ? (
              <div className="text-base leading-relaxed text-foreground [&>p]:mb-3 [&>p:last-child]:mb-0 [&>ul]:mb-3 [&>ul]:list-disc [&>ul]:pl-5 [&>ol]:mb-3 [&>ol]:list-decimal [&>ol]:pl-5 [&>li]:mb-1 [&>strong]:font-semibold">
                <ReactMarkdown>{summaryContent}</ReactMarkdown>
              </div>
            ) : (
              <p className="text-base leading-relaxed text-foreground">{summaryContent}</p>
            )}
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

        <aside className="xl:sticky xl:top-28">
          <RelatedNews articleId={article.id} />
        </aside>
        </div>
      </div>
    </>
  )
}

function RelatedNews({ articleId }: { articleId: string }) {
  const { data: articles = [] } = useQuery({
    queryKey: ['news', 'related', articleId],
    queryFn: () => fetchRelatedNews(articleId),
    staleTime: 5 * 60_000,
  })

  if (articles.length === 0) return null

  return (
    <div aria-label="Noticias relacionadas" className="rounded-xl border border-border bg-surface-elevated overflow-hidden">
      <div className="px-4 pt-4 pb-2 flex items-center gap-3">
        <h2 className="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Noticias relacionadas</h2>
        <div className="flex-1 h-px bg-border" />
      </div>
      <div className="divide-y divide-border">
        {articles.map(related => (
          <article key={related.id} className="px-4 py-3">
            <Link to={`/noticias/${related.id}`} className="block">
              <div className="hover:text-brand transition-colors">
                <h3 className="text-sm font-medium leading-5">{related.title}</h3>
              </div>
            </Link>
            <p className="mt-1 text-xs text-muted-foreground">
              {related.source} · {formatRelativeTime(related.publishedAt)}
            </p>
            {related.aiAnalysis?.headline ? (
              <p className="mt-1.5 text-xs text-muted-foreground line-clamp-2 leading-relaxed">
                {related.aiAnalysis.headline}
              </p>
            ) : null}
          </article>
        ))}
      </div>
    </div>
  )
}

function sortedFigures(figures: NewsKeyFigure[]) {
  const order = { alta: 0, media: 1, baja: 2 }
  return [...figures].sort((a, b) => (order[a.importance as keyof typeof order] ?? 3) - (order[b.importance as keyof typeof order] ?? 3))
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
