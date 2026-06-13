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
    private const string TiieSeries = "SF43783";
    private const string InpcSeries = "SP1";

    public async Task<decimal?> GetCetes28dAsync(CancellationToken ct = default)
        => await GetLatestRateAsync(
            string.IsNullOrWhiteSpace(configuration["Banxico:Series"])
                ? DefaultSeries
                : configuration["Banxico:Series"]!,
            "CETES 28d",
            ct);

    public Task<decimal?> GetTiie28dAsync(CancellationToken ct = default)
        => GetLatestRateAsync(TiieSeries, "TIIE 28d", ct);

    public async Task<IReadOnlyList<(DateOnly Periodo, decimal InpcIndex)>> GetInpcHistoryAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken ct = default)
    {
        var token = configuration["Banxico:Token"];
        if (string.IsNullOrWhiteSpace(token))
        {
            logger.LogWarning("BanxicoClient: Banxico:Token no configurado; INPC no disponible");
            return [];
        }

        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"https://www.banxico.org.mx/SieAPIRest/service/v1/series/{Uri.EscapeDataString(InpcSeries)}/datos/{from:yyyy-MM-dd}/{to:yyyy-MM-dd}");

            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.TryAddWithoutValidation("Bmx-Token", token);

            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "BanxicoClient: respuesta no exitosa ({StatusCode}) para serie {Series}",
                    (int)response.StatusCode,
                    InpcSeries);
                return [];
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            if (!TryGetDatos(document.RootElement, out var datos))
            {
                logger.LogWarning("BanxicoClient: respuesta sin datos INPC para serie {Series}", InpcSeries);
                return [];
            }

            var history = new List<(DateOnly Periodo, decimal InpcIndex)>();
            foreach (var dato in datos.EnumerateArray())
            {
                if (!TryParseInpcEntry(dato, out var periodo, out var inpcIndex))
                    continue;

                history.Add((periodo, inpcIndex));
            }

            return history;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("BanxicoClient: timeout consultando serie {Series}", InpcSeries);
            return [];
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "BanxicoClient: error consultando serie {Series}", InpcSeries);
            return [];
        }
    }

    private async Task<decimal?> GetLatestRateAsync(string series, string label, CancellationToken ct)
    {
        var token = configuration["Banxico:Token"];
        if (string.IsNullOrWhiteSpace(token))
        {
            logger.LogWarning("BanxicoClient: Banxico:Token no configurado; {Label} no disponible", label);
            return null;
        }

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
                logger.LogWarning("BanxicoClient: respuesta sin dato {Label} para serie {Series}", label, series);
                return null;
            }

            if (string.IsNullOrWhiteSpace(rawDato) || string.Equals(rawDato, "N/E", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning("BanxicoClient: serie {Series} sin dato disponible (N/E)", series);
                return null;
            }

            if (!decimal.TryParse(rawDato, NumberStyles.Number, CultureInfo.InvariantCulture, out var rate))
            {
                logger.LogWarning("BanxicoClient: no se pudo interpretar el dato {Label} '{Dato}'", label, rawDato);
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

    private static bool TryGetDatos(JsonElement root, out JsonElement datos)
    {
        datos = default;

        if (!root.TryGetProperty("bmx", out var bmx) ||
            !bmx.TryGetProperty("series", out var series) ||
            series.ValueKind != JsonValueKind.Array ||
            series.GetArrayLength() == 0)
        {
            return false;
        }

        var firstSeries = series[0];
        if (!firstSeries.TryGetProperty("datos", out datos) ||
            datos.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        return datos.GetArrayLength() > 0;
    }

    private static bool TryParseInpcEntry(JsonElement dato, out DateOnly periodo, out decimal inpcIndex)
    {
        periodo = default;
        inpcIndex = default;

        if (!dato.TryGetProperty("fecha", out var fechaProperty) ||
            !dato.TryGetProperty("dato", out var datoProperty))
        {
            return false;
        }

        var rawFecha = fechaProperty.GetString();
        var rawDato = datoProperty.GetString();
        if (string.IsNullOrWhiteSpace(rawFecha) ||
            string.IsNullOrWhiteSpace(rawDato) ||
            string.Equals(rawDato, "N/E", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!DateOnly.TryParseExact(rawFecha, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedFecha))
            return false;

        if (!decimal.TryParse(rawDato, NumberStyles.Number, CultureInfo.InvariantCulture, out inpcIndex))
            return false;

        periodo = new DateOnly(parsedFecha.Year, parsedFecha.Month, 1);
        return true;
    }
}
