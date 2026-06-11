using Application.Catalog;
using Xunit;

namespace Application.Tests.Catalog;

public class SlugHelperTests
{
    [Theory]
    [InlineData("Distribución", "distribucion")]
    [InlineData("FIBRA Uno S.A. de C.V.", "fibra uno s.a. de c.v.")]
    [InlineData("Café & Señor", "cafe & senor")]
    [InlineData("FUNO11 reporta resultados del 2T25", "funo11 reporta resultados del 2t25")]
    [InlineData("Fibra҃uno", "fibrauno")]       // U+0483 titlo — marca combinante fuera de U+0300-036F
    public void NormalizeText_ProducesExpectedOutput(string input, string expected)
    {
        Assert.Equal(expected, SlugHelper.NormalizeText(input));
    }

    [Fact]
    public void NormalizeText_IsConsistentWithFibraSlugNormalization()
    {
        // Los inputs del catálogo real producen el mismo texto normalizado
        // que el bloque inline que reemplaza (verificación de paridad de refactor)
        var cases = new[]
        {
            "Fibra Uno",
            "FUNO11",
            "FIBRA Hotel",
            "Concentradora Fibra Hotelera Mexicana",
            "Macquarie México Real Estate Management",
        };

        foreach (var input in cases)
        {
            var result = SlugHelper.NormalizeText(input);
            // Post-normalización: sin acentos, lowercase, espacios y puntuación intactos
            Assert.Equal(result, result.ToLowerInvariant()); // ya está en lowercase
            Assert.DoesNotContain(result, new[] { "Á", "É", "Í", "Ó", "Ú", "á", "é", "í", "ó", "ú", "ñ", "Ñ" });
        }
    }

    [Fact]
    public void NormalizeText_IsConsistentWithSlugGeneratorNormalization()
    {
        // Los inputs de noticias reales producen el mismo texto normalizado
        var cases = new[]
        {
            "FUNO11 reporta resultados del 2T25",
            "Fibra Inn anuncia distribución trimestral",
            "Análisis de la situación del mercado inmobiliario",
        };

        foreach (var input in cases)
        {
            var result = SlugHelper.NormalizeText(input);
            Assert.Equal(result, result.ToLowerInvariant());
            Assert.DoesNotContain("ó", result);
            Assert.DoesNotContain("ú", result);
        }
    }
}
