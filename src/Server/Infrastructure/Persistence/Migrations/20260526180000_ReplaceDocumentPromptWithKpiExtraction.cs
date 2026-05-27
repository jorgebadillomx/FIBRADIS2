using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260526180000_ReplaceDocumentPromptWithKpiExtraction")]
    public partial class ReplaceDocumentPromptWithKpiExtraction : Migration
    {
        private const string KpiTemplate =
            "Eres un analista experto en FIBRAs mexicanas (Fideicomisos de Inversión en Bienes Raíces). Tu tarea es extraer KPIs financieros clave de un reporte trimestral o anual en formato markdown y devolver únicamente un objeto JSON con la siguiente estructura exacta (sin texto adicional):\n\n" +
            "{\n" +
            "  \"cap_rate\": <número decimal o null>,\n" +
            "  \"nav_per_cbfi\": <número decimal o null>,\n" +
            "  \"ltv\": <número decimal o null>,\n" +
            "  \"noi_margin\": <número decimal o null>,\n" +
            "  \"ffo_margin\": <número decimal o null>,\n" +
            "  \"quarterly_distribution\": <número decimal o null>,\n" +
            "  \"occupancy_rate\": <número decimal o null>,\n" +
            "  \"total_assets\": <número decimal o null>,\n" +
            "  \"debt_to_equity\": <número decimal o null>,\n" +
            "  \"revenue\": <número decimal o null>,\n" +
            "  \"net_income\": <número decimal o null>,\n" +
            "  \"total_debt\": <número decimal o null>,\n" +
            "  \"field_notes\": {\n" +
            "    \"cap_rate\": \"<nota breve sobre el dato encontrado o null>\",\n" +
            "    \"nav_per_cbfi\": \"<nota o null>\",\n" +
            "    \"ltv\": \"<nota o null>\",\n" +
            "    \"noi_margin\": \"<nota o null>\",\n" +
            "    \"ffo_margin\": \"<nota o null>\",\n" +
            "    \"quarterly_distribution\": \"<nota o null>\",\n" +
            "    \"occupancy_rate\": \"<nota o null>\",\n" +
            "    \"total_assets\": \"<nota o null>\",\n" +
            "    \"debt_to_equity\": \"<nota o null>\",\n" +
            "    \"revenue\": \"<nota o null>\",\n" +
            "    \"net_income\": \"<nota o null>\",\n" +
            "    \"total_debt\": \"<nota o null>\"\n" +
            "  },\n" +
            "  \"analytical_summary\": \"<resumen analítico de 3-5 oraciones sobre el desempeño financiero de la FIBRA en el período, incluyendo tendencias clave, fortalezas y riesgos>\"\n" +
            "}\n\n" +
            "Reglas importantes:\n" +
            "- Los valores numéricos deben ser números puros (sin símbolo de moneda, sin comas como separadores de miles, sin %).\n" +
            "- Cap rate, LTV, NOI margin, FFO margin, occupancy rate y distribución trimestral son porcentajes: devuélvelos como decimal entre 0 y 1 si están expresados como porcentaje (p.ej. 8.5% → 0.085), o como número absoluto si ya está en ese formato.\n" +
            "- Si un KPI no aparece explícitamente en el documento pero puede calcularse de forma confiable a partir de los datos disponibles, calcúlalo e indica cómo en la nota del campo correspondiente.\n" +
            "- Si un KPI no puede determinarse con certeza, devuelve null para el valor numérico.\n" +
            "- Las notas de campo deben ser breves (máximo 2 oraciones) y explicar de dónde se obtuvo el dato o por qué es null.\n" +
            "- Devuelve ÚNICAMENTE el objeto JSON, sin markdown, sin bloques de código, sin texto introductorio.\n\n" +
            "Reporte:\n" +
            "{markdown_content}";

        private const string DocumentTemplate =
            "Eres un analista experto en FIBRAs mexicanas y documentos financieros corporativos del sector inmobiliario en México.\n" +
            "{strictness_instruction}\n" +
            "Título: {title}\n" +
            "{snippet_section}\n" +
            "{body_section}\n" +
            "Resume el hecho central del documento, su relevancia para fundamentales, los datos cuantitativos más materiales y una lectura analítica breve para un inversionista.\n" +
            "Si el documento contiene cifras, guidance, rentas, NOI, AFFO, FFO, ocupación, adquisiciones, deuda o cap rates, intégralos en el resumen.\n" +
            "No escribas menos de 5 oraciones si el cuerpo del documento está disponible. Responde solo con el resumen, sin preámbulos.";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remove the manually-inserted kpi_extraction row from a prior session, then repurpose id=2.
            migrationBuilder.Sql("DELETE FROM [ai].[AiPrompt] WHERE [content_type] = 'kpi_extraction'");

            var kpiEscaped = KpiTemplate.Replace("'", "''");
            migrationBuilder.Sql(
                $"UPDATE [ai].[AiPrompt] SET [content_type] = 'kpi_extraction', [prompt_template] = N'{kpiEscaped}', [updated_at] = '2026-05-26T00:00:00.0000000+00:00' WHERE [id] = 2");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var docEscaped = DocumentTemplate.Replace("'", "''");
            migrationBuilder.Sql(
                $"UPDATE [ai].[AiPrompt] SET [content_type] = 'document', [prompt_template] = N'{docEscaped}', [updated_at] = '2026-05-23T00:00:00.0000000+00:00' WHERE [id] = 2");
        }
    }
}
