import { useEffect, useMemo, useRef, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { CheckCircle2, DatabaseBackup, Filter, Search, ShieldAlert, Sparkles, SlidersHorizontal, Save } from 'lucide-react'
import {
  backfillSeoMetadata,
  fetchSeoMetadata,
  fetchSeoMetadataRow,
  updateSeoMetadata,
  type SeoMetadataDto,
} from '@/api/seoApi'
import {
  ROBOTS_PRESETS,
  buildRobotsDirectives,
  createDefaultRobotsDraft,
  parseRobotsDirectives,
  type RobotsDirectivesDraft,
} from '@/modules/seo/robotsDirectives'
import { cn } from '@/shared/lib/utils'

const PAGE_TYPE_OPTIONS = ['Home', 'StaticPage', 'Fibra', 'News'] as const

function matchesSearch(row: SeoMetadataDto, search: string): boolean {
  const term = search.trim().toLowerCase()
  if (!term) return true

  return [
    row.pageType,
    row.entityKey,
    row.title,
    row.metaDescription,
    row.canonicalPath,
    row.robotsDirectives,
  ]
    .filter((value): value is string => typeof value === 'string')
    .some((value) => value.toLowerCase().includes(term))
}

function sortRows(rows: SeoMetadataDto[]): SeoMetadataDto[] {
  return [...rows].sort((left, right) => {
    if (left.pageType === right.pageType) {
      return left.entityKey.localeCompare(right.entityKey, 'es')
    }

    return left.pageType.localeCompare(right.pageType, 'es')
  })
}

function canUsePreset(draft: RobotsDirectivesDraft, presetValue: string): boolean {
  return buildRobotsDirectives(draft) === presetValue
}

type ContentDraft = {
  title: string
  metaDescription: string
  ogImageUrl: string
  jsonLd: string
}

function createContentDraft(row: SeoMetadataDto): ContentDraft {
  return {
    title: row.title ?? '',
    metaDescription: row.metaDescription ?? '',
    ogImageUrl: row.ogImageUrl ?? '',
    jsonLd: row.jsonLd ?? '',
  }
}

function OverrideBadge() {
  return (
    <span className="rounded-full bg-emerald-100 px-2 py-0.5 text-[10px] font-semibold text-emerald-700">
      override
    </span>
  )
}

export function SeoPage() {
  const queryClient = useQueryClient()
  const [pageType, setPageType] = useState<string>('all')
  const [search, setSearch] = useState('')
  const [selectedId, setSelectedId] = useState<string | null>(null)
  const [draft, setDraft] = useState<RobotsDirectivesDraft>(() => createDefaultRobotsDraft())
  const [contentDraft, setContentDraft] = useState<ContentDraft | null>(null)
  const [feedback, setFeedback] = useState<string | null>(null)
  const [backfillResult, setBackfillResult] = useState<string | null>(null)
  // Última fila cuyo draft se inicializó: evita que un refetch en segundo plano (misma fila,
  // referencia nueva) reinicie el draft y descarte ediciones no guardadas.
  const initializedIdRef = useRef<string | null>(null)

  const seoQuery = useQuery({
    queryKey: ['ops', 'seo', 'robots', pageType, search],
    queryFn: () =>
      fetchSeoMetadata({
        pageType: pageType === 'all' ? undefined : pageType,
        search: search.trim() || undefined,
      }),
    retry: false,
    staleTime: 5 * 60_000,
  })

  const filteredRows = useMemo(
    () => sortRows((seoQuery.data ?? []).filter((row) => matchesSearch(row, search))),
    [search, seoQuery.data],
  )

  const selectedRow = useMemo(() => {
    if (selectedId) {
      return filteredRows.find((row) => row.id === selectedId) ?? null
    }

    return filteredRows[0] ?? null
  }, [filteredRows, selectedId])

  useEffect(() => {
    if (!selectedRow) {
      initializedIdRef.current = null
      setSelectedId(null)
      setDraft(createDefaultRobotsDraft())
      setContentDraft(null)
      return
    }

    setSelectedId(selectedRow.id)
    // Solo (re)inicializar el draft cuando cambia la fila seleccionada, no cuando la query
    // se refetch-ea y devuelve un objeto nuevo para la misma fila (eso pisaría la edición).
    if (initializedIdRef.current !== selectedRow.id) {
      initializedIdRef.current = selectedRow.id
      setDraft(parseRobotsDirectives(selectedRow.robotsDirectives))
      setContentDraft(createContentDraft(selectedRow))
      setFeedback(null)
    }
  }, [selectedRow])

  const refreshRowMutation = useMutation({
    mutationFn: fetchSeoMetadataRow,
    onSuccess: async (row) => {
      setSelectedId(row.id)
      initializedIdRef.current = row.id
      setDraft(parseRobotsDirectives(row.robotsDirectives))
      setContentDraft(createContentDraft(row))
      setFeedback(null)
      await queryClient.invalidateQueries({ queryKey: ['ops', 'seo', 'robots'] })
    },
  })

  const backfillMutation = useMutation({
    mutationFn: backfillSeoMetadata,
    onSuccess: async (result) => {
      setBackfillResult(
        `Backfill: ${result.staticPages} páginas fijas, ${result.fibras} fibras y ${result.news} noticias creadas.`,
      )
      await queryClient.invalidateQueries({ queryKey: ['ops', 'seo', 'robots'] })
    },
    onError: () => setBackfillResult(null),
  })

  const contentMutation = useMutation({
    mutationFn: async () => {
      if (!selectedRow || !contentDraft) {
        throw new Error('Selecciona una fila SEO antes de guardar contenido.')
      }

      return updateSeoMetadata(selectedRow.id, {
        robotsDirectives: null,
        title: contentDraft.title,
        metaDescription: contentDraft.metaDescription,
        ogImageUrl: contentDraft.ogImageUrl,
        jsonLd: contentDraft.jsonLd,
      })
    },
    onSuccess: async (row) => {
      setFeedback('Contenido SEO guardado. Los campos editados quedan marcados como override.')
      setSelectedId(row.id)
      initializedIdRef.current = row.id
      setContentDraft(createContentDraft(row))
      setDraft(parseRobotsDirectives(row.robotsDirectives))
      await queryClient.invalidateQueries({ queryKey: ['ops', 'seo', 'robots'] })
    },
    onError: () => setFeedback(null),
  })

  const saveMutation = useMutation({
    mutationFn: async () => {
      if (!selectedRow) {
        throw new Error('Selecciona una fila SEO antes de guardar.')
      }

      return updateSeoMetadata(selectedRow.id, {
        robotsDirectives: buildRobotsDirectives(draft),
      })
    },
    onSuccess: async (row) => {
      setFeedback(`Guardado: ${row.robotsDirectives || 'indexable por defecto'}`)
      setSelectedId(row.id)
      initializedIdRef.current = row.id
      setDraft(parseRobotsDirectives(row.robotsDirectives))
      await queryClient.invalidateQueries({ queryKey: ['ops', 'seo', 'robots'] })
    },
    onError: () => {
      // Limpia el feedback de éxito previo; el error se muestra vía saveMutation.isError.
      setFeedback(null)
    },
  })

  const preview = buildRobotsDirectives(draft)
  const presetMatch = ROBOTS_PRESETS.find((preset) => canUsePreset(draft, preset.value))?.label ?? null

  function updateDraft(patch: Partial<RobotsDirectivesDraft>) {
    setDraft((current) => ({
      ...current,
      ...patch,
    }))
    setFeedback(null)
  }

  function applyPreset(value: string) {
    setDraft(parseRobotsDirectives(value))
    setFeedback(null)
  }

  return (
    <div className="space-y-6">
      <section className="rounded-[1.75rem] border border-slate-200 bg-white/90 p-6 shadow-sm">
        <div className="flex flex-col gap-3 lg:flex-row lg:items-start lg:justify-between">
          <div>
            <p className="text-xs font-semibold uppercase tracking-[0.28em] text-teal-700">Ops / SEO Robots</p>
            <h1 className="mt-1 text-2xl font-semibold tracking-tight text-slate-950">
              Editar robots por página
            </h1>
            <p className="mt-2 max-w-3xl text-sm leading-6 text-slate-500">
              Ajusta indexación, follow y previews sin editar texto libre propenso a errores. Los presets guardan la cadena exacta y el backend valida tokens y contradicciones.
            </p>
          </div>

          <div className="flex flex-col gap-3">
            <div className="rounded-2xl border border-teal-200 bg-teal-50 px-4 py-3 text-sm text-teal-900">
              <p className="font-semibold">Salida segura</p>
              <p className="mt-1 leading-6">
                Vacío o recomendado equivale a indexable. Nunca se persiste `noindex` por omisión.
              </p>
            </div>

            <button
              type="button"
              onClick={() => backfillMutation.mutate()}
              disabled={backfillMutation.isPending}
              className="inline-flex items-center justify-center gap-2 rounded-xl border border-slate-200 bg-white px-4 py-2.5 text-sm font-semibold text-slate-700 transition hover:border-slate-300 hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-60"
            >
              <DatabaseBackup className="h-4 w-4" />
              {backfillMutation.isPending ? 'Generando filas...' : 'Backfill de contenido existente'}
            </button>
          </div>
        </div>

        {feedback ? (
          <p className="mt-4 rounded-xl border border-teal-200 bg-teal-50 px-4 py-3 text-sm text-teal-800">
            {feedback}
          </p>
        ) : null}

        {backfillResult ? (
          <p className="mt-4 rounded-xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-800">
            {backfillResult}
          </p>
        ) : null}

        {backfillMutation.isError ? (
          <p className="mt-4 rounded-xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">
            No se pudo ejecutar el backfill: {backfillMutation.error.message}
          </p>
        ) : null}
      </section>

      <div className="grid gap-6 xl:grid-cols-[minmax(0,18rem)_minmax(0,1fr)]">
        <aside className="space-y-4">
          <section className="rounded-[1.75rem] border border-slate-200 bg-white/90 p-5 shadow-sm">
            <div className="flex items-center gap-3">
              <span className="flex h-10 w-10 items-center justify-center rounded-2xl bg-slate-900 text-white">
                <Filter className="h-4 w-4" />
              </span>
              <div>
                <h2 className="text-base font-semibold text-slate-950">Filtros</h2>
                <p className="text-sm text-slate-500">Reduce el listado antes de editar.</p>
              </div>
            </div>

            <label className="mt-4 block space-y-1.5">
              <span className="text-xs font-semibold uppercase tracking-wide text-slate-500">PageType</span>
              <select
                value={pageType}
                onChange={(event) => setPageType(event.target.value)}
                className="flex h-11 w-full rounded-xl border border-slate-200 bg-white px-3 text-sm text-slate-900 outline-none transition focus:border-teal-600"
              >
                <option value="all">Todos</option>
                {PAGE_TYPE_OPTIONS.map((item) => (
                  <option key={item} value={item}>
                    {item}
                  </option>
                ))}
              </select>
            </label>

            <label className="mt-4 block space-y-1.5">
              <span className="text-xs font-semibold uppercase tracking-wide text-slate-500">Buscar</span>
              <div className="flex items-center gap-2 rounded-xl border border-slate-200 bg-white px-3">
                <Search className="h-4 w-4 text-slate-400" />
                <input
                  value={search}
                  onChange={(event) => setSearch(event.target.value)}
                  placeholder="Título, entityKey, robots..."
                  className="h-11 w-full bg-transparent text-sm text-slate-900 outline-none"
                />
              </div>
            </label>
          </section>

          <section className="rounded-[1.75rem] border border-slate-200 bg-white/90 p-5 shadow-sm">
            <div className="flex items-center gap-3">
              <span className="flex h-10 w-10 items-center justify-center rounded-2xl bg-teal-50 text-teal-700">
                <ShieldAlert className="h-4 w-4" />
              </span>
              <div>
                <h2 className="text-base font-semibold text-slate-950">Estado</h2>
                <p className="text-sm text-slate-500">Verifica permisos y data cargada.</p>
              </div>
            </div>

            {seoQuery.isLoading ? <p className="mt-4 text-sm text-slate-500">Cargando filas SEO...</p> : null}
            {seoQuery.isError ? <p className="mt-4 rounded-xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">{seoQuery.error.message}</p> : null}
            {seoQuery.isSuccess ? (
              <p className="mt-4 text-sm text-slate-600">
                {filteredRows.length} filas visibles.
              </p>
            ) : null}
          </section>
        </aside>

        <div className="grid gap-6 xl:grid-cols-[minmax(0,18rem)_minmax(0,1fr)]">
          <section className="rounded-[1.75rem] border border-slate-200 bg-white/90 p-5 shadow-sm">
            <div className="flex items-center gap-3">
              <span className="flex h-10 w-10 items-center justify-center rounded-2xl bg-teal-50 text-teal-700">
                <SlidersHorizontal className="h-4 w-4" />
              </span>
              <div>
                <h2 className="text-base font-semibold text-slate-950">Filas SEO</h2>
                <p className="text-sm text-slate-500">Selecciona una página para editar sus robots.</p>
              </div>
            </div>

            <div className="mt-4 space-y-3">
              {filteredRows.length === 0 ? (
                <p className="rounded-2xl border border-dashed border-slate-200 bg-slate-50 px-4 py-6 text-sm text-slate-500">
                  No hay filas para estos filtros.
                </p>
              ) : (
                filteredRows.map((row) => {
                  const isActive = row.id === selectedRow?.id

                  return (
                    <button
                      key={row.id}
                      type="button"
                      onClick={() => setSelectedId(row.id)}
                      className={cn(
                        'w-full rounded-2xl border p-4 text-left transition',
                        isActive
                          ? 'border-teal-500 bg-teal-50 shadow-[inset_0_0_0_1px_rgba(13,148,136,0.16)]'
                          : 'border-slate-200 bg-white hover:border-slate-300 hover:bg-slate-50',
                      )}
                    >
                      <div className="flex items-start justify-between gap-3">
                        <div>
                          <p className="text-[11px] font-semibold uppercase tracking-[0.24em] text-teal-700">
                            {row.pageType} · {row.isActive ? 'Activa' : 'Inactiva'}
                          </p>
                          <p className="mt-1 text-sm font-semibold text-slate-950">{row.entityKey}</p>
                        </div>

                        {row.robotsDirectivesIsOverridden ? (
                          <span className="rounded-full bg-emerald-100 px-2.5 py-1 text-[11px] font-semibold text-emerald-700">
                            override
                          </span>
                        ) : (
                          <span className="rounded-full bg-slate-100 px-2.5 py-1 text-[11px] font-semibold text-slate-500">
                            default
                          </span>
                        )}
                      </div>

                      <p className="mt-2 line-clamp-2 text-sm leading-6 text-slate-600">{row.title}</p>
                      <p className="mt-3 font-mono text-[11px] leading-5 text-slate-500">{row.robotsDirectives || 'indexable por defecto'}</p>
                    </button>
                  )
                })
              )}
            </div>
          </section>

          <section className="rounded-[1.75rem] border border-slate-200 bg-white/90 p-6 shadow-sm">
            <div className="flex flex-col gap-4 border-b border-slate-200 pb-4 lg:flex-row lg:items-start lg:justify-between">
              <div>
                <h2 className="text-lg font-semibold tracking-tight text-slate-950">Editor de robots</h2>
                <p className="mt-1 text-sm text-slate-500">
                  {selectedRow ? `${selectedRow.pageType} · ${selectedRow.entityKey}` : 'Selecciona una fila SEO'}
                </p>
              </div>

              {presetMatch ? (
                <span className="rounded-full bg-teal-50 px-3 py-1 text-xs font-semibold text-teal-700">
                  {presetMatch}
                </span>
              ) : (
                <span className="rounded-full bg-slate-100 px-3 py-1 text-xs font-semibold text-slate-500">
                  Personalizado
                </span>
              )}
            </div>

            {!selectedRow ? (
              <p className="mt-5 rounded-2xl border border-dashed border-slate-200 bg-slate-50 px-4 py-6 text-sm text-slate-500">
                No hay fila seleccionada.
              </p>
            ) : (
              <div className="mt-5 space-y-5">
                <div className="flex flex-wrap gap-2">
                  {ROBOTS_PRESETS.map((preset) => (
                    <button
                      key={preset.id}
                      type="button"
                      onClick={() => applyPreset(preset.value)}
                      className={cn(
                        'rounded-full border px-3 py-1.5 text-xs font-semibold transition',
                        buildRobotsDirectives(draft) === preset.value
                          ? 'border-teal-500 bg-teal-50 text-teal-800'
                          : 'border-slate-200 bg-white text-slate-700 hover:border-slate-300 hover:bg-slate-50',
                      )}
                    >
                      {preset.label}
                    </button>
                  ))}
                </div>

                <div className="grid gap-4 md:grid-cols-2">
                  <label className="flex items-center gap-3 rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3">
                    <input
                      type="checkbox"
                      checked={draft.index}
                      onChange={(event) => updateDraft({ index: event.target.checked })}
                      className="h-4 w-4 rounded border-slate-300 text-teal-700 focus:ring-teal-600"
                    />
                    <span className="text-sm text-slate-700">Index</span>
                  </label>

                  <label className="flex items-center gap-3 rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3">
                    <input
                      type="checkbox"
                      checked={draft.follow}
                      onChange={(event) => updateDraft({ follow: event.target.checked })}
                      className="h-4 w-4 rounded border-slate-300 text-teal-700 focus:ring-teal-600"
                    />
                    <span className="text-sm text-slate-700">Follow</span>
                  </label>
                </div>

                <div className="grid gap-4 md:grid-cols-3">
                  <label className="space-y-1.5">
                    <span className="text-xs font-semibold uppercase tracking-wide text-slate-500">max-snippet</span>
                    <input
                      value={draft.maxSnippet}
                      onChange={(event) => updateDraft({ maxSnippet: event.target.value })}
                      className="flex h-11 w-full rounded-xl border border-slate-200 bg-white px-3 text-sm text-slate-900 outline-none transition focus:border-teal-600"
                      placeholder="-1"
                    />
                  </label>

                  <label className="space-y-1.5">
                    <span className="text-xs font-semibold uppercase tracking-wide text-slate-500">max-image-preview</span>
                    <select
                      value={draft.maxImagePreview}
                      onChange={(event) => updateDraft({ maxImagePreview: event.target.value as RobotsDirectivesDraft['maxImagePreview'] })}
                      className="flex h-11 w-full rounded-xl border border-slate-200 bg-white px-3 text-sm text-slate-900 outline-none transition focus:border-teal-600"
                    >
                      <option value="">Predeterminado</option>
                      <option value="none">none</option>
                      <option value="standard">standard</option>
                      <option value="large">large</option>
                    </select>
                  </label>

                  <label className="space-y-1.5">
                    <span className="text-xs font-semibold uppercase tracking-wide text-slate-500">max-video-preview</span>
                    <input
                      value={draft.maxVideoPreview}
                      onChange={(event) => updateDraft({ maxVideoPreview: event.target.value })}
                      className="flex h-11 w-full rounded-xl border border-slate-200 bg-white px-3 text-sm text-slate-900 outline-none transition focus:border-teal-600"
                      placeholder="-1"
                    />
                  </label>
                </div>

                <div className="rounded-2xl border border-slate-200 bg-slate-50 p-4">
                  <p className="text-xs font-semibold uppercase tracking-[0.24em] text-slate-500">Preview</p>
                  <p className="mt-2 font-mono text-sm text-slate-900">{preview}</p>
                </div>

                <div className="flex flex-wrap items-center justify-between gap-3">
                  <p className="text-sm leading-6 text-slate-500">
                    {selectedRow.robotsDirectivesIsOverridden
                      ? 'Esta fila ya tiene override manual.'
                      : 'Al guardar, la fila queda marcada como override manual.'}
                  </p>

                  <div className="flex gap-2">
                    <button
                      type="button"
                      onClick={() => setDraft(parseRobotsDirectives(selectedRow.robotsDirectives))}
                      className="rounded-xl border border-slate-200 bg-white px-4 py-2.5 text-sm font-semibold text-slate-700 transition hover:border-slate-300 hover:bg-slate-50"
                    >
                      Revertir
                    </button>

                    <button
                      type="button"
                      onClick={() => refreshRowMutation.mutate(selectedRow.id)}
                      className="rounded-xl border border-slate-200 bg-white px-4 py-2.5 text-sm font-semibold text-slate-700 transition hover:border-slate-300 hover:bg-slate-50"
                    >
                      Recargar
                    </button>

                    <button
                      type="button"
                      onClick={() => saveMutation.mutate()}
                      disabled={saveMutation.isPending}
                      className="inline-flex items-center gap-2 rounded-xl bg-teal-700 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-teal-800 disabled:cursor-not-allowed disabled:opacity-60"
                    >
                      {saveMutation.isPending ? <Sparkles className="h-4 w-4" /> : <Save className="h-4 w-4" />}
                      {saveMutation.isPending ? 'Guardando...' : 'Guardar robots'}
                    </button>
                  </div>
                </div>

                {saveMutation.isError ? (
                  <p className="rounded-xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">
                    No se pudo guardar: {saveMutation.error.message}
                  </p>
                ) : null}
                {refreshRowMutation.isError ? (
                  <p className="rounded-xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">
                    No se pudo recargar la fila: {refreshRowMutation.error.message}
                  </p>
                ) : null}

                <div className="grid gap-3 rounded-2xl border border-slate-200 bg-white p-4 md:grid-cols-2">
                  <div>
                    <p className="text-xs font-semibold uppercase tracking-[0.24em] text-slate-500">Canonical</p>
                    <p className="mt-1 text-sm text-slate-700">{selectedRow.canonicalPath}</p>
                  </div>
                  <div>
                    <p className="text-xs font-semibold uppercase tracking-[0.24em] text-slate-500">Updated</p>
                    <p className="mt-1 text-sm text-slate-700">
                      {selectedRow.updatedAt} · {selectedRow.updatedBy ?? 'sin actor'}
                    </p>
                  </div>
                </div>

                {contentDraft ? (
                  <div className="space-y-4 rounded-2xl border border-slate-200 bg-white p-5">
                    <div className="flex items-center justify-between gap-3">
                      <h3 className="text-base font-semibold text-slate-950">Contenido SEO</h3>
                      <p className="text-xs text-slate-500">
                        Editar un campo lo marca como override (la regeneración automática ya no lo pisa).
                      </p>
                    </div>

                    <label className="block space-y-1.5">
                      <span className="flex items-center gap-2 text-xs font-semibold uppercase tracking-wide text-slate-500">
                        Title
                        {selectedRow.titleIsOverridden ? <OverrideBadge /> : null}
                      </span>
                      <input
                        value={contentDraft.title}
                        maxLength={120}
                        onChange={(event) => {
                          setContentDraft({ ...contentDraft, title: event.target.value })
                          setFeedback(null)
                        }}
                        className="flex h-11 w-full rounded-xl border border-slate-200 bg-white px-3 text-sm text-slate-900 outline-none transition focus:border-teal-600"
                      />
                    </label>

                    <label className="block space-y-1.5">
                      <span className="flex items-center gap-2 text-xs font-semibold uppercase tracking-wide text-slate-500">
                        Meta description ({contentDraft.metaDescription.length}/160)
                        {selectedRow.metaDescriptionIsOverridden ? <OverrideBadge /> : null}
                      </span>
                      <textarea
                        value={contentDraft.metaDescription}
                        maxLength={160}
                        rows={3}
                        onChange={(event) => {
                          setContentDraft({ ...contentDraft, metaDescription: event.target.value })
                          setFeedback(null)
                        }}
                        className="w-full rounded-xl border border-slate-200 bg-white px-3 py-2 text-sm text-slate-900 outline-none transition focus:border-teal-600"
                      />
                      {contentDraft.metaDescription.length > 0 && contentDraft.metaDescription.length < 120 ? (
                        <span className="text-xs text-amber-600">
                          Bajo el piso recomendado de 120 caracteres (no bloquea el guardado).
                        </span>
                      ) : null}
                    </label>

                    <label className="block space-y-1.5">
                      <span className="flex items-center gap-2 text-xs font-semibold uppercase tracking-wide text-slate-500">
                        og:image URL
                        {selectedRow.ogImageUrlIsOverridden ? <OverrideBadge /> : null}
                      </span>
                      <input
                        value={contentDraft.ogImageUrl}
                        onChange={(event) => {
                          setContentDraft({ ...contentDraft, ogImageUrl: event.target.value })
                          setFeedback(null)
                        }}
                        placeholder="https://… o /ruta-relativa"
                        className="flex h-11 w-full rounded-xl border border-slate-200 bg-white px-3 text-sm text-slate-900 outline-none transition focus:border-teal-600"
                      />
                    </label>

                    <label className="block space-y-1.5">
                      <span className="flex items-center gap-2 text-xs font-semibold uppercase tracking-wide text-slate-500">
                        JSON-LD
                        {selectedRow.jsonLdIsOverridden ? <OverrideBadge /> : null}
                      </span>
                      <textarea
                        value={contentDraft.jsonLd}
                        rows={6}
                        onChange={(event) => {
                          setContentDraft({ ...contentDraft, jsonLd: event.target.value })
                          setFeedback(null)
                        }}
                        placeholder='{"@context":"https://schema.org",…}'
                        className="w-full rounded-xl border border-slate-200 bg-white px-3 py-2 font-mono text-xs text-slate-900 outline-none transition focus:border-teal-600"
                      />
                    </label>

                    <div className="flex justify-end">
                      <button
                        type="button"
                        onClick={() => contentMutation.mutate()}
                        disabled={contentMutation.isPending}
                        className="inline-flex items-center gap-2 rounded-xl bg-slate-900 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-slate-800 disabled:cursor-not-allowed disabled:opacity-60"
                      >
                        <Save className="h-4 w-4" />
                        {contentMutation.isPending ? 'Guardando...' : 'Guardar contenido SEO'}
                      </button>
                    </div>

                    {contentMutation.isError ? (
                      <p className="rounded-xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">
                        No se pudo guardar el contenido: {contentMutation.error.message}
                      </p>
                    ) : null}
                  </div>
                ) : null}
              </div>
            )}
          </section>
        </div>
      </div>

      {preview === ROBOTS_PRESETS[0].value ? (
        <p className="text-sm text-teal-700">
          <CheckCircle2 className="mr-2 inline h-4 w-4 align-[-2px]" />
          La combinación actual coincide con el preset recomendado.
        </p>
      ) : null}
    </div>
  )
}
