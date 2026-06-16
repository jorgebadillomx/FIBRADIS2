import assert from 'node:assert/strict'
import test from 'node:test'
import { DEFAULT_LOGIN_REDIRECT, resolveLoginRedirect } from './login-redirect.ts'

test('resolveLoginRedirect returns the default landing when input is empty or unsafe', () => {
  assert.equal(resolveLoginRedirect(null), DEFAULT_LOGIN_REDIRECT)
  assert.equal(resolveLoginRedirect(undefined), DEFAULT_LOGIN_REDIRECT)
  assert.equal(resolveLoginRedirect(''), DEFAULT_LOGIN_REDIRECT)
  assert.equal(resolveLoginRedirect('   '), DEFAULT_LOGIN_REDIRECT)
  assert.equal(resolveLoginRedirect('https://evil.example/login'), DEFAULT_LOGIN_REDIRECT)
  assert.equal(resolveLoginRedirect('//evil.example'), DEFAULT_LOGIN_REDIRECT)
})

test('resolveLoginRedirect rejects backslash protocol-relative bypasses', () => {
  assert.equal(resolveLoginRedirect('/\\evil.example'), DEFAULT_LOGIN_REDIRECT)
  assert.equal(resolveLoginRedirect('/\\/evil.example'), DEFAULT_LOGIN_REDIRECT)
  assert.equal(resolveLoginRedirect('\\\\evil.example'), DEFAULT_LOGIN_REDIRECT)
})

test('resolveLoginRedirect rejects paths containing control characters', () => {
  assert.equal(resolveLoginRedirect('/portafolio\n//evil.example'), DEFAULT_LOGIN_REDIRECT)
  assert.equal(resolveLoginRedirect('/\t/evil.example'), DEFAULT_LOGIN_REDIRECT)
})

test('resolveLoginRedirect allows internal absolute paths', () => {
  assert.equal(resolveLoginRedirect('/portafolio'), '/portafolio')
  assert.equal(resolveLoginRedirect('/reportes?tab=trimestral'), '/reportes?tab=trimestral')
  assert.equal(resolveLoginRedirect('/portafolio#login'), '/portafolio#login')
})
