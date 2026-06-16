using System.Globalization;
using Application.Seo;
using Application.Market;
using Domain.Catalog;
using Domain.Market;
using Domain.Seo;
using Microsoft.AspNetCore.Hosting;
using SkiaSharp;

namespace Infrastructure.Seo;

public sealed class OgImageRenderer(IWebHostEnvironment env) : IOgImageRenderer
{
    private const int CanvasWidth = 1200;
    private const int CanvasHeight = 630;
    private const string FallbackImageName = "og-image.png";
    private const string LogoImageName = "logo.png";
    private const string BrandName = "Fibras Inmobiliarias";
    private static readonly SKColor Slate950 = new(15, 23, 42);
    private static readonly SKColor Slate700 = new(51, 65, 85);
    private static readonly SKColor Slate500 = new(100, 116, 139);
    private static readonly SKColor Slate200 = new(226, 232, 240);
    private static readonly SKColor Teal600 = new(13, 148, 136);
    private static readonly SKColor Teal700 = new(15, 118, 110);
    private static readonly SKColor Teal50 = new(240, 253, 250);
    private static readonly SKColor Amber50 = new(255, 251, 235);
    private static readonly SKColor Amber600 = new(217, 119, 6);
    private static readonly SKColor White = new(255, 255, 255);
    private static readonly SKColor BackgroundTop = new(248, 250, 252);
    private static readonly SKColor BackgroundBottom = new(236, 253, 245);
    private static readonly CultureInfo MexicanCulture = CultureInfo.GetCultureInfo("es-MX");

    // Tipo de letra resuelto una sola vez (inmutable y thread-safe). La cadena prioriza
    // la fuente de marca en Windows (dev) y cae a fuentes con cobertura es-MX presentes en
    // la imagen Debian de producción (Liberation/DejaVu, instaladas vía apt en el Dockerfile).
    private static readonly SKTypeface BaseTypeface =
        SKTypeface.FromFamilyName("Segoe UI")
        ?? SKTypeface.FromFamilyName("Liberation Sans")
        ?? SKTypeface.FromFamilyName("DejaVu Sans")
        ?? SKTypeface.FromFamilyName("Arial")
        ?? SKTypeface.Default;

    // Limita el render concurrente (Security Checklist 12-9): la composición SkiaSharp es
    // CPU-bound; sin tope, un burst de tickers válidos distintos saturaría el thread pool.
    private static readonly int MaxConcurrentRenders = Math.Max(2, Environment.ProcessorCount);
    private readonly SemaphoreSlim _renderGate = new(MaxConcurrentRenders);

    public async Task<byte[]> RenderFibraCardAsync(
        Fibra? fibra,
        FibraSeoMarketData? marketData,
        CancellationToken ct = default)
    {
        if (fibra is null || marketData?.LatestSnapshot?.LastPrice is not > 0m)
            return await LoadFallbackBytesAsync(ct);

        await _renderGate.WaitAsync(ct);
        try
        {
            return RenderCard(fibra, marketData);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return await LoadFallbackBytesAsync(ct);
        }
        finally
        {
            _renderGate.Release();
        }
    }

    private byte[] RenderCard(Fibra fibra, FibraSeoMarketData marketData)
    {
        var imageInfo = new SKImageInfo(CanvasWidth, CanvasHeight, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var bitmap = new SKBitmap(imageInfo);
        using var canvas = new SKCanvas(bitmap);

        DrawBackground(canvas);
        DrawDecorations(canvas);

        var snapshot = marketData.LatestSnapshot;
        var lastPrice = snapshot?.LastPrice ?? 0m;
        var asOfDate = marketData.AsOfDate ?? DateOnly.FromDateTime(snapshot?.CapturedAt.UtcDateTime ?? DateTime.UtcNow);
        var annualizedYield = YieldCalculator.Calculate(marketData.Distributions, lastPrice, asOfDate);
        var quarterlyDistribution = marketData.QuarterlyDistribution;

        DrawCardShell(canvas);
        DrawBrandArea(canvas);
        DrawFibraIdentity(canvas, fibra);
        DrawMetrics(canvas, fibra, snapshot, lastPrice, annualizedYield, quarterlyDistribution);

        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode();
        var bytes = encoded?.ToArray() ?? [];
        if (bytes.Length == 0)
            // Un PNG vacío serviría una imagen rota cacheada 6h; tratarlo como fallo → fallback.
            throw new InvalidOperationException("SkiaSharp devolvió un PNG vacío al codificar la card.");

        return bytes;
    }

    private static void DrawBackground(SKCanvas canvas)
    {
        using var paint = new SKPaint
        {
            Shader = SKShader.CreateLinearGradient(
                new SKPoint(0, 0),
                new SKPoint(CanvasWidth, CanvasHeight),
                [BackgroundTop, BackgroundBottom],
                [0, 1],
                SKShaderTileMode.Clamp),
            IsAntialias = true,
        };

        canvas.DrawRect(new SKRect(0, 0, CanvasWidth, CanvasHeight), paint);
    }

    private static void DrawDecorations(SKCanvas canvas)
    {
        using var blurPaint = new SKPaint
        {
            Color = new SKColor(13, 148, 136, 24),
            IsAntialias = true,
        };
        canvas.DrawCircle(1040, 110, 170, blurPaint);

        using var tealPaint = new SKPaint { Color = new SKColor(15, 118, 110, 20), IsAntialias = true };
        canvas.DrawCircle(180, 560, 140, tealPaint);

        using var amberPaint = new SKPaint { Color = new SKColor(217, 119, 6, 18), IsAntialias = true };
        canvas.DrawCircle(930, 560, 100, amberPaint);
    }

    private static void DrawCardShell(SKCanvas canvas)
    {
        var shellRect = new SKRect(54, 54, CanvasWidth - 54, CanvasHeight - 54);

        using var shadowPaint = new SKPaint
        {
            Color = new SKColor(15, 23, 42, 28),
            IsAntialias = true,
        };
        var shadowRect = new SKRect(shellRect.Left, shellRect.Top + 8, shellRect.Right, shellRect.Bottom + 8);
        canvas.DrawRoundRect(shadowRect, 38, 38, shadowPaint);

        using var fillPaint = new SKPaint
        {
            Color = White,
            IsAntialias = true,
        };
        canvas.DrawRoundRect(shellRect, 38, 38, fillPaint);

        using var borderPaint = new SKPaint
        {
            Color = new SKColor(226, 232, 240),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2,
            IsAntialias = true,
        };
        canvas.DrawRoundRect(shellRect, 38, 38, borderPaint);
    }

    private void DrawBrandArea(SKCanvas canvas)
    {
        var pillRect = new SKRect(90, 90, 372, 150);
        using var pillPaint = new SKPaint
        {
            Color = Teal50,
            IsAntialias = true,
        };
        canvas.DrawRoundRect(pillRect, 24, 24, pillPaint);

        using var borderPaint = new SKPaint
        {
            Color = new SKColor(153, 246, 228),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2,
            IsAntialias = true,
        };
        canvas.DrawRoundRect(pillRect, 24, 24, borderPaint);

        var logoBounds = new SKRect(108, 103, 134, 137);
        DrawLogo(canvas, logoBounds);

        using var brandStyle = CreateTextStyle(24, SKFontStyleWeight.SemiBold, Teal700);
        canvas.DrawText(BrandName, 154, 128, SKTextAlign.Left, brandStyle.Font, brandStyle.Paint);

        using var subtitleStyle = CreateTextStyle(13, SKFontStyleWeight.Normal, Slate500);
        canvas.DrawText("OG social card de fibra", 154, 143, SKTextAlign.Left, subtitleStyle.Font, subtitleStyle.Paint);
    }

    private void DrawFibraIdentity(SKCanvas canvas, Fibra fibra)
    {
        var titleBounds = new SKRect(90, 188, 720, 385);
        using var titleStyle = CreateTextStyle(58, SKFontStyleWeight.SemiBold, Slate950);
        DrawWrappedText(canvas, $"{fibra.FullName}", titleBounds, titleStyle.Font, titleStyle.Paint, maxLines: 2);

        using var tickerStyle = CreateTextStyle(20, SKFontStyleWeight.SemiBold, Teal700);
        var tickerText = fibra.Ticker.Trim().ToUpperInvariant();
        var tickerWidth = Math.Max(110f, tickerStyle.Font.MeasureText(tickerText, tickerStyle.Paint) + 40f);
        var tickerRect = new SKRect(90, 382, 90 + tickerWidth, 426);
        using var tickerBgPaint = new SKPaint { Color = Teal50, IsAntialias = true };
        canvas.DrawRoundRect(tickerRect, 18, 18, tickerBgPaint);
        using var tickerBorderPaint = new SKPaint
        {
            Color = new SKColor(153, 246, 228),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2,
            IsAntialias = true,
        };
        canvas.DrawRoundRect(tickerRect, 18, 18, tickerBorderPaint);
        canvas.DrawText(tickerText, tickerRect.Left + 20, tickerRect.Bottom - 13, SKTextAlign.Left, tickerStyle.Font, tickerStyle.Paint);

        var sector = string.IsNullOrWhiteSpace(fibra.Sector)
            ? "Cotiza en la BMV"
            : fibra.Sector.Trim();

        using var sectorLabelStyle = CreateTextStyle(13, SKFontStyleWeight.SemiBold, Slate500);
        using var sectorStyle = CreateTextStyle(22, SKFontStyleWeight.Medium, Slate700);
        canvas.DrawText("SECTOR", 90, 454, SKTextAlign.Left, sectorLabelStyle.Font, sectorLabelStyle.Paint);
        canvas.DrawText(sector, 90, 488, SKTextAlign.Left, sectorStyle.Font, sectorStyle.Paint);

        using var noteStyle = CreateTextStyle(16, SKFontStyleWeight.Normal, Slate500);
        canvas.DrawText("Card dinámica con precio y yield vivos.", 90, 532, SKTextAlign.Left, noteStyle.Font, noteStyle.Paint);
    }

    private void DrawMetrics(
        SKCanvas canvas,
        Fibra fibra,
        PriceSnapshot? snapshot,
        decimal lastPrice,
        decimal? annualizedYield,
        decimal? quarterlyDistribution)
    {
        var priceRect = new SKRect(770, 188, 1070, 328);
        var yieldRect = new SKRect(770, 346, 1070, 486);

        DrawMetricCard(
            canvas,
            priceRect,
            "Precio de cotización",
            FormatPrice(lastPrice, fibra.Currency),
            snapshot is not null
                ? $"Actualizado {snapshot.CapturedAt.UtcDateTime.ToString("dd MMM yyyy", MexicanCulture)}"
                : "Precio vivo",
            Teal50,
            Teal700,
            Slate950);

        var yieldValue = annualizedYield is not null
            ? $"{Math.Round(annualizedYield.Value * 100m, 2, MidpointRounding.AwayFromZero).ToString("N2", MexicanCulture)}%"
            : "—";

        var yieldSubtitle = quarterlyDistribution is > 0m
            ? "Yield TTM anualizado"
            : "Sin distribuciones recientes";

        DrawMetricCard(
            canvas,
            yieldRect,
            "Yield",
            yieldValue,
            yieldSubtitle,
            Amber50,
            Amber600,
            Slate950);

        using var footerStyle = CreateTextStyle(14, SKFontStyleWeight.SemiBold, Slate500);
        canvas.DrawText("Datos vivos · Fibras Inmobiliarias", 770, 540, SKTextAlign.Left, footerStyle.Font, footerStyle.Paint);
        using var footerSubtitleStyle = CreateTextStyle(16, SKFontStyleWeight.Normal, Slate700);
        canvas.DrawText("Compartible para redes e IA", 770, 566, SKTextAlign.Left, footerSubtitleStyle.Font, footerSubtitleStyle.Paint);
    }

    private static void DrawMetricCard(
        SKCanvas canvas,
        SKRect rect,
        string label,
        string value,
        string subtitle,
        SKColor background,
        SKColor accent,
        SKColor valueColor)
    {
        using var fillPaint = new SKPaint { Color = background, IsAntialias = true };
        canvas.DrawRoundRect(rect, 28, 28, fillPaint);

        using var borderPaint = new SKPaint
        {
            Color = Slate200,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1,
            IsAntialias = true,
        };
        canvas.DrawRoundRect(rect, 28, 28, borderPaint);

        using var labelStyle = CreateTextStyle(13, SKFontStyleWeight.SemiBold, accent);
        canvas.DrawText(label.ToUpperInvariant(), rect.Left + 22, rect.Top + 34, SKTextAlign.Left, labelStyle.Font, labelStyle.Paint);

        using var valueStyle = CreateTextStyle(34, SKFontStyleWeight.SemiBold, valueColor);
        canvas.DrawText(value, rect.Left + 22, rect.Top + 82, SKTextAlign.Left, valueStyle.Font, valueStyle.Paint);

        using var subtitleStyle = CreateTextStyle(16, SKFontStyleWeight.Normal, Slate500);
        DrawWrappedText(canvas, subtitle, new SKRect(rect.Left + 22, rect.Top + 94, rect.Right - 22, rect.Bottom - 20), subtitleStyle.Font, subtitleStyle.Paint, maxLines: 2);
    }

    private void DrawLogo(SKCanvas canvas, SKRect target)
    {
        var logoInfo = env.WebRootFileProvider.GetFileInfo(LogoImageName);
        if (logoInfo.Exists)
        {
            try
            {
                using var stream = logoInfo.CreateReadStream();
                using var logoBitmap = SKBitmap.Decode(stream);
                if (logoBitmap is not null)
                {
                    canvas.DrawBitmap(logoBitmap, SKRect.Create(target.Left, target.Top, target.Width, target.Height));
                    return;
                }
            }
            catch
            {
                // Si el logo no puede decodificarse, caemos al monograma.
            }
        }

        using var logoBg = new SKPaint { Color = Teal700, IsAntialias = true };
        canvas.DrawRoundRect(target, 10, 10, logoBg);
        using var logoStyle = CreateTextStyle(16, SKFontStyleWeight.SemiBold, White);
        canvas.DrawText("F", target.Left + 10, target.Bottom - 9, SKTextAlign.Left, logoStyle.Font, logoStyle.Paint);
    }

    private sealed class TextStyle : IDisposable
    {
        public TextStyle(SKPaint paint, SKFont font)
        {
            Paint = paint;
            Font = font;
        }

        public SKPaint Paint { get; }

        public SKFont Font { get; }

        public void Dispose()
        {
            Paint.Dispose();
            Font.Dispose();
        }
    }

    private static TextStyle CreateTextStyle(float size, SKFontStyleWeight weight, SKColor color)
    {
        var paint = new SKPaint
        {
            Color = color,
            IsAntialias = true,
        };

        // Reusar el typeface estático compartido (no disponer): evita un leak de handle nativo
        // por cada llamada y es seguro porque SKTypeface es inmutable y thread-safe.
        var font = new SKFont(BaseTypeface, size, 1, 0)
        {
            Embolden = weight >= SKFontStyleWeight.SemiBold,
        };

        return new TextStyle(paint, font);
    }

    private static void DrawWrappedText(SKCanvas canvas, string text, SKRect bounds, SKFont font, SKPaint paint, int maxLines = 2)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        var lines = WrapText(text, bounds.Width, font, paint, maxLines);
        var metrics = font.Metrics;
        var lineHeight = (metrics.Descent - metrics.Ascent + metrics.Leading) * 1.08f;
        var y = bounds.Top - metrics.Ascent;

        foreach (var line in lines)
        {
            canvas.DrawText(line, bounds.Left, y, SKTextAlign.Left, font, paint);
            y += lineHeight;
        }
    }

    private static IReadOnlyList<string> WrapText(string text, float maxWidth, SKFont font, SKPaint paint, int maxLines)
    {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
            return [];

        var lines = new List<string>();
        var current = string.Empty;

        foreach (var word in words)
        {
            var candidate = current.Length == 0 ? word : $"{current} {word}";
            if (font.MeasureText(candidate, paint) <= maxWidth || current.Length == 0)
            {
                current = candidate;
                continue;
            }

            lines.Add(current);
            current = word;

            if (lines.Count == maxLines - 1)
                break;
        }

        if (lines.Count < maxLines)
            lines.Add(current);

        if (lines.Count > maxLines)
            lines = lines.Take(maxLines).ToList();

        if (lines.Count == maxLines)
            lines[^1] = TruncateLine(lines[^1], maxWidth, font, paint);

        return lines;
    }

    private static string TruncateLine(string line, float maxWidth, SKFont font, SKPaint paint)
    {
        if (font.MeasureText(line, paint) <= maxWidth)
            return line;

        const string ellipsis = "…";
        var cut = line.Length;
        while (cut > 0 && font.MeasureText(line[..cut] + ellipsis, paint) > maxWidth)
            cut--;

        return cut <= 0 ? ellipsis : line[..cut].TrimEnd(' ', ',', ';', ':', '.') + ellipsis;
    }

    private static string FormatPrice(decimal price, string currency)
    {
        var code = string.IsNullOrWhiteSpace(currency) ? "MXN" : currency.Trim().ToUpperInvariant();
        return code == "MXN"
            ? $"${price.ToString("N2", MexicanCulture)}"
            : $"{code} {price.ToString("N2", MexicanCulture)}";
    }

    private async Task<byte[]> LoadFallbackBytesAsync(CancellationToken ct)
    {
        var file = env.WebRootFileProvider.GetFileInfo(FallbackImageName);
        if (file.Exists)
        {
            await using var fallbackStream = file.CreateReadStream();
            using var memory = new MemoryStream();
            await fallbackStream.CopyToAsync(memory, ct);
            return memory.ToArray();
        }

        return RenderPlainFallback();
    }

    private static byte[] RenderPlainFallback()
    {
        var imageInfo = new SKImageInfo(CanvasWidth, CanvasHeight, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var bitmap = new SKBitmap(imageInfo);
        using var canvas = new SKCanvas(bitmap);

        DrawBackground(canvas);
        DrawCardShell(canvas);

        using var titleStyle = CreateTextStyle(54, SKFontStyleWeight.SemiBold, Slate950);
        canvas.DrawText("Fibras Inmobiliarias", 96, 204, SKTextAlign.Left, titleStyle.Font, titleStyle.Paint);
        using var subtitleStyle = CreateTextStyle(22, SKFontStyleWeight.Normal, Slate700);
        canvas.DrawText("Imagen social genérica", 96, 258, SKTextAlign.Left, subtitleStyle.Font, subtitleStyle.Paint);
        canvas.DrawText("La versión dinámica se activa cuando hay datos de mercado.", 96, 304, SKTextAlign.Left, subtitleStyle.Font, subtitleStyle.Paint);

        using var footerStyle = CreateTextStyle(16, SKFontStyleWeight.SemiBold, Slate500);
        canvas.DrawText("Fallback de seguridad", 96, 370, SKTextAlign.Left, footerStyle.Font, footerStyle.Paint);

        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode();
        return encoded?.ToArray() ?? [];
    }
}
