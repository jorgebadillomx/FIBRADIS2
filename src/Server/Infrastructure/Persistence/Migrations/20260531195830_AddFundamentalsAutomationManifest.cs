using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFundamentalsAutomationManifest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FibraNewsMonths",
                schema: "ops",
                table: "OperationalConfig",
                newName: "fibra_news_months");

            migrationBuilder.AddColumn<int>(
                name: "fundamentals_cadence_minutes",
                schema: "ops",
                table: "OperationalConfig",
                type: "int",
                nullable: false,
                defaultValue: 360);

            migrationBuilder.CreateTable(
                name: "FundamentalSourceManifest",
                schema: "fundamentals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "newsequentialid()"),
                    source_name = table.Column<string>(type: "varchar(20)", nullable: false),
                    fibra_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    source_title = table.Column<string>(type: "nvarchar(300)", nullable: false),
                    period = table.Column<string>(type: "varchar(10)", nullable: true),
                    report_type = table.Column<string>(type: "varchar(30)", nullable: false),
                    discovery_status = table.Column<string>(type: "varchar(40)", nullable: false),
                    package_url = table.Column<string>(type: "nvarchar(500)", nullable: false),
                    download_url = table.Column<string>(type: "nvarchar(1000)", nullable: true),
                    download_signature = table.Column<string>(type: "nvarchar(500)", nullable: true),
                    pdf_url = table.Column<string>(type: "nvarchar(1000)", nullable: true),
                    file_name = table.Column<string>(type: "nvarchar(260)", nullable: true),
                    source_published_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    first_seen_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    last_seen_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    last_decision = table.Column<string>(type: "varchar(40)", nullable: false),
                    last_decision_reason = table.Column<string>(type: "nvarchar(500)", nullable: true),
                    last_processed_record_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    last_error = table.Column<string>(type: "nvarchar(500)", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "getutcdate()"),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "getutcdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FundamentalSourceManifest", x => x.id);
                    table.ForeignKey(
                        name: "FK_FundamentalSourceManifest_Fibra_fibra_id",
                        column: x => x.fibra_id,
                        principalSchema: "catalog",
                        principalTable: "Fibra",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                schema: "ops",
                table: "OperationalConfig",
                keyColumn: "id",
                keyValue: 1,
                column: "fundamentals_cadence_minutes",
                value: 360);

            migrationBuilder.CreateIndex(
                name: "IX_FundamentalSourceManifest_FibraId_Period_ReportType",
                schema: "fundamentals",
                table: "FundamentalSourceManifest",
                columns: new[] { "fibra_id", "period", "report_type" });

            migrationBuilder.CreateIndex(
                name: "UX_FundamentalSourceManifest_SourceName_PackageUrl",
                schema: "fundamentals",
                table: "FundamentalSourceManifest",
                columns: new[] { "source_name", "package_url" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FundamentalSourceManifest",
                schema: "fundamentals");

            migrationBuilder.DropColumn(
                name: "fundamentals_cadence_minutes",
                schema: "ops",
                table: "OperationalConfig");

            migrationBuilder.RenameColumn(
                name: "fibra_news_months",
                schema: "ops",
                table: "OperationalConfig",
                newName: "FibraNewsMonths");
        }
    }
}
