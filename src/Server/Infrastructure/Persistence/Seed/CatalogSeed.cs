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
            F("FUNO11",    "FUNO11.MX",    "Fibra Uno",                "Fibra Uno",   "Diversificado",     "BMV",      "MXN", "https://fibra.uno",                   "https://fibra.uno/inversionistas",                    "https://funo.mx/inversionistas/suplementos-informativos",                           ["Fibra Uno", "FUNO"],                              CatalogDescriptions.FUNO11),
            F("DANHOS13",  "DANHOS13.MX",  "Fibra Danhos",             "Danhos",      "Comercial",         "BMV",      "MXN", "https://fibradanhos.com.mx",           "https://fibradanhos.com.mx/ri",                       "https://fibradanhos.com.mx/reportes-trimestrales.html",                             ["Danhos", "DANHOS"],                               CatalogDescriptions.DANHOS13),
            F("TERRA13",   "TERRA13.MX",   "Fibra Terra",              "Terra",       "Industrial",        "BMV",      "MXN", "https://fibra-terra.com",             null,                                                  null,                                                                                ["Fibra Terra", "TERRA"],                           CatalogDescriptions.TERRA13),
            F("FIBRAMQ12", "FIBRAMQ12.MX", "Fibra Macquarie",          "FibraMQ",     "Industrial",        "BMV",      "MXN", "https://fibramacquarie.com.mx",        "https://fibramacquarie.com.mx/ri",                    "https://www.fibramacquarie.com/es/inversionistas.html",                             ["Fibra MQ", "Macquarie", "FIBRAMQ"],               CatalogDescriptions.FIBRAMQ12),
            F("FMTY14",    "FMTY14.MX",    "Fibra Monterrey",          "Fibra MTY",   "Industrial",        "BMV",      "MXN", "https://fibramty.com",                "https://fibramty.com/inversionistas",                 "https://www.fibramty.com/en/inversionistas",                                        ["Fibra Monterrey", "FibraMTY", "FMTY"],            CatalogDescriptions.FMTY14),
            F("FINN13",    "FINN13.MX",    "Fibra Inn",                "Fibra Inn",   "Hotelero",          "BMV",      "MXN", "https://fibrainn.com.mx",             null,                                                  "https://fibrainn.mx/inversionistas/resultados-trimestrales",                        ["Fibra Inn", "FINN"],                              CatalogDescriptions.FINN13),
            F("FIHO12",    "FIHO12.MX",    "Fibra Hotel",              "Fibra Hotel", "Hotelero",          "BMV",      "MXN", "https://fibrahotel.com",              null,                                                  "https://www.bmv.com.mx/es/emisoras/informacionfinanciera/FIHO-30057-CGEN_CAPIT",    ["Fibra Hotel", "FIHO"],                            CatalogDescriptions.FIHO12),
            F("VESTA15",   "VESTA.MX",     "Fibra Vesta",              "Vesta",       "Industrial",        "BMV",      "MXN", "https://fibravesta.com",              "https://fibravesta.com/ri",                           "https://ir.vesta.com.mx/es/financial-results",                                      ["Fibra Vesta", "VESTA"],                           CatalogDescriptions.VESTA15),
            F("HCITY17",   "HCITY.MX",     "Fibra Hotel City Express", "HC",          "Hotelero",          "BMV",      "MXN", "https://hcity.com.mx",                null,                                                  "https://www.norte19.com/investors",                                                 ["Hotel City Express", "HCITY", "HC"],              CatalogDescriptions.HCITY17),
            F("EDUCA18",   "EDUCA18.MX",   "Fibra Educa",              "Fibra Educa", "Educativo",         "BMV",      "MXN", "https://www.fibraeduca.com",           "https://www.fibraeduca.com/invertir",                 "https://www.fibraeduca.com/reportes-financieros",                                   ["Fibra Educa", "EDUCA", "EDUCA18"],                CatalogDescriptions.EDUCA18),
            F("FIBRAPL14", "FIBRAPL14.MX", "Fibra Prologis",           "Prologis",    "Industrial",        "BMV",      "MXN", "https://www.fibraprologis.com/en-US",  "https://www.fibraprologis.com/en-US/investors",       "https://www.fibraprologis.com/en-US/investors/financial-results",                   ["Fibra Prologis", "Prologis", "FIBRAPL"],          CatalogDescriptions.FIBRAPL14),
            F("FIBRAUP18", "FIBRAUP18.MX", "Fibra Upsite",             "Upsite",      "Industrial",        "BMV",      "MXN", "https://fibra-upsite.com",            null,                                                  "https://fibra-upsite.com/inversionistas/razones",                                   ["Fibra Upsite", "Upsite", "FIBRAUP"],              CatalogDescriptions.FIBRAUP18),
            F("FNOVA17",   "FNOVA17.MX",   "Fibra Nova",               "Fibra Nova",  "Diversificado",     "BIVA",     "MXN", "https://www.fibra-nova.com",           "https://www.fibra-nova.com/inversionistas/como-invertir", "https://www.fibra-nova.com/inversionistas/reportes-trimestrales",               ["Fibra Nova", "FNOVA", "FNOVA17"],                 CatalogDescriptions.FNOVA17),
            F("FPLUS16",   "FPLUS16.MX",   "Fibra Plus",               "Fibra Plus",  "Diversificado",     "BMV",      "MXN", "https://www.fibraplus.mx",             null,                                                  "https://www.fibraplus.mx/es/financiera/trimestrales",                               ["Fibra Plus", "FPLUS", "FPLUS16"],                 CatalogDescriptions.FPLUS16),
            F("FSHOP13",   "FSHOP13.MX",   "Fibra Shop",               "Fibra Shop",  "Comercial",         "BMV",      "MXN", "https://fibrashop.mx",                "https://fibrashop.mx/contacto/",                      "https://fibrashop.mx/informes-financieros/",                                        ["Fibra Shop", "FSHOP", "FSHOP13"],                 CatalogDescriptions.FSHOP13),
            F("NEXT25",    "NEXT25.MX",    "Fibra Next",               "Fibra Next",  "Industrial",        "BMV",      "MXN", "https://fibranext.mx",                "https://fibranext.mx/investors",                      "https://fibranext.mx/investors",                                                    ["Fibra Next", "NEXT", "NEXT25"],                   CatalogDescriptions.NEXT25),
            F("SOMA21",    "SOMA21.MX",    "Fibra SOMA",               "Fibra SOMA",  "Comercial",         "BIVA",     "MXN", "https://fibrasoma.group",              null,                                                  "https://fibrasoma.group/investors/quarterly-reports-2/",                            ["Fibra SOMA", "SOMA", "SOMA21"],                   CatalogDescriptions.SOMA21),
            F("STORAGE18", "STORAGE18.MX", "Fibra Storage",            "Fibra Storage", "Autoalmacenaje",  "BMV",      "MXN", "https://fibrastorage.com",             null,                                                  "https://fibrastorage.com/repositorio-informacion-financiera/",                      ["Fibra Storage", "Storage", "STORAGE18", "U-Storage"], CatalogDescriptions.STORAGE18),
            F("FHIPO14",   "FHIPO14.MX",   "FHipo",                    "FHipo",       "Hipotecario",       "BIVA",     "MXN", "https://fhipo.com/es/",                "https://fhipo.com/es/kit-para-inversionistas/",       "https://fhipo.com/es/reportes-trimestrales/",                                       ["FHipo", "Fideicomiso Hipotecario", "FHIPO", "FHIPO14"], CatalogDescriptions.FHIPO14),
            F("FCFE18",    "FCFE18.MX",    "CFE Fibra E",              "CFE Fibra E", "Infraestructura",   "BMV/BIVA", "MXN", "https://cfecapital.com.mx",            "https://cfecapital.com.mx/inversionistas",             "https://cfecapital.com.mx/informacion-financiera",                                  ["CFE Fibra E", "FCFE", "FCFE18"],                  CatalogDescriptions.FCFE18),
            F("FEXI21",    "FEXI21.MX",    "Fibra EXI",                "Fibra EXI",   "Infraestructura",   "BMV",      "MXN", "https://fibraexi.com/es/",             "https://fibraexi.com/es/inversionistas/",              "http://www.economatica.mx/FEXI/REPORTES%20TRIMESTRALES/",                           ["Fibra EXI", "FEXI", "FEXI21"],                    CatalogDescriptions.FEXI21),
            F("AGRO22",    "AGRO22.MX",    "AgroFibra",                "AgroFibra",   "Agroalimentario",   "BIVA",     "MXN", "https://agrofibra.com/",               "https://agrofibra.com/inversionistas/",                "http://www.economatica.mx/AGRO/REPORTES%20TRIMESTRALES%20/",                        ["AgroFibra", "Fibra AGRO", "AGRO", "AGRO22"],      CatalogDescriptions.AGRO22),
            F("FVIA16",    "FVIA16.MX",    "Fibra VIA",                "Fibra VIA",   "Infraestructura",   "BMV",      "MXN", null,                                   null,                                                  null,                                                                                ["Fibra VIA", "FVIA", "FVIA16"],                    CatalogDescriptions.FVIA16),
            BenchmarkF("^MXX",  "^MXX",  "IPC BMV", "IPC",     "BMV",  "MXN"),
            BenchmarkF("^GSPC", "^GSPC", "S&P 500", "S&P 500", "NYSE", "USD")
        );
    }

    private static Fibra F(
        string ticker, string yahooTicker, string fullName, string shortName,
        string sector, string market, string currency,
        string? siteUrl, string? investorUrl, string? reportsUrl,
        List<string> nameVariants,
        string? description = null)
        => new()
        {
            Id = GuidFromTicker(ticker),
            Ticker = ticker,
            YahooTicker = yahooTicker,
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
            Description = description,
            CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
        };

    private static Fibra BenchmarkF(
        string ticker, string yahooTicker, string fullName, string shortName,
        string market, string currency)
        => new()
        {
            Id = GuidFromTicker(ticker),
            Ticker = ticker,
            YahooTicker = yahooTicker,
            FullName = fullName,
            ShortName = shortName,
            Sector = "Índice",
            Market = market,
            Currency = currency,
            State = FibraState.Inactive,
            NameVariants = [],
            CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
        };

    // GUID determinista basado en MD5 del ticker completo — garantiza unicidad sin importar longitud o prefijos comunes
    private static Guid GuidFromTicker(string ticker)
    {
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(ticker));
        return new Guid(hash);
    }
}
