using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "catalog");

            migrationBuilder.CreateTable(
                name: "Fibra",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ticker = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    full_name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    short_name = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    sector = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    market = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    currency = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    state = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    site_url = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    investor_url = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    reports_url = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    name_variants = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fibra", x => x.Id);
                });

            migrationBuilder.InsertData(
                schema: "catalog",
                table: "Fibra",
                columns: new[] { "Id", "created_at", "currency", "full_name", "investor_url", "market", "name_variants", "reports_url", "sector", "short_name", "site_url", "state", "ticker" },
                values: new object[,]
                {
                    { new Guid("2799e174-1233-93ba-87ce-ade15c0c4010"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "MXN", "Fibra Macquarie", "https://fibramacquarie.com.mx/ri", "BMV", "[\"Fibra MQ\",\"Macquarie\",\"FIBRAMQ\"]", null, "Industrial", "FibraMQ", "https://fibramacquarie.com.mx", "Active", "FIBRAMQ12" },
                    { new Guid("38a237f5-9065-f4ec-7b19-c116a9642c6f"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "MXN", "Fibra Inn", null, "BMV", "[\"Fibra Inn\",\"FINN\"]", null, "Hotelero", "Fibra Inn", "https://fibrainn.com.mx", "Active", "FINN13" },
                    { new Guid("5d911406-8042-d1db-90b0-6c5aebb882bf"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "MXN", "Fibra Danhos", "https://fibradanhos.com.mx/ri", "BMV", "[\"Danhos\",\"DANHOS\"]", null, "Comercial", "Danhos", "https://fibradanhos.com.mx", "Active", "DANHOS13" },
                    { new Guid("6cbe26c2-2ed0-6df0-6242-1c78d011e9fc"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "MXN", "Fibra Hotel", null, "BMV", "[\"Fibra Hotel\",\"FIHO\"]", null, "Hotelero", "Fibra Hotel", "https://fibrahotel.com", "Active", "FIHO12" },
                    { new Guid("7347fc56-b1d8-1853-7712-ac4dc204564c"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "MXN", "Fibra Uno", "https://fibra.uno/inversionistas", "BMV", "[\"Fibra Uno\",\"FUNO\"]", null, "Diversificado", "Fibra Uno", "https://fibra.uno", "Active", "FUNO11" },
                    { new Guid("7882f6d3-304f-5de6-2211-60508c2021c6"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "MXN", "Fibra Monterrey", "https://fibramty.com/inversionistas", "BMV", "[\"Fibra Monterrey\",\"FibraMTY\",\"FMTY\"]", null, "Industrial", "Fibra MTY", "https://fibramty.com", "Active", "FMTY14" },
                    { new Guid("894a2120-2ad3-d3f4-44fd-0d0c423b849f"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "MXN", "Fibra Terra", null, "BMV", "[\"Fibra Terra\",\"TERRA\"]", null, "Industrial", "Terra", "https://fibra-terra.com", "Active", "TERRA13" },
                    { new Guid("ae26cf6f-b4dc-be5a-3355-f4f4a9165499"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "MXN", "Fibra Vesta", "https://fibravesta.com/ri", "BMV", "[\"Fibra Vesta\",\"VESTA\"]", null, "Industrial", "Vesta", "https://fibravesta.com", "Active", "VESTA15" },
                    { new Guid("cc194e75-e5a3-ee5e-0673-0f61be3fe509"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "MXN", "Fibra Plus", null, "BMV", "[\"Fibra Plus\",\"PLUS\"]", null, "Diversificado", "Fibra Plus", "https://fibraplus.mx", "Active", "PLUS18" },
                    { new Guid("d96dd899-9a7e-3c95-a9ba-bbf62577a241"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "MXN", "Fibra Hotel City Express", null, "BMV", "[\"Hotel City Express\",\"HCITY\",\"HC\"]", null, "Hotelero", "HC", "https://hcity.com.mx", "Active", "HCITY17" }
                });

            migrationBuilder.CreateIndex(
                name: "UX_Fibra_Ticker",
                schema: "catalog",
                table: "Fibra",
                column: "ticker",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Fibra",
                schema: "catalog");
        }
    }
}
