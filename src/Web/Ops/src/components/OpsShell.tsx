import { Suspense, useEffect, useRef, useState } from 'react'
import { NavLink, Outlet } from 'react-router'
import { Menu } from 'lucide-react'
import { Dialog, DialogContent, DialogDescription, DialogTitle } from '@/shared/ui/dialog'
import { cn } from '@/shared/lib/utils'
import { OPS_NAVIGATION_SECTIONS } from './ops-navigation'

export function OpsNavigationPanel({
  onNavigate,
}: {
  onNavigate?: () => void
}) {
  return (
    <div className="flex flex-col gap-4">
      <div className="border-b border-white/10 pb-5">
        <p className="text-[11px] font-semibold uppercase tracking-[0.34em] text-teal-300">AdminOps</p>
        <h1 className="mt-2 text-2xl font-semibold tracking-tight">Fibras Inmobiliarias Ops</h1>
        <p className="mt-2 text-sm leading-6 text-slate-300">
          Centro operativo para navegar módulos, ajustar prompts y diagnosticar fallas con contexto claro.
        </p>
      </div>

      <div className="space-y-4">
        {OPS_NAVIGATION_SECTIONS.map((section) => (
          <section key={section.title} className="space-y-2">
            <p className="px-1 text-[11px] font-semibold uppercase tracking-[0.28em] text-slate-400">
              {section.title}
            </p>
            <nav aria-label={section.title} className="space-y-2">
              {section.items.map((item) => (
                <NavLink
                  className={({ isActive }) =>
                    cn(
                      'group block rounded-2xl border px-4 py-3 transition-colors duration-150 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-teal-300 focus-visible:ring-offset-2 focus-visible:ring-offset-slate-950 cursor-pointer',
                      isActive
                        ? 'border-teal-500/60 bg-teal-500/16 text-white shadow-[inset_0_0_0_1px_rgba(45,212,191,0.35)]'
                        : 'border-white/8 bg-white/[0.03] text-slate-300 hover:border-white/16 hover:bg-white/[0.06] hover:text-white',
                    )
                  }
                  key={item.to}
                  onClick={onNavigate}
                  title={item.description}
                  to={item.to}
                >
                  {({ isActive }) => (
                    <div className="flex items-start justify-between gap-3">
                      <div className="min-w-0">
                        <p className="text-sm font-semibold tracking-tight">{item.label}</p>
                      </div>
                      <span
                        className={cn(
                          'mt-1 h-2.5 w-2.5 shrink-0 rounded-full border transition-colors duration-150',
                          isActive ? 'border-teal-300 bg-teal-300' : 'border-slate-500 bg-transparent',
                        )}
                      />
                    </div>
                  )}
                </NavLink>
              ))}
            </nav>
          </section>
        ))}
      </div>

      <div className="mt-1 rounded-2xl border border-teal-500/20 bg-teal-500/10 p-4">
        <p className="text-xs font-semibold uppercase tracking-[0.24em] text-teal-200">Ruta segura</p>
        <p className="mt-2 text-sm leading-6 text-slate-200">
          Todas las pantallas del shell siguen protegidas por `AdminOps` vía `OpsLoginGate`.
        </p>
      </div>

    </div>
  )
}

export function OpsShell() {
  const [mobileDrawerOpen, setMobileDrawerOpen] = useState(false)
  const drawerButtonRef = useRef<HTMLButtonElement>(null)
  const previousOpenRef = useRef(false)

  useEffect(() => {
    if (previousOpenRef.current && !mobileDrawerOpen) {
      drawerButtonRef.current?.focus()
    }
    previousOpenRef.current = mobileDrawerOpen
  }, [mobileDrawerOpen])

  function handleDrawerOpenChange(nextOpen: boolean) {
    setMobileDrawerOpen(nextOpen)
  }

  function handleNavigate() {
    setMobileDrawerOpen(false)
  }

  return (
    <div className="min-h-screen bg-[radial-gradient(circle_at_top_left,_rgba(13,148,136,0.16),_transparent_28%),radial-gradient(circle_at_top_right,_rgba(15,23,42,0.10),_transparent_24%),linear-gradient(180deg,_#f8fafc_0%,_#ecfdf5_100%)] text-foreground">
      <div className="mx-auto flex min-h-screen w-full max-w-[1800px] flex-col gap-4 px-4 py-4 md:px-6 lg:flex-row lg:gap-8 lg:px-8 xl:px-10">
        <header className="flex items-center justify-between rounded-[1.75rem] border border-white/70 bg-white/82 px-4 py-4 shadow-[0_20px_50px_rgba(15,23,42,0.08)] backdrop-blur lg:hidden">
          <div>
            <p className="text-[11px] font-semibold uppercase tracking-[0.34em] text-teal-600">AdminOps</p>
            <h1 className="mt-1 text-xl font-semibold tracking-tight text-slate-950">Fibras Inmobiliarias Ops</h1>
          </div>
          <Dialog open={mobileDrawerOpen} onOpenChange={handleDrawerOpenChange}>
            <button
              ref={drawerButtonRef}
              type="button"
              className="inline-flex h-11 w-11 items-center justify-center rounded-full border border-slate-200 text-slate-800 transition-colors duration-150 hover:border-teal-500 hover:text-teal-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-teal-400 focus-visible:ring-offset-2 focus-visible:ring-offset-white cursor-pointer"
              aria-label="Abrir navegación de Ops"
              aria-expanded={mobileDrawerOpen}
              onClick={() => handleDrawerOpenChange(true)}
            >
              <Menu className="size-5" />
            </button>
            <DialogContent className="left-0 top-0 h-dvh w-[min(88vw,20rem)] translate-x-0 translate-y-0 rounded-none rounded-r-[2rem] border-r border-slate-200 bg-slate-950/98 p-0 text-slate-50 shadow-2xl">
              <DialogTitle className="sr-only">Navegación de Ops</DialogTitle>
              <DialogDescription className="sr-only">
                Accede a los módulos operativos, de datos, contenido, SEO, IA y sistema.
              </DialogDescription>
              <div className="flex h-full flex-col p-5">
                <OpsNavigationPanel onNavigate={handleNavigate} />
              </div>
            </DialogContent>
          </Dialog>
        </header>

        <aside className="hidden lg:sticky lg:top-4 lg:block lg:w-72 lg:flex-none xl:w-80 lg:self-start">
          <div className="flex flex-col rounded-[2rem] border border-white/70 bg-slate-950/94 p-5 text-slate-50 shadow-[0_24px_60px_rgba(15,23,42,0.22)] backdrop-blur">
            <OpsNavigationPanel onNavigate={handleNavigate} />
          </div>
        </aside>

        <div className="min-w-0 flex-1">
          <div className="rounded-[2rem] border border-white/70 bg-white/78 px-4 py-5 shadow-[0_20px_50px_rgba(15,23,42,0.08)] backdrop-blur md:px-6 md:py-6">
            <Suspense
              fallback={
                <div className="flex items-center justify-center py-24 text-sm text-slate-500">
                  Cargando módulo…
                </div>
              }
            >
              <Outlet />
            </Suspense>
          </div>
        </div>
      </div>
    </div>
  )
}
