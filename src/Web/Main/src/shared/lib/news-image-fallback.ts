const SECTOR_IMAGES: Record<string, string> = {
  industrial: '/assets/sectors/industrial.jpg',
  comercial: '/assets/sectors/comercial.jpg',
  oficinas: '/assets/sectors/oficinas.jpg',
  diversificado: '/assets/sectors/diversificado.jpg',
  salud: '/assets/sectors/salud.jpg',
  infraestructura: '/assets/sectors/infraestructura.jpg',
  otro: '/assets/sectors/otro.jpg',
}

type ArticleLike = {
  imageUrl?: string | null
}

type FibraLike = {
  logoUrl?: string | null
  sector?: string | null
} | null | undefined

export function getSectorImageUrl(sector?: string | null): string {
  const key = sector?.trim().toLowerCase() ?? 'otro'
  return SECTOR_IMAGES[key] ?? SECTOR_IMAGES.otro
}

export function getArticleImageUrl(article: ArticleLike, fibra?: FibraLike): string | null {
  return article.imageUrl ?? fibra?.logoUrl ?? null
}

export { SECTOR_IMAGES }
