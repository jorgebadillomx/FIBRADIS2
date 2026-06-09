using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Norte19DiscoveryAndFixUrls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("ae26cf6f-b4dc-be5a-3355-f4f4a9165499"),
                column: "reports_url",
                value: "https://ir.vesta.com.mx/es/financial-results");

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("d96dd899-9a7e-3c95-a9ba-bbf62577a241"),
                column: "reports_url",
                value: "https://www.norte19.com/investors");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("ae26cf6f-b4dc-be5a-3355-f4f4a9165499"),
                column: "reports_url",
                value: "https://ir.vesta.com.mx/financial-results");

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("d96dd899-9a7e-3c95-a9ba-bbf62577a241"),
                column: "reports_url",
                value: "https://www.bmv.com.mx/es/emisoras/informacionfinanciera/HCITY-31249-CGEN_CAPIT");
        }
    }
}
