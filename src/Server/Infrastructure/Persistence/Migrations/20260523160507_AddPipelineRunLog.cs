using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPipelineRunLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PipelineRunLog",
                schema: "jobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "newsequentialid()"),
                    pipeline = table.Column<string>(type: "varchar(50)", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    status = table.Column<string>(type: "varchar(20)", nullable: false),
                    items_processed = table.Column<int>(type: "int", nullable: true),
                    error_count = table.Column<int>(type: "int", nullable: true),
                    triggered_by = table.Column<string>(type: "varchar(100)", nullable: true),
                    details = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "getutcdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PipelineRunLog", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PipelineRunLog_Pipeline_StartedAt",
                schema: "jobs",
                table: "PipelineRunLog",
                columns: new[] { "pipeline", "started_at" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PipelineRunLog",
                schema: "jobs");
        }
    }
}
