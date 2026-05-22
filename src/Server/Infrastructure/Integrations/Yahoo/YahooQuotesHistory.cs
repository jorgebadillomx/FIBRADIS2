using YahooQuotesApi;

namespace Infrastructure.Integrations.Yahoo;

// Wrapper para registrar como singleton separado en DI sin colisión con YahooQuotes
public class YahooQuotesHistory(YahooQuotes inner)
{
    public YahooQuotes Inner => inner;
}
