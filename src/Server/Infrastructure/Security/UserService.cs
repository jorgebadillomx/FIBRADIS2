using Application.Auth;
using Domain.Auth;
using Domain.Auth.Exceptions;
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Security;

public class UserService(AppDbContext db, IEmailEncryptor emailEncryptor) : IUserService
{
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

    public async Task ChangePasswordAsync(Guid id, string newPassword, CancellationToken ct = default)
    {
        var user = await db.Users.FindAsync([id], ct)
            ?? throw new UserNotFoundException();

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

    private UserData ToData(User u) =>
        new(u.Id, emailEncryptor.Decrypt(u.Email), u.Role.ToString(), u.IsActive, u.CreatedAt, u.Pago, u.FechaPago);

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
}
