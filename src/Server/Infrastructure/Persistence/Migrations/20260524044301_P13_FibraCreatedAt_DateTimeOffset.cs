using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class P13_FibraCreatedAt_DateTimeOffset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "created_at",
                schema: "catalog",
                table: "Fibra",
                type: "datetimeoffset",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("055c422a-c2df-ec0f-ab61-2b5c3ede52c2"),
                column: "created_at",
                value: new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("132f2dbd-1a77-3ca5-01eb-c65b7a03b14b"),
                column: "created_at",
                value: new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("15d3465f-5ff4-a84c-c883-1dd381fd22f0"),
                column: "created_at",
                value: new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("17e765b2-df1e-6842-3dcf-ec7506563c89"),
                column: "created_at",
                value: new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("2799e174-1233-93ba-87ce-ade15c0c4010"),
                column: "created_at",
                value: new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("2f25d292-5a8d-a262-1cdc-093621c7471c"),
                column: "created_at",
                value: new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("3129ee4f-d156-04c8-a03f-42bdc468ff27"),
                column: "created_at",
                value: new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("32377b6d-9244-a715-0279-2660cc6b62a5"),
                column: "created_at",
                value: new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("32418186-9e2c-942b-8f4a-1e61388760a4"),
                column: "created_at",
                value: new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("38a237f5-9065-f4ec-7b19-c116a9642c6f"),
                column: "created_at",
                value: new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("5d911406-8042-d1db-90b0-6c5aebb882bf"),
                column: "created_at",
                value: new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("6cbe26c2-2ed0-6df0-6242-1c78d011e9fc"),
                column: "created_at",
                value: new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("7347fc56-b1d8-1853-7712-ac4dc204564c"),
                column: "created_at",
                value: new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("7882f6d3-304f-5de6-2211-60508c2021c6"),
                column: "created_at",
                value: new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("894a2120-2ad3-d3f4-44fd-0d0c423b849f"),
                column: "created_at",
                value: new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("8d7ad206-8591-fd28-f33f-d0b887817b5c"),
                column: "created_at",
                value: new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("933f9202-f943-0342-0e05-5cfd283a5bbc"),
                column: "created_at",
                value: new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("9797ad53-1324-9a81-6102-992b9f07e92c"),
                column: "created_at",
                value: new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("ae26cf6f-b4dc-be5a-3355-f4f4a9165499"),
                column: "created_at",
                value: new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("d96dd899-9a7e-3c95-a9ba-bbf62577a241"),
                column: "created_at",
                value: new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                schema: "catalog",
                table: "Fibra",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset");

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("055c422a-c2df-ec0f-ab61-2b5c3ede52c2"),
                column: "created_at",
                value: new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("132f2dbd-1a77-3ca5-01eb-c65b7a03b14b"),
                column: "created_at",
                value: new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("15d3465f-5ff4-a84c-c883-1dd381fd22f0"),
                column: "created_at",
                value: new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("17e765b2-df1e-6842-3dcf-ec7506563c89"),
                column: "created_at",
                value: new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("2799e174-1233-93ba-87ce-ade15c0c4010"),
                column: "created_at",
                value: new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("2f25d292-5a8d-a262-1cdc-093621c7471c"),
                column: "created_at",
                value: new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("3129ee4f-d156-04c8-a03f-42bdc468ff27"),
                column: "created_at",
                value: new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("32377b6d-9244-a715-0279-2660cc6b62a5"),
                column: "created_at",
                value: new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("32418186-9e2c-942b-8f4a-1e61388760a4"),
                column: "created_at",
                value: new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("38a237f5-9065-f4ec-7b19-c116a9642c6f"),
                column: "created_at",
                value: new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("5d911406-8042-d1db-90b0-6c5aebb882bf"),
                column: "created_at",
                value: new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("6cbe26c2-2ed0-6df0-6242-1c78d011e9fc"),
                column: "created_at",
                value: new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("7347fc56-b1d8-1853-7712-ac4dc204564c"),
                column: "created_at",
                value: new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("7882f6d3-304f-5de6-2211-60508c2021c6"),
                column: "created_at",
                value: new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("894a2120-2ad3-d3f4-44fd-0d0c423b849f"),
                column: "created_at",
                value: new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("8d7ad206-8591-fd28-f33f-d0b887817b5c"),
                column: "created_at",
                value: new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("933f9202-f943-0342-0e05-5cfd283a5bbc"),
                column: "created_at",
                value: new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("9797ad53-1324-9a81-6102-992b9f07e92c"),
                column: "created_at",
                value: new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("ae26cf6f-b4dc-be5a-3355-f4f4a9165499"),
                column: "created_at",
                value: new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                schema: "catalog",
                table: "Fibra",
                keyColumn: "Id",
                keyValue: new Guid("d96dd899-9a7e-3c95-a9ba-bbf62577a241"),
                column: "created_at",
                value: new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));
        }
    }
}
