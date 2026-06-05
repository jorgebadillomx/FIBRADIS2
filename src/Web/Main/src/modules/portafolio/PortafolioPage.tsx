import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { Star } from 'lucide-react'
import type { components } from '@fibradis/shared-api-client'
import { apiClient } from '@/api/fibrasApi'
import { KpiCards } from '@/modules/portafolio/KpiCards'
import { ColumnPicker } from '@/modules/portafolio/ColumnPicker'
import { PositionsTable } from '@/modules/portafolio/PositionsTable'
import { UploadZone } from '@/modules/portafolio/UploadZone'
import { useFavorites } from '@/modules/oportunidades/useFavorites'
import { Button } from '@/shared/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/shared/ui/dialog'

type PortfolioResponseDto = components['schemas']['PortfolioResponseDto']
type PortfolioColumnConfigDto = components['schemas']['PortfolioColumnConfigDto']
type PortfolioSnapshotStatusDto = components['schemas']['PortfolioSnapshotStatusDto']

function formatSnapshotDate(archivedAt: string | null | undefined) {
  if (!archivedAt) return 'fecha desconocida'

  return new Intl.DateTimeFormat('es-MX', {
    day: '2-digit',
    month: 'long',
    year: 'numeric',
  }).format(new Date(archivedAt))
}

export function PortafolioPage() {
  const queryClient = useQueryClient()
  const [favoritasFirst, setFavoritasFirst] = useState(false)
  const { favoriteIds, toggle: toggleFavorite, isAuthenticated } = useFavorites()
  const portfolioQuery = useQuery<PortfolioResponseDto>({
    queryKey: ['portfolio', 'positions'],
    queryFn: async () => {
      const { data, error } = await apiClient.GET('/api/v1/portfolio', {})
      if (error || !data) throw new Error('No se pudo cargar el portafolio.')
      return data
    },
  })

  const columnConfigQuery = useQuery<PortfolioColumnConfigDto>({
    queryKey: ['portfolio', 'column-config'],
    queryFn: async () => {
      const { data, error } = await apiClient.GET('/api/v1/portfolio/column-config', {})
      if (error || !data) throw new Error('No se pudo cargar la configuración de columnas.')
      return data
    },
  })

  const snapshotQuery = useQuery<PortfolioSnapshotStatusDto>({
    queryKey: ['portfolio', 'snapshot'],
    queryFn: async () => {
      const { data, error } = await apiClient.GET('/api/v1/portfolio/snapshot', {})
      if (error || !data) throw new Error('No se pudo cargar el respaldo del portafolio.')
      return data
    },
  })

  const [showArchiveDialog, setShowArchiveDialog] = useState(false)
  const [showRestoreDialog, setShowRestoreDialog] = useState(false)

  const archiveMutation = useMutation({
    mutationFn: async () => {
      const { error } = await apiClient.POST('/api/v1/portfolio/archive', {})
      if (error) throw new Error('No se pudo archivar el portafolio.')
    },
    onSuccess: async () => {
      setShowArchiveDialog(false)
      await queryClient.invalidateQueries({ queryKey: ['portfolio'] })
    },
  })

  const restoreMutation = useMutation({
    mutationFn: async () => {
      const { error } = await apiClient.POST('/api/v1/portfolio/restore', {})
      if (error) throw new Error('No se pudo restaurar el respaldo.')
    },
    onSuccess: async () => {
      setShowRestoreDialog(false)
      await queryClient.invalidateQueries({ queryKey: ['portfolio'] })
    },
  })

  const enabledColumns = columnConfigQuery.data?.columns ?? []

  const patchMutation = useMutation({
    mutationFn: async ({
      fibraId,
      titulos,
      costoPromedio,
    }: {
      fibraId: string
      titulos: number
      costoPromedio: number
    }) => {
      const { error } = await apiClient.PATCH('/api/v1/portfolio/positions/{fibraId}', {
        params: { path: { fibraId } },
        body: { titulos, costoPromedio },
      })
      if (error) throw new Error('No se pudo guardar la posición.')
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['portfolio'] })
    },
  })

  const deleteMutation = useMutation({
    mutationFn: async (fibraId: string) => {
      const { error } = await apiClient.DELETE('/api/v1/portfolio/positions/{fibraId}', {
        params: { path: { fibraId } },
      })
      if (error) throw new Error('No se pudo eliminar la posición.')
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['portfolio'] })
    },
  })

  if (portfolioQuery.isLoading) {
    return (
      <div className="container mx-auto max-w-7xl px-4 py-8">
        <div className="rounded-2xl border border-border bg-card px-4 py-6 shadow-sm">
          <div className="h-4 w-32 animate-pulse rounded bg-muted" />
          <div className="mt-4 h-8 w-56 animate-pulse rounded bg-muted/80" />
          <div className="mt-6 space-y-3">
            <div className="h-24 animate-pulse rounded-2xl bg-muted/60" />
            <div className="h-72 animate-pulse rounded-2xl bg-muted/60" />
          </div>
        </div>
      </div>
    )
  }

  if (portfolioQuery.isError) {
    return (
      <div className="container mx-auto max-w-7xl px-4 py-8">
        <div className="rounded-2xl border border-destructive/20 bg-destructive/5 p-6">
          <h1 className="text-2xl font-semibold font-playfair">Mi Portafolio</h1>
          <p className="mt-2 text-sm text-muted-foreground">
            No se pudo cargar el portafolio. Intenta de nuevo.
          </p>
          <Button className="mt-4" onClick={() => void portfolioQuery.refetch()}>
            Reintentar
          </Button>
        </div>
      </div>
    )
  }

  const portfolio = portfolioQuery.data
  const positions = portfolio?.positions ?? []
  const hasPositions = positions.length > 0
  const favoritesCount = positions.filter((position) => favoriteIds.has(position.fibraId)).length
  const snapshot = snapshotQuery.data
  const hasSnapshot = snapshot?.hasSnapshot ?? false
  const archivedAtLabel = formatSnapshotDate(snapshot?.archivedAt)

  return (
    <div className="container mx-auto max-w-7xl px-4 py-8">
      <div className="mb-6 flex flex-col gap-3 lg:flex-row lg:items-end lg:justify-between">
        <div>
          <h1 className="text-3xl font-semibold tracking-tight font-playfair">Mi Portafolio</h1>
          <p className="mt-1 text-sm text-muted-foreground">
            KPIs calculados en backend y tabla compacta con orden multi-criterio.
          </p>
        </div>

        {hasPositions && (
          <div className="flex flex-wrap items-center gap-3">
            <button
              type="button"
              onClick={() => setFavoritasFirst((value) => !value)}
              className={`flex items-center gap-1.5 rounded-md border px-3 py-1.5 text-sm transition-colors ${
                favoritasFirst
                  ? 'border-primary bg-primary/10 text-primary'
                  : 'border-input text-muted-foreground hover:text-foreground'
              }`}
            >
              <Star size={14} className={favoritasFirst ? 'fill-primary text-primary' : ''} />
              Favoritas primero
            </button>
            <ColumnPicker
              enabledColumns={enabledColumns}
              onEnabledColumnsChange={(columns) => {
                queryClient.setQueryData<PortfolioColumnConfigDto>(['portfolio', 'column-config'], {
                  columns,
                })
              }}
            />
            <Button variant="outline" onClick={() => setShowArchiveDialog(true)}>
              Archivar portafolio
            </Button>
          </div>
        )}
      </div>

      {hasSnapshot && (
        <div className="mb-6 rounded-2xl border border-amber-200 bg-amber-50 px-4 py-3 text-amber-950 shadow-sm">
          <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <p className="text-sm">
              Tienes un respaldo del {archivedAtLabel}. Puedes restaurarlo cuando quieras.
            </p>
            <Button variant="outline" onClick={() => setShowRestoreDialog(true)}>
              Restaurar respaldo
            </Button>
          </div>
        </div>
      )}

      {!hasPositions ? (
        <div className="space-y-6">
          <div className="rounded-2xl border border-border bg-card p-5 shadow-sm">
            <p className="text-sm text-muted-foreground">
              Sube un archivo Excel o CSV con tus posiciones para comenzar. El archivo debe contener
              las columnas <code className="font-mono rounded bg-muted px-1">Ticker</code>,{' '}
              <code className="font-mono rounded bg-muted px-1">Qty</code> y{' '}
              <code className="font-mono rounded bg-muted px-1">AvgCost</code>.
            </p>
          </div>

          <UploadZone
            currentPositionCount={0}
            onUploadSuccess={() => {}}
          />
        </div>
      ) : (
        <div className="space-y-6">
          <KpiCards kpis={portfolio?.kpis} />
          {favoritesCount > 0 && (
            <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
              <article className="rounded-2xl border border-border bg-card/80 p-4 shadow-sm backdrop-blur-sm">
                <div className="mb-2 flex items-center gap-2 text-xs font-medium uppercase tracking-[0.18em] text-muted-foreground">
                  <span>Favoritas ★</span>
                </div>
                <div className="text-2xl font-semibold tracking-tight tabular-nums text-foreground">
                  {favoritesCount}
                </div>
              </article>
            </div>
          )}

          <div className="space-y-3">
            <div className="flex items-center justify-between gap-3">
              <div>
                <h2 className="text-lg font-semibold">Posiciones</h2>
                <p className="text-sm text-muted-foreground">
                  Haz clic en un encabezado para ordenar. Shift + clic agrega un segundo criterio.
                </p>
              </div>
            </div>

            <PositionsTable
              positions={positions}
              enabledColumns={enabledColumns}
              favoriteIds={favoriteIds}
              onToggleFavorite={toggleFavorite}
              favoritasFirst={favoritasFirst}
              isAuthenticated={isAuthenticated}
              onUpdate={(fibraId, titulos, costoPromedio) =>
                patchMutation.mutateAsync({ fibraId, titulos, costoPromedio })
              }
              onDelete={(fibraId) => deleteMutation.mutateAsync(fibraId)}
            />
          </div>

          <UploadZone
            currentPositionCount={positions.length}
            onUploadSuccess={() => {}}
          />
        </div>
      )}

      <Dialog open={showArchiveDialog} onOpenChange={(open) => { if (!open) setShowArchiveDialog(false) }}>
        <DialogContent showCloseButton={false}>
          <DialogHeader>
            <DialogTitle>¿Guardar respaldo y vaciar tu portafolio?</DialogTitle>
            <DialogDescription>
              Podrás restaurarlo después.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setShowArchiveDialog(false)} disabled={archiveMutation.isPending}>
              Cancelar
            </Button>
            <Button variant="destructive" onClick={() => archiveMutation.mutate()} disabled={archiveMutation.isPending}>
              {archiveMutation.isPending ? 'Archivando...' : 'Archivar'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog open={showRestoreDialog} onOpenChange={(open) => { if (!open) setShowRestoreDialog(false) }}>
        <DialogContent showCloseButton={false}>
          <DialogHeader>
            <DialogTitle>¿Restaurar el respaldo del {archivedAtLabel}?</DialogTitle>
            <DialogDescription>
              Tu portafolio actual se perderá si tienes posiciones.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setShowRestoreDialog(false)} disabled={restoreMutation.isPending}>
              Cancelar
            </Button>
            <Button onClick={() => restoreMutation.mutate()} disabled={restoreMutation.isPending}>
              {restoreMutation.isPending ? 'Restaurando...' : 'Restaurar'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}
