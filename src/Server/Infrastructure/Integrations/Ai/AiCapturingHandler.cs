using System.Text;

namespace Infrastructure.Integrations.Ai;

public class AiCapturingHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var capture = AiCallRawData.Current;

        if (capture is not null && request.Content is not null)
        {
            var reqBody = await request.Content.ReadAsStringAsync(ct);
            capture.RequestBody = reqBody;
            var mediaType = request.Content.Headers.ContentType?.MediaType ?? "application/json";
            request.Content = new StringContent(reqBody, Encoding.UTF8, mediaType);
        }

        var response = await base.SendAsync(request, ct);

        if (capture is not null)
        {
            var resBody = await response.Content.ReadAsStringAsync(ct);
            capture.ResponseBody = resBody;
            var mediaType = response.Content.Headers.ContentType?.MediaType ?? "application/json";
            response.Content = new StringContent(resBody, Encoding.UTF8, mediaType);
        }

        return response;
    }
}
