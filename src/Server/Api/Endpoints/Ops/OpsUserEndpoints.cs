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
            var user = await svc.CreateUserAsync(req.Email, req.Password, ct);
            var dto = ToDto(user);
            return Results.Created($"/api/v1/ops/users/{dto.Id}", dto);
        })
        .RequireAuthorization("AdminOps")
        .WithTags("Ops")
        .Produces<UserSummaryDto>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        return app;
    }

    private static UserSummaryDto ToDto(UserData u) =>
        new(u.Id, u.Email, u.Role, u.IsActive, u.CreatedAt);
}
