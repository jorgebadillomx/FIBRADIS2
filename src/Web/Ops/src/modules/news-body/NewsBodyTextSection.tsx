import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
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

  const [editingId, setEditingId] = useState<string | null>(null)
  const [editText, setEditText] = useState<string>('')
  const [savedId, setSavedId] = useState<string | null>(null)

  const listQuery = useQuery({
    queryKey: ['ops-news-list', page, pageSize],
    queryFn: () => fetchOpsNewsList(page, pageSize),
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

  function handleEdit(article: OpsNewsArticle) {
    setEditingId(article.id)
    setEditText('')
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

  // Pre-populate textarea once body text loads
  if (bodyQuery.isSuccess && editingId && editText === '') {
    setEditText(bodyQuery.data.bodyText ?? '')
  }

  const total = listQuery.data?.total ?? 0
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
        <table className="min-w-full border-collapse">
          <thead className="bg-teal-950/95 text-left text-xs uppercase tracking-[0.18em] text-teal-50">
            <tr>
              <th className="px-4 py-3 font-medium">Título</th>
              <th className="px-4 py-3 font-medium">Fuente</th>
              <th className="px-4 py-3 font-medium">Publicado</th>
              <th className="px-4 py-3 font-medium">Cuerpo</th>
              <th className="px-4 py-3 font-medium text-right">Acción</th>
            </tr>
          </thead>
          <tbody className="bg-white">
            {listQuery.isLoading ? (
              <tr>
                <td className="px-4 py-6 text-sm text-muted-foreground" colSpan={5}>
                  Cargando artículos...
                </td>
              </tr>
            ) : null}

            {listQuery.isError ? (
              <tr>
                <td className="px-4 py-6 text-sm text-destructive" colSpan={5}>
                  {listQuery.error.message}
                </td>
              </tr>
            ) : null}

            {listQuery.isSuccess && listQuery.data.items.length === 0 ? (
              <tr>
                <td className="px-4 py-6 text-sm text-muted-foreground" colSpan={5}>
                  No hay artículos registrados.
                </td>
              </tr>
            ) : null}

            {listQuery.data?.items.map((article) => (
              <>
                <tr
                  className="border-t border-border/70 text-sm"
                  key={article.id}
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
                    {savedId === article.id ? (
                      <span className="ml-2 text-xs text-teal-600">✓ Guardado</span>
                    ) : null}
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
                  <tr className="border-t border-teal-100 bg-teal-50/40" key={`edit-${article.id}`}>
                    <td colSpan={5} className="px-4 py-4">
                      {bodyQuery.isLoading ? (
                        <p className="text-sm text-muted-foreground">Cargando body text...</p>
                      ) : bodyQuery.isError ? (
                        <p className="text-sm text-destructive">{bodyQuery.error.message}</p>
                      ) : (
                        <div className="flex flex-col gap-3">
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
              </>
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
