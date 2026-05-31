# Retrospectiva Sprint — Extensión Cross-Épica + Épica 8

**Fecha:** 2026-05-31  
**Alcance:** 7 historias sin cobertura de retro desde epic-5-retro-ext-2026-05-26  
**Estado:** completada — deuda de review resuelta el 2026-05-31 (5-8 y 4-10)  
**Participantes:** Jorge (Project Lead), Amelia (Developer), Alice (Product Owner), Charlie (Senior Dev), Dana (QA Engineer), Elena (Junior Dev)

---

## Historias cubiertas en este retro

| Historia | Épica | Code review | Patches | Estado retro |
|---|---|---|---|---|
| 5-9 Análisis IA enriquecido fundamentales | 5 | ✅ Completo (2 pasadas) | 10 + 1 fix High | ✅ Incluida |
| 4-11 Página listado noticias | 4 | ✅ Completo | 6 | ✅ Incluida |
| 5-10 Página pública fundamentales cross-FIBRA | 5 | ✅ Completo | 6 | ✅ Incluida |
| 8-1 Sección "Conoce las FIBRAs" | 8 | ✅ Completo (2 pasadas) | 6 | ✅ Incluida |
| 8-2 Catálogo FIBRAs + Descripción + Página Pública | 8 | ✅ Completo | 0 | ✅ Incluida |
| **5-8 Observabilidad llamadas IA** | 5 | ✅ 8 patches aplicados (2026-05-31) | 8+AC-4 | ✅ Cerrada |
| **4-10 Análisis IA enriquecido noticias** | 4 | ✅ Code review completo (2026-05-31) | 2 | ✅ Cerrada |

---

## Métricas — Historias cubiertas

| Historia | Patches CR | Defers | Tests | Agente IA usado |
|---|---|---|---|---|
| 5-9 Análisis IA enriquecido fundamentales | 10 + fix High | varios | 397 backend + 62 Main | gpt-5-codex |
| 4-11 Listado noticias | 6 | 7 | 5 tests repositorio | claude-sonnet-4-6 |
| 5-10 Fundamentales cross-FIBRA | 6 | 5 | 6 unit tests | claude-sonnet-4-6 |
| 8-1 "Conoce las FIBRAs" | 6 | 10 | 193 unit tests | claude-sonnet-4-6 |
| **8-2 Catálogo + Descripción** | **0** | 8 | 22 integration tests | claude-sonnet-4-6 |
| **Total (5 historias)** | **~28 patches** | **35+** | — | — |

---

## Qué Salió Bien

### 1. 8-2 con 0 patches — benchmark de calidad del sprint

La historia con el spec más detallado (validación explícita 10k chars, rutas documentadas como `/fibras/:ticker` con la corrección de plural, campo `hasDescription` vs `description` en DTOs diferenciados, ReactMarkdown con wrapper div explicado) produjo 0 patches de code review. La relación es directa: la inversión en Dev Notes transfiere complejidad del momento de review al momento de spec.

### 2. 5-9 atrapa y corrige un bug conceptual (hallazgo High)

`UpdateKpiExtractionAsync()` decidía `Status` y `ErrorReason` basándose únicamente en KPIs numéricos, aunque el parser ya consideraba válidos análisis cualitativos (`summaryMarkdown`, `operationalSignals`, etc.). El code review lo detectó como High, se corrigieron 2 tests unitarios específicos para el caso cualitativo-only, y el fix quedó documentado. Es un ejemplo del proceso de review funcionando como detector de lógica incorrecta, no solo de estilo.

### 3. 5-9 resuelve deuda de workaround de historias anteriores

El workaround `ErrorReason = extractionNotes` que quedó pendiente desde 5-6 fue limpiado en 5-9. La historia resolvió la deuda que heredó y dejó el módulo más limpio de lo que lo encontró.

### 4. Velocidad de implementación — historias complejas en 1-2 días

5-9 (extender análisis IA + persistencia + 2 SPAs + 3 endpoints + tests), 5-10 (2 endpoints nuevos + página comparativa + 6 tests), 8-1 (módulo editorial completo + 2 SPAs + seed data + migración EF), 8-2 (campo + validación + página + breadcrumb + sección en FibraPage + 22 integration tests) — todas completadas con builds limpios y test suites verdes. El scaffolding del proyecto y los patrones establecidos hacen posible esta velocidad.

### 5. Decisiones de implementación bien razonadas (8-1 y 8-2)

- 8-1: Sidebar layout vs shadcn Tabs — el dev implementó una UI más usable que el spec prescrito. Validado en Playwright, aceptado por review.
- 8-1: `Application/Ops` en lugar de `Domain/Ops` para el repositorio — siguió el patrón real del repo, no el que indicaban Dev Notes.
- 8-2: Convirtió `SECTION_LABELS` de constante top-level a variable local para hacer la sección de Descripción condicional — elegante sin overengineering.

---

## Dificultades y Patrones Detectados

### 1. Deduplicación en batch queries (patrón de bug de alta frecuencia)

Dos bugs de deduplicación en el mismo sprint:
- **5-10 P1 HIGH**: `GetSummaryByPeriodAsync` retornaba múltiples filas para la misma FIBRA cuando existían varios registros `processed` del mismo período. Causaría colisiones de React `key={row.ticker}` en la UI.
- **4-11 P1**: `GetPagedPublicAsync` no proyectaba `FibraId` en el JOIN batch → `LinkedFibraDto.id = Guid.Empty` en todos los artículos con FIBRA asociada.

**Por qué pasan**: los tests con InMemory provider trabajan con colecciones en memoria donde las consultas LINQ son más permisivas que el SQL generado. Un test que verifique semánticamente el resultado con datos reales en SQL Server habría atrapado ambos.

**Mitigación propuesta**: para cualquier repositorio con JOIN + proyección manual, documentar en Dev Notes las columnas del resultado esperado con un ejemplo concreto.

### 2. Checklist SEO incompleto — og tags olvidados (4-11 P3)

El checklist de SEO actual en Dev Notes y convenciones documenta `<title>` y `<meta name="description">`, pero 4-11 implementó una ruta pública nueva sin `og:title`, `og:description`, `og:type`. El reviewer los detectó en P3.

**Patrón**: las rutas públicas con listados (noticias, catálogo, fundamentales) son las más probables de compartirse en redes sociales — exactamente donde og tags importan más.

**Mitigación propuesta**: actualizar el template de Dev Notes con el checklist de SEO expandido:
```
- [ ] <title>Nombre — FIBRADIS</title>
- [ ] <meta name="description" .../>
- [ ] <meta property="og:title" .../>
- [ ] <meta property="og:description" .../>
- [ ] <meta property="og:type" content="website"/>
```

### 3. TOCTOU en endpoints Ops (patrón recurrente de historias anteriores)

8-1 P1: el endpoint `PUT /pages/{slug}` hacía `GetBySlugAsync` + `UpdateContentAsync` en dos awaits. Fix: `UpdateContentAsync` retorna `int` (filas afectadas) y devuelve 404 si el resultado es 0.

Este patrón viene de Épicas anteriores (mencionado en retros de Épica 3-5). El template de Dev Notes para endpoints Ops debería incluir explícitamente: "¿El handler hace un check de existencia antes de la mutación? Si sí, consolidar en una operación atómica."

### 4. Presión de velocidad con consecuencias de calidad (5-8 y 4-10)

Este es el hallazgo más importante de la retro:
- **5-8** fue cerrada administrativamente con 9 patches del reviewer sin aplicar. Incluyen violations de AC (colores badge invertidos, `TryLogAsync` sin `logger.LogWarning` violando AC5) y tests de integración que solo verifican HTTP 200 sin assertions semánticas.
- **4-10** está en main sin code review.

**Jorge (Project Lead) lo reconoció directamente**: "La presión de velocidad tiene un costo."

El costo documentado: código en producción con bugs de AC y tests que no detectarían regresiones reales. El módulo de Observabilidad de Llamadas IA en Ops está operacionalmente incorrecto en presentación (badges) y en logging (AC5 violado).

**Conclusión de la retro**: el proceso de review funciona cuando se aplica — 8-2 con 0 patches lo demuestra. La decisión de saltarlo no acelera el proyecto; acumula deuda que toma más tiempo resolver después.

### 5. ReactMarkdown sin sanitización (deferred recurrente)

Deferred en 5-9, 8-1, 8-2 (también 4-11). El riesgo es bajo hoy porque el contenido lo escribe solo AdminOps. Se vuelve relevante si se amplían permisos de escritura. Documentado para revisión antes de cualquier feature que permita contenido externo.

---

## Seguimiento de Action Items de Retro Anterior

*(epic-5-retro-ext-2026-05-26)*

| Action item | Estado | Evidencia |
|---|---|---|
| [CP-1] Verificar observabilidad errores IA en dev | ✅ Verificado (2026-05-26) | PipelineErrorLog con detalle DiagnosticID — documentado en retro anterior |
| [CP-2] Probar extract-kpis con PDF real | ⚠️ No hay evidencia de validación en stories | No documentado en ninguna Completion Notes posterior |
| [P-1] AC como checklist pre-commit | ⚠️ Parcialmente aplicado | 8-2 limpia; 5-10 P2, 4-11 P4 y P5 son patches por AC no verificados pre-commit |
| [P-2] Casos edge de input en Dev Notes | ⚠️ Parcialmente aplicado | 8-2 tiene límite 10k en spec y se aplica; 8-1 no tiene límite en content hasta P4 del review |
| [P-3] Documentar ajustes manuales post-codegen | ✅ Aplicado en 8-2 | Completion Notes documentan fix de configApi.ts intersección de tipos |
| [R-1] PipelineRunLog/PipelineErrorLog sin retención | ❌ No resuelto | No hay evidencia en ninguna historia de este sprint |

**Hallazgo**: 3 de 6 action items del retro anterior no se completaron o se aplicaron parcialmente. La deuda de retención de PipelineRunLog viene desde epic-5-retro-2026-05-25 (más de una semana sin resolver).

---

## Aprendizajes Clave

1. **Mejor spec = menos patches. La inversión en Dev Notes paga en review.** 8-2 (spec completo con edge cases y validaciones explícitas) → 0 patches. El patrón es reproducible.

2. **Batch queries con JOINs manuales necesitan test de integración real, no InMemory.** Los dos bugs de deduplicación (5-10 P1, 4-11 P1) no habrían pasado con SQL Server real en los tests.

3. **Cerrar una historia administrativamente no acelera el proyecto.** Crea deuda que bloquea la siguiente etapa y toma más tiempo resolver que aplicar los patches originalmente.

4. **El checklist de SEO necesita og tags explícitos.** El olvido es recurrente; la solución es una línea en el template, no mejor intención.

5. **El proceso de review funciona.** Las 5 historias que siguieron el proceso completo tienen implementación correcta, tests verdes y deuda conocida y gestionada.

---

## Deuda Técnica Relevante para Épica 6

### Crítica (bloquea o degrada Épica 6 si no se resuelve)

- **[CR-1] 9 patches de 5-8 sin aplicar** — AC violations activas en Ops. Fix: `/bmad-dev-story` sobre story file de 5-8.
- **[CR-2] 4-10 sin code review** — Código en main sin revisión. Fix: `/bmad-code-review` sobre archivos de 4-10.
- **[CR-3] PipelineRunLog/PipelineErrorLog sin retención** — Deuda desde retro 2026-05-25. Incluir en primera historia de Épica 6 como tarea técnica obligatoria.

### Media (no bloquea Épica 6 pero degrada operación)

- **[DM-1] Índice (Provider, CreatedAt) faltante en AiCallLog** (deferred 5-8) — Queries de filtrado en Ops lentos con volumen creciente de logs.
- **[DM-2] ReactMarkdown sin rehype-sanitize** (deferred 5-9, 8-1, 8-2) — Sin urgencia hasta que se amplíen permisos de escritura.
- **[DM-3] fetchAllFibras hard-capped a 100** (deferred 8-2, 4-11) — Silencioso con catálogo actual de ~20 FIBRAs. Requiere loop si supera 100.

### Baja prioridad

- **[DB-1] GetBySlugAsync dead code en EditorialPage** (deferred 8-1) — No causa bug; podría usarse en endpoints futuros.
- **[DB-2] KpiExtractionResult con 15 parámetros posicionales** (deferred 5-7 heredado) — Riesgo de transposición en tests.

---

## Action Items

### Crítica — Antes de iniciar Épica 6

**[AC-1] Aplicar 8 patches de 5-8 (Observabilidad Llamadas IA)** ✅ CERRADO 2026-05-31  
`OperationBadge` corregido (KpiExtraction→amber, NewsSummary→sky), `ProviderBadge` añadido, `TryLogAsync` con `logger.LogWarning`, filtro case-insensitive, `ErrorMessage` truncado a 500 chars, `ValueGeneratedOnAdd()` en `CreatedAt`, tests semánticos en integración, test AC5 de aislamiento de logging.

**[AC-2] Code review completo de 4-10 (Análisis IA Enriquecido Noticias)** ✅ CERRADO 2026-05-31  
2 patches aplicados: P1 `<h1>` con `displayTitle` (headline de IA cuando existe), P2 snippet inyectado en prompt cuando no hay body text. 4 defers, 3 dismissed. Historia → done.

**[AC-3] Incluir retención de PipelineRunLog/PipelineErrorLog en primera historia de Épica 6** ⏳ PENDIENTE  
Al crear la primera historia de Épica 6, incluir como tarea técnica obligatoria (llevan 3 retros como pendiente).

**[AC-4] Índice (Provider, CreatedAt) en AiCallLog** ✅ CERRADO 2026-05-31  
Migración `20260531191741_AiCallLog_AddProviderIndex` generada y mergeada. Pendiente: `dotnet ef database update` en dev antes de iniciar Épica 6.

### De Proceso — Para historias de Épica 6

**[AP-1] Actualizar checklist SEO con og tags explícitos** ✅ CERRADO 2026-05-31  
`convenciones-fibradis.md` actualizado con `og:title`, `og:description`, `og:type` como ítems separados en el checklist.

**[AP-2] Para batch queries con JOINs, documentar columnas del resultado en Dev Notes**  
Owner: Development  
Acción: Añadir a las convenciones: "Todo repositorio que implementa batch loading o JOIN manual debe documentar en Dev Notes las columnas exactas de la proyección con un ejemplo de resultado esperado."  
Criterio: Los dos bugs de deduplicación (5-10 P1, 4-11 P1) habrían sido atrapados con este tipo de documentación.

**[AP-3] No cerrar historias administrativamente — aplicar o diferir explícitamente cada finding**  
Owner: Development  
Acción: Todo finding del reviewer debe ser marcado como `[x] [Patch]` (aplicado), `[x] [Defer]` (diferido con razón) o `[x] [Dismissed]` (descartado con justificación). Cierre administrativo = deuda invisible.  
Criterio: Cero historias en main con findings en estado `[ ]` (sin procesar).

---

## ~~Deuda Crítica Pendiente (5-8 y 4-10)~~ — RESUELTA 2026-05-31

✅ **5-8 — 8 patches aplicados el 2026-05-31**. Historia `done` con review completo. Índice (Provider, CreatedAt) incluido.

✅ **4-10 — Code review completo el 2026-05-31**. 2 patches aplicados (P1 `displayTitle` en `<h1>`, P2 snippet en prompt). Historia `done`.

**Acción requerida**: `/bmad-code-review` sobre los archivos de 4-10 antes de Épica 6.

---

## Readiness Assessment para Épica 6

| Área | Estado | Acción requerida |
|---|---|---|
| Código de 5 historias cubiertas | ✅ Verde, builds limpios | — |
| Tests suite general | ✅ >397 backend + tests SPAs | — |
| 5-8 patches abiertos | ❌ AC violations activas | AC-1 antes de Épica 6 |
| 4-10 sin code review | ❌ Superficie desconocida | AC-2 antes de Épica 6 |
| PipelineRunLog retención | ❌ Pendiente desde 2026-05-25 | AC-3 en primera historia Épica 6 |
| Épica 8 (in-progress) | ⚠️ 2 historias done, más por definir | Continuar creación de historias según necesidad |
| Blockers técnicos para Épica 6 | ✅ Ninguno técnico | Pendientes son de proceso |

**Veredicto**: El proyecto puede iniciar Épica 6 después de resolver AC-1 y AC-2. AC-3 se incluye en la primera historia de Épica 6. AC-4 puede ir junto con AC-1.

---

## Cierre

**Amelia (Developer):** "Este sprint entregó funcionalidad real: página educativa con contenido verificado sobre FIBRAs, catálogo público, análisis IA enriquecido en fundamentales y noticias, observabilidad de llamadas IA, y comparativa cross-FIBRA. 8-2 con 0 patches es el benchmark del sprint — demuestra que el proceso de calidad funciona."

**Alice (Product Owner):** "Lo que me quedo de esta retro: la presión de velocidad es real, pero cerrar 5-8 y omitir 4-10 no aceleró nada. Solo postergó el trabajo con interés."

**Charlie (Senior Dev):** "Y los dos bugs de deduplicación me recuerdan que los tests con InMemory tienen un techo. Las queries SQL tienen semántica diferente a LINQ en memoria."

**Dana (QA Engineer):** "El checklist de SEO necesita og tags. Es una línea. La haremos mañana."

**Jorge (Project Lead):** "Directo y sin filtros. Gracias al equipo."
