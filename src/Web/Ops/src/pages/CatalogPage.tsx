import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { fetchOpsCatalog, type FibraDetail } from '@/api/catalogApi'
import { CatalogTable } from '@/modules/catalog/CatalogTable'
import { FibraForm } from '@/modules/catalog/FibraForm'

type Mode = 'list' | 'create' | 'edit'

interface State {
  mode: Mode
  selected: FibraDetail | null
}

export function CatalogPage() {
  const [state, setState] = useState<State>({ mode: 'list', selected: null })
  const [notice, setNotice] = useState<string | null>(null)

  const catalogQuery = useQuery({
    queryKey: ['ops-catalog'],
    queryFn: fetchOpsCatalog,
    retry: false,
  })

  const handleCreate = () => {
    setNotice(null)
    setState({ mode: 'create', selected: null })
  }

  const handleEdit = (fibra: FibraDetail) => {
    setNotice(null)
    setState({ mode: 'edit', selected: fibra })
  }

  const handleBackToList = (message?: string) => {
    setNotice(message ?? null)
    setState({ mode: 'list', selected: null })
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-4 rounded-[1.75rem] border border-slate-200 bg-white/90 p-6 shadow-sm lg:flex-row lg:items-end lg:justify-between">
        <div>
          <p className="text-xs font-semibold uppercase tracking-[0.28em] text-teal-700">Ops / Catálogo</p>
          <h1 className="mt-2 text-2xl font-semibold tracking-tight text-slate-950">Catálogo de FIBRAs</h1>
          <p className="mt-2 max-w-3xl text-sm leading-6 text-slate-500">
            Agrega, corrige y activa o desactiva emisoras sin tocar base de datos ni redesplegar.
          </p>
        </div>

        {state.mode === 'list' ? (
          <button
            className="rounded-xl bg-teal-700 px-5 py-3 text-sm font-semibold text-white transition hover:bg-teal-800"
            onClick={handleCreate}
            type="button"
          >
            Agregar FIBRA
          </button>
        ) : null}
      </div>

      {notice ? (
        <div className="rounded-2xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm font-medium text-emerald-800">
          {notice}
        </div>
      ) : null}

      {state.mode === 'list' ? (
        <>
          {catalogQuery.isLoading ? (
            <section className="rounded-[1.75rem] border border-slate-200 bg-white p-6 text-sm text-slate-500 shadow-sm">
              Cargando catálogo...
            </section>
          ) : null}

          {catalogQuery.isError ? (
            <section className="rounded-[1.75rem] border border-rose-200 bg-rose-50 p-6 text-sm text-rose-700 shadow-sm">
              {catalogQuery.error.message}
            </section>
          ) : null}

          {catalogQuery.isSuccess ? (
            <CatalogTable
              fibras={catalogQuery.data}
              onEdit={handleEdit}
              onToggleState={(fibra) =>
                handleBackToList(`✓ Estado actualizado: ${fibra.ticker} ahora está ${fibra.state}.`)
              }
            />
          ) : null}
        </>
      ) : null}

      {state.mode === 'create' ? (
        <section className="rounded-[1.75rem] border border-slate-200 bg-white p-6 shadow-sm">
          <FibraForm
            key="create-fibra"
            onCancel={() => handleBackToList()}
            onSuccess={() => handleBackToList('✓ FIBRA guardada')}
          />
        </section>
      ) : null}

      {state.mode === 'edit' && state.selected ? (
        <section className="rounded-[1.75rem] border border-slate-200 bg-white p-6 shadow-sm">
          <FibraForm
            key={state.selected.id}
            initialData={state.selected}
            onCancel={() => handleBackToList()}
            onSuccess={() => handleBackToList('✓ FIBRA guardada')}
          />
        </section>
      ) : null}
    </div>
  )
}
