using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoapDesktopSender.Core;

public static class CoapMessageFormat
{
    public static string FormatRequestSummary(dynamic req)
    {
        // CoAP.NET の Request/Message のプロパティ名が多少違っても落ちにくいよう dynamic で吸収
        string? type = SafeToString(() => req.Type);
        string? code = SafeToString(() => req.Code);
        string? id = SafeToString(() => req.ID);
        string token = FormatToken(SafeGet<byte[]>(() => req.Token));
        return $"REQ  Type={type} Code={code} MID={id} Token={token}";
    }

    public static string FormatResponseSummary(dynamic res)
    {
        string? type = SafeToString(() => res.Type);
        string? code = SafeToString(() => res.StatusCode ?? res.Code);
        string? id = SafeToString(() => res.ID);
        string token = FormatToken(SafeGet<byte[]>(() => res.Token));
        int len = SafeGet<byte[]>(() => res.Payload)?.Length ?? 0;
        return $"RES  Type={type} Code={code} MID={id} Token={token} PayloadLen={len}";
    }

    public static string FormatOptions(dynamic msg)
    {
        // CoAP.NET 系はだいたい msg.Options が列挙可能
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine("Options:");
            foreach (var opt in (IEnumerable<dynamic>)msg.Options)
            {
                // opt.Type / opt.Number / opt.Name / opt.StringValue / opt.RawValue など実装差があるので吸収
                string name = SafeToString(() => opt.Name)
                              ?? SafeToString(() => opt.Type)
                              ?? SafeToString(() => opt.Number)
                              ?? "(unknown)";
                string? sval = SafeToString(() => opt.StringValue);
                byte[]? raw = SafeGet<byte[]>(() => opt.RawValue) ?? SafeGet<byte[]>(() => opt.Value);

                if (!string.IsNullOrEmpty(sval))
                    sb.AppendLine($" - {name}: \"{sval}\"");
                else if (raw is { Length: > 0 })
                    sb.AppendLine($" - {name}: 0x{Convert.ToHexString(raw)}");
                else
                    sb.AppendLine($" - {name}");
            }
            return sb.ToString().TrimEnd();
        }
        catch
        {
            return "Options: (unavailable)";
        }
    }

    private static string FormatToken(byte[]? token)
        => token is null || token.Length == 0 ? "(none)" : Convert.ToHexString(token);

    private static string? SafeToString(Func<object?> getter)
    {
        try { return getter()?.ToString(); } catch { return null; }
    }

    private static T? SafeGet<T>(Func<T?> getter)
    {
        try { return getter(); } catch { return default; }
    }
}
