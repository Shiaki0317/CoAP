using System;
using PeterO.Cbor;

namespace CoapDesktopSender.Core;

public static class CborPrettyPrinter
{
    /// <summary>
    /// CBOR bytes -> human-readable (diagnostic-like) string.
    /// - Decode失敗しても raw hex を返す
    /// - Map/Array なども見やすい文字列化（PeterOの ToString は概ね診断表記）
    /// </summary>
    public static string PrettyPrint(byte[] cbor)
    {
        if (cbor is null || cbor.Length == 0)
            return "(empty)";

        try
        {
            // 1) Decode
            var obj = CBORObject.DecodeFromBytes(cbor);

            // 2) Pretty
            // PeterO.Cbor の ToString() は CBOR diagnostic に近い表現を返す
            // 例: {"a":1,"b":"x"} / [1,2,3] / h'010203'
            // もし整形を強めたければここでインデント付きJSON化も可能
            return obj.ToString();
        }
        catch (Exception ex)
        {
            return $"(CBOR decode failed) {ex.GetType().Name}: {ex.Message}\nRaw: 0x{Convert.ToHexString(cbor)}";
        }
    }

    /// <summary>
    /// “ツリー表示”用途で、JSONへ寄せた整形も欲しい場合のオプション。
    /// Map/Array中心のCBORなら、ある程度見やすいJSONになります。
    /// （タグやバイト列などは表現が変わる点に注意）
    /// </summary>
    public static string PrettyPrintAsJson(byte[] cbor, bool indent = true)
    {
        if (cbor is null || cbor.Length == 0)
            return "(empty)";

        try
        {
            var obj = CBORObject.DecodeFromBytes(cbor);

            // JSONへ（必要に応じて）
            // indent=true の整形は PeterO 側には無いので、単純に JSON 文字列を返す
            // ※ インデントが必要なら UI 側で整形するか Newtonsoft.Json 等を追加
            var json = obj.ToJSONString();
            return json;
        }
        catch (Exception ex)
        {
            return $"(CBOR decode failed) {ex.GetType().Name}: {ex.Message}\nRaw: 0x{Convert.ToHexString(cbor)}";
        }
    }
}
