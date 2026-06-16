import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  changeUserPassword,
  createOpsUser,
  fetchOpsUsers,
  setUserActive,
  updateUserPayment,
  type UserSummaryDto,
} from '@/api/usersApi'

const ROLES = [
  { value: 'User', label: 'Main (portafolio)' },
  { value: 'AdminOps', label: 'AdminOps' },
]

function validatePassword(pwd: string): string | null {
  if (pwd.length < 8) return 'Debe tener al menos 8 caracteres.'
  if (!/[A-Z]/.test(pwd)) return 'Debe contener al menos una letra mayúscula.'
  if (!/[a-z]/.test(pwd)) return 'Debe contener al menos una letra minúscula.'
  if (!/\d/.test(pwd)) return 'Debe contener al menos un número.'
  if (!/[^a-zA-Z0-9]/.test(pwd)) return 'Debe contener al menos un carácter especial.'
  return null
}

function generateStrongPassword(): string {
  const upper = 'ABCDEFGHJKLMNPQRSTUVWXYZ'
  const lower = 'abcdefghjkmnpqrstuvwxyz'
  const digits = '23456789'
  const special = '!@#$%&*'
  const all = upper + lower + digits + special
  const randomIndex = (max: number) => {
    const buf = new Uint32Array(1)
    crypto.getRandomValues(buf)
    return buf[0] % max
  }
  const pick = (s: string) => s[randomIndex(s.length)]
  const chars = [
    pick(upper), pick(upper),
    pick(lower), pick(lower),
    pick(digits), pick(digits),
    pick(special),
    ...Array.from({ length: 5 }, () => pick(all)),
  ]
  for (let i = chars.length - 1; i > 0; i--) {
    const j = randomIndex(i + 1);
    [chars[i], chars[j]] = [chars[j], chars[i]]
  }
  return chars.join('')
}

// ── Create form ──────────────────────────────────────────────────────────────

function CreateUserForm({ onCreated }: { onCreated: () => void }) {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [role, setRole] = useState('User')
  const [pago, setPago] = useState('')
  const [fechaPago, setFechaPago] = useState('')
  const [formError, setFormError] = useState<string | null>(null)

  const mutation = useMutation({
    mutationFn: createOpsUser,
    onSuccess: () => {
      setEmail(''); setPassword(''); setRole('User'); setPago(''); setFechaPago('')
      setFormError(null)
      onCreated()
    },
    onError: (error: Error) => { setFormError(error.message) },
  })

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setFormError(null)
    const pwdError = validatePassword(password)
    if (pwdError) { setFormError(pwdError); return }
    mutation.mutate({
      email,
      password,
      role,
      pago: role === 'User' && pago ? Number(pago) : null,
      fechaPago: role === 'User' && fechaPago ? fechaPago : null,
    })
  }

  return (
    <div className="rounded-[1.75rem] border border-slate-200 bg-white/90 p-6 shadow-sm">
      <h3 className="text-base font-semibold tracking-tight text-slate-900">Crear usuario</h3>
      <form className="mt-4 grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3" onSubmit={handleSubmit}>
        <div className="flex flex-col gap-1.5">
          <label className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-500" htmlFor="u-email">
            Correo electrónico
          </label>
          <input
            className="h-10 rounded-2xl border border-slate-200 bg-slate-50 px-4 text-sm text-slate-900 placeholder-slate-400 focus:border-teal-500 focus:outline-none focus:ring-2 focus:ring-teal-500/30"
            disabled={mutation.isPending}
            id="u-email"
            onChange={(e) => setEmail(e.target.value)}
            placeholder="usuario@ejemplo.com"
            required
            type="email"
            value={email}
          />
        </div>

        <div className="flex flex-col gap-1.5">
          <label className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-500" htmlFor="u-role">
            Tipo de usuario
          </label>
          <select
            className="h-10 rounded-2xl border border-slate-200 bg-slate-50 px-4 text-sm text-slate-900 focus:border-teal-500 focus:outline-none focus:ring-2 focus:ring-teal-500/30"
            disabled={mutation.isPending}
            id="u-role"
            onChange={(e) => setRole(e.target.value)}
            value={role}
          >
            {ROLES.map((r) => (
              <option key={r.value} value={r.value}>{r.label}</option>
            ))}
          </select>
        </div>

        <div className="flex flex-col gap-1.5">
          <label className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-500" htmlFor="u-password">
            Contraseña
          </label>
          <div className="flex gap-2">
            <input
              className="h-10 min-w-0 flex-1 rounded-2xl border border-slate-200 bg-slate-50 px-4 text-sm text-slate-900 placeholder-slate-400 focus:border-teal-500 focus:outline-none focus:ring-2 focus:ring-teal-500/30"
              disabled={mutation.isPending}
              id="u-password"
              onChange={(e) => setPassword(e.target.value)}
              placeholder="Mínimo 8 chars, mayúscula, número, especial"
              required
              type="password"
              value={password}
            />
            <button
              className="h-10 flex-shrink-0 rounded-2xl border border-slate-200 bg-slate-100 px-3 text-slate-600 transition hover:bg-teal-50 hover:border-teal-300 hover:text-teal-700"
              onClick={() => setPassword(generateStrongPassword())}
              title="Generar contraseña segura"
              type="button"
            >
              🎲
            </button>
          </div>
        </div>

        {role === 'User' && (
          <>
            <div className="flex flex-col gap-1.5">
              <label className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-500" htmlFor="u-pago">
                Pago (opcional)
              </label>
              <input
                className="h-10 rounded-2xl border border-slate-200 bg-slate-50 px-4 text-sm text-slate-900 placeholder-slate-400 focus:border-teal-500 focus:outline-none focus:ring-2 focus:ring-teal-500/30"
                disabled={mutation.isPending}
                id="u-pago"
                min="0"
                onChange={(e) => setPago(e.target.value)}
                placeholder="0.00"
                step="0.01"
                type="number"
                value={pago}
              />
            </div>

            <div className="flex flex-col gap-1.5">
              <label className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-500" htmlFor="u-fecha">
                Fecha de pago (opcional)
              </label>
              <input
                className="h-10 rounded-2xl border border-slate-200 bg-slate-50 px-4 text-sm text-slate-900 focus:border-teal-500 focus:outline-none focus:ring-2 focus:ring-teal-500/30"
                disabled={mutation.isPending}
                id="u-fecha"
                onChange={(e) => setFechaPago(e.target.value)}
                type="date"
                value={fechaPago}
              />
            </div>
          </>
        )}

        <div className="flex items-end">
          <button
            className="h-10 inline-flex items-center justify-center rounded-2xl bg-slate-950 px-5 text-sm font-semibold text-white transition hover:bg-slate-800 disabled:cursor-not-allowed disabled:opacity-60"
            disabled={mutation.isPending}
            type="submit"
          >
            {mutation.isPending ? 'Creando...' : 'Crear cuenta'}
          </button>
        </div>
      </form>

      {formError ? <p className="mt-3 text-sm text-red-600">{formError}</p> : null}
      {mutation.isSuccess ? <p className="mt-3 text-sm text-teal-700">Usuario creado correctamente.</p> : null}
    </div>
  )
}

// ── Change password dialog ───────────────────────────────────────────────────

function ChangePasswordDialog({ userId, onClose }: { userId: string; onClose: () => void }) {
  const [pwd, setPwd] = useState('')
  const [error, setError] = useState<string | null>(null)
  const queryClient = useQueryClient()

  const mutation = useMutation({
    mutationFn: () => changeUserPassword(userId, pwd),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['ops-users'] })
      onClose()
    },
    onError: (e: Error) => setError(e.message),
  })

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError(null)
    const pwdError = validatePassword(pwd)
    if (pwdError) { setError(pwdError); return }
    mutation.mutate()
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
      <div className="w-full max-w-sm rounded-[1.75rem] border border-slate-200 bg-white p-6 shadow-xl">
        <h3 className="text-base font-semibold text-slate-900">Cambiar contraseña</h3>
        <form className="mt-4 flex flex-col gap-4" onSubmit={handleSubmit}>
          <div className="flex flex-col gap-1.5">
            <label className="text-xs font-semibold uppercase tracking-[0.18em] text-slate-500" htmlFor="cp-pwd">
              Nueva contraseña
            </label>
            <div className="flex gap-2">
              <input
                autoFocus
                className="h-10 min-w-0 flex-1 rounded-2xl border border-slate-200 bg-slate-50 px-4 text-sm text-slate-900 focus:border-teal-500 focus:outline-none focus:ring-2 focus:ring-teal-500/30"
                disabled={mutation.isPending}
                id="cp-pwd"
                onChange={(e) => setPwd(e.target.value)}
                placeholder="Mínimo 8 chars, mayúscula, número, especial"
                required
                type="password"
                value={pwd}
              />
              <button
                className="h-10 flex-shrink-0 rounded-2xl border border-slate-200 bg-slate-100 px-3 text-slate-600 transition hover:bg-teal-50 hover:border-teal-300 hover:text-teal-700"
                onClick={() => setPwd(generateStrongPassword())}
                title="Generar contraseña segura"
                type="button"
              >
                🎲
              </button>
            </div>
          </div>
          {error ? <p className="text-sm text-red-600">{error}</p> : null}
          <div className="flex gap-3">
            <button
              className="h-10 flex-1 rounded-2xl bg-slate-950 text-sm font-semibold text-white transition hover:bg-slate-800 disabled:opacity-60"
              disabled={mutation.isPending}
              type="submit"
            >
              {mutation.isPending ? 'Guardando...' : 'Guardar'}
            </button>
            <button
              className="h-10 flex-1 rounded-2xl border border-slate-200 text-sm font-semibold text-slate-700 transition hover:bg-slate-50"
              onClick={onClose}
              type="button"
            >
              Cancelar
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}

// ── Payment inline edit ──────────────────────────────────────────────────────

function PaymentCell({ user }: { user: UserSummaryDto }) {
  const queryClient = useQueryClient()
  const [editing, setEditing] = useState(false)
  const [pago, setPago] = useState(user.pago?.toString() ?? '')
  const [fecha, setFecha] = useState(
    user.fechaPago ? new Date(user.fechaPago).toISOString().substring(0, 10) : ''
  )

  const mutation = useMutation({
    mutationFn: () => updateUserPayment(
      user.id,
      pago ? Number(pago) : null,
      fecha || null,
    ),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['ops-users'] })
      setEditing(false)
    },
  })

  if (!editing) {
    return (
      <button
        className="text-left text-sm text-slate-700 underline-offset-2 hover:underline"
        onClick={() => setEditing(true)}
        type="button"
      >
        {user.pago != null
          ? `$${user.pago.toLocaleString('es-MX')} · ${fecha || '—'}`
          : 'Sin datos'}
      </button>
    )
  }

  return (
    <form
      className="flex flex-col gap-1"
      onSubmit={(e) => { e.preventDefault(); mutation.mutate() }}
    >
      <input
        className="h-8 w-28 rounded-xl border border-slate-200 bg-slate-50 px-2 text-xs"
        min="0"
        onChange={(e) => setPago(e.target.value)}
        placeholder="Pago"
        step="0.01"
        type="number"
        value={pago}
      />
      <input
        className="h-8 w-32 rounded-xl border border-slate-200 bg-slate-50 px-2 text-xs"
        onChange={(e) => setFecha(e.target.value)}
        title="Fecha de pago"
        type="date"
        value={fecha}
      />
      <div className="flex gap-1">
        <button className="rounded-lg bg-teal-700 px-2 py-0.5 text-xs font-semibold text-white" disabled={mutation.isPending} type="submit">
          {mutation.isPending ? '...' : 'OK'}
        </button>
        <button className="rounded-lg border border-slate-200 px-2 py-0.5 text-xs" onClick={() => setEditing(false)} type="button">
          ✕
        </button>
      </div>
    </form>
  )
}

// ── Main page ────────────────────────────────────────────────────────────────

export function UsersPage() {
  const queryClient = useQueryClient()
  const [changePwdUserId, setChangePwdUserId] = useState<string | null>(null)

  const usersQuery = useQuery({
    queryKey: ['ops-users'],
    queryFn: fetchOpsUsers,
    staleTime: 60_000,
    retry: false,
  })

  const toggleActiveMutation = useMutation({
    mutationFn: ({ id, isActive }: { id: string; isActive: boolean }) => setUserActive(id, isActive),
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: ['ops-users'] }),
  })

  const users = usersQuery.data ?? []

  return (
    <section className="space-y-6">
      <div className="rounded-[1.75rem] border border-slate-200 bg-[linear-gradient(135deg,_rgba(15,118,110,0.10),_rgba(255,255,255,0.96)_38%,_rgba(15,23,42,0.04))] p-6 shadow-sm">
        <p className="text-xs font-semibold uppercase tracking-[0.28em] text-teal-700">Gestión de acceso</p>
        <h2 className="mt-2 text-2xl font-semibold tracking-tight text-slate-950">Usuarios</h2>
        <p className="mt-3 max-w-3xl text-sm leading-7 text-slate-600">
          Crea y administra cuentas de usuario para el sitio principal de Fibras Inmobiliarias y para Ops.
        </p>
      </div>

      <CreateUserForm onCreated={() => void queryClient.invalidateQueries({ queryKey: ['ops-users'] })} />

      <div className="rounded-[1.75rem] border border-slate-200 bg-white/90 p-6 shadow-sm">
        <div className="flex items-center justify-between gap-4">
          <h3 className="text-base font-semibold tracking-tight text-slate-900">Cuentas registradas</h3>
          <span className="text-sm text-slate-500">{users.length} usuario{users.length !== 1 ? 's' : ''}</span>
        </div>

        {usersQuery.isLoading ? <p className="mt-4 text-sm text-slate-500">Cargando usuarios...</p> : null}
        {usersQuery.isError ? <p className="mt-4 text-sm text-red-600">{usersQuery.error.message}</p> : null}

        {usersQuery.isSuccess && users.length === 0 ? (
          <p className="mt-4 text-sm text-slate-500">No hay usuarios registrados todavía.</p>
        ) : null}

        {usersQuery.isSuccess && users.length > 0 ? (
          <div className="mt-4 overflow-x-auto rounded-2xl border border-slate-200">
            <table className="min-w-full border-collapse text-sm">
              <thead className="bg-slate-950 text-left text-[11px] uppercase tracking-[0.18em] text-slate-100">
                <tr>
                  <th className="px-4 py-3 font-medium">Correo</th>
                  <th className="px-4 py-3 font-medium">Tipo</th>
                  <th className="px-4 py-3 font-medium">Estado</th>
                  <th className="px-4 py-3 font-medium">Pago / Fecha</th>
                  <th className="px-4 py-3 font-medium">Creado</th>
                  <th className="px-4 py-3 font-medium">Acciones</th>
                </tr>
              </thead>
              <tbody className="bg-white">
                {users.map((user) => (
                  <tr className="border-t border-slate-200 text-slate-700" key={user.id}>
                    <td className="px-4 py-3 font-medium text-slate-900">{user.email}</td>
                    <td className="px-4 py-3">
                      <span className="rounded-full bg-teal-100 px-2.5 py-1 text-xs font-semibold text-teal-800">
                        {user.role === 'AdminOps' ? 'AdminOps' : 'Main'}
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
                    <td className="px-4 py-3">
                      {user.role === 'User' ? (
                        <PaymentCell user={user} />
                      ) : (
                        <span className="text-slate-400">—</span>
                      )}
                    </td>
                    <td className="px-4 py-3 text-slate-500">
                      {new Date(user.createdAt).toLocaleDateString('es-MX', { dateStyle: 'medium' })}
                    </td>
                    <td className="px-4 py-3">
                      <div className="flex gap-2">
                        <button
                          className={`rounded-xl px-3 py-1 text-xs font-semibold transition ${
                            user.isActive
                              ? 'bg-slate-100 text-slate-700 hover:bg-slate-200'
                              : 'bg-emerald-100 text-emerald-800 hover:bg-emerald-200'
                          }`}
                          disabled={toggleActiveMutation.isPending}
                          onClick={() => toggleActiveMutation.mutate({ id: user.id, isActive: !user.isActive })}
                          type="button"
                        >
                          {user.isActive ? 'Deshabilitar' : 'Habilitar'}
                        </button>
                        <button
                          className="rounded-xl bg-slate-100 px-3 py-1 text-xs font-semibold text-slate-700 transition hover:bg-slate-200"
                          onClick={() => setChangePwdUserId(user.id)}
                          type="button"
                        >
                          Contraseña
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ) : null}
      </div>

      {changePwdUserId ? (
        <ChangePasswordDialog
          onClose={() => setChangePwdUserId(null)}
          userId={changePwdUserId}
        />
      ) : null}
    </section>
  )
}
