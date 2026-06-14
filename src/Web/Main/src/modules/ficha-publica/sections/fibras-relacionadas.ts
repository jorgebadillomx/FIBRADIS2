import type { RelatedFibra } from '@/api/fibrasApi'

// Lógica pura testeable (sin imports de valor con alias @/, para correr bajo node --test).
// El mapeo a slug se hace en el componente con buildFibraSlug (ya cubierto por fibra-slug.test.ts).

/** La sección solo se muestra si hay al menos una fibra relacionada (AC-2: sin estado vacío feo). */
export function shouldShowRelacionadas(related: RelatedFibra[] | undefined): boolean {
  return (related?.length ?? 0) > 0
}
