using Application.Auth;
using Domain.Auth;
using Domain.Auth.Exceptions;
using Infrastructure.Persistence.SqlServer;
using Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Tests.Security;

public class UserServiceTests
{
    private static AppDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static UserService CreateSvc(AppDbContext db) =>
        new(db, new FakeEmailEncryptor());

    private class FakeEmailEncryptor : IEmailEncryptor
    {
        public string Encrypt(string plainEmail) => plainEmail;
        public string Decrypt(string storedEmail) => storedEmail;
    }

    // ── Create ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateUserAsync_ValidInputs_ReturnsUserData()
    {
        await using var db = CreateDb();
        var svc = CreateSvc(db);

        var result = await svc.CreateUserAsync("nuevo@fibradis.mx", "Fuerte1!", "User");

        Assert.Equal("nuevo@fibradis.mx", result.Email);
        Assert.Equal("User", result.Role);
        Assert.True(result.IsActive);
        Assert.NotEqual(Guid.Empty, result.Id);
    }

    [Fact]
    public async Task CreateUserAsync_AdminOpsRole_SetsRoleCorrectly()
    {
        await using var db = CreateDb();
        var svc = CreateSvc(db);

        var result = await svc.CreateUserAsync("admin@fibradis.mx", "Fuerte1!", "AdminOps");

        Assert.Equal("AdminOps", result.Role);
    }

    [Fact]
    public async Task CreateUserAsync_NormalizesEmailToLower()
    {
        await using var db = CreateDb();
        var svc = CreateSvc(db);

        var result = await svc.CreateUserAsync("Nuevo@FIBRADIS.MX", "Fuerte1!", "User");

        Assert.Equal("nuevo@fibradis.mx", result.Email);
    }

    [Fact]
    public async Task CreateUserAsync_DuplicateEmail_ThrowsDuplicateEmailException()
    {
        await using var db = CreateDb();
        var svc = CreateSvc(db);

        await svc.CreateUserAsync("dup@fibradis.mx", "Fuerte1!", "User");

        await Assert.ThrowsAsync<DuplicateEmailException>(
            () => svc.CreateUserAsync("dup@fibradis.mx", "Fuerte2@", "User"));
    }

    [Fact]
    public async Task CreateUserAsync_InvalidRole_ThrowsInvalidUserDataException()
    {
        await using var db = CreateDb();
        var svc = CreateSvc(db);

        await Assert.ThrowsAsync<InvalidUserDataException>(
            () => svc.CreateUserAsync("x@fibradis.mx", "Fuerte1!", "SuperAdmin"));
    }

    [Fact]
    public async Task CreateUserAsync_WithPaymentFields_StoresValues()
    {
        await using var db = CreateDb();
        var svc = CreateSvc(db);
        var fecha = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);

        var result = await svc.CreateUserAsync("pago@fibradis.mx", "Fuerte1!", "User", 150m, fecha);

        Assert.Equal(150m, result.Pago);
        Assert.Equal(fecha, result.FechaPago);
    }

    // ── Password validation ──────────────────────────────────────────────────

    [Fact]
    public async Task CreateUserAsync_PasswordTooShort_ThrowsInvalidUserDataException()
    {
        await using var db = CreateDb();
        var svc = CreateSvc(db);

        var ex = await Assert.ThrowsAsync<InvalidUserDataException>(
            () => svc.CreateUserAsync("x@fibradis.mx", "Ab1!", "User"));

        Assert.Contains("8 caracteres", ex.Message);
    }

    [Fact]
    public async Task CreateUserAsync_PasswordNoUppercase_ThrowsInvalidUserDataException()
    {
        await using var db = CreateDb();
        var svc = CreateSvc(db);

        var ex = await Assert.ThrowsAsync<InvalidUserDataException>(
            () => svc.CreateUserAsync("x@fibradis.mx", "fuerte1!", "User"));

        Assert.Contains("mayúscula", ex.Message);
    }

    [Fact]
    public async Task CreateUserAsync_PasswordNoLowercase_ThrowsInvalidUserDataException()
    {
        await using var db = CreateDb();
        var svc = CreateSvc(db);

        var ex = await Assert.ThrowsAsync<InvalidUserDataException>(
            () => svc.CreateUserAsync("x@fibradis.mx", "FUERTE1!", "User"));

        Assert.Contains("minúscula", ex.Message);
    }

    [Fact]
    public async Task CreateUserAsync_PasswordNoDigit_ThrowsInvalidUserDataException()
    {
        await using var db = CreateDb();
        var svc = CreateSvc(db);

        var ex = await Assert.ThrowsAsync<InvalidUserDataException>(
            () => svc.CreateUserAsync("x@fibradis.mx", "Fuerteee!", "User"));

        Assert.Contains("número", ex.Message);
    }

    [Fact]
    public async Task CreateUserAsync_PasswordNoSpecial_ThrowsInvalidUserDataException()
    {
        await using var db = CreateDb();
        var svc = CreateSvc(db);

        var ex = await Assert.ThrowsAsync<InvalidUserDataException>(
            () => svc.CreateUserAsync("x@fibradis.mx", "Fuerte123", "User"));

        Assert.Contains("carácter especial", ex.Message);
    }

    // ── GetAll ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllUsersAsync_ReturnsAllUsersOrderedByCreatedAt()
    {
        await using var db = CreateDb();
        var svc = CreateSvc(db);

        await svc.CreateUserAsync("a@fibradis.mx", "Fuerte1!", "User");
        await svc.CreateUserAsync("b@fibradis.mx", "Fuerte1!", "User");

        var users = await svc.GetAllUsersAsync();

        Assert.Equal(2, users.Count);
        Assert.Equal("a@fibradis.mx", users[0].Email);
        Assert.Equal("b@fibradis.mx", users[1].Email);
    }

    // ── SetUserActive ────────────────────────────────────────────────────────

    [Fact]
    public async Task SetUserActiveAsync_ExistingUser_UpdatesIsActive()
    {
        await using var db = CreateDb();
        var svc = CreateSvc(db);
        var user = await svc.CreateUserAsync("u@fibradis.mx", "Fuerte1!", "User");

        var result = await svc.SetUserActiveAsync(user.Id, false);

        Assert.False(result.IsActive);
    }

    [Fact]
    public async Task SetUserActiveAsync_NonExistingUser_ThrowsUserNotFoundException()
    {
        await using var db = CreateDb();
        var svc = CreateSvc(db);

        await Assert.ThrowsAsync<UserNotFoundException>(
            () => svc.SetUserActiveAsync(Guid.NewGuid(), false));
    }

    // ── ChangePassword ───────────────────────────────────────────────────────

    [Fact]
    public async Task ChangePasswordAsync_WeakPassword_ThrowsInvalidUserDataException()
    {
        await using var db = CreateDb();
        var svc = CreateSvc(db);
        var user = await svc.CreateUserAsync("u@fibradis.mx", "Fuerte1!", "User");

        await Assert.ThrowsAsync<InvalidUserDataException>(
            () => svc.ChangePasswordAsync(user.Id, "sinmayuscula1!"));
    }

    [Fact]
    public async Task ChangePasswordAsync_NonExistingUser_ThrowsUserNotFoundException()
    {
        await using var db = CreateDb();
        var svc = CreateSvc(db);

        await Assert.ThrowsAsync<UserNotFoundException>(
            () => svc.ChangePasswordAsync(Guid.NewGuid(), "Fuerte1!"));
    }

    // ── UpdatePayment ────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdatePaymentAsync_StoresPaymentValues()
    {
        await using var db = CreateDb();
        var svc = CreateSvc(db);
        var user = await svc.CreateUserAsync("u@fibradis.mx", "Fuerte1!", "User");
        var fecha = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);

        var result = await svc.UpdatePaymentAsync(user.Id, 300m, fecha);

        Assert.Equal(300m, result.Pago);
        Assert.Equal(fecha, result.FechaPago);
    }

    [Fact]
    public async Task UpdatePaymentAsync_NonExistingUser_ThrowsUserNotFoundException()
    {
        await using var db = CreateDb();
        var svc = CreateSvc(db);

        await Assert.ThrowsAsync<UserNotFoundException>(
            () => svc.UpdatePaymentAsync(Guid.NewGuid(), 100m, null));
    }

    // ── UpdateSubscription ──────────────────────────────────────────────────

    [Fact]
    public async Task UpdateSubscriptionAsync_AnnualPlan_UpdatesFieldsAndPersistsActiveState()
    {
        await using var db = CreateDb();
        var svc = CreateSvc(db);
        var user = await svc.CreateUserAsync("suscripcion@fibradis.mx", "Fuerte1!", "User");
        var startedAt = new DateTime(2026, 6, 18, 0, 0, 0, DateTimeKind.Utc);
        var endsAt = new DateTime(2027, 6, 18, 0, 0, 0, DateTimeKind.Utc);

        var result = await svc.UpdateSubscriptionAsync(user.Id, "Annual", startedAt, endsAt);

        Assert.Equal("Annual", result.SubscriptionType);
        Assert.Equal(startedAt, result.SubscriptionStartedAt);
        Assert.Equal(endsAt, result.SubscriptionEndsAt);
        Assert.True(result.IsActive);

        var stored = await db.Users.FindAsync([user.Id]);
        Assert.Equal(SubscriptionType.Annual, stored!.SubscriptionType);
        Assert.Equal(startedAt, stored.SubscriptionStartedAt);
        Assert.Equal(endsAt, stored.SubscriptionEndsAt);
        Assert.True(stored.IsActive);
    }

    [Fact]
    public async Task UpdateSubscriptionAsync_LifetimePlan_WithNullEndsAt_SetsActiveState()
    {
        await using var db = CreateDb();
        var svc = CreateSvc(db);
        var user = await svc.CreateUserAsync("lifetime@fibradis.mx", "Fuerte1!", "User");
        var startedAt = new DateTime(2026, 6, 18, 0, 0, 0, DateTimeKind.Utc);

        var result = await svc.UpdateSubscriptionAsync(user.Id, "Lifetime", startedAt, null);

        Assert.Equal("Lifetime", result.SubscriptionType);
        Assert.Equal(startedAt, result.SubscriptionStartedAt);
        Assert.Null(result.SubscriptionEndsAt);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task UpdateSubscriptionAsync_InvalidType_ThrowsInvalidUserDataException()
    {
        await using var db = CreateDb();
        var svc = CreateSvc(db);
        var user = await svc.CreateUserAsync("invalidtype@fibradis.mx", "Fuerte1!", "User");

        await Assert.ThrowsAsync<InvalidUserDataException>(
            () => svc.UpdateSubscriptionAsync(user.Id, "Quarterly", DateTime.UtcNow, null));
    }

    [Fact]
    public async Task UpdateSubscriptionAsync_MonthlyWithNullEndsAt_ThrowsInvalidUserDataException()
    {
        await using var db = CreateDb();
        var svc = CreateSvc(db);
        var user = await svc.CreateUserAsync("monthly-noends@fibradis.mx", "Fuerte1!", "User");

        await Assert.ThrowsAsync<InvalidUserDataException>(
            () => svc.UpdateSubscriptionAsync(user.Id, "Monthly", DateTime.UtcNow, null));
    }

    [Fact]
    public async Task UpdateSubscriptionAsync_DoesNotOverrideManualBan()
    {
        await using var db = CreateDb();
        var svc = CreateSvc(db);
        var user = await svc.CreateUserAsync("banned@fibradis.mx", "Fuerte1!", "User");
        await svc.SetUserActiveAsync(user.Id, false);

        var startedAt = new DateTime(2026, 6, 18, 0, 0, 0, DateTimeKind.Utc);
        var endsAt = new DateTime(2027, 6, 18, 0, 0, 0, DateTimeKind.Utc);
        var result = await svc.UpdateSubscriptionAsync(user.Id, "Annual", startedAt, endsAt);

        Assert.False(result.IsActive, "Un ban manual no debe ser revertido por UpdateSubscriptionAsync.");
    }

    // ── AcceptTerms ──────────────────────────────────────────────────────────

    [Fact]
    public async Task AcceptTermsAsync_SetsHasAcceptedTermsAndTimestamp()
    {
        await using var db = CreateDb();
        var svc = CreateSvc(db);
        var user = await svc.CreateUserAsync("u@fibradis.mx", "Fuerte1!", "User");

        await svc.AcceptTermsAsync(user.Id);

        var stored = await db.Users.FindAsync([user.Id]);
        Assert.True(stored!.HasAcceptedTerms);
        Assert.NotNull(stored.TermsAcceptedAt);
    }

    [Fact]
    public async Task AcceptTermsAsync_IsIdempotent_PreservesOriginalTimestamp()
    {
        await using var db = CreateDb();
        var svc = CreateSvc(db);
        var user = await svc.CreateUserAsync("u@fibradis.mx", "Fuerte1!", "User");

        await svc.AcceptTermsAsync(user.Id);
        var stored = await db.Users.FindAsync([user.Id]);
        var originalTimestamp = stored!.TermsAcceptedAt;

        await svc.AcceptTermsAsync(user.Id);

        stored = await db.Users.FindAsync([user.Id]);
        Assert.Equal(originalTimestamp, stored!.TermsAcceptedAt);
    }

    [Fact]
    public async Task AcceptTermsAsync_NonExistingUser_ThrowsUserNotFoundException()
    {
        await using var db = CreateDb();
        var svc = CreateSvc(db);

        await Assert.ThrowsAsync<UserNotFoundException>(
            () => svc.AcceptTermsAsync(Guid.NewGuid()));
    }
}
