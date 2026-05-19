using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddYahooTickerToFibra : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "yahoo_ticker",
                schema: "catalog",
                table: "Fibra",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("2799e174-1233-93ba-87ce-ade15c0c4010"),
                column: "yahoo_ticker",
                value: "FIBRAMQ12.MX");

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("38a237f5-9065-f4ec-7b19-c116a9642c6f"),
                column: "yahoo_ticker",
                value: "FINN13.MX");

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("5d911406-8042-d1db-90b0-6c5aebb882bf"),
                column: "yahoo_ticker",
                value: "DANHOS13.MX");

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("6cbe26c2-2ed0-6df0-6242-1c78d011e9fc"),
                column: "yahoo_ticker",
                value: "FIHO12.MX");

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("7347fc56-b1d8-1853-7712-ac4dc204564c"),
                column: "yahoo_ticker",
                value: "FUNO11.MX");

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("7882f6d3-304f-5de6-2211-60508c2021c6"),
                column: "yahoo_ticker",
                value: "FMTY14.MX");

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("894a2120-2ad3-d3f4-44fd-0d0c423b849f"),
                column: "yahoo_ticker",
                value: "TERRA13.MX");

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("ae26cf6f-b4dc-be5a-3355-f4f4a9165499"),
                column: "yahoo_ticker",
                value: "VESTA.MX");

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("cc194e75-e5a3-ee5e-0673-0f61be3fe509"),
                column: "yahoo_ticker",
                value: "PLUS18.MX");

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("d96dd899-9a7e-3c95-a9ba-bbf62577a241"),
                column: "yahoo_ticker",
                value: "HCITY.MX");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "yahoo_ticker",
                schema: "catalog",
                table: "Fibra");
        }
    }
}
