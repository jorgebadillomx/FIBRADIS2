using System.Globalization;
using Application.Portfolio;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using Domain.Catalog;
using Domain.Portfolio;

namespace Infrastructure.Portfolio;

public class PortfolioUploadService : IPortfolioUploadService
{
    private static readonly HashSet<string> RequiredHeaders =
        new(StringComparer.OrdinalIgnoreCase) { "Ticker", "Qty", "AvgCost" };

    public Task<PortfolioUploadResult> ParseAndValidateAsync(
        Stream fileStream,
        string fileName,
        IReadOnlyList<Fibra> activeFibras,
        decimal commissionFactor,
        CancellationToken ct = default)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();

        // P5: explicit extension guard — unknown types would fall through to CSV and produce confusing errors
        if (ext != ".xlsx" && ext != ".csv")
        {
            var error = new RowError(0, string.Empty, "Formato no soportado. Use .xlsx o .csv.");
            return Task.FromResult(new PortfolioUploadResult(false, [], [error]));
        }

        var result = ext == ".xlsx"
            ? ParseXlsx(fileStream, activeFibras, commissionFactor)
            : ParseCsv(fileStream, activeFibras, commissionFactor);

        return Task.FromResult(result);
    }

    private static PortfolioUploadResult ParseXlsx(
        Stream stream, IReadOnlyList<Fibra> activeFibras, decimal commissionFactor)
    {
        using var workbook = new XLWorkbook(stream);
        var ws = workbook.Worksheets.First();
        var lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 0;

        var headers = ws.Row(1)
            .Cells(1, lastCol)
            .Select(c => c.GetString()?.Trim() ?? string.Empty)
            .ToList();

        var headerError = ValidateHeaders(headers);
        if (headerError is not null)
            return headerError;

        var tickerIdx = GetHeaderIndex(headers, "Ticker");
        var qtyIdx = GetHeaderIndex(headers, "Qty");
        var avgCostIdx = GetHeaderIndex(headers, "AvgCost");

        var rows = new List<RawRow>();
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

        for (var r = 2; r <= lastRow; r++)
        {
            var ticker = ws.Cell(r, tickerIdx + 1).GetString()?.Trim() ?? string.Empty;
            var qtyStr = ws.Cell(r, qtyIdx + 1).GetString()?.Trim() ?? string.Empty;
            var avgCostStr = ws.Cell(r, avgCostIdx + 1).GetString()?.Trim() ?? string.Empty;

            // Handle numeric cells
            if (string.IsNullOrEmpty(qtyStr) && ws.Cell(r, qtyIdx + 1).DataType == XLDataType.Number)
                qtyStr = ws.Cell(r, qtyIdx + 1).GetDouble().ToString(CultureInfo.InvariantCulture);
            if (string.IsNullOrEmpty(avgCostStr) && ws.Cell(r, avgCostIdx + 1).DataType == XLDataType.Number)
                avgCostStr = ws.Cell(r, avgCostIdx + 1).GetDouble().ToString(CultureInfo.InvariantCulture);

            if (string.IsNullOrWhiteSpace(ticker) && string.IsNullOrWhiteSpace(qtyStr) && string.IsNullOrWhiteSpace(avgCostStr))
                continue;

            rows.Add(new RawRow(r - 1, ticker, qtyStr, avgCostStr));
        }

        return ValidateAndBuild(rows, activeFibras, commissionFactor);
    }

    private static PortfolioUploadResult ParseCsv(
        Stream stream, IReadOnlyList<Fibra> activeFibras, decimal commissionFactor)
    {
        using var reader = new StreamReader(stream);
        // P1: PrepareHeaderForMatch normalizes both header and lookup key to lowercase,
        // so GetField("Ticker") matches "ticker", "TICKER", etc.
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            PrepareHeaderForMatch = args => args.Header.Trim().ToLowerInvariant(),
        };
        using var csv = new CsvReader(reader, config);

        csv.Read();
        csv.ReadHeader();
        var headers = csv.HeaderRecord?.Select(h => h?.Trim() ?? string.Empty).ToList()
                      ?? new List<string>();

        var headerError = ValidateHeaders(headers);
        if (headerError is not null)
            return headerError;

        var rows = new List<RawRow>();
        var rowNum = 1;
        while (csv.Read())
        {
            rowNum++;
            var ticker = csv.GetField("ticker")?.Trim() ?? string.Empty;
            var qtyStr = csv.GetField("qty")?.Trim() ?? string.Empty;
            var avgCostStr = csv.GetField("avgcost")?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(ticker) && string.IsNullOrWhiteSpace(qtyStr) && string.IsNullOrWhiteSpace(avgCostStr))
                continue;

            rows.Add(new RawRow(rowNum, ticker, qtyStr, avgCostStr));
        }

        return ValidateAndBuild(rows, activeFibras, commissionFactor);
    }

    private static PortfolioUploadResult? ValidateHeaders(List<string> headers)
    {
        var missing = RequiredHeaders
            .Where(r => !headers.Any(h => string.Equals(h, r, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (missing.Count == 0) return null;

        var found = string.Join(", ", headers.Where(h => !string.IsNullOrEmpty(h)));
        var error = new RowError(
            RowNumber: 0,
            Ticker: string.Empty,
            Message: $"Columnas requeridas: Ticker, Qty, AvgCost. Encontradas: {found}.");

        return new PortfolioUploadResult(false, [], [error]);
    }

    private static int GetHeaderIndex(List<string> headers, string name)
        => headers.FindIndex(h => string.Equals(h, name, StringComparison.OrdinalIgnoreCase));

    private static PortfolioUploadResult ValidateAndBuild(
        List<RawRow> rows,
        IReadOnlyList<Fibra> activeFibras,
        decimal commissionFactor)
    {
        // P3: empty file (headers only) must fail explicitly, not silently delete the portfolio
        if (rows.Count == 0)
        {
            var empty = new RowError(0, string.Empty, "El archivo no contiene posiciones.");
            return new PortfolioUploadResult(false, [], [empty]);
        }

        var fibraByTicker = activeFibras.ToDictionary(
            f => f.Ticker,
            f => f,
            StringComparer.OrdinalIgnoreCase);

        var errors = new List<RowError>();
        var validRows = new List<ValidRow>();

        foreach (var row in rows)
        {
            if (!fibraByTicker.TryGetValue(row.Ticker, out var fibra))
            {
                errors.Add(new RowError(row.RowNumber, row.Ticker, "Ticker no encontrado en el catálogo."));
                continue;
            }

            if (!int.TryParse(row.QtyStr, out var qty) || qty <= 0)
            {
                errors.Add(new RowError(row.RowNumber, row.Ticker, "Qty debe ser un número entero mayor a 0."));
                continue;
            }

            if (!decimal.TryParse(row.AvgCostStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var avgCost) || avgCost <= 0)
            {
                errors.Add(new RowError(row.RowNumber, row.Ticker, "AvgCost debe ser un número decimal mayor a 0."));
                continue;
            }

            validRows.Add(new ValidRow(fibra.Id, row.Ticker, qty, avgCost));
        }

        if (errors.Count > 0)
            return new PortfolioUploadResult(false, [], errors);

        var consolidated = validRows
            .GroupBy(r => r.Ticker, StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var totalQty = g.Sum(r => r.Qty);
                var costoPromedio = g.Sum(r => (decimal)r.Qty * r.AvgCost) / totalQty;
                var costoTotal = totalQty * costoPromedio * (1 + commissionFactor);
                var fibraId = g.First().FibraId;

                return new PositionDto(fibraId, g.Key, totalQty, costoPromedio, costoTotal);
            })
            .ToList();

        return new PortfolioUploadResult(true, consolidated, []);
    }

    private record RawRow(int RowNumber, string Ticker, string QtyStr, string AvgCostStr);
    private record ValidRow(Guid FibraId, string Ticker, int Qty, decimal AvgCost);
}
