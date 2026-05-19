using System.Security.Cryptography;
using System.Text;
using Domain.News;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Seeds;

public static class NewsSeed
{
    private static readonly string[] DefaultBlocklist =
    [
        "fibra óptica",
        "fibra optica",
        "fibra alimentaria",
        "fibra dietética",
        "fibra dietetica",
        "fibra muscular",
        "fibra textil",
        "fibra de carbono",
        "internet fibra",
        "fibra de vidrio",
    ];

    public static void Seed(ModelBuilder modelBuilder)
    {
        var terms = DefaultBlocklist.Select(term => new BlocklistTerm
        {
            Id = GuidFromKey($"blocklist-{term}"),
            Term = term,
            CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
        });

        modelBuilder.Entity<BlocklistTerm>().HasData(terms);
    }

    private static Guid GuidFromKey(string key)
    {
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(key));
        return new Guid(hash);
    }
}
