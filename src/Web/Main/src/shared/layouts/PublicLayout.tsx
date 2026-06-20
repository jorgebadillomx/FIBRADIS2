import { lazy, Suspense, useEffect, useRef, useState } from 'react'
import type { RefObject } from 'react'
import { Link, Outlet, useNavigate } from 'react-router'
import { Menu } from 'lucide-react'
import { PriceCarousel } from '@/modules/home/PriceCarousel'
import { useAuth } from '@/modules/auth/AuthContext'
import { useProfile } from '@/modules/auth/useProfile'
import { useSiteContent } from '@/shared/hooks/useSiteContent'
import { cn } from '@/shared/lib/utils'
import { AdSenseLoader } from '@/shared/ui/AdSenseLoader'
import {
  isMenuEntryLink,
  MAIN_INVESTMENT_LINKS,
  MAIN_PRIMARY_LINKS,
  type MenuEntry,
  shouldCloseMenuOnEscape,
} from './public-navigation'

const GlobalSearch = lazy(() => import('@/modules/home/GlobalSearch').then(m => ({ default: m.GlobalSearch })))
const TermsModal = lazy(() => import('@/modules/auth/TermsModal').then(m => ({ default: m.TermsModal })))
const MobileNavigationDialog = lazy(() =>
  import('./MobileNavigationDialog').then(m => ({ default: m.MobileNavigationDialog })),
)

function truncateEmail(email: string): string {
  const [localPart] = email.split('@')
  return localPart ? `${localPart}@...` : email
}

function GlobalSearchFallback({ className }: { className?: string }) {
  return (
    <input
      aria-label="Buscar FIBRA por ticker o nombre"
      className={cn(
        'h-9 w-full rounded-lg border border-input bg-transparent px-2.5 py-1 text-base text-muted-foreground md:text-sm',
        className,
      )}
      disabled
      placeholder="Buscar FIBRA por ticker o nombre..."
      type="search"
    />
  )
}

function DeferredGlobalSearch({
  className,
  onSelect,
}: {
  className?: string
  onSelect?: (ticker: string) => void
}) {
  return (
    <Suspense fallback={<GlobalSearchFallback className={className} />}>
      <GlobalSearch className={className} onSelect={onSelect} />
    </Suspense>
  )
}

export function DesktopMenu({
  label,
  entries,
  open,
  onToggle,
  onClose,
  wrapperRef,
  align = 'left',
  triggerClassName,
}: {
  label: string
  entries: MenuEntry[]
  open: boolean
  onToggle: () => void
  onClose: () => void
  wrapperRef: RefObject<HTMLDivElement | null>
  align?: 'left' | 'right'
  triggerClassName?: string
}) {
  return (
    <div ref={wrapperRef} className="relative">
      <button
        aria-expanded={open}
        aria-haspopup="menu"
        className={cn(
          'inline-flex h-9 items-center gap-1 rounded-md border border-border px-3 text-sm font-medium text-foreground transition-colors duration-150 hover:border-primary hover:text-primary focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2 focus-visible:ring-offset-background cursor-pointer whitespace-nowrap',
          triggerClassName,
        )}
        onClick={onToggle}
        type="button"
      >
        {label}
        <span aria-hidden="true" className="text-xs">▾</span>
      </button>
      {open ? (
        <div
          aria-label={label}
          className={cn(
            'absolute top-[calc(100%+0.5rem)] z-50 min-w-48 rounded-xl border border-border bg-popover p-1.5 shadow-lg',
            align === 'right' ? 'right-0' : 'left-0',
          )}
          role="menu"
        >
          {entries.map((entry) =>
            isMenuEntryLink(entry) ? (
              <Link
                key={entry.to}
                className="block rounded-lg px-3 py-2 text-sm text-foreground transition-colors duration-150 hover:bg-muted focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2 focus-visible:ring-offset-background cursor-pointer"
                onClick={onClose}
                role="menuitem"
                to={entry.to}
              >
                {entry.label}
              </Link>
            ) : (
              <button
                key={entry.label}
                className="block w-full rounded-lg px-3 py-2 text-left text-sm text-foreground transition-colors duration-150 hover:bg-muted focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2 focus-visible:ring-offset-background cursor-pointer"
                onClick={() => {
                  onClose()
                  entry.onClick()
                }}
                role="menuitem"
                type="button"
              >
                {entry.label}
              </button>
            ),
          )}
        </div>
      ) : null}
    </div>
  )
}

export function PublicLayout() {
  const { status, logout, hasAcceptedTerms } = useAuth()
  const navigate = useNavigate()
  const { data: siteContent } = useSiteContent()
  const { data: profile } = useProfile()
  const [investmentOpen, setInvestmentOpen] = useState(false)
  const [accountOpen, setAccountOpen] = useState(false)
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false)
  const investmentMenuRef = useRef<HTMLDivElement>(null)
  const accountMenuRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (!investmentOpen && !accountOpen) return

    const handlePointerDown = (event: MouseEvent) => {
      const target = event.target as Node
      const refs = [investmentMenuRef, accountMenuRef]
      if (refs.some((ref) => ref.current?.contains(target))) return
      setInvestmentOpen(false)
      setAccountOpen(false)
    }

    const handleKeyDown = (event: KeyboardEvent) => {
      if (shouldCloseMenuOnEscape(event.key)) {
        setInvestmentOpen(false)
        setAccountOpen(false)
      }
    }

    document.addEventListener('mousedown', handlePointerDown)
    document.addEventListener('keydown', handleKeyDown)

    return () => {
      document.removeEventListener('mousedown', handlePointerDown)
      document.removeEventListener('keydown', handleKeyDown)
    }
  }, [accountOpen, investmentOpen])

  const showTermsModal =
    status === 'authenticated' &&
    !hasAcceptedTerms &&
    siteContent?.termsEnabled === true &&
    Boolean(siteContent.termsText)

  function closeDesktopMenus() {
    setInvestmentOpen(false)
    setAccountOpen(false)
  }

  function toggleInvestmentMenu() {
    setInvestmentOpen((value) => {
      const next = !value
      if (next) {
        setAccountOpen(false)
      }
      return next
    })
  }

  function toggleAccountMenu() {
    setAccountOpen((value) => {
      const next = !value
      if (next) {
        setInvestmentOpen(false)
      }
      return next
    })
  }

  function handleLogout() {
    closeDesktopMenus()
    setMobileMenuOpen(false)
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
      <AdSenseLoader />
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
        <div className="container mx-auto flex h-14 items-center gap-3 px-4 md:gap-4 lg:gap-6">
          <Link
            to="/"
            className="font-playfair text-lg font-bold tracking-tight text-primary shrink-0 md:text-xl"
          >
            Fibras Inmobiliarias
          </Link>

          <button
            type="button"
            className="inline-flex h-11 w-11 items-center justify-center rounded-md border border-border text-foreground transition-colors duration-150 hover:border-primary hover:text-primary focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2 focus-visible:ring-offset-background cursor-pointer lg:hidden"
            aria-label="Abrir navegación"
            aria-expanded={mobileMenuOpen}
            onClick={() => setMobileMenuOpen(true)}
          >
            <Menu className="size-4" />
          </button>

          {mobileMenuOpen ? (
            <Suspense fallback={<div role="dialog" aria-modal="true" aria-label="Cargando navegación" />}>
              <MobileNavigationDialog
                open={mobileMenuOpen}
                onOpenChange={setMobileMenuOpen}
                status={status}
                onLogout={handleLogout}
              />
            </Suspense>
          ) : null}

          <nav
            aria-label="Navegación principal"
            className="hidden lg:flex min-w-0 items-center gap-4 text-sm text-muted-foreground"
          >
            {MAIN_PRIMARY_LINKS.map((item) => (
              <Link
                key={item.to}
                to={item.to}
                className="whitespace-nowrap transition-colors duration-150 hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2 focus-visible:ring-offset-background"
              >
                {item.label}
              </Link>
            ))}

            {status === 'authenticated' ? (
              <DesktopMenu
                align="left"
                entries={MAIN_INVESTMENT_LINKS}
                label="Mi inversión"
                open={investmentOpen}
                onClose={() => setInvestmentOpen(false)}
                onToggle={toggleInvestmentMenu}
                triggerClassName="border-transparent px-0 text-sm text-muted-foreground hover:border-transparent hover:text-foreground"
                wrapperRef={investmentMenuRef}
              />
            ) : null}
          </nav>

          <div className="hidden min-w-0 flex-1 justify-center lg:flex">
            <DeferredGlobalSearch className="max-w-[12rem] lg:max-w-[24rem]" />
          </div>

          <div className="flex items-center gap-2 shrink-0">
            {status === 'authenticated' ? (
              <DesktopMenu
                align="right"
                entries={[
                  { label: 'Mi perfil', to: '/perfil' },
                  { label: 'Suscripción', to: '/suscripcion' },
                  { label: 'Cerrar sesión', onClick: handleLogout },
                ]}
                label={profileLabel}
                open={accountOpen}
                onClose={() => setAccountOpen(false)}
                onToggle={toggleAccountMenu}
                triggerClassName="max-w-[13rem] truncate text-xs text-foreground lg:text-sm"
                wrapperRef={accountMenuRef}
              />
            ) : status === 'anonymous' ? (
              <Link
                to="/portafolio"
                className="inline-flex h-9 items-center rounded-md border border-border px-3 text-xs font-medium text-foreground transition-colors duration-150 hover:border-primary hover:text-primary focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2 focus-visible:ring-offset-background cursor-pointer lg:text-sm"
              >
                Portafolio
              </Link>
            ) : null}
          </div>
        </div>
      </header>

      <div className="border-b border-border bg-background/95 backdrop-blur">
        <div className="container mx-auto px-4 py-2">
          <PriceCarousel />
        </div>
      </div>

      <main id="main-content" tabIndex={-1} className="flex-1 outline-none">
        <Outlet />
      </main>

      <footer className="border-t border-border bg-background/80 py-4 text-center text-xs text-muted-foreground">
        <div className="container mx-auto flex flex-col items-center gap-y-2 px-4">
          <p className="max-w-2xl leading-relaxed">
            La información en Fibras Inmobiliarias es solo con fines informativos y educativos. No constituye asesoría financiera, legal ni fiscal. Consulte a un asesor profesional antes de tomar decisiones de inversión. Fibras Inmobiliarias no está regulado por la CNBV.
          </p>
          <div className="flex flex-wrap items-center justify-center gap-x-6 gap-y-1">
            <span className="inline-flex items-center gap-1">
              © {new Date().getFullYear()}
              <Link
                className="transition-colors duration-150 hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2 focus-visible:ring-offset-background cursor-pointer"
                to="/"
              >
                Fibras Inmobiliarias
              </Link>
            </span>
            <Link
              className="transition-colors duration-150 hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2 focus-visible:ring-offset-background cursor-pointer"
              to="/plataforma"
            >
              Plataforma
            </Link>
            <a
              className="transition-colors duration-150 hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2 focus-visible:ring-offset-background cursor-pointer"
              href={`mailto:${siteContent?.contactEmail ?? 'portafoliodefibras@gmail.com'}`}
            >
              Contacto
            </a>
            <Link
              className="transition-colors duration-150 hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2 focus-visible:ring-offset-background cursor-pointer"
              to="/privacidad"
            >
              Aviso de privacidad
            </Link>
          </div>
        </div>
      </footer>

      {showTermsModal ? (
        <Suspense fallback={null}>
          <TermsModal termsText={siteContent!.termsText!} />
        </Suspense>
      ) : null}
    </div>
  )
}
