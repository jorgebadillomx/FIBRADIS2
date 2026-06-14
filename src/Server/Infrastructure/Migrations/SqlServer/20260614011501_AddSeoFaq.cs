using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class AddSeoFaq : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FaqItem",
                schema: "seo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    page_type = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    entity_key = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    question = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    answer = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    display_order = table.Column<int>(type: "int", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updated_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FaqItem", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FaqItem_PageType_EntityKey_Order",
                schema: "seo",
                table: "FaqItem",
                columns: new[] { "page_type", "entity_key", "display_order" });

            migrationBuilder.CreateIndex(
                name: "UX_FaqItem_PageType_EntityKey_Question",
                schema: "seo",
                table: "FaqItem",
                columns: new[] { "page_type", "entity_key", "question" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FaqItem",
                schema: "seo");
        }
    }
}
