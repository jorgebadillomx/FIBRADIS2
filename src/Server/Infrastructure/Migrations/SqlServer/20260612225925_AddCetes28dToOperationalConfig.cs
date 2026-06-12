using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class AddCetes28dToOperationalConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "cetes_28d_rate",
                schema: "ops",
                table: "OperationalConfig",
                type: "decimal(10,4)",
                precision: 10,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "cetes_28d_rate_updated_at",
                schema: "ops",
                table: "OperationalConfig",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.UpdateData(
                schema: "ops",
                table: "OperationalConfig",
                keyColumn: "id",
                keyValue: 1,
                columns: new[] { "cetes_28d_rate", "cetes_28d_rate_updated_at" },
                values: new object[] { null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "cetes_28d_rate",
                schema: "ops",
                table: "OperationalConfig");

            migrationBuilder.DropColumn(
                name: "cetes_28d_rate_updated_at",
                schema: "ops",
                table: "OperationalConfig");
        }
    }
}
