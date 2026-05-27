# Historia 5.7: Conclusiones IA por KPI y tooltips de definición en pantallas de fundamentales

Status: done

## Story

Como inversionista o analista que consulta los fundamentales de una FIBRA,
quiero ver junto a cada KPI su definición (qué es y cómo se calcula) y la conclusión que generó la IA al extraerlo del reporte,
para entender el dato en contexto sin necesidad de conocimiento previo de métricas inmobiliarias.

## Acceptance Criteria

### AC1 — El prompt de extracción devuelve una conclusión breve por cada campo numérico

**Dado que** el backend llama al endpoint de extracción,
**Cuando** la IA procesa el reporte,
**Entonces** el JSON devuelto incluye 6 campos nota adicionales (`capRateNote`, `navPerCbfiNote`, `ltvNote`, `noiMarginNote`, `ffoMarginNote`, `quarterlyDistributionNote`):
- Cada nota es una conclusión de 1-2 oraciones: si el valor fue explícito o calculado (con qué fórmula y datos base), y comparación con período anterior si el reporte la menciona
- Si el KPI correspondiente es `null`, la nota también es `null`
- Si el KPI tiene valor, la nota **no puede ser null**

### AC2 — Las notas se almacenan en BD junto al registro de fundamentales

**Dado que** el operador importa fundamentales (con o sin extracción IA),
**Cuando** el payload incluye `fieldNotes` (diccionario opcional),
**Entonces**:
- El backend persiste las notas como JSON en columna `FieldNotesJson` de `FundamentalRecord`
- Si `fieldNotes` es null o vacío, se guarda null — no es error
- Los endpoints GET devuelven `fieldNotes` como `Record<string, string>` en los DTOs

### AC3 — La ficha pública de FIBRA (Main) muestra definición + valor + conclusión IA por KPI

**Dado que** un inversionista visita la ficha pública de una FIBRA con fundamentales,
**Entonces** cada fila de la tabla de fundamentales muestra:
1. `{label} ⓘ` — al hacer hover sobre ⓘ se despliega un tooltip con la definición completa del KPI (texto fijo, ver Dev Notes)
2. El valor formateado (ya existente)
3. Si hay nota IA almacenada: debajo del valor, en texto secundario muted, la conclusión de la IA

**Layout de cada fila**:
```
NOI Margin ⓘ   Q1-2026 │ 79.2%
                        │ Calculado como NOI (inc. CU) / Ingresos. Creció 0.8pp vs 1T25.
```

### AC4 — FundamentalsHistory (Ops) muestra definiciones en cabeceras y notas por celda

**Dado que** un operador revisa el historial de fundamentales en Ops,
**Entonces**:
- Las cabeceras de columna KPI (`Cap Rate`, `NAV`, `LTV`, `NOI`, `FFO`, `Dist. Trim.`) incluyen un `ⓘ` con la definición al hacer hover
- Cada celda con valor KPI muestra un pequeño `ⓘ` después del número si tiene nota IA, con la nota en el tooltip de ese ícono
- Celdas sin nota: solo el valor, sin ícono adicional

### AC5 — El formulario de importación propaga las notas al payload de import

**Dado que** el operador usó "Extraer con IA" y obtuvo notas por campo,
**Cuando** hace submit del formulario de importación,
**Entonces** el payload `POST /import` incluye `fieldNotes` con las notas recibidas del endpoint `extract-kpis`.

---

## Tasks / Subtasks

### T1 — Backend: Actualizar prompt y parseo

- [x] T1.1 — Actualizar `KpiExtractionPrompt.cs` (o el método `Build`): añadir al JSON schema los 6 campos nota y sus instrucciones (ver Dev Notes — sección "Prompt actualizado")
- [x] T1.2 — Actualizar `KpiExtractionResult.cs`: añadir 6 propiedades string? (CapRateNote, NavPerCbfiNote, LtvNote, NoiMarginNote, FfoMarginNote, QuarterlyDistributionNote)
- [x] T1.3 — Actualizar `KpiExtractionDto.cs` (SharedApiContracts): mismo set de propiedades string?
- [x] T1.4 — Actualizar `KpiExtractionJsonParser.cs` (o donde se parsea el JSON de la IA): extraer los 6 campos nota del JSON y mapearlos al result/dto

### T2 — Backend: Persistencia de FieldNotes

- [x] T2.1 — Añadir columna `FieldNotesJson nvarchar(max) NULL` a `FundamentalRecord`:
  ```csharp
  public string? FieldNotesJson { get; private set; }
  ```
  Con método:
  ```csharp
  public void SetFieldNotes(Dictionary<string, string>? notes)
  {
      FieldNotesJson = notes is { Count: > 0 }
          ? JsonSerializer.Serialize(notes)
          : null;
  }
  ```
- [x] T2.2 — Crear migración EF Core: `AddFieldNotesJsonToFundamentalRecord`
- [x] T2.3 — Actualizar `NewsArticleConfiguration.cs` equivalente para fundamentales (si existe) o `AppDbContextModelSnapshot`
- [x] T2.4 — Actualizar `ImportFundamentalsRequest.cs` en SharedApiContracts: añadir `Dictionary<string, string>? FieldNotes`
- [x] T2.5 — Actualizar `POST /import` en `OpsFundamentalsEndpoints.cs`: llamar `record.SetFieldNotes(request.FieldNotes)` después de crear/actualizar el record
- [x] T2.6 — Actualizar `FundamentalRecordDto.cs` (SharedApiContracts): añadir `Dictionary<string, string>? FieldNotes`
- [x] T2.7 — Actualizar `FundamentalesPublicDto.cs` (SharedApiContracts): añadir `Dictionary<string, string>? FieldNotes`
- [x] T2.8 — Actualizar los mapeos en repositorio/endpoint GET para incluir `FieldNotes` deserializado del JSON
- [x] T2.9 — Regenerar `schema.d.ts` con `npm run codegen:api`

### T3 — Frontend Ops: Propagación de notas al import

- [x] T3.1 — En `FundamentalsImportForm.tsx`: al recibir el resultado de `extractKpisFromPdf`, guardar las 6 notas en un state `fieldNotes: Record<string, string>` (solo las no-null)
- [x] T3.2 — En el payload de `importFundamentals`, añadir `fieldNotes` desde el state (null si no hay extracción IA)
- [x] T3.3 — Actualizar la función `importFundamentals` en `fundamentalsApi.ts` para enviar `fieldNotes` en el body

### T4 — Frontend shared: Definiciones fijas de KPIs

- [x] T4.1 — Crear `src/Web/Main/src/shared/lib/kpi-definitions.ts` con el mapa de definiciones fijas (ver Dev Notes)
- [x] T4.2 — Crear `src/Web/Ops/src/lib/kpi-definitions.ts` con el mismo mapa
- [x] T4.3 — Crear componente `KpiLabel` en Main: `{label} + ⓘ con tooltip de definición`
  - Usar `title` attribute del HTML nativo (sin librería de tooltip) para keep it simple
  - Props: `kpiKey: string, label: string`

### T5 — Frontend Main: FundamentalesSection con definición + nota

- [x] T5.1 — En `FundamentalesSection.tsx`: para cada `item` que tenga `kpiKey`, usar `KpiLabel` en lugar del span plano del label
- [x] T5.2 — Añadir a `FundamentalesData` (o al item) el `kpiKey` para poder lookup la definición y la nota IA
- [x] T5.3 — Si el record tiene `fieldNotes[kpiKey]`: mostrar debajo del valor (dentro de la celda de valor) la nota en `text-xs text-muted-foreground italic`
- [x] T5.4 — `FibraPage.tsx`: pasar `fieldNotes` del DTO al item de `FundamentalesData`

### T6 — Frontend Ops: FundamentalsHistory con tooltips y notas

- [x] T6.1 — En `FundamentalsHistory.tsx`: reemplazar headers de columna KPI (`Cap Rate`, `NAV/CBFI`, `LTV`, `NOI`, `FFO`, `Dist. Trim.`) por `{header} ⓘ` usando `title` con la definición corta (formula únicamente, sin el texto largo)
- [x] T6.2 — En `HistoryRow`: para cada celda KPI, si `r.fieldNotes?.[key]` existe, mostrar `{value} ⓘ` donde el ⓘ tiene la nota como `title`

### T7 — Tests

- [x] T7.1 — Unit test `GeminiKpiExtractorServiceTests` / `DeepSeekKpiExtractorServiceTests`: verificar que el parser extrae los campos nota del JSON y los retorna en `KpiExtractionResult`
- [x] T7.2 — Integration test: `POST /import` con `fieldNotes` → `GET` devuelve los mismos `fieldNotes`
- [x] T7.3 — Integration test: `POST /import` sin `fieldNotes` (null) → `GET` devuelve `fieldNotes: null`, no error

---

## Dev Notes

### Prompt actualizado — añadir al JSON schema y a las instrucciones

Añadir 6 campos al JSON schema del prompt (después de los campos numéricos, antes de summary):

```json
{
  "capRate": null,
  "capRateNote": null,
  "navPerCbfi": null,
  "navPerCbfiNote": null,
  "ltv": null,
  "ltvNote": null,
  "noiMargin": null,
  "noiMarginNote": null,
  "ffoMargin": null,
  "ffoMarginNote": null,
  "quarterlyDistribution": null,
  "quarterlyDistributionNote": null,
  "summary": null,
  "extractionNotes": ""
}
```

Instrucciones a añadir por campo (inmediatamente después de la instrucción del campo numérico):

```
capRateNote (string o null):
  - Si capRate no es null: 1-2 oraciones indicando si fue explícito o calculado (con valores base: NOI trimestral y propiedades de inversión utilizados). Si el reporte incluye comparación vs período anterior, menciónala.
  - null si capRate es null.

navPerCbfiNote (string o null):
  - Si navPerCbfi no es null: 1-2 oraciones indicando si fue explícito o calculado (con valores base: patrimonio total y CBFIs en circulación). Si hay variación vs período anterior, menciónala.
  - null si navPerCbfi es null.

ltvNote (string o null):
  - Si ltv no es null: 1-2 oraciones indicando los valores usados (deuda total y propiedades de inversión) y si el apalancamiento aumentó/disminuyó vs período anterior si el reporte lo menciona.
  - null si ltv es null.

noiMarginNote (string o null):
  - Si noiMargin no es null: 1-2 oraciones indicando los valores base (NOI, ingresos totales), si se usó la versión inc./exc. CU, y tendencia vs período anterior si aplica.
  - null si noiMargin es null.

ffoMarginNote (string o null):
  - Si ffoMargin no es null: 1-2 oraciones indicando los valores base (FFO, ingresos totales) y tendencia vs período anterior si el reporte lo menciona.
  - null si ffoMargin es null.

quarterlyDistributionNote (string o null):
  - Si quarterlyDistribution no es null: 1-2 oraciones indicando si es distribución declarada o pagada, y variación vs distribución anterior si aplica.
  - null si quarterlyDistribution es null.
```

**Nota de implementación**: el orden alternado (campo + nota inmediata) en el schema JSON ayuda a la IA a asociar correctamente cada nota con su campo numérico.

### Definiciones fijas de KPIs (contenido del tooltip ⓘ)

```typescript
export const KPI_DEFINITIONS: Record<string, { label: string; formula: string; description: string }> = {
  capRate: {
    label: 'Cap Rate',
    formula: 'Cap Rate = NOI anualizado / Valor de propiedades de inversión',
    description:
      'Tasa de capitalización: mide el rendimiento operativo del portafolio inmobiliario en relación a su valor. ' +
      'Un Cap Rate más alto implica mayor rendimiento (y generalmente más riesgo); uno bajo refleja activos premium en alta demanda.',
  },
  navPerCbfi: {
    label: 'NAV por CBFI',
    formula: 'NAV = Valor de propiedades − Deuda total  |  NAV/CBFI = NAV / CBFIs en circulación',
    description:
      'Valor Neto de los Activos por certificado. Indica si el precio de mercado cotiza con descuento o premio ' +
      'respecto al valor real de los activos que respaldan cada CBFI. Métrica clave para evaluar si la FIBRA está cara o barata.',
  },
  ltv: {
    label: 'LTV',
    formula: 'LTV = Deuda total / Valor de propiedades de inversión',
    description:
      'Loan-to-Value: nivel de apalancamiento en relación al valor inmobiliario. ' +
      'LTV bajo indica solidez financiera y capacidad de endeudamiento futuro; LTV alto señala mayor exposición al riesgo financiero.',
  },
  noiMargin: {
    label: 'NOI Margin',
    formula: 'NOI Margin = NOI / Ingresos Totales',
    description:
      'Margen de Ingreso Neto Operativo: porcentaje de ingresos que queda tras descontar los gastos directos de operación. ' +
      'Mide la eficiencia operativa del portafolio — a mayor margen, menor desperdicio en gastos como mantenimiento, predial y servicios.',
  },
  ffoMargin: {
    label: 'FFO Margin',
    formula: 'FFO Margin = FFO / Ingresos Totales  |  FFO = Utilidad Neta + ajustes por valuación − ganancias cambiarias',
    description:
      'Fondos de Operación sobre ingresos. El FFO corrige la utilidad neta eliminando distorsiones contables ' +
      '(revaluaciones de propiedades, variaciones cambiarias) para mostrar cuánto genera realmente el portafolio en operación. ' +
      'Es el equivalente al margen neto pero depurado de ruido contable.',
  },
  quarterlyDistribution: {
    label: 'Dist. Trimestral',
    formula: 'Distribución = Resultado Fiscal Distribuido + Reembolso de Capital',
    description:
      'Pago en efectivo por CBFI cada trimestre. Las FIBRAs están obligadas a distribuir al menos el 95% de su resultado fiscal. ' +
      'Puede componerse de utilidades fiscales (gravables) y reembolso de capital (no gravable, reduce el costo fiscal del CBFI).',
  },
}
```

El tooltip debe mostrar `formula` + `description`. En el header de la tabla Ops puede mostrarse solo la `formula` (más corto).

### Tooltip implementation — usar HTML `title` nativo

No añadir ninguna librería de tooltips. Usar el atributo `title` nativo del HTML que todos los navegadores muestran al hacer hover:

```tsx
// Componente KpiLabel (Main)
export function KpiLabel({ kpiKey }: { kpiKey: string }) {
  const def = KPI_DEFINITIONS[kpiKey]
  if (!def) return <span>{kpiKey}</span>
  const tooltipText = `${def.formula}\n\n${def.description}`
  return (
    <span className="inline-flex items-center gap-1">
      {def.label}
      <span
        className="cursor-help text-muted-foreground/60 text-xs select-none"
        title={tooltipText}
      >
        ⓘ
      </span>
    </span>
  )
}
```

### Layout de FundamentalesSection actualizado

La tabla actual es `label | valor`. Con la nueva feature:

```tsx
<tr key={...}>
  <td className="py-2.5 pr-4">
    <KpiLabel kpiKey={item.kpiKey} />
    <span className="ml-2 text-xs text-muted-foreground/70">{item.period}</span>
  </td>
  <td className="py-2.5 text-right">
    <div className="font-mono font-medium tabular-nums">
      {formatFundamentalValue(item.value)}
    </div>
    {item.note && (
      <div className="mt-0.5 text-xs text-muted-foreground italic text-left">
        {item.note}
      </div>
    )}
  </td>
</tr>
```

El `item.note` puede ser `string | undefined`. Se muestra solo si está presente.

### Mapa de kpiKey en FibraPage

Al construir `FundamentalesData` en `FibraPage.tsx`, añadir `kpiKey` a cada item:

```typescript
const KPI_KEYS = ['capRate', 'navPerCbfi', 'ltv', 'noiMargin', 'ffoMargin', 'quarterlyDistribution'] as const
type KpiKey = typeof KPI_KEYS[number]

// Al mapear el DTO:
{
  label: KPI_DEFINITIONS[key].label,
  kpiKey: key,
  period: record.period,
  value: record[key as KpiKey],
  note: dto.fieldNotes?.[key] ?? undefined,
}
```

### Columna FieldNotesJson — EF Core

En `FundamentalRecordConfiguration` (o equivalente):
```csharp
builder.Property(x => x.FieldNotesJson)
    .HasColumnName("FieldNotesJson")
    .HasColumnType("nvarchar(max)")
    .IsRequired(false);
```

Al mapear a DTO, deserializar:
```csharp
FieldNotes = record.FieldNotesJson is not null
    ? JsonSerializer.Deserialize<Dictionary<string, string>>(record.FieldNotesJson)
    : null,
```

### Archivos a crear/modificar

**Backend (UPDATE)**:
- `src/Server/Infrastructure/Integrations/Ai/KpiExtractionPrompt.cs`
- `src/Server/Application/Fundamentals/KpiExtractionResult.cs`
- `src/Server/SharedApiContracts/Fundamentals/KpiExtractionDto.cs`
- `src/Server/Infrastructure/Integrations/Ai/KpiExtractionJsonParser.cs`
- `src/Server/Domain/Fundamentals/FundamentalRecord.cs`
- `src/Server/Infrastructure/Persistence/SqlServer/Configurations/Fundamentals/FundamentalRecordConfiguration.cs` (si existe)
- `src/Server/Api/Endpoints/Ops/OpsFundamentalsEndpoints.cs`
- `src/Server/SharedApiContracts/Fundamentals/ImportFundamentalsRequest.cs`
- `src/Server/SharedApiContracts/Fundamentals/FundamentalRecordDto.cs`
- `src/Server/SharedApiContracts/Fundamentals/FundamentalesPublicDto.cs`
- `scripts/codegen/Api.json` + `src/Web/SharedApiClient/schema.d.ts`

**Backend (NEW)**:
- `src/Server/Infrastructure/Persistence/Migrations/YYYYMMDDHHMMSS_AddFieldNotesJsonToFundamentalRecord.cs`

**Frontend (NEW)**:
- `src/Web/Main/src/shared/lib/kpi-definitions.ts`
- `src/Web/Main/src/shared/ui/KpiLabel.tsx`
- `src/Web/Ops/src/lib/kpi-definitions.ts`

**Frontend (UPDATE)**:
- `src/Web/Main/src/modules/ficha-publica/sections/FundamentalesSection.tsx`
- `src/Web/Main/src/modules/ficha-publica/sections/fundamentales.ts` (añadir kpiKey y note a items)
- `src/Web/Main/src/modules/ficha-publica/FibraPage.tsx`
- `src/Web/Ops/src/modules/fundamentals/FundamentalsHistory.tsx`
- `src/Web/Ops/src/modules/fundamentals/FundamentalsImportForm.tsx`
- `src/Web/Ops/src/api/fundamentalsApi.ts`

---

## Dev Agent Record

### Implementation Plan
- Extender extracción IA y contratos compartidos para devolver notas por KPI.
- Persistir `fieldNotes` en `FundamentalRecord` y exponerlo en endpoints Ops/Main.
- Propagar `fieldNotes` desde Ops import form hacia el backend.
- Renderizar definiciones KPI y notas IA en Main y Ops con `title` nativo.
- Cubrir parser + roundtrip import/GET con tests y regenerar cliente OpenAPI.

### Debug Log
- 2026-05-26 — Se creó branch `story/5-7-conclusiones-ia-por-kpi-y-tooltips-de-definicion` desde el estado actual del repo para aislar la historia sobre la base de `5-6`.
- 2026-05-26 — `dotnet ef migrations add AddFieldNotesJsonToFundamentalRecord --project src/Server/Infrastructure --startup-project src/Server/Api`
- 2026-05-26 — `npm run codegen:api`
- 2026-05-26 — `dotnet test tests/Unit/Infrastructure.Tests/Infrastructure.Tests.csproj --filter "FullyQualifiedName~KpiExtractorServiceTests"` → 4/4 passed
- 2026-05-26 — `dotnet test tests/Integration/Api.Tests/Api.Tests.csproj --filter "FullyQualifiedName~FundamentalsImportTests|FullyQualifiedName~FundamentalsExtractKpisTests"` → 25/25 passed
- 2026-05-26 — `npm run test --workspace=src/Web/Main` → 62/62 passed
- 2026-05-26 — `npm run build --workspace=src/Web/Main` → passed
- 2026-05-26 — `npm run build --workspace=src/Web/Ops` → passed

### Completion Notes
- Se extendió el flujo de extracción IA para devolver seis notas por KPI (`*Note`) en prompt, parser, servicios, DTOs y endpoint `extract-kpis`.
- Se agregó persistencia de notas por campo en `FundamentalRecord` mediante `FieldNotesJson`, con migración EF Core y mapeo de ida/vuelta en endpoints Ops y público.
- Main ahora renderiza `KpiLabel` con definición nativa por `title` y muestra la nota IA debajo del valor cuando existe.
- Ops ahora conserva `fieldNotes` tras `Extraer con IA`, los envía en `/import`, muestra fórmulas en headers y notas por celda en `FundamentalsHistory`.
- `Api.json` sí reflejó los contratos nuevos; `schema.d.ts` requirió ajuste manual posterior al codegen para alinear los tipos consumidos por los SPAs con el OpenAPI generado.
- Validación ejecutada y verde: 4 unit tests backend, 25 integration tests API, 62 tests Main, build Main, build Ops.

---

## File List

- _bmad-output/implementation-artifacts/5-7-conclusiones-ia-por-kpi-y-tooltips-de-definicion.md
- _bmad-output/implementation-artifacts/sprint-status.yaml
- scripts/codegen/Api.json
- src/Server/Api/Endpoints/Ops/OpsFundamentalsEndpoints.cs
- src/Server/Api/Endpoints/Public/FundamentalsEndpoints.cs
- src/Server/Application/Fundamentals/KpiExtractionResult.cs
- src/Server/Domain/Fundamentals/FundamentalRecord.cs
- src/Server/Infrastructure/Integrations/Ai/DeepSeekKpiExtractorService.cs
- src/Server/Infrastructure/Integrations/Ai/GeminiKpiExtractorService.cs
- src/Server/Infrastructure/Integrations/Ai/KpiExtractionJsonParser.cs
- src/Server/Infrastructure/Integrations/Ai/KpiExtractionPrompt.cs
- src/Server/Infrastructure/Persistence/Migrations/20260526145920_AddFieldNotesJsonToFundamentalRecord.cs
- src/Server/Infrastructure/Persistence/Migrations/20260526145920_AddFieldNotesJsonToFundamentalRecord.Designer.cs
- src/Server/Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs
- src/Server/Infrastructure/Persistence/SqlServer/Configurations/Fundamentals/FundamentalRecordConfiguration.cs
- src/Server/SharedApiContracts/Fundamentals/FundamentalRecordDto.cs
- src/Server/SharedApiContracts/Fundamentals/FundamentalesPublicDto.cs
- src/Server/SharedApiContracts/Fundamentals/ImportFundamentalsRequest.cs
- src/Server/SharedApiContracts/Fundamentals/KpiExtractionDto.cs
- src/Web/Main/src/modules/ficha-publica/FibraPage.tsx
- src/Web/Main/src/modules/ficha-publica/sections/FundamentalesSection.tsx
- src/Web/Main/src/modules/ficha-publica/sections/fundamentales.ts
- src/Web/Main/src/shared/lib/kpi-definitions.ts
- src/Web/Main/src/shared/ui/KpiLabel.tsx
- src/Web/Ops/src/lib/kpi-definitions.ts
- src/Web/Ops/src/modules/fundamentals/FundamentalsHistory.tsx
- src/Web/Ops/src/modules/fundamentals/FundamentalsImportForm.tsx
- src/Web/SharedApiClient/schema.d.ts
- tests/Integration/Api.Tests/Fundamentals/FundamentalsExtractKpisTests.cs
- tests/Integration/Api.Tests/Fundamentals/FundamentalsImportTests.cs
- tests/Unit/Infrastructure.Tests/Integrations/Ai/DeepSeekKpiExtractorServiceTests.cs
- tests/Unit/Infrastructure.Tests/Integrations/Ai/GeminiKpiExtractorServiceTests.cs

---

## Change Log

| Fecha | Cambio |
|-------|--------|
| 2026-05-26 | Story creada — conclusiones IA por KPI + tooltips de definición en Main y Ops |
| 2026-05-26 | Implementación completa — notas IA por KPI persistidas, tooltips/labels en Main y Ops, migración EF, contratos OpenAPI y tests verdes |

---

### Review Findings

- [x] [Review][Patch] **P2 — `GetFieldNotes()` lanza `JsonException` sin catch cuando `FieldNotesJson` está corrupto en BD** [`FundamentalRecord.cs`] — `JsonSerializer.Deserialize<Dictionary<string, string>>(FieldNotesJson)` sin try/catch. Un valor inválido en BD (edición manual, schema antiguo) revienta todos los GETs de fundamentales. Fix: envolver en try/catch y retornar `null` en `JsonException`.
- [x] [Review][Patch] **P7 — AC1: No se valida que nota IA sea no-null cuando KPI tiene valor** [`KpiExtractionJsonParser.cs`] — AC1 exige "Si KPI tiene valor → nota NO puede ser null". El parser acepta silenciosamente un KPI con nota null. Fix: tras parsear, verificar coherencia: para cada KPI numérico no-null, si la nota correspondiente es null, asignar un texto por defecto como `"Nota no disponible."` o loguear warning.
- [x] [Review][Patch] **P10 — Inconsistencia: `fieldNotes` se limpia en error de extracción pero `summary` y campos numéricos se preservan** [`FundamentalsImportForm.tsx`] — Si el usuario extrajo PDF-A con éxito (obtiene valores + notas), luego intenta PDF-B y falla: los valores y el summary quedan del PDF-A pero `fieldNotes` queda vacío. El import resultante tendría KPIs de PDF-A sin notas. Fix: en el bloque `catch` de `handleExtract`, NO limpiar `fieldNotes` (preservar estado de última extracción exitosa) — remover `setFieldNotes({})` del catch.
- [x] [Review][Defer] Pre-existing: Concurrent `/import` para mismo fibra+período crea registros duplicados [`OpsFundamentalsEndpoints.cs`] — no hay índice único para `(FibraId, Period)` en estado non-processed — deferred, pre-existente
- [x] [Review][Defer] Pre-existing: `KpiExtractionResult` tiene 15 parámetros posicionales — riesgo de transposición silenciosa en call sites [`KpiExtractionResult.cs`] — deferred, refactor mayor
