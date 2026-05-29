---
title: 'Compactación de texto Markdown post-extracción PDF'
type: 'feature'
created: '2026-05-27'
status: 'ready-for-dev'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** El texto extraído por `PdfMarkdownExtractor.Extract()` es texto plano crudo (PdfPig concatena `page.Text` por página). Llega al extractor de KPIs por IA cargado de artefactos: headers/footers repetidos, números de página, dobles espacios, saltos de línea raros, disclaimers repetidos y cortes de palabra por OCR. Esto aumenta tokens consumidos y puede degradar la calidad de extracción.

**Approach:** Crear `MarkdownCompactor.Compact(text)` como clase estática separada con responsabilidad única de limpieza. Llamarla en el endpoint (`OpsFundamentalsEndpoints`) justo después de `PdfMarkdownExtractor.Extract()`, antes de pasar el texto a `kpiExtractor.ExtractAsync()`. El compactor NO vive dentro del extractor (opción B, separación de responsabilidades).

## Boundaries & Constraints

**Always:**
- Preservar todo contenido numérico: porcentajes, montos, ratios, fechas (el AI los necesita para extraer KPIs)
- La limpieza es determinista, sin IA, solo regex/string operations
- Aplicar reglas en orden fijo (cada regla opera sobre el output de la anterior)
- El `MarkdownLength` devuelto en `KpiExtractionDto` debe reflejar el texto ya compactado

**Ask First:**
- Si en el futuro se requiere configuración de umbral de frecuencia para dedup

**Never:**
- No mover la llamada a Compact() dentro de `PdfMarkdownExtractor.Extract()`
- No reordenar bloques de contenido ni aplicar transformaciones semánticas
- No usar IA en el compactor
- No tocar la lógica de almacenamiento de `MarkdownContent` en BD (se guarda el texto compactado, que es mejor que el crudo)

## I/O & Edge-Case Matrix

| Scenario | Input | Expected Output | Error Handling |
|----------|-------|-----------------|----------------|
| Líneas solo números (pág.) | `"Texto útil\n123\nMás texto"` | `"Texto útil\nMás texto"` | N/A |
| Múltiples líneas en blanco | `"A\n\n\n\nB"` | `"A\n\nB"` | N/A |
| Dobles espacios | `"Cap  Rate  15%"` | `"Cap Rate 15%"` | N/A |
| Línea repetida ≥3 veces | `"DISC\nContenido\nDISC\nMás\nDISC"` | línea `"DISC"` removida | N/A |
| Corte OCR con guión | `"estruc-\ntura operativa"` | `"estructura operativa"` | N/A |
| Separador decorativo | `"---\n=====\n***"` | eliminado | N/A |
| Texto vacío / null | `""` o `null` | `""` sin excepción | Retorno temprano |

</frozen-after-approval>

## Code Map

- `src/Server/Infrastructure/Integrations/Pdf/MarkdownCompactor.cs` — nueva clase estática con `Compact(string text): string`
- `src/Server/Api/Endpoints/Ops/OpsFundamentalsEndpoints.cs:163` — punto de integración: llamar `MarkdownCompactor.Compact(markdown)` tras `PdfMarkdownExtractor.Extract()`
- `tests/Unit/Infrastructure.Tests/Integrations/Pdf/MarkdownCompactorTests.cs` — tests unitarios cubriendo la I/O Matrix completa

## Tasks & Acceptance

**Execution:**
- [ ] `src/Server/Infrastructure/Integrations/Pdf/MarkdownCompactor.cs` -- Crear clase estática con método `Compact(string? text): string` aplicando las 7 reglas en orden -- centraliza la lógica de limpieza de forma testeable e independiente del extractor
- [ ] `src/Server/Api/Endpoints/Ops/OpsFundamentalsEndpoints.cs` -- Tras la llamada a `PdfMarkdownExtractor.Extract(stream)` en el endpoint `POST /extract-kpis`, agregar `markdown = MarkdownCompactor.Compact(markdown)` -- integra limpieza en el flujo sin acoplar el extractor
- [ ] `tests/Unit/Infrastructure.Tests/Integrations/Pdf/MarkdownCompactorTests.cs` -- Tests unitarios para cada escenario de la I/O Matrix -- garantiza comportamiento correcto de cada regla de forma aislada

**Acceptance Criteria:**
- Given texto con líneas que son solo dígitos, when `Compact()`, then esas líneas son removidas
- Given texto con 3+ líneas en blanco consecutivas, when `Compact()`, then quedan máximo 2 consecutivas
- Given texto con dobles espacios, when `Compact()`, then espacios múltiples colapsan a uno
- Given una línea que aparece ≥3 veces en el documento sin contener dígitos, when `Compact()`, then esa línea es removida de todas las ocurrencias
- Given corte OCR con guión al final de línea (`palabra-\nsiguiente`), when `Compact()`, then el guión y salto se eliminan uniendo la palabra
- Given separadores decorativos (`---`, `===`, `***`, `___` de 3+ chars solos en su línea), when `Compact()`, then son removidos
- Given string vacío o null, when `Compact()`, then retorna `""` sin lanzar excepción

## Design Notes

Las 7 reglas se aplican en este orden sobre el texto completo:

1. **Join hyphenated OCR breaks**: `(\w+)-\r?\n(\w+)` → `$1$2`
2. **Remove page numbers**: líneas que son solo `^\s*\d{1,4}\s*$` → eliminar
3. **Remove decorative separators**: líneas que son solo `[-=*_]{3,}` → eliminar
4. **Frequency-based dedup**: contar ocurrencias de cada línea (trim); líneas sin dígitos que aparecen ≥3 veces → eliminar todas
5. **Collapse multiple spaces**: `[ \t]{2,}` → un espacio
6. **Trim lines**: cada línea se aplica `.Trim()`
7. **Collapse blank lines**: 3+ saltos de línea consecutivos → `\n\n`

El umbral de frecuencia es 3 (no configurable en v1). Los dígitos excluyen una línea de dedup para no eliminar líneas como "15% 15% 15%" que podrían ser valores repetidos intencionalmente.

## Verification

**Commands:**
- `dotnet build FIBRADIS.slnx` -- expected: Build succeeded, 0 errors
- `dotnet test tests/Unit/Infrastructure.Tests/ --filter "FullyQualifiedName~MarkdownCompactor"` -- expected: all tests pass

## Spec Change Log

