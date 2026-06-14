import { useEffect, useId, useState } from 'react'
import { ChevronDown, MessageSquareQuote } from 'lucide-react'
import ReactMarkdown from 'react-markdown'
import remarkGfm from 'remark-gfm'
import { cn } from '@/shared/lib/utils'
import type { FaqItemDto } from '@/api/faqApi'

interface Props {
  items: FaqItemDto[]
  kicker?: string
  title?: string
  description?: string
  className?: string
}

export function FaqAccordion({
  items,
  kicker = 'FAQ / GEO',
  title = 'Preguntas frecuentes',
  description = 'Las mismas respuestas visibles en pantalla se publican también como FAQPage JSON-LD.',
  className,
}: Props) {
  const baseId = useId()
  const [openId, setOpenId] = useState<string | null>(items[0]?.id ?? null)

  useEffect(() => {
    if (items.length === 0) {
      setOpenId(null)
      return
    }

    setOpenId((current) => (current && items.some((item) => item.id === current) ? current : items[0].id))
  }, [items])

  if (items.length === 0) {
    return null
  }

  return (
    <section
      className={cn(
        'rounded-[2rem] border border-slate-200 bg-[linear-gradient(180deg,rgba(255,255,255,0.94),rgba(247,250,255,0.92))] px-5 py-6 shadow-[0_20px_50px_rgba(15,23,42,0.08)] md:px-8',
        className,
      )}
      aria-labelledby={`${baseId}-title`}
    >
      <div className="grid gap-4 lg:grid-cols-[minmax(0,1.15fr)_20rem]">
        <div>
          <p className="text-xs font-semibold uppercase tracking-[0.3em] text-primary/80">{kicker}</p>
          <h2 id={`${baseId}-title`} className="mt-3 font-playfair text-3xl font-bold tracking-tight text-foreground md:text-4xl">
            {title}
          </h2>
          <p className="mt-3 max-w-2xl text-sm leading-7 text-muted-foreground md:text-base">
            {description}
          </p>
        </div>

        <div className="rounded-[1.5rem] border border-slate-200 bg-slate-950 px-5 py-4 text-slate-100 shadow-[0_20px_40px_rgba(15,23,42,0.16)]">
          <div className="flex items-center gap-3">
            <span className="flex h-11 w-11 items-center justify-center rounded-2xl bg-white/10">
              <MessageSquareQuote className="h-5 w-5 text-teal-200" />
            </span>
            <div>
              <p className="text-xs font-semibold uppercase tracking-[0.24em] text-teal-200">Contenido visible</p>
              <p className="mt-1 text-sm leading-6 text-slate-300">
                {items.length} respuestas alineadas con la metadata estructurada.
              </p>
            </div>
          </div>
        </div>
      </div>

      <div className="mt-6 space-y-3">
        {items.map((item, index) => {
          const expanded = openId === item.id
          const panelId = `${baseId}-panel-${item.id}`
          const buttonId = `${baseId}-button-${item.id}`

          return (
            <article
              key={item.id}
              className={cn(
                'overflow-hidden rounded-[1.5rem] border transition-colors',
                expanded
                  ? 'border-primary/30 bg-primary/5'
                  : 'border-slate-200 bg-white hover:border-slate-300 hover:bg-slate-50/80',
              )}
            >
              <button
                id={buttonId}
                type="button"
                aria-expanded={expanded}
                aria-controls={panelId}
                onClick={() => setOpenId(expanded ? null : item.id)}
                className="flex w-full items-start justify-between gap-4 px-5 py-4 text-left"
              >
                <span className="flex min-w-0 items-start gap-4">
                  <span className="mt-0.5 inline-flex h-9 w-9 shrink-0 items-center justify-center rounded-2xl bg-slate-900 text-xs font-semibold text-white">
                    {String(index + 1).padStart(2, '0')}
                  </span>
                  <span className="min-w-0">
                    <span className="block text-xs font-semibold uppercase tracking-[0.22em] text-primary/70">
                      Pregunta
                    </span>
                    <span className="mt-1 block text-base font-semibold leading-7 text-foreground md:text-lg">
                      {item.question}
                    </span>
                  </span>
                </span>

                <ChevronDown
                  className={cn(
                    'mt-1 h-5 w-5 shrink-0 text-muted-foreground transition-transform duration-300',
                    expanded && 'rotate-180 text-primary',
                  )}
                />
              </button>

              <div
                id={panelId}
                role="region"
                aria-labelledby={buttonId}
                className={cn(
                  'grid transition-[grid-template-rows,opacity] duration-300',
                  expanded ? 'grid-rows-[1fr] opacity-100' : 'grid-rows-[0fr] opacity-0',
                )}
              >
                <div className="overflow-hidden px-5 pb-5">
                  <div className="prose prose-sm max-w-none text-sm leading-7 text-slate-700 prose-headings:font-playfair prose-a:text-primary">
                    <ReactMarkdown remarkPlugins={[remarkGfm]}>{item.answer}</ReactMarkdown>
                  </div>
                </div>
              </div>
            </article>
          )
        })}
      </div>
    </section>
  )
}
