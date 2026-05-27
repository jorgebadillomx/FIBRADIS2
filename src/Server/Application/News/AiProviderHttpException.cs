namespace Application.News;

public sealed class AiProviderHttpException(string provider, string model, int statusCode, string responseBody)
    : Exception($"{provider} respondió con HTTP {statusCode} para modelo {model}.")
{
    public string Provider { get; } = provider;
    public string Model { get; } = model;
    public int StatusCode { get; } = statusCode;
    public string ResponseBody { get; } = responseBody;
}
