import { useRef, useCallback, useState, useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { useQuery, useMutation } from '@tanstack/react-query'
import type { FundamentalPreviewDto, FundamentalRecordDto } from '@/api/fundamentalsApi'
import {
  importFundamentals,
  uploadPdfWithRecord,
  triggerKpiExtraction,
  patchKpis,
  confirmFundamentals,
} from '@/api/fundamentalsApi'
import { fetchOpsCatalog } from '@/api/catalogApi'
import type { KpiKey } from '@/lib/kpi-definitions'

function generateRecentPeriods(): string[] {
  const now = new Date()
  const periods: string[] = []
  let year = now.getFullYear()
  let q = Math.ceil((now.getMonth() + 1) / 3)
  for (let i = 0; i < 12; i++) {
    periods.push(`Q${q}-${year}`)
    q--
    if (q === 0) { q = 4; year-- }
  }
  return periods
}

const PERIODS = generateRecentPeriods()

const KPI_FIELDS = [
  ['capRate', 'Cap Rate', 'Capitalización de renta: NOI anualizado / valor de propiedades. Decimal (ej. 0.08 = 8%).'],
  ['navPerCbfi', 'NAV por CBFI', 'Valor Neto del Activo por CBFI en pesos. Patrimonio − deuda / CBFIs en circulación.'],
  ['ltv', 'LTV', 'Loan to Value: deuda / valor de propiedades. Decimal (ej. 0.45 = 45%).'],
  ['noiMargin', 'Margen NOI', 'Net Operating Income margin. Decimal.'],
  ['ffoMargin', 'Margen FFO', 'Funds from Operations margin. Decimal.'],
  ['quarterlyDistribution', 'Dist. Trimestral', 'Distribución por CBFI en el trimestre, en MXN.'],
] as const

interface FormValues {
  fibraId: string
  period: string
  capRate: string
  navPerCbfi: string
  ltv: string
  noiMargin: string
  ffoMargin: string
  quarterlyDistribution: string
  summary: string
}

interface Props {
  onPreview: (preview: FundamentalPreviewDto, fibraId: string, record: FundamentalRecordDto) => void
  onFibraChange?: (fibraId: string) => void
  initialRecord?: FundamentalRecordDto
  initialFibraId?: string
}

function FieldInfo({ title }: { title: string }) {
  return (
    <span
      className="cursor-help text-slate-400 text-xs select-none"
      title={title}
      aria-label={title}
    >
      ⓘ
    </span>
  )
}

type AiStep = 'idle' | 'uploading' | 'extracting' | 'done' | 'error'

export function FundamentalsImportForm({ onPreview, onFibraChange, initialRecord, initialFibraId }: Props) {
  const fileInputRef = useRef<HTMLInputElement>(null)
  const [pdfFile, setPdfFile] = useState<File | null>(null)
  const [fieldNotes, setFieldNotes] = useState<Partial<Record<KpiKey, string>>>({})

  // AI flow state
  const [aiStep, setAiStep] = useState<AiStep>('idle')
  const [aiError, setAiError] = useState<string | null>(null)
  const [pendingRecordId, setPendingRecordId] = useState<string | null>(null)
  const [warningMessage, setWarningMessage] = useState<string | null>(null)
  const [awaitingReplaceConfirm, setAwaitingReplaceConfirm] = useState(false)
  const pendingExtractRecordId = useRef<string | null>(null)

  const { data: catalog = [], isLoading: catalogLoading } = useQuery({
    queryKey: ['catalog-ops'],
    queryFn: fetchOpsCatalog,
  })

  const { register, handleSubmit, setValue, watch, reset, formState: { errors } } = useForm<FormValues>({
    defaultValues: {
      fibraId: '', period: PERIODS[0],
      capRate: '', navPerCbfi: '', ltv: '',
      noiMargin: '', ffoMargin: '', quarterlyDistribution: '',
      summary: '',
    },
  })

  const watchedFibraId = watch('fibraId')
  useEffect(() => {
    if (watchedFibraId) onFibraChange?.(watchedFibraId)
  }, [watchedFibraId, onFibraChange])

  useEffect(() => {
    if (!initialRecord) return
    const toStr = (v: number | null | undefined) => v != null ? String(v) : ''
    reset({
      fibraId: initialFibraId ?? '',
      period: initialRecord.period,
      capRate: toStr(initialRecord.capRate),
      navPerCbfi: toStr(initialRecord.navPerCbfi),
      ltv: toStr(initialRecord.ltv),
      noiMargin: toStr(initialRecord.noiMargin),
      ffoMargin: toStr(initialRecord.ffoMargin),
      quarterlyDistribution: toStr(initialRecord.quarterlyDistribution),
      summary: initialRecord.summary ?? '',
    })
    const notes: Partial<Record<KpiKey, string>> = {}
    const kpiKeys: KpiKey[] = ['capRate', 'navPerCbfi', 'ltv', 'noiMargin', 'ffoMargin', 'quarterlyDistribution']
    for (const key of kpiKeys) {
      const note = initialRecord.fieldNotes?.[key]
      if (note) notes[key] = note
    }
    setFieldNotes(notes)
    setPendingRecordId(initialRecord.id)
    setAiStep('done')
    setAiError(null)
    setWarningMessage(null)
    setPdfFile(null)
    if (fileInputRef.current) fileInputRef.current.value = ''
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [initialRecord?.id])

  const toNum = (s: string) => { const n = parseFloat(s); return isNaN(n) ? null : n }

  const isConfirmMode = pendingRecordId !== null

  const resetAiState = () => {
    setAiStep('idle')
    setAiError(null)
    setPendingRecordId(null)
    setWarningMessage(null)
    setFieldNotes({})
    setAwaitingReplaceConfirm(false)
    pendingExtractRecordId.current = null
  }

  const runKpiExtraction = useCallback(async (recordId: string) => {
    setAiStep('extracting')
    try {
      const extracted = await triggerKpiExtraction(recordId)
      const numericFields = ['capRate', 'navPerCbfi', 'ltv', 'noiMargin', 'ffoMargin', 'quarterlyDistribution'] as const
      const notes: Partial<Record<KpiKey, string>> = {}
      for (const field of numericFields) {
        if (extracted[field] != null) setValue(field, String(extracted[field]), { shouldDirty: true })
        const note = extracted.fieldNotes?.[field]
        if (note) notes[field] = note
      }
      if (extracted.summary) setValue('summary', extracted.summary, { shouldDirty: true })
      setFieldNotes(notes)
      setPendingRecordId(recordId)
      setAiStep('done')
    } catch (err) {
      const msg = err instanceof Error ? err.message : 'Error al extraer KPIs'
      setAiError(msg)
      setAiStep('error')
      setPendingRecordId(recordId)
    }
  }, [setValue])

  const handleFileChange = (file: File | null) => {
    setPdfFile(file)
    resetAiState()
  }

  // Manual import flow (no PDF)
  const manualMutation = useMutation({
    mutationFn: async (values: FormValues) => {
      const preview = await importFundamentals({
        fibraId: values.fibraId,
        period: values.period,
        capRate: toNum(values.capRate),
        navPerCbfi: toNum(values.navPerCbfi),
        ltv: toNum(values.ltv),
        noiMargin: toNum(values.noiMargin),
        ffoMargin: toNum(values.ffoMargin),
        quarterlyDistribution: toNum(values.quarterlyDistribution),
        summary: values.summary.trim() || null,
        pdfReference: null,
      })

      const syntheticRecord: FundamentalRecordDto = {
        id: preview.id,
        fibraTicker: preview.fibraTicker,
        period: preview.period,
        status: preview.status,
        isPossibleUpdate: preview.isPossibleUpdate,
        capRate: toNum(values.capRate),
        navPerCbfi: toNum(values.navPerCbfi),
        ltv: toNum(values.ltv),
        noiMargin: toNum(values.noiMargin),
        ffoMargin: toNum(values.ffoMargin),
        quarterlyDistribution: toNum(values.quarterlyDistribution),
        summary: values.summary.trim() || null,
        pdfReference: preview.pdfReference,
        pdfUploadedAt: null,
        importedBy: null,
        confirmedBy: null,
        capturedAt: preview.capturedAt,
        confirmedAt: null,
        hasMarkdownContent: false,
        fieldNotes: undefined,
      }

      return { preview, fibraId: values.fibraId, record: syntheticRecord }
    },
    onSuccess: ({ preview, fibraId, record }) => {
      reset({ fibraId: '', period: PERIODS[0], capRate: '', navPerCbfi: '', ltv: '', noiMargin: '', ffoMargin: '', quarterlyDistribution: '', summary: '' })
      resetAiState()
      setPdfFile(null)
      onPreview(preview, fibraId, record)
    },
  })

  // AI confirm flow (PDF was uploaded + AI extracted)
  const confirmMutation = useMutation({
    mutationFn: async (values: FormValues) => {
      if (!pendingRecordId) throw new Error('No hay registro pendiente.')

      // Save any user edits before confirming
      await patchKpis(pendingRecordId, {
        capRate: toNum(values.capRate),
        navPerCbfi: toNum(values.navPerCbfi),
        ltv: toNum(values.ltv),
        noiMargin: toNum(values.noiMargin),
        ffoMargin: toNum(values.ffoMargin),
        quarterlyDistribution: toNum(values.quarterlyDistribution),
        summary: values.summary.trim() || null,
      })

      const record = await confirmFundamentals(pendingRecordId)

      const syntheticPreview: FundamentalPreviewDto = {
        id: record.id,
        fibraTicker: record.fibraTicker,
        period: record.period,
        status: record.status,
        isPossibleUpdate: record.isPossibleUpdate,
        warningMessage: null,
        presentFields: [],
        missingFields: [],
        pdfReference: record.pdfReference ?? null,
        capturedAt: record.capturedAt,
        hasMarkdownContent: record.hasMarkdownContent,
      }

      return { preview: syntheticPreview, fibraId: values.fibraId, record }
    },
    onSuccess: ({ preview, fibraId, record }) => {
      reset({ fibraId: '', period: PERIODS[0], capRate: '', navPerCbfi: '', ltv: '', noiMargin: '', ffoMargin: '', quarterlyDistribution: '', summary: '' })
      resetAiState()
      setPdfFile(null)
      onPreview(preview, fibraId, record)
    },
  })

  const onSubmit = handleSubmit(async (values) => {
    if (!pdfFile) {
      // Manual flow
      manualMutation.mutate(values)
      return
    }

    if (isConfirmMode) {
      // AI flow: confirm
      confirmMutation.mutate(values)
      return
    }

    // AI flow: Paso 1 → upload → (maybe confirm replace) → Paso 2
    setAiError(null)
    setAiStep('uploading')

    let recordId: string
    let isPossibleUpdate = false
    try {
      const uploadResult = await uploadPdfWithRecord(values.fibraId, values.period, pdfFile)
      recordId = uploadResult.id
      isPossibleUpdate = uploadResult.isPossibleUpdate
      if (uploadResult.warningMessage) setWarningMessage(uploadResult.warningMessage)
    } catch (err) {
      const msg = err instanceof Error ? err.message : 'Error al subir el PDF'
      setAiError(msg)
      setAiStep('error')
      return
    }

    if (isPossibleUpdate) {
      // Pause and ask the user before overwriting the existing processed record.
      pendingExtractRecordId.current = recordId
      setAiStep('idle')
      setAwaitingReplaceConfirm(true)
      return
    }

    await runKpiExtraction(recordId)
  })

  const isWorking = aiStep === 'uploading' || aiStep === 'extracting' || manualMutation.isPending || confirmMutation.isPending || awaitingReplaceConfirm

  const buttonLabel = () => {
    if (aiStep === 'uploading') return 'Subiendo PDF…'
    if (aiStep === 'extracting') return 'Extrayendo con IA…'
    if (manualMutation.isPending) return 'Importando…'
    if (confirmMutation.isPending) return 'Confirmando…'
    if (isConfirmMode) return 'Confirmar y guardar'
    return pdfFile ? 'Importar con IA y previsualizar' : 'Importar y previsualizar'
  }

  return (
    <form onSubmit={onSubmit} className="space-y-5">
      <div className="grid gap-4 sm:grid-cols-2">

        {/* FIBRA */}
        <div className="sm:col-span-2">
          <label className="flex items-center gap-1 text-sm font-medium text-slate-700 mb-1">
            FIBRA <span className="text-red-500">*</span>
            <FieldInfo title="FIBRA a la que pertenecen estos fundamentales." />
          </label>
          <select
            {...register('fibraId', { required: 'Selecciona una FIBRA' })}
            disabled={catalogLoading || isConfirmMode}
            className="w-full rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-teal-500 disabled:opacity-60"
          >
            <option value="">{catalogLoading ? 'Cargando catálogo…' : '— Selecciona una FIBRA —'}</option>
            {catalog.map((f) => (
              <option key={f.id} value={f.id}>
                {f.ticker} — {f.shortName ?? f.fullName}
              </option>
            ))}
          </select>
          {errors.fibraId && <p className="mt-1 text-xs text-red-600">{errors.fibraId.message}</p>}
        </div>

        {/* Período */}
        <div>
          <label className="flex items-center gap-1 text-sm font-medium text-slate-700 mb-1">
            Período <span className="text-red-500">*</span>
            <FieldInfo title="Trimestre al que corresponden los datos. Q3-2024 = tercer trimestre 2024." />
          </label>
          <select
            {...register('period', { required: 'Selecciona un período' })}
            disabled={isConfirmMode}
            className="w-full rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-teal-500 disabled:opacity-60"
          >
            {PERIODS.map((p) => <option key={p} value={p}>{p}</option>)}
          </select>
          {errors.period && <p className="mt-1 text-xs text-red-600">{errors.period.message}</p>}
        </div>

        {/* PDF */}
        <div>
          <label className="flex items-center gap-1 text-sm font-medium text-slate-700 mb-1">
            PDF del reporte
            <FieldInfo title="Sube el PDF para que la IA extraiga los KPIs automáticamente." />
          </label>
          <input
            ref={fileInputRef}
            type="file"
            accept="application/pdf"
            aria-label="Seleccionar archivo PDF"
            className="hidden"
            onChange={(e) => handleFileChange(e.target.files?.[0] ?? null)}
          />
          <div className="flex gap-2 items-center">
            <button
              type="button"
              onClick={() => fileInputRef.current?.click()}
              disabled={isWorking || isConfirmMode}
              className="flex-1 rounded-lg border border-slate-200 bg-white px-3 py-2 text-left text-sm text-slate-500 hover:bg-slate-50 focus:outline-none focus:ring-2 focus:ring-teal-500 disabled:opacity-60 transition"
            >
              {pdfFile ? (
                <span className="text-slate-800">{pdfFile.name}</span>
              ) : (
                'Seleccionar PDF (opcional)…'
              )}
            </button>
          </div>
          {pdfFile && !isConfirmMode && (
            <div className="mt-1 flex items-center gap-3">
              <button
                type="button"
                onClick={() => {
                  setPdfFile(null)
                  handleFileChange(null)
                  if (fileInputRef.current) fileInputRef.current.value = ''
                }}
                disabled={isWorking}
                className="text-xs text-red-500 hover:underline disabled:opacity-50"
              >
                Quitar PDF
              </button>
            </div>
          )}
          {isConfirmMode && (
            <button
              type="button"
              onClick={() => {
                resetAiState()
                reset()
                setPdfFile(null)
                if (fileInputRef.current) fileInputRef.current.value = ''
              }}
              className="mt-1 text-xs text-slate-500 hover:underline"
            >
              Cancelar y volver a empezar
            </button>
          )}
        </div>

        {/* Diálogo de confirmación de reemplazo */}
        {awaitingReplaceConfirm && (
          <div className="sm:col-span-2 rounded-xl border border-orange-300 bg-orange-50 p-4 space-y-3">
            <p className="text-sm font-medium text-orange-800">
              Ya existe un registro procesado para este período. Si continúas, el registro anterior quedará archivado y no se mostrará en las vistas públicas.
            </p>
            <p className="text-xs text-orange-700">
              {warningMessage}
            </p>
            <div className="flex gap-2">
              <button
                type="button"
                onClick={async () => {
                  const recordId = pendingExtractRecordId.current
                  if (!recordId) return
                  setAwaitingReplaceConfirm(false)
                  pendingExtractRecordId.current = null
                  await runKpiExtraction(recordId)
                }}
                className="rounded-lg bg-orange-600 px-3 py-1.5 text-xs font-semibold text-white hover:bg-orange-700 transition"
              >
                Continuar y archivar el anterior
              </button>
              <button
                type="button"
                onClick={() => {
                  resetAiState()
                  reset()
                  setPdfFile(null)
                  if (fileInputRef.current) fileInputRef.current.value = ''
                }}
                className="rounded-lg bg-white border border-orange-300 px-3 py-1.5 text-xs font-semibold text-orange-700 hover:bg-orange-100 transition"
              >
                Cancelar
              </button>
            </div>
          </div>
        )}

        {/* Feedback de estado */}
        {(aiStep !== 'idle' || (warningMessage && !awaitingReplaceConfirm)) && (
          <div className="sm:col-span-2 space-y-2">
            {warningMessage && (
              <p className="rounded-lg border border-orange-200 bg-orange-50 px-3 py-2 text-sm text-orange-700">
                {warningMessage}
              </p>
            )}
            {aiStep === 'uploading' && (
              <p className="rounded-lg border border-teal-200 bg-teal-50 px-3 py-2 text-sm text-teal-700 flex items-center gap-2">
                <Spinner /> Guardando PDF y extrayendo texto…
              </p>
            )}
            {aiStep === 'extracting' && (
              <p className="rounded-lg border border-teal-200 bg-teal-50 px-3 py-2 text-sm text-teal-700 flex items-center gap-2">
                <Spinner /> Analizando con IA…
              </p>
            )}
            {aiStep === 'done' && (
              <p className="rounded-lg border border-emerald-200 bg-emerald-50 px-3 py-2 text-sm text-emerald-700">
                IA extrajo los KPIs. Revisa y ajusta si es necesario antes de confirmar.
              </p>
            )}
            {aiStep === 'error' && aiError && (
              <p className="rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700">
                {aiError}
                {isConfirmMode && ' — Puedes confirmar con los valores que ingreses manualmente.'}
              </p>
            )}
          </div>
        )}

        {/* Separador */}
        <div className="sm:col-span-2 -mb-2">
          <p className="text-xs text-slate-500">
            {isConfirmMode
              ? 'Valores extraídos por IA — puedes ajustarlos antes de confirmar.'
              : pdfFile
                ? 'Al importar, el PDF se guardará y la IA extraerá los KPIs.'
                : 'Sin PDF: ingresa los valores manualmente.'}
          </p>
        </div>

        {/* Campos KPI */}
        {KPI_FIELDS.map(([name, label, tooltip]) => (
          <div key={name}>
            <label className="flex items-center gap-1 text-sm font-medium text-slate-700 mb-1">
              {label}
              <FieldInfo title={tooltip} />
              {fieldNotes[name] && (
                <FieldInfo title={`Nota IA: ${fieldNotes[name]}`} />
              )}
            </label>
            <input
              {...register(name)}
              type="number"
              step="any"
              disabled={isWorking}
              className="w-full rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-teal-500 disabled:opacity-50"
              placeholder="—"
            />
          </div>
        ))}

        {/* Resumen */}
        <div className="sm:col-span-2">
          <label className="mb-1 flex items-center gap-1 text-sm font-medium text-slate-700">
            Resumen
            <FieldInfo title="Resumen analítico del período generado por IA o escrito manualmente." />
          </label>
          <textarea
            {...register('summary')}
            rows={4}
            disabled={isWorking}
            className="w-full rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-teal-500 disabled:opacity-50"
            placeholder="Resumen analítico del trimestre (opcional)…"
          />
        </div>
      </div>

      {(manualMutation.isError || confirmMutation.isError) && (
        <p className="rounded-lg bg-red-50 border border-red-200 px-4 py-3 text-sm text-red-700">
          {(manualMutation.error ?? confirmMutation.error) instanceof Error
            ? (manualMutation.error ?? confirmMutation.error as Error).message
            : 'Error al importar'}
        </p>
      )}

      <button
        type="submit"
        disabled={isWorking || catalogLoading}
        className="w-full rounded-xl bg-teal-600 px-4 py-2.5 text-sm font-semibold text-white hover:bg-teal-700 disabled:opacity-50 transition"
      >
        {buttonLabel()}
      </button>
    </form>
  )
}

function Spinner() {
  return (
    <svg className="animate-spin h-3.5 w-3.5 shrink-0" viewBox="0 0 24 24" fill="none">
      <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
      <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v4a4 4 0 00-4 4H4z" />
    </svg>
  )
}
