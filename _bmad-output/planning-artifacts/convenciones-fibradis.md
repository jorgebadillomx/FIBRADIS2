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
6. **Gate de artefactos antes de review**: verificar que cada archivo listado en "Archivos a crear (NEW)" del story file existe físicamente en su ruta exacta. Si alguno falta → la story NO puede ir a `review`. Una task `[x]` con código inline en otro archivo NO cuenta como completada.
7. **Gate de migraciones EF**: en stories con migración EF, ejecutar `dotnet ef migrations list --project src/Server/Infrastructure --startup-project src/Server/Api` y confirmar que la nueva migración aparece en la lista antes de declarar `review`. Build verde + InMemory tests verdes NO garantizan que la migración existe.

Origen: retro Épica 10 — 10-1 llegó a review con 9 BLOCKERs por archivos frontend marcados `[x]` pero inline en otro componente, y migración EF ausente.

## Flujo obligatorio de code-review

1. Leer el story file COMPLETO antes de revisar el código
2. Verificar TODOS los Criterios de Aceptación, no solo el código
3. Hallazgos van a la sección "Senior Developer Review (AI)" del story file
4. Actualizar `sprint-status.yaml` solo si el review resulta en `done` o requiere `in-progress` de nuevo

**Micro-features que tocan BD, jobs o contratos de API requieren code review — sin excepción de tamaño.** Cualquier implementación con migración EF, cambio de Hangfire job o contrato de API nuevo/modificado debe pasar por un review pass documentado (Patch/Defer/Dismissed). 4-10 y 4-12 son el antecedente: código en producción sin revisión acumula deuda invisible.

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
- [ ] `<title>Nombre de la página — FIBRADIS</title>` presente en el componente
- [ ] `<meta name="description" content="..."/>` con descripción útil de 120-160 chars — **verificar con contador** (`"tu texto".length` en consola del navegador o en el editor)
- [ ] `<meta property="og:title" content="..."/>` — mismo texto que `<title>`
- [ ] `<meta property="og:description" content="..."/>` — mismo o similar a description
- [ ] `<meta property="og:type" content="website"/>` (o `article` para fichas de noticias)
- [ ] La hidratación de React Query no causa flash de contenido vacío
- [ ] El SPA fallback en el backend cubre la ruta (sin 404 en F5)
- [ ] El contraste de color y navegación por teclado cumplen WCAG 2.1 AA para los componentes nuevos
- [ ] `npm run build` pasa con 0 errores TypeScript y 0 advertencias

Estas verificaciones no las cubre el test suite — requieren browser o curl en dev server.

## EF Core — nunca `Task.WhenAll` con el mismo DbContext

El `DbContext` es Scoped (una instancia por request) y **no es thread-safe**. Ejecutar queries en paralelo sobre la misma instancia lanza `InvalidOperationException` en PostgreSQL real, aunque pase sin error con el proveedor InMemory usado en tests.

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

## Testing — Checklist de ACs antes del primer commit

Para cada Criterio de Aceptación que defina un límite, borde o caso null, debe existir al menos un test unitario explícito **antes del primer commit**, no solo en el code review. Aplica especialmente a:

- Límites numéricos (`min`/`max` items, rangos de chars, conteos)
- Comportamiento con `null`/`undefined` en datos de la API
- Flags booleanos de estado (`isExcluded`, `isLimitedData`, `isError`)
- Validaciones de URL/query params (tickers en `/comparar`, paginación, etc.)

```typescript
// Ejemplo: AC6 del comparador — máximo 4 FIBRAs desde URL
it('limita a MAX_COMPARE_FIBRAS tickers al parsear query param', () => {
  const result = parseCompareTickers('A,B,C,D,E')  // 5 tickers en URL
  expect(result).toHaveLength(MAX_COMPARE_FIBRAS)  // ← primer test antes de commit
})

// Ejemplo: AC5 — isExcluded muestra — en sub-componentes de score
it('renderScoreComponent muestra — cuando isExcluded es true', () => {
  // ...
})
```

Origen: retro Épica 8 — P1 (`isExcluded` en sub-rows) y P2 (`parseCompareTickers` sin cap) atrapados en review, no en dev.

## EF Core — batch queries con JOINs manuales

Los bugs de deduplicación son silenciosos con InMemory provider porque LINQ en memoria es más permisivo que SQL. Dos historias (5-10 y 4-11) tuvieron bugs de este tipo que solo el reviewer detectó.

**Regla**: todo repositorio que implementa batch loading o JOIN manual debe documentar en Dev Notes las columnas exactas de la proyección y un ejemplo de resultado esperado.

```csharp
// MAL — proyección incompleta: LinkDto.Id siempre Guid.Empty
var links = await _db.ArticleFibras
    .Where(l => ids.Contains(l.ArticleId))
    .Select(l => new { l.ArticleId, l.Fibra.Ticker })   // falta l.FibraId
    .ToListAsync(ct);

// BIEN — proyección completa documentada en Dev Notes
// Resultado esperado: { ArticleId, FibraId, Ticker } por cada link
var links = await _db.ArticleFibras
    .Where(l => ids.Contains(l.ArticleId))
    .Select(l => new { l.ArticleId, l.FibraId, l.Fibra.Ticker })
    .ToListAsync(ct);
```

**Regla de deduplicación**: si el resultado debe ser "uno por entidad padre" (una fila por FIBRA, un registro por período), agregar `DistinctBy` o `GroupBy` + `First` explícitamente, y documentar la invariante en Dev Notes. No asumir que el WHERE filtra correctamente.

## Endpoints con 5+ queries — documentar el grafo antes de implementar

Cuando un endpoint necesita 5 o más queries de base de datos, el grafo completo debe quedar documentado en Dev Notes **antes de codear el handler**. Esto previene la duplicación silenciosa de queries (e.g., cargar el mismo conjunto dos veces porque el dev perdió la pista de qué ya se consultó).

**Formato mínimo en Dev Notes:**

```text
Queries de este endpoint (en orden de ejecución):
1. fibraRepo.GetByTickerAsync — valida tickers seleccionados
2. marketRepo.GetLatestSnapshotPerFibraAsync — snapshots seleccionados y universo completo (una sola llamada, reusar)
3. fundamentalRepo.GetSummaryLatestAsync — fundas seleccionados y universo completo (una sola llamada, reusar)
4. marketRepo.GetDistributionsByFibrasAsync — distribuciones seleccionados
5. marketRepo.GetWeek52AvgByFibrasAsync — AVG 52S seleccionados
6. fibraRepo.GetAllActiveAsync — universo para score
7. marketRepo.GetDistributionsByFibrasAsync (universo) — reutilizar 4 si los ids coinciden
8. marketRepo.GetWeek52AvgByFibrasAsync (universo) — reutilizar 5 si los ids coinciden
```

Origen: retro Épica 8 — P3 en CompareEndpoints llamaba `GetLatestSnapshotPerFibraAsync` y `GetSummaryLatestAsync` dos veces (para seleccionados y para universo) en lugar de reusar.

## Hangfire jobs — async end-to-end en clientes HTTP

Los clientes HTTP dentro de un Hangfire job deben ser async en **todas las capas**, no solo en la de orquestación. `.GetAwaiter().GetResult()` o `.Result` en cualquier nivel bloquea sincrónicamente un thread del pool de workers, causando saturación con N pipelines concurrentes. Historia 5-11 tuvo dos bugs HIGH de este tipo (P2, P3).

```csharp
// MAL — bloqueo síncrono en cliente HTTP dentro de job
public IReadOnlyList<ListingItem> ParseListingItems(int page)
{
    var html = _httpClient.GetStringAsync(url).GetAwaiter().GetResult(); // ⛔
    return Parse(html);
}

// BIEN — async end-to-end
public async Task<IReadOnlyList<ListingItem>> ParseListingItemsAsync(int page, CancellationToken ct)
{
    var html = await _httpClient.GetStringAsync(url, ct);
    return Parse(html);
}
```

**Regla adicional**: el warmup o inicialización de sesión HTTP debe ser lazy (una sola vez) y tolerante a fallos. No invocar en cada método público del cliente.

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

## Testing — Tests con valores exactos en Dev Notes para funciones de cálculo o parsing

Toda historia que introduzca funciones de cálculo numérico o parsers de texto debe incluir en Dev Notes los casos de test con **valores numéricos exactos** antes de implementar. El formato mínimo es el bloque de código `it(...)` con inputs y expected outputs concretos.

```typescript
// Ejemplo correcto (Épica 10 — 10-2 calcIsr):
it('calcula con desglose: dist=0.62, taxable=0.40, 500 units', () => {
  const r = calcIsr(0.62, 500, 0.40);
  expect(r.taxableGross).toBeCloseTo(200);
  expect(r.isr).toBeCloseTo(60);
  expect(r.net).toBeCloseTo(250);
});
```

Esto obliga al agente a verificar contra casos concretos del spec, no contra intuición. Historias que aplican este patrón salen con 0 BLOCKERs en review; las que no lo aplican acumulan BLOCKERs de tipo "resultado incorrecto".

Origen: retro Épica 10 — 10-2 (0 BLOCKERs) vs 10-1 (9 BLOCKERs). La diferencia fue que Dev Notes de 10-2 incluían los tests exactos.

---

## Testing — Funciones de Cálculo Financiero

Toda función pura que realice división debe tener como **primer test** el caso denominador = 0, antes de cualquier otro escenario. Esta regla aplica sin excepción para funciones de cálculo de portafolio, score, yield, plusvalía, promedios ponderados o cualquier fórmula financiera.

```typescript
// Orden obligatorio de tests para funciones con división
describe('calcDifPct', () => {
  it('devuelve 0 cuando costoPromedio es 0', () => {  // ← primer test siempre
    expect(calcDifPct(100, 0)).toBe(0);
  });
  it('calcula correctamente con valores positivos', () => {
    expect(calcDifPct(110, 100)).toBeCloseTo(0.1);
  });
});
```

```csharp
// En C# igualmente: el primer test cubre el denominador cero
[Fact]
public void Calculate_WhenUniverseSizeIsZero_ReturnsSafeFallback()
{
    var result = UniverseCoverageCalculator.Calculate(fibrasWithPrice: 0, universeSize: 0);
    Assert.Equal(CoverageStatus.Normal, result.Status); // o el valor seguro documentado
}
```

**Regla de DevNotes**: si una historia introduce funciones de cálculo con división, la sección Dev Notes debe listar explícitamente los invariantes de denominador y cuál es el valor de retorno correcto cuando el denominador es cero (0, null, excepción controlada).

Origen: retro Épica 7, patrón 1 — tres instancias de división por cero en 7-2 (`difPct`, `calcNuevoAvg`, `calcNuevaPlusvaliaPct`) atrapadas en review, no en dev.

## mem0 — usar SOLO en estos casos

- Tomaste una decisión que contradice o extiende el story file Y afectará historias futuras
- Detectaste un patrón de error recurrente que no está documentado en ningún story ni en AGENTS.md
- Encontraste una restricción del proyecto que no está en ningún archivo

**NO usar mem0 por defecto antes/después de cada historia.** El contexto vive en el story file.
