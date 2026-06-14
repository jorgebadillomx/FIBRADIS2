namespace SharedApiContracts.Catalog;

/// <summary>
/// Datos mínimos para una tarjeta de "FIBRA relacionada" (enlazado interno por sector, story 12-8).
/// El slug se construye en el cliente con buildFibraSlug(fullName, ticker) — paridad con el resto del sitio.
/// </summary>
public record RelatedFibra(
    string Ticker,
    string FullName,
    string ShortName,
    string Sector);
