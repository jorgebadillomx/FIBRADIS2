using Domain.Ops;
using Domain.Seo;

namespace Application.Seo;

public static class FaqSeedFactory
{
    private static readonly DateTimeOffset SeedUpdatedAt = new(2026, 6, 19, 0, 0, 0, TimeSpan.Zero);

    private static readonly IReadOnlyDictionary<string, string> EditorialQuestions =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["que-son-las-fibras"] = "¿Qué son las FIBRAs?",
            ["historia"] = "¿Cuál es la historia de las FIBRAs?",
            ["como-se-estructuran"] = "¿Cómo se estructuran las FIBRAs?",
            ["por-que-invertir"] = "¿Por qué invertir en FIBRAs?",
            ["regimen-fiscal"] = "¿Cuál es el régimen fiscal de las FIBRAs?",
        };

    public static IReadOnlyList<FaqItem> BuildEditorialItems(IEnumerable<EditorialPage> pages)
        => pages
            .OrderBy(page => page.Order)
            .Select(page => new FaqItem
            {
                Id = Guid.NewGuid(),
                PageType = SeoPageType.StaticPage,
                EntityKey = GetEditorialEntityKey(),
                Question = EditorialQuestions.TryGetValue(page.Slug, out var question)
                    ? question
                    : page.Title,
                Answer = page.Content.Trim(),
                Order = page.Order,
                IsActive = true,
                UpdatedAt = page.UpdatedAt,
                UpdatedBy = "system",
            })
            .ToList();

    public static IReadOnlyList<FaqItem> BuildFundamentalsItems() =>
    [
        CreateFundamentalsItem(
            1,
            "¿Qué es Cap Rate?",
            "**Fórmula:** Cap Rate = NOI anualizado / Valor de propiedades de inversión\n\n" +
            "Tasa de capitalización que mide el rendimiento operativo neto del portafolio inmobiliario " +
            "como proporción de su valor de inversión, sin considerar el efecto del apalancamiento ni el " +
            "costo financiero de la deuda. Un Cap Rate alto implica mayor rendimiento bruto y generalmente " +
            "mayor riesgo del activo o ubicaciones secundarias; uno bajo refleja propiedades premium con " +
            "demanda constante — centros logísticos clase A o centros comerciales ancla en zonas de alta " +
            "afluencia. Para las FIBRAs mexicanas, el rango habitual oscila entre 6 % y 10 % según el " +
            "segmento: las industriales como FIBRAMQ12 tienden a Cap Rates superiores a las de oficinas o " +
            "comercio. Al comparar dos FIBRAs, un Cap Rate de 9 % vs. 6.5 % no significa automáticamente " +
            "que la primera sea mejor inversión; debe evaluarse junto con el LTV y la calidad del portafolio. " +
            "La CNBV exige que todas las FIBRAs reporten el NOI y el valor de sus propiedades de inversión " +
            "en sus informes trimestrales."),
        CreateFundamentalsItem(
            2,
            "¿Qué es NAV por CBFI?",
            "**Fórmula:** NAV = Valor de propiedades − Deuda total | NAV/CBFI = NAV / CBFIs en circulación\n\n" +
            "Valor Neto de los Activos por certificado bursátil: indica si el precio de mercado de la FIBRA " +
            "cotiza con **descuento** o **prima** respecto al valor intrínseco de los activos inmobiliarios " +
            "que respaldan cada CBFI. Un precio menor al NAV/CBFI sugiere que el mercado valúa la emisora " +
            "por debajo del valor de su portafolio — oportunidad potencial si el descuento es sostenido. Un " +
            "precio mayor indica que el mercado paga una prima, generalmente justificada por calidad de la " +
            "administración, contratos de largo plazo o expectativas de crecimiento. Por ejemplo, si el " +
            "NAV/CBFI de DANHOS13 es $35.00 y cotiza a $29.00, el descuento es del 17 %. La CNBV exige " +
            "reportar el valor de propiedades de inversión cada trimestre; AMEFIBRA incluye el ratio " +
            "Precio/NAV como métrica estándar en su análisis comparativo del sector de FIBRAs."),
        CreateFundamentalsItem(
            3,
            "¿Qué es LTV?",
            "**Fórmula:** LTV = Deuda total / Valor de propiedades de inversión\n\n" +
            "Loan-to-Value: nivel de apalancamiento financiero medido como la proporción de la deuda total " +
            "sobre el valor de mercado de las propiedades de inversión. Un LTV bajo — típicamente bajo 40 % — " +
            "indica solidez financiera y menor vulnerabilidad ante caídas en la valuación de activos o alzas " +
            "en tasas de interés; un LTV alto señala mayor exposición al riesgo de refinanciamiento y presión " +
            "sobre las distribuciones en períodos de tensión. Para las FIBRAs mexicanas reguladas por la CNBV, " +
            "el límite máximo de endeudamiento establecido en la normativa es del 60 % del valor total de " +
            "activos — aunque la mayoría opera entre 25 % y 45 %. Un LTV de 28 % ofrece más margen de " +
            "maniobra que uno de 45 % para adquirir nuevas propiedades sin diluir a los tenedores de CBFIs. " +
            "El dato se publica en los estados financieros trimestrales que cada FIBRA entrega a la CNBV."),
        CreateFundamentalsItem(
            4,
            "¿Qué es NOI Margin?",
            "**Fórmula:** NOI Margin = NOI / Ingresos Totales\n\n" +
            "Margen de Ingreso Neto Operativo: porcentaje de los ingresos totales que permanece después de " +
            "descontar los gastos directos de operación del portafolio inmobiliario — mantenimiento, " +
            "administración de propiedades, seguros, impuestos locales y honorarios del fiduciario. Mide la " +
            "eficiencia operativa del portafolio con independencia de la estructura de deuda. Un NOI Margin " +
            "alto — típicamente superior al 70 % para FIBRAs bien administradas — indica que una proporción " +
            "elevada de los ingresos por rentas se convierte en utilidad operativa disponible para distribuir " +
            "o invertir. Las FIBRAs industriales como FIBRAMQ12 o VESTA suelen alcanzar márgenes de " +
            "75 %–85 % gracias a contratos de arrendamiento triple-net, donde el inquilino asume los gastos " +
            "operativos. Las FIBRAs de uso mixto o comercial tienden a márgenes menores. La CNBV exige el " +
            "reporte desglosado de NOI en los estados financieros trimestrales de cada emisora."),
        CreateFundamentalsItem(
            5,
            "¿Qué es FFO Margin?",
            "**Fórmula:** FFO Margin = FFO / Ingresos Totales | FFO = Utilidad Neta + ajustes por valuación − ganancias cambiarias\n\n" +
            "Fondos de Operación como proporción de los ingresos totales. El FFO corrige la utilidad neta " +
            "contable eliminando tres distorsiones que no reflejan la generación real de efectivo: las " +
            "ganancias o pérdidas por valuación de propiedades a valor razonable (ajustes no realizados), " +
            "las ganancias o pérdidas cambiarias — relevantes para FIBRAs con deuda en dólares como " +
            "FIBRAMQ12 — y la depreciación contable, que en bienes raíces no implica pérdida de valor real " +
            "si el portafolio está bien mantenido. El resultado es una métrica más cercana al flujo de caja " +
            "operativo real. Por convención adoptada por AMEFIBRA y alineada con estándares de la NAREIT " +
            "estadounidense, el FFO es la base preferida para calcular el payout ratio de distribuciones, " +
            "ya que refleja mejor la capacidad real de pago de la FIBRA."),
        CreateFundamentalsItem(
            6,
            "¿Qué es la distribución trimestral?",
            "**Fórmula:** Distribución = Resultado Fiscal Distribuido + Reembolso de Capital\n\n" +
            "Pago en efectivo que la FIBRA entrega a cada tenedor de CBFIs registrado al cierre de cada " +
            "trimestre fiscal. El monto se decreta en la asamblea de tenedores o por el comité técnico del " +
            "fideicomiso y se publica como aviso en la Bolsa Mexicana de Valores con anterioridad a la " +
            "fecha ex-derecho. Conforme a la Ley del ISR y la regulación de la CNBV, la distribución puede " +
            "componerse de dos partes: la **parte fiscal** — correspondiente a utilidades fiscales " +
            "distribuibles, que el inversionista declara como ingreso gravable — y el **reembolso de " +
            "capital** — que no tributa como ingreso sino que reduce el costo de adquisición del CBFI para " +
            "efectos fiscales futuros. Por ejemplo, si FUNO11 distribuye $0.60 por CBFI y $0.15 son " +
            "reembolso de capital, solo $0.45 se declaran como ingreso en el período. Las FIBRAs están " +
            "obligadas a distribuir al menos el 95 % de su resultado fiscal neto."),
    ];

    private static FaqItem CreateFundamentalsItem(int order, string question, string answer) => new()
    {
        Id = Guid.NewGuid(),
        PageType = SeoPageType.StaticPage,
        EntityKey = "/fundamentales",
        Question = question,
        Answer = answer.Trim(),
        Order = order,
        IsActive = true,
        UpdatedAt = SeedUpdatedAt,
        UpdatedBy = "system",
    };

    public static IReadOnlyList<FaqItem> BuildStaticPagesItems() =>
    [
        // /fibras — Página principal / universo de FIBRAs
        CreateStaticItem("/fibras", 1,
            "¿Qué información muestra la tabla del universo de FIBRAs?",
            "La tabla muestra todas las FIBRAs activas que cotizan en la Bolsa Mexicana de Valores con sus " +
            "métricas principales actualizadas durante la jornada bursátil: **Precio** de mercado en pesos " +
            "mexicanos, **Variación diaria** en pesos y porcentaje respecto al cierre anterior, **Volumen** " +
            "de CBFIs negociados en la sesión actual, **Máximo y Mínimo de 52 semanas** como referencia del " +
            "rango histórico del último año, **Yield anualizado** calculado a partir de la última distribución " +
            "trimestral reportada dividida entre el precio actual y multiplicada por cuatro, y el **período " +
            "del último reporte fundamental** disponible en la plataforma. Puedes ordenar por cualquier " +
            "columna — de mayor a menor o viceversa — con un clic en el encabezado, y filtrar por ticker " +
            "para localizar rápidamente una emisora específica como FUNO11 o FIBRAMQ12. El precio y el " +
            "volumen se actualizan en tiempo real; los datos fundamentales se refrescan cada vez que la " +
            "FIBRA publica sus reportes trimestrales ante la CNBV."),
        CreateStaticItem("/fibras", 2,
            "¿Qué son los Ganadores y Perdedores del día?",
            "Los bloques **Ganadores** y **Perdedores del día** muestran el top 5 de FIBRAs con mayor y " +
            "menor variación porcentual de precio en la sesión bursátil en curso, calculada respecto al " +
            "cierre de la jornada anterior. El ranking no se basa en el precio absoluto sino en el " +
            "movimiento porcentual: una emisora de precio bajo puede encabezar la lista si su variación es " +
            "alta. Ambos bloques se actualizan en tiempo real conforme avanza la jornada de la Bolsa " +
            "Mexicana de Valores y se reinician al abrir la siguiente sesión. Su utilidad principal es de " +
            "lectura rápida de mercado: permite detectar en segundos qué emisoras están respondiendo a " +
            "eventos corporativos — como resultados trimestrales, cambios en el portafolio de propiedades " +
            "o avisos ante la CNBV — o a movimientos generales del mercado de bienes raíces mexicano. " +
            "No sustituyen el análisis fundamental, pero son el primer indicador visual de actividad " +
            "intradiaria del sector de FIBRAs afiliadas a AMEFIBRA."),
        CreateStaticItem("/fibras", 3,
            "¿Qué es el Yield que aparece en el catálogo?",
            "El **Yield** del catálogo es el rendimiento anualizado estimado por distribuciones, calculado " +
            "con la fórmula: **(última distribución trimestral × 4) / precio de mercado actual**. Es un " +
            "indicador dinámico que varía en tiempo real: sube automáticamente cuando el precio de la FIBRA " +
            "cae y baja cuando el precio sube, aunque el monto de la distribución no cambie. Por ejemplo, " +
            "si FUNO11 distribuye $0.60 MXN por CBFI y cotiza a $20.00, su Yield sería del 12.0 % anual; " +
            "si el precio asciende a $22.00 con la misma distribución, el Yield cae a 10.9 %. Es importante " +
            "distinguirlo del **Yield decretado**, que utiliza el monto oficial aprobado por la asamblea del " +
            "fideicomiso en lugar de la estimación anualizada. El Yield del catálogo sirve como referencia " +
            "rápida de comparación entre emisoras, pero debe complementarse con el análisis de Cap Rate, " +
            "LTV y solidez operativa antes de cualquier decisión de inversión, conforme a las " +
            "recomendaciones de AMEFIBRA."),

        // /comparar — Comparador
        CreateStaticItem("/comparar", 1,
            "¿Qué secciones incluye el comparador de FIBRAs?",
            "El comparador organiza la información de hasta cuatro FIBRAs simultáneamente en cuatro " +
            "secciones estructuradas para análisis comparativo. **Mercado**: precio actual en pesos, " +
            "variación del día en pesos y porcentaje, promedio de precio de las últimas 52 semanas y " +
            "volumen de la sesión. **Fundamentales**: los cinco KPIs operativos clave — Cap Rate " +
            "(rendimiento del portafolio), NAV por CBFI (valor neto de activos), LTV (apalancamiento " +
            "sobre valor inmobiliario), Margen NOI (eficiencia operativa) y Margen FFO (fondos generados " +
            "de la operación). **Distribuciones**: distribución trimestral por CBFI en pesos, Yield " +
            "calculado a precio actual y Yield decretado en la última asamblea. **Score público**: " +
            "puntuación de oportunidad de 0 a 100 con sus cinco componentes. Esta estructura permite " +
            "responder en una vista si DANHOS13 supera a FIBRAMQ12 en Cap Rate aunque tenga menor Yield, " +
            "o cuál de las dos presenta menor LTV según sus reportes ante la CNBV."),
        CreateStaticItem("/comparar", 2,
            "¿Cómo indica el comparador cuál FIBRA gana en cada métrica?",
            "La celda con el mejor valor de cada métrica se resalta en **verde** para identificarla de un " +
            "vistazo sin necesidad de comparar cifras manualmente. Debajo de cada valor aparece la " +
            "diferencia respecto al segundo lugar: '+1.3 pp vs FUNO11' para métricas de porcentaje como " +
            "Cap Rate o Yield, o '+8.4 pts vs MXCD' para el score de oportunidad de 0-100. En métricas " +
            "donde **menor es mejor** — como el LTV o la ratio de deuda sobre activos — el resalte " +
            "corresponde al valor más bajo, no al más alto. Por ejemplo, si FIBRAMQ12 tiene LTV de 24 % " +
            "y FUNO11 de 38 %, se resalta el 24 % de FIBRAMQ12 como favorable. Para el NAV por CBFI, el " +
            "resalte indica la emisora cuyo precio de mercado está más alineado con su valor neto de " +
            "activos, dato que la CNBV exige reportar trimestralmente. La lógica de resalte es consistente " +
            "con los criterios de análisis publicados por AMEFIBRA para evaluación comparativa del sector."),
        CreateStaticItem("/comparar", 3,
            "¿Puedo compartir una comparación específica?",
            "Sí. El comparador codifica siempre la selección activa de FIBRAs directamente en los " +
            "parámetros de la URL del navegador. Al elegir FUNO11, DANHOS13 y FMTY14 en el comparador, " +
            "la barra de dirección muestra automáticamente una URL que incluye esos tickers como " +
            "parámetros. Para compartir o guardar la comparación, copia el enlace completo del navegador " +
            "y compártelo por correo, mensaje o guárdalo en favoritos. Quien abra el enlace verá " +
            "exactamente las mismas emisoras cargadas en el comparador, actualizadas con los precios y " +
            "métricas del momento en que abra la URL — no del momento en que se generó el enlace. Esto " +
            "facilita el análisis colaborativo: por ejemplo, enviar a tu asesor financiero la comparación " +
            "de tres FIBRAs industriales que estás evaluando. La selección no se guarda en ninguna cuenta " +
            "ni base de datos; el enlace es la única forma de persistir una comparación específica entre " +
            "sesiones."),

        // /noticias — Noticias
        CreateStaticItem("/noticias", 1,
            "¿Cómo buscar noticias de una FIBRA en particular?",
            "Usa el selector **'Filtrar por FIBRA'** en la parte superior del listado para ver únicamente " +
            "las noticias asociadas a una emisora específica: al elegir FUNO11, por ejemplo, el listado se " +
            "filtra automáticamente para mostrar solo los artículos en los que esa FIBRA es mencionada. " +
            "Puedes combinar ese filtro con la **búsqueda por título**: escribe 'resultados 2T25' y verás " +
            "únicamente noticias de FUNO11 que contengan esas palabras en el titular. El botón **'Limpiar " +
            "filtros'** restablece ambos criterios al mismo tiempo con un solo clic. Las noticias se " +
            "ordenan siempre de más reciente a más antiguo dentro de los filtros activos. La asociación " +
            "entre noticias y FIBRAs es automática: el sistema detecta menciones de tickers como DANHOS13 " +
            "o FIBRAMQ12 y nombres comerciales en el cuerpo de cada artículo antes de publicarlo. Si una " +
            "nota menciona varias emisoras — por ejemplo, un análisis sectorial de la CNBV — aparece en " +
            "el filtro de cada una de ellas de forma independiente."),
        CreateStaticItem("/noticias", 2,
            "¿Qué datos muestra cada nota en el listado de noticias?",
            "Cada tarjeta de noticia en el listado muestra: la **fuente** de la nota (portal financiero, " +
            "blog especializado en bienes raíces o comunicado oficial de la BMV), el **tiempo relativo de " +
            "publicación** (por ejemplo, 'hace 3 horas' o 'ayer'), el **titular completo**, un **extracto** " +
            "de las primeras líneas del contenido para evaluar relevancia sin abrir la nota, y las " +
            "**etiquetas de FIBRAs** vinculadas — como VESTA, FUNO11 o FIBRAMQ12. Cuando hay más de cuatro " +
            "emisoras asociadas a una nota, el listado muestra las primeras etiquetas y un indicador '+N " +
            "más' para no sobrecargar la vista. Al hacer clic en la tarjeta accedes al **detalle completo** " +
            "de la nota, que puede incluir, cuando está disponible, el análisis generado por inteligencia " +
            "artificial: resumen ejecutivo, impacto estimado en el sector, hechos clave y la visión del " +
            "inversor derivada del contenido. Las noticias provienen de fuentes especializadas en mercados " +
            "financieros y bienes raíces inmobiliarios de México."),

        // /calculadora — Calculadora de compra
        CreateStaticItem("/calculadora", 1,
            "¿Cómo funciona la calculadora de compra de FIBRAs?",
            "Ingresa en la columna **'$ a calcular'** el presupuesto en pesos mexicanos que destinarías a " +
            "cada FIBRA de tu interés. La tabla recalcula de inmediato cuatro métricas por emisora: " +
            "**CBFIs a comprar** — el número de certificados enteros que puedes adquirir con ese " +
            "presupuesto, ya que los CBFIs se compran en unidades enteras en la BMV; **$ Sobra** — el " +
            "remanente que no alcanza para el siguiente CBFI; **Distribución proyectada** — el ingreso " +
            "trimestral y anual estimado por esos CBFIs basado en el último pago decretado por la FIBRA; " +
            "y **Renta Bruta** — el rendimiento por distribuciones sobre tu inversión total expresado en " +
            "porcentaje anual. Puedes ingresar montos distintos para cada emisora: por ejemplo, $10,000 " +
            "para FUNO11 y $30,000 para FIBRAMQ12. Los cálculos usan el precio en tiempo real. La " +
            "calculadora no considera comisiones de corretaje ni impuestos; esos factores dependen del " +
            "intermediario y del régimen fiscal del inversionista, conforme a la normativa de la CNBV."),
        CreateStaticItem("/calculadora", 2,
            "¿Qué significa '$ Sobra' en la calculadora?",
            "Es el remanente de tu presupuesto que no alcanza para adquirir un CBFI adicional. Dado que " +
            "los certificados bursátiles se transan en unidades enteras en la BMV — igual que cualquier " +
            "acción —, es imposible comprar 10.6 CBFIs; solo puedes comprar 10. **Ejemplo numérico**: si " +
            "el precio de DANHOS13 es $32.50 MXN y tu presupuesto es $1,000, puedes comprar 30 CBFIs " +
            "($975.00) y te sobran $25.00 — insuficientes para el CBFI número 31. Si ajustas el " +
            "presupuesto a $1,040, compras 32 CBFIs ($1,040.00) y el sobrante es $0.00, maximizando el " +
            "uso del capital. El campo '$ Sobra' es útil para optimizar la asignación: con el dato " +
            "calculado decides si redondeas el presupuesto al múltiplo exacto del precio, o redistribuyes " +
            "ese remanente en otra emisora del catálogo como FMTY14 o VESTA. La calculadora actualiza el " +
            "sobrante en tiempo real si el precio de la FIBRA cambia durante la sesión bursátil."),
        CreateStaticItem("/calculadora", 3,
            "¿Los montos se pierden al filtrar u ordenar la tabla?",
            "No. La calculadora conserva los presupuestos que ingresaste en cada FIBRA incluso si cambias " +
            "el orden de las columnas, aplicas un filtro por nombre de emisora o modificas el criterio de " +
            "ordenamiento de la tabla. Por ejemplo: si ingresaste $8,000 para FUNO11 y $20,000 para " +
            "FIBRAMQ12, y luego ordenas la tabla por **Renta Bruta** de mayor a menor para comparar qué " +
            "emisora genera más ingresos de distribución con esos montos, los presupuestos de ambas " +
            "permanecen intactos y los cálculos se mantienen visibles. Esta persistencia es intencional: " +
            "permite explorar distintos ordenamientos para evaluar el rendimiento proyectado de tu " +
            "asignación sin volver a capturar los datos. Los montos no se guardan entre sesiones de " +
            "navegador: si cierras la pestaña o recargas la página, deberás reingresar los presupuestos. " +
            "Para vaciar todos los montos durante una sesión activa, recarga la página o limpia " +
            "manualmente cada campo."),

        // /calendario — Calendario de eventos corporativos
        CreateStaticItem("/calendario", 1,
            "¿Qué tipos de eventos muestra el calendario de FIBRAs?",
            "El calendario registra tres categorías de eventos corporativos para todas las FIBRAs activas " +
            "que cotizan en la Bolsa Mexicana de Valores. **Pagos**: la fecha en que la FIBRA deposita la " +
            "distribución en efectivo en las cuentas de los tenedores de CBFIs registrados; habitualmente " +
            "ocurre entre dos y cinco días hábiles después de la ex-fecha. **Ex derechos (ex-fecha)**: el " +
            "día a partir del cual quien compre CBFIs ya no tiene derecho al cobro de la distribución en " +
            "curso; comprar antes de esta fecha es requisito indispensable para recibir el pago. **Avisos " +
            "BMV**: comunicados oficiales publicados por la FIBRA ante la Bolsa Mexicana de Valores, que " +
            "incluyen convocatorias de asambleas de tenedores, reestructuras de portafolio, cambios en el " +
            "fiduciario o actualizaciones regulatorias. Cada categoría se distingue con un color en la " +
            "vista mensual. Los eventos se obtienen directamente de los registros oficiales de la BMV y " +
            "se actualizan conforme llegan los comunicados de cada emisora."),
        CreateStaticItem("/calendario", 2,
            "¿Qué información aparece al revisar un evento en el calendario?",
            "Al hacer clic sobre un evento del calendario se despliega un panel con su información " +
            "completa. Para los **pagos y ex-derechos** verás: la emisora, la fecha del evento, el " +
            "**monto de distribución por CBFI** en pesos mexicanos — por ejemplo, $0.582 por CBFI si esa " +
            "fue la distribución trimestral de FUNO11 en ese período —, el **desglose fiscal** entre la " +
            "parte distribuible (que tributa como ingreso para el tenedor según el ISR) y el **reembolso " +
            "de capital** (que reduce el costo de adquisición del CBFI y no tributa como ingreso " +
            "inmediato), y el **enlace al aviso oficial** publicado por la FIBRA en la plataforma de la " +
            "BMV cuando está disponible. Para los **Avisos BMV** se muestra el título del comunicado y un " +
            "vínculo al texto completo registrado ante el regulador. La vista mensual agrupa los eventos " +
            "del mismo día para facilitar la lectura de semanas con alta actividad, como el cierre de " +
            "trimestre cuando varias FIBRAs reportan simultáneamente."),
        CreateStaticItem("/calendario", 3,
            "¿Qué es la fecha ex derecho y por qué importa?",
            "La **fecha ex derecho** — o ex-fecha — es el día a partir del cual quien adquiera CBFIs de " +
            "una FIBRA ya no tiene derecho a cobrar la distribución del trimestre en curso. Para recibir " +
            "el pago es indispensable tener los CBFIs registrados en tu cuenta de inversión **antes** de " +
            "esa fecha. En México, la liquidación bursátil opera en T+2 (dos días hábiles): si la ex-fecha " +
            "es el martes, debiste comprar a más tardar el jueves anterior para que la operación liquide a " +
            "tiempo y figure como tenedor registrado. Quien compre el mismo día de la ex-fecha o después " +
            "no recibirá esa distribución; el vendedor — quien tenía los CBFIs registrados — cobrará en " +
            "su lugar. Esta es la razón por la que el precio de una FIBRA puede caer en la ex-fecha un " +
            "monto similar al de la distribución, fenómeno conocido como ajuste de dividendo. La BMV y la " +
            "CNBV exigen que las FIBRAs publiquen estas fechas con anticipación suficiente en sus avisos " +
            "oficiales."),

        // /portafolio — Landing pública / puerta de entrada
        CreateStaticItem("/portafolio", 1,
            "¿Qué puedo consultar en Fibras Inmobiliarias sin iniciar sesión?",
            "Las secciones públicas de Fibras Inmobiliarias están siempre disponibles sin necesidad de " +
            "crear una cuenta ni proporcionar ningún dato personal. El **Catálogo de FIBRAs** muestra " +
            "precios, variaciones diarias, volumen y métricas de mercado de las aproximadamente 20 " +
            "emisoras activas afiliadas a AMEFIBRA en la BMV. Los **Fundamentales comparativos** " +
            "presentan Cap Rate, NAV por CBFI, LTV, Margen NOI y Margen FFO actualizados cada vez que " +
            "una FIBRA publica sus reportes trimestrales ante la CNBV. El flujo de **Noticias** está " +
            "abierto en su totalidad: cualquier visitante puede leer artículos del sector inmobiliario y " +
            "financiero con sus etiquetas por emisora y, cuando está disponible, el análisis de " +
            "inteligencia artificial. La **Calculadora de compra** permite estimar distribuciones " +
            "proyectadas sin guardar ningún dato. El **Comparador de FIBRAs** y el **Calendario de " +
            "distribuciones** también son de acceso libre. Para explorar el universo completo de emisoras, " +
            "sus precios y fundamentales no hace falta ningún registro."),
        CreateStaticItem("/portafolio", 2,
            "¿Qué incluye el acceso privado con cuenta?",
            "Con una cuenta accedes a cuatro módulos exclusivos que requieren autenticación. El " +
            "**Portafolio privado** te permite registrar tu tenencia real de CBFIs por FIBRA y calcular " +
            "KPIs consolidados de tu cartera: valor total de mercado, yield efectivo sobre tu costo de " +
            "adquisición, distribuciones recibidas en el período y tu calendario personal de cobros " +
            "futuros. Los **Reportes trimestrales** concentran los fundamentales financieros — NOI, FFO, " +
            "Cap Rate, LTV, distribuciones — con análisis generado por inteligencia artificial para cada " +
            "FIBRA, estructurado para ahorrar las horas de lectura que implica revisar los reportes ante " +
            "la CNBV. El módulo de **Oportunidades y ranking** puntúa a todas las emisoras en una escala " +
            "de 0 a 100 con cinco componentes configurables: precio vs NAV, Cap Rate sectorial, solidez " +
            "financiera medida por LTV, momento de precio y score compuesto. Las **Herramientas privadas** " +
            "complementan el análisis con opciones avanzadas para inversionistas activos en el mercado de " +
            "FIBRAs."),
        CreateStaticItem("/portafolio", 3,
            "¿Cómo inicio sesión desde esta página?",
            "El formulario de acceso está integrado directamente dentro de la página `/portafolio`, sin " +
            "necesidad de navegar a una URL separada de inicio de sesión. Haz clic en cualquier botón " +
            "**Iniciar sesión** de la página o desplázate hasta la sección de acceso privado donde " +
            "encontrarás el formulario con los campos de correo electrónico y contraseña. Al autenticarte " +
            "correctamente, la misma página muestra el **dashboard privado** de tu portafolio sin " +
            "redirigirte ni recargar la aplicación — la transición es instantánea en la misma pestaña. " +
            "Si aún no tienes cuenta, el registro está disponible para nuevos usuarios; consulta la página " +
            "**Acerca** o contacta vía correo para solicitarlo. La sesión permanece activa durante un " +
            "período configurado; si expira, la plataforma mostrará el formulario de inicio de sesión " +
            "nuevamente al intentar acceder a las secciones privadas. Las credenciales se transmiten " +
            "cifradas mediante HTTPS conforme a los estándares de seguridad aplicables a plataformas de " +
            "análisis financiero en México."),
    ];

    public static IReadOnlyList<FaqItem> BuildPrivatePagesItems() =>
    [
        // /portafolio — Dashboard privado (vista autenticada)
        CreatePrivateItem("/portafolio", 1,
            "¿Cómo registro mis posiciones en el portafolio?",
            "Tienes dos opciones. La más rápida es cargar un archivo **Excel o CSV** con tres columnas: " +
            "`Ticker` (clave de la FIBRA, por ejemplo FUNO11), `Qty` (número de CBFIs que tienes) y " +
            "`AvgCost` (tu precio promedio de adquisición en pesos). La zona de carga acepta archivos " +
            "arrastrados o seleccionados desde el botón. La segunda opción es editar directamente en la " +
            "tabla: haz clic sobre el campo **Títulos** o **Costo promedio** de cualquier FIBRA, escribe " +
            "el valor y confirma con Enter. Ambas opciones actualizan el portafolio en tiempo real y " +
            "recalculan todos los KPIs de forma inmediata."),
        CreatePrivateItem("/portafolio", 2,
            "¿Cómo se calcula el rendimiento de mi portafolio?",
            "La plataforma calcula dos tipos de rendimiento. El **rendimiento en pesos** compara el valor " +
            "actual de mercado de tus CBFIs (precio actual × número de títulos) contra tu costo total de " +
            "adquisición (costo promedio × número de títulos): la diferencia es tu ganancia o pérdida de " +
            "capital. El **rendimiento porcentual** divide esa diferencia entre tu costo total. Las " +
            "tarjetas superiores resumen: **Valor total de mercado**, **Rendimiento total** y **Renta anual " +
            "estimada** — calculada a partir de la distribución anualizada por CBFI multiplicada por tus " +
            "títulos. Todos los valores se actualizan con el precio en tiempo real durante la jornada " +
            "bursátil de la BMV."),
        CreatePrivateItem("/portafolio", 3,
            "¿Qué significa el rendimiento real vs INPC?",
            "El **rendimiento real** descuenta el efecto de la inflación sobre tu ganancia. Si una FIBRA " +
            "subió 8 % en precio durante el año pero el INPC del BANXICO fue del 5 %, tu rendimiento real " +
            "fue del 3 %: la ganancia neta en poder adquisitivo. La plataforma carga automáticamente el " +
            "INPC de los últimos 12 meses del BANXICO para mostrar este ajuste junto al rendimiento nominal. " +
            "Un rendimiento real negativo significa que la inversión perdió poder de compra aunque en pesos " +
            "nominales hayas ganado. Es el indicador más honesto para evaluar si tus FIBRAs están " +
            "protegiendo tu capital frente a la inflación."),
        CreatePrivateItem("/portafolio", 4,
            "¿Puedo cargar mis posiciones desde Excel o CSV?",
            "Sí. El archivo debe tener exactamente tres columnas: **Ticker** (clave bursátil, por ejemplo " +
            "FIBRAMQ12 o DANHOS13), **Qty** (número de CBFIs en tu cartera) y **AvgCost** (precio promedio " +
            "ponderado de adquisición en pesos mexicanos). La primera fila debe ser el encabezado con esos " +
            "nombres exactos. El sistema valida el archivo antes de importar: si detecta columnas faltantes " +
            "o valores no numéricos, muestra el error específico fila por fila antes de hacer cambios. Las " +
            "posiciones importadas se fusionan con las existentes; si ya tenías FUNO11 y el archivo incluye " +
            "FUNO11, ese registro se actualiza con los valores del archivo."),
        CreatePrivateItem("/portafolio", 5,
            "¿Para qué sirve la función de archivar y restaurar?",
            "**Archivar** guarda una copia completa de tu portafolio actual — todas tus posiciones con " +
            "títulos y costos — en un respaldo con fecha y hora. Es útil antes de reestructurar tu " +
            "cartera: archivas primero y puedes volver al estado anterior si cambias de opinión. " +
            "**Restaurar** carga ese respaldo y reemplaza el portafolio activo; si tienes posiciones " +
            "activas en ese momento, se sustituyen por las del respaldo. Solo se guarda un respaldo a la " +
            "vez — archivar de nuevo sobreescribe el anterior. La fecha del archivo aparece en el botón " +
            "de restaurar para que sepas qué tan reciente es."),
        CreatePrivateItem("/portafolio", 6,
            "¿Qué muestra la vista de Calendario?",
            "La vista **Calendario** muestra las distribuciones **confirmadas** de tus FIBRAs en posición, " +
            "tomadas del registro oficial de la BMV. La ventana cubre los últimos dos meses y el mes " +
            "siguiente, agrupadas por fecha de pago. Cada evento detalla el monto **bruto total** en pesos " +
            "y, cuando la emisora ya clasificó la distribución, el desglose fiscal completo: **Componente " +
            "CUFIN** (la parte que genera retención de ISR), **ISR estimado** calculado sobre el CUFIN con " +
            "la tasa de retención vigente, **Retorno de Capital** (la parte no gravada) y **Neto estimado** " +
            "— lo que efectivamente recibirías descontando el ISR. Si la clasificación fiscal aún no está " +
            "disponible, la distribución aparece con el monto bruto y la etiqueta \"clasificación fiscal " +
            "pendiente\". A diferencia de un proyector de flujo, el Calendario solo incluye distribuciones " +
            "ya registradas ante la BMV; no genera estimaciones basadas en el historial de pagos."),

        // /oportunidades — Ranking privado con score configurable
        CreatePrivateItem("/oportunidades", 1,
            "¿Cómo funciona el score de oportunidad?",
            "El score es una puntuación de 0 a 100 que mide el atractivo relativo de cada FIBRA dentro " +
            "del universo. Se calcula como la suma ponderada de seis componentes: **Descuento NAV** (qué " +
            "tan barato cotiza el precio respecto al valor neto de activos), **Dividend Yield** " +
            "(rendimiento por distribuciones), **LTV invertido** (solidez financiera — menor deuda, mayor " +
            "puntuación), **Margen NOI** (eficiencia operativa), **Precio vs promedio 52 semanas** " +
            "(momento de precio) y **Yield Real** (rendimiento ajustado por inflación INPC). Cada " +
            "componente se normaliza entre 0 y 100 a partir del universo actual y se multiplica por el " +
            "peso porcentual que tú configures. El score no predice retornos futuros; es una herramienta " +
            "de priorización comparativa según tus propios criterios."),
        CreatePrivateItem("/oportunidades", 2,
            "¿Qué significa cada componente del score?",
            "— **Descuento NAV**: porcentaje por debajo del valor neto de activos al que cotiza la FIBRA; " +
            "un descuento alto puntúa mejor. — **Dividend Yield**: distribución anualizada sobre el precio " +
            "actual; mayor yield, mayor puntuación. — **LTV invertido**: inversa del nivel de deuda sobre " +
            "valor de propiedades; menor LTV puntúa mejor porque indica menor riesgo financiero. — " +
            "**Margen NOI**: porcentaje de ingresos que queda tras gastos operativos directos; mayor " +
            "margen refleja un portafolio más eficiente. — **Precio vs AVG 52S**: distancia del precio " +
            "actual respecto al promedio de las últimas 52 semanas; una FIBRA por debajo de su media anual " +
            "puntúa mejor en este componente. — **Yield Real**: diferencia entre el dividend yield y el " +
            "INPC anual; valores positivos indican que la distribución supera a la inflación."),
        CreatePrivateItem("/oportunidades", 3,
            "¿Para qué sirven los perfiles Predeterminado, Renta y Crecimiento?",
            "Los perfiles son configuraciones predefinidas de pesos que representan diferentes estrategias. " +
            "**Predeterminado** distribuye de forma balanceada: Descuento NAV 30 %, Yield 30 %, LTV 20 %, " +
            "útil como punto de partida general. **Renta** prioriza el ingreso por distribuciones: asigna " +
            "50 % al Dividend Yield y reduce los demás, pensado para quienes buscan flujo de efectivo " +
            "inmediato. **Crecimiento** enfatiza el Descuento NAV (40 %) y la solidez financiera con LTV " +
            "(25 %), orientado a quienes buscan apreciación de capital. Puedes elegir cualquier perfil como " +
            "punto de partida y ajustar los sliders manualmente para refinar la configuración a tu " +
            "estrategia personal."),
        CreatePrivateItem("/oportunidades", 4,
            "¿Qué es la vista Promediar y cuándo usarla?",
            "La vista **Promediar** tiene dos zonas. La primera es una **tabla de posiciones**: lista " +
            "todas las FIBRAs de tu portafolio ordenadas por score de oportunidad. En la columna " +
            "**Títulos adicionales** puedes ingresar cuántos CBFIs más deseas comprar por fila; la " +
            "tabla recalcula en tiempo real el **nuevo costo promedio ponderado** (incluyendo comisión " +
            "e IVA), el **nuevo valor total** de la posición, la **nueva plusvalía** respecto al precio " +
            "actual y la **renta mensual estimada** con los títulos adicionales. La segunda zona es la " +
            "**Calculadora con comisión e IVA**: muestra todas las FIBRAs con al menos una distribución " +
            "en los últimos 4 trimestres. Ingresa el presupuesto por FIBRA — pre-llenado en $1,000 — y " +
            "la calculadora muestra los **CBFIs que puedes comprar** y el **sobrante**, incorporando la " +
            "comisión e IVA configurados en Ops al precio efectivo de compra. Un indicador en la cabecera " +
            "confirma que los cálculos ya contemplan esos costos. Para el simulador bidireccional " +
            "**¿Qué pasaría si?** — donde puedes explorar cualquier FIBRA con una renta objetivo o " +
            "número de títulos específico y ver el retorno histórico a 2 años — visita la sección " +
            "**Herramientas**, disponible en el menú principal."),
        CreatePrivateItem("/oportunidades", 5,
            "¿Por qué algunas FIBRAs aparecen en la sección Datos Limitados?",
            "Una FIBRA aparece en **Datos Limitados** cuando tiene información disponible para uno o dos " +
            "componentes del score pero no para todos. Ocurre principalmente con emisoras que tienen " +
            "fundamentales trimestrales incompletos — por ejemplo, una FIBRA que aún no ha reportado " +
            "suficientes trimestres para calcular el NAV/CBFI, o cuyo LTV no está disponible en el período " +
            "más reciente. El score de estas FIBRAs no es comparable directamente con el de la tabla " +
            "principal porque no refleja los mismos criterios. Se muestran por separado para que puedas " +
            "consultarlas sin que distorsionen el ranking del universo principal."),
        CreatePrivateItem("/oportunidades", 6,
            "¿Puedo guardar mis propios pesos personalizados?",
            "Sí. Una vez que ajustes los sliders a tu configuración — asegurándote de que la suma de todos " +
            "los pesos sea exactamente 100 % —, haz clic en **Guardar configuración**. Los pesos se " +
            "almacenan en tu cuenta y se cargan automáticamente la próxima vez que ingreses a Oportunidades. " +
            "Si quieres volver a los pesos del sistema, selecciona el perfil **Predeterminado** y guarda " +
            "de nuevo. La configuración guardada no afecta el score público visible en el Comparador; " +
            "solo modifica el ranking en tu vista privada."),

        // /herramientas — Hub de calculadoras privadas
        CreatePrivateItem("/herramientas", 1,
            "¿Cómo funciona la calculadora FIBRAs vs CETES?",
            "Compara el crecimiento de capital e ingresos por distribuciones de hasta cuatro FIBRAs " +
            "seleccionadas contra los CETES a 28 días en un horizonte de 1, 3, 5 o 10 años. Ingresas el " +
            "**monto inicial en pesos** y la calculadora proyecta para cada instrumento: el **capital final " +
            "estimado** (asumiendo reinversión de distribuciones), la **renta acumulada neta de ISR** — " +
            "descontando retención del 30 % para FIBRAs y 20 % para CETES conforme a la Ley del ISR — y " +
            "el **rendimiento real anual** si el INPC está disponible. La tasa de CETES se carga " +
            "automáticamente desde los indicadores del mercado; si necesitas ajustarla, puedes editarla " +
            "manualmente. La **TIIE 28d vigente** también se muestra como dato de referencia. Las " +
            "distribuciones se obtienen de la última distribución trimestral reportada. La proyección " +
            "es lineal y no modela variaciones de precio ni cambios en las tasas de distribución futuras."),
        CreatePrivateItem("/herramientas", 2,
            "¿Qué me dice la calculadora de Meta de Renta?",
            "Calcula cuánto capital necesitas invertir en cada FIBRA para generar una renta mensual " +
            "objetivo específica. Ingresas el **monto de renta mensual en pesos** que deseas recibir y " +
            "la calculadora muestra, para cada FIBRA seleccionada: el **Yield TTM** (distribución de los " +
            "últimos 12 meses sobre el precio actual), el **capital necesario** en pesos para que ese yield " +
            "cubra tu meta mensual y el **número estimado de CBFIs** a ese precio. Es útil para planificar " +
            "cuánto destinar al sector de FIBRAs para alcanzar un flujo de renta pasiva específico. Los " +
            "cálculos usan el Yield TTM y el precio en tiempo real; si alguno cambia, el capital necesario " +
            "se actualiza automáticamente."),
        CreatePrivateItem("/herramientas", 3,
            "¿Cómo se calcula el Retorno Total y para qué sirve?",
            "El retorno total combina dos fuentes de rendimiento: la **plusvalía** (ganancia o pérdida por " +
            "cambio en el precio) y la **renta acumulada** (distribuciones recibidas). Seleccionas la " +
            "**fecha de compra** con el selector de fecha — que arranca pre-llenado en hoy menos 1 año y " +
            "acepta fechas entre 2 años atrás y 1 año atrás, período con suficiente historial para un " +
            "análisis representativo. La calculadora usa el precio de mercado en esa fecha, calcula cuántos " +
            "CBFIs habrías comprado con tu inversión, suma las distribuciones recibidas en ese lapso y " +
            "muestra: la **plusvalía** de capital, las **distribuciones acumuladas netas de ISR**, el " +
            "**retorno total** en pesos y porcentaje, y el rendimiento **anualizado**. También incluye el " +
            "rendimiento real ajustado por el INPC si el dato de inflación está disponible, para saber si " +
            "la inversión superó a la inflación en ese período."),
        CreatePrivateItem("/herramientas", 4,
            "¿Por qué la calculadora descuenta el ISR?",
            "Las distribuciones de FIBRAs están sujetas a retención de **ISR del 30 %** sobre la parte " +
            "correspondiente al resultado fiscal distribuido, conforme a la Ley del ISR vigente. Los CETES " +
            "tienen retención del **20 % sobre los intereses** generados. La calculadora aplica estas tasas " +
            "para que la comparación sea sobre ingresos netos reales — el dinero que efectivamente recibes. " +
            "Sin descontar el ISR, la comparación sería entre cifras brutas que no reflejan lo que queda " +
            "disponible. Si tu régimen fiscal implica tasas distintas — por ejemplo si presentas declaración " +
            "anual con tasa efectiva diferente —, los resultados netos en tu caso real pueden variar " +
            "respecto a lo que muestra la calculadora."),
        CreatePrivateItem("/herramientas", 5,
            "¿Qué es el panel ¿Qué pasaría si? y cómo funciona?",
            "El panel **¿Qué pasaría si?** aparece en Herramientas justo antes del selector de FIBRAs. " +
            "Permite simular el impacto de comprar CBFIs adicionales de cualquier FIBRA del universo de " +
            "oportunidades — la tengas o no en cartera. Seleccionas la emisora, ingresas los " +
            "**Títulos a comprar** o directamente la **Renta mensual objetivo** y el simulador funciona " +
            "de forma bidireccional: cambiar los títulos actualiza la renta estimada; cambiar la renta " +
            "calcula los títulos necesarios y el capital requerido incluyendo comisión e IVA configurados " +
            "en Ops. El panel también muestra el **Retorno histórico** de la emisora seleccionada: " +
            "variación de capital, distribuciones acumuladas y rendimiento total anualizado en los últimos " +
            "2 años, para poner en perspectiva la decisión de promediar o iniciar posición."),
        CreatePrivateItem("/herramientas", 6,
            "¿Cada cuánto se actualizan los precios y tasas en las calculadoras?",
            "Los **precios actuales** de las FIBRAs se actualizan en tiempo real durante la jornada " +
            "bursátil de la BMV y se cargan automáticamente al seleccionar cada emisora. Las " +
            "**distribuciones TTM** se actualizan cada vez que una FIBRA publica su reporte trimestral " +
            "ante la CNBV — generalmente una vez por trimestre. La **tasa CETES 28d** se obtiene de los " +
            "indicadores del mercado y se actualiza periódicamente. El **INPC** para el rendimiento real " +
            "se carga de los datos más recientes del BANXICO. Si algún dato no está disponible para una " +
            "FIBRA, el campo correspondiente aparece como '—' y ese componente del cálculo se omite."),

        // /reportes — Reportes trimestrales con análisis IA
        CreatePrivateItem("/reportes", 1,
            "¿Qué información contiene un reporte trimestral de fundamentales?",
            "Cada reporte agrupa la información clave que una FIBRA publica ante la CNBV en dos capas. " +
            "La primera es la **tabla de KPIs**: seis métricas cuantitativas — Cap Rate, NAV por CBFI, " +
            "LTV, Margen NOI, Margen FFO y Distribución trimestral — con el valor del período seleccionado " +
            "y notas aclaratorias cuando la FIBRA reportó condiciones atípicas. La segunda es el **análisis " +
            "IA**: un resumen ejecutivo en lenguaje claro, señales operacionales y financieras detectadas " +
            "en el texto del reporte, alertas de riesgo si las hay, y la perspectiva del analista sobre " +
            "lo más relevante para el inversionista. Puedes navegar entre trimestres del mismo ticker para " +
            "comparar la evolución de los KPIs en el tiempo."),
        CreatePrivateItem("/reportes", 2,
            "¿Qué significa cada KPI del reporte?",
            "— **Cap Rate**: rendimiento operativo neto sobre el valor de propiedades; mayor Cap Rate " +
            "indica más rendimiento bruto, generalmente en activos de mayor riesgo o ubicaciones " +
            "secundarias. — **NAV por CBFI**: valor neto de activos por certificado; compáralo con el " +
            "precio de mercado para saber si la FIBRA cotiza con descuento o prima sobre sus activos " +
            "reales. — **LTV**: deuda sobre valor de propiedades; el límite CNBV es 60 %, pero la mayoría " +
            "de FIBRAs saludables opera bajo 45 %. — **Margen NOI**: porcentaje de ingresos que queda " +
            "tras gastos operativos directos; FIBRAs industriales bien administradas superan el 75 %. — " +
            "**Margen FFO**: fondos de operación sobre ingresos, corrige la utilidad neta eliminando " +
            "distorsiones contables no monetarias. — **Distribución trimestral**: monto en pesos por CBFI " +
            "pagado en el trimestre; puede incluir parte fiscal y reembolso de capital."),
        CreatePrivateItem("/reportes", 3,
            "¿Qué es el análisis de IA y cómo interpretarlo?",
            "El análisis IA procesa el texto completo del reporte trimestral publicado por la FIBRA ante " +
            "la CNBV y extrae cuatro secciones estructuradas. El **Resumen ejecutivo** condensa los puntos " +
            "más relevantes del trimestre. Las **Señales operacionales** son eventos en el portafolio — " +
            "cambios en ocupación, nuevos contratos, adquisiciones, desinversiones. Las **Señales " +
            "financieras** detectan movimientos en métricas clave: aumento en LTV, compresión de NOI, " +
            "variaciones en distribución. Las **Alertas de riesgo** identifican factores negativos: " +
            "concentración de inquilinos, vencimientos de contratos relevantes, presión de refinanciamiento " +
            "o comentarios del auditor. El análisis IA es una herramienta de síntesis, no de asesoría; " +
            "contrasta siempre las señales con los estados financieros completos y, ante cualquier " +
            "decisión de inversión, consulta con un asesor financiero registrado ante la CNBV."),
        CreatePrivateItem("/reportes", 4,
            "¿Con qué frecuencia se actualizan los reportes?",
            "Los reportes se actualizan conforme cada FIBRA publica sus resultados trimestrales ante la " +
            "CNBV, generalmente entre 30 y 60 días después del cierre de cada trimestre fiscal. El " +
            "calendario habitual es: resultados del **1T** disponibles en mayo, **2T** en agosto, **3T** " +
            "en noviembre y **4T** (informe anual) entre febrero y abril del siguiente año. El selector de " +
            "períodos muestra todos los trimestres disponibles para la FIBRA elegida. Cuando la plataforma " +
            "procesa un nuevo reporte, el análisis IA se genera automáticamente y el período aparece " +
            "disponible en el selector. Si el procesamiento está en curso, el resumen puede aparecer " +
            "como pendiente por unas horas."),
        CreatePrivateItem("/reportes", 5,
            "¿Qué son las señales operacionales, financieras y las alertas de riesgo?",
            "Son tres categorías que el análisis IA organiza a partir del contenido literal del reporte " +
            "trimestral. Las **señales operacionales** describen hechos concretos sobre el portafolio: " +
            "nuevas adquisiciones o ventas de inmuebles, cambios en la tasa de ocupación, renovación o " +
            "vencimiento de contratos relevantes, obras de expansión. Las **señales financieras** reflejan " +
            "movimientos en la estructura económica: variaciones en la deuda y el LTV, cambios en el " +
            "costo de fondeo, ajustes en el perfil de vencimientos, impacto de tipos de cambio en FIBRAs " +
            "con deuda en dólares. Las **alertas de riesgo** concentran los factores identificados como " +
            "potencialmente negativos: concentración de ingresos en pocos inquilinos, vencimientos " +
            "próximos de contratos importantes, comentarios del auditor o presión sobre la capacidad de " +
            "mantener el nivel de distribuciones."),
    ];

    private static FaqItem CreateStaticItem(string entityKey, int order, string question, string answer) => new()
    {
        Id = Guid.NewGuid(),
        PageType = SeoPageType.StaticPage,
        EntityKey = entityKey,
        Question = question,
        Answer = answer.Trim(),
        Order = order,
        IsActive = true,
        UpdatedAt = SeedUpdatedAt,
        UpdatedBy = "system",
    };

    private static FaqItem CreatePrivateItem(string entityKey, int order, string question, string answer) => new()
    {
        Id = Guid.NewGuid(),
        PageType = SeoPageType.PrivatePage,
        EntityKey = entityKey,
        Question = question,
        Answer = answer.Trim(),
        Order = order,
        IsActive = true,
        UpdatedAt = SeedUpdatedAt,
        UpdatedBy = "system",
    };

    private static string GetEditorialEntityKey() => "/conoce-las-fibras";

    // Copia intencional de los KPI compartidos de frontend: seed inicial de FAQ no debe
    // depender del bundle TS. Si cambia el texto aquí, debe cambiarse también en UI.
    private static class SharedKpiDefinitions
    {
        public static readonly KpiDefinition CapRate = new(
            "Cap Rate",
            "Cap Rate = NOI anualizado / Valor de propiedades de inversión",
            "Tasa de capitalización: mide el rendimiento operativo del portafolio inmobiliario en relación a su valor. Un Cap Rate más alto implica mayor rendimiento y generalmente más riesgo; uno bajo refleja activos premium en alta demanda.");

        public static readonly KpiDefinition NavPerCbfi = new(
            "NAV por CBFI",
            "NAV = Valor de propiedades − Deuda total | NAV/CBFI = NAV / CBFIs en circulación",
            "Valor Neto de los Activos por certificado. Indica si el precio de mercado cotiza con descuento o premio respecto al valor real de los activos que respaldan cada CBFI.");

        public static readonly KpiDefinition Ltv = new(
            "LTV",
            "LTV = Deuda total / Valor de propiedades de inversión",
            "Loan-to-Value: nivel de apalancamiento en relación al valor inmobiliario. LTV bajo indica solidez financiera; LTV alto señala mayor exposición al riesgo.");

        public static readonly KpiDefinition NoiMargin = new(
            "NOI Margin",
            "NOI Margin = NOI / Ingresos Totales",
            "Margen de Ingreso Neto Operativo: porcentaje de ingresos que queda tras descontar gastos directos de operación. Mide la eficiencia operativa del portafolio.");

        public static readonly KpiDefinition FfoMargin = new(
            "FFO Margin",
            "FFO Margin = FFO / Ingresos Totales | FFO = Utilidad Neta + ajustes por valuación − ganancias cambiarias",
            "Fondos de Operación sobre ingresos. El FFO corrige la utilidad neta eliminando distorsiones contables para mostrar cuánto genera realmente el portafolio en operación.");

        public static readonly KpiDefinition QuarterlyDistribution = new(
            "Dist. Trimestral",
            "Distribución = Resultado Fiscal Distribuido + Reembolso de Capital",
            "Pago en efectivo por CBFI cada trimestre. Puede componerse de utilidades fiscales y reembolso de capital.");
    }

    private sealed record KpiDefinition(string Label, string Formula, string Description);
}
