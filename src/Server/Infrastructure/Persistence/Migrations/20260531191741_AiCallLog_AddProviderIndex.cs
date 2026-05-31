using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AiCallLog_AddProviderIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FibraNewsMonths",
                schema: "ops",
                table: "OperationalConfig",
                newName: "fibra_news_months");

            migrationBuilder.CreateIndex(
                name: "IX_AiCallLog_Provider_CreatedAt",
                schema: "jobs",
                table: "AiCallLog",
                columns: new[] { "provider", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AiCallLog_Provider_CreatedAt",
                schema: "jobs",
                table: "AiCallLog");

            migrationBuilder.RenameColumn(
                name: "fibra_news_months",
                schema: "ops",
                table: "OperationalConfig",
                newName: "FibraNewsMonths");
        }
    }
}
