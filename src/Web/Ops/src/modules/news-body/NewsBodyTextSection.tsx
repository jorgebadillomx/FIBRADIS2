import { Fragment, useEffect, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import ReactMarkdown from 'react-markdown'
import {
  fetchOpsNewsList,
  fetchOpsNewsBody,
  updateNewsBodyText,
  type OpsNewsArticle,
} from '@/api/newsApi'

export function NewsBodyTextSection() {
  const queryClient = useQueryClient()
  const [page, setPage] = useState(1)
  const pageSize = 20
  const [search, setSearch] = useState('')
  const [debouncedSearch, setDebouncedSearch] = useState('')
  const [hasAiSummaryFilter, setHasAiSummaryFilter] = useState<'all' | 'with' | 'without'>('all')
  const [editedFilter, setEditedFilter] = useState<'all' | 'edited' | 'not-edited'>('all')

  const [editingId, setEditingId] = useState<string | null>(null)
  const [editText, setEditText] = useState<string>('')
  const [savedId, setSavedId] = useState<string | null>(null)

  useEffect(() => {
    const next = search.trim()
    const timer = window.setTimeout(() => {
      setDebouncedSearch(next.length >= 2 ? next : '')
    }, 400)

    return () => window.clearTimeout(timer)
  }, [search])

  useEffect(() => {
    setPage(1)
  }, [debouncedSearch, hasAiSummaryFilter, editedFilter])

  const listQuery = useQuery({
    queryKey: ['ops-news-list', page, pageSize, debouncedSearch, hasAiSummaryFilter, editedFilter],
    queryFn: () =>
      fetchOpsNewsList(
        page,
        pageSize,
        debouncedSearch || undefined,
        hasAiSummaryFilter === 'all' ? undefined : hasAiSummaryFilter === 'with',
        editedFilter === 'all' ? undefined : editedFilter === 'edited',
      ),
    retry: false,
  })

  const bodyQuery = useQuery({
    queryKey: ['ops-news-body', editingId],
    queryFn: () => fetchOpsNewsBody(editingId!),
    enabled: editingId !== null,
    retry: false,
  })

  const saveMutation = useMutation({
    mutationFn: ({ id, text }: { id: string; text: string | null }) =>
      updateNewsBodyText(id, text),
    onSuccess: async (_, { id }) => {
      setSavedId(id)
      setTimeout(() => setSavedId(null), 3000)
      setEditingId(null)
      await queryClient.invalidateQueries({ queryKey: ['ops-news-list'] })
      queryClient.removeQueries({ queryKey: ['ops-news-body', id] })
    },
  })

  useEffect(() => {
    if (bodyQuery.data && editingId) {
      setEditText(bodyQuery.data.bodyText ?? '')
    }
  }, [bodyQuery.data, editingId])

  function handleEdit(article: OpsNewsArticle) {
    setEditingId(article.id)
    setEditText(article.bodyTextPreview ?? '')
    setSavedId(null)
  }

  function handleCancelEdit() {
    setEditingId(null)
    setEditText('')
  }

  function handleSave(id: string) {
    const text = editText.trim()
    saveMutation.mutate({ id, text: text.length > 0 ? text : null })
  }

  function handleClear(id: string) {
    saveMutation.mutate({ id, text: null })
  }

  const total = Number(listQuery.data?.total ?? 0)
  const totalPages = Math.ceil(total / pageSize)

  return (
    <section className="rounded-2xl border border-border/80 bg-white/90 p-6 shadow-sm">
      <div className="flex flex-col gap-2">
        <h2 className="text-lg font-semibold tracking-tight">Editor de body text</h2>
        <p className="max-w-3xl text-sm text-muted-foreground">
          Edita manualmente el texto del artículo usado como insumo para los resúmenes de IA. Deja vacío para limpiar.
        </p>
      </div>

      <div className="mt-6 overflow-hidden rounded-2xl border border-border/80">
        <div className="border-b border-border/80 bg-slate-50/70 px-4 py-4">
          <div className="flex flex-col gap-3 lg:flex-row lg:items-end lg:justify-between">
            <div className="flex-1">
              <label className="text-xs font-semibold uppercase tracking-[0.18em] text-muted-foreground" htmlFor="ops-news-search">
                Buscar
              </label>
              <input
                id="ops-news-search"
                className="mt-2 h-11 w-full rounded-xl border border-border bg-white px-4 text-sm outline-none transition focus:border-teal-600"
                onChange={(event) => setSearch(event.target.value)}
                placeholder="Título, body text o resumen IA"
                value={search}
              />
            </div>

            <div className="lg:w-48">
              <label className="text-xs font-semibold uppercase tracking-[0.18em] text-muted-foreground" htmlFor="ops-news-ai-filter">
                Resumen IA
              </label>
              <select
                className="mt-2 h-11 w-full rounded-xl border border-border bg-white px-4 text-sm outline-none transition focus:border-teal-600"
                id="ops-news-ai-filter"
                onChange={(event) => setHasAiSummaryFilter(event.target.value as 'all' | 'with' | 'without')}
                value={hasAiSummaryFilter}
              >
                <option value="all">Todos</option>
                <option value="with">Con resumen IA</option>
                <option value="without">Sin resumen IA</option>
              </select>
            </div>

            <div className="lg:w-48">
              <label className="text-xs font-semibold uppercase tracking-[0.18em] text-muted-foreground" htmlFor="ops-news-edited-filter">
                Edición manual
              </label>
              <select
                className="mt-2 h-11 w-full rounded-xl border border-border bg-white px-4 text-sm outline-none transition focus:border-teal-600"
                id="ops-news-edited-filter"
                onChange={(event) => setEditedFilter(event.target.value as 'all' | 'edited' | 'not-edited')}
                value={editedFilter}
              >
                <option value="all">Todos</option>
                <option value="edited">Editados</option>
                <option value="not-edited">Sin editar</option>
              </select>
            </div>
          </div>
        </div>

        <table className="min-w-full border-collapse">
          <thead className="bg-teal-950/95 text-left text-xs uppercase tracking-[0.18em] text-teal-50">
            <tr>
              <th className="px-4 py-3 font-medium">Título</th>
              <th className="px-4 py-3 font-medium">Fuente</th>
              <th className="px-4 py-3 font-medium">Publicado</th>
              <th className="px-4 py-3 font-medium">Cuerpo</th>
              <th className="px-4 py-3 font-medium">Resumen IA</th>
              <th className="px-4 py-3 font-medium text-right">Acción</th>
            </tr>
          </thead>
          <tbody className="bg-white">
            {listQuery.isLoading ? (
              <tr>
                <td className="px-4 py-6 text-sm text-muted-foreground" colSpan={6}>
                  Cargando artículos...
                </td>
              </tr>
            ) : null}

            {listQuery.isError ? (
              <tr>
                <td className="px-4 py-6 text-sm text-destructive" colSpan={6}>
                  {listQuery.error.message}
                </td>
              </tr>
            ) : null}

            {listQuery.isSuccess && listQuery.data.items.length === 0 ? (
              <tr>
                <td className="px-4 py-6 text-sm text-muted-foreground" colSpan={6}>
                  No hay artículos registrados.
                </td>
              </tr>
            ) : null}

            {listQuery.data?.items.map((article) => (
              <Fragment key={article.id}>
                <tr
                  className="border-t border-border/70 text-sm"
                >
                  <td className="max-w-xs px-4 py-4 font-medium text-slate-900">
                    <a
                      className="line-clamp-2 hover:underline"
                      href={article.url}
                      rel="noopener noreferrer"
                      target="_blank"
                    >
                      {article.title}
                    </a>
                  </td>
                  <td className="px-4 py-4 text-muted-foreground">{article.source}</td>
                  <td className="px-4 py-4 text-muted-foreground">
                    {new Date(article.publishedAt).toLocaleString('es-MX', {
                      dateStyle: 'short',
                      timeStyle: 'short',
                    })}
                  </td>
                  <td className="px-4 py-4 text-muted-foreground">
                    {article.bodyTextLength != null
                      ? `${article.bodyTextLength} chars`
                      : 'Sin cuerpo'}
                    {article.hasAiSummary ? (
                      <span className="ml-2 rounded-full bg-teal-100 px-2 py-0.5 text-xs text-teal-700">
                        IA
                      </span>
                    ) : null}
                    {article.manuallyEditedAt ? (
                      <span
                        className="ml-2 rounded-full bg-amber-100 px-2 py-0.5 text-xs text-amber-700"
                        title={`Editado: ${new Date(article.manuallyEditedAt).toLocaleString('es-MX')}`}
                      >
                        Editado
                      </span>
                    ) : null}
                    {savedId === article.id ? (
                      <span className="ml-2 text-xs text-teal-600">✓ Guardado</span>
                    ) : null}
                  </td>
                  <td className="max-w-xs px-4 py-4 text-sm text-slate-700">
                    {article.aiSummaryPreview ? (
                      `${article.aiSummaryPreview.slice(0, 120)}${article.aiSummaryPreview.length > 120 ? '…' : ''}`
                    ) : (
                      <span className="text-muted-foreground">Sin resumen</span>
                    )}
                  </td>
                  <td className="px-4 py-4 text-right">
                    {editingId === article.id ? (
                      <button
                        className="rounded-lg border border-slate-200 px-3 py-2 text-sm font-medium text-slate-600 transition hover:bg-slate-50"
                        onClick={handleCancelEdit}
                        type="button"
                      >
                        Cancelar
                      </button>
                    ) : (
                      <button
                        className="rounded-lg border border-teal-200 px-3 py-2 text-sm font-medium text-teal-700 transition hover:bg-teal-50"
                        onClick={() => handleEdit(article)}
                        type="button"
                      >
                        Editar cuerpo
                      </button>
                    )}
                  </td>
                </tr>

                {editingId === article.id ? (
                  <tr className="border-t border-teal-100 bg-teal-50/40">
                    <td colSpan={6} className="px-4 py-4">
                      {bodyQuery.isLoading ? (
                        <p className="text-sm text-muted-foreground">Cargando body text...</p>
                      ) : bodyQuery.isError ? (
                        <p className="text-sm text-destructive">{bodyQuery.error.message}</p>
                      ) : (
                        <div className="flex flex-col gap-3">
                          {bodyQuery.data?.aiSummary ? (
                            <div className="rounded-2xl border border-teal-200 bg-white px-4 py-3">
                              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-teal-700">Resumen de IA</p>
                              <div className="mt-2 text-sm leading-7 text-slate-700 [&>p]:mb-3 [&>p:last-child]:mb-0 [&>ul]:mb-3 [&>ul]:list-disc [&>ul]:pl-5 [&>ol]:mb-3 [&>ol]:list-decimal [&>ol]:pl-5 [&>li]:mb-1 [&>strong]:font-semibold [&>h1]:font-bold [&>h2]:font-semibold [&>h3]:font-semibold">
                                <ReactMarkdown>{bodyQuery.data.aiSummary}</ReactMarkdown>
                              </div>
                            </div>
                          ) : null}
                          <textarea
                            className="h-48 w-full rounded-xl border border-border bg-white px-4 py-3 text-sm font-mono outline-none ring-0 transition focus:border-teal-600"
                            placeholder="Escribe o pega el body text limpio aquí. Deja vacío para limpiar (null)."
                            value={editText}
                            onChange={(e) => setEditText(e.target.value)}
                          />
                          <div className="flex items-center gap-3">
                            <button
                              className="rounded-lg bg-teal-700 px-4 py-2 text-sm font-medium text-white transition hover:bg-teal-800 disabled:cursor-not-allowed disabled:bg-teal-400"
                              disabled={saveMutation.isPending}
                              onClick={() => handleSave(article.id)}
                              type="button"
                            >
                              {saveMutation.isPending ? 'Guardando...' : 'Guardar'}
                            </button>
                            <button
                              className="rounded-lg border border-rose-200 px-4 py-2 text-sm font-medium text-rose-700 transition hover:bg-rose-50 disabled:cursor-not-allowed disabled:opacity-60"
                              disabled={saveMutation.isPending}
                              onClick={() => handleClear(article.id)}
                              type="button"
                            >
                              Limpiar (null)
                            </button>
                            {saveMutation.isError ? (
                              <p className="text-sm text-destructive">{saveMutation.error.message}</p>
                            ) : null}
                          </div>
                        </div>
                      )}
                    </td>
                  </tr>
                ) : null}
              </Fragment>
            ))}
          </tbody>
        </table>
      </div>

      {totalPages > 1 ? (
        <div className="mt-4 flex items-center justify-between">
          <p className="text-sm text-muted-foreground">
            Página {page} de {totalPages} ({total} artículos)
          </p>
          <div className="flex gap-2">
            <button
              className="rounded-lg border border-border px-4 py-2 text-sm font-medium disabled:cursor-not-allowed disabled:opacity-50"
              disabled={page <= 1}
              onClick={() => setPage((p) => p - 1)}
              type="button"
            >
              Anterior
            </button>
            <button
              className="rounded-lg border border-border px-4 py-2 text-sm font-medium disabled:cursor-not-allowed disabled:opacity-50"
              disabled={page >= totalPages}
              onClick={() => setPage((p) => p + 1)}
              type="button"
            >
              Siguiente
            </button>
          </div>
        </div>
      ) : null}
    </section>
  )
}
