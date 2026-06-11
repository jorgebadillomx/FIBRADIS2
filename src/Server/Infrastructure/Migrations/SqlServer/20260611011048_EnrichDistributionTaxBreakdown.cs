using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class EnrichDistributionTaxBreakdown : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "aviso_url",
                schema: "market",
                table: "Distribution",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "capital_return_amount",
                schema: "market",
                table: "Distribution",
                type: "decimal(18,6)",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ex_dividend_date",
                schema: "market",
                table: "Distribution",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "taxable_amount",
                schema: "market",
                table: "Distribution",
                type: "decimal(18,6)",
                nullable: true);

            migrationBuilder.UpdateData(
                schema: "market",
                table: "Distribution",
                keyColumn: "id",
                keyValue: new Guid("00777343-ebe9-68f0-c0d9-1a972125742f"),
                columns: new[] { "aviso_url", "capital_return_amount", "ex_dividend_date", "taxable_amount" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                schema: "market",
                table: "Distribution",
                keyColumn: "id",
                keyValue: new Guid("0137f687-5666-c94c-caeb-377119816221"),
                columns: new[] { "aviso_url", "capital_return_amount", "ex_dividend_date", "taxable_amount" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                schema: "market",
                table: "Distribution",
                keyColumn: "id",
                keyValue: new Guid("0712aff6-cf0f-55dd-a6dc-090e83c5233a"),
                columns: new[] { "aviso_url", "capital_return_amount", "ex_dividend_date", "taxable_amount" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                schema: "market",
                table: "Distribution",
                keyColumn: "id",
                keyValue: new Guid("08c47232-2945-58d3-e018-0c687cf9987d"),
                columns: new[] { "aviso_url", "capital_return_amount", "ex_dividend_date", "taxable_amount" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                schema: "market",
                table: "Distribution",
                keyColumn: "id",
                keyValue: new Guid("0bbbb154-b518-66f0-6637-9b15850397a8"),
                columns: new[] { "aviso_url", "capital_return_amount", "ex_dividend_date", "taxable_amount" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                schema: "market",
                table: "Distribution",
                keyColumn: "id",
                keyValue: new Guid("12754f3b-2021-99c6-dc06-a413948bc0fd"),
                columns: new[] { "aviso_url", "capital_return_amount", "ex_dividend_date", "taxable_amount" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                schema: "market",
                table: "Distribution",
                keyColumn: "id",
                keyValue: new Guid("134f77eb-4afc-4807-b139-36a64791d83f"),
                columns: new[] { "aviso_url", "capital_return_amount", "ex_dividend_date", "taxable_amount" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                schema: "market",
                table: "Distribution",
                keyColumn: "id",
                keyValue: new Guid("1864e970-626f-abaa-0c24-77b72ff123bd"),
                columns: new[] { "aviso_url", "capital_return_amount", "ex_dividend_date", "taxable_amount" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                schema: "market",
                table: "Distribution",
                keyColumn: "id",
                keyValue: new Guid("1c20f71e-193b-f476-88b7-b056789186a4"),
                columns: new[] { "aviso_url", "capital_return_amount", "ex_dividend_date", "taxable_amount" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                schema: "market",
                table: "Distribution",
                keyColumn: "id",
                keyValue: new Guid("22ab0fa4-a568-68ec-cf8f-04d474d88988"),
                columns: new[] { "aviso_url", "capital_return_amount", "ex_dividend_date", "taxable_amount" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                schema: "market",
                table: "Distribution",
                keyColumn: "id",
                keyValue: new Guid("24260c3b-ef78-0968-6f3c-efe375b565e3"),
                columns: new[] { "aviso_url", "capital_return_amount", "ex_dividend_date", "taxable_amount" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                schema: "market",
                table: "Distribution",
                keyColumn: "id",
                keyValue: new Guid("249e2535-408e-e0db-90e4-602ea74bb40d"),
                columns: new[] { "aviso_url", "capital_return_amount", "ex_dividend_date", "taxable_amount" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                schema: "market",
                table: "Distribution",
                keyColumn: "id",
                keyValue: new Guid("2c2c6ef6-29bb-468b-4a60-b8f1a5aad69b"),
                columns: new[] { "aviso_url", "capital_return_amount", "ex_dividend_date", "taxable_amount" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                schema: "market",
                table: "Distribution",
                keyColumn: "id",
                keyValue: new Guid("2ca1aa7d-0958-4583-040a-646cf586ad96"),
                columns: new[] { "aviso_url", "capital_return_amount", "ex_dividend_date", "taxable_amount" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                schema: "market",
                table: "Distribution",
                keyColumn: "id",
                keyValue: new Guid("2d5fcd74-962e-c0d6-f1f6-b8b25a0009b9"),
                columns: new[] { "aviso_url", "capital_return_amount", "ex_dividend_date", "taxable_amount" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                schema: "market",
                table: "Distribution",
                keyColumn: "id",
                keyValue: new Guid("3641773c-175e-3176-5af6-d3c9cc43fee0"),
                columns: new[] { "aviso_url", "capital_return_amount", "ex_dividend_date", "taxable_amount" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                schema: "market",
                table: "Distribution",
                keyColumn: "id",
                keyValue: new Guid("3ad4a8a2-8e06-5923-aedd-25a49939ea84"),
                columns: new[] { "aviso_url", "capital_return_amount", "ex_dividend_date", "taxable_amount" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                schema: "market",
                table: "Distribution",
                keyColumn: "id",
                keyValue: new Guid("3ccdc1eb-c781-6972-f073-fdd788cdb8b4"),
                columns: new[] { "aviso_url", "capital_return_amount", "ex_dividend_date", "taxable_amount" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                schema: "market",
                table: "Distribution",
                keyColumn: "id",
                keyValue: new Guid("3cd135c0-6ef9-07d2-c58c-9f23c8f6d969"),
                columns: new[] { "aviso_url", "capital_return_amount", "ex_dividend_date", "taxable_amount" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                schema: "market",
                table: "Distribution",
                keyColumn: "id",
                keyValue: new Guid("3e264b5f-5643-58fa-be69-21d0be470954"),
                columns: new[] { "aviso_url", "capital_return_amount", "ex_dividend_date", "taxable_amount" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                schema: "market",
                table: "Distribution",
                keyColumn: "id",
                keyValue: new Guid("3e2dbc4b-6422-5059-49c3-9238a184ee2e"),
                columns: new[] { "aviso_url", "capital_return_amount", "ex_dividend_date", "taxable_amount" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                schema: "market",
                table: "Distribution",
                keyColumn: "id",
                keyValue: new Guid("3ea58762-31cb-1687-2b59-931ba80860d8"),
                columns: new[] { "aviso_url", "capital_return_amount", "ex_dividend_date", "taxable_amount" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                schema: "market",
                table: "Distribution",
                keyColumn: "id",
                keyValue: new Guid("3fce78aa-5887-0f04-a1f3-ca1e7fcb4803"),
                columns: new[] { "aviso_url", "capital_return_amount", "ex_dividend_date", "taxable_amount" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                schema: "market",
                table: "Distribution",
                keyColumn: "id",
                keyValue: new Guid("49a7be56-9ac0-c735-7674-f1a584e26f70"),
                columns: new[] { "aviso_url", "capital_return_amount", "ex_dividend_date", "taxable_amount" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                schema: "market",
                table: "Distribution",
                keyColumn: "id",
                keyValue: new Guid("4f56248a-fe67-33bf-4033-6df6a608ca2b"),
                columns: new[] { "aviso_url", "capital_return_amount", "ex_dividend_date", "taxable_amount" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                schema: "market",
                table: "Distribution",
                keyColumn: "id",
                keyValue: new Guid("520331f0-8890-6215-eaa3-5fbca55ed140"),
                columns: new[] { "aviso_url", "capital_return_amount", "ex_dividend_date", "taxable_amount" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                schema: "market",
                table: "Distribution",
                keyColumn: "id",
                keyValue: new Guid("5233cae1-9172-0ef6-7c00-6ad251574aaf"),
                columns: new[] { "aviso_url", "capital_return_amount", "ex_dividend_date", "taxable_amount" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                schema: "market",
                table: "Distribution",
                keyColumn: "id",
                keyValue: new Guid("5278bfd8-b8a0-ea29-bcf7-0ba402d7e924"),
                columns: new[] { "aviso_url", "capital_return_amount", "ex_dividend_date", "taxable_amount" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                schema: "market",
                table: "Distribution",
                keyColumn: "id",
                keyValue: new Guid("52cae36d-59cc-bc56-cb33-cb5429192916"),
                columns: new[] { "aviso_url", "capital_return_amount", "ex_dividend_date", "taxable_amount" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                schema: "market",
                table: "Distribution",
                keyColumn: "id",
                keyValue: new Guid("535537f5-bb64-3772-486d-be8ef4b150ac"),
                columns: new[] { "aviso_url", "capital_return_amount", "ex_dividend_date", "taxable_amount" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                schema: "market",
                table: "Distribution",
                keyColumn: "id",
                keyValue: new Guid("546667b5-d9b5-8932-82fc-c2b4c7928d98"),
                columns: new[] { "aviso_url", "capital_return_amount", "ex_dividend_date", "taxable_amount" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                schema: "market",
                table: "Distribution",
                keyColumn: "id",
                keyValue: new Guid("5babaaff-7083-0205-c580-61ebd295fd10"),
                columns: new[] { "aviso_url", "capital_return_amount", "ex_dividend_date", "taxable_amount" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                schema: "market",
                table: "Distribution",
                keyColumn: "id",
                keyValue: new Guid("627ced36-69ac-91e8-7427-2824c2ef8221"),
                columns: new[] { "aviso_url", "capital_return_amount", "ex_dividend_date", "taxable_amount" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                schema: "market",
                table: "Distribution",
                keyColumn: "id",
                keyValue: new Guid("62b96edf-b872-2f4b-0799-674cb585264a"),
                columns: new[] { "aviso_url", "capital_return_amount", "ex_dividend_date", "taxable_amount" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                schema: "market",
                table: "Distribution",
                keyColumn: "id",
                keyValue: new Guid("662c2c1c-e08a-a6df-2c83-6331d8714653"),
                columns: new[] { "aviso_url", "capital_return_amount", "ex_dividend_date", "taxable_amount" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                schema: "market",
                table: "Distribution",
                keyColumn: "id",
                keyValue: new Guid("6c96a62e-17ed-a8a8-6f86-1f4841b22da3"),
                columns: new[] { "aviso_url", "capital_return_amount", "ex_dividend_date", "taxable_amount" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                schema: "market",
                table: "Distribution",
                keyColumn: "id",
                keyValue: new Guid("73cf3c5c-d05d-bdc0-e25c-15d18c654802"),
                columns: new[] { "aviso_url", "capital_return_amount", "ex_dividend_date", "taxable_amount" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                schema: "market",
                table: "Distribution",
                keyColumn: "id",
                keyValue: new Guid("7bfffaff-b5a1-0869-1ba2-a5b573b39dd3"),
                columns: new[] { "aviso_url", "capital_return_amount", "ex_dividend_date", "taxable_amount" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                schema: "market",
                table: "Distribution",
                keyColumn: "id",
                keyValue: new Guid("84dbaa5b-e460-a15e-2431-442e56d44f0f"),
                columns: new[] { "aviso_url", "capital_return_amount", "ex_dividend_date", "taxable_amount" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                schema: "market",
                table: "Distribution",
                keyColumn: "id",
                keyValue: new Guid("85a4ed94-b620-298c-c457-b124599fb3b7"),
                columns: new[] { "aviso_url", "capital_return_amount", "ex_dividend_date", "taxable_amount" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                schema: "market",
                table: "Distribution",
                keyColumn: "id",
                keyValue: new Guid("8671a635-0d04-2660-ae19-dac73d60fd1b"),
                columns: new[] { "aviso_url", "capital_return_amount", "ex_dividend_date", "taxable_amount" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                schema: "market",
                table: "Distribution",
                keyColumn: "id",
                keyValue: new Guid("900c466e-b739-8bef-221e-cff787935af6"),
                columns: new[] { "aviso_url", "capital_return_amount", "ex_dividend_date", "taxable_amount" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                schema: "market",
                table: "Distribution",
                keyColumn: "id",
                keyValue: new Guid("908677ac-2c56-9fbb-dae2-b610f6c491be"),
                columns: new[] { "aviso_url", "capital_return_amount", "ex_dividend_date", "taxable_amount" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                schema: "market",
                table: "Distribution",
                keyColumn: "id",
                keyValue: new Guid("a2e5c597-0238-9607-c8de-c3df6d9e4f65"),
                columns: new[] { "aviso_url", "capital_return_amount", "ex_dividend_date", "taxable_amount" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                schema: "market",
                table: "Distribution",
                keyColumn: "id",
                keyValue: new Guid("a84e7f7c-9852-61d3-fb7d-9dc07eeda3ce"),
                columns: new[] { "aviso_url", "capital_return_amount", "ex_dividend_date", "taxable_amount" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                schema: "market",
                table: "Distribution",
                keyColumn: "id",
                keyValue: new Guid("a9944cc5-75a9-4ccb-e53c-a64c9687c104"),
                columns: new[] { "aviso_url", "capital_return_amount", "ex_dividend_date", "taxable_amount" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                schema: "market",
                table: "Distribution",
                keyColumn: "id",
                keyValue: new Guid("aa044830-cfbd-bcf0-e79f-89c7287e8912"),
                columns: new[] { "aviso_url", "capital_return_amount", "ex_dividend_date", "taxable_amount" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                schema: "market",
                table: "Distribution",
                keyColumn: "id",
                keyValue: new Guid("aa379a05-ccf6-dd08-09a4-a4fa5a7225fd"),
                columns: new[] { "aviso_url", "capital_return_amount", "ex_dividend_date", "taxable_amount" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                schema: "market",
                table: "Distribution",
                keyColumn: "id",
                keyValue: new Guid("ac5bb4bb-0719-2425-d621-5f823e9da129"),
                columns: new[] { "aviso_url", "capital_return_amount", "ex_dividend_date", "taxable_amount" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                schema: "market",
                table: "Distribution",
                keyColumn: "id",
                keyValue: new Guid("b2b41c08-7711-6ede-c2f0-8936c7a72828"),
                columns: new[] { "aviso_url", "capital_return_amount", "ex_dividend_date", "taxable_amount" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                schema: "market",
                table: "Distribution",
                keyColumn: "id",
                keyValue: new Guid("b34cecad-3540-7d2b-5ea6-8000db9c5a0b"),
                columns: new[] { "aviso_url", "capital_return_amount", "ex_dividend_date", "taxable_amount" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                schema: "market",
                table: "Distribution",
                keyColumn: "id",
                keyValue: new Guid("c36fa6a1-117f-9d25-a947-8d6ee00e30f4"),
                columns: new[] { "aviso_url", "capital_return_amount", "ex_dividend_date", "taxable_amount" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                schema: "market",
                table: "Distribution",
                keyColumn: "id",
                keyValue: new Guid("ca11459c-c62d-01d5-e73d-2398c84b57ec"),
                columns: new[] { "aviso_url", "capital_return_amount", "ex_dividend_date", "taxable_amount" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                schema: "market",
                table: "Distribution",
                keyColumn: "id",
                keyValue: new Guid("cda927f7-2ff8-a628-515d-42ae2fac8182"),
                columns: new[] { "aviso_url", "capital_return_amount", "ex_dividend_date", "taxable_amount" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                schema: "market",
                table: "Distribution",
                keyColumn: "id",
                keyValue: new Guid("cff64c3f-d949-91fc-36e8-7dc331e611ec"),
                columns: new[] { "aviso_url", "capital_return_amount", "ex_dividend_date", "taxable_amount" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                schema: "market",
                table: "Distribution",
                keyColumn: "id",
                keyValue: new Guid("d8e7b864-fd0d-7a22-4a4a-4c46404a2544"),
                columns: new[] { "aviso_url", "capital_return_amount", "ex_dividend_date", "taxable_amount" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                schema: "market",
                table: "Distribution",
                keyColumn: "id",
                keyValue: new Guid("dd6a1e53-d748-bb6a-a5bc-1742bcb8d5fb"),
                columns: new[] { "aviso_url", "capital_return_amount", "ex_dividend_date", "taxable_amount" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                schema: "market",
                table: "Distribution",
                keyColumn: "id",
                keyValue: new Guid("de21e159-a5ad-0c4f-eb7e-8bbb833a9cae"),
                columns: new[] { "aviso_url", "capital_return_amount", "ex_dividend_date", "taxable_amount" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                schema: "market",
                table: "Distribution",
                keyColumn: "id",
                keyValue: new Guid("e1815ccd-c2de-c8d3-0a78-46348657eab0"),
                columns: new[] { "aviso_url", "capital_return_amount", "ex_dividend_date", "taxable_amount" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                schema: "market",
                table: "Distribution",
                keyColumn: "id",
                keyValue: new Guid("e2877d0a-5fa4-1b33-ec8f-1740601a3c3d"),
                columns: new[] { "aviso_url", "capital_return_amount", "ex_dividend_date", "taxable_amount" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                schema: "market",
                table: "Distribution",
                keyColumn: "id",
                keyValue: new Guid("e95c49b8-0da1-0cdb-9698-4ffee01cb629"),
                columns: new[] { "aviso_url", "capital_return_amount", "ex_dividend_date", "taxable_amount" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                schema: "market",
                table: "Distribution",
                keyColumn: "id",
                keyValue: new Guid("ed7b344a-e13d-5c8c-0159-09ccbf87835c"),
                columns: new[] { "aviso_url", "capital_return_amount", "ex_dividend_date", "taxable_amount" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                schema: "market",
                table: "Distribution",
                keyColumn: "id",
                keyValue: new Guid("ef00f014-3a95-0e27-a5fa-76badf3c49c5"),
                columns: new[] { "aviso_url", "capital_return_amount", "ex_dividend_date", "taxable_amount" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                schema: "market",
                table: "Distribution",
                keyColumn: "id",
                keyValue: new Guid("f0d429c6-9ba2-ca53-b252-1162a7e02a99"),
                columns: new[] { "aviso_url", "capital_return_amount", "ex_dividend_date", "taxable_amount" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                schema: "market",
                table: "Distribution",
                keyColumn: "id",
                keyValue: new Guid("f1a4ecac-f36b-e733-a7fc-e5ed6053e6de"),
                columns: new[] { "aviso_url", "capital_return_amount", "ex_dividend_date", "taxable_amount" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                schema: "market",
                table: "Distribution",
                keyColumn: "id",
                keyValue: new Guid("f2af8992-b244-6d43-9610-2f69c170477f"),
                columns: new[] { "aviso_url", "capital_return_amount", "ex_dividend_date", "taxable_amount" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                schema: "market",
                table: "Distribution",
                keyColumn: "id",
                keyValue: new Guid("f5c73975-4c45-b1fe-bb1c-e9e913f488a4"),
                columns: new[] { "aviso_url", "capital_return_amount", "ex_dividend_date", "taxable_amount" },
                values: new object[] { null, null, null, null });

            migrationBuilder.UpdateData(
                schema: "market",
                table: "Distribution",
                keyColumn: "id",
                keyValue: new Guid("ffa46d39-4893-5a33-b8c8-0417d48b73fb"),
                columns: new[] { "aviso_url", "capital_return_amount", "ex_dividend_date", "taxable_amount" },
                values: new object[] { null, null, null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "aviso_url",
                schema: "market",
                table: "Distribution");

            migrationBuilder.DropColumn(
                name: "capital_return_amount",
                schema: "market",
                table: "Distribution");

            migrationBuilder.DropColumn(
                name: "ex_dividend_date",
                schema: "market",
                table: "Distribution");

            migrationBuilder.DropColumn(
                name: "taxable_amount",
                schema: "market",
                table: "Distribution");
        }
    }
}
