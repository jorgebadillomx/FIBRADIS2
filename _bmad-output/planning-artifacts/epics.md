---
stepsCompleted:
  - step-01-validate-prerequisites
  - step-02-design-epics
  - step-03-create-stories
  - step-04-final-validation
inputDocuments:
  - docs/req/prd.md
  - docs/req/architecture.md
project_name: FIBRADIS
---

# FIBRADIS - Desglose de Épicas

## Visión General

Este documento presenta el desglose completo de épicas e historias para FIBRADIS, descomponiendo los requerimientos del PRD y la Arquitectura en historias implementables.

## Inventario de Requerimientos

### Requerimientos Funcionales

FR-01: El sistema debe mantener un catalogo maestro de FIBRAs con ticker unico, nombre completo, nombre corto, mercado, moneda base, sector, pais, estado y configuraciones por emisor. Trace: UJ-01, UJ-02, SC-02.

FR-02: Cada FIBRA debe conservar URLs oficiales de sitio, inversionistas y reportes para alimentar discovery de PDFs y asociacion de noticias. Trace: UJ-02, UJ-08.

FR-03: La Home publica debe mostrar encabezado con busqueda global, carrusel de precios, resumen general del mercado, top movers, ranking rapido y ultimas noticias. Trace: UJ-01, SC-01.

FR-04: El buscador global debe autocompletar por ticker o nombre y navegar a la ficha publica correspondiente; si no existe coincidencia, mostrar estado claro de no encontrado. Trace: UJ-01, SC-02.

FR-05: La Home debe mostrar datos cacheados o stale con timestamp visible cuando mercado no este fresco, y escalar a degradacion critica cuando la antiguedad supere el umbral documentado. Trace: UJ-01, SC-03, SC-11.

FR-06: La ficha publica de FIBRA debe consolidar: encabezado con precio actual, cambio porcentual diario y volumen; grafica historica con selectores 1M/3M/6M/1A; bloque de fundamentales vigentes del ultimo periodo; las ultimas 8 distribuciones; las ultimas 10 noticias asociadas; y listado de reportes oficiales. No incluye badge de senal en MVP. Trace: UJ-02.

FR-07: La ficha publica debe mostrar el periodo de origen junto a cada metrica fundamental y una advertencia visible cuando el ultimo reporte tenga mas de dos periodos de antiguedad. Trace: UJ-02, UJ-08.

FR-08: (Growth) El comparador publico en `/comparar` debe permitir seleccionar entre 2 y 4 FIBRAs mediante autocomplete y compararlas en cuatro bloques: Mercado, Fundamentales, Distribuciones y Score publico. La seleccion debe reflejarse en query params de URL. Trace: UJ-03.

FR-09: (Growth) El comparador debe tolerar valores faltantes por celda mostrando `—` sin desplazar columnas. Funciona sin autenticacion. Trace: UJ-03, SC-11, PT-05.

FR-10: El modulo de Mercado debe exponer Last Price, cambio diario, volumen, promedio 52S, comparacion vs 52S, historico y distribuciones por FIBRA. Trace: UJ-01, UJ-02, SC-03.

FR-11: El sistema debe recalcular yield anualizado usando frecuencia detectada de distribucion, sin asumir frecuencia fija. Trace: UJ-02, UJ-06.

FR-12: Si faltan eventos de distribucion o el proveedor no entrega datos, mostrar `yield` como no disponible sin romper score ni senales. Trace: UJ-02, UJ-05, UJ-06, SC-11.

FR-13: El modulo de Noticias debe ingerir notas cada hora desde Google News RSS usando queries especificas por FIBRA y queries generales de mercado, ambas configurables desde catalogo y Ops sin redeploy. Cada noticia se normaliza con titulo, fecha, fuente, URL, snippet y estado de procesamiento. Trace: UJ-01, UJ-02, SC-07.

FR-14: El sistema debe aplicar un blocklist global configurable desde Ops para descartar noticias no relacionadas con FIBRAs inmobiliarias, y eliminar duplicados exactos por URL y duplicados probables por similitud de titulo en ventana de 24h. Trace: UJ-01, SC-07.

FR-15: Cada noticia debe poder asociarse con cero, una o varias FIBRAs mediante coincidencia automatica de ticker o variante de nombre en titulo y snippet, sin intervencion de IA. Trace: UJ-01, UJ-02, SC-07, DR-07.

FR-16: Cuando AI_MODE=Off, publicar noticias sin resumen usando titulo, fuente, fecha, snippet y enlace original. El MVP arranca con AI_MODE=Off. Trace: UJ-01, SC-07.

FR-17: Cuando AI_MODE=Manual, el sistema debe permitir disparar generacion de resumen ejecutivo y, si falla, publicar la noticia con estado `parcial`. Trace: UJ-01, UJ-09, SC-07.

FR-18: El modulo de Fundamentales debe soportar tres modos: Off (almacena PDFs sin procesar), Manual (operador dispara skill externo que llama al endpoint de importacion), Api (pipeline detecta y procesa PDFs automaticamente). El cambio entre modos no requiere redeploy. Trace: UJ-08, SC-06.

FR-19: En modo Manual el sistema debe exponer un endpoint de importacion de fundamentales que recibe un payload JSON con FIBRA, periodo, campos fundamentales estructurados, resumen y referencia al PDF. El operador revisa y confirma desde Ops antes de que sea visible en M5. El contrato del endpoint queda documentado en la especificacion de arquitectura. Trace: UJ-08, UJ-09.

FR-20: (Growth) En modo Api el sistema debe detectar PDFs nuevos mediante reglas config-driven por FIBRA, descargarlos, llamar al proveedor de IA configurado y actualizar el historico por periodo. Trace: UJ-08, SC-06.

FR-21: El sistema debe tratar un PDF como nuevo solo si representa un periodo no registrado para esa FIBRA. Si el periodo ya existe, registrarlo como posible actualizacion y dejar el reproceso bajo control manual de AdminOps. Trace: UJ-08, UJ-09.

FR-22: El modulo de Portafolio debe permitir cargar archivos .xlsx, .xls o .csv con formato fijo de tres columnas: Ticker, Qty y AvgCost (case-insensitive). No se soportan formatos alternativos. Trace: UJ-04, SC-04.

FR-22b: El sistema debe permitir que metricas fundamentales faltantes queden en null sin bloquear el procesamiento ni el display de metricas disponibles. Trace: UJ-08, SC-11.

FR-22c: El modulo de Fundamentales debe almacenar por cada registro: FIBRA, periodo trimestral, fecha de procesamiento, modo, referencia al PDF, estado (pendiente/procesado/parcial/error), campos estructurados y resumen si disponible. Trace: UJ-08, DR-03, NFR-10.

FR-23: El unico input de costo aceptado es AvgCost (costo promedio por CBFI). El sistema calcula CostoTotalCompra = Qty x AvgCost x (1 + factor_comision). Si el mismo ticker aparece en multiples filas, se consolida sumando cantidades y calculando costo promedio ponderado. Trace: UJ-04, SC-04.

FR-24: El sistema debe procesar la carga de forma sincrona y devolver resultado en la misma operacion. Si hay errores, no se guarda ninguna posicion y se muestra tabla de errores por fila. Reglas: ticker debe existir en catalogo; Qty entero positivo; AvgCost numero positivo; header con exactamente tres columnas. Trace: UJ-04.

FR-25: Cuando el usuario sube un archivo nuevo y ya tiene portafolio activo, mostrar confirmacion antes de reemplazar. El usuario puede editar posiciones individuales inline (Qty y AvgCost). Al confirmar se recalcula y persiste inmediatamente. Escape cancela. La eliminacion de posicion requiere confirmacion. Trace: UJ-04.

FR-26: La pantalla unificada de portafolio debe mostrar en la parte superior los KPIs agregados: Inversion Total, Valor Total, Plusvalia Total (%), Ganancia Total ($), Rentas Anuales Brutas estimadas, Rentas Reales Brutas y % Rentas del Portafolio. Trace: UJ-05, SC-05.

FR-27: Cada fila de la tabla de portafolio debe ser expandible mostrando cuatro secciones: Mi posicion, Mercado, Fundamentales y Distribuciones. Trace: UJ-05.

FR-28: Cada posicion debe mostrar un badge de senal calculado con base en NAV vs Precio de Mercado: verde >10% descuento, amarillo ±10%, rojo >10% premium, gris sin dato NAV. El badge incluye tooltip con la explicacion textual. Trace: UJ-05.

FR-29: El modulo de Oportunidades debe presentar dos vistas: universo completo con ranking, y vista Promediar Posicion restringida a FIBRAs del portafolio del usuario. Trace: UJ-06, SC-08.

FR-30: El score de oportunidad se compone de cinco componentes con pesos configurables: Descuento NAV (30%), Dividend Yield (30%), LTV invertido (20%), Margen NOI (10%), Precio vs AVG 52S (10%). Normalizacion por percentil. Si falta un componente, su peso se redistribuye. Para aparecer en el ranking principal una FIBRA necesita al menos 3/5 componentes; las demas van a seccion "datos limitados" con advertencia. Si no puede calcular ningun componente con precio, se excluye. Trace: UJ-06, SC-08.

FR-31: El sistema debe ofrecer tres perfiles preconfigurados: Renta, Valor y Conservador. El usuario puede ajustar pesos libremente. La configuracion activa se persiste por usuario. El ranking se recalcula en tiempo real al cambiar pesos. Trace: UJ-06, SC-08.

FR-32: El sistema debe permitir al usuario marcar cualquier FIBRA como favorita desde M5, M8 y M9 mediante un icono de estrella. La preferencia se persiste por usuario. Trace: UJ-07, SC-09.

FR-33: Las FIBRAs marcadas como favoritas deben aparecer destacadas y agrupadas al inicio de las tablas en M8 y M9. No requieren tratamiento especial en M5. Trace: UJ-07.

FR-35: El Centro de Procesos interno se organiza en cinco secciones: Dashboard (estado global de pipelines y errores recientes), Pipelines (detalle y control de mercado y noticias), Fundamentales (importacion JSON e historial por FIBRA), Catalogo (gestion de FIBRAs) y Configuracion (parametros operativos del sistema). Trace: UJ-09, SC-10.

FR-36: El Dashboard operativo debe mostrar para cada pipeline: estado actual, timestamp y duracion de la ultima corrida, conteo de items procesados y errores, y los ultimos 5 errores globales. Trace: UJ-09, SC-10.

FR-37: La seccion Pipelines debe mostrar historial de las ultimas corridas con detalle por pipeline. Cada pipeline tiene boton Run now con auditoria del disparador manual. Trace: UJ-09, SC-10.

FR-38: La seccion Fundamentales debe incluir formulario de importacion que acepta payload JSON con campos: fibra_id, period, cap_rate, nav_per_cbfi, ltv, noi_margin, ffo_margin, quarterly_distribution y summary. El sistema valida, muestra preview y el operador confirma antes de persistir. El historial permite Reprocess por registro. Trace: UJ-08, UJ-09, FR-19.

FR-39: La seccion Catalogo debe permitir agregar, editar y desactivar FIBRAs. Campos editables: ticker, nombre completo, nombre corto, sector, mercado, moneda, estado y variantes de nombre para queries de Google News RSS. La desactivacion es soft delete. Trace: UJ-09, FR-49.

FR-40: La seccion Configuracion debe permitir editar sin redeploy: commission_factor, avg_periods, blocklist de terminos de noticias, AI_MODE (Off o Manual), y la cadencia de ejecucion de los pipelines de mercado y noticias. Cada cambio queda auditado con actor y timestamp. Trace: UJ-09, PT-07, DR-13, DR-15.

FR-41: El sistema debe mantener estados operativos por item y corrida: detected, pending, processing, processed, partial y error. Estos estados son visibles en Fundamentales y en el historial de pipelines. Trace: UJ-08, UJ-09.

FR-42: El mundo publico debe permanecer sin autenticacion; el mundo privado debe requerir autenticacion; las rutas y acciones de Ops deben restringirse a AdminOps. Trace: UJ-04, UJ-05, UJ-07, UJ-09, SC-10.

FR-43: El sistema debe calcular el porcentaje de portafolio de cada posicion usando el monto invertido como base: (Titulos x Costo_promedio) / Suma(Titulos_i x Costo_promedio_i). Trace: DR-12, UJ-05.

FR-44: El sistema debe aplicar un factor de comision de intermediacion configurable al calcular el Costo Total Compra. El factor debe ser editable desde el Centro de Procesos sin redeploy. Trace: DR-13, UJ-04, SC-04.

FR-45: El sistema debe calcular todos los promedios historicos de metricas fundamentales y de distribuciones usando los ultimos 4 periodos disponibles por FIBRA (N configurable desde Ops). Trace: DR-11, UJ-05.

FR-46: El portafolio debe mostrar exclusivamente posiciones con al menos un titulo activo. Trace: UJ-04, UJ-05.

FR-47: El portafolio y el dashboard privado se presentan en una sola pantalla unificada bajo la ruta `/portafolio`. No existe ruta `/dashboard` separada. Trace: UJ-04, UJ-05.

FR-48: La tabla del portafolio debe mostrar vista compacta con columnas default: FIBRA, Titulos, Costo promedio, Precio actual, Valor de mercado, Plusvalia (%), Ganancia ($), Renta anual y % Portafolio. El usuario puede configurar columnas adicionales agrupadas por seccion. La tabla soporta multi-sort. El orden y la configuracion de columnas se persisten por usuario. Trace: UJ-05.

FR-49: El catalogo maestro de cada FIBRA debe almacenar un conjunto de variantes de nombre y terminos de busqueda utilizados para las queries de Google News RSS, editables desde el Centro de Procesos sin redeploy. Trace: FR-13, UJ-01.

FR-50: La Home publica debe mostrar las 10 noticias mas recientes en orden de fecha de publicacion, sin importar si estan asociadas a una FIBRA especifica o son de mercado general. Trace: UJ-01, FR-03.

FR-51: La vista del universo de Oportunidades debe mostrar para cada FIBRA: posicion, nombre/ticker, score total (0-100), valores de cada componente y badge de datos disponibles. Al expandir se muestra el desglose visual del score. Filtros disponibles: solo FIBRAs con fundamentales, yield minimo, LTV maximo, sector y solo FIBRAs con precio activo. Trace: UJ-06.

FR-52: La vista Promediar Posicion debe mostrar para cada FIBRA del portafolio del usuario: costo promedio de entrada, precio actual, diferencia porcentual y score. Debe incluir un simulador que calcule el nuevo costo promedio ponderado al agregar titulos. El simulador no emite recomendaciones. Trace: UJ-06.

FR-53: La edicion de posiciones debe ser inline sobre la tabla. Solo Qty y AvgCost son editables. Al confirmar con Enter o click fuera, el sistema guarda y recalcula la fila inmediatamente. Escape cancela. La eliminacion requiere confirmacion explicita. Trace: UJ-04, UJ-05.

FR-54: El modulo de Oportunidades debe monitorear la cobertura de precios del universo activo. Si el porcentaje de FIBRAs sin precio supera el 30% (configurable desde Ops), mostrar advertencia de "universo degradado". Si la cobertura cae por debajo del 50%, suspender el ranking y mostrar estado "ranking no disponible por cobertura insuficiente". Trace: UJ-06, SC-08, SC-11, DR-15.

### Requerimientos No Funcionales

NFR-01: La Home publica debe responder en menos de 2 segundos en P95 usando datos cacheados o precargados, medido con telemetria de frontend y backend en ambiente productivo. Trace: SC-01.

NFR-02: El Dashboard privado debe responder en menos de 1 segundo en P95 con datos precalculados, medido con telemetria de backend y tiempos de render del cliente. Trace: SC-05.

NFR-03: El pipeline de mercado debe ejecutarse cada 15 minutos dentro del horario BMV (8:15am-3:15pm CDMX, dias habiles). Si el proveedor externo no entrega datos validos durante dos o mas ciclos consecutivos, el sistema debe clasificar el estado de precio como `critico` para las FIBRAs afectadas conforme a NFR-04 y continuar operando con los ultimos datos conocidos. Trace: SC-03, DR-15.

NFR-04: Clasificar frescura de mercado automaticamente: Fresh hasta 20 minutos; Stale mayor a 20 minutos y hasta 6 horas; degradacion critica mayor a 6 horas. Fuera del horario de mercado, mostrar estado `fuera-de-horario` cuando el timestamp corresponde al ultimo precio de cierre valido. Trace: SC-03, SC-11, DR-15.

NFR-05: El pipeline de noticias debe ejecutarse con cadencia default de 1 hora; cualquier cambio desde Ops debe surtir efecto en la siguiente evaluacion programada sin redeploy. En MVP el procesamiento de PDFs de Fundamentales es manual y no tiene schedule automatico. Trace: SC-06, SC-10.

NFR-06: Los snapshots diarios de mercado deben conservarse por 90 dias calendario y seguir consultables durante toda esa ventana. Trace: SC-03.

NFR-07: Los PDFs no deben eliminarse automaticamente en MVP; la politica de retencion a largo plazo debe ser definida explicitamente por el operador antes de poner el sistema en produccion. Trace: SC-06.

NFR-08: Toda vista debe tolerar datos faltantes mostrando `—`, `parcial`, `sin datos`, `no evaluable` o advertencias equivalentes sin producir errores fatales de UI. Trace: SC-11.

NFR-09: El sistema debe soportar al menos 30 FIBRAs activas y 5+ anos de historico relevante sin redisenar entidades base. Trace: Product Scope Vision.

NFR-10: Toda entidad relevante de datos debe conservar `fuente`, `captured_at`, `status` y `error_reason` cuando aplique. Trace: UJ-08, UJ-09.

NFR-11: El sistema debe proteger mundo privado y ops con autenticacion y autorizacion por roles User y AdminOps, validado con pruebas positivas y negativas. Trace: SC-10.

NFR-12: Los cambios de schedule, AI_MODE, reprocesos, retries manuales y cambios de configuracion PDF por FIBRA deben quedar auditados con actor, fecha y antes/despues, verificado mediante registro consultable del 100% de esos eventos. Trace: SC-10.

NFR-13: El sistema debe ofrecer observabilidad minima con logs estructurados para el 100% de corridas de pipeline, correlation ID por solicitud o job, y health checks separados de API, persistencia y pipelines. Trace: UJ-09.

NFR-14: La API debe mantener un contrato documentado y versionado suficiente para que frontend consuma endpoints sin ambiguedad y detecte cambios incompatibles antes de liberar. Trace: PT-06.

NFR-15: Las interfaces publicas, privadas y operativas deben mantener navegacion principal, accion primaria visible y ausencia de overflow horizontal no intencional en 360px, 768px y 1280px, validadas manualmente antes de liberacion de MVP. Trace: PT-05.

NFR-16: La plataforma debe operar bajo un despliegue unico que atienda mundo publico, mundo privado y procesamiento en background dentro del entorno objetivo de hosting compartido, manteniendo idempotencia, exclusion logica de ejecuciones concurrentes y estados persistentes de pipeline. Trace: PT-02, PT-03.

### Requerimientos Adicionales

**Plantilla Inicial (Arquitectura — Épica 1, Historia 1):**
- Inicializar la solución usando el baseline oficial:
  ```
  dotnet new sln -n FIBRADIS
  dotnet new webapi -n Api -o src/Server/Api
  npm create vite@latest src/Web/Main -- --template react-ts
  npm create vite@latest src/Web/Ops -- --template react-ts
  cd src/Web/Main && npx shadcn@latest init
  cd src/Web/Ops && npx shadcn@latest init
  ```
- Esta debe ser la primera historia de implementación.

**Runtime y Stack:**
- .NET 10 LTS + EF Core 10 + PostgreSQL 16
- React 19.2 + Vite 7 + Node.js 20.19+
- TanStack Query v5 + React Router 7 + React Hook Form + Zod
- shadcn sobre configuración Vite compatible con Tailwind

**Arquitectura de Datos:**
- Base de datos única PostgreSQL 16 con propiedad por esquema-módulo: `catalog`, `market`, `news`, `fundamentals`, `portfolio`, `ai`, `jobs`
- Favoritos almacenados en esquema `portfolio`; no existe esquema `alerts`
- Migraciones EF Core code-first, un stream para el monolito desplegable
- IMemoryCache + output caching de ASP.NET (sin Redis en MVP)
- Estados de datos explícitos: fresh, stale, partial, error, null-equivalent en dominio y modelos de lectura

**Autenticación y Seguridad:**
- Tokens de acceso JWT bearer + refresh tokens almacenados con hash en servidor con rotación y revocación
- Políticas basadas en roles: User y AdminOps
- El endpoint de refresh usa transporte de cookie HttpOnly para el refresh token
- HTTPS requerido en entornos no locales
- Secretos y cadenas de conexión fuera del control de fuente

**Diseño de API:**
- REST JSON, base de ruta `/api/v1`
- OpenAPI generado desde el backend como fuente de verdad
- SharedApiClient tipado generado para los SPAs Main y Ops
- Contrato de error ProblemDetails con domainCode + correlationId
- Respuestas de colección: `{ items, page, pageSize, total }`
- Comandos operativos async retornan `202 Accepted` con payload de seguimiento
- Sin HTTP interno entre módulos; comunicación a través de contratos de capa Application

**Arquitectura Frontend:**
- SPA Main servida desde `/`, SPA Ops servida desde `/ops`
- Las rutas públicas requieren prerender o equivalente SSR para SEO (title, meta description, canonical)
- Producto privado y Ops permanecen en CSR para MVP
- Estructura por carpeta de funcionalidad: `src/modules/*`, `src/shared/*`
- Estado del servidor bajo TanStack Query; estado de UI navegable en params de URL; store de cliente solo para preocupaciones de shell/sesión

**Infraestructura y Despliegue:**
- Host único ASP.NET Core: APIs backend + servidor Hangfire + servicio estático para Main y Ops
- Hangfire in-app con almacenamiento SQL persistente, todos los jobs restart-safe y protegidos contra solapamiento
- Configuración operativa respaldada en BD para parámetros editables en runtime
- Logs estructurados + correlation IDs + health checks (API, BD, frescura de pipelines)
- CI/CD: build backend + ambos frontends + ejecutar pruebas + generar cliente OpenAPI + gate de migraciones BD + validar artefactos de prerender de rutas públicas

**Pruebas:**
- Pruebas unitarias (Domain.Tests, Application.Tests, Infrastructure.Tests)
- Pruebas de integración (Api.Tests, Persistence.Tests, Jobs.Tests, Integrations.Tests)
- Pruebas de contrato (ApiCompatibility.Tests) para detección de drift de OpenAPI
- Pruebas E2E (Playwright para Main y Ops)

**Límites de Módulos:**
- Ningún módulo puede acceder directamente al repositorio o capa de persistencia de otro módulo
- Las proyecciones de lectura cruzadas entre módulos deben tener módulo propietario explícito
- El módulo Portfolio es el único propietario de las posiciones de usuarios; Dashboard y Oportunidades consumen vía contratos Application

**Configuración Operativa del Portafolio (persistida en BD):**
- `portfolio.avg_periods` = 4 (número de períodos históricos para métricas AVG; configurable desde Ops)
- `portfolio.commission_factor` = documentado en la configuración inicial del sistema (configurable desde Ops sin redespliegue)
- Un cambio en commission_factor NO retroactúa los valores `costo_total_compra` existentes; se aplica solo a cálculos de lectura futuros

**Convenciones de Nomenclatura:**
- BD: esquemas en minúsculas singular, tablas en PascalCase singular, columnas en snake_case
- API: segmentos de ruta de recursos en minúsculas, colecciones en plural, JSON y query params en camelCase
- C#: tipos/miembros públicos en PascalCase, campos privados en _camelCase
- TS: componentes en PascalCase, hooks `useThing.ts`, utils en `kebab-case.ts`

### Requerimientos de Diseño UX

No se proporcionó documento de diseño UX como insumo. Los requerimientos de UI/UX se derivan de los Viajes de Usuario y NFRs del PRD:

UX-DR1: La Home publica debe mostrarse completamente funcional en 360px, 768px y 1280px sin overflow horizontal no intencional. Trace: NFR-15, PT-05.

UX-DR2: La tabla de portafolio debe soportar multi-sort y configuracion de columnas persistida por usuario. Trace: FR-48.

UX-DR3: Las filas expandibles del portafolio deben mostrar cuatro secciones (Mi posicion, Mercado, Fundamentales, Distribuciones) de forma clara y sin ruptura de layout en mobile. Trace: FR-27, NFR-15.

UX-DR4: Los estados de carga deben usar el ciclo `idle/loading/success/error` con overlays `partial` o `stale` cuando aplique. Las paginas publicas prefieren skeleton o display optimista de datos stale sobre spinners bloqueantes. Trace: architecture loading state patterns.

UX-DR5: Las acciones de Ops deben mostrar estados queued, running y result en lugar de simular completion sincrona. Trace: architecture process patterns.

UX-DR6: Las superficies publicas e indexables (Home, ficha publica, rutas publicas) deben servir HTML inicial rastreable con title, meta description, URL canonica y semantica estructural suficiente para SEO organico. Trace: PT-10, architecture rendering model.

UX-DR7: Todas las superficies MVP deben cumplir WCAG 2.1 AA en navegacion por teclado, contraste, foco visible, nombres accesibles y semantica estructural. Trace: PT-11.

### Mapa de Cobertura de RFs

FR-01: Épica 2 — Catálogo maestro con ticker único y metadatos
FR-02: Épica 2 — URLs oficiales por FIBRA
FR-03: Épica 2 — Home pública con búsqueda, carrusel, top movers, noticias (estructura)
FR-04: Épica 2 — Buscador global por ticker/nombre
FR-05: Épica 3 — Home con estados de frescura Fresh/Stale/fuera-de-horario
FR-06: Épica 2 — Ficha pública consolidada
FR-07: Épica 2 — Periodo de origen y advertencia de antigüedad en fundamentales
FR-08: GROWTH — Comparador público (excluido MVP)
FR-09: GROWTH — Comparador tolerancia a faltantes (excluido MVP)
FR-10: Épica 3 — Módulo de Mercado con Last Price, cambio, volumen, histórico
FR-11: Épica 3 — Yield anualizado por frecuencia detectada
FR-12: Épica 3 — Yield no disponible sin romper score
FR-13: Épica 4 — Ingesta RSS Google News con queries por FIBRA y generales
FR-14: Épica 4 — Blocklist + dedupe exacto y probable
FR-15: Épica 4 — Asociación automática de noticias con FIBRAs
FR-16: Épica 4 — Publicación sin resumen cuando AI_MODE=Off
FR-17: Épica 4 — Generación de resumen en AI_MODE=Manual
FR-18: Épica 5 — Fundamentales tres modos Off/Manual/Api
FR-19: Épica 5 — Endpoint de importación de fundamentales
FR-20: GROWTH — Modo Api automático (excluido MVP)
FR-21: Épica 5 — Detección de PDF nuevo por periodo
FR-22: Épica 6 — Carga de portafolio (Ticker/Qty/AvgCost)
FR-22b: Épica 5 — Métricas fundamentales faltantes en null
FR-22c: Épica 5 — Modelo de almacenamiento de fundamentales por registro
FR-23: Épica 6 — Fórmula CostoTotalCompra con factor comisión
FR-24: Épica 6 — Validación síncrona de carga, tabla de errores por fila
FR-25: Épica 6 — Confirmación de reemplazo + edición inline
FR-26: Épica 6 — KPIs agregados del portafolio
FR-27: Épica 6 — Filas expandibles con cuatro secciones
FR-28: Épica 6 — Badge de señal NAV vs Precio
FR-29: Épica 7 — Oportunidades universo completo + vista Promediar Posición
FR-30: Épica 7 — Score con 5 componentes, redistribución de pesos, umbral 3/5
FR-31: Épica 7 — Tres perfiles preconfigurados + score configurable por usuario
FR-32: Épica 7 — Marcar FIBRA como favorita desde M5/M8/M9
FR-33: Épica 7 — FIBRAs favoritas destacadas al inicio de tablas en M8 y M9
FR-35: Épica 5 — Cinco secciones del Centro de Procesos
FR-36: Épica 5 — Dashboard operativo con estado de pipelines y errores
FR-37: Épica 5 — Historial de corridas con Ejecutar ahora auditado
FR-38: Épica 5 — Formulario de importación JSON en sección Fundamentales de Ops
FR-39: Épica 5 — Gestión de catálogo desde Ops (CRUD + soft delete)
FR-40: Épica 5 — Configuración editable sin redespliegue desde Ops
FR-41: Épica 5 — Estados operativos por item y corrida
FR-42: Épica 1 — Autenticación y separación de superficies
FR-43: Épica 6 — % Portafolio por monto invertido (DR-12)
FR-44: Épica 6 — Factor de comisión configurable (DR-13)
FR-45: Épica 6 — AVG últimos 4 periodos configurable (DR-11)
FR-46: Épica 6 — Solo posiciones activas en portafolio
FR-47: Épica 6 — Pantalla unificada /portafolio sin ruta /dashboard
FR-48: Épica 6 — Tabla con columnas configurables y multi-sort persistido
FR-49: Épica 2 — Variantes de nombre para queries RSS (editables desde Ops en Épica 5)
FR-50: Épica 4 — 10 noticias recientes en Home
FR-51: Épica 7 — Vista universo Oportunidades con score desglosado
FR-52: Épica 7 — Vista Promediar Posición con simulador
FR-53: Épica 6 — Edición inline de posiciones (Qty/AvgCost)
FR-54: Épica 7 — Monitoreo cobertura universo + advertencia de ranking degradado

## Lista de Épicas

### Épica 1: Fundación, Infraestructura y Acceso
El sistema está deployable, ambos SPAs corren, AdminOps puede autenticarse y el backbone técnico está operativo: auth JWT+refresh, API v1 con OpenAPI y SharedApiClient tipado, Hangfire in-app, health checks, logs estructurados con correlation IDs, single-deploy sobre IIS.

**RFs cubiertos:** FR-42
**Reqs adicionales cubiertos:** Setup inicial completo (.NET 10 + PostgreSQL 16 + EF Core + Hangfire + React 19.2 + Vite 7 + shadcn), auth JWT+refresh, API v1 + OpenAPI + SharedApiClient, ambos SPAs, observabilidad mínima, pipeline CI/CD baseline
**NFRs:** NFR-11, NFR-13, NFR-16

### Épica 2: Catálogo Maestro y Descubrimiento Público
Los visitantes públicos pueden explorar el catálogo de FIBRAs, buscar por ticker/nombre y acceder a la ficha pública con datos estructurales, fundamentales estáticos y distribuciones básicas.

**RFs cubiertos:** FR-01, FR-02, FR-03, FR-04, FR-06, FR-07, FR-49
**NFRs:** NFR-01, NFR-09, NFR-15
**PT reqs:** PT-09 (soporte de navegadores), PT-10 (SEO/prerender rutas públicas), PT-11 (WCAG 2.1 AA)

### Épica 3: Mercado y Frescura de Datos
Los visitantes y usuarios autenticados ven precios de mercado actualizados cada 15 minutos con clasificación de frescura (Fresh/Stale/fuera-de-horario/crítico) en Home y ficha pública. El pipeline opera exclusivamente dentro del horario BMV.

**RFs cubiertos:** FR-05, FR-10, FR-11, FR-12
**NFRs:** NFR-03, NFR-04, NFR-06

### Épica 4: Noticias y Contenido
Los visitantes y usuarios autenticados ven noticias de FIBRAs en la Home y en la ficha pública, con ingesta automática horaria, deduplicación, blocklist configurable y soporte para AI_MODE=Off y AI_MODE=Manual.

**RFs cubiertos:** FR-13, FR-14, FR-15, FR-16, FR-17, FR-50
**NFRs:** NFR-05 (cadencia configurable)

### Épica 5: Centro de Procesos y Fundamentales
AdminOps puede operar y monitorear todos los pipelines desde el Centro de Procesos, importar PDFs de fundamentales en modo Manual con preview y confirmación, gestionar el catálogo de FIBRAs y configurar todos los parámetros operativos del sistema sin redespliegue.

**RFs cubiertos:** FR-18, FR-19, FR-21, FR-22b, FR-22c, FR-35, FR-36, FR-37, FR-38, FR-39, FR-40, FR-41
**NFRs:** NFR-05, NFR-07, NFR-10, NFR-12, NFR-13

### Épica 6: Portafolio Unificado
Los usuarios autenticados pueden cargar su portafolio en `/portafolio`, ver KPIs agregados, analizar cada posición con datos de mercado y fundamentales en filas expandibles, ver el badge de señal NAV vs precio, y editar posiciones inline con factor de comisión aplicado.

**RFs cubiertos:** FR-22, FR-23, FR-24, FR-25, FR-26, FR-27, FR-28, FR-43, FR-44, FR-45, FR-46, FR-47, FR-48, FR-53
**NFRs:** NFR-02

### Épica 7: Oportunidades y Favoritos
Los usuarios autenticados pueden identificar oportunidades con un score configurable y explicable, usar el simulador de promediar posición, monitorear la cobertura del universo activo, y marcar FIBRAs como favoritas desde la ficha pública, el portafolio y las oportunidades.

**RFs cubiertos:** FR-29, FR-30, FR-31, FR-32, FR-33, FR-51, FR-52, FR-54

---

## Épica 1: Fundación, Infraestructura y Acceso

El sistema está deployable, ambos SPAs corren, AdminOps puede autenticarse y el backbone técnico está operativo.

### Historia 1.1: Inicialización de la solución y estructura del proyecto

Como desarrollador,
quiero la solución FIBRADIS inicializada con la estructura correcta (ASP.NET Core API, dos SPAs Vite React TS, shadcn, PostgreSQL con migraciones de EF Core, estructura de directorios completa),
para que el equipo tenga una base consistente y ejecutable antes de construir cualquier funcionalidad.

**Criterios de Aceptación:**

**Dado que** clono el repositorio y ejecuto los comandos de inicialización,
**Cuando** ejecuto `dotnet build` sobre la solución,
**Entonces** la solución compila sin errores.

**Dado que** ejecuto ambos servidores de desarrollo Vite (`src/Web/Main` y `src/Web/Ops`),
**Cuando** cada aplicación inicia,
**Entonces** Main carga en localhost en su puerto y Ops carga en un puerto separado, cada una mostrando el shell shadcn/Tailwind por defecto sin errores de consola.

**Dado que** la cadena de conexión de base de datos está configurada,
**Cuando** ejecuto las migraciones de EF Core,
**Entonces** el esquema inicial se crea en PostgreSQL sin errores y `SELECT 1` pasa correctamente.

**Dado que** la estructura del proyecto está en su lugar,
**Entonces** existen los directorios: `src/Server/Api`, `src/Server/Application`, `src/Server/Domain`, `src/Server/Infrastructure`, `src/Web/Main/src/modules/`, `src/Web/Ops/src/modules/`, `tests/Unit/`, `tests/Integration/`, `tests/Contract/`.

---

### Historia 1.2: Backend API v1 con OpenAPI y cliente tipado para los SPAs

Como desarrollador,
quiero el baseline del ASP.NET Core API configurado con rutas REST JSON bajo `/api/v1`, documento OpenAPI generado al arranque, contrato de error ProblemDetails y un SharedApiClient generado para ambos SPAs,
para que los desarrolladores de frontend puedan consumir contratos tipados del backend y detectar cambios incompatibles antes de liberar.

**Criterios de Aceptación:**

**Dado que** el backend está en ejecución,
**Cuando** hago GET a `/api/v1/health`,
**Entonces** recibo una respuesta 200 con estado JSON.

**Dado que** el backend está en ejecución,
**Cuando** navego al endpoint de OpenAPI (`/swagger` o `/openapi/v1.json`),
**Entonces** el spec completo de la API está disponible.

**Dado que** el spec de OpenAPI existe,
**Cuando** ejecuto el script de generación de código,
**Entonces** los tipos TypeScript y funciones cliente se generan en `src/Web/SharedApiClient/` y tanto `Main` como `Ops` pueden importar desde él sin errores de TypeScript.

**Dado que** el backend retorna un error de dominio (por ejemplo, no encontrado),
**Cuando** el cliente recibe la respuesta,
**Entonces** el cuerpo de la respuesta cumple con `ProblemDetails` con campos `type`, `title`, `status`, `domainCode` y `correlationId`.

---

### Historia 1.3: Autenticación JWT y autorización por roles

Como usuario o AdminOps,
quiero autenticarme con correo y contraseña, recibir un token de acceso JWT de vida corta y un refresh token rotado, y tener mi rol aplicado en las rutas protegidas,
para que las superficies privadas y de operaciones sean inaccesibles sin credenciales válidas.

**Criterios de Aceptación:**

**Dado que** tengo credenciales válidas de una cuenta Usuario,
**Cuando** hago POST a `/api/v1/auth/login`,
**Entonces** recibo un token de acceso JWT en el cuerpo de la respuesta y un refresh token en una cookie HttpOnly.

**Dado que** el token de acceso ha expirado,
**Cuando** hago POST a `/api/v1/auth/refresh`,
**Entonces** recibo un nuevo token de acceso; el refresh token anterior queda invalidado.

**Dado que** tengo una solicitud con un JWT válido de Usuario,
**Cuando** accedo a una ruta privada (`/portafolio`),
**Entonces** recibo 200.

**Dado que** tengo una solicitud con un JWT válido de Usuario,
**Cuando** accedo a una ruta de Ops (`/api/v1/ops/*`),
**Entonces** recibo 403 Forbidden.

**Dado que** tengo una solicitud con un JWT válido de AdminOps,
**Cuando** accedo a una ruta de Ops,
**Entonces** recibo 200.

**Dado que** no se proporciona token,
**Cuando** accedo a una ruta pública,
**Entonces** recibo 200. Cuando accedo a una ruta privada, entonces recibo 401.

---

### Historia 1.4: Hangfire, health checks y observabilidad mínima

Como AdminOps,
quiero que el sistema exponga endpoints de health check para API, base de datos y frescura de pipelines, registre todas las solicitudes con logs estructurados y correlation IDs, y soporte jobs en background de Hangfire con ejecución restart-safe,
para que pueda diagnosticar la salud del sistema y los jobs se ejecuten de forma confiable incluso después de reinicios del proceso.

**Criterios de Aceptación:**

**Dado que** el sistema está en ejecución,
**Cuando** hago GET a `/health`,
**Entonces** recibo una respuesta JSON mostrando el estado de API, conexión a base de datos y frescura de pipelines (degraded/healthy).

**Dado que** se procesa cualquier solicitud HTTP,
**Entonces** se genera o reenvía un `correlationId`, y todas las entradas de log relacionadas con esa solicitud comparten el mismo `correlationId`.

**Dado que** un job recurrente de Hangfire está configurado,
**Cuando** el proceso se reinicia a mitad de la ejecución de un job,
**Entonces** el job se reanuda o reinicia de forma idempotente en el siguiente ciclo sin crear registros duplicados.

**Dado que** un job de Hangfire se ejecuta,
**Entonces** su ejecución (inicio, fin, error) es visible en el dashboard de Hangfire en `/hangfire` (acceso solo AdminOps).

---

## Épica 2: Catálogo Maestro y Descubrimiento Público

Los visitantes públicos pueden explorar el catálogo de FIBRAs, buscar por ticker/nombre y acceder a la ficha pública.

### Historia 2.1: Catálogo maestro de FIBRAs con datos semilla iniciales

Como visitante público,
quiero ver un catálogo de FIBRAs con su ticker, nombre completo, sector, mercado, moneda, estado y URLs oficiales,
para que pueda encontrar e identificar cualquier FIBRA activa en la plataforma.

**Criterios de Aceptación:**

**Dado que** el sistema está sembrado con al menos 10 FIBRAs activas,
**Cuando** hago GET a `/api/v1/fibras`,
**Entonces** recibo una lista paginada con ticker, fullName, shortName, sector, market, currency, state y siteUrl para cada FIBRA.

**Dado que** hago GET a `/api/v1/fibras/FUNO11`,
**Entonces** recibo los metadatos completos de FUNO11 incluyendo todas las URLs y variantes de nombre para queries de RSS.

**Dado que** una FIBRA está desactivada (state=inactive),
**Entonces** queda excluida de las respuestas del universo activo pero sus datos históricos siguen siendo accesibles.

**Dado que** el esquema del módulo de catálogo está en su lugar,
**Entonces** existen tablas en el esquema `catalog` con las columnas correctas (fibra_id, ticker, campos de nombre, sector, estado, URLs, variantes de nombre) sin referencias a esquemas de otros módulos.

---

### Historia 2.2: Home pública con búsqueda global y layout

Como visitante público,
quiero llegar a la página de inicio y usar la barra de búsqueda global para encontrar cualquier FIBRA por ticker o nombre,
para que pueda navegar el universo de FIBRAs y llegar al perfil de cualquier FIBRA específica en menos de dos clics.

**Criterios de Aceptación:**

**Dado que** navego a `/`,
**Cuando** la Home carga,
**Entonces** veo el encabezado con barra de búsqueda, una sección de carrusel de precios (mostrando estado placeholder si no hay datos de mercado), sección de top movers (vacío/placeholder si no hay datos), sección de ranking rápido (placeholder) y sección de noticias (placeholder para los últimos 10 items).

**Dado que** escribo "FUN" en la barra de búsqueda,
**Cuando** aparecen los resultados,
**Entonces** veo sugerencias de autocompletado que coinciden con FIBRAs por ticker o nombre (insensible a mayúsculas), limitadas a 8 resultados.

**Dado que** selecciono "FUNO11" de las sugerencias,
**Cuando** soy redirigido,
**Entonces** llego a `/fibras/FUNO11`.

**Dado que** escribo una cadena que no coincide con nada en el catálogo,
**Entonces** veo un estado claro de "sin resultados encontrados" — sin error.

**Dado que** no hay datos de mercado cargados aún,
**Entonces** la Home renderiza correctamente con estados placeholder/cargando; no ocurren errores de JavaScript.

---

### Historia 2.3: Ficha pública de FIBRA

Como visitante público,
quiero ver la página de perfil público completo de una FIBRA mostrando sus datos de encabezado, gráfica de historial de precios, fundamentales del último período disponible, últimas 8 distribuciones, últimas 10 noticias asociadas y lista de reportes oficiales,
para que pueda investigar una FIBRA de forma completa sin navegar fuera de su ficha.

**Criterios de Aceptación:**

**Dado que** navego a `/fibras/FUNO11`,
**Cuando** la página carga,
**Entonces** veo: encabezado con nombre y ticker de la FIBRA (el precio muestra placeholder hasta la Épica 3), una gráfica de precios con selectores 1M/3M/6M/1A (estado vacío si no hay datos de mercado), sección de fundamentales mostrando el último período disponible con etiquetas estilo "Cap Rate — Q3 2024", últimas 8 distribuciones en orden cronológico inverso (o estado vacío), sección de noticias placeholder (se pobla en Épica 4) y sección de reportes.

**Dado que** los datos de fundamentales de FUNO11 son de hace 3 trimestres,
**Entonces** se muestra una advertencia visible: "Último reporte disponible: hace 3 periodos — datos podrían estar desactualizados."

**Dado que** una métrica de fundamentales es null para el último período,
**Entonces** esa métrica muestra `—` en la UI — sin error, sin sustitución por cero.

**Dado que** navego a un ticker inexistente (`/fibras/FAKE99`),
**Entonces** veo una página clara de "FIBRA no encontrada" con un enlace de regreso a la Home.

---

### Historia 2.4: SEO, prerender y accesibilidad WCAG 2.1 AA

Como propietario del sitio,
quiero que las rutas públicas (Home, ficha pública) sirvan HTML rastreable con meta tags correctos, y que todos los elementos interactivos cumplan los estándares de accesibilidad WCAG 2.1 AA,
para que los motores de búsqueda puedan indexar FIBRADIS y los usuarios con tecnologías de asistencia puedan navegar la plataforma de forma efectiva.

**Criterios de Aceptación:**

**Dado que** obtengo `/` sin JavaScript habilitado (o vía prerender),
**Entonces** la respuesta HTML incluye un `<title>`, un `<meta name="description">`, una etiqueta `<link>` canónica y jerarquía semántica de encabezados (`h1`, `h2`).

**Dado que** obtengo `/fibras/FUNO11` sin JavaScript,
**Entonces** el HTML incluye un título como "FUNO11 — FIBRA Uno | FIBRADIS" y una meta description con resumen de la FIBRA.

**Dado que** navego la Home usando solo teclado,
**Entonces** todos los elementos interactivos (barra de búsqueda, enlaces de navegación, tarjetas de FIBRA) son alcanzables mediante Tab, tienen indicadores de foco visibles y nombres accesibles.

**Dado que** veo la Home en un viewport de 360px de ancho,
**Entonces** no ocurre overflow horizontal y la acción principal (búsqueda) es visible sin desplazarse.

**Dado que** veo la Home en 768px y 1280px,
**Entonces** el layout se adapta correctamente sin elementos rotos.

---

## Épica 3: Mercado y Frescura de Datos

Los visitantes y usuarios autenticados ven precios de mercado actualizados cada 15 minutos con clasificación de frescura correcta.

### Historia 3.1: Pipeline de mercado — ingesta y snapshots

Como AdminOps,
quiero que el pipeline de mercado obtenga Last Price, cambio diario, volumen y datos de 52 semanas del proveedor externo cada 15 minutos, pero solo durante el horario BMV (8:15am–3:15pm CDMX, días hábiles), y que persista snapshots diarios,
para que los precios de las FIBRAs se mantengan frescos durante el horario de mercado y no se ejecuten ciclos cuando el mercado está cerrado.

**Criterios de Aceptación:**

**Dado que** el reloj del sistema es 10:00am CDMX un martes,
**Cuando** se ejecuta un ciclo del pipeline,
**Entonces** se persisten registros PriceSnapshot para todas las FIBRAs activas con timestamp `captured_at` y `status=processed`.

**Dado que** el reloj del sistema es 4:00pm CDMX cualquier día hábil,
**Entonces** no se dispara ningún ciclo del pipeline de mercado — Hangfire no programa el job fuera del horario BMV.

**Dado que** el proveedor externo retorna un error para FMTY14 pero tiene éxito para todas las demás,
**Entonces** FMTY14 obtiene `status=error` con `error_reason` poblado, mientras que todas las demás FIBRAs se actualizan normalmente.

**Dado que** transcurre un día completo de operaciones,
**Entonces** se persiste un registro de snapshot diario por FIBRA activa en el esquema `market` con campos OHLC.

**Dado que** dos ciclos consecutivos fallan para una FIBRA,
**Entonces** el sistema clasifica el precio de esa FIBRA como `critico` según NFR-04.

---

### Historia 3.2: Clasificación de frescura y estados en UI

Como visitante público,
quiero ver indicadores de frescura precisos (Fresh / Stale / fuera-de-horario / crítico) sobre los precios en el carrusel de la Home, top movers y ficha pública,
para que siempre sepa si el precio que estoy viendo es actual.

**Criterios de Aceptación:**

**Dado que** el precio de una FIBRA fue actualizado hace 10 minutos durante el horario de mercado,
**Cuando** veo el carrusel de la Home o la ficha,
**Entonces** el precio muestra un indicador verde "Fresh" con el timestamp de última actualización.

**Dado que** un precio fue actualizado hace 90 minutos durante el horario de mercado,
**Entonces** muestra un indicador ámbar "Stale".

**Dado que** el mercado está cerrado (después de las 3:15pm o fin de semana) y el último precio es el precio de cierre del día,
**Entonces** el precio muestra "fuera-de-horario" — no "Stale" ni "crítico".

**Dado que** un precio no ha sido actualizado por más de 6 horas durante una sesión de mercado abierta,
**Entonces** muestra un indicador rojo de degradación "crítico".

**Dado que** no existe ningún dato de precio para una FIBRA,
**Entonces** los campos de precio muestran `—` y no se muestra indicador de frescura.

---

### Historia 3.3: Historial de precios, yield anualizado y snapshots a 90 días

Como visitante público,
quiero ver 90 días de historial de precios en la gráfica de la ficha pública y ver un yield de dividendo anualizado calculado a partir de la frecuencia de distribución real detectada de la FIBRA,
para que pueda analizar tendencias de precios e ingresos por rendimiento basados en patrones reales y no en periodicidad asumida.

**Criterios de Aceptación:**

**Dado que** FUNO11 tiene 60 días de snapshots diarios,
**Cuando** selecciono el selector de gráfica "3M" en su ficha,
**Entonces** se renderizan hasta 60 puntos de datos diarios disponibles; los días sin snapshot muestran un hueco, no un cero.

**Dado que** FUNO11 tiene distribuciones en intervalos irregulares,
**Cuando** el sistema calcula el yield anualizado,
**Entonces** usa el patrón de frecuencia detectado (ej: 3 pagos en 12 meses → anualizar por 3), NO una suposición trimestral fija.

**Dado que** no existen datos de distribución para una FIBRA,
**Entonces** el yield se muestra como "no disponible" sin ninguna estimación numérica.

**Dado que** se consultan snapshots de hace 90 días,
**Entonces** siguen siendo recuperables y aparecen en la gráfica — no son eliminados ni ocultados.

---

## Épica 4: Noticias y Contenido

Los visitantes y usuarios autenticados ven noticias de FIBRAs con ingesta automática, deduplicación y soporte para AI_MODE.

### Historia 4.1: Ingesta RSS, blocklist y deduplicación de noticias

Como visitante público,
quiero que la plataforma ingiera automáticamente noticias relacionadas con FIBRAs cada hora desde Google News RSS, filtrando contenido irrelevante y eliminando duplicados,
para que solo aparezcan noticias limpias y relevantes sobre FIBRAs inmobiliarias en la plataforma.

**Criterios de Aceptación:**

**Dado que** el pipeline de noticias se ejecuta por primera vez,
**Cuando** se obtienen items usando queries específicas por FIBRA (ej: "FUNO11 FIBRA") y queries generales de mercado (ej: "FIBRAs Mexico BMV"),
**Entonces** todos los items se normalizan con título, fuente, fecha, URL y snippet, y se persisten con `status=pending`.

**Dado que** un item contiene "fibra óptica" en su título o snippet,
**Cuando** se aplica el blocklist,
**Entonces** el item se descarta y no se guarda en la base de datos.

**Dado que** dos items comparten exactamente la misma URL,
**Entonces** solo se almacena la primera ocurrencia; el duplicado se descarta.

**Dado que** dos items tienen títulos casi idénticos publicados en un período de 24 horas,
**Entonces** solo se almacena uno; el duplicado probable se descarta.

**Dado que** el blocklist en Ops se actualiza para agregar un nuevo término,
**Entonces** el siguiente ciclo del pipeline aplica el blocklist actualizado sin redespliegue.

---

### Historia 4.2: Asociación de noticias con FIBRAs y display en Home y ficha

Como visitante público,
quiero que los items de noticias se vinculen automáticamente a FIBRAs relevantes basándose en coincidencia de ticker y variantes de nombre, y ver las 10 noticias más recientes en la Home y noticias específicas de cada FIBRA en su ficha,
para que pueda encontrar noticias relevantes para FIBRAs específicas sin curación manual.

**Criterios de Aceptación:**

**Dado que** el título de un item de noticias contiene "FUNO11",
**Cuando** se ejecuta el paso de asociación,
**Entonces** el item se vincula a FUNO11 y aparece en la sección de noticias de su ficha pública (últimas 10, orden cronológico inverso).

**Dado que** un item de noticias no contiene ningún ticker ni variante de nombre conocida,
**Entonces** tiene cero asociaciones a FIBRAs y aparece en el feed general de noticias de la Home pero no en ninguna sección específica de FIBRA.

**Dado que** veo la página de la Home,
**Cuando** carga la sección de noticias,
**Entonces** los 10 items más recientes (independientemente de su asociación a FIBRA) aparecen en orden de fecha de publicación.

**Dado que** una FIBRA no tiene noticias asociadas,
**Entonces** la sección de noticias de su ficha muestra un estado vacío claro de "Sin noticias disponibles" — sin error.

---

### Historia 4.3: Soporte para AI_MODE en noticias (Off y Manual)

Como AdminOps,
quiero controlar si se generan resúmenes de noticias mediante la configuración AI_MODE (Off o Manual), con la plataforma iniciando en Off en el primer despliegue,
para que las noticias siempre se publiquen incluso sin procesamiento de IA, y pueda disparar resúmenes manualmente cuando sea necesario.

**Criterios de Aceptación:**

**Dado que** AI_MODE=Off (valor por defecto del sistema en el primer despliegue),
**Cuando** se ingiere un item de noticias,
**Entonces** se publica con título, fuente, fecha, snippet y enlace original; no se realiza ninguna llamada a IA; el estado es `processed`.

**Dado que** AI_MODE=Manual y AdminOps dispara la generación de resumen para un item de noticias específico desde Ops,
**Cuando** la llamada a IA al proveedor configurado tiene éxito,
**Entonces** el resumen del item se actualiza y se muestra en la plataforma.

**Dado que** AI_MODE=Manual y la llamada a IA falla,
**Entonces** el item se publica con `status=parcial`, conservando título, fuente, fecha, snippet y enlace original; la UI muestra el item sin resumen pero sin estado de error.

**Dado que** AdminOps cambia AI_MODE de Off a Manual en la sección de configuración de Ops,
**Entonces** el cambio toma efecto en el siguiente ciclo del pipeline sin redespliegue, y el cambio queda registrado en el log de auditoría.

---

## Épica 5: Centro de Procesos y Fundamentales

AdminOps puede operar todos los pipelines, importar PDFs de fundamentales, gestionar el catálogo y configurar el sistema desde el Centro de Procesos.

### Historia 5.1: Dashboard operativo y control de pipelines

Como AdminOps,
quiero ver el dashboard de operaciones con salud de pipelines, historial de ejecuciones recientes, detalle de errores y un botón "Ejecutar ahora" para cada pipeline,
para que pueda diagnosticar rápidamente el estado del sistema y disparar ejecuciones manuales cuando sea necesario.

**Criterios de Aceptación:**

**Dado que** navego a `/ops/dashboard`,
**Cuando** la página carga,
**Entonces** veo cada pipeline (mercado, noticias, fundamentales) con: estado actual (activo/fallando/pausado), timestamp y duración de la última ejecución, items procesados y conteo de errores en la última ejecución.

**Dado que** existen los últimos 5 errores globales,
**Entonces** aparecen en un panel con timestamp, nombre del pipeline y descripción del error.

**Dado que** hago clic en "Ejecutar ahora" en el pipeline de noticias,
**Cuando** la acción se completa,
**Entonces** se dispara la ejecución del pipeline, el botón muestra un estado de cargando/en cola, y el log de auditoría registra el disparo manual con nombre de usuario del actor y timestamp.

**Dado que** veo la sección de historial de pipelines,
**Entonces** veo las últimas ejecuciones de cada pipeline con: FIBRAs procesadas, tickers con errores y sus causas (para mercado), items nuevos / duplicados descartados / errores (para noticias).

---

### Historia 5.2: Importación de fundamentales en modo Manual

Como AdminOps,
quiero enviar datos de fundamentales extraídos para una FIBRA y trimestre, revisar un preview de los datos parseados y confirmar antes de que sean visibles en la plataforma,
para que todos los datos financieros publicados a los usuarios hayan sido validados por un operador humano.

**Criterios de Aceptación:**

**Dado que** envío un payload JSON válido al endpoint de importación de fundamentales con fibra_id, period, cap_rate, nav_per_cbfi, ltv, noi_margin, ffo_margin, quarterly_distribution y summary,
**Entonces** el sistema crea un registro con `status=pendiente` y retorna un preview listando campos reconocidos, campos faltantes y la referencia al PDF.

**Dado que** confirmo la importación desde la UI de Fundamentales en Ops,
**Entonces** el estado del registro cambia a `procesado` y los datos se vuelven visibles en el perfil público de la FIBRA bajo la etiqueta del trimestre correcto.

**Dado que** ya existe un registro para la misma FIBRA y período con `status=procesado`,
**Cuando** envío un nuevo payload para el mismo período,
**Entonces** el sistema lo marca como posible actualización con una advertencia y no lo sobreescribe automáticamente — se requiere un Reproceso explícito.

**Dado que** algunos campos son null o están ausentes en el payload,
**Entonces** el registro se crea con `status=parcial`, los campos disponibles se almacenan correctamente, y los campos null se muestran como `—` en la ficha sin error.

**Dado que** adjunto un archivo PDF al formulario de importación de Fundamentales en Ops,
**Entonces** el PDF se almacena y su referencia se vincula al registro de fundamentales con los campos `captured_at` y `source` poblados.

---

### Historia 5.3: Gestión del catálogo de FIBRAs desde Ops

Como AdminOps,
quiero agregar nuevas FIBRAs, editar metadatos de FIBRAs existentes y realizar borrado suave (desactivación) de FIBRAs desde la sección de catálogo de Ops,
para que el universo activo de FIBRAs se mantenga preciso sin acceso directo a la base de datos ni redespliegue.

**Criterios de Aceptación:**

**Dado que** completo los campos requeridos para una nueva FIBRA (ticker, nombre completo, sector, mercado, moneda) y la envío,
**Entonces** la FIBRA se agrega al catálogo, aparece en el universo activo y está disponible inmediatamente para la ingesta de datos de mercado y la asociación de noticias.

**Dado que** edito el campo de variantes de nombre para FUNO11 (agregando "Fibra Uno" como variante),
**Entonces** el siguiente ciclo del pipeline de noticias usa las variantes actualizadas en sus queries de RSS.

**Dado que** desactivo DANHOS13,
**Entonces** DANHOS13 queda excluida de los rankings del universo activo, el pipeline de mercado y las queries de noticias, pero todos sus datos históricos de precio, fundamentales y noticias siguen siendo accesibles vía URL directa.

**Dado que** intento agregar una FIBRA con un ticker que ya existe,
**Entonces** el formulario muestra un error de validación: "El ticker ya existe en el catálogo."

---

### Historia 5.4: Configuración operativa desde Ops sin redespliegue

Como AdminOps,
quiero editar todos los parámetros operativos clave desde la sección de configuración de Ops — commission_factor, avg_periods, términos del blocklist de noticias, AI_MODE y cadencia de ejecución de pipelines — con cada cambio completamente auditado,
para que pueda ajustar el comportamiento del sistema en producción sin ningún redespliegue ni intervención de un desarrollador.

**Criterios de Aceptación:**

**Dado que** actualizo `commission_factor` de 0.006 a 0.008 en Configuración de Ops,
**Entonces** los cálculos posteriores de lectura del portafolio usan 0.008; los valores de `costo_total_compra` ya persistidos en la base de datos no cambian.

**Dado que** actualizo `avg_periods` de 4 a 6,
**Entonces** todos los cálculos subsiguientes de métricas AVG (fundamentales y distribuciones) usan los últimos 6 períodos disponibles.

**Dado que** agrego "fibra cervical" al blocklist de noticias,
**Entonces** el siguiente ciclo de ingesta de noticias descarta cualquier item que coincida con ese término.

**Dado que** actualizo la cadencia del pipeline de noticias de 60 minutos a 30 minutos,
**Entonces** la siguiente evaluación del pipeline usa el nuevo intervalo sin redespliegue.

**Dado que** se cambia cualquier campo de configuración,
**Entonces** se crea una entrada en el log de auditoría con: nombre de usuario del actor, timestamp, nombre del campo, valor anterior y nuevo valor — todo recuperable desde la vista de auditoría de Ops.

---

## Épica 6: Portafolio Unificado

Los usuarios autenticados pueden cargar su portafolio, analizar posiciones con datos de mercado y fundamentales, y editar posiciones inline.

### Historia 6.1: Carga y validación del portafolio

Como usuario,
quiero subir un archivo Excel o CSV con mis posiciones en FIBRAs (Ticker, Qty, AvgCost) y recibir retroalimentación inmediata y clara sobre cualquier error de validación,
para que pueda cargar mi portafolio correctamente sin contactar soporte ni adivinar qué salió mal.

**Criterios de Aceptación:**

**Dado que** subo un archivo .xlsx válido con 5 filas (columnas Ticker, Qty, AvgCost),
**Cuando** se procesa la carga,
**Entonces** las 5 posiciones se almacenan y el dashboard del portafolio se muestra inmediatamente.

**Dado que** subo un archivo donde una fila tiene el ticker "FAKEXX" (no está en el catálogo),
**Entonces** no se guarda ninguna posición, y veo una tabla con: fila número 2, ticker "FAKEXX", error "Ticker no encontrado en el catálogo."

**Dado que** subo un archivo donde FUNO11 aparece dos veces (500 unidades a $47, 300 unidades a $45),
**Entonces** las posiciones se consolidan en 1 fila: 800 unidades al costo promedio ponderado ($46.25).

**Dado que** subo un archivo con un encabezado incorrecto (ej: "Cost" en lugar de "AvgCost"),
**Entonces** todas las filas fallan con un solo error: "Columnas requeridas: Ticker, Qty, AvgCost. Encontradas: Ticker, Qty, Cost."

**Dado que** ya tengo un portafolio activo y subo un nuevo archivo,
**Entonces** aparece un diálogo de confirmación: "Esto reemplazará tus 5 posiciones actuales. ¿Continuar?". Al cancelar, nada cambia.

---

### Historia 6.2: KPIs del portafolio y tabla con multi-sort y columnas configurables

Como usuario,
quiero ver los KPIs de mi portafolio (Inversión Total, Valor Total, Plusvalía, Ganancia, Rentas) y una tabla de posiciones compacta con multi-sort y columnas configurables que cargue en menos de un segundo,
para que pueda evaluar la salud de mi portafolio de un vistazo y profundizar en los datos que más me importan.

**Criterios de Aceptación:**

**Dado que** mi portafolio tiene 3 posiciones y todas tienen precios de mercado actuales,
**Cuando** carga la página `/portafolio`,
**Entonces** el encabezado de KPIs muestra valores correctos de Inversión Total (suma de costo_total_compra), Valor Total (suma de titulos × precio_mercado), Plusvalía % y $, Rentas Anuales Brutas, Rentas Reales Brutas y % Rentas del Portafolio.

**Dado que** la página carga con datos precalculados,
**Entonces** el tiempo de respuesta P95 es inferior a 1 segundo (medido en backend).

**Dado que** hago clic en el encabezado de la columna Plusvalía una vez y luego en el encabezado de Ganancia mientras sostengo Shift,
**Entonces** la tabla ordena por Plusvalía como criterio primario y Ganancia como secundario.

**Dado que** abro el panel de configuración de columnas y activo la columna "Cap Rate",
**Entonces** la columna aparece en la tabla inmediatamente y persiste en mi próximo inicio de sesión.

**Dado que** una posición no tiene precio de mercado actual,
**Entonces** Precio Actual, Valor de Mercado, Plusvalía y Ganancia muestran `—` para esa fila; los KPIs que incluyen esa posición muestran anotación `parcial`.

**Y** no existe la ruta `/dashboard` — toda la funcionalidad del portafolio está bajo `/portafolio`.

---

### Historia 6.3: Filas expandibles con detalle de posición y badge de señal NAV

Como usuario,
quiero expandir cualquier fila del portafolio para ver secciones de detalle (Mi posición, Mercado, Fundamentales, Distribuciones) y un badge de señal de color mostrando NAV vs precio de mercado,
para que pueda analizar cada posición en profundidad directamente desde la tabla del portafolio.

**Criterios de Aceptación:**

**Dado que** hago clic para expandir la fila de FUNO11,
**Cuando** se renderiza la sección expandida,
**Entonces** veo cuatro secciones claramente etiquetadas: Mi posición (títulos, costo promedio, valor de mercado, plusvalía % y $), Mercado (precio actual, cambio %, volumen, AVG 52S, máximo/mínimo 52S), Fundamentales (Cap Rate, NAV, LTV, NOI, FFO con etiquetas de período), Distribuciones (últimas 4 distribuciones, renta trimestral y anual, yield calculado y decretado).

**Dado que** el NAV por CBFI de FUNO11 es $120 y el precio actual es $100 (16.7% por debajo del NAV),
**Entonces** el badge de señal es verde.

**Dado que** una posición donde el precio es $105 y el NAV es $100 (5% por encima del NAV, dentro de ±10%),
**Entonces** el badge es amarillo.

**Dado que** una posición donde el precio es $115 y el NAV es $100 (15% por encima del NAV),
**Entonces** el badge es rojo.

**Dado que** los datos de NAV no están disponibles para una posición,
**Entonces** el badge es gris.

**Y** todos los badges tienen un tooltip que explica el criterio: "Cotiza con descuento de X% respecto al NAV" o equivalente.

---

### Historia 6.4: Edición inline y eliminación de posiciones

Como usuario,
quiero editar posiciones individuales del portafolio de forma inline haciendo clic en la celda de Qty o AvgCost, y eliminar posiciones con confirmación explícita,
para que pueda corregir errores de datos sin volver a subir el archivo completo.

**Criterios de Aceptación:**

**Dado que** hago doble clic en la celda de Qty de la fila de FUNO11,
**Entonces** la celda entra en modo de edición mostrando el valor actual en un campo de entrada.

**Dado que** escribo 600 y presiono Enter,
**Entonces** la posición se guarda con 600 unidades, `costo_total_compra` se recalcula usando el `commission_factor` actual, y todos los KPIs afectados se actualizan inmediatamente.

**Dado que** escribo -50 en el campo de Qty,
**Entonces** la validación rechaza el valor con "La cantidad debe ser un entero positivo" y se restaura el valor original.

**Dado que** presiono Escape mientras edito,
**Entonces** la celda vuelve al modo de lectura con el valor original sin cambios.

**Dado que** hago clic en el botón de eliminar para una posición,
**Entonces** aparece un diálogo de confirmación: "¿Eliminar posición FUNO11? Esta acción no se puede deshacer." Al confirmar, la posición se elimina y todos los KPIs se recalculan. Al cancelar, nada cambia.

---

## Épica 7: Oportunidades y Favoritos

Los usuarios autenticados pueden identificar oportunidades con score configurable, simular promediado de posición, y marcar FIBRAs como favoritas en todas las superficies.

### Historia 7.1: Score de oportunidad y ranking del universo

Como usuario,
quiero ver todas las FIBRAs activas clasificadas por un score de oportunidad configurable con cinco componentes (Descuento NAV 30%, Dividend Yield 30%, LTV invertido 20%, Margen NOI 10%, Precio vs AVG 52S 10%),
para que pueda identificar las FIBRAs más atractivas según mis propios criterios de inversión.

**Criterios de Aceptación:**

**Dado que** el universo activo tiene 20 FIBRAs con datos completos,
**Cuando** abro Oportunidades,
**Entonces** las FIBRAs se muestran en orden descendente de score (0–100), con cada fila mostrando nombre/ticker de la FIBRA, score total y el valor de cada componente.

**Dado que** expando una fila de FIBRA,
**Entonces** veo un desglose visual mostrando la contribución de cada componente al score total.

**Dado que** una FIBRA tiene datos para solo 2 de 5 componentes,
**Entonces** aparece en una sección separada de "datos limitados" debajo del ranking principal, con una advertencia visible: "Score referencial — datos insuficientes para el ranking principal."

**Dado que** una FIBRA no tiene ningún componente calculable con precio actual,
**Entonces** queda excluida completamente tanto del ranking principal como de la sección de datos limitados.

**Dado que** arrastro el control deslizante de peso de Yield de 30% a 50%,
**Entonces** el ranking se recalcula en tiempo real sin recargar la página.

**Dado que** selecciono el perfil "Renta",
**Entonces** los pesos se establecen en Yield 50%, NOI 20%, Descuento NAV 20%, LTV 10% y el ranking se actualiza inmediatamente. La configuración persiste en mi próximo inicio de sesión.

---

### Historia 7.2: Vista Promediar Posición con simulador

Como usuario,
quiero ver mis posiciones del portafolio clasificadas por score de oportunidad y usar un simulador de promediado de costo para entender el impacto de comprar unidades adicionales al precio actual,
para que pueda tomar decisiones informadas sobre promediar hacia abajo sin que la plataforma emita recomendaciones de compra o venta.

**Criterios de Aceptación:**

**Dado que** cambio a la pestaña "Promediar Posición" en Oportunidades,
**Entonces** solo aparecen las FIBRAs de mi portafolio, mostrando: nombre/ticker de la FIBRA, mi costo promedio de entrada, precio de mercado actual, diferencia porcentual entre costo y precio, y score de oportunidad.

**Dado que** ingreso 500 en el campo de simulador "Títulos adicionales" para FUNO11 (precio actual $100, mi costo promedio $110, tengo 1000 unidades),
**Entonces** el simulador muestra: nuevo costo promedio $103.33, nuevo valor total de la posición (1500 × $100 = $150,000) y el cambio en plusvalía de -9.1% a -3.2%.

**Dado que** el simulador muestra resultados,
**Entonces** aparece un descargo de responsabilidad visible: "Este simulador es informativo. No constituye una recomendación de compra o venta."

**Dado que** borro el campo de entrada del simulador,
**Entonces** los valores simulados desaparecen y se restauran los valores originales de la posición.

---

### Historia 7.3: Monitoreo de cobertura del universo y ranking degradado

Como usuario,
quiero recibir una advertencia cuando una porción significativa del universo activo de FIBRAs no tiene precio actual, y que el ranking se suspenda si la cobertura es críticamente baja,
para que no tome decisiones basadas en un ranking engañosamente parcial.

**Criterios de Aceptación:**

**Dado que** 8 de 25 FIBRAs activas (32%) no tienen datos de precio actuales,
**Cuando** veo el ranking del universo de Oportunidades,
**Entonces** aparece un banner prominente: "Universo degradado: 8 FIBRAs (32%) sin precio disponible. Último dato válido: [timestamp]." El ranking permanece visible debajo del banner.

**Dado que** 13 de 25 FIBRAs activas (52%) no tienen datos de precio actuales,
**Cuando** veo Oportunidades,
**Entonces** el ranking es reemplazado por: "Ranking no disponible — cobertura insuficiente (52% de FIBRAs sin precio). El ranking se restaurará cuando la cobertura supere el 50%."

**Dado que** AdminOps actualiza el umbral de degradación de 30% a 20% en la Configuración de Ops,
**Entonces** el nuevo umbral se aplica en la siguiente evaluación del universo sin redespliegue.

---

### Historia 7.4: Favoritos — marcar y destacar FIBRAs en todas las superficies

Como usuario,
quiero marcar cualquier FIBRA como favorita usando un ícono de estrella desde su perfil público (M5), mi portafolio (M8) u Oportunidades (M9), y ver mis favoritos destacados al inicio de las tablas en M8 y M9,
para que pueda acceder rápidamente a las FIBRAs que sigo más de cerca sin filtrado manual.

**Criterios de Aceptación:**

**Dado que** hago clic en el ícono de estrella del perfil público de FUNO11,
**Entonces** FUNO11 se marca como favorita (la estrella se rellena), la preferencia se persiste en mi cuenta y el cambio es visible inmediatamente en M5, M8 y M9.

**Dado que** veo mi portafolio (M8) y tengo FUNO11 marcada como favorita,
**Entonces** FUNO11 aparece al inicio de la tabla de posiciones, visualmente separada de las posiciones no favoritas (ej: con ícono de estrella o fila resaltada).

**Dado que** veo Oportunidades (M9) con FUNO11 como favorita,
**Entonces** FUNO11 aparece al inicio de la tabla del ranking del universo, antes que las FIBRAs no favoritas independientemente del orden de score.

**Dado que** hago clic en la estrella rellena de FUNO11 en M9 para quitar el favorito,
**Entonces** FUNO11 regresa inmediatamente a su posición sin destacar en las tablas de M8 y M9, y el cambio persiste al recargar la página.

**Dado que** no tengo ningún favorito marcado,
**Entonces** las tablas de M8 y M9 muestran todas las posiciones/FIBRAs en su orden predeterminado sin encabezado de sección "favoritos".
