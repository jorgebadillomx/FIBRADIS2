import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { useQueryClient, useMutation } from '@tanstack/react-query'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/shared/ui/dialog'
import { Input } from '@/shared/ui/input'
import { Button } from '@/shared/ui/button'
import { useProfile, PROFILE_QUERY_KEY } from '@/modules/auth/useProfile'
import { changePassword, updateApodo } from '@/modules/auth/authApi'

function toSafeApodo(email: string | null | undefined, apodo: string | null | undefined): string {
  const trimmedApodo = apodo?.trim()
  if (trimmedApodo) return trimmedApodo

  if (!email) return 'Cuenta'

  const [localPart] = email.split('@')
  return localPart ? `${localPart}@...` : email
}

export function PerfilPage() {
  const queryClient = useQueryClient()
  const { data: profile, isLoading, isError, error } = useProfile()
  const [apodoDraft, setApodoDraft] = useState('')
  const [apodoError, setApodoError] = useState<string | null>(null)
  const [apodoSuccess, setApodoSuccess] = useState<string | null>(null)
  const [passwordOpen, setPasswordOpen] = useState(false)
  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [passwordError, setPasswordError] = useState<string | null>(null)
  const [passwordSuccess, setPasswordSuccess] = useState<string | null>(null)

  useEffect(() => {
    setApodoDraft(profile?.apodo ?? '')
  }, [profile?.apodo])

  const updateApodoMutation = useMutation({
    mutationFn: updateApodo,
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: PROFILE_QUERY_KEY })
      setApodoError(null)
      setApodoSuccess('Apodo actualizado.')
    },
    onError: (err) => {
      setApodoSuccess(null)
      setApodoError(err instanceof Error ? err.message : 'No se pudo actualizar el apodo.')
    },
  })

  const changePasswordMutation = useMutation({
    mutationFn: ({ current, next }: { current: string; next: string }) =>
      changePassword(current, next),
    onSuccess: () => {
      setPasswordOpen(false)
      setCurrentPassword('')
      setNewPassword('')
      setConfirmPassword('')
      setPasswordError(null)
      setPasswordSuccess('Contraseña actualizada.')
    },
    onError: (err) => {
      setPasswordSuccess(null)
      setPasswordError(err instanceof Error ? err.message : 'No se pudo cambiar la contraseña.')
    },
  })

  async function handleApodoSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault()
    setApodoSuccess(null)

    if (apodoDraft.length > 50) {
      setApodoError('El apodo no puede tener más de 50 caracteres.')
      return
    }

    setApodoError(null)
    try {
      await updateApodoMutation.mutateAsync(apodoDraft.length > 0 ? apodoDraft : null)
    } catch {
      // onError ya establece el mensaje visible
    }
  }

  function closePasswordDialog() {
    setPasswordOpen(false)
    setCurrentPassword('')
    setNewPassword('')
    setConfirmPassword('')
    setPasswordError(null)
  }

  async function handlePasswordSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault()

    if (newPassword !== confirmPassword) {
      setPasswordError('Las contraseñas nuevas no coinciden.')
      return
    }

    setPasswordError(null)
    try {
      await changePasswordMutation.mutateAsync({ current: currentPassword, next: newPassword })
    } catch {
      // onError ya establece el mensaje visible
    }
  }

  const displayName = toSafeApodo(profile?.email, profile?.apodo)

  return (
    <div className="container mx-auto max-w-3xl px-4 py-10">
      <div className="relative overflow-hidden rounded-3xl border border-border bg-card p-6 shadow-sm sm:p-8">
        <div className="absolute inset-x-0 top-0 h-1 bg-gradient-to-r from-primary via-primary/60 to-transparent" />
        <p className="text-xs font-semibold uppercase tracking-[0.32em] text-primary">Perfil</p>
        <h1 className="mt-3 font-playfair text-3xl font-semibold tracking-tight text-balance">
          Mi cuenta
        </h1>
        <p className="mt-2 max-w-2xl text-sm text-muted-foreground">
          Actualiza tu apodo y cambia tu contraseña sin depender de Ops.
        </p>

        {isLoading ? (
          <div className="mt-8 space-y-4">
            <div className="h-5 w-24 animate-pulse rounded bg-muted" />
            <div className="h-10 w-full animate-pulse rounded-lg bg-muted/70" />
            <div className="h-5 w-20 animate-pulse rounded bg-muted" />
            <div className="h-10 w-full animate-pulse rounded-lg bg-muted/70" />
          </div>
        ) : isError ? (
          <p className="mt-8 rounded-lg border border-destructive/30 bg-destructive/10 px-4 py-3 text-sm text-destructive">
            {error instanceof Error ? error.message : 'No se pudo cargar el perfil.'}
          </p>
        ) : (
          <div className="mt-8 space-y-8">
            <section className="grid gap-2">
              <span className="text-sm font-medium text-foreground">Email</span>
              <div className="rounded-xl border border-border bg-muted/30 px-4 py-3 text-sm text-foreground">
                {profile?.email ?? '—'}
              </div>
            </section>

            <form className="grid gap-3" onSubmit={(e) => void handleApodoSubmit(e)}>
              <label className="grid gap-2">
                <span className="text-sm font-medium text-foreground">Apodo</span>
                <Input
                  aria-invalid={apodoError ? 'true' : undefined}
                  autoComplete="nickname"
                  maxLength={50}
                  onChange={(e) => {
                    setApodoDraft(e.target.value)
                    setApodoError(null)
                    setApodoSuccess(null)
                  }}
                  value={apodoDraft}
                />
              </label>
              <div className="flex flex-wrap items-center gap-3">
                <Button disabled={updateApodoMutation.isPending} type="submit">
                  {updateApodoMutation.isPending ? 'Guardando...' : 'Guardar'}
                </Button>
                <Button
                  type="button"
                  variant="outline"
                  onClick={() => setPasswordOpen(true)}
                >
                  Cambiar contraseña
                </Button>
                <span className="text-xs text-muted-foreground">
                  {apodoDraft.length}/50
                </span>
              </div>
              {apodoError ? (
                <p className="text-sm text-destructive" role="alert">
                  {apodoError}
                </p>
              ) : null}
              {apodoSuccess ? (
                <p className="text-sm text-emerald-700" role="status">
                  {apodoSuccess}
                </p>
              ) : null}
            </form>

            <section className="rounded-2xl border border-border bg-background/80 p-4">
              <p className="text-xs font-semibold uppercase tracking-[0.28em] text-muted-foreground">
                Resumen
              </p>
              <div className="mt-3 grid gap-2 text-sm">
                <p>
                  <span className="font-medium text-foreground">Apodo visible:</span>{' '}
                  {displayName}
                </p>
                <p className="text-muted-foreground">
                  Tu cuenta está asociada al rol <span className="font-medium text-foreground">{profile?.role ?? '—'}</span>.
                </p>
              </div>
            </section>

            {passwordSuccess ? (
              <p className="text-sm text-emerald-700" role="status">
                {passwordSuccess}
              </p>
            ) : null}
          </div>
        )}
      </div>

      <Dialog open={passwordOpen} onOpenChange={(open) => { if (!open) closePasswordDialog() }}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>Cambiar contraseña</DialogTitle>
            <DialogDescription>
              Confirma tu contraseña actual y define una nueva clave segura.
            </DialogDescription>
          </DialogHeader>

          <form className="grid gap-4" onSubmit={(e) => void handlePasswordSubmit(e)}>
            <label className="grid gap-2">
              <span className="text-sm font-medium">Contraseña actual</span>
              <Input
                autoComplete="current-password"
                onChange={(e) => setCurrentPassword(e.target.value)}
                type="password"
                value={currentPassword}
              />
            </label>
            <label className="grid gap-2">
              <span className="text-sm font-medium">Nueva contraseña</span>
              <Input
                autoComplete="new-password"
                onChange={(e) => setNewPassword(e.target.value)}
                type="password"
                value={newPassword}
              />
            </label>
            <label className="grid gap-2">
              <span className="text-sm font-medium">Confirmar nueva contraseña</span>
              <Input
                autoComplete="new-password"
                onChange={(e) => setConfirmPassword(e.target.value)}
                type="password"
                value={confirmPassword}
              />
            </label>

            {passwordError ? (
              <p className="text-sm text-destructive" role="alert">
                {passwordError}
              </p>
            ) : null}

            <DialogFooter className="px-0 pb-0 pt-2" showCloseButton={false}>
              <Button
                type="button"
                variant="outline"
                onClick={closePasswordDialog}
              >
                Cancelar
              </Button>
              <Button disabled={changePasswordMutation.isPending} type="submit">
                {changePasswordMutation.isPending ? 'Guardando...' : 'Guardar'}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </div>
  )
}
