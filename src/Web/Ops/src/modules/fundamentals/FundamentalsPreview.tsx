import { useMutation } from '@tanstack/react-query'
import type { FundamentalPreviewDto, FundamentalRecordDto } from '@/api/fundamentalsApi'
import { confirmFundamentals } from '@/api/fundamentalsApi'
import { KPI_DEFINITIONS, type KpiKey } from '@/lib/kpi-definitions'

interface Props {
  preview: FundamentalPreviewDto
  record?: FundamentalRecordDto
  onCancel: () => void
  onConfirmed: () => void
}

const KPI_KEYS: KpiKey[] = ['capRate', 'navPerCbfi', 'ltv', 'noiMargin', 'ffoMargin', 'quarterlyDistribution']

function formatKpi(value: number | string | null | undefined): string {
  if (value == null) return '—'
  const n = typeof value === 'string' ? parseFloat(value) : value
  if (isNaN(n)) return String(value)
  return n.toLocaleString('es-MX', { maximumFractionDigits: 6 })
}

export function FundamentalsPreview({ preview, record, onCancel, onConfirmed }: Props) {
  const confirm = useMutation({
    mutationFn: () => confirmFundamentals(preview.id),
    onSuccess: () => {
      onConfirmed()
    },
  })

  return (
    <div className="space-y-4">
      <div className="flex items-center gap-3 flex-wrap">
        <span className="text-lg font-semibold">{preview.fibraTicker}</span>
        <span className="rounded-full bg-slate-100 px-2.5 py-0.5 text-xs font-medium text-slate-600">{preview.period}</span>
        <StatusBadge status={preview.status} />
        {preview.isPossibleUpdate && (
          <span className="rounded-full bg-orange-100 px-2.5 py-0.5 text-xs font-semibold text-orange-700">
            Actualización
          </span>
        )}
        {preview.hasMarkdownContent && (
          <span className="rounded-full bg-green-100 px-2.5 py-0.5 text-xs font-medium text-green-700">
            MD disponible
          </span>
        )}
      </div>

      {preview.warningMessage && (
        <div className="rounded-lg border border-orange-300 bg-orange-50 px-4 py-3 text-sm text-orange-800">
          {preview.warningMessage}
        </div>
      )}

      {/* KPIs */}
      {record && (
        <div className="rounded-xl border border-slate-200 bg-slate-50 overflow-hidden">
          <table className="w-full text-sm">
            <thead>
              <tr className="bg-slate-100 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">
                <th className="px-4 py-2">KPI</th>
                <th className="px-4 py-2 text-right">Valor</th>
                <th className="px-4 py-2 text-left">Nota IA</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-200">
              {KPI_KEYS.map((key) => {
                const def = KPI_DEFINITIONS[key]
                const value = record[key]
                const note = record.fieldNotes?.[key]
                return (
                  <tr key={key} className={value == null ? 'opacity-40' : ''}>
                    <td className="px-4 py-2 font-medium text-slate-700" title={def.formula}>
                      {def.label}
                    </td>
                    <td className="px-4 py-2 text-right tabular-nums font-mono">
                      {formatKpi(value)}
                    </td>
                    <td className="px-4 py-2 text-xs text-slate-500 italic">
                      {note ?? '—'}
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
      )}

      {/* Summary */}
      {record?.summary && (
        <div className="rounded-xl border border-slate-200 bg-white px-4 py-3">
          <p className="text-xs font-semibold uppercase tracking-wide text-slate-500 mb-1">Resumen</p>
          <p className="text-sm text-slate-700 leading-relaxed">{record.summary}</p>
        </div>
      )}

      {/* extraction error note */}
      {record?.fieldNotes?.extractionNotes && (
        <div className="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
          Error IA: {record.fieldNotes.extractionNotes}
        </div>
      )}

      {/* missing fields */}
      {preview.missingFields.length > 0 && (
        <div>
          <p className="text-xs font-semibold uppercase tracking-wide text-slate-500 mb-2">Campos faltantes</p>
          <div className="flex flex-wrap gap-2">
            {preview.missingFields.map((f) => (
              <span key={f} className="rounded-full bg-slate-100 px-2.5 py-0.5 text-xs font-medium text-slate-400">
                {f}
              </span>
            ))}
          </div>
        </div>
      )}

      {confirm.isError && (
        <p className="rounded-lg bg-red-50 border border-red-200 px-4 py-3 text-sm text-red-700">
          {confirm.error instanceof Error ? confirm.error.message : 'Error al confirmar'}
        </p>
      )}

      <div className="flex gap-3 pt-2">
        <button
          type="button"
          onClick={() => confirm.mutate()}
          disabled={confirm.isPending}
          className="flex-1 rounded-xl bg-teal-600 px-4 py-2.5 text-sm font-semibold text-white hover:bg-teal-700 disabled:opacity-50 transition"
        >
          {confirm.isPending ? 'Confirmando…' : preview.isPossibleUpdate ? 'Reprocess' : 'Confirmar'}
        </button>
        <button
          type="button"
          onClick={onCancel}
          disabled={confirm.isPending}
          className="rounded-xl border border-slate-200 px-4 py-2.5 text-sm font-semibold text-slate-700 hover:bg-slate-50 disabled:opacity-50 transition"
        >
          Cancelar
        </button>
      </div>
    </div>
  )
}

function StatusBadge({ status }: { status: string }) {
  const map: Record<string, string> = {
    pending: 'bg-slate-100 text-slate-600',
    partial: 'bg-yellow-100 text-yellow-700',
    processed: 'bg-green-100 text-green-700',
  }
  return (
    <span className={`rounded-full px-2.5 py-0.5 text-xs font-medium ${map[status] ?? 'bg-slate-100 text-slate-600'}`}>
      {status}
    </span>
  )
}
