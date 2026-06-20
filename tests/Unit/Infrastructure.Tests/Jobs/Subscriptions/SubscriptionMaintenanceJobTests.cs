using Application.Auth;
using Application.Email;
using Application.Jobs;
using Domain.Auth;
using Domain.Jobs;
using Infrastructure.Jobs.Subscriptions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Infrastructure.Tests.Jobs.Subscriptions;

public class SubscriptionMaintenanceJobTests
{
    [Fact]
    public async Task ExecuteAsync_ProcessesExpirationsAndReminders()
    {
        var expiredSubscription = CreateUserData("expired@fibradis.mx", subscriptionEndsAt: DateTime.UtcNow.AddHours(-2));
        var expiringTrial = CreateUserData("trial@fibradis.mx", trialEndsAt: DateTime.UtcNow.Date.AddDays(3).AddHours(8));
        var expiringMonthly = CreateUserData(
            "monthly@fibradis.mx",
            subscriptionType: SubscriptionType.Monthly.ToString(),
            subscriptionEndsAt: DateTime.UtcNow.Date.AddDays(3).AddHours(11));
        var expiringAnnual = CreateUserData(
            "annual@fibradis.mx",
            subscriptionType: SubscriptionType.Annual.ToString(),
            subscriptionEndsAt: DateTime.UtcNow.Date.AddDays(30).AddHours(11));

        var userService = new FakeUserService(
            [expiredSubscription],
            [expiringTrial],
            new Dictionary<(int DaysAhead, SubscriptionType Type), IReadOnlyList<UserData>>
            {
                [(3, SubscriptionType.Monthly)] = [expiringMonthly],
                [(30, SubscriptionType.Annual)] = [expiringAnnual],
            });
        var emailService = new FakeUserServiceEmail();
        var runLogs = new CapturingRunLogRepo();
        var errorLogs = new CapturingErrorLogRepo();
        var job = new SubscriptionMaintenanceJob(
            userService,
            emailService,
            runLogs,
            errorLogs,
            NullLogger<SubscriptionMaintenanceJob>.Instance);

        await job.ExecuteAsync();

        Assert.Single(userService.BulkDeactivateCalls);
        Assert.Equal(expiredSubscription.Id, userService.BulkDeactivateCalls[0][0]);
        Assert.Equal([expiredSubscription.Email], emailService.AccessExpiredEmails);
        Assert.Equal([(expiringTrial.Email, 3)], emailService.TrialExpiringEmails);
        Assert.Equal(
            [(expiringMonthly.Email, 3), (expiringAnnual.Email, 30)],
            emailService.SubscriptionExpiringEmails);

        Assert.Single(runLogs.Items);
        Assert.Equal("Completed", runLogs.Items[0].Status);
        Assert.Equal(5, runLogs.Items[0].ItemsProcessed);
        Assert.Equal(0, runLogs.Items[0].ErrorCount);
        Assert.Empty(errorLogs.Items);
    }

    [Fact]
    public async Task ExecuteAsync_WhenEmailFails_LogsPipelineErrorAndContinues()
    {
        var expiredSubscription = CreateUserData("expired-fail@fibradis.mx", subscriptionEndsAt: DateTime.UtcNow.AddHours(-2));
        var expiringTrial = CreateUserData("trial-ok@fibradis.mx", trialEndsAt: DateTime.UtcNow.Date.AddDays(3).AddHours(8));

        var userService = new FakeUserService(
            [expiredSubscription],
            [expiringTrial],
            new Dictionary<(int DaysAhead, SubscriptionType Type), IReadOnlyList<UserData>>());
        var emailService = new FakeUserServiceEmail(
            failAccessExpiredForEmail: expiredSubscription.Email);
        var runLogs = new CapturingRunLogRepo();
        var errorLogs = new CapturingErrorLogRepo();
        var job = new SubscriptionMaintenanceJob(
            userService,
            emailService,
            runLogs,
            errorLogs,
            NullLogger<SubscriptionMaintenanceJob>.Instance);

        await job.ExecuteAsync();

        Assert.Single(errorLogs.Items);
        Assert.Equal("SubscriptionMaintenance", errorLogs.Items[0].Pipeline);
        Assert.Contains("userId=", errorLogs.Items[0].Context ?? string.Empty);
        Assert.Equal([(expiringTrial.Email, 3)], emailService.TrialExpiringEmails);

        Assert.Single(runLogs.Items);
        Assert.Equal("Completed", runLogs.Items[0].Status);
        Assert.Equal(1, runLogs.Items[0].ErrorCount);
    }

    private static UserData CreateUserData(
        string email,
        bool isActive = true,
        string? subscriptionType = null,
        DateTime? subscriptionEndsAt = null,
        DateTime? trialEndsAt = null)
        => new(
            Guid.NewGuid(),
            email,
            "User",
            isActive,
            DateTime.UtcNow,
            null,
            null,
            subscriptionType,
            DateTime.UtcNow,
            subscriptionEndsAt,
            trialEndsAt,
            null);

    private sealed class FakeUserService(
        IReadOnlyList<UserData> toDeactivate,
        IReadOnlyList<UserData> expiringTrials,
        IReadOnlyDictionary<(int DaysAhead, SubscriptionType Type), IReadOnlyList<UserData>> expiringSubscriptions)
        : IUserService
    {
        public List<IReadOnlyList<Guid>> BulkDeactivateCalls { get; } = [];

        public Task<IReadOnlyList<UserData>> FindUsersToDeactivateAsync(CancellationToken ct = default)
            => Task.FromResult(toDeactivate);

        public Task BulkDeactivateUsersAsync(IReadOnlyList<Guid> ids, CancellationToken ct = default)
        {
            BulkDeactivateCalls.Add(ids.ToList());
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<UserData>> FindUsersWithExpiringTrialAsync(int daysAhead, CancellationToken ct = default)
            => Task.FromResult(expiringTrials);

        public Task<IReadOnlyList<UserData>> FindUsersWithExpiringSubscriptionAsync(
            int daysAhead,
            SubscriptionType type,
            CancellationToken ct = default)
            => Task.FromResult(
                expiringSubscriptions.TryGetValue((daysAhead, type), out var users)
                    ? users
                    : Array.Empty<UserData>());

        public Task<UserData> RegisterAsync(string email, string password, string? apodo, HowDidYouHear? howDidYouHear, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<UserData> CreateUserAsync(string email, string password, string role, decimal? pago = null, DateTime? fechaPago = null, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<UserData>> GetAllUsersAsync(CancellationToken ct = default) => throw new NotImplementedException();
        public Task<UserData> SetUserActiveAsync(Guid id, bool isActive, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<UserData> ConfirmEmailAsync(Guid userId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<UserData?> FindByIdAsync(Guid id, CancellationToken ct = default) => throw new NotImplementedException();
        public Task ChangePasswordAsync(Guid id, string newPassword, CancellationToken ct = default) => throw new NotImplementedException();
        public Task ResetPasswordAsync(Guid userId, string newPassword, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<UserProfileData> GetProfileAsync(Guid userId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task UpdateApodoAsync(Guid userId, string? apodo, CancellationToken ct = default) => throw new NotImplementedException();
        public Task ChangeOwnPasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<UserData> UpdatePaymentAsync(Guid id, decimal? pago, DateTime? fechaPago, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<UserData> UpdateSubscriptionAsync(Guid id, string type, DateTime startedAt, DateTime? endsAt, CancellationToken ct = default) => throw new NotImplementedException();
        public Task AcceptTermsAsync(Guid userId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task ResendConfirmationAsync(string email, IEmailConfirmationTokenService tokenService, IEmailService emailService, string baseUrl, CancellationToken ct = default) => throw new NotImplementedException();
    }

    private sealed class FakeUserServiceEmail(string? failAccessExpiredForEmail = null) : IEmailService
    {
        public List<string> AccessExpiredEmails { get; } = [];
        public List<(string ToEmail, int DaysLeft)> TrialExpiringEmails { get; } = [];
        public List<(string ToEmail, int DaysLeft)> SubscriptionExpiringEmails { get; } = [];

        public Task SendEmailConfirmationAsync(string toEmail, string confirmationUrl, CancellationToken ct) => Task.CompletedTask;
        public Task SendPasswordResetAsync(string toEmail, string resetUrl, CancellationToken ct) => Task.CompletedTask;
        public Task SendPaymentNotificationAsync(Guid userId, string userEmail, byte[]? fileContent, string? fileName, CancellationToken ct) => Task.CompletedTask;

        public Task SendAccessExpiredAsync(string toEmail, CancellationToken ct)
        {
            if (string.Equals(toEmail, failAccessExpiredForEmail, StringComparison.OrdinalIgnoreCase))
                throw new HttpRequestException("Resend no disponible");

            AccessExpiredEmails.Add(toEmail);
            return Task.CompletedTask;
        }

        public Task SendAccessActivatedAsync(string toEmail, CancellationToken ct) => Task.CompletedTask;

        public Task SendTrialExpiringAsync(string toEmail, int daysLeft, CancellationToken ct)
        {
            TrialExpiringEmails.Add((toEmail, daysLeft));
            return Task.CompletedTask;
        }

        public Task SendSubscriptionExpiringAsync(string toEmail, int daysLeft, CancellationToken ct)
        {
            SubscriptionExpiringEmails.Add((toEmail, daysLeft));
            return Task.CompletedTask;
        }
    }

    private sealed class CapturingRunLogRepo : IPipelineRunLogRepository
    {
        public List<PipelineRunLog> Items { get; } = [];

        public Task AddAsync(PipelineRunLog entry, CancellationToken ct = default)
        {
            Items.Add(entry);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<PipelineRunLog>> GetRecentAsync(string? pipeline, int take, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<PipelineRunLog>>([]);

        public Task<PipelineRunLog?> GetLastCompletedAsync(string pipeline, CancellationToken ct = default)
            => Task.FromResult<PipelineRunLog?>(null);
    }

    private sealed class CapturingErrorLogRepo : IPipelineErrorLogRepository
    {
        public List<PipelineErrorLog> Items { get; } = [];

        public Task LogErrorAsync(PipelineErrorLog entry, CancellationToken ct = default)
        {
            Items.Add(entry);
            return Task.CompletedTask;
        }

        public Task<(IReadOnlyList<PipelineErrorLog> Items, int Total)> GetPagedAsync(string? pipeline, int page, int pageSize, CancellationToken ct = default)
            => Task.FromResult<(IReadOnlyList<PipelineErrorLog>, int)>(([], 0));
    }
}
