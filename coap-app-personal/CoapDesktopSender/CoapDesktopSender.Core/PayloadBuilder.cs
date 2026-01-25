using PeterO.Cbor;
using System.Text;

namespace CoapDesktopSender.Core;

public static class PayloadBuilder
{
    public static byte[]? Build(CoapSendModel m) => m.PayloadMode switch
    {
        PayloadMode.None => null,
        PayloadMode.TextUtf8 => Encoding.UTF8.GetBytes(m.PayloadText ?? ""),
        PayloadMode.HexBinary => HexUtil.ParseHex(m.PayloadHex ?? ""),
        PayloadMode.CborFromJson => BuildCborFromJson(m.PayloadJson ?? ""),
        _ => null
    };

    private static byte[] BuildCborFromJson(string json)
    {
        // JSON -> CBOR
        var obj = CBORObject.FromJSONString(json);
        return obj.EncodeToBytes();
    }
}
