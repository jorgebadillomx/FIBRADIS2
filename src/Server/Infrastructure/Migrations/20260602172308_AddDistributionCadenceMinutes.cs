using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDistributionCadenceMinutes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "distribution_cadence_minutes",
                schema: "ops",
                table: "OperationalConfig",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                schema: "ops",
                table: "OperationalConfig",
                keyColumn: "id",
                keyValue: 1,
                column: "distribution_cadence_minutes",
                value: 1440);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "distribution_cadence_minutes",
                schema: "ops",
                table: "OperationalConfig");
        }
    }
}
