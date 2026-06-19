using Application.Auth;
using SharedApiContracts.Auth;

namespace Api.Endpoints.Ops;

public static class OpsUserEndpoints
{
    public static IEndpointRouteBuilder MapOpsUsers(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/ops/users", async (IUserService svc, CancellationToken ct) =>
        {
            var users = await svc.GetAllUsersAsync(ct);
            return Results.Ok(users.Select(ToDto).ToList());
        })
        .RequireAuthorization("AdminOps")
        .WithTags("Ops")
        .Produces<IReadOnlyList<UserSummaryDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        app.MapPost("/api/v1/ops/users", async (
            CreateUserRequest req,
            IUserService svc,
            CancellationToken ct) =>
        {
            var user = await svc.CreateUserAsync(req.Email, req.Password, req.Role, req.Pago, req.FechaPago, ct);
            var dto = ToDto(user);
            return Results.Created($"/api/v1/ops/users/{dto.Id}", dto);
        })
        .RequireAuthorization("AdminOps")
        .WithTags("Ops")
        .Produces<UserSummaryDto>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        app.MapPatch("/api/v1/ops/users/{id:guid}/active", async (
            Guid id,
            SetUserActiveRequest req,
            IUserService svc,
            CancellationToken ct) =>
        {
            var user = await svc.SetUserActiveAsync(id, req.IsActive, ct);
            return Results.Ok(ToDto(user));
        })
        .RequireAuthorization("AdminOps")
        .WithTags("Ops")
        .Produces<UserSummaryDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound);

        app.MapPatch("/api/v1/ops/users/{id:guid}/password", async (
            Guid id,
            ChangePasswordRequest req,
            IUserService svc,
            CancellationToken ct) =>
        {
            await svc.ChangePasswordAsync(id, req.NewPassword, ct);
            return Results.NoContent();
        })
        .RequireAuthorization("AdminOps")
        .WithTags("Ops")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        app.MapPatch("/api/v1/ops/users/{id:guid}/payment", async (
            Guid id,
            UpdatePaymentRequest req,
            IUserService svc,
            CancellationToken ct) =>
        {
            var user = await svc.UpdatePaymentAsync(id, req.Pago, req.FechaPago, ct);
            return Results.Ok(ToDto(user));
        })
        .RequireAuthorization("AdminOps")
        .WithTags("Ops")
        .Produces<UserSummaryDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound);

        app.MapPatch("/api/v1/ops/users/{id:guid}/subscription", async (
            Guid id,
            UpdateSubscriptionRequest req,
            IUserService svc,
            CancellationToken ct) =>
        {
            var user = await svc.UpdateSubscriptionAsync(id, req.Type, req.StartedAt, req.EndsAt, ct);
            return Results.Ok(ToDto(user));
        })
        .RequireAuthorization("AdminOps")
        .WithTags("Ops")
        .Produces<UserSummaryDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        return app;
    }

    private static UserSummaryDto ToDto(UserData u) =>
        new(
            u.Id,
            u.Email,
            u.Role,
            u.IsActive,
            u.CreatedAt,
            u.Pago,
            u.FechaPago,
            u.SubscriptionType,
            u.SubscriptionStartedAt,
            u.SubscriptionEndsAt,
            u.TrialEndsAt,
            u.EmailConfirmedAt);
}
