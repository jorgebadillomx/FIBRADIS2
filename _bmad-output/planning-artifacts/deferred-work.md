# Deferred Work

## Deferred from: code review de 9-1-perfil-usuario-main (2026-06-06)

- D1: `UpdateApodoAsync` acepta `apodo = ""` (empty string) como valor válido — la validación solo se ejecuta cuando `apodo is not null`; una llamada directa a la API con `{ "apodo": "" }` almacena `""` en BD en lugar de `null`, inconsistente con el contrato implícito. Fix: `user.Apodo = string.IsNullOrEmpty(apodo) ? null : apodo` (`UserService.cs:115`).
- D2: `GetActor` helper duplicado verbatim en 6+ clases estáticas de Ops (`AiModeEndpoints`, `AiProviderEndpoints`, `OpsFundamentalsEndpoints`, `OpsMarketEndpoints`, `OpsConfigEndpoints`, `OpsAiPromptEndpoints`, `OpsCatalogEndpoints`) — riesgo de divergencia si se corrige un bug en una copia pero no en las demás. Extraer a método de extensión `HttpContext.GetDecryptedActorEmail(IEmailEncryptor)` o clase `OpsActorHelper`.
- D3: `EmailEncryptor.Decrypt` atrapa todas las excepciones sin loguear advertencia — si hay rotación de clave AES, el actor en el audit log queda como blob Base64 cifrado sin aviso observable en logs. Añadir `logger.LogWarning(ex, "Decrypt failed, storing raw value")` antes del return del fallback.
- D4: `ChangeOwnPasswordAsync` acepta la misma contraseña sin rechazarla — no es requisito de AC4 pero puede sorprender a los usuarios. Añadir `if (BCrypt.Verify(newPassword, user.PasswordHash)) throw new InvalidUserDataException(...)` antes de hashear.
- D5: Caracteres Unicode bidi/format no bloqueados en apodo (U+202E RTL override, U+200B zero-width space, etc.) — `char.IsControl` solo cubre categorías C0/C1 (código < 32). Añadir rechazo de categorías `UnicodeCategory.Format` y `UnicodeCategory.Surrogate` en `UpdateApodoAsync`.
- D6: `fetchProfile` que retorna 401 muestra mensaje genérico "Error al cargar el perfil." en lugar de redirigir a login — inconsistente con el comportamiento de otras queries autenticadas que disparan `MAIN_AUTH_REQUIRED_EVENT`. Inspeccionar `error.status` en `authApi.ts` y llamar `notifyMainAuthRequired()` si es 401.
- D7: Header actualiza apodo vía re-fetch en background (no optimista) — tras PATCH exitoso, `queryClient.invalidateQueries` dispara un re-fetch; hay un lag visual breve donde el header sigue mostrando el apodo anterior. Considerar `queryClient.setQueryData(PROFILE_QUERY_KEY, (old) => ({ ...old, apodo: newApodo }))` para actualización inmediata.

## Backlog: Historia 6-9 — Términos y condiciones, footer y editor de textos del sitio (2026-06-04)

Deferred desde el scope de 6-7 (split multi-goal). Implementar después de 6-8.

- **OperationalConfig**: agregar `TermsEnabled (bool)` y `TermsText (string?)` con migración; exponer en el endpoint de config existente.
- **User**: agregar `HasAcceptedTerms (bool, default false)` y `TermsAcceptedAt (DateTime?)` con migración.
- **Endpoint**: `POST /api/v1/account/accept-terms` (requiere auth User) — marca `HasAcceptedTerms = true`, guarda timestamp.
- **Main — Modal bloqueante**: al hacer login, si `TermsEnabled && !user.HasAcceptedTerms` mostrar modal con texto T&C que bloquea toda la UI hasta que el usuario acepte (botón "Acepto"). No puede cerrarse sin aceptar. La respuesta del login debe incluir `hasAcceptedTerms`.
- **Main — Footer**: componente fijo al pie que no rompe el diseño del sitio; contiene "Contacto" (mailto o URL configurable desde config) y "Términos y condiciones" (abre modal o enlaza a la página de T&C).
- **Ops — Sección "Contenido del sitio"**: editor para `TermsEnabled` (toggle), `TermsText` (textarea rico o markdown), `ContactEmail`. Guardar vía PATCH al endpoint de config.
- **T&C pre-poblado** al hacer seed inicial (o via migration data): texto en español que incluya: (1) la información del sitio es únicamente orientativa y no constituye asesoría de inversión; (2) FIBRADIS no se responsabiliza de pérdidas derivadas de decisiones de inversión tomadas con base en los datos del sitio; (3) los datos personales del usuario no son vendidos ni cedidos a terceros; (4) la información personal se almacena cifrada; (5) el usuario puede solicitar la eliminación de su cuenta contactando al administrador.

## Backlog: Historia 6-8 — Perfil del usuario en Main (apodo y cambio de contraseña propia) (2026-06-04)

Deferred desde el scope de 6-7 (split multi-goal). Implementar antes de 6-9.

- **User entity**: agregar `Apodo (string?)` nullable con migración.
- **JWT claims**: incluir `apodo` en el access token (o devolver en endpoint de perfil).
- **Endpoints account** (requieren auth User — no AdminOps):
  - `GET /api/v1/account/me` → devuelve email descifrado, role, apodo.
  - `PATCH /api/v1/account/me` `{apodo}` → actualiza apodo; validar max 50 chars.
  - `PATCH /api/v1/account/password` `{currentPassword, newPassword}` → valida contraseña actual antes de cambiar; aplica criterios de contraseña fuerte.
- **Main — Sección de perfil**: accesible desde el menú del usuario (ej. dropdown o página `/perfil`); muestra email (no editable), apodo (editable inline), botón "Cambiar contraseña" con diálogo.
- **Validación**: contraseña fuerte igual que en 6-7; apodo max 50 chars, sin caracteres de control.

## Deferred from: code review de 6-6-importacion-portafolio-gbm (2026-06-04)

- D1: `snapshotQuery` sin manejo de estado error en `PortafolioPage.tsx:53` — si `/portfolio/snapshot` devuelve 500, el banner de respaldo no aparece silenciosamente aunque exista snapshot. `portfolioQuery` sí tiene error UI.
- D2: `RestoreSnapshotAsync` carga snapshot fuera de la transacción `PortfolioRepository.cs:56` — mismo TOCTOU que P1; requiere refactorizar a variable local interna al lambda para corregirlo limpiamente.
- D3: Doble fetch de posiciones en modo merge — endpoint llama `GetByUserIdAsync` para chequeo de duplicados y `MergePositionsAsync` lo llama de nuevo internamente; ventana de race condition estrecha.
- D4: `handleConfirmDuplicate` en `UploadZone.tsx:98` no captura `selectedFile` al momento de detección — edge case teórico donde el archivo podría limpiarse antes de confirmar.
- D5: Banner post-archivado reutiliza el de AC-10 — AC-8 especifica texto "Portafolio archivado el [fecha]" pero la UI muestra "Tienes un respaldo del..." sin distinción visual.
- D6: `currentPositionCount={0}` hardcodeado en `PortafolioPage.tsx:211` — debería ser `positions.length` para consistencia; cosmético.

## Deferred from: code review de 6-5-autenticacion-y-menus-privados (2026-06-03)

- D1: Bootstrap de AuthContext marca status='authenticated' con token stale de sessionStorage cuando refreshMainSession() lanza error de red — patrón consistente con OpsLoginGate, produce flash de UI autenticada seguido de logout por 401 en primera query. (`AuthContext.tsx`)
- D2: Doble cleanup en logout — clearMainAccessToken/queryClient.clear/setStatus se ejecutan dos veces: una directa en logout() y otra en el handler del evento MAIN_AUTH_REQUIRED_EVENT disparado por logoutMain(). Idempotente, no rompe. (`AuthContext.tsx` + `authApi.ts`)
- D3: Bloque de seed en Program.cs sin guard de migración — db.Users.Any() lanza si la migración no se ha aplicado, crash en dev recién clonado. Scope: dev-only. (`Program.cs`)
- D4: GetAllUsersAsync materializa toda la tabla Users sin paginación. Consistente con patrón de otros endpoints Ops (catalogos, etc.). (`UserService.cs`)
- D5: Access token almacenado en sessionStorage es accesible por JS en caso de XSS — patrón idéntico al Ops SPA, deuda cross-cutting. (`mainAuth.ts`)

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
