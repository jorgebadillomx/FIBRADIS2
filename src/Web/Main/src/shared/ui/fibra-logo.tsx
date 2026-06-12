import { useMemo, useState } from 'react'

const TICKER_PALETTES = [
  'bg-teal-100 text-teal-700 border-teal-200',
  'bg-blue-100 text-blue-700 border-blue-200',
  'bg-violet-100 text-violet-700 border-violet-200',
  'bg-amber-100 text-amber-700 border-amber-200',
  'bg-rose-100 text-rose-700 border-rose-200',
  'bg-emerald-100 text-emerald-700 border-emerald-200',
  'bg-indigo-100 text-indigo-700 border-indigo-200',
  'bg-orange-100 text-orange-700 border-orange-200',
  'bg-cyan-100 text-cyan-700 border-cyan-200',
  'bg-pink-100 text-pink-700 border-pink-200',
]

function tickerPalette(ticker: string): string {
  const hash = ticker.split('').reduce((acc, c) => acc + c.charCodeAt(0), 0)
  return TICKER_PALETTES[hash % TICKER_PALETTES.length]
}

function getLogoUrl(siteUrl: string | null): string | null {
  if (!siteUrl) return null
  try {
    const url = siteUrl.startsWith('http') ? siteUrl : `https://${siteUrl}`
    return `https://t2.gstatic.com/faviconV2?client=SOCIAL&type=FAVICON&fallback_opts=TYPE,SIZE,URL&url=${encodeURIComponent(url)}&size=128`
  } catch {
    return null
  }
}

interface FibraLogoProps {
  siteUrl: string | null
  ticker: string
  size?: 'sm' | 'md'
}

export function FibraLogo({ siteUrl, ticker, size = 'sm' }: FibraLogoProps) {
  const [failed, setFailed] = useState(false)
  const logoUrl = useMemo(() => getLogoUrl(siteUrl), [siteUrl])
  const palette = tickerPalette(ticker)

  const sizeClass = size === 'md'
    ? 'h-12 w-12 rounded-xl text-xs'
    : 'h-10 w-10 rounded-lg text-[10px]'

  const imgSize = size === 'md' ? 48 : 40

  if (logoUrl && !failed) {
    return (
      <img
        alt={ticker}
        className={`shrink-0 border border-border bg-white object-contain p-1 ${sizeClass}`}
        height={imgSize}
        onError={() => setFailed(true)}
        src={logoUrl}
        width={imgSize}
      />
    )
  }

  return (
    <div className={`flex shrink-0 items-center justify-center border font-bold ${palette} ${sizeClass}`}>
      <span className="leading-none">{ticker.slice(0, 5)}</span>
    </div>
  )
}
