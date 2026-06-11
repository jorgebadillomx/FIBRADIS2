// LEGACY (decisión code review 11-2, 2026-06-11): el mecanismo canónico de metadata SEO
// en producción es SpaMetadataMiddleware (server-side, src/Server/Api/Middleware/).
// El deploy usa `npm run build` — NO usar `build:full`: este script consume el comentario
// <!-- prerender-meta --> del que depende el middleware y generaría HTML por ruta que
// el middleware cortocircuitaría. Mantener solo como referencia hasta retirarlo.
//
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
  { ticker: 'EDUCA18',   fullName: 'Fibra Educa',              shortName: 'Fibra Educa', sector: 'Educativo',     market: 'BMV', currency: 'MXN', state: 'Active', siteUrl: 'https://www.fibraeduca.com',    investorUrl: 'https://www.fibraeduca.com/invertir', reportsUrl: 'https://www.fibraeduca.com/reportes-financieros', nameVariants: ['Fibra Educa', 'EDUCA', 'EDUCA18'], createdAt: '2026-01-01T00:00:00Z' },
  { ticker: 'FIBRAPL14', fullName: 'Fibra Prologis',           shortName: 'Prologis',    sector: 'Industrial',    market: 'BMV', currency: 'MXN', state: 'Active', siteUrl: 'https://www.fibraprologis.com/en-US', investorUrl: 'https://www.fibraprologis.com/en-US/investors', reportsUrl: 'https://www.fibraprologis.com/en-US/investors/financial-results', nameVariants: ['Fibra Prologis', 'Prologis', 'FIBRAPL'], createdAt: '2026-01-01T00:00:00Z' },
  { ticker: 'FIBRAUP18', fullName: 'Fibra Upsite',             shortName: 'Upsite',      sector: 'Industrial',    market: 'BMV', currency: 'MXN', state: 'Active', siteUrl: 'https://fibra-upsite.com',      investorUrl: null,                                  reportsUrl: null, nameVariants: ['Fibra Upsite', 'Upsite', 'FIBRAUP'], createdAt: '2026-01-01T00:00:00Z' },
  { ticker: 'FNOVA17',   fullName: 'Fibra Nova',               shortName: 'Fibra Nova',  sector: 'Diversificado', market: 'BIVA', currency: 'MXN', state: 'Active', siteUrl: 'https://www.fibra-nova.com',   investorUrl: 'https://www.fibra-nova.com/inversionistas/como-invertir', reportsUrl: 'https://www.fibra-nova.com/inversionistas/reportes-trimestrales', nameVariants: ['Fibra Nova', 'FNOVA', 'FNOVA17'], createdAt: '2026-01-01T00:00:00Z' },
  { ticker: 'FPLUS16',   fullName: 'Fibra Plus',               shortName: 'Fibra Plus',  sector: 'Diversificado', market: 'BMV', currency: 'MXN', state: 'Active', siteUrl: 'https://www.fibraplus.mx',      investorUrl: null,                                  reportsUrl: 'https://www.fibraplus.mx/es/financiera/trimestrales', nameVariants: ['Fibra Plus', 'FPLUS', 'FPLUS16'], createdAt: '2026-01-01T00:00:00Z' },
  { ticker: 'FSHOP13',   fullName: 'Fibra Shop',               shortName: 'Fibra Shop',  sector: 'Comercial',     market: 'BMV', currency: 'MXN', state: 'Active', siteUrl: 'https://fibrashop.mx',          investorUrl: 'https://fibrashop.mx/contacto/',      reportsUrl: 'https://fibrashop.mx/informes-financieros/', nameVariants: ['Fibra Shop', 'FSHOP', 'FSHOP13'], createdAt: '2026-01-01T00:00:00Z' },
  { ticker: 'NEXT25',    fullName: 'Fibra Next',               shortName: 'Fibra Next',  sector: 'Industrial',    market: 'BMV', currency: 'MXN', state: 'Active', siteUrl: 'https://fibranext.mx',          investorUrl: 'https://fibranext.mx/investors',      reportsUrl: 'https://fibranext.mx/investors', nameVariants: ['Fibra Next', 'NEXT', 'NEXT25'], createdAt: '2026-01-01T00:00:00Z' },
  { ticker: 'SOMA21',    fullName: 'Fibra SOMA',               shortName: 'Fibra SOMA',  sector: 'Comercial',     market: 'BIVA', currency: 'MXN', state: 'Active', siteUrl: 'https://fibrasoma.group',       investorUrl: null,                                  reportsUrl: null, nameVariants: ['Fibra SOMA', 'SOMA', 'SOMA21'], createdAt: '2026-01-01T00:00:00Z' },
  { ticker: 'STORAGE18', fullName: 'Fibra Storage',            shortName: 'Fibra Storage', sector: 'Autoalmacenaje', market: 'BMV', currency: 'MXN', state: 'Active', siteUrl: 'https://fibrastorage.com',    investorUrl: null,                                  reportsUrl: 'https://fibrastorage.com/repositorio-informacion-financiera/', nameVariants: ['Fibra Storage', 'Storage', 'STORAGE18', 'U-Storage'], createdAt: '2026-01-01T00:00:00Z' },
  { ticker: 'FHIPO14',   fullName: 'FHipo',                    shortName: 'FHipo',       sector: 'Hipotecario',   market: 'BIVA', currency: 'MXN', state: 'Active', siteUrl: 'https://fhipo.com/es/',         investorUrl: 'https://fhipo.com/es/kit-para-inversionistas/', reportsUrl: 'https://fhipo.com/es/reportes-trimestrales/', nameVariants: ['FHipo', 'Fideicomiso Hipotecario', 'FHIPO', 'FHIPO14'], createdAt: '2026-01-01T00:00:00Z' },
  { ticker: 'FCFE18',    fullName: 'CFE Fibra E',              shortName: 'CFE Fibra E', sector: 'Infraestructura', market: 'BMV/BIVA', currency: 'MXN', state: 'Active', siteUrl: 'https://cfecapital.com.mx', investorUrl: 'https://cfecapital.com.mx/inversionistas', reportsUrl: 'https://cfecapital.com.mx/inversionistas', nameVariants: ['CFE Fibra E', 'FCFE', 'FCFE18'], createdAt: '2026-01-01T00:00:00Z' },
]


const routesToRender = [
  { url: '/', initialData: {} },
  { url: '/comparar', initialData: {} },
  { url: '/herramientas', initialData: {} },
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
        html = html.replace('<title>Fibras Inmobiliarias</title>', '')
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
