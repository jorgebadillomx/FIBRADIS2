namespace SharedApiContracts.Portfolio;

public sealed record UserFavoritesDto(IReadOnlyList<Guid> FibraIds);
