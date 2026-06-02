using Microsoft.Data.SqlClient;
using Npgsql;
using NpgsqlTypes;

var sqlConnStr = "Server=LAPBADIS;Database=FIBRADIS_Dev;Integrated Security=True;TrustServerCertificate=True";
var pgConnStr  = "Host=localhost;Port=5432;Database=fibradis_dev;Username=fibradis_app;Password=devpassword";

// Orden respetando FK: primero tablas sin dependencias, luego las que las tienen
var tables = new[]
{
    ("ai",           "AiModeConfig"),
    ("ai",           "AiPrompt"),
    ("ai",           "AiProviderConfig"),
    ("news",         "BlocklistTerm"),
    ("ops",          "OperationalConfig"),
    ("ops",          "EditorialPage"),
    ("ops",          "ConfigAuditLog"),
    ("fundamentals", "FundamentalSourceManifest"),
    ("news",         "NewsArticle"),
    ("market",       "PriceSnapshot"),
    ("market",       "DailySnapshot"),
    ("market",       "Distribution"),
    ("fundamentals", "FundamentalRecord"),
    ("jobs",         "PipelineRunLog"),
    ("jobs",         "PipelineErrorLog"),
    ("jobs",         "AiCallLog"),
    ("news",         "NewsArticleFibra"),
};

await using var sqlConn = new SqlConnection(sqlConnStr);
await using var pgConn  = new NpgsqlConnection(pgConnStr);

await sqlConn.OpenAsync();
await pgConn.OpenAsync();

Console.WriteLine("Conexiones establecidas.\n");

foreach (var (schema, table) in tables)
{
    Console.Write($"Migrando {schema}.{table}... ");

    // Leer metadatos de columnas desde SQL Server
    var colMeta = new List<(string Name, string Type)>();
    await using (var metaCmd = new SqlCommand(
        $"SELECT COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS " +
        $"WHERE TABLE_SCHEMA = '{schema}' AND TABLE_NAME = '{table}' " +
        $"ORDER BY ORDINAL_POSITION", sqlConn))
    await using (var metaReader = await metaCmd.ExecuteReaderAsync())
    {
        while (await metaReader.ReadAsync())
            colMeta.Add((metaReader.GetString(0), metaReader.GetString(1)));
    }

    if (colMeta.Count == 0) { Console.WriteLine("sin columnas, saltando."); continue; }

    // Detectar columnas que existen en PostgreSQL
    var pgColsExisting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    await using (var pgMetaCmd = new NpgsqlCommand(
        $"SELECT column_name FROM information_schema.columns WHERE table_schema = '{schema}' AND table_name = '{table}'", pgConn))
    await using (var pgMetaReader = await pgMetaCmd.ExecuteReaderAsync())
        while (await pgMetaReader.ReadAsync())
            pgColsExisting.Add(pgMetaReader.GetString(0));

    // Filtrar columnas que existen en ambas bases
    var filteredCols = colMeta.Where(c => pgColsExisting.Contains(c.Name) || pgColsExisting.Contains($"\"{c.Name}\"")).ToList();
    var skipped = colMeta.Except(filteredCols).Select(c => c.Name).ToList();
    if (skipped.Any()) Console.Write($"[skip: {string.Join(",", skipped)}] ");

    colMeta = filteredCols;

    // Truncar tabla destino (CASCADE para FKs)
    await using (var truncCmd = new NpgsqlCommand(
        $"TRUNCATE {schema}.\"{table}\" RESTART IDENTITY CASCADE", pgConn))
        await truncCmd.ExecuteNonQueryAsync();

    // Leer datos de SQL Server — solo columnas que existen en PG
    int count = 0;
    var sqlCols = string.Join(", ", colMeta.Select(c => $"[{c.Name}]"));
    await using var dataCmd = new SqlCommand($"SELECT {sqlCols} FROM [{schema}].[{table}]", sqlConn);
    await using var reader  = await dataCmd.ExecuteReaderAsync();

    // Construir columnas PostgreSQL (snake_case no necesita comillas, PascalCase sí)
    var pgCols = colMeta.Select(c =>
        c.Name == c.Name.ToLowerInvariant() ? c.Name : $"\"{c.Name}\""
    ).ToList();
    var colList = string.Join(", ", pgCols);

    await using var pgWriter = await pgConn.BeginBinaryImportAsync(
        $"COPY {schema}.\"{table}\" ({colList}) FROM STDIN (FORMAT BINARY)");

    while (await reader.ReadAsync())
    {
        await pgWriter.StartRowAsync();

        for (int i = 0; i < colMeta.Count; i++)
        {
            if (reader.IsDBNull(i)) { await pgWriter.WriteNullAsync(); continue; }

            var (_, dataType) = colMeta[i];
            switch (dataType.ToLower())
            {
                case "uniqueidentifier":
                    await pgWriter.WriteAsync(reader.GetGuid(i), NpgsqlDbType.Uuid);
                    break;
                case "bit":
                    await pgWriter.WriteAsync(reader.GetBoolean(i), NpgsqlDbType.Boolean);
                    break;
                case "int":
                    await pgWriter.WriteAsync(reader.GetInt32(i), NpgsqlDbType.Integer);
                    break;
                case "bigint":
                    await pgWriter.WriteAsync(reader.GetInt64(i), NpgsqlDbType.Bigint);
                    break;
                case "smallint":
                    await pgWriter.WriteAsync(reader.GetInt16(i), NpgsqlDbType.Smallint);
                    break;
                case "decimal":
                case "numeric":
                case "money":
                case "smallmoney":
                    await pgWriter.WriteAsync(reader.GetDecimal(i), NpgsqlDbType.Numeric);
                    break;
                case "float":
                    await pgWriter.WriteAsync(reader.GetDouble(i), NpgsqlDbType.Double);
                    break;
                case "real":
                    await pgWriter.WriteAsync((double)reader.GetFloat(i), NpgsqlDbType.Double);
                    break;
                case "datetime":
                case "datetime2":
                case "smalldatetime":
                    var dt = reader.GetDateTime(i);
                    await pgWriter.WriteAsync(
                        DateTime.SpecifyKind(dt, DateTimeKind.Utc),
                        NpgsqlDbType.TimestampTz);
                    break;
                case "datetimeoffset":
                    var dto = (DateTimeOffset)reader.GetValue(i);
                    await pgWriter.WriteAsync(dto, NpgsqlDbType.TimestampTz);
                    break;
                case "date":
                    await pgWriter.WriteAsync(DateOnly.FromDateTime(reader.GetDateTime(i)), NpgsqlDbType.Date);
                    break;
                default:
                    // varchar, nvarchar, text, char, nchar, xml, etc.
                    await pgWriter.WriteAsync(reader.GetValue(i).ToString()!, NpgsqlDbType.Text);
                    break;
            }
        }
        count++;
    }

    await pgWriter.CompleteAsync();
    Console.WriteLine($"{count} registros.");
}

Console.WriteLine("\n=== Migración completada ===");
