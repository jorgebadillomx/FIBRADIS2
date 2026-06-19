-- Script de datos: ejecutar DESPUÉS de "dotnet ef database update" en producción.
-- Asigna suscripción Lifetime a todos los usuarios activos existentes.
BEGIN TRANSACTION;

UPDATE [auth].[User]
SET subscription_type = 'Lifetime',
    subscription_started_at = COALESCE(FechaPago, SYSUTCDATETIME()),
    subscription_ends_at = NULL,
    IsActive = 1
WHERE IsActive = 1;

COMMIT;
GO
