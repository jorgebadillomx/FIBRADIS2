using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUniverseDegradationThreshold : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UniverseDegradationThresholdPct",
                schema: "ops",
                table: "OperationalConfig",
                type: "integer",
                nullable: false,
                defaultValue: 30);

            migrationBuilder.UpdateData(
                schema: "ops",
                table: "OperationalConfig",
                keyColumn: "id",
                keyValue: 1,
                column: "UniverseDegradationThresholdPct",
                value: 30);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UniverseDegradationThresholdPct",
                schema: "ops",
                table: "OperationalConfig");
        }
    }
}
