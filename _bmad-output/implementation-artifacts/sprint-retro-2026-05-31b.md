# Retrospectiva Sprint — 4-12 + 5-11 (segunda retro del 2026-05-31)

**Fecha:** 2026-05-31  
**Alcance:** 2 historias/features entregadas después del cierre del sprint-retro-ext-2026-05-31  
**Participantes:** Jorge (Project Lead), Amelia (Developer), Alice (Product Owner), Charlie (Senior Dev), Dana (QA Engineer), Elena (Junior Dev)

---

## Historias cubiertas

| Historia | Épica | Agente IA | Code review | Patches | Estado retro |
|---|---|---|---|---|---|
| 4-12 Gate body_text mínimo para IA | 4 | Claude Sonnet 4.6 | ⚠️ No documentado | — | ✅ Incluida |
| 5-11 Ingesta incremental AMEFIBRA | 5 | GPT-5 Codex | ✅ Completo | 9 aplicados, 6 deferred | ✅ Incluida |

---

## Métricas

| Métrica | 4-12 | 5-11 |
|---|---|---|
| Tipo | Micro-feature (1 commit) | Historia completa (4 commits) |
| Archivos modificados | 20 | 41 |
| Tests nuevos | ~167 (unit + integration) | Unit + integration cubiertos |
| Code review | ⚠️ Sin documentar | ✅ 9 patches, 6 defers |
| Deuda técnica nueva | 0 (feature puntual) | 6 defers documentados |

---

## Seguimiento de action items — sprint-retro-ext-2026-05-31

| Action item | Estado |
|---|---|
| [AC-1] 8 patches 5-8 aplicados | ✅ CERRADO (commit 97ec7f3) |
| [AC-2] Code review 4-10 completo | ✅ CERRADO (commit b527707) |
| [AC-3] Retención PipelineRunLog en Épica 6 | ❌ PENDIENTE — cuarta retro seguida sin resolver |
| [AC-4] Índice (Provider, CreatedAt) AiCallLog | ✅ CERRADO (migración en 5-11) |
| [AP-1] SEO og tags en convenciones | ✅ CERRADO (commit 238513f) |
| [AP-2] Batch queries JOINs: documentar columnas en Dev Notes | ⏳ Sin evidencia de aplicación en 5-11 o 4-12 |
| [AP-3] No cerrar historias sin procesar findings | ✅ Aplicado en 5-11 (todos los findings marcados) |

---

## Qué salió bien

### 1. 5-11 reutiliza el motor de Fundamentals sin crear vía paralela

`FundamentalRecord`, `MarkdownContent`, extracción KPIs, `AiCallLog` — el pipeline automático de AMEFIBRA los usa todos sin duplicar persistencia. Uno de los constraints más difíciles de respetar cuando se añade automatización encima de un flujo manual preexistente.

### 2. Code review de 5-11 captura 9 bugs reales antes de producción

Incluyendo: null dereference silencioso en error path (P1 HIGH), warmup N×2+1 HEAD requests por corrida (P2 HIGH), `.GetAwaiter().GetResult()` bloqueante en thread pool Hangfire (P3 HIGH), e inversión de orden PDF→BD que generaría archivos huérfanos (P5 MEDIUM). Sin review, estos llegaban a producción.

### 3. Cobertura de tests de 4-12 es seria para el scope de la feature

121 tests en `NewsPipelineJobThresholdTests.cs` para el gate de body_text. Umbral 0 (desactivar gate), null body_text, body_text exactamente en el umbral — los edge cases principales están cubiertos.

### 4. `AmefibraTitleParser` clasifica explícitamente los ambiguos

Trimestrales → `Q1-2024`, anuales → clasificados pero fuera del flujo trimestral, títulos ambiguos → `pending-classification` con observabilidad. Decisión de diseño correcta que evita silenciar errores de parseo.

### 5. `ShortYearRegex` acotado a 2018–2035 (P7)

Convertir cualquier número de 2 dígitos en año era un bug de parsing que habría producido períodos incorrectos con algunos títulos AMEFIBRA. El rango 2018–2035 es el dominio correcto para FIBRAs mexicanas.

---

## Dificultades y patrones detectados

### 1. 4-12 sin code review documentado — mismo patrón que 4-10

4-10 también fue implementada sin code review y quedó como deuda crítica que resolvimos el mismo día del sprint-retro-ext-2026-05-31. 4-12 repite el patrón: una sola sesión de desarrollo, sin review pass, cerrada como done. Las preguntas que un review habría cubierto:
- ¿El gate lee `MinBodyTextLengthForAi` una vez por corrida o por artículo? ¿Qué pasa si el valor cambia mientras el pipeline está corriendo?
- ¿El test de integración verifica explícitamente que el gate NO aplica a `/ai-summary` y `/ai-analysis`?

**Consecuencia directa**: necesitamos un review pass de 4-12 antes de iniciar Épica 6.

### 2. Bugs de código asíncrono en 5-11 — patrón de capas

P2 (`WarmupAsync` lazy) y P3 (`.GetAwaiter().GetResult()`) son bugs de programación asíncrona que aparecen cuando la orquestación usa `await` correctamente pero la capa de cliente HTTP usa patrones síncronos. GPT-5 Codex generó código async/await correcto en los niveles altos pero síncrono en los bajos. Este patrón de "correcto en el nivel visible, bloqueante en el nivel bajo" necesita atención explícita en las Dev Notes: "todos los clientes HTTP del pipeline deben ser async end-to-end."

### 3. P5 — inversión de orden PDF/BD: bug de integridad de datos

`IngestAsync` escribía el archivo PDF a disco antes de insertar el `FundamentalRecord` en BD. Si la BD falla, el archivo queda huérfano. El fix (pre-computar ruta → insertar en BD → escribir archivo) protege contra este escenario. Es un bug que no aparecería en tests con InMemory pero sí en producción con fallo de BD transitorio.

### 4. AC-3 acumula — cuarta retro sin resolverse

La retención de `PipelineRunLog`/`PipelineErrorLog` lleva cuatro retros seguidas como pendiente. Con 5-11 ahora hay dos pipelines activos (News + Fundamentals) enviando eventos al mismo log. El volumen crece más rápido que antes. Si no se incluye en 6-1 como condición (no como tarea opcional), seguirá postergándose.

### 5. D2 y D4 de 5-11 — deuda con riesgo latente

- **D2**: si `IngestAsync` falla después de `fundamentalRepo.AddAsync`, el manifest queda con `LastDecision = "error"` sin `LastProcessedRecordId`. Las corridas futuras marcarán ese reporte como "skip" indefinidamente — deuda silenciosa que se acumula.
- **D4**: `GetLatestByFibraAndPeriodAsync` filtra solo `quarterly`. Si AMEFIBRA publica una actualización de un reporte anual con distinto `packageUrl`, puede causar violación de unique constraint sin handler.

---

## Aprendizajes clave

1. **El review sigue siendo el detector más efectivo.** 9 issues en 5-11, incluyendo 3 HIGH y un bug de integridad de datos. Nada de esto aparecería en los tests con InMemory.

2. **Micro-feature no es sinónimo de exento de review.** 4-12 y 4-10 son el mismo patrón. El umbral es pequeño, pero toca BD, jobs y contratos de API.

3. **La deuda que se repite en retros necesita estructura diferente.** AC-3 no falla por falta de intención — falla porque siempre se posterga a "la primera historia de la siguiente épica". Necesita ser condición de inicio, no tarea interna.

4. **Clientes HTTP en pipelines async: documentar en Dev Notes que el async debe ser end-to-end.** El patrón `.GetAwaiter().GetResult()` es invisible en el nivel de orquestación y solo aparece en review o en saturación de workers.

5. **Deferred con riesgo de unique constraint (D4) merecen seguimiento activo.** Un deferred clasificado como "MEDIUM" puede bloquear una corrida en producción si se activa el caso edge.

---

## Deuda técnica

### Crítica — resolver antes de Épica 6

- **4-12 sin code review** — funcional pero superficie no verificada
- **D6 (5-11)**: tests de regresión `FundamentalsHistory` y endpoint público `/fundamentales` — el Portafolio va a depender de estos

### Media — seguimiento activo

- **D2 (5-11)**: estado inconsistente manifest/record en error parcial → skip indefinido
- **D4 (5-11)**: `possibleUpdate` no detecta anuales con distinto packageUrl → posible unique constraint

### Baja

- **D1 (5-11)**: PDFs en memoria completa (`ReadAsByteArrayAsync`) con presión de memoria potencial
- **D3 (5-11)**: `GetCronExpression` sin log en caso default
- **D5 (5-11)**: `SourcePublishedAt` null sin re-hidratación en skip path

---

## Action items

### Críticos — antes de iniciar Épica 6

**[AC-1] Code review de 4-12 (Gate body_text mínimo IA)**  
Owner: Development  
Deadline: Antes de iniciar historia 6-1  
Criterio: Review pass documentado con todos los findings en estado Patch/Defer/Dismissed. Verificar explícitamente: lectura de umbral por corrida vs por artículo, y ausencia del gate en endpoints manuales.

**[AC-2] D6 de 5-11 — Tests de regresión FundamentalsHistory y endpoint público**  
Owner: Development  
Deadline: Antes de historias de Épica 6 que consuman fundamentales  
Criterio: Al menos un test de integración que verifique que registros `ImportedBy = "system:amefibra"` aparecen correctamente en `FundamentalsHistory` (Ops) y en el endpoint público.

**[AC-3] Retención PipelineRunLog/PipelineErrorLog en historia 6-1 — condición, no tarea interna**  
Owner: Development  
Deadline: Historia 6-1 (cuarta retro seguida — no es opcional)  
Criterio: La retención se define, se implementa y se documenta como AC de 6-1. El Dev Notes de 6-1 debe incluirla explícitamente.

**[AC-4] Confirmar migración AddFundamentalsAutomationManifest aplicada en dev**  
Owner: Development  
Deadline: Inmediato (antes de cualquier corrida en dev)  
Criterio: `dotnet ef database update` ejecutado en LAPBADIS/FIBRADIS_Dev.

### De proceso — para todas las historias de Épica 6

**[AP-1] Micro-features que tocan BD/jobs/contratos de API requieren code review**  
Owner: Development / Jorge (process)  
Criterio: Cualquier implementación con migración EF, cambio de job, o contrato de API nuevo/modificado requiere un review pass documentado, sin excepción de tamaño.

**[AP-2] Clientes HTTP en pipelines: documentar en Dev Notes que async debe ser end-to-end**  
Owner: Development  
Criterio: Añadir a las convenciones/template de Dev Notes: "Todo cliente HTTP usado dentro de un Hangfire job debe ser async end-to-end — sin `.GetAwaiter().GetResult()` ni `.Result` en ningún nivel."

**[AP-3] AC-3 como condición de inicio de 6-1 (no sección de Dev Notes opcionales)**  
Owner: Jorge (Project Lead)  
Criterio: Al crear story 6-1, la retención de PipelineRunLog debe estar en los Acceptance Criteria, no en Dev Notes como "nice-to-have".

---

## Readiness assessment para Épica 6

| Área | Estado | Acción requerida |
|---|---|---|
| Módulo Fundamentals automático (5-11) | ✅ Funcional con review completo | D2/D4 seguimiento; D6 antes de Épica 6 |
| Módulo News + gate IA (4-12) | ⚠️ Funcional sin review | AC-1 antes de iniciar 6-1 |
| Migración DB aplicada en dev | ⚠️ Por confirmar | `dotnet ef database update` |
| PipelineRunLog retención | ❌ Cuarta retro pendiente | AC-3 en AC de 6-1, obligatorio |
| Tests regresión Fundamentals público | ⚠️ Deferred (D6) | AC-2 antes de historias que consumen |
| Blockers técnicos para portafolio | ✅ Ninguno crítico | — |

**Veredicto:** El proyecto puede iniciar Épica 6 después de completar AC-1 y AC-4. AC-2 debe ir antes de las primeras historias que consuman fundamentales. AC-3 es condición no negociable de 6-1.

---

## Cierre

**Amelia (Developer):** "5-11 es la historia más compleja que hemos entregado en un solo ciclo: discovery incremental, manifest de deduplicación, job automático, observabilidad, auto-confirm, integración completa. El code review fue el que convirtió 'funciona' en 'funciona correctamente'."

**Alice (Product Owner):** "Lo que me llevo: AC-3 en los Acceptance Criteria de 6-1. No en Dev Notes. En AC."

**Charlie (Senior Dev):** "Y 4-12 necesita su review. No porque esté roto — sino porque el patrón de saltar el review ya lo pagamos dos veces este sprint."

**Dana (QA Engineer):** "167 tests en 4-12 es excelente. Solo nos falta el segundo par de ojos."

**Elena (Junior Dev):** "5-11 me enseñó que el async end-to-end importa en cada capa, no solo en la orquestación."

**Jorge (Project Lead):** [cierre de sesión]
