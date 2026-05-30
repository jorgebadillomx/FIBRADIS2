import { useState, useCallback } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import type { FundamentalPreviewDto, FundamentalRecordDto } from '@/api/fundamentalsApi'
import { FundamentalsImportForm } from '@/modules/fundamentals/FundamentalsImportForm'
import { FundamentalsHistory } from '@/modules/fundamentals/FundamentalsHistory'

interface State {
  editRecord: FundamentalRecordDto | null
  selectedFibraId: string | null
}

export function FundamentalsPage() {
  const queryClient = useQueryClient()
  const [state, setState] = useState<State>({
    editRecord: null,
    selectedFibraId: null,
  })

  const handleDone = useCallback((_preview: FundamentalPreviewDto, fibraId: string) => {
    if (fibraId) queryClient.invalidateQueries({ queryKey: ['fundamentals', fibraId] })
    setState((s) => {
      if (s.selectedFibraId) queryClient.invalidateQueries({ queryKey: ['fundamentals', s.selectedFibraId] })
      return { ...s, editRecord: null }
    })
  }, [queryClient])

  const handleEdit = useCallback((record: FundamentalRecordDto) => {
    setState((s) => ({ ...s, editRecord: record }))
    window.scrollTo({ top: 0, behavior: 'smooth' })
  }, [])

  const handleFibraChange = useCallback((fibraId: string) => {
    setState((s) => s.selectedFibraId === fibraId ? s : { ...s, selectedFibraId: fibraId })
  }, [])

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

      <section className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm">
        <h2 className="mb-4 text-sm font-semibold uppercase tracking-wide text-slate-500">
          {state.editRecord
            ? `Editando — ${state.editRecord.fibraTicker} ${state.editRecord.period}`
            : 'Nuevo registro'}
        </h2>
        <FundamentalsImportForm
          onPreview={handleDone}
          onFibraChange={handleFibraChange}
          initialRecord={state.editRecord ?? undefined}
          initialFibraId={state.selectedFibraId ?? undefined}
        />
      </section>

      {state.selectedFibraId && (
        <section className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm">
          <h2 className="mb-4 text-sm font-semibold uppercase tracking-wide text-slate-500">
            Historial — {state.selectedFibraId}
          </h2>
          <FundamentalsHistory
            fibraId={state.selectedFibraId}
            onEdit={handleEdit}
          />
        </section>
      )}
    </div>
  )
}
