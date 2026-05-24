using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAiPromptAndPipelineErrorLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "jobs");

            migrationBuilder.CreateTable(
                name: "AiPrompt",
                schema: "ai",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    content_type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    prompt_template = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updated_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiPrompt", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "PipelineErrorLog",
                schema: "jobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "newsequentialid()"),
                    pipeline = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    error_type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    context = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ai_context = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "getutcdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PipelineErrorLog", x => x.id);
                });

            migrationBuilder.InsertData(
                schema: "ai",
                table: "AiPrompt",
                columns: new[] { "id", "content_type", "prompt_template", "updated_at", "updated_by" },
                values: new object[,]
                {
                    { 1, "news", "Eres un analista experto en FIBRAs mexicanas (Fideicomisos de Inversión en Bienes Raíces) con amplio conocimiento del mercado inmobiliario y bursátil de México.\n{strictness_instruction}\nTítulo: {title}\n{snippet_section}\n{body_section}\nIncluye: el hecho central, su relevancia para el sector de FIBRAs o bienes raíces en México, los datos más materiales del artículo cuando existan, y una lectura analítica breve para el inversionista.\nSi el artículo contiene cifras, fechas, montos, porcentajes, dividendos, emisiones, ocupación, adquisiciones o guidance, intégralos en el resumen.\nNo escribas menos de 5 oraciones si el cuerpo del artículo está disponible. Responde solo con el resumen, sin preámbulos.", new DateTimeOffset(new DateTime(2026, 5, 23, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system" },
                    { 2, "document", "Eres un analista experto en FIBRAs mexicanas y documentos financieros corporativos del sector inmobiliario en México.\n{strictness_instruction}\nTítulo: {title}\n{snippet_section}\n{body_section}\nResume el hecho central del documento, su relevancia para fundamentales, los datos cuantitativos más materiales y una lectura analítica breve para un inversionista.\nSi el documento contiene cifras, guidance, rentas, NOI, AFFO, FFO, ocupación, adquisiciones, deuda o cap rates, intégralos en el resumen.\nNo escribas menos de 5 oraciones si el cuerpo del documento está disponible. Responde solo con el resumen, sin preámbulos.", new DateTimeOffset(new DateTime(2026, 5, 23, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system" }
                });

            migrationBuilder.CreateIndex(
                name: "UQ_AiPrompt_ContentType",
                schema: "ai",
                table: "AiPrompt",
                column: "content_type",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PipelineErrorLog_Pipeline_CreatedAt",
                schema: "jobs",
                table: "PipelineErrorLog",
                columns: new[] { "pipeline", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiPrompt",
                schema: "ai");

            migrationBuilder.DropTable(
                name: "PipelineErrorLog",
                schema: "jobs");
        }
    }
}
