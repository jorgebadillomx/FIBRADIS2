# FIBRADIS — Reglas de Workflow Obligatorio

Fuente única de verdad para las reglas operativas del proyecto.
Todos los agentes (Claude Code, Codex CLI) deben leer y respetar este archivo.

---

## Regla 1: Branch dedicado por historia

Al crear una historia (`/bmad-create-story`), crear inmediatamente un branch dedicado:

```bash
git checkout -b story/X-Y-nombre-historia
```

El nombre del branch debe coincidir exactamente con la clave de la historia (ej. `story/2-4-seo-prerender`).
Nunca implementar directamente en `main` ni en el branch de otra historia.

---

## Regla 2: Merge a main al pasar a `done`

Cuando una historia pasa a `done`, ejecutar en orden:

```bash
# 1. Commit final en el branch de la historia
git add <archivos>
git commit -m "fix/feat(story-X.Y): descripción — done"

# 2. Merge a main con --no-ff para preservar historial
git checkout main
git merge --no-ff story/X-Y-nombre -m "merge: story X.Y Título — done"

# 3. Eliminar branch local
git branch -d story/X-Y-nombre
```

---

## Regla 3: Unit tests obligatorios antes de `done`

Antes de marcar una historia como `done`, el agente DEBE:

1. **Implementar** la funcionalidad de la historia.
2. **Agregar o actualizar** unit tests que cubran la lógica modificada.
   - No basta con que los tests existentes sigan pasando.
   - Los tests deben ejercitar los casos relevantes de la historia: happy path, casos nulos, errores esperados.
3. **Ejecutar** los tests relevantes.
4. **Si fallan**, corregir código o tests hasta que todos pasen.
5. **Actualizar el Dev Agent Record** con:
   - Archivos modificados
   - Tests creados/actualizados (nombres de archivo y método)
   - Comando de test ejecutado (`dotnet test`, `npm test`, etc.)
   - Resultado final (ej. `5 passed, 0 failed`)

**No marcar la story como `done` si no hay evidencia de tests ejecutados y pasando.**

### Comandos de test según capa

```bash
# Backend (.NET)
dotnet test tests/Unit/

# Frontend (si aplica)
npm test --workspace=src/Web/Main
```
