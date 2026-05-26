using Infrastructure.Integrations.Pdf;

namespace Infrastructure.Tests.Integrations.Pdf;

public class PdfMarkdownExtractorTests
{
    [Fact]
    public void Extract_WithInvalidBytes_ThrowsException()
    {
        using var stream = new MemoryStream([0x00, 0x01, 0x02, 0x03]);

        Assert.ThrowsAny<Exception>(() => PdfMarkdownExtractor.Extract(stream));
    }

    [Fact]
    public void Extract_WithEmptyStream_ThrowsException()
    {
        using var stream = new MemoryStream([]);

        Assert.ThrowsAny<Exception>(() => PdfMarkdownExtractor.Extract(stream));
    }

    [Fact]
    public void Extract_WithValidPdf_ReturnsNonEmptyText()
    {
        var pdf = BuildMinimalPdf("FIBRA Test");
        using var stream = new MemoryStream(pdf);

        var result = PdfMarkdownExtractor.Extract(stream);

        Assert.False(string.IsNullOrWhiteSpace(result));
        Assert.Contains("FIBRA", result, StringComparison.OrdinalIgnoreCase);
    }

    private static byte[] BuildMinimalPdf(string text)
    {
        var contentStr = $"BT /F1 12 Tf 10 10 Td ({text}) Tj ET\n";
        var streamLength = contentStr.Length;
        var sb = new System.Text.StringBuilder();
        sb.Append("%PDF-1.4\n");

        var offsets = new List<int>();

        offsets.Add(sb.Length);
        sb.Append("1 0 obj\n<</Type /Catalog /Pages 2 0 R>>\nendobj\n");

        offsets.Add(sb.Length);
        sb.Append("2 0 obj\n<</Type /Pages /Kids [3 0 R] /Count 1>>\nendobj\n");

        offsets.Add(sb.Length);
        sb.Append("3 0 obj\n<</Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] /Contents 4 0 R /Resources <</Font <</F1 5 0 R>>>>>>\nendobj\n");

        offsets.Add(sb.Length);
        sb.Append($"4 0 obj\n<</Length {streamLength}>>\nstream\n");
        sb.Append(contentStr);
        sb.Append("endstream\nendobj\n");

        offsets.Add(sb.Length);
        sb.Append("5 0 obj\n<</Type /Font /Subtype /Type1 /BaseFont /Helvetica>>\nendobj\n");

        var xrefStart = sb.Length;
        sb.Append("xref\n0 6\n");
        sb.Append("0000000000 65535 f \n");
        foreach (var offset in offsets)
            sb.Append($"{offset:D10} 00000 n \n");
        sb.Append("trailer\n<</Size 6 /Root 1 0 R>>\n");
        sb.Append($"startxref\n{xrefStart}\n%%EOF\n");

        return System.Text.Encoding.ASCII.GetBytes(sb.ToString());
    }
}
