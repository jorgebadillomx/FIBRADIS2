using Application.Seo;
using Domain.Seo;
using SharedApiContracts.Seo;

namespace Api.Endpoints.Public;

public static class FaqEndpoints
{
    public static IEndpointRouteBuilder MapFaq(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/faq", async (
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

            var items = await repo.GetByPageAsync(parsedPageType, entityKey, includeInactive: false, ct);
            return Results.Ok(items.Select(ToDto).ToList());
        })
        .AllowAnonymous()
        .WithTags("FAQ")
        .Produces<IReadOnlyList<FaqItemDto>>(StatusCodes.Status200OK)
        .ProducesValidationProblem(StatusCodes.Status400BadRequest);

        return app;
    }

    private static bool TryParsePageType(string? value, out SeoPageType pageType)
    {
        pageType = default;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        return Enum.TryParse(value.Trim(), ignoreCase: true, out pageType) &&
               Enum.IsDefined(pageType);
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
}
