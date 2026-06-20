using Application.Auth;
using Application.Email;
using Domain.Auth.Exceptions;
using Microsoft.IdentityModel.JsonWebTokens;
using SharedApiContracts.Auth;
using System.Security.Claims;

namespace Api.Endpoints.Private;

public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccount(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/account/me", async (
            IUserService svc,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var sub = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? ctx.User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (!Guid.TryParse(sub, out var userId))
                return Results.Unauthorized();

            try
            {
                var profile = await svc.GetProfileAsync(userId, ct);
                return Results.Ok(new UserProfileResponse(
                    profile.Email,
                    profile.Role,
                    profile.Apodo,
                    profile.IsActive,
                    profile.TrialEndsAt.HasValue
                        ? DateTime.SpecifyKind(profile.TrialEndsAt.Value, DateTimeKind.Utc).ToString("O")
                        : null,
                    profile.FechaPago.HasValue
                        ? DateTime.SpecifyKind(profile.FechaPago.Value, DateTimeKind.Utc).ToString("O")
                        : null,
                    profile.SubscriptionType,
                    profile.SubscriptionEndsAt.HasValue
                        ? DateTime.SpecifyKind(profile.SubscriptionEndsAt.Value, DateTimeKind.Utc).ToString("O")
                        : null));
            }
            catch (UserNotFoundException)
            {
                return Results.Unauthorized();
            }
        })
        .RequireAuthorization()
        .WithTags("Account")
        .Produces<UserProfileResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        app.MapPatch("/api/v1/account/me", async (
            UpdateApodoRequest req,
            IUserService svc,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var sub = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? ctx.User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (!Guid.TryParse(sub, out var userId))
                return Results.Unauthorized();

            if (req.Apodo is not null && req.Apodo.Length > 50)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["apodo"] = ["El apodo no puede tener más de 50 caracteres."],
                });
            }

            try
            {
                await svc.UpdateApodoAsync(userId, req.Apodo, ct);
            }
            catch (InvalidUserDataException ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
            catch (UserNotFoundException)
            {
                return Results.Unauthorized();
            }

            return Results.NoContent();
        })
        .RequireAuthorization()
        .WithTags("Account")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        app.MapPatch("/api/v1/account/password", async (
            ChangeOwnPasswordRequest req,
            IUserService svc,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var sub = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? ctx.User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (!Guid.TryParse(sub, out var userId))
                return Results.Unauthorized();

            try
            {
                await svc.ChangeOwnPasswordAsync(userId, req.CurrentPassword, req.NewPassword, ct);
            }
            catch (InvalidCredentialsException)
            {
                return Results.Problem(
                    detail: "Contraseña actual incorrecta",
                    statusCode: StatusCodes.Status401Unauthorized);
            }
            catch (InvalidUserDataException ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
            catch (UserNotFoundException)
            {
                return Results.Unauthorized();
            }

            return Results.NoContent();
        })
        .RequireAuthorization()
        .WithTags("Account")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        app.MapPost("/api/v1/account/accept-terms", async (
            IUserService svc,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var sub = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? ctx.User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (!Guid.TryParse(sub, out var userId))
                return Results.Unauthorized();

            try
            {
                await svc.AcceptTermsAsync(userId, ct);
            }
            catch (UserNotFoundException)
            {
                return Results.Unauthorized();
            }

            return Results.NoContent();
        })
        .RequireAuthorization()
        .WithTags("Account")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        app.MapPost("/api/v1/account/notify-payment", async (
            IFormFile? comprobante,
            IEmailService emailService,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var sub = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? ctx.User.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (!Guid.TryParse(sub, out var userId))
                return Results.Unauthorized();

            byte[]? fileBytes = null;
            string? fileName = null;

            if (comprobante is not null)
            {
                var contentType = comprobante.ContentType ?? "";
                var typeOk = contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
                          || contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase);
                if (!typeOk)
                {
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                        { ["comprobante"] = ["Solo se aceptan imágenes y PDF."] });
                }

                using var ms = new MemoryStream();
                await comprobante.CopyToAsync(ms, ct);
                fileBytes = ms.ToArray();

                if (ms.Length == 0)
                {
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                        { ["comprobante"] = ["El archivo está vacío."] });
                }

                if (ms.Length > 5_242_880)
                {
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                        { ["comprobante"] = ["El archivo supera el límite de 5 MB."] });
                }

                fileName = Path.GetFileName(comprobante.FileName ?? string.Empty);
            }

            var userEmail = ctx.User.FindFirstValue(JwtRegisteredClaimNames.Email) ?? "";
            await emailService.SendPaymentNotificationAsync(userId, userEmail, fileBytes, fileName, ct);

            return Results.NoContent();
        })
        .RequireAuthorization()
        .DisableAntiforgery()
        .WithTags("Account")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        return app;
    }
}
