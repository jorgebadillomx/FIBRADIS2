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

