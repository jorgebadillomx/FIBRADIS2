using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDistributionUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Distribution_FibraId_PaymentDate",
                schema: "market",
                table: "Distribution");

            migrationBuilder.CreateIndex(
                name: "UIX_Distribution_FibraId_PaymentDate",
                schema: "market",
                table: "Distribution",
                columns: new[] { "fibra_id", "payment_date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UIX_Distribution_FibraId_PaymentDate",
                schema: "market",
                table: "Distribution");

            migrationBuilder.CreateIndex(
                name: "IX_Distribution_FibraId_PaymentDate",
                schema: "market",
                table: "Distribution",
                columns: new[] { "fibra_id", "payment_date" });
        }
    }
}
