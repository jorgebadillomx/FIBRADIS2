using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAiCallLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiCallLog",
                schema: "jobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "newsequentialid()"),
                    timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    operation = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    provider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    model_id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    prompt_length = table.Column<int>(type: "int", nullable: false),
                    duration_ms = table.Column<long>(type: "bigint", nullable: false),
                    success = table.Column<bool>(type: "bit", nullable: false),
                    response_raw = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    error_message = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    context = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "getutcdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiCallLog", x => x.id);
                });

            migrationBuilder.UpdateData(
                schema: "ai",
                table: "AiPrompt",
                keyColumn: "id",
                keyValue: 2,
                column: "prompt_template",
                value: "Eres un analista experto en FIBRAs mexicanas (Fideicomisos de Inversión en Bienes Raíces). Tu tarea es extraer KPIs financieros clave de un reporte trimestral o anual en formato markdown y devolver ÚNICAMENTE un objeto JSON con la siguiente estructura (sin texto adicional, sin bloques de código):\n\n{\n  \"capRate\": <número decimal o null>,\n  \"capRateNote\": \"<de dónde proviene el dato, o por qué es null>\",\n  \"navPerCbfi\": <número decimal o null>,\n  \"navPerCbfiNote\": \"<nota>\",\n  \"ltv\": <número decimal o null>,\n  \"ltvNote\": \"<nota>\",\n  \"noiMargin\": <número decimal o null>,\n  \"noiMarginNote\": \"<nota>\",\n  \"ffoMargin\": <número decimal o null>,\n  \"ffoMarginNote\": \"<nota>\",\n  \"quarterlyDistribution\": <número decimal o null>,\n  \"quarterlyDistributionNote\": \"<nota>\",\n  \"summary\": \"<resumen analítico de 4-6 oraciones sobre el desempeño financiero de la FIBRA en el período: hechos clave, tendencias, fortalezas y riesgos>\",\n  \"extractionNotes\": \"<observaciones generales sobre la calidad o limitaciones de la extracción>\"\n}\n\nReglas:\n- Los valores numéricos son números puros (sin símbolo de moneda, sin comas de miles, sin %).\n- capRate, ltv, noiMargin, ffoMargin son porcentajes: si el reporte los expresa como \"8.5%\", devuelve 0.085.\n- quarterlyDistribution es la distribución por CBFI en pesos (valor absoluto, no porcentaje).\n- navPerCbfi es el NAV por CBFI en pesos.\n- Si un KPI puede calcularse con certeza a partir de los datos del reporte, calcúlalo e indícalo en la nota.\n- Si un KPI no puede determinarse, devuelve null y explica brevemente en la nota.\n- Las notas deben ser concisas (máximo 2 oraciones).\n- Devuelve ÚNICAMENTE el objeto JSON, sin texto introductorio.\n\nReporte:\n{markdown_content}");

            migrationBuilder.CreateIndex(
                name: "IX_AiCallLog_Operation_CreatedAt",
                schema: "jobs",
                table: "AiCallLog",
                columns: new[] { "operation", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiCallLog",
                schema: "jobs");

            migrationBuilder.UpdateData(
                schema: "ai",
                table: "AiPrompt",
                keyColumn: "id",
                keyValue: 2,
                column: "prompt_template",
                value: "Eres un analista experto en FIBRAs mexicanas (Fideicomisos de Inversión en Bienes Raíces). Tu tarea es extraer KPIs financieros clave de un reporte trimestral o anual en formato markdown y devolver únicamente un objeto JSON con la siguiente estructura exacta (sin texto adicional):\n\n{\n  \"cap_rate\": <número decimal o null>,\n  \"nav_per_cbfi\": <número decimal o null>,\n  \"ltv\": <número decimal o null>,\n  \"noi_margin\": <número decimal o null>,\n  \"ffo_margin\": <número decimal o null>,\n  \"quarterly_distribution\": <número decimal o null>,\n  \"occupancy_rate\": <número decimal o null>,\n  \"total_assets\": <número decimal o null>,\n  \"debt_to_equity\": <número decimal o null>,\n  \"revenue\": <número decimal o null>,\n  \"net_income\": <número decimal o null>,\n  \"total_debt\": <número decimal o null>,\n  \"field_notes\": {\n    \"cap_rate\": \"<nota breve sobre el dato encontrado o null>\",\n    \"nav_per_cbfi\": \"<nota o null>\",\n    \"ltv\": \"<nota o null>\",\n    \"noi_margin\": \"<nota o null>\",\n    \"ffo_margin\": \"<nota o null>\",\n    \"quarterly_distribution\": \"<nota o null>\",\n    \"occupancy_rate\": \"<nota o null>\",\n    \"total_assets\": \"<nota o null>\",\n    \"debt_to_equity\": \"<nota o null>\",\n    \"revenue\": \"<nota o null>\",\n    \"net_income\": \"<nota o null>\",\n    \"total_debt\": \"<nota o null>\"\n  },\n  \"analytical_summary\": \"<resumen analítico de 3-5 oraciones sobre el desempeño financiero de la FIBRA en el período, incluyendo tendencias clave, fortalezas y riesgos>\"\n}\n\nReglas importantes:\n- Los valores numéricos deben ser números puros (sin símbolo de moneda, sin comas como separadores de miles, sin %).\n- Cap rate, LTV, NOI margin, FFO margin, occupancy rate y distribución trimestral son porcentajes: devuélvelos como decimal entre 0 y 1 si están expresados como porcentaje (p.ej. 8.5% → 0.085), o como número absoluto si ya está en ese formato.\n- Si un KPI no aparece explícitamente en el documento pero puede calcularse de forma confiable a partir de los datos disponibles, calcúlalo e indica cómo en la nota del campo correspondiente.\n- Si un KPI no puede determinarse con certeza, devuelve null para el valor numérico.\n- Las notas de campo deben ser breves (máximo 2 oraciones) y explicar de dónde se obtuvo el dato o por qué es null.\n- Devuelve ÚNICAMENTE el objeto JSON, sin markdown, sin bloques de código, sin texto introductorio.\n\nReporte:\n{markdown_content}");
        }
    }
}
