using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class AddTiie28dAndInpcMonthly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "tiie_28d_rate",
                schema: "ops",
                table: "OperationalConfig",
                type: "decimal(10,4)",
                precision: 10,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "tiie_28d_rate_updated_at",
                schema: "ops",
                table: "OperationalConfig",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "InpcMonthly",
                schema: "ops",
                columns: table => new
                {
                    periodo = table.Column<DateOnly>(type: "date", nullable: false),
                    inpc_index = table.Column<decimal>(type: "decimal(10,4)", nullable: false),
                    captured_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InpcMonthly", x => x.periodo);
                });

            migrationBuilder.UpdateData(
                schema: "ops",
                table: "OperationalConfig",
                keyColumn: "id",
                keyValue: 1,
                columns: new[] { "tiie_28d_rate", "tiie_28d_rate_updated_at" },
                values: new object[] { null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InpcMonthly",
                schema: "ops");

            migrationBuilder.DropColumn(
                name: "tiie_28d_rate",
                schema: "ops",
                table: "OperationalConfig");

            migrationBuilder.DropColumn(
                name: "tiie_28d_rate_updated_at",
                schema: "ops",
                table: "OperationalConfig");
        }
    }
}
