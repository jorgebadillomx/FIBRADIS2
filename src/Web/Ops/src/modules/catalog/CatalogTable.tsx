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
                  <div className="flex justify-end gap-1">
                    <button
                      aria-label={`Editar ${fibra.ticker}`}
                      className="rounded p-1.5 text-slate-400 hover:text-teal-700 hover:bg-teal-50 transition"
                      onClick={() => onEdit(fibra)}
                      title="Editar"
                      type="button"
                    >
                      <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 16 16" fill="currentColor" className="size-4">
                        <path d="M11.013 1.427a1.75 1.75 0 0 1 2.474 2.474l-7.19 7.19a2.25 2.25 0 0 1-.95.55l-2.08.595a.75.75 0 0 1-.927-.927l.595-2.08a2.25 2.25 0 0 1 .55-.95l7.19-7.19Zm1.413 1.06a.25.25 0 0 0-.353 0l-.69.69 1.414 1.414.69-.69a.25.25 0 0 0 0-.353l-1.06-1.06ZM11.09 5.65 9.676 4.237 4.546 9.367a.75.75 0 0 0-.183.316l-.279.976.976-.279a.75.75 0 0 0 .316-.183l5.715-4.547Z" />
                        <path d="M2.5 13.25A1.75 1.75 0 0 0 4.25 15h7.5a.75.75 0 0 0 0-1.5h-7.5a.25.25 0 0 1-.25-.25v-7.5a.75.75 0 0 0-1.5 0v7.5Z" />
                      </svg>
                    </button>
                    <button
                      aria-label={isActive ? `Desactivar ${fibra.ticker}` : `Activar ${fibra.ticker}`}
                      className={`rounded p-1.5 transition disabled:cursor-not-allowed disabled:opacity-40 ${
                        isActive
                          ? 'text-slate-400 hover:text-rose-600 hover:bg-rose-50'
                          : 'text-slate-400 hover:text-teal-700 hover:bg-teal-50'
                      }`}
                      disabled={statePending}
                      onClick={() =>
                        isActive
                          ? deactivateMutation.mutate(fibra.ticker)
                          : activateMutation.mutate(fibra.ticker)
                      }
                      title={isActive ? 'Desactivar' : 'Activar'}
                      type="button"
                    >
                      {isActive ? (
                        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 16 16" fill="currentColor" className="size-4">
                          <path d="M5.28 4.22a.75.75 0 0 0-1.06 1.06L6.94 8l-2.72 2.72a.75.75 0 1 0 1.06 1.06L8 9.06l2.72 2.72a.75.75 0 1 0 1.06-1.06L9.06 8l2.72-2.72a.75.75 0 0 0-1.06-1.06L8 6.94 5.28 4.22Z" />
                        </svg>
                      ) : (
                        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 16 16" fill="currentColor" className="size-4">
                          <path fillRule="evenodd" d="M12.416 3.376a.75.75 0 0 1 .208 1.04l-5 7.5a.75.75 0 0 1-1.154.114l-3-3a.75.75 0 0 1 1.06-1.06l2.353 2.353 4.493-6.74a.75.75 0 0 1 1.04-.207Z" clipRule="evenodd" />
                        </svg>
                      )}
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
