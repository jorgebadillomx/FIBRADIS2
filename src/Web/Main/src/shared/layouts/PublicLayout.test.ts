import assert from 'node:assert/strict'
import test from 'node:test'
import {
  MAIN_INVESTMENT_LINKS,
  MAIN_MORE_LINKS,
  MAIN_PRIMARY_LINKS,
  buildMainMobileSections,
  shouldCloseMenuOnEscape,
} from './public-navigation.ts'

test('Main navigation keeps the four primary links and three secondary links', () => {
  assert.deepEqual(MAIN_PRIMARY_LINKS.map((item) => item.label), ['Fibras', 'Comparar', 'Noticias', 'Fundamentales'])
  assert.deepEqual(MAIN_MORE_LINKS.map((item) => item.label), ['Conoce las FIBRAs', 'Calendario', 'Calculadora'])
})

test('buildMainMobileSections hides Mi inversión for anonymous users and exposes it for authenticated users', () => {
  const anonymousSections = buildMainMobileSections('anonymous')
  const authenticatedSections = buildMainMobileSections('authenticated')

  assert.deepEqual(anonymousSections.map((section) => section.title), ['Navegar', 'Más', 'Cuenta'])
  assert.deepEqual(authenticatedSections.map((section) => section.title), ['Navegar', 'Más', 'Mi inversión', 'Cuenta'])

  assert.equal(
    anonymousSections.some((section) => section.items.some((item) => 'to' in item && MAIN_INVESTMENT_LINKS.some((link) => link.to === item.to))),
    false,
  )
  assert.equal(
    authenticatedSections.some((section) => section.items.some((item) => 'to' in item && MAIN_INVESTMENT_LINKS.some((link) => link.to === item.to))),
    true,
  )
})

test('authenticated mobile menu keeps the account actions and anonymous menu keeps the login link', () => {
  const anonymousSections = buildMainMobileSections('anonymous')
  const authenticatedSections = buildMainMobileSections('authenticated')

  const anonymousAccountSection = anonymousSections.find((section) => section.title === 'Cuenta')
  const authenticatedAccountSection = authenticatedSections.find((section) => section.title === 'Cuenta')

  assert.ok(anonymousAccountSection)
  assert.ok(authenticatedAccountSection)
  assert.deepEqual(anonymousAccountSection?.items.map((item) => item.label), ['Iniciar sesión'])
  assert.deepEqual(authenticatedAccountSection?.items.map((item) => item.label), ['Mi perfil', 'Cerrar sesión'])
})

test('shouldCloseMenuOnEscape only closes the dropdown on Escape', () => {
  assert.equal(shouldCloseMenuOnEscape('Escape'), true)
  assert.equal(shouldCloseMenuOnEscape('Enter'), false)
  assert.equal(shouldCloseMenuOnEscape('Tab'), false)
})
