using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using Application.Integrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Integrations.Banxico;

public class BanxicoClient(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<BanxicoClient> logger) : IBanxicoClient
{
    private const string DefaultSeries = "SF43936";

    public async Task<decimal?> GetCetes28dAsync(CancellationToken ct = default)
    {
        var token = configuration["Banxico:Token"];
        if (string.IsNullOrWhiteSpace(token))
        {
            logger.LogWarning("BanxicoClient: Banxico:Token no configurado; CETES 28d no disponible");
            return null;
        }

        var series = string.IsNullOrWhiteSpace(configuration["Banxico:Series"])
            ? DefaultSeries
            : configuration["Banxico:Series"]!;

        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"https://www.banxico.org.mx/SieAPIRest/service/v1/series/{Uri.EscapeDataString(series)}/datos/oportuno");

            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.TryAddWithoutValidation("Bmx-Token", token);

            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "BanxicoClient: respuesta no exitosa ({StatusCode}) para serie {Series}",
                    (int)response.StatusCode,
                    series);
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            if (!TryGetDato(document.RootElement, out var rawDato))
            {
                logger.LogWarning("BanxicoClient: respuesta sin dato CETES 28d para serie {Series}", series);
                return null;
            }

            if (string.IsNullOrWhiteSpace(rawDato) || string.Equals(rawDato, "N/E", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning("BanxicoClient: serie {Series} sin dato disponible (N/E)", series);
                return null;
            }

            if (!decimal.TryParse(rawDato, NumberStyles.Number, CultureInfo.InvariantCulture, out var rate))
            {
                logger.LogWarning("BanxicoClient: no se pudo interpretar el dato CETES 28d '{Dato}'", rawDato);
                return null;
            }

            if (rate <= 0)
            {
                logger.LogWarning("BanxicoClient: tasa no positiva '{Rate}' para serie {Series}", rate, series);
                return null;
            }

            return rate;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("BanxicoClient: timeout consultando serie {Series}", series);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "BanxicoClient: error consultando serie {Series}", series);
            return null;
        }
    }

    private static bool TryGetDato(JsonElement root, out string? dato)
    {
        dato = null;

        if (!root.TryGetProperty("bmx", out var bmx) ||
            !bmx.TryGetProperty("series", out var series) ||
            series.ValueKind != JsonValueKind.Array ||
            series.GetArrayLength() == 0)
        {
            return false;
        }

        var firstSeries = series[0];
        if (!firstSeries.TryGetProperty("datos", out var datos) ||
            datos.ValueKind != JsonValueKind.Array ||
            datos.GetArrayLength() == 0)
        {
            return false;
        }

        var firstDato = datos[datos.GetArrayLength() - 1];
        if (!firstDato.TryGetProperty("dato", out var datoProperty))
            return false;

        dato = datoProperty.GetString();
        return true;
    }
}
