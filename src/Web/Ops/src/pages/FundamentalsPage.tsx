import { useState } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import type { FundamentalPreviewDto, FundamentalRecordDto } from '@/api/fundamentalsApi'
import { FundamentalsImportForm } from '@/modules/fundamentals/FundamentalsImportForm'
import { FundamentalsHistory } from '@/modules/fundamentals/FundamentalsHistory'
import { FundamentalsPreview } from '@/modules/fundamentals/FundamentalsPreview'
import { KPI_DEFINITIONS } from '@/lib/kpi-definitions'

type Step = 'form' | 'preview'

interface State {
  step: Step
  preview: FundamentalPreviewDto | null
  previewRecord: FundamentalRecordDto | null
  selectedFibraId: string | null
}

export function FundamentalsPage() {
  const queryClient = useQueryClient()
  const [state, setState] = useState<State>({
    step: 'form',
    preview: null,
    previewRecord: null,
    selectedFibraId: null,
  })

  const handlePreview = (preview: FundamentalPreviewDto, fibraId: string) => {
    setState({ step: 'preview', preview, selectedFibraId: fibraId })
  }

  const handleCancel = () => {
    setState((s) => ({ ...s, step: 'form', preview: null, previewRecord: null }))
  }

  const handleConfirmed = () => {
    setState((s) => {
      if (s.selectedFibraId) {
        queryClient.invalidateQueries({ queryKey: ['fundamentals', s.selectedFibraId] })
      }
      return { ...s, step: 'form', preview: null, previewRecord: null }
    })
  }

  const handleReprocess = (record: FundamentalRecordDto) => {
    const kpiFields = ['capRate', 'navPerCbfi', 'ltv', 'noiMargin', 'ffoMargin', 'quarterlyDistribution'] as const
    const presentFields = kpiFields.filter(f => record[f] != null).map(f => KPI_DEFINITIONS[f].label)
    const missingFields = kpiFields.filter(f => record[f] == null).map(f => KPI_DEFINITIONS[f].label)

    const preview: FundamentalPreviewDto = {
      id: record.id,
      fibraTicker: record.fibraTicker,
      period: record.period,
      status: record.status,
      isPossibleUpdate: record.isPossibleUpdate,
      warningMessage: null,
      presentFields,
      missingFields,
      pdfReference: record.pdfReference ?? null,
      capturedAt: record.capturedAt,
      hasMarkdownContent: record.hasMarkdownContent,
    }

    setState((s) => ({ ...s, step: 'preview', preview, previewRecord: record }))
  }

  return (
    <div className="space-y-8">
      <div>
        <h1 className="text-xl font-semibold tracking-tight text-slate-900">
          Fundamentales — Importación Manual
        </h1>
        <p className="mt-1 text-sm text-slate-500">
          Importa, revisa y confirma datos financieros trimestrales por FIBRA.
        </p>
      </div>

      {state.step === 'form' && (
        <div className="space-y-8">
          <section className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm">
            <h2 className="mb-4 text-sm font-semibold uppercase tracking-wide text-slate-500">
              Nuevo registro
            </h2>
            <FundamentalsImportForm
              onPreview={handlePreview}
              onFibraChange={(fibraId) => setState((s) => ({ ...s, selectedFibraId: fibraId }))}
            />
          </section>

          {state.selectedFibraId && (
            <section className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm">
              <h2 className="mb-4 text-sm font-semibold uppercase tracking-wide text-slate-500">
                Historial — {state.selectedFibraId}
              </h2>
              <FundamentalsHistory
                fibraId={state.selectedFibraId}
                onReprocess={handleReprocess}
              />
            </section>
          )}
        </div>
      )}

      {state.step === 'preview' && state.preview && (
        <section className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm">
          <h2 className="mb-4 text-sm font-semibold uppercase tracking-wide text-slate-500">
            Preview del registro
          </h2>
          <FundamentalsPreview
            preview={state.preview}
            record={state.previewRecord ?? undefined}
            onCancel={handleCancel}
            onConfirmed={handleConfirmed}
          />
        </section>
      )}
    </div>
  )
}
