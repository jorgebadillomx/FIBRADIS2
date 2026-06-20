export type SubscriptionState =
  | { kind: 'trial'; trialEndsAt: string; daysRemaining: number }
  | { kind: 'active'; subscriptionType: 'Monthly' | 'Annual'; subscriptionEndsAt: string }
  | { kind: 'lifetime' }
  | { kind: 'expired'; hadTrial: boolean }

export function resolveSubscriptionState(
  isActive: boolean,
  subscriptionType: string | null,
  trialEndsAt: string | null,
  subscriptionEndsAt: string | null,
): SubscriptionState {
  if (isActive && subscriptionType === 'Lifetime') {
    return { kind: 'lifetime' }
  }
  if (isActive && subscriptionType) {
    return {
      kind: 'active',
      subscriptionType: subscriptionType as 'Monthly' | 'Annual',
      subscriptionEndsAt: subscriptionEndsAt ?? '',
    }
  }
  if (isActive && !subscriptionType && trialEndsAt) {
    const daysRemaining = Math.ceil(
      (new Date(trialEndsAt).getTime() - Date.now()) / 86400000,
    )
    return { kind: 'trial', trialEndsAt, daysRemaining }
  }
  return { kind: 'expired', hadTrial: trialEndsAt !== null }
}
