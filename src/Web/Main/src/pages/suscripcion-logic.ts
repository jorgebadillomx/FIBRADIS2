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
  // P4: Lifetime es permanente — no depende de isActive
  if (subscriptionType === 'Lifetime') {
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
    // P2: daysRemaining nunca negativo (lag del job / grace period del servidor)
    const daysRemaining = Math.max(0, Math.ceil(
      (new Date(trialEndsAt).getTime() - Date.now()) / 86400000,
    ))
    return { kind: 'trial', trialEndsAt, daysRemaining }
  }
  // P3: usuario activo sin datos de suscripción (modo degradado — fetchProfile falló)
  if (isActive) {
    return { kind: 'trial', trialEndsAt: trialEndsAt ?? '', daysRemaining: 0 }
  }
  return { kind: 'expired', hadTrial: trialEndsAt !== null }
}
