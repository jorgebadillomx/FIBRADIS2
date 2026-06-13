using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Infrastructure.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class AddBenchmarkFibras : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "catalog",
                table: "Fibra",
                columns: new[] { "Id", "created_at", "currency", "description", "full_name", "investor_url", "market", "name_variants", "reports_url", "sector", "short_name", "site_url", "state", "ticker", "yahoo_ticker" },
                values: new object[,]
                {
                    { new Guid("c874e0b2-dac0-2b26-da97-48e85de1b5a4"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", null, "IPC BMV", null, "BMV", "[]", null, "Índice", "IPC", null, "Inactive", "^MXX", "^MXX" },
                    { new Guid("d155fd8f-1d3d-33e7-6480-56768bc708e6"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "USD", null, "S&P 500", null, "NYSE", "[]", null, "Índice", "S&P 500", null, "Inactive", "^GSPC", "^GSPC" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("c874e0b2-dac0-2b26-da97-48e85de1b5a4"));

            migrationBuilder.DeleteData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("d155fd8f-1d3d-33e7-6480-56768bc708e6"));
        }
    }
}
