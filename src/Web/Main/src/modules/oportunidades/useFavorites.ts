import { useMemo } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '@/api/fibrasApi'
import { useAuth } from '@/modules/auth/AuthContext'

const favoritesQueryKey = ['favorites'] as const

export function useFavorites() {
  const { isAuthenticated } = useAuth()
  const queryClient = useQueryClient()

  const favoritesQuery = useQuery({
    queryKey: favoritesQueryKey,
    queryFn: async () => {
      const { data, error } = await apiClient.GET('/api/v1/portfolio/favorites', {})
      if (error || !data) throw new Error('No se pudieron cargar los favoritos.')
      return (data.fibraIds ?? []) as string[]
    },
    enabled: isAuthenticated,
    staleTime: 60_000,
  })

  const toggleMutation = useMutation({
    mutationFn: async ({ fibraId, removing }: { fibraId: string; removing: boolean }) => {
      if (removing) {
        const { error } = await apiClient.DELETE('/api/v1/portfolio/favorites/{fibraId}', {
          params: { path: { fibraId } },
        })
        if (error) throw error
        return
      }

      const { error } = await apiClient.PUT('/api/v1/portfolio/favorites/{fibraId}', {
        params: { path: { fibraId } },
      })
      if (error) throw error
    },
    onMutate: async ({ fibraId, removing }) => {
      await queryClient.cancelQueries({ queryKey: favoritesQueryKey })
      const previous = queryClient.getQueryData<string[]>(favoritesQueryKey) ?? []
      const next = removing
        ? previous.filter((id) => id !== fibraId)
        : previous.includes(fibraId)
          ? previous
          : [...previous, fibraId]
      queryClient.setQueryData(favoritesQueryKey, next)
      return { previous }
    },
    onError: (_error, _variables, context) => {
      if (context?.previous != null) {
        queryClient.setQueryData(favoritesQueryKey, context.previous)
      }
    },
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: favoritesQueryKey })
    },
  })

  const favoriteIds = useMemo(() => new Set(favoritesQuery.data ?? []), [favoritesQuery.data])

  const toggle = (fibraId: string) => {
    if (!isAuthenticated) return

    const current = queryClient.getQueryData<string[]>(favoritesQueryKey) ?? []
    toggleMutation.mutate({ fibraId, removing: current.includes(fibraId) })
  }

  return {
    favoriteIds,
    isLoading: favoritesQuery.isLoading,
    toggle,
    isAuthenticated,
  }
}
