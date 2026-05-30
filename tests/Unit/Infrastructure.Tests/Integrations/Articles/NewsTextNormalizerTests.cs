using Infrastructure.Integrations.Articles;

namespace Infrastructure.Tests.Integrations.Articles;

public class NewsTextNormalizerTests
{
    [Fact]
    public void Normalize_RemovesBoilerplateDecoratorsAndAdjacentDuplicates()
    {
        var input = """
            Compartir

            ---

            FUNO anunció una emisión de deuda por 12,000 millones de pesos.

            FUNO anunció una emisión de deuda por 12,000 millones de pesos.

            La colocación busca refinanciar vencimientos de corto plazo.

            Suscríbete
            """;

        var result = NewsTextNormalizer.Normalize(input);

        Assert.Equal(
            """
            FUNO anunció una emisión de deuda por 12,000 millones de pesos.

            La colocación busca refinanciar vencimientos de corto plazo.
            """,
            result);
    }

    [Fact]
    public void Normalize_PreservesMaterialNumericContent()
    {
        var input = """
            Fibra Uno reportó NOI de 1,250 millones de pesos en el trimestre.

            La ocupación cerró en 96.4% y el guidance para 2026 se mantuvo sin cambios.
            """;

        var result = NewsTextNormalizer.Normalize(input);

        Assert.Contains("1,250 millones", result);
        Assert.Contains("96.4%", result);
    }
}
