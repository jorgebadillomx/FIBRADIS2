import { useRef, useState } from 'react'
import { useForm } from 'react-hook-form'
import { useQuery, useMutation } from '@tanstack/react-query'
import type { FundamentalPreviewDto } from '@/api/fundamentalsApi'
import { extractKpisFromPdf, importFundamentals, uploadFundamentalPdf } from '@/api/fundamentalsApi'
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
  onPreview: (preview: FundamentalPreviewDto, fibraId: string) => void
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

export function FundamentalsImportForm({ onPreview }: Props) {
  const fileInputRef = useRef<HTMLInputElement>(null)
  const [pdfFile, setPdfFile] = useState<File | null>(null)
  const [fieldNotes, setFieldNotes] = useState<Partial<Record<KpiKey, string>>>({})
  const [extractionState, setExtractionState] = useState<'idle' | 'loading' | 'done' | 'error'>('idle')
  const [extractionError, setExtractionError] = useState<string | null>(null)
  const [extractedKpiCount, setExtractedKpiCount] = useState(0)

  const { data: catalog = [], isLoading: catalogLoading } = useQuery({
    queryKey: ['catalog-ops'],
    queryFn: fetchOpsCatalog,
  })

  const { register, handleSubmit, setValue, formState: { errors } } = useForm<FormValues>({
    defaultValues: {
      fibraId: '', period: PERIODS[0],
      capRate: '', navPerCbfi: '', ltv: '',
      noiMargin: '', ffoMargin: '', quarterlyDistribution: '',
      summary: '',
    },
  })

  const toNum = (s: string) => { const n = parseFloat(s); return isNaN(n) ? null : n }

  const runExtraction = async (file: File) => {
    setExtractionState('loading')
    setExtractionError(null)
    setFieldNotes({})
    setExtractedKpiCount(0)

    try {
      const result = await extractKpisFromPdf(file)
      const numericFields = ['capRate', 'navPerCbfi', 'ltv', 'noiMargin', 'ffoMargin', 'quarterlyDistribution'] as const
      const notes: Partial<Record<KpiKey, string>> = {}
      let count = 0

      for (const field of numericFields) {
        if (result[field] != null) {
          setValue(field, String(result[field]), { shouldDirty: true })
          count++
        }
        const noteKey = `${field}Note` as const
        const note = result[noteKey]
        if (note) notes[field] = note
      }

      if (result.summary) setValue('summary', result.summary, { shouldDirty: true })

      setFieldNotes(notes)
      setExtractedKpiCount(count)
      setExtractionState('done')
    } catch (err) {
      const msg = err instanceof Error ? err.message : 'Error al extraer KPIs desde el PDF'
      setExtractionError(msg)
      setExtractionState('error')
    }
  }

  const handleFileChange = (file: File | null) => {
    setPdfFile(file)
    setExtractionState('idle')
    setExtractionError(null)
    setFieldNotes({})
    setExtractedKpiCount(0)

    if (file) {
      // Auto-extract on file selection
      void runExtraction(file)
    }
  }

  const mutation = useMutation({
    mutationFn: async (values: FormValues) => {
      const notes = fieldNotes
      const aiErrorNote = extractionState === 'error' && extractionError
        ? { extractionNotes: extractionError }
        : {}

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
        fieldNotes: { ...notes, ...aiErrorNote },
      })

      if (pdfFile) {
        await uploadFundamentalPdf(preview.id, pdfFile)
      }

      return { preview, fibraId: values.fibraId }
    },
    onSuccess: ({ preview, fibraId }) => {
      onPreview(preview, fibraId)
    },
  })

  const onSubmit = handleSubmit((values) => {
    mutation.mutate(values)
  })

  const isExtracting = extractionState === 'loading'
  const isPending = mutation.isPending

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
            disabled={catalogLoading}
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
            className="w-full rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-teal-500"
          >
            {PERIODS.map((p) => <option key={p} value={p}>{p}</option>)}
          </select>
          {errors.period && <p className="mt-1 text-xs text-red-600">{errors.period.message}</p>}
        </div>

        {/* PDF */}
        <div>
          <label className="flex items-center gap-1 text-sm font-medium text-slate-700 mb-1">
            PDF del reporte
            <FieldInfo title="Al seleccionar el PDF se extrae el texto y la IA obtiene los KPIs automáticamente." />
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
              disabled={isExtracting}
              className="flex-1 rounded-lg border border-slate-200 bg-white px-3 py-2 text-left text-sm text-slate-500 hover:bg-slate-50 focus:outline-none focus:ring-2 focus:ring-teal-500 disabled:opacity-60 transition"
            >
              {pdfFile ? (
                <span className="text-slate-800">{pdfFile.name}</span>
              ) : (
                'Seleccionar PDF (opcional)…'
              )}
            </button>
            {isExtracting && (
              <span className="flex items-center gap-1.5 text-xs text-teal-600 font-medium shrink-0">
                <svg className="animate-spin h-3.5 w-3.5" viewBox="0 0 24 24" fill="none">
                  <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                  <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v4a4 4 0 00-4 4H4z" />
                </svg>
                Extrayendo con IA…
              </span>
            )}
          </div>
          {pdfFile && !isExtracting && (
            <div className="mt-1 flex items-center gap-3">
              <button
                type="button"
                onClick={() => {
                  setPdfFile(null)
                  handleFileChange(null)
                  if (fileInputRef.current) fileInputRef.current.value = ''
                }}
                className="text-xs text-red-500 hover:underline"
              >
                Quitar PDF
              </button>
              {extractionState === 'done' && (
                <button
                  type="button"
                  onClick={() => void runExtraction(pdfFile)}
                  className="text-xs text-teal-600 hover:underline"
                >
                  Re-extraer con IA
                </button>
              )}
              {(extractionState === 'error' || extractionState === 'idle') && (
                <button
                  type="button"
                  onClick={() => void runExtraction(pdfFile)}
                  className="text-xs text-teal-600 hover:underline"
                >
                  Extraer con IA
                </button>
              )}
            </div>
          )}
        </div>

        {/* Feedback de extracción */}
        {extractionState !== 'idle' && (
          <div className="sm:col-span-2">
            {extractionState === 'done' && extractedKpiCount > 0 && (
              <p className="rounded-lg border border-emerald-200 bg-emerald-50 px-3 py-2 text-sm text-emerald-700">
                IA extrajo {extractedKpiCount} de 6 KPIs. Revisa y ajusta si es necesario antes de importar.
              </p>
            )}
            {extractionState === 'done' && extractedKpiCount === 0 && (
              <p className="rounded-lg border border-amber-200 bg-amber-50 px-3 py-2 text-sm text-amber-700">
                La IA no encontró KPIs numéricos. Puedes ingresar los valores manualmente.
              </p>
            )}
            {extractionState === 'error' && (
              <p className="rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700">
                {extractionError ?? 'Error al extraer KPIs.'} — El error quedará registrado al importar.
              </p>
            )}
          </div>
        )}

        {/* Separador */}
        <div className="sm:col-span-2 -mb-2">
          <p className="text-xs text-slate-500">
            {pdfFile
              ? 'Valores extraídos por IA — puedes ajustarlos manualmente antes de importar.'
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
              disabled={isExtracting}
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
            disabled={isExtracting}
            className="w-full rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-teal-500 disabled:opacity-50"
            placeholder="Resumen analítico del trimestre (opcional)…"
          />
        </div>
      </div>

      {mutation.isError && (
        <p className="rounded-lg bg-red-50 border border-red-200 px-4 py-3 text-sm text-red-700">
          {mutation.error instanceof Error ? mutation.error.message : 'Error al importar'}
        </p>
      )}

      <button
        type="submit"
        disabled={isPending || isExtracting || catalogLoading}
        className="w-full rounded-xl bg-teal-600 px-4 py-2.5 text-sm font-semibold text-white hover:bg-teal-700 disabled:opacity-50 transition"
      >
        {isPending
          ? pdfFile ? 'Importando y procesando PDF…' : 'Importando…'
          : isExtracting
            ? 'Extrayendo con IA…'
            : 'Importar y previsualizar'}
      </button>
    </form>
  )
}
