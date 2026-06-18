using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Infrastructure.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class AddFibraEXIAndAGRO : Migration
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
                    { new Guid("28717c15-5e9a-367f-7079-4eb5013c9369"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", "Fibra EXI es un fideicomiso de inversión en energía e infraestructura (Fibra E) especializado en concesiones de autopistas de peaje. Su portafolio comprende más de 400 km de carreteras concesionadas en el centro del país, incluyendo la autopista Salamanca-León en Guanajuato. Cotiza en la BMV bajo el ticker FEXI21.", "Fibra EXI", "https://fibraexi.com/es/inversionistas/", "BMV", "[\"Fibra EXI\",\"FEXI\",\"FEXI21\"]", "http://www.economatica.mx/FEXI/REPORTES%20TRIMESTRALES/", "Infraestructura", "Fibra EXI", "https://fibraexi.com/es/", "Active", "FEXI21", "FEXI21.MX" },
                    { new Guid("53273c63-873a-3788-88ad-dca50bffca6e"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", "AgroFibra es el primer fideicomiso de inversión en bienes raíces en México especializado en el sector agroalimentario. Su portafolio incluye propiedades agrícolas arrendadas a productores y empresas de la cadena agroalimentaria en diversas regiones del país. Cotiza en BIVA bajo el ticker AGRO22.", "AgroFibra", "https://agrofibra.com/inversionistas/", "BIVA", "[\"AgroFibra\",\"Fibra AGRO\",\"AGRO\",\"AGRO22\"]", "http://www.economatica.mx/AGRO/REPORTES%20TRIMESTRALES%20/", "Agroalimentario", "AgroFibra", "https://agrofibra.com/", "Active", "AGRO22", "AGRO22.MX" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("28717c15-5e9a-367f-7079-4eb5013c9369"));

            migrationBuilder.DeleteData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("53273c63-873a-3788-88ad-dca50bffca6e"));
        }
    }
}
