using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFibraNewsMonthsToOperationalConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "fibra_news_months",
                schema: "ops",
                table: "OperationalConfig",
                type: "int",
                nullable: false,
                defaultValue: 15);

            migrationBuilder.Sql("UPDATE [ops].[OperationalConfig] SET [fibra_news_months] = 15 WHERE [fibra_news_months] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "fibra_news_months",
                schema: "ops",
                table: "OperationalConfig");
        }
    }
}
