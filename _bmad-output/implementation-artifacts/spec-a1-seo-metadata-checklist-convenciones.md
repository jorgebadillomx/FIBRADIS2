---
title: 'A1 — Checklist SEO Metadata en convenciones-fibradis.md'
type: 'chore'
created: '2026-06-11'
status: 'done'
route: 'one-shot'
---

## Intent

**Problem:** Las reglas de encoding, longitudes y og:title de los middlewares de metadata SEO se redescubrieron como patches de code review en 11-2 y luego en 11-4. Son reglas transversales al patrón pero vivían solo en los story files, no en convenciones.

**Approach:** Agregar sección "Middleware de Metadata SEO (server-side)" en `convenciones-fibradis.md` con las reglas canónicas de implementación, incluyendo el correcto encoder para JSON-LD, soft-404 obligatorio, y coordinación SEO↔auth.

## Suggested Review Order

- [`_bmad-output/planning-artifacts/convenciones-fibradis.md`](_bmad-output/planning-artifacts/convenciones-fibradis.md) — nueva sección completa (buscar "Middleware de Metadata SEO")
