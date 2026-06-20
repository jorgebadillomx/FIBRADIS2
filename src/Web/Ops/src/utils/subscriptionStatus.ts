import type { UserSummaryDto } from '../api/usersApi'

export type SubscriptionStatusColor = 'green' | 'amber' | 'gray'

export type SubscriptionStatus = {
  label: string
  color: SubscriptionStatusColor
}

function formatUtcDate(value: string): string {
  const date = new Date(value)
  if (isNaN(date.getTime())) return '—'
  const day = String(date.getUTCDate()).padStart(2, '0')
  const month = String(date.getUTCMonth() + 1).padStart(2, '0')
  const year = String(date.getUTCFullYear())
  return `${day}/${month}/${year}`
}

export function toDateInput(value: string): string {
  const date = new Date(value)
  if (isNaN(date.getTime())) return ''
  const year = String(date.getUTCFullYear())
  const month = String(date.getUTCMonth() + 1).padStart(2, '0')
  const day = String(date.getUTCDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}

export function getTodayDateInput(): string {
  const today = new Date()
  const year = String(today.getUTCFullYear())
  const month = String(today.getUTCMonth() + 1).padStart(2, '0')
  const day = String(today.getUTCDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}

export function formatSubscriptionDate(value: string): string {
  return formatUtcDate(value)
}

export function getSubscriptionStatus(user: UserSummaryDto): SubscriptionStatus {
  const now = Date.now()

  if (user.subscriptionType === 'Lifetime') {
    return { label: 'Lifetime', color: 'green' }
  }

  if (
    (user.subscriptionType === 'Monthly' || user.subscriptionType === 'Annual') &&
    user.subscriptionEndsAt != null &&
    new Date(user.subscriptionEndsAt).getTime() > now
  ) {
    return {
      label: `${user.subscriptionType} · vence ${formatSubscriptionDate(user.subscriptionEndsAt)}`,
      color: 'green',
    }
  }

  if (user.subscriptionType == null && user.trialEndsAt != null && new Date(user.trialEndsAt).getTime() > now) {
    return {
      label: `Trial · vence ${formatSubscriptionDate(user.trialEndsAt)}`,
      color: 'amber',
    }
  }

  return { label: 'Sin acceso', color: 'gray' }
}
