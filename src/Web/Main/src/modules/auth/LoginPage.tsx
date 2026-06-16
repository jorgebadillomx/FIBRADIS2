import { useEffect, useRef } from 'react'
import { useNavigate, useSearchParams } from 'react-router'
import { useAuth } from './AuthContext'
import { LoginForm } from './LoginForm'
import { resolveLoginRedirect } from './login-redirect'

export function LoginPage() {
  const { status } = useAuth()
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const hasStartedLoginRef = useRef(false)
  const redirectTarget = resolveLoginRedirect(searchParams.get('redirect'))

  useEffect(() => {
    if (status === 'authenticated' && !hasStartedLoginRef.current) {
      void navigate('/portafolio', { replace: true })
    }
  }, [status, navigate])

  if (status === 'checking') return null

  return (
    <div className="flex min-h-[calc(100vh-3.5rem)] items-center justify-center px-4 py-12">
      <LoginForm
        redirectTo={redirectTarget}
        titleAs="h1"
        onBeforeSubmit={() => {
          hasStartedLoginRef.current = true
        }}
      />
    </div>
  )
}
