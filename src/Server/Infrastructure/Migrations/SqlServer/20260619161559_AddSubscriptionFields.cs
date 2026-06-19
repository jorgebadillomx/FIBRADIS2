using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class AddSubscriptionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "email_confirmed_at",
                schema: "auth",
                table: "User",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "how_did_you_hear",
                schema: "auth",
                table: "User",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "subscription_ends_at",
                schema: "auth",
                table: "User",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "subscription_started_at",
                schema: "auth",
                table: "User",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "subscription_type",
                schema: "auth",
                table: "User",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "trial_ends_at",
                schema: "auth",
                table: "User",
                type: "datetime2",
                nullable: true);

            migrationBuilder.Sql("""
UPDATE [auth].[User]
SET subscription_type = 'Lifetime',
    subscription_started_at = COALESCE(FechaPago, SYSUTCDATETIME()),
    subscription_ends_at = NULL,
    IsActive = 1
WHERE IsActive = 1;
""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "email_confirmed_at",
                schema: "auth",
                table: "User");

            migrationBuilder.DropColumn(
                name: "how_did_you_hear",
                schema: "auth",
                table: "User");

            migrationBuilder.DropColumn(
                name: "subscription_ends_at",
                schema: "auth",
                table: "User");

            migrationBuilder.DropColumn(
                name: "subscription_started_at",
                schema: "auth",
                table: "User");

            migrationBuilder.DropColumn(
                name: "subscription_type",
                schema: "auth",
                table: "User");

            migrationBuilder.DropColumn(
                name: "trial_ends_at",
                schema: "auth",
                table: "User");
        }
    }
}
