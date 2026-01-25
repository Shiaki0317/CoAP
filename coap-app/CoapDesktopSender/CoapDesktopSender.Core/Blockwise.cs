using System;

namespace CoapDesktopSender.Core;

public readonly record struct BlockParam(int Num, bool More, int Szx)
{
    // SZX: 0..6 (16,32,64,128,256,512,1024)
    public int BlockSize => 1 << (Szx + 4); // 2^(SZX+4)
}

public static class Blockwise
{
    public static byte[] Encode(int num, bool more, int szx)
    {
        if (num < 0) throw new ArgumentOutOfRangeException(nameof(num));
        if (szx < 0 || szx > 6) throw new ArgumentOutOfRangeException(nameof(szx));

        uint v = ((uint)num << 4) | (more ? 0x8u : 0u) | (uint)szx;

        // 最小バイト長で network order（big endian）
        if (v <= 0xFF) return new[] { (byte)v };
        if (v <= 0xFFFF) return new[] { (byte)(v >> 8), (byte)v };
        return new[] { (byte)(v >> 16), (byte)(v >> 8), (byte)v };
    }

    public static BlockParam Decode(ReadOnlySpan<byte> raw)
    {
        if (raw.Length == 0) throw new ArgumentException("empty");
        uint v = 0;
        foreach (var b in raw) v = (v << 8) | b;

        int szx = (int)(v & 0x7);
        bool m = (v & 0x8) != 0;
        int num = (int)(v >> 4);
        return new BlockParam(num, m, szx);
    }
}
