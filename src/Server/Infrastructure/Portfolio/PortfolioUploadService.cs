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
        var a1 = ws.Cell(1, 1).GetString().Trim();

        if (a1.Equals("Emisora/Fondo", StringComparison.OrdinalIgnoreCase)
            || a1.StartsWith("Mercado de Capitales", StringComparison.OrdinalIgnoreCase))
            return ParseGbmOfficial(ws, activeFibras, commissionFactor);

        if (a1.Equals("Emisora", StringComparison.OrdinalIgnoreCase))
            return ParseGbmPasted(ws, activeFibras, commissionFactor);

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

    private static PortfolioUploadResult ParseGbmOfficial(
        IXLWorksheet ws,
        IReadOnlyList<Fibra> activeFibras,
        decimal commissionFactor)
    {
        var rows = new List<RawRow>();
        var fibraByTicker = activeFibras.ToDictionary(
            f => NormalizeTicker(f.Ticker),
            f => f,
            StringComparer.OrdinalIgnoreCase);
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

        for (var r = 2; r <= lastRow; r++)
        {
            var rawTicker = ws.Cell(r, 1).GetString().Replace("\u00A0", " ").Trim();
            if (IsGbmOfficialSectionHeader(rawTicker))
                continue;

            var ticker = NormalizeTicker(rawTicker);
            if (string.IsNullOrWhiteSpace(ticker))
                continue;

            if (ticker.Equals("EFEC.", StringComparison.OrdinalIgnoreCase))
                continue;

            if (!fibraByTicker.TryGetValue(ticker, out var fibra))
                continue;

            var qtyCell = ws.Cell(r, 2);
            var qtyStr = qtyCell.GetString()?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(qtyStr) && qtyCell.DataType == XLDataType.Number)
                qtyStr = qtyCell.GetDouble().ToString(CultureInfo.InvariantCulture);

            var avgCostCell = ws.Cell(r, 3);
            var avgCostStr = avgCostCell.GetString()?.Trim() ?? string.Empty;
            if (TryParseCurrencyString(avgCostStr, out var currencyValue))
            {
                avgCostStr = currencyValue.ToString(CultureInfo.InvariantCulture);
            }
            else if (string.IsNullOrWhiteSpace(avgCostStr) && avgCostCell.DataType == XLDataType.Number)
            {
                avgCostStr = avgCostCell.GetDouble().ToString(CultureInfo.InvariantCulture);
            }

            if (string.IsNullOrWhiteSpace(qtyStr) || string.IsNullOrWhiteSpace(avgCostStr))
                continue;

            rows.Add(new RawRow(r, fibra.Ticker, qtyStr, avgCostStr));
        }

        return ValidateAndBuild(rows, activeFibras, commissionFactor);
    }

    private static PortfolioUploadResult ParseGbmPasted(
        IXLWorksheet ws,
        IReadOnlyList<Fibra> activeFibras,
        decimal commissionFactor)
    {
        var rows = new List<RawRow>();
        var fibraByTicker = activeFibras.ToDictionary(
            f => NormalizeTicker(f.Ticker),
            f => f,
            StringComparer.OrdinalIgnoreCase);
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;
        RawRow? pendingData = null;

        for (var r = 2; r <= lastRow; r++)
        {
            var tickerCell = ws.Cell(r, 1).GetString()
                .Replace("&nbsp;", string.Empty)
                .Replace("\u00A0", string.Empty)
                .Trim();
            var qtyCell = ws.Cell(r, 2);
            var avgCostCell = ws.Cell(r, 3);

            var hasNumericData = qtyCell.DataType == XLDataType.Number || avgCostCell.DataType == XLDataType.Number;
            var isDataRow = string.IsNullOrWhiteSpace(tickerCell) && hasNumericData;

            if (isDataRow)
            {
                var qtyStr = qtyCell.DataType == XLDataType.Number
                    ? qtyCell.GetDouble().ToString(CultureInfo.InvariantCulture)
                    : qtyCell.GetString()?.Trim() ?? string.Empty;

                var avgCostStr = avgCostCell.GetString()?.Trim() ?? string.Empty;
                if (TryParseCurrencyString(avgCostStr, out var currencyValue))
                {
                    avgCostStr = currencyValue.ToString(CultureInfo.InvariantCulture);
                }
                else if (string.IsNullOrWhiteSpace(avgCostStr) && avgCostCell.DataType == XLDataType.Number)
                {
                    avgCostStr = avgCostCell.GetDouble().ToString(CultureInfo.InvariantCulture);
                }

                pendingData = new RawRow(r, string.Empty, qtyStr, avgCostStr);
                continue;
            }

            if (pendingData is not null && !string.IsNullOrWhiteSpace(tickerCell))
            {
                var ticker = NormalizeTicker(tickerCell);
                if (fibraByTicker.TryGetValue(ticker, out var fibra))
                    rows.Add(pendingData with { Ticker = fibra.Ticker });

                pendingData = null;
                continue;
            }

            pendingData = null;
        }

        return ValidateAndBuild(rows, activeFibras, commissionFactor);
    }

    private static bool IsGbmOfficialSectionHeader(string rawTicker)
        => rawTicker.StartsWith("Mercado de Capitales", StringComparison.OrdinalIgnoreCase)
            || rawTicker.Equals("Emisora/Fondo", StringComparison.OrdinalIgnoreCase)
            || rawTicker.Equals("Efectivo", StringComparison.OrdinalIgnoreCase);

    private static bool TryParseCurrencyString(string raw, out decimal value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(raw) || raw == "-")
            return false;

        var cleaned = raw
            .Replace("$", string.Empty)
            .Replace(",", string.Empty)
            .Replace("\u00A0", string.Empty)
            .Trim();

        return decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
    }

    private static string NormalizeTicker(string raw)
        => raw.Replace(" ", string.Empty).Trim();

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
