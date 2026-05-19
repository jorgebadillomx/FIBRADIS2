# Convenciones FIBRADIS — Cargadas en cada skill

Estas reglas se aplican en TODA historia y code review. Son no negociables.

## Stack — versiones exactas

- `react-router` v7 — NUNCA `react-router-dom`; el import es `from 'react-router'`
- TanStack Query v5 — sintaxis: `useQuery({ queryKey, queryFn, enabled })`
- openapi-fetch para todas las llamadas API — cliente en `fibrasApi.ts`
- shadcn/ui existente — NO ejecutar `npx shadcn@latest add` sin aprobación explícita
- Tailwind v4 — NO usar clases de v3 que no existan en v4
- NO añadir dependencias npm nuevas sin justificación; cuando valga la pena (mejor DX, menos código propio, librería estándar del stack), sugerirla al usuario antes de instalar

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

## EF Core Migrations — workaround con DLLs en uso

Si `dotnet ef migrations add` falla porque el proceso de la API tiene los DLLs bloqueados:

1. **Opción preferida**: detener el proceso de la API antes de ejecutar el comando.
2. **Alternativa**: agregar `--configuration Release` al comando:
   ```bash
   dotnet ef migrations add NombreMigracion --project src/Server/Infrastructure --startup-project src/Server/Api --configuration Release
   ```

Este workaround apareció en las historias 3.1 y 3.2. Es una convención del proyecto, no un error aislado.

## chart.tsx — fork manual para recharts 3.x

El archivo `src/Web/Main/src/shared/ui/chart.tsx` es un fork manual del componente oficial de shadcn porque:

1. `npx shadcn@latest add chart` está restringido (ver regla de shadcn/ui arriba).
2. recharts 3.x cambió los tipos TypeScript (`TooltipProps`, `LegendProps`) incompatibles con el componente oficial de shadcn.

**Antes de actualizar `recharts` o `shadcn/ui`**, verificar que `chart.tsx` sigue compilando sin errores. Si shadcn publica una versión oficial compatible con recharts 3.x, considerar migrar el fork.

## Checklist de cierre para historias públicas con SSR/SEO

Aplica a cualquier historia que entregue rutas accesibles sin autenticación (Home, ficha pública, landing pages).

Antes de marcar `done`, verificar:

- [ ] La ruta responde 200 en hit directo (no solo desde la SPA navegando)
- [ ] El prerender genera HTML con las `<meta>` correctas (title, description, og:*)
- [ ] La hidratación de React Query no causa flash de contenido vacío
- [ ] El SPA fallback en el backend cubre la ruta (sin 404 en F5)
- [ ] El contraste de color y navegación por teclado cumplen WCAG 2.1 AA para los componentes nuevos
- [ ] `npm run build` pasa con 0 errores TypeScript y 0 advertencias

Estas verificaciones no las cubre el test suite — requieren browser o curl en dev server.

## EF Core — nunca `Task.WhenAll` con el mismo DbContext

El `DbContext` es Scoped (una instancia por request) y **no es thread-safe**. Ejecutar queries en paralelo sobre la misma instancia lanza `InvalidOperationException` en SQL Server real, aunque pase sin error con el proveedor InMemory usado en tests.

```csharp
// MAL — causa 500 en producción, pasa en tests InMemory
var t1 = repo.GetDailySnapshotsAsync(id, days, ct);
var t2 = repo.GetDistributionsAsync(id, 365, ct);
await Task.WhenAll(t1, t2);

// BIEN — siempre secuencial cuando comparten DbContext
var snapshots = await repo.GetDailySnapshotsAsync(id, days, ct);
var dists     = await repo.GetDistributionsAsync(id, 365, ct);
```

**Regla**: si el endpoint llama a más de un método del mismo repositorio, usar `await` secuencial. La ganancia de paralelismo no existe cuando el cuello de botella es la misma conexión de BD.

## Tests de integración — seed temporal y assertions semánticas

Dos patrones que enmascaran bugs de lógica:

**1. Seed con un solo punto de datos**
Si el endpoint filtra por fecha/período, sembrar datos en múltiples puntos temporales. Con un solo registro, todos los períodos devuelven el mismo resultado y un mapeo incorrecto (`"6m" => 90` en vez de 180) pasa invisible.

Patrón recomendado para endpoints con filtro temporal:
```csharp
// Cubrir todos los rangos relevantes: dentro y fuera de cada período
var offsets = new[] { 5, 20, 50, 110, 220, 400 }; // días atrás
foreach (var d in offsets)
    db.Table.Add(new Entity { Date = today.AddDays(-d), ... });
```

**2. Assertions solo de status code**
Verificar `200 OK` y estructura JSON no es suficiente. Los tests deben validar **contenido semántico**:
- El período `"1m"` devuelve menos filas que `"1y"`
- Las fechas retornadas caen dentro del rango esperado
- Períodos distintos producen resultados distintos

```csharp
// Mal: solo verifica que no explota
Assert.Equal(HttpStatusCode.OK, response.StatusCode);

// Bien: verifica que el filtro funciona
Assert.Equal(2, priceHistory.Count); // seed tiene 2 puntos en los últimos 30 días
Assert.All(priceHistory, p =>
    Assert.True(DateOnly.Parse(p.date) >= DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30))));
```

## PriceCarousel — decisiones de diseño vigentes (historia 2.5)

`src/Web/Main/src/modules/home/PriceCarousel.tsx`

- **`FreshnessBadge` eliminado intencionalmente** — el carrusel ya no muestra el badge de frescura de datos. Esta decisión es deliberada (la tarjeta compacta horizontal no tiene espacio y el badge aportaba ruido visual en ese contexto). **No restaurar** en futuros reviews ni historias salvo decisión explícita del equipo.
- `hasPrice = lastPrice != null` — la condición anterior (`lastPrice != null && snap.freshnessStatus != null`) fue reemplazada; el precio se muestra si existe independientemente de `freshnessStatus`.
- La tarjeta usa layout horizontal compacto (`flex items-center justify-between`) con auto-scroll automático cada 3 s (pausa al hacer hover).

## Hallazgos de review no bloqueantes — cómo no perderlos

Cuando un hallazgo de code review no bloquea el cierre de la historia pero representa deuda real:

1. **Documentarlo en el story file** bajo "Senior Developer Review (AI) → Action Items" con severidad Media o Baja y checkbox `[ ]`.
2. **Abrirlo como tarea en la siguiente historia** del mismo módulo, bajo "Tasks/Subtasks" con prefijo `[Deuda]` y referencia a la historia origen (ej. `[Deuda 1.2] Agregar tipo de respuesta al health endpoint en OpenAPI`).
3. **No marcarlo como resuelto** en sprint-status.yaml hasta que el código esté corregido.

Este proceso evita que los findings de calidad media queden enterrados en Dev Agent Records sin visibilidad en el tracker.

## mem0 — usar SOLO en estos casos

- Tomaste una decisión que contradice o extiende el story file Y afectará historias futuras
- Detectaste un patrón de error recurrente que no está documentado en ningún story ni en AGENTS.md
- Encontraste una restricción del proyecto que no está en ningún archivo

**NO usar mem0 por defecto antes/después de cada historia.** El contexto vive en el story file.
