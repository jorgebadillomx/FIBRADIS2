using Application.Auth;
using Domain.Auth;
using Domain.Auth.Exceptions;
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Security;

public class AuthService(AppDbContext db, ITokenService tokenService) : IAuthService
{
    public async Task<(string AccessToken, string RefreshToken)> LoginAsync(
        string email, string password, CancellationToken ct = default)
    {
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Email == email && u.IsActive, ct);

        if (user is null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            throw new InvalidCredentialsException();

        return await IssueTokensAsync(user, ct);
    }

    public async Task<(string AccessToken, string RefreshToken)> RefreshAsync(
        string rawRefreshToken, CancellationToken ct = default)
    {
        var candidates = await db.RefreshTokens
            .Include(rt => rt.User)
            .Where(rt => rt.RevokedAt == null && rt.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(ct);

        var stored = candidates.FirstOrDefault(rt =>
            BCrypt.Net.BCrypt.Verify(rawRefreshToken, rt.TokenHash));

        if (stored is null || !stored.User!.IsActive)
            throw new InvalidRefreshTokenException();

        stored.RevokedAt = DateTime.UtcNow;

        try
        {
            return await IssueTokensAsync(stored.User!, ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new InvalidRefreshTokenException();
        }
    }

    private async Task<(string, string)> IssueTokensAsync(User user, CancellationToken ct)
    {
        var rawRefresh = tokenService.GenerateRefreshToken();

        db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = tokenService.HashRefreshToken(rawRefresh),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
        });

        await db.SaveChangesAsync(ct);

        return (tokenService.GenerateAccessToken(user), rawRefresh);
    }
}
