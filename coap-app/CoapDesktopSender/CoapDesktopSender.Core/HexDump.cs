using System;
using System.Text;

namespace CoapDesktopSender.Core;

public static class HexDump
{
    public static string Dump(byte[] data, int bytesPerLine = 16)
    {
        if (data == null || data.Length == 0)
            return "(empty)";

        var sb = new StringBuilder();
        for (int i = 0; i < data.Length; i += bytesPerLine)
        {
            sb.Append(i.ToString("X6")).Append("  ");

            // HEX
            for (int j = 0; j < bytesPerLine; j++)
            {
                if (i + j < data.Length)
                    sb.Append(data[i + j].ToString("X2")).Append(' ');
                else
                    sb.Append("   ");
            }

            sb.Append(" ");

            // ASCII
            for (int j = 0; j < bytesPerLine && i + j < data.Length; j++)
            {
                byte b = data[i + j];
                sb.Append(b >= 0x20 && b <= 0x7E ? (char)b : '.');
            }

            sb.AppendLine();
        }
        return sb.ToString();
    }
}
