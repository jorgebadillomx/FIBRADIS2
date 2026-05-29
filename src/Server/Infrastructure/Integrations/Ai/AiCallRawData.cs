namespace Infrastructure.Integrations.Ai;

public sealed class AiCallRawData
{
    public string? RequestBody { get; set; }
    public string? ResponseBody { get; set; }

    private static readonly AsyncLocal<AiCallRawData?> Ambient = new();

    public static AiCallRawData Begin()
    {
        var data = new AiCallRawData();
        Ambient.Value = data;
        return data;
    }

    public static AiCallRawData? Current => Ambient.Value;
}
