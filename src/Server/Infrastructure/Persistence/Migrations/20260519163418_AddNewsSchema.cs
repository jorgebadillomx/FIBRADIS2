using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNewsSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "news");

            migrationBuilder.CreateTable(
                name: "BlocklistTerm",
                schema: "news",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    term = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlocklistTerm", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "NewsArticle",
                schema: "news",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    title = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    title_normalized = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    source = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    published_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    url = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    snippet = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    captured_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    error_reason = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewsArticle", x => x.id);
                });

            migrationBuilder.InsertData(
                schema: "news",
                table: "BlocklistTerm",
                columns: new[] { "id", "created_at", "term" },
                values: new object[,]
                {
                    { new Guid("01198b97-3348-19ce-d5a6-92bbde33ebeb"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "fibra de carbono" },
                    { new Guid("18288ba5-5469-5321-b116-12f462def773"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "fibra textil" },
                    { new Guid("33addac2-f65d-b0e1-616f-0b72e7aad6c3"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "fibra alimentaria" },
                    { new Guid("3c3dbbea-5d57-c2e7-7395-0cef7bf9fba9"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "fibra dietetica" },
                    { new Guid("93629cda-318e-f05f-94a4-49e87f7a07b3"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "fibra dietética" },
                    { new Guid("c0166b06-2573-7815-6d80-6e448212bc1a"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "fibra muscular" },
                    { new Guid("c2c16834-61c4-a329-acb9-3f601fe94781"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "fibra de vidrio" },
                    { new Guid("e05324fb-2c04-9389-41f6-a1dce885eba2"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "fibra optica" },
                    { new Guid("e1e5c21c-7dcc-13b9-c5b2-065e293260cd"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "internet fibra" },
                    { new Guid("e42a6f48-fd39-9fcb-9cdd-b32936d4151a"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "fibra óptica" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_BlocklistTerm_Term",
                schema: "news",
                table: "BlocklistTerm",
                column: "term",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NewsArticle_TitleNormalized_CapturedAt",
                schema: "news",
                table: "NewsArticle",
                columns: new[] { "title_normalized", "captured_at" });

            migrationBuilder.CreateIndex(
                name: "IX_NewsArticle_Url",
                schema: "news",
                table: "NewsArticle",
                column: "url",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BlocklistTerm",
                schema: "news");

            migrationBuilder.DropTable(
                name: "NewsArticle",
                schema: "news");
        }
    }
}
