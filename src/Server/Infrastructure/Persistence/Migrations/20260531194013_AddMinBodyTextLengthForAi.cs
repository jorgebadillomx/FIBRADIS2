using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMinBodyTextLengthForAi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "min_body_text_length_for_ai",
                schema: "ai",
                table: "AiModeConfig",
                type: "int",
                nullable: false,
                defaultValue: 500);

            migrationBuilder.UpdateData(
                schema: "ai",
                table: "AiModeConfig",
                keyColumn: "id",
                keyValue: 1,
                column: "min_body_text_length_for_ai",
                value: 500);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "min_body_text_length_for_ai",
                schema: "ai",
                table: "AiModeConfig");
        }
    }
}
