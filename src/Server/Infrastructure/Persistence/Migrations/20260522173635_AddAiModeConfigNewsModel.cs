using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAiModeConfigNewsModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "news_model",
                schema: "ai",
                table: "AiModeConfig",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                schema: "ai",
                table: "AiModeConfig",
                keyColumn: "id",
                keyValue: 1,
                column: "news_model",
                value: "gemini-2.5-pro");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "news_model",
                schema: "ai",
                table: "AiModeConfig");
        }
    }
}
