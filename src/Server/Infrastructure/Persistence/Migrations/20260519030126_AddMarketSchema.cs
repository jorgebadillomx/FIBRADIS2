using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMarketSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "market");

            migrationBuilder.CreateTable(
                name: "DailySnapshot",
                schema: "market",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    fibra_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ticker = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    open = table.Column<decimal>(type: "decimal(18,6)", nullable: true),
                    high = table.Column<decimal>(type: "decimal(18,6)", nullable: true),
                    low = table.Column<decimal>(type: "decimal(18,6)", nullable: true),
                    close = table.Column<decimal>(type: "decimal(18,6)", nullable: true),
                    volume = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailySnapshot", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "PriceSnapshot",
                schema: "market",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    fibra_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ticker = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    last_price = table.Column<decimal>(type: "decimal(18,6)", nullable: true),
                    daily_change = table.Column<decimal>(type: "decimal(18,6)", nullable: true),
                    daily_change_pct = table.Column<decimal>(type: "decimal(10,4)", nullable: true),
                    volume = table.Column<long>(type: "bigint", nullable: true),
                    week52_high = table.Column<decimal>(type: "decimal(18,6)", nullable: true),
                    week52_low = table.Column<decimal>(type: "decimal(18,6)", nullable: true),
                    captured_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    error_reason = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceSnapshot", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "UX_DailySnapshot_FibraId_Date",
                schema: "market",
                table: "DailySnapshot",
                columns: new[] { "fibra_id", "date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PriceSnapshot_FibraId_CapturedAt",
                schema: "market",
                table: "PriceSnapshot",
                columns: new[] { "fibra_id", "captured_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailySnapshot",
                schema: "market");

            migrationBuilder.DropTable(
                name: "PriceSnapshot",
                schema: "market");
        }
    }
}
