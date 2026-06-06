using Application.Auth;
using Domain.Auth;
using Domain.Auth.Exceptions;
using Infrastructure.Persistence.SqlServer;
using Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Tests.Security;

public class UserProfileServiceTests
{
    private static AppDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static UserService CreateSvc(AppDbContext db) =>
        new(db, new FakeEmailEncryptor());

    private sealed class FakeEmailEncryptor : IEmailEncryptor
    {
        public string Encrypt(string plainEmail) => plainEmail;
        public string Decrypt(string storedEmail) => storedEmail;
    }

    [Fact]
    public async Task GetProfileAsync_ReturnsDecryptedEmailAndApodo()
    {
        await using var db = CreateDb();
        var svc = CreateSvc(db);
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "perfil@fibradis.mx",
            Apodo = "Mi apodo",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Fuerte1!"),
            Role = UserRole.User,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var profile = await svc.GetProfileAsync(user.Id);

        Assert.Equal("perfil@fibradis.mx", profile.Email);
        Assert.Equal("User", profile.Role);
        Assert.Equal("Mi apodo", profile.Apodo);
    }

    [Fact]
    public async Task UpdateApodoAsync_ApodoTooLong_ThrowsInvalidUserDataException()
    {
        await using var db = CreateDb();
        var svc = CreateSvc(db);
        var user = await svc.CreateUserAsync("u@fibradis.mx", "Fuerte1!", "User");

        var apodo = new string('a', 51);

        await Assert.ThrowsAsync<InvalidUserDataException>(
            () => svc.UpdateApodoAsync(user.Id, apodo));
    }

    [Fact]
    public async Task UpdateApodoAsync_ControlCharacters_ThrowsInvalidUserDataException()
    {
        await using var db = CreateDb();
        var svc = CreateSvc(db);
        var user = await svc.CreateUserAsync("u@fibradis.mx", "Fuerte1!", "User");

        await Assert.ThrowsAsync<InvalidUserDataException>(
            () => svc.UpdateApodoAsync(user.Id, "apodo\u0001malo"));
    }

    [Fact]
    public async Task ChangeOwnPasswordAsync_WrongCurrentPassword_ThrowsInvalidCredentialsException()
    {
        await using var db = CreateDb();
        var svc = CreateSvc(db);
        var user = await svc.CreateUserAsync("u@fibradis.mx", "Fuerte1!", "User");

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => svc.ChangeOwnPasswordAsync(user.Id, "incorrecta123", "NuevaFuerte1!"));
    }

    [Fact]
    public async Task ChangeOwnPasswordAsync_WeakNewPassword_ThrowsInvalidUserDataException()
    {
        await using var db = CreateDb();
        var svc = CreateSvc(db);
        var user = await svc.CreateUserAsync("u@fibradis.mx", "Fuerte1!", "User");

        await Assert.ThrowsAsync<InvalidUserDataException>(
            () => svc.ChangeOwnPasswordAsync(user.Id, "Fuerte1!", "sinmayuscula1!"));
    }
}
