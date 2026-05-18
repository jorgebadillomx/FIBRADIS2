using Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Persistence.Seed;

public static class CatalogSeed
{
    public static void Seed(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Fibra>().HasData(
            F("FUNO11",    "Fibra Uno",                "Fibra Uno",   "Diversificado", "BMV", "MXN", "https://fibra.uno",             "https://fibra.uno/inversionistas",    null, ["Fibra Uno", "FUNO"]),
            F("DANHOS13",  "Fibra Danhos",             "Danhos",      "Comercial",     "BMV", "MXN", "https://fibradanhos.com.mx",     "https://fibradanhos.com.mx/ri",       null, ["Danhos", "DANHOS"]),
            F("TERRA13",   "Fibra Terra",              "Terra",       "Industrial",    "BMV", "MXN", "https://fibra-terra.com",        null,                                  null, ["Fibra Terra", "TERRA"]),
            F("FIBRAMQ12", "Fibra Macquarie",          "FibraMQ",     "Industrial",    "BMV", "MXN", "https://fibramacquarie.com.mx",  "https://fibramacquarie.com.mx/ri",    null, ["Fibra MQ", "Macquarie", "FIBRAMQ"]),
            F("FMTY14",    "Fibra Monterrey",          "Fibra MTY",   "Industrial",    "BMV", "MXN", "https://fibramty.com",           "https://fibramty.com/inversionistas", null, ["Fibra Monterrey", "FibraMTY", "FMTY"]),
            F("FINN13",    "Fibra Inn",                "Fibra Inn",   "Hotelero",      "BMV", "MXN", "https://fibrainn.com.mx",        null,                                  null, ["Fibra Inn", "FINN"]),
            F("FIHO12",    "Fibra Hotel",              "Fibra Hotel", "Hotelero",      "BMV", "MXN", "https://fibrahotel.com",         null,                                  null, ["Fibra Hotel", "FIHO"]),
            F("VESTA15",   "Fibra Vesta",              "Vesta",       "Industrial",    "BMV", "MXN", "https://fibravesta.com",         "https://fibravesta.com/ri",           null, ["Fibra Vesta", "VESTA"]),
            F("HCITY17",   "Fibra Hotel City Express", "HC",          "Hotelero",      "BMV", "MXN", "https://hcity.com.mx",           null,                                  null, ["Hotel City Express", "HCITY", "HC"]),
            F("PLUS18",    "Fibra Plus",               "Fibra Plus",  "Diversificado", "BMV", "MXN", "https://fibraplus.mx",           null,                                  null, ["Fibra Plus", "PLUS"])
        );
    }

    private static Fibra F(
        string ticker, string fullName, string shortName,
        string sector, string market, string currency,
        string? siteUrl, string? investorUrl, string? reportsUrl,
        List<string> nameVariants)
        => new()
        {
            Id = GuidFromTicker(ticker),
            Ticker = ticker,
            FullName = fullName,
            ShortName = shortName,
            Sector = sector,
            Market = market,
            Currency = currency,
            State = FibraState.Active,
            SiteUrl = siteUrl,
            InvestorUrl = investorUrl,
            ReportsUrl = reportsUrl,
            NameVariants = nameVariants,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        };

    // GUID determinista basado en MD5 del ticker completo — garantiza unicidad sin importar longitud o prefijos comunes
    private static Guid GuidFromTicker(string ticker)
    {
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(ticker));
        return new Guid(hash);
    }
}
