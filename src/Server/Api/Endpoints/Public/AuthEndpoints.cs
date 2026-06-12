using Application.Auth;
using SharedApiContracts.Auth;

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

        return app;
    }

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
