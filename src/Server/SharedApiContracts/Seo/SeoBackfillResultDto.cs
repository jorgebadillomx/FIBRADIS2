namespace SharedApiContracts.Seo;

/// <summary>
/// Conteo por tipo de filas SeoMetadata creadas durante el backfill idempotente (AC-7 de 12-1).
/// Una re-ejecución devuelve ceros (no duplica filas existentes).
/// </summary>
public record SeoBackfillResultDto(
    int StaticPages,
    int Fibras,
    int News);
