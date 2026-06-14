---
title: 'Economatica como fuente primaria de fundamentales con fallback de ticker'
type: 'feature'
created: '2026-06-13'
status: 'done'
context: []
baseline_commit: 'ddc58637d9d315c632da3392313ef6c7508684ef'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** El pipeline de fundamentales solo consulta Economatica para una lista blanca hardcodeada de 19 tickers que no incluye FVIA16, pese a que `http://www.economatica.mx/FVIA/REPORTES%20TRIMESTRALES/` sí existe. Además Economatica corre al final del orden de fuentes, dejando que AMEFIBRA (match difuso por nombre, que falla con "Fibra E Vía") domine. Resultado: FVIA no trae reportes.

**Approach:** Volver `EconomaticaDiscoverySource` universal (aplica a todas las FIBRAs activas) y registrarlo como la PRIMERA fuente de discovery. Dentro de la fuente, intentar varias formas de ticker (base, sin dígitos finales, completo, variantes de nombre) y quedarse con la primera que devuelva PDFs; si ninguna funciona, devolver lista vacía sin lanzar excepción.

## Boundaries & Constraints

**Always:**
- `SupportedTickers => []` (universal), igual que AMEFIBRA.
- Economatica registrado antes que AMEFIBRA en DI (primera fuente del `IEnumerable`).
- Probar formas de ticker en orden, deduplicadas case-insensitive; la primera que devuelva ≥1 PDF gana y corta el resto.
- Si ninguna forma devuelve PDFs (incluye 404/errores de red en todas), devolver `[]` sin lanzar.
- Conservar `ParseEconomaticaPeriod`, el selector `a.ico_pdf[href]`, el formato de URL y la estructura de `FundamentalsDiscoveryCandidate`.
- Solo HTTP (cert SSL vencido en economatica.mx).

**Ask First:**
- Cambiar el `SourceName` de los candidatos (hoy `economatica:{Ticker}`) o el `recordId`/dedup del automation service.

**Never:**
- Tocar otras fuentes (AMEFIBRA, OfficialSite, Norte19) ni el `FundamentalsAutomationService`.
- Reintentos con backoff, caching entre fibras, ni paralelismo dentro de la fuente.
- Mapear variantes de nombre a algo que no sea un código tipo-ticker normalizado (Economatica usa códigos, no nombres largos).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Ticker base encuentra | `FVIA16`, URL `/FVIA/` devuelve PDFs | Candidatos de `/FVIA/`; no se prueban más formas | N/A |
| Fallback a alternativa | Forma base 404, forma siguiente devuelve PDFs | Candidatos de la forma que sí respondió | 404 en forma previa se ignora |
| No listada en Economatica | Todas las formas 404 / sin PDFs | `[]` | Sin excepción, sin log de error |
| Página responde sin `ico_pdf` | HTML 200 pero 0 PDFs | Tratar como "no encontró"; probar siguiente forma | N/A |
| Ticker ≤ 2 chars | `Ticker.Length <= 2` | Omitir la forma `Ticker[..^2]`; usar otras formas | N/A |

</frozen-after-approval>

## Code Map

- `src/Server/Infrastructure/Integrations/PdfDiscovery/EconomaticaDiscoverySource.cs` -- fuente a modificar: `SupportedTickers`, lógica multi-forma, manejo de 404.
- `src/Server/Api/CompositionRoot/ApiServiceExtensions.cs` -- reordenar registro DI (~204-237): Economatica primero.
- `src/Server/Infrastructure/Jobs/Fundamentals/FundamentalsAutomationService.cs` -- consumidor (gating `SupportedTickers`, dedup por período); NO se modifica, se respeta.
- `src/Server/Domain/Catalog/Fibra.cs` -- `Ticker`, `NameVariants` para derivar formas.
- `tests/Unit/Infrastructure.Tests/Integrations/PdfDiscovery/EconomaticaDiscoverySourceTests.cs` -- tests a actualizar/extender.
- `tests/Fixtures/economatica-fhipo-sample.html` -- fixture HTML existente reutilizable.

## Tasks & Acceptance

**Execution:**
- [x] `EconomaticaDiscoverySource.cs` -- cambiar `SupportedTickers` a `[]`; extraer la lógica de fetch+parse a un método por-forma; construir lista ordenada y deduplicada de formas de ticker (`Ticker[..^2]` si len>2, `Ticker.TrimEnd(dígitos)`, `Ticker`, variantes normalizadas); iterar hasta la primera con PDFs; envolver fetch en try/catch para tratar 404/errores como "sin resultados" y continuar; devolver `[]` si ninguna forma rinde.
- [x] `ApiServiceExtensions.cs` -- mover el bloque de registro de `EconomaticaDiscoverySource` para que sea el primer `IFundamentalsDiscoverySource` registrado (antes de AMEFIBRA).
- [x] `EconomaticaDiscoverySourceTests.cs` -- reemplazar `SupportedTickers_Contains19...` por aserción de lista vacía; agregar handler de test que mapee URL→respuesta; cubrir: base encuentra, fallback a alternativa, todas 404 → `[]` sin throw. Mantener tests de parseo/URL/período.

**Acceptance Criteria:**
- Given una FIBRA activa cualquiera (sin lista blanca), when corre el pipeline de fundamentales, then Economatica se intenta primero, antes que AMEFIBRA.
- Given FVIA16 y que `/FVIA/` tiene reportes, when corre la fuente, then devuelve los candidatos trimestrales de `/FVIA/`.
- Given una FIBRA cuyo código no existe en Economatica, when corre la fuente, then devuelve `[]` y el `PipelineErrorLog` no registra error por esa FIBRA.

## Design Notes

Orden de formas (deduplicar con `StringComparer.OrdinalIgnoreCase`, omitir vacías):
1. `Ticker[..^2]` — sufijo de serie de 2 dígitos (proven: `FHIPO14`→`FHIPO`, `FVIA16`→`FVIA`). Solo si `Length > 2`.
2. `Ticker.TrimEnd('0'..'9')` — quita todos los dígitos finales (cubre sufijos no-2-dígitos).
3. `Ticker` completo — algunos códigos incluyen el número.
4. `NameVariants` normalizadas (mayúsculas, solo alfanumérico) — red de seguridad si una variante es un código.

"Devuelve PDFs" = la página respondió 200 y produjo ≥1 candidato `a.ico_pdf`. Tragar errores es intencional (requisito 4): Economatica es primaria best-effort y AMEFIBRA rellena huecos; no se quiere ruido en `PipelineErrorLog` para las ~decenas de FIBRAs ausentes de Economatica.

`SourceName` se mantiene `economatica:{fibra.Ticker}` (estable, atado a la FIBRA en BD); la forma que casó queda implícita en la URL del candidato.

## Verification

**Commands:**
- `dotnet build FIBRADIS.slnx` -- expected: compila sin errores.
- `dotnet test tests/Unit/Infrastructure.Tests --filter "FullyQualifiedName~EconomaticaDiscoverySource"` -- expected: todos los tests pasan, incluidos los nuevos de fallback.

## Suggested Review Order

**Prioridad de la fuente (DI)**

- Entrada: por qué Economatica primero — el orden del `IEnumerable` decide la fuente primaria; las demás rellenan huecos vía dedup por período.
  [`ApiServiceExtensions.cs:203`](../../src/Server/Api/CompositionRoot/ApiServiceExtensions.cs#L203)

**Lógica de descubrimiento multi-forma**

- Bucle principal: prueba formas en orden, primera con PDFs gana, `[]` sin lanzar si ninguna rinde.
  [`EconomaticaDiscoverySource.cs:31`](../../src/Server/Infrastructure/Integrations/PdfDiscovery/EconomaticaDiscoverySource.cs#L31)

- Manejo de errores (el punto más delicado): cancelación real del caller se propaga; 404/timeout HTTP se tratan como "forma no resolvió".
  [`EconomaticaDiscoverySource.cs:52`](../../src/Server/Infrastructure/Integrations/PdfDiscovery/EconomaticaDiscoverySource.cs#L52)

- Derivación de formas de ticker, deduplicadas case-insensitive, con guard de `NameVariants` null.
  [`EconomaticaDiscoverySource.cs:97`](../../src/Server/Infrastructure/Integrations/PdfDiscovery/EconomaticaDiscoverySource.cs#L97)

- Fuente universal: `SupportedTickers` vacío (sin whitelist).
  [`EconomaticaDiscoverySource.cs:21`](../../src/Server/Infrastructure/Integrations/PdfDiscovery/EconomaticaDiscoverySource.cs#L21)

**Tests**

- Base-encuentra (no prueba alternativas), fallback-con-orden, todas-404→`[]`, timeout-no-aborta, cancelación-propaga.
  [`EconomaticaDiscoverySourceTests.cs:79`](../../tests/Unit/Infrastructure.Tests/Integrations/PdfDiscovery/EconomaticaDiscoverySourceTests.cs#L79)
