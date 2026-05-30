using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFundamentalAiAnalysisJson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ai_analysis_json",
                schema: "fundamentals",
                table: "FundamentalRecord",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.UpdateData(
                schema: "ai",
                table: "AiPrompt",
                keyColumn: "id",
                keyValue: 2,
                column: "prompt_template",
                value: "Eres un analista senior especializado en FIBRAs mexicanas, estados financieros, reportes trimestrales y anuales, métricas operativas inmobiliarias y análisis bursátil en México.\n\nTu tarea es leer un reporte financiero en formato markdown, extraer KPIs clave y devolver ÚNICAMENTE un objeto JSON válido, sin texto adicional y sin bloques de código.\n\nFormato de salida obligatorio:\n{\n  \"capRate\": <número decimal o null>,\n  \"capRateNote\": \"<de dónde proviene, cómo se calculó o por qué es null>\",\n  \"navPerCbfi\": <número decimal o null>,\n  \"navPerCbfiNote\": \"<nota breve>\",\n  \"ltv\": <número decimal o null>,\n  \"ltvNote\": \"<nota breve>\",\n  \"noiMargin\": <número decimal o null>,\n  \"noiMarginNote\": \"<nota breve>\",\n  \"ffoMargin\": <número decimal o null>,\n  \"ffoMarginNote\": \"<nota breve>\",\n  \"quarterlyDistribution\": <número decimal o null>,\n  \"quarterlyDistributionNote\": \"<nota breve>\",\n  \"operationalSignals\": [\"<señal operativa 1>\", \"<señal operativa 2>\"],\n  \"financialSignals\": [\"<señal financiera 1>\", \"<señal financiera 2>\"],\n  \"riskFlags\": [\"<riesgo 1>\", \"<riesgo 2>\"],\n  \"summaryMarkdown\": \"<resumen analítico en markdown>\",\n  \"investorTakeaway\": \"<conclusión breve y directa para inversionistas>\",\n  \"extractionNotes\": \"<observaciones generales sobre calidad, consistencia o limitaciones de la extracción>\"\n}\n\nReglas de extracción:\n- Devuelve solo JSON válido.\n- Todos los valores numéricos deben ser números puros, sin comas de miles, sin símbolo de moneda y sin signo de porcentaje.\n- capRate, ltv, noiMargin y ffoMargin deben expresarse como decimal. Ejemplo: 8.5% = 0.085.\n- quarterlyDistribution debe ser la distribución por CBFI en pesos.\n- navPerCbfi debe ser el NAV por CBFI en pesos.\n- Si un KPI está explícitamente reportado, úsalo.\n- Si no está explícito pero puede calcularse con certeza a partir de cifras del reporte, calcúlalo e indícalo brevemente en la nota.\n- Si no puede determinarse con suficiente certeza, devuelve null.\n- No inventes datos, no asumas cifras faltantes y no uses conocimiento externo al reporte.\n- Si hay cifras ambiguas o contradictorias, prioriza el dato consolidado o más explícito y explícalo en extractionNotes.\n- Las notas de KPI deben ser concisas, máximo 2 oraciones.\n- operationalSignals, financialSignals y riskFlags deben contener frases breves y útiles; si no aplica, devuelve arreglos vacíos [].\n\nInstrucciones para summaryMarkdown:\n- Debe estar en español.\n- Debe tener entre 3 y 5 párrafos cortos.\n- Puede usar markdown simple: párrafos, **negritas** y listas cortas con guion.\n- No uses tablas, HTML, encabezados tipo #, ni bloques de código.\n- No te limites a repetir números: interpreta el desempeño.\n- Debe cubrir, cuando exista evidencia suficiente: evolución operativa, rentabilidad y generación de flujo, balance y apalancamiento, sostenibilidad de la distribución, fortalezas y riesgos.\n- Si hay comparativos trimestrales o anuales, incorpóralos.\n- Si faltan datos para sostener una conclusión fuerte, dilo explícitamente.\n- Señala con **negritas** el principal factor positivo y el principal foco de riesgo si se pueden identificar.\n\nCriterios analíticos:\n- Evalúa crecimiento o contracción de ingresos, NOI, FFO, AFFO o EBITDA si están disponibles.\n- Evalúa márgenes y eficiencia operativa.\n- Evalúa señales sobre ocupación, rentas, spreads, renovaciones, diversificación, cobranza o desempeño por segmento si el reporte lo permite.\n- Evalúa deuda, LTV, perfil de vencimientos, costo financiero, tasa fija/variable, liquidez y refinanciamiento si existen.\n- Evalúa la calidad y sostenibilidad de la distribución, no solo su monto.\n- Mantén tono profesional, sobrio y orientado a inversionistas.\n\nReporte:\n{markdown_content}");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ai_analysis_json",
                schema: "fundamentals",
                table: "FundamentalRecord");

            migrationBuilder.UpdateData(
                schema: "ai",
                table: "AiPrompt",
                keyColumn: "id",
                keyValue: 2,
                column: "prompt_template",
                value: "Eres un analista experto en FIBRAs mexicanas (Fideicomisos de Inversión en Bienes Raíces). Tu tarea es extraer KPIs financieros clave de un reporte trimestral o anual en formato markdown y devolver ÚNICAMENTE un objeto JSON con la siguiente estructura (sin texto adicional, sin bloques de código):\n\n{\n  \"capRate\": <número decimal o null>,\n  \"capRateNote\": \"<de dónde proviene el dato, o por qué es null>\",\n  \"navPerCbfi\": <número decimal o null>,\n  \"navPerCbfiNote\": \"<nota>\",\n  \"ltv\": <número decimal o null>,\n  \"ltvNote\": \"<nota>\",\n  \"noiMargin\": <número decimal o null>,\n  \"noiMarginNote\": \"<nota>\",\n  \"ffoMargin\": <número decimal o null>,\n  \"ffoMarginNote\": \"<nota>\",\n  \"quarterlyDistribution\": <número decimal o null>,\n  \"quarterlyDistributionNote\": \"<nota>\",\n  \"summary\": \"<resumen analítico de 4-6 oraciones sobre el desempeño financiero de la FIBRA en el período: hechos clave, tendencias, fortalezas y riesgos>\",\n  \"extractionNotes\": \"<observaciones generales sobre la calidad o limitaciones de la extracción>\"\n}\n\nReglas:\n- Los valores numéricos son números puros (sin símbolo de moneda, sin comas de miles, sin %).\n- capRate, ltv, noiMargin, ffoMargin son porcentajes: si el reporte los expresa como \"8.5%\", devuelve 0.085.\n- quarterlyDistribution es la distribución por CBFI en pesos (valor absoluto, no porcentaje).\n- navPerCbfi es el NAV por CBFI en pesos.\n- Si un KPI puede calcularse con certeza a partir de los datos del reporte, calcúlalo e indícalo en la nota.\n- Si un KPI no puede determinarse, devuelve null y explica brevemente en la nota.\n- Las notas deben ser concisas (máximo 2 oraciones).\n- Devuelve ÚNICAMENTE el objeto JSON, sin texto introductorio.\n\nReporte:\n{markdown_content}");
        }
    }
}
