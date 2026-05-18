// Genera HTML estático para rutas públicas conocidas.
// Requiere: dist/ (del `vite build`) y dist-server/ (del `vite build --ssr`)
import { readFileSync, writeFileSync, mkdirSync } from 'fs'
import { resolve, dirname } from 'path'
import { fileURLToPath, pathToFileURL } from 'url'
import { extractHeadElements } from './prerender-utils.mjs'

const __dirname = dirname(fileURLToPath(import.meta.url))
const projectRoot = resolve(__dirname, '..')

const { render } = await import(pathToFileURL(resolve(projectRoot, 'dist-server/entry-server.js')).href)

const template = readFileSync(resolve(projectRoot, 'dist/index.html'), 'utf-8')

// MANTENER SINCRONIZADO CON src/shared/data/catalog-seed.ts
const FIBRAS_SEED = [
  { ticker: 'FUNO11',    fullName: 'Fibra Uno',                shortName: 'Fibra Uno',   sector: 'Diversificado', market: 'BMV', currency: 'MXN', state: 'Active', siteUrl: 'https://fibra.uno',             investorUrl: 'https://fibra.uno/inversionistas',    reportsUrl: null, nameVariants: ['Fibra Uno', 'FUNO'],               createdAt: '2026-01-01T00:00:00Z' },
  { ticker: 'DANHOS13',  fullName: 'Fibra Danhos',             shortName: 'Danhos',      sector: 'Comercial',     market: 'BMV', currency: 'MXN', state: 'Active', siteUrl: 'https://fibradanhos.com.mx',    investorUrl: 'https://fibradanhos.com.mx/ri',       reportsUrl: null, nameVariants: ['Danhos', 'DANHOS'],                createdAt: '2026-01-01T00:00:00Z' },
  { ticker: 'TERRA13',   fullName: 'Fibra Terra',              shortName: 'Terra',       sector: 'Industrial',    market: 'BMV', currency: 'MXN', state: 'Active', siteUrl: 'https://fibra-terra.com',       investorUrl: null,                                  reportsUrl: null, nameVariants: ['Fibra Terra', 'TERRA'],            createdAt: '2026-01-01T00:00:00Z' },
  { ticker: 'FIBRAMQ12', fullName: 'Fibra Macquarie',          shortName: 'FibraMQ',     sector: 'Industrial',    market: 'BMV', currency: 'MXN', state: 'Active', siteUrl: 'https://fibramacquarie.com.mx', investorUrl: 'https://fibramacquarie.com.mx/ri',    reportsUrl: null, nameVariants: ['Fibra MQ', 'Macquarie', 'FIBRAMQ'], createdAt: '2026-01-01T00:00:00Z' },
  { ticker: 'FMTY14',    fullName: 'Fibra Monterrey',          shortName: 'Fibra MTY',   sector: 'Industrial',    market: 'BMV', currency: 'MXN', state: 'Active', siteUrl: 'https://fibramty.com',          investorUrl: 'https://fibramty.com/inversionistas', reportsUrl: null, nameVariants: ['Fibra Monterrey', 'FibraMTY', 'FMTY'], createdAt: '2026-01-01T00:00:00Z' },
  { ticker: 'FINN13',    fullName: 'Fibra Inn',                shortName: 'Fibra Inn',   sector: 'Hotelero',      market: 'BMV', currency: 'MXN', state: 'Active', siteUrl: 'https://fibrainn.com.mx',       investorUrl: null,                                  reportsUrl: null, nameVariants: ['Fibra Inn', 'FINN'],               createdAt: '2026-01-01T00:00:00Z' },
  { ticker: 'FIHO12',    fullName: 'Fibra Hotel',              shortName: 'Fibra Hotel', sector: 'Hotelero',      market: 'BMV', currency: 'MXN', state: 'Active', siteUrl: 'https://fibrahotel.com',        investorUrl: null,                                  reportsUrl: null, nameVariants: ['Fibra Hotel', 'FIHO'],             createdAt: '2026-01-01T00:00:00Z' },
  { ticker: 'VESTA15',   fullName: 'Fibra Vesta',              shortName: 'Vesta',       sector: 'Industrial',    market: 'BMV', currency: 'MXN', state: 'Active', siteUrl: 'https://fibravesta.com',        investorUrl: 'https://fibravesta.com/ri',           reportsUrl: null, nameVariants: ['Fibra Vesta', 'VESTA'],            createdAt: '2026-01-01T00:00:00Z' },
  { ticker: 'HCITY17',   fullName: 'Fibra Hotel City Express', shortName: 'HC',          sector: 'Hotelero',      market: 'BMV', currency: 'MXN', state: 'Active', siteUrl: 'https://hcity.com.mx',          investorUrl: null,                                  reportsUrl: null, nameVariants: ['Hotel City Express', 'HCITY', 'HC'], createdAt: '2026-01-01T00:00:00Z' },
  { ticker: 'PLUS18',    fullName: 'Fibra Plus',               shortName: 'Fibra Plus',  sector: 'Diversificado', market: 'BMV', currency: 'MXN', state: 'Active', siteUrl: 'https://fibraplus.mx',          investorUrl: null,                                  reportsUrl: null, nameVariants: ['Fibra Plus', 'PLUS'],              createdAt: '2026-01-01T00:00:00Z' },
]


const routesToRender = [
  { url: '/', initialData: {} },
  ...FIBRAS_SEED.map(f => ({
    url: `/fibras/${f.ticker}`,
    initialData: {
      [JSON.stringify(['fibra', f.ticker])]: { id: `seed-${f.ticker.toLowerCase()}`, ...f },
    },
  })),
]

console.log(`\nPrerenderizando ${routesToRender.length} rutas...\n`)

for (const { url, initialData } of routesToRender) {
  try {
    const { html: rendered, dehydratedState } = await render(url, initialData)

    // 1. Extrae meta elements del rendered ANTES de inyectarlo en el template
    const { headElements, cleanBody } = extractHeadElements(rendered)

    // 2. Inyecta el body limpio en el template (función evita interpretación de '$')
    let html = template.replace(
      '<div id="root"></div>',
      () => `<div id="root">${cleanBody}</div>`
    )

    // 3. Reemplaza el placeholder y elimina el título base cuando hay uno específico
    if (headElements.length > 0) {
      const hasTitle = headElements.some(el => /^<title/i.test(el))
      if (hasTitle) {
        // Elimina el título fallback del template para dejar solo el título de la página
        html = html.replace('<title>FIBRADIS</title>', '')
      }
      html = html.replace('<!-- prerender-meta -->', () => headElements.join('\n    '))
    }

    // 4. Inyecta estado de React Query para evitar hydration mismatch en fichas de FIBRA
    if (Object.keys(initialData).length > 0 && dehydratedState) {
      const stateScript = `<script>window.__QUERY_INITIAL_DATA__=${JSON.stringify(dehydratedState)}</script>`
      html = html.replace('</body>', () => `${stateScript}\n  </body>`)
    }

    const outputPath = url === '/'
      ? resolve(projectRoot, 'dist/index.html')
      : resolve(projectRoot, `dist${url}/index.html`)

    mkdirSync(dirname(outputPath), { recursive: true })
    writeFileSync(outputPath, html)
    console.log(`  ✓ ${url}`)
  } catch (err) {
    console.error(`  ✗ ${url}:`, err.message)
    process.exit(1)
  }
}

console.log('\n✓ Prerender completado.\n')
