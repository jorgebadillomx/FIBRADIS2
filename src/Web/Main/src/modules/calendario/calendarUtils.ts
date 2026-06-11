export function formatDateOnly(date: Date): string {
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}

export function endOfMonth(date: Date): Date {
  return new Date(date.getFullYear(), date.getMonth() + 1, 0)
}

export function calcMonthRange(year: number, month: number): { from: string; to: string } {
  const start = new Date(year, month - 1, 1)
  return { from: formatDateOnly(start), to: formatDateOnly(endOfMonth(start)) }
}
