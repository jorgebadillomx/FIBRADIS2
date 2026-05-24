using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFundamentalRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "fundamentals");

            migrationBuilder.CreateTable(
                name: "FundamentalRecord",
                schema: "fundamentals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "newsequentialid()"),
                    fibra_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    period = table.Column<string>(type: "varchar(10)", nullable: false),
                    status = table.Column<string>(type: "varchar(20)", nullable: false),
                    processing_mode = table.Column<string>(type: "varchar(20)", nullable: false),
                    cap_rate = table.Column<decimal>(type: "decimal(18,6)", nullable: true),
                    nav_per_cbfi = table.Column<decimal>(type: "decimal(18,6)", nullable: true),
                    ltv = table.Column<decimal>(type: "decimal(18,6)", nullable: true),
                    noi_margin = table.Column<decimal>(type: "decimal(18,6)", nullable: true),
                    ffo_margin = table.Column<decimal>(type: "decimal(18,6)", nullable: true),
                    quarterly_distribution = table.Column<decimal>(type: "decimal(18,6)", nullable: true),
                    summary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    pdf_reference = table.Column<string>(type: "nvarchar(500)", nullable: true),
                    pdf_uploaded_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    is_possible_update = table.Column<bool>(type: "bit", nullable: false),
                    imported_by = table.Column<string>(type: "varchar(100)", nullable: true),
                    confirmed_by = table.Column<string>(type: "varchar(100)", nullable: true),
                    captured_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "getutcdate()"),
                    confirmed_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    error_reason = table.Column<string>(type: "nvarchar(500)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FundamentalRecord", x => x.id);
                    table.ForeignKey(
                        name: "FK_FundamentalRecord_Fibra_fibra_id",
                        column: x => x.fibra_id,
                        principalSchema: "catalog",
                        principalTable: "Fibra",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FundamentalRecord_FibraId_Period_Status",
                schema: "fundamentals",
                table: "FundamentalRecord",
                columns: new[] { "fibra_id", "period", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FundamentalRecord",
                schema: "fundamentals");
        }
    }
}
