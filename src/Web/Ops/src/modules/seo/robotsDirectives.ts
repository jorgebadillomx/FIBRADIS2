export const INDEXABLE_ROBOTS_DIRECTIVES = 'index,follow,max-image-preview:large,max-snippet:-1,max-video-preview:-1'
export const NOINDEX_ROBOTS_DIRECTIVES = 'noindex,nofollow'
export const INDEX_WITHOUT_SNIPPET_ROBOTS_DIRECTIVES = 'index,follow,max-snippet:0'

export type RobotsImagePreview = 'none' | 'standard' | 'large'

export interface RobotsDirectivesDraft {
  index: boolean
  follow: boolean
  maxSnippet: string
  maxImagePreview: '' | RobotsImagePreview
  maxVideoPreview: string
  // Tokens válidos sin control dedicado en la UI (noarchive, nosnippet, noimageindex).
  // Se preservan tal cual para que abrir+guardar una fila no los elimine silenciosamente.
  extras: string[]
}

const PASSTHROUGH_DIRECTIVES = new Set(['noarchive', 'nosnippet', 'noimageindex'])

export interface RobotsPreset {
  id: string
  label: string
  value: string
}

export const ROBOTS_PRESETS: RobotsPreset[] = [
  {
    id: 'indexable',
    label: 'Indexable (recomendado)',
    value: INDEXABLE_ROBOTS_DIRECTIVES,
  },
  {
    id: 'noindex',
    label: 'No indexar',
    value: NOINDEX_ROBOTS_DIRECTIVES,
  },
  {
    id: 'index-without-snippet',
    label: 'Indexar sin snippet',
    value: INDEX_WITHOUT_SNIPPET_ROBOTS_DIRECTIVES,
  },
]

const VALID_IMAGE_PREVIEWS: RobotsImagePreview[] = ['none', 'standard', 'large']

export function createDefaultRobotsDraft(): RobotsDirectivesDraft {
  return parseRobotsDirectives('')
}

export function parseRobotsDirectives(value?: string | null): RobotsDirectivesDraft {
  const normalized = (value ?? '').trim().toLowerCase()

  // Vacío = default seguro (indexable). Se devuelve el mismo draft mínimo que para
  // 'index,follow' para no inflar una fila default en un override verboso al guardar.
  const draft: RobotsDirectivesDraft = {
    index: true,
    follow: true,
    maxSnippet: '',
    maxImagePreview: '',
    maxVideoPreview: '',
    extras: [],
  }

  if (!normalized) {
    return draft
  }

  const directives = normalized.split(',').map((item) => item.trim()).filter(Boolean)

  for (const directive of directives) {
    if (directive === 'index' || directive === 'all') {
      draft.index = true
      if (directive === 'all') draft.follow = true
      continue
    }

    if (directive === 'noindex' || directive === 'none') {
      draft.index = false
      if (directive === 'none') draft.follow = false
      continue
    }

    if (directive === 'follow') {
      draft.follow = true
      continue
    }

    if (directive === 'nofollow') {
      draft.follow = false
      continue
    }

    if (directive.startsWith('max-snippet:')) {
      draft.maxSnippet = directive.slice('max-snippet:'.length)
      continue
    }

    if (directive.startsWith('max-image-preview:')) {
      const valuePart = directive.slice('max-image-preview:'.length) as RobotsImagePreview
      if (VALID_IMAGE_PREVIEWS.includes(valuePart)) {
        draft.maxImagePreview = valuePart
      }
      continue
    }

    if (directive.startsWith('max-video-preview:')) {
      draft.maxVideoPreview = directive.slice('max-video-preview:'.length)
      continue
    }

    // Tokens válidos en backend pero sin control en la UI: preservarlos en el round-trip.
    if (PASSTHROUGH_DIRECTIVES.has(directive) && !draft.extras.includes(directive)) {
      draft.extras.push(directive)
    }
  }

  return draft
}

export function buildRobotsDirectives(draft: RobotsDirectivesDraft): string {
  const tokens: string[] = []
  tokens.push(draft.index ? 'index' : 'noindex')
  tokens.push(draft.follow ? 'follow' : 'nofollow')

  if (draft.maxImagePreview) {
    tokens.push(`max-image-preview:${draft.maxImagePreview}`)
  }

  const maxSnippet = draft.maxSnippet.trim()
  if (maxSnippet) {
    tokens.push(`max-snippet:${maxSnippet}`)
  }

  const maxVideoPreview = draft.maxVideoPreview.trim()
  if (maxVideoPreview) {
    tokens.push(`max-video-preview:${maxVideoPreview}`)
  }

  for (const extra of draft.extras) {
    tokens.push(extra)
  }

  return tokens.join(',')
}

export function applyRobotsPreset(value: string): RobotsDirectivesDraft {
  return parseRobotsDirectives(value)
}
