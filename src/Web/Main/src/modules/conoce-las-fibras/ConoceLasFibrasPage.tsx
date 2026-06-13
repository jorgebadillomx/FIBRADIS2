import { useState } from 'react'
import { usePageTitle } from '@/shared/hooks/usePageTitle'
import { useQuery } from '@tanstack/react-query'
import ReactMarkdown from 'react-markdown'
import { fetchEditorialPages } from '@/api/editorialApi'
import { cn } from '@/shared/lib/utils'

const PAGE_TITLE = 'Conoce las FIBRAs — Fibras Inmobiliarias'
const PAGE_DESCRIPTION =
  'Guía completa sobre FIBRAs inmobiliarias mexicanas: qué son, cómo funcionan, historia, por qué invertir y su régimen fiscal.'

const TAB_LABELS: Record<string, string> = {
  'que-son-las-fibras': '¿Qué son?',
  historia: 'Historia',
  'como-se-estructuran': '¿Cómo se estructuran?',
  'por-que-invertir': 'Por qué invertir',
  'regimen-fiscal': 'Régimen fiscal',
}

export function ConoceLasFibrasPage() {
  const [activeSlug, setActiveSlug] = useState<string>('')
  const pagesQuery = useQuery({
    queryKey: ['editorial-pages'],
    queryFn: fetchEditorialPages,
    staleTime: 60 * 60_000,
  })

  const activePage =
    pagesQuery.data?.find((page) => page.slug === activeSlug) ?? pagesQuery.data?.[0] ?? null

  usePageTitle(PAGE_TITLE, PAGE_DESCRIPTION)

  return (
    <>
      <section className="bg-[radial-gradient(circle_at_top_left,rgba(38,103,255,0.12),transparent_34%),linear-gradient(180deg,rgba(10,14,26,0.02),transparent_26%)]">
        <div className="container mx-auto px-4 py-10 md:py-14">
          <div className="max-w-3xl">
            <p className="text-xs font-semibold uppercase tracking-[0.28em] text-primary/80">Aprende antes de invertir</p>
            <h1 className="mt-3 font-playfair text-4xl font-bold tracking-tight text-foreground md:text-5xl">
              Conoce las FIBRAs
            </h1>
            <p className="mt-4 text-base leading-8 text-muted-foreground md:text-lg">
              Una guía editorial para entender qué son las FIBRAs, cómo se estructuran y qué variables importan
              antes de analizarlas en el mercado.
            </p>
          </div>

          {pagesQuery.isLoading ? <EditorialSkeleton /> : null}

          {pagesQuery.isError ? (
            <div className="mt-8 rounded-[1.5rem] border border-rose-200 bg-white px-5 py-4 text-sm text-rose-700 shadow-sm">
              {pagesQuery.error.message}
            </div>
          ) : null}

          {pagesQuery.isSuccess && pagesQuery.data.length === 0 ? (
            <div className="mt-8 rounded-[1.5rem] border border-slate-200 bg-white px-5 py-4 text-sm text-slate-600 shadow-sm">
              No hay contenido editorial disponible.
            </div>
          ) : null}

          {activePage ? (
            <div className="mt-8 grid gap-6 lg:grid-cols-[18rem_minmax(0,1fr)]">
              <nav
                aria-label="Secciones educativas"
                className="h-fit rounded-[1.75rem] border border-border bg-surface-elevated p-3 shadow-[0_18px_50px_rgba(15,23,42,0.06)]"
              >
                <div className="flex gap-2 overflow-x-auto lg:flex-col">
                  {pagesQuery.data?.map((page) => (
                    <button
                      key={page.slug}
                      type="button"
                      onClick={() => setActiveSlug(page.slug)}
                      className={cn(
                        'min-w-max rounded-2xl border px-4 py-3 text-left text-sm font-medium transition lg:min-w-0',
                        page.slug === activePage?.slug
                          ? 'border-primary bg-primary text-primary-foreground shadow-sm'
                          : 'border-transparent bg-muted text-muted-foreground hover:border-border hover:bg-background',
                      )}
                    >
                      {TAB_LABELS[page.slug] ?? page.title}
                    </button>
                  ))}
                </div>
              </nav>

              <article className="rounded-[1.75rem] border border-border bg-surface-elevated p-6 shadow-[0_18px_50px_rgba(15,23,42,0.06)] md:p-8">
                <div className="flex flex-col gap-2 border-b border-border pb-5">
                  <p className="text-xs font-semibold uppercase tracking-[0.24em] text-primary/80">
                    {TAB_LABELS[activePage.slug] ?? activePage.title}
                  </p>
                  <h2 className="font-playfair text-3xl font-bold tracking-tight text-foreground">
                    {activePage.title}
                  </h2>
                  <p className="text-sm text-muted-foreground">
                    Actualizado{' '}
                    {new Date(activePage.updatedAt).toLocaleDateString('es-MX', {
                      dateStyle: 'long',
                    })}
                  </p>
                </div>

                <div className="prose prose-sm mt-6 max-w-none text-sm leading-relaxed text-foreground/90">
                  <ReactMarkdown>{activePage.content}</ReactMarkdown>
                </div>
              </article>
            </div>
          ) : null}
        </div>
      </section>
    </>
  )
}

function EditorialSkeleton() {
  return (
    <div className="mt-8 grid gap-6 lg:grid-cols-[18rem_minmax(0,1fr)]">
      <div className="rounded-[1.75rem] border border-border bg-surface-elevated p-3 shadow-[0_18px_50px_rgba(15,23,42,0.06)]">
        <div className="flex gap-2 overflow-x-auto lg:flex-col">
          {Array.from({ length: 5 }).map((_, index) => (
            <div key={index} className="h-11 min-w-28 animate-pulse rounded-2xl bg-muted lg:min-w-0" />
          ))}
        </div>
      </div>

      <div className="rounded-[1.75rem] border border-border bg-surface-elevated p-6 shadow-[0_18px_50px_rgba(15,23,42,0.06)] md:p-8">
        <div className="h-5 w-28 animate-pulse rounded bg-muted" />
        <div className="mt-4 h-9 w-1/2 animate-pulse rounded bg-muted" />
        <div className="mt-10 space-y-3">
          <div className="h-4 w-full animate-pulse rounded bg-muted" />
          <div className="h-4 w-full animate-pulse rounded bg-muted" />
          <div className="h-4 w-11/12 animate-pulse rounded bg-muted" />
          <div className="h-4 w-3/4 animate-pulse rounded bg-muted" />
        </div>
      </div>
    </div>
  )
}
