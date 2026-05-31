import { useEffect, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { fetchEditorialPages, updateEditorialPage, type EditorialPageDto } from '@/api/editorialApi'

const textareaClassName =
  'min-h-[24rem] w-full rounded-2xl border border-slate-200 bg-white px-4 py-3 font-mono text-sm leading-6 text-slate-900 outline-none transition focus:border-teal-600'

export function EditorialPage() {
  const pagesQuery = useQuery({
    queryKey: ['editorial-pages'],
    queryFn: fetchEditorialPages,
    retry: false,
  })

  return (
    <div className="space-y-6">
      <section className="rounded-[1.75rem] border border-slate-200 bg-white/90 p-6 shadow-sm">
        <div className="flex flex-col gap-2">
          <p className="text-xs font-semibold uppercase tracking-[0.28em] text-teal-700">Ops / Contenido editorial</p>
          <h1 className="text-2xl font-semibold tracking-tight text-slate-950">
            Editar la sección Conoce las FIBRAs
          </h1>
          <p className="max-w-3xl text-sm leading-6 text-slate-500">
            El catálogo de secciones es fijo. Aquí solo editas el markdown visible en la ruta pública
            `/conoce-las-fibras`.
          </p>
        </div>

        {pagesQuery.isLoading ? <p className="mt-6 text-sm text-slate-500">Cargando páginas editoriales...</p> : null}

        {pagesQuery.isError ? (
          <p className="mt-6 rounded-xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">
            {pagesQuery.error.message}
          </p>
        ) : null}
      </section>

      {pagesQuery.data?.map((page) => (
        <EditorialCard key={page.slug} page={page} />
      ))}
    </div>
  )
}

function EditorialCard({ page }: { page: EditorialPageDto }) {
  const queryClient = useQueryClient()
  const [content, setContent] = useState(page.content)
  const [successMessage, setSuccessMessage] = useState<string | null>(null)
  const [localError, setLocalError] = useState<string | null>(null)

  useEffect(() => {
    setContent(page.content)
  }, [page.content])

  const saveMutation = useMutation({
    mutationFn: (nextContent: string) => updateEditorialPage(page.slug, nextContent),
    onSuccess: async () => {
      setLocalError(null)
      setSuccessMessage('✓ Contenido guardado')
      await queryClient.invalidateQueries({ queryKey: ['editorial-pages'] })
    },
  })

  const isDirty = content !== page.content

  function handleSave() {
    setSuccessMessage(null)
    setLocalError(null)

    if (content.trim().length === 0) {
      setLocalError('El contenido no puede estar vacío.')
      return
    }

    saveMutation.mutate(content)
  }

  return (
    <section className="rounded-[1.75rem] border border-slate-200 bg-white/90 p-6 shadow-sm">
      <div className="flex flex-col gap-2 border-b border-slate-200 pb-5">
        <p className="text-xs font-semibold uppercase tracking-[0.24em] text-teal-700">{page.slug}</p>
        <h2 className="text-xl font-semibold tracking-tight text-slate-950">{page.title}</h2>
        <p className="text-sm text-slate-500">
          Última actualización:{' '}
          {new Date(page.updatedAt).toLocaleString('es-MX', {
            dateStyle: 'medium',
            timeStyle: 'short',
          })}
        </p>
      </div>

      <div className="mt-5 space-y-4">
        <textarea
          className={textareaClassName}
          rows={20}
          value={content}
          onChange={(event) => { setContent(event.target.value); setSuccessMessage(null) }}
          spellCheck={false}
        />

        {successMessage ? (
          <p className="rounded-xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-800">
            {successMessage}
          </p>
        ) : null}

        {localError ? (
          <p className="rounded-xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">{localError}</p>
        ) : null}

        {saveMutation.isError ? (
          <p className="rounded-xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">
            {saveMutation.error.message}
          </p>
        ) : null}

        <div className="flex items-center justify-between gap-3">
          <p className="text-sm text-slate-500">
            {isDirty ? 'Hay cambios pendientes por guardar.' : 'Sin cambios pendientes.'}
          </p>
          <button
            className="rounded-xl bg-teal-700 px-5 py-2.5 text-sm font-semibold text-white transition hover:bg-teal-800 disabled:cursor-not-allowed disabled:opacity-60"
            type="button"
            disabled={saveMutation.isPending || !isDirty}
            onClick={handleSave}
          >
            {saveMutation.isPending ? 'Guardando...' : 'Guardar'}
          </button>
        </div>
      </div>
    </section>
  )
}
