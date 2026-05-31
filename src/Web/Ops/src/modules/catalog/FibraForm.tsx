import { useState, type ReactNode } from 'react'
import { useForm } from 'react-hook-form'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import {
  createFibra,
  updateFibra,
  type CreateFibraRequest,
  type FibraDetail,
  type UpdateFibraRequest,
} from '@/api/catalogApi'

interface Props {
  initialData?: FibraDetail
  onSuccess: () => void
  onCancel: () => void
}

interface FormValues {
  ticker: string
  yahooTicker: string
  fullName: string
  shortName: string
  sector: string
  market: string
  currency: string
  siteUrl: string
  investorUrl: string
  reportsUrl: string
}

const MAX_DESC = 10_000

export function FibraForm({ initialData, onSuccess, onCancel }: Props) {
  const queryClient = useQueryClient()
  const [variants, setVariants] = useState<string[]>(initialData?.nameVariants ?? [])
  const [description, setDescription] = useState<string>(initialData?.description ?? '')

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<FormValues>({
    defaultValues: {
      ticker: initialData?.ticker ?? '',
      yahooTicker: initialData?.yahooTicker ?? '',
      fullName: initialData?.fullName ?? '',
      shortName: initialData?.shortName ?? '',
      sector: initialData?.sector ?? '',
      market: initialData?.market ?? '',
      currency: initialData?.currency ?? 'MXN',
      siteUrl: initialData?.siteUrl ?? '',
      investorUrl: initialData?.investorUrl ?? '',
      reportsUrl: initialData?.reportsUrl ?? '',
    },
  })

  const mutation = useMutation({
    mutationFn: async (values: FormValues) => {
      const normalizedVariants = variants.map((variant) => variant.trim()).filter((variant) => variant.length > 0)

      const normalizedDescription = description.trim() === '' ? null : description

      if (initialData) {
        const payload: UpdateFibraRequest = {
          yahooTicker: values.yahooTicker.trim(),
          fullName: values.fullName.trim(),
          shortName: values.shortName.trim(),
          sector: values.sector.trim(),
          market: values.market.trim(),
          currency: values.currency.trim().toUpperCase(),
          siteUrl: toNullable(values.siteUrl),
          investorUrl: toNullable(values.investorUrl),
          reportsUrl: toNullable(values.reportsUrl),
          nameVariants: normalizedVariants,
          description: normalizedDescription,
        }

        return updateFibra(initialData.ticker, payload)
      }

      const payload: CreateFibraRequest = {
        ticker: values.ticker.trim().toUpperCase(),
        yahooTicker: values.yahooTicker.trim(),
        fullName: values.fullName.trim(),
        shortName: values.shortName.trim(),
        sector: values.sector.trim(),
        market: values.market.trim(),
        currency: values.currency.trim().toUpperCase(),
        siteUrl: toNullable(values.siteUrl),
        investorUrl: toNullable(values.investorUrl),
        reportsUrl: toNullable(values.reportsUrl),
        nameVariants: normalizedVariants,
        description: normalizedDescription,
      }

      return createFibra(payload)
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['ops-catalog'] })
      onSuccess()
    },
  })

  const onSubmit = handleSubmit((values) => mutation.mutate(values))
  const title = initialData ? 'Editar FIBRA' : 'Nueva FIBRA'

  return (
    <form className="space-y-6" onSubmit={onSubmit}>
      <div className="flex items-start justify-between gap-4">
        <div>
          <h2 className="text-lg font-semibold tracking-tight text-slate-950">{title}</h2>
          <p className="mt-1 text-sm text-slate-500">
            Completa los metadatos operativos para mantener el universo activo consistente.
          </p>
        </div>
        <button
          className="rounded-lg border border-slate-300 px-3 py-2 text-sm font-medium text-slate-700 transition hover:bg-slate-50"
          onClick={onCancel}
          type="button"
        >
          Cancelar
        </button>
      </div>

      <div className="grid gap-4 md:grid-cols-2">
        {!initialData ? (
          <Field label="Ticker" error={errors.ticker?.message} required>
            <input
              {...register('ticker', {
                required: 'El ticker es requerido.',
                maxLength: { value: 20, message: 'Máximo 20 caracteres.' },
              })}
              className={inputClassName}
              placeholder="FUNO11"
            />
          </Field>
        ) : (
          <Field label="Ticker">
            <input className={`${inputClassName} bg-slate-100`} disabled value={initialData.ticker} />
          </Field>
        )}

        <Field label="YahooTicker" error={errors.yahooTicker?.message} required>
          <input
            {...register('yahooTicker', {
              required: 'YahooTicker es requerido.',
              maxLength: { value: 32, message: 'Máximo 32 caracteres.' },
            })}
            className={inputClassName}
            placeholder="FUNO11.MX"
          />
        </Field>

        <Field label="Nombre completo" error={errors.fullName?.message} required>
          <input
            {...register('fullName', {
              required: 'El nombre completo es requerido.',
              maxLength: { value: 256, message: 'Máximo 256 caracteres.' },
            })}
            className={inputClassName}
            placeholder="Fibra Uno"
          />
        </Field>

        <Field label="Nombre corto" error={errors.shortName?.message} required>
          <input
            {...register('shortName', {
              required: 'El nombre corto es requerido.',
              maxLength: { value: 64, message: 'Máximo 64 caracteres.' },
            })}
            className={inputClassName}
            placeholder="FUNO"
          />
        </Field>

        <Field label="Sector" error={errors.sector?.message} required>
          <input
            {...register('sector', {
              required: 'El sector es requerido.',
              maxLength: { value: 64, message: 'Máximo 64 caracteres.' },
            })}
            className={inputClassName}
            placeholder="Industrial"
          />
        </Field>

        <Field label="Mercado" error={errors.market?.message} required>
          <input
            {...register('market', {
              required: 'El mercado es requerido.',
              maxLength: { value: 32, message: 'Máximo 32 caracteres.' },
            })}
            className={inputClassName}
            placeholder="BMV"
          />
        </Field>

        <Field label="Moneda" error={errors.currency?.message} required>
          <input
            {...register('currency', {
              required: 'La moneda es requerida.',
              maxLength: { value: 8, message: 'Máximo 8 caracteres.' },
            })}
            className={inputClassName}
            placeholder="MXN"
          />
        </Field>

        <Field label="SiteUrl" error={errors.siteUrl?.message}>
          <input {...register('siteUrl')} className={inputClassName} placeholder="https://example.com" />
        </Field>

        <Field label="InvestorUrl" error={errors.investorUrl?.message}>
          <input
            {...register('investorUrl')}
            className={inputClassName}
            placeholder="https://example.com/investors"
          />
        </Field>

        <Field label="ReportsUrl" error={errors.reportsUrl?.message}>
          <input
            {...register('reportsUrl')}
            className={inputClassName}
            placeholder="https://example.com/reports"
          />
        </Field>
      </div>

      <section className="rounded-2xl border border-slate-200 bg-slate-50/70 p-4">
        <div className="mb-4 flex items-center justify-between gap-4">
          <div>
            <h3 className="text-sm font-semibold uppercase tracking-[0.18em] text-slate-600">NameVariants</h3>
            <p className="mt-1 text-sm text-slate-500">Aliases usados por el pipeline de noticias y búsqueda.</p>
          </div>
          <button
            className="rounded-lg border border-teal-300 px-3 py-2 text-sm font-medium text-teal-700 transition hover:bg-teal-50"
            onClick={() => setVariants((current) => [...current, ''])}
            type="button"
          >
            + Agregar variante
          </button>
        </div>

        <div className="space-y-3">
          {variants.length === 0 ? (
            <p className="text-sm text-slate-500">Sin variantes adicionales.</p>
          ) : null}

          {variants.map((variant, index) => (
            <div className="flex gap-3" key={index}>
              <input
                className={inputClassName}
                onChange={(event) =>
                  setVariants((current) =>
                    current.map((item, currentIndex) => (currentIndex === index ? event.target.value : item)),
                  )
                }
                placeholder="Ej. Fibra Uno"
                value={variant}
              />
              <button
                className="rounded-lg border border-rose-200 px-3 py-2 text-sm font-medium text-rose-700 transition hover:bg-rose-50"
                onClick={() => setVariants((current) => current.filter((_, currentIndex) => currentIndex !== index))}
                type="button"
              >
                Quitar
              </button>
            </div>
          ))}
        </div>
      </section>

      <section className="rounded-2xl border border-slate-200 bg-slate-50/70 p-4 space-y-3">
        <div className="flex items-center justify-between gap-4">
          <div>
            <h3 className="text-sm font-semibold uppercase tracking-[0.18em] text-slate-600">
              Descripción editorial
            </h3>
            <p className="mt-1 text-sm text-slate-500">
              Texto largo en formato Markdown (~10 000 chars). Visible en la ficha pública y catálogo.
            </p>
          </div>
          <span className={`text-xs font-mono ${description.length > MAX_DESC ? 'text-rose-600' : 'text-slate-400'}`}>
            {description.length} / {MAX_DESC}
          </span>
        </div>
        <textarea
          className="w-full rounded-xl border border-slate-200 bg-white px-3 py-2.5 text-sm text-slate-900 font-mono outline-none transition focus:border-teal-600 resize-y"
          onChange={(e) => setDescription(e.target.value)}
          placeholder={'# Título\n\nDescribe la FIBRA en Markdown...'}
          rows={20}
          value={description}
        />
        {description.length > MAX_DESC ? (
          <p className="text-xs text-rose-600">
            La descripción supera el límite de {MAX_DESC} caracteres.
          </p>
        ) : null}
      </section>

      {mutation.isError ? (
        <p className="rounded-xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">
          {mutation.error.message}
        </p>
      ) : null}

      <div className="flex justify-end">
        <button
          className="rounded-xl bg-teal-700 px-5 py-2.5 text-sm font-semibold text-white transition hover:bg-teal-800 disabled:cursor-not-allowed disabled:opacity-60"
          disabled={mutation.isPending || description.length > MAX_DESC}
          type="submit"
        >
          {mutation.isPending ? 'Guardando...' : 'Guardar FIBRA'}
        </button>
      </div>
    </form>
  )
}

function Field({
  children,
  error,
  label,
  required = false,
}: {
  children: ReactNode
  error?: string
  label: string
  required?: boolean
}) {
  return (
    <label className="block space-y-1.5">
      <span className="text-sm font-medium text-slate-700">
        {label} {required ? <span className="text-rose-600">*</span> : null}
      </span>
      {children}
      {error ? <span className="text-xs text-rose-700">{error}</span> : null}
    </label>
  )
}

function toNullable(value: string): string | null {
  const normalized = value.trim()
  return normalized.length === 0 ? null : normalized
}

const inputClassName =
  'w-full rounded-xl border border-slate-200 bg-white px-3 py-2.5 text-sm text-slate-900 outline-none transition focus:border-teal-600'
