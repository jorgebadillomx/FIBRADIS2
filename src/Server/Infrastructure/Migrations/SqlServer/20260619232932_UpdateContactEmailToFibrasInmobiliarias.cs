using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class UpdateContactEmailToFibrasInmobiliarias : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                schema: "ops",
                table: "OperationalConfig",
                keyColumn: "id",
                keyValue: 1,
                column: "contact_email",
                value: "contacto@fibrasinmobiliarias.com");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                schema: "ops",
                table: "OperationalConfig",
                keyColumn: "id",
                keyValue: 1,
                column: "contact_email",
                value: "portafoliodefibras@gmail.com");
        }
    }
}
