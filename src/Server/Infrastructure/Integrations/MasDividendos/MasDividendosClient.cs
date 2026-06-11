using System.Net.Http.Json;
using System.Text.Json;

namespace Infrastructure.Integrations.MasDividendos;

public sealed class MasDividendosClient(HttpClient httpClient) : IMasDividendosClient
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<IReadOnlyList<MasDividendosRecord>> GetAllAsync(CancellationToken ct = default)
    {
        using var response = await httpClient.GetAsync("config/conexiones/database.php", ct);
        response.EnsureSuccessStatusCode();

        try
        {
            var records = await response.Content.ReadFromJsonAsync<List<MasDividendosRecord>>(_jsonOptions, ct);
            return records ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
