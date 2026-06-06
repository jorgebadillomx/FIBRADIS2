import { useQuery } from '@tanstack/react-query'
import { useAuth } from './AuthContext'
import { fetchProfile } from './authApi'

export const PROFILE_QUERY_KEY = ['account', 'me'] as const

export function useProfile() {
  const { isAuthenticated } = useAuth()

  return useQuery({
    queryKey: PROFILE_QUERY_KEY,
    queryFn: fetchProfile,
    enabled: isAuthenticated,
    staleTime: 5 * 60 * 1000,
  })
}
