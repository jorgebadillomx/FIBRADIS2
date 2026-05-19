using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNewsArticleFibra : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NewsArticleFibra",
                schema: "news",
                columns: table => new
                {
                    news_article_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    fibra_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewsArticleFibra", x => new { x.news_article_id, x.fibra_id });
                    table.ForeignKey(
                        name: "FK_NewsArticleFibra_NewsArticle_news_article_id",
                        column: x => x.news_article_id,
                        principalSchema: "news",
                        principalTable: "NewsArticle",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NewsArticleFibra_FibraId",
                schema: "news",
                table: "NewsArticleFibra",
                column: "fibra_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NewsArticleFibra",
                schema: "news");
        }
    }
}
