using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExpandCatalogUniverse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("cc194e75-e5a3-ee5e-0673-0f61be3fe509"));

            migrationBuilder.InsertData(
                schema: "catalog",
                table: "Fibra",
                columns: new[] { "Id", "created_at", "currency", "full_name", "investor_url", "market", "name_variants", "reports_url", "sector", "short_name", "site_url", "state", "ticker", "yahoo_ticker" },
                values: new object[,]
                {
                    { new Guid("055c422a-c2df-ec0f-ab61-2b5c3ede52c2"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "MXN", "FHipo", "https://fhipo.com/es/kit-para-inversionistas/", "BIVA", "[\"FHipo\",\"Fideicomiso Hipotecario\",\"FHIPO\",\"FHIPO14\"]", "https://fhipo.com/es/reportes-trimestrales/", "Hipotecario", "FHipo", "https://fhipo.com/es/", "Active", "FHIPO14", "FHIPO14.MX" },
                    { new Guid("132f2dbd-1a77-3ca5-01eb-c65b7a03b14b"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "MXN", "Fibra Upsite", null, "BMV", "[\"Fibra Upsite\",\"Upsite\",\"FIBRAUP\"]", null, "Industrial", "Upsite", "https://fibra-upsite.com", "Active", "FIBRAUP18", "FIBRAUP18.MX" },
                    { new Guid("15d3465f-5ff4-a84c-c883-1dd381fd22f0"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "MXN", "CFE Fibra E", "https://cfecapital.com.mx/inversionistas", "BMV/BIVA", "[\"CFE Fibra E\",\"FCFE\",\"FCFE18\"]", "https://cfecapital.com.mx/inversionistas", "Infraestructura", "CFE Fibra E", "https://cfecapital.com.mx", "Active", "FCFE18", "FCFE18.MX" },
                    { new Guid("17e765b2-df1e-6842-3dcf-ec7506563c89"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "MXN", "Fibra Storage", null, "BMV", "[\"Fibra Storage\",\"Storage\",\"STORAGE18\",\"U-Storage\"]", "https://fibrastorage.com/repositorio-informacion-financiera/", "Autoalmacenaje", "Fibra Storage", "https://fibrastorage.com", "Active", "STORAGE18", "STORAGE18.MX" },
                    { new Guid("2f25d292-5a8d-a262-1cdc-093621c7471c"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "MXN", "Fibra Next", "https://fibranext.mx/investors", "BMV", "[\"Fibra Next\",\"NEXT\",\"NEXT25\"]", "https://fibranext.mx/investors", "Industrial", "Fibra Next", "https://fibranext.mx", "Active", "NEXT25", "NEXT25.MX" },
                    { new Guid("3129ee4f-d156-04c8-a03f-42bdc468ff27"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "MXN", "Fibra Educa", "https://www.fibraeduca.com/invertir", "BMV", "[\"Fibra Educa\",\"EDUCA\",\"EDUCA18\"]", "https://www.fibraeduca.com/reportes-financieros", "Educativo", "Fibra Educa", "https://www.fibraeduca.com", "Active", "EDUCA18", "EDUCA18.MX" },
                    { new Guid("32377b6d-9244-a715-0279-2660cc6b62a5"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "MXN", "Fibra Prologis", "https://www.fibraprologis.com/en-US/investors", "BMV", "[\"Fibra Prologis\",\"Prologis\",\"FIBRAPL\"]", "https://www.fibraprologis.com/en-US/investors/financial-results", "Industrial", "Prologis", "https://www.fibraprologis.com/en-US", "Active", "FIBRAPL14", "FIBRAPL14.MX" },
                    { new Guid("32418186-9e2c-942b-8f4a-1e61388760a4"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "MXN", "Fibra Plus", null, "BMV", "[\"Fibra Plus\",\"FPLUS\",\"FPLUS16\"]", "https://www.fibraplus.mx/es/financiera/trimestrales", "Diversificado", "Fibra Plus", "https://www.fibraplus.mx", "Active", "FPLUS16", "FPLUS16.MX" },
                    { new Guid("8d7ad206-8591-fd28-f33f-d0b887817b5c"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "MXN", "Fibra Nova", "https://www.fibra-nova.com/inversionistas/como-invertir", "BIVA", "[\"Fibra Nova\",\"FNOVA\",\"FNOVA17\"]", "https://www.fibra-nova.com/inversionistas/reportes-trimestrales", "Diversificado", "Fibra Nova", "https://www.fibra-nova.com", "Active", "FNOVA17", "FNOVA17.MX" },
                    { new Guid("933f9202-f943-0342-0e05-5cfd283a5bbc"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "MXN", "Fibra Shop", "https://fibrashop.mx/contacto/", "BMV", "[\"Fibra Shop\",\"FSHOP\",\"FSHOP13\"]", "https://fibrashop.mx/informes-financieros/", "Comercial", "Fibra Shop", "https://fibrashop.mx", "Active", "FSHOP13", "FSHOP13.MX" },
                    { new Guid("9797ad53-1324-9a81-6102-992b9f07e92c"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "MXN", "Fibra SOMA", null, "BIVA", "[\"Fibra SOMA\",\"SOMA\",\"SOMA21\"]", null, "Comercial", "Fibra SOMA", "https://fibrasoma.group", "Active", "SOMA21", "SOMA21.MX" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("055c422a-c2df-ec0f-ab61-2b5c3ede52c2"));

            migrationBuilder.DeleteData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("132f2dbd-1a77-3ca5-01eb-c65b7a03b14b"));

            migrationBuilder.DeleteData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("15d3465f-5ff4-a84c-c883-1dd381fd22f0"));

            migrationBuilder.DeleteData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("17e765b2-df1e-6842-3dcf-ec7506563c89"));

            migrationBuilder.DeleteData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("2f25d292-5a8d-a262-1cdc-093621c7471c"));

            migrationBuilder.DeleteData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("3129ee4f-d156-04c8-a03f-42bdc468ff27"));

            migrationBuilder.DeleteData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("32377b6d-9244-a715-0279-2660cc6b62a5"));

            migrationBuilder.DeleteData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("32418186-9e2c-942b-8f4a-1e61388760a4"));

            migrationBuilder.DeleteData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("8d7ad206-8591-fd28-f33f-d0b887817b5c"));

            migrationBuilder.DeleteData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("933f9202-f943-0342-0e05-5cfd283a5bbc"));

            migrationBuilder.DeleteData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("9797ad53-1324-9a81-6102-992b9f07e92c"));

            migrationBuilder.InsertData(
                schema: "catalog",
                table: "Fibra",
                columns: new[] { "Id", "created_at", "currency", "full_name", "investor_url", "market", "name_variants", "reports_url", "sector", "short_name", "site_url", "state", "ticker", "yahoo_ticker" },
                values: new object[] { new Guid("cc194e75-e5a3-ee5e-0673-0f61be3fe509"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "MXN", "Fibra Plus", null, "BMV", "[\"Fibra Plus\",\"PLUS\"]", null, "Diversificado", "Fibra Plus", "https://fibraplus.mx", "Active", "PLUS18", "PLUS18.MX" });
        }
    }
}
