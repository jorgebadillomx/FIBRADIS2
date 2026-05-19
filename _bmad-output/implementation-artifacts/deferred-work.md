# Deferred Work

Items diferidos durante code reviews. Cada sección tiene la historia origen y la fecha.

## Deferred from: code review of 2-5-home-topmovers-tabla-y-ganadores-perdedores (2026-05-19)

- **`dailyChangePct = 0` excluido silenciosamente de GainersLosers** [`movers-logic.ts:39,44`] — El filtro `> 0` / `< 0` excluye valores exactamente cero sin indicación al usuario. Comportamiento no especificado en los AC; abordar si el negocio lo requiere.
- **Doble llamada a `numOf` en comparador de `getTopMovers`** [`movers-logic.ts:24-26`] — Micro-optimización: `numOf` se llama dos veces por elemento por comparación. Refactorizar a variable local si el corpus de snapshots crece.
- **`formatVolume`: rango [999_500, 1_000_000) muestra "1000K"** [`movers-logic.ts:15`] — Edge case de formateo: `(999_500 / 1_000).toFixed(0) = "1000"` → "1000K". Sin impacto con volúmenes actuales de FIBRAs.
- **`TopMovers` sin empty state cuando `snapshots = []`** [`TopMovers.tsx`] — Si la API devuelve array vacío y no hay error, el componente renderiza un contenedor vacío sin mensaje. Inconsistente con `GainersLosers` que sí tiene empty state.
