using Application.Auth;
using Domain.Auth;
using Domain.Auth.Exceptions;
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Security;

public class UserService(AppDbContext db) : IUserService
{
    public async Task<UserData> CreateUserAsync(
        string email, string password, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new InvalidUserDataException("El correo electrónico es requerido.");
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
            throw new InvalidUserDataException("La contraseña debe tener al menos 8 caracteres.");

        var normalizedEmail = email.Trim().ToLowerInvariant();

        var exists = await db.Users
            .AnyAsync(u => u.Email == normalizedEmail, ct);

        if (exists)
            throw new DuplicateEmailException();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = normalizedEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = UserRole.User,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

        try
        {
            db.Users.Add(user);
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Unique constraint violation on Email — concurrent insert beat us
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

    private static UserData ToData(User u) =>
        new(u.Id, u.Email, u.Role.ToString(), u.IsActive, u.CreatedAt);
}
