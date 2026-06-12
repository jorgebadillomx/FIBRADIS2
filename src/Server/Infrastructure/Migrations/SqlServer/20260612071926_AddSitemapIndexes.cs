using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class AddSitemapIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_NewsArticle_Sitemap",
                schema: "news",
                table: "NewsArticle",
                column: "published_at",
                descending: new bool[0],
                filter: "[status] = 'Processed' AND [deleted_at] IS NULL AND [slug] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NewsArticle_Sitemap",
                schema: "news",
                table: "NewsArticle");
        }
    }
}
