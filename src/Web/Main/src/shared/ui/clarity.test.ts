import assert from 'node:assert/strict'
import test from 'node:test'
import {
  CLARITY_SCRIPT_URL,
  injectClarityScript,
  type ClarityDocumentLike,
} from './clarity.ts'

function createDocumentStub() {
  const appendedScripts: Array<{ async: boolean; src: string }> = []

  const doc: ClarityDocumentLike = {
    querySelector(selector: string) {
      if (selector !== `script[src="${CLARITY_SCRIPT_URL}"]`) return null
      return (appendedScripts[0] ?? null) as unknown as Element | null
    },
    createElement() {
      return { async: false, src: '' } as unknown as HTMLScriptElement
    },
    head: {
      appendChild(node) {
        appendedScripts.push(node as unknown as { async: boolean; src: string })
      },
    },
  }

  return { doc, appendedScripts }
}

test('injectClarityScript inyecta el script exactamente una vez (idempotente)', () => {
  const { doc, appendedScripts } = createDocumentStub()

  injectClarityScript(doc)
  injectClarityScript(doc)

  assert.equal(appendedScripts.length, 1)
})

test('injectClarityScript configura async=true y src correcto', () => {
  const { doc, appendedScripts } = createDocumentStub()

  injectClarityScript(doc)

  assert.equal(appendedScripts[0]?.src, CLARITY_SCRIPT_URL)
  assert.equal(appendedScripts[0]?.async, true)
})
