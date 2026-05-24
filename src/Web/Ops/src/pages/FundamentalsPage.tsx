import { useState } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import type { FundamentalPreviewDto, FundamentalRecordDto } from '@/api/fundamentalsApi'
import { FundamentalsImportForm } from '@/modules/fundamentals/FundamentalsImportForm'
import { FundamentalsHistory } from '@/modules/fundamentals/FundamentalsHistory'
import { FundamentalsPreview } from '@/modules/fundamentals/FundamentalsPreview'

type Step = 'form' | 'preview'

interface State {
  step: Step
  preview: FundamentalPreviewDto | null
  selectedFibraId: string | null
}

export function FundamentalsPage() {
  const queryClient = useQueryClient()
  const [state, setState] = useState<State>({
    step: 'form',
    preview: null,
    selectedFibraId: null,
  })

  const handlePreview = (preview: FundamentalPreviewDto, fibraId: string) => {
    setState({ step: 'preview', preview, selectedFibraId: fibraId })
  }

  const handleCancel = () => {
    setState((s) => ({ ...s, step: 'form', preview: null }))
  }

  const handleConfirmed = () => {
    setState((s) => {
      if (s.selectedFibraId) {
        queryClient.invalidateQueries({ queryKey: ['fundamentals', s.selectedFibraId] })
      }
      return { ...s, step: 'form', preview: null }
    })
  }

  const handleReprocess = (_record: FundamentalRecordDto) => {
    setState((s) => ({ ...s, step: 'form', preview: null }))
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
            <FundamentalsImportForm onPreview={handlePreview} />
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
            onCancel={handleCancel}
            onConfirmed={handleConfirmed}
          />
        </section>
      )}
    </div>
  )
}
