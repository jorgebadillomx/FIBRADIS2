import assert from 'node:assert/strict'
import test from 'node:test'

// Minimal sessionStorage mock for Node environment
function makeStoreMock() {
  const store = new Map<string, string>()
  return {
    getItem: (key: string) => store.get(key) ?? null,
    setItem: (key: string, value: string) => { store.set(key, value) },
    removeItem: (key: string) => { store.delete(key) },
    clear: () => { store.clear() },
  }
}

const sessionStorageMock = makeStoreMock()
const dispatchedEvents: string[] = []

// @ts-expect-error – Node has no window; we install a minimal shim for the module under test
globalThis.window = {
  sessionStorage: sessionStorageMock,
  dispatchEvent: (event: Event) => { dispatchedEvents.push(event.type) },
}

// Import AFTER setting up globalThis.window so the module sees the shim
const { getStoredMainAccessToken, storeMainAccessToken, clearMainAccessToken, getMainAuthHeaders, notifyMainAuthRequired, MAIN_AUTH_REQUIRED_EVENT } =
  await import('./mainAuth.ts')

test('getStoredMainAccessToken returns null when nothing is stored', () => {
  sessionStorageMock.clear()
  assert.equal(getStoredMainAccessToken(), null)
})

test('storeMainAccessToken persists token; getStoredMainAccessToken retrieves it', () => {
  sessionStorageMock.clear()
  storeMainAccessToken('test-token-abc')
  assert.equal(getStoredMainAccessToken(), 'test-token-abc')
})

test('clearMainAccessToken removes stored token', () => {
  sessionStorageMock.clear()
  storeMainAccessToken('to-be-cleared')
  clearMainAccessToken()
  assert.equal(getStoredMainAccessToken(), null)
})

test('getMainAuthHeaders returns Authorization header when token is present', () => {
  sessionStorageMock.clear()
  storeMainAccessToken('my-jwt')
  const headers = getMainAuthHeaders()
  assert.equal((headers as { Authorization: string }).Authorization, 'Bearer my-jwt')
})

test('getMainAuthHeaders returns empty object when no token', () => {
  sessionStorageMock.clear()
  const headers = getMainAuthHeaders()
  assert.deepEqual(headers, {})
})

test('notifyMainAuthRequired dispatches the correct event', () => {
  dispatchedEvents.length = 0
  notifyMainAuthRequired()
  assert.equal(dispatchedEvents.length, 1)
  assert.equal(dispatchedEvents[0], MAIN_AUTH_REQUIRED_EVENT)
})
