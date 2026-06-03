import { Navigate } from 'react-router'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import type { components } from '@fibradis/shared-api-client'
import { apiClient } from '@/api/fibrasApi'
import { KpiCards } from '@/modules/portafolio/KpiCards'
import { ColumnPicker } from '@/modules/portafolio/ColumnPicker'
import { PositionsTable } from '@/modules/portafolio/PositionsTable'
import { UploadZone } from '@/modules/portafolio/UploadZone'
import { Button } from '@/shared/ui/button'

type PortfolioResponseDto = components['schemas']['PortfolioResponseDto']
type PortfolioColumnConfigDto = components['schemas']['PortfolioColumnConfigDto']

const ACCESS_TOKEN_KEYS = [
  'fibradis.accessToken',
  'accessToken',
  'fibradis.user.accessToken',
] as const

function getStoredAccessToken(): string | null {
  if (typeof window === 'undefined') return null

  for (const key of ACCESS_TOKEN_KEYS) {
    const token = window.localStorage.getItem(key)
    if (token) return token
  }

  return null
}

export function PortafolioPage() {
  const accessToken = getStoredAccessToken()
  const queryClient = useQueryClient()

  const portfolioQuery = useQuery<PortfolioResponseDto>({
    queryKey: ['portfolio', 'positions'],
    enabled: Boolean(accessToken),
    queryFn: async () => {
      const { data, error } = await apiClient.GET('/api/v1/portfolio', {})
      if (error || !data) throw new Error('No se pudo cargar el portafolio.')
      return data
    },
  })

  const columnConfigQuery = useQuery<PortfolioColumnConfigDto>({
    queryKey: ['portfolio', 'column-config'],
    enabled: Boolean(accessToken),
    queryFn: async () => {
      const { data, error } = await apiClient.GET('/api/v1/portfolio/column-config', {})
      if (error || !data) throw new Error('No se pudo cargar la configuración de columnas.')
      return data
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
      void queryClient.invalidateQueries({ queryKey: ['portfolio', 'positions'] })
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
      void queryClient.invalidateQueries({ queryKey: ['portfolio', 'positions'] })
    },
  })

  if (!accessToken) {
    return <Navigate to="/login" replace />
  }

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
          <div className="flex items-center gap-3">
            <ColumnPicker
              enabledColumns={enabledColumns}
              onEnabledColumnsChange={(columns) => {
                queryClient.setQueryData<PortfolioColumnConfigDto>(['portfolio', 'column-config'], {
                  columns,
                })
              }}
            />
          </div>
        )}
      </div>

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
    </div>
  )
}
