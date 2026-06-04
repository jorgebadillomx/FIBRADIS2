using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSiteContentAndTermsAccepted : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasAcceptedTerms",
                schema: "auth",
                table: "User",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "TermsAcceptedAt",
                schema: "auth",
                table: "User",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "contact_email",
                schema: "ops",
                table: "OperationalConfig",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "terms_enabled",
                schema: "ops",
                table: "OperationalConfig",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "terms_text",
                schema: "ops",
                table: "OperationalConfig",
                type: "text",
                nullable: true);

            migrationBuilder.UpdateData(
                schema: "ops",
                table: "OperationalConfig",
                keyColumn: "id",
                keyValue: 1,
                columns: new[] { "contact_email", "terms_text" },
                values: new object[] { "contacto@fibradis.mx", null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasAcceptedTerms",
                schema: "auth",
                table: "User");

            migrationBuilder.DropColumn(
                name: "TermsAcceptedAt",
                schema: "auth",
                table: "User");

            migrationBuilder.DropColumn(
                name: "contact_email",
                schema: "ops",
                table: "OperationalConfig");

            migrationBuilder.DropColumn(
                name: "terms_enabled",
                schema: "ops",
                table: "OperationalConfig");

            migrationBuilder.DropColumn(
                name: "terms_text",
                schema: "ops",
                table: "OperationalConfig");
        }
    }
}
