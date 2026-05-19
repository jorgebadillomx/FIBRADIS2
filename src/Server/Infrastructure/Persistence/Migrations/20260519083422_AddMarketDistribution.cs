using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMarketDistribution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Distribution",
                schema: "market",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    fibra_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ticker = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    payment_date = table.Column<DateOnly>(type: "date", nullable: false),
                    amount_per_unit = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    source = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    captured_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Distribution", x => x.id);
                });

            migrationBuilder.InsertData(
                schema: "market",
                table: "Distribution",
                columns: new[] { "id", "amount_per_unit", "captured_at", "currency", "fibra_id", "payment_date", "source", "ticker" },
                values: new object[,]
                {
                    { new Guid("0712aff6-cf0f-55dd-a6dc-090e83c5233a"), 0.3720m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("7347fc56-b1d8-1853-7712-ac4dc204564c"), new DateOnly(2025, 6, 16), "seed", "FUNO11" },
                    { new Guid("3ad4a8a2-8e06-5923-aedd-25a49939ea84"), 0.2150m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("5d911406-8042-d1db-90b0-6c5aebb882bf"), new DateOnly(2025, 3, 17), "seed", "DANHOS13" },
                    { new Guid("3fce78aa-5887-0f04-a1f3-ca1e7fcb4803"), 0.1520m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("2799e174-1233-93ba-87ce-ade15c0c4010"), new DateOnly(2025, 12, 15), "seed", "FIBRAMQ12" },
                    { new Guid("4f56248a-fe67-33bf-4033-6df6a608ca2b"), 0.1820m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("894a2120-2ad3-d3f4-44fd-0d0c423b849f"), new DateOnly(2025, 12, 15), "seed", "TERRA13" },
                    { new Guid("52cae36d-59cc-bc56-cb33-cb5429192916"), 0.1750m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("894a2120-2ad3-d3f4-44fd-0d0c423b849f"), new DateOnly(2025, 6, 16), "seed", "TERRA13" },
                    { new Guid("85a4ed94-b620-298c-c457-b124599fb3b7"), 0.3780m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("7347fc56-b1d8-1853-7712-ac4dc204564c"), new DateOnly(2025, 9, 15), "seed", "FUNO11" },
                    { new Guid("8671a635-0d04-2660-ae19-dac73d60fd1b"), 0.1800m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("894a2120-2ad3-d3f4-44fd-0d0c423b849f"), new DateOnly(2025, 9, 15), "seed", "TERRA13" },
                    { new Guid("ac5bb4bb-0719-2425-d621-5f823e9da129"), 0.2200m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("5d911406-8042-d1db-90b0-6c5aebb882bf"), new DateOnly(2025, 6, 16), "seed", "DANHOS13" },
                    { new Guid("b2b41c08-7711-6ede-c2f0-8936c7a72828"), 0.1480m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("2799e174-1233-93ba-87ce-ade15c0c4010"), new DateOnly(2025, 9, 15), "seed", "FIBRAMQ12" },
                    { new Guid("b34cecad-3540-7d2b-5ea6-8000db9c5a0b"), 0.3610m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("7347fc56-b1d8-1853-7712-ac4dc204564c"), new DateOnly(2025, 3, 17), "seed", "FUNO11" },
                    { new Guid("c36fa6a1-117f-9d25-a947-8d6ee00e30f4"), 0.3840m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("7347fc56-b1d8-1853-7712-ac4dc204564c"), new DateOnly(2025, 12, 15), "seed", "FUNO11" },
                    { new Guid("e95c49b8-0da1-0cdb-9698-4ffee01cb629"), 0.2250m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("5d911406-8042-d1db-90b0-6c5aebb882bf"), new DateOnly(2025, 9, 15), "seed", "DANHOS13" },
                    { new Guid("f2af8992-b244-6d43-9610-2f69c170477f"), 0.2300m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("5d911406-8042-d1db-90b0-6c5aebb882bf"), new DateOnly(2025, 12, 15), "seed", "DANHOS13" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Distribution_FibraId_PaymentDate",
                schema: "market",
                table: "Distribution",
                columns: new[] { "fibra_id", "payment_date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Distribution",
                schema: "market");
        }
    }
}
