import { useMutation, useQueryClient } from '@tanstack/react-query'
import { activateFibra, deactivateFibra, type FibraDetail } from '@/api/catalogApi'

interface Props {
  fibras: FibraDetail[]
  onEdit: (fibra: FibraDetail) => void
  onToggleState: (fibra: FibraDetail) => void
}

export function CatalogTable({ fibras, onEdit, onToggleState }: Props) {
  const queryClient = useQueryClient()

  const activateMutation = useMutation({
    mutationFn: activateFibra,
    onSuccess: async (fibra) => {
      await queryClient.invalidateQueries({ queryKey: ['ops-catalog'] })
      onToggleState(fibra)
    },
  })

  const deactivateMutation = useMutation({
    mutationFn: deactivateFibra,
    onSuccess: async (fibra) => {
      await queryClient.invalidateQueries({ queryKey: ['ops-catalog'] })
      onToggleState(fibra)
    },
  })

  const pendingTicker = activateMutation.isPending
    ? activateMutation.variables
    : deactivateMutation.isPending
      ? deactivateMutation.variables
      : null

  return (
    <div className="overflow-hidden rounded-[1.5rem] border border-slate-200 bg-white shadow-sm">
      <table className="min-w-full border-collapse">
        <thead className="bg-slate-950 text-left text-xs uppercase tracking-[0.18em] text-slate-50">
          <tr>
            <th className="px-4 py-3 font-medium">Ticker</th>
            <th className="px-4 py-3 font-medium">Nombre completo</th>
            <th className="px-4 py-3 font-medium">Sector</th>
            <th className="px-4 py-3 font-medium">Mercado</th>
            <th className="px-4 py-3 font-medium">Moneda</th>
            <th className="px-4 py-3 font-medium">Estado</th>
            <th className="px-4 py-3 font-medium">Descripción</th>
            <th className="px-4 py-3 font-medium text-right">Acciones</th>
          </tr>
        </thead>
        <tbody className="bg-white">
          {fibras.length === 0 ? (
            <tr>
              <td className="px-4 py-6 text-sm text-slate-500" colSpan={8}>
                No hay FIBRAs registradas.
              </td>
            </tr>
          ) : null}

          {fibras.map((fibra) => {
            const statePending =
              pendingTicker === fibra.ticker && (activateMutation.isPending || deactivateMutation.isPending)
            const isActive = fibra.state === 'Active'

            return (
              <tr className="border-t border-slate-200 text-sm text-slate-700" key={fibra.id}>
                <td className="px-4 py-4 font-semibold text-slate-950">{fibra.ticker}</td>
                <td className="px-4 py-4">{fibra.fullName}</td>
                <td className="px-4 py-4">{fibra.sector}</td>
                <td className="px-4 py-4">{fibra.market}</td>
                <td className="px-4 py-4">{fibra.currency}</td>
                <td className="px-4 py-4">
                  <span
                    className={`inline-flex rounded-full px-3 py-1 text-xs font-semibold ${
                      isActive ? 'bg-emerald-100 text-emerald-700' : 'bg-slate-200 text-slate-700'
                    }`}
                  >
                    {fibra.state}
                  </span>
                </td>
                <td className="px-4 py-4">
                  <span
                    className={`inline-flex rounded-full px-3 py-1 text-xs font-semibold ${
                      fibra.description
                        ? 'bg-emerald-100 text-emerald-700'
                        : 'bg-slate-200 text-slate-500'
                    }`}
                  >
                    {fibra.description ? 'Con texto' : 'Sin texto'}
                  </span>
                </td>
                <td className="px-4 py-4">
                  <div className="flex justify-end gap-2">
                    <button
                      className="rounded-lg border border-slate-300 px-3 py-2 font-medium text-slate-700 transition hover:border-slate-400 hover:bg-slate-50"
                      onClick={() => onEdit(fibra)}
                      type="button"
                    >
                      Editar
                    </button>
                    <button
                      className={`rounded-lg px-3 py-2 font-medium text-white transition disabled:cursor-not-allowed disabled:opacity-60 ${
                        isActive ? 'bg-rose-600 hover:bg-rose-700' : 'bg-teal-700 hover:bg-teal-800'
                      }`}
                      disabled={statePending}
                      onClick={() =>
                        isActive
                          ? deactivateMutation.mutate(fibra.ticker)
                          : activateMutation.mutate(fibra.ticker)
                      }
                      type="button"
                    >
                      {statePending ? 'Guardando...' : isActive ? 'Desactivar' : 'Activar'}
                    </button>
                  </div>
                </td>
              </tr>
            )
          })}
        </tbody>
      </table>

      {activateMutation.isError ? (
        <p className="border-t border-slate-200 px-4 py-3 text-sm text-rose-700">{activateMutation.error.message}</p>
      ) : null}
      {deactivateMutation.isError ? (
        <p className="border-t border-slate-200 px-4 py-3 text-sm text-rose-700">{deactivateMutation.error.message}</p>
      ) : null}
    </div>
  )
}
