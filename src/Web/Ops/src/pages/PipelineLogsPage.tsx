import { Fragment, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { fetchPipelineLogs, type PipelineLogPipeline } from '@/api/pipelineLogsApi'
import { cn } from '@/shared/lib/utils'

const pageSize = 50
const pipelines: PipelineLogPipeline[] = ['all', 'Market', 'News', 'Distribution', 'BodyTextRetry']

function getPipelineBadgeClass(pipeline: string) {
  switch (pipeline) {
    case 'Market':
      return 'bg-emerald-100 text-emerald-800'
    case 'News':
      return 'bg-sky-100 text-sky-800'
    case 'Distribution':
      return 'bg-amber-100 text-amber-800'
    case 'BodyTextRetry':
      return 'bg-violet-100 text-violet-800'
    default:
      return 'bg-slate-100 text-slate-700'
  }
}

export function PipelineLogsPage() {
  const [pipeline, setPipeline] = useState<PipelineLogPipeline>('all')
  const [page, setPage] = useState(1)
  const [expandedId, setExpandedId] = useState<string | null>(null)

  const logsQuery = useQuery({
    queryKey: ['pipeline-logs', pipeline, page, pageSize],
    queryFn: () => fetchPipelineLogs(pipeline, page, pageSize),
    retry: false,
  })

  const total = Number(logsQuery.data?.total ?? 0)
  const totalPages = Math.max(1, Math.ceil(total / pageSize))

  return (
    <section className="rounded-2xl border border-border/80 bg-white/90 p-6 shadow-sm">
      <div className="flex flex-col gap-3 md:flex-row md:items-end md:justify-between">
        <div>
          <p className="text-xs font-semibold uppercase tracking-[0.24em] text-teal-700">Diagnóstico</p>
          <h2 className="mt-1 text-lg font-semibold tracking-tight">Logs del pipeline</h2>
          <p className="mt-2 max-w-3xl text-sm text-muted-foreground">
            Errores estructurados con contexto operativo y texto legible por IA para análisis rápido desde Ops.
          </p>
        </div>

        <div className="flex flex-col gap-2">
          <label className="text-xs font-medium uppercase tracking-[0.18em] text-muted-foreground">
            Pipeline
          </label>
          <select
            className="h-11 rounded-xl border border-border bg-white px-4 text-sm outline-none transition focus:border-teal-600"
            onChange={(event) => {
              setPipeline(event.target.value as PipelineLogPipeline)
              setPage(1)
              setExpandedId(null)
            }}
            value={pipeline}
          >
            {pipelines.map((value) => (
              <option key={value} value={value}>
                {value === 'all' ? 'Todos' : value}
              </option>
            ))}
          </select>
        </div>
      </div>

      <div className="mt-6 overflow-hidden rounded-2xl border border-border/80">
        <table className="min-w-full border-collapse">
          <thead className="bg-teal-950/95 text-left text-xs uppercase tracking-[0.18em] text-teal-50">
            <tr>
              <th className="px-4 py-3 font-medium">Pipeline</th>
              <th className="px-4 py-3 font-medium">Timestamp</th>
              <th className="px-4 py-3 font-medium">ErrorType</th>
              <th className="px-4 py-3 font-medium">Message</th>
              <th className="px-4 py-3 font-medium">AiContext</th>
            </tr>
          </thead>
          <tbody className="bg-white">
            {logsQuery.isLoading ? (
              <tr>
                <td className="px-4 py-6 text-sm text-muted-foreground" colSpan={5}>
                  Cargando logs...
                </td>
              </tr>
            ) : null}

            {logsQuery.isError ? (
              <tr>
                <td className="px-4 py-6 text-sm text-destructive" colSpan={5}>
                  {logsQuery.error.message}
                </td>
              </tr>
            ) : null}

            {logsQuery.isSuccess && logsQuery.data.items.length === 0 ? (
              <tr>
                <td className="px-4 py-6 text-sm text-muted-foreground" colSpan={5}>
                  No hay errores registrados para el filtro actual.
                </td>
              </tr>
            ) : null}

            {logsQuery.data?.items.map((item) => {
              const isExpanded = expandedId === item.id
              return (
                <Fragment key={item.id}>
                  <tr
                    className="cursor-pointer border-t border-border/70 text-sm transition hover:bg-slate-50"
                    onClick={() => setExpandedId(isExpanded ? null : item.id)}
                  >
                    <td className="px-4 py-4">
                      <span className={cn('rounded-full px-2.5 py-1 text-xs font-semibold', getPipelineBadgeClass(item.pipeline))}>
                        {item.pipeline}
                      </span>
                    </td>
                    <td className="px-4 py-4 text-muted-foreground">
                      {new Date(item.timestamp).toLocaleString('es-MX', {
                        dateStyle: 'short',
                        timeStyle: 'short',
                      })}
                    </td>
                    <td className="px-4 py-4 font-medium text-slate-800">{item.errorType}</td>
                    <td className="max-w-sm px-4 py-4 text-muted-foreground">{item.message.slice(0, 80)}</td>
                    <td className="max-w-md px-4 py-4 text-slate-700">
                      {item.aiContext.slice(0, 120)}
                      {item.aiContext.length > 120 ? '…' : ''}
                    </td>
                  </tr>

                  {isExpanded ? (
                    <tr className="border-t border-teal-100 bg-teal-50/45">
                      <td className="px-4 py-5" colSpan={5}>
                        <div className="grid gap-4 lg:grid-cols-[minmax(0,1fr)_minmax(0,1.2fr)]">
                          <div className="rounded-2xl border border-slate-200 bg-slate-950 p-4 text-slate-100">
                            <p className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-400">Context JSON</p>
                            <pre className="mt-3 overflow-x-auto whitespace-pre-wrap text-xs leading-6">
                              {item.context
                                ? (() => { try { return JSON.stringify(JSON.parse(item.context), null, 2) } catch { return item.context } })()
                                : 'Sin contexto JSON.'}
                            </pre>
                          </div>
                          <div className="rounded-2xl border border-teal-200 bg-white p-4">
                            <p className="text-xs font-semibold uppercase tracking-[0.18em] text-teal-700">AiContext completo</p>
                            <p className="mt-3 text-sm leading-7 text-slate-700">{item.aiContext}</p>
                          </div>
                        </div>
                      </td>
                    </tr>
                  ) : null}
                </Fragment>
              )
            })}
          </tbody>
        </table>
      </div>

      {totalPages > 1 ? (
        <div className="mt-4 flex items-center justify-between">
          <p className="text-sm text-muted-foreground">
            Página {page} de {totalPages} ({total} registros)
          </p>
          <div className="flex gap-2">
            <button
              className="rounded-lg border border-border px-4 py-2 text-sm font-medium disabled:cursor-not-allowed disabled:opacity-50"
              disabled={page <= 1}
              onClick={() => setPage((current) => current - 1)}
              type="button"
            >
              Anterior
            </button>
            <button
              className="rounded-lg border border-border px-4 py-2 text-sm font-medium disabled:cursor-not-allowed disabled:opacity-50"
              disabled={page >= totalPages}
              onClick={() => setPage((current) => current + 1)}
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
