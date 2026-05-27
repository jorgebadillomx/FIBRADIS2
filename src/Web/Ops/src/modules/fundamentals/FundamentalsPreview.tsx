import { useMutation } from '@tanstack/react-query'
import type { FundamentalPreviewDto } from '@/api/fundamentalsApi'
import { confirmFundamentals } from '@/api/fundamentalsApi'

interface Props {
  preview: FundamentalPreviewDto
  onCancel: () => void
  onConfirmed: () => void
}

export function FundamentalsPreview({ preview, onCancel, onConfirmed }: Props) {
  const confirm = useMutation({
    mutationFn: () => confirmFundamentals(preview.id),
    onSuccess: () => {
      onConfirmed()
    },
  })

  return (
    <div className="space-y-4">
      <div className="flex items-center gap-3">
        <span className="text-lg font-semibold">{preview.fibraTicker}</span>
        <span className="rounded-full bg-slate-100 px-2.5 py-0.5 text-xs font-medium text-slate-600">{preview.period}</span>
        <StatusBadge status={preview.status} />
        {preview.isPossibleUpdate && (
          <span className="rounded-full bg-orange-100 px-2.5 py-0.5 text-xs font-semibold text-orange-700">
            Actualización
          </span>
        )}
      </div>

      {preview.hasMarkdownContent && (
        <span className="rounded-full bg-green-100 px-2.5 py-0.5 text-xs font-medium text-green-700">
          MD disponible
        </span>
      )}

      {preview.warningMessage && (
        <div className="rounded-lg border border-orange-300 bg-orange-50 px-4 py-3 text-sm text-orange-800">
          {preview.warningMessage}
        </div>
      )}

      {preview.presentFields.length > 0 && (
        <div>
          <p className="text-xs font-semibold uppercase tracking-wide text-slate-500 mb-2">Campos presentes</p>
          <div className="flex flex-wrap gap-2">
            {preview.presentFields.map((f) => (
              <span key={f} className="rounded-full bg-teal-100 px-2.5 py-0.5 text-xs font-medium text-teal-800">
                {f}
              </span>
            ))}
          </div>
        </div>
      )}

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
