import { useState, useRef } from 'react'
import { useNavigate } from 'react-router'
import { notifyPayment } from '@/modules/auth/authApi'
import { useAuth } from '@/modules/auth/AuthContext'
import { Button } from '@/shared/ui/button'
import { validateReceiptFile } from './notify-receipt-logic'
import { PAYMENT_INFO, RECEIPT_ACCEPT } from './payment-plans'

type Step = 'idle' | 'uploading' | 'sending' | 'sent' | 'error'

export function NotifyWithReceiptButton() {
  const { status } = useAuth()
  const navigate = useNavigate()
  const [step, setStep] = useState<Step>('idle')
  const [file, setFile] = useState<File | null>(null)
  const [fileError, setFileError] = useState<string | null>(null)
  const fileRef = useRef<HTMLInputElement>(null)

  function handleFirstClick() {
    if (status === 'checking') return
    if (status !== 'authenticated') {
      navigate('/login', { replace: true })
      return
    }
    setStep('uploading')
  }

  function handleFileChange(e: React.ChangeEvent<HTMLInputElement>) {
    const selected = e.target.files?.[0] ?? null
    if (!selected) {
      setFile(null)
      setFileError(null)
      return
    }
    const result = validateReceiptFile(selected)
    if (!result.valid) {
      setFile(null)
      setFileError(
        result.error === 'invalid_type'
          ? 'Solo se aceptan imágenes y PDF'
          : 'El archivo supera el límite de 5 MB',
      )
      if (fileRef.current) fileRef.current.value = ''
      return
    }
    setFile(selected)
    setFileError(null)
  }

  async function handleSend(withFile: boolean) {
    setStep('sending')
    try {
      await notifyPayment(withFile && file ? file : undefined)
      setStep('sent')
    } catch {
      setStep('error')
    }
  }

  if (step === 'sent') {
    return (
      <p role="status" className="text-sm text-emerald-700 font-medium">
        ✓ Comprobante enviado. Te contactaremos para activar tu acceso.
      </p>
    )
  }

  if (step === 'error') {
    return (
      <p role="alert" className="text-sm text-destructive">
        No se pudo enviar. Escríbenos a {PAYMENT_INFO.contacto}.
      </p>
    )
  }

  if (step === 'idle') {
    return (
      <Button onClick={handleFirstClick}>Ya pagué — notificar al equipo</Button>
    )
  }

  const isSending = step === 'sending'

  return (
    <div className="flex flex-col gap-3">
      <div className="flex flex-col gap-1.5">
        <label htmlFor="comprobante-file" className="text-sm font-medium">
          Adjunta tu comprobante (imagen o PDF, máx. 5 MB)
        </label>
        <input
          id="comprobante-file"
          name="comprobante"
          type="file"
          aria-label="Comprobante de pago"
          accept={RECEIPT_ACCEPT}
          ref={fileRef}
          onChange={handleFileChange}
          disabled={isSending}
          className="text-sm file:mr-3 file:rounded-md file:border-0 file:bg-muted file:px-3 file:py-1.5 file:text-xs file:font-medium"
        />
        {fileError ? (
          <p role="alert" className="text-xs text-destructive">
            {fileError}
          </p>
        ) : null}
      </div>
      <Button onClick={() => handleSend(true)} disabled={!file || !!fileError || isSending}>
        {isSending ? 'Enviando…' : 'Enviar comprobante'}
      </Button>
      <button
        type="button"
        onClick={() => handleSend(false)}
        disabled={isSending}
        className="text-sm text-muted-foreground underline underline-offset-2 hover:text-foreground transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
      >
        Enviar sin comprobante
      </button>
    </div>
  )
}
