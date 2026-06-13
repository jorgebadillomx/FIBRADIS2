import { Fragment, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { fetchPipelineDashboard, runPipeline, type PipelineDashboardDto, type RunPipelineTarget } from '@/api/dashboardApi'
import { cn } from '@/shared/lib/utils'

type PipelineName = PipelineDashboardDto['pipelines'][number]['pipeline']

const pipelineCards: Array<{ name: PipelineName; target: RunPipelineTarget; accent: string }> = [
  { name: 'Market', target: 'market', accent: 'from-emerald-500/18 to-emerald-100' },
  { name: 'News', target: 'news', accent: 'from-sky-500/18 to-sky-100' },
  { name: 'Distribution', target: 'distribution', accent: 'from-amber-500/18 to-amber-100' },
  { name: 'Fundamentals', target: 'fundamentals', accent: 'from-cyan-500/18 to-cyan-100' },
  { name: 'BanxicoSync', target: 'banxico-sync', accent: 'from-violet-500/18 to-violet-100' },
  { name: 'BanxicoInpc', target: 'banxico-inpc', accent: 'from-orange-500/18 to-orange-100' },
  { name: 'DailySnapshot', target: 'daily-snapshot', accent: 'from-indigo-500/18 to-indigo-100' },
]

export function DashboardPage() {
  const queryClient = useQueryClient()
  const [expandedErrorId, setExpandedErrorId] = useState<string | null>(null)

  const dashboardQuery = useQuery({
    queryKey: ['pipeline-dashboard'],
    queryFn: fetchPipelineDashboard,
    staleTime: 30_000,
    retry: false,
  })

  const invalidateDashboard = async () => {
    await queryClient.invalidateQueries({ queryKey: ['pipeline-dashboard'] })
  }

  const marketMutation = useMutation({
    mutationFn: () => runPipeline('market'),
    onSuccess: invalidateDashboard,
  })

  const newsMutation = useMutation({
    mutationFn: () => runPipeline('news'),
    onSuccess: invalidateDashboard,
  })

  const distributionMutation = useMutation({
    mutationFn: () => runPipeline('distribution'),
    onSuccess: invalidateDashboard,
  })

  const fundamentalsMutation = useMutation({
    mutationFn: () => runPipeline('fundamentals'),
    onSuccess: invalidateDashboard,
  })

  const banxicoSyncMutation = useMutation({
    mutationFn: () => runPipeline('banxico-sync'),
    onSuccess: invalidateDashboard,
  })

  const banxicoInpcMutation = useMutation({
    mutationFn: () => runPipeline('banxico-inpc'),
    onSuccess: invalidateDashboard,
  })

  const dailySnapshotMutation = useMutation({
    mutationFn: () => runPipeline('daily-snapshot'),
    onSuccess: invalidateDashboard,
  })

  const mutationByTarget = {
    market: marketMutation,
    news: newsMutation,
    distribution: distributionMutation,
    fundamentals: fundamentalsMutation,
    'banxico-sync': banxicoSyncMutation,
    'banxico-inpc': banxicoInpcMutation,
    'daily-snapshot': dailySnapshotMutation,
  } as const

  const pipelines = dashboardQuery.data?.pipelines ?? []
  const recentErrors = dashboardQuery.data?.recentErrors ?? []

  return (
    <section className="space-y-6">
      <div className="rounded-[1.75rem] border border-slate-200 bg-[linear-gradient(135deg,_rgba(15,118,110,0.10),_rgba(255,255,255,0.96)_38%,_rgba(15,23,42,0.04))] p-6 shadow-sm">
        <p className="text-xs font-semibold uppercase tracking-[0.28em] text-teal-700">Centro de procesos</p>
        <h2 className="mt-2 text-2xl font-semibold tracking-tight text-slate-950">Dashboard Operativo</h2>
        <p className="mt-3 max-w-3xl text-sm leading-7 text-slate-600">
          Salud de pipelines, últimas ejecuciones y errores globales para disparar diagnósticos y corridas manuales desde un solo lugar.
        </p>
      </div>

      {dashboardQuery.isLoading ? (
        <div className="rounded-3xl border border-border/80 bg-white/85 p-6 text-sm text-muted-foreground shadow-sm">
          Cargando dashboard operativo...
        </div>
      ) : null}

      {dashboardQuery.isError ? (
        <div className="rounded-3xl border border-destructive/30 bg-destructive/5 p-6 text-sm text-destructive shadow-sm">
          {dashboardQuery.error.message}
        </div>
      ) : null}

      {dashboardQuery.isSuccess ? (
        <>
          <div className="grid gap-5 xl:grid-cols-4">
            {pipelineCards.map((card) => {
              const pipeline = pipelines.find((item) => item.pipeline === card.name)
              const mutation = mutationByTarget[card.target]

              return (
                <article
                  className={cn(
                    'overflow-hidden rounded-[1.75rem] border border-slate-200 bg-gradient-to-br p-5 shadow-sm',
                    card.accent,
                  )}
                  key={card.name}
                >
                  <div className="flex items-start justify-between gap-4">
                    <div>
                      <p className="text-xs font-semibold uppercase tracking-[0.24em] text-slate-500">Pipeline</p>
                      <h3 className="mt-2 text-xl font-semibold tracking-tight text-slate-950">{card.name}</h3>
                    </div>
                    <span className={cn('rounded-full px-3 py-1 text-xs font-semibold', getDerivedStatusClass(pipeline?.derivedStatus))}>
                      {pipeline?.derivedStatus ?? 'Sin datos'}
                    </span>
                  </div>

                  <dl className="mt-5 grid grid-cols-2 gap-3 text-sm">
                    <MetricTile label="Última ejecución" value={formatDateTime(pipeline?.lastRunAt ?? null)} />
                    <MetricTile label="Duración" value={formatSeconds(pipeline?.lastDurationSeconds ?? null)} />
                    <MetricTile label="Items" value={formatNumber(pipeline?.lastItemsProcessed ?? null)} />
                    <MetricTile label="Errores" value={formatNumber(pipeline?.lastErrorCount ?? null)} />
                  </dl>

                  <button
                    className="mt-5 inline-flex h-11 items-center justify-center rounded-2xl bg-slate-950 px-4 text-sm font-semibold text-white transition hover:bg-slate-800 disabled:cursor-not-allowed disabled:opacity-60"
                    disabled={mutation.isPending}
                    onClick={() => mutation.mutate()}
                    type="button"
                  >
                    {mutation.isPending ? 'Ejecutando...' : 'Ejecutar ahora'}
                  </button>

                  {mutation.isError ? (
                    <p className="mt-3 text-sm text-destructive">{mutation.error.message}</p>
                  ) : null}

                  <div className="mt-6 rounded-[1.5rem] border border-white/80 bg-white/84 p-4">
                    <h4 className="text-sm font-semibold tracking-tight text-slate-900">Últimas 5 ejecuciones</h4>

                    {pipeline && pipeline.recentRuns.length > 0 ? (
                      <div className="mt-4 overflow-hidden rounded-2xl border border-slate-200">
                        <table className="min-w-full border-collapse text-sm">
                          <thead className="bg-slate-950 text-left text-[11px] uppercase tracking-[0.18em] text-slate-100">
                            <tr>
                              <th className="px-3 py-3 font-medium">Hora</th>
                              <th className="px-3 py-3 font-medium">Estado</th>
                              <th className="px-3 py-3 font-medium">Items</th>
                              <th className="px-3 py-3 font-medium">Errores</th>
                              <th className="px-3 py-3 font-medium">Disparado por</th>
                            </tr>
                          </thead>
                          <tbody className="bg-white">
                            {pipeline.recentRuns.map((run) => (
                              <tr className="border-t border-slate-200 text-slate-700" key={run.id}>
                                <td className="px-3 py-3">{formatDateTime(run.startedAt)}</td>
                                <td className="px-3 py-3">
                                  <span className={cn('rounded-full px-2.5 py-1 text-xs font-semibold', getRunStatusClass(run.status))}>
                                    {run.status}
                                  </span>
                                </td>
                                <td className="px-3 py-3">{formatNumber(run.itemsProcessed ?? null)}</td>
                                <td className="px-3 py-3">{formatNumber(run.errorCount ?? null)}</td>
                                <td className="px-3 py-3">{formatTriggeredBy(run.triggeredBy ?? null)}</td>
                              </tr>
                            ))}
                          </tbody>
                        </table>
                      </div>
                    ) : (
                      <p className="mt-4 text-sm text-muted-foreground">Sin ejecuciones registradas.</p>
                    )}

                    {card.name === 'Fundamentals' && pipeline?.recentRuns[0]?.details ? (
                      <FundamentalsRunSummary details={pipeline.recentRuns[0].details} />
                    ) : null}
                  </div>
                </article>
              )
            })}
          </div>

          <section className="rounded-[1.75rem] border border-slate-200 bg-white/90 p-6 shadow-sm">
            <div className="flex flex-col gap-2 md:flex-row md:items-end md:justify-between">
              <div>
                <p className="text-xs font-semibold uppercase tracking-[0.24em] text-rose-700">Diagnóstico global</p>
                <h3 className="mt-1 text-xl font-semibold tracking-tight text-slate-950">Últimos errores</h3>
                <p className="mt-2 text-sm text-muted-foreground">
                  Haz clic en un error para ver el `AiContext` completo y el contexto JSON formateado.
                </p>
              </div>
              <span className="text-sm font-medium text-slate-500">{recentErrors.length} registros</span>
            </div>

            <div className="mt-5 overflow-hidden rounded-3xl border border-slate-200">
              <div className="divide-y divide-slate-200 bg-white">
                {recentErrors.length === 0 ? (
                  <div className="px-5 py-6 text-sm text-muted-foreground">No hay errores globales recientes.</div>
                ) : (
                  recentErrors.map((error) => {
                    const isExpanded = expandedErrorId === error.id

                    return (
                      <Fragment key={error.id}>
                        <button
                          className="flex w-full flex-col gap-3 px-5 py-4 text-left transition hover:bg-slate-50 md:flex-row md:items-start md:justify-between"
                          onClick={() => setExpandedErrorId(isExpanded ? null : error.id)}
                          type="button"
                        >
                          <div className="min-w-0 flex-1">
                            <div className="flex flex-wrap items-center gap-2">
                              <span className={cn('rounded-full px-2.5 py-1 text-xs font-semibold', getPipelineBadgeClass(error.pipeline))}>
                                {error.pipeline}
                              </span>
                              <span className="text-xs uppercase tracking-[0.18em] text-slate-400">
                                {formatDateTime(error.timestamp)}
                              </span>
                            </div>
                            <p className="mt-3 text-sm font-semibold text-slate-900">{error.message}</p>
                          </div>
                          <span className="text-xs font-medium text-slate-500">{isExpanded ? 'Ocultar detalle' : 'Ver detalle'}</span>
                        </button>

                        {isExpanded ? (
                          <div className="grid gap-4 border-t border-slate-200 bg-slate-50/80 px-5 py-5 lg:grid-cols-[minmax(0,1fr)_minmax(0,1.1fr)]">
                            <div className="rounded-2xl border border-slate-200 bg-slate-950 p-4 text-slate-100">
                              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-400">Context</p>
                              <pre className="mt-3 overflow-x-auto whitespace-pre-wrap text-xs leading-6">
                                {formatJson(error.context ?? null, 'Sin contexto JSON.')}
                              </pre>
                            </div>
                            <div className="rounded-2xl border border-rose-200 bg-white p-4">
                              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-rose-700">AiContext completo</p>
                              <p className="mt-3 whitespace-pre-wrap text-sm leading-7 text-slate-700">{error.aiContext}</p>
                            </div>
                          </div>
                        ) : null}
                      </Fragment>
                    )
                  })
                )}
              </div>
            </div>
          </section>
        </>
      ) : null}
    </section>
  )
}

function MetricTile({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-2xl border border-white/80 bg-white/84 p-3">
      <dt className="text-[11px] font-semibold uppercase tracking-[0.18em] text-slate-400">{label}</dt>
      <dd className="mt-2 text-sm font-semibold text-slate-900">{value}</dd>
    </div>
  )
}

function getDerivedStatusClass(status?: string) {
  switch (status) {
    case 'Completado':
      return 'bg-emerald-100 text-emerald-800'
    case 'Fallando':
      return 'bg-rose-100 text-rose-800'
    default:
      return 'bg-slate-100 text-slate-700'
  }
}

function getRunStatusClass(status: string) {
  switch (status) {
    case 'Completed':
      return 'bg-emerald-100 text-emerald-800'
    case 'Failed':
      return 'bg-rose-100 text-rose-800'
    case 'Queued':
      return 'bg-amber-100 text-amber-800'
    default:
      return 'bg-slate-100 text-slate-700'
  }
}

function getPipelineBadgeClass(pipeline: string) {
  switch (pipeline) {
    case 'Market':
      return 'bg-emerald-100 text-emerald-800'
    case 'News':
      return 'bg-sky-100 text-sky-800'
    case 'Distribution':
      return 'bg-amber-100 text-amber-800'
    case 'Fundamentals':
      return 'bg-cyan-100 text-cyan-800'
    default:
      return 'bg-slate-100 text-slate-700'
  }
}

function FundamentalsRunSummary({ details }: { details: string }) {
  try {
    const parsed = JSON.parse(details) as Partial<Record<string, number>>
    const entries: Array<[string, number | undefined]> = [
      ['Detectados', parsed.reportsDetected],
      ['Nuevos', parsed.newReports],
      ['Skips', parsed.skippedReports],
      ['Possible updates', parsed.possibleUpdates],
      ['Ambiguos', parsed.ambiguousReports],
      ['Errores', parsed.errors],
    ]

    return (
      <div className="mt-4 grid grid-cols-2 gap-2 text-xs text-slate-600">
        {entries.map(([label, value]) => (
          <div className="rounded-xl border border-slate-200 bg-slate-50 px-3 py-2" key={label}>
            <span className="block uppercase tracking-[0.14em] text-slate-400">{label}</span>
            <span className="mt-1 block text-sm font-semibold text-slate-900">{typeof value === 'number' ? value : '—'}</span>
          </div>
        ))}
      </div>
    )
  } catch {
    return null
  }
}

function formatDateTime(value: string | null) {
  if (!value) return '—'

  return new Date(value).toLocaleString('es-MX', {
    dateStyle: 'short',
    timeStyle: 'short',
  })
}

function formatSeconds(value: string | number | null) {
  if (value == null) return '—'

  const numericValue = typeof value === 'number' ? value : Number(value)
  return Number.isFinite(numericValue) ? `${numericValue}s` : '—'
}

function formatNumber(value: string | number | null) {
  if (value == null) return '—'

  const numericValue = typeof value === 'number' ? value : Number(value)
  return Number.isFinite(numericValue) ? numericValue.toLocaleString('es-MX') : '—'
}

function formatTriggeredBy(value: string | null) {
  if (!value) return 'Automático'

  const localPart = value.split('@')[0]?.replace(/[._-]+/g, ' ').trim()
  if (!localPart) return value

  return localPart.replace(/\b\w/g, (letter) => letter.toUpperCase())
}

function formatJson(value: string | null, fallback: string) {
  if (!value) return fallback

  try {
    return JSON.stringify(JSON.parse(value), null, 2)
  } catch {
    return value
  }
}
