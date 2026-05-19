# FIBRADIS — Claude Code Instructions

Lee `AGENTS.md` para el contexto completo del proyecto y las instrucciones de memoria compartida con otros agentes.

## mem0 — contexto que trasciende el story file

El skill `bmad-dev-story` y `bmad-code-review` ejecutan automáticamente búsquedas en mem0 al activarse. No necesitas hacerlo manualmente en la mayoría de casos.

**Cuándo guardar manualmente:** solo si aplica una de estas condiciones exactas:

1. Tomaste una decisión que contradice o extiende el story file Y afectará historias futuras.
2. Detectaste un patrón de error recurrente no documentado en ningún story ni en `AGENTS.md`.
3. Encontraste una restricción del toolchain no documentada en ningún archivo.

**Formato obligatorio al guardar:**
- Decisión técnica: `"contexto: <área> | problema: <qué pasó> | solución: <cómo se resolvió>"`
- Patrón de review: `"patrón review: <área> | antipatrón: <qué evitar> | corrección: <cómo hacerlo bien>"`

**Ejemplos reales de memorias válidas (tomadas de Épicas 1-3):**
- `"contexto: scaffolding frontend | problema: npm create vite@latest instala Vite 8 en lugar de Vite 7 | solución: usar npm create vite@7 para fijar versión exacta"`
- `"contexto: CPM + dotnet templates | problema: dotnet new agrega versiones inline en .csproj | solución: limpiar después de cada scaffold, solo definir versiones en Directory.Packages.props"`
- `"contexto: auth JWT | problema: ExecuteUpdateAsync sin WHERE RevokedAt IS NULL permite race condition en refresh tokens | solución: siempre añadir condición atómica al invalidar tokens"`
- `"patrón review: auth | antipatrón: Cookie.Secure basada en comparación literal de hostname | corrección: usar Request.IsHttps"`

**Comandos:**
```bash
python scripts/memory/memory_cli.py search "<tema>"     # buscar
python scripts/memory/memory_cli.py add "<memoria>"     # guardar
python scripts/memory/memory_cli.py list                # listar todo
```

## Flujo de Trabajo

1. Usa los skills BMAD (`/bmad-dev-story`, `/bmad-code-review`) — son el flujo de trabajo; no los omitas
2. Lee el story file COMPLETO antes de implementar — las Dev Notes tienen todo el contexto necesario
3. Implementa siguiendo las reglas en `AGENTS.md` y `_bmad-output/planning-artifacts/convenciones-fibradis.md`
4. **Lee `workflow-rules.md`** — branch por historia, merge a main al completar, unit tests obligatorios antes de `done`
5. Usa mem0 solo si aplica alguna de las condiciones de arriba
6. Usa `context7` cuando necesites documentación actualizada de librerías, frameworks o APIs externas; no sustituye la documentación del proyecto

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
