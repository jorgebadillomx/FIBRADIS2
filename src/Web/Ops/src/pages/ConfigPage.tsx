import { useEffect, useState, type ReactNode } from 'react'
import { useForm } from 'react-hook-form'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  fetchAuditLog,
  fetchOpsConfig,
  updateOpsConfig,
  type UpdateOperationalConfigRequest,
} from '@/api/configApi'

interface FormValues {
  commissionFactor: number
  avgPeriods: number
  newsCadenceMinutes: number
  fibraNewsMonths: number
  fundamentalsCadenceMinutes: number
  distributionCadenceMinutes: number
}

const inputClassName =
  'w-full rounded-xl border border-slate-200 bg-white px-3 py-2.5 text-sm text-slate-900 outline-none transition focus:border-teal-600'

export function ConfigPage() {
  const queryClient = useQueryClient()
  const [successMessage, setSuccessMessage] = useState<string | null>(null)

  const configQuery = useQuery({
    queryKey: ['ops-config'],
    queryFn: fetchOpsConfig,
    retry: false,
  })

  const auditLogQuery = useQuery({
    queryKey: ['ops-audit-log'],
    queryFn: fetchAuditLog,
    retry: false,
  })

  const {
    register,
    reset,
    handleSubmit,
    formState: { dirtyFields, errors, isDirty },
  } = useForm<FormValues>({
    defaultValues: {
      commissionFactor: 0.006,
      avgPeriods: 4,
      newsCadenceMinutes: 1440,
      fibraNewsMonths: 15,
      fundamentalsCadenceMinutes: 1440,
      distributionCadenceMinutes: 1440,
    },
  })

  useEffect(() => {
    if (!configQuery.data) return

    reset({
      commissionFactor: Number(configQuery.data.commissionFactor),
      avgPeriods: Number(configQuery.data.avgPeriods),
      newsCadenceMinutes: Number(configQuery.data.newsCadenceMinutes),
      fibraNewsMonths: Number(configQuery.data.fibraNewsMonths ?? 15),
      fundamentalsCadenceMinutes: Number(configQuery.data.fundamentalsCadenceMinutes ?? 1440),
      distributionCadenceMinutes: Number(configQuery.data.distributionCadenceMinutes ?? 1440),
    })
  }, [configQuery.data, reset])

  const saveMutation = useMutation({
    mutationFn: updateOpsConfig,
    onSuccess: async () => {
      setSuccessMessage('✓ Configuración guardada')
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ['ops-config'] }),
        queryClient.invalidateQueries({ queryKey: ['ops-audit-log'] }),
      ])
    },
  })

  const onSubmit = handleSubmit((values) => {
    setSuccessMessage(null)

    const payload: Partial<UpdateOperationalConfigRequest> = {}
    if (dirtyFields.commissionFactor) payload.commissionFactor = values.commissionFactor
    if (dirtyFields.avgPeriods) payload.avgPeriods = values.avgPeriods
    if (dirtyFields.newsCadenceMinutes) payload.newsCadenceMinutes = values.newsCadenceMinutes
    if (dirtyFields.fibraNewsMonths) payload.fibraNewsMonths = values.fibraNewsMonths
    if (dirtyFields.fundamentalsCadenceMinutes) payload.fundamentalsCadenceMinutes = values.fundamentalsCadenceMinutes
    if (dirtyFields.distributionCadenceMinutes) payload.distributionCadenceMinutes = values.distributionCadenceMinutes

    if (Object.keys(payload).length === 0) return
    saveMutation.mutate(payload)
  })

  return (
    <div className="space-y-6">
      <section className="rounded-[1.75rem] border border-slate-200 bg-white/90 p-6 shadow-sm">
        <div className="flex flex-col gap-2">
          <p className="text-xs font-semibold uppercase tracking-[0.28em] text-teal-700">Ops / Configuración</p>
          <h1 className="text-2xl font-semibold tracking-tight text-slate-950">
            Parámetros operativos sin redespliegue
          </h1>
          <p className="max-w-3xl text-sm leading-6 text-slate-500">
            Ajusta comisión, ventanas promedio y cadencias automáticas de noticias y fundamentales con persistencia y auditoría inmediata.
          </p>
        </div>

        {configQuery.isLoading ? (
          <p className="mt-6 text-sm text-slate-500">Cargando configuración...</p>
        ) : null}

        {configQuery.isError ? (
          <p className="mt-6 rounded-xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">
            {configQuery.error.message}
          </p>
        ) : null}

        {configQuery.isSuccess ? (
          <form className="mt-6 space-y-6" onSubmit={onSubmit}>
            <div className="grid gap-4 md:grid-cols-3">
              <Field label="commission_factor" error={errors.commissionFactor?.message} required>
                <input
                  {...register('commissionFactor', {
                    required: 'commission_factor es requerido.',
                    min: { value: 0.001, message: 'Mínimo 0.001.' },
                    max: { value: 0.1, message: 'Máximo 0.1.' },
                    valueAsNumber: true,
                  })}
                  className={inputClassName}
                  step="0.001"
                  type="number"
                />
              </Field>

              <Field label="avg_periods" error={errors.avgPeriods?.message} required>
                <input
                  {...register('avgPeriods', {
                    required: 'avg_periods es requerido.',
                    min: { value: 1, message: 'Mínimo 1.' },
                    max: { value: 20, message: 'Máximo 20.' },
                    valueAsNumber: true,
                  })}
                  className={inputClassName}
                  type="number"
                />
              </Field>

              <Field label="news_cadence_minutes" error={errors.newsCadenceMinutes?.message} required>
                <select
                  {...register('newsCadenceMinutes', {
                    required: 'news_cadence_minutes es requerido.',
                    valueAsNumber: true,
                  })}
                  className={inputClassName}
                >
                  {[1440].map((value) => (
                    <option key={value} value={value}>
                      24 horas
                    </option>
                  ))}
                </select>
              </Field>

              <Field label="fibra_news_months" error={errors.fibraNewsMonths?.message} required>
                <input
                  {...register('fibraNewsMonths', {
                    required: 'fibra_news_months es requerido.',
                    min: { value: 1, message: 'Mínimo 1.' },
                    max: { value: 36, message: 'Máximo 36.' },
                    valueAsNumber: true,
                  })}
                  className={inputClassName}
                  type="number"
                  min={1}
                  max={36}
                />
              </Field>

              <Field label="fundamentals_cadence_minutes" error={errors.fundamentalsCadenceMinutes?.message} required>
                <select
                  {...register('fundamentalsCadenceMinutes', {
                    required: 'fundamentals_cadence_minutes es requerido.',
                    valueAsNumber: true,
                  })}
                  className={inputClassName}
                >
                  {[60, 120, 180, 240, 360, 720, 1440].map((value) => (
                    <option key={value} value={value}>
                      {value === 1440 ? '24 horas' : `${value / 60} horas`}
                    </option>
                  ))}
                </select>
              </Field>

              <Field label="distribution_cadence_minutes" error={errors.distributionCadenceMinutes?.message} required>
                <select
                  {...register('distributionCadenceMinutes', {
                    required: 'distribution_cadence_minutes es requerido.',
                    valueAsNumber: true,
                  })}
                  className={inputClassName}
                >
                  {[720, 1440].map((value) => (
                    <option key={value} value={value}>
                      {value === 1440 ? '24 horas' : '12 horas'}
                    </option>
                  ))}
                </select>
              </Field>
            </div>

            <div className="rounded-2xl border border-slate-200 bg-slate-50/80 p-4 text-xs text-slate-500">
              Última actualización:{' '}
              {new Date(configQuery.data.updatedAt).toLocaleString('es-MX', {
                dateStyle: 'medium',
                timeStyle: 'short',
              })}
              {configQuery.data.updatedBy ? ` · por ${configQuery.data.updatedBy}` : ''}
            </div>

            {successMessage ? (
              <p className="rounded-xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-800">
                {successMessage}
              </p>
            ) : null}

            {saveMutation.isError ? (
              <p className="rounded-xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">
                {saveMutation.error.message}
              </p>
            ) : null}

            <div className="flex items-center justify-between gap-3">
              <p className="text-sm text-slate-500">
                {isDirty ? 'Hay cambios pendientes por guardar.' : 'Sin cambios pendientes.'}
              </p>
              <button
                className="rounded-xl bg-teal-700 px-5 py-2.5 text-sm font-semibold text-white transition hover:bg-teal-800 disabled:cursor-not-allowed disabled:opacity-60"
                disabled={saveMutation.isPending || !isDirty}
                type="submit"
              >
                {saveMutation.isPending ? 'Guardando...' : 'Guardar configuración'}
              </button>
            </div>
          </form>
        ) : null}
      </section>

      <section className="overflow-hidden rounded-[1.75rem] border border-slate-200 bg-white/90 shadow-sm">
        <div className="border-b border-slate-200 px-6 py-5">
          <h2 className="text-lg font-semibold tracking-tight text-slate-950">Auditoría de cambios</h2>
          <p className="mt-1 text-sm text-slate-500">Historial recuperable de modificaciones sobre `OperationalConfig`.</p>
        </div>

        <div className="overflow-x-auto">
          <table className="min-w-full border-collapse">
            <thead className="bg-slate-950 text-left text-xs uppercase tracking-[0.18em] text-slate-100">
              <tr>
                <th className="px-4 py-3 font-medium">Campo</th>
                <th className="px-4 py-3 font-medium">Valor Anterior</th>
                <th className="px-4 py-3 font-medium">Nuevo Valor</th>
                <th className="px-4 py-3 font-medium">Actor</th>
                <th className="px-4 py-3 font-medium">Timestamp</th>
              </tr>
            </thead>
            <tbody className="bg-white">
              {auditLogQuery.isLoading ? (
                <tr>
                  <td className="px-4 py-6 text-sm text-slate-500" colSpan={5}>
                    Cargando auditoría...
                  </td>
                </tr>
              ) : null}

              {auditLogQuery.isError ? (
                <tr>
                  <td className="px-4 py-6 text-sm text-rose-700" colSpan={5}>
                    {auditLogQuery.error.message}
                  </td>
                </tr>
              ) : null}

              {auditLogQuery.isSuccess && auditLogQuery.data.length === 0 ? (
                <tr>
                  <td className="px-4 py-6 text-sm text-slate-500" colSpan={5}>
                    No hay cambios auditados todavía.
                  </td>
                </tr>
              ) : null}

              {auditLogQuery.data?.map((entry) => (
                <tr className="border-t border-slate-100 text-sm" key={entry.id}>
                  <td className="px-4 py-4 font-medium text-slate-900">{entry.fieldName}</td>
                  <td className="px-4 py-4 text-slate-600">{entry.previousValue ?? '—'}</td>
                  <td className="px-4 py-4 text-slate-900">{entry.newValue ?? '—'}</td>
                  <td className="px-4 py-4 text-slate-600">{entry.actor}</td>
                  <td className="px-4 py-4 text-slate-600">{new Date(entry.changedAt).toLocaleString('es-MX')}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>
    </div>
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
