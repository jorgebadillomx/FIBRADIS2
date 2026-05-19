namespace Api.HealthChecks;

public record HealthCheckResponse(string Status, IEnumerable<HealthCheckEntry> Checks);

public record HealthCheckEntry(string Name, string Status, string? Description);
