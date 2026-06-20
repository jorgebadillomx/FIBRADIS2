BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260619232932_UpdateContactEmailToFibrasInmobiliarias'
)
BEGIN
    EXEC(N'UPDATE [ops].[OperationalConfig] SET [contact_email] = N''contacto@fibrasinmobiliarias.com''
    WHERE [id] = 1;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260619232932_UpdateContactEmailToFibrasInmobiliarias'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260619232932_UpdateContactEmailToFibrasInmobiliarias', N'10.0.8');
END;

COMMIT;
GO

