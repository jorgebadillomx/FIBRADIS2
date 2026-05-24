import { useRef } from 'react'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import type { FundamentalRecordDto } from '@/api/fundamentalsApi'
import { downloadFundamentalPdf, fetchFundamentalsByFibra, uploadFundamentalPdf } from '@/api/fundamentalsApi'

interface Props {
  fibraId: string
  onReprocess: (record: FundamentalRecordDto) => void
}

export function FundamentalsHistory({ fibraId, onReprocess }: Props) {
  const queryClient = useQueryClient()
  const fileInputRefs = useRef<Record<string, HTMLInputElement | null>>({})

  const { data: records = [], isLoading } = useQuery({
    queryKey: ['fundamentals', fibraId],
    queryFn: () => fetchFundamentalsByFibra(fibraId),
    enabled: !!fibraId,
  })

  const handlePdfUpload = async (id: string, file: File) => {
    try {
      await uploadFundamentalPdf(id, file)
      queryClient.invalidateQueries({ queryKey: ['fundamentals', fibraId] })
    } catch (e) {
      console.error('Error al subir PDF:', e)
    }
  }

  const handlePdfDownload = async (id: string, ticker: string) => {
    try {
      const blob = await downloadFundamentalPdf(id)
      const url = URL.createObjectURL(blob)
      const a = document.createElement('a')
      a.href = url
      a.download = `fundamentales-${ticker}-${id}.pdf`
      a.click()
      URL.revokeObjectURL(url)
    } catch (e) {
      console.error('Error al descargar PDF:', e)
    }
  }

  if (isLoading) {
    return <p className="text-sm text-slate-400 py-4">Cargando historial…</p>
  }

  if (records.length === 0) {
    return <p className="text-sm text-slate-400 py-4">Sin registros para esta FIBRA.</p>
  }

  return (
    <div className="overflow-x-auto">
      <table className="w-full text-sm">
        <thead>
          <tr className="border-b border-slate-200 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">
            <th className="py-2 pr-3">Período</th>
            <th className="py-2 pr-3">Estado</th>
            <th className="py-2 pr-3">Cap Rate</th>
            <th className="py-2 pr-3">NAV</th>
            <th className="py-2 pr-3">LTV</th>
            <th className="py-2 pr-3">NOI</th>
            <th className="py-2 pr-3">FFO</th>
            <th className="py-2 pr-3">Dist. Trim.</th>
            <th className="py-2 pr-3">Importado por</th>
            <th className="py-2 pr-3">Fecha</th>
            <th className="py-2">Acciones</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-slate-100">
          {records.map((r) => (
            <tr key={r.id} className="hover:bg-slate-50 transition-colors">
              <td className="py-2 pr-3 font-mono font-medium">{r.period}</td>
              <td className="py-2 pr-3">
                <RecordStatusBadge status={r.status} isPossibleUpdate={r.isPossibleUpdate} />
              </td>
              <td className="py-2 pr-3 tabular-nums">{r.capRate ?? '—'}</td>
              <td className="py-2 pr-3 tabular-nums">{r.navPerCbfi ?? '—'}</td>
              <td className="py-2 pr-3 tabular-nums">{r.ltv ?? '—'}</td>
              <td className="py-2 pr-3 tabular-nums">{r.noiMargin ?? '—'}</td>
              <td className="py-2 pr-3 tabular-nums">{r.ffoMargin ?? '—'}</td>
              <td className="py-2 pr-3 tabular-nums">{r.quarterlyDistribution ?? '—'}</td>
              <td className="py-2 pr-3 text-slate-500">{r.importedBy ?? '—'}</td>
              <td className="py-2 pr-3 text-slate-500 whitespace-nowrap">
                {new Date(r.capturedAt).toLocaleDateString('es-MX')}
              </td>
              <td className="py-2">
                <div className="flex gap-2">
                  {(r.status === 'processed' || r.status === 'partial') && (
                    <button
                      onClick={() => onReprocess(r)}
                      className="rounded-lg bg-orange-100 px-2 py-1 text-xs font-medium text-orange-700 hover:bg-orange-200 transition"
                    >
                      Reprocess
                    </button>
                  )}
                  {r.pdfReference ? (
                    <button
                      type="button"
                      onClick={() => handlePdfDownload(r.id, r.fibraTicker)}
                      className="rounded-lg bg-blue-100 px-2 py-1 text-xs font-medium text-blue-700 hover:bg-blue-200 transition"
                    >
                      Ver PDF
                    </button>
                  ) : (
                    <>
                      <input
                        ref={(el) => { fileInputRefs.current[r.id] = el }}
                        type="file"
                        accept="application/pdf"
                        className="hidden"
                        onChange={(e) => {
                          const file = e.target.files?.[0]
                          if (file) handlePdfUpload(r.id, file)
                        }}
                      />
                      <button
                        onClick={() => fileInputRefs.current[r.id]?.click()}
                        className="rounded-lg bg-slate-100 px-2 py-1 text-xs font-medium text-slate-600 hover:bg-slate-200 transition"
                      >
                        Subir PDF
                      </button>
                    </>
                  )}
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

function RecordStatusBadge({ status, isPossibleUpdate }: { status: string; isPossibleUpdate: boolean }) {
  if (isPossibleUpdate && status !== 'processed') {
    return (
      <span className="rounded-full bg-orange-100 px-2.5 py-0.5 text-xs font-medium text-orange-700">
        posible update
      </span>
    )
  }
  const map: Record<string, string> = {
    processed: 'bg-green-100 text-green-700',
    partial: 'bg-yellow-100 text-yellow-700',
    pending: 'bg-slate-100 text-slate-600',
    error: 'bg-red-100 text-red-700',
  }
  return (
    <span className={`rounded-full px-2.5 py-0.5 text-xs font-medium ${map[status] ?? 'bg-slate-100 text-slate-600'}`}>
      {status}
    </span>
  )
}
