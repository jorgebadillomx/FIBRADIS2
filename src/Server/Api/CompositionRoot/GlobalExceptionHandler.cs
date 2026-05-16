using Domain.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Api.CompositionRoot;

public class GlobalExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        if (exception is not DomainException domainEx)
            return false;

        httpContext.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;

        await problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails =
            {
                Status = StatusCodes.Status422UnprocessableEntity,
                Title = domainEx.Message,
                Extensions = { ["domainCode"] = domainEx.DomainCode },
            },
            Exception = exception,
        });

        return true;
    }
}
