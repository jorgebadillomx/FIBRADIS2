using Application.Auth;
using Application.Email;
using Domain.Auth;
using Domain.Auth.Exceptions;
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Security;

public class UserService(AppDbContext db, IEmailEncryptor emailEncryptor) : IUserService
{
    public async Task<UserData> RegisterAsync(
        string email,
        string password,
        string? apodo,
        HowDidYouHear? howDidYouHear,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new InvalidUserDataException("El correo electrónico es requerido.");

        ValidateStrongPassword(password);

        var normalizedEmail = email.Trim().ToLowerInvariant();
        if (!IsValidEmailFormat(normalizedEmail))
            throw new InvalidUserDataException("El formato del correo electrónico no es válido.");

        if (DisposableEmailDomains.IsDisposable(normalizedEmail))
            throw new DisposableEmailException();

        var encryptedEmail = emailEncryptor.Encrypt(normalizedEmail);
        var exists = await db.Users.AnyAsync(u => u.Email == encryptedEmail, ct);
        if (exists)
            throw new DuplicateEmailException();

        var normalizedApodo = NormalizeApodo(apodo);
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = encryptedEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = UserRole.User,
            IsActive = false,
            CreatedAt = DateTime.UtcNow,
            Apodo = normalizedApodo,
            HowDidYouHear = howDidYouHear,
            EmailConfirmedAt = null,
            TrialEndsAt = null,
        };

        try
        {
            db.Users.Add(user);
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            throw new DuplicateEmailException();
        }

        return ToData(user);
    }

    public async Task<UserData> CreateUserAsync(
        string email,
        string password,
        string role,
        decimal? pago = null,
        DateTime? fechaPago = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new InvalidUserDataException("El correo electrónico es requerido.");

        ValidateStrongPassword(password);

        if (!Enum.TryParse<UserRole>(role, ignoreCase: true, out var userRole))
            throw new InvalidUserDataException($"Rol inválido: {role}. Valores válidos: User, AdminOps.");

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var encryptedEmail = emailEncryptor.Encrypt(normalizedEmail);

        var exists = await db.Users.AnyAsync(u => u.Email == encryptedEmail, ct);
        if (exists)
            throw new DuplicateEmailException();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = encryptedEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = userRole,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Pago = pago,
            FechaPago = fechaPago,
        };

        try
        {
            db.Users.Add(user);
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            throw new DuplicateEmailException();
        }

        return ToData(user);
    }

    public async Task<IReadOnlyList<UserData>> GetAllUsersAsync(CancellationToken ct = default)
    {
        var users = await db.Users
            .OrderBy(u => u.CreatedAt)
            .ToListAsync(ct);

        return users.Select(ToData).ToList();
    }

    public async Task<UserData> SetUserActiveAsync(Guid id, bool isActive, CancellationToken ct = default)
    {
        var user = await db.Users.FindAsync([id], ct)
            ?? throw new UserNotFoundException();

        user.IsActive = isActive;
        await db.SaveChangesAsync(ct);
        return ToData(user);
    }

    public async Task<UserData> ConfirmEmailAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await db.Users.FindAsync([userId], ct)
            ?? throw new UserNotFoundException();

        // Riesgo de race condition aceptado por spec (security checklist 14.2): en caso de dos
        // requests concurrentes, el segundo retornará token_already_used al leer EmailConfirmedAt != null.
        if (user.EmailConfirmedAt is not null)
            throw new EmailAlreadyConfirmedException();

        var confirmedAt = DateTime.UtcNow;
        user.EmailConfirmedAt = confirmedAt;
        user.TrialEndsAt = confirmedAt.AddDays(14);
        user.IsActive = user.ComputedIsActive;

        await db.SaveChangesAsync(ct);
        return ToData(user);
    }

    public async Task<UserData?> FindByIdAsync(Guid id, CancellationToken ct = default)
    {
        var user = await db.Users.FindAsync([id], ct);
        return user is null ? null : ToData(user);
    }

    public async Task ChangePasswordAsync(Guid id, string newPassword, CancellationToken ct = default)
    {
        var user = await db.Users.FindAsync([id], ct)
            ?? throw new UserNotFoundException();

        ValidateStrongPassword(newPassword);

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await db.SaveChangesAsync(ct);
    }

    public async Task<UserProfileData> GetProfileAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await db.Users.FindAsync([userId], ct)
            ?? throw new UserNotFoundException();

        return new UserProfileData(
            user.Id,
            emailEncryptor.Decrypt(user.Email),
            user.Role.ToString(),
            user.Apodo,
            user.IsActive,
            user.TrialEndsAt,
            user.FechaPago);
    }

    public async Task UpdateApodoAsync(Guid userId, string? apodo, CancellationToken ct = default)
    {
        var user = await db.Users.FindAsync([userId], ct)
            ?? throw new UserNotFoundException();

        if (apodo is not null)
        {
            if (apodo.Length > 50)
                throw new InvalidUserDataException("El apodo no puede tener más de 50 caracteres.");

            if (apodo.Any(char.IsControl))
                throw new InvalidUserDataException("El apodo contiene caracteres no permitidos.");
        }

        user.Apodo = apodo;
        await db.SaveChangesAsync(ct);
    }

    public async Task ChangeOwnPasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken ct = default)
    {
        var user = await db.Users.FindAsync([userId], ct)
            ?? throw new UserNotFoundException();

        if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
            throw new InvalidCredentialsException();

        ValidateStrongPassword(newPassword);

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await db.SaveChangesAsync(ct);
    }

    public async Task<UserData> UpdatePaymentAsync(
        Guid id, decimal? pago, DateTime? fechaPago, CancellationToken ct = default)
    {
        var user = await db.Users.FindAsync([id], ct)
            ?? throw new UserNotFoundException();

        user.Pago = pago;
        user.FechaPago = fechaPago;
        await db.SaveChangesAsync(ct);
        return ToData(user);
    }

    public async Task<UserData> UpdateSubscriptionAsync(
        Guid id,
        string type,
        DateTime startedAt,
        DateTime? endsAt,
        CancellationToken ct = default)
    {
        var user = await db.Users.FindAsync([id], ct)
            ?? throw new UserNotFoundException();

        if (!Enum.TryParse<SubscriptionType>(type, ignoreCase: true, out var subscriptionType))
            throw new InvalidUserDataException($"Tipo de suscripción inválido: {type}. Valores válidos: Monthly, Annual, Lifetime.");

        if (subscriptionType != SubscriptionType.Lifetime && !endsAt.HasValue)
            throw new InvalidUserDataException("EndsAt es obligatorio para suscripciones Monthly y Annual.");

        user.SubscriptionType = subscriptionType;
        user.SubscriptionStartedAt = DateTime.SpecifyKind(startedAt, DateTimeKind.Utc);
        user.SubscriptionEndsAt = endsAt.HasValue ? DateTime.SpecifyKind(endsAt.Value, DateTimeKind.Utc) : null;
        user.TrialEndsAt = null;

        await db.SaveChangesAsync(ct);
        return ToData(user);
    }

    public async Task AcceptTermsAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await db.Users.FindAsync([userId], ct)
            ?? throw new UserNotFoundException();

        if (user.HasAcceptedTerms)
            return;

        user.HasAcceptedTerms = true;
        user.TermsAcceptedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task ResendConfirmationAsync(
        string email,
        IEmailConfirmationTokenService tokenService,
        IEmailService emailService,
        string baseUrl,
        CancellationToken ct = default)
    {
        try
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            var encryptedEmail = emailEncryptor.Encrypt(normalizedEmail);
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == encryptedEmail, ct);

            if (user is null || user.EmailConfirmedAt is not null)
                return;

            var token = tokenService.GenerateToken(user.Id);
            var confirmationUrl = $"{baseUrl}/confirmar-email?token={Uri.EscapeDataString(token)}";
            await emailService.SendEmailConfirmationAsync(normalizedEmail, confirmationUrl, ct);
        }
        catch (Exception)
        {
            // Silenciar cualquier excepción — nunca revelar estado al caller
        }
    }

    private UserData ToData(User u) =>
        new(
            u.Id,
            emailEncryptor.Decrypt(u.Email),
            u.Role.ToString(),
            u.IsActive,
            u.CreatedAt,
            u.Pago,
            u.FechaPago,
            u.SubscriptionType?.ToString(),
            u.SubscriptionStartedAt,
            u.SubscriptionEndsAt,
            u.TrialEndsAt,
            u.EmailConfirmedAt);

    private static void ValidateStrongPassword(string password)
    {
        if (password.Length < 8)
            throw new InvalidUserDataException("La contraseña debe tener al menos 8 caracteres.");
        if (!password.Any(char.IsUpper))
            throw new InvalidUserDataException("La contraseña debe contener al menos una letra mayúscula.");
        if (!password.Any(char.IsLower))
            throw new InvalidUserDataException("La contraseña debe contener al menos una letra minúscula.");
        if (!password.Any(char.IsDigit))
            throw new InvalidUserDataException("La contraseña debe contener al menos un número.");
        if (!password.Any(c => !char.IsLetterOrDigit(c)))
            throw new InvalidUserDataException("La contraseña debe contener al menos un carácter especial.");
    }

    private static string? NormalizeApodo(string? apodo)
    {
        if (string.IsNullOrWhiteSpace(apodo))
            return null;

        var normalized = apodo.Trim();
        if (normalized.Length > 50)
            throw new InvalidUserDataException("El apodo no puede tener más de 50 caracteres.");

        if (normalized.Any(char.IsControl))
            throw new InvalidUserDataException("El apodo contiene caracteres no permitidos.");

        return normalized;
    }

    private static bool IsValidEmailFormat(string email)
    {
        var atIndex = email.LastIndexOf('@');
        if (atIndex <= 0 || atIndex == email.Length - 1)
            return false;

        var domain = email[(atIndex + 1)..];
        var dotIndex = domain.IndexOf('.');
        return dotIndex > 0 && dotIndex < domain.Length - 1;
    }
}
