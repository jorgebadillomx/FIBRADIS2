# FIBRADIS — Claude Code Instructions

Lee `AGENTS.md` para el contexto completo del proyecto y las instrucciones de memoria compartida con otros agentes.

## mem0 — solo cuando el contexto no vive en ningún archivo

**No usar mem0 por defecto.** El contexto de cada historia vive en el story file (Dev Notes, Dev Agent Record). Usar mem0 únicamente si:

1. Tomaste una decisión que contradice o extiende el story file Y afectará historias futuras.
2. Detectaste un patrón de error recurrente que no está en ningún story ni en `AGENTS.md`.
3. Encontraste una restricción del proyecto no documentada en ningún archivo.

Si aplica, los comandos son:

```bash
python scripts/memory/memory_cli.py search "<tema>"   # buscar
python scripts/memory/memory_cli.py add "decisión: ..." # guardar
```

## Flujo de Trabajo

1. Usa los skills BMAD (`/bmad-dev-story`, `/bmad-code-review`) — son el flujo de trabajo; no los omitas
2. Lee el story file COMPLETO antes de implementar — las Dev Notes tienen todo el contexto necesario
3. Implementa siguiendo las reglas en `AGENTS.md` y `_bmad-output/planning-artifacts/convenciones-fibradis.md`
4. **Lee `workflow-rules.md`** — branch por historia, merge a main al completar, unit tests obligatorios antes de `done`
5. Usa mem0 solo si aplica alguna de las condiciones de arriba

## Comandos de Desarrollo

```bash
# Backend
dotnet build FIBRADIS.slnx
dotnet ef database update --project src/Server/Infrastructure --startup-project src/Server/Api

# Frontend Main (puerto 5173)
npm run dev:main

# Frontend Ops (puerto 5174)
npm run dev:ops

# Generar cliente API tipado
npm run codegen:api
```
