# FIBRADIS — Módulos y Responsabilidades

## Módulos Backend (Application/Domain/Infrastructure)

### Catalog
- Catálogo maestro de FIBRAs: ticker único, nombre completo, nombre corto, sector, mercado, moneda, estado (activo/inactivo)
- URLs oficiales por FIBRA para discovery de PDFs
- Variantes de nombre para queries Google News RSS (editables desde Ops sin redeploy)
- Soft delete de FIBRAs (no borra histórico)
- Schema: `catalog`

### Market
- Precios Last Price desde Yahoo Finance — actualización cada 15 min en horario BMV (8:15am–3:15pm CDMX, días hábiles)
- Snapshots diarios retenidos 90 días
- Clasificación de frescura: `Fresh` (<20 min), `Stale` (20 min–6 h), `critico` (>6 h), `fuera-de-horario`
- Histórico de precio para gráficas (selectores 1M, 3M, 6M, 1A)
- Distribuciones desde Yahoo Finance (frecuencia detectada, no asumida)
- Yield anualizado calculado con frecuencia real detectada
- Schema: `market`

### News
- Ingesta horaria desde Google News RSS
- Queries por ticker/variantes + queries generales de mercado (configurables desde Ops)
- Blocklist de términos configurable desde Ops (ej: "fibra óptica", "fibra dietética")
- Deduplicación por URL exacta + similitud de título (ventana 24h)
- Asociación automática con FIBRAs por coincidencia de ticker en título/snippet
- Una noticia puede asociarse con 0, 1 o N FIBRAs — las de 0 van al feed general
- Resumen IA según AI_MODE (Off: sin resumen, Manual: resumen disparado por AdminOps, Api: automático)
- Schema: `news`

### Fundamentals
- Almacenamiento y procesamiento de PDFs de reportes financieros trimestrales
- Tres modos según AI_MODE (Off/Manual/Api) — mismo motor de almacenamiento
- Campos por registro: fibra_id, periodo (trimestral), cap_rate, nav_per_cbfi, ltv, noi_margin, ffo_margin, quarterly_distribution, summary, pdf_ref, status, processing_mode, captured_at
- Endpoint de importación: `POST /api/v1/ops/fundamentals/import` (payload JSON)
- AdminOps confirma antes de que sea visible en M5
- Estados: `pendiente`, `procesado`, `parcial`, `error`
- Schema: `fundamentals`

### Portfolio
- Carga de posiciones via Excel/CSV (columnas: Ticker, Qty, AvgCost — case-insensitive)
- Formato: un único `AvgCost` por posición; si hay múltiples filas del mismo ticker, consolida sumando Qty y calculando costo promedio ponderado
- Validación síncrona: si hay errores, no se persiste nada, se devuelven errores por fila
- Edición inline de Qty y AvgCost con recálculo inmediato
- Solo posiciones con titulos > 0 se mantienen activas
- Schema: `portfolio`
- Campos persistidos: fibra_id, user_id, titulos, costo_promedio, costo_total_compra, uploaded_at
- Favoritos de usuario también en schema `portfolio`
- Ownership: ningún otro módulo escribe en este schema

### Dashboard (Read Model)
- Módulo de agregación — NO tiene schema propio en MVP
- La pantalla `/portafolio` une carga, gestión y dashboard en una sola superficie
- No existe ruta `/dashboard` separada
- Consume datos de Market, Fundamentals, Portfolio via contratos de Application
- KPIs: Inversión Total, Valor Total, Plusvalía Total (%), Ganancia Total ($), Rentas Anuales Brutas, Rentas Reales Brutas, % Rentas del Portafolio

### Opportunities (Read Model)
- Score de oportunidad (0-100) con 5 componentes configurables por usuario:
  - Descuento NAV (default 30%), Dividend Yield (30%), LTV invertido (20%), Margen NOI (10%), Precio vs AVG 52S (10%)
- Normalización por percentil dentro del universo activo (no umbrales absolutos)
- Si falta un componente: redistribución proporcional de peso entre disponibles
- FIBRAs necesitan ≥3 componentes para ranking principal; <3 van a sección "datos limitados"
- Si ningún componente calculable con precio → `no evaluable`, excluída del ranking
- Tres perfiles preconfigurados: Renta, Valor, Conservador
- Vista Universo Completo + Vista Promediar Posición (solo FIBRAs del portafolio del usuario)
- Simulador de promedio en vista Promediar (sin recomendaciones de compra/venta)
- Degradation warning si >30% de FIBRAs sin precio; ranking suspendido si cobertura <50%
- NO tiene schema propio; preferencias y pesos persisten en schema `portfolio`

### Ops
- Dashboard operativo: estado de pipelines, últimas corridas, últimos 5 errores globales
- Sección Pipelines: historial con detalle, Run now con auditoría
- Sección Fundamentales: importación JSON, preview antes de confirmar, historial por FIBRA
- Sección Catálogo: CRUD de FIBRAs + variantes de nombre
- Sección Configuración: commission_factor, avg_periods, blocklist, AI_MODE, cadencias de pipelines
- Toda acción auditada con actor + timestamp + antes/después

### Ai
- Work items de procesamiento IA
- Gestión de AI_MODE (Off/Manual/Api)
- Bridge para proveedor IA (MVP: Manual/Off; Api: Growth)
- Schema: `ai`

### Jobs (Infrastructure)
- Schema transversal para `PipelineRun` y `WorkItem`
- Estado operativo por corrida e item: `detected`, `pending`, `processing`, `processed`, `partial`, `error`
- No reemplaza ownership de dominio de otros módulos

## Módulos Frontend

### Main SPA (`src/Web/Main`)

| Módulo | Ruta | Auth |
|---|---|---|
| home | `/` | Público |
| mercado | `/mercado` | Público |
| catalogo | `/catalogo` | Público |
| noticias | `/noticias` | Público |
| ficha-publica | `/fibras/:ticker` | Público |
| comparador | `/comparar` | Público (Growth) |
| portafolio | `/portafolio` | `User` |
| oportunidades | `/oportunidades` | `User` |
| fundamentales | `/fundamentales` | Público/Privado |

### Ops SPA (`src/Web/Ops`)

| Módulo | Ruta |
|---|---|
| dashboard-operativo | `/ops` |
| corridas | `/ops/pipelines` |
| work-items | `/ops/work-items` |
| schedules | `/ops/schedules` |
| pdf-config | `/ops/pdf-config` |
| ai-mode | `/ops/ai-mode` |
| auditoria | `/ops/auditoria` |

## Reglas de Cálculo Críticas

### commission_factor (DR-13)
`Costo Total Compra = Titulos × Costo_promedio × (1 + commission_factor)`
- Configurable desde Ops sin redeploy
- Cambio NO retroactúa posiciones existentes; aplica solo a cálculos de lectura futuros

### avg_periods (DR-11)
- Default: 4 periodos (≈ 1 año)
- Se usa para todos los promedios AVG de fundamentales y distribuciones
- Configurable desde Ops sin redeploy

### % Portafolio (DR-12)
`(Titulos_i × Costo_promedio_i) / Suma(Titulos_j × Costo_promedio_j)`
- Basado en monto invertido, no en cantidad de CBFIs

### Dividend Yield (DR-14) — jerarquía de fuente
1. Yield del reporte oficial de Fundamentales (si hay periodo dentro de últimos N)
2. Distribuciones de Yahoo Finance
3. No disponible (nunca inventar)
- La UI debe mostrar la fuente utilizada

### Horario de Mercado (DR-15)
- Pipeline corre 8:15am–3:15pm hora CDMX, lunes–viernes días hábiles
- Fuera de horario: estado `fuera-de-horario` (no Stale ni crítico)
