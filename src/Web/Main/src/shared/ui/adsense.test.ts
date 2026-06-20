import assert from 'node:assert/strict'
import test from 'node:test'
import {
  AD_SENSE_SCRIPT_URL,
  isAdSenseEnabled,
  shouldLoadAdSense,
  syncAdSenseScript,
  type AdSenseDocumentLike,
} from './adsense.ts'

function createDocumentStub() {
  const appendedScripts: Array<{
    async: boolean
    crossOrigin: string
    src: string
    removed: boolean
    remove: () => void
  }> = []

  const doc: AdSenseDocumentLike = {
    querySelector(selector: string) {
      if (selector !== `script[src="${AD_SENSE_SCRIPT_URL}"]`) return null
      return (appendedScripts[0] ?? null) as unknown as Element | null
    },
    createElement() {
      const script = {
        async: false,
        crossOrigin: '',
        src: '',
        removed: false,
        remove() {
          this.removed = true
          appendedScripts.length = 0
        },
      }

      return script as unknown as HTMLScriptElement
    },
    head: {
      appendChild(node) {
        appendedScripts.push(node as unknown as {
          async: boolean
          crossOrigin: string
          src: string
          removed: boolean
          remove: () => void
        })
      },
    },
  }

  return { doc, appendedScripts }
}

test('isAdSenseEnabled returns false only for authenticated users', () => {
  assert.equal(isAdSenseEnabled('checking'), true)
  assert.equal(isAdSenseEnabled('anonymous'), true)
  assert.equal(isAdSenseEnabled('authenticated'), false)
})

test('shouldLoadAdSense skips the bootstrap check when a session cookie is already present', () => {
  assert.equal(shouldLoadAdSense('checking', false), true)
  assert.equal(shouldLoadAdSense('checking', true), false)
  assert.equal(shouldLoadAdSense('anonymous', false), true)
  assert.equal(shouldLoadAdSense('anonymous', true), true)
  assert.equal(shouldLoadAdSense('authenticated', false), false)
})

test('syncAdSenseScript injects the AdSense script once for anonymous users', () => {
  const { doc, appendedScripts } = createDocumentStub()

  syncAdSenseScript('anonymous', doc)
  syncAdSenseScript('anonymous', doc)

  assert.equal(appendedScripts.length, 1)
  assert.equal(appendedScripts[0]?.src, AD_SENSE_SCRIPT_URL)
  assert.equal(appendedScripts[0]?.async, true)
  assert.equal(appendedScripts[0]?.crossOrigin, 'anonymous')
  assert.equal(appendedScripts[0]?.removed, false)
})

test('syncAdSenseScript removes the AdSense script for authenticated users', () => {
  const { doc, appendedScripts } = createDocumentStub()

  syncAdSenseScript('anonymous', doc)
  assert.equal(appendedScripts.length, 1)

  syncAdSenseScript('authenticated', doc)

  assert.equal(appendedScripts.length, 0)
  assert.equal(appendedScripts[0], undefined)
})
