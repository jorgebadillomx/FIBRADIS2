---
stepsCompleted:
  - 1
  - 2
  - 3
  - 4
  - 5
  - 6
  - 7
  - 8
  - 9
  - 10
  - 11
  - 12
  - 13
  - 14
inputDocuments:
  - docs/req/prd.md
  - docs/req/architecture.md
  - _bmad-output/planning-artifacts/epics.md
status: complete
---

# Especificación de Diseño UX — FIBRADIS

**Autor:** Jorge
**Fecha:** 2026-05-15

---

<!-- El contenido de diseño UX se irá agregando secuencialmente a través de los pasos del flujo colaborativo -->

## Experiencia de Usuario Central

### Experiencia Definitoria

El momento de mayor valor en FIBRADIS es cuando un usuario registrado abre la plataforma, ve el estado de su portafolio con señales actualizadas y puede responder en segundos: **"¿Hay alguna FIBRA que valga la pena comprar hoy?"**

El sistema responde con dos dimensiones de oportunidad:
- **Oportunidad de mercado**: precio actual vs. promedio de 52 semanas — ¿está barata respecto a su historia reciente?
- **Oportunidad personal**: precio actual vs. mi costo promedio de entrada — ¿me conviene promediar?

El score de oportunidades es una herramienta de priorización y exploración, no una señal de convicción. El usuario toma la decisión final; el sistema le reduce el tiempo de búsqueda.

### Estrategia de Plataforma

- Web responsive — sin app nativa en MVP
- **Escritorio (1280px)**: pantalla primaria para portafolio y oportunidades — datos densos requieren espacio horizontal
- **Tablet (768px)**: soporte completo con layout adaptado
- **Móvil (360px)**: Home y ficha pública totalmente funcionales; portafolio accesible con experiencia condensada
- Sin modo offline — los datos son en tiempo real por naturaleza
- Mouse/teclado primario; touch secundario
- El usuario conoce hojas de cálculo: FIBRADIS debe sentirse como "mi hoja de cálculo, pero con precios en tiempo real y señales que yo no puedo calcular manualmente"

### Interacciones que Deben Sentirse Sin Fricción

- **Búsqueda global**: autocompletado por ticker o nombre, resultado en ≤ 1 clic
- **Carga de portafolio**: un archivo (igual que exportar desde cualquier broker), validación inmediata, datos listos al instante — sin formularios fila por fila
- **Edición de posición**: doble clic sobre el dato → editar → Enter, sin modales
- **Score en tiempo real**: mover un slider recalcula el ranking visiblemente sin botón "aplicar"
- **Marcar favorita**: un clic en estrella, efecto inmediato en todas las superficies
- **Salto portafolio → oportunidades**: desde cualquier posición del portafolio, llegar a su score y al simulador de promediado en ≤ 2 clics

### Momentos Críticos de Éxito

1. **Decisión de compra asistida**: el usuario abre FIBRADIS y en < 30 segundos identifica cuál FIBRA tiene el mejor descuento respecto a 52 semanas o el mejor argumento para promediar
2. **Primera carga de portafolio**: sube el mismo archivo que usaba en Excel y al instante ve su plusvalía total y las señales NAV — el momento "esto hace lo que mi hoja no puede"
3. **Score como filtro personal**: configura su perfil de inversión y la FIBRA que intuitivamente considera buena sube al top del ranking — el score confirma o desafía su tesis
4. **Confianza en el dato de mercado**: el precio dice "Fresh · hace 3 min" — el usuario sabe que puede actuar con ese número

### Principios de Experiencia

1. **La decisión de compra es la métrica de éxito** — No basta mostrar datos; el flujo portafolio → score → "¿compro hoy?" debe ser el camino de menor resistencia de la plataforma.
2. **Portafolio y Oportunidades son una experiencia continua** — Las señales del portafolio (badge NAV, diferencia vs. promedio) deben llevar naturalmente hacia la vista de Oportunidades; son dos pantallas de un mismo flujo de decisión.
3. **Datos con contexto, no datos crudos** — Cada métrica lleva su estado (frescura, período de origen, señal de calidad). Un precio sin contexto temporal no sirve para decidir.
4. **Densidad escaneable** — Información financiera organizada en jerarquías visuales claras; el dato clave visible en segundos, sin scroll obligatorio.
5. **Transparencia sobre incertidumbre** — Cuando los datos faltan o están degradados, comunicarlo directamente. Nunca enmascarar limitaciones con ceros o silencio.
6. **Acceso progresivo sin fricción** — El visitante recibe valor real sin registrarse. El registro desbloquea la herramienta de decisión personal. No bloquear el mundo público.

---

## Respuesta Emocional Deseada

### Objetivos Emocionales Primarios

**Principal: Confianza + Claridad.** El usuario debe sentirse capaz de tomar una decisión de inversión informada. No abrumado por la densidad de datos financieros, no inseguro por precios sin contexto. La sensación de que sabe exactamente cómo está parado y qué opciones tiene.

**Secundario: Eficiencia.** El usuario encontró lo que necesitaba rápido — sin buscarlo en varios lugares ni exportar a Excel para calcular algo.

### Mapa del Viaje Emocional

| Etapa | Emoción objetivo |
|-------|-----------------|
| Primer contacto (visitante) | Curiosidad → "esto tiene todo lo que busco en un solo lugar" |
| Home pública | Orientación rápida → entiendo el mercado sin esfuerzo |
| Ficha pública | Profundización ordenada → datos jerarquizados, no un dump |
| Login + carga de portafolio | Alivio → "ya no necesito mi Excel" |
| Ver KPIs del portafolio | Claridad → sé exactamente cuánto gané/perdí y en qué |
| Ver score de oportunidades | Confianza + Control → "el sistema me ayuda a priorizar, yo decido" |
| Tomar una decisión | Seguridad → actué con información completa |
| Dato degradado / faltante | Honestidad aceptada → "me dice que no tiene el dato, lo entiendo, sigo" |

### Micro-Emociones

- **Confianza vs. Escepticismo** → el indicador de frescura (Fresh/Stale/fuera-de-horario) junto al precio elimina la duda sobre si el número es válido para actuar
- **Claridad vs. Confusión** → jerarquía visual en ficha y portafolio: el número grande primero, el contexto después, el detalle en expansión bajo demanda
- **Eficiencia vs. Frustración** → acciones primarias siempre en el viewport; carga por archivo en lugar de formulario manual fila por fila
- **Seguridad vs. Ansiedad** → el score siempre va acompañado de su desglose; el simulador de promediado lleva disclaimer explícito que reduce la presión de decisión
- **Descubrimiento vs. Aburrimiento** → la Home pública engancha al visitante con carrusel de precios, top movers y noticias sin abrumarlo

### Emociones a Evitar Activamente

- **Sobrecarga cognitiva** — data dump sin jerarquía visual
- **Desconfianza** — precios desactualizados mostrados sin aviso de frescura
- **Ansiedad por datos faltantes** — ceros engañosos en lugar de `—` o `parcial`
- **Frustración** — flujos de 3+ pasos para acciones frecuentes (favorita, editar posición, ver simulador)
- **Presión de compra** — el score debe sentirse como explorador, no como señal urgente de acción

### Implicaciones de Diseño por Emoción

- **Confianza** → indicadores de frescura junto al precio; timestamps en cada métrica fundamental; warnings de datos degradados como elementos de diseño integrado, no como mensajes de error
- **Claridad** → escala tipográfica financiera: número grande · unidad/contexto mediano · estado/badge pequeño al lado
- **Eficiencia** → la acción primaria de cada pantalla es visible sin scroll en cualquier breakpoint
- **Seguridad** → desglose del score accesible en 1 clic; disclaimer del simulador en texto visible pero no intrusivo
- **Descubrimiento** → la ficha pública progresa de lo más relevante (precio, gráfica) a lo más profundo (fundamentales, distribuciones) — no todo al mismo nivel visual

---

## Resumen Ejecutivo

### Visión del Proyecto

FIBRADIS es una plataforma especializada en el análisis de FIBRAs mexicanas (REITs del BMV) que combina datos de mercado en tiempo real, noticias, fundamentales financieros y herramientas de análisis personal de inversión. Opera bajo un modelo freemium: mundo público de acceso libre y mundo privado de suscripción con portafolio, oportunidades y favoritos.

### Usuarios Objetivo

**Visitante público (UJ-01/02):** Persona interesada en el mercado de FIBRAs mexicanas que explora precios, noticias y el perfil de emisoras específicas. Nivel financiero medio. Accede desde escritorio y móvil. No requiere registro.

**Inversor registrado (UJ-04/05/06/07):** Inversionista individual en FIBRAs que lleva registro activo de su portafolio y busca oportunidades de entrada o promediado de posición. Conocimiento financiero medio-alto, orientado a datos. Suscriptor de pago.

**AdminOps (UJ-08/09):** Operador interno del equipo de FIBRADIS que procesa fundamentales financieros, monitorea pipelines de datos y configura el sistema. Usuario de herramientas de back-office.

### Retos Clave de Diseño

1. **Densidad de datos financieros en ficha pública:** precio + gráfica + fundamentales + distribuciones + noticias + reportes en una sola pantalla — jerarquizar sin abrumar, especialmente en 360px.
2. **Comunicación de estados de datos degradados:** cuatro estados de frescura de precio y fundamentales parciales o nulos deben transmitir confianza o incertidumbre sin romper el flujo visual.
3. **Tabla de portafolio compleja:** filas expandibles con 4 secciones, multi-sort, columnas configurables y edición inline — coherente en escritorio y móvil.
4. **Score configurable en tiempo real:** sliders de peso con recálculo inmediato del ranking requieren un patrón de interacción que haga evidente la causa-efecto.

### Oportunidades de Diseño

1. **Indicadores de frescura como diferenciador de confianza:** el sistema Fresh/Stale/fuera-de-horario/crítico puede posicionarse como un estándar de transparencia de datos que otros no ofrecen.
2. **Portafolio como hub de análisis personal:** vista unificada `/portafolio` con KPIs + tabla expandible — la pantalla más poderosa si logra hacer escaneable información muy densa.
3. **Score explicable como herramienta educativa:** el desglose visual de 5 componentes enseña al usuario a evaluar FIBRAs — diferenciándose de plataformas que solo muestran un número.

---

## Análisis de Patrones UX e Inspiración

### Productos de Referencia

**TradingView** — Referencia en visualización de datos financieros en tiempo real
- Indicadores de estado del mercado (abierto/cerrado) siempre visibles junto al precio
- Gráficas de velas con superposición de indicadores técnicos — densidad bien resuelta
- Tabla de screener con columnas configurables y ordenamiento multi-criterio
- *Adoptar*: tratamiento del indicador de frescura de precio, layout de gráfica + datos laterales

**Wealthsimple** — Referencia en portafolio personal claro y emocionalmente legible
- KPIs de portafolio en superficie sin scroll: valor total, retorno total, retorno del día
- Jerarquía tipográfica financiera: número grande primero, contexto porcentual secundario
- Código de color verde/rojo aplicado con consistencia para plusvalía/minusvalía
- *Adoptar*: jerarquía de KPIs en la vista `/portafolio`, tratamiento de plusvalía/minusvalía

**Linear** — Referencia en edición inline y densidad de información en tablas
- Doble clic sobre cualquier celda activa edición in situ — sin modal, sin navegación
- Filas expandibles que revelan detalle sin perder el contexto de la lista
- *Adoptar*: patrón de edición inline para posiciones del portafolio (cantidad, costo promedio)

**Morningstar** — Referencia en fundamentales financieros y datos comparativos
- Ficha de instrumento con secciones colapsables por tipo de dato
- Ratings con desglose de componentes — el score no es solo un número, tiene dimensiones
- Datos históricos de dividendos presentados en tabla + gráfica de barras
- *Adoptar*: estructura de ficha pública, desglose del score de oportunidad

### Patrones UX Transferibles

| Patrón | Origen | Aplicación en FIBRADIS |
|--------|--------|------------------------|
| Indicador de frescura junto al precio | TradingView | Badge Fresh/Stale/fuera-de-horario en Header de ficha y portafolio |
| KPIs en tarjetas sobre la tabla | Wealthsimple | Fila de KPIs fija en `/portafolio`: valor total, plusvalía, rendimiento |
| Edición inline en tabla | Linear | Doble clic en cantidad o costo promedio → editar → Enter |
| Score con desglose en 1 clic | Morningstar | Score numérico con barra desplegable de 5 componentes |
| Columnas configurables en screener | TradingView | Selector de columnas en tabla de portafolio y de oportunidades |
| Navegación por secciones en ficha | Morningstar | Anclas: Precio · Fundamentales · Distribuciones · Noticias |

### Anti-Patrones a Evitar

- **Bloomberg Terminal**: densidad máxima sin jerarquía — todo tiene el mismo peso visual → en FIBRADIS el número clave debe ser siempre el más grande
- **Portal BMV**: tablas sin estados de frescura, sin indicadores de calidad de dato → todos los precios en FIBRADIS llevan su estado
- **Tab hell**: contenido dividido en 8+ tabs sin conexión visual → FIBRADIS usa anclas dentro de una sola página de scroll
- **Modales para acciones frecuentes**: abrir un modal para editar un valor o marcar favorito → todas las acciones frecuentes son inline o de 1 clic
- **Números sin contexto temporal**: precio sin timestamp o badge de frescura → regla de diseño: ningún precio aparece sin su estado

### Estrategia de Inspiración

| Nivel | Referente | Descripción |
|-------|-----------|-------------|
| Adoptar directamente | TradingView · Wealthsimple | Tratamiento del precio en tiempo real y KPIs de portafolio |
| Adaptar al contexto | Linear · Morningstar | Edición inline y desglose de score — ajustados a datos financieros de FIBRAs |
| Evitar explícitamente | Bloomberg · Portal BMV | Densidad sin jerarquía, precios sin estado de frescura |

---

## Sistema de Diseño

### Decisión: shadcn/ui + Tailwind CSS

**Selección confirmada por arquitectura.** El documento de arquitectura fija `shadcn@latest init` en ambos frontends Vite (`src/Web/Main` y `src/Web/Ops`). No es una decisión abierta del diseño UX — es una restricción técnica ya aprobada.

### Justificación UX

| Criterio | Beneficio para FIBRADIS |
|----------|------------------------|
| Componentes headless personalizables | Tabla de portafolio, sliders de score y badges de frescura requieren variantes que no existen en librerías opinionadas |
| Tailwind CSS | Escala tipográfica financiera (number-lg + context-sm) implementable con clases utilitarias consistentes |
| Accesibilidad integrada (Radix UI) | Base WCAG 2.1 AA para teclado, foco, ARIA — cumple NFR sin esfuerzo adicional |
| Consistencia Main ↔ Ops | Mismo design token base en los dos frontends — un solo CLAUDE.md de estilos |
| Vite-native | Treeshaking agresivo; solo los componentes usados se incluyen en el bundle |

### Tokens de Diseño

**Paleta de color:**
- `--color-positive`: verde para plusvalía / precio subiendo / Fresh
- `--color-negative`: rojo para minusvalía / precio bajando / Stale-crítico
- `--color-neutral`: gris para sin cambio / datos parciales / fuera-de-horario
- `--color-accent`: azul FIBRADIS para acciones primarias y score destacado
- `--color-surface-elevated`: fondo de tarjetas KPI y filas expandidas

**Escala tipográfica financiera:**
```
number-hero   → 2.5rem / 700  — valor total de portafolio, precio principal en ficha
number-large  → 1.5rem / 600  — KPIs de portafolio, precio en tabla de oportunidades
number-base   → 1rem / 500    — columnas de tabla, valores en fila expandida
label-meta    → 0.75rem / 400 — badge de frescura, ticker, período de origen
```

**Espaciado:** escala 4px base (Tailwind default) — `gap-2`, `p-4`, `gap-6` son los valores más frecuentes en tablas financieras densas.

### Componentes Base del Sistema

| Componente | Uso principal | Variantes clave |
|------------|--------------|-----------------|
| `DataTable` | Portafolio, Oportunidades, Mercado | expandible / compacto / con-sort |
| `Freshnessbadge` | Junto a todo precio en tiempo real | Fresh / Stale / Fuera-horario / Crítico |
| `KpiCard` | Fila de KPIs en portafolio | positivo / negativo / neutro |
| `ScoreBar` | Score de oportunidad con desglose | inline / expandido |
| `InlineEditor` | Edición de posición en tabla | texto / número / moneda |
| `FibrasSearch` | Búsqueda global por ticker o nombre | header / modal |
| `PriceHeader` | Encabezado de ficha pública | con-badge / sin-badge |
| `ExpandableRow` | Fila de portafolio expandida | 4 secciones: mercado / fundamentales / distribuciones / portafolio |

---

## Inventario de Pantallas

### Frontend Principal (`src/Web/Main`)

#### Mundo Público (acceso anónimo)

| ID | Ruta | Pantalla | Descripción |
|----|------|----------|-------------|
| S-01 | `/` | Home pública | Carrusel de precios, top movers, ranking rápido, noticias recientes, búsqueda global |
| S-02 | `/mercado` | Mercado | Tabla completa del universo FIBRA con last price, cambio, yield, volumen |
| S-03 | `/catalogo` | Catálogo | Lista de emisores con metadatos estructurales y filtros |
| S-04 | `/noticias` | Noticias | Feed de noticias asociadas a FIBRAs con filtros por emisor |
| S-05 | `/fibras/:ticker` | Ficha pública | Precio + gráfica + fundamentales + distribuciones + noticias + reportes |
| S-06 | `/login` | Inicio de sesión | Formulario de autenticación |
| S-07 | `/registro` | Registro | Alta de cuenta con plan de suscripción |

#### Mundo Privado (requiere autenticación + suscripción)

| ID | Ruta | Pantalla | Descripción |
|----|------|----------|-------------|
| S-08 | `/portafolio` | Portafolio / Dashboard | KPIs agregados + tabla expandible de posiciones + carga de archivo |
| S-09 | `/oportunidades` | Oportunidades | Ranking del universo con score configurable + vista promediar posición |
| S-10 | `/favoritos` | Favoritos | Acceso rápido a FIBRAs marcadas (integrado como filtro en S-08 y S-09) |

#### Pantallas de soporte

| ID | Ruta | Pantalla | Descripción |
|----|------|----------|-------------|
| S-11 | `/perfil` | Perfil de usuario | Configuración de cuenta y preferencias de score |
| S-12 | `/suscripcion` | Suscripción | Estado del plan, upgrade/downgrade |

### Frontend Ops (`src/Web/Ops`)

| ID | Ruta | Pantalla | Descripción |
|----|------|----------|-------------|
| S-13 | `/ops` | Dashboard operativo | Estado de pipelines, jobs recientes, alertas de falla |
| S-14 | `/ops/mercado` | Operaciones de mercado | Historial de corridas, Run now por ticker, Retry |
| S-15 | `/ops/noticias` | Operaciones de noticias | Inbox de work items, reprocess, asociación manual |
| S-16 | `/ops/fundamentales` | Operaciones de fundamentales | PDFs detectados, estado de extracción, confirmación JSON |
| S-17 | `/ops/distribuciones` | Operaciones de distribuciones | Historial de distribuciones por FIBRA, corrección manual |
| S-18 | `/ops/configuracion` | Configuración | Schedules, AI_MODE, fuentes por emisor |

---

## Arquitectura de Navegación

### Navegación Principal — Frontend Main

```
Header fijo (todas las rutas)
├── Logo / Home
├── Menú público: Mercado · Catálogo · Noticias
├── Búsqueda global (autocomplete siempre visible)
└── Auth: [Iniciar sesión] o [Portafolio · Oportunidades · Avatar]

Navegación privada (visible solo autenticado)
├── Portafolio   → /portafolio
└── Oportunidades → /oportunidades
```

**Regla de diseño:** el menú privado (Portafolio / Oportunidades) no es un menú lateral — son items del header con el mismo peso visual que el menú público. La separación de "mundos" es por acceso, no por layout.

### Navegación Intra-Pantalla — Ficha Pública (S-05)

```
Header de ficha (sticky)
├── Ticker + nombre + estrella favorito
├── Precio · cambio · FreshnessBADGE
└── Anclas: Mercado · Fundamentales · Distribuciones · Noticias · Reportes

Cuerpo (scroll)
├── Sección: Mercado (gráfica + métricas de mercado)
├── Sección: Fundamentales (tabla por período)
├── Sección: Distribuciones (histórico + yield)
├── Sección: Noticias asociadas
└── Sección: Reportes oficiales
```

### Flujo de Decisión Principal (portafolio → oportunidad)

```
/portafolio
├── KPI "Mejor oportunidad" → link directo a /oportunidades?view=promediar
├── Badge NAV en fila → tooltip con señal + link a /fibras/:ticker
└── Acción "Ver oportunidades" → /oportunidades

/oportunidades
├── Score de FIBRA → desglose expandible
├── "Ver ficha" → /fibras/:ticker
└── "Simular promediado" → modal inline (no modal de pantalla completa)
```

**Regla de diseño:** el flujo portafolio → oportunidades nunca debe requerir más de 2 clics. Los vínculos de acción deben ser visibles sin scroll en el viewport de 1280px.

### Navegación Ops — Frontend Ops

```
Sidebar fijo izquierdo
├── Dashboard (inicio)
├── Mercado
├── Noticias
├── Fundamentales
├── Distribuciones
└── Configuración
```

La navegación de Ops es un sidebar clásico — densidad operativa, no exploración.

---

## Diseños Clave de Pantalla

### S-01 — Home Pública

**Objetivo:** capturar al visitante en < 10 segundos. Debe responder "¿qué está pasando en el mercado de FIBRAs hoy?"

**Layout (1280px):**
```
[Header con búsqueda global prominente]
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
[Carrusel de precios — scroll horizontal de FIBRAs activas]
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
[Top Gainers · Top Losers · Mayor Volumen]  [Noticias recientes — 3 items]
     (tabla 3-columnas, 5 filas)              (lista con timestamp y fuente)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
[Ranking rápido del universo — tabla completa con paginación]
```

**Reglas de diseño:**
- El carrusel de precios lleva FreshnessBADGE por ítem — nunca precio sin estado
- Top movers muestra variación % del día con color positivo/negativo
- La búsqueda global es el CTA principal; siempre visible sin scroll
- Sin login wall en esta pantalla — acceso total

**Layout (360px):**
```
[Header con búsqueda]
[Carrusel de precios — scroll horizontal]
[Noticias — lista vertical 3 items]
[Top movers — tabs: Gainers / Losers]
[Ranking — tabla compacta]
```

---

### S-05 — Ficha Pública (:`ticker`)

**Objetivo:** vista consolidada de una FIBRA. Responde "¿qué pasa con esta FIBRA?"

**Layout (1280px):**
```
[Sticky header]
Ticker · Nombre · ★             Anclas: Mercado · Fund. · Dist. · Noticias · Reportes
$XX.XX  +1.23%  [Fresh · hace 5 min]

[Sección: Mercado]
[Gráfica histórica — 60% ancho] [Métricas: 52W high/low, vol, yield — 40%]

[Sección: Fundamentales]
Período vigente · Fuente (con badge de estado)
[Tabla: P/FFO · Cap Rate · LTV · NOI · Ocupación]
[Acordeón: historial por período]

[Sección: Distribuciones]
[Gráfica de barras: distribuciones 12 meses] [Tabla detalle]

[Sección: Noticias]
[Lista de noticias asociadas — con fecha y fuente]

[Sección: Reportes]
[Lista de PDFs descargables — con período y fecha de detección]
```

**Reglas de diseño:**
- El FreshnessBADGE del precio es el elemento de mayor confianza — siempre junto al número
- Los fundamentales muestran período de origen — nunca un número sin contexto temporal
- Si no hay fundamentales disponibles: `— No disponible · Período [Q/YYYY]` — nunca `0`
- La estrella de favorito es 1-clic desde el header sticky

---

### S-08 — Portafolio / Dashboard

**Objetivo:** hub de análisis personal. Responde "¿cómo estoy y qué conviene hacer?"

**Layout (1280px):**
```
[Fila de KPIs — sticky o muy arriba del scroll]
Valor Total   Plusvalía Total   Rendimiento %   Posiciones   FIBRAs favoritas↑

[Controles de tabla]
[Buscar posición] [Columnas] [Multi-sort] [Favoritas primero ☑] [Cargar archivo]

[Tabla de portafolio]
Ticker  Nombre  Cantidad  Costo Prom  Precio Act  Plusvalía  %  Badge NAV  ▷
   ▼ FILA EXPANDIDA (4 columnas de secciones)
   [Mercado: P vs 52W, yield]  [Fund.: P/FFO, LTV]  [Distrib.: yield anual]  [Mi posición: pesos invertidos, % del portafolio]

[Bottom bar cuando hay posición seleccionada]
"Ver en Oportunidades →"
```

**Reglas de diseño:**
- Los KPIs siempre visibles sin scroll en 1280px — son la razón principal de abrir la pantalla
- Edición inline: doble clic en `Cantidad` o `Costo Promedio` → campo editable → Enter guarda
- El badge NAV lleva tooltip con explicación de la señal (no solo un color)
- "Favoritas primero" es un toggle que reordena la tabla inmediatamente
- La carga de archivo reemplaza el portafolio con confirmación explícita

**Layout (360px):**
```
[KPIs en 2x2 grid — scroll si necesario]
[Tabla compacta: Ticker · Precio · Plusvalía%]
[Tap en fila → sheet bottom con detalle]
```

---

### S-09 — Oportunidades

**Objetivo:** responder "¿qué FIBRA conviene comprar hoy?"

**Layout (1280px):**
```
[Tabs: Universo completo | Promediar posición]

[Tab: Universo completo]
[Sliders de peso — accordion colapsable]
  Descuento 52W [====●====] 30%   Yield [===●=====] 25%   P/FFO [====●====] 20%   ...

[Tabla de oportunidades — recalcula en tiempo real con sliders]
# Rank  Ticker  Nombre  Score  Descuento52W  Yield  P/FFO  LTV  Badge  ★  →Ficha

[Tab: Promediar posición]
[Solo FIBRAs del portafolio del usuario]
Ticker  Costo Prom Mío  Precio Act  Diferencia%  Score  Señal  →Simular

[Panel de simulación — inline debajo de fila seleccionada]
Si compro [N] títulos más a $XX.XX:
  Nuevo costo promedio: $XX.XX
  Nuevo rendimiento potencial: X%
  [Disclaimer: simulación informativa, no recomendación de inversión]
```

**Reglas de diseño:**
- Los sliders de peso recalculan el ranking sin botón "aplicar" — causa y efecto visible
- El tab "Promediar posición" solo aparece si el usuario tiene portafolio cargado
- El simulador de promediado es inline — no modal de pantalla completa
- El disclaimer del simulador es texto visible pero no intrusivo (gris claro, tamaño 12px)
- Score siempre con desglose en 1 clic (icon de info → tooltip con tabla de componentes)

---

### S-13 — Dashboard Operativo (Ops)

**Objetivo:** dar al AdminOps visibilidad del estado del sistema en < 30 segundos.

**Layout (1280px):**
```
[Sidebar izquierdo fijo — íconos + labels]

[Área principal]
[4 KPI cards: jobs ejecutados hoy · fallos · duración promedio · últimas 24h]

[Tabla de pipelines activos]
Pipeline  Último run  Estado  Duración  Próximo run  Acciones [Run now] [Ver historial]

[Feed de eventos recientes — cronológico inverso]
[icon estado] Pipeline · Ticker · Duración · Resultado · [Retry si falló]
```

---

## Inventario de Componentes

### Componentes Compartidos (Main + Ops)

| Nombre | Descripción | Props clave |
|--------|-------------|-------------|
| `AppHeader` | Header principal con búsqueda y nav | `isAuthenticated`, `user` |
| `FreshnessBadge` | Badge de estado de precio | `status: fresh\|stale\|off-hours\|critical`, `lastUpdated` |
| `DataTable` | Tabla financiera base con sort | `columns`, `data`, `expandable`, `onSort` |
| `ExpandableRow` | Fila expandible de portafolio | `ticker`, `sections: TabContent[]` |
| `KpiCard` | Tarjeta de métrica con estado | `label`, `value`, `change`, `sentiment` |
| `FibrasSearch` | Autocomplete de FIBRAs | `onSelect`, `placeholder` |
| `StarButton` | Toggle de favorito | `isFavorite`, `onChange`, `fibra` |
| `EmptyState` | Estado vacío consistente | `icon`, `title`, `description`, `action` |
| `DegradedDataCell` | Celda para dato faltante/parcial | `state: missing\|partial\|stale`, `tooltip` |

### Componentes del Mundo Privado

| Nombre | Descripción | Props clave |
|--------|-------------|-------------|
| `PortfolioUpload` | Zona de carga drag & drop | `onFileSelect`, `onValidate`, `accept` |
| `InlineEditor` | Editor inline en tabla | `value`, `type: number\|currency`, `onSave` |
| `ScoreBar` | Barra de score con desglose | `score`, `components[]`, `expandable` |
| `ScoreSliders` | Sliders de peso configurable | `weights`, `onChange` (debounced) |
| `PromediadorPanel` | Simulador de promediado | `position`, `currentPrice`, `onClose` |
| `NavBadge` | Badge NAV con tooltip | `signal`, `explanation` |

### Componentes Ops

| Nombre | Descripción | Props clave |
|--------|-------------|-------------|
| `PipelineCard` | Card de pipeline con acciones | `pipeline`, `onRunNow`, `onRetry` |
| `JobHistoryTable` | Tabla de historial de jobs | `jobs`, `filters` |
| `WorkItemInbox` | Inbox de items pendientes | `items`, `onProcess`, `onReject` |
| `AiModeToggle` | Switch de AI_MODE | `currentMode`, `onChange`, `requiresConfirm` |

---

## Patrones de Interacción

### PI-01 — Edición Inline en Tabla de Portafolio

**Trigger:** doble clic sobre celda `Cantidad` o `Costo Promedio`

**Flujo:**
1. La celda se convierte en `<input>` con el valor actual preseleccionado
2. El resto de la tabla continúa siendo legible (no blur ni dim)
3. `Enter` → guarda, recalcula KPIs, regresa a modo lectura
4. `Escape` → cancela sin cambio
5. `Tab` → pasa a la siguiente celda editable

**Validación:** errores en la misma celda (borde rojo + tooltip), sin toast ni modal. El botón guardar no existe — Enter es la acción.

---

### PI-02 — Score con Sliders en Tiempo Real

**Trigger:** mover cualquier slider en el panel de pesos

**Flujo:**
1. El valor del slider se actualiza visualmente en tiempo real (mousemove/touchmove)
2. Los pesos restantes se redistribuyen automáticamente para sumar 100%
3. La tabla de oportunidades recalcula el ranking con debounce de 300ms
4. Los cambios de posición en el ranking se animan suavemente (transición CSS)
5. El botón "Guardar configuración" persiste los pesos — no guarda automáticamente

**Regla:** el usuario debe ver el efecto de sus cambios antes de decidir guardarlos.

---

### PI-03 — Favorito de FIBRA

**Trigger:** clic en ★ desde ficha pública (S-05), tabla de portafolio (S-08), tabla de oportunidades (S-09)

**Flujo:**
1. El ícono cambia de estado visualmente de forma inmediata (optimistic update)
2. La petición al servidor se hace en background
3. Si falla: el ícono revierte con toast de error mínimo
4. En S-08 y S-09: si "Favoritas primero" está activo, la fila se reordena al inicio al marcar

---

### PI-04 — Carga de Portafolio por Archivo

**Trigger:** clic en "Cargar portafolio" o drag & drop en la zona designada

**Flujo:**
1. El usuario selecciona o arrastra un `.xlsx`, `.xls` o `.csv`
2. Validación inmediata del lado cliente: columnas requeridas, tickers reconocidos, valores numéricos
3. Errores de validación: listados claramente con número de fila y descripción del problema
4. Sin errores: preview de posiciones normalizadas antes de confirmar
5. Confirmación → el portafolio se reemplaza; los KPIs y la tabla se actualizan al instante
6. Si ya existe un portafolio: mensaje de confirmación antes de reemplazar

---

### PI-05 — Estado de Datos Degradados

**Regla universal:** ningún precio o dato financiero aparece sin su estado de calidad.

| Estado | Tratamiento Visual |
|--------|-------------------|
| `Fresh` | FreshnessBADGE verde · "hace N min" |
| `Stale` | FreshnessBADGE amarillo · "hace N h" |
| `Fuera de horario` | FreshnessBADGE gris · "mercado cerrado" |
| `Crítico / sin dato` | FreshnessBADGE rojo · el valor muestra `—` |
| `Fundamental parcial` | Badge `parcial` + tooltip con campos disponibles |
| `Fundamental no disponible` | Celda con `—` + tooltip "No disponible · Período QYYY" |

**Regla:** nunca mostrar `0` cuando el dato es nulo. `—` es la representación de "no tenemos este dato".

---

## Estrategia Responsive

### Breakpoints

| Token | Ancho | Dispositivo objetivo |
|-------|-------|---------------------|
| `mobile` | 360px–767px | Smartphone portrait |
| `tablet` | 768px–1279px | Tablet landscape + portátiles pequeños |
| `desktop` | 1280px+ | Desktop y laptops estándar |

### Adaptaciones por Pantalla

**S-01 Home pública:**
- `desktop`: layout de 2 columnas (top movers + noticias en paralelo)
- `tablet`: columna única con top movers antes de noticias
- `mobile`: carrusel + noticias + top movers verticales; ranking compacto

**S-05 Ficha pública:**
- `desktop`: gráfica 60% + métricas 40% en paralelo
- `tablet`: gráfica full width + métricas debajo
- `mobile`: gráfica compacta (altura reducida) + métricas en grid 2x2 + secciones en acordeón

**S-08 Portafolio:**
- `desktop`: KPIs en fila + tabla completa con filas expandibles
- `tablet`: KPIs en 2x2 + tabla con columnas reducidas + fila expandida simplificada
- `mobile`: KPIs en 2x2 + tabla compacta (Ticker · Precio · Plusvalía%) + tap → sheet bottom

**S-09 Oportunidades:**
- `desktop`: sliders en panel lateral o acordeón + tabla completa
- `tablet`: sliders colapsados (acordeón) + tabla con columnas esenciales
- `mobile`: sliders en modal bottom sheet + lista de oportunidades (no tabla)

**S-13-18 Ops:**
- Ops es desktop-only en MVP — los operadores usan escritorio
- No se diseña layout tablet/mobile para el frontend Ops en MVP

### Reglas Generales de Responsividad

1. Las acciones primarias de cada pantalla son siempre accesibles sin scroll en todos los breakpoints
2. Las tablas financieras no hacen scroll horizontal en mobile — colapsan a card/list
3. Los tooltips de FreshnessBADGE se convierten en modales bottom-sheet en mobile (no hover)
4. El InlineEditor no funciona en mobile — la edición en mobile se hace mediante un formulario simple en bottom sheet
5. La búsqueda global siempre está en el header en todos los breakpoints

---

## Accesibilidad

### Estándar Base: WCAG 2.1 Nivel AA

Confirmado en NFR-02 del PRD. Las siguientes son las decisiones de implementación concretas.

### Navegación por Teclado

| Elemento | Comportamiento esperado |
|----------|------------------------|
| Header y menú de navegación | `Tab` navega en orden lógico; menú se abre con `Enter`/`Space` |
| Tabla de portafolio | `Tab` entre celdas; `Enter` activa edición inline; `Escape` cancela |
| Sliders de score | `ArrowLeft`/`ArrowRight` ajustan valor en incrementos de 5% |
| Estrella de favorito | `Enter`/`Space` toggle; estado anunciado por screen reader |
| Fila expandible | `Enter`/`Space` sobre la fila abre/cierra detalle |
| FibrasSearch | Autocompletado navegable con flechas; `Enter` selecciona; `Escape` cierra |

### Contraste y Color

- Ratio mínimo de contraste: 4.5:1 para texto normal, 3:1 para texto grande
- El estado de FreshnessBADGE nunca depende solo del color — siempre incluye texto/ícono
- El estado de plusvalía/minusvalía nunca depende solo del color — siempre incluye + / − en el número
- El score de oportunidad incluye texto numérico además del color de la barra

### Atributos ARIA

| Componente | Atributos clave |
|-----------|-----------------|
| `FreshnessBadge` | `role="status"`, `aria-label="Precio actualizado hace X min"` |
| `DataTable` con sort | `aria-sort="ascending\|descending\|none"` en th activos |
| `ExpandableRow` | `aria-expanded="true\|false"`, `aria-controls="row-detail-[id]"` |
| `StarButton` | `aria-pressed="true\|false"`, `aria-label="Marcar/Desmarcar como favorita"` |
| `ScoreSliders` | `role="slider"`, `aria-valuenow`, `aria-valuemin`, `aria-valuemax` |
| `InlineEditor` | `aria-label` con nombre del campo + valor actual |

### Formularios y Mensajes de Error

- Todos los errores de validación se anuncian con `role="alert"` o `aria-live="polite"`
- Los campos requeridos llevan `aria-required="true"`
- Los mensajes de error se asocian al campo con `aria-describedby`

### Consideraciones para Mobile (touch)

- Áreas de toque mínimas de 44×44px para botones e íconos interactivos
- La estrella de favorito (★) tiene target de 44px aunque visualmente sea más pequeña
- Los sliders en mobile tienen target suficiente para operación con pulgar

---

## Síntesis Final

### Lo Que Este Documento Define

Esta especificación UX cubre:

1. **Experiencia de usuario central** — la decisión de compra como métrica de éxito, portafolio → score → compra como flujo de menor resistencia
2. **Respuesta emocional objetivo** — Confianza + Claridad como norte de todas las decisiones de diseño
3. **Análisis de inspiración** — patrones adoptados, adaptados y evitados explícitamente
4. **Sistema de diseño** — shadcn/ui + Tailwind CSS, tokens de color y tipografía financiera
5. **Inventario completo de pantallas** — 18 rutas entre Main y Ops, con acceso y propósito
6. **Arquitectura de navegación** — header principal, anclas de ficha, flujo portafolio → oportunidades
7. **Diseños clave de 5 pantallas** — Home, Ficha, Portafolio, Oportunidades, Dashboard Ops
8. **Inventario de componentes** — 20+ componentes clasificados por superficie
9. **5 patrones de interacción críticos** — edición inline, sliders, favoritos, carga de archivo, datos degradados
10. **Estrategia responsive** — 3 breakpoints, adaptaciones por pantalla, reglas generales
11. **Accesibilidad** — WCAG 2.1 AA, teclado, contraste, ARIA, touch

### Restricciones de Diseño No Negociables

- Ningún precio aparece sin FreshnessBADGE y timestamp
- Ningún dato faltante se representa con `0` — siempre `—` con tooltip explicativo
- El flujo portafolio → oportunidades nunca requiere más de 2 clics
- Las acciones frecuentes (editar, favorito, ver simulador) son ≤ 1-2 clics — sin modales de pantalla completa
- El score siempre muestra su desglose en ≤ 1 clic adicional
- El disclaimer del simulador es siempre visible en el viewport — no requiere scroll

### Listo para Implementación

Este documento, junto con el PRD, la arquitectura y los épicos, completa el conjunto de artefactos de planeación necesarios para iniciar la implementación por historia. El siguiente paso es ejecutar `bmad-sprint-planning` para generar el plan de sprints o iniciar con `bmad-create-story` para la primera historia disponible.
