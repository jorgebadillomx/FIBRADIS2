import type { FormEvent, ReactNode } from 'react'
import { useMemo, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  createOpsDistribution,
  deleteOpsDistribution,
  fetchOpsDistributions,
  syncOpsDistributions,
  updateOpsDistribution,
  type DistributionAdminDto,
  type DistributionUpsertRequest,
} from '@/api/distributionsApi'
import { fetchOpsCatalog } from '@/api/catalogApi'

type DistributionFormState = {
  ticker: string
  paymentDate: string
  exDividendDate: string
  amountPerUnit: string
  taxableAmount: string
  capitalReturnAmount: string
  avisoUrl: string
}

const EMPTY_FORM: DistributionFormState = {
  ticker: '',
  paymentDate: '',
  exDividendDate: '',
  amountPerUnit: '',
  taxableAmount: '',
  capitalReturnAmount: '',
  avisoUrl: '',
}

const inputClass =
  'h-10 rounded-2xl border border-slate-200 bg-slate-50 px-4 text-sm text-slate-900 placeholder-slate-400 focus:border-teal-500 focus:outline-none focus:ring-2 focus:ring-teal-500/30'

export function DistributionsPage() {
  const queryClient = useQueryClient()
  const [editingId, setEditingId] = useState<string | null>(null)
  const [form, setForm] = useState(EMPTY_FORM)
  const [formError, setFormError] = useState<string | null>(null)
  const [isFormOpen, setIsFormOpen] = useState(false)

  const catalogQuery = useQuery({
    queryKey: ['ops-catalog'],
    queryFn: fetchOpsCatalog,
    staleTime: 5 * 60_000,
    retry: false,
  })

  const activeFibras = useMemo(
    () => (catalogQuery.data ?? []).filter((f) => f.isActive).sort((a, b) => a.ticker.localeCompare(b.ticker)),
    [catalogQuery.data],
  )

  const distributionsQuery = useQuery({
    queryKey: ['ops-distributions'],
    queryFn: fetchOpsDistributions,
    staleTime: 60_000,
    retry: false,
  })

  const orderedDistributions = useMemo(
    () => [...(distributionsQuery.data ?? [])].sort((left, right) => right.paymentDate.localeCompare(left.paymentDate) || left.ticker.localeCompare(right.ticker)),
    [distributionsQuery.data],
  )

  const summary = useMemo(() => buildSummary(orderedDistributions), [orderedDistributions])

  const syncMutation = useMutation({
    mutationFn: syncOpsDistributions,
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['ops-distributions'] })
    },
  })

  const saveMutation = useMutation({
    mutationFn: async (payload: { id: string | null; body: DistributionUpsertRequest }) => {
      return payload.id
        ? updateOpsDistribution(payload.id, payload.body)
        : createOpsDistribution(payload.body)
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['ops-distributions'] })
      resetForm()
    },
  })

  const deleteMutation = useMutation({
    mutationFn: deleteOpsDistribution,
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['ops-distributions'] })
      if (editingId) resetForm()
    },
  })

  function resetForm() {
    setEditingId(null)
    setForm(EMPTY_FORM)
    setFormError(null)
    setIsFormOpen(false)
  }

  function openCreate() {
    setEditingId(null)
    setForm(EMPTY_FORM)
    setFormError(null)
    setIsFormOpen(true)
  }

  function startEdit(distribution: DistributionAdminDto) {
    setEditingId(distribution.id)
    setForm({
      ticker: distribution.ticker,
      paymentDate: distribution.paymentDate,
      exDividendDate: distribution.exDividendDate ?? '',
      amountPerUnit: distribution.amountPerUnit.toString(),
      taxableAmount: distribution.taxableAmount?.toString() ?? '',
      capitalReturnAmount: distribution.capitalReturnAmount?.toString() ?? '',
      avisoUrl: distribution.avisoUrl ?? '',
    })
    setFormError(null)
    setIsFormOpen(true)
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setFormError(null)

    try {
      const body = buildPayload(form)
      await saveMutation.mutateAsync({ id: editingId, body })
    } catch (error) {
      setFormError(error instanceof Error ? error.message : 'No se pudo guardar la distribución.')
    }
  }

  async function handleDelete(distribution: DistributionAdminDto) {
    const confirmed = window.confirm(`Eliminar la distribución de ${distribution.ticker} del ${distribution.paymentDate}?`)
    if (!confirmed) return

    try {
      await deleteMutation.mutateAsync(distribution.id)
    } catch (error) {
      setFormError(error instanceof Error ? error.message : 'No se pudo eliminar la distribución.')
    }
  }

  const isBusy = syncMutation.isPending || saveMutation.isPending || deleteMutation.isPending

  return (
    <section className="space-y-6">
      {isFormOpen ? (
        <div className="fixed inset-0 z-50 flex items-start justify-center overflow-y-auto bg-slate-900/40 p-4 backdrop-blur-sm" onClick={resetForm}>
          <div className="mt-12 w-full max-w-lg rounded-[1.75rem] border border-slate-200 bg-white p-6 shadow-xl" onClick={(e) => e.stopPropagation()}>
            <div className="flex items-start justify-between gap-3">
              <div>
                <p className="text-xs font-semibold uppercase tracking-[0.22em] text-slate-500">
                  {editingId ? 'Editar distribución' : 'Nueva distribución'}
                </p>
                <h3 className="mt-1 text-xl font-semibold tracking-tight text-slate-950">
                  {editingId ? 'Ajuste manual' : 'Captura manual'}
                </h3>
              </div>
              <button
                type="button"
                className="rounded-full border border-slate-200 px-3 py-1 text-xs font-semibold text-slate-600 transition hover:bg-slate-50"
                onClick={resetForm}
              >
                Cancelar
              </button>
            </div>

            <form className="mt-5 space-y-4" onSubmit={handleSubmit}>
              <Field label="FIBRA" required>
                <select
                  className={inputClass}
                  disabled={isBusy || catalogQuery.isLoading}
                  onChange={(e) => setForm((s) => ({ ...s, ticker: e.target.value }))}
                  required
                  title="FIBRA"
                  value={form.ticker}
                >
                  <option value="">— Selecciona una FIBRA —</option>
                  {activeFibras.map((f) => (
                    <option key={f.ticker} value={f.ticker}>
                      {f.ticker} — {f.fullName}
                    </option>
                  ))}
                </select>
              </Field>

              <div className="grid gap-4 sm:grid-cols-2">
                <Field label="Fecha de pago" required>
                  <input
                    className={inputClass}
                    disabled={isBusy}
                    onChange={(e) => setForm((s) => ({ ...s, paymentDate: e.target.value }))}
                    required
                    title="Fecha de pago"
                    type="date"
                    value={form.paymentDate}
                  />
                </Field>

                <Field label="Fecha ex derecho">
                  <input
                    className={inputClass}
                    disabled={isBusy}
                    onChange={(e) => setForm((s) => ({ ...s, exDividendDate: e.target.value }))}
                    title="Fecha ex derecho"
                    type="date"
                    value={form.exDividendDate}
                  />
                </Field>
              </div>

              <Field label="Monto por unidad" required>
                <input
                  className={inputClass}
                  disabled={isBusy}
                  onChange={(e) => setForm((s) => ({ ...s, amountPerUnit: e.target.value }))}
                  inputMode="decimal"
                  placeholder="0.1234"
                  required
                  step="0.0001"
                  type="number"
                  value={form.amountPerUnit}
                />
              </Field>

              <div className="grid gap-4 sm:grid-cols-2">
                <Field label="Monto fiscal">
                  <input
                    className={inputClass}
                    disabled={isBusy}
                    onChange={(e) => setForm((s) => ({ ...s, taxableAmount: e.target.value }))}
                    inputMode="decimal"
                    placeholder="0.0000"
                    step="0.0001"
                    type="number"
                    value={form.taxableAmount}
                  />
                </Field>

                <Field label="Monto capital">
                  <input
                    className={inputClass}
                    disabled={isBusy}
                    onChange={(e) => setForm((s) => ({ ...s, capitalReturnAmount: e.target.value }))}
                    inputMode="decimal"
                    placeholder="0.0000"
                    step="0.0001"
                    type="number"
                    value={form.capitalReturnAmount}
                  />
                </Field>
              </div>

              <Field label="Aviso BMV">
                <input
                  className={inputClass}
                  disabled={isBusy}
                  onChange={(e) => setForm((s) => ({ ...s, avisoUrl: e.target.value }))}
                  placeholder="https://www.bmv.com.mx/..."
                  type="url"
                  value={form.avisoUrl}
                />
              </Field>

              {formError ? <p className="text-sm text-rose-700">{formError}</p> : null}

              <div className="flex flex-wrap gap-3">
                <button
                  className="inline-flex h-10 items-center justify-center rounded-2xl bg-slate-950 px-4 text-sm font-semibold text-white transition hover:bg-slate-800 disabled:cursor-not-allowed disabled:opacity-60"
                  disabled={isBusy}
                  type="submit"
                >
                  {saveMutation.isPending ? 'Guardando...' : editingId ? 'Actualizar' : 'Crear'}
                </button>
                <button
                  className="inline-flex h-10 items-center justify-center rounded-2xl border border-slate-200 px-4 text-sm font-semibold text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-60"
                  disabled={isBusy}
                  onClick={resetForm}
                  type="button"
                >
                  Cancelar
                </button>
              </div>
            </form>
          </div>
        </div>
      ) : null}

      <div className="rounded-[1.75rem] border border-slate-200 bg-[linear-gradient(135deg,_rgba(15,118,110,0.10),_rgba(255,255,255,0.96)_42%,_rgba(245,158,11,0.05))] p-6 shadow-sm">
        <p className="text-xs font-semibold uppercase tracking-[0.28em] text-teal-700">Operación de calendario</p>
        <h2 className="mt-2 text-2xl font-semibold tracking-tight text-slate-950">Distribuciones</h2>
        <p className="mt-3 max-w-3xl text-sm leading-7 text-slate-600">
          Revisa, corrige y crea distribuciones manuales. La sincronización vuelve a cruzar Yahoo y MasDividendos antes de abrir la edición directa.
        </p>
      </div>

      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <MetricCard label="Distribuciones" value={summary.total} />
        <MetricCard label="FIBRAs" value={summary.tickers} />
        <MetricCard label="Importadas" value={summary.imported} />
        <MetricCard label="Manuales" value={summary.manual} />
      </div>

      <section className="rounded-[1.75rem] border border-slate-200 bg-white/92 p-5 shadow-sm">
          <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
            <div>
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-slate-500">Base operativa</p>
              <h3 className="mt-1 text-xl font-semibold tracking-tight text-slate-950">Últimas distribuciones</h3>
            </div>

            <div className="flex flex-wrap gap-2">
              <button
                className="inline-flex h-10 items-center justify-center rounded-2xl bg-slate-950 px-4 text-sm font-semibold text-white transition hover:bg-slate-800"
                onClick={openCreate}
                type="button"
              >
                Nueva distribución
              </button>
              <button
                className="inline-flex h-10 items-center justify-center rounded-2xl border border-teal-200 bg-teal-50 px-4 text-sm font-semibold text-teal-800 transition hover:border-teal-300 hover:bg-teal-100 disabled:cursor-not-allowed disabled:opacity-60"
                disabled={syncMutation.isPending}
                onClick={() => syncMutation.mutate()}
                type="button"
              >
                {syncMutation.isPending ? 'Sincronizando...' : 'Sincronizar ahora'}
              </button>
            </div>
          </div>

          {syncMutation.isError ? (
            <p className="mt-3 text-sm text-rose-700">{syncMutation.error.message}</p>
          ) : null}

          {distributionsQuery.isLoading ? (
            <p className="mt-5 text-sm text-slate-500">Cargando distribuciones...</p>
          ) : distributionsQuery.isError ? (
            <p className="mt-5 text-sm text-rose-700">{distributionsQuery.error.message}</p>
          ) : orderedDistributions.length === 0 ? (
            <p className="mt-5 text-sm text-slate-500">No hay distribuciones registradas todavía.</p>
          ) : (
            <div className="mt-5 overflow-x-auto rounded-2xl border border-slate-200">
              <table className="min-w-full border-collapse text-sm">
                <thead className="bg-slate-950 text-left text-[11px] uppercase tracking-[0.18em] text-slate-100">
                  <tr>
                    <th className="px-4 py-3 font-medium">Ticker</th>
                    <th className="px-4 py-3 font-medium">Empresa</th>
                    <th className="px-4 py-3 font-medium">Pago</th>
                    <th className="px-4 py-3 font-medium">Ex derecho</th>
                    <th className="px-4 py-3 font-medium">Monto</th>
                    <th className="px-4 py-3 font-medium">Fuente</th>
                    <th className="px-4 py-3 font-medium">Capturado</th>
                    <th className="px-4 py-3 font-medium">Acciones</th>
                  </tr>
                </thead>
                <tbody className="bg-white">
                  {orderedDistributions.map((distribution) => (
                    <tr className="border-t border-slate-200 text-slate-700" key={distribution.id}>
                      <td className="px-4 py-3 font-semibold text-slate-950">{distribution.ticker}</td>
                      <td className="px-4 py-3">{distribution.empresa}</td>
                      <td className="px-4 py-3">{formatDateOnly(distribution.paymentDate)}</td>
                      <td className="px-4 py-3">{distribution.exDividendDate ? formatDateOnly(distribution.exDividendDate) : '—'}</td>
                      <td className="px-4 py-3">
                        <div className="space-y-1">
                          <p className="font-semibold text-slate-950">{formatCurrency(distribution.amountPerUnit)}</p>
                          <p className="text-xs text-slate-500">{formatBreakdown(distribution.taxableAmount, distribution.capitalReturnAmount)}</p>
                        </div>
                      </td>
                      <td className="px-4 py-3">
                        <span className={sourceBadgeClass(distribution.source)}>{distribution.source}</span>
                      </td>
                      <td className="px-4 py-3 text-slate-500">{formatDateTime(distribution.capturedAt)}</td>
                      <td className="px-4 py-3">
                        <div className="flex gap-2">
                          <button
                            className="rounded-xl bg-slate-100 px-3 py-1 text-xs font-semibold text-slate-700 transition hover:bg-slate-200"
                            onClick={() => startEdit(distribution)}
                            type="button"
                          >
                            Editar
                          </button>
                          <button
                            className="rounded-xl bg-rose-100 px-3 py-1 text-xs font-semibold text-rose-700 transition hover:bg-rose-200 disabled:opacity-60"
                            disabled={deleteMutation.isPending}
                            onClick={() => void handleDelete(distribution)}
                            type="button"
                          >
                            Eliminar
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </section>
    </section>
  )
}

function Field({ label, required = false, children }: { label: string; required?: boolean; children: ReactNode }) {
  return (
    <label className="flex flex-col gap-1.5">
      <span className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-500">
        {label}{required ? ' *' : ''}
      </span>
      {children}
    </label>
  )
}

function MetricCard({ label, value }: { label: string; value: number }) {
  return (
    <div className="rounded-[1.5rem] border border-slate-200 bg-white/90 p-4 shadow-sm">
      <p className="text-[11px] font-semibold uppercase tracking-[0.24em] text-slate-500">{label}</p>
      <p className="mt-2 text-2xl font-semibold tracking-tight text-slate-950">{value}</p>
    </div>
  )
}

function buildPayload(form: DistributionFormState): DistributionUpsertRequest {
  const ticker = form.ticker.trim().toUpperCase()
  if (!ticker) throw new Error('El ticker es obligatorio.')
  if (!form.paymentDate) throw new Error('La fecha de pago es obligatoria.')

  const amountPerUnit = parseRequiredNumber(form.amountPerUnit, 'Monto por unidad')

  return {
    ticker,
    paymentDate: form.paymentDate,
    exDividendDate: form.exDividendDate.trim() || null,
    amountPerUnit,
    taxableAmount: parseOptionalNumber(form.taxableAmount, 'Monto fiscal'),
    capitalReturnAmount: parseOptionalNumber(form.capitalReturnAmount, 'Monto capital'),
    avisoUrl: normalizeAvisoUrl(form.avisoUrl),
  }
}

function parseOptionalNumber(value: string, label: string): number | null {
  const trimmed = value.trim()
  if (!trimmed) return null

  const parsed = Number(trimmed)
  if (!Number.isFinite(parsed)) throw new Error(`${label} debe ser un número válido.`)
  return parsed
}

function parseRequiredNumber(value: string, label: string): number {
  const parsed = parseOptionalNumber(value, label)
  if (parsed == null) throw new Error(`${label} es obligatorio.`)
  return parsed
}

function normalizeAvisoUrl(value: string): string | null {
  const trimmed = value.trim()
  if (!trimmed) return null

  let url: URL
  try {
    url = new URL(trimmed)
  } catch {
    throw new Error('El aviso BMV debe ser una URL válida.')
  }

  if (!url.host.endsWith('bmv.com.mx')) {
    throw new Error('El aviso BMV debe pertenecer al dominio bmv.com.mx.')
  }

  return url.toString()
}

function formatCurrency(value: number | string) {
  const numericValue = normalizeNumber(value)
  return numericValue == null
    ? '—'
    : numericValue.toLocaleString('es-MX', { style: 'currency', currency: 'MXN' })
}

function formatBreakdown(taxableAmount: number | string | null, capitalReturnAmount: number | string | null) {
  const taxable = normalizeNumber(taxableAmount)
  const capital = normalizeNumber(capitalReturnAmount)
  const parts: string[] = []
  if (taxable != null) parts.push(`Fiscal $${taxable.toFixed(4)}`)
  if (capital != null) parts.push(`Capital $${capital.toFixed(4)}`)
  return parts.length > 0 ? parts.join(' · ') : 'Sin desglose'
}

function formatDateOnly(value: string) {
  return new Date(`${value}T12:00:00`).toLocaleDateString('es-MX', { dateStyle: 'medium' })
}

function formatDateTime(value: string) {
  return new Date(value).toLocaleString('es-MX', { dateStyle: 'medium', timeStyle: 'short' })
}

function buildSummary(distributions: DistributionAdminDto[]) {
  const imported = distributions.filter((item) => item.source.toLowerCase() === 'yahoo' || item.source.toLowerCase() === 'masdividendos').length
  return {
    total: distributions.length,
    tickers: new Set(distributions.map((item) => item.ticker)).size,
    imported,
    manual: distributions.length - imported,
  }
}

function sourceBadgeClass(source: string) {
  const normalized = source.toLowerCase()
  if (normalized === 'yahoo') return 'rounded-full bg-emerald-100 px-2.5 py-1 text-xs font-semibold text-emerald-800'
  if (normalized === 'masdividendos') return 'rounded-full bg-amber-100 px-2.5 py-1 text-xs font-semibold text-amber-800'
  return 'rounded-full bg-slate-100 px-2.5 py-1 text-xs font-semibold text-slate-700'
}

function normalizeNumber(value: number | string | null | undefined) {
  if (value == null) return null
  const numericValue = typeof value === 'number' ? value : Number(value)
  return Number.isFinite(numericValue) ? numericValue : null
}
