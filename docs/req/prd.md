---
stepsCompleted:
  - step-01-init
  - step-02-discovery
  - step-02b-vision
  - step-02c-executive-summary
  - step-03-success
  - step-04-journeys
  - step-05-domain
  - step-06-innovation
  - step-07-project-type
  - step-08-scoping
  - step-09-functional
  - step-10-nonfunctional
  - step-11-polish
  - step-12-complete
classification:
  domain: general
  projectType: web_app
date: '2026-03-30'
inputDocuments:
  - docs/propuesta_funcional_global_v1.md
  - docs/modulo_1_home_publica_detallado.md
  - docs/modulo_2_mercado_detallado.md
  - docs/modulo_3_catalogo_fibras_detallado.md
  - docs/modulo_4_noticias_detallado.md
  - docs/modulo_5_ficha_publica_detallado.md
  - docs/modulo_6_comparador_publico_detallado.md
  - docs/modulo_7_portafolio_detallado.md
  - docs/modulo_8_dashboard_detallado.md
  - docs/modulo_9_oportunidades_detallado.md
  - docs/modulo_10_fundamentales_detallado.md
  - docs/modulo_11_alertas_detallado.md
  - docs/1 input_tecnico_consolidado_v1.md
  - docs/2 sistema_principal_tecnico_v1.md
  - docs/3 centro_de_procesos_tecnico_v1.md
workflowType: 'prd'
---

# Product Requirements Document - FIBRADIS

**Author:** Jorge
**Date:** 2026-03-30

## Executive Summary

FIBRADIS es una plataforma integral de analisis y gestion de FIBRAs inmobiliarias mexicanas. El producto concentra informacion de mercado, distribuciones, noticias y reportes financieros en una experiencia unificada con dos superficies de producto y una superficie operativa interna:

- Mundo publico para descubrimiento, comparacion y consulta consolidada por FIBRA.
- Mundo privado para carga de portafolio, analisis de posiciones, ranking de oportunidades y seguimiento personal de favoritos.
- Centro de Procesos interno para monitoreo operativo, ejecucion manual segura, configuracion de pipelines y control del modo IA.

El producto no ejecuta operaciones bursatiles. Su objetivo es convertir informacion dispersa y heterogenea en metricas estructuradas, senales explicables, score configurable y herramientas practicas de decision.

Usuarios objetivo:

- Visitante publico que quiere entender rapidamente el universo de FIBRAs y comparar emisores.
- Usuario autenticado que quiere evaluar su portafolio actual, detectar oportunidades y definir reglas de seguimiento.
- Operador interno `AdminOps` que necesita diagnosticar pipelines, corregir errores, ajustar schedules y gobernar la capa de procesamiento IA.

Modelo de negocio: el mundo publico es de acceso libre sin registro. Las funcionalidades del mundo privado — portafolio, oportunidades, score configurable y favoritos — requieren autenticacion y se ofrecen bajo un modelo de suscripcion de pago. El Centro de Procesos es de uso exclusivo del equipo operativo interno.

Capacidades diferenciales:

- Extraccion automatica de fundamentales desde PDFs oficiales con historico por periodo y trazabilidad a fuente.
- Score configurable por usuario para reflejar estrategias distintas sin imponer una sola logica de decision.
- Tolerancia a datos incompletos con estados claros, degradacion transparente y sin ruptura de UI.
- Centro de Procesos interno integrado para operar mercado, noticias, PDFs, distribuciones e IA sin depender de redeploy.

## Success Criteria

SC-01. La Home publica responde en menos de 2 segundos en P95 con datos cacheados o precargados y muestra timestamp de ultima actualizacion.

SC-02. El 100% del catalogo activo puede buscarse por ticker o nombre desde la Home y navegarse hacia la ficha publica de cada emisor.

SC-03. El pipeline de mercado actualiza Last Price cada 15 minutos y la UI clasifica frescura con umbrales `Fresh`, `Stale` y degradacion critica.

SC-04. Un usuario puede cargar un archivo valido de portafolio, normalizar posiciones y ver su inversion total sin depender de historico transaccional.

SC-05. El dashboard privado responde en menos de 1 segundo en P95 cuando consume datos precalculados del portafolio.

SC-06. Un PDF de periodo nuevo puede almacenarse, procesarse mediante importacion JSON confirmada desde Ops y reflejar su estado en UI dentro del ciclo operativo. En MVP el flujo es manual; en modo Api la deteccion puede automatizarse sin redisenar la capa funcional.

SC-07. El sistema publica noticias con titulo, fuente, fecha y link original aun cuando no exista resumen IA, y evita duplicados exactos o probables segun reglas documentadas.

SC-08. El modulo de Oportunidades recalcula score al cambiar pesos del usuario y mantiene explicabilidad del resultado.

SC-09. El usuario puede marcar FIBRAs como favoritas desde M5, M8 y M9, y acceder rapidamente a ellas destacadas al inicio de las tablas. La preferencia de favoritos se persiste por usuario.

SC-10. `AdminOps` puede ejecutar `Run now`, `Run by ticker/fibra`, `Retry` y `Reprocess` con auditoria completa y sin duplicar datos.

SC-11. Si un proveedor externo falla o entrega informacion parcial, las vistas permanecen operativas y exponen estado parcial, degradado o no evaluable segun corresponda.

## Product Scope

### MVP

- M1 Home publica con busqueda, carrusel, resumen general, top movers, ranking rapido y noticias recientes.
- M2 Mercado con Last Price cada 15 minutos, snapshot diario de campos no intradia, historico y calculo de yield anualizado.
- M3 Catalogo maestro de FIBRAs con metadatos estructurales, fuentes oficiales y configuracion por emisor.
- M4 Noticias con ingesta automatica, normalizacion, dedupe, asociacion con FIBRAs, filtros y resumen sujeto a `AI_MODE`.
- M5 Ficha publica consolidada con mercado, fundamentales, distribuciones, historico, noticias y reportes.
- M7+M8 Portafolio unificado en `/portafolio`: carga por Excel/CSV, validacion, normalizacion, edicion inline, KPIs agregados, tabla compacta expandible con multi-sort y senales explicables. No existe ruta `/dashboard` separada.
- M9 Oportunidades con ranking del universo, vista de promediar posicion, filtros y score configurable.
- M10 Fundamentales con deteccion de PDFs, extraccion estructurada, historico por periodo y ultimo valor vigente.
- Favoritos integrado en M5, M8 y M9: el usuario puede marcar FIBRAs con estrella desde cualquier superficie y acceder rapidamente a ellas. Las FIBRAs favoritas aparecen destacadas al inicio de las tablas en M8 y M9. Sin reglas ni umbrales en MVP.
- Centro de Procesos interno `/ops/*` con dashboard operativo, historial, inbox de work items, ejecucion manual y configuracion de schedules.
- `AI_MODE` soportado en `Off` y `Manual`; `Api` queda preparado arquitectonicamente pero no habilitado por defecto en MVP.

### Growth

- M6 Comparador publico entre 2 y 4 FIBRAs con bloques de Mercado, Fundamentales, Distribuciones y Score publico, URL compartible y tolerancia a datos faltantes por celda.
- Guardado y comparticion de comparaciones publicas.
- Exportaciones de comparador, dashboard y reportes operativos.
- Alertas con reglas configurables por metrica, operador y umbral, habilitadas cuando el sistema soporte notificaciones externas por email o canales de colaboracion.
- Notificaciones externas de fallas operativas por email o canales de colaboracion.
- Mejores herramientas de correccion manual para asociacion de noticias y clasificacion de PDFs.
- Mas politicas de score y presets por perfil de inversion.
- Habilitacion operativa de `AI_MODE=Api` con limites de presupuesto y prioridad de colas.

### Vision

- Soporte a multiples proveedores de mercado y enriquecimiento de datos.
- Analitica historica mas profunda y benchmarking sectorial.
- Operacion semi-automatizada de IA por API con limites de gasto y priorizacion dinamica.
- Evolucion del modulo de alertas hacia un centro personal de monitoreo.

### Out of Scope for MVP

- Ejecucion de ordenes, integracion con brokers o custodia.
- Trading automatico o recomendaciones de compra/venta no explicadas.
- Historico transaccional completo del portafolio.
- Calculo fiscal, contable o conciliacion de movimientos.
- Versionado automatico de PDFs repetidos del mismo periodo.
- Notificaciones push o email para alertas personales.

## User Journeys

### UJ-01 Descubrimiento General del Universo

Actor: visitante publico.

Trigger: entra al sitio para entender que esta pasando en el universo de FIBRAs.

Flow:

1. Abre Home y consulta carrusel, top movers, ranking rapido y noticias recientes.
2. Usa el buscador global para localizar una FIBRA por ticker o nombre.
3. Navega a la ficha publica para profundizar en mercado, fundamentales, distribuciones y reportes.
4. Si necesita contraste, agrega emisores al comparador publico.

Outcome: el usuario entiende el estado general del mercado y detecta emisores para analisis mas profundo sin autenticarse.

### UJ-02 Consulta Profunda por FIBRA

Actor: visitante publico o usuario autenticado.

Trigger: quiere entender el estado actual y el contexto historico de una FIBRA especifica.

Flow:

1. Entra a la ficha publica.
2. Revisa encabezado con precio actual, cambio diario, yield y volumen.
3. Consulta bloque de mercado con historico y comparacion vs 52 semanas.
4. Consulta fundamentales vigentes con periodo de origen.
5. Revisa distribuciones, noticias asociadas y reportes oficiales.

Outcome: obtiene una vista consolidada con contexto de periodo y sin tener que recorrer multiples pantallas o fuentes externas.

### UJ-03 Comparacion Publica de Emisores _(Growth — no incluido en MVP)_

Actor: visitante publico.

Trigger: quiere comparar de 2 a 4 FIBRAs para evaluar valor relativo.

Flow:

1. Abre `/comparar`.
2. Selecciona entre 2 y 4 FIBRAs con autocomplete.
3. Revisa resumen rapido, mercado, fundamentales, distribuciones e historico comparativo.
4. Interpreta score y senales si estan habilitados.

Outcome: realiza un analisis estructurado sin autenticacion y sin abrir varias fichas por separado.

### UJ-04 Construccion de Portafolio

Actor: usuario autenticado `User`.

Trigger: quiere cargar sus posiciones actuales al sistema.

Flow:

1. Entra al modulo Portafolio y sube archivo `.xlsx`, `.xls` o `.csv`.
2. El sistema valida columnas, tickers, cantidades, costos y duplicados.
3. Corrige errores, normaliza posiciones y calcula costo promedio ponderado cuando aplica.
4. El portafolio queda activo y el dashboard se habilita.

Outcome: el usuario obtiene una representacion consistente de su portafolio actual sin depender de integraciones con el broker.

### UJ-05 Analisis del Portafolio

Actor: usuario autenticado `User`.

Trigger: quiere entender en segundos el estado de su portafolio y luego profundizar por posicion.

Flow:

1. Abre Dashboard y revisa KPIs generales.
2. Usa multi-sort y filtros para priorizar posiciones.
3. Expande una fila y revisa mercado, fundamentales, distribuciones e impacto en su portafolio.
4. Interpreta badge de senal y tooltip explicativo.

Outcome: puede revisar rendimiento, riesgo y valor relativo de cada posicion desde una sola superficie.

### UJ-06 Identificacion de Oportunidades

Actor: usuario autenticado `User`.

Trigger: quiere saber que hacer ahora con el universo completo o con posiciones existentes.

Flow:

1. Entra a Oportunidades.
2. Revisa ranking del universo completo.
3. Filtra por yield, descuento NAV, LTV o disponibilidad de fundamentales.
4. Ajusta pesos del score.
5. Cambia a vista `Promediar Posicion` para comparar precio actual contra su costo promedio.

Outcome: identifica emisores atractivos para entrar o aumentar posicion con criterios configurables y explicables.

### UJ-07 Seguimiento Personal con Favoritos

Actor: usuario autenticado `User`.

Trigger: quiere seguir FIBRAs concretas sin revisar todo el universo cada vez.

Flow:

1. Marca FIBRAs como favoritas desde M5, M8 o M9 con un icono de estrella.
2. Las FIBRAs favoritas aparecen destacadas al inicio de las tablas en M8 y M9.
3. El usuario accede rapidamente a su seleccion personal sin filtrar manualmente.

Outcome: mantiene un acceso rapido a las FIBRAs de su interes sin necesidad de reglas ni umbrales configurables.

### UJ-08 Ciclo de Nuevo Reporte Financiero

Actor: `AdminOps` (modo Manual, MVP) o sistema (modo Api, Growth).

Trigger: se publica un nuevo reporte financiero trimestral de una FIBRA.

Flow (modo Manual — MVP):

1. `AdminOps` descarga el PDF oficialmente publicado por la FIBRA.
2. Ejecuta el skill externo (Claude Code u otro CLI) pasando el PDF como entrada.
3. El skill extrae campos estructurados y resumen, y llama a `POST /api/v1/ops/fundamentals/import` con el payload JSON.
4. El sistema valida el payload, crea el registro con estado `pendiente` y muestra un preview en la seccion Fundamentales de Ops.
5. `AdminOps` revisa y confirma el registro; el estado cambia a `procesado`.
6. Se actualiza el historico de fundamentales y el ultimo valor vigente por FIBRA.
7. Se recalculan score, senales y vistas afectadas.

En modo Api (Growth): los pasos 1-3 son automaticos mediante deteccion config-driven por FIBRA y llamada al proveedor IA configurado; los pasos 4-7 son identicos.

Outcome: el sistema transforma reportes heterogeneos en datos estructurados y trazables con trazabilidad de fuente y periodo.

### UJ-09 Operacion del Centro de Procesos

Actor: `AdminOps`.

Trigger: necesita monitorear, corregir o ajustar el comportamiento operativo.

Flow:

1. Entra al dashboard operativo.
2. Consulta corridas, backlog, errores y cobertura por pipeline.
3. Ejecuta `Run now`, `Run by ticker/fibra`, `Retry` o `Reprocess`.
4. Ajusta schedules, configuracion PDF por FIBRA y `AI_MODE`.

Outcome: mantiene continuidad operativa, observabilidad y control sin depender de cambios de despliegue.

## Domain Requirements

DR-01. El producto debe tratar a las FIBRAs como un catalogo maestro con ticker unico, metadatos estructurales y configuracion operativa reutilizable por todos los modulos.

DR-02. El producto es una herramienta de analisis y monitoreo; no ejecuta operaciones ni debe presentarse como sistema de trading.

DR-03. Toda metrica fundamental debe conservar periodo de origen, fuente PDF y estado de calidad.

DR-04. El portafolio representa estado actual de posiciones, no historico transaccional ni registro fiscal.

DR-05. El sistema nunca debe asumir frecuencia trimestral de distribuciones; la anualizacion depende del patron real detectado.

DR-06. El sistema nunca debe inventar datos en resúmenes IA ni inferir cifras ausentes como hechos.

DR-07. Las noticias pueden relacionarse con cero, una o varias FIBRAs; la ausencia de relacion explicita no debe bloquear la publicacion de la nota.

DR-08. El score y las senales deben poder operar con datos faltantes sin penalizacion automatica por default, mediante redistribucion proporcional de pesos faltantes.

DR-09. La UI debe mostrar estados `parcial`, `sin datos`, `no evaluable` o degradacion cuando falten insumos; nunca debe romperse por ausencia de datos.

DR-10. El mundo publico, el privado y el operativo deben mantener separacion estricta de acceso y visibilidad.

DR-11. Todos los promedios historicos de metricas fundamentales y de distribuciones, identificados con el prefijo AVG en las vistas del sistema, deben calcularse usando los ultimos 4 periodos disponibles para cada FIBRA, equivalente aproximadamente a un anio de informacion. El valor N=4 es la configuracion por defecto, debe ser modificable desde el Centro de Procesos sin redeploy y aplica de forma uniforme a todas las metricas AVG del sistema.

DR-12. El peso porcentual de cada posicion en el portafolio se calcula con base en el monto invertido en terminos monetarios: `(Titulos x Costo_promedio) / Suma(Titulos_i x Costo_promedio_i)` para todas las posiciones activas del usuario. Una posicion con mayor capital invertido refleja mayor peso, independientemente de su cantidad de CBFIs.

DR-13. El Costo Total Compra de cada posicion incorpora un factor de comision de intermediacion fijo y configurable: `Titulos x Costo_promedio x (1 + factor_comision)`. El factor aplica uniformemente a todas las posiciones, es configurable desde el Centro de Procesos sin redeploy y su valor por defecto debe quedar documentado en la configuracion inicial del sistema. Un cambio de factor no retroactua posiciones existentes; aplica solo a calculos de lectura posteriores al cambio.

DR-14. Para el calculo del Dividend Yield el sistema debe aplicar la siguiente jerarquia de fuente: primero usa el yield declarado en el reporte oficial de Fundamentales si existe un periodo dentro de los ultimos N periodos configurados; si no hay dato de Fundamentales disponible, utiliza las distribuciones provenientes de Yahoo Finance; si ninguna fuente tiene dato, muestra el campo como no disponible sin inventar ni estimar valores. Cuando el yield se muestra en UI debe incluir un indicador visible de la fuente utilizada (reporte oficial o mercado) para que el usuario conozca la confiabilidad del dato.

DR-15. El pipeline de mercado debe ejecutarse unicamente en horario de operacion de la BMV con un margen operativo: de 8:15am a 3:15pm hora Ciudad de Mexico, lunes a viernes en dias habiles. Fuera de ese horario el sistema no ejecuta ciclos de actualizacion de precio. La UI debe distinguir entre dos estados de precio no fresco: `fuera-de-horario` cuando el mercado esta cerrado y el dato corresponde al ultimo precio de cierre valido, y `stale` o `critico` cuando el pipeline fallo durante horario de mercado segun los umbrales definidos en NFR-04.

## Innovation Analysis

IA-01. La combinacion de Home, ficha y comparador crea un recorrido publico completo de descubrimiento sin necesidad de login.

IA-02. El modulo de Fundamentales convierte PDFs no estandarizados en un historico estructurado con trazabilidad por periodo, algo dificil de encontrar en experiencias de inversion minorista.

IA-03. El score configurable permite que el usuario exprese una estrategia propia sin aceptar un ranking opaco impuesto por el sistema.

IA-04. El Dashboard y Oportunidades separan dos preguntas distintas: "como va lo que ya tengo" y "que podria hacer ahora", mejorando claridad de decision.

IA-05. Favoritos funciona como una capa de acceso rapido personal integrada en M5, M8 y M9, sin crear logicas ni modulos paralelos. Las reglas de alertas configurables quedan diferidas a cuando el sistema soporte notificaciones externas.

IA-06. El Centro de Procesos convierte la operacion de datos en una capacidad del producto y no en una caja negra para el equipo.

## Project-Type Requirements

PT-01. La plataforma debe operar como aplicacion web con dos frontends React independientes: sistema principal y centro operativo `/ops/*`.

PT-02. Debe existir una sola superficie backend y un solo despliegue que sirva mundo publico, mundo privado y jobs en background.

PT-03. Debe soportar procesos asincronos para mercado, distribuciones, noticias, PDFs y procesamiento IA con estados persistentes.

PT-04. Debe soportar carga segura de archivos de portafolio en Excel y CSV.

PT-05. Debe ofrecer interfaces responsive para Home, ficha, comparador, portafolio, dashboard, oportunidades y ops.

PT-06. Debe exponer un contrato API documentado para consumo tipado desde frontend.

PT-07. Debe permitir ajustar schedules y configuraciones operativas sin redeploy en la mayor cantidad posible de casos.

PT-08. Debe preservar compatibilidad entre `AI_MODE` manual y `AI_MODE` API sin redisenar la capa funcional.

### Browser Support

PT-09. La experiencia MVP debe estar soportada en las dos versiones estables mas recientes de Chrome, Edge y Safari, y en la version estable mas reciente de Firefox, en desktop y mobile cuando aplique.

### SEO Strategy

PT-10. Home, ficha publica, comparador, noticias y cualquier otra superficie publica indexable deben exponer `title`, `meta description`, URL canonica, participacion coherente en sitemap/robots y estructura semantica suficiente para descubrimiento organico basico, mediante HTML inicial rastreable o prerender equivalente en las rutas publicas aplicables.

### Accessibility Level

PT-11. Las superficies publicas y privadas del MVP deben cumplir como minimo con WCAG 2.1 AA en navegacion por teclado, contraste, foco visible, nombres accesibles y semantica estructural.

## Functional Requirements

### Catalogo y Descubrimiento Publico

FR-01. El sistema debe mantener un catalogo maestro de FIBRAs con ticker unico, nombre completo, nombre corto, mercado, moneda base, sector, pais, estado y configuraciones por emisor. Trace: UJ-01, UJ-02, SC-02.

FR-02. Cada FIBRA debe conservar URLs oficiales de sitio, inversionistas y reportes para alimentar discovery de PDFs y asociacion de noticias. Trace: UJ-02, UJ-08.

FR-03. La Home publica debe mostrar encabezado con busqueda global, carrusel de precios, resumen general del mercado, top movers, ranking rapido y ultimas noticias. Trace: UJ-01, SC-01.

FR-04. El buscador global debe autocompletar por ticker o nombre y navegar a la ficha publica correspondiente; si no existe coincidencia, debe mostrar estado claro de no encontrado. Trace: UJ-01, SC-02.

FR-05. La Home debe mostrar datos cacheados o stale con timestamp visible cuando mercado no este fresco, y debe escalar a degradacion critica cuando la antiguedad supere el umbral documentado. Trace: UJ-01, SC-03, SC-11.

FR-06. La ficha publica de FIBRA debe consolidar los siguientes bloques en una sola pagina: encabezado con precio actual, cambio porcentual diario y volumen; grafica historica de precio con selectores de periodo 1M, 3M, 6M y 1A alimentada por snapshots de M2; bloque de fundamentales vigentes del ultimo periodo disponible; las ultimas 8 distribuciones en orden cronologico inverso medidas en trimestres; las ultimas 10 noticias asociadas a esa FIBRA en orden de publicacion; y un listado de reportes oficiales disponibles con periodo y tipo de reporte. No incluye badge de senal en MVP. Trace: UJ-02.

FR-07. La ficha publica debe mostrar el periodo de origen junto a cada metrica fundamental (ejemplo: "Cap Rate — Q1 2025") y una advertencia visible cuando el ultimo reporte disponible tenga mas de dos periodos de antiguedad. Trace: UJ-02, UJ-08.

FR-08. _(Growth)_ El comparador publico en la ruta `/comparar` debe permitir seleccionar entre 2 y 4 FIBRAs mediante autocomplete por ticker o nombre, y compararlas lado a lado en cuatro bloques de filas: Mercado (precio actual, cambio dia %, AVG 52S, volumen), Fundamentales (ultima informacion, Cap Rate, NAV por CBFI, LTV, Margen NOI, Margen FFO), Distribuciones (distribucion trimestral, yield calculado, yield decretado) y Score publico calculado con perfil Balanceado (pesos iguales entre los cinco componentes de M9) mostrado como referencia neutral sin personalizacion. La seleccion de FIBRAs debe reflejarse en los query params de la URL para permitir compartir la comparacion: `/comparar?fibras=FUNO11,FMTY14,DANHOS13`. Trace: UJ-03.

FR-09. _(Growth)_ El comparador debe tolerar valores faltantes por celda mostrando `—` sin desplazar encabezados de columna ni ocultar el boton de quitar emisor. El usuario puede quitar una FIBRA del comparador en cualquier momento. La vista debe funcionar sin overflow horizontal no intencional en 360px, 768px y 1280px. El modulo es publico y no requiere autenticacion. Trace: UJ-03, SC-11, PT-05.

### Mercado, Noticias y Fundamentales

FR-10. El modulo de Mercado debe exponer Last Price, cambio diario, volumen, promedio de 52 semanas, comparacion vs 52 semanas, historico y distribuciones por FIBRA. Trace: UJ-01, UJ-02, SC-03.

FR-11. El sistema debe recalcular yield anualizado usando frecuencia detectada de distribucion y no debe asumir frecuencia fija. Trace: UJ-02, UJ-06.

FR-12. Si faltan eventos suficientes de distribucion o el proveedor no entrega datos, el sistema debe mostrar `yield` como no disponible sin romper score ni senales. Trace: UJ-02, UJ-05, UJ-06, SC-11.

FR-13. El modulo de Noticias debe ingerir notas cada hora desde Google News RSS usando dos tipos de queries configurables desde el catalogo y desde Ops sin redeploy: queries especificas por FIBRA (ticker y variantes de nombre almacenadas en el catalogo maestro) y queries generales de mercado (ej. "FIBRAs Mexico", "BMV FIBRAs", "sector FIBRA Mexico"). Cada noticia debe normalizarse con titulo, fecha, fuente, URL y snippet, y almacenar estado de procesamiento por item. Trace: UJ-01, UJ-02, SC-07.

FR-14. El sistema debe aplicar un blocklist global de terminos que descarten noticias no relacionadas con FIBRAs inmobiliarias antes de persistirlas. Ejemplos de terminos bloqueados: "fibra optica", "fibra dietetica", "fibra alimentaria", "fibra natural". El blocklist debe ser configurable desde Ops sin redeploy. Adicionalmente el sistema debe eliminar duplicados exactos por URL y duplicados probables por similitud de titulo dentro de una ventana de 24 horas. Trace: UJ-01, SC-07.

FR-15. Cada noticia debe poder asociarse con cero, una o varias FIBRAs mediante coincidencia automatica de ticker o variante de nombre en titulo y snippet, sin intervencion de IA. Una noticia de mercado general puede tener cero asociaciones y debe publicarse igualmente en Home y feed general. Las noticias con asociacion a una FIBRA especifica aparecen adicionalmente en la ficha publica de esa FIBRA. Trace: UJ-01, UJ-02, SC-07, DR-07.

FR-16. Cuando `AI_MODE` sea `Off`, el sistema debe publicar noticias sin resumen usando titulo, fuente, fecha, snippet y enlace original. El MVP arranca con `AI_MODE=Off` como valor inicial del sistema. Trace: UJ-01, SC-07.

FR-17. Cuando `AI_MODE` sea `Manual`, el sistema debe permitir disparar generacion de resumen ejecutivo y, si el resumen falla, publicar la noticia con estado `parcial` conservando titulo, fuente, fecha, snippet y enlace original. Trace: UJ-01, UJ-09, SC-07.

FR-49. El catalogo maestro de cada FIBRA debe almacenar un conjunto de variantes de nombre y terminos de busqueda utilizados para las queries de Google News RSS. Este conjunto debe ser editable desde el Centro de Procesos sin redeploy. Trace: FR-13, UJ-01.

FR-50. La Home publica debe mostrar las 10 noticias mas recientes en orden de fecha de publicacion, sin importar si estan asociadas a una FIBRA especifica o son de mercado general. Trace: UJ-01, FR-03.

FR-18. El modulo de Fundamentales debe soportar tres modos de operacion controlados por `AI_MODE`, todos compartiendo el mismo motor de almacenamiento y display: `Off` almacena PDFs sin procesar; `Manual` el operador dispara el procesamiento mediante un skill externo (Claude Code u otro CLI) que llama al endpoint de importacion; `Api` el pipeline detecta y procesa PDFs automaticamente mediante llamadas a un proveedor de IA configurado. El cambio entre modos no requiere redeploy ni redisenar el flujo de datos. Trace: UJ-08, SC-06.

FR-19. En modo `Manual` el sistema debe exponer un endpoint de importacion de fundamentales que recibe un payload JSON con FIBRA, periodo, campos fundamentales estructurados, resumen y referencia al PDF. El operador revisa y confirma el registro desde Ops antes de que sea visible en M5. Este endpoint es el punto de integracion del skill externo con la base de datos; el contrato de la interfaz queda documentado en la especificacion de arquitectura. Trace: UJ-08, UJ-09.

FR-20. En modo `Api` el sistema debe detectar PDFs nuevos mediante reglas config-driven por FIBRA (URLs, patrones de link, patrones de periodo), descargarlos, llamar al proveedor de IA configurado para extraccion y resumen, y actualizar el historico por periodo. La configuracion de URLs y patrones por FIBRA debe ser editable desde el Centro de Procesos sin redeploy. Trace: UJ-08, SC-06.

FR-21. El sistema debe tratar un PDF como nuevo solo si representa un periodo no registrado para esa FIBRA. Si ya existe un registro del mismo periodo, debe registrarlo como posible actualizacion y dejar el reproceso bajo control manual de `AdminOps`. Trace: UJ-08, UJ-09.

FR-22b. El sistema debe permitir que metricas fundamentales faltantes queden en `null` sin bloquear el procesamiento ni el display de las metricas disponibles. Los campos sin dato se muestran como no disponible en UI segun NFR-08. Trace: UJ-08, SC-11.

FR-22c. El modulo de Fundamentales debe almacenar por cada registro: FIBRA, periodo (formato trimestral), fecha de procesamiento, modo de procesamiento utilizado, referencia al PDF original, estado (pendiente, procesado, parcial, error), campos estructurados extraidos y resumen si esta disponible. Trace: UJ-08, DR-03, NFR-10.

### Portafolio, Dashboard y Oportunidades

FR-22. El modulo de Portafolio debe permitir cargar archivos `.xlsx`, `.xls` o `.csv` con un formato fijo de tres columnas: `Ticker`, `Qty` y `AvgCost`. Los nombres de columna son case-insensitive. No se soportan formatos alternativos ni deteccion automatica de columnas. Trace: UJ-04, SC-04.

FR-23. El unico input de costo aceptado es `AvgCost` (costo promedio por CBFI). El sistema calcula `CostoTotalCompra = Qty x AvgCost x (1 + factor_comision)`. No se acepta costo total como campo de entrada directo. Si el mismo ticker aparece en multiples filas del archivo, el sistema consolida la posicion sumando cantidades y calculando el costo promedio ponderado: `Suma(Qty_i x AvgCost_i) / Suma(Qty_i)`. Trace: UJ-04, SC-04.

FR-24. El sistema debe procesar la carga de forma sincrona y devolver el resultado al usuario en la misma operacion. Si el archivo contiene errores, no se guarda ninguna posicion y se muestra una tabla de errores por fila con: numero de fila, ticker y descripcion del problema. Las reglas de validacion son: ticker debe existir en el catalogo maestro (case-insensitive); Qty debe ser un entero positivo mayor a cero; AvgCost debe ser un numero positivo mayor a cero; el header debe contener exactamente las tres columnas requeridas. Si el ticker no existe pero hay una coincidencia parcial en el catalogo, el sistema puede sugerir el ticker correcto en el mensaje de error. Trace: UJ-04.

FR-25. Cuando el usuario sube un archivo nuevo y ya tiene un portafolio activo, el sistema debe mostrar una confirmacion antes de reemplazar las posiciones existentes. El usuario puede tambien editar posiciones individuales de forma inline directamente en la tabla: los campos editables son `Qty` y `AvgCost`; al confirmar la edicion el sistema recalcula y persiste inmediatamente; al cancelar no se aplica ningun cambio. El usuario puede eliminar una posicion individual con confirmacion. Trace: UJ-04.

FR-43. El sistema debe calcular el porcentaje de portafolio de cada posicion usando el monto invertido como base segun DR-12, de modo que posiciones con mayor capital reflejen mayor peso independientemente de la cantidad de CBFIs. Trace: DR-12, UJ-05.

FR-44. El sistema debe aplicar un factor de comision de intermediacion configurable al calcular el Costo Total Compra de cada posicion segun DR-13. El factor debe ser editable desde el Centro de Procesos sin redeploy y su valor inicial debe quedar documentado en la configuracion operativa del sistema. Trace: DR-13, UJ-04, SC-04.

FR-45. El sistema debe calcular todos los promedios historicos de metricas fundamentales y de distribuciones usando los ultimos 4 periodos disponibles por FIBRA segun DR-11. El valor N debe ser configurable desde el Centro de Procesos sin redeploy. Trace: DR-11, UJ-05.

FR-46. El portafolio debe mostrar exclusivamente posiciones con al menos un titulo activo. Las FIBRAs sin posicion no deben aparecer en la vista de portafolio ni ser incluidas en los KPIs agregados del usuario. Trace: UJ-04, UJ-05.

FR-47. El portafolio y el dashboard privado se presentan en una sola pantalla unificada bajo la ruta `/portafolio`. La pantalla incluye: boton de carga de archivo siempre visible en la parte superior, bloque de KPIs agregados del portafolio, y tabla central que sirve simultaneamente como vista de analisis y gestion de posiciones. No existe una ruta `/dashboard` separada. Trace: UJ-04, UJ-05.

FR-48. La tabla del portafolio debe mostrar por defecto una vista compacta con las siguientes columnas: FIBRA (nombre corto y ticker), Titulos, Costo promedio, Precio actual, Valor de mercado, Plusvalia (%), Ganancia ($), Renta anual y % Portafolio. El usuario puede configurar columnas adicionales mediante un panel de checkboxes agrupados por seccion (Bursatil, Fundamental, Rentabilidad). La tabla soporta multi-sort: el usuario puede ordenar por multiples columnas simultaneamente. El orden y la configuracion de columnas se persisten por usuario. Trace: UJ-05.

FR-53. La edicion de posiciones debe ser inline sobre la tabla. Solo los campos Qty y AvgCost son editables. Al confirmar con Enter o click fuera, el sistema guarda y recalcula la fila inmediatamente. Escape cancela sin guardar. La eliminacion de una posicion requiere confirmacion explicita. Trace: UJ-04, UJ-05.

### Definicion de Metricas Calculadas del Portafolio

La vista principal del portafolio agrupa campos en cuatro bloques. La tabla siguiente define cada campo, su origen y su formula. Los campos marcados con AVG aplican DR-11 (ultimos 4 periodos). El calculo de peso porcentual aplica DR-12. El Costo Total Compra aplica DR-13. Cuando un campo depende de datos de mercado no disponibles, el sistema muestra el estado segun NFR-08 sin romper la vista.

**Bloque: Identidad**

| Campo | Origen | Formula o regla |
|---|---|---|
| CLAVE | Catalogo maestro | Ticker unico |
| FIBRA | Catalogo maestro | Nombre completo del emisor |
| Ultima informacion | Modulo Fundamentales | Periodo del PDF mas reciente con datos disponibles para esa FIBRA. Puede no corresponder al trimestre en curso si no han llegado nuevos reportes. |
| % Portafolio | Calculado | `(Titulos x Costo_promedio) / Suma(Titulos_i x Costo_promedio_i)` — DR-12 |

**Bloque: Informacion Bursatil**

| Campo | Origen | Formula o regla |
|---|---|---|
| Numero de Titulos | Excel cargado | Input directo. Multiples lotes del mismo ticker se consolidan sumando titulos. |
| Costo promedio | Excel cargado | Input directo. Si hay multiples lotes: `Suma(Costo_j x Titulos_j) / Suma(Titulos_j)`. |
| Precio de Mercado | Yahoo Finance | Last price. Refresco cada 15 min. `null` si no disponible. |
| AVG Precio de Mercado 52S | Yahoo Finance | Promedio del precio en las ultimas 52 semanas. `null` si no disponible. |
| Cambio vs dia anterior (%) | Yahoo Finance | Cambio porcentual diario. |
| Volumen | Yahoo Finance | Volumen del dia. |
| Costo Total Promedio | Calculado | `Titulos x Costo_promedio` sin comision. |
| Costo Total Compra | Calculado | `Titulos x Costo_promedio x (1 + factor_comision)` — DR-13. |
| Valor de Mercado | Calculado | `Titulos x Precio_de_Mercado`. `null` si precio no disponible. |

**Bloque: Informacion Fundamental**

Cada metrica fundamental tiene dos versiones: el valor del ultimo periodo disponible y el AVG de los ultimos N periodos segun DR-11. Todos los valores fundamentales conservan el periodo de origen segun DR-03.

| Campo | Origen | Regla |
|---|---|---|
| Cap Rate Implicito / AVG | PDF — Fundamentales | Ultimo periodo disponible / AVG ultimos N periodos |
| NAV por CBFI / AVG | PDF — Fundamentales | Ultimo periodo disponible / AVG ultimos N periodos |
| Distribucion Trimestral por CBFI / AVG | Modulo Distribuciones | Ultimo pago registrado / AVG ultimos N periodos |
| Distribucion Anual por CBFI / AVG | Calculado | `Dist_Trimestral x frecuencia_detectada`. No asume frecuencia fija — DR-05. La version AVG usa AVG_Trimestral con la misma frecuencia detectada. |
| Loan To Value / AVG | PDF — Fundamentales | Ultimo periodo disponible / AVG ultimos N periodos |
| Margen NOI / AVG | PDF — Fundamentales | Ultimo periodo disponible / AVG ultimos N periodos |
| Margen FFO / AVG | PDF — Fundamentales | Ultimo periodo disponible / AVG ultimos N periodos |

**Bloque: Rentabilidad**

| Campo | Formula |
|---|---|
| Portafolio Plusvalia (%) | `(Precio_Mercado - Costo_promedio) / Costo_promedio` |
| AVG Plusvalia (%) | `(AVG_Precio_52S - Costo_promedio) / Costo_promedio` |
| Portafolio Ganancia ($) | `(Precio_Mercado - Costo_promedio) x Titulos` |
| AVG Ganancia ($) | `(AVG_Precio_52S - Costo_promedio) x Titulos` |
| NAV vs Precio Mercado | `(NAV_por_CBFI - Precio_Mercado) / Precio_Mercado`. Valor positivo indica que la FIBRA cotiza con descuento respecto a su NAV. |
| Dividend Yield Calculado | `Distribucion_Anual / Precio_Mercado` |
| AVG Dividend Yield Calculado | `Distribucion_Anual / AVG_Precio_52S` |
| Dividend Yield Decretado | Yield declarado por la FIBRA en su ultimo reporte oficial. Fuente: modulo Fundamentales o Distribuciones. |
| AVG Dividend Yield Decretado | Promedio del yield decretado en los ultimos N periodos — DR-11. |
| AVG Dividend Yield | Promedio del yield (calculado o decretado segun disponibilidad) a lo largo de los ultimos N periodos disponibles — DR-11. |
| Renta Trimestral | `Distribucion_Trimestral x Titulos` |
| AVG Renta Trimestral | `AVG_Distribucion_Trimestral x Titulos` |
| Renta Anual | `Distribucion_Anual x Titulos` |
| AVG Renta Anual | `AVG_Distribucion_Anual x Titulos` |
| Dividendo Ponderado Bruto | `Renta_Anual / Inversion_Total_del_portafolio`. Expresa que fraccion de la inversion total del usuario genera esta posicion en rentas anuales. |

**KPIs agregados del portafolio**

Los siguientes valores resumen el estado global del portafolio del usuario y se calculan sobre el conjunto de todas las posiciones activas.

| KPI | Formula |
|---|---|
| Inversion Total del Portafolio | `Suma(Costo_Total_Compra_i)` — incluye factor de comision segun DR-13 |
| Valor Total del Portafolio | `Suma(Valor_de_Mercado_i)`. Parcial si alguna posicion no tiene precio disponible. |
| Plusvalia Total (%) | `(Valor_Total - Inversion_Total) / Inversion_Total` |
| Ganancia Total ($) | `Valor_Total - Inversion_Total` |
| Rentas Anuales Brutas | `Suma(Renta_Anual_i)` — estimado basado en la distribucion mas reciente de cada posicion |
| Rentas Reales Brutas | `Suma(pagos efectivamente registrados en el historial de distribuciones del usuario)` |
| % Rentas del Portafolio | `Rentas_Anuales_Brutas / Inversion_Total` |

FR-26. La pantalla unificada de portafolio debe mostrar en la parte superior los siguientes KPIs agregados: Inversion Total, Valor Total del Portafolio, Plusvalia Total (%), Ganancia Total ($), Rentas Anuales Brutas estimadas, Rentas Reales Brutas y % Rentas del Portafolio. Trace: UJ-05, SC-05.

FR-27. Cada fila de la tabla de portafolio debe ser expandible. Al expandir una posicion se muestran cuatro secciones: Mi posicion (titulos, costo promedio, valor de mercado, plusvalia en % y $); Mercado (precio actual, cambio %, volumen, AVG 52S, high/low 52S); Fundamentales (Cap Rate, NAV, LTV, NOI, FFO con periodo de origen); Distribuciones (ultimas 4, renta trimestral, renta anual, yield calculado, yield decretado). Trace: UJ-05.

FR-28. Cada posicion en la tabla debe mostrar un badge de senal calculado con base en NAV vs Precio de Mercado: verde cuando el precio cotiza mas del 10% por debajo del NAV; amarillo cuando la diferencia esta entre -10% y +10%; rojo cuando cotiza mas del 10% por encima del NAV; gris cuando no hay dato de NAV disponible. El badge debe incluir un tooltip con la explicacion textual del criterio aplicado. Trace: UJ-05.

FR-29. El modulo de Oportunidades debe presentar dos vistas: universo completo con ranking de todas las FIBRAs activas, y vista Promediar Posicion restringida a las FIBRAs que el usuario posee en su portafolio. Ambas vistas comparten el mismo score pero la segunda agrega el contexto del costo promedio del usuario y un simulador de promedio. Trace: UJ-06, SC-08.

FR-30. El score de oportunidad se compone de cinco componentes con pesos configurables por usuario: Descuento NAV (peso default 30%), Dividend Yield (30%), LTV invertido (20%), Margen NOI (10%) y Precio vs AVG 52S (10%). La normalizacion es por percentil dentro del universo activo en ese momento, no por umbrales absolutos. Si falta el dato de un componente, su peso se redistribuye proporcionalmente entre los componentes disponibles. Para aparecer en el ranking principal una FIBRA debe tener datos suficientes para calcular al menos 3 de los 5 componentes; las FIBRAs que cumplan menos de ese umbral se separan en una seccion secundaria "datos limitados" donde el score se muestra como referencial y se acompana de una advertencia visible. Si una FIBRA no puede calcular ningun componente con precio, se marca como no evaluable y se excluye completamente. Trace: UJ-06, SC-08.

FR-31. El sistema debe ofrecer tres perfiles preconfigurados de pesos: Renta (Yield 50%, NOI 20%, Descuento NAV 20%, LTV 10%), Valor (Descuento NAV 50%, LTV 20%, Yield 20%, AVG 52S 10%) y Conservador (LTV 40%, NOI 30%, Yield 20%, Descuento NAV 10%). El usuario puede usar un perfil como punto de partida y ajustar pesos libremente. La configuracion activa se persiste por usuario. El ranking se recalcula en tiempo real al cambiar pesos, y automaticamente cuando cambian datos de mercado o fundamentales. Trace: UJ-06, SC-08.

FR-51. La vista del universo debe mostrar para cada FIBRA: posicion en el ranking, nombre y ticker, score total (0-100), valores de cada componente, y badge de datos disponibles. Al expandir una fila se muestra el desglose de score con la contribucion de cada componente visualmente. Los filtros disponibles son: solo FIBRAs con fundamentales cargados, yield minimo, LTV maximo, sector y solo FIBRAs con precio activo. Los filtros reducen el universo antes de calcular el ranking. Trace: UJ-06.

FR-52. La vista Promediar Posicion debe mostrar para cada FIBRA del portafolio del usuario: costo promedio de entrada, precio actual, diferencia porcentual y score. Debe incluir un simulador que reciba el numero de titulos adicionales a comprar al precio actual y calcule el nuevo costo promedio ponderado, el nuevo valor total de la posicion y el cambio en plusvalia. El simulador no emite recomendaciones ni señales de compra o venta. Trace: UJ-06.

FR-54. El modulo de Oportunidades debe monitorear la cobertura de precios del universo activo y mostrar una advertencia de "universo degradado" cuando el porcentaje de FIBRAs activas sin precio disponible supere el 30%. Mientras el universo este degradado, el ranking se mantiene visible pero con una nota prominente indicando el porcentaje de FIBRAs sin precio y la hora del ultimo dato valido. Si la cobertura cae por debajo del 50% del universo activo, el ranking debe suspenderse y mostrar un estado explicito de "ranking no disponible por cobertura insuficiente" en lugar de un ranking parcial que pueda inducir a error. El umbral del 30% es configurable desde el Centro de Procesos sin redeploy. Trace: UJ-06, SC-08, SC-11, DR-15.

### Favoritos

FR-32. El sistema debe permitir al usuario marcar cualquier FIBRA como favorita desde M5 (ficha publica), M8 (portafolio) y M9 (oportunidades) mediante un icono de estrella. La preferencia se persiste por usuario. Trace: UJ-07, SC-09.

FR-33. Las FIBRAs marcadas como favoritas deben aparecer destacadas y agrupadas al inicio de las tablas en M8 y M9, antes del resto del listado. No requieren tratamiento especial en M5. Trace: UJ-07.

### Superficie Operativa Interna

FR-35. El Centro de Procesos interno se organiza en cinco secciones: Dashboard (estado global de pipelines y errores recientes), Pipelines (detalle y control de mercado y noticias), Fundamentales (importacion JSON y historial por FIBRA), Catalogo (gestion de FIBRAs) y Configuracion (parametros operativos del sistema). Trace: UJ-09, SC-10.

FR-36. El Dashboard operativo debe mostrar para cada pipeline: estado actual (activo, fallando, pausado), timestamp y duracion de la ultima corrida, conteo de items procesados y errores en la ultima corrida, y los ultimos 5 errores globales con timestamp, pipeline y descripcion. Trace: UJ-09, SC-10.

FR-37. La seccion Pipelines debe mostrar el historial de las ultimas corridas de cada pipeline con detalle: para Mercado (FIBRAs procesadas, tickers con error y causa); para Noticias (items nuevos, duplicados descartados, errores). Cada pipeline debe tener un boton Run now con auditoria del disparador manual. Trace: UJ-09, SC-10.

FR-38. La seccion Fundamentales debe incluir un formulario de importacion que acepta un payload JSON con los campos: fibra_id, period, cap_rate, nav_per_cbfi, ltv, noi_margin, ffo_margin, quarterly_distribution y summary. El sistema valida el JSON, muestra un preview con campos reconocidos y campos faltantes, y el operador confirma antes de persistir. La seccion tambien permite adjuntar el PDF de referencia al registro. El historial de periodos por FIBRA muestra estado (procesado, parcial, error) y permite Reprocess por registro. Trace: UJ-08, UJ-09, FR-19.

FR-39. La seccion Catalogo debe permitir agregar, editar y desactivar FIBRAs. Los campos editables son: ticker, nombre completo, nombre corto, sector, mercado, moneda, estado (activo/inactivo) y variantes de nombre para queries de Google News RSS. La desactivacion es un soft delete que excluye la FIBRA del universo activo sin eliminar su historial. Trace: UJ-09, FR-49.

FR-40. La seccion Configuracion debe permitir editar sin redeploy: commission_factor, avg_periods, blocklist de terminos de noticias, AI_MODE (Off o Manual), y la cadencia de ejecucion de los pipelines de mercado y noticias. Cada cambio queda auditado con actor y timestamp. Trace: UJ-09, PT-07, DR-13, DR-15.

FR-41. El sistema debe mantener estados operativos por item y corrida: detected, pending, processing, processed, partial y error. Estos estados son visibles en la seccion Fundamentales y en el historial de pipelines. Trace: UJ-08, UJ-09.

### Acceso y Separacion

FR-42. El mundo publico debe permanecer sin autenticacion; el mundo privado debe requerir autenticacion; las rutas y acciones de Ops deben restringirse a `AdminOps`. Trace: UJ-04, UJ-05, UJ-07, UJ-09, SC-10.

## Non-Functional Requirements

### Rendimiento y Frescura

NFR-01. La Home publica debe responder en menos de 2 segundos en P95 usando datos cacheados o precargados, medido con telemetria de frontend y backend en ambiente productivo. Trace: SC-01.

NFR-02. El Dashboard privado debe responder en menos de 1 segundo en P95 con datos precalculados, medido con telemetria de backend y tiempos de render del cliente. Trace: SC-05.

NFR-03. El pipeline de mercado debe ejecutarse cada 15 minutos dentro del horario de operacion definido en DR-15 (8:15am a 3:15pm hora Ciudad de Mexico, dias habiles) y mantener un timestamp visible de ultima actualizacion en UI, verificado mediante registros de ejecucion y datos persistidos. El sistema opera con un unico proveedor externo de precios en MVP; si ese proveedor no entrega datos validos durante dos o mas ciclos consecutivos, el sistema debe clasificar el estado de precio como `critico` para las FIBRAs afectadas conforme a NFR-04 y continuar operando con los ultimos datos conocidos. Trace: SC-03, DR-15.

NFR-04. El sistema debe clasificar frescura de mercado con estas reglas aplicadas automaticamente sobre timestamps persistidos: `Fresh` hasta 20 minutos desde la ultima actualizacion; `Stale` mayor a 20 minutos y hasta 6 horas; degradacion critica mayor a 6 horas. Fuera del horario de mercado el sistema muestra el estado `fuera-de-horario` en lugar de `Stale` o `critico` cuando el timestamp corresponde al ultimo precio de cierre valido del dia. Trace: SC-03, SC-11, DR-15.

NFR-05. El pipeline de noticias debe ejecutarse con cadencia default de 1 hora; cualquier cambio realizado desde Ops debe surtir efecto en la siguiente evaluacion programada sin redeploy. En MVP el procesamiento de PDFs de Fundamentales es manual y no tiene schedule automatico; en modo Api (Growth) se puede configurar una cadencia diaria desde Ops. Trace: SC-06, SC-10.

NFR-06. Los snapshots diarios de mercado deben conservarse por 90 dias calendario y seguir consultables durante toda esa ventana. Trace: SC-03.

### Resiliencia, Datos y Almacenamiento

NFR-07. Los PDFs no deben eliminarse automaticamente en MVP; la politica de retencion a largo plazo debe ser definida explicitamente por el operador antes de poner el sistema en produccion y documentada en la configuracion del sistema. No existe un plazo predeterminado: la ausencia de jobs de purge es la garantia minima en MVP. Trace: SC-06.

NFR-08. Toda vista debe tolerar datos faltantes mostrando `—`, `parcial`, `sin datos`, `no evaluable` o advertencias equivalentes sin producir errores fatales de UI, validado por pruebas funcionales sobre casos incompletos. Trace: SC-11.

NFR-09. El sistema debe soportar al menos 30 FIBRAs activas y 5 o mas anos de historico relevante sin redisenar entidades base ni romper calculos existentes, validado con carga de datos de prueba, migraciones evolutivas y pruebas de regresion de calculos. Trace: Product Scope Vision.

NFR-10. Toda entidad relevante de datos debe conservar `fuente`, `captured_at`, `status` y `error_reason` cuando aplique, verificado en contratos de persistencia y validaciones de escritura. Trace: UJ-08, UJ-09.

### Seguridad y Auditoria

NFR-11. El sistema debe proteger mundo privado y ops con autenticacion y autorizacion por roles `User` y `AdminOps`, validado con pruebas positivas y negativas sobre rutas, acciones y endpoints. Trace: SC-10.

NFR-12. Los cambios de schedule, cambios de `AI_MODE`, reprocesos, retries manuales y cambios de configuracion PDF por FIBRA deben quedar auditados con actor, fecha y antes/despues cuando aplique, verificado mediante registro consultable del 100% de esos eventos en Ops o persistencia equivalente. Trace: SC-10.

### Operacion Tecnica y Calidad

NFR-13. El sistema debe ofrecer observabilidad minima con logs estructurados para el 100% de corridas de pipeline, correlation ID por solicitud o job y health checks separados de API, persistencia y pipelines, verificado mediante endpoints operativos y registros consultables por `AdminOps`. Trace: UJ-09.

NFR-14. La API debe mantener un contrato documentado y versionado suficiente para que frontend consuma endpoints sin ambiguedad y detecte cambios incompatibles antes de liberar, verificado en cada version publicada del backend. Trace: PT-06.

NFR-15. Las interfaces publicas, privadas y operativas deben mantener navegacion principal, accion primaria visible y ausencia de overflow horizontal no intencional en 360px, 768px y 1280px, validadas manualmente antes de liberacion de MVP. Trace: PT-05.

NFR-16. La plataforma debe operar bajo un despliegue unico que atienda mundo publico, mundo privado y procesamiento en background dentro del entorno objetivo de hosting compartido, manteniendo idempotencia, exclusion logica de ejecuciones concurrentes y estados persistentes de pipeline, verificado en pruebas operativas. Trace: PT-02, PT-03.
