import { useEffect, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { fetchAiPrompt, updateAiPrompt } from '@/api/aiPromptsApi'

type PromptContentType = 'news' | 'kpi_extraction'

export function AiPromptsPage() {
  const queryClient = useQueryClient()
  const [templates, setTemplates] = useState<Record<PromptContentType, string>>({
    news: '',
    kpi_extraction: '',
  })

  const newsQuery = useQuery({
    queryKey: ['ai-prompt', 'news'],
    queryFn: () => fetchAiPrompt('news'),
    retry: false,
  })

  const kpiQuery = useQuery({
    queryKey: ['ai-prompt', 'kpi_extraction'],
    queryFn: () => fetchAiPrompt('kpi_extraction'),
    retry: false,
  })

  useEffect(() => {
    if (newsQuery.data) {
      setTemplates((current) => ({ ...current, news: newsQuery.data.promptTemplate }))
    }
  }, [newsQuery.data])

  useEffect(() => {
    if (kpiQuery.data) {
      setTemplates((current) => ({ ...current, kpi_extraction: kpiQuery.data.promptTemplate }))
    }
  }, [kpiQuery.data])

  const newsMutation = useMutation({
    mutationFn: (promptTemplate: string) => updateAiPrompt('news', promptTemplate),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['ai-prompt', 'news'] })
    },
  })

  const kpiMutation = useMutation({
    mutationFn: (promptTemplate: string) => updateAiPrompt('kpi_extraction', promptTemplate),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['ai-prompt', 'kpi_extraction'] })
    },
  })

  const sections = [
    { contentType: 'news' as const, label: 'Prompt de noticias', placeholders: '{title}, {snippet_section}, {body_section}', query: newsQuery, mutation: newsMutation },
    { contentType: 'kpi_extraction' as const, label: 'Prompt de extracción KPI', placeholders: '{markdown_content}', query: kpiQuery, mutation: kpiMutation },
  ]

  return (
    <section className="rounded-2xl border border-border/80 bg-white/90 p-6 shadow-sm">
      <div>
        <p className="text-xs font-semibold uppercase tracking-[0.24em] text-teal-700">Prompt Studio</p>
        <h2 className="mt-1 text-lg font-semibold tracking-tight">Prompts de IA</h2>
        <p className="mt-2 max-w-3xl text-sm text-muted-foreground">
          Edita los templates que usan Gemini y DeepSeek para noticias y documentos. Los cambios aplican al siguiente resumen generado.
        </p>
      </div>

      <div className="mt-6 grid gap-5 xl:grid-cols-2">
        {sections.map(({ contentType, label, placeholders, query, mutation }) => (
          <section className={`rounded-2xl border border-border/80 bg-slate-50/70 p-5${contentType === 'kpi_extraction' ? ' xl:col-span-2' : ''}`} key={contentType}>
            <div className="flex items-center justify-between gap-3">
              <div>
                <h3 className="text-base font-semibold tracking-tight">{label}</h3>
                <p className="mt-1 text-sm text-muted-foreground">
                  Placeholder requerido: <code className="rounded bg-slate-100 px-1 py-0.5 text-xs">{placeholders}</code>
                </p>
              </div>
              <span className="rounded-full bg-teal-100 px-3 py-1 text-xs font-semibold uppercase tracking-[0.18em] text-teal-800">
                {contentType}
              </span>
            </div>

            {query.isLoading ? (
              <p className="mt-4 text-sm text-muted-foreground">Cargando template...</p>
            ) : query.isError ? (
              <p className="mt-4 text-sm text-destructive">{query.error.message}</p>
            ) : (
              <>
                <textarea
                  aria-label={label}
                  className={`mt-4 w-full rounded-2xl border border-border bg-white px-4 py-3 font-mono text-sm leading-7 outline-none transition focus:border-teal-600 ${contentType === 'kpi_extraction' ? 'h-[48rem]' : 'h-[24rem]'}`}
                  onChange={(event) =>
                    setTemplates((current) => ({
                      ...current,
                      [contentType]: event.target.value,
                    }))
                  }
                  value={templates[contentType]}
                />

                <div className="mt-4 flex flex-wrap items-center gap-3">
                  <button
                    className="rounded-xl bg-teal-700 px-5 py-2.5 text-sm font-medium text-white transition hover:bg-teal-800 disabled:cursor-not-allowed disabled:bg-teal-400"
                    disabled={mutation.isPending}
                    onClick={() => mutation.mutate(templates[contentType])}
                    type="button"
                  >
                    {mutation.isPending ? 'Guardando...' : 'Guardar'}
                  </button>

                  <p className="text-xs text-muted-foreground">
                    Último cambio:{' '}
                    {query.data
                      ? `${new Date(query.data.updatedAt).toLocaleString('es-MX', {
                          dateStyle: 'medium',
                          timeStyle: 'short',
                        })} · ${query.data.updatedBy}`
                      : 'sin datos'}
                  </p>
                </div>

                {mutation.isError ? (
                  <p className="mt-3 text-sm text-destructive">{mutation.error.message}</p>
                ) : null}

                {mutation.isSuccess ? (
                  <p className="mt-3 text-sm text-teal-700">Template actualizado correctamente.</p>
                ) : null}
              </>
            )}
          </section>
        ))}
      </div>
    </section>
  )
}
