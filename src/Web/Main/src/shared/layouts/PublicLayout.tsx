import { useEffect, useRef, useState } from 'react'
import type { RefObject } from 'react'
import { Link, Outlet, useNavigate } from 'react-router'
import { Menu, X } from 'lucide-react'
import { GlobalSearch } from '@/modules/home/GlobalSearch'
import { PriceCarousel } from '@/modules/home/PriceCarousel'
import { useAuth } from '@/modules/auth/AuthContext'
import { useProfile } from '@/modules/auth/useProfile'
import { TermsModal } from '@/modules/auth/TermsModal'
import { useSiteContent } from '@/shared/hooks/useSiteContent'
import {
  Dialog,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/shared/ui/dialog'
import { cn } from '@/shared/lib/utils'
import {
  MAIN_INVESTMENT_LINKS,
  MAIN_PRIMARY_LINKS,
  type MenuEntry,
  type NavLinkItem,
  type PublicLayoutStatus,
  buildMainMobileSections,
  shouldCloseMenuOnEscape,
} from './public-navigation'

function truncateEmail(email: string): string {
  const [localPart] = email.split('@')
  return localPart ? `${localPart}@...` : email
}

function isMenuEntryLink(entry: MenuEntry): entry is NavLinkItem {
  return 'to' in entry
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

function MobileSection({
  title,
  items,
  onNavigate,
}: {
  title: string
  items: MenuEntry[]
  onNavigate: () => void
}) {
  return (
    <section className="space-y-2">
      <p className="text-xs font-semibold uppercase tracking-[0.18em] text-muted-foreground">
        {title}
      </p>
      <div className="space-y-1">
        {items.map((item) =>
          isMenuEntryLink(item) ? (
            <Link
              key={item.to}
              className="flex min-h-11 items-center rounded-lg px-3 py-3 text-foreground transition-colors duration-150 hover:bg-muted focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2 focus-visible:ring-offset-background cursor-pointer"
              onClick={onNavigate}
              to={item.to}
            >
              {item.label}
            </Link>
          ) : (
            <button
              key={item.label}
              className="flex min-h-11 w-full items-center rounded-lg px-3 py-3 text-left text-foreground transition-colors duration-150 hover:bg-muted focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2 focus-visible:ring-offset-background cursor-pointer"
              onClick={() => {
                onNavigate()
                item.onClick()
              }}
              type="button"
            >
              {item.label}
            </button>
          ),
        )}
      </div>
    </section>
  )
}

export function MobileMenuContent({
  status,
  onNavigate,
  onLogout,
}: {
  status: PublicLayoutStatus
  onNavigate: () => void
  onLogout: () => void
}) {
  const sections = buildMainMobileSections(status, onLogout)

  return (
    <nav aria-label="Navegación móvil" className="space-y-5 text-sm">
      {sections.map((section) => (
        <MobileSection
          key={section.title}
          onNavigate={onNavigate}
          title={section.title}
          items={section.items}
        />
      ))}
    </nav>
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

          <Dialog open={mobileMenuOpen} onOpenChange={setMobileMenuOpen}>
            <DialogTrigger asChild>
              <button
                type="button"
                className="inline-flex h-11 w-11 items-center justify-center rounded-md border border-border text-foreground transition-colors duration-150 hover:border-primary hover:text-primary focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2 focus-visible:ring-offset-background cursor-pointer lg:hidden"
                aria-label="Abrir navegación"
                aria-expanded={mobileMenuOpen}
              >
                <Menu className="size-4" />
              </button>
            </DialogTrigger>
            <DialogContent
              className="left-0 top-0 h-dvh w-[min(88vw,22rem)] translate-x-0 translate-y-0 rounded-none rounded-r-[2rem] border-r border-border bg-background p-0 text-foreground shadow-2xl"
              showCloseButton={false}
            >
              <div className="flex h-full flex-col">
                <DialogHeader className="border-b border-border px-4 py-4">
                  <div className="flex items-start justify-between gap-3">
                    <div>
                      <DialogTitle>Navegación</DialogTitle>
                      <DialogDescription>
                        Busca una FIBRA o navega entre las superficies públicas y privadas.
                      </DialogDescription>
                    </div>
                    <DialogClose asChild>
                      <button
                        type="button"
                        className="inline-flex h-11 w-11 items-center justify-center rounded-md border border-border text-foreground transition-colors duration-150 hover:border-primary hover:text-primary focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2 focus-visible:ring-offset-background cursor-pointer"
                        aria-label="Cerrar navegación"
                      >
                        <X className="size-4" />
                      </button>
                    </DialogClose>
                  </div>
                </DialogHeader>

                <div className="flex-1 space-y-5 overflow-y-auto px-4 py-4">
                  <div className="rounded-2xl border border-border/70 bg-background/70 p-3">
                    <GlobalSearch className="max-w-none" onSelect={() => setMobileMenuOpen(false)} />
                  </div>
                  <MobileMenuContent
                    status={status}
                    onNavigate={() => setMobileMenuOpen(false)}
                    onLogout={handleLogout}
                  />
                </div>
              </div>
            </DialogContent>
          </Dialog>

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
            <GlobalSearch className="max-w-[12rem] lg:max-w-[24rem]" />
          </div>

          <div className="flex items-center gap-2 shrink-0">
            {status === 'authenticated' ? (
              <DesktopMenu
                align="right"
                entries={[
                  { label: 'Mi perfil', to: '/perfil' },
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
                to="/login"
                className="inline-flex h-9 items-center rounded-md border border-border px-3 text-xs font-medium text-foreground transition-colors duration-150 hover:border-primary hover:text-primary focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2 focus-visible:ring-offset-background cursor-pointer lg:text-sm"
              >
                Iniciar sesión
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
            La información en FIBRADIS es solo con fines informativos y educativos. No constituye asesoría financiera, legal ni fiscal. Consulte a un asesor profesional antes de tomar decisiones de inversión. FIBRADIS no está regulado por la CNBV.
          </p>
          <div className="flex flex-wrap items-center justify-center gap-x-6 gap-y-1">
            <span>© {new Date().getFullYear()} Fibras Inmobiliarias</span>
            <a
              className="transition-colors hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2 focus-visible:ring-offset-background"
              href={`mailto:${siteContent?.contactEmail ?? 'contacto@fibradis.mx'}`}
            >
              Contacto
            </a>
            <Link
              className="transition-colors hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2 focus-visible:ring-offset-background"
              to="/privacidad"
            >
              Aviso de privacidad
            </Link>
          </div>
        </div>
      </footer>

      {showTermsModal ? <TermsModal termsText={siteContent!.termsText!} /> : null}
    </div>
  )
}
