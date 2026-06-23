export function EventChip({
  eventType,
  compact = false,
  estimated = false,
}: {
  eventType: string
  compact?: boolean
  estimated?: boolean
}) {
  const isPayment = eventType === 'Pago'
  const className = isPayment
    ? 'border-green-200 bg-green-100 text-green-800'
    : 'border-blue-200 bg-blue-100 text-blue-800'

  if (compact) {
    return (
      <span
        className={[
          'inline-flex shrink-0 h-1.5 w-1.5 rounded-full',
          isPayment ? 'bg-green-600' : 'bg-blue-600',
          estimated ? 'ring-1 ring-amber-400 ring-offset-1' : '',
        ].join(' ')}
      />
    )
  }

  return (
    <span
      className={`inline-flex shrink-0 items-center rounded-full border px-2 py-0.5 text-[10px] font-semibold uppercase tracking-[0.14em] ${className} ${estimated ? 'border-dashed' : ''}`}
    >
      {eventType}
    </span>
  )
}
