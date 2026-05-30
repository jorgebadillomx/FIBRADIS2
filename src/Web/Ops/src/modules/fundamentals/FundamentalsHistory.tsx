import { useRef, useState } from 'react'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import type { FundamentalRecordDto } from '@/api/fundamentalsApi'
import {
  downloadFundamentalPdf,
  fetchFundamentalsByFibra,
  uploadFundamentalPdf,
} from '@/api/fundamentalsApi'
import { KPI_DEFINITIONS, type KpiKey } from '@/lib/kpi-definitions'
import { EditFieldNotesDialog } from '@/modules/fundamentals/EditFieldNotesDialog'

interface Props {
  fibraId: string
  onEdit: (record: FundamentalRecordDto) => void
}

export function FundamentalsHistory({ fibraId, onEdit }: Props) {
  const queryClient = useQueryClient()
  const fileInputRefs = useRef<Record<string, HTMLInputElement | null>>({})
  const [pdfDownloadErrors, setPdfDownloadErrors] = useState<Record<string, string>>({})
  const [notesRecord, setNotesRecord] = useState<FundamentalRecordDto | null>(null)

  const { data: records = [], isLoading } = useQuery({
    queryKey: ['fundamentals', fibraId],
    queryFn: () => fetchFundamentalsByFibra(fibraId),
    enabled: !!fibraId,
  })

  const handlePdfUpload = async (id: string, file: File) => {
    await uploadFundamentalPdf(id, file)
    queryClient.invalidateQueries({ queryKey: ['fundamentals', fibraId] })
  }

  const handlePdfDownload = async (id: string, ticker: string) => {
    setPdfDownloadErrors((prev) => { const next = { ...prev }; delete next[id]; return next })
    try {
      const blob = await downloadFundamentalPdf(id)
      const url = URL.createObjectURL(blob)
      const a = document.createElement('a')
      a.href = url
      a.download = `fundamentales-${ticker}-${id}.pdf`
      a.click()
      URL.revokeObjectURL(url)
    } catch (e) {
      const msg = e instanceof Error ? e.message : 'Error al descargar el PDF'
      setPdfDownloadErrors((prev) => ({ ...prev, [id]: msg }))
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
            <th className="py-2 pr-3"><KpiHeader kpiKey="capRate" /></th>
            <th className="py-2 pr-3"><KpiHeader kpiKey="navPerCbfi" /></th>
            <th className="py-2 pr-3"><KpiHeader kpiKey="ltv" /></th>
            <th className="py-2 pr-3"><KpiHeader kpiKey="noiMargin" /></th>
            <th className="py-2 pr-3"><KpiHeader kpiKey="ffoMargin" /></th>
            <th className="py-2 pr-3"><KpiHeader kpiKey="quarterlyDistribution" /></th>
            <th className="py-2 pr-3">MD</th>
            <th className="py-2 pr-3">Importado por</th>
            <th className="py-2 pr-3">Fecha</th>
            <th className="py-2">Acciones</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-slate-100">
          {records.map((r) => (
            <HistoryRow
              key={r.id}
              record={r}
              fibraId={fibraId}
              fileInputRefs={fileInputRefs}
              pdfDownloadError={pdfDownloadErrors[r.id]}
              onPdfUpload={handlePdfUpload}
              onPdfDownload={handlePdfDownload}
              onEdit={onEdit}
              onEditNotes={setNotesRecord}
            />
          ))}
        </tbody>
      </table>
      <EditFieldNotesDialog
        fibraId={fibraId}
        record={notesRecord}
        open={notesRecord !== null}
        onClose={() => setNotesRecord(null)}
      />
    </div>
  )
}

interface RowProps {
  record: FundamentalRecordDto
  fibraId: string
  fileInputRefs: React.MutableRefObject<Record<string, HTMLInputElement | null>>
  pdfDownloadError?: string
  onPdfUpload: (id: string, file: File) => Promise<void>
  onPdfDownload: (id: string, ticker: string) => Promise<void>
  onEdit: (record: FundamentalRecordDto) => void
  onEditNotes: (record: FundamentalRecordDto) => void
}

const MAX_PDF_BYTES = 20 * 1024 * 1024

function HistoryRow({
  record: r,
  fileInputRefs,
  pdfDownloadError,
  onPdfUpload,
  onPdfDownload,
  onEdit,
  onEditNotes,
}: RowProps) {
  const [pdfUploadError, setPdfUploadError] = useState<string | null>(null)
  const isDeleted = !!r.deletedAt
  const canEditNotes = !isDeleted && (r.status === 'processed' || r.status === 'partial')

  return (
    <tr className={`transition-colors ${isDeleted ? 'opacity-50 bg-slate-50' : 'hover:bg-slate-50'}`}>
      <td className="py-2 pr-3 font-mono font-medium">
        <span className={isDeleted ? 'line-through text-slate-400' : ''}>{r.period}</span>
      </td>
      <td className="py-2 pr-3">
        {isDeleted
          ? <span className="rounded-full bg-slate-200 px-2.5 py-0.5 text-xs font-medium text-slate-500">archivado</span>
          : <RecordStatusBadge status={r.status} isPossibleUpdate={r.isPossibleUpdate} />
        }
      </td>
      <td className="py-2 pr-3 tabular-nums"><KpiCell kpiKey="capRate" value={r.capRate} note={r.fieldNotes?.capRate} /></td>
      <td className="py-2 pr-3 tabular-nums"><KpiCell kpiKey="navPerCbfi" value={r.navPerCbfi} note={r.fieldNotes?.navPerCbfi} /></td>
      <td className="py-2 pr-3 tabular-nums"><KpiCell kpiKey="ltv" value={r.ltv} note={r.fieldNotes?.ltv} /></td>
      <td className="py-2 pr-3 tabular-nums"><KpiCell kpiKey="noiMargin" value={r.noiMargin} note={r.fieldNotes?.noiMargin} /></td>
      <td className="py-2 pr-3 tabular-nums"><KpiCell kpiKey="ffoMargin" value={r.ffoMargin} note={r.fieldNotes?.ffoMargin} /></td>
      <td className="py-2 pr-3 tabular-nums"><KpiCell kpiKey="quarterlyDistribution" value={r.quarterlyDistribution} note={r.fieldNotes?.quarterlyDistribution} /></td>
      <td className="py-2 pr-3">
        {r.hasMarkdownContent ? (
          <span className="rounded-full bg-green-100 px-2 py-0.5 text-xs font-medium text-green-700">MD</span>
        ) : (
          <span className="text-slate-300 text-xs">—</span>
        )}
      </td>
      <td className="py-2 pr-3 text-slate-500">{r.importedBy ?? '—'}</td>
      <td className="py-2 pr-3 text-slate-500 whitespace-nowrap">
        {new Date(r.capturedAt).toLocaleDateString('es-MX')}
      </td>
      <td className="py-2">
        <div className="flex flex-wrap gap-2">
          {!isDeleted && (
            <button
              type="button"
              onClick={() => onEdit(r)}
              className="rounded-lg bg-slate-100 px-2 py-1 text-xs font-medium text-slate-600 hover:bg-slate-200 transition"
            >
              Editar
            </button>
          )}

          {canEditNotes && (
            <button
              type="button"
              onClick={() => onEditNotes(r)}
              className="rounded-lg bg-violet-100 px-2 py-1 text-xs font-medium text-violet-700 hover:bg-violet-200 transition"
            >
              Notas
            </button>
          )}

          {r.pdfReference ? (
            <button
              type="button"
              onClick={() => onPdfDownload(r.id, r.fibraTicker)}
              className="rounded-lg bg-blue-100 px-2 py-1 text-xs font-medium text-blue-700 hover:bg-blue-200 transition"
            >
              Ver PDF
            </button>
          ) : !isDeleted ? (
            <>
              <input
                ref={(el) => { fileInputRefs.current[r.id] = el }}
                type="file"
                accept="application/pdf"
                aria-label="Seleccionar PDF para subir"
                className="hidden"
                onChange={(e) => {
                  const file = e.target.files?.[0]
                  if (!file) return
                  if (file.size > MAX_PDF_BYTES) {
                    setPdfUploadError('El archivo excede el límite de 20 MB.')
                    return
                  }
                  setPdfUploadError(null)
                  onPdfUpload(r.id, file).catch((err: unknown) => {
                    setPdfUploadError(err instanceof Error ? err.message : 'Error al subir PDF')
                  })
                }}
              />
              <button
                type="button"
                onClick={() => fileInputRefs.current[r.id]?.click()}
                className="rounded-lg bg-slate-100 px-2 py-1 text-xs font-medium text-slate-600 hover:bg-slate-200 transition"
              >
                Subir PDF
              </button>
            </>
          ) : null}
        </div>
        {pdfUploadError && (
          <p className="mt-1 text-xs text-red-500">{pdfUploadError}</p>
        )}
        {pdfDownloadError && (
          <p className="mt-1 text-xs text-red-500">{pdfDownloadError}</p>
        )}
      </td>
    </tr>
  )
}

function KpiHeader({ kpiKey }: { kpiKey: KpiKey }) {
  const definition = KPI_DEFINITIONS[kpiKey]

  return (
    <span className="inline-flex items-center gap-1">
      <span>{definition.label}</span>
      <span className="cursor-help select-none text-[10px] text-slate-400" title={definition.formula}>
        ⓘ
      </span>
    </span>
  )
}

function KpiCell({ kpiKey, value, note }: { kpiKey: KpiKey; value: number | string | null; note?: string }) {
  if (value == null) {
    return <span>—</span>
  }

  return (
    <span className="inline-flex items-center gap-1">
      <span>{value}</span>
      {note && (
        <span
          className="cursor-help select-none text-[10px] text-slate-400"
          title={note}
          aria-label={`Nota IA de ${KPI_DEFINITIONS[kpiKey].label}`}
        >
          ⓘ
        </span>
      )}
    </span>
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
