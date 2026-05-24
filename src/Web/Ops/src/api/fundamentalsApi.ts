import { createPathBasedClient } from 'openapi-fetch'
import type { components, paths } from '@fibradis/shared-api-client'
import { assertOpsAccessToken, getOpsApiErrorMessage, getOpsAuthHeaders } from '@/api/opsAuth'

const apiClient = createPathBasedClient<paths>({ baseUrl: '' })

export type ImportFundamentalsRequest = components['schemas']['ImportFundamentalsRequest']
export type FundamentalPreviewDto = components['schemas']['FundamentalPreviewDto']
export type FundamentalRecordDto = components['schemas']['FundamentalRecordDto']

export async function importFundamentals(payload: ImportFundamentalsRequest): Promise<FundamentalPreviewDto> {
  assertOpsAccessToken()

  const { data, error } = await apiClient['/api/v1/ops/fundamentals/import'].POST({
    headers: getOpsAuthHeaders(),
    body: payload,
  })

  if (error) throw new Error(getOpsApiErrorMessage(error, `Error al importar fundamentales: ${JSON.stringify(error)}`))
  if (!data) throw new Error('La API no devolvió datos.')
  return data
}

export async function confirmFundamentals(id: string): Promise<FundamentalRecordDto> {
  assertOpsAccessToken()

  const { data, error } = await apiClient['/api/v1/ops/fundamentals/{id}/confirm'].POST({
    headers: getOpsAuthHeaders(),
    params: { path: { id } },
  })

  if (error) throw new Error(getOpsApiErrorMessage(error, `Error al confirmar fundamentales: ${JSON.stringify(error)}`))
  if (!data) throw new Error('La API no devolvió datos.')
  return data
}

export async function fetchFundamentalsByFibra(fibraId: string): Promise<FundamentalRecordDto[]> {
  assertOpsAccessToken()

  const { data, error } = await apiClient['/api/v1/ops/fundamentals'].GET({
    headers: getOpsAuthHeaders(),
    params: { query: { fibraId } },
  })

  if (error) throw new Error(getOpsApiErrorMessage(error, `Error al obtener historial: ${JSON.stringify(error)}`))
  return data ?? []
}

export async function downloadFundamentalPdf(id: string): Promise<Blob> {
  assertOpsAccessToken()

  const headers = getOpsAuthHeaders()
  const response = await fetch(`/api/v1/ops/fundamentals/${id}/pdf`, {
    method: 'GET',
    headers,
  })

  if (!response.ok) throw new Error(`Error al descargar PDF: ${response.status}`)
  return response.blob()
}

export async function uploadFundamentalPdf(id: string, file: File): Promise<{ path: string }> {
  assertOpsAccessToken()

  const formData = new FormData()
  formData.append('file', file)

  const headers = getOpsAuthHeaders()
  const response = await fetch(`/api/v1/ops/fundamentals/${id}/pdf`, {
    method: 'POST',
    headers,
    body: formData,
  })

  if (!response.ok) {
    const text = await response.text()
    throw new Error(`Error al subir PDF: ${response.status} ${text}`)
  }

  return response.json() as Promise<{ path: string }>
}
