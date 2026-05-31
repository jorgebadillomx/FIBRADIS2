import { Fragment, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { fetchAiCallLogs, type AiCallOperation, type AiCallProvider, type AiCallSuccess } from '@/api/aiCallLogsApi'
import { cn } from '@/shared/lib/utils'

const PAGE_SIZE = 50
const operations: AiCallOperation[] = ['all', 'KpiExtraction', 'NewsSummary', 'News', 'Document']
const providers: AiCallProvider[] = ['all', 'Gemini', 'DeepSeek']
const successOptions: { value: AiCallSuccess; label: string }[] = [
  { value: 'all', label: 'Todos' },
  { value: 'true', label: 'Éxito' },
  { value: 'false', label: 'Error' },
]

function SuccessBadge({ success }: { success: boolean }) {
  return (
    <span className={cn('rounded-full px-2.5 py-1 text-xs font-semibold',
      success ? 'bg-emerald-100 text-emerald-800' : 'bg-red-100 text-red-700')}>
      {success ? 'OK' : 'Error'}
    </span>
  )
}

function OperationBadge({ operation }: { operation: string }) {
  const cls = operation === 'KpiExtraction'
    ? 'bg-amber-100 text-amber-800'
    : operation === 'NewsSummary'
      ? 'bg-sky-100 text-sky-800'
      : operation === 'News'
        ? 'bg-sky-100 text-sky-800'
        : 'bg-slate-100 text-slate-700'
  return <span className={cn('rounded-full px-2.5 py-1 text-xs font-semibold', cls)}>{operation}</span>
}

function ProviderBadge({ provider }: { provider: string }) {
  const cls = provider === 'Gemini'
    ? 'bg-teal-100 text-teal-800'
    : provider === 'DeepSeek'
      ? 'bg-violet-100 text-violet-800'
      : 'bg-slate-100 text-slate-700'
  return <span className={cn('rounded-full px-2.5 py-1 text-xs font-semibold', cls)}>{provider}</span>
}

export function AiCallLogsPage() {
  const [operation, setOperation] = useState<AiCallOperation>('all')
  const [provider, setProvider] = useState<AiCallProvider>('all')
  const [success, setSuccess] = useState<AiCallSuccess>('all')
  const [page, setPage] = useState(1)
  const [expandedId, setExpandedId] = useState<string | null>(null)

  const resetPage = () => { setPage(1); setExpandedId(null) }

  const query = useQuery({
    queryKey: ['ai-call-logs', operation, provider, success, page],
    queryFn: () => fetchAiCallLogs(operation, provider, success, page, PAGE_SIZE),
    retry: false,
  })

  const total = Number(query.data?.total ?? 0)
  const totalPages = Math.max(1, Math.ceil(total / PAGE_SIZE))

  return (
    <section className="rounded-2xl border border-border/80 bg-white/90 p-6 shadow-sm">
      <div className="flex flex-col gap-3 md:flex-row md:items-end md:justify-between">
        <div>
          <p className="text-xs font-semibold uppercase tracking-[0.24em] text-teal-700">Diagnóstico</p>
          <h2 className="mt-1 text-lg font-semibold tracking-tight">Llamadas a la IA</h2>
          <p className="mt-2 max-w-3xl text-sm text-muted-foreground">
            Historial de todas las llamadas realizadas al proveedor de IA: operación, duración, resultado y respuesta completa.
          </p>
        </div>

        <div className="flex flex-wrap gap-3">
          <div className="flex flex-col gap-1.5">
            <label className="text-xs font-medium uppercase tracking-[0.18em] text-muted-foreground">Operación</label>
            <select
              aria-label="Filtrar por operación"
              className="h-9 rounded-xl border border-border bg-white px-3 text-sm outline-none transition focus:border-teal-600"
              value={operation}
              onChange={(e) => { setOperation(e.target.value as AiCallOperation); resetPage() }}
            >
              {operations.map((op) => (
                <option key={op} value={op}>{op === 'all' ? 'Todas' : op}</option>
              ))}
            </select>
          </div>
          <div className="flex flex-col gap-1.5">
            <label className="text-xs font-medium uppercase tracking-[0.18em] text-muted-foreground">Proveedor</label>
            <select
              aria-label="Filtrar por proveedor"
              className="h-9 rounded-xl border border-border bg-white px-3 text-sm outline-none transition focus:border-teal-600"
              value={provider}
              onChange={(e) => { setProvider(e.target.value as AiCallProvider); resetPage() }}
            >
              {providers.map((p) => (
                <option key={p} value={p}>{p === 'all' ? 'Todos' : p}</option>
              ))}
            </select>
          </div>
          <div className="flex flex-col gap-1.5">
            <label className="text-xs font-medium uppercase tracking-[0.18em] text-muted-foreground">Estado</label>
            <select
              aria-label="Filtrar por estado"
              className="h-9 rounded-xl border border-border bg-white px-3 text-sm outline-none transition focus:border-teal-600"
              value={success}
              onChange={(e) => { setSuccess(e.target.value as AiCallSuccess); resetPage() }}
            >
              {successOptions.map((opt) => (
                <option key={opt.value} value={opt.value}>{opt.label}</option>
              ))}
            </select>
          </div>
        </div>
      </div>

      <div className="mt-6 overflow-hidden rounded-2xl border border-border/80">
        <table className="min-w-full border-collapse">
          <thead className="bg-teal-950/95 text-left text-xs uppercase tracking-[0.18em] text-teal-50">
            <tr>
              <th className="px-4 py-3 font-medium">Operación</th>
              <th className="px-4 py-3 font-medium">Timestamp</th>
              <th className="px-4 py-3 font-medium">Proveedor / Modelo</th>
              <th className="px-4 py-3 font-medium">Prompt</th>
              <th className="px-4 py-3 font-medium">Duración</th>
              <th className="px-4 py-3 font-medium">Resultado</th>
            </tr>
          </thead>
          <tbody className="bg-white">
            {query.isLoading && (
              <tr><td className="px-4 py-6 text-sm text-muted-foreground" colSpan={6}>Cargando…</td></tr>
            )}
            {query.isError && (
              <tr><td className="px-4 py-6 text-sm text-destructive" colSpan={6}>{query.error.message}</td></tr>
            )}
            {query.isSuccess && query.data.items.length === 0 && (
              <tr><td className="px-4 py-6 text-sm text-muted-foreground" colSpan={6}>Sin registros para el filtro actual.</td></tr>
            )}
            {query.data?.items.map((item) => {
              const isExpanded = expandedId === item.id
              return (
                <Fragment key={item.id}>
                  <tr
                    className="cursor-pointer border-t border-border/70 text-sm transition hover:bg-slate-50"
                    onClick={() => setExpandedId(isExpanded ? null : item.id)}
                  >
                    <td className="px-4 py-4"><OperationBadge operation={item.operation} /></td>
                    <td className="px-4 py-4 text-muted-foreground">
                      {new Date(item.timestamp).toLocaleString('es-MX', { dateStyle: 'short', timeStyle: 'short' })}
                    </td>
                    <td className="px-4 py-4">
                      <ProviderBadge provider={item.provider} />
                      <span className="ml-2 text-xs text-muted-foreground">{item.modelId}</span>
                    </td>
                    <td className="px-4 py-4 text-muted-foreground">{item.promptLength.toLocaleString()} chars</td>
                    <td className="px-4 py-4 text-muted-foreground">{item.durationMs.toLocaleString()} ms</td>
                    <td className="px-4 py-4">
                      <SuccessBadge success={item.success} />
                      {!item.success && item.errorMessage && (
                        <p className="mt-1 max-w-xs text-xs text-red-600">{item.errorMessage.slice(0, 80)}</p>
                      )}
                    </td>
                  </tr>

                  {isExpanded && (
                    <tr className="border-t border-teal-100 bg-teal-50/45">
                      <td className="px-4 py-5" colSpan={6}>
                        <div className="grid gap-4 lg:grid-cols-2">
                          {/* Llamada — JSON exacto enviado a la IA */}
                          <div className="rounded-2xl border border-slate-200 bg-slate-950 p-4 text-slate-100">
                            <p className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-400">
                              Llamada — request HTTP ({item.promptLength.toLocaleString()} chars input)
                            </p>
                            <pre className="mt-3 max-h-96 overflow-auto whitespace-pre-wrap text-xs leading-6">
                              {item.requestRaw
                                ? (() => { try { return JSON.stringify(JSON.parse(item.requestRaw), null, 2) } catch { return item.requestRaw } })()
                                : 'Sin datos de llamada.'}
                            </pre>
                          </div>
                          {/* Respuesta */}
                          <div className="space-y-3">
                            <div className="rounded-2xl border border-slate-200 bg-slate-950 p-4 text-slate-100">
                              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-400">Respuesta IA</p>
                              <pre className="mt-3 max-h-64 overflow-auto whitespace-pre-wrap text-xs leading-6">
                                {item.responseRaw
                                  ? (() => { try { return JSON.stringify(JSON.parse(item.responseRaw), null, 2) } catch { return item.responseRaw } })()
                                  : 'Sin respuesta.'}
                              </pre>
                            </div>
                            {item.errorMessage && (
                              <div className="rounded-2xl border border-red-200 bg-red-50 p-4">
                                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-red-600">Error</p>
                                <p className="mt-2 text-sm text-red-700">{item.errorMessage}</p>
                              </div>
                            )}
                            {item.context && (
                              <div className="rounded-2xl border border-slate-200 bg-white p-4">
                                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-500">Context</p>
                                <pre className="mt-2 overflow-x-auto whitespace-pre-wrap text-xs leading-6 text-slate-700">
                                  {(() => { try { return JSON.stringify(JSON.parse(item.context), null, 2) } catch { return item.context } })()}
                                </pre>
                              </div>
                            )}
                            <div className="rounded-2xl border border-slate-200 bg-white p-4 text-xs text-slate-500 space-y-1">
                              <p><span className="font-medium">ID:</span> {item.id}</p>
                              <p><span className="font-medium">Prompt:</span> {item.promptLength.toLocaleString()} chars</p>
                              <p><span className="font-medium">Duración:</span> {item.durationMs.toLocaleString()} ms</p>
                            </div>
                          </div>
                        </div>
                      </td>
                    </tr>
                  )}
                </Fragment>
              )
            })}
          </tbody>
        </table>
      </div>

      {totalPages > 1 && (
        <div className="mt-4 flex items-center justify-between">
          <p className="text-sm text-muted-foreground">Página {page} de {totalPages} ({total} registros)</p>
          <div className="flex gap-2">
            <button
              type="button"
              disabled={page <= 1}
              onClick={() => setPage((p) => p - 1)}
              className="rounded-lg border border-border px-4 py-2 text-sm font-medium disabled:cursor-not-allowed disabled:opacity-50"
            >
              Anterior
            </button>
            <button
              type="button"
              disabled={page >= totalPages}
              onClick={() => setPage((p) => p + 1)}
              className="rounded-lg border border-border px-4 py-2 text-sm font-medium disabled:cursor-not-allowed disabled:opacity-50"
            >
              Siguiente
            </button>
          </div>
        </div>
      )}
    </section>
  )
}
