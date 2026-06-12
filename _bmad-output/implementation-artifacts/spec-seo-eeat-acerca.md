---
title: 'E-E-A-T: expandir /acerca con misión, año de fundación y metodología técnica'
type: 'feature'
created: '2026-06-12'
status: 'done'
baseline_commit: '45f8a065a4e6736aaac10e5224958898ca4d4621'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** La página `/acerca` existe pero es mínima desde el punto de vista E-E-A-T: no hay año de fundación ni declaración de misión, y la sección de metodología menciona los indicadores sin explicar cómo se calculan. Los crawlers de IA y Google usan estas señales para evaluar credibilidad; sin ellas, el sitio aparece como una caja negra de datos.

**Approach:** Expandir `AcercaPage.tsx` con (1) una sección de misión que incluya el año de fundación, (2) la metodología de cálculo de cada fundamental con sus fórmulas explícitas, y (3) una descripción conceptual del score de oportunidad. Sin exponer nombres de personas (opción B anónima).

## Boundaries & Constraints

**Always:**
- Todo el contenido es estático en el componente React (sin llamadas a API ni CMS).
- El estilo sigue los patrones de `AcercaPage.tsx` actual: `text-sm leading-7`, secciones `<section>` con `<h2>`.
- Cada fórmula se muestra en texto legible, no en notación LaTeX ni imágenes.
- La fecha de última actualización de la página se fija como literal `"Junio 2026"` en el componente.

**Ask First:**
- Si el score de oportunidad usa pesos específicos (e.g., 40% yield + 30% Cap Rate…), Jorge debe confirmarlos antes de publicarlos. Si no quiere exponerlos, se describe cualitativamente.

**Never:**
- No añadir fotos ni nombres de personas (opción A está descartada).
- No crear nuevas rutas ni componentes adicionales.
- No mover contenido al CMS; todo queda hardcodeado en el componente.

</frozen-after-approval>

## Code Map

- `src/Web/Main/src/modules/acerca/AcercaPage.tsx` — único archivo a modificar; contiene toda la página estática

## Tasks & Acceptance

**Execution:**
- [x] `src/Web/Main/src/modules/acerca/AcercaPage.tsx` -- Reestructurar y expandir el componente con las secciones descritas en Design Notes -- El contenido actual es demasiado escueto para generar confianza E-E-A-T

**Acceptance Criteria:**
- Dado que el usuario visita `/acerca`, cuando la página carga, entonces ve una declaración de misión con el año 2023.
- Dado que el usuario visita `/acerca`, cuando lee la sección Metodología, entonces encuentra la fórmula de cada indicador (Cap Rate, NAV/CBFI, NOI Margin, LTV) expresada en texto.
- Dado que el usuario visita `/acerca`, cuando lee la sección Score de Oportunidad, entonces entiende qué variables componen el ranking sin ver pesos exactos (descripción cualitativa).
- Dado que el usuario visita `/acerca`, cuando lee el pie de la sección, entonces ve "Actualizado: Junio 2026".

## Design Notes

**Estructura de secciones propuesta (en orden):**

1. **Header** — mantener el h1 + subtítulo actuales.

2. **Misión** *(nueva)*
   > "FIBRADIS nació en 2023 con un objetivo: centralizar en un solo lugar los datos dispersos de las FIBRAs mexicanas y hacerlos accesibles para el inversionista individual. Somos independientes, no recibimos compensación de ningún fideicomiso ni casa de bolsa."

3. **¿Qué es FIBRADIS?** *(existente, sin cambios sustantivos)*

4. **Fuentes de datos** *(renombrar desde "Actualización de datos", expandir)*
   - Cotizaciones BMV (tiempo real durante 8:30–15:00 h CDMX)
   - Reportes trimestrales de cada FIBRA ante la CNBV
   - Comunicados de prensa y documentos de asambleas de fideicomisarios
   - Nota de actualización: precios en tiempo real, fundamentales tras cada reporte trimestral, rankings diariamente

5. **Metodología de fundamentales** *(expandir de 2 párrafos a tabla/lista con fórmulas)*

   | Indicador | Fórmula | Fuente de datos |
   |-----------|---------|-----------------|
   | Cap Rate | NOI anualizado ÷ capitalización bursátil | Reporte trimestral + precio BMV |
   | NAV por CBFI | (Activos totales − Pasivos totales) ÷ CBFIs en circulación | Balance trimestral |
   | NOI Margin | NOI ÷ Ingresos totales | Estado de resultados trimestral |
   | LTV | Deuda financiera total ÷ Activos totales | Balance trimestral |
   | Yield TTM | Suma de distribuciones últimos 12 meses ÷ Precio actual | Historial de distribuciones + precio BMV |

6. **Score de oportunidad** *(nueva)*
   > "El score combina el yield de distribución histórico de la FIBRA, su descuento o prima respecto al NAV, el Cap Rate relativo a su sector y el momentum reciente de precio. Las FIBRAs con mayor score presentan, en conjunto, indicadores de valoración más atractivos respecto a su propio historial y a su grupo de pares."

7. **Disclaimer legal** *(existente "Metodología", segundo párrafo — mover aquí como sección propia "Aviso")*

8. **Contacto** *(existente)*

**Pie de página de la sección:**
Añadir al final, antes del cierre del `<div>` contenedor:
```tsx
<p className="mt-8 text-xs text-muted-foreground/60">Actualizado: Junio 2026</p>
```

## Verification

**Commands:**
- `cd src/Web/Main && npx tsc --noEmit` -- expected: 0 errores de tipos

**Manual checks (if no CLI):**
- Abrir `/acerca` en dev server y verificar que todas las secciones renderizan sin overflow ni layout roto en móvil (< 400px) y desktop.
- Confirmar con Jorge si quiere exponer los pesos del score antes de aprobarlo.

## Suggested Review Order

- Componente completo: expandido de ~57 a ~140 líneas con 6 nuevas secciones
  [`AcercaPage.tsx:1`](../../src/Web/Main/src/modules/acerca/AcercaPage.tsx#L1)

- Sección Misión: año de fundación 2023 + declaración de independencia
  [`AcercaPage.tsx:11`](../../src/Web/Main/src/modules/acerca/AcercaPage.tsx#L11)

- Tabla de fórmulas con `scope="col"` (patch de accesibilidad aplicado en review)
  [`AcercaPage.tsx:57`](../../src/Web/Main/src/modules/acerca/AcercaPage.tsx#L57)

- Score de oportunidad: descripción cualitativa, sin pesos exactos
  [`AcercaPage.tsx:97`](../../src/Web/Main/src/modules/acerca/AcercaPage.tsx#L97)

- Fecha hardcodeada — defer registrado en `deferred-work.md`
  [`AcercaPage.tsx:134`](../../src/Web/Main/src/modules/acerca/AcercaPage.tsx#L134)
