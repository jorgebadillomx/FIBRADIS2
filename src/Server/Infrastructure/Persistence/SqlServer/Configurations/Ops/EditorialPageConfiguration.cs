using Domain.Ops;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.SqlServer.Configurations.Ops;

public class EditorialPageConfiguration : IEntityTypeConfiguration<EditorialPage>
{
    private static readonly DateTimeOffset SeedUpdatedAt = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public void Configure(EntityTypeBuilder<EditorialPage> builder)
    {
        builder.ToTable("EditorialPage", schema: "ops");
        builder.HasKey(page => page.Slug);

        builder.Property(page => page.Slug)
            .HasColumnName("slug")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(page => page.Title)
            .HasColumnName("title")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(page => page.Content)
            .HasColumnName("content")
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(page => page.Order)
            .HasColumnName("display_order")
            .IsRequired();

        builder.Property(page => page.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.HasData(
            CreatePage(
                "que-son-las-fibras",
                "¿Qué son las FIBRAs?",
                1,
                """
                ## Fideicomisos de Inversión en Bienes Raíces

                Las FIBRAs son vehículos de inversión que permiten a cualquier persona participar en el mercado inmobiliario institucional mexicano sin necesidad de comprar propiedades directamente. Son fideicomisos constituidos conforme a la Ley General de Títulos y Operaciones de Crédito, con régimen fiscal especial regulado en los artículos 187 y 188 de la Ley del Impuesto Sobre la Renta (LISR).

                ## ¿Cómo funciona un fideicomiso FIBRA?

                El fideicomiso adquiere y administra inmuebles que generan renta: centros comerciales, parques industriales, hoteles, oficinas corporativas y almacenes logísticos. Los inversionistas compran **CBFIs** (Certificados Bursátiles Fiduciarios Inmobiliarios) en la BMV o en la BIVA. Las rentas cobradas a los inquilinos, menos gastos operativos y deuda, se distribuyen entre los tenedores en proporción a su participación.

                Para calificar como FIBRA y acceder al régimen fiscal preferente, el fideicomiso debe cumplir tres requisitos estructurales:

                - **Mínimo 70%** del patrimonio invertido en inmuebles para arrendamiento
                - Los inmuebles deben permanecer en el fideicomiso al menos **cuatro años**
                - Distribuir al menos el **95% del resultado fiscal anual**

                ## Los CBFIs: qué son y cómo se negocian

                Un CBFI representa una participación en el patrimonio del fideicomiso. No es una acción tradicional; da derecho a recibir distribuciones, participar en la apreciación del portafolio y votar en asambleas de tenedores.

                Los tickers bursátiles siguen la convención de combinar el nombre abreviado con un número de serie: FUNO11, FMTY14, FIBRAPL14 o DANHOS13. Esto permite que inversionistas individuales entren al sector con montos pequeños comparados con la compra directa de inmuebles.

                ## Tipos de inmuebles

                | Segmento | Características clave |
                |---|---|
                | **Industrial y logístico** | Naves manufactureras, parques industriales y centros de distribución |
                | **Comercial** | Centros comerciales y plazas de retail |
                | **Corporativo** | Oficinas clase A en principales ciudades |
                | **Hotelero** | Hoteles de negocios y resorts |
                | **Mixto** | Combinación de varios segmentos |

                ## ¿En qué se diferencian de los REITs de EE.UU.?

                Las FIBRAs son el equivalente mexicano de los REITs. Comparten la obligación de distribuir la mayor parte del flujo, pero en México operan como fideicomisos y cuentan con un tratamiento fiscal local específico.

                ## ¿Quién puede invertir?

                Cualquier persona física o moral con acceso a una casa de bolsa o plataforma de inversión puede comprar CBFIs. También participan Afores, aseguradoras y fondos institucionales, lo que aporta profundidad al mercado.
                """),
            CreatePage(
                "historia",
                "Historia",
                2,
                """
                ## De la regulación al mercado

                Las reglas iniciales para este tipo de instrumento surgieron en 2004, pero fue hasta **2011** cuando el mercado tomó forma con la salida a bolsa de **FIBRA Uno (FUNO11)**.

                ### 18 de marzo de 2011: el primer CBFI

                FUNO11 realizó su Oferta Pública Inicial con un portafolio inicial de 16 inmuebles y un precio de salida de **19.50 pesos por CBFI**. Ese evento marcó el nacimiento formal del mercado de FIBRAs en México.

                ### Expansión 2012–2014

                Después de FUNO llegaron vehículos especializados en hotelería, industria y retail. También comenzaron adquisiciones de gran escala que consolidaron al instrumento como una fuente relevante de capital inmobiliario.

                ### Consolidación 2015–2019

                En 2015 se formalizó el índice **S&P/BMV FIBRAS**, y ese mismo año surgió el concepto de **FIBRA E** para energía e infraestructura. Esto amplió el universo de activos securitizables bajo la misma lógica de distribuciones recurrentes.

                ### Prueba de estrés: pandemia 2020

                La pandemia afectó sobre todo a los segmentos comercial y hotelero. Las industriales resistieron mejor y posteriormente se beneficiaron del nearshoring.

                ### Mercado maduro 2021–2025

                Para 2025 el sector acumulaba cientos de miles de millones de pesos en activos administrados, miles de propiedades y una ocupación promedio robusta. El instrumento dejó de ser experimental y pasó a ser una categoría establecida dentro del mercado público mexicano.
                """),
            CreatePage(
                "como-se-estructuran",
                "¿Cómo se estructuran?",
                3,
                """
                ## La arquitectura jurídica de una FIBRA

                Una FIBRA existe como un **fideicomiso irrevocable**. El fideicomiso es el propietario legal de los inmuebles y el emisor de los CBFIs.

                ### Actores principales

                - **Fideicomitentes**: aportan inmuebles o efectivo al vehículo
                - **Fiduciario**: institución financiera que mantiene la titularidad legal
                - **Fideicomisarios**: tenedores de CBFIs
                - **Administrador**: opera inmuebles, cobra rentas y ejecuta la estrategia
                - **Comité Técnico**: órgano de gobierno equivalente a un consejo

                ### Asamblea de Tenedores

                Es el máximo órgano de decisión. Cada CBFI representa derechos económicos y, en los supuestos aplicables, derechos de voto.

                ### Reguladores relevantes

                | Autoridad | Función |
                |---|---|
                | **CNBV** | Supervisión del mercado y obligaciones de revelación |
                | **BMV / BIVA** | Listado y negociación secundaria |
                | **SAT** | Vigilancia del cumplimiento del régimen fiscal especial |

                ### Distribución mínima del 95%

                El requisito de distribuir al menos el **95% del resultado fiscal** es uno de los rasgos más distintivos del instrumento. Esto obliga a que gran parte del flujo llegue a los inversionistas de forma periódica.

                ### FIBRA inmobiliaria vs FIBRA E

                Las FIBRAs inmobiliarias invierten en bienes raíces para arrendamiento. Las **FIBRA E** monetizan flujos o activos de energía e infraestructura, pero comparten la lógica de vehículo listado y tratamiento fiscal especializado.
                """),
            CreatePage(
                "por-que-invertir",
                "Por qué invertir",
                4,
                """
                ## Razones para considerar FIBRAs en un portafolio

                ### Acceso al inmobiliario institucional

                Comprar directamente un inmueble institucional exige mucho capital, poca diversificación y gestión operativa. Las FIBRAs permiten exposición a ese mercado con tickets significativamente menores.

                ### Liquidez bursátil

                Un inmueble directo puede tardar meses en venderse. Un CBFI puede comprarse o venderse en bolsa en tiempo real durante la sesión del mercado.

                ### Distribuciones periódicas

                El sector ha sido atractivo históricamente por su capacidad de entregar distribuciones recurrentes. El rendimiento exacto cambia por emisora, tasas y valuaciones, pero el marco obliga a repartir flujo.

                ### Diversificación y gestión profesional

                Con un solo instrumento puedes acceder a múltiples inmuebles, ciudades, inquilinos y segmentos. Además, la administración corre por cuenta de equipos especializados.

                ---

                ## Riesgos reales

                Ninguna tesis de inversión está completa sin riesgos claros:

                - **Tasas de interés**: cuando suben, los CBFIs suelen presionarse
                - **Vacancia**: menos ocupación implica menos flujo
                - **Tipo de cambio**: relevante en portafolios dolarizados
                - **Dilución**: nuevas emisiones pueden reducir rendimiento por certificado
                - **Gobierno corporativo**: la alineación del administrador importa

                El análisis debe considerar tanto yield como calidad del portafolio, deuda, ocupación y disciplina de capital.
                """),
            CreatePage(
                "regimen-fiscal",
                "Régimen fiscal",
                5,
                """
                ## Tratamiento fiscal general de las FIBRAs

                > Esta sección es educativa y no sustituye asesoría fiscal profesional.

                ### Transparencia fiscal del fideicomiso

                El fideicomiso determina un resultado fiscal y lo distribuye a los tenedores. El tratamiento final depende del tipo de inversionista.

                ### Personas físicas residentes en México

                En términos generales, las distribuciones provenientes del resultado fiscal están sujetas a retención provisional. Una parte puede corresponder a reembolso de capital, con tratamiento distinto al momento del cobro.

                ### Ganancia de capital en bolsa

                Uno de los atributos más relevantes es que, bajo las condiciones aplicables, la ganancia por venta bursátil de CBFIs puede tener un tratamiento fiscal más favorable que otros instrumentos patrimoniales.

                ### Personas morales, fondos y extranjeros

                El efecto fiscal cambia según si el inversionista es persona moral, fondo de pensiones o residente en el extranjero. Siempre conviene revisar el régimen concreto y, si aplica, tratados internacionales.

                ### Resumen práctico

                | Tipo de inversionista | Distribuciones | Venta bursátil |
                |---|---|---|
                | Persona física residente | Retención provisional y acumulación anual | Tratamiento fiscal específico del régimen FIBRA |
                | Persona moral residente | Acumulación corporativa | Depende del régimen aplicable |
                | Fondos elegibles | Beneficios particulares bajo reglas específicas | Revisar reglas del vehículo |
                | Extranjero | Sujeto a retenciones o tratado | Depende de LISR y tratado |

                Antes de invertir, conviene entender si tu tesis depende del flujo distribuido, de la apreciación de capital o de ambas.
                """));
    }

    private static EditorialPage CreatePage(string slug, string title, int order, string content) => new()
    {
        Slug = slug,
        Title = title,
        Order = order,
        UpdatedAt = SeedUpdatedAt,
        Content = content,
    };
}
