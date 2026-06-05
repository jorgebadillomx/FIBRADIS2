# Story {{epic_num}}.{{story_num}}: {{story_title}}

Status: ready-for-dev

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a {{role}},
I want {{action}},
so that {{benefit}}.

## Acceptance Criteria

1. [Add acceptance criteria from epics/PRD]

## Tasks / Subtasks

- [ ] Task 1 (AC: #)
  - [ ] Subtask 1.1
- [ ] Task 2 (AC: #)
  - [ ] Subtask 2.1

## Dev Notes

- Relevant architecture patterns and constraints
- Source tree components to touch
- Testing standards summary

### Security Checklist — completar antes del primer commit

Para cada endpoint de escritura nuevo (`POST`/`PUT`/`DELETE`) y componente interactivo:

- [ ] **TOCTOU doble-request**: ¿qué pasa si el mismo usuario envía dos requests en paralelo (doble-click, retry)?  
  Para `AddAsync`/`CreateAsync` con PK única: capturar `DbUpdateException` y retornar 409 o idempotencia.
- [ ] **Auth-gating de componentes UI**: ¿los botones/controles de acción que requieren autenticación están condicionalmente renderizados o deshabilitados para usuarios anónimos?  
  Patrón: `{isAuthenticated && <ActionButton />}` — nunca renderizar para anónimos sin guardia explícita.
- [ ] **Denominador cero (funciones de cálculo)**: ¿las funciones puras con división tienen el caso denominador = 0 como primer test? Ver convenciones-fibradis.md sección "Testing — Funciones de Cálculo Financiero".

### Project Structure Notes

- Alignment with unified project structure (paths, modules, naming)
- Detected conflicts or variances (with rationale)

### References

- Cite all technical details with source paths and sections, e.g. [Source: docs/<file>.md#Section]

## Dev Agent Record

### Agent Model Used

{{agent_model_name_version}}

### Debug Log References

### Completion Notes List

### File List
