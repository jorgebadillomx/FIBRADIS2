using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class AddFiscalRatesConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "isr_retention_rate",
                schema: "ops",
                table: "OperationalConfig",
                type: "decimal(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "iva_rate",
                schema: "ops",
                table: "OperationalConfig",
                type: "decimal(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.UpdateData(
                schema: "ops",
                table: "OperationalConfig",
                keyColumn: "id",
                keyValue: 1,
                columns: new[] { "isr_retention_rate", "iva_rate" },
                values: new object[] { 0.30m, 0.16m });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "isr_retention_rate",
                schema: "ops",
                table: "OperationalConfig");

            migrationBuilder.DropColumn(
                name: "iva_rate",
                schema: "ops",
                table: "OperationalConfig");
        }
    }
}
