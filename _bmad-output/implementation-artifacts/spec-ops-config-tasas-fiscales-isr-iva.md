---
title: 'Tasas fiscales ISR e IVA en configuración de Ops'
type: 'feature'
created: '2026-06-19'
status: 'done'
baseline_commit: 'a5ec04bb661678697f5bbd88f37983713c403673'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Las tasas fiscales de retención ISR (30%) e IVA (16%) están hardcodeadas en el frontend en tres archivos distintos — con una duplicación de ISR en `PortafolioCalendario.tsx`. Un cambio de tasa exige redespliegue y puede quedar inconsistente entre archivos.

**Approach:** Agregar `IsrRetentionRate` e `IvaRate` a la entidad `OperationalConfig` existente, exponerlas en un endpoint público sin auth, y que el frontend Main las consuma vía React Query con fallback a los defaults actuales. La página ConfigPage de Ops ganará dos campos editables con auditoría.

## Boundaries & Constraints

**Always:**
- Los campos nuevos van en la tabla `OperationalConfig` existente (singleton Id=1) — no crear tabla nueva.
- Auditoría obligatoria en `ConfigAuditLog` por cada campo cambiado (misma lógica que `commission_factor`).
- Validación de rango: ISR `> 0 && ≤ 0.50`; IVA `> 0 && ≤ 0.30`.
- El endpoint público `GET /api/v1/config/fiscal-rates` no requiere autenticación.
- `calcCostoPurchase` en `simulador-logic.ts` acepta `ivaFactor` como último parámetro opcional (no-breaking para tests existentes).

**Ask First:**
- Si durante la implementación la migración seed entra en conflicto con datos ya existentes en la BD de dev.

**Never:**
- No mover lógica de cálculo fiscal al backend en esta historia.
- No romper los tests existentes de `isrCalculator.test.ts` ni `simulador-logic.test.ts`.
- No crear endpoints separados por tasa — una sola ruta retorna ambas.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| GET fiscal-rates anónimo | — | `{ isrRetentionRate: 0.30, ivaRate: 0.16 }` | — |
| PUT ops/config ISR fuera de rango | `isrRetentionRate: 0.60` | 400 Bad Request | ValidationProblem igual que commission_factor |
| PUT ops/config IVA inválido | `ivaRate: 0` | 400 Bad Request | ValidationProblem |
| Fetch de tasas falla en frontend | API caída | Usa defaults 0.30 / 0.16 sin lanzar error visible | Catch retorna constantes |
| Admin cambia ISR a 0.32 | PUT ops/config | BD actualiza + entrada en ConfigAuditLog fieldName="isr_retention_rate" | — |
| PortafolioCalendario con ISR 0.32 en API | API retorna 0.32 | Cálculo usa 0.32, no la constante local | — |

</frozen-after-approval>

## Code Map

- `src/Server/Domain/Ops/OperationalConfig.cs` — entidad singleton; agregar dos propiedades
- `src/Server/Infrastructure/Persistence/SqlServer/Configurations/Ops/OperationalConfigConfiguration.cs` — column mappings y seed
- `src/Server/Application/Ops/IOperationalConfigRepository.cs` — firma de UpdateAsync
- `src/Server/Infrastructure/Persistence/Repositories/Ops/OperationalConfigRepository.cs` — audit de los dos campos nuevos
- `src/Server/SharedApiContracts/Ops/OperationalConfigDto.cs` — dos propiedades nuevas
- `src/Server/SharedApiContracts/Ops/UpdateOperationalConfigRequest.cs` — dos propiedades opcionales nuevas
- `src/Server/Api/Endpoints/Ops/OpsConfigEndpoints.cs` — validaciones + pasar al repositorio
- `src/Server/Api/Endpoints/Public/FiscalRatesEndpoint.cs` (nuevo) — GET /api/v1/config/fiscal-rates sin auth
- `src/Server/Api/Program.cs` — registrar nuevo endpoint
- `scripts/codegen/Api.json` + `src/Web/SharedApiClient/schema.d.ts` — regenerar tras codegen
- `src/Web/Ops/src/pages/ConfigPage.tsx` — dos campos nuevos con edición parcial
- `src/Web/Main/src/api/fiscalRatesApi.ts` (nuevo) — fetch público con fallback
- `src/Web/Main/src/modules/herramientas/isrCalculator.ts` — ISR_RATE se convierte en DEFAULT_ISR_RATE; funciones reciben isrRate como param con default
- `src/Web/Main/src/modules/herramientas/IsrCalculatorWidget.tsx` — consumir tasa desde fiscalRatesApi
- `src/Web/Main/src/modules/portafolio/PortafolioCalendario.tsx` — eliminar const local ISR_RATE; usar fiscalRatesApi
- `src/Web/Main/src/modules/oportunidades/simulador-logic.ts` — calcCostoPurchase acepta ivaFactor opcional
- `src/Web/Main/src/modules/oportunidades/PromediarTab.tsx` — obtener ivaRate de fiscalRatesApi y pasarlo a calcCostoPurchase
- `src/Web/Main/src/modules/herramientas/isrCalculator.test.ts` — actualizar firma si cambió

## Tasks & Acceptance

**Execution:**

- [ ] `src/Server/Domain/Ops/OperationalConfig.cs` -- agregar `public decimal IsrRetentionRate { get; set; } = 0.30m;` y `public decimal IvaRate { get; set; } = 0.16m;`

- [ ] `src/Server/Infrastructure/Persistence/SqlServer/Configurations/Ops/OperationalConfigConfiguration.cs` -- mapear columnas `isr_retention_rate` y `iva_rate` (precision 5,4); agregar al seed existente (Id=1) con values 0.30m y 0.16m

- [ ] `src/Server/Application/Ops/IOperationalConfigRepository.cs` -- agregar `decimal? isrRetentionRate` y `decimal? ivaRate` a firma de `UpdateAsync`

- [ ] `src/Server/Infrastructure/Persistence/Repositories/Ops/OperationalConfigRepository.cs` -- añadir bloques de audit para `isr_retention_rate` e `iva_rate` siguiendo el patrón idéntico al de `commission_factor`

- [ ] `src/Server/SharedApiContracts/Ops/OperationalConfigDto.cs` -- agregar `decimal IsrRetentionRate` y `decimal IvaRate`

- [ ] `src/Server/SharedApiContracts/Ops/UpdateOperationalConfigRequest.cs` -- agregar `decimal? IsrRetentionRate` y `decimal? IvaRate`

- [ ] `src/Server/Api/Endpoints/Ops/OpsConfigEndpoints.cs` -- en PUT: validar `IsrRetentionRate` con `> 0 && <= 0.50m` e `IvaRate` con `> 0 && <= 0.30m`; pasar ambos a `UpdateAsync`

- [ ] `src/Server/Api/Endpoints/Public/FiscalRatesEndpoint.cs` (nuevo) -- endpoint minimal: `GET /api/v1/config/fiscal-rates` sin auth; inyecta `IOperationalConfigRepository`; retorna `{ isrRetentionRate, ivaRate }` como registro anónimo o record; registrar con `app.MapFiscalRates()` en Program.cs

- [ ] EF Core migration -- `dotnet ef migrations add AddFiscalRatesConfig --project src/Server/Infrastructure --startup-project src/Server/Api`; verificar columnas y seed en la migración generada

- [ ] `npm run codegen:api` -- regenerar schema.d.ts y Api.json

- [ ] `src/Web/Ops/src/pages/ConfigPage.tsx` -- agregar campos `isr_retention_rate` (label "Retención ISR %", input number step=0.01, mostrar como porcentaje) e `iva_rate` (label "IVA %", step=0.01); seguir el mismo patrón de `editedFields` ya implementado; los nuevos campos aparecen en la tabla de auditoría automáticamente

- [ ] `src/Web/Main/src/api/fiscalRatesApi.ts` (nuevo) -- `export async function fetchFiscalRates(): Promise<{ isrRetentionRate: number; ivaRate: number }>` — fetch a `/api/v1/config/fiscal-rates`, en catch retornar `{ isrRetentionRate: 0.30, ivaRate: 0.16 }`

- [ ] `src/Web/Main/src/modules/herramientas/isrCalculator.ts` -- renombrar `ISR_RATE` a `DEFAULT_ISR_RATE`; actualizar funciones internas para que acepten `isrRate = DEFAULT_ISR_RATE` como param con default (no-breaking)

- [ ] `src/Web/Main/src/modules/herramientas/IsrCalculatorWidget.tsx` -- agregar `useQuery({ queryKey: ['fiscal-rates'], queryFn: fetchFiscalRates })`; pasar `isrRate` a las funciones de cálculo; mostrar tasa real en el label en lugar de string hardcodeado "30%"

- [ ] `src/Web/Main/src/modules/portafolio/PortafolioCalendario.tsx` -- eliminar `const ISR_RATE = 0.30` local; agregar `useQuery` de `fetchFiscalRates`; usar `data?.isrRetentionRate ?? 0.30` en los cálculos

- [ ] `src/Web/Main/src/modules/oportunidades/simulador-logic.ts` -- agregar `ivaFactor = IVA_FACTOR` como último param de `calcCostoPurchase`; mantener `export const IVA_FACTOR = 0.16` como constante de fallback

- [ ] `src/Web/Main/src/modules/oportunidades/PromediarTab.tsx` -- agregar `useQuery` de `fetchFiscalRates`; pasar `ivaRate ?? 0.16` a `calcCostoPurchase`

- [ ] `src/Web/Main/src/modules/herramientas/isrCalculator.test.ts` -- actualizar llamadas si la firma de `calcIsr` cambió; verificar que los tests siguen pasando con los defaults

**Acceptance Criteria:**

- Given `GET /api/v1/config/fiscal-rates` sin token, when ejecuta, then 200 OK con `isrRetentionRate: 0.30` e `ivaRate: 0.16`
- Given AdminOps en `GET /api/v1/ops/config`, when responde, then incluye `isrRetentionRate` e `ivaRate`
- Given AdminOps envía `isrRetentionRate: 0.60` en PUT, when valida, then 400 Bad Request con detalle de rango
- Given AdminOps cambia ISR a 0.32 desde ConfigPage, when guarda, then la tabla de auditoría muestra `isr_retention_rate` con valor anterior y nuevo
- Given la API de tasas retorna `isrRetentionRate: 0.32`, when PortafolioCalendario calcula ISR de distribuciones, then usa 0.32 y no la constante local
- Given la API de tasas falla, when cualquier componente del Main la consume, then los cálculos usan 0.30 / 0.16 sin error visible en UI
- Given `dotnet build` y `npm run build` en Main y Ops, when ejecutan, then 0 errores ni warnings de TypeScript

## Spec Change Log

## Design Notes

`FiscalRatesEndpoint.cs` reutiliza `IOperationalConfigRepository` (singleton en BD) — la query es O(1) por la clave primaria fija Id=1. Sin caché especial en servidor; React Query en el cliente maneja TTL (`staleTime: 1000 * 60 * 10` recomendado para tasas que cambian raramente).

Firma no-breaking de `calcCostoPurchase`:
```typescript
export function calcCostoPurchase(
  precio: number,
  cantidad: number,
  commissionFactor: number,
  ivaFactor = IVA_FACTOR   // default mantiene tests existentes en verde
): number
```

## Verification

**Commands:**
- `dotnet build FIBRADIS.slnx` -- expected: Build succeeded, 0 error(s)
- `dotnet test tests/Unit/Infrastructure.Tests/Infrastructure.Tests.csproj -m:1` -- expected: verde
- `dotnet test tests/Integration/Api.Tests/Api.Tests.csproj --filter OpsConfigEndpoint -m:1` -- expected: verde
- `npm run build --workspace=src/Web/Main` -- expected: sin errores TypeScript
- `npm run build --workspace=src/Web/Ops` -- expected: sin errores TypeScript
- `npx vitest run src/Web/Main/src/modules/herramientas/isrCalculator.test.ts` -- expected: todos pasan
- `npx vitest run src/Web/Main/src/modules/oportunidades/simulador-logic.test.ts` -- expected: todos pasan

## Suggested Review Order

**Modelo de dominio y migración**

- Dos propiedades nuevas en el singleton; defaults declarativos en C#
  [`OperationalConfig.cs:1`](../../src/Server/Domain/Ops/OperationalConfig.cs#L1)

- Column mappings y seed; precision(5,4) para decimales de tasa
  [`OperationalConfigConfiguration.cs:1`](../../src/Server/Infrastructure/Persistence/SqlServer/Configurations/Ops/OperationalConfigConfiguration.cs#L1)

- Migración generada: AddColumn con defaultValue:0m + UpdateData para el seed row
  [`20260619061714_AddFiscalRatesConfig.cs:1`](../../src/Server/Infrastructure/Migrations/SqlServer/20260619061714_AddFiscalRatesConfig.cs#L1)

**Endpoint público y validación**

- Endpoint anónimo GET + validación de rango ISR/IVA en PUT; entrada al cambio de backend
  [`OpsConfigEndpoints.cs:1`](../../src/Server/Api/Endpoints/Ops/OpsConfigEndpoints.cs#L1)

- DTO de respuesta para el endpoint público
  [`FiscalRatesDto.cs:1`](../../src/Server/SharedApiContracts/Ops/FiscalRatesDto.cs#L1)

- Contrato de UpdateAsync extendido con dos params opcionales
  [`IOperationalConfigRepository.cs:1`](../../src/Server/Application/Ops/IOperationalConfigRepository.cs#L1)

**Auditoría en repositorio**

- Bloques de audit para isr_retention_rate e iva_rate; patrón idéntico a commission_factor
  [`OperationalConfigRepository.cs:1`](../../src/Server/Infrastructure/Persistence/Repositories/Ops/OperationalConfigRepository.cs#L1)

**Frontend — capa API y fallback**

- fetch con fallback y validación defensiva contra valores ≤0 retornados por la API
  [`fiscalRatesApi.ts:1`](../../src/Web/Main/src/api/fiscalRatesApi.ts#L1)

**Frontend — consumidores React**

- ISR_RATE → DEFAULT_ISR_RATE; calcIsr acepta isrRate opcional (no-breaking)
  [`isrCalculator.ts:1`](../../src/Web/Main/src/modules/herramientas/isrCalculator.ts#L1)

- Widget que muestra tasa real vía React Query; label dinámico
  [`IsrCalculatorWidget.tsx:1`](../../src/Web/Main/src/modules/ficha-publica/IsrCalculatorWidget.tsx#L1)

- Calendario de distribuciones: elimina ISR_RATE local, usa import + query
  [`PortafolioCalendario.tsx:1`](../../src/Web/Main/src/modules/portafolio/PortafolioCalendario.tsx#L1)

- PromediarTab: ivaFactor dinámico pasa a calcCostoPurchase como 4to arg
  [`PromediarTab.tsx:1`](../../src/Web/Main/src/modules/oportunidades/PromediarTab.tsx#L1)

**Ops ConfigPage**

- Dos campos nuevos con placeholder de formato y validación de rango
  [`ConfigPage.tsx:286`](../../src/Web/Ops/src/pages/ConfigPage.tsx#L286)

**Contratos y tipos de soporte**

- DTO público extendido con isrRetentionRate e ivaRate
  [`OperationalConfigDto.cs:1`](../../src/Server/SharedApiContracts/Ops/OperationalConfigDto.cs#L1)

- Request de actualización extendido con campos opcionales
  [`UpdateOperationalConfigRequest.cs:1`](../../src/Server/SharedApiContracts/Ops/UpdateOperationalConfigRequest.cs#L1)

- Schema generado por codegen; verificar FiscalRatesDto presente
  [`schema.d.ts:1`](../../src/Web/SharedApiClient/schema.d.ts#L1)
