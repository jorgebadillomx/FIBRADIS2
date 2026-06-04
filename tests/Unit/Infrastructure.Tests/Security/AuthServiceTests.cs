using Application.Auth;
using Domain.Auth;
using Domain.Auth.Exceptions;
using Infrastructure.Persistence.SqlServer;
using Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Tests.Security;

public class AuthServiceTests
{
    private static AppDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private class FakeEmailEncryptor : IEmailEncryptor
    {
        public string Encrypt(string plainEmail) => plainEmail;
        public string Decrypt(string storedEmail) => storedEmail;
    }

    private class FakeTokenService : ITokenService
    {
        public string GenerateAccessToken(User user) => "fake-access-token";
        public string GenerateRefreshToken() => Guid.NewGuid().ToString();
        public string HashRefreshToken(string rawToken) => BCrypt.Net.BCrypt.HashPassword(rawToken);
    }

    private static AuthService CreateSvc(AppDbContext db) =>
        new(db, new FakeTokenService(), new FakeEmailEncryptor());

    private static async Task<User> SeedUserAsync(AppDbContext db, string email, bool isActive = true)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Fuerte1!"),
            Role = UserRole.User,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow,
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsTokens()
    {
        await using var db = CreateDb();
        await SeedUserAsync(db, "u@fibradis.mx");
        var svc = CreateSvc(db);

        var (access, refresh) = await svc.LoginAsync("u@fibradis.mx", "Fuerte1!");

        Assert.NotEmpty(access);
        Assert.NotEmpty(refresh);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ThrowsInvalidCredentialsException()
    {
        await using var db = CreateDb();
        await SeedUserAsync(db, "u@fibradis.mx");
        var svc = CreateSvc(db);

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => svc.LoginAsync("u@fibradis.mx", "WrongPass1!"));
    }

    [Fact]
    public async Task LoginAsync_EmailNotFound_ThrowsInvalidCredentialsException()
    {
        await using var db = CreateDb();
        var svc = CreateSvc(db);

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => svc.LoginAsync("noexiste@fibradis.mx", "Fuerte1!"));
    }

    [Fact]
    public async Task LoginAsync_DisabledAccount_ThrowsAccountDisabledException()
    {
        await using var db = CreateDb();
        await SeedUserAsync(db, "disabled@fibradis.mx", isActive: false);
        var svc = CreateSvc(db);

        await Assert.ThrowsAsync<AccountDisabledException>(
            () => svc.LoginAsync("disabled@fibradis.mx", "Fuerte1!"));
    }
}
