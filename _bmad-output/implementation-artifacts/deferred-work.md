# Deferred Work

## Deferred from: code review of 9-5-mejoras-fundamentales (2026-06-09)

- **D1 (LOW): Dos llamadas DB por FIBRA en `FundamentalsAutomationService`** — `GetLatestProcessedByFibraAsync` y `GetProcessedPeriodsAsync` son dos round-trips donde el period del primero está implícito en la lista del segundo. Podría derivarse `latestProcessed` haciendo `ComparePeriods` sobre `processedPeriods` usando el helper, eliminando una query. Impacto bajo dado que solo hay ~19 FIBRAs activas.
- **D2 (LOW): Magic number `-20` en `ComputeFromPeriod`** [`FundamentalsDiscoveryPeriodHelper.cs:34`] — Los 5 años hacia atrás (20 trimestres) están hardcodeados sin constante nombrada. Extraer a `private const int DefaultLookbackQuarters = 20` cuando se toque el helper de nuevo.
- **D3 (LOW): Aserción `db.FundamentalSourceManifests.CountAsync()` vacuamente verdadera en test** [`FundamentalsAutomationServiceTests.cs`] — El test `WhenCandidatePeriodAlreadyExistsInProcessedPeriods` aserta sobre el mismo `db` que el `ThrowingManifestRepository` ignora; el count siempre es 0 independientemente del comportamiento. La protección real es `ThrowingManifestRepository`; la aserción es ruido. Eliminar en próxima limpieza de tests.
- **D4 (LOW): Sin test para AC4 fallback implícito entre fuentes** — No existe test donde Fuente1 retorna candidatos anteriores a la ventana y Fuente2 retorna candidatos válidos para la misma FIBRA, verificando que Fuente2 procesa normalmente. Agregar en próxima historia del módulo Fundamentals.
- **D5 (LOW): `ComputeFromPeriod` no tiene test con string vacío `""`** [`FundamentalsDiscoveryPeriodHelperTests.cs`] — El test `StartsFiveYearsBack` solo cubre `null`. `IsNullOrWhiteSpace` también acepta `""` y `"   "`; agregar casos InlineData si se extiende la suite del helper.

## Deferred from: quick-dev mejoras módulo Oportunidades (2026-06-05)

- **D1 (LOW): Comparador "mi portafolio vs universo"** — Mostrar qué percentil ocupa el portafolio del usuario en el universo de oportunidades (no solo FIBRAs individuales), con un score promedio ponderado por peso de posición. Requiere nuevo endpoint que cruza `portfolio.UserPositions` con el ranking calculado. Evaluar cuando el módulo Portafolio tenga suficiente tracción.

- **D2 (MEDIUM): Snapshots diarios de component scores + deltas ▲/▼ en ComponentBar** — Nueva entidad `FibraScoreSnapshot` (`market.FibraScoreSnapshots`, PK compuesta `(FibraId, SnapshotDate)`), interfaz + repositorio (`GetLatestBeforeAsync`, `UpsertManyAsync`), job Hangfire diario `DailyScoreSnapshotJob` con cron `"0 6 * * *"` usando `OpportunityWeights.Default`, 5 campos delta en `OpportunityFibraRowDto` como `{ get; init; }`, carga de snapshot 7 días atrás en el endpoint, y `ComponentBar` con prop `delta?: number | null` que muestra ▲/▼ si `|delta| ≥ 0.5`. Implementar en historia dedicada del módulo Oportunidades.

- **D3 (LOW): Alertas de cambio relevante de score** — Notificar al usuario cuando una FIBRA sube/baja más de N puntos en el ranking. Prerequisito: D2 (snapshots) debe estar estable primero.

- **D3 (LOW): Histórico de score completo en UI** — Mostrar gráfica de tendencia de score de los últimos 30 días por FIBRA en el panel expandido. Prerequisito igual que D2.

## Deferred from: code review of 7-4-favoritos-marcar-y-destacar-fibras-en-todas-las-superficies (2026-06-05)

- ~~**D1 (MEDIUM): `GetUserId` sin guarda si falta el claim `NameIdentifier`**~~ → **RESUELTO** 2026-06-05: `TryGetUserId` retorna `Guid?` en `OpportunityEndpoints`, `FavoriteEndpoints`, `PortfolioEndpoints` (16 handlers). Cada handler responde 401 si falta el claim.
- **D2 (MEDIUM): Sin FK para `UserFavorite.FibraId`/`UserId`** [`portfolio.UserFavorites`] — Convención del módulo Portfolio: ninguna entidad declara FKs explícitas. Si una FIBRA es eliminada, sus favoritos quedan huérfanos y se siguen devolviendo en `GetFavoriteIdsAsync`. Evaluar al introducir borrado físico de FIBRAs.
- **D3 (LOW): `isAuthenticated` no migrado en 4 callers pre-existentes** [`PublicLayout.tsx`, `LoginPage.tsx`] — Usan `status === 'authenticated'` inline en lugar del nuevo campo `isAuthenticated` del contexto. Pre-existente antes de esta historia; cosmético, no funcional.
- **D4 (LOW): Separador visual desplazado cuando la última fila favorita está expandida** [`OportunidadesPage.tsx:181`, `PositionsTable.tsx:254`] — `showSeparator` se renderiza después de la fila de detalle expandida, no de la fila principal. Edge case visual, sin impacto funcional.
- **D5 (LOW): Gaps de tests en `UserFavoritesRepositoryTests`** — No cubren: concurrencia en `AddAsync` (dos inserciones simultáneas), aislamiento cross-user en `RemoveAsync`, y que el orden retornado por `GetFavoriteIdsAsync` sea estable por `AddedAt`. Mejoras incrementales.
- **D6 (LOW): `fibrasWithoutPrice` puede producir `NaN` en banner de degradación** [`OportunidadesPage.tsx:384`] — Teórico: requeriría que `coverage.universeSize` llegue como string inválido desde el servidor, imposible con la serialización tipada actual. Agregar `|| 0` como guarda defensiva si se detectan problemas en producción.
- **D7 (LOW): `useFavorites()` se instancia en rutas públicas para usuarios anónimos** [`FibraPage.tsx`] — La query está deshabilitada (`enabled: isAuthenticated`), overhead mínimo. Aceptable como está.

## Deferred from: code review of 7-3-monitoreo-de-cobertura-del-universo-y-ranking-degradado (2026-06-05)

- ~~**D1 (HIGH): Pre-existing Task.WhenAll con múltiples repos compartiendo AppDbContext**~~ → **RESUELTO** 2026-06-05 en `fix/deuda-d1-d2-whenall-getuserid`: awaits secuenciales en `OpportunityEndpoints.cs`

- **D5 (MEDIUM): `degradationThresholdPct = 0` haría todo el universo perpetuamente "Degraded"** [`src/Server/Application/Opportunities/UniverseCoverageCalculator.cs`] — Solo alcanzable vía `UPDATE` directo en SQL; el endpoint valida 1–49, `GetAsync` fallback usa default C# `= 30`. Agregar guard `ArgumentOutOfRangeException` o `Math.Max(1, ...)` en historia futura del módulo.
- **D6 (LOW): Status strings `"Normal"/"Degraded"/"Suspended"` sin type union en TypeScript** [`src/Web/Main/src/modules/oportunidades/OportunidadesPage.tsx`] — Comparaciones `coverage?.status === 'Degraded'` son frágiles a renombres silenciosos. Definir `type CoverageStatus = 'Normal' | 'Degraded' | 'Suspended'` cuando se toque este módulo de nuevo.
- **D7 (LOW): Tests de `UniverseCoverageCalculator` sin casos boundary negativos** [`tests/Unit/Application.Tests/Opportunities/UniverseCoverageCalculatorTests.cs`] — Faltan: `fibrasWithPrice > universeSize` (verifica Math.Max guard), `threshold=0` (documenta comportamiento), propagación de `lastValidPriceAt` al resultado. Agregar en próxima historia del módulo Oportunidades.

- **D1 (HIGH): Pre-existing Task.WhenAll con múltiples repos compartiendo AppDbContext** [`src/Server/Api/Endpoints/Private/OpportunityEndpoints.cs`] — El endpoint ya tenía 3 repos en WhenAll violando la convención del proyecto; detectado al agregar el 4to repo. No causado por esta historia. Refactorizar a awaits secuenciales en la próxima historia del módulo Oportunidades.
- **D2 (MEDIUM): `lastValidPriceAt` itera `snapshotByFibra.Values` (todos los snapshots) mientras `fibrasWithPrice` itera solo `fibras` (FIBRAs activas)** [`src/Server/Api/Endpoints/Private/OpportunityEndpoints.cs`] — Fuentes potencialmente inconsistentes si `snapshotByFibra` incluye FIBRAs inactivas. Requiere verificar cómo se construye `snapshotByFibra`; impacto visible solo en `lastValidPriceAt` reportado.
- **D3 (LOW): `universeSize == 0` retorna `"Normal"` — universo vacío indistinguible de buena cobertura** [`src/Server/Application/Opportunities/UniverseCoverageCalculator.cs`] — Comportamiento consciente y testeado; solo relevante en setup inicial sin datos.
- **D4 (LOW): `UpdateData` en migración redundante con `defaultValue: 30`** [`src/Server/Infrastructure/Migrations/20260605183529_AddUniverseDegradationThreshold.cs`] — Sin impacto funcional. No vale la pena una migración correctiva.

## Deferred from: code review of 7-2-vista-promediar-posicion-con-simulador (2026-06-05)

- **D1 (LOW): `staleTime: Infinity` en `rankingQuery` de PromediarTab puede servir scores obsoletos** — Si OportunidadesPage refresca el ranking en background (staleTime default 0), PromediarTab nunca adoptará esos datos actualizados. Tradeoff intencional documentado en dev notes para evitar doble fetch. Evaluar al introducir refetch manual en tab Universo.
- **D2 (LOW): Unicidad de `fibraId` en portfolio positions asumida sin validación** — Si la API retornara la misma FIBRA en múltiples posiciones, `rowByFibraId` descartaría silenciosamente la primera y React emitiría warning por keys duplicadas. Contrato de API fuera del alcance de esta historia.
- **D3 (LOW): `toNum` en `OportunidadesPage.tsx` retorna `undefined` para null** — La función declara retorno `number` pero cuando `v` es null/undefined la rama else retorna el valor crudo. Pre-existente en historias anteriores; evaluar refactor en próxima historia del módulo Oportunidades.
- **D4 (LOW): `ScoreBadge` y `toNum` duplicados entre `PromediarTab.tsx` y `OportunidadesPage.tsx`** — Dev notes de la historia lo permiten explícitamente. Extraer a `oportunidades-ui.ts` compartido cuando se agregue una tercera reutilización.

## Deferred from: code review of 6-9-terminos-footer-contenido (2026-06-04)

- **D1 (MEDIUM): Imposible limpiar `TermsText`/`ContactEmail` una vez guardado** — El repositorio usa `if (termsText is not null)` para decidir si actualizar; el frontend convierte string vacío a `null` con `|| null`. El resultado es que no hay forma de borrar el texto desde la UI. Requiere cambiar la semántica: usar string vacío como señal de "borrar" o agregar endpoints dedicados de clear.
- **D2 (HIGH): Cero tests de integración para `POST /api/v1/account/accept-terms`** [`AccountEndpoints.cs`] — No existe ningún test que valide: 204 para usuario autenticado, 401 para anónimo, 401 para userId inexistente en DB, idempotencia. Agregar en próxima historia del módulo Auth.
- **D3 (HIGH): Cero tests de integración para `GET /api/v1/site-content`** [`OpsConfigEndpoints.cs`] — No se prueba: endpoint público sin token devuelve 200, `TermsEnabled = false` oculta `TermsText` en respuesta, fila de config inexistente. Agregar en próxima historia del módulo Ops.
- **D4 (MEDIUM): `OpsConfigEndpointTests` no resetea `TermsEnabled/TermsText/ContactEmail` en el fixture cleanup** — Si un test guarda `termsEnabled = true`, ese estado persiste para el siguiente test en la fixture compartida. Agregar los nuevos campos al método `ResetOperationalConfigAsync` del fixture.
- **D5 (LOW): `TermsModal` sin focus trap (WCAG 2.5)** [`TermsModal.tsx`] — El modal bloquea visualmente pero no atrapa el foco del teclado. Un usuario puede tabular hacia elementos detrás del overlay. Implementar focus trap y `aria-modal="true"` al mejorar la suite de accesibilidad.
- **D6 (LOW): `UserData` DTO no expone `HasAcceptedTerms`/`TermsAcceptedAt`** [`UserService.cs:ToData`] — Campos en DB pero invisibles por API. Si Ops necesita auditar qué usuarios aceptaron los términos (ej. ante actualización de T&C), estos campos no estarán disponibles. Extender `UserData` cuando se requiera la funcionalidad.
- **D7 (LOW): Email cifrado en JWT claim `email` (pre-existente)** [`TokenService.cs`] — El claim `email` en el token contiene el valor cifrado de la BD, no el plaintext. Si código futuro lee `email` del JWT en el frontend, obtendrá el valor cifrado. Pre-existente; documentar en convenciones al ampliar uso de claims en cliente.

## Deferred from: code review of 5-11-amefibra-pdf-sync (2026-05-31)

- **D1 (MEDIUM): `DownloadPdfAsync` materializa PDF completo en memoria** [`AmefibraDiscoveryClient.cs:DownloadPdfAsync`] — Usa `ResponseHeadersRead` pero luego `ReadAsByteArrayAsync` carga todo el contenido en `byte[]`. Con PDFs grandes o corridas concurrentes puede causar presión de memoria severa.
- **D2 (MEDIUM): Inconsistencia estado manifest/record en error parcial** [`FundamentalsAutomationService.cs:IngestAsync`] — Si `IngestAsync` falla después de insertar el `FundamentalRecord` en BD, el `FundamentalSourceManifest` queda con `LastDecision = "error"` sin `LastProcessedRecordId`. Las corridas futuras detectarán el manifest y marcarán el item como "skip" indefinidamente, dejando el record huérfano. Requiere transacción o manejo de compensación (cleanup del record si el manifest falla).
- **D3 (LOW): `GetCronExpression` silent fallback** [`FundamentalsPipelineSchedule.cs:GetCronExpression`] — Devuelve `"0 */6 * * *"` silenciosamente para valores no reconocidos en lugar de loggear warning. Un valor corrupto de BD pasa invisible para el operador.
- **D4 (MEDIUM): `GetLatestByFibraAndPeriodAsync` filtra solo quarterly** [`FundamentalSourceManifestRepository.cs:GetLatestByFibraAndPeriodAsync`] — La lógica de `possibleUpdate` no detecta reportes anuales con distinto packageUrl para el mismo período. Si AMEFIBRA publica una segunda URL para un reporte anual ya registrado, puede causar violación de la unique constraint `UX_FundamentalSourceManifest_SourceName_PackageUrl`.
- **D5 (LOW): Skips no hidratan `SourcePublishedAt` si quedó null** [`FundamentalsAutomationService.cs:ExecuteAsync`] — Manifiestos en el path de skip no llaman a `HydrateDetailsAsync`. Si `SourcePublishedAt` quedó null en una corrida anterior (portal no disponible), el campo nunca se actualiza en corridas posteriores.
- **D6 (LOW): Sin tests de regresión para FundamentalsHistory y endpoint público** — El spec requiere verificar que `FundamentalsHistory` en Ops y el endpoint público de fundamentales en Main sigan mostrando correctamente registros `ImportedBy = "system:amefibra"` sin duplicados de período. Agregar en próxima historia del módulo Fundamentals.

## Deferred from: code review of spec-4-12-umbral-body-text-ai-noticias (2026-05-31)

- **D1: Artículos Partial por body corto nunca se re-procesan con IA** — `NewsBodyTextRetryJob` solo reintenta artículos con `BodyText IS NULL`. Artículos guardados como Partial por `bodyText.Length < MinBodyTextLengthForAi` (body no nulo pero corto) no son recogidos por el retry job, ni después de un body text edit manual en Ops. No hay mecanismo automático para re-intentar el análisis IA una vez que el body mejora. Considerar: after `UpdateBodyTextAsync` en Ops, si article.Status == Partial → enqueue AI re-analysis.
- **D2: Race condition pre-existente en UpdateConfigAsync** [`AiModeRepository.cs`] — `FindAsync → if null → Add` no es atómico. En BD nueva (antes del primer registro seed), dos PUT concurrentes pueden ambos intentar insertar Id=1. El try/catch DbUpdateException + retry mitiga esto, pero el retry re-entra el mismo path no atómico. Pre-existente; impacto bajo bajo Ops de baja concurrencia.

## Deferred from: code review of 8-2-catalogo-fibras-descripcion-pagina-publica (2026-05-31)

- **D1: ReactMarkdown sin rehype-sanitize en FibraPage pública** [`FibraPage.tsx:239`] — Patrón consistente con NoticiaPage. Descripción solo la escriben AdminOps. Evaluar `rehype-sanitize` si los permisos de escritura se amplían a usuarios no-admin.
- **D2: fetchAllFibras hard-capped a pageSize=100** [`fibrasApi.ts:8`] — Con 20 FIBRAs actuales no es problema. Requiere loop de paginación o endpoint de "all" sin paginar si el universo crece más de 100 FIBRAs activas.
- **D3: Contador "N emisoras activas" usa items retornados, no total del servidor** [`CatalogoPage.tsx`] — Consecuencia del cap de 100. Se corrige junto con D2.
- **D4: Búsqueda en /catalogo no incluye shortName ni nameVariants** [`CatalogoPage.tsx:30`] — FibraListItem no expone esos campos. Requiere ampliar el DTO o un endpoint de búsqueda dedicado.
- **D5: sectionLabels sin useMemo en FibraPage** [`FibraPage.tsx:142`] — Array recreado en cada render. No causa bugs; el diffing de React absorbe el costo. Optimización cosmética.
- **D6: Estado de error en FibraPage no muestra breadcrumb a /catalogo** [`FibraPage.tsx:136`] — El layout principal tiene navegación. Agregar fallback de navegación en FibraErrorState si se mejora la UX de errores.
- **D7: Búsqueda sin debounce — AC8 especificaba "debounced"** [`CatalogoPage.tsx:67`] — El filtrado es client-side sobre datos cargados; debounce no aporta valor. Spec tenía un requisito innecesario; deuda documental.
- **D8: Sin test automatizado para hit directo 200 en /catalogo** — Verificación manual en T9.4. Agregar en story de e2e/infra cuando se implemente suite de smoke tests.

## Deferred from: code review of 5-9-analisis-ia-enriquecido-fundamentales (2026-05-30)

- **D1: `UpdateStatusAsync` guard silencia re-confirmación por actor diferente** [`FundamentalRepository.cs:53-54`] — El guard `if (record.Status == status && status == "processed") return` fue introducido para idempotencia pero impide que un segundo AdminOps registre su nombre como confirmador. Comportamiento deliberado; revisitar si el negocio requiere audit trail de re-confirmaciones.
- **D2: `UpdateKpiExtractionAsync` sobreescribe notas editoriales de Ops al re-extraer** [`FundamentalRepository.cs:87-100`] — Re-extracción reemplaza `FieldNotesJson` completo sin merge con ediciones previas del operador. Agregar lógica de merge (solo actualizar notas de KPIs que cambiaron) en historia futura del módulo IA si el workflow de edición → re-extracción se vuelve común.
- **D3: Race condition — dos records `processed` para el mismo período sin unique constraint** [`FundamentalRepository.cs`] — Pre-existente. No hay índice único `(FibraId, Period)` que evite dos confirmaciones concurrentes. Agregar índice único en próxima migración del módulo fundamentales.
- **D4: Parser sigue leyendo campo legacy `summary` que el nuevo prompt ya no emite** [`KpiExtractionJsonParser.cs:84`] — Dead code inofensivo; el fallback `SummaryMarkdown ?? Summary` funciona. Limpiar al deprecar el campo `Summary` del contrato de extracción en una historia futura.

## Deferred from: code review of 5-8-observabilidad-llamadas-ia-log-en-ops (2026-05-30)

- **D1: Índice `(Provider, CreatedAt)` ausente en `AiCallLogConfiguration`** [`src/Server/Infrastructure/Persistence/SqlServer/Configurations/Ai/AiCallLogConfiguration.cs:41`] — Dev Notes documenta que debería existir. Solo se creó `(Operation, CreatedAt)`. Añadir en próxima migración del módulo Ai.
- **D2: Esquema de entidad diverge del spec AC1** [`src/Server/Domain/Ai/AiCallLog.cs`] — Spec define `Model`, `InputChars`, `OutputChars`, `ErrorType`; implementado sin `OutputChars` ni `ErrorType`. Documentado en Completion Notes. Alinear si se necesita para reportes externos o requiere migración.
- **D3: `OrderByDescending` antes de `CountAsync`** [`src/Server/Infrastructure/Persistence/Repositories/Ai/AiCallLogRepository.cs:27-28`] — Genera ORDER BY innecesario en query de conteo. SQL Server lo ignora, sin impacto funcional. Mover en próxima refactor del repositorio.
- **D4: `AsyncLocal` sin cleanup en `AiCallRawData`** [`src/Server/Infrastructure/Integrations/Ai/AiCallRawData.cs`] — No existe método `End()` para limpiar el contexto. Riesgo teórico de context bleed bajo Hangfire. Bajo impacto con `WorkerCount=1`.
- **D5: Paginación sin snapshot isolation** [`src/Server/Infrastructure/Persistence/Repositories/Ai/AiCallLogRepository.cs:28-32`] — `CountAsync` y `ToListAsync` en dos queries separadas; `total` puede diferir de `items` bajo inserción concurrente. Aceptable para MVP de observabilidad.
- **D6: `newsequentialid()` como SQL default nunca se usa en `Id`** [`src/Server/Domain/Ai/AiCallLog.cs:5`] — `AiCallLogConfiguration` define `HasDefaultValueSql("newsequentialid()")` pero sin `ValueGeneratedOnAdd()`, por lo que EF siempre envía `Guid.NewGuid()` del constructor. El índice PK queda con GUIDs no secuenciales → fragmentación. Añadir `ValueGeneratedOnAdd()` al Id en próxima migración.
- **D7: Test 403 ausente** [`tests/Integration/Api.Tests/Ops/AiCallLogEndpointTests.cs`] — Solo existe test de 401; falta test con usuario autenticado sin rol AdminOps.

## Deferred from: code review of 5-5-fundamentales-catalogo-pdf-md-y-summary-ia (2026-05-26)

- **D1: Race condition en uploads concurrentes del mismo registro** [`src/Server/Api/Endpoints/Ops/OpsFundamentalsEndpoints.cs`] — Dos requests simultáneos pueden sobreescribir el mismo archivo y ejecutar `UpdatePdfReferenceAsync` + `UpdateMarkdownContentAsync` de forma no atómica. Herramienta Ops de baja concurrencia; pre-existente en la lógica de PDF. Mitigar con exclusión mutex por id si se detecta en producción.
- **D2: Race condition en ai-summary — doble gasto de créditos IA** [`src/Server/Api/Endpoints/Ops/OpsFundamentalsEndpoints.cs`] — Dos requests simultáneos pasan la validación de MarkdownContent y ambos llaman a GenerateSummaryAsync; resultado: dos llamadas al proveedor IA con costo duplicado. Baja probabilidad en Ops; mitigar con campo `generating` o mutex por id en historia futura.
- **D3: HttpClient timeout 30 s insuficiente para Document + retry** [`src/Server/Infrastructure/Integrations/Ai/GeminiAiSummaryService.cs`, `DeepSeekAiSummaryService.cs`] — Para documentos largos con AiContentType.Document, el primer intento puede tardar >30 s con razonamiento extendido. Pre-existente. Ajustar timeout en configuración si se detecta en producción.
- **D4: Record huérfano si `uploadFundamentalPdf` falla post-import** [`src/Web/Ops/src/modules/fundamentals/FundamentalsImportForm.tsx`] — El import crea el record (estado pending/partial) antes de intentar el upload del PDF; si el upload falla, el record queda en BD sin PDF ni Markdown y el usuario no recibe indicación de que existe. Arquitectura pre-existente de dos pasos; mejorar con mensaje de error que indique "record creado, sube el PDF desde historial".
- **D5: Ordenamiento de períodos por SUBSTRING string en SQL** [`src/Server/Infrastructure/Persistence/Repositories/Fundamentals/FundamentalRepository.cs`] — `GetLatestProcessedByFibraAsync` ordena por año/trimestre como string via SUBSTRING. Pre-existente; riesgo mínimo con el formato Q#-YYYY.
- **D6: Separador de ruta OS vs relativePath con `/`** [`src/Server/Api/Endpoints/Ops/OpsFundamentalsEndpoints.cs`] — `Path.Combine` usa `\` en Windows mientras `relativePath` se construye con `/`. Pre-existente; sin impacto en runtime.
- **D7: UglyToad.PdfPig en versión prerelease** [`Directory.Packages.props`] — Versión `1.7.0-custom-5` no es release oficial. Única opción MIT/pure-.NET documentada. Actualizar cuando se publique versión estable.
- **D8: Updates pdfReference y markdownContent en dos transacciones separadas** [`src/Server/Infrastructure/Persistence/Repositories/Fundamentals/FundamentalRepository.cs`] — Si el proceso termina entre los dos SaveChanges, el registro queda con pdfReference pero sin MarkdownContent. Estado recuperable (re-subir PDF re-extrae el MD). Refactorizar a transacción única si se añade consistencia fuerte.


## Deferred from: code review of 5-4-configuracion-operativa-desde-ops-sin-redespliegue (2026-05-25)

- **D1: Sin transacción entre `SaveChangesAsync` y `Hangfire.AddOrUpdate`** [`src/Server/Api/Endpoints/Ops/OpsConfigEndpoints.cs`] — Si Hangfire falla post-commit, la BD tiene la nueva cadencia pero el job mantiene el schedule anterior. Mitiga: el arranque lee BD y corrige. Implementar compensación si se detecta el fallo en producción.
- **D2: Race condition teórico en PUT concurrente** [`src/Server/Api/Endpoints/Ops/OpsConfigEndpoints.cs`] — Dos PUTs simultáneos pueden llamar `Hangfire.AddOrUpdate` dos veces con el mismo cron (idempotente). Admin-only, probabilidad despreciable. Resolver con rowversion/`IsConcurrencyToken` en `OperationalConfig` si se añade UI multi-usuario.
- **D3: Validaciones de negocio solo en capa HTTP** [`src/Server/Infrastructure/Persistence/Repositories/Ops/OperationalConfigRepository.cs`] — `commissionFactor > 0 && <= 0.1` y `avgPeriods` 1–20 no se validan en el repositorio. Patrón ya aceptado en `AiModeConfig` y otros repos del proyecto.
- **D4: `FIBRADIS_SKIP_STARTUP_DB_READS` env var redundante con try/catch** [`src/Server/Api/Program.cs`] — El guard extra previene leer la BD durante generación de OpenAPI en build-time; el try/catch ya lo cubre. Simplificar al try/catch solo en refactor de Program.cs.

Items diferidos durante code reviews. Cada sección tiene la historia origen y la fecha.

## Deferred from: code review of news-soft-delete-cancel-dedup (2026-05-31)

- **D1: `deleteNewsArticle` usa `fetch()` raw en lugar del typed client** [`src/Web/Ops/src/api/newsApi.ts`] — El endpoint DELETE no está en el schema generado aún (requiere restart del API server). Migrar al client tipado `apiClient['/api/v1/ops/news/{articleId}'].DELETE(...)` en la próxima sesión de codegen.
- **D2: DELETE endpoint no registra audit trail** [`src/Server/Api/Endpoints/Ops/AiModeEndpoints.cs`] — El actor (email del AdminOps) no se persiste al soft-deletear. Si se requiere trazabilidad de eliminaciones, agregar campo `DeletedBy` a `NewsArticle` y capturarlo del JWT claim.
- **D3: `GetExistingUrlsAsync` incluye URLs de artículos soft-deleted** [`src/Server/Infrastructure/Persistence/Repositories/News/NewsRepository.cs`] — Una URL soft-deleted nunca puede ser re-ingestada. Decisión deliberada (si un admin eliminó el artículo, la URL sigue ocupada); revisar si el caso de uso de "restaurar artículo eliminado" surge.
- **D4: Métricas del pipeline no cuentan title-duplicates guardados como deleted** [`src/Server/Infrastructure/Jobs/News/NewsPipelineJob.cs`] — Los `saved` count solo incluye artículos fresh. Title-dups salvados con `DeletedAt` no se contabilizan. Agregar contador si las métricas de ingesta necesitan reflejar el total de filas escritas.

## Deferred from: spec-ops-pdf-feedback-main-period-selector (2026-05-27)

- **D5: Selector de período no indica truncamiento** [`src/Web/Main/src/modules/ficha-publica/FibraPage.tsx`] — El endpoint retorna máximo 12 períodos sin notificar al frontend que hay más. Si una FIBRA acumula >12 períodos procesados, los más antiguos no son accesibles. Considerar agregar un header `X-Total-Count` o un campo `truncated: bool` en la respuesta.

## Deferred from: code review of 5-3-gestion-del-catalogo-de-fibras-desde-ops (2026-05-23)

- **D1: GetAllAsync sin paginación ni límite** [`FibraRepository.cs:53`] — Aceptable para el tamaño actual del catálogo (~6 FIBRAs); añadir paginación cuando el catálogo crezca.
- **D2: `State` serializado como `ToString()` sin contrato explícito** [`OpsCatalogEndpoints.cs:349`] — Patrón consistente en el proyecto; considerar JsonConverter si hay clients heterogéneos en el futuro.
- **D3: ILoggerFactory instanciado por request** [`OpsCatalogEndpoints.cs:43`] — Impacto de performance despreciable para endpoint Ops de baja frecuencia; refactorizar a ILogger<T> en limpieza general.
- **D4: `UpdateAsync` llama `db.Fibras.Update()` en entidad ya tracked** [`FibraRepository.cs:21`] — Genera UPDATE completo en vez de diferencial, pero correcto; refactorizar con mejora de EF tracking en futura épica.
- **D5: `GetActor` fallback a "unknown" sin log de advertencia** [`OpsCatalogEndpoints.cs:352`] — Riesgo bajo para MVP con AdminOps autenticado; añadir `LogWarning` en siguiente historia de auditoría (historia 5-4).

## Deferred from: code review of 5-2-importacion-de-fundamentales-en-modo-manual (2026-05-23)

- **W1: GetByFibraAsync sin paginación** [`FundamentalRepository.cs:23`] — Retorna todos los registros históricos por FIBRA sin límite. Añadir paginación en historia futura de historial Ops cuando el volumen de datos sea relevante.
- **W2: Magic strings de status sin constantes** [`FundamentalRecord.cs`] — "pending", "partial", "processed", "error" repetidos en dominio, repositorio, endpoints y tests sin fuente única. Extraer en enum o clase de constantes en refactor futuro.
- **W3: Uploads PDF concurrentes pueden corromper archivo** [`OpsFundamentalsEndpoints.cs:205`] — File.Create trunca el archivo si dos uploads del mismo record llegan simultáneamente. File locking o nombrado único por timestamp para MVP; implementar si el uso concurrent se detecta en producción.
- **W4: Case sensitivity en GetByTickerAsync** [`FundamentalsEndpoints.cs`] — Problema pre-existente del módulo catalog: si un ticker está en minúsculas en la BD, GetByTickerAsync con input en mayúsculas retorna null. Resolver en historia de gestión de catálogo (Épica 5).

## Deferred from: code review of 5-0-ops-shell-navegacion-y-modulos (2026-05-23)

- **D1: Fallback hardcoded AiPromptTemplateDefaults sin validación de placeholders** [`src/Server/Infrastructure/Integrations/Ai/AiPromptTemplateDefaults.cs`] — El fallback no se valida automáticamente para contener los mismos placeholders que el código de interpolación espera. Riesgo de regresión silenciosa si el template se modifica en el futuro.
- **D2: GetPromptAsync puede lanzar excepción sin try/catch en BuildPromptAsync** [`src/Server/Infrastructure/Integrations/Ai/GeminiAiSummaryService.cs`, `DeepSeekAiSummaryService.cs`] — El job lo atrapa pero no distingue entre fallo de IA y fallo de BD del prompt. Mejora de observabilidad futura.
- **D3: CreatedAt HasDefaultValueSql("getutcdate()") sin ValueGeneratedOnAdd — letra muerta** [`src/Server/Infrastructure/Persistence/SqlServer/Configurations/Jobs/PipelineErrorLogConfiguration.cs`] — EF envía el valor de C# en el INSERT; la SQL default nunca se ejecuta. Inconsistencia no funcional.
- **D4: PipelineErrorLog sin mecanismo de retención o purga** [`src/Server/Application/Jobs/IPipelineErrorLogRepository.cs`] — La tabla puede crecer indefinidamente. Agregar `DeleteOldEntriesAsync` o un job de limpieza en Épica 5.
- **D6: PipelineRunLog sin mecanismo de retención o purga** [`src/Server/Application/Jobs/IPipelineRunLogRepository.cs`] — La nueva tabla de auditoría de ejecuciones también crece indefinidamente. Agregar `DeleteOldEntriesAsync` o un job de limpieza en una historia futura de Ops.
- **D5: Mensaje de validación PUT ai-prompts no especifica qué placeholder falta** [`src/Server/Api/Endpoints/Ops/OpsAiPromptEndpoints.cs`] — Siempre muestra los tres placeholders aunque solo falte uno. Mejora menor de UX.

## Deferred from: code review of 5-1-dashboard-operativo-y-control-de-pipelines (2026-05-23)

- **`GetActor` puede retornar GUID si falta claim de email** [`OpsMarketEndpoints.cs`] — Fallback a `ClaimTypes.NameIdentifier` es un GUID/sub opaco, no un email. Sigue el patrón Dev Notes; email siempre presente para AdminOps con JWT actual. Revisar si se añaden tipos de token sin email.
- **Dashboard muestra "Sin datos" tras trigger manual** [`DashboardPage.tsx`] — `GetLastCompletedAsync` excluye Queued, el badge no cambia hasta job completado. UX improvement: añadir optimistic update o mensaje "En cola..." usando el entry Queued visible en recentRuns.
- **`PipelineRunLogConfiguration` sin índice en columna `Status`** [`PipelineRunLogConfiguration.cs`] — `GetLastCompletedAsync` filtra por `(Pipeline, Status IN ...)` con solo índice en `(Pipeline, StartedAt)`. Optimización prematura dado volumen operativo actual; revisar si el historial crece significativamente.
- **`OpsMarketEndpoints` contiene rutas del pipeline de noticias** [`OpsMarketEndpoints.cs`] — Naming confusion pre-existente de story 5-0. Mover `newsGroup` a `OpsNewsPipelineEndpoints.cs` en refactor futuro.
- **Jobs registran `OperationCanceledException` como `Status="Failed"`** — Shutdown limpio de Hangfire deja pipelines mostrando "Fallando". Por diseño per spec actual. Añadir `Status="Cancelled"` en futura historia de observabilidad si genera ruido operativo.

## Deferred from: code review of ops-session-stability (2026-05-23)

- **`_refreshInFlight` singleton no exporta reset para tests** [`src/Web/Ops/src/api/authApi.ts`] — La variable de módulo persiste entre tests si se agrega cobertura. Exportar una función `_resetRefreshInFlight()` o mover a factory para aislar en tests.
- **`setPassword('')` persiste plaintext en estado React durante toda la sesión** [`OpsLoginGate.tsx`] — Con access token de 8h, la contraseña vive hasta 8h en memoria. Pre-existente; consider usar un ref en lugar de state, o limpiar el password en el effect de autenticación.
- **Tab freeze / background throttle puede saltarse el refresh proactivo** [`OpsLoginGate.tsx`] — Chrome Memory Saver y iOS Safari pueden suspender `setInterval` en tabs en background. Añadir listener `document.visibilitychange` que llame `refreshOpsSession()` cuando el tab vuelva a ser visible.
- **`AccessTokenMinutes` como string en lugar de número en JSON** [`appsettings.json`] — Pre-existente. El valor `"480"` debería ser `480` (sin comillas) para consistencia con JSON schema; C# lo parsea con `int.Parse` por lo que funciona, pero es frágil ante errores tipográficos.
- **Sin fuente única de verdad para el lifetime del token** [`OpsLoginGate.tsx` / `appsettings.json`] — `PROACTIVE_REFRESH_MS = 4h` está hardcodeado en frontend independientemente del valor de `AccessTokenMinutes` en backend. Si el backend cambia el lifetime, el frontend no lo sabe. Considerar incluir `expiresIn` en el response de login/refresh para que el frontend derive el intervalo dinámicamente.

## Deferred from: code review of 4-1-ingesta-rss-blocklist-y-deduplicacion-de-noticias (2026-05-19)

- **GUIDs de seed generados con MD5** [`NewsSeed.cs:GuidFromKey`] — Si se modifica o reordena `DefaultBlocklist`, los GUIDs cambian y EF emite DELETE+INSERT en la siguiente migración. Considerar GUIDs literales hardcodeados.
- **`PublishedAt` sin valor por defecto** [`NewsArticle.cs`] — Queda como `DateTimeOffset.MinValue` si código futuro no lo asigna; puede causar ordering inesperado en queries `ORDER BY PublishedAt DESC`.
- **RSS fetches secuenciales** [`NewsPipelineJob.cs`] — N fibras × M queries = N×M llamadas HTTP en serie. Intencional (rate limiting Google News), pero el tiempo de ejecución escala linealmente. Revisar si el número de FIBRAs crece significativamente.
- **`CancellationToken.None` en Hangfire** [`Program.cs:50`] — Sin cancelación graceful en shutdown del host. Mismo patrón que `MarketPipelineJob`; abordar junto con ese job en Epic 5 si se necesita shutdown determinístico.
- **`AddAsync` con `SaveChangesAsync` individual** [`NewsRepository.cs`] — N round-trips a SQL Server en lugar de un batch. Race condition cubierta por unique constraint; aceptable en volumen MVP (~50-200 artículos/hora).
- **`Status` como `nvarchar(16)`** [`NewsArticleConfiguration.cs`] — Frágil si futuros valores del enum superan 16 chars. Ampliar a 32 en próxima migración del módulo `news`.

## Deferred from: code review of 4-1-ingesta-rss-blocklist-y-deduplicacion-de-noticias — 3ª pasada (2026-05-19)

- **`GetExistingUrlsAsync` límite 2100 parámetros SQL IN** [`NewsRepository.cs:23`] — EF Core traduce `.Contains()` en IN-clause; SQL Server limita ~2100 parámetros. Con >100 FIBRAs activas podría alcanzarse. Fix: chunking del array antes del query.
- **`FetchAsync` traga `OperationCanceledException`** [`GoogleNewsRssClient.cs:17`] — `catch (Exception)` captura cancelación. Alineado con patrón CancellationToken.None ya deferido. Resolver junto con graceful shutdown global.
- **Rate-limit/bloqueo Google News silencioso** [`NewsPipelineJob.cs`] — Si Google bloquea la IP o responde 429, todos los FetchAsync devuelven `[]` sin que el job lo distinga de "sin noticias nuevas". Saved=0/errors=0 no dispara alerta. Considerar métricas por query en Epic 5.
- **`[DisableConcurrentExecution]` ausente en `NewsPipelineJob`** [`NewsPipelineJob.cs`] — Hangfire puede solapar ejecuciones si una tarda más de 1h. El unique index en URL absorbe duplicados con `errors++` espurios. Agregar el atributo junto con la próxima modificación del job.
- **Test del pipeline no cubre general queries** [`NewsPipelineJobTests.cs`] — `FakeRssClient` retorna el mismo set para cualquier query; una regresión que elimine el `foreach (GeneralQueries)` no sería detectada. Agregar test explícito cuando se extienda el job.

## Deferred from: code review of 4-2-asociacion-de-noticias-con-fibras-y-display-en-home-y-ficha (2026-05-19)

- **`GetLatestForFibraAsync` NullReferenceException teórico** [`NewsRepository.cs:58-65`] — EF Core genera INNER JOIN; filas huérfanas son imposibles con FK+cascade. Solo relevante ante corrupción directa de BD.
- **AC2 sin test de integración** [`NewsEndpoints.cs`] — `Associate_NoMatchReturnsEmpty` cubre la lógica; falta test end-to-end que verifique artículo sin asociación aparece en `/api/v1/news` pero no en `/api/v1/news/fibras/{id}`.
- **`JSON.stringify(error)` en mensajes de throw** [`newsApi.ts`, `fibraNewsApi.ts`] — Patrón heredado de `fibrasApi.ts`. No llega al usuario final pero sería mejor serializar solo el status o el `domainCode`.

## Deferred from: code review of 4-2-asociacion-de-noticias-con-fibras-y-display-en-home-y-ficha — 2ª pasada (2026-05-19)

- **DbContext en estado sucio tras `SaveChangesAsync` fallido** [`NewsRepository.cs:AddWithLinksAsync`] — Tras una `DbUpdateException` (ej. URL duplicada), las entidades en estado `Added` no se desvinculan del tracker. El siguiente `SaveChangesAsync` del mismo scope intenta persistir AMBAS entidades (la fallida + la nueva), repitiendo el error en cascada para el resto del batch. Fix: detach entidades en error tras la excepción, o usar `IDbContextFactory` con un contexto por artículo. Pre-existing del AddAsync de story 4.1.
- **Variante de nombre que normaliza a ≤2 chars matchea cualquier token del mismo tamaño** [`NewsAssociator.cs:MatchesVariant`] — Si una FIBRA tiene una variante de nombre que `NormalizeTitle` reduce a 1-2 caracteres (datos corruptos o abreviaciones), matcheará casi cualquier artículo. Agregar guard `normalizedVariant.Length >= 3` en `MatchesVariant`. Teórico con datos actuales (variantes son frases multi-word).

## Deferred from: code review of 2-5-home-topmovers-tabla-y-ganadores-perdedores (2026-05-19)

- **`dailyChangePct = 0` excluido silenciosamente de GainersLosers** [`movers-logic.ts:39,44`] — El filtro `> 0` / `< 0` excluye valores exactamente cero sin indicación al usuario. Comportamiento no especificado en los AC; abordar si el negocio lo requiere.
- **Doble llamada a `numOf` en comparador de `getTopMovers`** [`movers-logic.ts:24-26`] — Micro-optimización: `numOf` se llama dos veces por elemento por comparación. Refactorizar a variable local si el corpus de snapshots crece.
- **`formatVolume`: rango [999_500, 1_000_000) muestra "1000K"** [`movers-logic.ts:15`] — Edge case de formateo: `(999_500 / 1_000).toFixed(0) = "1000"` → "1000K". Sin impacto con volúmenes actuales de FIBRAs.
- **`TopMovers` sin empty state cuando `snapshots = []`** [`TopMovers.tsx`] — Si la API devuelve array vacío y no hay error, el componente renderiza un contenedor vacío sin mensaje. Inconsistente con `GainersLosers` que sí tiene empty state.

## Deferred from: code review of 4-3-soporte-para-ai-mode-en-noticias-off-y-manual (2026-05-19)

- **Inyección de prompt via `title`/`snippet`** [`AnthropicAiSummaryService.cs:26-33`] — Inputs RSS interpolados directamente en el prompt sin sanitizar saltos de línea. Fuente RSS pre-filtrada; surface de ataque baja en MVP. Sanitizar con `ReplaceLineEndings(" ")` cuando el corpus de fuentes se amplíe.
- **TOCTOU modo Off→Manual entre check y llamada a Anthropic** [`AiModeEndpoints.cs:69,84`] — Ventana temporal muy pequeña para operación admin manual. Documentar como limitación conocida o releer el modo dentro del try si el problema emerge en producción.
- **HTTP 429 tratado igual que 500 sin retry/backoff** [`AnthropicAiSummaryService.cs:53`] — Sin distinción de códigos de error Anthropic. Implementar retry con `Retry-After` header cuando se active uso intensivo de la API.
- **Artículos `Pending` visibles en endpoints públicos** [`NewsRepository.cs`] — Comportamiento pre-existente desde historia 4.1. En modo Manual los artículos quedan en `Pending` hasta que el admin dispara el resumen; el fallback `aiSummary ?? snippet` los muestra correctamente pero sin resumen. Evaluar si se debe ocultar `Pending` en futuras historias del módulo AI.
- **`PreviousMode = null` en rama de creación de `AiModeConfig`** [`AiModeRepository.cs:27-34`] — Rama inalcanzable en producción (seed EF garantiza fila Id=1). Completar el objeto si se agrega un test para esa rama.
- **Sin tests para endpoint `POST /{id}/ai-summary`** [`AiModeEndpoints.cs`] — Task 9 del spec solo exige tests del pipeline. Los ACs 2 y 3 del endpoint de trigger manual no tienen cobertura. Agregar tests de integración en historia 5.x que extienda el módulo AI.
- **`UpdateSummaryAsync` silencioso con 0 filas afectadas** [`NewsRepository.cs:54-59`] — Si el artículo fue eliminado entre `GetByIdAsync` y `ExecuteUpdateAsync`, la operación retorna 0 sin error. Probabilidad muy baja en operación admin-only; verificar filas afectadas si se habilita borrado de artículos.
- **Token expirado sin refresh en `aiModeApi.ts`** [`aiModeApi.ts`] — Patrón heredado de `newsApi.ts`. Implementar refresh flow cuando se añada gestión de sesión al Ops SPA.
- **Cambio de modo a mitad de ejecución del pipeline** [`NewsPipelineJob.cs:25`] — Por diseño según spec (aplica en el siguiente ciclo). Documentar en las notas de operación del pipeline si la duración del job crece significativamente con más FIBRAs.

## Deferred from: code review of 4-3-soporte-para-ai-mode-en-noticias-off-y-manual — 2ª pasada (2026-05-19)

- **`opsAccessTokenStorageKey` y `getAuthHeaders()` duplicados** [`aiModeApi.ts`, `newsApi.ts`] — Patrón copy-paste pre-existente en todo el Ops SPA. Refactorizar a un módulo de auth compartido cuando se implemente gestión de sesión/refresh.
- **`SetAiModeRequest.Mode` acepta strings numéricos vía `Enum.TryParse`** [`AiModeEndpoints.cs`] — `"0"` o `"1"` son válidos como Mode (se parsean como `AiMode.Off`/`Manual`). Endpoint admin-only, impacto bajo; la respuesta serializa el enum como "Off"/"Manual" correctamente.

## Deferred from: code review of 4-3-soporte-para-ai-mode-en-noticias-off-y-manual — 4ª pasada (2026-05-19)

- **Anthropic error body descartado en fallo de `EnsureSuccessStatusCode`** [`AnthropicAiSummaryService.cs`] — El operador solo ve el código HTTP (401, 429, 500) en el log, no el mensaje de Anthropic. Leer y loguear el body del error antes de lanzar en una próxima mejora de observabilidad.
- **`GetConfigAsync` fallback in-memory con `UpdatedAt` variable** [`AiModeRepository.cs:GetConfigAsync`] — Si la fila seed no existiera, dos GETs consecutivos devolverían timestamps distintos. Inalcanzable en producción; completar solo si se añade un test explícito para esa rama.

## Deferred from: code review of 4-3-soporte-para-ai-mode-en-noticias-off-y-manual — 5ª pasada (2026-05-19)

- **`UpdatedAt` calculado antes de `SaveChangesAsync`** [`AiModeRepository.cs:25`] — En caso de retry el timestamp del primer intento se pierde; el registro de auditoría refleja el momento del intento, no del commit. Menor impacto en auditoría; asignar `DateTimeOffset.UtcNow` justo antes del `SaveChangesAsync` si la precisión importa.
- **Retry de insert concurrente puede relanzar en réplica de lectura** [`AiModeRepository.cs:44-48`] — `FindAsync` post-`ChangeTracker.Clear()` puede retornar null si la réplica de lectura no ha replicado el insert ganador, relanzando `DbUpdateException` como 500. Improbable con SQL Server single-node; resolver si se añade read replica.
- **Actor fallback `"unknown"` persiste sin log de advertencia** [`AiModeEndpoints.cs:46-49`] — La cadena Name→Email→NameIdentifier→"unknown" es funcional, pero `"unknown"` en `updated_by` no identifica al actor real. Añadir `LogWarning` cuando se cae en "unknown" para detectar JWTs con claims ausentes.
- **PK singleton `1` hardcodeado en 5 lugares** [`AiModeRepository.cs`, `AiModeConfig.cs`, `AiModeConfigConfiguration.cs`] — Extraer `public const int SingletonId = 1` en `AiModeConfig` y referenciar desde repositorio y configuración EF en próxima iteración del módulo AI.
- **Modo stale en ventana de refetch (~200ms) tras Off→Manual** [`AiModeSection.tsx:104`] — El botón de trigger queda disabled brevemente después de guardar cambio Off→Manual porque `currentMode` lee del caché obsoleto. Resolver con `queryClient.setQueryData` optimista en `saveMutation.onSuccess`.

## Deferred from: code review of 4-3-soporte-para-ai-mode-en-noticias-off-y-manual — 6ª pasada (2026-05-20)

- **Gemini API key en query param `?key=` visible en logs HTTP/telemetría** [`GeminiAiSummaryService.cs:42`] — Usar header `x-goog-api-key` sería mejor para log hygiene; el spec Dev Notes especifica la URL con `?key={apiKey}` siguiendo la documentación oficial de Gemini. Migrar si se añade observabilidad centralizada de HTTP.

## Deferred from: code review of 4-3-soporte-para-ai-mode-en-noticias-off-y-manual — 7ª pasada (2026-05-20)

- **`ILoggerFactory.CreateLogger` por request en lugar de `ILogger<T>` tipado** [`AiModeEndpoints.cs:71`] — Allocación menor en endpoint admin-only de bajo volumen. Refactorizar a inyección tipada si el endpoint crece.
- **Fallback `AiMode.Off` ante BD caída puede dejar artículos del período de falla como `Processed` permanentemente** [`NewsPipelineJob.cs:34-42`] — En modo Manual, artículos ingestados durante falla de BD quedan `Processed` sin AI; el guard de idempotencia bloquea re-proceso posterior. Trade-off del diseño de fallback; el admin puede hacer trigger manual de artículos específicos.
- **Safety-block de Gemini (HTTP 200 sin candidatos) devuelve 502 "proveedor no disponible"** [`GeminiAiSummaryService.cs:67-71`, `AiModeEndpoints.cs:125-127`] — El mensaje 502 implica falla de red cuando la causa real es bloqueo por política de contenido. El log de error contiene la `InvalidOperationException` con detalle. Mejorar al añadir observabilidad centralizada.
- **`modeQuery.data?.mode as 'Off' | 'Manual'` sin validación en runtime** [`AiModeSection.tsx:30`] — TypeScript `as` no valida en runtime; un tercer modo del backend deshabilitaría el panel de trigger sin mensaje claro. Validar al extender el enum.

## Deferred from: code review of 4-3-soporte-para-ai-mode-en-noticias-off-y-manual — 8ª pasada (2026-05-20)

- **Race condition: dos requests concurrentes de AdminOps sobre el mismo artículo `Pending` pasan el guard de idempotencia y llaman a Gemini dos veces** [`AiModeEndpoints.cs:85-100`] — Endpoint admin-only de muy bajo volumen; worst case = cuota Gemini desperdiciada + segundo summary sobreescribe al primero de forma inocua. Resolver con `ExecuteUpdateAsync WHERE Status != Processed` atómico si el uso aumenta.
- **`AiSummary` se persiste sin validar longitud máxima** [`GeminiAiSummaryService.cs:78`, `NewsRepository.cs:58`] — Columna `nvarchar(2048)`; 256 output tokens ≈ 800-1000 chars, bien bajo el límite, pero no hay truncación explícita. Si se excediera, `DbUpdateException` sería capturada como fallo de proveedor con mensaje "proveedor no disponible" que es engañoso. Agregar `text.Truncate(2048)` o aumentar límite de columna si se amplía `maxOutputTokens`.

## Deferred from: code review of 2-6-home-reorganizacion-y-tabla-universo-fibras (2026-05-21)

- **Sin roles ARIA de tabla en `FibraUniverseTable`** [`FibraUniverseTable.tsx:52-210`] — El grid CSS no lleva `role="table"/"row"/"columnheader"/"cell"`. Pre-existing pattern en GainersLosers y TopMovers (mismo enfoque visual). Revisar en historia de mejoras de accesibilidad WCAG si el score de audit lo requiere.

## Deferred from: code review of 4-5-1-scraping-imagen-ogimage-y-fallback-visual (2026-05-20)

- **Regex backtracking teórico en OgImageScraper** [`OgImageScraper.cs`] — `[^>]*` bounded por `>` limita el backtracking; GeneratedRegex de .NET optimiza el patrón. Riesgo bajo en producción. Evaluar `RegexOptions.NonBacktracking` si se detecta CPU spikes en métricas.
- **Scraping og:image secuencial bloquea pipeline ~5s por artículo nuevo** [`NewsPipelineJob.cs`] — Intencional per Dev Notes (rate limiting implícito). Revisar si el volumen de artículos nuevos crece significativamente; considerar throttled `Task.WhenAll` en ese momento.
- **Sin retry/circuit-breaker en OgImageScraper** [`ApiServiceExtensions.cs`] — Dominio caído consume 5s por artículo en cada run. Agregar Polly circuit-breaker por host en Epic 5 junto con la infraestructura de resiliencia global.
- **Regex no cubre atributos HTML5 sin comillas en `<meta>`** [`OgImageScraper.cs`] — Atributos unquoted son válidos en HTML5 pero infrecuentes. No requerido por spec; revisar si tasa de `imageUrl=null` en producción sugiere miss rate alto.
- **Race condition entre ejecuciones concurrentes del pipeline** [`NewsPipelineJob.cs`] — Pre-existing de historia 4.1 (`[DisableConcurrentExecution]` ausente). Dos runs simultáneos pueden scraper la misma URL dos veces e intentar insertar el mismo artículo.

## Deferred from: code review of 6-4-edicion-inline-y-eliminacion-de-posiciones (2026-06-03)

- **D1 (MEDIUM): Stale data en onSave — campo "par" usa valor pre-refetch** [`PositionsTable.tsx`] — `onSave={(newVal) => onUpdate(position.fibraId, newVal, Number(position.costoPromedio))}` captura `costoPromedio` del render actual. Si el usuario edita Títulos → guarda → edita CostoPromedio antes de que el refetch complete, el segundo PATCH sobreescribe el primer campo con el valor viejo. Requiere cambio arquitectónico: optimistic updates o read-before-write en backend.
- **D2 (MEDIUM): DELETE no idempotente en retry de red** [`PortfolioEndpoints.cs`] — Si la respuesta DELETE se pierde en tránsito y el cliente reintenta, recibe 404 y muestra error aunque la posición fue eliminada exitosamente. Decisión de diseño REST; cambiar a `return Results.NoContent()` siempre si se quiere semántica idempotente.
- **D3 (MEDIUM): `db.Update()` sobre entidad ya rastreada — frágil ante futuros refactors** [`PortfolioRepository.cs`] — El endpoint llama `GetPositionAsync` (EF trackea la entidad) y luego `UpdatePositionAsync` que llama `db.Update()`. Funciona hoy porque es la misma instancia, pero si `GetPositionAsync` se cambia a `.AsNoTracking()` en el futuro, `Update()` puede lanzar `InvalidOperationException` con una instancia desconectada.
- **D4 (MEDIUM): Sin integration tests para endpoints PATCH y DELETE** — Los tests nuevos son solo de repository (InMemory EF). La autorización (`RequireAuthorization("User")`), el binding JSON y la deuda D1 (`GetUserId` con `FormatException`) no están cubiertos end-to-end. Deuda de proyecto.
- **D5 (LOW): `CostoPromedio` sin límite de escala decimal en el DTO** [`PortfolioPositionPatchDto`] — `parseFloat` sin redondeo en el frontend. Si la columna BD tiene scale fijo (e.g. `decimal(18,4)`), EF puede truncar silenciosamente en `SaveChangesAsync`. Verificar la migración correspondiente.
- **D6 (LOW): `deletingFibraId` no se limpia en error → UUID crudo como ticker** [`PositionsTable.tsx`] — Si la posición desaparece durante el error path (refetch concurrente del portfolio), `positions.find(p => p.fibraId === deletingFibraId)?.ticker ?? deletingFibraId` muestra el UUID como ticker en el dialog. Edge case visual de baja probabilidad.

## Deferred from: code review of 4-5-3-pagina-lectora-interna-noticias — Pasada 1 (2026-05-21)

- **AC 4.5.3/3 — logo de FIBRA en `NoticiaPage`** [`newsApi.ts`, `NoticiaPage.tsx`] — `NewsArticleDto` no lleva asociación de FIBRA; el fallback de imagen en la página lectora solo puede llegar a imagen sectorial. Requiere extender el DTO con datos de FIBRA o un endpoint auxiliar. Alineado con decisión de AC2 en historia 4.5.1.
- **`GET /api/v1/news/{id}` retorna 404 sin `ProblemDetails`** [`NewsEndpoints.cs:39`] — Inconsistencia con convención de la API (otros endpoints retornan `ProblemDetails` en error). Impacto nulo en comportamiento funcional; normalizar en próxima iteración del módulo noticias.
- **`staleTime: 10 min` cachea el sentinel `null` (404)** [`NoticiaPage.tsx:17`] — Un artículo inexistente queda cacheado como "no encontrado" durante 10 minutos. Aceptable dado que los IDs son GUIDs y artículos no se crean retroactivamente en el mismo ID.

## Deferred from: code review of 4-5-1-scraping-imagen-ogimage-y-fallback-visual — Pasada 3 (2026-05-21)

- **DNS rebinding (TOCTOU) entre `IsAllowedHostAsync` y la HTTP request real** [`OgImageScraper.cs:15`] — La resolución DNS ocurre antes del request; con TTL corto un atacante puede devolver IP pública para el check e IP privada para la conexión. Fix requiere `HttpMessageHandler` custom con IP pinning. Limitación arquitectural aceptada.
- **URL de `og:image` extraída no validada contra SSRF allowlist** [`OgImageScraper.cs:35`] — Un publisher malicioso puede poner `og:image = http://169.254.169.254/...`; la URL se almacena y se sirve como `<img src>` al browser (no fetch server-side). Riesgo browser-side únicamente con stack actual.
- **HTTP 416 `Range Not Satisfiable` retorna `null` silencioso** [`OgImageScraper.cs:24`] — Páginas HTML pequeñas que retornan 416 hacen que el scraper abandone silenciosamente en lugar de reintentar sin el header Range. Edge case infrecuente.
- **IPv6 ULA `fc00::/7` no bloqueado en `IsAllowedIp`** [`OgImageScraper.cs:81`] — Unique Local Addresses son el equivalente IPv6 de RFC 1918; no bloqueados en la validación actual. Bajo riesgo con infraestructura actual; cubrir junto con la revisión global de SSRF si se amplía el scraping.

## Deferred from: code review of 4-5-1-scraping-imagen-ogimage-y-fallback-visual — Pasada 2 (2026-05-20)

- **AC2: color de identidad visual de FIBRA no implementado en fallback de imagen** [`news-image-fallback.ts`, `NoticiasSection.tsx`] — `getArticleImageUrl` usa `fibra?.logoUrl` pero no un `brandColor`. Si la FIBRA no tiene logo, cae a sector asset. Requiere nuevo campo `brandColor` en entidad `Fibra`, migración, seed y frontend. Diferido para historia futura del módulo noticias.
- **`AllowAutoRedirect=false` silencia og:image de fuentes con redirect HTTP→HTTPS** [`ApiServiceExtensions.cs`] — Trade-off de seguridad aceptado conscientemente; el fix SSRF previo eligió esta opción. Revisitar si miss rate en producción es alto.
- **`ResponseContentRead` puede buffear respuesta completa si servidor ignora `Range` header** [`OgImageScraper.cs`] — Intencional per Dev Notes; el timeout de 5s acota la exposición total. Evaluar si hay memory pressure en métricas de producción.
- **Dominios redirect de Google (`goo.gl`, `googleusercontent.com`) no cubiertos por filtro `news.google.com`** [`GoogleNewsRssClient.cs`] — Escenario especulativo; si Google emite GUIDs con short-links, el scraper intentaría extraer og:image del landing de Google en vez del artículo real. Revisar si aparece en logs como `imageUrl=null` con origen `goo.gl`.
- **Charset decoding ambiguo en respuestas Range sin header `Content-Type; charset=`** [`OgImageScraper.cs`] — ReadAsStringAsync cae a ISO-8859-1 per HTTP spec si no hay charset. En práctica las URLs de og:image son ASCII-safe; solo afectaría titles u otras partes del HTML, no al scraping.
- **`ExtractLink` retorna `string.Empty` como sentinela de fallo en vez de `null`** [`GoogleNewsRssClient.cs`] — Inconsistente con convención `string?`; el caller actual maneja `string.Empty` correctamente. Normalizar en refactor de GoogleNewsRssClient.
- **Sectores nuevos (Educativo, Autoalmacenaje, Hipotecario) sin asset en `SECTOR_IMAGES`** [`news-image-fallback.ts`] — Caen a `otro.jpg`; fuera del scope de AC3 (7 sectores definidos). Agregar assets sectoriales en historia futura si el miss rate visual es relevante.

## Deferred from: code review of 4-6-noticias-display-fixes-y-aimode-on (2026-05-21)

- **SSRF: destino del redirect no validado en OgImageScraper** [`ApiServiceExtensions.cs`] — `AllowAutoRedirect=true` permite que un redirect externo apunte a endpoints internos; `IsAllowedHostAsync` valida solo la URL de origen. Riesgo bajo por diseño (dev notes lo acepta explícitamente). Revisar junto con SSRF global si se amplía el scraping.
- **CancellationToken swallowed en bloque AI del pipeline** [`NewsPipelineJob.cs`] — Si el job Hangfire se cancela durante la llamada a Gemini, `OperationCanceledException` es capturada por el `catch(Exception)` interno y el artículo queda como `Partial` permanentemente (deduplicador lo excluye en la próxima corrida). Recuperable via regeneración manual con el endpoint de trigger.

## Deferred from: code review of 3-4-pipeline-historico-distribuciones — 2ª pasada (2026-05-21)

- **`WithHistoryStartDate(2020-01-01)` estático vs ventana dinámica `AddYears(-5)`** [`ApiServiceExtensions.cs`] — No rompe en 2026 (filtro client-side absorbe el desfase). Actualizar si el piso de 2020 queda dentro de la ventana de 5 años solicitada.
- **Conversión `ToDateTimeUtc()` → `DateOnly` puede desplazar un día** [`YahooFinanceClient.cs:GetDividendHistoryAsync`] — FIBRAs mexicanas no tienen dividendos cerca de medianoche UTC en práctica; revisar si aparecen desfases de fecha en logs de producción.
- **`YahooQuotesHistory` singleton no implementa `IDisposable`** [`ApiServiceExtensions.cs`] — Patrón pre-existente del `YahooQuotes` singleton; abordar junto con la limpieza de singletons HTTP en Épica 5.
- **Dos dividendos el mismo día para la misma FIBRA descarta el segundo** [`DistributionConfiguration.cs`] — Diseño intencional del índice único `(FibraId, PaymentDate)`; si Yahoo reporta ajustes intradiarios, el segundo queda silenciosamente descartado.

## Deferred from: code review of 3-5-daily-snapshot-historico-y-limpieza-price-snapshots (2026-05-21)

- **`SaveChangesAsync` por candle en backfill histórico** [`DailySnapshotHistoricalJob.cs`] — Decisión de diseño intencional siguiendo el patrón de `DistributionPipelineJob`. Para un job de backfill de única ejecución es aceptable; evaluar batching si el catálogo crece significativamente.
- **`RecurringJob.AddOrUpdate<DistributionPipelineJob>` fue omitido en story 3.4 y añadido aquí** [`Program.cs`] — Omisión pre-existente. El comportamiento final es correcto; sin acción requerida.
- **`DailySnapshotHistoricalJob` usa `DateTime.UtcNow` directo en lugar de `ITimeService`** [`DailySnapshotHistoricalJob.cs:19`] — El spec Dev Notes especifica explícitamente `DateTime.UtcNow.AddYears(-5)`. Violación menor de convención de tiempo para job de backfill único.
- **Endpoint manual `/daily-snapshot-historical/run` sin guard contra re-enqueueing** [`OpsMarketEndpoints.cs`] — Múltiples POSTs acumulan jobs en la cola Hangfire; `[DisableConcurrentExecution(0)]` los serializa. Para un job de backfill manual de uso infrecuente es aceptable; agregar check via `IMonitoringApi` en Epic 5 si el abuso operacional se detecta.

## Deferred from: code review of 3-4-pipeline-historico-distribuciones (2026-05-21)

- **`CancellationToken.None` en schedule de Hangfire** [`Program.cs`] — Patrón pre-existente idéntico en `MarketPipelineJob` y `NewsPipelineJob`. Sin cancelación graceful en shutdown del host. Abordar junto con los otros jobs en Épica 5.
- **`historyClient is null` devuelve `[]` silenciosamente** [`YahooFinanceClient.cs`] — Comportamiento intencional para test isolation, documentado en dev notes. Agregar `LogWarning` si se detecta null en producción (implicaría error en el DI).
- **Sin rate limiting entre llamadas a Yahoo Finance** [`DistributionPipelineJob.cs`] — Pre-existente en toda la integración YahooQuotesApi. Un bloqueo de IP es indistinguible de "sin dividendos". Mitigar en Épica 5 con métricas de pipeline y alertas por inserted=0.
- **`Take(60)` trunca "Ver historial completo" en teoría** [`MarketEndpoints.cs`] — Sin impacto real con catálogo actual (FIBRAs trimestrales: max ~20 registros / 5 años). Agregar paginación si se añaden FIBRAs de pago mensual.
- **`FakeDistYahooClient` con dos constructores de lógica dividida** [`DistributionPipelineJobTests.cs`] — No puede combinar múltiples entries + throwForTicker. Refactorizar si se añaden tests más complejos.
- **`YahooQuotesHistory.Inner` expone tipo interno** [`YahooQuotesHistory.cs`] — Diseño documentado en dev notes; wrapper creado para resolver colisión de DI, no para encapsulamiento. Evaluar encapsular en historia de refactor si el módulo crece.
- **Sin test para `CapturedAt`** [`DistributionPipelineJobTests.cs`] — Campo de auditoría sin cobertura. Agregar en próxima iteración del módulo market.
- **Null/empty `YahooTicker` sin guardia explícita** [`DistributionPipelineJob.cs`] — Pre-existente en todos los pipelines; catálogo controlado. Agregar guard defensivo si se abre la gestión del catálogo en Épica 5.
- **React row key `d.date` puede colisionar** [`DistribucionesSection.tsx`] — El UIX garantiza unicidad en DB; sin impacto real. Usar `${d.date}-${i}` si se añade soporte para múltiples distribuciones por fecha.

## Deferred from: code review of 4-5-4-limpieza-semantica-del-body-text-de-noticias (2026-05-22)

- **Regex `ArticleBlockRegex` no-greedy captura primer `</article>` anidado** [`ArticleContentScraper.cs`] — Artículos con elementos `<article>` anidados (WordPress related posts, microdata) capturan el bloque más pequeño; limitación del enfoque regex sin HTML parser.
- **`CountSentenceTerminators` cuenta puntos decimales** [`GeminiAiSummaryService.cs`] — `text.Count(c => c is '.' or '!')` cuenta decimales financieros (`94.5%`, `$0.42`, `NOI 1.2mmdp`) como terminadores de oración; puede aprobar resúmenes de 1-2 oraciones con muchos números.
- **Retry de Gemini sin delay entre intentos** [`GeminiAiSummaryService.cs`] — Las dos llamadas a `GenerateSummaryCoreAsync` son inmediatamente consecutivas; un 429 en el primer intento podría agravar el rate limiting.
- **`TryExtractByContentClassStart` trunca HTML a 40k chars** [`ArticleContentScraper.cs`] — Artículos muy largos (informes trimestrales) pueden ser cortados a mitad de un tag, produciendo un párrafo final con contenido de otro bloque.

## Deferred from: code review of 4-7-editor-manual-body-text-ops (2026-05-22)

- **Doble `FindAsync` en el flujo PUT body-text** [`AiModeEndpoints.cs` + `NewsRepository.cs`] — El endpoint llama `GetByIdAsync` para verificar 404, luego `UpdateBodyTextAsync` llama `FindAsync` de nuevo; EF Core devuelve la entidad trackeada sin ir a BD (no es doble roundtrip real), pero la redundancia puede confundir en mantenimiento futuro.
- **`GetPagedForOpsAsync` COUNT y SELECT sin snapshot transaccional** [`NewsRepository.cs`] — Artículos insertados entre ambas queries producen `total` inconsistente con `items`; menor impacto para UI de backoffice de bajo volumen.
- **Botón "Limpiar (null)" sin diálogo de confirmación** [`NewsBodyTextSection.tsx`] — Destructivo e irreversible desde la UI; un clic accidental elimina el body_text sin posibilidad de recuperación desde el panel Ops. Decisión de UX.
- **Sin límite de longitud en `UpdateBodyTextRequest.BodyText`** [`AiModeEndpoints.cs`] — Columna `nvarchar(max)` sin `[MaxLength]`; un payload grande sería persistido y enviado íntegro a Gemini en el siguiente trigger ai-summary.

## Deferred from: code review of 4-7-seleccion-modelo-ia-noticias (2026-05-22)

- **Whitelist de modelos duplicada en C#/TS/UI** [`AiModeEndpoints.cs` / `aiModeApi.ts` / `AiModeSection.tsx`] — Los valores `"gemini-2.5-flash"` / `"gemini-2.5-pro"` aparecen en ≥4 archivos sin fuente única de verdad. Un nuevo modelo requiere cambios coordinados en backend y frontend. Sin impacto funcional actual.
- **`UpdateConfigAsync(null, null, ...)` silently no-ops sin audit trail** [`AiModeRepository.cs`] — La firma permite ambos parámetros null; el método retorna sin `SaveChangesAsync`. No hay llamador actual con ambos null (el endpoint valida primero). Interfaz permisiva pero sin consecuencia práctica hoy.
- **Upsert-on-insert asume singleton cuando solo `newsModel` es proporcionado** [`AiModeRepository.cs:32-35`] — Si la fila id=1 no existe y se llama solo con `newsModel`, se inserta con `Mode = Off` como default, potencialmente inesperado. Inalcanzable con seed EF en producción.
- **Conflicto route handler LIFO en test E2E** [`news-epic4.spec.ts:~427`] — El test "Guardar con Flash" registra un segundo handler de Playwright sobre el mock base (LIFO); funcional hoy pero frágil ante reordenamiento de `beforeEach`.

## Deferred from: code review of 4-3-soporte-para-ai-mode-en-noticias-off-y-manual — Hardening Pass (2026-05-21)

- **Token de AdminOps en `sessionStorage` expuesto a XSS** [`src/Web/Ops/src/api/opsAuth.ts`] — Decisión arquitectural aceptada para SPA; el Ops SPA tiene surface de ataque reducida (acceso restringido a AdminOps). Revisar si se migra a HttpOnly cookie en Epic 5 junto con gestión de sesión.
- **`getStoredOpsAccessToken` lee de `localStorage` sin que `storeOpsAccessToken` escriba ahí** [`src/Web/Ops/src/api/opsAuth.ts`] — Compatibilidad intencional con versiones anteriores. Evaluar si se simplifica solo a sessionStorage cuando se certifique que no hay tokens legacy en producción.
- **Bootstrap `catch` reutiliza token previo como indicador de sesión válida cuando `refreshOpsSession` lanza excepción de red** [`src/Web/Ops/src/components/OpsLoginGate.tsx`] — El primer 401 del backend recupera el estado vía `OPS_AUTH_REQUIRED_EVENT`. Mejorar con timeout y fallback explícito a `anonymous` en Epic 5.
- **Multi-tab: `clearOpsAccessToken` en Tab A no propaga estado React en Tab B** [`src/Web/Ops/src/api/opsAuth.ts`] — Limitación conocida de auth por storage; tool de uso single-admin en MVP. Implementar `storage` event listener si el uso multi-tab se vuelve necesario.
- **Estado `'checking'` sin timeout en `refreshOpsSession`** [`src/Web/Ops/src/api/authApi.ts`] — Timeout del browser como fallback; aceptable para tool admin de bajo volumen. Agregar `AbortController` con timeout configurable en Epic 5 junto con infraestructura de resiliencia global.

- **[fix-amefibra-fibra-matching] FundamentalRecords huérfanos sin manifest link no corregidos por migración** — Si la migración FixFibraPlusPrologisMismatch no puede encontrar el registro vía last_processed_record_id (manifest.LastProcessedRecordId = null), el FundamentalRecord queda con FibraId de Prologis. Poco frecuente; se recupera en el siguiente run del pipeline con el algoritmo corregido.


## Deferred from: code review of 8-3-comparador-publico (2026-06-05)

- ~~**D1 — `fetchAllFibras` capped at 100**~~ ✅ **RESUELTO 2026-06-05** — Loop de paginación implementado en `fibrasApi.ts`; carga todas las páginas hasta que `items.length < pageSize`.
- ~~**D2 — `/comparar` ausente del listado SSR prerender**~~ ✅ **RESUELTO 2026-06-05** — Ruta agregada en `routesToRender` de `prerender.mjs`; meta tags disponibles para crawlers.
- ~~**D3 — Botón "Reintentar" siempre visible en el selector**~~ ✅ **RESUELTO 2026-06-05** — Botón condicional a `hasComparisonError`; solo se muestra cuando hay error activo.
- ~~**D4 — Browse-on-focus suprimido**~~ ✅ **RESUELTO 2026-06-05** — Eliminada la condición `search.trim().length > 0` de `showSuggestions`; el pool inicial de 8 FIBRAs se renderiza al hacer foco.
- **D5 — `cutoff` usa `DateTime.UtcNow` fresco** [`src/Server/Api/Endpoints/Public/CompareEndpoints.cs:99`] — `utcNow` ya está capturado en línea 71; la segunda llamada puede diferir milisegundos a medianoche UTC. Inmaterial en práctica.

## Deferred from: code review of infra/db-migrate-to-sqlserver (2026-06-08)

- **D1 (MEDIUM): Credencial PostgreSQL hardcodeada en script de migración** [`scripts/migrate-data/Program.cs:6`] — `Password=devpassword` en texto plano en source control. Pre-existente desde el script original. Mover a variable de entorno o archivo `.env` ignorado por git si el entorno de origen cambia a credenciales sensibles.
- **D2 (MEDIUM): SQL injection en queries de metadata del script** [`scripts/migrate-data/Program.cs`, líneas ~63, ~78] — `schema` y `table` se interpolan directamente en INFORMATION_SCHEMA queries sin parametrizar. Valores provienen de array hardcodeado en el código (no de input externo), por lo que el riesgo es teórico en el estado actual. Parametrizar si el script evoluciona para aceptar argumentos externos.
- **D3 (LOW): `DesignTimeDbContextFactory` apunta a servidor específico LAPBADIS** [`src/Server/Infrastructure/DesignTimeDbContextFactory.cs:12`] — Bloquea `dotnet ef migrations add` en cualquier otra máquina. Leer de `appsettings.Development.json` o variable de entorno `EF_CONNECTION_STRING` en próxima iteración de onboarding del equipo.
- **D4 (LOW): Hangfire `InvisibilityTimeout` implícito de 30 min vs. 5 min previos de PostgreSQL** [`src/Server/Api/CompositionRoot/ApiServiceExtensions.cs`] — El campo `InvisibilityTimeout` fue removido por obsoleto en `Hangfire.SqlServer 1.8.23`. El valor por defecto subió de ~5 min a 30 min. Sin impacto funcional para los jobs actuales (todos terminan en <2 min), pero documentado por si se agregan jobs de larga duración.
- **D5 (LOW): Script de migración de datos carga tablas completas en DataTable** [`scripts/migrate-data/Program.cs`] — Para volúmenes grandes (>100k filas en `market.PriceSnapshot`, `fundamentals.FundamentalRecord`) puede presionar memoria. Optimizar pasando `NpgsqlDataReader` directamente a `SqlBulkCopy.WriteToServerAsync(IDataReader)` si la migración se vuelve lenta en producción.

## Deferred from: code review of 9-3-mejoras-portafolio (2026-06-09)

- **D1** — Serie de performance no filtra por fecha de adquisición de posición (posiciones contribuyen a todo el historial histórico) — MVP simplification documentada en Dev Notes [PortfolioEndpoints.cs]
- **D2** — N+1 queries en `BuildPerformanceSeriesAsync`: 1 query por posición — escala mal en portafolios grandes [PortfolioEndpoints.cs]
- **D3** — `BuildNormalizedPoints` retorna `[]` cuando el primer valor del período es 0 sin mensaje al usuario [PortfolioEndpoints.cs]
- **D4** — `detectCadence` clasifica por conteo de distribuciones en el año, no por intervalo real entre pagos — puede proyectar fechas incorrectas [portfolio-calendar.ts]
- **D5** — `annualDistCutoff` usa `DateTime.UtcNow` — inconsistencia si `PaymentDate` se almacena en hora México [PortfolioEndpoints.cs]
- **D6** — `calcNewAvgCost` usa floating-point JS — rounding errors en display; `formatMoney` mitiga el impacto visible [simulador-logic.ts]
- **D7** — AC-9 especificaba usar `Tabs` de shadcn/ui; se implementaron custom `<button>` sin ARIA tablist/tabpanel [PortafolioPage.tsx]
- **D8** — `enabledColumns` vacío → `['yoc']`: puede sobreescribir configuración explícita de usuario sin columnas opcionales [PortafolioPage.tsx]
- **D9** — `addSeries` en PerformanceChart sin guard contra `undefined` en el parámetro `series` [PerformanceChart.tsx]
- **D10** — `PortafolioCalendario` recalcula `projectNextPayments` sin `useMemo`, pasa `new Date()` en cada render [PortafolioCalendario.tsx]
- **D11** — Logo img: flash por intento de red en cada mount; `failedLogos` no persiste en sessionStorage [PositionsTable.tsx]
- **D12** — `/performance` acepta cualquier string como `range`; valor inválido silenciosamente devuelve 30 días [PortfolioEndpoints.cs]
- **D13** — Fallback frontend `calcYieldPortafolio` puede divergir de las condiciones del backend cuando `kpis.yieldPortafolio` es null [KpiCards.tsx]

## Deferred from: code review of 9-8-fundamentales-ejecutar-por-fibra (2026-06-09)

- **D6 (LOW): `CronExpression = "0 0 * * *"` en `FundamentalsPipelineSchedule` es dead code** [`src/Server/Infrastructure/Jobs/Fundamentals/FundamentalsPipelineSchedule.cs:10`] — La constante ya no se referencia en ningún sitio tras migrar a `GetCronExpression(cadenceMinutes)`. Eliminar cuando sea conveniente.
- **D7 (LOW): Sin `onError` en `runMutation` de `FundamentalsHistory`** [`src/Web/Ops/src/modules/fundamentals/FundamentalsHistory.tsx:37`] — Si el endpoint devuelve 404 (ticker inactivo) el error cae silenciosamente; el botón vuelve a "Ejecutar ahora" sin feedback visible al operador. Añadir toast/mensaje de error en próxima historia del módulo.
