namespace CoapDesktopSender.Core;

public static class CoapFormatMapper
{
    // よく使うものだけ
    public static int ToContentFormatId(string? name) => (name ?? "").Trim().ToLowerInvariant() switch
    {
        "text/plain" => 0,
        "application/octet-stream" => 42,
        "application/cbor" => 60,
        _ => throw new NotSupportedException($"Unsupported format: {name}")
    };
}
