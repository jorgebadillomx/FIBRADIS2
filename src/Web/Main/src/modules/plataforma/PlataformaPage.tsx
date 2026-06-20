import { type LucideIcon, ArrowRight, BarChart3, CalendarDays, Calculator, FileText, Heart, LayoutGrid, LineChart, ShieldCheck, Sparkles, TrendingUp, Wallet } from 'lucide-react'
import { Link } from 'react-router'
import { Button } from '@/shared/ui/button'
import { usePageTitle } from '@/shared/hooks/usePageTitle'

const PAGE_TITLE = 'Plataforma de Fibras Inmobiliarias — descubre funciones y acceso | Fibras Inmobiliarias'
const PAGE_DESCRIPTION =
  'Descubre Fibras Inmobiliarias: catálogo, fichas, comparador, calculadora, noticias, calendario, portafolio, reportes y herramientas para invertir mejor.'

type PublicFeature = {
  title: string
  description: string
  href: string
  cta: string
  icon: LucideIcon
}

type PrivateFeature = {
  title: string
  description: string
  note: string
  icon: LucideIcon
}

const PUBLIC_FEATURES: PublicFeature[] = [
  {
    title: 'Catálogo y fichas de FIBRAs',
    description: 'Explora el universo completo de emisoras con búsqueda y filtros, y abre la ficha pública de cada FIBRA con precio, distribuciones, fundamentales y score.',
    href: '/fibras',
    cta: 'Abrir catálogo',
    icon: LayoutGrid,
  },
  {
    title: 'Comparador',
    description: 'Pon hasta cuatro FIBRAs lado a lado para revisar precio, yield, fundamentales y oportunidad relativa.',
    href: '/comparar',
    cta: 'Comparar',
    icon: BarChart3,
  },
  {
    title: 'Fundamentales',
    description: 'Revisa Cap Rate, NAV, LTV, NOI Margin y FFO Margin con lectura comparativa entre emisoras.',
    href: '/fundamentales',
    cta: 'Ver métricas',
    icon: LineChart,
  },
  {
    title: 'Calculadora',
    description: 'Simula cuántos CBFIs puedes comprar, cuánto ingreso objetivo necesitas y qué retorno total esperar.',
    href: '/calculadora',
    cta: 'Usar calculadora',
    icon: Calculator,
  },
  {
    title: 'Calendario',
    description: 'Sigue asambleas, distribuciones y eventos corporativos de FIBRAs mexicanas en una vista ordenada.',
    href: '/calendario',
    cta: 'Abrir calendario',
    icon: CalendarDays,
  },
  {
    title: 'Noticias',
    description: 'Lee la cobertura editorial del mercado con noticias recientes, contexto y seguimiento por FIBRA.',
    href: '/noticias',
    cta: 'Leer noticias',
    icon: FileText,
  },
  {
    title: 'Guía “¿Qué son las FIBRAs?”',
    description: 'Empieza por la explicación base del producto, su funcionamiento y por qué importa para el inversionista.',
    href: '/conoce-las-fibras',
    cta: 'Leer guía',
    icon: Sparkles,
  },
]

const PRIVATE_FEATURES: PrivateFeature[] = [
  {
    title: 'Portafolio y dashboard',
    description: 'KPIs consolidados, seguimiento de posiciones y calendario de distribuciones en una sola vista privada.',
    note: 'Disponible tras iniciar sesión',
    icon: Wallet,
  },
  {
    title: 'Reportes trimestrales por FIBRA',
    description: 'Fundamentales por período con análisis IA, señales, alertas y perspectiva del trimestre seleccionado.',
    note: 'Disponible tras iniciar sesión',
    icon: FileText,
  },
  {
    title: 'Oportunidades y ranking',
    description: 'Score configurable para comparar emisoras y priorizar oportunidades con mayor potencial relativo.',
    note: 'Disponible tras iniciar sesión',
    icon: TrendingUp,
  },
  {
    title: 'Herramientas',
    description: 'Calculadoras y escenarios avanzados para analizar rendimiento, renta y retorno sin salir de la plataforma.',
    note: 'Disponible tras iniciar sesión',
    icon: ShieldCheck,
  },
  {
    title: 'Favoritos',
    description: 'Guarda tus FIBRAs de interés y mantenlas listas para revisarlas desde portafolio y oportunidades.',
    note: 'Disponible tras iniciar sesión',
    icon: Heart,
  },
]

export function PlataformaPage() {
  usePageTitle(PAGE_TITLE, PAGE_DESCRIPTION, { canonicalPath: '/plataforma' })

  return (
    <div className="relative overflow-hidden bg-[radial-gradient(circle_at_top_left,rgba(26,74,58,0.12),transparent_28%),radial-gradient(circle_at_top_right,rgba(154,123,46,0.12),transparent_24%),linear-gradient(180deg,rgba(10,14,26,0.03),transparent_24%)]">
      <div className="pointer-events-none absolute inset-x-0 top-0 h-80 bg-[linear-gradient(180deg,rgba(26,74,58,0.08),transparent)]" />

      <div className="container mx-auto max-w-7xl px-4 py-10 md:py-12">
        <section className="grid gap-8 lg:grid-cols-[minmax(0,1.15fr)_minmax(18rem,0.85fr)] lg:items-start">
          <div className="space-y-6">
            <div className="inline-flex items-center gap-2 rounded-full border border-border bg-surface-elevated px-3 py-1 text-[11px] font-semibold uppercase tracking-[0.28em] text-primary shadow-sm">
              Descubrimiento de plataforma
            </div>

            <div className="space-y-4">
              <h1 className="max-w-3xl font-playfair text-4xl font-bold leading-tight text-foreground md:text-5xl">
                Todo lo que Fibras Inmobiliarias ofrece, explicado de forma ordenada y listo para descubrir.
              </h1>
              <p className="max-w-3xl text-base leading-7 text-muted-foreground md:text-lg">
                Esta landing muestra el lado público y privado de la plataforma sin datos vivos:
                catálogo, fichas, comparador, calculadora, noticias y guía para descubrir el producto;
                luego el portafolio, los reportes, las oportunidades, las herramientas y los favoritos.
              </p>
            </div>

            <div className="flex flex-wrap gap-3">
              <Button asChild size="lg" className="cursor-pointer">
                <Link to="/fibras">
                  Ver catálogo
                  <ArrowRight className="size-4" />
                </Link>
              </Button>
              <Button asChild size="lg" variant="outline" className="cursor-pointer">
                <Link to="/registro">Crear cuenta</Link>
              </Button>
              <Button asChild size="lg" variant="ghost" className="cursor-pointer">
                <Link to="/portafolio">Iniciar sesión</Link>
              </Button>
            </div>

            <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
              <StatTile label="Funciones públicas" value="7" />
              <StatTile label="Funciones privadas" value="5" />
              <StatTile label="Ruta de marca" value="/plataforma" />
              <StatTile label="Entrada privada" value="/portafolio" />
            </div>
          </div>

          <aside className="rounded-3xl border border-border bg-surface-elevated/95 p-6 shadow-sm backdrop-blur">
            <div className="space-y-4">
              <p className="text-xs font-semibold uppercase tracking-[0.24em] text-muted-foreground">
                Cómo leer esta página
              </p>
              <ul className="space-y-3 text-sm leading-6 text-foreground">
                <li>• Cada bloque público enlaza directamente a una superficie indexable del sitio.</li>
                <li>• Las capacidades privadas se describen sin exponer datos ni forzar login en la landing.</li>
                <li>• El acceso al área privada siempre parte de /portafolio.</li>
              </ul>
            </div>
          </aside>
        </section>

        <section id="publicas" className="mt-10">
          <div className="max-w-3xl space-y-3">
            <p className="text-xs font-semibold uppercase tracking-[0.24em] text-muted-foreground">
              Público
            </p>
            <h2 className="font-playfair text-3xl font-semibold tracking-tight text-foreground">
              Funcionalidades públicas
            </h2>
            <p className="text-sm leading-6 text-muted-foreground md:text-base">
              Estas superficies están pensadas para descubrimiento orgánico y enlazan a contenido que
              cualquier visitante puede leer sin autenticarse.
            </p>
          </div>

          <div className="mt-6 grid gap-4 md:grid-cols-2 xl:grid-cols-4">
            {PUBLIC_FEATURES.map((feature) => {
              const Icon = feature.icon

              return (
                <Link
                  key={feature.title}
                  to={feature.href}
                  className="group rounded-3xl border border-border bg-background/90 p-5 shadow-sm transition-all duration-200 hover:-translate-y-0.5 hover:border-primary/40 hover:shadow-md focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2 focus-visible:ring-offset-background motion-reduce:transition-none motion-reduce:hover:translate-y-0 cursor-pointer"
                >
                  <div className="flex items-start gap-4">
                    <div className="flex size-12 shrink-0 items-center justify-center rounded-2xl border border-border bg-surface-elevated text-primary">
                      <Icon className="size-5" />
                    </div>
                    <div className="min-w-0 flex-1">
                      <h3 className="font-playfair text-xl font-semibold text-foreground">
                        {feature.title}
                      </h3>
                      <p className="mt-2 text-sm leading-6 text-muted-foreground">
                        {feature.description}
                      </p>
                    </div>
                  </div>

                  <div className="mt-5 inline-flex items-center gap-1 text-sm font-semibold text-primary transition-colors duration-150 group-hover:text-primary/80">
                    {feature.cta}
                    <ArrowRight className="size-4" />
                  </div>
                </Link>
              )
            })}
          </div>
        </section>

        <section id="privadas" className="mt-10">
          <div className="max-w-3xl space-y-3">
            <p className="text-xs font-semibold uppercase tracking-[0.24em] text-muted-foreground">
              Privado
            </p>
            <h2 className="font-playfair text-3xl font-semibold tracking-tight text-foreground">
              Funcionalidades privadas
            </h2>
            <p className="text-sm leading-6 text-muted-foreground md:text-base">
              Estos módulos quedan detrás de autenticación, pero la landing los describe para que
              el visitante entienda el alcance real de la plataforma antes de entrar.
            </p>
          </div>

          <div className="mt-6 grid gap-4 md:grid-cols-2 xl:grid-cols-3">
            {PRIVATE_FEATURES.map((feature) => {
              const Icon = feature.icon

              return (
                <article
                  key={feature.title}
                  className="rounded-3xl border border-border bg-background/90 p-5 shadow-sm"
                >
                  <div className="flex items-start gap-4">
                    <div className="flex size-12 shrink-0 items-center justify-center rounded-2xl border border-border bg-surface-elevated text-primary">
                      <Icon className="size-5" />
                    </div>
                    <div className="min-w-0 flex-1">
                      <div className="flex flex-wrap items-center gap-2">
                        <h3 className="font-playfair text-xl font-semibold text-foreground">
                          {feature.title}
                        </h3>
                        <span className="rounded-full border border-amber-200 bg-amber-50 px-2 py-0.5 text-[10px] font-semibold uppercase tracking-[0.22em] text-amber-700">
                          Privado
                        </span>
                      </div>
                      <p className="mt-2 text-sm leading-6 text-muted-foreground">
                        {feature.description}
                      </p>
                    </div>
                  </div>

                  <p className="mt-4 text-xs font-semibold uppercase tracking-[0.22em] text-muted-foreground">
                    {feature.note}
                  </p>
                </article>
              )
            })}
          </div>

          <div className="mt-8 rounded-3xl border border-border bg-surface-elevated/95 p-6 shadow-sm">
            <div className="grid gap-4 lg:grid-cols-[minmax(0,1fr)_auto] lg:items-center">
              <div className="space-y-2">
                <p className="text-xs font-semibold uppercase tracking-[0.24em] text-muted-foreground">
                  Entrada privada
                </p>
                <h3 className="font-playfair text-2xl font-semibold text-foreground">
                  Entra a tu portafolio y desbloquea el resto de la plataforma
                </h3>
                <p className="max-w-2xl text-sm leading-6 text-muted-foreground">
                  El mismo acceso te lleva al dashboard, a los reportes trimestrales, a las
                  oportunidades configurables, a las herramientas y a los favoritos.
                </p>
              </div>

              <Button asChild size="lg" className="cursor-pointer justify-self-start lg:justify-self-end">
                <Link to="/portafolio">
                  Ir a Portafolio
                  <ArrowRight className="size-4" />
                </Link>
              </Button>
            </div>
          </div>
        </section>
      </div>
    </div>
  )
}

function StatTile({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-2xl border border-border bg-background/80 px-4 py-3 shadow-sm">
      <p className="text-[11px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">
        {label}
      </p>
      <p className="mt-2 text-lg font-semibold tracking-tight text-foreground">{value}</p>
    </div>
  )
}
