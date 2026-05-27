import { createPathBasedClient } from 'openapi-fetch'
import type { components, paths } from '@fibradis/shared-api-client'
import { assertOpsAccessToken, getOpsApiErrorMessage, getOpsAuthHeaders } from '@/api/opsAuth'

const apiClient = createPathBasedClient<paths>({ baseUrl: '' })

export type ImportFundamentalsRequest = components['schemas']['ImportFundamentalsRequest']
export type FundamentalPreviewDto = components['schemas']['FundamentalPreviewDto']
export type FundamentalRecordDto = components['schemas']['FundamentalRecordDto']
export type KpiExtractionDto = components['schemas']['KpiExtractionDto']

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

export async function uploadFundamentalPdf(id: string, file: File): Promise<{ path: string; markdownExtracted: boolean }> {
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

  return response.json() as Promise<{ path: string; markdownExtracted: boolean }>
}

export async function extractKpisFromPdf(file: File): Promise<KpiExtractionDto> {
  assertOpsAccessToken()

  const formData = new FormData()
  formData.append('file', file)

  const response = await fetch('/api/v1/ops/fundamentals/extract-kpis', {
    method: 'POST',
    headers: getOpsAuthHeaders(),
    body: formData,
  })

  if (!response.ok) {
    const text = await response.text()
    let detail = `${response.status} ${text}`
    try {
      const json = JSON.parse(text) as { detail?: string; title?: string; errors?: Record<string, string[]> }
      const firstError = json.detail ?? json.title ?? Object.values(json.errors ?? {})[0]?.[0]
      if (firstError) detail = firstError
    } catch {
      // Non-JSON response.
    }

    throw new Error(`Error al extraer KPIs: ${detail}`)
  }

  return response.json() as Promise<KpiExtractionDto>
}

