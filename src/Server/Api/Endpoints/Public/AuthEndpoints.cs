using Application.Auth;
using Application.Email;
using Domain.Auth;
using Domain.Auth.Exceptions;
using Infrastructure.Persistence.SqlServer;
using SharedApiContracts.Auth;
using Microsoft.EntityFrameworkCore;

namespace Api.Endpoints.Public;

public static class AuthEndpoints
{
    private const string RefreshTokenCookie = "refreshToken";
    // Cookie no-HttpOnly que el cliente lee para saber si tiene sesión activa,
    // evitando llamar a /refresh en cada visita anónima (L-4 SEO audit)
    private const string SessionIndicatorCookie = "s";

    public static IEndpointRouteBuilder MapAuth(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth").WithTags("Auth");

        group.MapPost("/register", async (
            RegisterRequest request,
            IUserService userService,
            IEmailConfirmationTokenService tokenService,
            IEmailService emailService,
            IConfiguration config,
            ILogger<Program> logger,
            CancellationToken ct) =>
        {
            // P1: Validar App:BaseUrl antes de persistir el usuario para evitar estado huérfano
            var baseUrl = config["App:BaseUrl"]?.TrimEnd('/');
            if (string.IsNullOrWhiteSpace(baseUrl))
                return Results.Problem(
                    detail: "App:BaseUrl no está configurado.",
                    statusCode: StatusCodes.Status503ServiceUnavailable);

            if (DisposableEmailDomains.IsDisposable(request.Email))
                return Results.UnprocessableEntity(new { code = "disposable_email" });

            HowDidYouHear? howDidYouHear = null;
            if (!string.IsNullOrWhiteSpace(request.HowDidYouHear))
            {
                if (!Enum.TryParse<HowDidYouHear>(request.HowDidYouHear, ignoreCase: true, out var parsedHowDidYouHear))
                    return Results.BadRequest(new { code = "invalid_user_data" });

                howDidYouHear = parsedHowDidYouHear;
            }

            try
            {
                var user = await userService.RegisterAsync(
                    request.Email,
                    request.Password,
                    request.Apodo,
                    howDidYouHear,
                    ct);

                try
                {
                    var token = tokenService.GenerateToken(user.Id);
                    var confirmationUrl = $"{baseUrl}/api/v1/auth/confirm-email-redirect?token={Uri.EscapeDataString(token)}";
                    await emailService.SendEmailConfirmationAsync(user.Email, confirmationUrl, ct);
                }
                catch (Exception emailEx)
                {
                    logger.LogError(emailEx, "Error al enviar email de confirmación tras registro de {UserId}.", user.Id);
                }

                return Results.Ok(new RegisterResponse("Revisa tu email para confirmar tu cuenta."));
            }
            catch (DisposableEmailException)
            {
                return Results.UnprocessableEntity(new { code = "disposable_email" });
            }
            catch (DuplicateEmailException)
            {
                return Results.Conflict(new { code = "duplicate_email" });
            }
            catch (InvalidUserDataException ex)
            {
                return Results.BadRequest(new { code = "invalid_user_data", message = ex.Message });
            }
        })
        .AllowAnonymous()
        .Produces<RegisterResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status409Conflict)
        .Produces(StatusCodes.Status422UnprocessableEntity);

        group.MapGet("/confirm-email-redirect", async (
            string? token,
            IEmailConfirmationTokenService tokenService,
            IUserService userService,
            IConfiguration config,
            ILogger<Program> logger,
            CancellationToken ct) =>
        {
            var baseUrl = config["App:BaseUrl"]?.TrimEnd('/') ?? string.Empty;
            var confirmedUrl = $"{baseUrl}/confirmar-email";
            var errorUrl = $"{confirmedUrl}?status=error";

            try
            {
                var validation = tokenService.ValidateToken(token ?? string.Empty);
                if (!validation.IsValid)
                    return Results.Redirect(errorUrl);

                if (validation.IsExpired)
                    return Results.Redirect($"{confirmedUrl}?status=expired");

                var user = await userService.FindByIdAsync(validation.UserId, ct);
                if (user is null)
                    return Results.Redirect(errorUrl);

                if (user.EmailConfirmedAt is not null)
                    return Results.Redirect($"{confirmedUrl}?status=already_confirmed");

                try
                {
                    var confirmed = await userService.ConfirmEmailAsync(validation.UserId, ct);
                    var trialEndsAt = confirmed.TrialEndsAt
                        ?? throw new InvalidOperationException("ConfirmEmailAsync no asignó TrialEndsAt.");
                    var t = Uri.EscapeDataString(
                        new DateTimeOffset(DateTime.SpecifyKind(trialEndsAt, DateTimeKind.Utc)).ToString("O"));

                    return Results.Redirect($"{confirmedUrl}?status=confirmed&t={t}");
                }
                catch (EmailAlreadyConfirmedException)
                {
                    return Results.Redirect($"{confirmedUrl}?status=already_confirmed");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error en confirm-email-redirect — redirigiendo a error.");
                return Results.Redirect(errorUrl);
            }
        })
        .AllowAnonymous()
        .Produces(StatusCodes.Status302Found);

        group.MapGet("/confirm-email", async (
            string? token,
            IEmailConfirmationTokenService tokenService,
            IUserService userService,
            CancellationToken ct) =>
        {
            var validation = tokenService.ValidateToken(token ?? string.Empty);
            if (!validation.IsValid)
                return Results.BadRequest(new { code = "token_invalid" });

            if (validation.IsExpired)
                return Results.BadRequest(new { code = "token_expired" });

            var user = await userService.FindByIdAsync(validation.UserId, ct);
            if (user is null)
                return Results.NotFound();

            if (user.EmailConfirmedAt is not null)
                return Results.BadRequest(new { code = "token_already_used" });

            try
            {
                var confirmed = await userService.ConfirmEmailAsync(validation.UserId, ct);
                var trialEndsAt = confirmed.TrialEndsAt
                    ?? throw new InvalidOperationException("ConfirmEmailAsync no asignó TrialEndsAt.");
                return Results.Ok(new ConfirmEmailResponse(new DateTimeOffset(
                    DateTime.SpecifyKind(trialEndsAt, DateTimeKind.Utc))));
            }
            catch (EmailAlreadyConfirmedException)
            {
                return Results.BadRequest(new { code = "token_already_used" });
            }
        })
        .AllowAnonymous()
        .Produces<ConfirmEmailResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/login", async (
            LoginRequest request,
            IAuthService authService,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var (accessToken, refreshToken) = await authService.LoginAsync(request.Email, request.Password, ct);
            SetRefreshCookie(ctx, refreshToken);
            return Results.Ok(new LoginResponse(accessToken));
        })
        .AllowAnonymous()
        .Produces<LoginResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/refresh", async (
            IAuthService authService,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var rawToken = ctx.Request.Cookies[RefreshTokenCookie];
            if (string.IsNullOrEmpty(rawToken))
                return Results.Unauthorized();

            var (accessToken, newRefreshToken) = await authService.RefreshAsync(rawToken, ct);
            SetRefreshCookie(ctx, newRefreshToken);
            return Results.Ok(new RefreshResponse(accessToken));
        })
        .AllowAnonymous()
        .Produces<RefreshResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/resend-confirmation", async (
            ResendConfirmationRequest request,
            IUserService userService,
            IEmailConfirmationTokenService tokenService,
            IEmailService emailService,
            IConfiguration config,
            CancellationToken ct) =>
        {
            var baseUrl = config["App:BaseUrl"]?.TrimEnd('/');
            if (string.IsNullOrWhiteSpace(baseUrl))
                return Results.Problem(
                    detail: "App:BaseUrl no está configurado.",
                    statusCode: StatusCodes.Status503ServiceUnavailable);

            await userService.ResendConfirmationAsync(
                request.Email, tokenService, emailService, baseUrl, ct);
            return Results.Ok(new { message = "Si el email existe, recibirás un enlace de confirmación." });
        })
        .AllowAnonymous()
        .Produces(StatusCodes.Status200OK);

        group.MapPost("/logout", async (
            IAuthService authService,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var rawToken = ctx.Request.Cookies[RefreshTokenCookie];
            if (!string.IsNullOrEmpty(rawToken))
                await authService.LogoutAsync(rawToken, ct);

            ctx.Response.Cookies.Delete(RefreshTokenCookie);
            ctx.Response.Cookies.Delete(SessionIndicatorCookie);
            return Results.NoContent();
        })
        .AllowAnonymous()
        .WithTags("Auth")
        .Produces(StatusCodes.Status204NoContent);

        app.MapPasswordReset();
        return app;
    }

    public static IEndpointRouteBuilder MapPasswordReset(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth").WithTags("Auth");

        group.MapPost("/forgot-password", async (
            ForgotPasswordRequest request,
            AppDbContext db,
            IEmailEncryptor emailEncryptor,
            IPasswordResetTokenService tokenService,
            IEmailService emailService,
            IConfiguration config,
            ILogger<Program> logger,
            CancellationToken ct) =>
        {
            var normalizedEmail = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
            if (!string.IsNullOrWhiteSpace(normalizedEmail))
            {
                try
                {
                    var baseUrl = config["App:BaseUrl"]?.TrimEnd('/');
                    if (!string.IsNullOrWhiteSpace(baseUrl))
                    {
                        var encryptedEmail = emailEncryptor.Encrypt(normalizedEmail);
                        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == encryptedEmail, ct);

                        if (user is { EmailConfirmedAt: not null, IsActive: true })
                        {
                            var token = tokenService.GenerateToken(user.Id, user.PasswordHash);
                            var resetUrl = $"{baseUrl}/api/v1/auth/reset-password-redirect?token={Uri.EscapeDataString(token)}";
                            await emailService.SendPasswordResetAsync(emailEncryptor.Decrypt(user.Email), resetUrl, ct);
                        }
                    }
                    else
                    {
                        logger.LogWarning("App:BaseUrl no está configurado — reset email no enviado.");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error en forgot-password.");
                }
            }

            return Results.Ok(new { message = "Si ese email está registrado, recibirás un enlace." });
        })
        .AllowAnonymous()
        .Produces(StatusCodes.Status200OK);

        group.MapPost("/reset-password", async (
            ResetPasswordRequest request,
            AppDbContext db,
            IUserService userService,
            IPasswordResetTokenService tokenService,
            CancellationToken ct) =>
        {
            var userId = tokenService.TryDecodeUserId(request.Token ?? string.Empty);
            if (userId is null)
                return Results.BadRequest(new { code = "token_invalid" });

            var user = await db.Users.FindAsync([userId.Value], ct);
            if (user is null)
                return Results.BadRequest(new { code = "token_invalid" });

            var result = tokenService.ValidateToken(request.Token ?? string.Empty, user.PasswordHash);
            if (result is PasswordResetTokenValidationResult.Invalid or PasswordResetTokenValidationResult.Expired)
                return Results.BadRequest(new { code = "token_invalid" });

            try
            {
                await userService.ResetPasswordAsync(userId.Value, request.NewPassword ?? string.Empty, ct);
            }
            catch (InvalidUserDataException ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
            catch (UserNotFoundException)
            {
                return Results.BadRequest(new { code = "token_invalid" });
            }
            catch (DbUpdateException)
            {
                return Results.Problem(
                    detail: "No se pudo actualizar la contraseña. Por favor, intenta de nuevo.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }

            return Results.Ok(new { message = "Contraseña actualizada." });
        })
        .AllowAnonymous()
        .Produces(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/reset-password-redirect", async (
            string? token,
            AppDbContext db,
            IPasswordResetTokenService tokenService,
            IConfiguration config,
            ILogger<Program> logger,
            CancellationToken ct) =>
        {
            var baseUrl = config["App:BaseUrl"]?.TrimEnd('/');
            var prefix = string.IsNullOrWhiteSpace(baseUrl) ? string.Empty : baseUrl;
            var errorUrl = $"{prefix}/nueva-contrasena?status=invalid";

            try
            {
                var userId = tokenService.TryDecodeUserId(token ?? string.Empty);
                if (userId is null)
                    return Results.Redirect(errorUrl);

                var user = await db.Users.FindAsync([userId.Value], ct);
                if (user is null)
                    return Results.Redirect(errorUrl);

                var result = tokenService.ValidateToken(token ?? string.Empty, user.PasswordHash);
                return result switch
                {
                    PasswordResetTokenValidationResult.Valid =>
                        Results.Redirect($"{prefix}/nueva-contrasena?token={Uri.EscapeDataString(token ?? string.Empty)}"),
                    PasswordResetTokenValidationResult.Expired =>
                        Results.Redirect($"{prefix}/nueva-contrasena?status=expired"),
                    _ => Results.Redirect(errorUrl),
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error en reset-password-redirect — redirigiendo a error.");
                return Results.Redirect(errorUrl);
            }
        })
        .AllowAnonymous()
        .Produces(StatusCodes.Status302Found);

        return app;
    }

    private sealed record ForgotPasswordRequest(string Email);
    private sealed record ResetPasswordRequest(string Token, string NewPassword);

    private static void SetRefreshCookie(HttpContext ctx, string token)
    {
        var expiry = DateTimeOffset.UtcNow.AddDays(7);
        ctx.Response.Cookies.Append(RefreshTokenCookie, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = ctx.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Expires = expiry,
        });
        ctx.Response.Cookies.Append(SessionIndicatorCookie, "1", new CookieOptions
        {
            HttpOnly = false,
            Secure = ctx.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Expires = expiry,
        });
    }
}
