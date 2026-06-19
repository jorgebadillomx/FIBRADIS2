import { lazy, Suspense } from 'react'
import { Link } from 'react-router'
import { X } from 'lucide-react'
import {
  Dialog,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from '@/shared/ui/dialog'
import {
  buildMainMobileSections,
  isMenuEntryLink,
  type MenuEntry,
  type PublicLayoutStatus,
} from './public-navigation'

const GlobalSearch = lazy(() =>
  import('@/modules/home/GlobalSearch').then(m => ({ default: m.GlobalSearch })),
)

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

function MobileMenuContent({
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

export function MobileNavigationDialog({
  open,
  onOpenChange,
  status,
  onLogout,
}: {
  open: boolean
  onOpenChange: (open: boolean) => void
  status: PublicLayoutStatus
  onLogout: () => void
}) {
  const close = () => onOpenChange(false)

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent
        className="left-0 top-0 flex h-dvh flex-col gap-0 w-[min(88vw,22rem)] translate-x-0 translate-y-0 rounded-none rounded-r-[2rem] border-r border-border bg-background p-0 text-foreground shadow-2xl"
        showCloseButton={false}
      >
        <DialogHeader className="shrink-0 border-b border-border px-4 py-4">
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
            <Suspense fallback={<div className="h-9 w-full rounded-lg border border-input bg-transparent" />}>
              <GlobalSearch className="max-w-none" onSelect={close} />
            </Suspense>
          </div>
          <MobileMenuContent status={status} onNavigate={close} onLogout={onLogout} />
        </div>
      </DialogContent>
    </Dialog>
  )
}
