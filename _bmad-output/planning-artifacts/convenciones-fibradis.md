# Convenciones FIBRADIS — Cargadas en cada skill

Estas reglas se aplican en TODA historia y code review. Son no negociables.

## Stack — versiones exactas

- `react-router` v7 — NUNCA `react-router-dom`; el import es `from 'react-router'`
- TanStack Query v5 — sintaxis: `useQuery({ queryKey, queryFn, enabled })`
- openapi-fetch para todas las llamadas API — cliente en `fibrasApi.ts`
- shadcn/ui existente — NO ejecutar `npx shadcn@latest add` sin aprobación explícita
- Tailwind v4 — NO usar clases de v3 que no existan en v4
- NO añadir dependencias npm nuevas sin aprobación explícita del usuario

## Convenciones de código TypeScript/React

- `noUnusedLocals: true` en tsconfig — cada import declarado DEBE usarse
- Componentes: PascalCase, archivos `.tsx`
- Imports absolutos con alias `@/` (no rutas relativas `../../`)
- Nunca mostrar `0` para datos financieros nulos — siempre `—`
- Nullables de la API (`siteUrl`, etc.) vienen como `null` desde C# — usar `?? null` o `?? defaultValue`

## Flujo obligatorio de dev-story

1. Seguir las tareas del story file EN ORDEN — no reordenar, no saltarse
2. Marcar tarea `[x]` SOLO cuando el build pasa sin errores
3. Ejecutar `npm run build --workspace=src/Web/Main` antes de marcar Task 6 completa
4. Actualizar `sprint-status.yaml`: `in-progress` al empezar, `review` al terminar
5. Actualizar File List y Change Log en el story file antes de marcar `review`

## Flujo obligatorio de code-review

1. Leer el story file COMPLETO antes de revisar el código
2. Verificar TODOS los Criterios de Aceptación, no solo el código
3. Hallazgos van a la sección "Senior Developer Review (AI)" del story file
4. Actualizar `sprint-status.yaml` solo si el review resulta en `done` o requiere `in-progress` de nuevo

## mem0 — usar SOLO en estos casos

- Tomaste una decisión que contradice o extiende el story file Y afectará historias futuras
- Detectaste un patrón de error recurrente que no está documentado en ningún story ni en AGENTS.md
- Encontraste una restricción del proyecto que no está en ningún archivo

**NO usar mem0 por defecto antes/después de cada historia.** El contexto vive en el story file.
