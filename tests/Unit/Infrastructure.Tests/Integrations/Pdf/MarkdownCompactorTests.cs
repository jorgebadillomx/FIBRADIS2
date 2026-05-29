using Infrastructure.Integrations.Pdf;

namespace Infrastructure.Tests.Integrations.Pdf;

public class MarkdownCompactorTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Compact_NullOrWhitespace_ReturnsEmpty(string? input)
    {
        Assert.Equal(string.Empty, MarkdownCompactor.Compact(input));
    }

    [Fact]
    public void Compact_RemovesPageNumberLines()
    {
        var input = "Texto útil\n123\nMás texto";
        var result = MarkdownCompactor.Compact(input);

        Assert.DoesNotContain("123", result);
        Assert.Contains("Texto útil", result);
        Assert.Contains("Más texto", result);
    }

    [Fact]
    public void Compact_CollapsesMultipleBlankLines()
    {
        var input = "A\n\n\n\nB";
        var result = MarkdownCompactor.Compact(input);

        Assert.DoesNotContain("\n\n\n", result);
        Assert.Contains("A", result);
        Assert.Contains("B", result);
    }

    [Fact]
    public void Compact_CollapsesDoubleSpaces()
    {
        var input = "Cap  Rate  15%";
        var result = MarkdownCompactor.Compact(input);

        Assert.DoesNotContain("  ", result);
        Assert.Contains("Cap Rate 15%", result);
    }

    [Fact]
    public void Compact_RemovesHighFrequencyNonNumericLines()
    {
        var input = "DISCLAIMER\nContenido útil\nDISCLAIMER\nMás contenido\nDISCLAIMER";
        var result = MarkdownCompactor.Compact(input);

        Assert.DoesNotContain("DISCLAIMER", result);
        Assert.Contains("Contenido útil", result);
        Assert.Contains("Más contenido", result);
    }

    [Fact]
    public void Compact_DoesNotRemoveHighFrequencyLinesWithDigits()
    {
        var input = "15%\nContenido\n15%\nMás texto\n15%";
        var result = MarkdownCompactor.Compact(input);

        Assert.Equal(3, result.Split('\n').Count(l => l.Trim() == "15%"));
    }

    [Fact]
    public void Compact_JoinsOcrHyphenatedLineBreaks()
    {
        var input = "estruc-\ntura operativa";
        var result = MarkdownCompactor.Compact(input);

        Assert.Contains("estructura operativa", result);
        Assert.DoesNotContain("-\n", result);
    }

    [Fact]
    public void Compact_RemovesDecorativeSeparators()
    {
        var input = "Título\n---\nContenido\n=====\nMás\n***";
        var result = MarkdownCompactor.Compact(input);

        Assert.DoesNotContain("---", result);
        Assert.DoesNotContain("=====", result);
        Assert.DoesNotContain("***", result);
        Assert.Contains("Título", result);
        Assert.Contains("Contenido", result);
    }

    [Fact]
    public void Compact_PreservesNumericContent()
    {
        var input = "Cap Rate: 8.5%\nNAV per CBFI: $12.50\nLTV: 45%";
        var result = MarkdownCompactor.Compact(input);

        Assert.Contains("8.5%", result);
        Assert.Contains("$12.50", result);
        Assert.Contains("45%", result);
    }

    [Fact]
    public void Compact_PageNumbersUpToFourDigits_AreRemoved()
    {
        var input = "Texto\n9999\nMás texto";
        var result = MarkdownCompactor.Compact(input);

        Assert.DoesNotContain("9999", result);
    }

    [Fact]
    public void Compact_FiveDigitNumbers_ArePreserved()
    {
        var input = "Texto\n12345\nMás texto";
        var result = MarkdownCompactor.Compact(input);

        Assert.Contains("12345", result);
    }
}
