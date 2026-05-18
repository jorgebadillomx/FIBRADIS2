---
project: FIBRADIS
epics: [2]
version: 1.0
date: 2026-05-18
status: draft
prepared_by: QA Lead (bmad-testing-guide)
epic_title: Catálogo Maestro y Descubrimiento Público
---

# Guía de Pruebas — Épica 2: Catálogo Maestro y Descubrimiento Público
## FIBRADIS

---

## 1. Propósito de este documento

Este documento es una guía de pruebas funcionales para el sistema **FIBRADIS**. Está escrito para testers que no participaron en el desarrollo y que pueden no conocer el sistema previamente.

La Épica 2 es la primera que entrega algo visible para el usuario final: la página de inicio pública, la búsqueda de FIBRAs y la ficha de detalle de cada FIBRA. También cubre que el sitio sea rastreable por buscadores (Google, Bing) y sea accesible para personas con discapacidad.

El objetivo de esta guía es verificar que:
- Un visitante puede explorar el catálogo de FIBRAs desde la página de inicio
- La búsqueda por ticker o nombre funciona correctamente
- La ficha de cada FIBRA muestra la información estructural del activo
- El sitio puede ser indexado por motores de búsqueda (tiene meta tags correctos)
- El sitio puede navegarse usando solo el teclado (accesibilidad)
- Las páginas se ven correctamente en distintos tamaños de pantalla

No se requiere conocimiento técnico. Solo necesitas un navegador web moderno.

---

## 2. Alcance de esta guía

### Épica 2: Catálogo Maestro y Descubrimiento Público

**Catálogo de FIBRAs vía API (Historia 2.1)**
- El catálogo con 10+ FIBRAs activas está disponible en el sistema
- Cada FIBRA tiene: ticker, nombre completo, nombre corto, sector, mercado, moneda, estado y URLs oficiales
- FIBRAs inactivas están excluidas del listado principal

**Home pública con búsqueda global (Historia 2.2)**
- La página de inicio carga con su estructura completa
- La barra de búsqueda permite autocompletar por ticker o nombre
- Al seleccionar una FIBRA, navega a su ficha

**Ficha pública de FIBRA (Historia 2.3)**
- La ficha muestra el encabezado con metadatos de la FIBRA
- Secciones de Mercado, Fundamentales, Distribuciones y Noticias están presentes (con estados placeholder porque sus datos llegan en épicas futuras)
- La sección de Reportes muestra links reales cuando existen
- Si la FIBRA no existe, se muestra una página clara de "no encontrado"

**SEO, prerender y accesibilidad (Historia 2.4)**
- Las páginas tienen meta tags correctos visibles sin JavaScript
- La Home y las fichas cumplen navegación por teclado (WCAG 2.1 AA básico)
- El sitio se ve correctamente en 360px, 768px y 1280px sin overflow

---

## 3. Fuera de alcance

Las siguientes áreas **no** son parte de esta guía y no deben evaluarse en esta ronda:

- **Precios de mercado en tiempo real** — La sección de Mercado en la ficha pública mostrará datos reales en la Épica 3. En esta guía solo se verifica el estado placeholder
- **Noticias asociadas a FIBRAs** — Las noticias llegan en la Épica 4. Las secciones de noticias en Home y ficha serán placeholders
- **Datos de fundamentales** — Los datos financieros detallados (CAP rate, NAV, LTV, FFO) llegan en la Épica 5. Solo se verifica la estructura preparada
- **Distribuciones históricas** — Épica 3. Solo se verifica el estado placeholder
- **Comparador de FIBRAs** — Funcionalidad de crecimiento (GROWTH), excluida del MVP
- **Área privada (portafolio, login)** — Épica 6. La Épica 2 es completamente pública; no se necesita login para ninguna prueba de esta guía
- **Centro de Procesos (Ops)** — Épica 5
- **Gestión del catálogo desde interfaz de administración** — Épica 5. En esta épica el catálogo solo puede editarse desde la base de datos

> **Nota sobre los placeholders:** Muchas secciones de la ficha mostrarán mensajes como "disponible en Épica 3" o "disponible en Épica 5". Esto es correcto y esperado — no lo marques como error. Lo que sí debes verificar es que estas secciones carguen sin errores y el mensaje sea claro para el usuario.

---

## 4. Antes de empezar

### 4.1 Prerrequisitos

**Acceso al sistema:**
- URL del sitio web público (ejemplo: `http://localhost:5173` en desarrollo, o la URL del servidor de pruebas)
- No necesitas cuenta ni contraseña para ninguna prueba de esta guía — toda la Épica 2 es pública

**Herramientas:**
- Navegador web actualizado: Chrome, Firefox o Edge (los tres funcionan)
- Herramientas de desarrollo del navegador (opcional, para verificar meta tags): F12 → pestaña Elements
- Para pruebas de accesibilidad: usa solo el teclado (sin ratón)
- Para pruebas responsive: usa las herramientas de desarrollo del navegador (F12 → ícono de dispositivo móvil) o achica la ventana del navegador

**Datos que deben existir en el sistema:**
- El catálogo debe estar sembrado con al menos 10 FIBRAs activas, incluyendo:
  - **FUNO11** (FIBRA Uno) — la más conocida, úsala como referencia en las pruebas
  - **FIBRAM** (FIBRA Macquarie)
  - Al menos 8 FIBRAs adicionales activas
- Debe existir al menos 1 FIBRA inactiva (para la prueba N3)

> Si el catálogo está vacío, contacta al equipo de desarrollo — los datos semilla deben haberse aplicado durante el despliegue.

### 4.2 Roles de usuario disponibles

Esta épica es completamente pública. No hay roles de usuario — cualquier visitante puede ver todo el contenido cubierto en esta guía.

### 4.3 Cómo acceder al sistema

Simplemente abre tu navegador y navega a la URL de la plataforma. La página de inicio es la primera pantalla que verás.

Para navegar a la ficha de una FIBRA específica, la URL sigue el patrón:
```
/fibras/TICKER
```
Por ejemplo: `/fibras/FUNO11` para ver la ficha de FIBRA Uno.

### 4.4 Glosario

| Término | Significado |
|---------|-------------|
| **FIBRA** | Fideicomiso de Inversión en Bienes Raíces — un tipo de fondo de inversión en bienes raíces que cotiza en la Bolsa Mexicana de Valores (BMV) |
| **Ticker** | El código corto que identifica a una FIBRA en el mercado. Por ejemplo: `FUNO11`, `FIBRAM`, `TERRA13` |
| **CBFI** | Certificado Bursátil Fiduciario Inmobiliario — la unidad mínima de inversión en una FIBRA |
| **Catálogo maestro** | La base de datos de todas las FIBRAs registradas en la plataforma |
| **Ficha pública** | La página de detalle de una FIBRA específica, accesible en `/fibras/<ticker>` |
| **Home** | La página de inicio pública, accesible en `/` |
| **Autocompletado** | Función de la barra de búsqueda que sugiere resultados mientras escribes |
| **Placeholder** | Un elemento visual que ocupa el lugar de datos que todavía no están disponibles. Muestra un mensaje indicando cuándo estarán disponibles |
| **Prerender / HTML estático** | El HTML generado antes de que el usuario cargue la página, necesario para que Google pueda indexar el sitio |
| **Meta tags** | Información escondida en el código HTML de la página que le dice a Google el título y descripción del contenido |
| **WCAG 2.1 AA** | Estándar internacional de accesibilidad web. En esta guía verificamos que el sitio puede usarse con teclado solamente |
| **Skip link** | Un enlace invisible que aparece cuando presionas Tab por primera vez, para saltar directamente al contenido principal |
| **Overflow horizontal** | Cuando el contenido de una página se sale del ancho de la pantalla y aparece una barra de desplazamiento horizontal no deseada |
| **Viewport** | El área visible del navegador. En móvil suele ser ~360px de ancho; en tableta ~768px; en escritorio ~1280px |
| **Cap rate / NAV / LTV / FFO** | Métricas financieras de FIBRAs — no disponibles en esta épica, aparecerán en la Épica 5 |
| **Distribuciones** | Los dividendos que paga una FIBRA a sus inversionistas — no disponibles en esta épica, aparecerán en la Épica 3 |

---

## 5. Escenarios de prueba

---

### 5.1. Home pública — carga y estructura general

**¿Qué es esto?**
La página de inicio es lo primero que ve cualquier visitante de FIBRADIS. Tiene un header con la barra de búsqueda y secciones que mostrarán datos del mercado cuando estén disponibles. Por ahora, las secciones de precios y noticias mostrarán placeholders.

**¿Quién lo usa?**
Cualquier persona que visita FIBRADIS por primera vez.

---

#### Prueba 1.1: La Home carga sin errores

**Objetivo:** Verificar que la página de inicio carga correctamente con todas sus secciones.

**Pasos:**

1. Abre el navegador y navega a la raíz del sitio (`/`).
   → *Debes ver:* La página carga completamente sin pantallas de error

2. Abre las herramientas de desarrollo del navegador (F12) y revisa la consola.
   → *Debes ver:* No hay errores de JavaScript en rojo

3. Verifica visualmente que la página tiene estas secciones:
   - Un header fijo en la parte superior con la barra de búsqueda
   - Una sección de carrusel de precios (mostrará placeholder o estado vacío — correcto)
   - Una sección de "Top Movers" (placeholder — correcto)
   - Una sección de "Ranking Rápido" (placeholder — correcto)
   - Una sección de noticias (placeholder — correcto)
   → *Debes ver:* Todas las secciones presentes, con o sin datos reales

**¿Qué debes ver al terminar?** La Home carga completamente, el header con búsqueda es visible, no hay errores de JavaScript.

**Criterio de éxito:** Página carga, todas las secciones presentes, consola sin errores en rojo.

---

#### Prueba 1.2: El header es visible y no se rompe al hacer scroll

**Objetivo:** Verificar que el header con la barra de búsqueda se mantiene visible al desplazarse por la página.

**Pasos:**

1. Estando en la Home, desplázate hacia abajo con el scroll.
   → *Debes ver:* El header con la barra de búsqueda permanece visible en la parte superior (está "pegado" al tope de la pantalla — es un header fijo)

**Criterio de éxito:** El header es visible en todo momento al hacer scroll.

---

### 5.2. Búsqueda global — autocompletado y navegación

**¿Qué es esto?**
La barra de búsqueda en el header permite encontrar cualquier FIBRA escribiendo su ticker o nombre. Mientras escribes, aparecen sugerencias de autocompletado. Al seleccionar una, navegas directamente a su ficha.

**¿Quién lo usa?**
Cualquier visitante que quiere encontrar una FIBRA específica.

---

#### Prueba 2.1: Búsqueda por ticker parcial muestra sugerencias

**Objetivo:** Verificar que escribir las primeras letras de un ticker muestra sugerencias relevantes.

**Pasos:**

1. Haz clic en la barra de búsqueda del header.
   → *Debes ver:* La barra de búsqueda se activa (cursor de texto)

2. Escribe `FUN` (las primeras tres letras de FUNO11).
   → *Debes ver:* Una lista desplegable de sugerencias aparece debajo de la barra con FIBRAs que coinciden con "FUN"

3. Verifica que `FUNO11` o `FIBRA Uno` aparece en las sugerencias.
   → *Debes ver:* Al menos una sugerencia que incluya FUNO11

4. Verifica que la búsqueda no distingue mayúsculas/minúsculas: escribe `fun` (en minúsculas).
   → *Debes ver:* Las mismas sugerencias que con `FUN`

**¿Qué debes ver al terminar?** Sugerencias de autocompletado aparecen con coincidencias por ticker o nombre, insensible a mayúsculas.

**Criterio de éxito:** Se muestran sugerencias relevantes; la búsqueda es case-insensitive.

**Casos especiales a verificar:**
- Las sugerencias deben limitarse a máximo 8 resultados aunque haya más coincidencias

---

#### Prueba 2.2: Búsqueda por nombre completo o parcial

**Objetivo:** Verificar que la búsqueda también funciona por nombre, no solo por ticker.

**Pasos:**

1. En la barra de búsqueda, escribe `Macquarie`.
   → *Debes ver:* FIBRAM (o "FIBRA Macquarie") aparece en las sugerencias

2. Escribe `Uno`.
   → *Debes ver:* FUNO11 (o "FIBRA Uno") aparece en las sugerencias

**Criterio de éxito:** Las sugerencias coinciden tanto con ticker como con nombre.

---

#### Prueba 2.3: Seleccionar una sugerencia navega a la ficha de la FIBRA

**Objetivo:** Verificar que hacer clic en una sugerencia lleva a la ficha correcta.

**Pasos:**

1. Escribe `FUNO11` en la barra de búsqueda y espera las sugerencias.
2. Haz clic en la sugerencia de FUNO11 (o FIBRA Uno).
   → *Debes ver:* El navegador navega a la URL `/fibras/FUNO11`

3. Verifica que la URL en la barra del navegador es `/fibras/FUNO11`.
   → *Debes ver:* La URL cambió correctamente

4. Verifica que la página de la ficha de FUNO11 carga.
   → *Debes ver:* El contenido de la ficha pública de FIBRA Uno

**Criterio de éxito:** Al seleccionar FUNO11, la URL cambia a `/fibras/FUNO11` y la ficha carga.

---

#### Prueba 2.4: Búsqueda sin resultados muestra estado vacío claro

**Objetivo:** Verificar que escribir algo que no coincide con ninguna FIBRA muestra un mensaje claro, sin error.

**Pasos:**

1. En la barra de búsqueda, escribe `XYZABC123` (texto que no coincide con ninguna FIBRA).
   → *Debes ver:* El dropdown de sugerencias aparece pero sin opciones de FIBRA

2. Verifica el contenido del dropdown.
   → *Debes ver:* Un mensaje claro como "Sin resultados encontrados" o equivalente — NO una pantalla de error ni el dropdown vacío sin explicación

**Criterio de éxito:** Se muestra un estado de "sin resultados" claro y comprensible.

---

### 5.3. Ficha pública de FIBRA

**¿Qué es esto?**
Cada FIBRA tiene su propia página de detalle accesible en `/fibras/<ticker>`. Muestra la información estructural del activo (nombre, sector, mercado, URLs oficiales) y secciones preparadas para datos que llegarán en épicas futuras.

**¿Quién lo usa?**
Visitantes interesados en investigar una FIBRA específica.

---

#### Prueba 3.1: La ficha de FUNO11 carga con todas sus secciones

**Objetivo:** Verificar que la ficha de una FIBRA existente carga correctamente con todas sus secciones.

**Pasos:**

1. Navega a `/fibras/FUNO11`.
   → *Debes ver:* La página carga sin errores

2. Verifica que hay un estado de carga visible mientras se obtienen los datos.
   → *Debes ver:* Un skeleton (áreas grises de carga) mientras la FIBRA se está cargando. NO debe aparecer "no encontrado" o una pantalla en blanco

3. Una vez que carga, verifica el encabezado:
   → *Debes ver:*
   - Nombre completo de la FIBRA ("FIBRA Uno" o equivalente)
   - El ticker: `FUNO11`
   - Su sector (por ejemplo: "Diversificado")
   - Su mercado: `BMV`
   - Su moneda: `MXN`
   - Su estado: `Activo`
   - Un placeholder claro para el precio (el precio real llega en Épica 3)

4. Verifica que existen anclas de navegación hacia las secciones.
   → *Debes ver:* Botones o enlaces que permiten ir a: Mercado, Fundamentales, Distribuciones, Noticias, Reportes

5. Verifica cada sección:
   - **Sección Mercado**: Selectores 1M/3M/6M/1A presentes; área de gráfica en estado vacío (correcto)
   - **Sección Fundamentales**: Mensaje indicando que los datos estarán disponibles en una épica futura (correcto)
   - **Sección Distribuciones**: Mensaje indicando que los datos estarán disponibles en una épica futura (correcto)
   - **Sección Noticias**: Placeholder (correcto)
   - **Sección Reportes**: Si FUNO11 tiene URLs configuradas, deben aparecer como links reales

**¿Qué debes ver al terminar?** Ficha cargada con encabezado completo, todas las secciones presentes, sin errores de JavaScript.

**Criterio de éxito:** Todas las secciones están presentes, el encabezado tiene los datos correctos de FUNO11, la consola no muestra errores.

---

#### Prueba 3.2: La sección de Reportes muestra links reales

**Objetivo:** Verificar que los links a fuentes oficiales de la FIBRA funcionan correctamente.

**Pasos:**

1. En la ficha de FUNO11, desplázate a la sección "Reportes".
2. Verifica qué links aparecen (depende de los datos del catálogo):
   - Si hay URL de sitio oficial (`siteUrl`): debe aparecer como link
   - Si hay URL para inversionistas (`investorUrl`): debe aparecer como link
   - Si hay URL de reportes (`reportsUrl`): debe aparecer como link
   - Si alguna URL no existe en el catálogo: debe mostrar `—` en lugar de un link vacío o un error

3. Haz clic en al menos un link que exista.
   → *Debes ver:* Se abre en una nueva pestaña o redirige al sitio oficial (dependiendo de la configuración)

**Criterio de éxito:** Los links existentes son clickeables; los que no existen muestran `—` sin romper el layout.

---

#### Prueba 3.3: Estado de carga visible durante la obtención de datos

**Objetivo:** Verificar que la ficha muestra un skeleton mientras carga, sin flash de "no encontrado".

**Pasos:**

1. Con la caché del navegador limpia (Ctrl+Shift+Delete → borrar caché), navega a `/fibras/FUNO11`.
2. Observa los primeros milisegundos de carga.
   → *Debes ver:* Un skeleton (áreas de carga) antes de que aparezca el contenido real

**¿Qué debes ver al terminar?** Nunca debe aparecer "FIBRA no encontrada" o una pantalla en blanco antes de cargar los datos reales.

**Criterio de éxito:** La transición de carga a contenido es skeleton → datos reales, nunca contenido vacío o error.

---

#### Prueba 3.4: Navegar entre diferentes FIBRAs actualiza el contenido correctamente

**Objetivo:** Verificar que navegar de una ficha a otra actualiza todos los datos mostrados.

**Pasos:**

1. Navega a `/fibras/FUNO11` y espera a que cargue.
2. Usa la búsqueda o navega directamente a `/fibras/FIBRAM`.
   → *Debes ver:* La ficha se actualiza para mostrar los datos de FIBRAM, no los de FUNO11

3. Verifica que el nombre y ticker en el encabezado corresponden a FIBRAM.
   → *Debes ver:* Encabezado con los datos de FIBRAM

**Criterio de éxito:** Cambiar de ficha actualiza toda la información mostrada correctamente.

---

### 5.4. SEO — Metadatos visibles sin JavaScript

**¿Qué es esto?**
Para que Google y otros buscadores puedan indexar FIBRADIS, las páginas deben incluir información de SEO (título, descripción, URL canónica) en el HTML que se sirve al buscador, incluso antes de que el JavaScript se ejecute. Esto se verifica revisando el código fuente de la página.

**¿Quién lo usa?**
Motores de búsqueda como Google. Para el tester, la verificación es técnica pero sencilla.

---

#### Prueba 4.1: La Home tiene meta tags correctos

**Objetivo:** Verificar que la página de inicio incluye los meta tags básicos de SEO.

**Pasos:**

1. Navega a `/`.
2. Haz clic derecho en cualquier parte de la página y selecciona "Ver código fuente de la página" (no "Inspeccionar").
   → *Debes ver:* El código HTML de la página en una nueva pestaña

3. Busca (Ctrl+F) la etiqueta `<title>`.
   → *Debes ver:* Una etiqueta como `<title>FIBRADIS</title>` o similar en el `<head>` de la página

4. Busca `<meta name="description"`.
   → *Debes ver:* Una etiqueta `<meta name="description" content="...">` con una descripción del sitio

5. Busca `<link rel="canonical"`.
   → *Debes ver:* Una etiqueta que define la URL canónica de la página

6. Busca `<h1`.
   → *Debes ver:* Una etiqueta `<h1>` en el HTML (puede estar visualmente oculta para usuarios, pero visible para buscadores)

**Criterio de éxito:** `<title>`, `<meta name="description">`, `<link rel="canonical">` y al menos un `<h1>` presentes en el código fuente.

---

#### Prueba 4.2: La ficha de FUNO11 tiene meta tags específicos de la FIBRA

**Objetivo:** Verificar que la ficha pública incluye un título y descripción específicos de la FIBRA.

**Pasos:**

1. Navega a `/fibras/FUNO11`.
2. Ver código fuente de la página (clic derecho → "Ver código fuente").
3. Busca `<title>`.
   → *Debes ver:* Un título que incluya "FUNO11" y/o "FIBRA Uno", como: `<title>FUNO11 — FIBRA Uno | FIBRADIS</title>`

4. Busca `<meta name="description"`.
   → *Debes ver:* Una descripción que mencione la FIBRA específica

**Criterio de éxito:** El `<title>` incluye el ticker y nombre de FUNO11; existe `<meta name="description">` específica.

---

### 5.5. Accesibilidad — Navegación por teclado

**¿Qué es esto?**
El sitio debe poder usarse completamente con el teclado, sin ratón. Esto es necesario para personas con discapacidad motriz y también es un criterio de accesibilidad WCAG 2.1 AA.

**¿Quién lo usa?**
Personas que usan el teclado o tecnologías de asistencia como lectores de pantalla.

---

#### Prueba 5.1: Skip link visible al presionar Tab por primera vez

**Objetivo:** Verificar que el skip link ("Ir al contenido principal") aparece cuando se empieza a navegar con teclado.

**Pasos:**

1. Navega a la Home (`/`).
2. Haz clic una vez en cualquier área de la página y **luego presiona Tab** (sin hacer clic en nada más).
   → *Debes ver:* Aparece un enlace visible en la parte superior que dice algo como "Ir al contenido principal" o "Skip to main content"

3. Presiona Enter sobre ese enlace.
   → *Debes ver:* El foco salta directamente al contenido principal de la página, pasando por encima del header

**Criterio de éxito:** El skip link aparece al presionar Tab y funciona al presionar Enter.

---

#### Prueba 5.2: La barra de búsqueda es alcanzable y usable con teclado

**Objetivo:** Verificar que la búsqueda puede usarse completamente con teclado.

**Pasos:**

1. Desde la Home, usa la tecla Tab para navegar hasta la barra de búsqueda. (Puede requerir varios Tab dependiendo de los elementos del header).
   → *Debes ver:* Un indicador de foco visible (borde, resaltado) sobre la barra de búsqueda cuando está seleccionada

2. Escribe `FUNO11` usando el teclado.
   → *Debes ver:* Las sugerencias de autocompletado aparecen

3. Usa las teclas de flecha (↑↓) para navegar entre las sugerencias.
   → *Debes ver:* El elemento seleccionado cambia al navegar con flechas; hay un indicador visual de cuál está activo

4. Presiona Enter sobre la sugerencia de FUNO11.
   → *Debes ver:* El navegador va a `/fibras/FUNO11`

**Criterio de éxito:** La búsqueda completa (activar, escribir, navegar sugerencias, seleccionar) es posible con solo el teclado.

---

#### Prueba 5.3: Todos los elementos interactivos tienen indicador de foco visible

**Objetivo:** Verificar que al navegar con Tab, siempre es visible qué elemento está activo.

**Pasos:**

1. Desde la Home, presiona Tab repetidamente para navegar por todos los elementos interactivos.
2. En cada elemento que recibe el foco (barra de búsqueda, links de navegación, etc.):
   → *Debes ver:* Un indicador visual claro de cuál elemento está activo (borde, fondo diferente, subrayado visible, etc.)

**¿Qué debes ver al terminar?** En ningún momento el foco "desaparece" — siempre es visible qué elemento está seleccionado.

**Criterio de éxito:** El indicador de foco es visible en todos los elementos interactivos durante la navegación por teclado.

---

### 5.6. Responsive — Vista en distintos tamaños de pantalla

**¿Qué es esto?**
El sitio debe verse correctamente en distintos tamaños de pantalla: desde teléfonos móviles pequeños (360px) hasta monitores de escritorio (1280px+). No debe haber elementos que "se salgan" de la pantalla ni barras de scroll horizontales no deseadas.

---

#### Cómo simular distintos tamaños de pantalla

En Chrome o Firefox:
1. Presiona F12 para abrir herramientas de desarrollo
2. Haz clic en el ícono de dispositivo móvil (o presiona Ctrl+Shift+M en Chrome)
3. En la parte superior puedes escribir el ancho deseado (360, 768, 1280)

---

#### Prueba 6.1: Home en 360px (móvil pequeño) sin overflow

**Objetivo:** Verificar que la Home se ve correctamente en un teléfono pequeño sin scroll horizontal.

**Pasos:**

1. Configura el viewport a 360px de ancho.
2. Navega a `/`.
3. Verifica que NO aparece una barra de scroll horizontal en la página.
   → *Debes ver:* La página se adapta al ancho de 360px sin contenido saliendo de los bordes
4. Verifica que la barra de búsqueda es visible sin necesidad de scroll horizontal.
   → *Debes ver:* La búsqueda es la acción principal y está visible en la pantalla
5. Verifica que el texto y los elementos son legibles sin necesidad de zoom.

**Criterio de éxito:** Sin scroll horizontal, búsqueda visible, contenido legible en 360px.

---

#### Prueba 6.2: Home en 768px (tableta)

**Objetivo:** Verificar que la Home se adapta correctamente en tablet.

**Pasos:**

1. Configura el viewport a 768px de ancho.
2. Navega a `/`.
3. Verifica que no hay overflow horizontal.
4. Verifica que el layout se adapta al ancho (las secciones pueden reorganizarse).

**Criterio de éxito:** Sin overflow horizontal, layout adaptado correctamente.

---

#### Prueba 6.3: Home en 1280px (escritorio estándar)

**Objetivo:** Verificar que la Home se ve correctamente en escritorio.

**Pasos:**

1. Configura el viewport a 1280px de ancho (o usa la ventana del navegador en tamaño normal).
2. Navega a `/`.
3. Verifica que el contenido ocupa el ancho disponible de forma adecuada sin elementos cortados o mal alineados.

**Criterio de éxito:** Layout correcto y completo en 1280px.

---

#### Prueba 6.4: Ficha de FUNO11 responsive en 360px, 768px y 1280px

**Objetivo:** Verificar que la ficha pública también se ve bien en todos los breakpoints.

**Pasos:**

1. Para cada tamaño de pantalla (360px, 768px, 1280px):
   - Navega a `/fibras/FUNO11`
   - Verifica que no hay overflow horizontal
   - Verifica que el encabezado con el nombre y ticker es legible
   - Verifica que todas las secciones son accesibles haciendo scroll vertical

**Criterio de éxito:** Sin overflow horizontal en los tres breakpoints, contenido legible y accesible.

---

## 6. Pruebas negativas

---

#### Prueba N1: Navegar a la ficha de una FIBRA que no existe

**Qué intenta hacer el tester:** Escribir directamente la URL de una FIBRA con un ticker que no existe en el catálogo.

**Qué hacer:** Navega a `/fibras/FAKE99` en el navegador.

**Qué debe hacer el sistema:**
- Mostrar una página clara de "FIBRA no encontrada"
- El mensaje debe incluir el ticker buscado (`FAKE99`)
- Debe haber un enlace visible para volver a la Home
- NO debe aparecer una pantalla de error de JavaScript, una pantalla en blanco, ni la aplicación rota

**Criterio de éxito:** Página de "no encontrado" clara, con enlace a Home, sin errores de JavaScript.

---

#### Prueba N2: Búsqueda con caracteres especiales

**Qué intenta hacer el tester:** Escribir caracteres especiales en la barra de búsqueda.

**Qué hacer:** Escribe `<script>alert(1)</script>` en la barra de búsqueda.

**Qué debe hacer el sistema:**
- Tratar la entrada como texto de búsqueda normal
- Mostrar "Sin resultados" (ya que ninguna FIBRA tiene ese ticker)
- NO ejecutar el script ni mostrar una alerta del navegador
- NO romper el layout de la barra de búsqueda

**Criterio de éxito:** La entrada es tratada como texto, sin ejecución de código, layout intacto.

---

#### Prueba N3: Acceder al catálogo API con ticker inactivo devuelve error claro

**Qué intenta hacer el tester:** Usar la API para buscar una FIBRA que existe pero está inactiva.

**Qué hacer:** Obtén el ticker de una FIBRA inactiva del equipo y navega a `/fibras/<ticker-inactivo>`.

**Qué debe hacer el sistema:**
- Mostrar los metadatos de la FIBRA inactiva (incluyendo su estado como "Inactiva")
- El detalle sí debe ser accesible directamente por ticker aunque no aparezca en el listado del catálogo

> **Nota:** Si no conoces el ticker de una FIBRA inactiva, omite esta prueba y menciónalo en el registro de resultados.

**Criterio de éxito:** La ficha de la FIBRA inactiva carga y muestra su estado correcto.

---

## 7. Checklist de aceptación

Antes de firmar que la prueba fue exitosa, verifica que todos los puntos siguientes están cubiertos:

### Home y estructura general
- [ ] La Home carga sin errores de JavaScript en la consola del navegador
- [ ] El header es fijo y permanece visible al hacer scroll
- [ ] Las secciones de carrusel, top movers, ranking y noticias están presentes (con placeholders — es correcto)

### Búsqueda global
- [ ] Escribir `FUN` muestra sugerencias que incluyen FUNO11
- [ ] La búsqueda es case-insensitive (mismos resultados con `fun` que con `FUN`)
- [ ] Máximo 8 sugerencias se muestran a la vez
- [ ] Seleccionar una FIBRA navega a `/fibras/<ticker>`
- [ ] Texto sin coincidencias muestra estado de "sin resultados" claro y sin error

### Ficha pública
- [ ] La ficha de FUNO11 carga con skeleton visible durante la carga
- [ ] El encabezado muestra ticker, nombre completo, sector, mercado, moneda y estado
- [ ] Existe una sección de Mercado con selectores 1M/3M/6M/1A (aunque sin datos reales)
- [ ] Existe una sección de Fundamentales (con placeholder indicando épica futura)
- [ ] Existe una sección de Distribuciones (con placeholder)
- [ ] Existe una sección de Noticias (con placeholder)
- [ ] La sección de Reportes muestra links reales cuando la FIBRA los tiene, o `—` cuando no
- [ ] Navegar a `/fibras/FAKE99` muestra "FIBRA no encontrada" con enlace a Home
- [ ] Cambiar de `/fibras/FUNO11` a `/fibras/FIBRAM` actualiza todos los datos

### SEO y metadatos
- [ ] El código fuente de la Home incluye `<title>`, `<meta name="description">`, `<link rel="canonical">` y `<h1>`
- [ ] El código fuente de `/fibras/FUNO11` incluye un `<title>` con el ticker y nombre de la FIBRA

### Accesibilidad
- [ ] El skip link aparece al presionar Tab desde la Home y funciona con Enter
- [ ] La barra de búsqueda es alcanzable con Tab y usable completamente con teclado
- [ ] El indicador de foco es visible en todos los elementos interactivos al navegar con Tab

### Responsive — sin overflow horizontal en ningún breakpoint
- [ ] Home en 360px: sin scroll horizontal, búsqueda visible
- [ ] Home en 768px: sin scroll horizontal, layout adaptado
- [ ] Home en 1280px: layout completo y correcto
- [ ] Ficha de FUNO11 en 360px: sin overflow, contenido legible
- [ ] Ficha de FUNO11 en 768px: sin overflow
- [ ] Ficha de FUNO11 en 1280px: sin overflow

---

## 8. Registro de resultados

| # Prueba | Nombre | Resultado | Observaciones | Fecha |
|----------|--------|-----------|---------------|-------|
| 1.1 | Home carga sin errores | ⬜ Pendiente | | |
| 1.2 | Header fijo al hacer scroll | ⬜ Pendiente | | |
| 2.1 | Autocompletado por ticker parcial | ⬜ Pendiente | | |
| 2.2 | Búsqueda por nombre | ⬜ Pendiente | | |
| 2.3 | Seleccionar sugerencia navega a ficha | ⬜ Pendiente | | |
| 2.4 | Sin resultados muestra estado vacío | ⬜ Pendiente | | |
| 3.1 | Ficha FUNO11 - todas las secciones | ⬜ Pendiente | | |
| 3.2 | Ficha - sección Reportes | ⬜ Pendiente | | |
| 3.3 | Estado de carga (skeleton) | ⬜ Pendiente | | |
| 3.4 | Cambiar entre fichas actualiza contenido | ⬜ Pendiente | | |
| 4.1 | Meta tags en Home | ⬜ Pendiente | | |
| 4.2 | Meta tags en ficha FUNO11 | ⬜ Pendiente | | |
| 5.1 | Skip link con Tab | ⬜ Pendiente | | |
| 5.2 | Búsqueda usable con teclado | ⬜ Pendiente | | |
| 5.3 | Foco visible en todos los elementos | ⬜ Pendiente | | |
| 6.1 | Home en 360px | ⬜ Pendiente | | |
| 6.2 | Home en 768px | ⬜ Pendiente | | |
| 6.3 | Home en 1280px | ⬜ Pendiente | | |
| 6.4a | Ficha FUNO11 en 360px | ⬜ Pendiente | | |
| 6.4b | Ficha FUNO11 en 768px | ⬜ Pendiente | | |
| 6.4c | Ficha FUNO11 en 1280px | ⬜ Pendiente | | |
| N1 | FIBRA inexistente - página no encontrado | ⬜ Pendiente | | |
| N2 | Caracteres especiales en búsqueda | ⬜ Pendiente | | |
| N3 | FIBRA inactiva accesible por ticker | ⬜ Pendiente | | |

**Hallazgos encontrados:**

| # | Descripción | Prueba | Severidad | Estado |
|---|-------------|--------|-----------|--------|
| 1 | | | | |

**Severidad:**
- **Crítica** — La plataforma no carga, se rompe o expone errores técnicos al usuario
- **Alta** — Una funcionalidad principal (búsqueda, ficha, navegación) no opera como se espera
- **Media** — Comportamiento incorrecto pero existe alternativa (ej. puede navegar directamente por URL aunque la búsqueda tenga un bug menor)
- **Baja** — Problema estético, de responsive, texto o accesibilidad menor

---

## 9. Notas finales

**Sobre los placeholders:** Esta épica entrega la estructura y navegación pública de FIBRADIS. Muchas secciones muestran mensajes de "disponible en Épica X" — esto es diseño intencional, no un error. Lo que sí debes reportar es si esas secciones causan errores de JavaScript o si los mensajes placeholder no son claros para un usuario.

**Sobre los datos de mercado:** No verás precios, gráficas de historial ni distribuciones reales en ninguna ficha. Eso es correcto — llegan en la Épica 3. Si ves datos de precios reales, eso sería inesperado y sí deberías reportarlo.

**Si la búsqueda no muestra resultados:** Verifica primero que el catálogo esté sembrado con datos. El equipo de desarrollo puede confirmar si los datos semilla fueron aplicados correctamente.

**Para pruebas de SEO:** Si tienes acceso al servidor, también puedes solicitar la URL con `curl -H "Accept-Encoding: identity"` para ver el HTML sin JavaScript. En un navegador con JavaScript habilitado, el HTML final puede verse diferente al código fuente.

---

*Documento generado por bmad-testing-guide el 2026-05-18.*
*Para preguntas sobre el alcance o los criterios, contactar al equipo de desarrollo.*
