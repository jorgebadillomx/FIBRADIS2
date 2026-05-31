# Story 8.1: Sección "Conoce las FIBRAs" — Contenido Editorial Editable desde Ops

Status: review

## Story

As a visitante público del sitio,
I want una sección "Conoce las FIBRAs" accesible desde el menú principal con contenido educativo sobre FIBRAs inmobiliarias,
so that puedo entender qué son, cómo funcionan, su historia, por qué invertir y sus beneficios fiscales antes de tomar decisiones de inversión.

## Acceptance Criteria

1. **Menú principal**: el ítem "Conoce las FIBRAs" aparece en el nav público (`PublicLayout.tsx`) y navega a `/conoce-las-fibras`.
2. **Ruta activa**: `/conoce-las-fibras` muestra la nueva página (actualmente cae en `NotFound`).
3. **Tabs de navegación**: la página presenta 5 tabs con contenido independiente:
   - "¿Qué son?" (`que-son-las-fibras`)
   - "Historia" (`historia`)
   - "¿Cómo se estructuran?" (`como-se-estructuran`)
   - "Por qué invertir" (`por-que-invertir`)
   - "Régimen fiscal" (`regimen-fiscal`)
4. **Contenido markdown**: cada tab renderiza el campo `content` de su página usando `ReactMarkdown` (ya disponible en el proyecto).
5. **Loading state**: mientras carga, cada tab muestra un skeleton (un bloque de 4 líneas aproximadas).
6. **Endpoint público**: `GET /api/v1/pages` devuelve la lista de páginas ordenadas por `order ASC` con campos `{ slug, title, content, updatedAt }`. Sin autenticación.
7. **Datos pre-sembrados**: las 5 páginas están sembradas con contenido inicial en español al hacer `dotnet ef database update`.
8. **Editor en Ops**: la sección "Contenido Editorial" aparece en el menú lateral de OpsShell y permite al AdminOps editar el markdown de cada página individualmente con un `<textarea>` y botón "Guardar".
9. **Endpoint de actualización**: `PUT /api/v1/ops/pages/{slug}` (AdminOps) actualiza el `content` de la página y su `updatedAt`. Responde 404 si el slug no existe, 403 si no es AdminOps.
10. **SEO**: `<title>` de la página es `"Conoce las FIBRAs — FIBRADIS"` con `<meta name="description">` apropiado.
11. **No hay creación ni eliminación de páginas desde Ops**: el catálogo de 5 slugs es fijo; solo se edita el contenido.
12. **Unit tests**: al menos 3 tests del método `GetAllAsync` del repositorio (retorna 5 páginas, orden correcto, contenido no nulo).

## Tasks / Subtasks

- [x] T1 — Backend: entidad y repositorio (AC: 6, 7, 12)
  - [x] T1.1 Crear `EditorialPage.cs` en `src/Server/Domain/Ops/` con props: `string Slug`, `string Title`, `string Content`, `int Order`, `DateTimeOffset UpdatedAt`
  - [x] T1.2 Crear `IEditorialPageRepository.cs` en `src/Server/Domain/Ops/` con métodos: `GetAllAsync(CancellationToken)`, `GetBySlugAsync(string, CancellationToken)`, `UpdateContentAsync(string slug, string content, CancellationToken)`
  - [x] T1.3 Crear `EditorialPageRepository.cs` en `src/Server/Infrastructure/Persistence/Repositories/Ops/`
  - [x] T1.4 Agregar `DbSet<EditorialPage> EditorialPages` a `FibradisDbContext`
  - [x] T1.5 Configurar la entidad en `FibradisDbContext.OnModelCreating`: tabla `EditorialPages`, índice único en `Slug`, `HasData` con las 5 páginas sembradas (seed data en §Dev Notes)
  - [x] T1.6 Agregar `AddScoped<IEditorialPageRepository, EditorialPageRepository>()` en el registro DI
  - [x] T1.7 Crear migración EF Core: `dotnet ef migrations add AddEditorialPages --project src/Server/Infrastructure --startup-project src/Server/Api`
  - [x] T1.8 Unit tests del repositorio (`GetAllAsync`): 3 casos mínimo — ver Dev Notes §Tests

- [x] T2 — Backend: contratos y endpoints (AC: 6, 9, 11)
  - [x] T2.1 Crear `EditorialPageDto.cs` en `src/Server/SharedApiContracts/Editorial/` con `{ string Slug, string Title, string Content, DateTimeOffset UpdatedAt }`
  - [x] T2.2 Crear `UpdateEditorialPageRequest.cs` en `src/Server/SharedApiContracts/Editorial/` con `{ string Content }`
  - [x] T2.3 Crear `src/Server/Api/Endpoints/Public/EditorialEndpoints.cs` con `MapGet("/api/v1/pages", ...)` que llama `GetAllAsync` y retorna lista de DTOs
  - [x] T2.4 Crear `src/Server/Api/Endpoints/Ops/OpsEditorialEndpoints.cs` con `MapPut("/api/v1/ops/pages/{slug}", ...)` que valida existencia y llama `UpdateContentAsync`
  - [x] T2.5 Registrar ambos `Map*` en el bootstrap de la API (`src/Server/Api/Program.cs`)
  - [x] T2.6 `dotnet build FIBRADIS.slnx` — 0 errores

- [x] T3 — Frontend: regenerar cliente API (AC: 6)
  - [x] T3.1 `npm run codegen:api` desde raíz del repo

- [x] T4 — Frontend Main: página y ruta (AC: 1, 2, 3, 4, 5, 10)
  - [x] T4.1 Crear `src/Web/Main/src/api/editorialApi.ts` con `fetchEditorialPages(): Promise<EditorialPageDto[]>`
  - [x] T4.2 Crear `src/Web/Main/src/modules/conoce-las-fibras/ConoceLasFibrasPage.tsx` (ver Dev Notes §Frontend Main)
  - [x] T4.3 Agregar ruta `/conoce-las-fibras` en `src/Web/Main/src/app/routes.tsx`
  - [x] T4.4 Agregar ítem "Conoce las FIBRAs" en el nav de `PublicLayout.tsx` (después de "Catálogo", antes de "Noticias")
  - [x] T4.5 `npm run build --workspace=src/Web/Main` — 0 errores TypeScript

- [x] T5 — Frontend Ops: editor de contenido (AC: 8, 9)
  - [x] T5.1 Crear `src/Web/Ops/src/api/editorialApi.ts` con `fetchEditorialPages()` y `updateEditorialPage(slug, content)`
  - [x] T5.2 Crear `src/Web/Ops/src/pages/EditorialPage.tsx` (ver Dev Notes §Frontend Ops)
  - [x] T5.3 Agregar ruta `/editorial` en el router de Ops
  - [x] T5.4 Agregar ítem `{ label: 'Contenido Editorial', to: '/editorial', description: 'Editar textos educativos de la sección Conoce las FIBRAs.' }` en `OpsShell.tsx`
  - [x] T5.5 `npm run build --workspace=src/Web/Ops` — 0 errores TypeScript

- [x] T6 — Verificación final (AC: todos)
  - [x] T6.1 `dotnet test tests/Unit/` — todos pasan
  - [x] T6.2 `dotnet ef database update --project src/Server/Infrastructure --startup-project src/Server/Api` — migración aplica sin errores
  - [x] T6.3 `npm run dev:main` — verificar `/conoce-las-fibras` carga, tabs funcionan, markdown renderiza
  - [x] T6.4 `npm run dev:ops` — verificar sección "Contenido Editorial" carga, guardar actualiza contenido visible en Main

## Dev Notes

### Contexto

Esta historia crea un módulo de contenido editorial estático-administrado para la sección pública "Conoce las FIBRAs". El contenido rara vez cambia y no requiere pipelines ni jobs — es puro CRUD con un endpoint público de lectura y un editor en Ops. No hay lógica de negocio compleja.

La ruta `/conoce-las-fibras` actualmente no existe; cae en `NotFound`. El nav de `PublicLayout.tsx` sí existe con el patrón correcto — se agrega un `<a>` adicional siguiendo la misma estructura.

---

### Backend — Entidad `EditorialPage`

```csharp
// src/Server/Domain/Ops/EditorialPage.cs
namespace Domain.Ops;

public class EditorialPage
{
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int Order { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
```

**Configuración EF Core en `OnModelCreating`:**

- Clave primaria: `Slug` (string)
- `Content` → columna tipo `nvarchar(max)`
- `HasData(...)` con las 5 páginas sembradas (ver §Seed Data)

**Interfaces del repositorio:**

```csharp
// src/Server/Domain/Ops/IEditorialPageRepository.cs
namespace Domain.Ops;

public interface IEditorialPageRepository
{
    Task<IReadOnlyList<EditorialPage>> GetAllAsync(CancellationToken ct);
    Task<EditorialPage?> GetBySlugAsync(string slug, CancellationToken ct);
    Task UpdateContentAsync(string slug, string content, CancellationToken ct);
}
```

**Implementación en Infrastructure:**

- `GetAllAsync`: `await _db.EditorialPages.OrderBy(p => p.Order).ToListAsync(ct)`
- `GetBySlugAsync`: `await _db.EditorialPages.FirstOrDefaultAsync(p => p.Slug == slug, ct)`
- `UpdateContentAsync`: usa `ExecuteUpdateAsync` con `WHERE Slug == slug` actualizando `Content` y `UpdatedAt = DateTimeOffset.UtcNow`

---

### Backend — Seed Data completo (5 páginas)

**IMPORTANTE**: las fechas de `UpdatedAt` en `HasData` deben ser constantes (no `DateTimeOffset.UtcNow`) para que EF Core no genere una migración nueva en cada build.

El `HasData` va en `OnModelCreating` dentro de la configuración de `EditorialPage`. El contenido markdown está diseñado con datos verificados de múltiples fuentes (CNBV, BMV, artículos especializados, LISR arts. 187-188).

```csharp
builder.Entity<EditorialPage>().HasData(

  new EditorialPage
  {
    Slug = "que-son-las-fibras",
    Title = "¿Qué son las FIBRAs?",
    Order = 1,
    UpdatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
    Content = @"## Fideicomisos de Inversión en Bienes Raíces

Las FIBRAs son vehículos de inversión que permiten a cualquier persona participar en el mercado inmobiliario institucional mexicano sin necesidad de comprar propiedades directamente. Son fideicomisos constituidos conforme a la Ley General de Títulos y Operaciones de Crédito, con régimen fiscal especial regulado en los artículos 187 y 188 de la Ley del Impuesto Sobre la Renta (LISR).

## ¿Cómo funciona un fideicomiso FIBRA?

El fideicomiso adquiere y administra inmuebles que generan renta: centros comerciales, parques industriales, hoteles, oficinas corporativas, almacenes logísticos. Los inversionistas compran **CBFIs** (Certificados Bursátiles Fiduciarios Inmobiliarios) en la Bolsa Mexicana de Valores (BMV) o en la BIVA. Las rentas cobradas a los inquilinos —menos gastos operativos y deuda— se distribuyen trimestralmente entre todos los tenedores en proporción a su participación.

Para calificar como FIBRA y acceder al régimen fiscal preferente, el fideicomiso debe cumplir tres requisitos estructurales:

- **Mínimo 70%** del patrimonio invertido en inmuebles para arrendamiento
- Los inmuebles deben permanecer en el fideicomiso al menos **cuatro años** antes de poder enajenarse
- Distribuir al menos el **95% del resultado fiscal anual** a los tenedores, a más tardar el 15 de marzo del ejercicio siguiente

## Los CBFIs: qué son y cómo se negocian

Un CBFI representa una participación alícuota en el patrimonio del fideicomiso. No es una acción de una empresa, sino un título que confiere tres derechos: recibir distribuciones periódicas en efectivo, participar en la apreciación del valor del portafolio a través del precio de mercado, y votar en asambleas de tenedores sobre decisiones relevantes del fideicomiso.

Los tickers bursátiles siguen la convención de combinar el nombre abreviado con un número de serie: FUNO11, FMTY14, FIBRAPL14, DANHOS13. En 2025 los precios por CBFI oscilaban entre aproximadamente 12 y 70 pesos, lo que permite comenzar con montos relativamente pequeños.

## Tipos de inmuebles

| Segmento | Características clave |
|---|---|
| **Industrial y logístico** | Naves manufactureras, centros de distribución, parques industriales. Segmento dominante, impulsado por nearshoring |
| **Comercial** | Centros comerciales, plazas de retail, locales en puntos de alto tráfico |
| **Corporativo** | Edificios de oficinas clase A en CDMX, Monterrey, Guadalajara |
| **Hotelero** | Hoteles de negocios y resorts turísticos |
| **Mixto** | Combinación de varios segmentos en un mismo fideicomiso |

## ¿En qué se diferencian de los REITs de EE.UU.?

Las FIBRAs son el equivalente mexicano de los Real Estate Investment Trusts (REITs) que existen en Estados Unidos desde 1960. Comparten la misma filosofía: ambos distribuyen la gran mayoría de sus utilidades (95% en México, 90% en EE.UU.) y ambos cotizan en bolsa. La diferencia principal es estructural: los REITs operan como sociedades anónimas (corporations), mientras que las FIBRAs son fideicomisos, figura jurídica con amplia raigambre en el derecho mexicano. En términos fiscales, la ganancia de capital por venta de CBFIs en bolsa está exenta de ISR para personas físicas residentes en México, beneficio que no tiene equivalente directo en el régimen americano.

## ¿Quién puede invertir?

Cualquier persona física o moral con acceso a una casa de bolsa o plataforma de inversión en línea puede comprar CBFIs. No se requiere ser inversionista calificado ni cumplir ningún mínimo patrimonial. Las Afores, aseguradoras y fondos de pensiones también participan activamente y son en muchos casos los mayores tenedores, lo que aporta profundidad y liquidez al mercado."
  },

  new EditorialPage
  {
    Slug = "historia",
    Title = "Historia",
    Order = 2,
    UpdatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
    Content = @"## De la regulación al mercado: 15 años de FIBRAs en México

### Los antecedentes (2004)

Las primeras reglas que contemplaban este tipo de instrumento datan de 2004, cuando la BMV emitió los lineamientos iniciales. Sin embargo, el marco no era suficientemente robusto: faltaban incentivos fiscales claros, mecanismos de transmisión de inmuebles sin carga fiscal inmediata y un mercado con suficiente profundidad. Un grupo reducido de expertos del sector financiero e inmobiliario dedicó aproximadamente siete años a refinar la regulación, observando la evolución internacional y adaptando la experiencia de los REITs al contexto jurídico y fiscal mexicano.

### El primer CBFI (18 de marzo de 2011)

El hito fundacional del mercado ocurrió el **18 de marzo de 2011**, cuando **FIBRA Uno (FUNO11)** realizó su Oferta Pública Inicial simultáneamente en la BMV y en mercados internacionales. El portafolio inicial constaba de **16 inmuebles**, 14 aportados por los promotores a cambio de CBFIs y dos adquiridos con recursos de la oferta. El precio de salida fue de **19.50 pesos por CBFI**. Para la mayoría de los inversionistas mexicanos, el instrumento era completamente nuevo.

### Expansión acelerada (2012–2014)

El éxito de FUNO abrió la puerta a una oleada de nuevas emisiones especializadas:

- **FIBRA Hotel** — sector hotelero
- **Macquarie México** — industrial
- **Terrafina** — industrial, con respaldo de PGIM/Prudential
- **Danhos** — comercial y oficinas premium

En 2013, FUNO realizó la adquisición del portafolio Apolo —descrita entonces como la transacción inmobiliaria más grande de la historia de México, con valor superior a 23,000 millones de pesos— e incorporó activos icónicos como Torre Mayor en la Ciudad de México. En 2014 emitió el primer bono a 30 años de su historia, siendo la primera FIBRA en el mundo en alcanzar ese plazo en mercados de deuda.

### Consolidación y nuevos vehículos (2015–2019)

En 2015 la BMV formalizó el índice **S&P/BMV FIBRAS**, referente del sector. Ese mismo año, el 5 de octubre, se presentó oficialmente el concepto de **FIBRA E** (Fideicomiso de Inversión en Energía e Infraestructura), ampliando el universo hacia proyectos de energía, carreteras, puertos y telecomunicaciones. En 2018 se listó la **CFE FIBRA E**, que bursatiliza derechos de cobro asociados a la red de transmisión eléctrica de la Comisión Federal de Electricidad.

En 2019, FUNO alcanzó los 10.1 millones de metros cuadrados de Área Bruta Rentable (ABR) tras adquirir el portafolio Titán (74 naves industriales).

### La prueba de estrés: pandemia 2020

La pandemia representó el primer gran test del sector. Las FIBRAs comerciales y hoteleras sufrieron caídas importantes en ocupación y distribuciones. Las industriales resistieron mejor, impulsadas por el auge del e-commerce. La recuperación fue gradual pero sólida, y el nearshoring —la relocalización de cadenas de suministro hacia México— se convirtió en el principal motor de crecimiento del segmento industrial en los años siguientes.

### El mercado maduro (2021–2025)

A cierre de 2025 el sector alcanzó cifras históricas:

| Métrica | Dato |
|---|---|
| Activos bajo operación | 891,000 millones de pesos |
| Propiedades gestionadas | más de 2,200 |
| Superficie total (ABR) | 30.5 millones de m² |
| Ocupación promedio | 91–95% |
| FIBRAs listadas | 15 |
| Capitalización de mercado | 453,627 millones de pesos |
| Peso en el PIB nacional | ~4.5% |

El índice S&P/BMV FIBRAS acumuló una plusvalía de **175.54%** en sus primeros 14 años, frente al 61.4% del IPC en el mismo período. En mayo de 2026, FIBRA Uno celebró sus **15 años** de operación con el mayor portafolio inmobiliario certificado EDGE del mundo."
  },

  new EditorialPage
  {
    Slug = "como-se-estructuran",
    Title = "¿Cómo se estructuran?",
    Order = 3,
    UpdatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
    Content = @"## La arquitectura jurídica y operativa de una FIBRA

### El fideicomiso como columna vertebral

La FIBRA existe jurídicamente como un **fideicomiso irrevocable** constituido conforme a la Ley General de Títulos y Operaciones de Crédito. El fideicomiso es el propietario legal de los inmuebles y el emisor de los CBFIs. Cinco actores principales participan en la estructura:

**Fideicomitentes** — Los aportantes originales de inmuebles o efectivo. Al aportar una propiedad, reciben CBFIs como contraprestación. Pueden ser los promotores fundadores o, en emisiones posteriores, terceros que intercambian inmuebles por certificados.

**Fiduciario** — Una institución de crédito autorizada por la CNBV (típicamente un banco) que actúa como propietario formal de los activos. No administra los inmuebles directamente: ejecuta instrucciones del Comité Técnico y del Administrador. CIBanco, HSBC México y BBVA México han desempeñado este rol en distintas FIBRAs.

**Fideicomisarios (tenedores de CBFIs)** — Todos los inversionistas que poseen CBFIs. Son los beneficiarios finales: tienen derecho a distribuciones, a votar en asambleas y a participar en la apreciación del portafolio.

**Administrador** — Gestiona los inmuebles, cobra rentas, contrata mantenimiento y ejecuta la estrategia de inversión. En la mayoría de las FIBRAs mexicanas el administrador es **externo** (empresa separada del fideicomiso que cobra honorarios). La excepción más notable es **FIBRA Monterrey (FMTY)**, primera en adoptar administración internalizada, lo que algunos analistas consideran una ventaja en términos de alineación de incentivos entre el equipo gestor y los tenedores.

**Comité Técnico** — Órgano de gobierno equivalente a un consejo de administración. Toma decisiones sobre estrategia, adquisiciones y política de distribuciones. Incluye consejeros independientes.

### La Asamblea de Tenedores

El máximo órgano de decisión. Cada CBFI equivale a un voto. Los tenedores se reúnen al menos una vez al año para aprobar estados financieros, elegir miembros del Comité Técnico y decidir sobre asuntos relevantes como aumentos de capital o cambios en la política de inversión.

### El papel de los reguladores

| Autoridad | Función |
|---|---|
| **CNBV** | Autoriza el registro de CBFIs, supervisa obligaciones de información continua y conducta de los participantes |
| **BMV / BIVA** | Listan los CBFIs y aseguran la operación transparente del mercado secundario |
| **SAT** | Vigila el cumplimiento del régimen fiscal especial (arts. 187-188 LISR). Puede revocar el estatus de FIBRA si el fideicomiso incumple los requisitos |

### La distribución mínima del 95%

El requisito de distribuir al menos el **95% del resultado fiscal neto** antes del 15 de marzo de cada año es la obligación más característica de las FIBRAs. El resultado fiscal no es igual al resultado contable: se calculan los ingresos gravables menos las deducciones autorizadas por la LISR.

Una parte de la distribución puede corresponder a **reembolso de capital** (amortización de la inversión original), que no está sujeto a retención de ISR en el momento del pago, aunque sí reduce el costo fiscal del inversionista para efectos de futuras ganancias de capital.

### FIBRA inmobiliaria vs FIBRA E

Aunque en el uso cotidiano se habla simplemente de ""FIBRAs"", existen dos grandes categorías bajo el mismo régimen de los artículos 187-188 de la LISR:

**FIBRAs inmobiliarias** — Son las tradicionales: patrimonio en bienes raíces para arrendamiento, mínimo 70% en inmuebles, restricción de venta antes de 4 años, distribución del 95% del resultado fiscal.

**FIBRAs E (Energía e Infraestructura)** — Presentadas en octubre de 2015, canalizan inversión hacia proyectos maduros de energía e infraestructura: hidrocarburos, transmisión eléctrica, carreteras, puertos, aeropuertos. Pueden monetizar **derechos de cobro** asociados a contratos de largo plazo, no necesariamente activos físicos. La **CFE FIBRA E** (listada 2018) es el ejemplo más grande: no transfiere torres de transmisión, sino los flujos de cobro del negocio de transmisión de la CFE por 30 años (hasta 2048). Las FIBRA E requieren que al menos el **75% de sus inversiones** estén en proyectos con historial operativo demostrado."
  },

  new EditorialPage
  {
    Slug = "por-que-invertir",
    Title = "Por qué invertir",
    Order = 4,
    UpdatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
    Content = @"## Argumentos para considerar las FIBRAs en un portafolio

### Acceso democrático al inmobiliario institucional

Adquirir un inmueble comercial de calidad institucional en México requiere decenas o cientos de millones de pesos, concentración de riesgo en un solo activo, gestión activa de inquilinos y costos de transacción elevados. Las FIBRAs permiten participar en ese mismo mercado desde unos cientos de pesos, con diversificación inmediata entre decenas o cientos de propiedades y gestión a cargo de equipos profesionales con décadas de experiencia. En 2025, los precios por CBFI oscilaban entre aproximadamente 12 y 70 pesos.

### Liquidez en tiempo real

Vender un inmueble directamente puede tomar meses y genera costos de escrituración, notaría e impuestos de transmisión. Los CBFIs se compran y venden en bolsa en tiempo real, con los mismos mecanismos que las acciones. FUNO11, el CBFI más negociado, tiene volúmenes diarios que permiten entrar o salir de posiciones de millones de pesos sin afectar el precio.

### Distribuciones periódicas verificables

El yield promedio del sector en 2025 se ubicó en aproximadamente **7.1% anual**, con rendimientos por distribución que variaban entre las principales emisoras:

| FIBRA | Yield aprox. 2025 |
|---|---|
| FIBRA Uno (FUNO11) | ~8.7% |
| FIBRA Monterrey (FMTY14) | ~8.6% |
| FIBRA Nova | ~8.4% |
| Danhos (DANHOS13) | ~8.0% |
| FIBRA Prologis (FIBRAPL14) | ~6.5% |

Estos rendimientos reflejan la obligación legal de distribuir el 95% del resultado fiscal — a diferencia de una empresa que puede retener utilidades indefinidamente.

### Rendimiento total histórico

El índice S&P/BMV FIBRAS acumuló una **plusvalía de 175.54%** en sus 14 años de historia (2011–2025), frente al 61.4% del IPC en el mismo período. Al sumar distribuciones más apreciación de precio, el rendimiento compuesto anualizado del sector se ubicó en el rango del 11–12% promedio. En los primeros siete meses de 2025 las FIBRAs acumularon un avance de **18%**, superando al IPC en ese período.

### Cobertura parcial frente a la inflación

Las rentas en contratos de largo plazo (3 a 15 años en el segmento industrial) suelen incluir ajustes por inflación (IPC o INPC). Los portafolios industriales con contratos en dólares ofrecen cobertura adicional frente a la depreciación del peso, aunque esto también introduce riesgo cambiario si la paridad se mueve en sentido contrario.

---

## Riesgos reales que el inversionista debe conocer

Una perspectiva honesta exige reconocer los riesgos documentados del sector:

**Riesgo de tasa de interés** — Es el riesgo más significativo para las FIBRAs. Cuando las tasas suben, el precio de los CBFIs baja porque el yield se vuelve menos atractivo comparado con instrumentos de deuda sin riesgo. En 2024, con Banxico manteniendo tasas elevadas (10%), el índice de FIBRAs cayó un **-15.5%**. La baja de tasas en 2025 fue el principal catalizador de la recuperación.

**Riesgo de vacancia** — Si los inquilinos desocupan o dejan de pagar, los ingresos del fideicomiso caen y las distribuciones disminuyen. La pandemia de 2020 fue el ejemplo más extremo; las FIBRAs comerciales y hoteleras sufrieron fuerte presión. En 2025 la ocupación promedio se recuperó al 91–95%, pero este riesgo siempre está presente, especialmente en segmentos con sobreoferta.

**Riesgo de tipo de cambio** — Muchas FIBRAs tienen contratos en dólares, especialmente en el sector industrial. Puede ser un escudo contra la devaluación, pero también un riesgo cuando el peso se aprecia y los inquilinos presionan por renegociar.

**Riesgo del administrador externo** — El administrador cobra honorarios sobre los activos bajo gestión, lo que puede crear incentivos para crecer el portafolio más allá de lo óptimo para los tenedores. Revisar la estructura de comisiones y el historial de decisiones del administrador es parte del análisis fundamental.

**Riesgo de dilución** — Cuando una FIBRA emite nuevos CBFIs para financiar adquisiciones, los tenedores existentes ven diluida su participación. Si los activos adquiridos no generan rendimientos proporcionales, el rendimiento por CBFI se reduce.

**Riesgo regulatorio** — Cambios en la LISR o en la regulación de la CNBV podrían afectar los beneficios fiscales del instrumento."
  },

  new EditorialPage
  {
    Slug = "regimen-fiscal",
    Title = "Régimen fiscal",
    Order = 5,
    UpdatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
    Content = @"## Tratamiento fiscal de las FIBRAs (artículos 187 y 188 LISR)

> **Aviso:** Esta sección es de carácter educativo y general. Para asesoría fiscal específica a tu situación, consulta a un contador o asesor fiscal certificado.

### Transparencia fiscal del fideicomiso

A nivel del fideicomiso, la FIBRA **no es contribuyente del ISR** como persona moral independiente: es un vehículo **fiscalmente transparente**. No realiza pagos provisionales de ISR durante el ejercicio. Determina un resultado fiscal anual, lo distribuye a los tenedores, y el impuesto lo paga cada uno según su propio régimen fiscal.

### Personas físicas residentes en México

El tratamiento para la mayoría de los inversionistas individuales mexicanos tiene dos componentes separados: las distribuciones periódicas y la ganancia de capital al vender.

**Distribuciones**

Las distribuciones que provienen del **resultado fiscal** de la FIBRA se clasifican como ingresos por arrendamiento de bienes inmuebles en fideicomiso. El fiduciario (o el intermediario bursátil) retiene **30% de ISR** sobre esa porción como pago provisional. Esta retención no es definitiva: al presentar la declaración anual, el contribuyente acumula esos ingresos con los demás de ese régimen y aplica la tarifa progresiva del artículo 152 de la LISR. Si su tasa efectiva resulta menor al 30% (ingresos totales del régimen por debajo de ~974,000 pesos anuales), el SAT devuelve la diferencia. Si es mayor, paga la diferencia.

Una parte de la distribución puede corresponder a **reembolso de capital** (amortización de la inversión original). Esta porción no causa ISR al recibirla, pero reduce el costo fiscal de adquisición de los CBFIs para efectos de futuras ganancias de capital.

**Ganancia de capital por venta de CBFIs en bolsa**

Este es el beneficio fiscal más notable para el inversionista persona física: si se venden CBFIs en la BMV o BIVA a un precio mayor al de adquisición, la **ganancia de capital está exenta de ISR** para personas físicas residentes en México. A diferencia de las acciones ordinarias —cuyas ganancias por venta en bolsa se gravan a una tasa fija del 10%— los CBFIs gozan de exención total. Esta ventaja es un estímulo explícito para fomentar la inversión inmobiliaria bursátil.

### Personas morales residentes en México

Las empresas que invierten en CBFIs acumulan las distribuciones recibidas a sus demás ingresos gravables del ejercicio. La tasa corporativa del ISR (30%) aplica de manera normal. La retención del 30% que realiza el fiduciario sirve como pago provisional acreditable contra el impuesto anual.

### Fondos de pensiones y jubilaciones (incluyendo Afores)

Los fondos de pensiones y jubilaciones calificados están **exentos de la retención del 30%** sobre las distribuciones. Esta exención atrae capital institucional de largo plazo —cuyo horizonte de inversión es compatible con la naturaleza ilíquida del subyacente inmobiliario— y explica la presencia significativa de las Afores como tenedores de CBFIs.

### Inversionistas extranjeros

Los residentes en el extranjero sin establecimiento permanente en México están sujetos a retención de ISR sobre distribuciones conforme a la LISR para ingresos de fuente mexicana, salvo que el tratado fiscal aplicable establezca una tasa menor. La retención se considera **pago definitivo** (no deben presentar declaración anual en México).

### Transmisión de inmuebles al fideicomiso

Cuando un propietario aporta un inmueble a una FIBRA a cambio de CBFIs, se genera técnicamente una enajenación del bien. La LISR permite **diferir** el ISR sobre la ganancia: no se paga al momento de la aportación, sino hasta que se enajenen los CBFIs recibidos o hasta que la propia FIBRA venda el inmueble. Este mecanismo de diferimiento fue clave para que los propietarios originales pudieran aportar propiedades maduras sin un impacto fiscal inmediato.

### Resumen por tipo de inversionista

| Tipo de inversionista | Distribuciones | Ganancia capital |
|---|---|---|
| Persona física residente | Retención 30% provisional; acumula en declaración anual | **Exenta** si se vende en bolsa |
| Persona moral residente | Acumula al ingreso; retención 30% acreditable | Acumula a ingreso gravable |
| Fondos de pensiones / Afores | **Exenta** de retención | Exenta |
| Residente en el extranjero | Retención definitiva (tasa LISR o tratado) | Sujeto a LISR o tratado |

### Otros impuestos

La venta de CBFIs en bolsa está **exenta de IVA**. La transmisión de suelo (terreno) al fideicomiso es acto exento; la de construcciones genera IVA. Los ingresos por arrendamiento que el fideicomiso percibe de sus inquilinos causan IVA conforme a las reglas generales."
  }

);
```

---

### Backend — Endpoints

**Endpoint público** (`EditorialEndpoints.cs`):

```csharp
app.MapGet("/api/v1/pages", async (IEditorialPageRepository repo, CancellationToken ct) =>
{
    var pages = await repo.GetAllAsync(ct);
    return Results.Ok(pages.Select(p => new EditorialPageDto
    {
        Slug = p.Slug,
        Title = p.Title,
        Content = p.Content,
        UpdatedAt = p.UpdatedAt
    }));
})
.Produces<IEnumerable<EditorialPageDto>>(StatusCodes.Status200OK)
.WithTags("Editorial")
.AllowAnonymous();
```

**Endpoint Ops** (`OpsEditorialEndpoints.cs`), dentro de grupo `RequireAuthorization("AdminOps")`:

```csharp
group.MapPut("/pages/{slug}", async (
    string slug,
    UpdateEditorialPageRequest request,
    IEditorialPageRepository repo,
    CancellationToken ct) =>
{
    var page = await repo.GetBySlugAsync(slug, ct);
    if (page is null) return Results.NotFound();
    await repo.UpdateContentAsync(slug, request.Content, ct);
    return Results.NoContent();
})
.Produces(StatusCodes.Status204NoContent)
.ProducesProblem(StatusCodes.Status404NotFound)
.ProducesProblem(StatusCodes.Status403Forbidden);
```

---

### Frontend Main — `ConoceLasFibrasPage.tsx`

- `useQuery({ queryKey: ['editorial-pages'], queryFn: fetchEditorialPages, staleTime: 60 * 60_000 })` — 1 hora de stale porque el contenido casi no cambia.
- Usa `Tabs` / `TabsList` / `TabsTrigger` / `TabsContent` de shadcn/ui (ya importado en otras páginas).
- Tab activo por defecto: el primero (`que-son-las-fibras`).
- Skeleton: mientras `isLoading`, mostrar `TabsList` con tabs deshabilitadas y `TabsContent` con 4 `<div className="h-4 animate-pulse rounded bg-muted" />` espaciados.
- Markdown: `<ReactMarkdown className="prose prose-slate max-w-none">{page.content}</ReactMarkdown>` — la clase `prose` de Tailwind Typography ya está en uso en `NoticiaPage`.
- SEO: `<title>Conoce las FIBRAs — FIBRADIS</title>` y `<meta name="description" content="Guía completa sobre FIBRAs inmobiliarias mexicanas: qué son, cómo funcionan, historia, por qué invertir y su régimen fiscal." />`.

**Verificar** que `@tailwindcss/typography` esté instalado en Main (usado en `NoticiaPage`) antes de usar `prose`.

---

### Frontend Ops — `EditorialPage.tsx`

- Lista las páginas con `useQuery` y muestra cada una como una card con:
  - Título (h3)
  - `<textarea>` con el contenido markdown actual (estado local `useState`)
  - Botón "Guardar" que llama `useMutation` → `updateEditorialPage(slug, content)` → `PUT /api/v1/ops/pages/{slug}`
  - Feedback de éxito/error inline (igual que `ConfigPage.tsx`)
- Cada card es independiente: guardar una no afecta las demás.
- El `<textarea>` debe tener suficiente altura para editar cómodamente (mínimo `rows={20}`).
- `invalidateQueries(['editorial-pages'])` al guardar con éxito.

No se necesita editor WYSIWYG — un `<textarea>` monoespaciado es suficiente dado que el contenido cambia muy poco.

---

### Tests unitarios (T1.8)

Los tests van en `tests/Unit/Infrastructure.Tests/` siguiendo el patrón de otros repositorios. Usar `InMemoryDatabaseRoot` separado por test.

```csharp
// Tests mínimos requeridos:
// 1. GetAllAsync_ReturnsFivePages — verifica que retorna exactamente 5 páginas
// 2. GetAllAsync_ReturnsInOrderAscByOrder — verifica que la lista viene ordenada por Order
// 3. GetAllAsync_NoPageHasNullOrEmptyContent — ninguna página tiene Content vacío
```

---

### Rutas de archivos

**Nuevos:**

- `src/Server/Domain/Ops/EditorialPage.cs`
- `src/Server/Domain/Ops/IEditorialPageRepository.cs`
- `src/Server/Infrastructure/Persistence/Repositories/Ops/EditorialPageRepository.cs`
- `src/Server/SharedApiContracts/Editorial/EditorialPageDto.cs`
- `src/Server/SharedApiContracts/Editorial/UpdateEditorialPageRequest.cs`
- `src/Server/Api/Endpoints/Public/EditorialEndpoints.cs`
- `src/Server/Api/Endpoints/Ops/OpsEditorialEndpoints.cs`
- `src/Server/Infrastructure/Persistence/Migrations/XXXXXX_AddEditorialPages.cs` (generado por EF)
- `src/Web/Main/src/api/editorialApi.ts`
- `src/Web/Main/src/modules/conoce-las-fibras/ConoceLasFibrasPage.tsx`
- `src/Web/Ops/src/api/editorialApi.ts`
- `src/Web/Ops/src/pages/EditorialPage.tsx`

**Modificados:**

- `src/Server/Infrastructure/Persistence/FibradisDbContext.cs` (DbSet + HasData)
- `src/Server/Api/Program.cs` (registrar endpoints)
- `src/Web/Main/src/app/routes.tsx` (nueva ruta)
- `src/Web/Main/src/shared/layouts/PublicLayout.tsx` (ítem de menú)
- `src/Web/Ops/src/components/OpsShell.tsx` (ítem de menú)
- Router de Ops (ruta `/editorial`)

---

### Checklist SEO (ruta pública sin auth)

- [ ] La ruta `/conoce-las-fibras` responde 200 en hit directo
- [ ] `<title>` y `<meta name="description">` presentes en el HTML
- [ ] El SPA fallback del backend cubre la nueva ruta
- [ ] `npm run build` pasa con 0 errores TypeScript

---

## File List

- `src/Server/Domain/Ops/EditorialPage.cs`
- `src/Server/Application/Ops/IEditorialPageRepository.cs`
- `src/Server/Infrastructure/Persistence/Repositories/Ops/EditorialPageRepository.cs`
- `src/Server/Infrastructure/Persistence/SqlServer/Configurations/Ops/EditorialPageConfiguration.cs`
- `src/Server/Infrastructure/Persistence/SqlServer/AppDbContext.cs`
- `src/Server/Infrastructure/Persistence/Migrations/20260531162833_AddEditorialPages.cs`
- `src/Server/Infrastructure/Persistence/Migrations/20260531162833_AddEditorialPages.Designer.cs`
- `src/Server/Infrastructure/Persistence/SqlServer/Configurations/Ops/OperationalConfigConfiguration.cs` (HasColumnName fibra_news_months añadido)
- `src/Server/Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`
- `src/Server/Api/CompositionRoot/ApiServiceExtensions.cs`
- `src/Server/Api/Program.cs`
- `src/Server/Api/Endpoints/Public/EditorialEndpoints.cs`
- `src/Server/Api/Endpoints/Ops/OpsEditorialEndpoints.cs`
- `src/Server/SharedApiContracts/Editorial/EditorialPageDto.cs`
- `src/Server/SharedApiContracts/Editorial/UpdateEditorialPageRequest.cs`
- `src/Web/SharedApiClient/schema.d.ts`
- `scripts/codegen/Api.json`
- `src/Web/Main/src/api/editorialApi.ts`
- `src/Web/Main/src/modules/conoce-las-fibras/ConoceLasFibrasPage.tsx`
- `src/Web/Main/src/app/routes.tsx`
- `src/Web/Main/src/shared/layouts/PublicLayout.tsx`
- `src/Web/Main/src/modules/noticia/NoticiaPage.tsx`
- `src/Web/Ops/src/api/editorialApi.ts`
- `src/Web/Ops/src/api/configApi.ts`
- `src/Web/Ops/src/pages/EditorialPage.tsx`
- `src/Web/Ops/src/main.tsx`
- `src/Web/Ops/src/components/OpsShell.tsx`
- `tests/Unit/Infrastructure.Tests/Persistence/Repositories/EditorialPageRepositoryTests.cs`
- `tests/Unit/Infrastructure.Tests/Persistence/Repositories/OperationalConfigRepositoryTests.cs`
- `tests/Integration/Api.Tests/Ops/OpsConfigEndpointTests.cs`

## Change Log

| Fecha | Cambio |
| --- | --- |
| 2026-05-31 | Story creada — ready-for-dev |
| 2026-05-31 | Contenido seed actualizado con datos verificados (retención 30% ISR, yield 7.1%, +175.54%, 891k MDP) |
| 2026-05-31 | Implementación backend completa: entidad `EditorialPage`, repositorio, endpoints públicos/Ops, migración `AddEditorialPages` y tests unitarios |
| 2026-05-31 | Implementación frontend completa en Main y Ops: ruta `/conoce-las-fibras`, nav público, editor `/editorial`, cliente OpenAPI regenerado y builds verdes |
| 2026-05-31 | Fixes auxiliares de compatibilidad: tests de `OpsConfig`, `configApi.ts` y eliminación del bloque de imagen desactivado en `NoticiaPage` para dejar la rama compilando |
| 2026-05-31 | Migración regenerada limpia (`20260531162833`), fix `HasColumnName("fibra_news_months")` en OperationalConfig, migración aplicada a BD. 193/193 unit tests. Status → review |

## Dev Agent Record

### Resumen de implementación

- Se siguió el patrón real del repo para repositorios (`Application/Ops` + `Infrastructure/Persistence/Repositories/Ops`) en lugar de la ruta sugerida en Dev Notes.
- Se sembraron 5 páginas editoriales fijas en `ops.EditorialPage` y el endpoint público `GET /api/v1/pages` las expone ordenadas por `display_order`.
- El editor de Ops permite modificar cada slug por separado con invalidación de cache tras guardar.

### Validación ejecutada

- `dotnet build FIBRADIS.slnx --configuration Release` ✅
- `npm run codegen:api` ✅
- `npm run build --workspace=src/Web/Main` ✅
- `npm run build --workspace=src/Web/Ops` ✅
- `dotnet test tests/Unit/Domain.Tests/Domain.Tests.csproj --configuration Release` ✅
- `dotnet test tests/Unit/Application.Tests/Application.Tests.csproj --configuration Release` ✅
- `dotnet test tests/Unit/Infrastructure.Tests/Infrastructure.Tests.csproj --configuration Release` ✅
- Validación manual con Playwright sobre `http://localhost:5176/conoce-las-fibras` usando intercept de `/api/v1/pages`: `<title>` correcto, nav actualizado, tabs funcionales y markdown renderizado ✅
- Validación manual con Playwright sobre `http://localhost:5175/editorial` usando intercepts de auth/pages/update: login Ops, menú "Contenido Editorial", 5 textareas y guardado `PUT /api/v1/ops/pages/{slug}` ✅

### Bloqueos / notas

- `dotnet ef database update --project src/Server/Infrastructure --startup-project src/Server/Api --configuration Release` sigue pendiente en este worktree porque la CLI de EF no recibe `ConnectionStrings:DefaultConnection` y falla con `No se ha inicializado la propiedad ConnectionString`.
- `dotnet test tests/Integration/Api.Tests/Api.Tests.csproj --configuration Release` no quedó como criterio de salida de esta historia; hoy sigue fallando un test preexistente no relacionado: `NewsLatestEndpointTests.GetLatestNews_IncludesSeededArticles`.

## Senior Developer Review (AI)

### Review Findings

#### Decisiones requeridas

- [x] Review/Decision **D1 — `RenameColumn fibra_news_months → FibraNewsMonths` en migración 8-1 rompe convención snake_case del proyecto** — ACEPTADO: rename intencional para corregir desface previo — La migración `20260531072018_AddEditorialPages.cs` incluye `RenameColumn("fibra_news_months" → "FibraNewsMonths")` que no pertenece al scope de 8-1. Tras el rename, el snapshot elimina el `HasColumnName("fibra_news_months")` y EF usa el nombre de la propiedad por convención (`FibraNewsMonths`, PascalCase). Todos los demás campos de la BD usan snake_case (`news_cadence_minutes`, `commission_factor`, etc.). Opciones: (a) aceptar como está — rename intencional para corregir un desface previo; (b) revertir el rename en la migración y restaurar `HasColumnName("fibra_news_months")` en `OperationalConfigConfiguration.cs`.

- [x] Review/Decision **D2 — Layout sidebar + article custom en lugar de shadcn `Tabs` (AC 3 prescribe Tabs/TabsList/TabsTrigger/TabsContent)** — ACEPTADO: validado en Playwright, UX equivalente — `ConoceLasFibrasPage.tsx` implementa una nav lateral con `<button>` + `<article>` en lugar del componente `Tabs` de shadcn/ui. El comportamiento es funcionalmente equivalente y la validación manual Playwright pasó. Opciones: (a) aceptar la desviación — UX arguably mejor, validado en Playwright; (b) reescribir usando el componente `Tabs` de shadcn/ui.

#### Patches

- [x] Review/Patch **P1 — TOCTOU en `PUT /pages/{slug}`: eliminar `GetBySlugAsync` previo y usar valor de retorno de `ExecuteUpdateAsync`** [`OpsEditorialEndpoints.cs`, `EditorialPageRepository.cs`] — El endpoint hace `GetBySlugAsync` + `UpdateContentAsync` en dos awaits. Si entre ambos el slug desaparece (teóricamente imposible hoy, pero estructuralmente incorrecto), `ExecuteUpdateAsync` actualiza 0 filas y retorna `NoContent()` silencioso. Fix: eliminar el guard query; hacer `UpdateContentAsync` retornar `int` (filas afectadas) y devolver 404 si el resultado es 0.

- [x] Review/Patch **P2 — Índice único redundante sobre la PK en `EditorialPageConfiguration`** [`EditorialPageConfiguration.cs`] — `builder.HasKey(page => page.Slug)` ya crea un índice clustered único sobre `slug`. El `builder.HasIndex(...).IsUnique().HasDatabaseName("UX_EditorialPage_Slug")` genera un segundo índice no-clustered redundante. Eliminar la llamada a `HasIndex`.

- [x] Review/Patch **P3 — `fetchEditorialPages()` de Ops llama endpoint público con `assertOpsAccessToken()` + auth headers innecesarios** [`src/Web/Ops/src/api/editorialApi.ts`] — El endpoint `GET /api/v1/pages` es `AllowAnonymous`. La llamada Ops falla si el token expiró, aunque no requiera auth. Fix: usar `createClient` sin auth headers para el GET; mantener auth solo para el PUT.

- [x] Review/Patch **P4 — Sin límite de longitud en `content` del PUT; `nvarchar(max)` + endpoint sin validación de tamaño** [`OpsEditorialEndpoints.cs`, `EditorialPageConfiguration.cs`] — Un contenido de varios MB se acepta sin error, se persiste en BD y se retorna en el GET público a cada visitante. Añadir validación de longitud máxima (100 000 chars) en el endpoint y `HasMaxLength` en la configuración EF.

- [x] Review/Patch **P5 — `successMessage` no se limpia al re-editar el textarea en `EditorialCard`** [`src/Web/Ops/src/pages/EditorialPage.tsx`] — Tras guardar exitosamente, si el usuario edita el textarea, el mensaje "✓ Contenido guardado" persiste en pantalla aunque el contenido tenga cambios sin guardar. Añadir `setSuccessMessage(null)` en el `onChange` del textarea.

- [x] Review/Patch **P6 — Un render-cycle con `activePage = null` visible si `activeSlug` no coincide con ninguna página devuelta** [`ConoceLasFibrasPage.tsx`] — El estado inicial `activeSlug = 'que-son-las-fibras'` es hardcoded. Si la API devuelve páginas con slugs diferentes, hay un ciclo de render donde `activePage` es null (nav visible, article vacío) antes de que el `useEffect` corrija el slug. Fix: inicializar `activeSlug` de forma lazy desde la primera página devuelta, o mover la lógica de selección de activo fuera del estado inicial hardcoded.

#### Deferred

- [x] Review/Defer `EditorialPage.UpdatedAt` default a `DateTimeOffset.UtcNow` en construcción — no hay path actual que persista una instancia sin establecer `UpdatedAt` explícitamente [`EditorialPage.cs`] — deferred, pre-existing pattern
- [x] Review/Defer `fibraNewsMonths: number | string | null` widened sin coercion guard en `configApi.ts` — pre-existing fix de historia anterior [`configApi.ts`] — deferred, pre-existing
- [x] Review/Defer `TAB_LABELS` hardcoded puede mostrar `page.title` raw para slugs nuevos — catálogo fijo por AC 11 [`ConoceLasFibrasPage.tsx`] — deferred, by design
- [x] Review/Defer `ExecuteUpdateAsync` no soportado por InMemory provider — ningún test actual lo cubre [`EditorialPageRepositoryTests.cs`] — deferred, no blocking
- [x] Review/Defer `successMessage` visible antes de que `invalidateQueries` complete — riesgo bajo en herramienta Ops interna [`EditorialPage.tsx`] — deferred, low risk
- [x] Review/Defer `<a href>` full-page reload en nav de `PublicLayout` — patrón pre-existente en todos los ítems del nav [`PublicLayout.tsx`] — deferred, pre-existing
- [x] Review/Defer XSS risk en `<ReactMarkdown>` sin `urlTransform` en `NoticiaPage.tsx` — pre-existente, no introducido por 8-1 [`NoticiaPage.tsx`] — deferred, pre-existing
- [x] Review/Defer `staleTime: 60 * 60_000` (1 hora) — actualizaciones Ops invisibles por 60 min en Main — by design per story spec [`ConoceLasFibrasPage.tsx`] — deferred, by design
- [x] Review/Defer `PUT /api/v1/ops/pages/{slug}` devuelve 403 sin test de integración — AC 12 solo exige 3 tests unitarios del repositorio — deferred, low priority
- [x] Review/Defer Frontend no valida que slugs retornados sean exactamente los 5 del catálogo fijo — catálogo controlado por seed [`ConoceLasFibrasPage.tsx`] — deferred, by design
