export const NOTICIAS_PAGINATED_ROBOTS = 'noindex,follow'

export function buildNoticiasCanonicalPath(page: number): string {
  return page > 1 ? `/noticias?page=${page}` : '/noticias'
}

export function buildNoticiasRobotsDirectives(page: number): string | null {
  return page > 1 ? NOTICIAS_PAGINATED_ROBOTS : null
}
