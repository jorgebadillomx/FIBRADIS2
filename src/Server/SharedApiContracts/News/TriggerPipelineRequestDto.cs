namespace SharedApiContracts.News;

/// <summary>
/// Payload para disparar el pipeline de noticias manualmente.
/// Si FibraIds está vacío o es null, se procesan todas las fibras activas.
/// </summary>
public record TriggerPipelineRequestDto(Guid[]? FibraIds = null);
