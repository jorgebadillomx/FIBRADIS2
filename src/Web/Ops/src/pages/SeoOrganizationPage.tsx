import { useEffect, useMemo, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { fetchSeoOrganization, updateSeoOrganization } from '@/api/seoOrganizationApi'

function splitLines(value: string): string[] {
  return value
    .split(/\r?\n/)
    .map((line) => line.trim())
    .filter(Boolean)
}

function formatLines(value: string[]): string {
  return value.join('\n')
}

function validateUrls(urls: string[]): string | null {
  const seen = new Set<string>()
  for (const url of urls) {
    try {
      const parsed = new URL(url)
      if (parsed.protocol !== 'http:' && parsed.protocol !== 'https:') {
        return 'Cada URL debe usar http o https.'
      }
      const normalized = parsed.toString().toLowerCase()
      if (seen.has(normalized)) {
        return 'No se permiten URLs duplicadas.'
      }
      seen.add(normalized)
    } catch {
      return `URL inválida: ${url}`
    }
  }

  return null
}

export function SeoOrganizationPage() {
  const queryClient = useQueryClient()
  const organizationQuery = useQuery({
    queryKey: ['ops', 'seo', 'organization'],
    queryFn: fetchSeoOrganization,
    retry: false,
    staleTime: 5 * 60_000,
  })

  const initialValue = useMemo(
    () => formatLines(organizationQuery.data?.sameAs ?? []),
    [organizationQuery.data?.sameAs],
  )
  const [draft, setDraft] = useState('')
  const [feedback, setFeedback] = useState<string | null>(null)

  useEffect(() => {
    setDraft(initialValue)
  }, [initialValue])

  const saveMutation = useMutation({
    mutationFn: updateSeoOrganization,
    onSuccess: async (result) => {
      setFeedback(`Perfiles guardados: ${result.sameAs.length}.`)
      await queryClient.invalidateQueries({ queryKey: ['ops', 'seo', 'organization'] })
    },
  })

  function handleSave() {
    setFeedback(null)
    const urls = splitLines(draft)
    const validationError = validateUrls(urls)
    if (validationError) {
      setFeedback(validationError)
      return
    }

    saveMutation.mutate({ sameAs: urls })
  }

  return (
    <div className="space-y-6">
      <section className="rounded-[1.75rem] border border-slate-200 bg-white/90 p-6 shadow-sm">
        <p className="text-xs font-semibold uppercase tracking-[0.28em] text-teal-700">Ops / SEO Organization</p>
        <h1 className="mt-1 text-2xl font-semibold tracking-tight text-slate-950">
          Perfiles oficiales para `sameAs`
        </h1>
        <p className="mt-2 max-w-3xl text-sm leading-6 text-slate-500">
          Una URL por línea. Solo perfiles oficiales verificados. Si no hay URLs, `sameAs` se omite del JSON-LD.
        </p>

        {organizationQuery.isLoading ? <p className="mt-6 text-sm text-slate-500">Cargando perfiles...</p> : null}
        {organizationQuery.isError ? (
          <p className="mt-6 rounded-xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">
            {organizationQuery.error.message}
          </p>
        ) : null}

        {organizationQuery.data ? (
          <div className="mt-6 grid gap-4 xl:grid-cols-[minmax(0,1fr)_20rem]">
            <div className="space-y-2">
              <label className="text-sm font-medium text-slate-700" htmlFor="same-as">
                URLs verificadas
              </label>
              <textarea
                className="min-h-[18rem] w-full rounded-2xl border border-slate-200 bg-white px-4 py-3 font-mono text-sm leading-6 text-slate-900 outline-none transition focus:border-teal-600"
                id="same-as"
                onChange={(event) => setDraft(event.target.value)}
                placeholder="https://youtube.com/@fibrasinmobiliarias"
                value={draft}
              />
              <p className="text-xs leading-5 text-slate-500">
                Se guardan en `OperationalConfig` y se reflejan en el JSON-LD del home cuando el SSR del sitio lo consume.
              </p>
            </div>

            <div className="rounded-2xl border border-slate-200 bg-slate-50 p-4 text-sm text-slate-600">
              <p className="font-semibold text-slate-900">Estado actual</p>
              <p className="mt-2">
                Actualizado:{' '}
                {new Date(organizationQuery.data.updatedAt).toLocaleString('es-MX', {
                  dateStyle: 'medium',
                  timeStyle: 'short',
                })}
              </p>
              <p className="mt-1">{organizationQuery.data.updatedBy ? `Por ${organizationQuery.data.updatedBy}` : 'Sin actor registrado'}</p>
              <p className="mt-4 text-xs leading-5 text-slate-500">
                Si el campo queda vacío, la señal `sameAs` no se emite.
              </p>
            </div>
          </div>
        ) : null}

        {feedback ? (
          <p className="mt-4 rounded-xl border border-teal-200 bg-teal-50 px-4 py-3 text-sm text-teal-800">
            {feedback}
          </p>
        ) : null}

        {saveMutation.isError ? (
          <p className="mt-4 rounded-xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">
            {saveMutation.error.message}
          </p>
        ) : null}

        <div className="mt-5 flex items-center justify-between gap-3">
          <p className="text-sm text-slate-500">
            {splitLines(draft).length > 0 ? `${splitLines(draft).length} URL(s) listas` : 'Sin perfiles guardados.'}
          </p>
          <button
            className="rounded-xl bg-teal-700 px-5 py-2.5 text-sm font-semibold text-white transition hover:bg-teal-800 disabled:cursor-not-allowed disabled:opacity-60"
            disabled={saveMutation.isPending}
            onClick={handleSave}
            type="button"
          >
            {saveMutation.isPending ? 'Guardando...' : 'Guardar perfiles'}
          </button>
        </div>
      </section>
    </div>
  )
}
