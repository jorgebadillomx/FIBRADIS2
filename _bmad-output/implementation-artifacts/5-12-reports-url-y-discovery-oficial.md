# Historia 5.12: Cobertura completa de ReportsUrl y discovery nativo para fibras fuera de Amefibra

Status: review

## Story

Como AdminOps,
quiero que todas las FIBRAs activas del catálogo tengan una URL de reportes correcta en BD y que las 8 FIBRAs ausentes de Amefibra descubran sus PDFs trimestrales desde sus fuentes oficiales,
para que el pipeline de KPIs tenga cobertura completa del universo activo y no quede ninguna fibra sin ingesta automática.

## Acceptance Criteria

### AC1 — Todas las FIBRAs activas tienen ReportsUrl válida en BD

**Dado que** existen 20 FIBRAs en el catálogo (19 activas, TERRA13 inactiva),
**Cuando** se aplica la migración EF,
**Entonces** ninguna FIBRA activa tiene `ReportsUrl = null`,
y cada URL apunta a la página donde la FIBRA publica sus reportes trimestrales.

URLs verificadas el 2026-06-02 (todas retornan HTTP 200 con contenido de reportes):

| Ticker | ReportsUrl | Estado en BD |
|--------|-----------|--------------|
| FUNO11 | `https://funo.mx/inversionistas/suplementos-informativos` | agregar |
| DANHOS13 | `https://fibradanhos.com.mx/reportes-trimestrales.html` | agregar |
| TERRA13 | — | **Inactive — omitir** |
| FIBRAMQ12 | `https://www.fibramacquarie.com/es/inversionistas.html` | agregar |
| FMTY14 | `https://www.fibramty.com/en/inversionistas` | agregar |
| FINN13 | `https://fibrainn.mx/inversionistas/resultados-trimestrales` | agregar |
| FIHO12 | `https://www.bmv.com.mx/es/emisoras/informacionfinanciera/FIHO-30057-CGEN_CAPIT` | agregar |
| VESTA15 | `https://vesta.com.mx/informacion-financiera/asg` | agregar |
| HCITY17 | `https://www.bmv.com.mx/es/emisoras/informacionfinanciera/HCITY-31249-CGEN_CAPIT` | agregar |
| FIBRAUP18 | `https://fibra-upsite.com/inversionistas/razones` | agregar |
| SOMA21 | `https://fibrasoma.group/investors/quarterly-reports-2/` | agregar |
| EDUCA18 | `https://www.fibraeduca.com/reportes-financieros` | ya existe ✓ |
| FIBRAPL14 | `https://www.fibraprologis.com/en-US/investors/financial-results` | ya existe ✓ |
| FNOVA17 | `https://www.fibra-nova.com/inversionistas/reportes-trimestrales` | ya existe ✓ |
| FPLUS16 | `https://www.fibraplus.mx/es/financiera/trimestrales` | ya existe ✓ |
| FSHOP13 | `https://fibrashop.mx/informes-financieros/` | ya existe ✓ |
| NEXT25 | `https://fibranext.mx/investors` | ya existe ✓ |
| STORAGE18 | `https://fibrastorage.com/repositorio-informacion-financiera/` | ya existe ✓ |
| FHIPO14 | `https://fhipo.com/es/reportes-trimestrales/` | ya existe ✓ |
| FCFE18 | `https://cfecapital.com.mx/informacion-financiera` | **actualizar** (era igual a InvestorUrl) |

**Y** el cambio se implementa con migración EF Core nueva.

### AC2 — Interfaz genérica `IFundamentalsDiscoverySource` introducida

**Dado que** el pipeline actual solo conoce `IAmefibraDiscoveryClient`,
**Cuando** se refactoriza `FundamentalsAutomationService`,
**Entonces**:
- Existe `IFundamentalsDiscoverySource` en `Application/Fundamentals/` con contrato unificado
- `FundamentalsAutomationService` acepta `IEnumerable<IFundamentalsDiscoverySource>`
- `AmefibraDiscoverySource` adapta `IAmefibraDiscoveryClient` a la nueva interfaz
- El pipeline existente (Amefibra) sigue funcionando sin cambios de comportamiento

### AC3 — Discovery nativo para las 8 FIBRAs fuera de Amefibra

**Dado que** las siguientes FIBRAs no publican en Amefibra:
FIBRAMQ12, FIHO12, VESTA15, HCITY17, NEXT25, SOMA21, FHIPO14, FCFE18,

**Cuando** el pipeline ejecuta discovery,
**Entonces** cada una tiene un discovery client funcional que extrae candidatos de su fuente oficial y los pasa al mismo flujo `IngestAsync → markdown → KPI → FundamentalRecord`.

Cada PDF nuevo:
- Crea `FundamentalSourceManifest` con `SourceName` del cliente correspondiente
- Crea `FundamentalRecord` con `ImportedBy = "system:{source}"` y `ProcessingMode = "api"`
- La deduplicación por `fibra + period` funciona igual que con Amefibra

### AC4 — Los registros existentes de Amefibra no se rompen

**Cuando** se ejecuta el pipeline después del refactor,
**Entonces** los registros previos con `ImportedBy = "system:amefibra"` son visibles en Ops y en Main sin cambios, y no hay duplicación de período para FIBRAs que ya tenían registros.

## Tasks / Subtasks

### T1 — Actualizar ReportsUrl en CatalogSeed.cs + migración (AC1)

- [x] T1.1 — Actualizar `src/Server/Infrastructure/Persistence/Seed/CatalogSeed.cs`:
  - 10 fibras agregan ReportsUrl (ver tabla AC1)
  - FCFE18 actualiza de `https://cfecapital.com.mx/inversionistas` → `https://cfecapital.com.mx/informacion-financiera`
  - TERRA13 permanece sin cambios (Inactive)
- [x] T1.2 — Generar migración:
  ```bash
  dotnet ef migrations add UpdateFibraReportsUrls --project src/Server/Infrastructure --startup-project src/Server/Api
  ```
  Si hay DLLs bloqueados, agregar `--configuration Release` (ver convenciones).
- [x] T1.3 — Verificar que la migración genera UPDATE (no INSERT) al aplicarse sobre registros seed existentes

### T2 — Introducir `IFundamentalsDiscoverySource` y refactorizar el servicio (AC2)

- [x] T2.1 — Crear en `src/Server/Application/Fundamentals/`:

  ```csharp
  // IFundamentalsDiscoverySource.cs
  public interface IFundamentalsDiscoverySource
  {
      string SourceName { get; }
      IReadOnlyList<string> SupportedTickers { get; }
      Task<IReadOnlyList<FundamentalsDiscoveryCandidate>> DiscoverCandidatesAsync(
          Fibra fibra, CancellationToken ct);
  }

  // FundamentalsDiscoveryCandidate.cs
  public sealed record FundamentalsDiscoveryCandidate(
      string SourceName,
      string SourceTitle,
      string PackageUrl,       // clave de deduplicación única
      string? DownloadUrl,     // URL directa al PDF
      string? Period,          // "Q1-2024" normalizado
      string ReportType,       // "quarterly" | "annual" | "pending-classification"
      DateTimeOffset? PublishedAt);
  ```

- [x] T2.2 — Crear `src/Server/Infrastructure/Integrations/PdfDiscovery/AmefibraDiscoverySource.cs`:
  - Implementa `IFundamentalsDiscoverySource`
  - `SourceName = "AMEFIBRA"`
  - `SupportedTickers`: todas las FIBRAs activas (Amefibra devuelve por título, no por fibra)
  - Internamente cachea los listings de `IAmefibraDiscoveryClient` **una sola vez por corrida** (variable privada `List<AmefibraListingItem>? _cachedListings`) y filtra por fibra en memoria para evitar scraping repetido
  - Para fibras no soportadas por una fuente, `DiscoverCandidatesAsync` retorna `[]` sin error

- [x] T2.3 — Refactorizar `FundamentalsAutomationService`:
  - Cambiar ctor: `IEnumerable<IFundamentalsDiscoverySource> discoverySources` en vez de `IAmefibraDiscoveryClient`
  - Por cada fibra activa, iterar todas las fuentes registradas que la soportan
  - La lógica de `IngestAsync` **no cambia** — solo cambia el origen de los candidatos
  - `FundamentalsAutomationRunResult` puede agregar contadores por fuente si se necesita observabilidad

- [x] T2.4 — Actualizar DI en `ApiServiceExtensions.cs`:
  - Registrar cada `IFundamentalsDiscoverySource` como `AddSingleton<IFundamentalsDiscoverySource, XxxSource>()`
  - Verificar que `IEnumerable<IFundamentalsDiscoverySource>` se resuelve correctamente

### T3 — Discovery para FIBRAMQ12, VESTA15, FHIPO14, FCFE18 — HTML estático (AC3)

Estas 4 fibras tienen links de PDFs en HTML estático. Un único `OfficialSiteDiscoverySource` con configuración por fibra (patrón de `SiteExtractionCatalog` de historia 4-8).

- [x] T3.1 — Crear `src/Server/Infrastructure/Integrations/PdfDiscovery/OfficialSiteDiscoverySource.cs`:
  - Acepta un catálogo `IReadOnlyDictionary<string, OfficialSiteConfig>` (ticker → config)
  - `OfficialSiteConfig`: `{ DiscoveryUrl, PdfLinkSelector, BaseUrl }`
  - Usa AngleSharp para extraer `<a href="*.pdf">` con el selector configurado
  - Usa `OfficialSitePeriodParser` para inferir período desde nombre de archivo o texto del link
  - `SourceName = "official:{ticker}"`, `ImportedBy = "system:official-{ticker}"`

- [x] T3.2 — Configuración por fibra (en el catálogo interno del cliente):

  | Ticker | DiscoveryUrl | PdfLinkSelector | Notas |
  |--------|-------------|-----------------|-------|
  | FIBRAMQ12 | `https://www.fibramacquarie.com/es/inversionistas.html` | `a[href*=".pdf"]` | Solo español: filtrar `-spa.pdf` o `-earnings-release-spa` |
  | VESTA15 | `https://vesta.com.mx/informacion-financiera/asg` | `a[href*="/storage/app/uploads/"][href$=".pdf"]` | |
  | FHIPO14 | `https://fhipo.com/es/reportes-trimestrales/` | `a[href*="wp-content/uploads/"][href$=".pdf"]` | WordPress |
  | FCFE18 | `https://cfecapital.com.mx/informacion-financiera` | `a[href*="wp-content/uploads/"][href$=".pdf"]` | WordPress |

- [x] T3.3 — Crear `OfficialSitePeriodParser` en `Application/Fundamentals/`:
  - Normaliza el nombre de archivo a período `Q1-2024`
  - Patrones: `1Q26`, `1T26`, `2T25`, `Q1-2024`, `1er-trimestre-2024`, año en path `2025/02/`
  - Si no puede inferir → `ReportType = "pending-classification"`, `Period = null`
  - Reutilizar lógica existente de `AmefibraTitleParser` donde aplique

- [x] T3.4 — Registrar en DI

### T4 — Discovery para NEXT25 y FUNO11 — HTML dinámico con links predecibles (AC3 parcial)

NEXT25 y FUNO11 usan el mismo patrón de URL: `/site_media/uploads/documentos/documento-{code}-{timestamp}.pdf`. Los links están en el HTML estático de la página (no JS), por lo que el mismo `OfficialSiteDiscoverySource` aplica.

- [x] T4.1 — Agregar NEXT25 y FUNO11 al catálogo de `OfficialSiteDiscoverySource`:

  | Ticker | DiscoveryUrl | PdfLinkSelector |
  |--------|-------------|-----------------|
  | NEXT25 | `https://fibranext.mx/investors` | `a[href*="site_media/uploads/documentos/"][href$=".pdf"]` |
  | FUNO11 | `https://funo.mx/inversionistas/suplementos-informativos` | `a[href*="site_media/uploads/documentos/"][href$=".pdf"]` |

  Nota: FUNO11 está en Amefibra; al tener discovery propio también, la deduplicación por `fibra + period + packageUrl` evitará reprocesar lo que Amefibra ya ingresó.

### T5 — Discovery para SOMA21 — REST API JSON (AC3)

- [x] T5.1 — Crear `src/Server/Infrastructure/Integrations/PdfDiscovery/SomaDiscoverySource.cs`:
  - `SupportedTickers = ["SOMA21"]`
  - `SourceName = "official:SOMA21"`
  - Llama `GET https://fibrasoma.group/wp-json/soma/documents`
  - Deserializa la respuesta JSON: cada item tiene título, fecha y URL directa del PDF
  - Filtra solo `reportType == "quarterly"` (excluir anuales, sostenibilidad, factsheets)
  - Infiere período desde el nombre del PDF o el campo fecha del JSON
  - `PackageUrl = DownloadUrl = {url_del_pdf}` (no hay página intermedia)
  - Ejemplo de URL confirmada: `https://fibrasoma.group/wp-content/uploads/2026/05/Fibra-SOMA-1Q26-Quarterly-Report-VF.pdf`

- [x] T5.2 — Registrar en DI

### T6 — Discovery para FIHO12 y HCITY17 — BMV scraping (AC3)

Ambas fibras no tienen página propia con PDFs directos. La BMV es su fuente regulatoria oficial.

- [x] T6.1 — Crear `src/Server/Infrastructure/Integrations/PdfDiscovery/BmvDiscoverySource.cs`:
  - `SupportedTickers = ["FIHO12", "HCITY17"]`
  - `SourceName = "bmv:{ticker}"`
  - Por fibra, calcula su URL de BMV desde `fibra.ReportsUrl` (ya en BD)
  - Scrapea la página BMV con AngleSharp: extrae links con patrón `/docs-pub/indrpfn/indrpfn_{id}_{year}-{quarter}_1.pdf`
  - Base URL de BMV: `https://www.bmv.com.mx`
  - Infiere período desde el patrón `{year}-{quarter}` en el nombre del archivo (ej: `2026-01` = Q1 2026, `2025-04` = Q4 2025)
  - Los IDs de documento BMV no son predecibles — se extraen scrapeando la página (no se construyen)

- [x] T6.2 — Conversión de formato período BMV:
  - `2026-01` → `Q1-2026`
  - `2025-04` → `Q4-2025`
  - `2025-03` → `Q3-2025`
  - `2025-02` → `Q2-2025`

- [x] T6.3 — Registrar en DI

### T7 — Tests (AC2–AC4)

- [x] T7.1 — Unit tests `OfficialSiteDiscoverySourceTests`:
  - Extracción de PDFs desde HTML de muestra para cada fibra (fixtures en `tests/Fixtures/`)
  - Caso: página sin PDFs → lista vacía
  - Período inferido correctamente desde nombre de archivo

- [x] T7.2 — Unit tests `SomaDiscoverySourceTests`:
  - Deserialización de respuesta JSON del endpoint `wp-json/soma/documents`
  - Filtrado correcto de tipo "quarterly"
  - Fixture: `tests/Fixtures/soma-documents-api-sample.json`

- [x] T7.3 — Unit tests `BmvDiscoverySourceTests`:
  - Extracción de links `indrpfn_*.pdf` desde HTML de BMV
  - Conversión correcta `2026-01` → `Q1-2026`
  - Fixture: `tests/Fixtures/bmv-fiho-sample.html`

- [x] T7.4 — Unit tests `AmefibraDiscoverySourceTests`:
  - Caching de listings: `GetListingItemsAsync` se llama una sola vez por instancia aunque se llame `DiscoverCandidatesAsync` para varias fibras
  - Fibra no en Amefibra → retorna `[]`

- [x] T7.5 — Unit tests `FundamentalsAutomationServiceTests` multi-source:
  - Dos fuentes, un candidato nuevo en cada una → 2 records creados
  - Misma fibra + período desde dos fuentes → segundo queda `possibleUpdate`
  - Fuente que lanza excepción → error aislado, resto continúa

- [x] T7.6 — Regression: `FundamentalsHistory` en Ops sigue mostrando registros de Amefibra existentes

### T8 — Build final y codegen

- [x] T8.1 — `dotnet build FIBRADIS.slnx` sin errores
- [x] T8.2 — `dotnet ef database update` aplica migración de T1
- [x] T8.3 — `npm run codegen:api` si hay contratos de API nuevos/modificados
- [x] T8.4 — `dotnet test tests/Unit/` — 0 fallos
- [x] T8.5 — `dotnet test tests/Integration/` — 0 fallos

## Dev Notes

### Hallazgos de investigación real (2026-06-02)

**Amefibra está activo y actualizado hasta 2026.** Tiene 41 páginas de paginación con reportes hasta 2026 T1. Cubre 11 de las 19 FIBRAs activas (FUNO, DANHOS, FMTY, FINN, EDUCA, FIBRAPL, FIBRAUP, FNOVA, FPLUS, FSHOP, STORAGE).

**Las 8 FIBRAs fuera de Amefibra y sus fuentes verificadas:**

```
FIBRAMQ12 → fibramacquarie.com  HTML estático
            PDFs: /assets/fibra/docs/events-and-presentations/{year}/fibra-mq-mx-{Q}-earnings-release-spa.pdf

FIHO12    → BMV                 Scraping HTML
            PDFs: https://www.bmv.com.mx/docs-pub/indrpfn/indrpfn_{id}_{year}-{quarter}_1.pdf
            ID BMV: FIHO-30057

VESTA15   → vesta.com.mx        HTML estático
            PDFs: /storage/app/uploads/public/{hash}/{hash}.pdf  (hash en path, link en página)

HCITY17   → BMV                 Scraping HTML  (sitio propio ECONNREFUSED)
            PDFs: https://www.bmv.com.mx/docs-pub/indrpfn/indrpfn_{id}_{year}-{quarter}_1.pdf
            ID BMV: HCITY-31249

NEXT25    → fibranext.mx        HTML estático
            PDFs: /site_media/uploads/documentos/documento-{code}-{timestamp}.pdf

SOMA21    → REST API JSON        Sin HTML scraping
            Endpoint: GET https://fibrasoma.group/wp-json/soma/documents
            Retorna lista completa con URLs directas de PDFs (21 trimestres desde 1Q21 hasta 1Q26)

FHIPO14   → fhipo.com           WordPress HTML estático
            PDFs: https://fhipo.com/wp-content/uploads/{year}/{month}/{filename}.pdf
            (ya tiene ReportsUrl correcta en BD)

FCFE18    → cfecapital.com.mx   WordPress HTML estático
            PDFs: https://cfecapital.com.mx/wp-content/uploads/{year}/{month}/{filename}.pdf
```

**FUNO11 y NEXT25** comparten el mismo patrón de URL de PDF (`/site_media/uploads/documentos/documento-{code}-{timestamp}.pdf`). Aunque FUNO está en Amefibra, también puede tener discovery propio — la deduplicación por `packageUrl` previene reprocesamiento.

**FMTY14** tiene PDFs en CDN de InvestorCloud (`cdn.investorcloud.net/fibramty/...`) pero su página pública no lista los PDFs directamente. Su ReportsUrl apunta al hub general de inversores; Amefibra lo cubre para el pipeline. El dev puede verificar si existe una URL de listado más específica antes de marcar T1 completo.

### Arquitectura de fuentes de discovery

```
IFundamentalsDiscoverySource
├── AmefibraDiscoverySource        cubre: 11 FIBRAs en Amefibra
│     adapta IAmefibraDiscoveryClient + cachea listings por corrida
├── OfficialSiteDiscoverySource    cubre: FIBRAMQ, VESTA, FHIPO, FCFE, NEXT, FUNO
│     catálogo interno con selector CSS por fibra (patrón SiteExtractionCatalog)
├── SomaDiscoverySource            cubre: SOMA21
│     GET fibrasoma.group/wp-json/soma/documents → JSON
└── BmvDiscoverySource             cubre: FIHO, HCITY
      scraping HTML de página BMV por fibra (URL desde fibra.ReportsUrl)
```

El `FundamentalsAutomationService` itera las fuentes por fibra. El ciclo queda así:

```csharp
foreach (var fibra in activeFibras)
    foreach (var source in discoverySources.Where(s => s.SupportedTickers.Contains(fibra.Ticker)))
        foreach (var candidate in await source.DiscoverCandidatesAsync(fibra, ct))
            await ProcessCandidateAsync(fibra, candidate, ct);
```

`AmefibraDiscoverySource` es multi-fibra internamente pero expone el mismo contrato por-fibra. Su `SupportedTickers` devuelve todos los tickers activos; internamente cachea los listings para no llamar a Amefibra más de una vez.

### URL de Amefibra — permanece en código

La URL de Amefibra (`https://amefibra.com/reportes-de-fibras/`) queda hardcodeada en `AmefibraDiscoveryClient` como está actualmente, igual que las URLs de Gemini y DeepSeek. No se mueve a BD ni a `OperationalConfig`.

Para las fuentes oficiales por fibra, la URL de descubrimiento se deriva de `fibra.ReportsUrl` (ya en BD) — excepto `SomaDiscoverySource` cuyo endpoint de API es fijo en código.

### Reglas críticas a preservar de historias previas

Todas aplican igual que en 5-11:
- **Async end-to-end** en clientes HTTP (no `.GetAwaiter().GetResult()`)
- **Warmup HTTP lazy** y tolerante a fallos (un solo intento al inicio)
- **BD antes que disco**: `fundamentalRepo.AddAsync(record)` → `SavePdfAsync()` en ese orden
- **`CancellationToken.None`** en escrituras post-catch de manifests
- **No `Task.WhenAll`** sobre el mismo DbContext (secuencial siempre)

### Fixtures de test requeridos

```
tests/Fixtures/
  fibramq-investors-sample.html         HTML de fibramacquarie.com/es/inversionistas.html
  vesta-financial-info-sample.html      HTML de vesta.com.mx/informacion-financiera/asg
  bmv-fiho-sample.html                  HTML de bmv.com.mx/.../FIHO-30057-CGEN_CAPIT
  bmv-hcity-sample.html                 HTML de bmv.com.mx/.../HCITY-31249-CGEN_CAPIT
  soma-documents-api-sample.json        Respuesta de fibrasoma.group/wp-json/soma/documents
  fhipo-reportes-sample.html            HTML de fhipo.com/es/reportes-trimestrales/
  cfecapital-info-financiera-sample.html HTML de cfecapital.com.mx/informacion-financiera
```

### Project Structure Notes

**Archivos nuevos:**
```
src/Server/Application/Fundamentals/
  IFundamentalsDiscoverySource.cs
  FundamentalsDiscoveryCandidate.cs
  OfficialSitePeriodParser.cs

src/Server/Infrastructure/Integrations/PdfDiscovery/
  AmefibraDiscoverySource.cs
  OfficialSiteDiscoverySource.cs      (FIBRAMQ, VESTA, FHIPO, FCFE, NEXT, FUNO)
  SomaDiscoverySource.cs
  BmvDiscoverySource.cs               (FIHO, HCITY)
```

**Archivos modificados:**
```
src/Server/Infrastructure/Persistence/Seed/CatalogSeed.cs
src/Server/Infrastructure/Persistence/Migrations/           (nueva migración)
src/Server/Infrastructure/Jobs/Fundamentals/FundamentalsAutomationService.cs
src/Server/Api/CompositionRoot/ApiServiceExtensions.cs
src/Server/Infrastructure/Persistence/SqlServer/AppDbContext.cs  (snapshot actualización)
tests/Fixtures/                                             (nuevos HTML/JSON)
```

### Referencias

- [src/Server/Infrastructure/Persistence/Seed/CatalogSeed.cs](src/Server/Infrastructure/Persistence/Seed/CatalogSeed.cs)
- [src/Server/Application/Fundamentals/IAmefibraDiscoveryClient.cs](src/Server/Application/Fundamentals/IAmefibraDiscoveryClient.cs)
- [src/Server/Infrastructure/Integrations/PdfDiscovery/AmefibraDiscoveryClient.cs](src/Server/Infrastructure/Integrations/PdfDiscovery/AmefibraDiscoveryClient.cs)
- [src/Server/Infrastructure/Jobs/Fundamentals/FundamentalsAutomationService.cs](src/Server/Infrastructure/Jobs/Fundamentals/FundamentalsAutomationService.cs)
- [src/Server/Domain/Fundamentals/FundamentalSourceManifest.cs](src/Server/Domain/Fundamentals/FundamentalSourceManifest.cs)
- [_bmad-output/implementation-artifacts/5-11-amefibra-pdf-sync.md](5-11-amefibra-pdf-sync.md) — pipeline Amefibra, reglas que no romper
- [_bmad-output/planning-artifacts/convenciones-fibradis.md](../planning-artifacts/convenciones-fibradis.md)
- [Source: docs/req/prd.md#FR-02, FR-20] — ReportsUrl para discovery; modo Api fundamentales

## Dev Agent Record

### Agent Model Used

claude-sonnet-4-6

### Debug Log References

- Investigación real de Amefibra el 2026-06-02: 41 páginas con datos hasta 2026 T1. 11 FIBRAs activas cubiertas.
- Investigación real de las 8 fuentes oficiales el 2026-06-02: URLs, patrones de PDF y estrategia de scraping verificados por fetch directo.
- SOMA21: endpoint REST `wp-json/soma/documents` devuelve 21 reportes trimestrales (1Q21–1Q26) en JSON.
- FIHO12 y HCITY17: sitio propio sin PDFs estáticos; BMV es fuente regulatoria confirmada.
- TERRA13: state=Inactive en BD — excluir del pipeline y de la actualización de ReportsUrl.

### Completion Notes List

- T1: 11 FIBRAs actualizadas en CatalogSeed.cs (10 nuevas + FCFE18 corregida). Migración `20260603011150_UpdateFibraReportsUrls` generada y aplicada. Solo contiene `UpdateData` sin `InsertData`/`DeleteData`.
- T2: `IFundamentalsDiscoverySource` + `FundamentalsDiscoveryCandidate` en Application/Fundamentals/. `AmefibraDiscoverySource` adapta el cliente Amefibra existente con caché interna de listings por corrida. `FundamentalsAutomationService` refactorizado: usa `IEnumerable<IFundamentalsDiscoverySource>` + `IHttpClientFactory` en lugar de `IAmefibraDiscoveryClient`. Loop fibra-first con `SupportedTickers.Any() || Contains(ticker)`.
- T3+T4: `OfficialSiteDiscoverySource` con catálogo interno para 6 fibras (FIBRAMQ12, VESTA15, FHIPO14, FCFE18, NEXT25, FUNO11). Filtro español en FIBRAMQ12. `OfficialSitePeriodParser` con regex combinada que maneja `1q26`, `Q1-2024`, `1T26`.
- T5: `SomaDiscoverySource` llama `GET wp-json/soma/documents`. Filtra quarterly por campo `type` o heurísticas de título/URL.
- T6: `BmvDiscoverySource` para FIHO12 y HCITY17. Extrae links `indrpfn_*.pdf` con AngleSharp. Convierte `{year}-{quarter}` → `Q{q}-{year}`.
- T7: 17 tests nuevos en 4 archivos. 7 fixtures HTML/JSON creados. 198 unit tests + 207 integration tests, 0 fallos.
- T8: Build Release 0 errores. `dotnet ef database update` aplicado. `codegen:api` ejecutado (detectó parámetro `recent` preexistente).

### File List

**Archivos nuevos:**
- src/Server/Application/Fundamentals/IFundamentalsDiscoverySource.cs
- src/Server/Application/Fundamentals/FundamentalsDiscoveryCandidate.cs
- src/Server/Application/Fundamentals/OfficialSitePeriodParser.cs
- src/Server/Infrastructure/Integrations/PdfDiscovery/AmefibraDiscoverySource.cs
- src/Server/Infrastructure/Integrations/PdfDiscovery/OfficialSiteDiscoverySource.cs
- src/Server/Infrastructure/Integrations/PdfDiscovery/SomaDiscoverySource.cs
- src/Server/Infrastructure/Integrations/PdfDiscovery/BmvDiscoverySource.cs
- src/Server/Infrastructure/Migrations/20260603011150_UpdateFibraReportsUrls.cs
- src/Server/Infrastructure/Migrations/20260603011150_UpdateFibraReportsUrls.Designer.cs
- tests/Fixtures/fibramq-investors-sample.html
- tests/Fixtures/vesta-financial-info-sample.html
- tests/Fixtures/bmv-fiho-sample.html
- tests/Fixtures/bmv-hcity-sample.html
- tests/Fixtures/soma-documents-api-sample.json
- tests/Fixtures/fhipo-reportes-sample.html
- tests/Fixtures/cfecapital-info-financiera-sample.html
- tests/Unit/Infrastructure.Tests/Integrations/PdfDiscovery/OfficialSiteDiscoverySourceTests.cs
- tests/Unit/Infrastructure.Tests/Integrations/PdfDiscovery/SomaDiscoverySourceTests.cs
- tests/Unit/Infrastructure.Tests/Integrations/PdfDiscovery/BmvDiscoverySourceTests.cs
- tests/Unit/Infrastructure.Tests/Integrations/PdfDiscovery/AmefibraDiscoverySourceTests.cs

**Archivos modificados:**
- src/Server/Infrastructure/Persistence/Seed/CatalogSeed.cs
- src/Server/Infrastructure/Jobs/Fundamentals/FundamentalsAutomationService.cs
- src/Server/Api/CompositionRoot/ApiServiceExtensions.cs
- src/Server/Infrastructure/Migrations/AppDbContextModelSnapshot.cs
- src/Web/SharedApiClient/schema.d.ts
- tests/Unit/Infrastructure.Tests/Jobs/Fundamentals/FundamentalsAutomationServiceTests.cs
- _bmad-output/implementation-artifacts/sprint-status.yaml

## Change Log

- 2026-06-02 — Story 5.12 creada con investigación real de Amefibra (41 pp, hasta 2026) y las 8 fuentes oficiales. Arquitectura 4 discovery sources. 19 URLs de ReportsUrl verificadas.
- 2026-06-02 — Revisada y completada: URL de Amefibra permanece en código; todas las FIBRAs activas tienen ReportsUrl verificada.
- 2026-06-03 — Implementación completa: AC1 migración aplicada, AC2 IFundamentalsDiscoverySource + refactor servicio, AC3 4 discovery clients (OfficialSite, Soma, BMV, Amefibra), AC4 regression OK. 198 unit + 207 integration tests verdes.
