# Deferred Work

Items diferidos durante code reviews. Cada sección tiene la historia origen y la fecha.

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

## Deferred from: code review of 2-5-home-topmovers-tabla-y-ganadores-perdedores (2026-05-19)

- **`dailyChangePct = 0` excluido silenciosamente de GainersLosers** [`movers-logic.ts:39,44`] — El filtro `> 0` / `< 0` excluye valores exactamente cero sin indicación al usuario. Comportamiento no especificado en los AC; abordar si el negocio lo requiere.
- **Doble llamada a `numOf` en comparador de `getTopMovers`** [`movers-logic.ts:24-26`] — Micro-optimización: `numOf` se llama dos veces por elemento por comparación. Refactorizar a variable local si el corpus de snapshots crece.
- **`formatVolume`: rango [999_500, 1_000_000) muestra "1000K"** [`movers-logic.ts:15`] — Edge case de formateo: `(999_500 / 1_000).toFixed(0) = "1000"` → "1000K". Sin impacto con volúmenes actuales de FIBRAs.
- **`TopMovers` sin empty state cuando `snapshots = []`** [`TopMovers.tsx`] — Si la API devuelve array vacío y no hay error, el componente renderiza un contenedor vacío sin mensaje. Inconsistente con `GainersLosers` que sí tiene empty state.
