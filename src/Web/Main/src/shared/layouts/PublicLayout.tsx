import { Outlet } from 'react-router'
import { GlobalSearch } from '@/modules/home/GlobalSearch'

export function PublicLayout() {
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
          <a href="/" className="text-lg font-semibold tracking-tight">FIBRADIS</a>
          <nav
            aria-label="Navegación principal"
            className="hidden md:flex items-center gap-4 text-sm text-muted-foreground"
          >
            <a href="/mercado" className="hover:text-foreground transition-colors">Mercado</a>
            <a href="/catalogo" className="hover:text-foreground transition-colors">Catálogo</a>
            <a href="/noticias" className="hover:text-foreground transition-colors">Noticias</a>
          </nav>
          <div className="flex-1 min-w-0 flex justify-center">
            <GlobalSearch />
          </div>
          <div className="flex items-center gap-2">
            <a href="/login" className="text-sm text-muted-foreground hover:text-foreground transition-colors">
              Iniciar sesión
            </a>
          </div>
        </div>
      </header>

      <main id="main-content" tabIndex={-1} className="flex-1 outline-none">
        <Outlet />
      </main>
    </div>
  )
}
