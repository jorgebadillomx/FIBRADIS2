BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614061543_AddOrganizationSameAsJson'
)
BEGIN
    ALTER TABLE [ops].[OperationalConfig] ADD [organization_same_as_json] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614061543_AddOrganizationSameAsJson'
)
BEGIN
    EXEC(N'UPDATE [ops].[OperationalConfig] SET [organization_same_as_json] = NULL
    WHERE [id] = 1;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614061543_AddOrganizationSameAsJson'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260614061543_AddOrganizationSameAsJson', N'10.0.8');
END;

COMMIT;
GO

