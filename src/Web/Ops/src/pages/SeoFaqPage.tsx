import { useEffect, useMemo, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Check, DatabaseZap, PencilLine, RefreshCw, Sparkles } from 'lucide-react'
import ReactMarkdown from 'react-markdown'
import { createFaqItem, deactivateFaqItem, fetchFaqItems, seedFaqItems, updateFaqItem, type FaqItemDto, type UpsertFaqItemRequest } from '@/api/seoFaqApi'

const PAGE_TARGETS = [
  { id: 'home', label: 'Home', pageType: 'Home', entityKey: '/' },
  { id: 'editorial', label: 'Conoce las FIBRAs', pageType: 'StaticPage', entityKey: '/conoce-las-fibras' },
  { id: 'fundamentales', label: 'Fundamentales', pageType: 'StaticPage', entityKey: '/fundamentales' },
  { id: 'fibra', label: 'FIBRA específica', pageType: 'Fibra', entityKey: 'FUNO11' },
  { id: 'news', label: 'Noticia', pageType: 'News', entityKey: 'funo11-reporta-resultados-del-2t25' },
] as const

type DraftState = UpsertFaqItemRequest

function buildDefaultDraft(pageType: string, entityKey: string, items?: FaqItemDto[]): DraftState {
  const nextOrder = Math.max(0, ...(items ?? []).map((item) => Number(item.order))) + 1

  return {
    pageType,
    entityKey,
    question: '',
    answer: '',
    order: Number(nextOrder),
    isActive: true,
  }
}

export function SeoFaqPage() {
  const queryClient = useQueryClient()
  const [targetId, setTargetId] = useState<(typeof PAGE_TARGETS)[number]['id']>('editorial')
  const [editingId, setEditingId] = useState<string | null>(null)
  const [draft, setDraft] = useState<DraftState>(() => buildDefaultDraft('StaticPage', '/conoce-las-fibras'))
  const [feedback, setFeedback] = useState<string | null>(null)

  const target = useMemo(
    () => PAGE_TARGETS.find((item) => item.id === targetId) ?? PAGE_TARGETS[0],
    [targetId],
  )

  const faqQuery = useQuery({
    queryKey: ['ops', 'seo', 'faq', target.pageType, target.entityKey],
    queryFn: () => fetchFaqItems(target.pageType, target.entityKey),
    retry: false,
    staleTime: 5 * 60_000,
  })

  useEffect(() => {
    if (editingId) {
      return
    }

    setDraft(buildDefaultDraft(target.pageType, target.entityKey, faqQuery.data))
  }, [editingId, faqQuery.data, target.entityKey, target.pageType])

  const seedMutation = useMutation({
    mutationFn: seedFaqItems,
    onSuccess: async (result) => {
      setFeedback(`Seed listo: ${result.createdCount} creadas, ${result.skippedCount} omitidas.`)
      await queryClient.invalidateQueries({ queryKey: ['ops', 'seo', 'faq'] })
    },
  })

  const saveMutation = useMutation({
    mutationFn: async (payload: DraftState & { id?: string }) => {
      if (payload.id) {
        return updateFaqItem(payload.id, payload)
      }

      return createFaqItem(payload)
    },
    onSuccess: async () => {
      setFeedback('FAQ guardada.')
      setEditingId(null)
      await queryClient.invalidateQueries({ queryKey: ['ops', 'seo', 'faq'] })
    },
  })

  const deactivateMutation = useMutation({
    mutationFn: deactivateFaqItem,
    onSuccess: async () => {
      setFeedback('FAQ desactivada.')
      await queryClient.invalidateQueries({ queryKey: ['ops', 'seo', 'faq'] })
    },
  })

  const items = faqQuery.data ?? []
  const selectedItem = editingId ? items.find((item) => item.id === editingId) ?? null : null

  function handleSelectTarget(nextId: (typeof PAGE_TARGETS)[number]['id']) {
    setTargetId(nextId)
    setEditingId(null)
    setFeedback(null)
  }

  function handleEdit(item: FaqItemDto) {
    setEditingId(item.id)
    setDraft({
      pageType: item.pageType,
      entityKey: item.entityKey,
      question: item.question,
      answer: item.answer,
      order: item.order,
      isActive: item.isActive,
    })
    setFeedback(null)
  }

  function handleReset() {
    setEditingId(null)
    setFeedback(null)
    setDraft(buildDefaultDraft(target.pageType, target.entityKey, faqQuery.data))
  }

  function handleSubmit() {
    if (!draft.pageType.trim() || !draft.entityKey.trim() || !draft.question.trim() || !draft.answer.trim()) {
      setFeedback('Completa pageType, entityKey, pregunta y respuesta antes de guardar.')
      return
    }

    if (Number(draft.order) < 1) {
      setFeedback('El orden debe ser mayor o igual a 1.')
      return
    }

    saveMutation.mutate({
      id: editingId ?? undefined,
      ...draft,
      question: draft.question.trim(),
      answer: draft.answer.trim(),
      entityKey: draft.entityKey.trim(),
    } satisfies DraftState & { id?: string })
  }

  return (
    <div className="space-y-6">
      <section className="rounded-[1.75rem] border border-slate-200 bg-white/90 p-6 shadow-sm">
        <div className="flex flex-col gap-3 lg:flex-row lg:items-start lg:justify-between">
          <div>
            <p className="text-xs font-semibold uppercase tracking-[0.28em] text-teal-700">Ops / SEO FAQ</p>
            <h1 className="mt-1 text-2xl font-semibold tracking-tight text-slate-950">
              Administrar preguntas frecuentes por página
            </h1>
            <p className="mt-2 max-w-3xl text-sm leading-6 text-slate-500">
              El contenido visible y el FAQPage JSON-LD deben coincidir. Usa este módulo para crear, reordenar y desactivar respuestas por página.
            </p>
          </div>

          <button
            type="button"
            className="inline-flex items-center gap-2 rounded-xl bg-teal-700 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-teal-800 disabled:cursor-not-allowed disabled:opacity-60"
            onClick={() => seedMutation.mutate()}
            disabled={seedMutation.isPending}
          >
            <DatabaseZap className="h-4 w-4" />
            {seedMutation.isPending ? 'Sembrando...' : 'Cargar seed inicial'}
          </button>
        </div>

        <div className="mt-5 grid gap-4 lg:grid-cols-[minmax(0,1fr)_22rem]">
          <label className="space-y-1.5">
            <span className="text-xs font-semibold uppercase tracking-wide text-slate-500">Página objetivo</span>
            <select
              value={targetId}
              onChange={(event) => handleSelectTarget(event.target.value as (typeof PAGE_TARGETS)[number]['id'])}
              className="flex h-11 w-full rounded-xl border border-slate-200 bg-white px-3 text-sm text-slate-900 outline-none transition focus:border-teal-600"
            >
              {PAGE_TARGETS.map((item) => (
                <option key={item.id} value={item.id}>
                  {item.label}
                </option>
              ))}
            </select>
          </label>

          <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-600">
            <p className="font-semibold text-slate-900">Destino actual</p>
            <p className="mt-1 font-mono text-xs text-slate-500">
              {target.pageType} · {target.entityKey}
            </p>
            <p className="mt-2 leading-6">
              Esta combinación es la misma que usa el middleware para emitir JSON-LD y el componente público para pintar el acordeón.
            </p>
          </div>
        </div>

        {feedback ? (
          <p className="mt-4 rounded-xl border border-teal-200 bg-teal-50 px-4 py-3 text-sm text-teal-800">
            {feedback}
          </p>
        ) : null}
      </section>

      <div className="grid gap-6 xl:grid-cols-[minmax(0,1fr)_24rem]">
        <section className="rounded-[1.75rem] border border-slate-200 bg-white/90 p-6 shadow-sm">
          <div className="flex items-center justify-between gap-3 border-b border-slate-200 pb-4">
            <div>
              <h2 className="text-lg font-semibold tracking-tight text-slate-950">
                {editingId ? 'Editar FAQ' : 'Crear FAQ'}
              </h2>
              <p className="mt-1 text-sm text-slate-500">
                {editingId ? `Editando ${selectedItem?.question ?? 'selección actual'}` : 'Define pregunta, respuesta y orden.'}
              </p>
            </div>

            {selectedItem ? (
              <span className={selectedItem.isActive ? 'rounded-full bg-emerald-50 px-3 py-1 text-xs font-semibold text-emerald-700' : 'rounded-full bg-slate-100 px-3 py-1 text-xs font-semibold text-slate-500'}>
                {selectedItem.isActive ? 'Activa' : 'Inactiva'}
              </span>
            ) : null}
          </div>

          <div className="mt-5 grid gap-4">
            <div className="grid gap-4 md:grid-cols-2">
              <label className="space-y-1.5">
                <span className="text-xs font-semibold uppercase tracking-wide text-slate-500">PageType</span>
                <select
                  value={draft.pageType}
                  onChange={(event) => setDraft((current) => ({ ...current, pageType: event.target.value }))}
                  className="flex h-11 w-full rounded-xl border border-slate-200 bg-white px-3 text-sm text-slate-900 outline-none transition focus:border-teal-600"
                >
                  {['Home', 'StaticPage', 'Fibra', 'News', 'Blog'].map((item) => (
                    <option key={item} value={item}>
                      {item}
                    </option>
                  ))}
                </select>
              </label>

              <label className="space-y-1.5">
                <span className="text-xs font-semibold uppercase tracking-wide text-slate-500">EntityKey</span>
                <input
                  value={draft.entityKey}
                  onChange={(event) => setDraft((current) => ({ ...current, entityKey: event.target.value }))}
                  className="flex h-11 w-full rounded-xl border border-slate-200 bg-white px-3 text-sm text-slate-900 outline-none transition focus:border-teal-600"
                  placeholder="/fundamentales"
                />
              </label>
            </div>

            <label className="space-y-1.5">
              <span className="text-xs font-semibold uppercase tracking-wide text-slate-500">Pregunta</span>
              <input
                value={draft.question}
                onChange={(event) => setDraft((current) => ({ ...current, question: event.target.value }))}
                className="flex h-11 w-full rounded-xl border border-slate-200 bg-white px-3 text-sm text-slate-900 outline-none transition focus:border-teal-600"
                placeholder="¿Qué es Cap Rate?"
              />
            </label>

            <label className="space-y-1.5">
              <span className="text-xs font-semibold uppercase tracking-wide text-slate-500">Respuesta</span>
              <textarea
                value={draft.answer}
                onChange={(event) => setDraft((current) => ({ ...current, answer: event.target.value }))}
                className="min-h-[16rem] w-full rounded-2xl border border-slate-200 bg-white px-4 py-3 font-mono text-sm leading-6 text-slate-900 outline-none transition focus:border-teal-600"
                placeholder="Texto en markdown o texto plano."
              />
            </label>

            <div className="grid gap-4 md:grid-cols-2">
              <label className="space-y-1.5">
                <span className="text-xs font-semibold uppercase tracking-wide text-slate-500">Orden</span>
                <input
                  type="number"
                  min={1}
                  value={draft.order}
                  onChange={(event) => setDraft((current) => ({ ...current, order: Number(event.target.value) }))}
                  className="flex h-11 w-full rounded-xl border border-slate-200 bg-white px-3 text-sm text-slate-900 outline-none transition focus:border-teal-600"
                />
              </label>

              <label className="flex items-end gap-3 rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3">
                <input
                  type="checkbox"
                  checked={draft.isActive}
                  onChange={(event) => setDraft((current) => ({ ...current, isActive: event.target.checked }))}
                  className="h-4 w-4 rounded border-slate-300 text-teal-700 focus:ring-teal-600"
                />
                <span className="text-sm text-slate-700">FAQ activa</span>
              </label>
            </div>

            <div className="flex flex-wrap items-center justify-between gap-3">
              <p className="text-sm text-slate-500">
                {editingId ? 'Reordena o ajusta el texto y guarda para actualizar la fila activa.' : 'Crear una nueva FAQ no borra el resto de respuestas de la página.'}
              </p>

              <div className="flex gap-2">
                {editingId ? (
                  <button
                    type="button"
                    className="rounded-xl border border-slate-200 bg-white px-4 py-2.5 text-sm font-semibold text-slate-700 transition hover:border-slate-300 hover:bg-slate-50"
                    onClick={handleReset}
                  >
                    Cancelar
                  </button>
                ) : null}

                <button
                  type="button"
                  className="inline-flex items-center gap-2 rounded-xl bg-teal-700 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-teal-800 disabled:cursor-not-allowed disabled:opacity-60"
                  onClick={handleSubmit}
                  disabled={saveMutation.isPending}
                >
                  {editingId ? <PencilLine className="h-4 w-4" /> : <Sparkles className="h-4 w-4" />}
                  {saveMutation.isPending ? 'Guardando...' : editingId ? 'Actualizar FAQ' : 'Crear FAQ'}
                </button>
              </div>
            </div>
          </div>
        </section>

        <aside className="space-y-4">
          <section className="rounded-[1.75rem] border border-slate-200 bg-white/90 p-6 shadow-sm">
            <div className="flex items-center gap-3">
              <span className="flex h-11 w-11 items-center justify-center rounded-2xl bg-teal-50 text-teal-700">
                <RefreshCw className="h-5 w-5" />
              </span>
              <div>
                <h2 className="text-lg font-semibold tracking-tight text-slate-950">Estado de carga</h2>
                <p className="text-sm text-slate-500">Consulta el listado de FAQ para el destino activo.</p>
              </div>
            </div>

            {faqQuery.isLoading ? <p className="mt-4 text-sm text-slate-500">Cargando FAQ...</p> : null}
            {faqQuery.isError ? <p className="mt-4 rounded-xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">{faqQuery.error.message}</p> : null}
            {faqQuery.isSuccess ? (
              <p className="mt-4 text-sm text-slate-600">
                {items.length} elementos cargados para {target.label}.
              </p>
            ) : null}
          </section>

          <section className="rounded-[1.75rem] border border-slate-200 bg-white/90 p-6 shadow-sm">
            <div className="flex items-center gap-3">
              <span className="flex h-11 w-11 items-center justify-center rounded-2xl bg-slate-900 text-white">
                <Check className="h-5 w-5" />
              </span>
              <div>
                <h2 className="text-lg font-semibold tracking-tight text-slate-950">Vista previa activa</h2>
                <p className="text-sm text-slate-500">Lo que aquí ves es lo que el usuario leerá en público.</p>
              </div>
            </div>

            {items.length === 0 ? (
              <p className="mt-4 text-sm text-slate-500">No hay FAQ cargadas para este destino.</p>
            ) : (
              <div className="mt-4 space-y-3">
                {items.map((item) => (
                  <article key={item.id} className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3">
                    <div className="flex items-start justify-between gap-3">
                      <div>
                        <p className="text-[11px] font-semibold uppercase tracking-[0.24em] text-teal-700">
                          #{item.order} · {item.isActive ? 'Activa' : 'Inactiva'}
                        </p>
                        <p className="mt-1 text-sm font-semibold text-slate-950">{item.question}</p>
                      </div>

                      <div className="flex gap-2">
                        <button
                          type="button"
                          className="rounded-lg border border-slate-200 bg-white px-2 py-1 text-xs font-semibold text-slate-700 transition hover:border-teal-300 hover:text-teal-700"
                          onClick={() => handleEdit(item)}
                        >
                          Editar
                        </button>

                        {item.isActive ? (
                          <button
                            type="button"
                            className="rounded-lg border border-rose-200 bg-white px-2 py-1 text-xs font-semibold text-rose-700 transition hover:border-rose-300 hover:bg-rose-50"
                            onClick={() => deactivateMutation.mutate(item.id)}
                            disabled={deactivateMutation.isPending}
                          >
                            Desactivar
                          </button>
                        ) : null}
                      </div>
                    </div>

                    <div className="prose prose-sm mt-3 max-w-none text-sm leading-6 text-slate-700">
                      <ReactMarkdown>{item.answer}</ReactMarkdown>
                    </div>
                  </article>
                ))}
              </div>
            )}
          </section>
        </aside>
      </div>
    </div>
  )
}
