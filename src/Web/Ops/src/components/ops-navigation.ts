export type OpsNavItem = {
  label: string
  to: string
  description: string
}

export type OpsNavSection = {
  title: string
  items: OpsNavItem[]
}

export const OPS_NAVIGATION_SECTIONS: OpsNavSection[] = [
  {
    title: 'Operación',
    items: [
      { label: 'Dashboard', to: '/dashboard', description: 'Estado de pipelines, errores y disparos manuales.' },
      { label: 'Logs del Pipeline', to: '/pipeline-logs', description: 'Errores estructurados listos para diagnóstico.' },
      { label: 'Llamadas IA', to: '/ai-call-logs', description: 'Historial de llamadas al proveedor de IA con respuestas.' },
    ],
  },
  {
    title: 'Datos',
    items: [
      { label: 'Catálogo', to: '/catalog', description: 'Agregar, editar y desactivar FIBRAs del universo.' },
      { label: 'Distribuciones', to: '/distribuciones', description: 'Calendario de pagos, ex derechos y edición manual.' },
      { label: 'Fundamentales', to: '/fundamentals', description: 'Importar, revisar y confirmar datos financieros por FIBRA.' },
    ],
  },
  {
    title: 'Contenido',
    items: [
      { label: 'Contenido Editorial', to: '/editorial', description: 'Editar textos educativos de la sección Conoce las FIBRAs.' },
      { label: 'Noticias', to: '/noticias', description: 'Curación de body text y resúmenes IA.' },
      { label: 'Blocklist', to: '/blocklist', description: 'Términos excluidos del pipeline RSS.' },
    ],
  },
  {
    title: 'SEO',
    items: [
      { label: 'SEO Organization', to: '/seo/organization', description: 'Gestionar perfiles oficiales verificados para sameAs.' },
      { label: 'SEO FAQ', to: '/seo/faq', description: 'Administrar preguntas frecuentes visibles y JSON-LD.' },
      { label: 'SEO Robots', to: '/seo/robots', description: 'Editar robots por página con presets y validación.' },
      { label: 'SEO Redirects', to: '/seo/redirects', description: 'Gestionar redirects 301/302 cacheados en memoria.' },
    ],
  },
  {
    title: 'IA',
    items: [
      { label: 'AI Config', to: '/ai-config', description: 'Modo, proveedor y disparos manuales.' },
      { label: 'Prompts de IA', to: '/ai-prompts', description: 'Templates editables sin redespliegue.' },
    ],
  },
  {
    title: 'Sistema',
    items: [
      { label: 'Configuración', to: '/config', description: 'Parámetros operativos del sistema sin redespliegue.' },
      { label: 'Usuarios', to: '/users', description: 'Crear y listar cuentas de usuario del sitio principal.' },
    ],
  },
]
