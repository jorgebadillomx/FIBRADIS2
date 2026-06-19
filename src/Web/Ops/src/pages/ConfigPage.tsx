import { useEffect, useState, type ReactNode } from 'react'
import { useForm } from 'react-hook-form'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  fetchAuditLog,
  fetchOpsConfig,
  updateOpsConfig,
  type UpdateOperationalConfigRequest,
} from '@/api/configApi'

const DEFAULT_TERMS_TEXT = `Fibras Inmobiliarias — Términos de uso y aviso de privacidad

1. Información de referencia únicamente
La información publicada en esta plataforma tiene carácter exclusivamente informativo y no constituye asesoría de inversión ni recomendación de compra o venta de valores. Las decisiones de inversión son de exclusiva responsabilidad del usuario. Fibras Inmobiliarias no será responsable de pérdidas derivadas del uso de los datos del sitio.

2. Protección de datos personales
Tu correo electrónico se almacena cifrado en nuestra base de datos. No lo compartimos, vendemos ni cedemos a terceros. Lo usamos únicamente para identificarte en la plataforma.

3. Seguridad
Las contraseñas se almacenan como hash unidireccional (bcrypt). La comunicación entre tu dispositivo y nuestros servidores usa HTTPS. Tu información está cifrada y segura.

4. Uso apropiado
El acceso a la plataforma es personal e intransferible. Está prohibido su uso para actividades ilegales o que violen derechos de terceros.

5. Derechos del usuario
Para ejercer derechos de acceso, rectificación o cancelación, escribe a portafoliodefibras@gmail.com. Atendemos en 5 días hábiles.

Al usar esta plataforma aceptas estos términos.`

interface FormValues {
  commissionFactor: number
  avgPeriods: number
  newsCadenceMinutes: number
  fibraNewsMonths: number
  fundamentalsCadenceMinutes: number
  distributionCadenceMinutes: number
  universeDegradationThresholdPct: number
}

const inputClassName =
  'w-full rounded-xl border border-slate-200 bg-white px-3 py-2.5 text-sm text-slate-900 outline-none transition focus:border-teal-600'

export function ConfigPage() {
  const queryClient = useQueryClient()
  const [successMessage, setSuccessMessage] = useState<string | null>(null)
  const [termsEnabled, setTermsEnabled] = useState(false)
  const [termsText, setTermsText] = useState('')
  const [contactEmail, setContactEmail] = useState('')
  const [siteSuccessMessage, setSiteSuccessMessage] = useState<string | null>(null)
  const [siteValidationError, setSiteValidationError] = useState<string | null>(null)

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
      universeDegradationThresholdPct: 30,
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
      universeDegradationThresholdPct: Number(configQuery.data.universeDegradationThresholdPct ?? 30),
    })
    setTermsEnabled(configQuery.data.termsEnabled ?? false)
    setTermsText(configQuery.data.termsText ?? DEFAULT_TERMS_TEXT)
    setContactEmail(configQuery.data.contactEmail ?? 'portafoliodefibras@gmail.com')
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

  const saveSiteMutation = useMutation({
    mutationFn: (payload: Partial<UpdateOperationalConfigRequest>) => updateOpsConfig(payload),
    onSuccess: async () => {
      setSiteSuccessMessage('✓ Contenido del sitio guardado')
      await queryClient.invalidateQueries({ queryKey: ['ops-config'] })
    },
  })

  function handleSaveSite() {
    setSiteSuccessMessage(null)
    setSiteValidationError(null)
    if (termsEnabled && !termsText.trim()) {
      setSiteValidationError('El texto de términos es obligatorio cuando el modal está activo.')
      return
    }
    saveSiteMutation.mutate({
      termsEnabled,
      termsText: termsText || null,
      contactEmail: contactEmail || null,
    })
  }

  const onSubmit = handleSubmit((values) => {
    setSuccessMessage(null)

    const payload: Partial<UpdateOperationalConfigRequest> = {}
    if (dirtyFields.commissionFactor) payload.commissionFactor = values.commissionFactor
    if (dirtyFields.avgPeriods) payload.avgPeriods = values.avgPeriods
    if (dirtyFields.newsCadenceMinutes) payload.newsCadenceMinutes = values.newsCadenceMinutes
    if (dirtyFields.fibraNewsMonths) payload.fibraNewsMonths = values.fibraNewsMonths
    if (dirtyFields.fundamentalsCadenceMinutes) payload.fundamentalsCadenceMinutes = values.fundamentalsCadenceMinutes
    if (dirtyFields.distributionCadenceMinutes) payload.distributionCadenceMinutes = values.distributionCadenceMinutes
    if (dirtyFields.universeDegradationThresholdPct) payload.universeDegradationThresholdPct = values.universeDegradationThresholdPct

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
                    min: { value: 0.0001, message: 'Mínimo 0.0001.' },
                    max: { value: 0.1, message: 'Máximo 0.1.' },
                    valueAsNumber: true,
                  })}
                  className={inputClassName}
                  step="0.0001"
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

              <Field label="Umbral de degradación del universo (%)" error={errors.universeDegradationThresholdPct?.message} required>
                <input
                  {...register('universeDegradationThresholdPct', {
                    required: 'El umbral de degradación es requerido.',
                    min: { value: 1, message: 'Mínimo 1%.' },
                    max: { value: 49, message: 'Máximo 49% (el umbral de suspensión es fijo en 50%).' },
                    valueAsNumber: true,
                  })}
                  className={inputClassName}
                  type="number"
                  min={1}
                  max={49}
                />
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

      {/* ── Contenido del sitio ───────────────────────────────────────────── */}
      <section className="rounded-[1.75rem] border border-slate-200 bg-white/90 p-6 shadow-sm">
        <h2 className="text-lg font-semibold tracking-tight text-slate-950">Contenido del sitio</h2>
        <p className="mt-1 text-sm text-slate-500">
          Términos y condiciones + correo de contacto que aparecen en el footer de Main.
        </p>

        <div className="mt-6 space-y-5">
          <div className="flex items-center gap-3">
            <input
              checked={termsEnabled}
              className="h-4 w-4 rounded border-slate-300 accent-teal-600"
              id="terms-enabled"
              onChange={(e) => setTermsEnabled(e.target.checked)}
              type="checkbox"
            />
            <label className="text-sm font-medium text-slate-700" htmlFor="terms-enabled">
              Activar modal de términos y condiciones al primer login
            </label>
          </div>

          <div className="flex flex-col gap-1.5">
            <label className="text-sm font-medium text-slate-700" htmlFor="contact-email">
              Correo de contacto (footer)
            </label>
            <input
              className="w-full rounded-xl border border-slate-200 bg-white px-3 py-2.5 text-sm text-slate-900 outline-none transition focus:border-teal-600"
              id="contact-email"
              onChange={(e) => setContactEmail(e.target.value)}
              placeholder="portafoliodefibras@gmail.com"
              type="email"
              value={contactEmail}
            />
          </div>

          <div className="flex flex-col gap-1.5">
            <div className="flex items-center justify-between">
              <label className="text-sm font-medium text-slate-700" htmlFor="terms-text">
                Texto de términos y condiciones
              </label>
              <button
                className="text-xs text-teal-600 hover:underline"
                onClick={() => setTermsText(DEFAULT_TERMS_TEXT)}
                type="button"
              >
                Restaurar texto por defecto
              </button>
            </div>
            <textarea
              className="min-h-[280px] w-full rounded-xl border border-slate-200 bg-white px-3 py-2.5 text-sm text-slate-900 outline-none transition focus:border-teal-600"
              id="terms-text"
              onChange={(e) => setTermsText(e.target.value)}
              placeholder="Escribe aquí los términos..."
              value={termsText}
            />
          </div>

          {siteValidationError ? (
            <p className="rounded-xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">
              {siteValidationError}
            </p>
          ) : null}
          {siteSuccessMessage ? (
            <p className="rounded-xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-800">
              {siteSuccessMessage}
            </p>
          ) : null}
          {saveSiteMutation.isError ? (
            <p className="rounded-xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">
              {saveSiteMutation.error.message}
            </p>
          ) : null}

          <button
            className="rounded-xl bg-teal-700 px-5 py-2.5 text-sm font-semibold text-white transition hover:bg-teal-800 disabled:cursor-not-allowed disabled:opacity-60"
            disabled={saveSiteMutation.isPending}
            onClick={handleSaveSite}
            type="button"
          >
            {saveSiteMutation.isPending ? 'Guardando...' : 'Guardar contenido'}
          </button>
        </div>
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
