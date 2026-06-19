export interface FiscalRates {
  isrRetentionRate: number
  ivaRate: number
}

const FALLBACK: FiscalRates = { isrRetentionRate: 0.30, ivaRate: 0.16 }

export async function fetchFiscalRates(): Promise<FiscalRates> {
  try {
    const res = await fetch('/api/v1/config/fiscal-rates')
    if (!res.ok) return FALLBACK
    const data = await res.json() as FiscalRates
    if (
      typeof data?.isrRetentionRate !== 'number' || !Number.isFinite(data.isrRetentionRate) || data.isrRetentionRate <= 0 ||
      typeof data?.ivaRate !== 'number' || !Number.isFinite(data.ivaRate) || data.ivaRate <= 0
    ) return FALLBACK
    return data
  } catch {
    return FALLBACK
  }
}
