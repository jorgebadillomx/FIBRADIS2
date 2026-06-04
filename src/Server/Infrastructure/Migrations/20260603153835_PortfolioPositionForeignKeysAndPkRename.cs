using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PortfolioPositionForeignKeysAndPkRename : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "portfolio",
                table: "PortfolioPositions",
                newName: "id");

            migrationBuilder.CreateIndex(
                name: "IX_PortfolioPositions_fibra_id",
                schema: "portfolio",
                table: "PortfolioPositions",
                column: "fibra_id");

            migrationBuilder.AddForeignKey(
                name: "FK_PortfolioPositions_Fibra_fibra_id",
                schema: "portfolio",
                table: "PortfolioPositions",
                column: "fibra_id",
                principalSchema: "catalog",
                principalTable: "Fibra",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PortfolioPositions_User_user_id",
                schema: "portfolio",
                table: "PortfolioPositions",
                column: "user_id",
                principalSchema: "auth",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PortfolioPositions_Fibra_fibra_id",
                schema: "portfolio",
                table: "PortfolioPositions");

            migrationBuilder.DropForeignKey(
                name: "FK_PortfolioPositions_User_user_id",
                schema: "portfolio",
                table: "PortfolioPositions");

            migrationBuilder.DropIndex(
                name: "IX_PortfolioPositions_fibra_id",
                schema: "portfolio",
                table: "PortfolioPositions");

            migrationBuilder.RenameColumn(
                name: "id",
                schema: "portfolio",
                table: "PortfolioPositions",
                newName: "Id");
        }
    }
}
