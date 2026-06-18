BEGIN TRANSACTION;
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'created_at', N'currency', N'description', N'full_name', N'investor_url', N'market', N'name_variants', N'reports_url', N'sector', N'short_name', N'site_url', N'state', N'ticker', N'yahoo_ticker') AND [object_id] = OBJECT_ID(N'[catalog].[Fibra]'))
    SET IDENTITY_INSERT [catalog].[Fibra] ON;
INSERT INTO [catalog].[Fibra] ([Id], [created_at], [currency], [description], [full_name], [investor_url], [market], [name_variants], [reports_url], [sector], [short_name], [site_url], [state], [ticker], [yahoo_ticker])
VALUES ('28717c15-5e9a-367f-7079-4eb5013c9369', '2026-01-01T00:00:00.0000000+00:00', N'MXN', 'Fibra EXI es un fideicomiso de inversión en energía e infraestructura (Fibra E) especializado en concesiones de autopistas de peaje. Su portafolio comprende más de 400 km de carreteras concesionadas en el centro del país, incluyendo la autopista Salamanca-León en Guanajuato. Cotiza en la BMV bajo el ticker FEXI21.', N'Fibra EXI', N'https://fibraexi.com/es/inversionistas/', N'BMV', '["Fibra EXI","FEXI","FEXI21"]', N'http://www.economatica.mx/FEXI/REPORTES%20TRIMESTRALES/', N'Infraestructura', N'Fibra EXI', N'https://fibraexi.com/es/', N'Active', N'FEXI21', N'FEXI21.MX'),
('53273c63-873a-3788-88ad-dca50bffca6e', '2026-01-01T00:00:00.0000000+00:00', N'MXN', 'AgroFibra es el primer fideicomiso de inversión en bienes raíces en México especializado en el sector agroalimentario. Su portafolio incluye propiedades agrícolas arrendadas a productores y empresas de la cadena agroalimentaria en diversas regiones del país. Cotiza en BIVA bajo el ticker AGRO22.', N'AgroFibra', N'https://agrofibra.com/inversionistas/', N'BIVA', '["AgroFibra","Fibra AGRO","AGRO","AGRO22"]', N'http://www.economatica.mx/AGRO/REPORTES%20TRIMESTRALES%20/', N'Agroalimentario', N'AgroFibra', N'https://agrofibra.com/', N'Active', N'AGRO22', N'AGRO22.MX');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'created_at', N'currency', N'description', N'full_name', N'investor_url', N'market', N'name_variants', N'reports_url', N'sector', N'short_name', N'site_url', N'state', N'ticker', N'yahoo_ticker') AND [object_id] = OBJECT_ID(N'[catalog].[Fibra]'))
    SET IDENTITY_INSERT [catalog].[Fibra] OFF;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260618180316_AddFibraEXIAndAGRO', N'10.0.8');

COMMIT;
GO

