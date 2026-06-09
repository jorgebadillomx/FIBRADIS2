import { useState } from 'react'
import { useAuth } from './AuthContext'

interface Props {
  termsText: string
}

export function TermsModal({ termsText }: Props) {
  const { acceptTerms } = useAuth()
  const [isPending, setIsPending] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function handleAccept() {
    setIsPending(true)
    setError(null)
    try {
      await acceptTerms()
    } catch {
      setError('No se pudo registrar la aceptación. Intenta de nuevo.')
      setIsPending(false)
    }
  }

  return (
    <div className="fixed inset-0 z-[200] flex items-center justify-center bg-black/60 px-4">
      <div className="w-full max-w-lg rounded-2xl border border-border bg-card shadow-2xl flex flex-col max-h-[90vh]">
        <div className="px-6 pt-6 pb-4 border-b border-border">
          <p className="text-xs font-semibold uppercase tracking-widest text-primary">Fibras Inmobiliarias</p>
          <h2 className="mt-1 font-playfair text-xl font-semibold tracking-tight">
            Términos de uso y aviso de privacidad
          </h2>
          <p className="mt-1 text-xs text-muted-foreground">
            Lee y acepta los términos para continuar.
          </p>
        </div>

        <div className="flex-1 overflow-y-auto px-6 py-4">
          <div className="whitespace-pre-wrap text-sm leading-7 text-foreground/80">
            {termsText}
          </div>
        </div>

        <div className="px-6 py-4 border-t border-border">
          {error ? <p className="mb-3 text-sm text-destructive">{error}</p> : null}
          <button
            className="w-full h-10 rounded-lg bg-primary text-sm font-medium text-primary-foreground transition hover:bg-primary/90 disabled:cursor-not-allowed disabled:opacity-50"
            disabled={isPending}
            onClick={() => void handleAccept()}
            type="button"
          >
            {isPending ? 'Registrando...' : 'Acepto los términos y condiciones'}
          </button>
        </div>
      </div>
    </div>
  )
}
