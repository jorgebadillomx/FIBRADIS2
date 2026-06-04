using Application.Auth;
using Domain.Auth.Exceptions;
using Infrastructure.Persistence.SqlServer;
using Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Tests.Security;

public class UserServiceTests
{
    private static AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task CreateUserAsync_ValidEmailAndPassword_ReturnsUserSummary()
    {
        await using var db = CreateInMemoryContext();
        var svc = new UserService(db);

        var result = await svc.CreateUserAsync("nuevo@fibradis.mx", "Pass123!");

        Assert.Equal("nuevo@fibradis.mx", result.Email);
        Assert.Equal("User", result.Role);
        Assert.True(result.IsActive);
        Assert.NotEqual(Guid.Empty, result.Id);
    }

    [Fact]
    public async Task CreateUserAsync_NormalizesEmailToLower()
    {
        await using var db = CreateInMemoryContext();
        var svc = new UserService(db);

        var result = await svc.CreateUserAsync("Nuevo@FIBRADIS.MX", "Pass123!");

        Assert.Equal("nuevo@fibradis.mx", result.Email);
    }

    [Fact]
    public async Task CreateUserAsync_DuplicateEmail_ThrowsDuplicateEmailException()
    {
        await using var db = CreateInMemoryContext();
        var svc = new UserService(db);

        await svc.CreateUserAsync("dup@fibradis.mx", "Pass123!");

        await Assert.ThrowsAsync<DuplicateEmailException>(
            () => svc.CreateUserAsync("dup@fibradis.mx", "OtroPass!"));
    }

    [Fact]
    public async Task GetAllUsersAsync_ReturnsAllUsers_OrderedByCreatedAt()
    {
        await using var db = CreateInMemoryContext();
        var svc = new UserService(db);

        await svc.CreateUserAsync("a@fibradis.mx", "Pass123!");
        await svc.CreateUserAsync("b@fibradis.mx", "Pass123!");

        var users = await svc.GetAllUsersAsync();

        Assert.Equal(2, users.Count);
        Assert.Equal("a@fibradis.mx", users[0].Email);
        Assert.Equal("b@fibradis.mx", users[1].Email);
    }
}
