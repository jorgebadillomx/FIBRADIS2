using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class AddSeoModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "seo");

            migrationBuilder.CreateTable(
                name: "SeoMetadata",
                schema: "seo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    page_type = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    entity_key = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    title = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    meta_description = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    canonical_path = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    og_title = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    og_description = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    og_type = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    og_image_url = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    og_locale = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    twitter_card = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    robots_directives = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    json_ld = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updated_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    title_is_overridden = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    meta_description_is_overridden = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    canonical_path_is_overridden = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    og_title_is_overridden = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    og_description_is_overridden = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    og_type_is_overridden = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    og_image_url_is_overridden = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    og_locale_is_overridden = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    twitter_card_is_overridden = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    robots_directives_is_overridden = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    json_ld_is_overridden = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeoMetadata", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "UX_SeoMetadata_PageType_EntityKey",
                schema: "seo",
                table: "SeoMetadata",
                columns: new[] { "page_type", "entity_key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SeoMetadata",
                schema: "seo");
        }
    }
}
