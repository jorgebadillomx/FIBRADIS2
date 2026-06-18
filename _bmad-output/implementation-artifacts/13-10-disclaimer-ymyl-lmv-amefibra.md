# Story 13.10: Disclaimer YMYL — fecha de revisión, LMV y AMEFIBRA

Status: ready-for-dev

## Story

As a **visitante de Fibras Inmobiliarias que consulta información financiera (dominio YMYL)**,
I want **ver un aviso legal que incluya la fecha exacta de revisión, referencia a la Ley del Mercado de Valores y un enlace a AMEFIBRA**,
so that **confío en la solidez regulatoria y actualidad de la información antes de tomar decisiones de inversión**.

## Acceptance Criteria

**AC-1 — Fecha de revisión explícita.** La sección "Aviso" en `/acerca` muestra la fecha de revisión en formato `d de mes de YYYY` (p.ej. "Revisado: 18 de junio de 2026"), reemplazando el texto genérico "Junio 2026" actual.

**AC-2 — Referencia a la Ley del Mercado de Valores.** El texto del aviso incluye explícitamente "Ley del Mercado de Valores" (o su sigla "LMV") como marco regulatorio bajo el cual operan las FIBRAs en México.

**AC-3 — Enlace a AMEFIBRA.** El aviso incluye una referencia a AMEFIBRA (Asociación Mexicana de FIBRAs Inmobiliarias) con enlace externo `https://amefibra.com` y atributos `target="_blank" rel="noopener noreferrer"`.

**AC-4 — Pie de página de fecha actualizado.** El `<p>` con `text-xs text-muted-foreground/60` al final de la página cambia de "Actualizado: Junio 2026" a "Revisado: {fecha exacta}".

**AC-5 — Build limpio.** `npm run build --workspace=src/Web/Main` pasa sin errores ni warnings de TypeScript (`noUnusedLocals: true`).

## Tasks / Subtasks

- [ ] T1 — Actualizar sección "Aviso" en `AcercaPage.tsx` (AC-1, AC-2, AC-3)
  - [ ] Sustituir el párrafo actual del aviso por texto enriquecido con fecha, LMV y enlace AMEFIBRA
  - [ ] Usar la fecha hardcodeada `18 de junio de 2026` (no dinámica — es fecha de autoría del contenido, no de deploy)
- [ ] T2 — Actualizar pie de fecha en `AcercaPage.tsx` (AC-4)
  - [ ] Cambiar `<p>Actualizado: Junio 2026</p>` a `<p>Revisado: 18 de junio de 2026</p>`
- [ ] T3 — Build y verificación (AC-5)
  - [ ] `npm run build --workspace=src/Web/Main` sin errores

## Dev Notes

### Estado actual de AcercaPage.tsx

Archivo: [src/Web/Main/src/modules/acerca/AcercaPage.tsx](src/Web/Main/src/modules/acerca/AcercaPage.tsx)

La sección "Aviso" actual (líneas 144–151):
```tsx
<section>
  <h2 className="font-semibold text-base text-foreground">Aviso</h2>
  <p className="mt-2">
    La información publicada en Fibras Inmobiliarias es de referencia y orientativa. No constituye asesoría
    de inversión, recomendación de compra o venta de valores, ni ningún servicio regulado por
    la CNBV. Consulte a un asesor financiero certificado antes de tomar decisiones de inversión.
  </p>
</section>
```

El pie de fecha actual (línea 163):
```tsx
<p className="mt-4 text-xs text-muted-foreground/60">Actualizado: Junio 2026</p>
```

### Texto de reemplazo sugerido para el aviso

El dev puede ajustar el copy siempre que mantenga los tres elementos obligatorios (AC-1, AC-2, AC-3):

```tsx
<section>
  <h2 className="font-semibold text-base text-foreground">Aviso legal</h2>
  <p className="mt-2">
    La información publicada en Fibras Inmobiliarias es de carácter informativo y orientativo.
    No constituye asesoría de inversión, recomendación de compra o venta de valores, ni ningún
    servicio regulado por la Comisión Nacional Bancaria y de Valores (CNBV). Las FIBRAs son
    instrumentos de inversión regulados por la{' '}
    <strong>Ley del Mercado de Valores (LMV)</strong> y supervisados por la CNBV. Para
    información oficial del sector, consulte{' '}
    <a
      href="https://amefibra.com"
      target="_blank"
      rel="noopener noreferrer"
      className="text-primary hover:underline"
    >
      AMEFIBRA
    </a>{' '}
    (Asociación Mexicana de FIBRAs Inmobiliarias). Revisado el{' '}
    <strong>18 de junio de 2026</strong>. Consulte a un asesor financiero certificado antes
    de tomar decisiones de inversión.
  </p>
</section>
```

Y el pie de fecha:
```tsx
<p className="mt-4 text-xs text-muted-foreground/60">Revisado: 18 de junio de 2026</p>
```

### Reglas que NO romper

- **No añadir imports nuevos** — el componente ya importa `useSiteContent` y `usePageTitle`; este cambio es solo JSX estático.
- **No extraer componente nuevo** — la historia es scope reducido (2-3h); no hay beneficio de extraer `<YmylDisclaimer>` en esta iteración.
- **Fecha hardcodeada, no dinámica** — la fecha de revisión es un dato editorial (cuándo revisamos el contenido), no la fecha de deploy. Va hardcodeada como string.
- **Tailwind v4** — usar únicamente clases que existen en v4; `text-muted-foreground/60` y `text-primary` ya están en uso en el archivo.
- **Link externo** — siempre `target="_blank" rel="noopener noreferrer"` para links a dominios externos (convención de seguridad).

### Por qué esto importa (contexto SEO/YMYL)

Fibras Inmobiliarias es clasificado por Google como dominio **YMYL** (Your Money Your Life) porque cubre decisiones financieras. Las Quality Rater Guidelines de Google exigen señales de confianza explícitas para este tipo de sitios:
- **Fecha de revisión** → señal de frescura y mantenimiento activo del contenido
- **LMV** → marco legal verificable y citeable, señal de autoridad regulatoria
- **AMEFIBRA** → fuente institucional del sector; enlace outbound a autoridad aumenta trust en YMYL

Story 12-4 ya implementó E-E-A-T base (autoría editorial, schema Organization, dateModified). Esta historia complementa ese trabajo con el disclaimer legal explícito pendiente del ACTION-PLAN.

### Security Checklist — completar antes del primer commit

- [ ] **TOCTOU doble-request**: N/A — cambio estático JSX, sin endpoints.
- [ ] **Auth-gating UI**: N/A — `/acerca` es ruta pública.
- [ ] **Denominador cero**: N/A — sin cálculos financieros.

### Archivos a modificar (UPDATE)

| Archivo | Cambio |
|---------|--------|
| [src/Web/Main/src/modules/acerca/AcercaPage.tsx](src/Web/Main/src/modules/acerca/AcercaPage.tsx) | Sección "Aviso" + pie de fecha |

Sin cambios en: backend, BD, migraciones EF, endpoints, sprint-status.

### References

- [AcercaPage.tsx — estado actual](src/Web/Main/src/modules/acerca/AcercaPage.tsx) (sección "Aviso" líneas 144–151, pie línea 163)
- [Story 12-4](\_bmad-output/implementation-artifacts/12-4-eeat-autoridad-ymyl.md) — antecedente E-E-A-T; confirmó "disclaimer YMYL claro" como punto fuerte pero sin LMV/AMEFIBRA/fecha
- [ACTION-PLAN.md punto 20](ACTION-PLAN.md) — "Incluir fecha de revisión, mención de Ley del Mercado de Valores y referencia a AMEFIBRA"
- AMEFIBRA: https://amefibra.com
- Ley del Mercado de Valores: DOF 30-12-2005, última reforma 2024

## Dev Agent Record

### Agent Model Used

_pendiente_

### Debug Log References

### Completion Notes List

### File List
