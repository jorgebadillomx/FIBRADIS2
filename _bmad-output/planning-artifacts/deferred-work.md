# Deferred Work

## Deferred from: code review de 6-3-filas-expandibles-con-detalle-de-posicion-y-badge-de-senal-nav (2026-06-03)

- D1: `GetUserId` (PortfolioEndpoints.cs:207) lanza 500 en vez de 401 si el claim NameIdentifier está ausente/malformado — pre-existente de 6-1/6-2.
- D2: `Take(4)` en memoria en el endpoint tras `GetDistributionsByFibrasAsync` — trae todo el año y filtra en app; aceptable para portafolios pequeños de FIBRAs.
- D3: `totalCols = 11` magic number en PositionsTable.tsx:128 — frágil si se añaden/quitan columnas fijas.
- D4: `declaredYield × 4` asume cadencia trimestral para todas las FIBRAs — incorrecto para FIBRAs con distribución mensual. Requiere campo `distributionFrequency` en el DTO.
- D5: `getDistributionPeriodLabel` etiqueta con el período del pago en vez del período económico al que corresponde. Requiere heurística de desfase por cadencia o config por FIBRA.
- D6: `GetLatestSnapshotPerFibraAsync` carga todos los snapshots sin filtrar por `fibraIds` — filtra en memoria. Pre-existente de 6-1/6-2.
- D7: `NormalizeColumns` whitelist no incluye `week52Low`, `week52Avg`, `volume` a pesar de que están en el DTO. Pre-existente de 6-1/6-2.
- D8: `PortfolioDistributionDto.PaymentDate` como `string` fuerza `yyyy-MM-dd` sin localización en UI. Decisión de diseño de 6-1/6-2.

## Deferred from: code review de 6-2-kpis-del-portafolio-y-tabla-con-multi-sort-y-columnas-configurables (2026-06-03)

- D1: `PlusvaliaTotal_Mxn`/`PlusvaliaTotal_Pct` son null cuando IsPartial=true — Decisión consciente: el valor parcial vs inversión total sería engañoso. El badge `(parcial)` aparece junto a `—`. Reconsiderar si en el futuro se quiere mostrar la ganancia parcial explícita.
- D2: `enabledColumns` sincronizado vía `useEffect` en `PortafolioPage.tsx:56-58` — Flash de columnas vacías al cargar y race condition si la query refetchea mientras el PUT de ColumnPicker está en vuelo. Considerar derivar directamente de `columnConfigQuery.data?.columns ?? []`.
- D3: `getStoredAccessToken()` intenta 3 keys hardcodeadas en `PortafolioPage.tsx:16-28` — Verificar contra el módulo auth real y usar la key exacta.
- D4: Error del PUT de ColumnPicker no visible si el Popover está cerrado (`ColumnPicker.tsx:58-61`) — UX edge case; considerar toast o banner fuera del Popover.
- D5: Drop zone sin accesibilidad de teclado (`UploadZone.tsx:102-127`) — El `div` exterior no tiene `tabIndex` ni `onKeyDown`. El `<input>` interior sí es accesible. Deuda WCAG 2.1 AA.

## Deferred from: code review de 5-12-reports-url-y-discovery-oficial (2026-06-03)

- `AmefibraDiscoverySource._cachedListings`: el caché funciona porque `sources.ToList()` materializa la lista una vez en `ExecuteAsync`, pero es frágil. Si el patrón de resolución cambia, dejar de funcionar sin aviso. Considerar registrar como `Scoped` en lugar de `Transient`.
- `OfficialSiteDiscoverySource.ResolveUrl`: la tercera rama (`href.StartsWith('/') && Uri.TryCreate(baseUrl, ...)`) es código muerto (inalcanzable si `pageUrl` es válida). Limpiar en siguiente toque al archivo.
- `BmvDiscoverySource`: concatenación `BmvBase + href` asume que todos los hrefs relativos del BMV empiezan con `/`. Si BMV usara un href relativo sin `/`, produciría URL malformada. Bajo riesgo con el patrón actual del BMV.
- `FundamentalsAutomationService.TryLogPipelineErrorAsync`: pierde `SourceTitle` del candidato en el contexto JSON del error log. Afecta trazabilidad cuando falla un PDF específico.
- `FundamentalsAutomationService`: 2 queries BD por candidato elegible para calcular `isPossibleUpdate` (sin caché local por fibra+período). Puede generar N queries redundantes si una fibra tiene muchos candidatos del mismo período.
- `OfficialSitePeriodParser.CombinedPeriodRegex`: sin anclas de límite de palabra; puede hacer match dentro de tokens más largos en filenames no estándar. Bajo riesgo con los patrones reales de las fuentes actuales.

## Deferred from: code review de 6-1-carga-y-validacion-del-portafolio (2026-06-03)

- D1 — `PortafolioPage.tsx`: `PositionsTable` nunca se popula porque `POST /upload` retorna solo `positionCount`, no las posiciones. Componente dead code en esta historia. Resolución pendiente en 6.2 cuando se implemente `GET /portfolio`.
- D2 — `PortfolioEndpoints.cs`: sin límite de tamaño de archivo en `POST /upload`. Archivos grandes se leen completamente en memoria. Especificar límite al diseñar 6.2 o 6.4.
- D3 — `PortafolioPage.tsx`: sin auth guard en `/portafolio`. Usuario no autenticado queda en estado de error silencioso. Story notes aplazan a 6.2 con redirect a `/login`.
- D4 — `PortfolioRepository.cs`: `GetByUserIdAsync` ordena por `FibraId` (Guid arbitrario, sin significado semántico). Cambiar a `OrderBy(p => p.UploadedAt)` o join para ordenar por ticker en 6.2.
