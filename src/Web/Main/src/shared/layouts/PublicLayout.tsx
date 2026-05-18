import { Outlet } from 'react-router'
import { GlobalSearch } from '@/modules/home/GlobalSearch'

export function PublicLayout() {
  return (
    <div className="min-h-screen flex flex-col bg-background text-foreground">
      <header className="sticky top-0 z-50 border-b border-border bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60">
        <div className="container mx-auto flex h-14 items-center gap-6 px-4">
          <a href="/" className="text-lg font-semibold tracking-tight">FIBRADIS</a>
          <nav className="hidden md:flex items-center gap-4 text-sm text-muted-foreground">
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
      <main className="flex-1">
        <Outlet />
      </main>
    </div>
  )
}
