import { NavLink, Outlet } from 'react-router'
import { cn } from '@/shared/lib/utils'

const navigationItems = [
  { label: 'Dashboard', to: '/dashboard', description: 'Estado de pipelines, errores y disparos manuales.' },
  { label: 'Distribuciones', to: '/distribuciones', description: 'Calendario de pagos, ex derechos y edición manual.' },
  { label: 'Catálogo', to: '/catalog', description: 'Agregar, editar y desactivar FIBRAs del universo.' },
  { label: 'AI Config', to: '/ai-config', description: 'Modo, proveedor y disparos manuales.' },
  { label: 'Contenido Editorial', to: '/editorial', description: 'Editar textos educativos de la sección Conoce las FIBRAs.' },
  { label: 'SEO Organization', to: '/seo/organization', description: 'Gestionar perfiles oficiales verificados para sameAs.' },
  { label: 'SEO FAQ', to: '/seo/faq', description: 'Administrar preguntas frecuentes visibles y JSON-LD.' },
  { label: 'SEO Robots', to: '/seo/robots', description: 'Editar robots por página con presets y validación.' },
  { label: 'SEO Redirects', to: '/seo/redirects', description: 'Gestionar redirects 301/302 cacheados en memoria.' },
  { label: 'Noticias', to: '/noticias', description: 'Curación de body text y resúmenes IA.' },
  { label: 'Blocklist', to: '/blocklist', description: 'Términos excluidos del pipeline RSS.' },
  { label: 'Logs del Pipeline', to: '/pipeline-logs', description: 'Errores estructurados listos para diagnóstico.' },
  { label: 'Llamadas IA', to: '/ai-call-logs', description: 'Historial de llamadas al proveedor de IA con respuestas.' },
  { label: 'Fundamentales', to: '/fundamentals', description: 'Importar, revisar y confirmar datos financieros por FIBRA.' },
  { label: 'Prompts de IA', to: '/ai-prompts', description: 'Templates editables sin redespliegue.' },
  { label: 'Configuración', to: '/config', description: 'Parámetros operativos del sistema sin redespliegue.' },
  { label: 'Usuarios', to: '/users', description: 'Crear y listar cuentas de usuario del sitio principal.' },
]

export function OpsShell() {
  return (
    <div className="min-h-screen bg-[radial-gradient(circle_at_top_left,_rgba(13,148,136,0.16),_transparent_28%),radial-gradient(circle_at_top_right,_rgba(15,23,42,0.10),_transparent_24%),linear-gradient(180deg,_#f8fafc_0%,_#ecfdf5_100%)] text-foreground">
      <div className="mx-auto flex min-h-screen w-full max-w-[1800px] flex-col gap-6 px-4 py-4 md:px-6 lg:flex-row lg:gap-8 lg:px-8 xl:px-10">
        <aside className="lg:sticky lg:top-4 lg:w-72 xl:w-80 lg:flex-none lg:self-start">
          <div className="flex flex-col rounded-[2rem] border border-white/70 bg-slate-950/94 p-5 text-slate-50 shadow-[0_24px_60px_rgba(15,23,42,0.22)] backdrop-blur">
            <div className="border-b border-white/10 pb-5">
              <p className="text-[11px] font-semibold uppercase tracking-[0.34em] text-teal-300">AdminOps</p>
              <h1 className="mt-2 text-2xl font-semibold tracking-tight">FIBRADIS Ops</h1>
              <p className="mt-2 text-sm leading-6 text-slate-300">
                Centro operativo para navegar módulos, ajustar prompts y diagnosticar fallas con contexto claro.
              </p>
            </div>

            <nav className="mt-5 flex flex-col gap-2">
              {navigationItems.map((item) => (
                <NavLink
                  className={({ isActive }) =>
                    cn(
                      'group rounded-2xl border px-4 py-3 transition',
                      isActive
                        ? 'border-teal-500/60 bg-teal-500/16 text-white shadow-[inset_0_0_0_1px_rgba(45,212,191,0.35)]'
                        : 'border-white/8 bg-white/[0.03] text-slate-300 hover:border-white/16 hover:bg-white/[0.06] hover:text-white',
                    )
                  }
                  key={item.to}
                  to={item.to}
                >
                  {({ isActive }) => (
                    <div className="flex items-start justify-between gap-3">
                      <div>
                        <p className="text-sm font-semibold tracking-tight">{item.label}</p>
                        <p className="mt-1 text-xs leading-5 text-slate-400 group-hover:text-slate-300">
                          {item.description}
                        </p>
                      </div>
                      <span
                        className={cn(
                          'mt-1 h-2.5 w-2.5 rounded-full border transition',
                          isActive ? 'border-teal-300 bg-teal-300' : 'border-slate-500 bg-transparent',
                        )}
                      />
                    </div>
                  )}
                </NavLink>
              ))}
            </nav>

            <div className="mt-auto rounded-2xl border border-teal-500/20 bg-teal-500/10 p-4">
              <p className="text-xs font-semibold uppercase tracking-[0.24em] text-teal-200">Ruta segura</p>
              <p className="mt-2 text-sm leading-6 text-slate-200">
                Todas las pantallas del shell siguen protegidas por `AdminOps` vía `OpsLoginGate`.
              </p>
            </div>
          </div>
        </aside>

        <div className="min-w-0 flex-1">
          <div className="rounded-[2rem] border border-white/70 bg-white/78 px-4 py-5 shadow-[0_20px_50px_rgba(15,23,42,0.08)] backdrop-blur md:px-6 md:py-6">
            <Outlet />
          </div>
        </div>
      </div>
    </div>
  )
}
