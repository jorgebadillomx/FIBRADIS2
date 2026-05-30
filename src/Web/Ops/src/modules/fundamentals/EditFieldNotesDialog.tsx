import { useEffect, useMemo, useRef, useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import type { FundamentalRecordDto, PatchFieldNotesRequest } from '@/api/fundamentalsApi'
import { patchFieldNotes } from '@/api/fundamentalsApi'
import { KPI_DEFINITIONS } from '@/lib/kpi-definitions'
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/shared/ui/dialog'

interface Props {
  fibraId: string
  record: FundamentalRecordDto | null
  open: boolean
  onClose: () => void
}

const FIELD_NOTE_KEYS = [
  ['capRate', 'capRateNote'],
  ['navPerCbfi', 'navPerCbfiNote'],
  ['ltv', 'ltvNote'],
  ['noiMargin', 'noiMarginNote'],
  ['ffoMargin', 'ffoMarginNote'],
  ['quarterlyDistribution', 'quarterlyDistributionNote'],
] as const

type FieldKey = typeof FIELD_NOTE_KEYS[number][0]
type RequestKey = typeof FIELD_NOTE_KEYS[number][1]

type NoteFormState = Record<RequestKey, string>

function buildInitialState(record: FundamentalRecordDto | null): NoteFormState {
  return {
    capRateNote: record?.fieldNotes?.capRate ?? '',
    navPerCbfiNote: record?.fieldNotes?.navPerCbfi ?? '',
    ltvNote: record?.fieldNotes?.ltv ?? '',
    noiMarginNote: record?.fieldNotes?.noiMargin ?? '',
    ffoMarginNote: record?.fieldNotes?.ffoMargin ?? '',
    quarterlyDistributionNote: record?.fieldNotes?.quarterlyDistribution ?? '',
  }
}

export function EditFieldNotesDialog({ fibraId, record, open, onClose }: Props) {
  const queryClient = useQueryClient()
  const initialState = useMemo(() => buildInitialState(record), [record])
  const [values, setValues] = useState<NoteFormState>(initialState)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)
  const prevOpenRef = useRef(false)

  useEffect(() => {
    const justOpened = open && !prevOpenRef.current
    prevOpenRef.current = open
    if (!justOpened) return
    setValues(initialState)
    setErrorMessage(null)
  }, [open, initialState])

  const isDirty = JSON.stringify(values) !== JSON.stringify(initialState)

  const mutation = useMutation({
    mutationFn: async (payload: PatchFieldNotesRequest) => {
      if (!record) throw new Error('No hay registro seleccionado.')
      return patchFieldNotes(record.id, payload)
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['fundamentals', fibraId] })
      onClose()
    },
    onError: (error: unknown) => {
      setErrorMessage(error instanceof Error ? error.message : 'Error al guardar notas')
    },
  })

  const handleSave = () => {
    setErrorMessage(null)
    mutation.mutate(values)
  }

  return (
    <Dialog open={open} onOpenChange={(nextOpen) => { if (!nextOpen) onClose() }}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Notas IA por KPI</DialogTitle>
          <DialogDescription>
            {record ? `${record.fibraTicker} · ${record.period}` : 'Sin registro seleccionado'}
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4">
          {FIELD_NOTE_KEYS.map(([fieldKey, requestKey]) => (
            <label key={requestKey} className="block space-y-1.5">
              <span className="text-sm font-medium text-slate-700">
                {KPI_DEFINITIONS[fieldKey as FieldKey].label}
              </span>
              <textarea
                value={values[requestKey]}
                onChange={(event) => {
                  const nextValue = event.target.value
                  setValues((current) => ({ ...current, [requestKey]: nextValue }))
                }}
                rows={3}
                className="w-full rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm text-slate-700 outline-none transition focus:border-teal-500 focus:ring-2 focus:ring-teal-500/20"
                placeholder="Sin nota"
              />
            </label>
          ))}

          {errorMessage && (
            <p className="rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700">
              {errorMessage}
            </p>
          )}
        </div>

        <DialogFooter>
          <button
            type="button"
            onClick={onClose}
            className="rounded-lg border border-slate-200 px-4 py-2 text-sm font-medium text-slate-600 transition hover:bg-slate-50"
          >
            Cancelar
          </button>
          <button
            type="button"
            onClick={handleSave}
            disabled={!isDirty || mutation.isPending}
            className="rounded-lg bg-teal-600 px-4 py-2 text-sm font-semibold text-white transition hover:bg-teal-700 disabled:cursor-not-allowed disabled:opacity-50"
          >
            {mutation.isPending ? 'Guardando…' : 'Guardar notas'}
          </button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
