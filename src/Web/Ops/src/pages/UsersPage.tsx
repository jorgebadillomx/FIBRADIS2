import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { createOpsUser, fetchOpsUsers } from '@/api/usersApi'

export function UsersPage() {
  const queryClient = useQueryClient()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [formError, setFormError] = useState<string | null>(null)

  const usersQuery = useQuery({
    queryKey: ['ops-users'],
    queryFn: fetchOpsUsers,
    staleTime: 60_000,
    retry: false,
  })

  const createMutation = useMutation({
    mutationFn: createOpsUser,
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['ops-users'] })
      setEmail('')
      setPassword('')
      setFormError(null)
    },
    onError: (error: Error) => {
      setFormError(error.message)
    },
  })

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setFormError(null)
    createMutation.mutate({ email, password })
  }

  const users = usersQuery.data ?? []

  return (
    <section className="space-y-6">
      <div className="rounded-[1.75rem] border border-slate-200 bg-[linear-gradient(135deg,_rgba(15,118,110,0.10),_rgba(255,255,255,0.96)_38%,_rgba(15,23,42,0.04))] p-6 shadow-sm">
        <p className="text-xs font-semibold uppercase tracking-[0.28em] text-teal-700">Gestión de acceso</p>
        <h2 className="mt-2 text-2xl font-semibold tracking-tight text-slate-950">Usuarios</h2>
        <p className="mt-3 max-w-3xl text-sm leading-7 text-slate-600">
          Crea cuentas de usuario para el sitio principal de FIBRADIS. Cada cuenta recibe rol <strong>Usuario</strong> por defecto.
        </p>
      </div>

      <div className="rounded-[1.75rem] border border-slate-200 bg-white/90 p-6 shadow-sm">
        <h3 className="text-base font-semibold tracking-tight text-slate-900">Crear usuario</h3>
        <form className="mt-4 flex flex-col gap-4 sm:flex-row sm:items-end" onSubmit={handleSubmit}>
          <div className="flex flex-col gap-1.5">
            <label className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-500" htmlFor="user-email">
              Correo electrónico
            </label>
            <input
              className="h-10 rounded-2xl border border-slate-200 bg-slate-50 px-4 text-sm text-slate-900 placeholder-slate-400 focus:border-teal-500 focus:outline-none focus:ring-2 focus:ring-teal-500/30"
              disabled={createMutation.isPending}
              id="user-email"
              onChange={(e) => setEmail(e.target.value)}
              placeholder="usuario@ejemplo.com"
              required
              type="email"
              value={email}
            />
          </div>

          <div className="flex flex-col gap-1.5">
            <label className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-500" htmlFor="user-password">
              Contraseña
            </label>
            <input
              className="h-10 rounded-2xl border border-slate-200 bg-slate-50 px-4 text-sm text-slate-900 placeholder-slate-400 focus:border-teal-500 focus:outline-none focus:ring-2 focus:ring-teal-500/30"
              disabled={createMutation.isPending}
              id="user-password"
              minLength={8}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="Mínimo 8 caracteres"
              required
              type="password"
              value={password}
            />
          </div>

          <button
            className="h-10 inline-flex items-center justify-center rounded-2xl bg-slate-950 px-5 text-sm font-semibold text-white transition hover:bg-slate-800 disabled:cursor-not-allowed disabled:opacity-60"
            disabled={createMutation.isPending}
            type="submit"
          >
            {createMutation.isPending ? 'Creando...' : 'Crear cuenta'}
          </button>
        </form>

        {formError ? (
          <p className="mt-3 text-sm text-red-600">{formError}</p>
        ) : null}

        {createMutation.isSuccess ? (
          <p className="mt-3 text-sm text-teal-700">Usuario creado correctamente.</p>
        ) : null}
      </div>

      <div className="rounded-[1.75rem] border border-slate-200 bg-white/90 p-6 shadow-sm">
        <div className="flex items-center justify-between gap-4">
          <h3 className="text-base font-semibold tracking-tight text-slate-900">Cuentas registradas</h3>
          <span className="text-sm text-slate-500">{users.length} usuario{users.length !== 1 ? 's' : ''}</span>
        </div>

        {usersQuery.isLoading ? (
          <p className="mt-4 text-sm text-slate-500">Cargando usuarios...</p>
        ) : null}

        {usersQuery.isError ? (
          <p className="mt-4 text-sm text-red-600">{usersQuery.error.message}</p>
        ) : null}

        {usersQuery.isSuccess ? (
          users.length === 0 ? (
            <p className="mt-4 text-sm text-slate-500">No hay usuarios registrados todavía.</p>
          ) : (
            <div className="mt-4 overflow-hidden rounded-2xl border border-slate-200">
              <table className="min-w-full border-collapse text-sm">
                <thead className="bg-slate-950 text-left text-[11px] uppercase tracking-[0.18em] text-slate-100">
                  <tr>
                    <th className="px-4 py-3 font-medium">Correo</th>
                    <th className="px-4 py-3 font-medium">Rol</th>
                    <th className="px-4 py-3 font-medium">Estado</th>
                    <th className="px-4 py-3 font-medium">Creado</th>
                  </tr>
                </thead>
                <tbody className="bg-white">
                  {users.map((user) => (
                    <tr className="border-t border-slate-200 text-slate-700" key={user.id}>
                      <td className="px-4 py-3 font-medium text-slate-900">{user.email}</td>
                      <td className="px-4 py-3">
                        <span className="rounded-full bg-teal-100 px-2.5 py-1 text-xs font-semibold text-teal-800">
                          {user.role}
                        </span>
                      </td>
                      <td className="px-4 py-3">
                        <span
                          className={
                            user.isActive
                              ? 'rounded-full bg-emerald-100 px-2.5 py-1 text-xs font-semibold text-emerald-800'
                              : 'rounded-full bg-slate-100 px-2.5 py-1 text-xs font-semibold text-slate-600'
                          }
                        >
                          {user.isActive ? 'Activo' : 'Inactivo'}
                        </span>
                      </td>
                      <td className="px-4 py-3 text-slate-500">
                        {new Date(user.createdAt).toLocaleDateString('es-MX', { dateStyle: 'medium' })}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )
        ) : null}
      </div>
    </section>
  )
}
