using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOpsConfigAndAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "ops");

            migrationBuilder.CreateTable(
                name: "ConfigAuditLog",
                schema: "ops",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    actor = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    changed_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    field_name = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    previous_value = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    new_value = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfigAuditLog", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "OperationalConfig",
                schema: "ops",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false),
                    commission_factor = table.Column<decimal>(type: "decimal(10,6)", precision: 10, scale: 6, nullable: false),
                    avg_periods = table.Column<int>(type: "int", nullable: false),
                    news_cadence_minutes = table.Column<int>(type: "int", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updated_by = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperationalConfig", x => x.id);
                });

            migrationBuilder.InsertData(
                schema: "ops",
                table: "OperationalConfig",
                columns: new[] { "id", "avg_periods", "commission_factor", "news_cadence_minutes", "updated_at", "updated_by" },
                values: new object[] { 1, 4, 0.006m, 60, new DateTimeOffset(new DateTime(2026, 5, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system" });

            migrationBuilder.CreateIndex(
                name: "IX_ConfigAuditLog_changed_at",
                schema: "ops",
                table: "ConfigAuditLog",
                column: "changed_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConfigAuditLog",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "OperationalConfig",
                schema: "ops");
        }
    }
}
