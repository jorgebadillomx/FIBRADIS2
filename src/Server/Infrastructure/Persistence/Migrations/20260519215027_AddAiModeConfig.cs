using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAiModeConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "ai");

            migrationBuilder.AddColumn<string>(
                name: "ai_summary",
                schema: "news",
                table: "NewsArticle",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AiModeConfig",
                schema: "ai",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false),
                    mode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updated_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    previous_mode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiModeConfig", x => x.id);
                });

            migrationBuilder.InsertData(
                schema: "ai",
                table: "AiModeConfig",
                columns: new[] { "id", "mode", "previous_mode", "updated_at", "updated_by" },
                values: new object[] { 1, "Off", null, new DateTimeOffset(new DateTime(2026, 5, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiModeConfig",
                schema: "ai");

            migrationBuilder.DropColumn(
                name: "ai_summary",
                schema: "news",
                table: "NewsArticle");
        }
    }
}
