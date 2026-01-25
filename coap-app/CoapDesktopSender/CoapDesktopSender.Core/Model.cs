namespace CoapDesktopSender.Core;

public enum PayloadMode
{
    None,
    TextUtf8,
    HexBinary,
    CborFromJson
}

public sealed class CoapSendModel
{
    public string UriText { get; init; } = "";
    public string Method { get; init; } = "POST";

    public PayloadMode PayloadMode { get; init; }

    public string PayloadText { get; init; } = "";
    public string PayloadHex { get; init; } = "";
    public string PayloadJson { get; init; } = "";

    public bool UseCon { get; init; } = true;
    public bool UseObserve { get; init; }

    public bool SetContentFormat { get; init; }
    public string ContentFormatName { get; init; } = "application/octet-stream";

    public bool SetAccept { get; init; }
    public string AcceptFormatName { get; init; } = "application/octet-stream";
}

public sealed class CoapResponseInfo
{
    public string Code { get; init; } = "";
    public byte[]? PayloadBytes { get; init; }
}
