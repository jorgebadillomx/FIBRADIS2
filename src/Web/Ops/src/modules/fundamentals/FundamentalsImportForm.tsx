import { useForm } from 'react-hook-form'
import { useMutation } from '@tanstack/react-query'
import type { FundamentalPreviewDto } from '@/api/fundamentalsApi'
import { importFundamentals } from '@/api/fundamentalsApi'

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
  pdfReference: string
}

interface Props {
  onPreview: (preview: FundamentalPreviewDto, fibraId: string) => void
}

export function FundamentalsImportForm({ onPreview }: Props) {
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<FormValues>({
    defaultValues: {
      fibraId: '',
      period: '',
      capRate: '',
      navPerCbfi: '',
      ltv: '',
      noiMargin: '',
      ffoMargin: '',
      quarterlyDistribution: '',
      summary: '',
      pdfReference: '',
    },
  })

  const toNum = (s: string) => { const n = parseFloat(s); return isNaN(n) ? null : n }

  const mutation = useMutation({
    mutationFn: (values: FormValues) =>
      importFundamentals({
        fibraId: values.fibraId,
        period: values.period,
        capRate: toNum(values.capRate),
        navPerCbfi: toNum(values.navPerCbfi),
        ltv: toNum(values.ltv),
        noiMargin: toNum(values.noiMargin),
        ffoMargin: toNum(values.ffoMargin),
        quarterlyDistribution: toNum(values.quarterlyDistribution),
        summary: values.summary || null,
        pdfReference: values.pdfReference || null,
      }),
    onSuccess: (data, variables) => {
      onPreview(data, variables.fibraId)
    },
  })

  const onSubmit = handleSubmit((values) => mutation.mutate(values))

  return (
    <form onSubmit={onSubmit} className="space-y-5">
      <div className="grid gap-4 sm:grid-cols-2">
        <div className="sm:col-span-2">
          <label className="block text-sm font-medium text-slate-700 mb-1">
            FIBRA ID <span className="text-red-500">*</span>
          </label>
          <input
            {...register('fibraId', {
              required: 'El FIBRA ID es requerido',
              pattern: {
                value: /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i,
                message: 'Debe ser un UUID válido (xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx)',
              },
            })}
            className="w-full rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-teal-500"
            placeholder="xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
          />
          {errors.fibraId && <p className="mt-1 text-xs text-red-600">{errors.fibraId.message}</p>}
        </div>

        <div>
          <label className="block text-sm font-medium text-slate-700 mb-1">
            Período <span className="text-red-500">*</span>
          </label>
          <input
            {...register('period', {
              required: 'El período es requerido',
              pattern: { value: /^Q[1-4]-\d{4}$/, message: 'Formato: Q1-2024, Q2-2024…' },
            })}
            className="w-full rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-teal-500"
            placeholder="Q3-2024"
          />
          {errors.period && <p className="mt-1 text-xs text-red-600">{errors.period.message}</p>}
        </div>

        {([
          ['capRate', 'Cap Rate'],
          ['navPerCbfi', 'NAV por CBFI'],
          ['ltv', 'LTV'],
          ['noiMargin', 'Margen NOI'],
          ['ffoMargin', 'Margen FFO'],
          ['quarterlyDistribution', 'Dist. Trimestral'],
        ] as const).map(([name, label]) => (
          <div key={name}>
            <label className="block text-sm font-medium text-slate-700 mb-1">{label}</label>
            <input
              {...register(name)}
              type="number"
              step="any"
              className="w-full rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-teal-500"
              placeholder="—"
            />
          </div>
        ))}

        <div className="sm:col-span-2">
          <label className="block text-sm font-medium text-slate-700 mb-1">Resumen (Summary)</label>
          <textarea
            {...register('summary')}
            rows={3}
            className="w-full rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-teal-500 resize-none"
            placeholder="Resumen cualitativo del trimestre…"
          />
        </div>

        <div className="sm:col-span-2">
          <label className="block text-sm font-medium text-slate-700 mb-1">Referencia PDF</label>
          <input
            {...register('pdfReference')}
            className="w-full rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-teal-500"
            placeholder="uploads/fundamentals/…"
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
        disabled={mutation.isPending}
        className="w-full rounded-xl bg-teal-600 px-4 py-2.5 text-sm font-semibold text-white hover:bg-teal-700 disabled:opacity-50 transition"
      >
        {mutation.isPending ? 'Importando…' : 'Importar y previsualizar'}
      </button>
    </form>
  )
}
