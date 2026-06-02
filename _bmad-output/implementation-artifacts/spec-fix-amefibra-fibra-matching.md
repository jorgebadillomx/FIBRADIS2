---
title: 'Fix AMEFIBRA fibra matching — colisión prefijo Prologis/Plus'
type: 'bugfix'
created: '2026-06-02'
status: 'done'
baseline_commit: '2d950d6c528dc44a09aec1af0380e9f35c653de3'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** `MatchFibra()` en `FundamentalsAutomationService` usa `FirstOrDefault` con matching de subcadena bidireccional; el ticker normalizado de Fibra Prologis "FIBRAPL" (7 chars) es prefijo de "fibraplus" (hint normalizado de Fibra Plus), y como Prologis precede a Plus en el orden de la BD, todos los PDFs de Fibra Plus han sido asignados erróneamente a Fibra Prologis.

**Approach:** Cambiar el algoritmo a score-based best-match (ratio de overlap candidate/hint), eligiendo la fibra con el score más alto en vez de la primera con cualquier match. Aplicar una migración de datos SQL para corregir los registros ya corrompidos.

## Boundaries & Constraints

**Always:**
- El algoritmo corregido debe seguir siendo insensible a mayúsculas/acentos (la normalización existente se conserva).
- La data migration debe ejecutarse en el mismo `dotnet ef database update` del deploy, no requerir pasos manuales.
- Los `FundamentalRecord` y `FundamentalSourceManifest` afectados deben quedar con `FibraId` de Fibra Plus (`32418186-9e2c-942b-8f4a-1e61388760a4`) y no de Fibra Prologis (`32377b6d-9244-a715-0279-2660cc6b62a5`).
- Agrega un test unitario que cubra el escenario Prologis-antes-de-Plus con ambas fibras presentes.

**Ask First:**
- Si en la BD hay `FundamentalRecord` de Fibra Plus Y de Fibra Prologis para el mismo período (mismo `Period` string), antes de mover datos consultar a Jorge — hay riesgo de duplicados semánticos.

**Never:**
- No borrar FundamentalRecord; solo actualizar FibraId.
- No cambiar la lógica de NormalizeMatchKey.
- No alterar el pipeline de ingestión más allá de MatchFibra.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Colisión prefijo — Prologis gana sin fix | fibras=[Prologis, Plus], hint="fibra plus" | **Bug:** devuelve Prologis (score "fibrapl"⊂"fibraplus" = 0.78) | — |
| Colisión prefijo — Plus gana con fix | fibras=[Prologis, Plus], hint="fibra plus" | Devuelve Plus (score "fibraplus"=="fibraplus" = 1.0 > 0.78) | — |
| PDF Prologis con hint exacto | fibras=[Prologis, Plus], hint="prologis" | Devuelve Prologis (score 1.0 via "prologis"=="prologis") | — |
| Hint ambiguo, sin match | fibras=[Prologis, Plus], hint="xyz" | Devuelve null | null → DiscoveryStatus "unmatched-fibra" |
| Dos fibras empatan con score = 1.0 | fibras=[A, B] ambas exactas | Devuelve la de Ticker alfabéticamente menor (tiebreaker determinista) | — |

</frozen-after-approval>

## Code Map

- `src/Server/Infrastructure/Jobs/Fundamentals/FundamentalsAutomationService.cs:246` — método `MatchFibra()` a reemplazar por score-based
- `src/Server/Infrastructure/Migrations/` — nueva migración EF Core con SQL de data-fix
- `tests/Unit/Infrastructure.Tests/Jobs/Fundamentals/FundamentalsAutomationServiceTests.cs` — test de colisión Prologis/Plus

## Tasks & Acceptance

**Execution:**
- [x] `src/Server/Infrastructure/Jobs/Fundamentals/FundamentalsAutomationService.cs` -- Reemplazar `MatchFibra()` con score-based best-match: para cada fibra calcular el score máximo entre sus candidatos normalizados (`max(candidate.Length/hint.Length, hint.Length/candidate.Length)` cuando hay substring match), retornar la fibra con el score más alto; tiebreaker por `fibra.Ticker` alfanumérico ascendente; score=0 si ningún candidato hace match.
- [x] `src/Server/Infrastructure/Migrations/<timestamp>_FixFibraPlusPrologisMismatch.cs` -- Nueva migración EF Core que ejecuta SQL: (1) actualiza `FundamentalRecords.FibraId` de Prologis a Plus donde el registro fue importado por "system:amefibra" y el manifiesto asociado (join por `LastProcessedRecordId`) tiene `SourceTitle` ILIKE `%fibra plus%` OR `%fplus%`; (2) actualiza `FundamentalSourceManifests.FibraId` de Prologis a Plus con mismo filtro de título.
- [x] `tests/Unit/Infrastructure.Tests/Jobs/Fundamentals/FundamentalsAutomationServiceTests.cs` -- Añadir test `ExecuteAsync_WhenFibrasPrologisAndPlusArePresent_FibraPlusTitleMatchesFibraPlus` que seeda dos fibras (FIBRAPL14 con NameVariants=["Fibra Prologis","FIBRAPL"] y FPLUS16 con FullName="Fibra Plus"), lanza el pipeline con un listing titulado "Fibra Plus Reporte Trimestral T4 2023", y verifica que el `FundamentalRecord` creado tenga `FibraId` de Fibra Plus.

**Acceptance Criteria:**
- Given que el catálogo tiene Fibra Prologis (con variante "FIBRAPL") y Fibra Plus, when el pipeline procesa un PDF con título "Fibra Plus Reporte Trimestral T4 2023", then el `FundamentalRecord` resultante tiene `FibraId` de Fibra Plus, no de Prologis.
- Given que la migración se aplica, when se consultan `FundamentalSourceManifests` con `FibraId` de Prologis y `SourceTitle` conteniendo "fibra plus" o "fplus", then el resultado es vacío (todos fueron actualizados a FibraId de Plus).
- Given un PDF con título "Prologis Reporte Trimestral T3 2023", when el pipeline lo procesa, then el `FundamentalRecord` tiene `FibraId` de Fibra Prologis.
- Given que el test suite corre `dotnet test tests/Unit/Infrastructure.Tests`, then todos los tests pasan.

## Spec Change Log

## Design Notes

**Score-based MatchFibra — lógica concreta:**

```csharp
private Fibra? MatchFibra(IReadOnlyList<Fibra> fibras, string? fibraHint)
{
    if (string.IsNullOrWhiteSpace(fibraHint)) return null;
    var normalizedHint = NormalizeMatchKey(fibraHint);

    return fibras
        .Select(fibra =>
        {
            var candidates = new[]
                    { fibra.Ticker, fibra.Ticker.TrimEnd('0','1','2','3','4','5','6','7','8','9'),
                      fibra.ShortName, fibra.FullName }
                .Concat(fibra.NameVariants)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(NormalizeMatchKey)
                .Distinct(StringComparer.Ordinal);

            var best = candidates.Aggregate(0.0, (acc, c) =>
            {
                double s = 0;
                if (c.Contains(normalizedHint, StringComparison.Ordinal))
                    s = Math.Max(s, (double)normalizedHint.Length / c.Length);
                if (normalizedHint.Contains(c, StringComparison.Ordinal))
                    s = Math.Max(s, (double)c.Length / normalizedHint.Length);
                return Math.Max(acc, s);
            });
            return (Fibra: fibra, Score: best);
        })
        .Where(x => x.Score > 0)
        .OrderByDescending(x => x.Score)
        .ThenBy(x => x.Fibra.Ticker, StringComparer.Ordinal)
        .Select(x => x.Fibra)
        .FirstOrDefault();
}
```

**Por qué score 1.0 gana:** "fibraplus" (hint) vs candidate "fibraplus" de Fibra Plus → ratio 9/9 = 1.0. Prologis aporta como mejor candidate "fibrapl" → ratio 7/9 ≈ 0.78. Plus gana.

**SQL data-fix (PostgreSQL):**

```sql
-- 1. Corregir FundamentalRecords vía join con manifiesto
UPDATE fundamentals."FundamentalRecord" fr
SET fibra_id = '32418186-9e2c-942b-8f4a-1e61388760a4'
FROM fundamentals."FundamentalSourceManifest" fsm
WHERE fsm.last_processed_record_id = fr.id
  AND fr.fibra_id = '32377b6d-9244-a715-0279-2660cc6b62a5'
  AND (LOWER(fsm.source_title) LIKE '%fibra plus%' OR LOWER(fsm.source_title) LIKE '%fplus%');

-- 2. Corregir manifiestos
UPDATE fundamentals."FundamentalSourceManifest"
SET fibra_id = '32418186-9e2c-942b-8f4a-1e61388760a4'
WHERE fibra_id = '32377b6d-9244-a715-0279-2660cc6b62a5'
  AND (LOWER(source_title) LIKE '%fibra plus%' OR LOWER(source_title) LIKE '%fplus%');
```

## Verification

**Commands:**
- `dotnet build FIBRADIS.slnx` -- expected: Build succeeded
- `dotnet test tests/Unit/Infrastructure.Tests --filter "FundamentalsAutomation"` -- expected: todos pasan incluyendo el nuevo test Prologis/Plus

## Suggested Review Order

**Algoritmo de matching (corrección del bug)**

- Score-based replace: `FirstOrDefault` → `Select+OrderByDescending`; elimina false positives por prefijo corto.
  [`FundamentalsAutomationService.cs:249`](../../src/Server/Infrastructure/Jobs/Fundamentals/FundamentalsAutomationService.cs#L249)

**Data migration (corrección de datos existentes)**

- SQL que corrige FundamentalRecords vía JOIN con manifest por `last_processed_record_id`.
  [`20260602190000_FixFibraPlusPrologisMismatch.cs:14`](../../src/Server/Infrastructure/Migrations/20260602190000_FixFibraPlusPrologisMismatch.cs#L14)

- SQL que corrige FundamentalSourceManifests directamente por título.
  [`20260602190000_FixFibraPlusPrologisMismatch.cs:27`](../../src/Server/Infrastructure/Migrations/20260602190000_FixFibraPlusPrologisMismatch.cs#L27)

**Test de regresión**

- Test que reproduce el bug original: Prologis primero en lista, título "Fibra Plus …", verifica FibraId de Plus.
  [`FundamentalsAutomationServiceTests.cs:158`](../../tests/Unit/Infrastructure.Tests/Jobs/Fundamentals/FundamentalsAutomationServiceTests.cs#L158)
