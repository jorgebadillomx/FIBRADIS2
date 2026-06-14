import { assertOpsAccessToken, getOpsAuthHeaders } from '@/api/opsAuth'

export interface OrganizationSameAsDto {
  updatedAt: string
  updatedBy: string | null
  sameAs: string[]
}

export interface UpdateOrganizationSameAsRequest {
  sameAs: string[]
}

async function parseProblemDetails(response: Response): Promise<string> {
  try {
    const data = await response.json()
    return data?.detail || data?.title || `Error HTTP ${response.status}`
  } catch {
    return `Error HTTP ${response.status}`
  }
}

export async function fetchSeoOrganization(): Promise<OrganizationSameAsDto> {
  assertOpsAccessToken()
  const response = await fetch('/api/v1/ops/seo/organization', {
    headers: getOpsAuthHeaders(),
  })
  if (!response.ok) throw new Error(await parseProblemDetails(response))
  return (await response.json()) as OrganizationSameAsDto
}

export async function updateSeoOrganization(payload: UpdateOrganizationSameAsRequest): Promise<OrganizationSameAsDto> {
  assertOpsAccessToken()
  const response = await fetch('/api/v1/ops/seo/organization', {
    method: 'PUT',
    headers: {
      ...getOpsAuthHeaders(),
      'content-type': 'application/json',
    },
    body: JSON.stringify(payload),
  })
  if (!response.ok) throw new Error(await parseProblemDetails(response))
  return (await response.json()) as OrganizationSameAsDto
}
