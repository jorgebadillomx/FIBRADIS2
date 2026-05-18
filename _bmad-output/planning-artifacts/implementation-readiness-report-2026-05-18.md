---
stepsCompleted:
  - step-01-document-discovery
  - step-02-prd-analysis
  - step-03-epic-coverage-validation
  - step-04-ux-alignment
  - step-05-epic-quality-review
  - step-06-final-assessment
documentsIncluded:
  prd: docs/req/prd.md
  architecture: docs/req/architecture.md
  epics: _bmad-output/planning-artifacts/epics.md
  ux: _bmad-output/planning-artifacts/ux-design-specification.md
  stories:
    - _bmad-output/implementation-artifacts/2-2-home-publica-con-busqueda-global-y-layout.md
    - _bmad-output/implementation-artifacts/2-3-ficha-publica-de-fibra.md
    - _bmad-output/implementation-artifacts/2-4-seo-prerender-y-accesibilidad-wcag-2-1-aa.md
---

# Implementation Readiness Assessment Report

**Date:** 2026-05-18
**Project:** FIBRADIS

## Document Inventory

| Tipo | Archivo |
|------|---------|
| PRD | docs/req/prd.md |
| Architecture | docs/req/architecture.md |
| Epics | _bmad-output/planning-artifacts/epics.md |
| UX Design | _bmad-output/planning-artifacts/ux-design-specification.md |
| Story 1.1 | _bmad-output/implementation-artifacts/1-1-inicializacion-de-la-solucion-y-estructura-del-proyecto.md |
| Story 1.2 | _bmad-output/implementation-artifacts/1-2-backend-api-v1-con-openapi-y-cliente-tipado-para-los-spas.md |
| Story 1.3 | _bmad-output/implementation-artifacts/1-3-autenticacion-jwt-y-autorizacion-por-roles.md |
| Story 1.4 | _bmad-output/implementation-artifacts/1-4-hangfire-health-checks-y-observabilidad-minima.md |
| Story 2.1 | _bmad-output/implementation-artifacts/2-1-catalogo-maestro-de-fibras-con-datos-semilla-iniciales.md |
| Story 2.2 | _bmad-output/implementation-artifacts/2-2-home-publica-con-busqueda-global-y-layout.md |
| Story 2.3 | _bmad-output/implementation-artifacts/2-3-ficha-publica-de-fibra.md |
| Story 2.4 | _bmad-output/implementation-artifacts/2-4-seo-prerender-y-accesibilidad-wcag-2-1-aa.md |

---

## PRD Analysis

### Functional Requirements

| ID | Descripción | Scope |
|----|-------------|-------|
| FR-01 | Catálogo maestro de FIBRAs con ticker único, nombre completo, nombre corto, mercado, moneda, sector, país, estado y configuraciones por emisor | MVP |
| FR-02 | Cada FIBRA conserva URLs oficiales de sitio, inversionistas y reportes | MVP |
| FR-03 | Home pública: encabezado con búsqueda global, carrusel de precios, resumen general del mercado, top movers, ranking rápido y últimas noticias | MVP |
| FR-04 | Buscador global: autocompletar por ticker o nombre, navegar a ficha; estado claro de no encontrado | MVP |
| FR-05 | Home muestra datos cacheados/stale con timestamp visible, escala a degradación crítica según umbral | MVP |
| FR-06 | Ficha pública: encabezado, gráfica histórica, fundamentales vigentes, últimas 8 distribuciones, últimas 10 noticias y reportes oficiales | MVP |
| FR-07 | Ficha pública muestra período de origen de métricas fundamentales y advertencia si último reporte > 2 períodos de antigüedad | MVP |
| FR-08 | Comparador público en `/comparar`: 2-4 FIBRAs, bloques Mercado/Fundamentales/Distribuciones/Score, URL compartible | Growth |
| FR-09 | Comparador tolera valores faltantes por celda, funciona en 360/768/1280px | Growth |
| FR-10 | Mercado: Last Price, cambio diario, volumen, AVG 52S, histórico y distribuciones por FIBRA | MVP |
| FR-11 | Yield anualizado calculado con frecuencia detectada (no frecuencia fija) | MVP |
| FR-12 | Si faltan datos de distribución, yield se muestra como no disponible | MVP |
| FR-13 | Noticias ingeridas cada hora desde Google News RSS con queries configurables por FIBRA y generales | MVP |
| FR-14 | Blocklist global de términos para descartar noticias no relacionadas; eliminar duplicados exactos y probables | MVP |
| FR-15 | Asociación de noticias con cero, una o varias FIBRAs por coincidencia de ticker/nombre | MVP |
| FR-16 | AI_MODE=Off: noticias publicadas sin resumen usando título, fuente, fecha, snippet y enlace original | MVP |
| FR-17 | AI_MODE=Manual: generación de resumen ejecutivo; si falla, noticia publicada como parcial | MVP |
| FR-18 | Módulo Fundamentales soporta tres modos: Off, Manual y Api (sin redeploy al cambiar) | MVP |
| FR-19 | Modo Manual: endpoint de importación JSON para fundamentales con preview y confirmación del operador | MVP |
| FR-20 | Modo Api: detección config-driven de PDFs nuevos, descarga, extracción IA y actualización del histórico | Growth |
| FR-21 | PDF tratado como nuevo solo si representa período no registrado; duplicado queda bajo control manual | MVP |
| FR-22b | Métricas fundamentales faltantes quedan en null sin bloquear procesamiento ni display | MVP |
| FR-22c | Registro de Fundamentales almacena: FIBRA, período, fecha, modo, referencia PDF, estado, campos y resumen | MVP |
| FR-22 | Portafolio: carga de archivos .xlsx/.xls/.csv con tres columnas fijas: Ticker, Qty, AvgCost | MVP |
| FR-23 | Único input de costo es AvgCost; CostoTotalCompra = Qty × AvgCost × (1+factor_comisión); consolidación de lotes | MVP |
| FR-24 | Carga de portafolio síncrona con tabla de errores por fila cuando falla | MVP |
| FR-25 | Confirmación previa al reemplazar portafolio activo; edición inline de Qty/AvgCost con confirmación | MVP |
| FR-26 | Portafolio muestra KPIs agregados: Inversión Total, Valor Total, Plusvalía, Ganancia, Rentas Anuales/Reales, % Rentas | MVP |
| FR-27 | Filas de portafolio expandibles: Mi Posición, Mercado, Fundamentales, Distribuciones | MVP |
| FR-28 | Badge de señal por NAV vs Precio: verde/amarillo/rojo/gris con tooltip explicativo | MVP |
| FR-29 | Oportunidades: dos vistas — universo completo y Promediar Posición | MVP |
| FR-30 | Score de oportunidad: 5 componentes con pesos configurables, normalización por percentil, redistribución de pesos faltantes, sección "datos limitados" | MVP |
| FR-31 | Tres perfiles preconfigurados: Renta, Valor, Conservador; configuración activa persistida por usuario; recálculo en tiempo real | MVP |
| FR-32 | Favoritos: marcar cualquier FIBRA como favorita desde M5, M8 y M9 con estrella; persistido por usuario | MVP |
| FR-33 | FIBRAs favoritas aparecen al inicio de tablas en M8 y M9 | MVP |
| FR-35 | Centro de Procesos: Dashboard, Pipelines, Fundamentales, Catálogo y Configuración | MVP |
| FR-36 | Dashboard operativo: estado, timestamp, duración, conteo de items/errores por pipeline; últimos 5 errores globales | MVP |
| FR-37 | Pipelines: historial de corridas con detalle (Mercado y Noticias), botón Run now con auditoría | MVP |
| FR-38 | Sección Fundamentales: formulario importación JSON con validación/preview/confirmación, adjunto PDF, historial por FIBRA con Reprocess | MVP |
| FR-39 | Sección Catálogo: agregar, editar y desactivar FIBRAs; soft delete | MVP |
| FR-40 | Sección Configuración: commission_factor, avg_periods, blocklist, AI_MODE, cadencias de pipelines; auditoría de cambios | MVP |
| FR-41 | Estados operativos por item: detected, pending, processing, processed, partial, error | MVP |
| FR-42 | Mundo público sin auth; mundo privado con auth; Ops restringido a AdminOps | MVP |
| FR-43 | % portafolio calculado por monto invertido según DR-12 | MVP |
| FR-44 | Factor comisión configurable desde Ops sin redeploy | MVP |
| FR-45 | Promedios históricos usando últimos N (default 4) períodos por FIBRA; N configurable desde Ops | MVP |
| FR-46 | Portafolio muestra solo posiciones con al menos un título activo | MVP |
| FR-47 | Portafolio y dashboard unificados en `/portafolio`; no existe `/dashboard` separado | MVP |
| FR-48 | Tabla portafolio: vista compacta, columnas configurables por checkboxes, multi-sort, persistencia de configuración por usuario | MVP |
| FR-49 | Catálogo almacena variantes de nombre para queries Google News RSS; editable desde Ops | MVP |
| FR-50 | Home muestra las 10 noticias más recientes sin importar asociación a FIBRA | MVP |
| FR-51 | Vista universo Oportunidades: posición en ranking, nombre/ticker, score, valores de componentes, badge, filtros | MVP |
| FR-52 | Vista Promediar Posición: costo promedio, precio actual, diferencia, score y simulador de promedio | MVP |
| FR-53 | Edición inline de Qty/AvgCost; Enter confirma y guarda; Escape cancela; eliminación requiere confirmación | MVP |
| FR-54 | Oportunidades monitorea cobertura de precios; advertencia "universo degradado" >30%; suspensión ranking si cobertura <50% | MVP |

**Total FRs MVP:** 48 | **Total FRs Growth:** 3 (FR-08, FR-09, FR-20)

---

### Non-Functional Requirements

| ID | Categoría | Descripción |
|----|-----------|-------------|
| NFR-01 | Rendimiento | Home pública < 2 seg P95 con datos cacheados/precargados |
| NFR-02 | Rendimiento | Dashboard privado < 1 seg P95 con datos precalculados |
| NFR-03 | Frescura | Pipeline de mercado cada 15 min dentro de horario BMV (8:15am-3:15pm CDMX días hábiles) |
| NFR-04 | Frescura | Clasificación de frescura: Fresh (≤20 min), Stale (20 min–6 h), Crítico (>6 h), Fuera-de-horario |
| NFR-05 | Rendimiento | Pipeline noticias con cadencia default 1 hora; cambio desde Ops sin redeploy |
| NFR-06 | Almacenamiento | Snapshots diarios de mercado conservados 90 días calendario |
| NFR-07 | Almacenamiento | PDFs no eliminados automáticamente en MVP; política de retención definida explícitamente |
| NFR-08 | Resiliencia | Toda vista tolera datos faltantes (`—`, `parcial`, `sin datos`, `no evaluable`) sin errores fatales de UI |
| NFR-09 | Escalabilidad | Soportar ≥30 FIBRAs activas y ≥5 años de histórico sin rediseñar entidades base |
| NFR-10 | Trazabilidad | Toda entidad relevante conserva fuente, captured_at, status y error_reason |
| NFR-11 | Seguridad | Auth y autorización por roles User/AdminOps; pruebas positivas y negativas |
| NFR-12 | Auditoría | Cambios de schedule, AI_MODE, reprocesos, retries y configuraciones auditados con actor/fecha/antes-después |
| NFR-13 | Observabilidad | Logs estructurados 100% corridas de pipeline, correlation ID por solicitud/job, health checks separados |
| NFR-14 | API | Contrato documentado y versionado; detección de cambios incompatibles antes de liberar |
| NFR-15 | Responsive | Navegación principal visible y sin overflow horizontal en 360/768/1280px |
| NFR-16 | Despliegue | Despliegue único: mundo público + privado + background; idempotencia y exclusión lógica |

**Total NFRs:** 16

---

### Requisitos Adicionales (Constraints, PT, DR)

- **PT-01 a PT-11**: Plataforma web con dos SPAs React; un solo backend; procesos asíncronos; carga de archivos; responsive; OpenAPI; ajuste de schedules sin redeploy; AI_MODE compatible; soporte browsers Chrome/Edge/Safari/Firefox últimas 2 versiones estables; SEO en superficies públicas; WCAG 2.1 AA
- **DR-01 a DR-15**: Reglas de dominio sobre catálogo maestro, no-trading, trazabilidad de fundamentales, portafolio como estado actual, frecuencia de distribuciones no asumida, no inventar datos IA, noticias sin asociación obligatoria, score con redistribución de pesos, estados UI explícitos, separación de acceso, AVG N=4 períodos configurable, peso por monto invertido, factor comisión, jerarquía de fuente para Dividend Yield, horario BMV pipeline de mercado

### PRD Completeness Assessment

- PRD **completo y bien estructurado**: contiene visión, alcance, journeys de usuario, requisitos de dominio, FRs numerados con trazabilidad a journeys/SC, NFRs con criterios de verificación, y distinción clara MVP vs Growth.
- **54 FRs numerados** (FR-01 a FR-54, sin FR-34 y algunos numerados con letras como FR-22b/c).
- **16 NFRs numerados** (NFR-01 a NFR-16).
- Trazabilidad explícita mediante `Trace:` en cada FR y NFR.
- Los módulos M6 (comparador) y M11 (alertas) están explícitamente diferidos a Growth.

---

## Epic Coverage Validation

### Coverage Matrix

| FR | Épica | Historia(s) que lo implementan | Estado |
|----|-------|--------------------------------|--------|
| FR-01 | Épica 2 | 2.1 Catálogo maestro | ✅ Cubierto |
| FR-02 | Épica 2 | 2.1 Catálogo maestro (URLs oficiales) | ✅ Cubierto |
| FR-03 | Épica 2 | 2.2 Home pública con búsqueda global | ✅ Cubierto |
| FR-04 | Épica 2 | 2.2 Home pública (autocompletar) | ✅ Cubierto |
| FR-05 | Épica 3 | 3.2 Clasificación de frescura en UI | ✅ Cubierto |
| FR-06 | Épica 2 | 2.3 Ficha pública de FIBRA | ✅ Cubierto |
| FR-07 | Épica 2 | 2.3 Ficha pública (período de origen, advertencia antigüedad) | ✅ Cubierto |
| FR-08 | GROWTH | N/A — excluido MVP | ⏭ Growth |
| FR-09 | GROWTH | N/A — excluido MVP | ⏭ Growth |
| FR-10 | Épica 3 | 3.1 Pipeline de mercado + 3.3 Historial | ✅ Cubierto |
| FR-11 | Épica 3 | 3.3 Historial, yield anualizado | ✅ Cubierto |
| FR-12 | Épica 3 | 3.3 Yield no disponible | ✅ Cubierto |
| FR-13 | Épica 4 | 4.1 Ingesta RSS, queries por FIBRA y generales | ✅ Cubierto |
| FR-14 | Épica 4 | 4.1 Blocklist y deduplicación | ✅ Cubierto |
| FR-15 | Épica 4 | 4.2 Asociación de noticias con FIBRAs | ✅ Cubierto |
| FR-16 | Épica 4 | 4.3 AI_MODE=Off | ✅ Cubierto |
| FR-17 | Épica 4 | 4.3 AI_MODE=Manual | ✅ Cubierto |
| FR-18 | Épica 5 | 5.2 Importación fundamentales (tres modos) | ✅ Cubierto |
| FR-19 | Épica 5 | 5.2 Endpoint importación JSON | ✅ Cubierto |
| FR-20 | GROWTH | N/A — modo Api excluido MVP | ⏭ Growth |
| FR-21 | Épica 5 | 5.2 Detección de PDF nuevo por período | ✅ Cubierto |
| FR-22 | Épica 6 | 6.1 Carga y validación del portafolio | ✅ Cubierto |
| FR-22b | Épica 5 | 5.2 Métricas faltantes en null | ✅ Cubierto |
| FR-22c | Épica 5 | 5.2 Modelo de almacenamiento por registro | ✅ Cubierto |
| FR-23 | Épica 6 | 6.1 Carga (AvgCost, CostoTotalCompra, consolidación de lotes) | ✅ Cubierto |
| FR-24 | Épica 6 | 6.1 Validación síncrona con tabla de errores | ✅ Cubierto |
| FR-25 | Épica 6 | 6.1 Confirmación reemplazo; 6.4 Edición inline | ✅ Cubierto |
| FR-26 | Épica 6 | 6.2 KPIs agregados del portafolio | ✅ Cubierto |
| FR-27 | Épica 6 | 6.3 Filas expandibles con cuatro secciones | ✅ Cubierto |
| FR-28 | Épica 6 | 6.3 Badge de señal NAV vs Precio | ✅ Cubierto |
| FR-29 | Épica 7 | 7.1 Ranking universo + 7.2 Vista Promediar Posición | ✅ Cubierto |
| FR-30 | Épica 7 | 7.1 Score 5 componentes, redistribución, umbral 3/5 | ✅ Cubierto |
| FR-31 | Épica 7 | 7.1 Tres perfiles preconfigurados, pesos persistidos | ✅ Cubierto |
| FR-32 | Épica 7 | 7.4 Favoritos — marcar desde M5/M8/M9 | ✅ Cubierto |
| FR-33 | Épica 7 | 7.4 Favoritos destacados al inicio de tablas M8 y M9 | ✅ Cubierto |
| FR-35 | Épica 5 | 5.1 Dashboard operativo (5 secciones Centro de Procesos) | ✅ Cubierto |
| FR-36 | Épica 5 | 5.1 Dashboard operativo con estado y errores de pipelines | ✅ Cubierto |
| FR-37 | Épica 5 | 5.1 Historial de corridas y Run now auditado | ✅ Cubierto |
| FR-38 | Épica 5 | 5.2 Formulario importación JSON con preview y confirmación | ✅ Cubierto |
| FR-39 | Épica 5 | 5.3 Gestión catálogo desde Ops | ✅ Cubierto |
| FR-40 | Épica 5 | 5.4 Configuración operativa sin redespliegue | ✅ Cubierto |
| FR-41 | Épica 5 | 5.2 Estados operativos por item | ✅ Cubierto |
| FR-42 | Épica 1 | 1.3 Auth JWT y autorización por roles | ✅ Cubierto |
| FR-43 | Épica 6 | 6.2 % portafolio por monto invertido (DR-12) | ✅ Cubierto |
| FR-44 | Épica 6 | 6.2 Factor comisión configurable (DR-13) | ✅ Cubierto |
| FR-45 | Épica 6 | 6.2 AVG últimos N períodos configurable (DR-11) | ✅ Cubierto |
| FR-46 | Épica 6 | 6.1 Solo posiciones activas en portafolio | ✅ Cubierto |
| FR-47 | Épica 6 | 6.2 Pantalla unificada /portafolio sin /dashboard | ✅ Cubierto |
| FR-48 | Épica 6 | 6.2 Columnas configurables, multi-sort, persistencia | ✅ Cubierto |
| FR-49 | Épica 2 / 5 | 2.1 (schema) + 5.3 (edición desde Ops) | ✅ Cubierto |
| FR-50 | Épica 4 | 4.2 10 noticias recientes en Home | ✅ Cubierto |
| FR-51 | Épica 7 | 7.1 Ranking universo (score desglosado, sección datos limitados) | ⚠️ **PARCIAL** |
| FR-52 | Épica 7 | 7.2 Vista Promediar Posición con simulador | ✅ Cubierto |
| FR-53 | Épica 6 | 6.4 Edición inline con Enter/Escape, eliminación con confirmación | ✅ Cubierto |
| FR-54 | Épica 7 | 7.3 Monitoreo cobertura y ranking degradado | ✅ Cubierto |

**Total FRs MVP:** 48 | **Cubiertos:** 47 | **Parciales:** 1 (FR-51) | **Growth:** 3 (FR-08, FR-09, FR-20)

---

### Missing Requirements

#### ⚠️ Cobertura Parcial

**FR-51 — Filtros del universo de Oportunidades (MEDIUM)**

El PRD define explícitamente cinco filtros disponibles: "solo FIBRAs con fundamentales cargados, yield mínimo, LTV máximo, sector y solo FIBRAs con precio activo."

La Historia 7.1 cubre el ranking, el desglose visual del score, la sección "datos limitados" y los perfiles de pesos. **Sin embargo, ningún criterio de aceptación de la Historia 7.1 menciona ni prueba los cinco filtros especificados en FR-51.**

- **Impacto:** Los filtros son parte del MVP y mejoran materialmente la usabilidad del módulo de Oportunidades. Sin ellos, el usuario no puede acotar el ranking por sector, yield mínimo o disponibilidad de fundamentales.
- **Recomendación:** Agregar en Historia 7.1 (o una historia 7.1b) un criterio de aceptación específico: "Dado que activo el filtro 'solo FIBRAs con fundamentales cargados', entonces el ranking se reduce solo a FIBRAs que tienen al menos un registro de fundamentales procesado."

---

#### ⚠️ Requisitos no mapeados a FRs (LOW)

**DR-14 — Jerarquía de fuente para Dividend Yield con indicador de fuente en UI**

DR-14 establece: "Cuando el yield se muestra en UI debe incluir un indicador visible de la fuente utilizada (reporte oficial o mercado) para que el usuario conozca la confiabilidad del dato."

Esta regla de dominio no está trazada a ningún FR específico y por tanto no aparece en los criterios de aceptación de ninguna historia. Las historias que muestran yield (2.3 Ficha pública, 6.3 Portafolio expandible, 7.1 Oportunidades) no incluyen el indicador de fuente.

- **Recomendación:** Agregar un AC en las historias donde se muestra yield: "Dado que el yield se muestra en UI, entonces existe un indicador visible (badge/tooltip) que indica si proviene de Fundamentales (reporte oficial) o de Yahoo Finance (distribuciones)."

**NFR-01 — Performance de Home pública (< 2 seg P95)**

NFR-01 está asignado a Épica 2 en el mapa de cobertura, pero ninguna historia de la Épica 2 incluye un criterio de aceptación que valide el tiempo de respuesta. NFR-02 (Dashboard < 1 seg) sí está explícitamente en los ACs de Historia 6.2.

- **Recomendación:** Agregar AC en Historia 2.2: "Dado que cargo la Home con datos precacheados, entonces la respuesta del API de Home es inferior a 2 segundos en P95."

**NFR-09 — Soporte de ≥30 FIBRAs activas y ≥5 años de histórico**

La Historia 2.1 siembra solo 10+ FIBRAs. NFR-09 no tiene historia ni criterio de aceptación que pruebe la capacidad con 30+ FIBRAs o 5+ años de histórico.

- **Recomendación:** Agregar en la suite de pruebas de integración (no necesariamente en una historia separada) un test de regresión de carga que valide cálculos con 30 FIBRAs y 20 períodos de datos.

---

### Coverage Statistics

| Métrica | Valor |
|---------|-------|
| Total FRs PRD (MVP) | 48 |
| FRs cubiertos completamente | 47 |
| FRs cubiertos parcialmente | 1 (FR-51) |
| FRs Growth (excluidos MVP) | 3 |
| Cobertura FR MVP | **97.9%** |
| Total NFRs | 16 |
| NFRs con AC explícito en historias | 11 |
| NFRs cross-cutting (validados implícitamente) | 5 (NFR-08, NFR-09, NFR-01, NFR-07, NFR-16) |
| Cobertura NFR explícita | **68.8%** |

---

## UX Alignment Assessment

### UX Document Status

**Encontrado:** `_bmad-output/planning-artifacts/ux-design-specification.md` (2026-05-15)

El documento cubre: experiencia definitoria, respuesta emocional, sistema de diseño, inventario de 18 pantallas, 20+ componentes, 5 patrones de interacción, estrategia responsive (360/768/1280px) y accesibilidad WCAG 2.1 AA. Fue creado **después** de los épicos y usando el PRD, arquitectura y epics.md como insumos — esto es relevante porque los épicos se construyeron **sin** este UX spec.

---

### Alineación UX ↔ PRD ✅

Los siguientes requisitos del UX spec están bien alineados y trazados a FRs/NFRs:

| Requisito UX | Trazabilidad |
|--------------|-------------|
| Freshness badges (Fresh/Stale/fuera-horario/crítico) | NFR-04, FR-05 |
| Responsive 360/768/1280px sin overflow horizontal | NFR-15, PT-05 |
| WCAG 2.1 AA completo (teclado, contraste, ARIA) | PT-11 |
| Edición inline Qty/AvgCost (Enter guarda, Escape cancela) | FR-53, FR-25 |
| Score sliders con recálculo en tiempo real | FR-30, FR-31 |
| Favoritos con 1 clic, efecto inmediato en todas las superficies | FR-32, FR-33 |
| Portafolio unificado `/portafolio` sin `/dashboard` | FR-47 |
| Datos degradados con `—` nunca `0` | NFR-08, DR-09 |
| Score con desglose expandible en ≤ 1 clic | FR-30, FR-51 |

---

### Pantallas UX sin historia ni FR correspondiente ⚠️

El UX spec define 18 pantallas. Las siguientes **no tienen story ni FR explícito** en los épicos:

| ID Pantalla | Ruta | Prioridad |
|-------------|------|-----------|
| S-02 | `/mercado` — tabla completa del universo con last price, yield, volumen | MEDIUM — FR-10 cubre el módulo de mercado pero no especifica una ruta `/mercado` pública como pantalla independiente |
| S-03 | `/catalogo` — lista de emisores con metadatos y filtros | MEDIUM — FR-01 cubre el catálogo como entidad, pero no una página pública de browsing |
| S-04 | `/noticias` — feed general de noticias con filtros por emisor | LOW — las noticias existen en Home (FR-50) y ficha (FR-06), pero no una ruta `/noticias` dedicada |
| S-07 | `/registro` — alta de cuenta y plan de suscripción | HIGH — el modelo freemium requiere registro pero no hay ningún FR ni story que lo implemente |
| S-11 | `/perfil` — configuración de cuenta y preferencias de score | MEDIUM — gestión de cuenta no cubierta en ningún FR ni story |
| S-12 | `/suscripcion` — estado del plan, upgrade/downgrade | MEDIUM — el modelo de suscripción se menciona en el PRD como modelo de negocio pero no tiene FR ni story |
| S-17 | `/ops/distribuciones` — historial de distribuciones por FIBRA, corrección manual | LOW — distribuciones operativas en Ops no tienen story dedicada |

---

### Micro-interacciones UX no capturadas en story ACs ⚠️

Las siguientes especificaciones del UX spec tienen detalle de interacción que **no aparece en ningún criterio de aceptación**:

1. **PI-02 — Redistribución automática de pesos al mover slider:** "Los pesos restantes se redistribuyen automáticamente para sumar 100%." El FR-30 menciona redistribución de pesos cuando *falta un componente de datos*, pero no la redistribución de sliders al moverlos manualmente. Historia 7.1 no incluye este AC.

2. **PI-04 Paso 4 — Preview de posiciones antes de confirmar carga:** "preview de posiciones normalizadas antes de confirmar." Historia 6.1 cubre validación de errores pero no el preview de posiciones válidas antes de confirmar el reemplazo.

3. **Edición inline en mobile:** "El InlineEditor no funciona en mobile — la edición en mobile se hace mediante un formulario simple en bottom sheet." Ninguna historia especifica este comportamiento diferencial por breakpoint.

4. **Anclas de navegación en ficha pública (S-05):** Header sticky con anclas (Mercado · Fundamentales · Distribuciones · Noticias · Reportes). Historia 2.3 no menciona navegación por anclas dentro de la ficha.

5. **Bottom bar "Ver en Oportunidades"** en portafolio cuando hay posición seleccionada. No cubierto en ninguna historia.

---

### Advertencias de Alineación

⚠️ **ALTA — S-07 Registro de usuario no tiene story:** El modelo freemium requiere flujo de alta de cuenta para acceder al mundo privado. Sin esto, FR-42 (auth) funciona para usuarios ya existentes pero no hay forma de crear nuevas cuentas desde la UI. Si FIBRADIS MVP requiere onboarding de usuarios, esto necesita una historia.

⚠️ **MEDIA — S-02/S-03 Rutas públicas adicionales:** La UX define `/mercado` y `/catalogo` como pantallas públicas independientes. Si no están en scope para MVP, deben marcarse explícitamente como Growth en el UX spec para evitar ambigüedad durante implementación.

⚠️ **BAJA — Épicos no usaron UX spec como insumo:** Los épicos fueron generados antes del UX spec. Los UX-DRx en epics.md son derivados del PRD, no del spec real. Los 5 gaps de micro-interacción listados arriba son consecuencia directa de este orden. **No requiere regenerar los épicos**, pero sí agregar los ACs faltantes en las historias afectadas.

---

## Epic Quality Review

### Validación de Valor para el Usuario

| Épica | Orientación | ¿Valor de usuario? | Veredicto |
|-------|-------------|-------------------|-----------|
| Épica 1: Fundación, Infraestructura y Acceso | Técnica/infraestructura | Parcial (auth sí, setup no) | ✅ Aceptable — greenfield necesita fundación; la autenticación entrega valor real a AdminOps |
| Épica 2: Catálogo Maestro y Descubrimiento Público | Orientada al usuario | ✅ Sí — visitante puede explorar el universo de FIBRAs | ✅ |
| Épica 3: Mercado y Frescura de Datos | Orientada al usuario | ✅ Sí — precios actualizados con estado de frescura | ✅ |
| Épica 4: Noticias y Contenido | Orientada al usuario | ✅ Sí — noticias relevantes de FIBRAs | ✅ |
| Épica 5: Centro de Procesos y Fundamentales | Orientada al operador | ✅ Sí — AdminOps opera y monitorea el sistema | ✅ |
| Épica 6: Portafolio Unificado | Orientada al usuario | ✅ Sí — análisis personal de inversión | ✅ |
| Épica 7: Oportunidades y Favoritos | Orientada al usuario | ✅ Sí — identificar oportunidades con score configurable | ✅ |

---

### Validación de Independencia de Épicas

| Secuencia | ¿Funciona con épicas previas? | Dependencias detectadas | Estado |
|-----------|-------------------------------|------------------------|--------|
| Épica 1 | Independiente (greenfield) | Ninguna | ✅ |
| Épica 2 | Usa Épica 1 (auth baseline, API) | Épica 1 → 2 ✅ | ✅ |
| Épica 3 | Usa Épica 1 + 2 (FIBRAs en catálogo para actualizar precios) | Épicas 1-2 → 3 ✅ | ✅ |
| Épica 4 | Usa Épica 1 + 2 (catálogo para asociar noticias) | Épicas 1-2 → 4 ✅ | ✅ |
| Épica 5 | Usa Épica 1 (auth AdminOps) + 2 (catálogo a gestionar) | Épicas 1-2 → 5 ✅ | ✅ |
| Épica 6 | Usa Épica 1 (auth User) + 2 (catálogo para validar tickers) + 3 (precios de mercado) + 5 (fundamentales para badges NAV y datos expandidos) | Épicas 1-3, 5 → 6 | ⚠️ Épicas 3 y 5 no son prerequisito estricto — las historias manejan estados de grading nulo/degradado |
| Épica 7 | Usa Épicas 1-6 | Épicas 1-6 → 7 ✅ | ✅ |

**Nota sobre Épica 6:** Las historias 6.2 y 6.3 funcionan con datos vacíos de Épicas 3 (precios `—`) y 5 (NAV badge gris). El degraded state handling está explícitamente en los ACs. No es una violación de independencia — es diseño correcto.

---

### Revisión de Calidad de Historias — Hallazgos por Severidad

#### 🔴 Violaciones Críticas

**Ninguna encontrada.**

---

#### 🟠 Issues Importantes

**Issue M-01 — Historia 7.1: Filtros de FR-51 ausentes en ACs**

Historia 7.1 cubre el ranking, desglose de score y perfiles configurables, pero **ningún AC prueba los 5 filtros del universo** definidos en FR-51 (solo FIBRAs con fundamentales, yield mínimo, LTV máximo, sector, solo FIBRAs con precio activo).

- Historia afectada: Historia 7.1
- Remediación: Agregar AC: "Dado que activo el filtro 'yield mínimo' con 5%, cuando la tabla se actualiza, entonces solo aparecen FIBRAs cuyo yield calculado es ≥ 5%."

**Issue M-02 — Historia 5.2: Conexión fundamentales → ficha pública no explícita en ACs**

La Historia 5.2 dice "los datos se vuelven visibles en el perfil público de la FIBRA bajo la etiqueta del trimestre correcto." Pero la Historia 2.3 fue implementada con datos de seed/estáticos. **No hay historia que explícitamente conecte el módulo de importación de Épica 5 con la visualización dinámica de la Épica 2.** El implementador debe inferir que al importar en Epic 5, la ficha de Epic 2 ya leerá esos datos dinámicamente.

- Remediación: Clarificar en el Dev Notes de Historia 5.2 que la integración con la ficha pública es automática por diseño de la capa Application (el endpoint de fundamentales de la ficha ya lee del mismo repositorio que la importación).

**Issue M-03 — Épica 2 Historias 2.2 y 2.3: Criterio "done" ambiguo con estados placeholder**

Las Historias 2.2 y 2.3 incluyen secciones que explícitamente quedan en estado placeholder (carrusel, top movers, noticias en 2.2; precio, noticias en 2.3) hasta que se completen las Épicas 3 y 4. Esto es correcto de diseño, pero el criterio "done" de cada historia no es el producto final — es un esqueleto funcional. Si el equipo usa "done" para medir avance de sprint, estas historias pueden generar confusión.

- Remediación: Agregar una nota explícita en cada historia: "Se considera done cuando todos los estados placeholder están implementados y verificados correctamente. La población de datos reales llega en Épicas 3 y 4."

---

#### 🟡 Observaciones Menores

**Minor-01 — Historia 1.4: Falta AC para restricción de auth en `/hangfire`**

El AC dice "visible en el dashboard de Hangfire en `/hangfire` (acceso solo AdminOps)." Esta es una restricción funcional pero no hay un AC que pruebe: "Dado que tengo JWT de rol User, cuando accedo a `/hangfire`, entonces recibo 403."

**Minor-02 — Historia 7.1: Falta estado vacío cuando NO hay FIBRAs con precio**

Los ACs cubren el caso de "datos limitados" (< 3 componentes calculables) y "no evaluable" (ningún componente con precio). Pero no hay AC para el estado inicial cuando el sistema tiene FIBRAs pero ninguna tiene precio aún — ¿qué ve el usuario en Oportunidades en ese momento?

**Minor-03 — Timing de creación de schemas por módulo no explicitado**

Los épicos asumen que cada módulo crea sus propias tablas cuando las necesita (patrón correcto). Sin embargo, ninguna historia especifica explícitamente las migraciones EF Core que crea. Esto funciona bien con el patrón de dev-story de BMAD (el implementador crea las migraciones necesarias) pero podría causar fricciones si el implementador no conoce qué tablas crear.

**Minor-04 — Épica 3, Historia 3.1: Typo en AC**

"todos los demás FIBRAs" — debe ser "todas las demás FIBRAs" (género femenino).

---

### Checklist de Conformidad por Épica

| Criterio | E1 | E2 | E3 | E4 | E5 | E6 | E7 |
|----------|----|----|----|----|----|----|-----|
| ✅ Entrega valor de usuario | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| ✅ Funciona con épicas previas | N/A | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| ✅ Historias tamaño apropiado | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| ✅ Sin dependencias hacia adelante | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| ✅ Tablas creadas cuando se necesitan | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| ✅ ACs en formato Given/When/Then | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| ✅ ACs cubren casos de error | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ⚠️ Minor-02 |
| ✅ Trazabilidad a FRs mantenida | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ⚠️ M-01 filtros |

---

## Resumen y Recomendaciones

### Estado General de Readiness

> ## ✅ LISTO PARA IMPLEMENTAR — CON CONDICIONES

El proyecto tiene fundamentos de planificación sólidos: alta cobertura de requisitos (97.9% FRs), sin dependencias hacia adelante entre historias, estructura de épicos correctamente secuenciada, y artefactos completos (PRD + Arquitectura + Épicos + UX Spec). **Los issues encontrados son todos aditivos** — requieren agregar ACs, aclarar scope o documentar decisiones, no rediseñar ni reescribir épicos.

---

### Issues Que Requieren Acción Antes de Implementar

#### 🔶 ALTA PRIORIDAD — Aclarar en epics.md o en las historias afectadas

**1. S-07 Registro de usuario — scope indefinido**

No hay ningún FR ni story para crear cuentas de usuario. El modelo freemium requiere registro. Acción requerida: **decidir explícitamente** si en MVP (a) las cuentas se crean por AdminOps desde Ops/base de datos directamente, o (b) se agrega una historia de registro de cuenta. Si es (a), documentarlo en epics.md como decisión de scope MVP.

**2. FR-51 Filtros del universo de Oportunidades — falta AC en Historia 7.1**

Los 5 filtros (fundamentales cargados, yield mínimo, LTV máximo, sector, precio activo) están en el PRD pero no en ningún criterio de aceptación. Acción: agregar ACs a Historia 7.1 o crear Historia 7.1b para filtros.

#### 🔷 MEDIA PRIORIDAD — Agregar antes de crear story files para las historias afectadas

**3. DR-14 Indicador de fuente para Dividend Yield**

La jerarquía de fuente (Fundamentales → Yahoo Finance → no disponible) con indicador de fuente en UI no está mapeada a ningún FR ni AC. Afecta Historias 2.3, 6.3 y 7.1. Acción: agregar AC específico en cada historia donde se muestra yield.

**4. NFR-01 Validación de performance de Home (< 2 seg P95)**

NFR-01 está asignado a Épica 2 pero ninguna historia lo prueba. Historia 2.2 debería incluir AC de tiempo de respuesta. Acción: agregar AC en Historia 2.2.

**5. PI-02 Redistribución automática de sliders de peso en Historia 7.1**

El comportamiento "los pesos restantes se redistribuyen automáticamente para sumar 100%" al mover un slider está en el UX spec pero no en los ACs. Acción: agregar AC a Historia 7.1.

**6. Rutas S-02/S-03/S-04 en UX spec — scope MVP no clarificado**

Las páginas `/mercado`, `/catalogo` y `/noticias` aparecen en el UX spec como pantallas independientes sin stories correspondientes. Acción: clarificar en el UX spec si son MVP o Growth. Si son MVP, crear historias. Si son Growth, marcarlas explícitamente.

#### 🔹 BAJA PRIORIDAD — Recomendados pero no bloqueantes

**7. Historia 1.4:** Agregar AC para verificar que `/hangfire` retorna 403 para rol User.

**8. Historia 7.1:** Agregar AC para el estado vacío cuando ninguna FIBRA tiene precio activo.

**9. Historia 5.2:** Agregar nota en Dev Notes que la visibilidad en ficha pública es automática por diseño de la capa Application (mismo repositorio de fundamentales).

**10. Historias 2.2 y 2.3:** Clarificar en la definición de "done" que las secciones con placeholder son estados válidos y verificables.

---

### Siguientes Pasos Recomendados

1. **Resolver item #1 (registro de usuario)** — es la única decisión de scope que puede afectar el flujo de onboarding del producto.
2. **Agregar ACs faltantes** (#2 filtros FR-51, #3 DR-14, #4 NFR-01, #5 PI-02) en epics.md antes de crear story files para esas historias.
3. **Clarificar scope de rutas UX** (#6) con una revisión de 30 minutos del UX spec.
4. **Proceder a implementación** comenzando por la Épica 1 con `/bmad-create-story` para Historia 1.1.

---

### Nota Final

Esta evaluación identificó **10 issues** en 4 categorías:
- **0 críticos** (ninguno bloquea implementación)
- **2 de alta prioridad** (decisión de scope + gap de ACs crítico)
- **4 de media prioridad** (ACs faltantes en historias específicas)
- **4 de baja prioridad** (mejoras de calidad no bloqueantes)

La cobertura de FRs es del **97.9% para MVP** con todos los Growth-only correctamente excluidos. La arquitectura de épicos es sólida, el secuenciamiento es correcto y los artefactos de planificación (PRD, arquitectura, épicos, UX) están completos y alineados. Con las correcciones de alta prioridad aplicadas, el proyecto está en condiciones óptimas para iniciar implementación.

---
*Evaluación generada el 2026-05-18. Evaluador: Claude Sonnet 4.6 vía bmad-check-implementation-readiness.*

