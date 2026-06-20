using Application.Auth;
using Application.Email;
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

    private static async Task<User> SeedUserAsync(
        AppDbContext db,
        string email,
        bool isActive,
        DateTime? trialEndsAt = null,
        SubscriptionType? subscriptionType = null,
        DateTime? subscriptionEndsAt = null,
        DateTime? subscriptionStartedAt = null)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Fuerte1!"),
            Role = UserRole.User,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow,
            TrialEndsAt = trialEndsAt,
            SubscriptionType = subscriptionType,
            SubscriptionEndsAt = subscriptionEndsAt,
            SubscriptionStartedAt = subscriptionStartedAt ?? (subscriptionType == SubscriptionType.Lifetime ? DateTime.UtcNow : null),
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
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

    // ── Register ───────────────────────────────────────────────────────────

    [Fact]
    public async Task RegisterAsync_ValidInputs_CreatesInactiveUserWithTrialFieldsNull()
    {
        await using var db = CreateDb();
        var svc = CreateSvc(db);

        var result = await svc.RegisterAsync(
            "nuevo@fibradis.mx",
            "Fuerte1!",
            "Apodo",
            HowDidYouHear.Google);

        Assert.Equal("nuevo@fibradis.mx", result.Email);
        Assert.Equal("User", result.Role);
        Assert.False(result.IsActive);
        Assert.Null(result.TrialEndsAt);
        Assert.Null(result.EmailConfirmedAt);

        var stored = await db.Users.FindAsync([result.Id]);
        Assert.NotNull(stored);
        Assert.False(stored!.IsActive);
        Assert.Equal("Apodo", stored.Apodo);
        Assert.Equal(HowDidYouHear.Google, stored.HowDidYouHear);
        Assert.Null(stored.EmailConfirmedAt);
        Assert.Null(stored.TrialEndsAt);
    }

    [Fact]
    public async Task RegisterAsync_DisposableEmail_ThrowsDisposableEmailException()
    {
        await using var db = CreateDb();
        var svc = CreateSvc(db);

        await Assert.ThrowsAsync<DisposableEmailException>(
            () => svc.RegisterAsync("user@mailinator.com", "Fuerte1!", null, null));
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ThrowsDuplicateEmailException()
    {
        await using var db = CreateDb();
        var svc = CreateSvc(db);

        await svc.RegisterAsync("dup@fibradis.mx", "Fuerte1!", null, null);

        await Assert.ThrowsAsync<DuplicateEmailException>(
            () => svc.RegisterAsync("dup@fibradis.mx", "Fuerte2@", null, null));
    }

    [Theory]
    [InlineData("notanemail")]
    [InlineData("user@")]
    [InlineData("@domain.com")]
    public async Task RegisterAsync_InvalidEmailFormat_ThrowsInvalidUserDataException(string email)
    {
        await using var db = CreateDb();
        var svc = CreateSvc(db);

        await Assert.ThrowsAsync<InvalidUserDataException>(
            () => svc.RegisterAsync(email, "Fuerte1!", null, null));
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

    // ── Subscription maintenance queries ───────────────────────────────────

    [Fact]
    public async Task FindUsersToDeactivateAsync_ReturnsExpiredSubscription()
    {
        await using var db = CreateDb();
        var svc = CreateSvc(db);
        var user = await SeedUserAsync(
            db,
            "expired-subscription@fibradis.mx",
            true,
            subscriptionType: SubscriptionType.Monthly,
            subscriptionEndsAt: DateTime.UtcNow.AddHours(-1));

        var result = await svc.FindUsersToDeactivateAsync();

        Assert.Single(result);
        Assert.Equal(user.Id, result[0].Id);
    }

    [Fact]
    public async Task FindUsersToDeactivateAsync_ExcludesLifetime()
    {
        await using var db = CreateDb();
        var svc = CreateSvc(db);
        await SeedUserAsync(
            db,
            "lifetime@fibradis.mx",
            true,
            subscriptionType: SubscriptionType.Lifetime,
            subscriptionStartedAt: DateTime.UtcNow.AddDays(-30));

        var result = await svc.FindUsersToDeactivateAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task FindUsersToDeactivateAsync_ReturnsExpiredTrial()
    {
        await using var db = CreateDb();
        var svc = CreateSvc(db);
        var user = await SeedUserAsync(
            db,
            "expired-trial@fibradis.mx",
            true,
            trialEndsAt: DateTime.UtcNow.AddHours(-2));

        var result = await svc.FindUsersToDeactivateAsync();

        Assert.Single(result);
        Assert.Equal(user.Id, result[0].Id);
    }

    [Fact]
    public async Task FindUsersToDeactivateAsync_ExcludesActiveUsers()
    {
        await using var db = CreateDb();
        var svc = CreateSvc(db);
        await SeedUserAsync(
            db,
            "active-trial@fibradis.mx",
            true,
            trialEndsAt: DateTime.UtcNow.AddDays(2));

        var result = await svc.FindUsersToDeactivateAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task FindUsersWithExpiringTrialAsync_ReturnsUsersInWindow()
    {
        await using var db = CreateDb();
        var svc = CreateSvc(db);
        var userInWindow = await SeedUserAsync(
            db,
            "trial-window@fibradis.mx",
            true,
            trialEndsAt: DateTime.UtcNow.Date.AddDays(3).AddHours(9));
        await SeedUserAsync(
            db,
            "trial-outside@fibradis.mx",
            true,
            trialEndsAt: DateTime.UtcNow.Date.AddDays(4));

        var result = await svc.FindUsersWithExpiringTrialAsync(3);

        Assert.Single(result);
        Assert.Equal(userInWindow.Id, result[0].Id);
    }

    [Fact]
    public async Task FindUsersWithExpiringSubscriptionAsync_Monthly()
    {
        await using var db = CreateDb();
        var svc = CreateSvc(db);
        var monthly = await SeedUserAsync(
            db,
            "monthly-window@fibradis.mx",
            true,
            subscriptionType: SubscriptionType.Monthly,
            subscriptionEndsAt: DateTime.UtcNow.Date.AddDays(3).AddHours(14));
        await SeedUserAsync(
            db,
            "annual-window@fibradis.mx",
            true,
            subscriptionType: SubscriptionType.Annual,
            subscriptionEndsAt: DateTime.UtcNow.Date.AddDays(3).AddHours(14));

        var result = await svc.FindUsersWithExpiringSubscriptionAsync(3, SubscriptionType.Monthly);

        Assert.Single(result);
        Assert.Equal(monthly.Id, result[0].Id);
    }

    [Fact]
    public async Task FindUsersWithExpiringSubscriptionAsync_Annual_30Days()
    {
        await using var db = CreateDb();
        var svc = CreateSvc(db);
        var annual = await SeedUserAsync(
            db,
            "annual-30@fibradis.mx",
            true,
            subscriptionType: SubscriptionType.Annual,
            subscriptionEndsAt: DateTime.UtcNow.Date.AddDays(30).AddHours(6));
        await SeedUserAsync(
            db,
            "annual-31@fibradis.mx",
            true,
            subscriptionType: SubscriptionType.Annual,
            subscriptionEndsAt: DateTime.UtcNow.Date.AddDays(31));

        var result = await svc.FindUsersWithExpiringSubscriptionAsync(30, SubscriptionType.Annual);

        Assert.Single(result);
        Assert.Equal(annual.Id, result[0].Id);
    }

    [Fact]
    public async Task FindUsersToDeactivateAsync_ExcludesConvertedMidTrialUser()
    {
        await using var db = CreateDb();
        var svc = CreateSvc(db);
        await SeedUserAsync(
            db,
            "converted@fibradis.mx",
            true,
            trialEndsAt: DateTime.UtcNow.AddHours(-1),
            subscriptionType: SubscriptionType.Monthly,
            subscriptionEndsAt: DateTime.UtcNow.AddDays(20));

        var result = await svc.FindUsersToDeactivateAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task BulkDeactivateUsersAsync_SetsIsActiveFalse()
    {
        await using var db = CreateDb();
        var svc = CreateSvc(db);
        var user1 = await SeedUserAsync(db, "bulk-1@fibradis.mx", true);
        var user2 = await SeedUserAsync(db, "bulk-2@fibradis.mx", true);
        var user3 = await SeedUserAsync(db, "bulk-3@fibradis.mx", true);

        await svc.BulkDeactivateUsersAsync([user1.Id, user2.Id]);

        var stored1 = await db.Users.FindAsync([user1.Id]);
        var stored2 = await db.Users.FindAsync([user2.Id]);
        var stored3 = await db.Users.FindAsync([user3.Id]);

        Assert.False(stored1!.IsActive);
        Assert.False(stored2!.IsActive);
        Assert.True(stored3!.IsActive);
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

    [Fact]
    public async Task ResetPasswordAsync_ValidUser_UpdatesPasswordHash()
    {
        await using var db = CreateDb();
        var svc = CreateSvc(db);
        var user = await svc.CreateUserAsync("reset@fibradis.mx", "Fuerte1!", "User");

        await svc.ResetPasswordAsync(user.Id, "Nueva1!x");

        var stored = await db.Users.FindAsync([user.Id]);
        Assert.NotNull(stored);
        Assert.True(BCrypt.Net.BCrypt.Verify("Nueva1!x", stored!.PasswordHash));
        Assert.False(BCrypt.Net.BCrypt.Verify("Fuerte1!", stored.PasswordHash));
    }

    [Fact]
    public async Task ResetPasswordAsync_WeakPassword_ThrowsInvalidUserDataException()
    {
        await using var db = CreateDb();
        var svc = CreateSvc(db);
        var user = await svc.CreateUserAsync("weak-reset@fibradis.mx", "Fuerte1!", "User");

        await Assert.ThrowsAsync<InvalidUserDataException>(
            () => svc.ResetPasswordAsync(user.Id, "sinmayuscula1!"));
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

    // ── ConfirmEmail ────────────────────────────────────────────────────────

    [Fact]
    public async Task ConfirmEmailAsync_ValidUser_SetsConfirmationAndTrial()
    {
        await using var db = CreateDb();
        var svc = CreateSvc(db);
        var user = await svc.RegisterAsync("confirm@fibradis.mx", "Fuerte1!", null, null);

        var confirmed = await svc.ConfirmEmailAsync(user.Id);

        Assert.NotNull(confirmed.EmailConfirmedAt);
        Assert.NotNull(confirmed.TrialEndsAt);
        Assert.True(confirmed.IsActive);
        Assert.Equal(TimeSpan.FromDays(14), confirmed.TrialEndsAt!.Value - confirmed.EmailConfirmedAt!.Value);

        var stored = await db.Users.FindAsync([user.Id]);
        Assert.NotNull(stored);
        Assert.True(stored!.IsActive);
        Assert.NotNull(stored.EmailConfirmedAt);
        Assert.NotNull(stored.TrialEndsAt);
    }

    [Fact]
    public async Task ConfirmEmailAsync_AlreadyConfirmed_ThrowsEmailAlreadyConfirmedException()
    {
        await using var db = CreateDb();
        var svc = CreateSvc(db);
        var user = await svc.RegisterAsync("already@fibradis.mx", "Fuerte1!", null, null);

        await svc.ConfirmEmailAsync(user.Id);

        await Assert.ThrowsAsync<EmailAlreadyConfirmedException>(
            () => svc.ConfirmEmailAsync(user.Id));
    }

    [Fact]
    public async Task ResendConfirmationAsync_QueuesRedirectConfirmationEmail()
    {
        await using var db = CreateDb();
        var svc = CreateSvc(db);
        await svc.CreateUserAsync("reenviar@fibradis.mx", "Fuerte1!", "User");

        var tokenService = new FakeEmailConfirmationTokenService();
        var emailService = new CapturingEmailService();

        await svc.ResendConfirmationAsync(
            "reenviar@fibradis.mx",
            tokenService,
            emailService,
            "https://localhost:5001");

        Assert.Single(emailService.Emails);
        Assert.Equal("reenviar@fibradis.mx", emailService.Emails[0].ToEmail);
        Assert.Contains("/api/v1/auth/confirm-email-redirect?token=generated-token", emailService.Emails[0].ConfirmationUrl);
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

    // ── GetProfileAsync — 14.3 ──────────────────────────────────────────────

    [Fact]
    public async Task GetProfileAsync_ActiveUser_ReturnsIsActiveTrue()
    {
        await using var db = CreateDb();
        var svc = CreateSvc(db);
        var user = await svc.CreateUserAsync("perfil@fibradis.mx", "Fuerte1!", "User");

        var profile = await svc.GetProfileAsync(user.Id);

        Assert.True(profile.IsActive);
        Assert.Null(profile.TrialEndsAt);
        Assert.Null(profile.FechaPago);
    }

    [Fact]
    public async Task GetProfileAsync_TrialUser_ReturnsTrialEndsAt()
    {
        await using var db = CreateDb();
        var svc = CreateSvc(db);
        var user = await svc.RegisterAsync("trial@fibradis.mx", "Fuerte1!", null, null);
        await svc.ConfirmEmailAsync(user.Id);

        var profile = await svc.GetProfileAsync(user.Id);

        Assert.True(profile.IsActive);
        Assert.NotNull(profile.TrialEndsAt);
    }

    [Fact]
    public async Task GetProfileAsync_UserWithPayment_ReturnsFechaPago()
    {
        await using var db = CreateDb();
        var svc = CreateSvc(db);
        var fecha = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        var user = await svc.CreateUserAsync("pago-perfil@fibradis.mx", "Fuerte1!", "User", 299m, fecha);

        var profile = await svc.GetProfileAsync(user.Id);

        Assert.Equal(fecha, profile.FechaPago);
    }

    [Fact]
    public async Task GetProfileAsync_InactiveUserNoTrial_ReturnsIsActiveFalse()
    {
        await using var db = CreateDb();
        var svc = CreateSvc(db);
        var user = await svc.RegisterAsync("inactivo@fibradis.mx", "Fuerte1!", null, null);
        // Usuario registrado pero sin confirmar email: IsActive=false, TrialEndsAt=null

        var profile = await svc.GetProfileAsync(user.Id);

        Assert.False(profile.IsActive);
        Assert.Null(profile.TrialEndsAt);
    }

    private sealed class CapturingEmailService : IEmailService
    {
        public List<(string ToEmail, string ConfirmationUrl)> Emails { get; } = [];

        public Task SendEmailConfirmationAsync(string toEmail, string confirmationUrl, CancellationToken ct)
        {
            Emails.Add((toEmail, confirmationUrl));
            return Task.CompletedTask;
        }

        public Task SendPasswordResetAsync(string toEmail, string resetUrl, CancellationToken ct) => Task.CompletedTask;
        public Task SendPaymentNotificationAsync(Guid userId, string userEmail, byte[]? fileContent, string? fileName, CancellationToken ct) => Task.CompletedTask;
        public Task SendAccessExpiredAsync(string toEmail, CancellationToken ct) => Task.CompletedTask;
        public Task SendAccessActivatedAsync(string toEmail, CancellationToken ct) => Task.CompletedTask;
        public Task SendTrialExpiringAsync(string toEmail, int daysLeft, CancellationToken ct) => Task.CompletedTask;
        public Task SendSubscriptionExpiringAsync(string toEmail, int daysLeft, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeEmailConfirmationTokenService : IEmailConfirmationTokenService
    {
        public string GenerateToken(Guid userId) => "generated-token";

        public EmailTokenValidationResult ValidateToken(string token)
            => new(Guid.Empty, IsExpired: false, IsValid: false);
    }
}
