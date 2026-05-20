import { useState } from 'react'
import type { FormEvent } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { AiModeSection } from '@/modules/ai-mode/AiModeSection'
import { createBlocklistTerm, deleteBlocklistTerm, fetchBlocklistTerms } from '@/api/newsApi'

function App() {
  const queryClient = useQueryClient()
  const [term, setTerm] = useState('')

  const blocklistQuery = useQuery({
    queryKey: ['news-blocklist-terms'],
    queryFn: fetchBlocklistTerms,
  })

  const createMutation = useMutation({
    mutationFn: createBlocklistTerm,
    onSuccess: async () => {
      setTerm('')
      await queryClient.invalidateQueries({ queryKey: ['news-blocklist-terms'] })
    },
  })

  const deleteMutation = useMutation({
    mutationFn: deleteBlocklistTerm,
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['news-blocklist-terms'] })
    },
  })

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const nextTerm = term.trim()
    if (nextTerm.length === 0 || createMutation.isPending) return
    createMutation.mutate(nextTerm)
  }

  return (
    <div className="min-h-screen bg-[radial-gradient(circle_at_top,_rgba(15,118,110,0.12),_transparent_45%),linear-gradient(180deg,_#f8fafc_0%,_#eef6f3_100%)] text-foreground">
      <header className="border-b border-border/80 bg-white/80 px-6 py-5 backdrop-blur">
        <div className="mx-auto flex w-full max-w-5xl items-center justify-between gap-4">
          <div>
            <p className="text-xs font-semibold uppercase tracking-[0.24em] text-teal-700">AdminOps</p>
            <h1 className="text-xl font-semibold tracking-tight">FIBRADIS Ops</h1>
          </div>
          <p className="text-sm text-muted-foreground">Configuración operativa de noticias</p>
        </div>
      </header>
      <main className="mx-auto flex w-full max-w-5xl flex-1 flex-col gap-6 px-6 py-8">
        <AiModeSection />
        <section className="rounded-2xl border border-border/80 bg-white/90 p-6 shadow-sm">
          <div className="flex flex-col gap-2">
            <h2 className="text-lg font-semibold tracking-tight">Blocklist de noticias</h2>
            <p className="max-w-3xl text-sm text-muted-foreground">
              Los términos agregados aquí se aplican en el siguiente ciclo del pipeline sin redespliegue.
            </p>
          </div>

          <form className="mt-6 flex flex-col gap-3 md:flex-row" onSubmit={handleSubmit}>
            <label className="sr-only" htmlFor="blocklist-term">
              Nuevo término
            </label>
            <input
              id="blocklist-term"
              className="h-11 flex-1 rounded-xl border border-border bg-white px-4 text-sm outline-none ring-0 transition focus:border-teal-600"
              placeholder="Ej. fibra satelital"
              value={term}
              onChange={(event) => setTerm(event.target.value)}
            />
            <button
              className="h-11 rounded-xl bg-teal-700 px-5 text-sm font-medium text-white transition hover:bg-teal-800 disabled:cursor-not-allowed disabled:bg-teal-400"
              disabled={createMutation.isPending}
              type="submit"
            >
              {createMutation.isPending ? 'Guardando...' : 'Agregar término'}
            </button>
          </form>

          {createMutation.isError ? (
            <p className="mt-3 text-sm text-destructive">{createMutation.error.message}</p>
          ) : null}

          <div className="mt-6 overflow-hidden rounded-2xl border border-border/80">
            <table className="min-w-full border-collapse">
              <thead className="bg-teal-950/95 text-left text-xs uppercase tracking-[0.18em] text-teal-50">
                <tr>
                  <th className="px-4 py-3 font-medium">Término</th>
                  <th className="px-4 py-3 font-medium">Creado</th>
                  <th className="px-4 py-3 font-medium text-right">Acción</th>
                </tr>
              </thead>
              <tbody className="bg-white">
                {blocklistQuery.isLoading ? (
                  <tr>
                    <td className="px-4 py-6 text-sm text-muted-foreground" colSpan={3}>
                      Cargando términos...
                    </td>
                  </tr>
                ) : null}

                {blocklistQuery.isError ? (
                  <tr>
                    <td className="px-4 py-6 text-sm text-destructive" colSpan={3}>
                      {blocklistQuery.error.message}
                    </td>
                  </tr>
                ) : null}

                {blocklistQuery.isSuccess && blocklistQuery.data.length === 0 ? (
                  <tr>
                    <td className="px-4 py-6 text-sm text-muted-foreground" colSpan={3}>
                      No hay términos registrados.
                    </td>
                  </tr>
                ) : null}

                {blocklistQuery.data?.map((item) => (
                  <tr className="border-t border-border/70 text-sm" key={item.id}>
                    <td className="px-4 py-4 font-medium text-slate-900">{item.term}</td>
                    <td className="px-4 py-4 text-muted-foreground">
                      {new Date(item.createdAt).toLocaleString('es-MX', {
                        dateStyle: 'medium',
                        timeStyle: 'short',
                      })}
                    </td>
                    <td className="px-4 py-4 text-right">
                      <button
                        className="rounded-lg border border-rose-200 px-3 py-2 text-sm font-medium text-rose-700 transition hover:bg-rose-50 disabled:cursor-not-allowed disabled:opacity-60"
                        disabled={deleteMutation.isPending}
                        onClick={() => deleteMutation.mutate(item.id)}
                        type="button"
                      >
                        Eliminar
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {deleteMutation.isError ? (
            <p className="mt-3 text-sm text-destructive">{deleteMutation.error.message}</p>
          ) : null}
        </section>
      </main>
    </div>
  )
}

export default App
