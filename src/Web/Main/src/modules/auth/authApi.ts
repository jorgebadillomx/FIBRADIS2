import { createPathBasedClient } from 'openapi-fetch'
import type { components, paths } from '@fibradis/shared-api-client'
import {
  clearMainAccessToken,
  clearSessionIndicator,
  decodeTokenRole,
  getMainAuthHeaders,
  notifyMainAuthRequired,
  storeMainAccessToken,
} from './mainAuth'

const authClient = createPathBasedClient<paths>({ baseUrl: '' })

export type UserProfileResponse = components['schemas']['UserProfileResponse']
export type UpdateApodoRequest = components['schemas']['UpdateApodoRequest']
export type ChangeOwnPasswordRequest = components['schemas']['ChangeOwnPasswordRequest']
export type ConfirmEmailResponse = components['schemas']['ConfirmEmailResponse']
export type RegisterResponse = components['schemas']['RegisterResponse']

export class AuthApiError extends Error {
  public readonly code: string

  constructor(
    code: string,
    message: string,
  ) {
    super(message)
    this.code = code
    this.name = 'AuthApiError'
    Object.setPrototypeOf(this, new.target.prototype)
  }
}

type ApiErrorShape = {
  detail?: string
  errors?: Record<string, string[]>
  message?: string
  status?: number
  title?: string
}

function getMainApiErrorMessage(error: unknown, fallback: string): string {
  if (error && typeof error === 'object') {
    const typedError = error as ApiErrorShape

    if (typeof typedError.detail === 'string' && typedError.detail.length > 0) {
      return typedError.detail
    }

    if (typeof typedError.message === 'string' && typedError.message.length > 0) {
      return typedError.message
    }

    if (typedError.errors && typeof typedError.errors === 'object') {
      const firstField = Object.values(typedError.errors).find((msgs) => msgs.length > 0)
      if (firstField) return firstField[0]
    }

    if (typeof typedError.title === 'string' && typedError.title.length > 0) {
      return typedError.title
    }
  }

  return fallback
}

let _refreshInFlight: Promise<boolean> | null = null

export async function loginMain(email: string, password: string): Promise<void> {
  const { data, error } = await authClient['/api/v1/auth/login'].POST({
    body: { email, password },
  })

  if (error) {
    const domainCode =
      error && typeof error === 'object' && 'domainCode' in error && typeof error.domainCode === 'string'
        ? error.domainCode
        : null
    if (domainCode === 'ACCOUNT_DISABLED') {
      throw new Error('Tu cuenta está deshabilitada. Contacta al administrador.')
    }
    throw new Error('Correo o contraseña incorrectos.')
  }

  const token = data?.accessToken?.trim()
  if (!token) throw new Error('La API no devolvió access token.')

  if (decodeTokenRole(token) === 'AdminOps') {
    throw new Error('Las cuentas AdminOps no pueden iniciar sesión en el sitio principal.')
  }

  storeMainAccessToken(token)
}

export function refreshMainSession(): Promise<boolean> {
  if (_refreshInFlight) return _refreshInFlight

  _refreshInFlight = (async () => {
    const { data, error } = await authClient['/api/v1/auth/refresh'].POST({})

    if (error) {
      const status =
        error && typeof error === 'object' && 'status' in error && typeof error.status === 'number'
          ? error.status
          : null
      if (status === 401) {
        clearSessionIndicator()
        return false
      }
      throw new Error('No se pudo restaurar la sesión.')
    }

    const token = data?.accessToken?.trim()
    if (!token) return false

    if (decodeTokenRole(token) === 'AdminOps') {
      // Shared refresh cookie picked up Ops credentials — invalidate server-side and reject
      void fetch('/api/v1/auth/logout', { method: 'POST' })
      return false
    }

    storeMainAccessToken(token)
    return true
  })().finally(() => {
    _refreshInFlight = null
  })

  return _refreshInFlight
}

export function logoutMain(): void {
  void fetch('/api/v1/auth/logout', { method: 'POST' })
  clearMainAccessToken()
  notifyMainAuthRequired()
}

export async function acceptTermsApi(): Promise<void> {
  const res = await fetch('/api/v1/account/accept-terms', {
    method: 'POST',
    headers: { ...getMainAuthHeaders() },
  })
  if (!res.ok) throw new Error('Error al aceptar términos.')
}

export async function fetchProfile(): Promise<UserProfileResponse> {
  const { data, error } = await authClient['/api/v1/account/me'].GET({
    headers: getMainAuthHeaders(),
  })

  if (error) {
    throw new Error(getMainApiErrorMessage(error, 'Error al cargar el perfil.'))
  }

  if (!data) throw new Error('La API no devolvió datos.')
  return data
}

export async function updateApodo(apodo: string | null): Promise<void> {
  const { error } = await authClient['/api/v1/account/me'].PATCH({
    headers: getMainAuthHeaders(),
    body: { apodo },
  })

  if (error) {
    throw new Error(getMainApiErrorMessage(error, 'No se pudo actualizar el apodo.'))
  }
}

export async function changePassword(currentPassword: string, newPassword: string): Promise<void> {
  const { error } = await authClient['/api/v1/account/password'].PATCH({
    headers: getMainAuthHeaders(),
    body: { currentPassword, newPassword },
  })

  if (error) {
    throw new Error(getMainApiErrorMessage(error, 'No se pudo cambiar la contraseña.'))
  }
}

export async function registerUser(
  email: string,
  password: string,
  apodo?: string | null,
  howDidYouHear?: string | null,
): Promise<RegisterResponse> {
  const { data, error, response } = await authClient['/api/v1/auth/register'].POST({
    body: { email, password, apodo: apodo ?? null, howDidYouHear: howDidYouHear ?? null },
  })

  if (error) {
    const typedError = error as { code?: unknown; message?: unknown }
    const code =
      typeof typedError.code === 'string'
        ? typedError.code
        : response.status === 422
          ? 'disposable_email'
          : response.status === 409
            ? 'duplicate_email'
            : 'register_failed'
    const message =
      code === 'invalid_user_data' && typeof typedError.message === 'string' && typedError.message.length > 0
        ? typedError.message
        : 'No se pudo completar el registro.'
    throw new AuthApiError(code, message)
  }

  if (!data) throw new AuthApiError('register_failed', 'La API no devolvió datos.')
  return data
}

export async function notifyPayment(): Promise<void> {
  const res = await fetch('/api/v1/account/notify-payment', {
    method: 'POST',
    headers: { ...getMainAuthHeaders() },
  })
  if (!res.ok) throw new AuthApiError('notify_payment_failed', 'Error al notificar el pago.')
}

export async function resendConfirmation(email: string): Promise<void> {
  await fetch('/api/v1/auth/resend-confirmation', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email }),
  })
}

export async function forgotPassword(email: string): Promise<void> {
  try {
    await fetch('/api/v1/auth/forgot-password', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email }),
    })
  } catch {
    // Siempre es silencioso: no revelar estado del email al caller.
  }
}

export async function resetPassword(token: string, newPassword: string): Promise<void> {
  const response = await fetch('/api/v1/auth/reset-password', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ token, newPassword }),
  })

  if (response.ok) return

  let body: unknown = null
  try {
    body = await response.json()
  } catch {
    body = null
  }

  if (response.status === 400) {
    const typedBody = body as { code?: unknown }
    const code = typeof typedBody.code === 'string' ? typedBody.code : 'reset_password_failed'
    throw new AuthApiError(
      code,
      getMainApiErrorMessage(body, 'No se pudo restablecer la contraseña.'),
    )
  }

  throw new AuthApiError(
    'reset_password_failed',
    getMainApiErrorMessage(body, 'No se pudo restablecer la contraseña.'),
  )
}

export async function confirmEmail(token: string): Promise<ConfirmEmailResponse> {
  const { data, error, response } = await authClient['/api/v1/auth/confirm-email'].GET({
    params: { query: { token } },
  })

  if (error) {
    const typedError = error as { code?: unknown }
    const code =
      typeof typedError.code === 'string'
        ? typedError.code
        : response.status === 400
          ? 'token_invalid'
          : 'confirm_email_failed'

    throw new AuthApiError(code, 'No se pudo confirmar el correo.')
  }

  if (!data) {
    throw new AuthApiError('confirm_email_failed', 'La API no devolvió datos.')
  }

  return data
}
