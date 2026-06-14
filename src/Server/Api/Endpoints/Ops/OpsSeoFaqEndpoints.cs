using System.Security.Claims;
using Application.Auth;
using Application.Ops;
using Application.Seo;
using Domain.Seo;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SharedApiContracts.Seo;

namespace Api.Endpoints.Ops;

public static class OpsSeoFaqEndpoints
{
    private const int MaxQuestionLength = 256;
    private const int MaxAnswerLength = 20_000;
    private const int MaxEntityKeyLength = 256;

    public static IEndpointRouteBuilder MapOpsSeoFaq(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/ops/seo/faq")
            .RequireAuthorization("AdminOps")
            .WithTags("OpsSeo");

        group.MapGet("", async (
            string? pageType,
            string? entityKey,
            IFaqRepository repo,
            CancellationToken ct) =>
        {
            if (!TryParsePageType(pageType, out var parsedPageType))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["pageType"] = ["pageType es requerido y debe ser un valor válido de SeoPageType."],
                });
            }

            if (string.IsNullOrWhiteSpace(entityKey))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["entityKey"] = ["entityKey es requerido."],
                });
            }

            var items = await repo.GetByPageAsync(parsedPageType, entityKey, includeInactive: true, ct);
            return Results.Ok(items.Select(ToDto).ToList());
        })
        .Produces<IReadOnlyList<FaqItemDto>>(StatusCodes.Status200OK)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPost("", async (
            UpsertFaqItemRequest request,
            IFaqRepository repo,
            IEmailEncryptor emailEncryptor,
            ILoggerFactory loggerFactory,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger("OpsSeoFaqEndpoints");
            var validation = ValidateRequest(request);
            if (validation.Count > 0)
                return Results.ValidationProblem(validation);

            if (!TryParsePageType(request.PageType, out var pageType))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["pageType"] = ["pageType es requerido y debe ser un valor válido de SeoPageType."],
                });
            }

            var entityKey = NormalizeEntityKey(request.EntityKey);
            if (await repo.ExistsAsync(pageType, entityKey, request.Question, ct))
            {
                return Results.Problem(
                    title: "FAQ duplicada",
                    detail: "Ya existe una FAQ con la misma pregunta para esa página.",
                    statusCode: StatusCodes.Status409Conflict,
                    extensions: new Dictionary<string, object?> { ["domainCode"] = "FAQ_ALREADY_EXISTS" });
            }

            var item = new FaqItem
            {
                Id = Guid.NewGuid(),
                PageType = pageType,
                EntityKey = entityKey,
                Question = request.Question.Trim(),
                Answer = request.Answer.Trim(),
                Order = request.Order,
                IsActive = request.IsActive,
                UpdatedAt = DateTimeOffset.UtcNow,
                UpdatedBy = GetActor(ctx, emailEncryptor, logger),
            };

            try
            {
                await repo.AddAsync(item, ct);
            }
            catch (DbUpdateException)
            {
                return Results.Problem(
                    title: "FAQ duplicada",
                    detail: "Ya existe una FAQ con la misma pregunta para esa página.",
                    statusCode: StatusCodes.Status409Conflict,
                    extensions: new Dictionary<string, object?> { ["domainCode"] = "FAQ_ALREADY_EXISTS" });
            }

            logger.LogInformation(
                "Ops {Action} FAQ {FaqId} by {Actor} at {Timestamp}",
                "CREATE",
                item.Id,
                item.UpdatedBy,
                item.UpdatedAt);

            return Results.Created($"/api/v1/ops/seo/faq/{item.Id}", ToDto(item));
        })
        .Produces<FaqItemDto>(StatusCodes.Status201Created)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}", async (
            Guid id,
            UpsertFaqItemRequest request,
            IFaqRepository repo,
            IEmailEncryptor emailEncryptor,
            ILoggerFactory loggerFactory,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger("OpsSeoFaqEndpoints");
            var validation = ValidateRequest(request);
            if (validation.Count > 0)
                return Results.ValidationProblem(validation);

            if (!TryParsePageType(request.PageType, out var pageType))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["pageType"] = ["pageType es requerido y debe ser un valor válido de SeoPageType."],
                });
            }

            var entityKey = NormalizeEntityKey(request.EntityKey);
            var current = await repo.GetByIdAsync(id, ct);
            if (current is null)
                return Results.NotFound();

            var collision = await repo.GetByNaturalKeyAsync(pageType, entityKey, request.Question, ct);
            if (collision is not null && collision.Id != id)
            {
                return Results.Problem(
                    title: "FAQ duplicada",
                    detail: "Ya existe una FAQ con la misma pregunta para esa página.",
                    statusCode: StatusCodes.Status409Conflict,
                    extensions: new Dictionary<string, object?> { ["domainCode"] = "FAQ_ALREADY_EXISTS" });
            }

            current.PageType = pageType;
            current.EntityKey = entityKey;
            current.Question = request.Question.Trim();
            current.Answer = request.Answer.Trim();
            current.Order = request.Order;
            current.IsActive = request.IsActive;
            current.UpdatedAt = DateTimeOffset.UtcNow;
            current.UpdatedBy = GetActor(ctx, emailEncryptor, logger);

            try
            {
                await repo.UpdateAsync(current, ct);
            }
            catch (DbUpdateException)
            {
                return Results.Problem(
                    title: "FAQ duplicada",
                    detail: "Ya existe una FAQ con la misma pregunta para esa página.",
                    statusCode: StatusCodes.Status409Conflict,
                    extensions: new Dictionary<string, object?> { ["domainCode"] = "FAQ_ALREADY_EXISTS" });
            }

            logger.LogInformation(
                "Ops {Action} FAQ {FaqId} by {Actor} at {Timestamp}",
                "UPDATE",
                current.Id,
                current.UpdatedBy,
                current.UpdatedAt);

            return Results.Ok(ToDto(current));
        })
        .Produces<FaqItemDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapDelete("/{id:guid}", async (
            Guid id,
            IFaqRepository repo,
            IEmailEncryptor emailEncryptor,
            ILoggerFactory loggerFactory,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger("OpsSeoFaqEndpoints");
            var current = await repo.GetByIdAsync(id, ct);
            if (current is null)
                return Results.NotFound();

            if (current.IsActive)
            {
                current.IsActive = false;
                current.UpdatedAt = DateTimeOffset.UtcNow;
                current.UpdatedBy = GetActor(ctx, emailEncryptor, logger);
                await repo.UpdateAsync(current, ct);

                logger.LogInformation(
                    "Ops {Action} FAQ {FaqId} by {Actor} at {Timestamp}",
                    "DEACTIVATE",
                    current.Id,
                    current.UpdatedBy,
                    current.UpdatedAt);
            }

            return Results.NoContent();
        })
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPost("/seed", async (
            IEditorialPageRepository editorialRepo,
            IFaqRepository faqRepo,
            IEmailEncryptor emailEncryptor,
            ILoggerFactory loggerFactory,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger("OpsSeoFaqEndpoints");
            var actor = GetActor(ctx, emailEncryptor, logger);

            var editorialPages = await editorialRepo.GetAllAsync(ct);
            var seedItems = FaqSeedFactory.BuildEditorialItems(editorialPages)
                .Concat(FaqSeedFactory.BuildFundamentalsItems())
                .ToList();

            var createdCount = 0;
            var skippedCount = 0;

            foreach (var item in seedItems)
            {
                if (await faqRepo.AddIfMissingAsync(item, ct))
                    createdCount++;
                else
                    skippedCount++;
            }

            logger.LogInformation(
                "Ops {Action} FAQ seed by {Actor} at {Timestamp}: {Created} created, {Skipped} skipped",
                "SEED",
                actor,
                DateTimeOffset.UtcNow,
                createdCount,
                skippedCount);

            return Results.Ok(new FaqSeedResultDto(createdCount, skippedCount));
        })
        .Produces<FaqSeedResultDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .ProducesProblem(StatusCodes.Status403Forbidden);

        return app;
    }

    private static Dictionary<string, string[]> ValidateRequest(UpsertFaqItemRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        AddRequired(errors, "pageType", request.PageType, 16);
        AddRequired(errors, "entityKey", request.EntityKey, MaxEntityKeyLength);
        AddRequired(errors, "question", request.Question, MaxQuestionLength);
        AddRequired(errors, "answer", request.Answer, MaxAnswerLength);

        if (request.Order < 1)
        {
            errors["order"] = ["order debe ser mayor o igual a 1."];
        }

        return errors;
    }

    private static void AddRequired(Dictionary<string, string[]> errors, string field, string value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors[field] = [$"El campo {field} es requerido."];
            return;
        }

        if (value.Trim().Length > maxLength)
        {
            errors[field] = [$"El campo {field} no puede superar {maxLength} caracteres."];
        }
    }

    private static bool TryParsePageType(string? value, out SeoPageType pageType)
    {
        pageType = default;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        return Enum.TryParse(value.Trim(), ignoreCase: true, out pageType) &&
               Enum.IsDefined(pageType);
    }

    private static string NormalizeEntityKey(string entityKey)
    {
        var normalized = entityKey.Trim();
        if (normalized.Length == 0)
            return normalized;

        return normalized == "/" ? "/" : normalized.TrimEnd('/');
    }

    private static FaqItemDto ToDto(FaqItem item) => new(
        item.Id,
        item.PageType.ToString(),
        item.EntityKey,
        item.Question,
        item.Answer,
        item.Order,
        item.IsActive,
        item.UpdatedAt,
        item.UpdatedBy);

    private static string GetActor(HttpContext ctx, IEmailEncryptor emailEncryptor, ILogger logger)
    {
        var actor = ctx.User.Identity?.Name
            ?? ctx.User.FindFirstValue(ClaimTypes.Email)
            ?? ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (actor is null)
        {
            logger.LogWarning("GetActor: no identity claim found in JWT; using 'unknown'");
            return "unknown";
        }

        return emailEncryptor.Decrypt(actor);
    }
}
