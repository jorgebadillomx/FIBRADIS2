using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "jobs");

            migrationBuilder.EnsureSchema(
                name: "ai");

            migrationBuilder.EnsureSchema(
                name: "news");

            migrationBuilder.EnsureSchema(
                name: "ops");

            migrationBuilder.EnsureSchema(
                name: "market");

            migrationBuilder.EnsureSchema(
                name: "catalog");

            migrationBuilder.EnsureSchema(
                name: "fundamentals");

            migrationBuilder.EnsureSchema(
                name: "auth");

            migrationBuilder.CreateTable(
                name: "AiCallLog",
                schema: "jobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    operation = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    model_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    prompt_length = table.Column<int>(type: "integer", nullable: false),
                    duration_ms = table.Column<long>(type: "bigint", nullable: false),
                    success = table.Column<bool>(type: "boolean", nullable: false),
                    request_raw = table.Column<string>(type: "text", nullable: true),
                    response_raw = table.Column<string>(type: "text", nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    context = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiCallLog", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "AiModeConfig",
                schema: "ai",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    mode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    news_model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    previous_mode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    min_body_text_length_for_ai = table.Column<int>(type: "integer", nullable: false, defaultValue: 500)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiModeConfig", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "AiPrompt",
                schema: "ai",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    content_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    prompt_template = table.Column<string>(type: "text", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiPrompt", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "AiProviderConfig",
                schema: "ai",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    model_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiProviderConfig", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "BlocklistTerm",
                schema: "news",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    term = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlocklistTerm", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ConfigAuditLog",
                schema: "ops",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    field_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    previous_value = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    new_value = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfigAuditLog", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "DailySnapshot",
                schema: "market",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fibra_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ticker = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    open = table.Column<decimal>(type: "numeric(18,6)", nullable: true),
                    high = table.Column<decimal>(type: "numeric(18,6)", nullable: true),
                    low = table.Column<decimal>(type: "numeric(18,6)", nullable: true),
                    close = table.Column<decimal>(type: "numeric(18,6)", nullable: true),
                    volume = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailySnapshot", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Distribution",
                schema: "market",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fibra_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ticker = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    payment_date = table.Column<DateOnly>(type: "date", nullable: false),
                    amount_per_unit = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    captured_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Distribution", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "EditorialPage",
                schema: "ops",
                columns: table => new
                {
                    slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EditorialPage", x => x.slug);
                });

            migrationBuilder.CreateTable(
                name: "Fibra",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ticker = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    yahoo_ticker = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    full_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    short_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    sector = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    market = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    currency = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    state = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    site_url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    investor_url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    reports_url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    name_variants = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fibra", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NewsArticle",
                schema: "news",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    title_normalized = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    source = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    url = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    snippet = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    body_text = table.Column<string>(type: "text", nullable: true),
                    image_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    ai_summary = table.Column<string>(type: "text", nullable: true),
                    ai_analysis_json = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    captured_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    error_reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewsArticle", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "OperationalConfig",
                schema: "ops",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    commission_factor = table.Column<decimal>(type: "numeric(10,6)", precision: 10, scale: 6, nullable: false),
                    avg_periods = table.Column<int>(type: "integer", nullable: false),
                    news_cadence_minutes = table.Column<int>(type: "integer", nullable: false),
                    fibra_news_months = table.Column<int>(type: "integer", nullable: false),
                    fundamentals_cadence_minutes = table.Column<int>(type: "integer", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperationalConfig", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "PipelineErrorLog",
                schema: "jobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    pipeline = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    error_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    message = table.Column<string>(type: "text", nullable: false),
                    context = table.Column<string>(type: "text", nullable: true),
                    ai_context = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PipelineErrorLog", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "PipelineRunLog",
                schema: "jobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    pipeline = table.Column<string>(type: "varchar(50)", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "varchar(20)", nullable: false),
                    items_processed = table.Column<int>(type: "integer", nullable: true),
                    error_count = table.Column<int>(type: "integer", nullable: true),
                    triggered_by = table.Column<string>(type: "varchar(100)", nullable: true),
                    details = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PipelineRunLog", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "PriceSnapshot",
                schema: "market",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fibra_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ticker = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    last_price = table.Column<decimal>(type: "numeric(18,6)", nullable: true),
                    daily_change = table.Column<decimal>(type: "numeric(18,6)", nullable: true),
                    daily_change_pct = table.Column<decimal>(type: "numeric(10,4)", nullable: true),
                    volume = table.Column<long>(type: "bigint", nullable: true),
                    week52_high = table.Column<decimal>(type: "numeric(18,6)", nullable: true),
                    week52_low = table.Column<decimal>(type: "numeric(18,6)", nullable: true),
                    captured_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    error_reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceSnapshot", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "User",
                schema: "auth",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FundamentalRecord",
                schema: "fundamentals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    fibra_id = table.Column<Guid>(type: "uuid", nullable: false),
                    period = table.Column<string>(type: "varchar(10)", nullable: false),
                    status = table.Column<string>(type: "varchar(20)", nullable: false),
                    processing_mode = table.Column<string>(type: "varchar(20)", nullable: false),
                    cap_rate = table.Column<decimal>(type: "numeric(18,6)", nullable: true),
                    nav_per_cbfi = table.Column<decimal>(type: "numeric(18,6)", nullable: true),
                    ltv = table.Column<decimal>(type: "numeric(18,6)", nullable: true),
                    noi_margin = table.Column<decimal>(type: "numeric(18,6)", nullable: true),
                    ffo_margin = table.Column<decimal>(type: "numeric(18,6)", nullable: true),
                    quarterly_distribution = table.Column<decimal>(type: "numeric(18,6)", nullable: true),
                    summary = table.Column<string>(type: "text", nullable: true),
                    markdown_content = table.Column<string>(type: "text", nullable: true),
                    FieldNotesJson = table.Column<string>(type: "text", nullable: true),
                    ai_analysis_json = table.Column<string>(type: "text", nullable: true),
                    pdf_reference = table.Column<string>(type: "varchar(500)", nullable: true),
                    pdf_uploaded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    is_possible_update = table.Column<bool>(type: "boolean", nullable: false),
                    imported_by = table.Column<string>(type: "varchar(100)", nullable: true),
                    confirmed_by = table.Column<string>(type: "varchar(100)", nullable: true),
                    captured_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    confirmed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    error_reason = table.Column<string>(type: "varchar(500)", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<string>(type: "varchar(100)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FundamentalRecord", x => x.id);
                    table.ForeignKey(
                        name: "FK_FundamentalRecord_Fibra_fibra_id",
                        column: x => x.fibra_id,
                        principalSchema: "catalog",
                        principalTable: "Fibra",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FundamentalSourceManifest",
                schema: "fundamentals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    source_name = table.Column<string>(type: "varchar(20)", nullable: false),
                    fibra_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source_title = table.Column<string>(type: "varchar(300)", nullable: false),
                    period = table.Column<string>(type: "varchar(10)", nullable: true),
                    report_type = table.Column<string>(type: "varchar(30)", nullable: false),
                    discovery_status = table.Column<string>(type: "varchar(40)", nullable: false),
                    package_url = table.Column<string>(type: "varchar(500)", nullable: false),
                    download_url = table.Column<string>(type: "varchar(1000)", nullable: true),
                    download_signature = table.Column<string>(type: "varchar(500)", nullable: true),
                    pdf_url = table.Column<string>(type: "varchar(1000)", nullable: true),
                    file_name = table.Column<string>(type: "varchar(260)", nullable: true),
                    source_published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    first_seen_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_seen_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_decision = table.Column<string>(type: "varchar(40)", nullable: false),
                    last_decision_reason = table.Column<string>(type: "varchar(500)", nullable: true),
                    last_processed_record_id = table.Column<Guid>(type: "uuid", nullable: true),
                    last_error = table.Column<string>(type: "varchar(500)", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FundamentalSourceManifest", x => x.id);
                    table.ForeignKey(
                        name: "FK_FundamentalSourceManifest_Fibra_fibra_id",
                        column: x => x.fibra_id,
                        principalSchema: "catalog",
                        principalTable: "Fibra",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NewsArticleFibra",
                schema: "news",
                columns: table => new
                {
                    news_article_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fibra_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewsArticleFibra", x => new { x.news_article_id, x.fibra_id });
                    table.ForeignKey(
                        name: "FK_NewsArticleFibra_NewsArticle_news_article_id",
                        column: x => x.news_article_id,
                        principalSchema: "news",
                        principalTable: "NewsArticle",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RefreshToken",
                schema: "auth",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshToken", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshToken_User_UserId",
                        column: x => x.UserId,
                        principalSchema: "auth",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "ai",
                table: "AiModeConfig",
                columns: new[] { "id", "min_body_text_length_for_ai", "mode", "news_model", "previous_mode", "updated_at", "updated_by" },
                values: new object[] { 1, 500, "Off", "gemini-2.5-pro", null, new DateTimeOffset(new DateTime(2026, 5, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system" });

            migrationBuilder.InsertData(
                schema: "ai",
                table: "AiPrompt",
                columns: new[] { "id", "content_type", "prompt_template", "updated_at", "updated_by" },
                values: new object[,]
                {
                    { 1, "news", "Eres un analista experto en FIBRAs mexicanas (Fideicomisos de Inversión en Bienes Raíces) con amplio conocimiento del mercado inmobiliario y bursátil de México.\nTítulo: {title}\n{snippet_section}\n{body_section}\nIncluye: el hecho central, su relevancia para el sector de FIBRAs o bienes raíces en México, los datos más materiales del artículo cuando existan, y una lectura analítica breve para el inversionista.\nSi el artículo contiene cifras, fechas, montos, porcentajes, dividendos, emisiones, ocupación, adquisiciones o guidance, intégralos en el resumen.\nNo escribas menos de 5 oraciones si el cuerpo del artículo está disponible. Responde solo con el resumen, sin preámbulos.", new DateTimeOffset(new DateTime(2026, 5, 23, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system" },
                    { 2, "kpi_extraction", "Eres un analista senior especializado en FIBRAs mexicanas, estados financieros, reportes trimestrales y anuales, métricas operativas inmobiliarias y análisis bursátil en México.\n\nTu tarea es leer un reporte financiero en formato markdown, extraer KPIs clave y devolver ÚNICAMENTE un objeto JSON válido, sin texto adicional y sin bloques de código.\n\nFormato de salida obligatorio:\n{\n  \"capRate\": <número decimal o null>,\n  \"capRateNote\": \"<de dónde proviene, cómo se calculó o por qué es null>\",\n  \"navPerCbfi\": <número decimal o null>,\n  \"navPerCbfiNote\": \"<nota breve>\",\n  \"ltv\": <número decimal o null>,\n  \"ltvNote\": \"<nota breve>\",\n  \"noiMargin\": <número decimal o null>,\n  \"noiMarginNote\": \"<nota breve>\",\n  \"ffoMargin\": <número decimal o null>,\n  \"ffoMarginNote\": \"<nota breve>\",\n  \"quarterlyDistribution\": <número decimal o null>,\n  \"quarterlyDistributionNote\": \"<nota breve>\",\n  \"operationalSignals\": [\"<señal operativa 1>\", \"<señal operativa 2>\"],\n  \"financialSignals\": [\"<señal financiera 1>\", \"<señal financiera 2>\"],\n  \"riskFlags\": [\"<riesgo 1>\", \"<riesgo 2>\"],\n  \"summaryMarkdown\": \"<resumen analítico en markdown>\",\n  \"investorTakeaway\": \"<conclusión breve y directa para inversionistas>\",\n  \"extractionNotes\": \"<observaciones generales sobre calidad, consistencia o limitaciones de la extracción>\"\n}\n\nReglas de extracción:\n- Devuelve solo JSON válido.\n- Todos los valores numéricos deben ser números puros, sin comas de miles, sin símbolo de moneda y sin signo de porcentaje.\n- capRate, ltv, noiMargin y ffoMargin deben expresarse como decimal. Ejemplo: 8.5% = 0.085.\n- quarterlyDistribution debe ser la distribución por CBFI en pesos.\n- navPerCbfi debe ser el NAV por CBFI en pesos.\n- Si un KPI está explícitamente reportado, úsalo.\n- Si no está explícito pero puede calcularse con certeza a partir de cifras del reporte, calcúlalo e indícalo brevemente en la nota.\n- Si no puede determinarse con suficiente certeza, devuelve null.\n- No inventes datos, no asumas cifras faltantes y no uses conocimiento externo al reporte.\n- Si hay cifras ambiguas o contradictorias, prioriza el dato consolidado o más explícito y explícalo en extractionNotes.\n- Las notas de KPI deben ser concisas, máximo 2 oraciones.\n- operationalSignals, financialSignals y riskFlags deben contener frases breves y útiles; si no aplica, devuelve arreglos vacíos [].\n\nInstrucciones para summaryMarkdown:\n- Debe estar en español.\n- Debe tener entre 3 y 5 párrafos cortos.\n- Puede usar markdown simple: párrafos, **negritas** y listas cortas con guion.\n- No uses tablas, HTML, encabezados tipo #, ni bloques de código.\n- No te limites a repetir números: interpreta el desempeño.\n- Debe cubrir, cuando exista evidencia suficiente: evolución operativa, rentabilidad y generación de flujo, balance y apalancamiento, sostenibilidad de la distribución, fortalezas y riesgos.\n- Si hay comparativos trimestrales o anuales, incorpóralos.\n- Si faltan datos para sostener una conclusión fuerte, dilo explícitamente.\n- Señala con **negritas** el principal factor positivo y el principal foco de riesgo si se pueden identificar.\n\nCriterios analíticos:\n- Evalúa crecimiento o contracción de ingresos, NOI, FFO, AFFO o EBITDA si están disponibles.\n- Evalúa márgenes y eficiencia operativa.\n- Evalúa señales sobre ocupación, rentas, spreads, renovaciones, diversificación, cobranza o desempeño por segmento si el reporte lo permite.\n- Evalúa deuda, LTV, perfil de vencimientos, costo financiero, tasa fija/variable, liquidez y refinanciamiento si existen.\n- Evalúa la calidad y sostenibilidad de la distribución, no solo su monto.\n- Mantén tono profesional, sobrio y orientado a inversionistas.\n\nReporte:\n{markdown_content}", new DateTimeOffset(new DateTime(2026, 5, 26, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system" },
                    { 3, "news_analysis", "Eres un analista experto en FIBRAs mexicanas (Fideicomisos de Inversión en Bienes Raíces) con amplio conocimiento del mercado inmobiliario y bursátil de México.\n\nTu tarea es analizar la siguiente noticia y devolver ÚNICAMENTE un objeto JSON con la estructura indicada. No uses bloques de código markdown. No incluyas texto antes o después del JSON.\n\nTítulo: {title}\n{snippet_section}\n{body_section}\n\nDevuelve exactamente este JSON (sin texto adicional):\n{\n  \"isRelevant\": true,\n  \"relevanceReason\": \"string | null\",\n  \"headline\": \"string | null\",\n  \"impact\": \"alto\",\n  \"sectorTags\": [\"string\"],\n  \"subsector\": \"industrial\",\n  \"affectedFibers\": [\"FUNO\"],\n  \"keyFacts\": [\"string\"],\n  \"keyFigures\": [{\"label\": \"string\", \"valueText\": \"string\", \"importance\": \"alta\"}],\n  \"summaryMarkdown\": \"string | null\",\n  \"investorTakeaway\": \"string | null\",\n  \"confidence\": 0.85,\n  \"extractionNotes\": \"string | null\"\n}\n\nReglas obligatorias:\n- Responde ÚNICAMENTE con el JSON. No uses bloques de código markdown (no uses ```json).\n- impact debe ser exactamente uno de: alto, medio, bajo, nulo.\n- subsector debe ser exactamente uno de: industrial, oficinas, comercial, hotelero, residencial, logistico, educativo, salud, mixto, otro, o null.\n- affectedFibers debe contener solo tickers de FIBRAs mexicanas reales (FUNO, FIBRAMQ, FIBRAPL, TERRA, FMTY, DANHOS, FNOVA, FIHO, HGLSI, etc.) que se mencionen explícitamente en el artículo.\n- Si un campo no aplica, usa null para strings/objetos o [] para arrays.\n- confidence es un número decimal entre 0 y 1 que refleja tu certeza de extracción, no la calidad de la noticia.\n- Si isRelevant es false, impact debe ser \"nulo\".\n- keyFigures solo debe incluir cifras explícitas y concretas del artículo (montos, porcentajes, fechas financieras).", new DateTimeOffset(new DateTime(2026, 5, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system" }
                });

            migrationBuilder.InsertData(
                schema: "ai",
                table: "AiProviderConfig",
                columns: new[] { "id", "model_id", "provider", "updated_at", "updated_by" },
                values: new object[] { 1, "gemini-2.5-flash", "Gemini", new DateTimeOffset(new DateTime(2026, 5, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system" });

            migrationBuilder.InsertData(
                schema: "news",
                table: "BlocklistTerm",
                columns: new[] { "id", "created_at", "term" },
                values: new object[,]
                {
                    { new Guid("01198b97-3348-19ce-d5a6-92bbde33ebeb"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "fibra de carbono" },
                    { new Guid("18288ba5-5469-5321-b116-12f462def773"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "fibra textil" },
                    { new Guid("33addac2-f65d-b0e1-616f-0b72e7aad6c3"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "fibra alimentaria" },
                    { new Guid("3c3dbbea-5d57-c2e7-7395-0cef7bf9fba9"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "fibra dietetica" },
                    { new Guid("93629cda-318e-f05f-94a4-49e87f7a07b3"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "fibra dietética" },
                    { new Guid("c0166b06-2573-7815-6d80-6e448212bc1a"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "fibra muscular" },
                    { new Guid("c2c16834-61c4-a329-acb9-3f601fe94781"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "fibra de vidrio" },
                    { new Guid("e05324fb-2c04-9389-41f6-a1dce885eba2"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "fibra optica" },
                    { new Guid("e1e5c21c-7dcc-13b9-c5b2-065e293260cd"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "internet fibra" },
                    { new Guid("e42a6f48-fd39-9fcb-9cdd-b32936d4151a"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "fibra óptica" }
                });

            migrationBuilder.InsertData(
                schema: "market",
                table: "Distribution",
                columns: new[] { "id", "amount_per_unit", "captured_at", "currency", "fibra_id", "payment_date", "source", "ticker" },
                values: new object[,]
                {
                    { new Guid("00777343-ebe9-68f0-c0d9-1a972125742f"), 0.3180m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("7347fc56-b1d8-1853-7712-ac4dc204564c"), new DateOnly(2021, 9, 13), "seed", "FUNO11" },
                    { new Guid("0137f687-5666-c94c-caeb-377119816221"), 0.1950m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("5d911406-8042-d1db-90b0-6c5aebb882bf"), new DateOnly(2021, 6, 14), "seed", "DANHOS13" },
                    { new Guid("0712aff6-cf0f-55dd-a6dc-090e83c5233a"), 0.3720m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("7347fc56-b1d8-1853-7712-ac4dc204564c"), new DateOnly(2025, 6, 16), "seed", "FUNO11" },
                    { new Guid("08c47232-2945-58d3-e018-0c687cf9987d"), 0.1750m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("894a2120-2ad3-d3f4-44fd-0d0c423b849f"), new DateOnly(2024, 6, 17), "seed", "TERRA13" },
                    { new Guid("0bbbb154-b518-66f0-6637-9b15850397a8"), 0.3100m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("7347fc56-b1d8-1853-7712-ac4dc204564c"), new DateOnly(2021, 3, 15), "seed", "FUNO11" },
                    { new Guid("12754f3b-2021-99c6-dc06-a413948bc0fd"), 0.1450m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("2799e174-1233-93ba-87ce-ade15c0c4010"), new DateOnly(2024, 12, 16), "seed", "FIBRAMQ12" },
                    { new Guid("134f77eb-4afc-4807-b139-36a64791d83f"), 0.3380m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("7347fc56-b1d8-1853-7712-ac4dc204564c"), new DateOnly(2022, 6, 13), "seed", "FUNO11" },
                    { new Guid("1864e970-626f-abaa-0c24-77b72ff123bd"), 0.3680m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("7347fc56-b1d8-1853-7712-ac4dc204564c"), new DateOnly(2024, 3, 18), "seed", "FUNO11" },
                    { new Guid("1c20f71e-193b-f476-88b7-b056789186a4"), 0.1620m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("894a2120-2ad3-d3f4-44fd-0d0c423b849f"), new DateOnly(2022, 9, 12), "seed", "TERRA13" },
                    { new Guid("22ab0fa4-a568-68ec-cf8f-04d474d88988"), 0.1900m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("5d911406-8042-d1db-90b0-6c5aebb882bf"), new DateOnly(2021, 3, 15), "seed", "DANHOS13" },
                    { new Guid("24260c3b-ef78-0968-6f3c-efe375b565e3"), 0.1980m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("5d911406-8042-d1db-90b0-6c5aebb882bf"), new DateOnly(2021, 9, 13), "seed", "DANHOS13" },
                    { new Guid("249e2535-408e-e0db-90e4-602ea74bb40d"), 0.3150m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("7347fc56-b1d8-1853-7712-ac4dc204564c"), new DateOnly(2021, 6, 14), "seed", "FUNO11" },
                    { new Guid("2c2c6ef6-29bb-468b-4a60-b8f1a5aad69b"), 0.3300m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("7347fc56-b1d8-1853-7712-ac4dc204564c"), new DateOnly(2022, 3, 14), "seed", "FUNO11" },
                    { new Guid("2ca1aa7d-0958-4583-040a-646cf586ad96"), 0.3460m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("7347fc56-b1d8-1853-7712-ac4dc204564c"), new DateOnly(2022, 12, 12), "seed", "FUNO11" },
                    { new Guid("2d5fcd74-962e-c0d6-f1f6-b8b25a0009b9"), 0.1300m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("2799e174-1233-93ba-87ce-ade15c0c4010"), new DateOnly(2023, 6, 12), "seed", "FIBRAMQ12" },
                    { new Guid("3641773c-175e-3176-5af6-d3c9cc43fee0"), 0.1640m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("894a2120-2ad3-d3f4-44fd-0d0c423b849f"), new DateOnly(2022, 12, 12), "seed", "TERRA13" },
                    { new Guid("3ad4a8a2-8e06-5923-aedd-25a49939ea84"), 0.2150m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("5d911406-8042-d1db-90b0-6c5aebb882bf"), new DateOnly(2025, 3, 17), "seed", "DANHOS13" },
                    { new Guid("3ccdc1eb-c781-6972-f073-fdd788cdb8b4"), 0.3200m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("7347fc56-b1d8-1853-7712-ac4dc204564c"), new DateOnly(2021, 12, 13), "seed", "FUNO11" },
                    { new Guid("3cd135c0-6ef9-07d2-c58c-9f23c8f6d969"), 0.2250m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("5d911406-8042-d1db-90b0-6c5aebb882bf"), new DateOnly(2024, 12, 16), "seed", "DANHOS13" },
                    { new Guid("3e264b5f-5643-58fa-be69-21d0be470954"), 0.1100m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("2799e174-1233-93ba-87ce-ade15c0c4010"), new DateOnly(2021, 6, 14), "seed", "FIBRAMQ12" },
                    { new Guid("3e2dbc4b-6422-5059-49c3-9238a184ee2e"), 0.3420m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("7347fc56-b1d8-1853-7712-ac4dc204564c"), new DateOnly(2022, 9, 12), "seed", "FUNO11" },
                    { new Guid("3ea58762-31cb-1687-2b59-931ba80860d8"), 0.2160m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("5d911406-8042-d1db-90b0-6c5aebb882bf"), new DateOnly(2023, 9, 11), "seed", "DANHOS13" },
                    { new Guid("3fce78aa-5887-0f04-a1f3-ca1e7fcb4803"), 0.1520m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("2799e174-1233-93ba-87ce-ade15c0c4010"), new DateOnly(2025, 12, 15), "seed", "FIBRAMQ12" },
                    { new Guid("49a7be56-9ac0-c735-7674-f1a584e26f70"), 0.3800m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("7347fc56-b1d8-1853-7712-ac4dc204564c"), new DateOnly(2024, 12, 16), "seed", "FUNO11" },
                    { new Guid("4f56248a-fe67-33bf-4033-6df6a608ca2b"), 0.1820m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("894a2120-2ad3-d3f4-44fd-0d0c423b849f"), new DateOnly(2025, 12, 15), "seed", "TERRA13" },
                    { new Guid("520331f0-8890-6215-eaa3-5fbca55ed140"), 0.1730m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("894a2120-2ad3-d3f4-44fd-0d0c423b849f"), new DateOnly(2024, 3, 18), "seed", "TERRA13" },
                    { new Guid("5233cae1-9172-0ef6-7c00-6ad251574aaf"), 0.1700m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("894a2120-2ad3-d3f4-44fd-0d0c423b849f"), new DateOnly(2023, 9, 11), "seed", "TERRA13" },
                    { new Guid("5278bfd8-b8a0-ea29-bcf7-0ba402d7e924"), 0.3720m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("7347fc56-b1d8-1853-7712-ac4dc204564c"), new DateOnly(2024, 6, 17), "seed", "FUNO11" },
                    { new Guid("52cae36d-59cc-bc56-cb33-cb5429192916"), 0.1750m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("894a2120-2ad3-d3f4-44fd-0d0c423b849f"), new DateOnly(2025, 6, 16), "seed", "TERRA13" },
                    { new Guid("535537f5-bb64-3772-486d-be8ef4b150ac"), 0.1770m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("894a2120-2ad3-d3f4-44fd-0d0c423b849f"), new DateOnly(2024, 9, 16), "seed", "TERRA13" },
                    { new Guid("546667b5-d9b5-8932-82fc-c2b4c7928d98"), 0.1150m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("2799e174-1233-93ba-87ce-ade15c0c4010"), new DateOnly(2021, 12, 13), "seed", "FIBRAMQ12" },
                    { new Guid("5babaaff-7083-0205-c580-61ebd295fd10"), 0.3580m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("7347fc56-b1d8-1853-7712-ac4dc204564c"), new DateOnly(2023, 6, 12), "seed", "FUNO11" },
                    { new Guid("627ced36-69ac-91e8-7427-2824c2ef8221"), 0.1680m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("894a2120-2ad3-d3f4-44fd-0d0c423b849f"), new DateOnly(2023, 6, 12), "seed", "TERRA13" },
                    { new Guid("62b96edf-b872-2f4b-0799-674cb585264a"), 0.1350m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("2799e174-1233-93ba-87ce-ade15c0c4010"), new DateOnly(2023, 12, 11), "seed", "FIBRAMQ12" },
                    { new Guid("662c2c1c-e08a-a6df-2c83-6331d8714653"), 0.1580m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("894a2120-2ad3-d3f4-44fd-0d0c423b849f"), new DateOnly(2022, 3, 14), "seed", "TERRA13" },
                    { new Guid("6c96a62e-17ed-a8a8-6f86-1f4841b22da3"), 0.2180m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("5d911406-8042-d1db-90b0-6c5aebb882bf"), new DateOnly(2023, 12, 11), "seed", "DANHOS13" },
                    { new Guid("73cf3c5c-d05d-bdc0-e25c-15d18c654802"), 0.3500m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("7347fc56-b1d8-1853-7712-ac4dc204564c"), new DateOnly(2023, 3, 13), "seed", "FUNO11" },
                    { new Guid("7bfffaff-b5a1-0869-1ba2-a5b573b39dd3"), 0.1650m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("894a2120-2ad3-d3f4-44fd-0d0c423b849f"), new DateOnly(2023, 3, 13), "seed", "TERRA13" },
                    { new Guid("84dbaa5b-e460-a15e-2431-442e56d44f0f"), 0.3660m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("7347fc56-b1d8-1853-7712-ac4dc204564c"), new DateOnly(2023, 12, 11), "seed", "FUNO11" },
                    { new Guid("85a4ed94-b620-298c-c457-b124599fb3b7"), 0.3780m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("7347fc56-b1d8-1853-7712-ac4dc204564c"), new DateOnly(2025, 9, 15), "seed", "FUNO11" },
                    { new Guid("8671a635-0d04-2660-ae19-dac73d60fd1b"), 0.1800m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("894a2120-2ad3-d3f4-44fd-0d0c423b849f"), new DateOnly(2025, 9, 15), "seed", "TERRA13" },
                    { new Guid("900c466e-b739-8bef-221e-cff787935af6"), 0.1250m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("2799e174-1233-93ba-87ce-ade15c0c4010"), new DateOnly(2022, 12, 12), "seed", "FIBRAMQ12" },
                    { new Guid("908677ac-2c56-9fbb-dae2-b610f6c491be"), 0.1720m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("894a2120-2ad3-d3f4-44fd-0d0c423b849f"), new DateOnly(2023, 12, 11), "seed", "TERRA13" },
                    { new Guid("a2e5c597-0238-9607-c8de-c3df6d9e4f65"), 0.2020m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("5d911406-8042-d1db-90b0-6c5aebb882bf"), new DateOnly(2022, 3, 14), "seed", "DANHOS13" },
                    { new Guid("a84e7f7c-9852-61d3-fb7d-9dc07eeda3ce"), 0.2180m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("5d911406-8042-d1db-90b0-6c5aebb882bf"), new DateOnly(2024, 3, 18), "seed", "DANHOS13" },
                    { new Guid("a9944cc5-75a9-4ccb-e53c-a64c9687c104"), 0.2100m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("5d911406-8042-d1db-90b0-6c5aebb882bf"), new DateOnly(2023, 3, 13), "seed", "DANHOS13" },
                    { new Guid("aa044830-cfbd-bcf0-e79f-89c7287e8912"), 0.2200m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("5d911406-8042-d1db-90b0-6c5aebb882bf"), new DateOnly(2024, 6, 17), "seed", "DANHOS13" },
                    { new Guid("aa379a05-ccf6-dd08-09a4-a4fa5a7225fd"), 0.1530m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("894a2120-2ad3-d3f4-44fd-0d0c423b849f"), new DateOnly(2021, 9, 13), "seed", "TERRA13" },
                    { new Guid("ac5bb4bb-0719-2425-d621-5f823e9da129"), 0.2200m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("5d911406-8042-d1db-90b0-6c5aebb882bf"), new DateOnly(2025, 6, 16), "seed", "DANHOS13" },
                    { new Guid("b2b41c08-7711-6ede-c2f0-8936c7a72828"), 0.1480m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("2799e174-1233-93ba-87ce-ade15c0c4010"), new DateOnly(2025, 9, 15), "seed", "FIBRAMQ12" },
                    { new Guid("b34cecad-3540-7d2b-5ea6-8000db9c5a0b"), 0.3610m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("7347fc56-b1d8-1853-7712-ac4dc204564c"), new DateOnly(2025, 3, 17), "seed", "FUNO11" },
                    { new Guid("c36fa6a1-117f-9d25-a947-8d6ee00e30f4"), 0.3840m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("7347fc56-b1d8-1853-7712-ac4dc204564c"), new DateOnly(2025, 12, 15), "seed", "FUNO11" },
                    { new Guid("ca11459c-c62d-01d5-e73d-2398c84b57ec"), 0.2130m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("5d911406-8042-d1db-90b0-6c5aebb882bf"), new DateOnly(2023, 6, 12), "seed", "DANHOS13" },
                    { new Guid("cda927f7-2ff8-a628-515d-42ae2fac8182"), 0.1400m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("2799e174-1233-93ba-87ce-ade15c0c4010"), new DateOnly(2024, 6, 17), "seed", "FIBRAMQ12" },
                    { new Guid("cff64c3f-d949-91fc-36e8-7dc331e611ec"), 0.2080m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("5d911406-8042-d1db-90b0-6c5aebb882bf"), new DateOnly(2022, 9, 12), "seed", "DANHOS13" },
                    { new Guid("d8e7b864-fd0d-7a22-4a4a-4c46404a2544"), 0.3760m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("7347fc56-b1d8-1853-7712-ac4dc204564c"), new DateOnly(2024, 9, 16), "seed", "FUNO11" },
                    { new Guid("dd6a1e53-d748-bb6a-a5bc-1742bcb8d5fb"), 0.2050m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("5d911406-8042-d1db-90b0-6c5aebb882bf"), new DateOnly(2022, 6, 13), "seed", "DANHOS13" },
                    { new Guid("de21e159-a5ad-0c4f-eb7e-8bbb833a9cae"), 0.1790m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("894a2120-2ad3-d3f4-44fd-0d0c423b849f"), new DateOnly(2024, 12, 16), "seed", "TERRA13" },
                    { new Guid("e1815ccd-c2de-c8d3-0a78-46348657eab0"), 0.1500m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("894a2120-2ad3-d3f4-44fd-0d0c423b849f"), new DateOnly(2021, 6, 14), "seed", "TERRA13" },
                    { new Guid("e2877d0a-5fa4-1b33-ec8f-1740601a3c3d"), 0.2220m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("5d911406-8042-d1db-90b0-6c5aebb882bf"), new DateOnly(2024, 9, 16), "seed", "DANHOS13" },
                    { new Guid("e95c49b8-0da1-0cdb-9698-4ffee01cb629"), 0.2250m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("5d911406-8042-d1db-90b0-6c5aebb882bf"), new DateOnly(2025, 9, 15), "seed", "DANHOS13" },
                    { new Guid("ed7b344a-e13d-5c8c-0159-09ccbf87835c"), 0.1550m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("894a2120-2ad3-d3f4-44fd-0d0c423b849f"), new DateOnly(2021, 12, 13), "seed", "TERRA13" },
                    { new Guid("ef00f014-3a95-0e27-a5fa-76badf3c49c5"), 0.2000m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("5d911406-8042-d1db-90b0-6c5aebb882bf"), new DateOnly(2021, 12, 13), "seed", "DANHOS13" },
                    { new Guid("f0d429c6-9ba2-ca53-b252-1162a7e02a99"), 0.1600m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("894a2120-2ad3-d3f4-44fd-0d0c423b849f"), new DateOnly(2022, 6, 13), "seed", "TERRA13" },
                    { new Guid("f1a4ecac-f36b-e733-a7fc-e5ed6053e6de"), 0.3620m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("7347fc56-b1d8-1853-7712-ac4dc204564c"), new DateOnly(2023, 9, 11), "seed", "FUNO11" },
                    { new Guid("f2af8992-b244-6d43-9610-2f69c170477f"), 0.2300m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("5d911406-8042-d1db-90b0-6c5aebb882bf"), new DateOnly(2025, 12, 15), "seed", "DANHOS13" },
                    { new Guid("f5c73975-4c45-b1fe-bb1c-e9e913f488a4"), 0.2100m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("5d911406-8042-d1db-90b0-6c5aebb882bf"), new DateOnly(2022, 12, 12), "seed", "DANHOS13" },
                    { new Guid("ffa46d39-4893-5a33-b8c8-0417d48b73fb"), 0.1200m, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", new Guid("2799e174-1233-93ba-87ce-ade15c0c4010"), new DateOnly(2022, 6, 13), "seed", "FIBRAMQ12" }
                });

            migrationBuilder.InsertData(
                schema: "ops",
                table: "EditorialPage",
                columns: new[] { "slug", "content", "display_order", "title", "updated_at" },
                values: new object[,]
                {
                    { "como-se-estructuran", "## La arquitectura jurídica de una FIBRA\n\nUna FIBRA existe como un **fideicomiso irrevocable**. El fideicomiso es el propietario legal de los inmuebles y el emisor de los CBFIs.\n\n### Actores principales\n\n- **Fideicomitentes**: aportan inmuebles o efectivo al vehículo\n- **Fiduciario**: institución financiera que mantiene la titularidad legal\n- **Fideicomisarios**: tenedores de CBFIs\n- **Administrador**: opera inmuebles, cobra rentas y ejecuta la estrategia\n- **Comité Técnico**: órgano de gobierno equivalente a un consejo\n\n### Asamblea de Tenedores\n\nEs el máximo órgano de decisión. Cada CBFI representa derechos económicos y, en los supuestos aplicables, derechos de voto.\n\n### Reguladores relevantes\n\n| Autoridad | Función |\n|---|---|\n| **CNBV** | Supervisión del mercado y obligaciones de revelación |\n| **BMV / BIVA** | Listado y negociación secundaria |\n| **SAT** | Vigilancia del cumplimiento del régimen fiscal especial |\n\n### Distribución mínima del 95%\n\nEl requisito de distribuir al menos el **95% del resultado fiscal** es uno de los rasgos más distintivos del instrumento. Esto obliga a que gran parte del flujo llegue a los inversionistas de forma periódica.\n\n### FIBRA inmobiliaria vs FIBRA E\n\nLas FIBRAs inmobiliarias invierten en bienes raíces para arrendamiento. Las **FIBRA E** monetizan flujos o activos de energía e infraestructura, pero comparten la lógica de vehículo listado y tratamiento fiscal especializado.", 3, "¿Cómo se estructuran?", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { "historia", "## De la regulación al mercado\n\nLas reglas iniciales para este tipo de instrumento surgieron en 2004, pero fue hasta **2011** cuando el mercado tomó forma con la salida a bolsa de **FIBRA Uno (FUNO11)**.\n\n### 18 de marzo de 2011: el primer CBFI\n\nFUNO11 realizó su Oferta Pública Inicial con un portafolio inicial de 16 inmuebles y un precio de salida de **19.50 pesos por CBFI**. Ese evento marcó el nacimiento formal del mercado de FIBRAs en México.\n\n### Expansión 2012–2014\n\nDespués de FUNO llegaron vehículos especializados en hotelería, industria y retail. También comenzaron adquisiciones de gran escala que consolidaron al instrumento como una fuente relevante de capital inmobiliario.\n\n### Consolidación 2015–2019\n\nEn 2015 se formalizó el índice **S&P/BMV FIBRAS**, y ese mismo año surgió el concepto de **FIBRA E** para energía e infraestructura. Esto amplió el universo de activos securitizables bajo la misma lógica de distribuciones recurrentes.\n\n### Prueba de estrés: pandemia 2020\n\nLa pandemia afectó sobre todo a los segmentos comercial y hotelero. Las industriales resistieron mejor y posteriormente se beneficiaron del nearshoring.\n\n### Mercado maduro 2021–2025\n\nPara 2025 el sector acumulaba cientos de miles de millones de pesos en activos administrados, miles de propiedades y una ocupación promedio robusta. El instrumento dejó de ser experimental y pasó a ser una categoría establecida dentro del mercado público mexicano.", 2, "Historia", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { "por-que-invertir", "## Razones para considerar FIBRAs en un portafolio\n\n### Acceso al inmobiliario institucional\n\nComprar directamente un inmueble institucional exige mucho capital, poca diversificación y gestión operativa. Las FIBRAs permiten exposición a ese mercado con tickets significativamente menores.\n\n### Liquidez bursátil\n\nUn inmueble directo puede tardar meses en venderse. Un CBFI puede comprarse o venderse en bolsa en tiempo real durante la sesión del mercado.\n\n### Distribuciones periódicas\n\nEl sector ha sido atractivo históricamente por su capacidad de entregar distribuciones recurrentes. El rendimiento exacto cambia por emisora, tasas y valuaciones, pero el marco obliga a repartir flujo.\n\n### Diversificación y gestión profesional\n\nCon un solo instrumento puedes acceder a múltiples inmuebles, ciudades, inquilinos y segmentos. Además, la administración corre por cuenta de equipos especializados.\n\n---\n\n## Riesgos reales\n\nNinguna tesis de inversión está completa sin riesgos claros:\n\n- **Tasas de interés**: cuando suben, los CBFIs suelen presionarse\n- **Vacancia**: menos ocupación implica menos flujo\n- **Tipo de cambio**: relevante en portafolios dolarizados\n- **Dilución**: nuevas emisiones pueden reducir rendimiento por certificado\n- **Gobierno corporativo**: la alineación del administrador importa\n\nEl análisis debe considerar tanto yield como calidad del portafolio, deuda, ocupación y disciplina de capital.", 4, "Por qué invertir", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { "que-son-las-fibras", "## Fideicomisos de Inversión en Bienes Raíces\n\nLas FIBRAs son vehículos de inversión que permiten a cualquier persona participar en el mercado inmobiliario institucional mexicano sin necesidad de comprar propiedades directamente. Son fideicomisos constituidos conforme a la Ley General de Títulos y Operaciones de Crédito, con régimen fiscal especial regulado en los artículos 187 y 188 de la Ley del Impuesto Sobre la Renta (LISR).\n\n## ¿Cómo funciona un fideicomiso FIBRA?\n\nEl fideicomiso adquiere y administra inmuebles que generan renta: centros comerciales, parques industriales, hoteles, oficinas corporativas y almacenes logísticos. Los inversionistas compran **CBFIs** (Certificados Bursátiles Fiduciarios Inmobiliarios) en la BMV o en la BIVA. Las rentas cobradas a los inquilinos, menos gastos operativos y deuda, se distribuyen entre los tenedores en proporción a su participación.\n\nPara calificar como FIBRA y acceder al régimen fiscal preferente, el fideicomiso debe cumplir tres requisitos estructurales:\n\n- **Mínimo 70%** del patrimonio invertido en inmuebles para arrendamiento\n- Los inmuebles deben permanecer en el fideicomiso al menos **cuatro años**\n- Distribuir al menos el **95% del resultado fiscal anual**\n\n## Los CBFIs: qué son y cómo se negocian\n\nUn CBFI representa una participación en el patrimonio del fideicomiso. No es una acción tradicional; da derecho a recibir distribuciones, participar en la apreciación del portafolio y votar en asambleas de tenedores.\n\nLos tickers bursátiles siguen la convención de combinar el nombre abreviado con un número de serie: FUNO11, FMTY14, FIBRAPL14 o DANHOS13. Esto permite que inversionistas individuales entren al sector con montos pequeños comparados con la compra directa de inmuebles.\n\n## Tipos de inmuebles\n\n| Segmento | Características clave |\n|---|---|\n| **Industrial y logístico** | Naves manufactureras, parques industriales y centros de distribución |\n| **Comercial** | Centros comerciales y plazas de retail |\n| **Corporativo** | Oficinas clase A en principales ciudades |\n| **Hotelero** | Hoteles de negocios y resorts |\n| **Mixto** | Combinación de varios segmentos |\n\n## ¿En qué se diferencian de los REITs de EE.UU.?\n\nLas FIBRAs son el equivalente mexicano de los REITs. Comparten la obligación de distribuir la mayor parte del flujo, pero en México operan como fideicomisos y cuentan con un tratamiento fiscal local específico.\n\n## ¿Quién puede invertir?\n\nCualquier persona física o moral con acceso a una casa de bolsa o plataforma de inversión puede comprar CBFIs. También participan Afores, aseguradoras y fondos institucionales, lo que aporta profundidad al mercado.", 1, "¿Qué son las FIBRAs?", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) },
                    { "regimen-fiscal", "## Tratamiento fiscal general de las FIBRAs\n\n> Esta sección es educativa y no sustituye asesoría fiscal profesional.\n\n### Transparencia fiscal del fideicomiso\n\nEl fideicomiso determina un resultado fiscal y lo distribuye a los tenedores. El tratamiento final depende del tipo de inversionista.\n\n### Personas físicas residentes en México\n\nEn términos generales, las distribuciones provenientes del resultado fiscal están sujetas a retención provisional. Una parte puede corresponder a reembolso de capital, con tratamiento distinto al momento del cobro.\n\n### Ganancia de capital en bolsa\n\nUno de los atributos más relevantes es que, bajo las condiciones aplicables, la ganancia por venta bursátil de CBFIs puede tener un tratamiento fiscal más favorable que otros instrumentos patrimoniales.\n\n### Personas morales, fondos y extranjeros\n\nEl efecto fiscal cambia según si el inversionista es persona moral, fondo de pensiones o residente en el extranjero. Siempre conviene revisar el régimen concreto y, si aplica, tratados internacionales.\n\n### Resumen práctico\n\n| Tipo de inversionista | Distribuciones | Venta bursátil |\n|---|---|---|\n| Persona física residente | Retención provisional y acumulación anual | Tratamiento fiscal específico del régimen FIBRA |\n| Persona moral residente | Acumulación corporativa | Depende del régimen aplicable |\n| Fondos elegibles | Beneficios particulares bajo reglas específicas | Revisar reglas del vehículo |\n| Extranjero | Sujeto a retenciones o tratado | Depende de LISR y tratado |\n\nAntes de invertir, conviene entender si tu tesis depende del flujo distribuido, de la apreciación de capital o de ambas.", 5, "Régimen fiscal", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) }
                });

            migrationBuilder.InsertData(
                schema: "catalog",
                table: "Fibra",
                columns: new[] { "Id", "created_at", "currency", "description", "full_name", "investor_url", "market", "name_variants", "reports_url", "sector", "short_name", "site_url", "state", "ticker", "yahoo_ticker" },
                values: new object[,]
                {
                    { new Guid("055c422a-c2df-ec0f-ab61-2b5c3ede52c2"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", null, "FHipo", "https://fhipo.com/es/kit-para-inversionistas/", "BIVA", "[\"FHipo\",\"Fideicomiso Hipotecario\",\"FHIPO\",\"FHIPO14\"]", "https://fhipo.com/es/reportes-trimestrales/", "Hipotecario", "FHipo", "https://fhipo.com/es/", "Active", "FHIPO14", "FHIPO14.MX" },
                    { new Guid("132f2dbd-1a77-3ca5-01eb-c65b7a03b14b"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", null, "Fibra Upsite", null, "BMV", "[\"Fibra Upsite\",\"Upsite\",\"FIBRAUP\"]", null, "Industrial", "Upsite", "https://fibra-upsite.com", "Active", "FIBRAUP18", "FIBRAUP18.MX" },
                    { new Guid("15d3465f-5ff4-a84c-c883-1dd381fd22f0"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", null, "CFE Fibra E", "https://cfecapital.com.mx/inversionistas", "BMV/BIVA", "[\"CFE Fibra E\",\"FCFE\",\"FCFE18\"]", "https://cfecapital.com.mx/inversionistas", "Infraestructura", "CFE Fibra E", "https://cfecapital.com.mx", "Active", "FCFE18", "FCFE18.MX" },
                    { new Guid("17e765b2-df1e-6842-3dcf-ec7506563c89"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", null, "Fibra Storage", null, "BMV", "[\"Fibra Storage\",\"Storage\",\"STORAGE18\",\"U-Storage\"]", "https://fibrastorage.com/repositorio-informacion-financiera/", "Autoalmacenaje", "Fibra Storage", "https://fibrastorage.com", "Active", "STORAGE18", "STORAGE18.MX" },
                    { new Guid("2799e174-1233-93ba-87ce-ade15c0c4010"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", null, "Fibra Macquarie", "https://fibramacquarie.com.mx/ri", "BMV", "[\"Fibra MQ\",\"Macquarie\",\"FIBRAMQ\"]", null, "Industrial", "FibraMQ", "https://fibramacquarie.com.mx", "Active", "FIBRAMQ12", "FIBRAMQ12.MX" },
                    { new Guid("2f25d292-5a8d-a262-1cdc-093621c7471c"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", null, "Fibra Next", "https://fibranext.mx/investors", "BMV", "[\"Fibra Next\",\"NEXT\",\"NEXT25\"]", "https://fibranext.mx/investors", "Industrial", "Fibra Next", "https://fibranext.mx", "Active", "NEXT25", "NEXT25.MX" },
                    { new Guid("3129ee4f-d156-04c8-a03f-42bdc468ff27"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", null, "Fibra Educa", "https://www.fibraeduca.com/invertir", "BMV", "[\"Fibra Educa\",\"EDUCA\",\"EDUCA18\"]", "https://www.fibraeduca.com/reportes-financieros", "Educativo", "Fibra Educa", "https://www.fibraeduca.com", "Active", "EDUCA18", "EDUCA18.MX" },
                    { new Guid("32377b6d-9244-a715-0279-2660cc6b62a5"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", null, "Fibra Prologis", "https://www.fibraprologis.com/en-US/investors", "BMV", "[\"Fibra Prologis\",\"Prologis\",\"FIBRAPL\"]", "https://www.fibraprologis.com/en-US/investors/financial-results", "Industrial", "Prologis", "https://www.fibraprologis.com/en-US", "Active", "FIBRAPL14", "FIBRAPL14.MX" },
                    { new Guid("32418186-9e2c-942b-8f4a-1e61388760a4"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", null, "Fibra Plus", null, "BMV", "[\"Fibra Plus\",\"FPLUS\",\"FPLUS16\"]", "https://www.fibraplus.mx/es/financiera/trimestrales", "Diversificado", "Fibra Plus", "https://www.fibraplus.mx", "Active", "FPLUS16", "FPLUS16.MX" },
                    { new Guid("38a237f5-9065-f4ec-7b19-c116a9642c6f"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", null, "Fibra Inn", null, "BMV", "[\"Fibra Inn\",\"FINN\"]", null, "Hotelero", "Fibra Inn", "https://fibrainn.com.mx", "Active", "FINN13", "FINN13.MX" },
                    { new Guid("5d911406-8042-d1db-90b0-6c5aebb882bf"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", null, "Fibra Danhos", "https://fibradanhos.com.mx/ri", "BMV", "[\"Danhos\",\"DANHOS\"]", null, "Comercial", "Danhos", "https://fibradanhos.com.mx", "Active", "DANHOS13", "DANHOS13.MX" },
                    { new Guid("6cbe26c2-2ed0-6df0-6242-1c78d011e9fc"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", null, "Fibra Hotel", null, "BMV", "[\"Fibra Hotel\",\"FIHO\"]", null, "Hotelero", "Fibra Hotel", "https://fibrahotel.com", "Active", "FIHO12", "FIHO12.MX" },
                    { new Guid("7347fc56-b1d8-1853-7712-ac4dc204564c"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", null, "Fibra Uno", "https://fibra.uno/inversionistas", "BMV", "[\"Fibra Uno\",\"FUNO\"]", null, "Diversificado", "Fibra Uno", "https://fibra.uno", "Active", "FUNO11", "FUNO11.MX" },
                    { new Guid("7882f6d3-304f-5de6-2211-60508c2021c6"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", null, "Fibra Monterrey", "https://fibramty.com/inversionistas", "BMV", "[\"Fibra Monterrey\",\"FibraMTY\",\"FMTY\"]", null, "Industrial", "Fibra MTY", "https://fibramty.com", "Active", "FMTY14", "FMTY14.MX" },
                    { new Guid("894a2120-2ad3-d3f4-44fd-0d0c423b849f"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", null, "Fibra Terra", null, "BMV", "[\"Fibra Terra\",\"TERRA\"]", null, "Industrial", "Terra", "https://fibra-terra.com", "Active", "TERRA13", "TERRA13.MX" },
                    { new Guid("8d7ad206-8591-fd28-f33f-d0b887817b5c"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", null, "Fibra Nova", "https://www.fibra-nova.com/inversionistas/como-invertir", "BIVA", "[\"Fibra Nova\",\"FNOVA\",\"FNOVA17\"]", "https://www.fibra-nova.com/inversionistas/reportes-trimestrales", "Diversificado", "Fibra Nova", "https://www.fibra-nova.com", "Active", "FNOVA17", "FNOVA17.MX" },
                    { new Guid("933f9202-f943-0342-0e05-5cfd283a5bbc"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", null, "Fibra Shop", "https://fibrashop.mx/contacto/", "BMV", "[\"Fibra Shop\",\"FSHOP\",\"FSHOP13\"]", "https://fibrashop.mx/informes-financieros/", "Comercial", "Fibra Shop", "https://fibrashop.mx", "Active", "FSHOP13", "FSHOP13.MX" },
                    { new Guid("9797ad53-1324-9a81-6102-992b9f07e92c"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", null, "Fibra SOMA", null, "BIVA", "[\"Fibra SOMA\",\"SOMA\",\"SOMA21\"]", null, "Comercial", "Fibra SOMA", "https://fibrasoma.group", "Active", "SOMA21", "SOMA21.MX" },
                    { new Guid("ae26cf6f-b4dc-be5a-3355-f4f4a9165499"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", null, "Fibra Vesta", "https://fibravesta.com/ri", "BMV", "[\"Fibra Vesta\",\"VESTA\"]", null, "Industrial", "Vesta", "https://fibravesta.com", "Active", "VESTA15", "VESTA.MX" },
                    { new Guid("d96dd899-9a7e-3c95-a9ba-bbf62577a241"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "MXN", null, "Fibra Hotel City Express", null, "BMV", "[\"Hotel City Express\",\"HCITY\",\"HC\"]", null, "Hotelero", "HC", "https://hcity.com.mx", "Active", "HCITY17", "HCITY.MX" }
                });

            migrationBuilder.InsertData(
                schema: "ops",
                table: "OperationalConfig",
                columns: new[] { "id", "avg_periods", "commission_factor", "fibra_news_months", "fundamentals_cadence_minutes", "news_cadence_minutes", "updated_at", "updated_by" },
                values: new object[] { 1, 4, 0.006m, 15, 1440, 1440, new DateTimeOffset(new DateTime(2026, 5, 24, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "system" });

            migrationBuilder.CreateIndex(
                name: "IX_AiCallLog_Operation_CreatedAt",
                schema: "jobs",
                table: "AiCallLog",
                columns: new[] { "operation", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_AiCallLog_Provider_CreatedAt",
                schema: "jobs",
                table: "AiCallLog",
                columns: new[] { "provider", "created_at" });

            migrationBuilder.CreateIndex(
                name: "UQ_AiPrompt_ContentType",
                schema: "ai",
                table: "AiPrompt",
                column: "content_type",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BlocklistTerm_Term",
                schema: "news",
                table: "BlocklistTerm",
                column: "term",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConfigAuditLog_changed_at",
                schema: "ops",
                table: "ConfigAuditLog",
                column: "changed_at");

            migrationBuilder.CreateIndex(
                name: "UX_DailySnapshot_FibraId_Date",
                schema: "market",
                table: "DailySnapshot",
                columns: new[] { "fibra_id", "date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UIX_Distribution_FibraId_PaymentDate",
                schema: "market",
                table: "Distribution",
                columns: new[] { "fibra_id", "payment_date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Fibra_Ticker",
                schema: "catalog",
                table: "Fibra",
                column: "ticker",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FundamentalRecord_FibraId_Period_Status",
                schema: "fundamentals",
                table: "FundamentalRecord",
                columns: new[] { "fibra_id", "period", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_FundamentalSourceManifest_FibraId_Period_ReportType",
                schema: "fundamentals",
                table: "FundamentalSourceManifest",
                columns: new[] { "fibra_id", "period", "report_type" });

            migrationBuilder.CreateIndex(
                name: "UX_FundamentalSourceManifest_SourceName_PackageUrl",
                schema: "fundamentals",
                table: "FundamentalSourceManifest",
                columns: new[] { "source_name", "package_url" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NewsArticle_TitleNormalized_CapturedAt",
                schema: "news",
                table: "NewsArticle",
                columns: new[] { "title_normalized", "captured_at" });

            migrationBuilder.CreateIndex(
                name: "IX_NewsArticle_Url",
                schema: "news",
                table: "NewsArticle",
                column: "url",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NewsArticleFibra_FibraId",
                schema: "news",
                table: "NewsArticleFibra",
                column: "fibra_id");

            migrationBuilder.CreateIndex(
                name: "IX_PipelineErrorLog_Pipeline_CreatedAt",
                schema: "jobs",
                table: "PipelineErrorLog",
                columns: new[] { "pipeline", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_PipelineRunLog_Pipeline_StartedAt",
                schema: "jobs",
                table: "PipelineRunLog",
                columns: new[] { "pipeline", "started_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_PriceSnapshot_FibraId_CapturedAt",
                schema: "market",
                table: "PriceSnapshot",
                columns: new[] { "fibra_id", "captured_at" });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshToken_UserId_RevokedAt",
                schema: "auth",
                table: "RefreshToken",
                columns: new[] { "UserId", "RevokedAt" });

            migrationBuilder.CreateIndex(
                name: "UX_User_Email",
                schema: "auth",
                table: "User",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiCallLog",
                schema: "jobs");

            migrationBuilder.DropTable(
                name: "AiModeConfig",
                schema: "ai");

            migrationBuilder.DropTable(
                name: "AiPrompt",
                schema: "ai");

            migrationBuilder.DropTable(
                name: "AiProviderConfig",
                schema: "ai");

            migrationBuilder.DropTable(
                name: "BlocklistTerm",
                schema: "news");

            migrationBuilder.DropTable(
                name: "ConfigAuditLog",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "DailySnapshot",
                schema: "market");

            migrationBuilder.DropTable(
                name: "Distribution",
                schema: "market");

            migrationBuilder.DropTable(
                name: "EditorialPage",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "FundamentalRecord",
                schema: "fundamentals");

            migrationBuilder.DropTable(
                name: "FundamentalSourceManifest",
                schema: "fundamentals");

            migrationBuilder.DropTable(
                name: "NewsArticleFibra",
                schema: "news");

            migrationBuilder.DropTable(
                name: "OperationalConfig",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "PipelineErrorLog",
                schema: "jobs");

            migrationBuilder.DropTable(
                name: "PipelineRunLog",
                schema: "jobs");

            migrationBuilder.DropTable(
                name: "PriceSnapshot",
                schema: "market");

            migrationBuilder.DropTable(
                name: "RefreshToken",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "Fibra",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "NewsArticle",
                schema: "news");

            migrationBuilder.DropTable(
                name: "User",
                schema: "auth");
        }
    }
}
