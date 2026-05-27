# Retrospectiva de Extensión — Sprint IA: Historias 5-6 y 5-7

Fecha: 2026-05-26
Alcance: Historias 5-6 y 5-7 (completadas después del cierre de epic-5-retro-2026-05-25)
Tipo: Retrospectiva de extensión (adendum a Épica 5)
Estado: completada
Participantes: Jorge (Project Lead), Amelia (Developer), Alice (Product Owner), Charlie (Senior Dev), Dana (QA Engineer), Elena (Junior Dev)

---

## Contexto

La retrospectiva de Épica 5 se completó el 2026-05-25 y cubrió las historias originales 5-0 a 5-4. Las historias 5-6 y 5-7 se crearon e implementaron el 2026-05-26 como extensión del sprint de IA, y se revisan en este documento separado.

---

## Métricas

| Historia | Patches CR | Tests | Deferred | Dismissed |
|---|---|---|---|---|
| 5-6 — Extracción KPIs desde PDF vía IA | 7 | 180 int + 134 unit + 35 app | 2 | — |
| 5-7 — Notas IA por KPI + tooltips de definición | 3 | 4 unit + 25 int + 62 Main | 2 | 8 |
| **Total sprint IA** | **10** | **29 verdes (confirmados)** | **4** | **8** |

Dos historias complejas completadas en un solo día (2026-05-26).
Code review conjunto sobre rama `story/5-7-conclusiones-ia-por-kpi-y-tooltips-de-definicion`.

---

## Qué Salió Bien

### 1. IA calcula KPIs no explícitos desde datos del balance

El prompt instruye a la IA a **calcular** métricas que los reportes de FIBRAs mexicanas rara vez publican explícitamente (capRate, navPerCbfi, ffoMargin) desde los datos del balance. Solo devuelve `null` si los datos base tampoco están. Validado manualmente con Fibra Danhos 1T26 — los valores calculados coincidieron con los esperados.

### 2. `RoutingKpiExtractorService` — decisión arquitectónica no prescrita

El story file pedía registrar `IKpiExtractorService` replicando el patrón de `IAiSummaryService`. Durante implementación se creó `RoutingKpiExtractorService` para hacerlo correctamente sin acoplar el endpoint a un proveedor específico. Decisión tomada sin indicación explícita, resultado correcto.

### 3. Tooltips con `title` nativo — sin nueva dependencia

Se usó el atributo `title` del HTML para los tooltips de definición de KPI en lugar de añadir una librería. Simple, funcional, cero overhead de dependencia.

### 4. Manejo de errores correcto en el formulario de extracción

Si la IA falla, el formulario no se rompe — el error aparece inline y el operador puede continuar con captura manual. El flujo degradado fue implementado desde el inicio, no como afterthought.

### 5. Velocidad con calidad

Dos historias que tocan dos SPAs, backend, migración EF Core, dos implementaciones de servicio IA y regeneración de OpenAPI — completadas en un día con 29 tests verdes.

---

## Dificultades y Patrones Detectados

### 1. Validaciones de input faltantes en endpoints nuevos (patrón repetido)

- **P1**: Bounds check faltante en array `candidates`/`choices` de la respuesta IA — Gemini puede devolver `finishReason: SAFETY` sin `content`, DeepSeek puede devolver `choices: []`
- **P5**: Archivo de 0 bytes pasa la validación de tamaño y llega al extractor de PDF
- **P4**: `ex.Message` expuesto en la respuesta 502 — puede incluir URLs internas o nombres de modelos

Mismo patrón que "validaciones de frontera faltantes" de Épicas 3-4. El checklist acordado en retro 2026-05-25 no se aplicó porque las historias se crearon el mismo día que ese retro.

### 2. AC no completamente cubiertos en implementación inicial

- **P11**: AC2 especifica spinner visual de carga; la implementación solo cambió el texto del botón a "Extrayendo…" sin animación `animate-spin`
- **P7**: AC1 exige que si KPI tiene valor, la nota correspondiente no puede ser null; el parser lo aceptaba silenciosamente

Los AC se leyeron como especificación funcional pero no como checklist de verificación pre-commit.

### 3. Observabilidad de errores IA insuficiente (hallazgo operativo de Jorge)

Jorge (único operador actual) reportó que en el flujo de noticias había errores de API con la IA que nunca llegaban con suficiente detalle al módulo de errores de Ops. El módulo `PipelineErrorLog` de 5-0 fue patcheado para mejorar el detalle de logging, pero la verificación fue solo en código — nunca se validó con un error real en producción que el detalle diagnóstico sea legible desde la UI.

El endpoint `extract-kpis` devuelve 200 siempre (correcto por diseño para el flujo del formulario), lo que significa que errores "suaves" de la IA (JSON inválido, array vacío) llegan como `extractionNotes` en la respuesta — visible solo si el operador lo lee activamente.

### 4. `schema.d.ts` requirió ajuste manual post-codegen

Las Completion Notes de 5-7 documentan: "schema.d.ts requirió ajuste manual posterior al codegen para alinear los tipos consumidos por los SPAs con el OpenAPI generado." Si el ajuste manual no se documenta, la próxima regeneración puede romper los tipos silenciosamente.

---

## Aprendizajes Clave

1. **El checklist de review de Dev Notes necesita estar en el template antes de crear la primera historia de una épica, no acordarse en el retro anterior.** El timing del retro 2026-05-25 y la creación de 5-6 fue el mismo día — no hubo oportunidad de aplicarlo.

2. **Recorrer AC como checklist de verificación pre-commit, no solo leerlos como especificación.** P7 y P11 habrían sido atrapados antes del review.

3. **La observabilidad de errores de IA no es un nice-to-have cuando hay un único operador.** Si el operador no puede diagnosticar el origen de un fallo de IA desde la UI de Ops, está operando a ciegas.

4. **Los endpoints que devuelven 200 siempre (diseño correcto para UX) necesitan una estrategia explícita de visibilidad de errores internos.** `extractionNotes` es correcto para el operador, pero no reemplaza el logging diagnóstico en el servidor.

5. **Documentar los ajustes manuales post-codegen en Dev Notes.** Un ajuste sin documentar es una bomba de tiempo para la próxima regeneración.

---

## Deuda Técnica Relevante para Épica 6

- **`schema.d.ts` ajuste manual documentado** — verificar que está anotado y entendido antes de la primera historia de Épica 6 que requiera codegen
- **Observabilidad de errores IA en PipelineErrorLog** — validar con error real antes de 6-1 (ver CP-1)
- Deferred de 5-6: `isPossibleUpdate` inserta duplicados (pre-existente desde 5-2), `Period` sin `[Required]` (pre-existente)
- Deferred de 5-7: `KpiExtractionResult` con 15 parámetros posicionales (riesgo transposición), race condition en `/import` sin índice único `(FibraId, Period)` (pre-existente)

---

## Action Items

### Critical Path — Antes de iniciar historia 6-1

**[CP-1] Verificar observabilidad de errores IA en dev** — ✅ VERIFICADO (2026-05-26)
PipelineErrorLog contiene 3 entradas de ManualAiSummary con detalle diagnóstico suficiente: proveedor (DeepSeek), razón (respuesta vacía / resumen incompleto), título e ID del artículo afectado. La observabilidad de errores IA manuales funciona correctamente.
Hallazgo adicional: los 12 Hangfire failed jobs son fallos de infraestructura (ObjectDisposedException en IServiceProvider para NewsPipelineJob, DistributedLockTimeout para DailySnapshotHistoricalJob) — no errores de IA. Estos fallos son visibles en el Hangfire dashboard pero no en PipelineErrorLog (por diseño).

**[CP-2] Probar `extract-kpis` con PDF real en dev**
Owner: Jorge
Acción: Subir un PDF real de una FIBRA al formulario de importación en dev → verificar extracción de KPIs, que los valores calculados son razonables, y que los tooltips y notas IA aparecen en la ficha pública de Main.
Criterio: Flujo end-to-end validado manualmente antes de usar en producción.

### De Proceso — Para historias de Épica 6

**[P-1] AC como checklist de verificación pre-commit**
Owner: Development (aplicar desde 6-1)
Acción: Antes de hacer commit, recorrer cada AC explícitamente y marcar ✓.
Criterio: Cero patches de code review por AC no cubierto en implementación inicial.

**[P-2] Casos edge de input en Dev Notes para todo endpoint nuevo**
Owner: Development (aplicar desde 6-1)
Acción: Todo endpoint nuevo incluye en Dev Notes lista de casos edge: null, vacío, tamaño 0, tipo incorrecto, concurrencia. Los guards se implementan antes del commit.
Criterio: Cero patches de tipo "falta guard de validación de input".

**[P-3] Documentar ajustes manuales post-codegen**
Owner: Development
Acción: Si `npm run codegen:api` requiere ajuste manual en `schema.d.ts`, documentar qué y por qué en Dev Notes de la historia correspondiente.
Criterio: No hay desincronizaciones silenciosas entre OpenAPI y tipos de los SPAs.

### Pendiente de Retro 2026-05-25 (confirmar antes de 6-1)

**[R-1] PipelineRunLog/PipelineErrorLog sin retención** — incluir en historia 6-1 como tarea técnica obligatoria. No diferir nuevamente.

---

## Readiness Assessment

| Área | Estado | Acción requerida |
|---|---|---|
| Código 5-6 y 5-7 | ✅ Verde, 0 errores de build | — |
| Tests | ✅ 29 tests verdes confirmados | — |
| Observabilidad errores IA | ⚠️ No verificado en producción | CP-1 antes de 6-1 |
| Flujo extract-kpis con PDF real | ⚠️ No probado manualmente | CP-2 antes de review de 6-1 |
| Blockers técnicos para Épica 6 | ✅ Ninguno | — |

---

## Cierre

Amelia (Developer): "Dos historias de IA bien ejecutadas. El patrón de extracción estructurada con JSON mode, routing multi-proveedor y notas por KPI es nuevo en el proyecto y quedó limpio. Los 10 patches de review son manejables — la mitad son validaciones de input que el checklist de Dev Notes habría atrapado."

Alice (Product Owner): "El hallazgo de observabilidad es el más valioso de esta retro. No estaba en ningún issue tracker — salió porque Jorge lo mencionó."

Charlie (Senior Dev): "Y CP-1 y CP-2 son verificaciones de 30 minutos que cierran riesgos reales antes de Épica 6."

Jorge (Project Lead): "Todo chido."
