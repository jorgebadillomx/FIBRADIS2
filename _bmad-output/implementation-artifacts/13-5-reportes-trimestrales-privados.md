# Story 13.5: Reportes trimestrales privados (mover análisis IA de la ficha a `/reportes`)

Status: ready-for-dev

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a **inversionista autenticado de FIBRADIS**,
I want **una página privada `/reportes` donde seleccione una FIBRA y un período (de un combo con todos sus registros de fundamentales) y vea el reporte trimestral completo — datos fundamentales (KPIs) + análisis IA (resumen analítico, señales, alertas, perspectiva)**,
so that **pueda consultar el análisis profundo por trimestre de cualquier FIBRA en un solo lugar, mientras la ficha pública queda más ligera y el análisis premium queda detrás del login**.

## Contexto del problema

Hoy la **ficha pública** `/fibras/:slug` muestra, dentro de su sección de fundamentales (`FundamentalesSection`), tanto la **tabla de KPIs** como el **análisis IA** (resumen analítico, señales operativas/financieras, alertas de riesgo, perspectiva del analista). Decisión de producto:

1. **Mover el análisis IA a una página privada nueva `/reportes`**, que son los **reportes trimestrales** de las FIBRAs.
2. En `/reportes`: **seleccionar una FIBRA** → aparece un **combo con todos sus registros de fundamentales** (períodos/trimestres) → al elegir uno se muestra el reporte completo de ese período (KPIs + análisis IA).
3. **Quitar el análisis IA de la ficha pública** (`/fibras/:slug`): la ficha **conserva la tabla KPI básica** y el selector de período, pero ya no muestra resumen analítico ni señales/alertas/perspectiva.

**Decisión confirmada con el usuario:** la ficha pública pierde el **análisis IA completo** (resumen analítico + señales operativas + señales financieras + alertas de riesgo + perspectiva del analista), **conservando la tabla KPI**. El análisis pasa a ser contenido **privado** (tras login) en `/reportes`.

### Dependencias y coordinación

- **13-6 (Portafolio público):** su landing describe esta página de Reportes entre las capacidades de la plataforma. Coordinación de contenido, no de código.
- **`public-navigation.ts`:** esta historia añade "Reportes" al desplegable **"Mi inversión"**. Ese archivo lo tocan también **13-1** (revertir "Más") y **13-6** (botón "Portafolio"). **Coordinar orden de merge** (las tres tocan `public-navigation.ts`/`PublicLayout.tsx`).

## Acceptance Criteria

### A. Página privada `/reportes` (routing + nav + explicación)

1. Nueva ruta **`/reportes`** bajo `<ProtectedRoute>` en `routes.tsx` (privada, **lazy**). Un usuario anónimo que navegue a `/reportes` es redirigido a `/login?redirect=/reportes` (comportamiento existente del guard, sin código nuevo).
2. Se añade **"Reportes" → `/reportes`** al desplegable **"Mi inversión"** (`MAIN_INVESTMENT_LINKS` en `public-navigation.ts`), visible en desktop y móvil **solo** para `status === 'authenticated'` (auth-gating real, nunca CSS). Coordinar con 13-1/13-6 (mismo archivo).
3. La página incluye un **encabezado + texto introductorio** que explica qué son: los **reportes trimestrales** de las FIBRAs (datos fundamentales + análisis), y cómo usarla (selecciona FIBRA y período).

### B. Selección de FIBRA + combo de períodos

4. **Selector de FIBRA**: reutilizar el patrón existente de búsqueda/selección de FIBRA (ver `HerramientasPage` — búsqueda por ticker/empresa con sugerencias — o la lista de fibras del catálogo). Al seleccionar una FIBRA se cargan sus períodos vía `GET /api/v1/fundamentals/{ticker}/periods`.
5. **Combo de períodos**: un `<select>`/combo con **todos los registros de fundamentales** (períodos/trimestres) de la FIBRA seleccionada, ordenados del más reciente al más antiguo. Al elegir un período se muestra el reporte de ese período. Por defecto se preselecciona el período más reciente.
6. **Estados explícitos** (regla del proyecto — nunca inferidos): sin FIBRA seleccionada (prompt de selección), cargando, FIBRA sin fundamentales (empty state), y error de carga.

### C. Contenido del reporte (KPIs + análisis IA)

7. Para la FIBRA + período seleccionados, mostrar:
   - **Tabla KPI**: Cap Rate, NAV/CBFI, LTV, NOI Margin, FFO Margin, Quarterly Distribution (+ período y fecha de captura).
   - **Análisis IA**: resumen analítico (markdown), señales operativas, señales financieras, alertas de riesgo (styling de alerta), perspectiva del analista (`investorTakeaway`).
   - Reutilizar la lógica/markup existente de `FundamentalesSection` extrayéndola a componentes compartidos (ver Dev Notes), **sin reescribir** el renderizado de markdown/señales.

### D. Quitar el análisis IA de la ficha pública

8. En `FundamentalesSection.tsx` (ficha pública `/fibras/:slug`): **eliminar** los bloques de análisis IA — resumen analítico (≈ l.137-154), señales operativas (≈ l.156-161), señales financieras (≈ l.163-168), alertas de riesgo (≈ l.170-178) y perspectiva del analista (≈ l.180-189). **Conservar** la tabla KPI (≈ l.102-133), el período/fecha de captura y el selector de período.
9. La ficha pública **ya no consume ni expone** el análisis IA (ver E: el endpoint público deja de devolverlo). Verificar que la ficha sigue funcionando con KPIs + selector de período sin errores por campos ausentes.

### E. Gating server-side del análisis IA (decisión clave de diseño)

10. El análisis IA deja de servirse por el endpoint **público**. **Enfoque recomendado** (gating real, no solo UI):
    - **Público** `GET /api/v1/fundamentals/{ticker}/latest` y su DTO → **solo KPIs** (quitar `Summary`, `SummaryMarkdown`, `InvestorTakeaway`, `OperationalSignals`, `FinancialSignals`, `RiskFlags`). Crear un DTO público slim o vaciar esos campos.
    - **Nuevo endpoint privado autenticado** `GET /api/v1/fundamentals/{ticker}/report?period=X` (requiere auth; 401 anónimo) → KPIs + análisis IA completo. Lo consume `/reportes`.
    - `GET /api/v1/fundamentals/{ticker}/periods` puede seguir **público** (la lista de períodos no es sensible).
    - **Alternativa NO recomendada:** solo ocultar el análisis en la UI dejándolo en el endpoint público → el contenido premium quedaría scrapeable vía API. **Si el dev opta por esta, debe justificarlo y marcarlo para review.**
11. Si cambian DTOs/endpoints, **regenerar el cliente tipado**: `npm run codegen:api` y verificar que `SharedApiClient`/`schema.d.ts` quedan consistentes en ambos SPAs.

### F. Tests (obligatorios antes de `review`, por `workflow-rules.md`)

12. **Backend (xUnit):**
    - Nuevo endpoint privado: **200 autenticado** devolviendo análisis para FIBRA+período; **401 anónimo**.
    - Endpoint público `/latest`: test de que **ya no incluye** el análisis IA (campos ausentes/vacíos).
    - `dotnet test` verde.
13. **Frontend Main (`node:test`, sin DOM):** testear lógica pura extraíble (p. ej. construcción/orden de opciones del combo de períodos, selección por defecto del más reciente, guards de estado vacío). `npm run test --workspace=src/Web/Main` verde. Builds Main + backend verdes.

## Tasks / Subtasks

- [ ] **T1 — Backend: gating del análisis IA** (AC: 10, 11)
  - [ ] Quitar el análisis IA del DTO/endpoint público `/fundamentals/{ticker}/latest` (DTO slim o campos vaciados).
  - [ ] Nuevo endpoint privado `GET /fundamentals/{ticker}/report?period=X` (auth) que devuelve KPIs + análisis completo (reusar `IFundamentalRepository.GetProcessedByFibraAndPeriodAsync`/`GetLatestProcessedByFibraAsync`).
  - [ ] `npm run codegen:api`; verificar `SharedApiClient`/`schema.d.ts`.
- [ ] **T2 — Frontend: extraer componentes compartidos de fundamentales** (AC: 7, 8)
  - [ ] Extraer de `FundamentalesSection.tsx` la **tabla KPI** (`FundamentalKpiTable`) y el **análisis IA** (`FundamentalAnalysis`: resumen + señales + alertas + perspectiva) a componentes reutilizables.
  - [ ] La ficha pública usa solo `FundamentalKpiTable` (+ selector de período + captura); **elimina** el análisis IA (AC-8).
- [ ] **T3 — Frontend: página `/reportes`** (AC: 1, 3, 4, 5, 6, 7)
  - [ ] Crear `src/Web/Main/src/modules/reportes/ReportesPage.tsx`: encabezado + explicación; selector de FIBRA (reusar patrón Herramientas/catálogo); combo de períodos (desde `/periods`); render del reporte (`FundamentalKpiTable` + `FundamentalAnalysis`) desde el endpoint privado; estados vacío/cargando/error.
  - [ ] `routes.tsx`: añadir `/reportes` (lazy) bajo `<ProtectedRoute>`.
- [ ] **T4 — Nav: "Reportes" en "Mi inversión"** (AC: 2)
  - [ ] Añadir `{ label: 'Reportes', to: '/reportes' }` a `MAIN_INVESTMENT_LINKS` (`public-navigation.ts`). Coordinar con 13-1/13-6.
- [ ] **T5 — Tests** (AC: 12, 13)
  - [ ] Backend: endpoint privado (200 auth / 401 anónimo) + público sin análisis.
  - [ ] Frontend: lógica pura del combo/estado.
  - [ ] Builds Main + backend verdes.
- [ ] **T6 — Verificación manual** (AC: 6, 8, 9)
  - [ ] Ficha pública sin análisis IA (solo KPIs) y sin errores; `/reportes` autenticado: seleccionar FIBRA → combo períodos → reporte; anónimo redirige a login; sin scroll horizontal 375/768/1024/1440.

## Dev Notes

### Estado actual — archivos a MODIFICAR (UPDATE) y CREAR (NEW)

**Frontend:**
- `src/Web/Main/src/modules/ficha-publica/sections/FundamentalesSection.tsx` (≈ 201 líneas) — **UPDATE**. Hoy renderiza: tabla KPI (≈ l.102-133), período/fecha (≈ l.83-98), y el análisis IA: resumen analítico (≈ l.137-154), señales operativas (≈ l.156-161), señales financieras (≈ l.163-168), alertas de riesgo (≈ l.170-178), perspectiva (≈ l.180-189). **Quitar el análisis IA; conservar KPIs + selector de período.**
- `src/Web/Main/src/modules/ficha-publica/sections/fundamentales.ts` — interface `FundamentalesData` (period, periodsAgo, capturedAt, summary, summaryMarkdown, investorTakeaway, operationalSignals[], financialSignals[], riskFlags[], items[]). Ajustar el tipo público (sin campos de análisis) y mantener el tipo completo para `/reportes`.
- `src/Web/Main/src/api/fundamentalesApi.ts` — añadir llamada al nuevo endpoint privado del reporte; ajustar el público a KPI-only.
- `src/Web/Main/src/app/routes.tsx` — añadir `/reportes` (lazy) en el bloque `<ProtectedRoute>` (junto a `/portafolio`*, `/oportunidades`, `/herramientas`, `/perfil`). *Nota: 13-6 saca `/portafolio` de ese bloque; coordinar.
- `src/Web/Main/src/shared/layouts/public-navigation.ts` — `MAIN_INVESTMENT_LINKS`: añadir `{ label: 'Reportes', to: '/reportes' }`.
- **NEW:** `src/Web/Main/src/modules/reportes/ReportesPage.tsx`; componentes compartidos `FundamentalKpiTable`/`FundamentalAnalysis` (ubicar en `ficha-publica/sections/` o un `shared/` de fundamentales — decidir según imports).
- Patrón de selector de FIBRA: `src/Web/Main/src/modules/herramientas/HerramientasPage.tsx` (búsqueda por ticker/empresa con sugerencias, hasta 4) — reusar el input/sugerencias para seleccionar **una** FIBRA.

**Backend:**
- `src/Server/Api/Endpoints/Public/FundamentalsEndpoints.cs` (≈ 122 líneas) — endpoints actuales: `/summary`, `/periods`, `/{ticker}/latest` (devuelve `FundamentalesPublicDto` con KPIs + análisis), `/{ticker}/periods`. **UPDATE:** `/latest` → KPI-only. **NEW:** endpoint privado del reporte (auth) con análisis completo.
- `src/Server/SharedApiContracts/Fundamentals/FundamentalesPublicDto.cs` — hoy: KPIs + Summary/SummaryMarkdown/InvestorTakeaway/OperationalSignals/FinancialSignals/RiskFlags. Separar en DTO público (KPI-only) + DTO de reporte (privado, completo).
- `src/Server/Application/Fundamentals/IFundamentalRepository.cs` — métodos existentes: `GetLatestProcessedByFibraAsync`, `GetProcessedByFibraAndPeriodAsync`, `GetSummaryByPeriodAsync`, `GetSummaryForRecentPeriodsAsync`. Reusar para el endpoint privado (no requiere métodos nuevos probablemente).
- `src/Server/Domain/Fundamentals/FundamentalRecord.cs` (MarkdownContent) + `FundamentalAiAnalysis.cs` (AiAnalysisJson) — origen del análisis. Sin cambios de dominio esperados.
- Auth: el endpoint privado debe exigir autenticación (reusar el mismo esquema/atributo que otros endpoints privados de portfolio).

### Guardrails técnicos (de cumplimiento estricto)

- 🚫 **NO `npx shadcn@latest add` sin aprobación.** Reusar componentes/estilos existentes.
- **Reutilizar, no duplicar:** extraer el render de KPIs/análisis de `FundamentalesSection` a componentes compartidos (no copiar markup ni la config de `ReactMarkdown`).
- **Auth-gating real (server-side, no solo UI):** el análisis IA debe quedar tras un endpoint autenticado (AC-10). Ocultar solo en UI deja el contenido scrapeable — anti-patrón.
- **Estados de datos explícitos:** `fresh/stale/partial/error/null` nunca inferidos (regla del proyecto).
- **Codegen:** tras cambios de contrato, `npm run codegen:api` (no editar `schema.d.ts` a mano).
- **No romper la ficha pública:** tras quitar el análisis, verificar que `FundamentalesSection` no falle por campos ausentes y que el selector de período siga operando.

### Security Checklist — completar antes del primer commit

- [ ] **TOCTOU doble-request:** N/A — endpoints de solo lectura.
- [ ] **Auth-gating de componentes UI / API:** "Reportes" en nav solo autenticado; ruta `/reportes` bajo `ProtectedRoute`; **y** el análisis IA tras endpoint autenticado (401 anónimo) — verificado por test (AC-12).
- [ ] **Denominador cero:** N/A — sin funciones de cálculo nuevas (los KPIs ya vienen calculados del backend).
- [ ] **Exposición de datos:** confirmar que el endpoint público `/latest` ya NO devuelve el análisis IA.

### Project Structure Notes

- Naming: componentes `PascalCase.tsx`, módulo `modules/reportes/`. API routes `/api/v1/...` kebab-case. C# `PascalCase`.
- Sin migraciones EF (los datos ya existen en `FundamentalRecord`/`FundamentalAiAnalysis`).

### Limitación de toolchain de tests (Main)

Runner de Main = **`node:test` sin DOM** — testear lógica pura (construcción de opciones del combo, orden por período, default al más reciente, guards de estado). El backend usa xUnit (tests de endpoint público/privado).

### References

- [Source: src/Web/Main/src/modules/ficha-publica/sections/FundamentalesSection.tsx] — KPIs + análisis IA a separar
- [Source: src/Web/Main/src/modules/ficha-publica/sections/fundamentales.ts] — tipos
- [Source: src/Web/Main/src/modules/fundamentales/FundamentalesPage.tsx] — página pública comparativa (referencia de selector de período)
- [Source: src/Web/Main/src/modules/herramientas/HerramientasPage.tsx] — patrón de selector/búsqueda de FIBRA
- [Source: src/Web/Main/src/api/fundamentalesApi.ts] — llamadas API actuales
- [Source: src/Web/Main/src/app/routes.tsx] — bloque `<ProtectedRoute>`
- [Source: src/Web/Main/src/shared/layouts/public-navigation.ts] — `MAIN_INVESTMENT_LINKS`
- [Source: src/Server/Api/Endpoints/Public/FundamentalsEndpoints.cs] — endpoints de fundamentales
- [Source: src/Server/SharedApiContracts/Fundamentals/FundamentalesPublicDto.cs] — DTO a separar
- [Source: src/Server/Application/Fundamentals/IFundamentalRepository.cs] — métodos de repositorio
- [Source: _bmad-output/implementation-artifacts/13-6-portafolio-landing-publico.md] — el landing describe Reportes
- [Source: AGENTS.md#Reglas Críticas] — auth-gating, estados de datos explícitos, OpenAPI → SharedApiClient
- [Source: design-system/fibradis/MASTER.md#Pre-Delivery Checklist]

## Dev Agent Record

### Agent Model Used

### Debug Log References

### Completion Notes List

### File List
