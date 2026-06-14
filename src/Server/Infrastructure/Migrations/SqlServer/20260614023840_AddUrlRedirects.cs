using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class AddUrlRedirects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UrlRedirect",
                schema: "seo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    from_path = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    to_path = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    status_code = table.Column<int>(type: "int", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updated_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UrlRedirect", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "UX_UrlRedirect_FromPath",
                schema: "seo",
                table: "UrlRedirect",
                column: "from_path",
                unique: true);

            migrationBuilder.InsertData(
                schema: "seo",
                table: "UrlRedirect",
                columns: new[]
                {
                    "id",
                    "from_path",
                    "to_path",
                    "status_code",
                    "is_active",
                    "notes",
                    "created_at",
                    "created_by",
                    "updated_at",
                    "updated_by",
                },
                values: new object[,]
                {
                    {
                        Guid.Parse("2d1bbf9b-5a95-4a7f-9d87-39d2efc2b1a1"),
                        "/blog",
                        "/noticias",
                        301,
                        true,
                        "Migrado desde redirect hardcodeado",
                        new DateTimeOffset(new DateTime(2026, 6, 14, 0, 0, 0, DateTimeKind.Unspecified), TimeSpan.Zero),
                        "system",
                        new DateTimeOffset(new DateTime(2026, 6, 14, 0, 0, 0, DateTimeKind.Unspecified), TimeSpan.Zero),
                        "system",
                    },
                    {
                        Guid.Parse("1bcb1f7d-ef1b-4a1f-8d51-15df4f4f0cc2"),
                        "/catalogo",
                        "/fibras",
                        301,
                        true,
                        "Migrado desde redirect hardcodeado",
                        new DateTimeOffset(new DateTime(2026, 6, 14, 0, 0, 0, DateTimeKind.Unspecified), TimeSpan.Zero),
                        "system",
                        new DateTimeOffset(new DateTime(2026, 6, 14, 0, 0, 0, DateTimeKind.Unspecified), TimeSpan.Zero),
                        "system",
                    },
                    {
                        Guid.Parse("f3f9f7b2-5e89-4c34-bf17-9f7d2d27f1a3"),
                        "/aviso-de-privacidad",
                        "/privacidad",
                        301,
                        true,
                        "Migrado desde redirect hardcodeado",
                        new DateTimeOffset(new DateTime(2026, 6, 14, 0, 0, 0, DateTimeKind.Unspecified), TimeSpan.Zero),
                        "system",
                        new DateTimeOffset(new DateTime(2026, 6, 14, 0, 0, 0, DateTimeKind.Unspecified), TimeSpan.Zero),
                        "system",
                    },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "seo",
                table: "UrlRedirect",
                keyColumn: "id",
                keyValue: Guid.Parse("2d1bbf9b-5a95-4a7f-9d87-39d2efc2b1a1"));

            migrationBuilder.DeleteData(
                schema: "seo",
                table: "UrlRedirect",
                keyColumn: "id",
                keyValue: Guid.Parse("1bcb1f7d-ef1b-4a1f-8d51-15df4f4f0cc2"));

            migrationBuilder.DeleteData(
                schema: "seo",
                table: "UrlRedirect",
                keyColumn: "id",
                keyValue: Guid.Parse("f3f9f7b2-5e89-4c34-bf17-9f7d2d27f1a3"));

            migrationBuilder.DropTable(
                name: "UrlRedirect",
                schema: "seo");
        }
    }
}
