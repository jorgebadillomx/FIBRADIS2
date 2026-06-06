import { useEffect, useRef, useState } from 'react'
import { Link, Outlet, useNavigate } from 'react-router'
import { GlobalSearch } from '@/modules/home/GlobalSearch'
import { useAuth } from '@/modules/auth/AuthContext'
import { useProfile } from '@/modules/auth/useProfile'
import { TermsModal } from '@/modules/auth/TermsModal'
import { useSiteContent } from '@/shared/hooks/useSiteContent'

function truncateEmail(email: string): string {
  const [localPart] = email.split('@')
  return localPart ? `${localPart}@...` : email
}

export function PublicLayout() {
  const { status, logout, hasAcceptedTerms } = useAuth()
  const navigate = useNavigate()
  const { data: siteContent } = useSiteContent()
  const { data: profile } = useProfile()
  const [menuOpen, setMenuOpen] = useState(false)
  const menuRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (!menuOpen) return

    const handlePointerDown = (event: MouseEvent) => {
      if (menuRef.current && !menuRef.current.contains(event.target as Node)) {
        setMenuOpen(false)
      }
    }

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        setMenuOpen(false)
      }
    }

    document.addEventListener('mousedown', handlePointerDown)
    document.addEventListener('keydown', handleKeyDown)

    return () => {
      document.removeEventListener('mousedown', handlePointerDown)
      document.removeEventListener('keydown', handleKeyDown)
    }
  }, [menuOpen])

  const showTermsModal =
    status === 'authenticated' &&
    !hasAcceptedTerms &&
    siteContent?.termsEnabled === true &&
    Boolean(siteContent.termsText)

  function handleLogout() {
    setMenuOpen(false)
    logout()
    void navigate('/', { replace: true })
  }

  const profileLabel = profile?.apodo?.trim()
    ? profile.apodo.trim()
    : profile?.email
      ? truncateEmail(profile.email)
      : 'Cuenta'

  return (
    <div className="min-h-screen flex flex-col bg-background text-foreground">
      <a
        href="#main-content"
        className="sr-only focus:not-sr-only focus:fixed focus:top-2 focus:left-2 focus:z-[100] focus:px-4 focus:py-2 focus:bg-background focus:border focus:border-border focus:rounded focus:text-sm focus:text-foreground"
      >
        Ir al contenido principal
      </a>

      <header
        role="banner"
        className="sticky top-0 z-50 border-b border-border bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60"
      >
        <div className="container mx-auto flex h-14 items-center gap-6 px-4">
          <Link to="/" className="font-playfair text-xl font-bold text-primary tracking-tight shrink-0">
            FIBRADIS
          </Link>
          <nav
            aria-label="Navegación principal"
            className="hidden md:flex items-center gap-5 text-sm text-muted-foreground"
          >
            <a href="/conoce-las-fibras" className="hover:text-foreground transition-colors duration-150">Conoce las FIBRAs</a>
            <a href="/catalogo" className="hover:text-foreground transition-colors duration-150">Catálogo</a>
            <Link to="/comparar" className="hover:text-foreground transition-colors duration-150">Comparar</Link>
            <Link to="/noticias" className="hover:text-foreground transition-colors duration-150">Noticias</Link>
            <Link to="/fundamentales" className="hover:text-foreground transition-colors duration-150">Fundamentales</Link>
            {status === 'authenticated' && (
              <>
                <Link to="/portafolio" className="hover:text-foreground transition-colors duration-150">
                  Portafolio
                </Link>
                <Link to="/oportunidades" className="hover:text-foreground transition-colors duration-150">
                  Oportunidades
                </Link>
              </>
            )}
          </nav>
          <div className="flex-1 min-w-0 flex justify-center">
            <GlobalSearch />
          </div>
          <div className="flex items-center gap-2 shrink-0">
            {status === 'authenticated' ? (
              <div ref={menuRef} className="relative">
                <button
                  aria-expanded={menuOpen}
                  aria-haspopup="menu"
                  className="max-w-[13rem] text-sm font-medium px-3 py-1.5 rounded border border-border text-foreground hover:border-primary hover:text-primary transition-colors duration-150 cursor-pointer truncate"
                  onClick={() => setMenuOpen((value) => !value)}
                  type="button"
                >
                  {profileLabel}
                </button>
                {menuOpen ? (
                  <div
                    className="absolute right-0 top-[calc(100%+0.5rem)] z-50 min-w-44 rounded-xl border border-border bg-popover p-1.5 shadow-lg"
                    role="menu"
                  >
                    <Link
                      className="block rounded-lg px-3 py-2 text-sm text-foreground hover:bg-muted"
                      onClick={() => setMenuOpen(false)}
                      role="menuitem"
                      to="/perfil"
                    >
                      Mi perfil
                    </Link>
                    <button
                      className="block w-full rounded-lg px-3 py-2 text-left text-sm text-foreground hover:bg-muted cursor-pointer"
                      onClick={handleLogout}
                      role="menuitem"
                      type="button"
                    >
                      Cerrar sesión
                    </button>
                  </div>
                ) : null}
              </div>
            ) : status === 'anonymous' ? (
              <Link
                to="/login"
                className="text-sm font-medium px-3 py-1.5 rounded border border-border text-foreground hover:border-primary hover:text-primary transition-colors duration-150"
              >
                Iniciar sesión
              </Link>
            ) : null}
          </div>
        </div>
      </header>

      <main id="main-content" tabIndex={-1} className="flex-1 outline-none">
        <Outlet />
      </main>

      <footer className="border-t border-border bg-background/80 py-4 text-center text-xs text-muted-foreground">
        <div className="container mx-auto flex flex-wrap items-center justify-center gap-x-6 gap-y-1 px-4">
          <span>© {new Date().getFullYear()} FIBRADIS — Solo información de referencia, no asesoría de inversión.</span>
          <a
            className="hover:text-foreground transition-colors"
            href={`mailto:${siteContent?.contactEmail ?? 'contacto@fibradis.mx'}`}
          >
            Contacto
          </a>
          <Link
            className="hover:text-foreground transition-colors"
            to="/privacidad"
          >
            Aviso de privacidad
          </Link>
        </div>
      </footer>

      {showTermsModal ? <TermsModal termsText={siteContent!.termsText!} /> : null}
    </div>
  )
}
