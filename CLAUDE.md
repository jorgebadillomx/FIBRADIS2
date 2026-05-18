# FIBRADIS — Claude Code Instructions

Lee `AGENTS.md` para el contexto completo del proyecto y las instrucciones de memoria compartida con otros agentes.

## Memoria Persistente

Este proyecto usa mem0 en `project-memory/`. Antes de trabajar en una tarea nueva, carga contexto:

```bash
python scripts/memory/memory_cli.py search "<tema relevante>"
```

Después de decisiones arquitectónicas o cambios de diseño importantes, guarda en mem0:

```bash
python scripts/memory/memory_cli.py add "decisión: ..."
```

## Flujo de Trabajo

1. Lee `project-memory/seeds/05-current-status.md` para saber qué está hecho
2. Busca en mem0 contexto relevante antes de implementar
3. Implementa siguiendo las reglas en `AGENTS.md`
4. Si tomas decisiones no triviales, agrégalas a mem0

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
