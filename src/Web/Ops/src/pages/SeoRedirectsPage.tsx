import { useEffect, useMemo, useState } from 'react'
import { useForm } from 'react-hook-form'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { CheckCircle2, PencilLine, Plus, RefreshCw, ToggleLeft, ToggleRight } from 'lucide-react'
import {
  activateUrlRedirect,
  createUrlRedirect,
  deactivateUrlRedirect,
  fetchUrlRedirects,
  updateUrlRedirect,
  type UpsertUrlRedirectRequest,
  type UrlRedirectDto,
} from '@/api/redirectsApi'

type FormValues = UpsertUrlRedirectRequest

const EMPTY_FORM: FormValues = {
  fromPath: '',
  toPath: '',
  statusCode: 301,
  isActive: true,
  notes: '',
}

export function SeoRedirectsPage() {
  const queryClient = useQueryClient()
  const [editingId, setEditingId] = useState<string | null>(null)
  const [message, setMessage] = useState<string | null>(null)

  const redirectsQuery = useQuery({
    queryKey: ['ops', 'seo', 'redirects'],
    queryFn: fetchUrlRedirects,
    retry: false,
    staleTime: 5 * 60_000,
  })

  const currentItem = useMemo(
    () => redirectsQuery.data?.find((item) => item.id === editingId) ?? null,
    [editingId, redirectsQuery.data],
  )

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isDirty },
  } = useForm<FormValues>({
    defaultValues: EMPTY_FORM,
  })

  useEffect(() => {
    if (editingId || !redirectsQuery.data) {
      return
    }

    reset(EMPTY_FORM)
  }, [editingId, redirectsQuery.data, reset])

  const saveMutation = useMutation({
    mutationFn: async (values: FormValues & { id?: string }) => {
      const payload: UpsertUrlRedirectRequest = {
        fromPath: values.fromPath.trim(),
        toPath: values.toPath.trim(),
        statusCode: Number(values.statusCode),
        isActive: values.isActive,
        notes: values.notes?.trim() ? values.notes.trim() : null,
      }

      if (values.id) {
        return updateUrlRedirect(values.id, payload)
      }

      return createUrlRedirect(payload)
    },
    onSuccess: async () => {
      setMessage('✓ Redirect guardado')
      setEditingId(null)
      reset(EMPTY_FORM)
      await queryClient.invalidateQueries({ queryKey: ['ops', 'seo', 'redirects'] })
    },
  })

  const toggleMutation = useMutation({
    mutationFn: async (item: UrlRedirectDto) =>
      item.isActive ? deactivateUrlRedirect(item.id) : activateUrlRedirect(item.id),
    onSuccess: async () => {
      setMessage('✓ Estado actualizado')
      await queryClient.invalidateQueries({ queryKey: ['ops', 'seo', 'redirects'] })
    },
  })

  const rows = redirectsQuery.data ?? []

  function handleEdit(item: UrlRedirectDto) {
    setEditingId(item.id)
    setMessage(null)
    reset({
      fromPath: item.fromPath,
      toPath: item.toPath,
      statusCode: item.statusCode,
      isActive: item.isActive,
      notes: item.notes ?? '',
    })
  }

  function handleCancel() {
    setEditingId(null)
    setMessage(null)
    reset(EMPTY_FORM)
  }

  function handleCreateNew() {
    setEditingId(null)
    setMessage(null)
    reset(EMPTY_FORM)
  }

  const onSubmit = handleSubmit((values) => {
    setMessage(null)
    saveMutation.mutate({ id: editingId ?? undefined, ...values })
  })

  return (
    <div className="space-y-6">
      <section className="rounded-[1.75rem] border border-slate-200 bg-white/90 p-6 shadow-sm">
        <div className="flex flex-col gap-3 lg:flex-row lg:items-start lg:justify-between">
          <div>
            <p className="text-xs font-semibold uppercase tracking-[0.28em] text-teal-700">Ops / SEO Redirects</p>
            <h1 className="mt-1 text-2xl font-semibold tracking-tight text-slate-950">
              Administrar redirects 301/302
            </h1>
            <p className="mt-2 max-w-3xl text-sm leading-6 text-slate-500">
              Mantén reglas de redirección editables desde Ops. Las rutas activas se cachean en memoria y se aplican antes de servir HTML.
            </p>
          </div>

          <button
            type="button"
            className="inline-flex items-center gap-2 rounded-xl bg-teal-700 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-teal-800"
            onClick={handleCreateNew}
          >
            <Plus className="h-4 w-4" />
            Nueva regla
          </button>
        </div>

        {message ? (
          <p className="mt-4 rounded-xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-800">
            {message}
          </p>
        ) : null}
      </section>

      <div className="grid gap-6 xl:grid-cols-[minmax(0,1fr)_24rem]">
        <section className="rounded-[1.75rem] border border-slate-200 bg-white/90 p-6 shadow-sm">
          <div className="flex items-center justify-between gap-3 border-b border-slate-200 pb-4">
            <div>
              <h2 className="text-lg font-semibold tracking-tight text-slate-950">
                {editingId ? 'Editar redirect' : 'Crear redirect'}
              </h2>
              <p className="mt-1 text-sm text-slate-500">
                FromPath y ToPath deben ser rutas internas. El FromPath se normaliza a lowercase sin trailing slash.
              </p>
            </div>

            {currentItem ? (
              <span className={currentItem.isActive ? 'rounded-full bg-emerald-50 px-3 py-1 text-xs font-semibold text-emerald-700' : 'rounded-full bg-slate-100 px-3 py-1 text-xs font-semibold text-slate-500'}>
                {currentItem.isActive ? 'Activo' : 'Inactivo'}
              </span>
            ) : null}
          </div>

          <form className="mt-5 grid gap-4" onSubmit={onSubmit}>
            <div className="grid gap-4 md:grid-cols-2">
              <label className="space-y-1.5">
                <span className="text-xs font-semibold uppercase tracking-wide text-slate-500">FromPath</span>
                <input
                  {...register('fromPath', {
                    required: 'FromPath es requerido.',
                    validate: (value) =>
                      value.trim().startsWith('/') ? true : 'FromPath debe comenzar con /',
                  })}
                  className={inputClassName}
                  placeholder="/blog"
                />
                {errors.fromPath ? <span className="text-xs text-rose-700">{errors.fromPath.message}</span> : null}
              </label>

              <label className="space-y-1.5">
                <span className="text-xs font-semibold uppercase tracking-wide text-slate-500">ToPath</span>
                <input
                  {...register('toPath', {
                    required: 'ToPath es requerido.',
                    validate: (value) =>
                      value.trim().startsWith('/') ? true : 'ToPath debe comenzar con /',
                  })}
                  className={inputClassName}
                  placeholder="/noticias"
                />
                {errors.toPath ? <span className="text-xs text-rose-700">{errors.toPath.message}</span> : null}
              </label>
            </div>

            <div className="grid gap-4 md:grid-cols-2">
              <label className="space-y-1.5">
                <span className="text-xs font-semibold uppercase tracking-wide text-slate-500">StatusCode</span>
                <select
                  {...register('statusCode', {
                    setValueAs: (value) => Number(value),
                    required: 'StatusCode es requerido.',
                  })}
                  className={inputClassName}
                >
                  <option value={301}>301</option>
                  <option value={302}>302</option>
                </select>
                {errors.statusCode ? <span className="text-xs text-rose-700">{errors.statusCode.message}</span> : null}
              </label>

              <label className="flex items-end gap-3 rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3">
                <input
                  {...register('isActive')}
                  className="h-4 w-4 rounded border-slate-300 text-teal-700 focus:ring-teal-600"
                  type="checkbox"
                />
                <span className="text-sm text-slate-700">Regla activa</span>
              </label>
            </div>

            <label className="space-y-1.5">
              <span className="text-xs font-semibold uppercase tracking-wide text-slate-500">Notas</span>
              <textarea
                {...register('notes')}
                className="min-h-[8rem] w-full rounded-2xl border border-slate-200 bg-white px-4 py-3 text-sm text-slate-900 outline-none transition focus:border-teal-600"
                placeholder="Contexto operativo de la regla"
              />
            </label>

            <div className="flex flex-wrap items-center justify-between gap-3">
              <p className="text-sm text-slate-500">
                {isDirty
                  ? 'Hay cambios pendientes por guardar.'
                  : editingId
                    ? 'Edita la regla y guarda para invalidar la cache de redirects activos.'
                    : 'Cada guardado invalida la cache en memoria del middleware.'}
              </p>

              <div className="flex gap-2">
                {editingId ? (
                  <button
                    type="button"
                    className="rounded-xl border border-slate-200 bg-white px-4 py-2.5 text-sm font-semibold text-slate-700 transition hover:border-slate-300 hover:bg-slate-50"
                    onClick={handleCancel}
                  >
                    Cancelar
                  </button>
                ) : null}

                <button
                  type="submit"
                  className="inline-flex items-center gap-2 rounded-xl bg-teal-700 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-teal-800 disabled:cursor-not-allowed disabled:opacity-60"
                  disabled={saveMutation.isPending}
                >
                  <PencilLine className="h-4 w-4" />
                  {saveMutation.isPending ? 'Guardando...' : editingId ? 'Actualizar' : 'Crear'}
                </button>
              </div>
            </div>
          </form>
        </section>

        <aside className="space-y-4">
          <section className="rounded-[1.75rem] border border-slate-200 bg-white/90 p-6 shadow-sm">
            <div className="flex items-center gap-3">
              <span className="flex h-11 w-11 items-center justify-center rounded-2xl bg-teal-50 text-teal-700">
                <RefreshCw className="h-5 w-5" />
              </span>
              <div>
                <h2 className="text-lg font-semibold tracking-tight text-slate-950">Estado de carga</h2>
                <p className="text-sm text-slate-500">Listado de reglas activas e inactivas.</p>
              </div>
            </div>

            {redirectsQuery.isLoading ? <p className="mt-4 text-sm text-slate-500">Cargando redirects...</p> : null}
            {redirectsQuery.isError ? <p className="mt-4 rounded-xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">{redirectsQuery.error.message}</p> : null}
            {redirectsQuery.isSuccess ? (
              <p className="mt-4 text-sm text-slate-600">
                {rows.length} redirects cargados.
              </p>
            ) : null}
          </section>

          <section className="rounded-[1.75rem] border border-slate-200 bg-white/90 p-6 shadow-sm">
            <div className="flex items-center gap-3">
              <span className="flex h-11 w-11 items-center justify-center rounded-2xl bg-slate-900 text-white">
                <CheckCircle2 className="h-5 w-5" />
              </span>
              <div>
                <h2 className="text-lg font-semibold tracking-tight text-slate-950">Reglas</h2>
                <p className="text-sm text-slate-500">Lo que se vea aquí es lo que aplicará el middleware.</p>
              </div>
            </div>

            {rows.length === 0 ? (
              <p className="mt-4 text-sm text-slate-500">No hay redirects configurados.</p>
            ) : (
              <div className="mt-4 space-y-3">
                {rows.map((item) => (
                  <article key={item.id} className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3">
                    <div className="flex items-start justify-between gap-3">
                      <div>
                        <p className="text-[11px] font-semibold uppercase tracking-[0.24em] text-teal-700">
                          {item.statusCode} · {item.isActive ? 'Activa' : 'Inactiva'}
                        </p>
                        <p className="mt-1 text-sm font-semibold text-slate-950">{item.fromPath}</p>
                        <p className="mt-1 text-sm text-slate-600">→ {item.toPath}</p>
                        {item.notes ? <p className="mt-2 text-xs text-slate-500">{item.notes}</p> : null}
                      </div>

                      <div className="flex flex-col gap-2">
                        <button
                          type="button"
                          className="rounded-lg border border-slate-200 bg-white px-2 py-1 text-xs font-semibold text-slate-700 transition hover:border-teal-300 hover:text-teal-700"
                          onClick={() => handleEdit(item)}
                        >
                          Editar
                        </button>

                        <button
                          type="button"
                          className="rounded-lg border border-slate-200 bg-white px-2 py-1 text-xs font-semibold text-slate-700 transition hover:border-teal-300 hover:text-teal-700"
                          onClick={() => toggleMutation.mutate(item)}
                          disabled={toggleMutation.isPending}
                        >
                          {item.isActive ? (
                            <span className="inline-flex items-center gap-1">
                              <ToggleLeft className="h-3.5 w-3.5" />
                              Desactivar
                            </span>
                          ) : (
                            <span className="inline-flex items-center gap-1">
                              <ToggleRight className="h-3.5 w-3.5" />
                              Activar
                            </span>
                          )}
                        </button>
                      </div>
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

const inputClassName =
  'w-full rounded-xl border border-slate-200 bg-white px-3 py-2.5 text-sm text-slate-900 outline-none transition focus:border-teal-600'
