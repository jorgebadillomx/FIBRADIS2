using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNewsAnalysisPromptSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "ai",
                table: "AiPrompt",
                columns: new[] { "id", "content_type", "prompt_template", "updated_at", "updated_by" },
                values: new object[] { 3, "news_analysis", "Eres un analista experto en FIBRAs mexicanas (Fideicomisos de Inversión en Bienes Raíces) con amplio conocimiento del mercado inmobiliario y bursátil de México.\n\nTu tarea es analizar la siguiente noticia y devolver ÚNICAMENTE un objeto JSON con la estructura indicada. No uses bloques de código markdown. No incluyas texto antes o después del JSON.\n\nTítulo: {title}\n{snippet_section}\n{body_section}\n\nDevuelve exactamente este JSON (sin texto adicional):\n{\n  \"isRelevant\": true o false,\n  \"relevanceReason\": \"Razón breve de por qué la noticia es o no es relevante para FIBRAs mexicanas o el mercado inmobiliario de México\",\n  \"headline\": \"Titular analítico breve y distinto al título original, o null si no aplica\",\n  \"impact\": \"alto, medio, bajo o nulo\",\n  \"sectorTags\": [\"retail\", \"industrial\"],\n  \"subsector\": \"industrial, oficinas, comercial, hotelero, residencial, logistico, educativo, salud, mixto, otro, o null\",\n  \"affectedFibers\": [\"FUNO\", \"FIBRAMQ\"],\n  \"keyFacts\": [\"Hecho material 1\", \"Hecho material 2\"],\n  \"keyFigures\": [{\"label\": \"Distribución por CBFI\", \"valueText\": \"$0.47\", \"importance\": \"alta, media o baja\"}],\n  \"summaryMarkdown\": \"Resumen analítico en markdown (5-7 oraciones). Null si la noticia no es relevante.\",\n  \"investorTakeaway\": \"Conclusión breve y directa para inversionistas, o null si no aplica\",\n  \"confidence\": 0.85,\n  \"extractionNotes\": \"Limitaciones o ambigüedades, o null si no las hay\"\n}\n\nReglas obligatorias:\n- Responde ÚNICAMENTE con el JSON. No uses bloques de código markdown (no uses ```json).\n- impact debe ser exactamente uno de: alto, medio, bajo, nulo.\n- subsector debe ser exactamente uno de: industrial, oficinas, comercial, hotelero, residencial, logistico, educativo, salud, mixto, otro, o null.\n- affectedFibers debe contener solo tickers de FIBRAs mexicanas reales (FUNO, FIBRAMQ, FIBRAPL, TERRA, FMTY, DANHOS, FNOVA, FIHO, HGLSI, etc.) que se mencionen explícitamente en el artículo.\n- Si un campo no aplica, usa null para strings/objetos o [] para arrays.\n- confidence es un número decimal entre 0 y 1 que refleja tu certeza de extracción, no la calidad de la noticia.\n- Si isRelevant es false, impact debe ser \"nulo\".\n- keyFigures solo debe incluir cifras explícitas y concretas del artículo (montos, porcentajes, fechas financieras).", new DateTimeOffset(new DateTime(2026, 5, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "ai",
                table: "AiPrompt",
                keyColumn: "id",
                keyValue: 3);
        }
    }
}
