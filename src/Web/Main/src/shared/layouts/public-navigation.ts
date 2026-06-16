export type PublicLayoutStatus = 'checking' | 'anonymous' | 'authenticated'

export type NavLinkItem = {
  label: string
  to: string
}

export type MenuEntry = NavLinkItem | {
  label: string
  onClick: () => void
}

export type MobileSection = {
  title: string
  items: MenuEntry[]
}

export const MAIN_PRIMARY_LINKS: NavLinkItem[] = [
  { label: 'Conoce las FIBRAs', to: '/conoce-las-fibras' },
  { label: 'Fibras', to: '/fibras' },
  { label: 'Comparar', to: '/comparar' },
  { label: 'Noticias', to: '/noticias' },
  { label: 'Calculadora', to: '/calculadora' },
  { label: 'Calendario', to: '/calendario' },
  { label: 'Fundamentales', to: '/fundamentales' },
]

export const MAIN_INVESTMENT_LINKS: NavLinkItem[] = [
  { label: 'Portafolio', to: '/portafolio' },
  { label: 'Oportunidades', to: '/oportunidades' },
  { label: 'Herramientas', to: '/herramientas' },
  { label: 'Reportes', to: '/reportes' },
]

export const MAIN_ACCOUNT_LINKS: MenuEntry[] = [{ label: 'Mi perfil', to: '/perfil' }]

export function buildMainMobileSections(
  status: PublicLayoutStatus,
  onLogout: () => void = () => undefined,
): MobileSection[] {
  const sections: MobileSection[] = [{ title: 'Navegar', items: MAIN_PRIMARY_LINKS }]

  if (status === 'authenticated') {
    sections.push({ title: 'Mi inversión', items: MAIN_INVESTMENT_LINKS })
  }

  sections.push({
    title: 'Cuenta',
    items: status === 'authenticated'
      ? [...MAIN_ACCOUNT_LINKS, { label: 'Cerrar sesión', onClick: onLogout }]
      : [{ label: 'Iniciar sesión', to: '/login' }],
  })

  return sections
}

export function shouldCloseMenuOnEscape(key: string): boolean {
  return key === 'Escape'
}
