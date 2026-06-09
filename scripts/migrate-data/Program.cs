using Microsoft.Data.SqlClient;
using Npgsql;
using System.Data;

var sqlConnStr = "Server=LAPBADIS;Database=FIBRADIS_Dev;Integrated Security=True;TrustServerCertificate=True";
var pgConnStr  = "Host=localhost;Port=5432;Database=fibradis_dev;Username=fibradis_app;Password=devpassword";

// Orden respetando FK: tablas raíz primero, luego dependientes
var tables = new[]
{
    // Auth
    ("auth",          "User"),
    // Catalog
    ("catalog",       "Fibra"),
    // AI
    ("ai",            "AiModeConfig"),
    ("ai",            "AiPrompt"),
    ("ai",            "AiProviderConfig"),
    // News (sin FK a Fibra directamente)
    ("news",          "BlocklistTerm"),
    ("news",          "NewsArticle"),
    // Ops
    ("ops",           "OperationalConfig"),
    ("ops",           "EditorialPage"),
    ("ops",           "ConfigAuditLog"),
    // Fundamentals (depende de Fibra)
    ("fundamentals",  "FundamentalSourceManifest"),
    ("fundamentals",  "FundamentalRecord"),
    // Market (depende de Fibra)
    ("market",        "PriceSnapshot"),
    ("market",        "DailySnapshot"),
    ("market",        "Distribution"),
    // Jobs
    ("jobs",          "PipelineRunLog"),
    ("jobs",          "PipelineErrorLog"),
    ("jobs",          "AiCallLog"),
    // News JT (depende de NewsArticle + Fibra)
    ("news",          "NewsArticleFibra"),
    // Auth (depende de User)
    ("auth",          "RefreshToken"),
    // Portfolio (depende de User + Fibra)
    ("portfolio",     "PortfolioPositions"),
    ("portfolio",     "PortfolioSnapshots"),
    ("portfolio",     "UserPortfolioSettings"),
    ("portfolio",     "UserOpportunityWeights"),
    ("portfolio",     "UserFavorites"),
};

await using var sqlConn = new SqlConnection(sqlConnStr);
await using var pgConn  = new NpgsqlConnection(pgConnStr);

await sqlConn.OpenAsync();
await pgConn.OpenAsync();

Console.WriteLine("Conexiones establecidas.\n");

foreach (var (schema, table) in tables)
{
    Console.Write($"Migrando {schema}.{table}... ");

    // Leer metadatos de columnas desde SQL Server (destino)
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

    if (colMeta.Count == 0) { Console.WriteLine("sin columnas en SQL Server, saltando."); continue; }

    // Detectar columnas que existen en PostgreSQL (fuente)
    // PostgreSQL usa snake_case (ya configurado con HasColumnName en EF Core)
    var pgColsExisting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    await using (var pgMetaCmd = new NpgsqlCommand(
        $"SELECT column_name FROM information_schema.columns WHERE table_schema = '{schema}' AND table_name = '{table}'", pgConn))
    await using (var pgMetaReader = await pgMetaCmd.ExecuteReaderAsync())
        while (await pgMetaReader.ReadAsync())
            pgColsExisting.Add(pgMetaReader.GetString(0));

    if (pgColsExisting.Count == 0) { Console.WriteLine("tabla no existe en PostgreSQL, saltando."); continue; }

    // Filtrar columnas que existen en ambas bases (por nombre SQL Server, mapeando a PG snake_case)
    var matchedCols = colMeta.Where(c => pgColsExisting.Contains(c.Name) || pgColsExisting.Contains(ToSnakeCase(c.Name))).ToList();
    var skipped = colMeta.Except(matchedCols).Select(c => c.Name).ToList();
    if (skipped.Any()) Console.Write($"[skip cols: {string.Join(",", skipped)}] ");

    if (matchedCols.Count == 0) { Console.WriteLine("sin columnas coincidentes."); continue; }

    // Construir SELECT desde PostgreSQL (usando el nombre snake_case de la columna PG)
    var pgSelectCols = matchedCols.Select(c =>
    {
        var pgName = pgColsExisting.Contains(c.Name) ? c.Name : ToSnakeCase(c.Name);
        return $"\"{pgName}\"";
    });
    var pgSelect = string.Join(", ", pgSelectCols);

    // Leer datos de PostgreSQL
    var dt = new DataTable();
    foreach (var (colName, _) in matchedCols)
        dt.Columns.Add(colName);

    int count = 0;
    await using var pgDataCmd = new NpgsqlCommand($"SELECT {pgSelect} FROM {schema}.\"{table}\"", pgConn);
    await using var pgReader = await pgDataCmd.ExecuteReaderAsync();
    while (await pgReader.ReadAsync())
    {
        var row = dt.NewRow();
        for (int i = 0; i < matchedCols.Count; i++)
            row[i] = pgReader.IsDBNull(i) ? DBNull.Value : pgReader.GetValue(i);
        dt.Rows.Add(row);
        count++;
    }

    if (count == 0) { Console.WriteLine("0 registros en PostgreSQL."); continue; }

    // Truncar destino SQL Server
    await using (var truncCmd = new SqlCommand($"DELETE FROM [{schema}].[{table}]", sqlConn))
        await truncCmd.ExecuteNonQueryAsync();

    // Escribir a SQL Server con SqlBulkCopy
    using var bulkCopy = new SqlBulkCopy(sqlConn)
    {
        DestinationTableName = $"[{schema}].[{table}]",
        BulkCopyTimeout = 300,
    };
    foreach (DataColumn col in dt.Columns)
        bulkCopy.ColumnMappings.Add(col.ColumnName, col.ColumnName);

    await bulkCopy.WriteToServerAsync(dt);
    Console.WriteLine($"{count} registros.");
}

Console.WriteLine("\n=== Migración completada ===");

static string ToSnakeCase(string name)
{
    if (string.IsNullOrEmpty(name)) return name;
    var sb = new System.Text.StringBuilder();
    for (int i = 0; i < name.Length; i++)
    {
        if (char.IsUpper(name[i]) && i > 0)
            sb.Append('_');
        sb.Append(char.ToLower(name[i]));
    }
    return sb.ToString();
}
