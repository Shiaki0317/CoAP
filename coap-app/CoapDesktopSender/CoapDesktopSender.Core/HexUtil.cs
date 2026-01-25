using System.Globalization;
using System.Text;

namespace CoapDesktopSender.Core;

public static class HexUtil
{
    public static byte[] ParseHex(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return Array.Empty<byte>();

        // 16進文字だけ抽出（"0x", "," , 空白, 改行など許容）
        var sb = new StringBuilder(s.Length);
        foreach (var ch in s)
        {
            if (Uri.IsHexDigit(ch))
                sb.Append(ch);
        }

        var hex = sb.ToString();
        if (hex.Length == 0) return Array.Empty<byte>();
        if (hex.Length % 2 != 0)
            throw new FormatException("Hex length must be even (after stripping separators).");

        var bytes = new byte[hex.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
        {
            var b = hex.Substring(i * 2, 2);
            bytes[i] = byte.Parse(b, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }
        return bytes;
    }

    public static string ToHex(byte[] bytes)
    {
        var sb = new StringBuilder(bytes.Length * 3);
        for (int i = 0; i < bytes.Length; i++)
        {
            if (i > 0) sb.Append(' ');
            sb.Append(bytes[i].ToString("X2"));
        }
        return sb.ToString();
    }
}
