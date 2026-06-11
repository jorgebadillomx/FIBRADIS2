using Microsoft.AspNetCore.Http.Extensions;

namespace Api.Middleware;

public class WwwToNonWwwMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var host = context.Request.Host.Host;
        if (host.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
        {
            var newHost = new HostString(host[4..]);
            var redirectUrl = UriHelper.BuildAbsolute(
                "https",
                newHost,
                context.Request.PathBase,
                context.Request.Path,
                context.Request.QueryString);

            context.Response.StatusCode = StatusCodes.Status301MovedPermanently;
            context.Response.Headers.Location = redirectUrl;
            return;
        }

        await next(context);
    }
}
