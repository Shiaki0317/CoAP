using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CoapDesktopSender.Core;

public sealed class CoapSender
{
    public async Task<CoapSendResult> SendAsync(
        Uri uri,
        string method,
        byte[] payload,
        bool confirmable,
        bool enableBlock1,
        int block1Num,
        bool block1More,
        int block1Szx,
        bool enableBlock2,
        int block2Num,
        bool block2More,
        int block2Szx,
        CancellationToken ct)
    {
        var log = new StringBuilder();

        dynamic? lastReq = null;
        dynamic? lastRes = null;
        byte[]? lastResPayload = null;

        dynamic CreateRequest(string m)
        {
            var reqType = FindTypeByFullName("CoAP.Request")
                ?? throw new InvalidOperationException("Cannot find type: CoAP.Request");

            dynamic methodValue = GetCoapMethod(m);

            dynamic req = Activator.CreateInstance(reqType, methodValue)
                ?? throw new InvalidOperationException("Cannot create CoAP.Request instance");

            // URI / Uri 吸収
            if (!TrySet(req, "URI", uri) && !TrySet(req, "Uri", uri))
                throw new InvalidOperationException("Cannot set URI on CoAP request (URI/Uri property not found).");

            // Type(CON/NON)
            try
            {
                dynamic mt = GetMessageType(confirmable ? "CON" : "NON");
                TrySet(req, "Type", mt);
            }
            catch { }

            return req;
        }

        // ---- Block1: payload 分割送信 ----
        if (enableBlock1 &&
            payload.Length > 0 &&
            (method.Equals("POST", StringComparison.OrdinalIgnoreCase) || method.Equals("PUT", StringComparison.OrdinalIgnoreCase)))
        {
            var bp = new BlockParam(block1Num, block1More, block1Szx);
            int blockSize = bp.BlockSize;
            int num = 0;

            for (int offset = 0; offset < payload.Length; offset += blockSize)
            {
                ct.ThrowIfCancellationRequested();

                bool more = (offset + blockSize) < payload.Length;
                int len = Math.Min(blockSize, payload.Length - offset);
                var slice = new byte[len];
                Array.Copy(payload, offset, slice, 0, len);

                dynamic req = CreateRequest(method);
                lastReq = req;

                SetPayloadBytes(req, slice);
                AddOption(req, "Block1", Blockwise.Encode(num, more, block1Szx));

                TryInvoke(req, "Send");

                // Send後にMID/Tokenが確定する実装もあるので、ログ/summaryはSend後に取る
                log.AppendLine($"[TX Block1] num={num} m={(more ? 1 : 0)} szx={block1Szx} size={slice.Length}");
                log.AppendLine(CoapMessageFormat.FormatRequestSummary(req));
                log.AppendLine(CoapMessageFormat.FormatOptions(req));

                dynamic res = await WaitForResponseAsync(req, ct);
                lastRes = res;
                lastResPayload = GetPayloadBytes(res);

                log.AppendLine(CoapMessageFormat.FormatResponseSummary(res));
                log.AppendLine(CoapMessageFormat.FormatOptions(res));

                num++;
            }

            // Block2 追跡
            if (enableBlock2 && lastRes is not null)
            {
                var combined = await ReceiveBlock2Async(uri, lastRes, block2Szx, log, ct);
                return BuildResult(lastReq, lastRes, combined, log);
            }

            return BuildResult(lastReq, lastRes, lastResPayload, log);
        }

        // ---- 通常送信 ----
        {
            dynamic req = CreateRequest(method);
            lastReq = req;

            if (payload.Length > 0)
                SetPayloadBytes(req, payload);

            // Block2 の要求（任意）
            if (enableBlock2)
                AddOption(req, "Block2", Blockwise.Encode(block2Num, false, block2Szx));

            TryInvoke(req, "Send");

            log.AppendLine(CoapMessageFormat.FormatRequestSummary(req));
            log.AppendLine(CoapMessageFormat.FormatOptions(req));

            dynamic res = await WaitForResponseAsync(req, ct);
            lastRes = res;
            lastResPayload = GetPayloadBytes(res);

            log.AppendLine(CoapMessageFormat.FormatResponseSummary(res));
            log.AppendLine(CoapMessageFormat.FormatOptions(res));

            if (enableBlock2)
            {
                var combined = await ReceiveBlock2Async(uri, res, block2Szx, log, ct);
                return BuildResult(lastReq, lastRes, combined, log);
            }

            return BuildResult(lastReq, lastRes, lastResPayload, log);
        }
    }

    // ===== Block2 receive (追いかけ) =====
    private async Task<byte[]?> ReceiveBlock2Async(Uri uri, dynamic firstResponse, int szx, StringBuilder log, CancellationToken ct)
    {
        byte[]? firstPayload = GetPayloadBytes(firstResponse);
        if (firstPayload is null) return null;

        using var ms = new MemoryStream();
        ms.Write(firstPayload, 0, firstPayload.Length);

        while (TryGetOption(firstResponse, "Block2", out byte[] raw))
        {
            BlockParam b = Blockwise.Decode(raw);
            log.AppendLine($"[RX Block2] num={b.Num} m={(b.More ? 1 : 0)} szx={b.Szx} payload={firstPayload.Length}");

            if (!b.More) break;

            int nextNum = b.Num + 1;

            dynamic req = CreateFollowUpGet(uri, nextNum, szx);

            TryInvoke(req, "Send");

            log.AppendLine($"[TX Block2-GET] num={nextNum} szx={szx}");
            log.AppendLine(CoapMessageFormat.FormatRequestSummary(req));
            log.AppendLine(CoapMessageFormat.FormatOptions(req));

            dynamic res = await WaitForResponseAsync(req, ct);

            log.AppendLine(CoapMessageFormat.FormatResponseSummary(res));
            log.AppendLine(CoapMessageFormat.FormatOptions(res));

            var p = GetPayloadBytes(res) ?? Array.Empty<byte>();
            ms.Write(p, 0, p.Length);

            firstResponse = res;
            firstPayload = p;
        }

        return ms.ToArray();

        dynamic CreateFollowUpGet(Uri u, int blockNum, int blockSzx)
        {
            var reqType = FindTypeByFullName("CoAP.Request")
                ?? throw new InvalidOperationException("Cannot find type: CoAP.Request");

            dynamic methodValue = GetCoapMethod("GET");
            dynamic req = Activator.CreateInstance(reqType, methodValue)
                ?? throw new InvalidOperationException("Cannot create CoAP.Request instance");

            if (!TrySet(req, "URI", u) && !TrySet(req, "Uri", u))
                throw new InvalidOperationException("Cannot set URI on CoAP request (URI/Uri property not found).");

            AddOption(req, "Block2", Blockwise.Encode(blockNum, false, blockSzx));
            return req;
        }
    }

    // ===== Result（Request Summary/Options を確実に詰める） =====
    private static CoapSendResult BuildResult(dynamic? request, dynamic? response, byte[]? responsePayload, StringBuilder log)
    {
        string? textLog = null;
        string? cborLog = null;
        string? binaryLog = null;

        if (responsePayload is { Length: > 0 })
        {
            // Text
            try
            {
                var txt = Encoding.UTF8.GetString(responsePayload);
                if (!txt.Contains('\0')) textLog = txt;
            }
            catch { }

            // CBOR
            try { cborLog = CborPrettyPrinter.PrettyPrint(responsePayload); } catch { }

            // Binary
            try { binaryLog = HexDump.Dump(responsePayload); } catch { binaryLog = Convert.ToHexString(responsePayload); }
        }

        return new CoapSendResult(
            Ok: response is not null,
            Log: log.ToString().TrimEnd(),

            TextLog: textLog,
            CborLog: cborLog,
            BinaryLog: binaryLog,

            RequestSummary: request is null ? null : CoapMessageFormat.FormatRequestSummary(request),
            ResponseSummary: response is null ? null : CoapMessageFormat.FormatResponseSummary(response),
            RequestOptions: request is null ? null : CoapMessageFormat.FormatOptions(request),
            ResponseOptions: response is null ? null : CoapMessageFormat.FormatOptions(response)
        );
    }

    // ====== CoAP.NET API吸収（反射ヘルパー） ======

    private static dynamic GetCoapMethod(string method)
    {
        var m = (method ?? "").Trim().ToUpperInvariant();

        var t = FindTypeByFullName("CoAP.Method");
        if (t is not null)
        {
            if (t.IsEnum) return Enum.Parse(t, m, ignoreCase: true);

            var v = t.GetField(m)?.GetValue(null)
                 ?? t.GetProperty(m)?.GetValue(null);
            if (v is not null) return v;
        }

        var alt = FindFirstTypeByNameCandidates(new[]
        {
            "CoAP.MethodType",
            "CoAP.RequestMethod",
            "CoAP.Code"
        });

        if (alt is not null)
        {
            if (alt.IsEnum) return Enum.Parse(alt, m, ignoreCase: true);

            var v = alt.GetField(m)?.GetValue(null)
                 ?? alt.GetProperty(m)?.GetValue(null);
            if (v is not null) return v;
        }

        throw new InvalidOperationException($"Cannot resolve CoAP method type. method='{method}'");
    }

    private static dynamic GetMessageType(string type)
    {
        var tname = (type ?? "").Trim().ToUpperInvariant();

        var t = FindTypeByFullName("CoAP.MessageType");
        if (t is not null)
        {
            if (t.IsEnum) return Enum.Parse(t, tname, ignoreCase: true);

            var v = t.GetField(tname)?.GetValue(null)
                 ?? t.GetProperty(tname)?.GetValue(null);
            if (v is not null) return v;
        }

        var alt = FindFirstTypeByNameCandidates(new[]
        {
            "CoAP.MessageTypes",
            "CoAP.Type"
        });

        if (alt is not null)
        {
            if (alt.IsEnum) return Enum.Parse(alt, tname, ignoreCase: true);

            var v = alt.GetField(tname)?.GetValue(null)
                 ?? alt.GetProperty(tname)?.GetValue(null);
            if (v is not null) return v;
        }

        throw new InvalidOperationException($"Cannot resolve CoAP message type. type='{type}'");
    }

    private static Type? FindTypeByFullName(string fullName)
    {
        // 1) まずロード済みから
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            var t = asm.GetType(fullName, throwOnError: false, ignoreCase: false);
            if (t is not null) return t;
        }

        // 2) 参照アセンブリをロードして探す
        var current = typeof(CoapSender).Assembly;
        foreach (var an in current.GetReferencedAssemblies())
        {
            try
            {
                var asm = Assembly.Load(an);
                var t = asm.GetType(fullName, throwOnError: false, ignoreCase: false);
                if (t is not null) return t;
            }
            catch { }
        }

        // 3) 追加保険
        foreach (var name in new[] { "CoAP", "CoAP.NET", "CoAPNet", "CoAPnet" })
        {
            try
            {
                var asm = Assembly.Load(new AssemblyName(name));
                var t = asm.GetType(fullName, throwOnError: false, ignoreCase: false);
                if (t is not null) return t;
            }
            catch { }
        }

        return null;
    }

    private static Type? FindFirstTypeByNameCandidates(string[] fullNames)
    {
        foreach (var n in fullNames)
        {
            var t = FindTypeByFullName(n);
            if (t is not null) return t;
        }
        return null;
    }

    private static bool TrySet(dynamic obj, string propName, object value)
    {
        var p = obj.GetType().GetProperty(propName);
        if (p is null) return false;
        p.SetValue(obj, value);
        return true;
    }

    private static bool TryInvoke(dynamic obj, string methodName)
    {
        var m = obj.GetType().GetMethod(methodName, Type.EmptyTypes);
        if (m is null) return false;
        m.Invoke(obj, null);
        return true;
    }

    private static byte[]? GetPayloadBytes(dynamic msg)
    {
        try { return (byte[])msg.Payload; } catch { }
        try { return (byte[])msg.PayloadBytes; } catch { }
        return null;
    }

    private static void SetPayloadBytes(dynamic req, byte[] bytes)
    {
        if (TrySet(req, "Payload", bytes)) return;

        var mi = req.GetType().GetMethod("SetPayload", new[] { typeof(byte[]) });
        if (mi is not null) { mi.Invoke(req, new object[] { bytes }); return; }

        throw new InvalidOperationException("Cannot set payload bytes (Payload property / SetPayload(byte[]) not found).");
    }

    private static void AddOption(dynamic msg, string optionName, byte[] raw)
    {
        var optTypeEnum = FindTypeByFullName("CoAP.OptionType")
            ?? throw new InvalidOperationException("Cannot find type: CoAP.OptionType");

        var optEnumVal = optTypeEnum.IsEnum
            ? Enum.Parse(optTypeEnum, optionName, ignoreCase: true)
            : (optTypeEnum.GetField(optionName)?.GetValue(null) ?? optTypeEnum.GetProperty(optionName)?.GetValue(null)
                ?? throw new InvalidOperationException($"Cannot resolve option type: {optionName}"));

        var optClass = FindTypeByFullName("CoAP.Option")
            ?? throw new InvalidOperationException("Cannot find type: CoAP.Option");

        dynamic opt = Activator.CreateInstance(optClass, optEnumVal)
            ?? throw new InvalidOperationException("Cannot create CoAP.Option");

        if (!TrySet(opt, "RawValue", raw) && !TrySet(opt, "Value", raw))
            throw new InvalidOperationException("Cannot set option raw bytes (RawValue/Value not found).");

        try
        {
            msg.Options.Add(opt);
        }
        catch
        {
            var setType = FindTypeByFullName("CoAP.OptionSet");
            if (setType is null)
                throw new InvalidOperationException("Cannot find CoAP.OptionSet and cannot add options.");

            msg.Options = Activator.CreateInstance(setType);
            msg.Options.Add(opt);
        }
    }

    private static bool TryGetOption(dynamic msg, string optionName, out byte[] raw)
    {
        raw = Array.Empty<byte>();
        try
        {
            foreach (var opt in msg.Options)
            {
                string? name = null;
                try { name = (string?)opt.Name; } catch { }
                name ??= opt.Type?.ToString();
                name ??= opt.Number?.ToString();

                if (string.Equals(name, optionName, StringComparison.OrdinalIgnoreCase))
                {
                    try { raw = (byte[])opt.RawValue; return true; } catch { }
                    try { raw = (byte[])opt.Value; return true; } catch { }
                }
            }
        }
        catch { }
        return false;
    }

    private static Task<dynamic> WaitForResponseAsync(dynamic req, CancellationToken ct)
    {
        return Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();

            var t = req.GetType();
            var mi = t.GetMethod("WaitForResponse", Type.EmptyTypes)
                  ?? t.GetMethod("GetResponse", Type.EmptyTypes);

            if (mi is null)
                throw new InvalidOperationException("No WaitForResponse/GetResponse method on CoAP.Request.");

            var res = mi.Invoke(req, null);
            if (res is null) throw new TimeoutException("No response (timeout).");
            return (dynamic)res;
        }, ct);
    }
}
