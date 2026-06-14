BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611011048_EnrichDistributionTaxBreakdown'
)
BEGIN
    ALTER TABLE [market].[Distribution] ADD [aviso_url] nvarchar(500) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611011048_EnrichDistributionTaxBreakdown'
)
BEGIN
    ALTER TABLE [market].[Distribution] ADD [capital_return_amount] decimal(18,6) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611011048_EnrichDistributionTaxBreakdown'
)
BEGIN
    ALTER TABLE [market].[Distribution] ADD [ex_dividend_date] date NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611011048_EnrichDistributionTaxBreakdown'
)
BEGIN
    ALTER TABLE [market].[Distribution] ADD [taxable_amount] decimal(18,6) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611011048_EnrichDistributionTaxBreakdown'
)
BEGIN
    EXEC(N'UPDATE [market].[Distribution] SET [aviso_url] = NULL, [capital_return_amount] = NULL, [ex_dividend_date] = NULL, [taxable_amount] = NULL
    WHERE [id] = ''00777343-ebe9-68f0-c0d9-1a972125742f'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611011048_EnrichDistributionTaxBreakdown'
)
BEGIN
    EXEC(N'UPDATE [market].[Distribution] SET [aviso_url] = NULL, [capital_return_amount] = NULL, [ex_dividend_date] = NULL, [taxable_amount] = NULL
    WHERE [id] = ''0137f687-5666-c94c-caeb-377119816221'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611011048_EnrichDistributionTaxBreakdown'
)
BEGIN
    EXEC(N'UPDATE [market].[Distribution] SET [aviso_url] = NULL, [capital_return_amount] = NULL, [ex_dividend_date] = NULL, [taxable_amount] = NULL
    WHERE [id] = ''0712aff6-cf0f-55dd-a6dc-090e83c5233a'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611011048_EnrichDistributionTaxBreakdown'
)
BEGIN
    EXEC(N'UPDATE [market].[Distribution] SET [aviso_url] = NULL, [capital_return_amount] = NULL, [ex_dividend_date] = NULL, [taxable_amount] = NULL
    WHERE [id] = ''08c47232-2945-58d3-e018-0c687cf9987d'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611011048_EnrichDistributionTaxBreakdown'
)
BEGIN
    EXEC(N'UPDATE [market].[Distribution] SET [aviso_url] = NULL, [capital_return_amount] = NULL, [ex_dividend_date] = NULL, [taxable_amount] = NULL
    WHERE [id] = ''0bbbb154-b518-66f0-6637-9b15850397a8'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611011048_EnrichDistributionTaxBreakdown'
)
BEGIN
    EXEC(N'UPDATE [market].[Distribution] SET [aviso_url] = NULL, [capital_return_amount] = NULL, [ex_dividend_date] = NULL, [taxable_amount] = NULL
    WHERE [id] = ''12754f3b-2021-99c6-dc06-a413948bc0fd'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611011048_EnrichDistributionTaxBreakdown'
)
BEGIN
    EXEC(N'UPDATE [market].[Distribution] SET [aviso_url] = NULL, [capital_return_amount] = NULL, [ex_dividend_date] = NULL, [taxable_amount] = NULL
    WHERE [id] = ''134f77eb-4afc-4807-b139-36a64791d83f'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611011048_EnrichDistributionTaxBreakdown'
)
BEGIN
    EXEC(N'UPDATE [market].[Distribution] SET [aviso_url] = NULL, [capital_return_amount] = NULL, [ex_dividend_date] = NULL, [taxable_amount] = NULL
    WHERE [id] = ''1864e970-626f-abaa-0c24-77b72ff123bd'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611011048_EnrichDistributionTaxBreakdown'
)
BEGIN
    EXEC(N'UPDATE [market].[Distribution] SET [aviso_url] = NULL, [capital_return_amount] = NULL, [ex_dividend_date] = NULL, [taxable_amount] = NULL
    WHERE [id] = ''1c20f71e-193b-f476-88b7-b056789186a4'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611011048_EnrichDistributionTaxBreakdown'
)
BEGIN
    EXEC(N'UPDATE [market].[Distribution] SET [aviso_url] = NULL, [capital_return_amount] = NULL, [ex_dividend_date] = NULL, [taxable_amount] = NULL
    WHERE [id] = ''22ab0fa4-a568-68ec-cf8f-04d474d88988'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611011048_EnrichDistributionTaxBreakdown'
)
BEGIN
    EXEC(N'UPDATE [market].[Distribution] SET [aviso_url] = NULL, [capital_return_amount] = NULL, [ex_dividend_date] = NULL, [taxable_amount] = NULL
    WHERE [id] = ''24260c3b-ef78-0968-6f3c-efe375b565e3'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611011048_EnrichDistributionTaxBreakdown'
)
BEGIN
    EXEC(N'UPDATE [market].[Distribution] SET [aviso_url] = NULL, [capital_return_amount] = NULL, [ex_dividend_date] = NULL, [taxable_amount] = NULL
    WHERE [id] = ''249e2535-408e-e0db-90e4-602ea74bb40d'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611011048_EnrichDistributionTaxBreakdown'
)
BEGIN
    EXEC(N'UPDATE [market].[Distribution] SET [aviso_url] = NULL, [capital_return_amount] = NULL, [ex_dividend_date] = NULL, [taxable_amount] = NULL
    WHERE [id] = ''2c2c6ef6-29bb-468b-4a60-b8f1a5aad69b'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611011048_EnrichDistributionTaxBreakdown'
)
BEGIN
    EXEC(N'UPDATE [market].[Distribution] SET [aviso_url] = NULL, [capital_return_amount] = NULL, [ex_dividend_date] = NULL, [taxable_amount] = NULL
    WHERE [id] = ''2ca1aa7d-0958-4583-040a-646cf586ad96'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611011048_EnrichDistributionTaxBreakdown'
)
BEGIN
    EXEC(N'UPDATE [market].[Distribution] SET [aviso_url] = NULL, [capital_return_amount] = NULL, [ex_dividend_date] = NULL, [taxable_amount] = NULL
    WHERE [id] = ''2d5fcd74-962e-c0d6-f1f6-b8b25a0009b9'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611011048_EnrichDistributionTaxBreakdown'
)
BEGIN
    EXEC(N'UPDATE [market].[Distribution] SET [aviso_url] = NULL, [capital_return_amount] = NULL, [ex_dividend_date] = NULL, [taxable_amount] = NULL
    WHERE [id] = ''3641773c-175e-3176-5af6-d3c9cc43fee0'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611011048_EnrichDistributionTaxBreakdown'
)
BEGIN
    EXEC(N'UPDATE [market].[Distribution] SET [aviso_url] = NULL, [capital_return_amount] = NULL, [ex_dividend_date] = NULL, [taxable_amount] = NULL
    WHERE [id] = ''3ad4a8a2-8e06-5923-aedd-25a49939ea84'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611011048_EnrichDistributionTaxBreakdown'
)
BEGIN
    EXEC(N'UPDATE [market].[Distribution] SET [aviso_url] = NULL, [capital_return_amount] = NULL, [ex_dividend_date] = NULL, [taxable_amount] = NULL
    WHERE [id] = ''3ccdc1eb-c781-6972-f073-fdd788cdb8b4'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611011048_EnrichDistributionTaxBreakdown'
)
BEGIN
    EXEC(N'UPDATE [market].[Distribution] SET [aviso_url] = NULL, [capital_return_amount] = NULL, [ex_dividend_date] = NULL, [taxable_amount] = NULL
    WHERE [id] = ''3cd135c0-6ef9-07d2-c58c-9f23c8f6d969'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611011048_EnrichDistributionTaxBreakdown'
)
BEGIN
    EXEC(N'UPDATE [market].[Distribution] SET [aviso_url] = NULL, [capital_return_amount] = NULL, [ex_dividend_date] = NULL, [taxable_amount] = NULL
    WHERE [id] = ''3e264b5f-5643-58fa-be69-21d0be470954'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611011048_EnrichDistributionTaxBreakdown'
)
BEGIN
    EXEC(N'UPDATE [market].[Distribution] SET [aviso_url] = NULL, [capital_return_amount] = NULL, [ex_dividend_date] = NULL, [taxable_amount] = NULL
    WHERE [id] = ''3e2dbc4b-6422-5059-49c3-9238a184ee2e'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611011048_EnrichDistributionTaxBreakdown'
)
BEGIN
    EXEC(N'UPDATE [market].[Distribution] SET [aviso_url] = NULL, [capital_return_amount] = NULL, [ex_dividend_date] = NULL, [taxable_amount] = NULL
    WHERE [id] = ''3ea58762-31cb-1687-2b59-931ba80860d8'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611011048_EnrichDistributionTaxBreakdown'
)
BEGIN
    EXEC(N'UPDATE [market].[Distribution] SET [aviso_url] = NULL, [capital_return_amount] = NULL, [ex_dividend_date] = NULL, [taxable_amount] = NULL
    WHERE [id] = ''3fce78aa-5887-0f04-a1f3-ca1e7fcb4803'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611011048_EnrichDistributionTaxBreakdown'
)
BEGIN
    EXEC(N'UPDATE [market].[Distribution] SET [aviso_url] = NULL, [capital_return_amount] = NULL, [ex_dividend_date] = NULL, [taxable_amount] = NULL
    WHERE [id] = ''49a7be56-9ac0-c735-7674-f1a584e26f70'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611011048_EnrichDistributionTaxBreakdown'
)
BEGIN
    EXEC(N'UPDATE [market].[Distribution] SET [aviso_url] = NULL, [capital_return_amount] = NULL, [ex_dividend_date] = NULL, [taxable_amount] = NULL
    WHERE [id] = ''4f56248a-fe67-33bf-4033-6df6a608ca2b'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611011048_EnrichDistributionTaxBreakdown'
)
BEGIN
    EXEC(N'UPDATE [market].[Distribution] SET [aviso_url] = NULL, [capital_return_amount] = NULL, [ex_dividend_date] = NULL, [taxable_amount] = NULL
    WHERE [id] = ''520331f0-8890-6215-eaa3-5fbca55ed140'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611011048_EnrichDistributionTaxBreakdown'
)
BEGIN
    EXEC(N'UPDATE [market].[Distribution] SET [aviso_url] = NULL, [capital_return_amount] = NULL, [ex_dividend_date] = NULL, [taxable_amount] = NULL
    WHERE [id] = ''5233cae1-9172-0ef6-7c00-6ad251574aaf'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611011048_EnrichDistributionTaxBreakdown'
)
BEGIN
    EXEC(N'UPDATE [market].[Distribution] SET [aviso_url] = NULL, [capital_return_amount] = NULL, [ex_dividend_date] = NULL, [taxable_amount] = NULL
    WHERE [id] = ''5278bfd8-b8a0-ea29-bcf7-0ba402d7e924'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611011048_EnrichDistributionTaxBreakdown'
)
BEGIN
    EXEC(N'UPDATE [market].[Distribution] SET [aviso_url] = NULL, [capital_return_amount] = NULL, [ex_dividend_date] = NULL, [taxable_amount] = NULL
    WHERE [id] = ''52cae36d-59cc-bc56-cb33-cb5429192916'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611011048_EnrichDistributionTaxBreakdown'
)
BEGIN
    EXEC(N'UPDATE [market].[Distribution] SET [aviso_url] = NULL, [capital_return_amount] = NULL, [ex_dividend_date] = NULL, [taxable_amount] = NULL
    WHERE [id] = ''535537f5-bb64-3772-486d-be8ef4b150ac'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611011048_EnrichDistributionTaxBreakdown'
)
BEGIN
    EXEC(N'UPDATE [market].[Distribution] SET [aviso_url] = NULL, [capital_return_amount] = NULL, [ex_dividend_date] = NULL, [taxable_amount] = NULL
    WHERE [id] = ''546667b5-d9b5-8932-82fc-c2b4c7928d98'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611011048_EnrichDistributionTaxBreakdown'
)
BEGIN
    EXEC(N'UPDATE [market].[Distribution] SET [aviso_url] = NULL, [capital_return_amount] = NULL, [ex_dividend_date] = NULL, [taxable_amount] = NULL
    WHERE [id] = ''5babaaff-7083-0205-c580-61ebd295fd10'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611011048_EnrichDistributionTaxBreakdown'
)
BEGIN
    EXEC(N'UPDATE [market].[Distribution] SET [aviso_url] = NULL, [capital_return_amount] = NULL, [ex_dividend_date] = NULL, [taxable_amount] = NULL
    WHERE [id] = ''627ced36-69ac-91e8-7427-2824c2ef8221'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611011048_EnrichDistributionTaxBreakdown'
)
BEGIN
    EXEC(N'UPDATE [market].[Distribution] SET [aviso_url] = NULL, [capital_return_amount] = NULL, [ex_dividend_date] = NULL, [taxable_amount] = NULL
    WHERE [id] = ''62b96edf-b872-2f4b-0799-674cb585264a'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611011048_EnrichDistributionTaxBreakdown'
)
BEGIN
    EXEC(N'UPDATE [market].[Distribution] SET [aviso_url] = NULL, [capital_return_amount] = NULL, [ex_dividend_date] = NULL, [taxable_amount] = NULL
    WHERE [id] = ''662c2c1c-e08a-a6df-2c83-6331d8714653'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611011048_EnrichDistributionTaxBreakdown'
)
BEGIN
    EXEC(N'UPDATE [market].[Distribution] SET [aviso_url] = NULL, [capital_return_amount] = NULL, [ex_dividend_date] = NULL, [taxable_amount] = NULL
    WHERE [id] = ''6c96a62e-17ed-a8a8-6f86-1f4841b22da3'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611011048_EnrichDistributionTaxBreakdown'
)
BEGIN
    EXEC(N'UPDATE [market].[Distribution] SET [aviso_url] = NULL, [capital_return_amount] = NULL, [ex_dividend_date] = NULL, [taxable_amount] = NULL
    WHERE [id] = ''73cf3c5c-d05d-bdc0-e25c-15d18c654802'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611011048_EnrichDistributionTaxBreakdown'
)
BEGIN
    EXEC(N'UPDATE [market].[Distribution] SET [aviso_url] = NULL, [capital_return_amount] = NULL, [ex_dividend_date] = NULL, [taxable_amount] = NULL
    WHERE [id] = ''7bfffaff-b5a1-0869-1ba2-a5b573b39dd3'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611011048_EnrichDistributionTaxBreakdown'
)
BEGIN
    EXEC(N'UPDATE [market].[Distribution] SET [aviso_url] = NULL, [capital_return_amount] = NULL, [ex_dividend_date] = NULL, [taxable_amount] = NULL
    WHERE [id] = ''84dbaa5b-e460-a15e-2431-442e56d44f0f'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611011048_EnrichDistributionTaxBreakdown'
)
BEGIN
    EXEC(N'UPDATE [market].[Distribution] SET [aviso_url] = NULL, [capital_return_amount] = NULL, [ex_dividend_date] = NULL, [taxable_amount] = NULL
    WHERE [id] = ''85a4ed94-b620-298c-c457-b124599fb3b7'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611011048_EnrichDistributionTaxBreakdown'
)
BEGIN
    EXEC(N'UPDATE [market].[Distribution] SET [aviso_url] = NULL, [capital_return_amount] = NULL, [ex_dividend_date] = NULL, [taxable_amount] = NULL
    WHERE [id] = ''8671a635-0d04-2660-ae19-dac73d60fd1b'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611011048_EnrichDistributionTaxBreakdown'
)
BEGIN
    EXEC(N'UPDATE [market].[Distribution] SET [aviso_url] = NULL, [capital_return_amount] = NULL, [ex_dividend_date] = NULL, [taxable_amount] = NULL
    WHERE [id] = ''900c466e-b739-8bef-221e-cff787935af6'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611011048_EnrichDistributionTaxBreakdown'
)
BEGIN
    EXEC(N'UPDATE [market].[Distribution] SET [aviso_url] = NULL, [capital_return_amount] = NULL, [ex_dividend_date] = NULL, [taxable_amount] = NULL
    WHERE [id] = ''908677ac-2c56-9fbb-dae2-b610f6c491be'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611011048_EnrichDistributionTaxBreakdown'
)
BEGIN
    EXEC(N'UPDATE [market].[Distribution] SET [aviso_url] = NULL, [capital_return_amount] = NULL, [ex_dividend_date] = NULL, [taxable_amount] = NULL
    WHERE [id] = ''a2e5c597-0238-9607-c8de-c3df6d9e4f65'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611011048_EnrichDistributionTaxBreakdown'
)
BEGIN
    EXEC(N'UPDATE [market].[Distribution] SET [aviso_url] = NULL, [capital_return_amount] = NULL, [ex_dividend_date] = NULL, [taxable_amount] = NULL
    WHERE [id] = ''a84e7f7c-9852-61d3-fb7d-9dc07eeda3ce'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611011048_EnrichDistributionTaxBreakdown'
)
BEGIN
    EXEC(N'UPDATE [market].[Distribution] SET [aviso_url] = NULL, [capital_return_amount] = NULL, [ex_dividend_date] = NULL, [taxable_amount] = NULL
    WHERE [id] = ''a9944cc5-75a9-4ccb-e53c-a64c9687c104'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611011048_EnrichDistributionTaxBreakdown'
)
BEGIN
    EXEC(N'UPDATE [market].[Distribution] SET [aviso_url] = NULL, [capital_return_amount] = NULL, [ex_dividend_date] = NULL, [taxable_amount] = NULL
    WHERE [id] = ''aa044830-cfbd-bcf0-e79f-89c7287e8912'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611011048_EnrichDistributionTaxBreakdown'
)
BEGIN
    EXEC(N'UPDATE [market].[Distribution] SET [aviso_url] = NULL, [capital_return_amount] = NULL, [ex_dividend_date] = NULL, [taxable_amount] = NULL
    WHERE [id] = ''aa379a05-ccf6-dd08-09a4-a4fa5a7225fd'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611011048_EnrichDistributionTaxBreakdown'
)
BEGIN
    EXEC(N'UPDATE [market].[Distribution] SET [aviso_url] = NULL, [capital_return_amount] = NULL, [ex_dividend_date] = NULL, [taxable_amount] = NULL
    WHERE [id] = ''ac5bb4bb-0719-2425-d621-5f823e9da129'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611011048_EnrichDistributionTaxBreakdown'
)
BEGIN
    EXEC(N'UPDATE [market].[Distribution] SET [aviso_url] = NULL, [capital_return_amount] = NULL, [ex_dividend_date] = NULL, [taxable_amount] = NULL
    WHERE [id] = ''b2b41c08-7711-6ede-c2f0-8936c7a72828'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611011048_EnrichDistributionTaxBreakdown'
)
BEGIN
    EXEC(N'UPDATE [market].[Distribution] SET [aviso_url] = NULL, [capital_return_amount] = NULL, [ex_dividend_date] = NULL, [taxable_amount] = NULL
    WHERE [id] = ''b34cecad-3540-7d2b-5ea6-8000db9c5a0b'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611011048_EnrichDistributionTaxBreakdown'
)
BEGIN
    EXEC(N'UPDATE [market].[Distribution] SET [aviso_url] = NULL, [capital_return_amount] = NULL, [ex_dividend_date] = NULL, [taxable_amount] = NULL
    WHERE [id] = ''c36fa6a1-117f-9d25-a947-8d6ee00e30f4'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611011048_EnrichDistributionTaxBreakdown'
)
BEGIN
    EXEC(N'UPDATE [market].[Distribution] SET [aviso_url] = NULL, [capital_return_amount] = NULL, [ex_dividend_date] = NULL, [taxable_amount] = NULL
    WHERE [id] = ''ca11459c-c62d-01d5-e73d-2398c84b57ec'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611011048_EnrichDistributionTaxBreakdown'
)
BEGIN
    EXEC(N'UPDATE [market].[Distribution] SET [aviso_url] = NULL, [capital_return_amount] = NULL, [ex_dividend_date] = NULL, [taxable_amount] = NULL
    WHERE [id] = ''cda927f7-2ff8-a628-515d-42ae2fac8182'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611011048_EnrichDistributionTaxBreakdown'
)
BEGIN
    EXEC(N'UPDATE [market].[Distribution] SET [aviso_url] = NULL, [capital_return_amount] = NULL, [ex_dividend_date] = NULL, [taxable_amount] = NULL
    WHERE [id] = ''cff64c3f-d949-91fc-36e8-7dc331e611ec'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611011048_EnrichDistributionTaxBreakdown'
)
BEGIN
    EXEC(N'UPDATE [market].[Distribution] SET [aviso_url] = NULL, [capital_return_amount] = NULL, [ex_dividend_date] = NULL, [taxable_amount] = NULL
    WHERE [id] = ''d8e7b864-fd0d-7a22-4a4a-4c46404a2544'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611011048_EnrichDistributionTaxBreakdown'
)
BEGIN
    EXEC(N'UPDATE [market].[Distribution] SET [aviso_url] = NULL, [capital_return_amount] = NULL, [ex_dividend_date] = NULL, [taxable_amount] = NULL
    WHERE [id] = ''dd6a1e53-d748-bb6a-a5bc-1742bcb8d5fb'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611011048_EnrichDistributionTaxBreakdown'
)
BEGIN
    EXEC(N'UPDATE [market].[Distribution] SET [aviso_url] = NULL, [capital_return_amount] = NULL, [ex_dividend_date] = NULL, [taxable_amount] = NULL
    WHERE [id] = ''de21e159-a5ad-0c4f-eb7e-8bbb833a9cae'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611011048_EnrichDistributionTaxBreakdown'
)
BEGIN
    EXEC(N'UPDATE [market].[Distribution] SET [aviso_url] = NULL, [capital_return_amount] = NULL, [ex_dividend_date] = NULL, [taxable_amount] = NULL
    WHERE [id] = ''e1815ccd-c2de-c8d3-0a78-46348657eab0'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611011048_EnrichDistributionTaxBreakdown'
)
BEGIN
    EXEC(N'UPDATE [market].[Distribution] SET [aviso_url] = NULL, [capital_return_amount] = NULL, [ex_dividend_date] = NULL, [taxable_amount] = NULL
    WHERE [id] = ''e2877d0a-5fa4-1b33-ec8f-1740601a3c3d'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611011048_EnrichDistributionTaxBreakdown'
)
BEGIN
    EXEC(N'UPDATE [market].[Distribution] SET [aviso_url] = NULL, [capital_return_amount] = NULL, [ex_dividend_date] = NULL, [taxable_amount] = NULL
    WHERE [id] = ''e95c49b8-0da1-0cdb-9698-4ffee01cb629'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611011048_EnrichDistributionTaxBreakdown'
)
BEGIN
    EXEC(N'UPDATE [market].[Distribution] SET [aviso_url] = NULL, [capital_return_amount] = NULL, [ex_dividend_date] = NULL, [taxable_amount] = NULL
    WHERE [id] = ''ed7b344a-e13d-5c8c-0159-09ccbf87835c'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611011048_EnrichDistributionTaxBreakdown'
)
BEGIN
    EXEC(N'UPDATE [market].[Distribution] SET [aviso_url] = NULL, [capital_return_amount] = NULL, [ex_dividend_date] = NULL, [taxable_amount] = NULL
    WHERE [id] = ''ef00f014-3a95-0e27-a5fa-76badf3c49c5'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611011048_EnrichDistributionTaxBreakdown'
)
BEGIN
    EXEC(N'UPDATE [market].[Distribution] SET [aviso_url] = NULL, [capital_return_amount] = NULL, [ex_dividend_date] = NULL, [taxable_amount] = NULL
    WHERE [id] = ''f0d429c6-9ba2-ca53-b252-1162a7e02a99'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611011048_EnrichDistributionTaxBreakdown'
)
BEGIN
    EXEC(N'UPDATE [market].[Distribution] SET [aviso_url] = NULL, [capital_return_amount] = NULL, [ex_dividend_date] = NULL, [taxable_amount] = NULL
    WHERE [id] = ''f1a4ecac-f36b-e733-a7fc-e5ed6053e6de'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611011048_EnrichDistributionTaxBreakdown'
)
BEGIN
    EXEC(N'UPDATE [market].[Distribution] SET [aviso_url] = NULL, [capital_return_amount] = NULL, [ex_dividend_date] = NULL, [taxable_amount] = NULL
    WHERE [id] = ''f2af8992-b244-6d43-9610-2f69c170477f'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611011048_EnrichDistributionTaxBreakdown'
)
BEGIN
    EXEC(N'UPDATE [market].[Distribution] SET [aviso_url] = NULL, [capital_return_amount] = NULL, [ex_dividend_date] = NULL, [taxable_amount] = NULL
    WHERE [id] = ''f5c73975-4c45-b1fe-bb1c-e9e913f488a4'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611011048_EnrichDistributionTaxBreakdown'
)
BEGIN
    EXEC(N'UPDATE [market].[Distribution] SET [aviso_url] = NULL, [capital_return_amount] = NULL, [ex_dividend_date] = NULL, [taxable_amount] = NULL
    WHERE [id] = ''ffa46d39-4893-5a33-b8c8-0417d48b73fb'';
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611011048_EnrichDistributionTaxBreakdown'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260611011048_EnrichDistributionTaxBreakdown', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611161638_AddNewsArticleSlug'
)
BEGIN
    ALTER TABLE [news].[NewsArticle] ADD [slug] nvarchar(256) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611161638_AddNewsArticleSlug'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_NewsArticle_Slug] ON [news].[NewsArticle] ([slug]) WHERE [slug] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260611161638_AddNewsArticleSlug'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260611161638_AddNewsArticleSlug', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612071926_AddSitemapIndexes'
)
BEGIN
    EXEC(N'CREATE INDEX [IX_NewsArticle_Sitemap] ON [news].[NewsArticle] ([published_at] DESC) WHERE [status] = ''Processed'' AND [deleted_at] IS NULL AND [slug] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612071926_AddSitemapIndexes'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260612071926_AddSitemapIndexes', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612225925_AddCetes28dToOperationalConfig'
)
BEGIN
    ALTER TABLE [ops].[OperationalConfig] ADD [cetes_28d_rate] decimal(10,4) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612225925_AddCetes28dToOperationalConfig'
)
BEGIN
    ALTER TABLE [ops].[OperationalConfig] ADD [cetes_28d_rate_updated_at] datetimeoffset NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612225925_AddCetes28dToOperationalConfig'
)
BEGIN
    EXEC(N'UPDATE [ops].[OperationalConfig] SET [cetes_28d_rate] = NULL, [cetes_28d_rate_updated_at] = NULL
    WHERE [id] = 1;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260612225925_AddCetes28dToOperationalConfig'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260612225925_AddCetes28dToOperationalConfig', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260613000042_AddBenchmarkFibras'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'created_at', N'currency', N'description', N'full_name', N'investor_url', N'market', N'name_variants', N'reports_url', N'sector', N'short_name', N'site_url', N'state', N'ticker', N'yahoo_ticker') AND [object_id] = OBJECT_ID(N'[catalog].[Fibra]'))
        SET IDENTITY_INSERT [catalog].[Fibra] ON;
    EXEC(N'INSERT INTO [catalog].[Fibra] ([Id], [created_at], [currency], [description], [full_name], [investor_url], [market], [name_variants], [reports_url], [sector], [short_name], [site_url], [state], [ticker], [yahoo_ticker])
    VALUES (''c874e0b2-dac0-2b26-da97-48e85de1b5a4'', ''2026-01-01T00:00:00.0000000+00:00'', N''MXN'', NULL, N''IPC BMV'', NULL, N''BMV'', ''[]'', NULL, N''Índice'', N''IPC'', NULL, N''Inactive'', N''^MXX'', N''^MXX''),
    (''d155fd8f-1d3d-33e7-6480-56768bc708e6'', ''2026-01-01T00:00:00.0000000+00:00'', N''USD'', NULL, N''S&P 500'', NULL, N''NYSE'', ''[]'', NULL, N''Índice'', N''S&P 500'', NULL, N''Inactive'', N''^GSPC'', N''^GSPC'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'created_at', N'currency', N'description', N'full_name', N'investor_url', N'market', N'name_variants', N'reports_url', N'sector', N'short_name', N'site_url', N'state', N'ticker', N'yahoo_ticker') AND [object_id] = OBJECT_ID(N'[catalog].[Fibra]'))
        SET IDENTITY_INSERT [catalog].[Fibra] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260613000042_AddBenchmarkFibras'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260613000042_AddBenchmarkFibras', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260613003417_AddTiie28dAndInpcMonthly'
)
BEGIN
    ALTER TABLE [ops].[OperationalConfig] ADD [tiie_28d_rate] decimal(10,4) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260613003417_AddTiie28dAndInpcMonthly'
)
BEGIN
    ALTER TABLE [ops].[OperationalConfig] ADD [tiie_28d_rate_updated_at] datetimeoffset NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260613003417_AddTiie28dAndInpcMonthly'
)
BEGIN
    CREATE TABLE [ops].[InpcMonthly] (
        [periodo] date NOT NULL,
        [inpc_index] decimal(10,4) NOT NULL,
        [captured_at] datetimeoffset NOT NULL,
        CONSTRAINT [PK_InpcMonthly] PRIMARY KEY ([periodo])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260613003417_AddTiie28dAndInpcMonthly'
)
BEGIN
    EXEC(N'UPDATE [ops].[OperationalConfig] SET [tiie_28d_rate] = NULL, [tiie_28d_rate_updated_at] = NULL
    WHERE [id] = 1;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260613003417_AddTiie28dAndInpcMonthly'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260613003417_AddTiie28dAndInpcMonthly', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260613222344_AddSeoModule'
)
BEGIN
    IF SCHEMA_ID(N'seo') IS NULL EXEC(N'CREATE SCHEMA [seo];');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260613222344_AddSeoModule'
)
BEGIN
    CREATE TABLE [seo].[SeoMetadata] (
        [id] uniqueidentifier NOT NULL,
        [page_type] nvarchar(16) NOT NULL,
        [entity_key] nvarchar(256) NOT NULL,
        [title] nvarchar(120) NOT NULL,
        [meta_description] nvarchar(160) NOT NULL,
        [canonical_path] nvarchar(256) NOT NULL,
        [og_title] nvarchar(120) NOT NULL,
        [og_description] nvarchar(160) NOT NULL,
        [og_type] nvarchar(32) NOT NULL,
        [og_image_url] nvarchar(512) NOT NULL,
        [og_locale] nvarchar(16) NOT NULL,
        [twitter_card] nvarchar(32) NOT NULL,
        [robots_directives] nvarchar(256) NOT NULL,
        [json_ld] nvarchar(max) NULL,
        [is_active] bit NOT NULL DEFAULT CAST(1 AS bit),
        [updated_at] datetimeoffset NOT NULL,
        [updated_by] nvarchar(256) NOT NULL,
        [title_is_overridden] bit NOT NULL DEFAULT CAST(0 AS bit),
        [meta_description_is_overridden] bit NOT NULL DEFAULT CAST(0 AS bit),
        [canonical_path_is_overridden] bit NOT NULL DEFAULT CAST(0 AS bit),
        [og_title_is_overridden] bit NOT NULL DEFAULT CAST(0 AS bit),
        [og_description_is_overridden] bit NOT NULL DEFAULT CAST(0 AS bit),
        [og_type_is_overridden] bit NOT NULL DEFAULT CAST(0 AS bit),
        [og_image_url_is_overridden] bit NOT NULL DEFAULT CAST(0 AS bit),
        [og_locale_is_overridden] bit NOT NULL DEFAULT CAST(0 AS bit),
        [twitter_card_is_overridden] bit NOT NULL DEFAULT CAST(0 AS bit),
        [robots_directives_is_overridden] bit NOT NULL DEFAULT CAST(0 AS bit),
        [json_ld_is_overridden] bit NOT NULL DEFAULT CAST(0 AS bit),
        CONSTRAINT [PK_SeoMetadata] PRIMARY KEY ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260613222344_AddSeoModule'
)
BEGIN
    CREATE UNIQUE INDEX [UX_SeoMetadata_PageType_EntityKey] ON [seo].[SeoMetadata] ([page_type], [entity_key]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260613222344_AddSeoModule'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260613222344_AddSeoModule', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614011501_AddSeoFaq'
)
BEGIN
    CREATE TABLE [seo].[FaqItem] (
        [id] uniqueidentifier NOT NULL,
        [page_type] nvarchar(16) NOT NULL,
        [entity_key] nvarchar(256) NOT NULL,
        [question] nvarchar(256) NOT NULL,
        [answer] nvarchar(max) NOT NULL,
        [display_order] int NOT NULL,
        [is_active] bit NOT NULL DEFAULT CAST(1 AS bit),
        [updated_at] datetimeoffset NOT NULL,
        [updated_by] nvarchar(256) NOT NULL,
        CONSTRAINT [PK_FaqItem] PRIMARY KEY ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614011501_AddSeoFaq'
)
BEGIN
    CREATE INDEX [IX_FaqItem_PageType_EntityKey_Order] ON [seo].[FaqItem] ([page_type], [entity_key], [display_order]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614011501_AddSeoFaq'
)
BEGIN
    CREATE UNIQUE INDEX [UX_FaqItem_PageType_EntityKey_Question] ON [seo].[FaqItem] ([page_type], [entity_key], [question]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614011501_AddSeoFaq'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260614011501_AddSeoFaq', N'10.0.8');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614023840_AddUrlRedirects'
)
BEGIN
    CREATE TABLE [seo].[UrlRedirect] (
        [id] uniqueidentifier NOT NULL,
        [from_path] nvarchar(256) NOT NULL,
        [to_path] nvarchar(256) NOT NULL,
        [status_code] int NOT NULL,
        [is_active] bit NOT NULL DEFAULT CAST(1 AS bit),
        [notes] nvarchar(max) NULL,
        [created_at] datetimeoffset NOT NULL,
        [created_by] nvarchar(256) NOT NULL,
        [updated_at] datetimeoffset NOT NULL,
        [updated_by] nvarchar(256) NOT NULL,
        CONSTRAINT [PK_UrlRedirect] PRIMARY KEY ([id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614023840_AddUrlRedirects'
)
BEGIN
    CREATE UNIQUE INDEX [UX_UrlRedirect_FromPath] ON [seo].[UrlRedirect] ([from_path]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614023840_AddUrlRedirects'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'id', N'from_path', N'to_path', N'status_code', N'is_active', N'notes', N'created_at', N'created_by', N'updated_at', N'updated_by') AND [object_id] = OBJECT_ID(N'[seo].[UrlRedirect]'))
        SET IDENTITY_INSERT [seo].[UrlRedirect] ON;
    EXEC(N'INSERT INTO [seo].[UrlRedirect] ([id], [from_path], [to_path], [status_code], [is_active], [notes], [created_at], [created_by], [updated_at], [updated_by])
    VALUES (''2d1bbf9b-5a95-4a7f-9d87-39d2efc2b1a1'', N''/blog'', N''/noticias'', 301, CAST(1 AS bit), N''Migrado desde redirect hardcodeado'', ''2026-06-14T00:00:00.0000000+00:00'', N''system'', ''2026-06-14T00:00:00.0000000+00:00'', N''system''),
    (''1bcb1f7d-ef1b-4a1f-8d51-15df4f4f0cc2'', N''/catalogo'', N''/fibras'', 301, CAST(1 AS bit), N''Migrado desde redirect hardcodeado'', ''2026-06-14T00:00:00.0000000+00:00'', N''system'', ''2026-06-14T00:00:00.0000000+00:00'', N''system''),
    (''f3f9f7b2-5e89-4c34-bf17-9f7d2d27f1a3'', N''/aviso-de-privacidad'', N''/privacidad'', 301, CAST(1 AS bit), N''Migrado desde redirect hardcodeado'', ''2026-06-14T00:00:00.0000000+00:00'', N''system'', ''2026-06-14T00:00:00.0000000+00:00'', N''system'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'id', N'from_path', N'to_path', N'status_code', N'is_active', N'notes', N'created_at', N'created_by', N'updated_at', N'updated_by') AND [object_id] = OBJECT_ID(N'[seo].[UrlRedirect]'))
        SET IDENTITY_INSERT [seo].[UrlRedirect] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614023840_AddUrlRedirects'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260614023840_AddUrlRedirects', N'10.0.8');
END;

COMMIT;
GO

