using System.Text.Json.Serialization;

namespace Infrastructure.Integrations.MasDividendos;

public sealed record MasDividendosRecord(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("empresa")] string? Empresa,
    [property: JsonPropertyName("ticker")] string? Ticker,
    [property: JsonPropertyName("monto")] string? Monto,
    [property: JsonPropertyName("comentario")] string? Comentario,
    [property: JsonPropertyName("fecha_pago")] DateOnly? FechaPago,
    [property: JsonPropertyName("fecha_ex_derecho")] DateOnly? FechaExDerecho,
    [property: JsonPropertyName("link_aviso")] string? LinkAviso
);
