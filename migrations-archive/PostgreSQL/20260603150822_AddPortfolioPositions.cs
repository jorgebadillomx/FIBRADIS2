using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPortfolioPositions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "portfolio");

            migrationBuilder.CreateTable(
                name: "PortfolioPositions",
                schema: "portfolio",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fibra_id = table.Column<Guid>(type: "uuid", nullable: false),
                    titulos = table.Column<int>(type: "integer", nullable: false),
                    costo_promedio = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    costo_total_compra = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    uploaded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PortfolioPositions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "UX_PortfolioPositions_UserId_FibraId",
                schema: "portfolio",
                table: "PortfolioPositions",
                columns: new[] { "user_id", "fibra_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PortfolioPositions",
                schema: "portfolio");
        }
    }
}
