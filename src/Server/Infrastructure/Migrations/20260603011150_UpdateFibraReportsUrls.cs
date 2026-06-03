using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateFibraReportsUrls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("132f2dbd-1a77-3ca5-01eb-c65b7a03b14b"),
                column: "reports_url",
                value: "https://fibra-upsite.com/inversionistas/razones");

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("15d3465f-5ff4-a84c-c883-1dd381fd22f0"),
                column: "reports_url",
                value: "https://cfecapital.com.mx/informacion-financiera");

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("2799e174-1233-93ba-87ce-ade15c0c4010"),
                column: "reports_url",
                value: "https://www.fibramacquarie.com/es/inversionistas.html");

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("38a237f5-9065-f4ec-7b19-c116a9642c6f"),
                column: "reports_url",
                value: "https://fibrainn.mx/inversionistas/resultados-trimestrales");

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("5d911406-8042-d1db-90b0-6c5aebb882bf"),
                column: "reports_url",
                value: "https://fibradanhos.com.mx/reportes-trimestrales.html");

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("6cbe26c2-2ed0-6df0-6242-1c78d011e9fc"),
                column: "reports_url",
                value: "https://www.bmv.com.mx/es/emisoras/informacionfinanciera/FIHO-30057-CGEN_CAPIT");

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("7347fc56-b1d8-1853-7712-ac4dc204564c"),
                column: "reports_url",
                value: "https://funo.mx/inversionistas/suplementos-informativos");

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("7882f6d3-304f-5de6-2211-60508c2021c6"),
                column: "reports_url",
                value: "https://www.fibramty.com/en/inversionistas");

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("9797ad53-1324-9a81-6102-992b9f07e92c"),
                column: "reports_url",
                value: "https://fibrasoma.group/investors/quarterly-reports-2/");

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("ae26cf6f-b4dc-be5a-3355-f4f4a9165499"),
                column: "reports_url",
                value: "https://vesta.com.mx/informacion-financiera/asg");

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("d96dd899-9a7e-3c95-a9ba-bbf62577a241"),
                column: "reports_url",
                value: "https://www.bmv.com.mx/es/emisoras/informacionfinanciera/HCITY-31249-CGEN_CAPIT");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("132f2dbd-1a77-3ca5-01eb-c65b7a03b14b"),
                column: "reports_url",
                value: null);

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("15d3465f-5ff4-a84c-c883-1dd381fd22f0"),
                column: "reports_url",
                value: "https://cfecapital.com.mx/inversionistas");

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("2799e174-1233-93ba-87ce-ade15c0c4010"),
                column: "reports_url",
                value: null);

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("38a237f5-9065-f4ec-7b19-c116a9642c6f"),
                column: "reports_url",
                value: null);

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("5d911406-8042-d1db-90b0-6c5aebb882bf"),
                column: "reports_url",
                value: null);

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("6cbe26c2-2ed0-6df0-6242-1c78d011e9fc"),
                column: "reports_url",
                value: null);

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("7347fc56-b1d8-1853-7712-ac4dc204564c"),
                column: "reports_url",
                value: null);

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("7882f6d3-304f-5de6-2211-60508c2021c6"),
                column: "reports_url",
                value: null);

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("9797ad53-1324-9a81-6102-992b9f07e92c"),
                column: "reports_url",
                value: null);

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("ae26cf6f-b4dc-be5a-3355-f4f4a9165499"),
                column: "reports_url",
                value: null);

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("d96dd899-9a7e-3c95-a9ba-bbf62577a241"),
                column: "reports_url",
                value: null);
        }
    }
}
