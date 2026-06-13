# Story 12.7: Core Web Vitals (INP / LCP / CLS) — medición y optimización

Status: ready-for-dev

<!-- Independiente de 12-1. Empieza con un spike de medición que define el alcance real. -->

## Story

As an **equipo de FIBRADIS**,
I want **medir y optimizar los Core Web Vitals (INP, LCP, CLS) de las páginas públicas**,
so that **el sitio cumpla el umbral "Good" de Google (factor de ranking desde 2024, INP reemplazó a FID) y mejore la experiencia real del usuario**.

## ⚠️ Esta historia empieza con un spike de medición
El alcance real **depende de la medición**. Si las CWV ya están en verde, el trabajo es mínimo (documentar + guardrail). Si hay ámbar/rojo (probable en LCP por fuentes Google + JS de SPA, o INP por hidratación), se priorizan los fixes. **No comprometer fixes a ciegas.**

## Dependencias y contexto
- SPA **Vite 7 + React 19.2**; metadata server-side ya inyectada (Épica 11). Fuentes Google preconnect/preload (Playfair Display + IBM Plex Sans) en [index.html](src/Web/Main/index.html). GTM (`GTM-T44HV55`) deferido en `<body>`.
- Herramientas disponibles: Lighthouse / Chrome DevTools MCP (lab) y CrUX (field, vía PageSpeed Insights) para datos reales de campo.
- Umbrales 2026 "Good": **LCP ≤ 2.5s**, **INP ≤ 200ms**, **CLS ≤ 0.1** (p75 móvil).

## Acceptance Criteria

**AC-1 — Baseline medido.** Se mide LCP/INP/CLS (lab + field cuando haya datos CrUX) en las rutas públicas clave: `/`, `/fibras`, `/fibras/{slug}`, `/noticias`, `/noticias/{slug}`, `/fundamentales`, `/comparar`. Se documenta el baseline (móvil y desktop) en el Dev Agent Record con la herramienta y fecha.

**AC-2 — Diagnóstico priorizado.** Por cada métrica fuera de umbral, se identifica la causa raíz (p.ej. LCP = fuente/imagen hero; INP = handler pesado/hidratación; CLS = imágenes sin dimensiones / font swap) y se prioriza por impacto.

**AC-3 — Fixes de las métricas en rojo/ámbar.** Se aplican las correcciones de mayor impacto, que pueden incluir (según diagnóstico): `font-display: optional/swap` + subset/preload correcto; dimensiones explícitas (`width`/`height` o `aspect-ratio`) en imágenes (og/logos/noticias) para CLS; `loading="lazy"`/`decoding="async"` en imágenes below-the-fold; code-splitting / defer de JS no crítico; reducir trabajo en main thread de los handlers con peor INP; asegurar que GTM no bloquea. Cada fix con su métrica objetivo.

**AC-4 — Verificación post-fix.** Re-medición demostrando mejora hacia "Good" en las métricas atacadas. Documentar antes/después.

**AC-5 — Sin regresión funcional ni SEO.** Los fixes no rompen la inyección de metadata server-side, la hidratación de React Query, ni el render de las páginas. `npm run build` 0 errores TS.

**AC-6 — Guardrail (opcional).** Si es viable, dejar una verificación de CWV en el flujo (Lighthouse CI o un check manual documentado en convenciones) para evitar regresiones futuras.

## Tasks / Subtasks

- [ ] **T1 — Spike de medición (AC-1, AC-2)**: correr Lighthouse/Chrome DevTools sobre las rutas clave (móvil + desktop); consultar CrUX/PSI para field data si existe. Documentar baseline + causas raíz. **Gate de decisión:** si todo verde → saltar a T5 (documentar + guardrail). Si no → continuar.
- [ ] **T2 — Fixes de LCP (AC-3)**: optimizar fuentes (preload/subset/`font-display`), imagen/hero LCP, critical CSS si aplica.
- [ ] **T3 — Fixes de CLS (AC-3)**: dimensiones/aspect-ratio en imágenes (logos, og, noticias), reservar espacio para contenido async, evitar layout shift por fuentes.
- [ ] **T4 — Fixes de INP (AC-3)**: identificar interacciones con peor INP, reducir trabajo síncrono, `useTransition`/debounce donde aplique, revisar listeners pesados, asegurar GTM no bloquea el hilo principal.
- [ ] **T5 — Re-medición + documentación (AC-4, AC-5)**: antes/después; verificar no-regresión SEO/funcional; `npm run build`.
- [ ] **T6 — Guardrail (AC-6, opcional)**: Lighthouse CI o check documentado en `convenciones-fibradis.md`.

## Dev Notes
- **Stack real = SQL Server** (irrelevante aquí; historia 100% frontend/perf, sin tablas).
- **Spike primero**: no asumir problemas. Medir, luego arreglar. El alcance se ajusta tras T1 (documentar en Dev Agent Record si la historia resultó mínima por estar ya en verde).
- **INP (2024+)** es lo más sensible en SPAs: mide latencia de TODAS las interacciones, no solo la primera. Tablas grandes (universo de fibras, comparador) y filtros son los sospechosos.
- **No romper SEO server-side**: los fixes de JS/lazy no deben afectar el HTML inicial que inyectan los middlewares (Épica 11/12-1). El contenido crítico para crawlers ya viene en el shell.
- **Fuentes**: Playfair + IBM Plex vía Google Fonts — candidato #1 de LCP/CLS; evaluar self-host + `font-display`.
- **Reusar herramientas existentes**: Chrome DevTools MCP / Lighthouse ya disponibles en el entorno.

### Security Checklist — antes del primer commit
- [ ] **TOCTOU / Auth-gating / Denominador cero**: N/A (historia de performance, sin endpoints de escritura ni cálculo nuevo).
- [ ] **No introducir scripts de terceros** adicionales que degraden privacidad/perf sin justificación.

### References
- [index.html](src/Web/Main/index.html) (fuentes, GTM, prerender-meta)
- [HomePage.tsx](src/Web/Main/src/modules/home/HomePage.tsx), [FibraPage.tsx](src/Web/Main/src/modules/ficha-publica/FibraPage.tsx), [NoticiaPage.tsx](src/Web/Main/src/modules/noticia/NoticiaPage.tsx), [chart.tsx](src/Web/Main/src/shared/ui/chart.tsx)
- Story 12-1: [12-1-modulo-seo-administrable.md](_bmad-output/implementation-artifacts/12-1-modulo-seo-administrable.md)
- 2026: [SEO Best Practices 2026 — ALM](https://almcorp.com/blog/seo-best-practices-complete-guide-2026/) · [HTML Tags for SEO 2026](https://www.clickrank.ai/html-tags-for-seo/)

## Hallazgos de auditoría SEO (2026-06-13)

> Auditoría completa (score 84/100): [seo-audit/FULL-AUDIT-REPORT.md](../../seo-audit/FULL-AUDIT-REPORT.md). Baseline parcial ya medido — **input directo para el spike T1** (reduce el alcance del spike, no lo reemplaza: faltan field data CrUX, INP y throttling móvil real).

### Baseline lab medido (Chrome DevTools, sin throttling)
| Página | LCP | CLS | TTFB | Nota |
|--------|-----|-----|------|------|
| `/` (home) | **355 ms** ✅ | **0.08** ✅ | 67 ms | Excelente; arquitectura de carga ya optimizada (GTM diferido, fuentes non-blocking, modulepreload) |
| `/fibras/{slug}` | — | **0.195** ❌ | — | CLS "needs improvement" (objetivo <0.10) |

- **CrUX field data: NO existe** para estas URLs (tráfico real insuficiente). El spike T1 debe contar con que PSI/CrUX devolverá `n/a` por ahora; re-medir cuando crezca el tráfico.
- **INP no medido** (requiere interacción) — sigue siendo el sospechoso #1 en SPA (tablas grandes, comparador, filtros). El spike debe cubrirlo.

### 🟡 M1 — CLS concentrado en páginas data-heavy (prioriza T3)
El CLS malo (0.195) está en la **ficha de fibra**, no en home (0.08). Causa probable: gráficas (`chart.tsx`), tablas de fundamentales/distribuciones y el price-ticker que reflowean tras cargar datos async + font-swap. **T3 debe priorizar** reservar espacio (min-height / aspect-ratio en contenedores de gráfica, skeletons dimensionados al contenido final, fijar la fila del ticker). Objetivo: CLS <0.10 en `/fibras/{slug}` y `/fundamentales`.

### 🟡 M3 — Accesibilidad (Lighthouse a11y 93/100) — adyacente, no es CWV
Detectado durante el mismo Lighthouse del spike. No es Core Web Vitals pero sí calidad frontend; registrar y decidir si se aborda aquí o en historia aparte:
- **43 elementos fallan contraste de color** — sobre todo texto atenuado (`opacity-60`), badges `text-xs`, y celdas "nota"/"diferencia". Subir los tokens de `muted-foreground` a ≥4.5:1.
- **`aria-expanded` inválido en `<tr>`** (filas expandibles de la tabla de distribuciones en la ficha) — mover el affordance a un `<button>` o usar un patrón de disclosure válido para ese rol.

## Dev Agent Record
### Agent Model Used
### Debug Log References
### Completion Notes List
### File List
